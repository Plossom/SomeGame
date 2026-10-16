using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The set pieces of a journey race (Play mode): an arch over the road at every section line (SECTION 2,
    /// SECTION 3, FINISH; confetti when you pass under it), level crossings where a little mine train rolls
    /// across now and then (lights blink and barriers drop first; it shoves cars aside; a toy train in the
    /// house), fallen logs across the forest road (behind their ramps) and landmarks with moving parts
    /// (windmill sails). The race's surprise seed makes the timetable the same on every phone online.
    /// </summary>
    public class TrackJourney : MonoBehaviour
    {
        [SerializeField] Track track;
        [SerializeField] Sprite pillarSprite, bannerSprite;
        [SerializeField] Sprite locoSprite, wagonSprite, crossingSprite, barrierSprite, lampSprite;
        [SerializeField] Material railsMaterial;
        [SerializeField] Sprite toyLocoSprite, toyWagonSprite;
        [SerializeField] Material toyRailsMaterial;
        [SerializeField] Sprite logSprite;
        [SerializeField] Sprite windmillTowerSprite, windmillSailsSprite;
        [SerializeField] Sprite cowSprite, fenceSprite, buntingSprite;
        [SerializeField] Sprite carouselBaseSprite, carouselTopSprite, swingHubSprite, swingSeatsSprite, teacupFloorSprite, teacupSprite;
        [SerializeField] Sprite coasterCarSprite;
        [SerializeField] Material coasterRailsMaterial;
        [SerializeField] Material particleMaterial;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] int overheadOrder = 40;
        // Painted on the road: above the road, its lines and the boost pads, below walls and cars.
        const int GroundOrder = -45;

        readonly List<GameObject> _spawned = new();
        readonly List<Arch> _arches = new();
        readonly List<Crossing> _crossings = new();
        readonly List<Transform> _sails = new();
        readonly List<CowView> _cows = new();
        readonly List<(Transform t, float speed)> _spinners = new();
        readonly List<CoasterRun> _coasters = new();
        ParticleSystem _fireworks;
        float _nextFirework;
        readonly List<SomeGame.Car.CarTerrain> _listening = new();
        float _carsCheckedAt = -10f;
        SomeGame.Race.RaceManager _race;

        class Arch
        {
            public float Distance;
            public Transform Banner;
            public ParticleSystem Confetti;
            public float PoppedAt = -10f;
        }

        class CoasterRun
        {
            public TrackPath Path;
            public TrackLayout.Coaster Data;
            public Transform[] Cars, Shadows;
            public float Along;
        }

        class CowView
        {
            public Transform Visual;
            public float Phase, StartledAt = -10f;
            public float Angle, Scale;
        }

        class Crossing
        {
            public TrackLayout.Train Data;
            public Vector2 Centre, Across; // the rails run along Across
            public float Reach, Length;
            public Rigidbody2D[] Cars;
            public SpriteRenderer[] Lamps;
            public Transform[] Barriers;
            public float[] BarrierUp, BarrierDown; // angles
            public float NextAt;
            public int Direction = 1;
            public bool Rang;
        }

        void Start()
        {
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            Build();
            track.Rebuilt += Build;
            if (_race != null) _race.PlayerLapCompleted += OnPlayerSection;
        }

        void OnDestroy()
        {
            if (track != null) track.Rebuilt -= Build;
            if (_race != null) _race.PlayerLapCompleted -= OnPlayerSection;
            foreach (var t in _listening) if (t != null) t.CowHit -= OnCowHit;
        }

        void Build()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear(); _arches.Clear(); _crossings.Clear(); _sails.Clear(); _cows.Clear(); _spinners.Clear(); _coasters.Clear(); _fireworks = null;
            var layout = track.Layout;
            if (layout == null) return;
            if (track.Sections > 1) BuildArches();
            foreach (var train in layout.trains) BuildCrossing(train);
            if (logSprite != null && layout.logJumps != null) foreach (float log in layout.logJumps) BuildLog(log);
            foreach (var prop in layout.props) BuildProp(prop);
            if (fenceSprite != null && layout.fences != null) foreach (var stretch in layout.fences) BuildFence(stretch, fenceSprite, 1.2f, 2.2f, 7);
            if (buntingSprite != null && layout.buntings != null) foreach (var stretch in layout.buntings) BuildFence(stretch, buntingSprite, 1.1f, 3.4f, 8);
            if (layout.coasters != null) foreach (var coaster in layout.coasters) BuildCoaster(coaster);
            if (layout.fireworks && particleMaterial != null) BuildFireworks();
            if (cowSprite != null && layout.cows != null) foreach (var cow in layout.cows) BuildCow(cow);
        }

        // ------------------------------------------------------------------ farm

        // Wooden fence along both sides of the road, a little off the kerb; left open wherever it would stand on
        // another piece of road, in water or right beside a branch's mouth.
        void BuildFence(Vector2 stretch, Sprite sprite, float offset, float thickness, int order)
        {
            var path = track.Path;
            const float Step = 2.6f;
            float span = Mathf.Repeat(stretch.y - stretch.x, path.Length);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2? last = null;
                for (float k = 0f; k <= span; k += Step)
                {
                    float d = path.WrapDistance(stretch.x + k);
                    Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + offset);
                    if (!FenceFits(p)) { last = null; continue; }
                    if (last.HasValue)
                    {
                        Vector2 a = last.Value, along = p - a;
                        if (along.magnitude < Step * 1.6f)
                        {
                            var sr = Sprite(sprite.name, sprite, transform, (a + p) * 0.5f, 1f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg, order);
                            float unit = sprite.bounds.size.x;
                            sr.transform.localScale = new Vector3(along.magnitude / unit * 1.06f, thickness, 1f);
                        }
                    }
                    last = p;
                }
            }
        }

        bool FenceFits(Vector2 p)
        {
            var at = track.Path.Project(p);
            if (Mathf.Abs(at.Lateral) < track.OffRoadAt(at.Distance) + 0.6f) return false; // another stretch of the road
            if (track.WaterDistance(p) < 1.5f) return false;
            if (track.BranchAt(p, out _, out _) >= 0) return false;
            foreach (var branch in track.Layout.branches)
                foreach (float end in new[] { branch.from, branch.to })
                    if (Mathf.Abs(track.Path.DeltaDistance(end, at.Distance)) < 10f) return false;
            foreach (var b in track.Layout.bouncers)
                if ((b.position - p).sqrMagnitude < 25f) return false;
            return true;
        }

        void BuildCow(TrackLayout.Cow cow)
        {
            var path = track.Path;
            Vector2 tangent = path.TangentAt(cow.distance);
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f + cow.angle;
            var root = new GameObject("Cow").transform;
            root.SetParent(transform, false);
            root.position = track.CowPosition(cow);
            _spawned.Add(root.gameObject);
            var sr = Sprite("Visual", cowSprite, root, Vector2.zero, cow.radius * 2.6f, angle, 9);
            _cows.Add(new CowView { Visual = sr.transform, Phase = Random.value * 10f, Angle = angle, Scale = sr.transform.localScale.x });
        }

        void OnCowHit(int index)
        {
            if (index >= 0 && index < _cows.Count) _cows[index].StartledAt = Time.time;
        }

        // Cows breathe slowly; bumped, one jumps up with a start and shakes its head.
        void UpdateCows()
        {
            if (_cows.Count == 0) return;
            if (Time.time - _carsCheckedAt > 1f)
            {
                _carsCheckedAt = Time.time;
                foreach (var t in FindObjectsByType<SomeGame.Car.CarTerrain>())
                    if (!_listening.Contains(t)) { _listening.Add(t); t.CowHit += OnCowHit; }
            }
            foreach (var cow in _cows)
            {
                float since = Time.time - cow.StartledAt;
                float hop = since < 0.7f ? Mathf.Sin(since / 0.7f * Mathf.PI) : 0f;
                float wiggle = since < 1.1f ? Mathf.Sin(since * 30f) * (1f - since / 1.1f) * 14f : 0f;
                float breathe = 1f + Mathf.Sin(Time.time * 1.6f + cow.Phase) * 0.018f;
                cow.Visual.localScale = Vector3.one * (cow.Scale * breathe * (1f + hop * 0.22f));
                cow.Visual.localRotation = Quaternion.Euler(0f, 0f, cow.Angle + wiggle);
            }
        }

        // ------------------------------------------------------------------ section arches

        void BuildArches()
        {
            var path = track.Path;
            int sections = track.Sections;
            for (int k = 0; k < sections; k++)
            {
                float d = track.SectionLineDistance(k);
                Vector2 centre = path.PointAt(d), normal = path.NormalAt(d), tangent = path.TangentAt(d);
                float half = track.OffRoadAt(d) + 1.6f;
                // The banner runs across the road with its words upright for the drivers coming towards it.
                float angle = Mathf.Atan2(-normal.y, -normal.x) * Mathf.Rad2Deg;
                var root = new GameObject(k == 0 ? "FinishArch" : $"SectionArch{k + 1}").transform;
                root.SetParent(transform, false);
                root.position = centre;
                _spawned.Add(root.gameObject);
                // Two stone pillars at the road's edges (beside the road, so no car passes under them).
                foreach (float side in new[] { -1f, 1f })
                    Sprite("Pillar", pillarSprite, root, normal * side * half, 2.6f, angle, GroundOrder + 50);
                // The line is painted on the road, under the cars: a dark band, a checkered stripe, the words.
                var banner = new GameObject("Banner").transform;
                banner.SetParent(root, false);
                banner.localRotation = Quaternion.Euler(0f, 0f, angle);
                var board = Sprite("Board", bannerSprite, banner, Vector2.zero, 1f, 0f, GroundOrder);
                board.color = new Color(0.09f, 0.24f, 0.2f, 0.92f);
                board.transform.localScale = new Vector3(half * 2f / BannerSize, 1.9f / BannerSize, 1f);
                int squares = Mathf.RoundToInt(half * 2f / 0.5f);
                for (int s = 0; s < squares; s++)
                    for (int r = 0; r < 2; r++)
                    {
                        if ((s + r) % 2 == 0) continue;
                        var sq = Sprite("Check", bannerSprite, banner, new Vector2(-half + (s + 0.5f) * (half * 2f / squares), -0.62f - r * 0.24f), 1f, 0f, GroundOrder + 2);
                        sq.color = Color.white;
                        sq.transform.localScale = new Vector3((half * 2f / squares) / BannerSize, 0.24f / BannerSize, 1f);
                    }
                var stripe = Sprite("Stripe", bannerSprite, banner, new Vector2(0f, -0.74f), 1f, 0f, GroundOrder + 1);
                stripe.color = new Color(0.12f, 0.12f, 0.13f);
                stripe.transform.localScale = new Vector3(half * 2f / BannerSize, 0.5f / BannerSize, 1f);
                if (font != null)
                {
                    var text = new GameObject("Words").AddComponent<TextMeshPro>();
                    text.transform.SetParent(banner, false);
                    text.transform.localPosition = new Vector2(0f, 0.22f);
                    text.font = font;
                    text.text = k == 0 ? "FINISH" : $"SECTION {k + 1}";
                    text.fontSize = 7.5f;
                    text.fontStyle = FontStyles.Italic;
                    text.characterSpacing = 8f;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = k == 0 ? new Color(1f, 0.78f, 0.25f) : new Color(0.96f, 0.9f, 0.78f);
                    text.rectTransform.sizeDelta = new Vector2(half * 2f, 1.4f);
                    text.sortingOrder = GroundOrder + 3;
                }
                var confetti = Confetti(centre);
                _arches.Add(new Arch { Distance = d, Banner = banner, Confetti = confetti });
            }
        }

        // The player crossed a section line: the arch they passed under bursts with confetti and bounces.
        void OnPlayerSection(float lapTime, bool newBest)
        {
            if (_arches.Count == 0 || _race == null) return;
            var path = track.Path;
            float d = _race.Player.GetComponent<TrackSensor>().Current.Distance;
            Arch nearest = null;
            float best = float.MaxValue;
            foreach (var a in _arches)
            {
                float gap = Mathf.Abs(path.DeltaDistance(a.Distance, d));
                if (gap < best) { best = gap; nearest = a; }
            }
            if (nearest == null || best > 40f) return;
            nearest.Confetti.Emit(140);
            nearest.PoppedAt = Time.time;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.8f);
        }

        // ------------------------------------------------------------------ level crossings

        void BuildCrossing(TrackLayout.Train data)
        {
            var path = track.Path;
            Vector2 centre = path.PointAt(data.distance), normal = path.NormalAt(data.distance), tangent = path.TangentAt(data.distance);
            var c = new Crossing { Data = data, Centre = centre, Across = normal, Reach = data.reach };
            // The rails: a strip across the road and out into the land on both sides.
            var rails = data.toy && toyRailsMaterial != null ? toyRailsMaterial : railsMaterial;
            if (rails != null)
            {
                var go = new GameObject("Rails", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                _spawned.Add(go);
                var mesh = new Mesh { name = "Rails" };
                Vector2 a = centre - normal * data.reach, b = centre + normal * data.reach, w = tangent * 1.1f;
                mesh.SetVertices(new List<Vector3> { a - w, a + w, b + w, b - w });
                float u = data.reach * 2f / 4f;
                mesh.SetUVs(0, new List<Vector2> { new(0, 0), new(0, 1), new(u, 1), new(u, 0) });
                mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
                mesh.RecalculateBounds();
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = rails;
                r.sortingOrder = -46;
            }
            // Signals and barriers on both sides of the road, before and after the rails.
            float edge = track.OffRoadAt(data.distance) + 1.4f;
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
            var lamps = new List<SpriteRenderer>();
            var barriers = new List<Transform>();
            var ups = new List<float>();
            var downs = new List<float>();
            foreach (float side in new[] { -1f, 1f })
            {
                // Traffic from each direction stops on its own side of the rails.
                Vector2 post = centre + normal * side * edge - tangent * side * 2.4f;
                Sprite("Signal", crossingSprite, transform, post, 2.2f, angle, 8);
                for (int l = 0; l < 2; l++)
                {
                    var lamp = Sprite("Lamp", lampSprite, transform, post + tangent * (l == 0 ? 0.55f : -0.55f) * side, 0.9f, 0f, 9);
                    lamp.color = new Color(1f, 0.2f, 0.15f, 0f);
                    lamps.Add(lamp);
                }
                // The barrier arm pivots at its post and swings down across its half of the road.
                var pivot = new GameObject("BarrierPivot").transform;
                pivot.SetParent(transform, false);
                pivot.position = post;
                _spawned.Add(pivot.gameObject);
                float armLength = edge + 0.4f;
                var arm = Sprite("Barrier", barrierSprite, pivot, Vector2.zero, armLength, 0f, 8);
                arm.transform.localPosition = new Vector3(0f, armLength * 0.5f, 0f);
                float down = Mathf.Atan2(-normal.y * side, -normal.x * side) * Mathf.Rad2Deg - 90f;
                ups.Add(down + 80f * side);
                downs.Add(down);
                pivot.rotation = Quaternion.Euler(0f, 0f, down + 80f * side);
                barriers.Add(pivot);
            }
            c.Lamps = lamps.ToArray();
            c.Barriers = barriers.ToArray();
            c.BarrierUp = ups.ToArray();
            c.BarrierDown = downs.ToArray();
            // The train: an engine and its wagons, solid (they shove cars), parked off-screen until it is due.
            var cars = new List<Rigidbody2D>();
            for (int i = 0; i <= data.wagons; i++)
            {
                var go = new GameObject(i == 0 ? "MineLoco" : $"Wagon{i}");
                go.transform.SetParent(transform, false);
                _spawned.Add(go);
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.useFullKinematicContacts = true;
                go.AddComponent<BoxCollider2D>().size = new Vector2(1.9f, i == 0 ? 3.1f : 2.5f);
                var loco = data.toy && toyLocoSprite != null ? toyLocoSprite : locoSprite;
                var wagon = data.toy && toyWagonSprite != null ? toyWagonSprite : wagonSprite;
                Sprite(i == 0 ? "Loco" : "Wagon", i == 0 ? loco : wagon, go.transform, Vector2.zero, i == 0 ? 3.3f : 2.7f, 0f, 7);
                body.position = centre + normal * (data.reach + 20f);
                cars.Add(body);
            }
            c.Cars = cars.ToArray();
            c.Length = 3.3f + data.wagons * 2.8f;
            c.NextAt = Time.time + 4f + Track.SeedShift(data.distance) * data.period; // the first train's time differs every race
            c.Direction = Track.SeedShift(data.distance + 1f) < 0.5f ? 1 : -1;
            _crossings.Add(c);
        }

        void UpdateCrossing(Crossing c)
        {
            float now = Time.time;
            float travel = c.Reach * 2f + c.Length;
            float duration = travel / c.Data.speed;
            float t = now - c.NextAt; // > 0 while the train runs
            const float warning = 2.6f;
            bool running = t >= 0f && t <= duration;
            // Where the engine is along the rails (from one end to the other).
            float head = -c.Reach - c.Length + t * c.Data.speed;
            // The barriers are down from the warning until the last wagon has passed the road.
            float roadHalf = track.OffRoadAt(c.Data.distance) + 2f;
            float tail = head - c.Length;
            bool closed = (t >= -warning && head < -roadHalf) || (running && tail < roadHalf && head > -roadHalf - 1f);
            for (int i = 0; i < c.Barriers.Length; i++)
            {
                float goal = closed ? c.BarrierDown[i] : c.BarrierUp[i];
                float z = Mathf.MoveTowardsAngle(c.Barriers[i].eulerAngles.z, goal, 70f * Time.deltaTime);
                c.Barriers[i].rotation = Quaternion.Euler(0f, 0f, z);
            }
            bool blink = closed && Mathf.Repeat(now * 2.2f, 1f) < 0.5f;
            for (int i = 0; i < c.Lamps.Length; i++)
            {
                bool on = closed && ((i % 2 == 0) == blink);
                c.Lamps[i].color = new Color(1f, 0.2f, 0.15f, on ? 1f : 0.15f);
            }
            // A ding when it closes, if you are near enough to hear it.
            if (closed && !c.Rang)
            {
                c.Rang = true;
                if (_race != null && Vector2.Distance(_race.Player.transform.position, c.Centre) < 45f)
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.beepHigh, 0.6f);
            }
            if (!closed) c.Rang = false;
        }

        void FixedUpdate()
        {
            float now = Time.time;
            foreach (var c in _crossings)
            {
                float travel = c.Reach * 2f + c.Length;
                float duration = travel / c.Data.speed;
                float t = now - c.NextAt;
                if (t > duration)
                {
                    // The next one comes after a while (a little earlier or later each time), from either side.
                    c.NextAt = now + c.Data.period * Mathf.Lerp(0.7f, 1.3f, Track.SeedShift(c.NextAt));
                    c.Direction = -c.Direction;
                    t = now - c.NextAt;
                }
                float head = -c.Reach - c.Length + Mathf.Max(0f, t) * c.Data.speed;
                if (t < 0f) head = -c.Reach - 40f; // parked away
                Vector2 along = c.Across * c.Direction;
                float angle = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg - 90f;
                float offset = 0f;
                for (int i = 0; i < c.Cars.Length; i++)
                {
                    float at = head - offset;
                    var body = c.Cars[i];
                    Vector2 target = c.Centre + along * at;
                    if (t < 0f || (body.position - target).sqrMagnitude > 100f) body.position = target; // jump into place
                    else body.MovePosition(target);
                    body.MoveRotation(angle);
                    offset += i == 0 ? 3.0f : 2.8f;
                }
            }
        }

        // ------------------------------------------------------------------ forest logs

        // A fallen trunk across the road just past its little ramp (the jump itself is the track's).
        void BuildLog(float distance)
        {
            var path = track.Path;
            Vector2 normal = path.NormalAt(distance);
            float span = (track.OffRoadAt(distance) + 1.5f) * 2f;
            var log = Sprite("ForestLog", logSprite, transform, path.PointAt(distance), span, Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg, 6);
            log.transform.localScale = new Vector3(span / logSprite.bounds.size.x, span / logSprite.bounds.size.x * 1.3f, 1f);
        }

        // ------------------------------------------------------------------ props

        void BuildProp(TrackLayout.Prop prop)
        {
            switch (prop.kind)
            {
                case TrackLayout.PropKind.Windmill:
                    Sprite("WindmillTower", windmillTowerSprite, transform, prop.position, prop.size * 0.45f, 0f, 11);
                    _sails.Add(Sprite("WindmillSails", windmillSailsSprite, transform, prop.position + new Vector2(0f, 0.6f), prop.size, 0f, 12).transform);
                    Solid(prop.position, prop.size * 0.2f);
                    break;
                case TrackLayout.PropKind.Carousel:
                    if (carouselTopSprite == null) return;
                    Sprite("CarouselBase", carouselBaseSprite, transform, prop.position, prop.size, 0f, 10);
                    _spinners.Add((Sprite("Carousel", carouselTopSprite, transform, prop.position, prop.size * 0.92f, 0f, 12).transform, 24f));
                    Solid(prop.position, prop.size * 0.42f);
                    break;
                case TrackLayout.PropKind.SwingRide:
                    if (swingSeatsSprite == null) return;
                    _spinners.Add((Sprite("SwingSeats", swingSeatsSprite, transform, prop.position, prop.size, 0f, 13).transform, -70f));
                    _spinners.Add((Sprite("SwingHub", swingHubSprite, transform, prop.position, prop.size * 0.36f, 0f, 14).transform, -70f));
                    Solid(prop.position, prop.size * 0.16f);
                    break;
                case TrackLayout.PropKind.Teacups:
                    if (teacupSprite == null) return;
                    var floor = Sprite("TeacupFloor", teacupFloorSprite, transform, prop.position, prop.size, 0f, 10).transform;
                    _spinners.Add((floor, 16f));
                    // Three cups riding round on the turning floor, each spinning on its own the other way.
                    float unit = prop.size / Mathf.Max(0.01f, floor.localScale.x);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * Mathf.PI * 2f / 3f + 0.3f;
                        var cup = Sprite("Teacup", teacupSprite, floor, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * unit * 0.27f, 1f, 0f, 12).transform;
                        cup.localScale = Vector3.one * (unit * 0.3f / Mathf.Max(teacupSprite.bounds.size.x, teacupSprite.bounds.size.y));
                        _spinners.Add((cup, -110f - 40f * i));
                    }
                    Solid(prop.position, prop.size * 0.46f);
                    break;
            }
        }

        void Solid(Vector2 at, float radius)
        {
            var collider = new GameObject("PropBase");
            collider.transform.SetParent(transform, false);
            collider.transform.position = at;
            collider.AddComponent<CircleCollider2D>().radius = radius;
            _spawned.Add(collider);
        }

        // ------------------------------------------------------------------ fairground

        // A roller coaster on stilts: its rails (above the cars where they cross the road), their shadow on the
        // ground, stilts beside the road, and a train racing round, faster downhill.
        void BuildCoaster(TrackLayout.Coaster data)
        {
            if (data.points == null || data.points.Length < 3 || coasterRailsMaterial == null) return;
            var loop = new TrackPath(data.points, 1f);
            Strip("CoasterShadow", loop, 2.4f, new Vector2(1.4f, -1.9f), plainShadow: true, order: 1);
            Strip("CoasterRails", loop, 2.4f, Vector2.zero, plainShadow: false, order: 34);
            for (float d = 0f; d < loop.Length; d += 7f)
            {
                Vector2 p = loop.PointAt(d) + new Vector2(0.7f, -0.95f);
                var on = track.Path.Project(p);
                if (Mathf.Abs(on.Lateral) < track.OffRoadAt(on.Distance) + 0.8f) continue; // no stilts on the road
                var post = Sprite("Stilt", bannerSprite, transform, p, 0.7f, 0f, 2);
                post.color = new Color(0.35f, 0.2f, 0.2f, 0.85f);
            }
            var run = new CoasterRun { Path = loop, Data = data, Cars = new Transform[data.cars], Shadows = new Transform[data.cars] };
            for (int i = 0; i < data.cars; i++)
            {
                run.Shadows[i] = Sprite("CoasterCarShadow", coasterCarSprite, transform, Vector2.zero, 2.2f, 0f, 3).transform;
                run.Shadows[i].GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.3f);
                run.Cars[i] = Sprite("CoasterCar", coasterCarSprite, transform, Vector2.zero, 2.2f, 0f, 35).transform;
            }
            _coasters.Add(run);
        }

        void Strip(string name, TrackPath loop, float width, Vector2 offset, bool plainShadow, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            _spawned.Add(go);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var tris = new List<int>();
            int n = Mathf.CeilToInt(loop.Length / 1f);
            for (int i = 0; i <= n; i++)
            {
                float d = loop.Length * i / n;
                Vector2 p = loop.PointAt(d) + offset, side = loop.NormalAt(d) * (width * 0.5f);
                verts.Add(p - side); verts.Add(p + side);
                uvs.Add(new Vector2(d / 2.4f, 0f)); uvs.Add(new Vector2(d / 2.4f, 1f));
                Color c = plainShadow ? new Color(0f, 0f, 0f, 0.22f) : Color.white;
                colors.Add(c); colors.Add(c);
                if (i < n) { int k = i * 2; tris.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 }); }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetColors(colors); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = coasterRailsMaterial; // the shadow: the same rails, tinted dark
            r.sortingOrder = order;
        }

        void UpdateCoasters()
        {
            foreach (var run in _coasters)
            {
                float length = run.Path.Length;
                float wave = Mathf.Sin(run.Along / length * Mathf.PI * 6f);
                run.Along = Mathf.Repeat(run.Along + run.Data.speed * (1f + 0.45f * wave) * Time.deltaTime, length);
                for (int i = 0; i < run.Cars.Length; i++)
                {
                    float d = Mathf.Repeat(run.Along - i * 2.5f, length);
                    Vector2 p = run.Path.PointAt(d), t = run.Path.TangentAt(d);
                    var rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg - 90f);
                    run.Cars[i].SetPositionAndRotation(p, rot);
                    run.Shadows[i].SetPositionAndRotation(p + new Vector2(1.4f, -1.9f), rot);
                }
            }
        }

        // Fireworks: bursts of coloured sparks over the fair, never over the road.
        void BuildFireworks()
        {
            _fireworks = Particles("Fireworks", Vector2.zero, Color.white, 9f, 1.3f, new Vector2(0.6f, 1.1f));
            var main = _fireworks.main;
            main.gravityModifier = 0f;
            var drag = _fireworks.limitVelocityOverLifetime;
            drag.enabled = true; drag.dampen = 0.08f; drag.limit = 2f;
            _fireworks.GetComponent<ParticleSystemRenderer>().sortingOrder = overheadOrder + 5;
            _fireworks.Play();
            _nextFirework = Time.time + 1f;
        }

        static readonly Color[] FireworkColors =
        {
            new(1f, 0.35f, 0.3f), new(1f, 0.85f, 0.3f), new(0.4f, 0.85f, 1f), new(0.75f, 0.45f, 1f), new(0.45f, 1f, 0.55f), new(1f, 0.55f, 0.85f),
        };

        void UpdateFireworks()
        {
            if (_fireworks == null || Time.time < _nextFirework) return;
            _nextFirework = Time.time + Random.Range(0.5f, 1.3f);
            var cam = Camera.main;
            if (cam == null) return;
            // Somewhere on screen, well away from the road, so it never hides a car.
            float h = cam.orthographicSize, w = h * cam.aspect;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 p = (Vector2)cam.transform.position + new Vector2(Random.Range(-w, w), Random.Range(-h, h)) * 0.9f;
                var on = track.Path.Project(p);
                if (Mathf.Abs(on.Lateral) < track.OffRoadAt(on.Distance) + 8f) continue;
                Color color = FireworkColors[Random.Range(0, FireworkColors.Length)];
                var emit = new ParticleSystem.EmitParams { position = p, applyShapeToPosition = false };
                for (int i = 0; i < 48; i++)
                {
                    float a = i / 48f * Mathf.PI * 2f + Random.value * 0.1f;
                    emit.velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(8f, 12f);
                    emit.startColor = i % 4 == 0 ? Color.white : color;
                    emit.startSize = Random.Range(0.6f, 1.1f);
                    emit.startLifetime = Random.Range(0.9f, 1.4f);
                    _fireworks.Emit(emit, 1);
                }
                break;
            }
        }

        // ------------------------------------------------------------------ every frame

        void Update()
        {
            foreach (var s in _sails) s.Rotate(0f, 0f, -38f * Time.deltaTime);
            foreach (var c in _crossings) UpdateCrossing(c);
            UpdateCows();
            UpdateCoasters();
            UpdateFireworks();
            foreach (var (t, speed) in _spinners) if (t != null) t.Rotate(0f, 0f, speed * Time.deltaTime);
            foreach (var a in _arches)
            {
                float k = (Time.time - a.PoppedAt) / 0.5f;
                float bounce = k >= 0f && k < 1f ? Mathf.Sin(k * Mathf.PI) * (1f - k) * 0.18f : 0f;
                a.Banner.localScale = Vector3.one * (1f + bounce);
            }
        }

        // ------------------------------------------------------------------ helpers

        float BannerSize => bannerSprite != null ? bannerSprite.bounds.size.x : 1f;

        SpriteRenderer Sprite(string name, Sprite sprite, Transform parent, Vector2 position, float size, float angle, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (parent == transform) { _spawned.Add(go); go.transform.position = position; }
            else go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null && size != 1f)
                go.transform.localScale = Vector3.one * (size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            return sr;
        }

        ParticleSystem Confetti(Vector2 at)
        {
            var system = Particles("Confetti", at, Color.white, 9f, 1.6f, new Vector2(0.25f, 0.5f));
            var main = system.main;
            main.gravityModifier = 0f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(1f, 0.48f, 0.1f), 0f), new GradientColorKey(new Color(0.24f, 0.49f, 0.85f), 0.25f),
                    new GradientColorKey(new Color(0.95f, 0.76f, 0.19f), 0.5f), new GradientColorKey(new Color(0.91f, 0.44f, 0.69f), 0.75f),
                    new GradientColorKey(new Color(0.17f, 0.7f, 0.64f), 1f),
                },
            }) { mode = ParticleSystemGradientMode.RandomColor };
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 3f;
            shape.rotation = Vector3.zero;
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.08f;
            limit.limit = 1f;
            system.GetComponent<ParticleSystemRenderer>().sortingOrder = overheadOrder + 4;
            return system;
        }

        ParticleSystem Particles(string name, Vector2 position, Color color, float speed, float lifetime, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            _spawned.Add(go);
            go.transform.position = position;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.5f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = 9;
            system.Play();
            return system;
        }
    }
}
