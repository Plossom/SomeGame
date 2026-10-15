using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The set pieces of a journey race (Play mode): an arch over the road at every section line (SECTION 2,
    /// SECTION 3, FINISH; confetti when you pass under it), level crossings where a little mine train rolls
    /// across now and then (lights blink and barriers drop first; it shoves cars aside), the rockslide that
    /// closes one of two ways (a different one each race) and landmarks with moving parts (windmill sails).
    /// The race's surprise seed makes the timetable and the rockslide the same on every phone online.
    /// </summary>
    public class TrackJourney : MonoBehaviour
    {
        [SerializeField] Track track;
        [SerializeField] Sprite pillarSprite, bannerSprite;
        [SerializeField] Sprite locoSprite, wagonSprite, crossingSprite, barrierSprite, lampSprite;
        [SerializeField] Material railsMaterial;
        [SerializeField] Sprite rockPileSprite;
        [SerializeField] Sprite windmillTowerSprite, windmillSailsSprite;
        [SerializeField] Material particleMaterial;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] int overheadOrder = 40;

        readonly List<GameObject> _spawned = new();
        readonly List<Arch> _arches = new();
        readonly List<Crossing> _crossings = new();
        readonly List<Transform> _sails = new();
        SomeGame.Race.RaceManager _race;

        class Arch
        {
            public float Distance;
            public Transform Banner;
            public ParticleSystem Confetti;
            public float PoppedAt = -10f;
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
        }

        void Build()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear(); _arches.Clear(); _crossings.Clear(); _sails.Clear();
            track.ClosedBranch = track.ForcedBranch = -1;
            var layout = track.Layout;
            if (layout == null) return;
            if (track.Sections > 1) BuildArches();
            foreach (var train in layout.trains) BuildCrossing(train);
            foreach (var slide in layout.rockslides) BuildRockslide(slide);
            foreach (var prop in layout.props) BuildProp(prop);
        }

        // ------------------------------------------------------------------ section arches

        void BuildArches()
        {
            var path = track.Path;
            int sections = track.Sections;
            for (int k = 0; k < sections; k++)
            {
                float d = path.Length * k / sections;
                Vector2 centre = path.PointAt(d), normal = path.NormalAt(d), tangent = path.TangentAt(d);
                float half = track.OffRoadAt(d) + 1.6f;
                float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
                var root = new GameObject(k == 0 ? "FinishArch" : $"SectionArch{k + 1}").transform;
                root.SetParent(transform, false);
                root.position = centre;
                _spawned.Add(root.gameObject);
                // Two stone pillars at the road's edges.
                foreach (float side in new[] { -1f, 1f })
                    Sprite("Pillar", pillarSprite, root, normal * side * half, 2.6f, angle, overheadOrder);
                // The banner across: its shadow on the road, the board, a checkered stripe, the words.
                var banner = new GameObject("Banner").transform;
                banner.SetParent(root, false);
                banner.localRotation = Quaternion.Euler(0f, 0f, angle);
                var shadow = Sprite("Shadow", bannerSprite, root, new Vector2(0.7f, -1.1f), 1f, angle, -40);
                shadow.color = new Color(0f, 0f, 0f, 0.22f);
                shadow.transform.localScale = new Vector3(half * 2f / BannerSize, 1.9f / BannerSize, 1f);
                var board = Sprite("Board", bannerSprite, banner, Vector2.zero, 1f, 0f, overheadOrder + 1);
                board.color = new Color(0.09f, 0.24f, 0.2f);
                board.transform.localScale = new Vector3(half * 2f / BannerSize, 1.9f / BannerSize, 1f);
                int squares = Mathf.RoundToInt(half * 2f / 0.5f);
                for (int s = 0; s < squares; s++)
                    for (int r = 0; r < 2; r++)
                    {
                        if ((s + r) % 2 == 0) continue;
                        var sq = Sprite("Check", bannerSprite, banner, new Vector2(-half + (s + 0.5f) * (half * 2f / squares), -0.62f - r * 0.24f), 1f, 0f, overheadOrder + 2);
                        sq.color = Color.white;
                        sq.transform.localScale = new Vector3((half * 2f / squares) / BannerSize, 0.24f / BannerSize, 1f);
                    }
                var stripe = Sprite("Stripe", bannerSprite, banner, new Vector2(0f, -0.74f), 1f, 0f, overheadOrder + 1);
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
                    text.sortingOrder = overheadOrder + 3;
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
            if (railsMaterial != null)
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
                r.sharedMaterial = railsMaterial;
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
                Sprite(i == 0 ? "Loco" : "Wagon", i == 0 ? locoSprite : wagonSprite, go.transform, Vector2.zero, i == 0 ? 3.3f : 2.7f, 0f, 7);
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

        // ------------------------------------------------------------------ rockslide

        void BuildRockslide(TrackLayout.Rockslide slide)
        {
            var branches = track.Branches;
            if (slide.branch < 0 || slide.branch >= branches.Count) return;
            var route = branches[slide.branch];
            bool closeBranch = Track.SeedShift(route.From + 0.5f) < 0.5f;
            Vector2 at;
            float radius;
            if (closeBranch)
            {
                at = route.PointAt(route.Length * 0.4f);
                radius = route.HalfWidth + 1.2f;
                track.ClosedBranch = slide.branch;
            }
            else
            {
                var path = track.Path;
                float mid = route.From + track.Path.DeltaDistance(route.From, route.To) * 0.5f;
                at = path.PointAt(mid);
                radius = track.OffRoadAt(mid) + 1.2f;
                track.ForcedBranch = slide.branch;
            }
            var pile = Sprite("Rockslide", rockPileSprite, transform, at, radius * 2.4f, Random.Range(0f, 360f), 6);
            var wall = new GameObject("RockslideWall");
            wall.transform.SetParent(transform, false);
            wall.transform.position = at;
            wall.AddComponent<CircleCollider2D>().radius = radius;
            _spawned.Add(wall);
            // Dust still drifting off it, and a sign.
            var dust = Particles("SlideDust", at, new Color(0.85f, 0.6f, 0.4f, 0.4f), 0.8f, 2.2f, new Vector2(1.4f, 3f));
            var e = dust.emission; e.rateOverTime = 6f;
            if (font != null)
            {
                var text = new GameObject("RoadClosed").AddComponent<TextMeshPro>();
                text.transform.SetParent(transform, false);
                text.transform.position = at + Vector2.up * (radius + 1.2f);
                _spawned.Add(text.gameObject);
                text.font = font;
                text.text = "ROAD CLOSED";
                text.fontSize = 5f;
                text.fontStyle = FontStyles.Italic;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(1f, 0.78f, 0.25f);
                text.outlineColor = new Color32(23, 61, 51, 255);
                text.outlineWidth = 0.25f;
                text.rectTransform.sizeDelta = new Vector2(12f, 2f);
                text.sortingOrder = overheadOrder - 2;
            }
        }

        // ------------------------------------------------------------------ props

        void BuildProp(TrackLayout.Prop prop)
        {
            if (prop.kind != TrackLayout.PropKind.Windmill) return;
            Sprite("WindmillTower", windmillTowerSprite, transform, prop.position, prop.size * 0.45f, 0f, 11);
            var sails = Sprite("WindmillSails", windmillSailsSprite, transform, prop.position + new Vector2(0f, 0.6f), prop.size, 0f, 12);
            _sails.Add(sails.transform);
            var collider = new GameObject("WindmillBase");
            collider.transform.SetParent(transform, false);
            collider.transform.position = prop.position;
            collider.AddComponent<CircleCollider2D>().radius = prop.size * 0.2f;
            _spawned.Add(collider);
        }

        // ------------------------------------------------------------------ every frame

        void Update()
        {
            foreach (var s in _sails) s.Rotate(0f, 0f, -38f * Time.deltaTime);
            foreach (var c in _crossings) UpdateCrossing(c);
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
