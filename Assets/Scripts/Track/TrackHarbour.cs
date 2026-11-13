using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The harbour's moving set pieces (Water Cup race 2):
    /// - the car ferry berthed across a branch: doors open, you race in over its bow ramp, along its car deck past the
    ///   parked cars and out of its stern (FERRY HOP!); then its horn blows, the doors close and booms drop across the
    ///   branch until it opens again;
    /// - gantry cranes swinging a container across the channel on their hook (it knocks you aside);
    /// - rubber ducks by the hundred bobbing in the channel, scattering as the boats plough through (DUCK HUNT!);
    /// - a cruise ship steaming along beside the channel (a moving wall; its horn shakes the screen);
    /// - fireboats spraying arcs of water across the channel (they shove you aside; drops splash over the screen);
    /// - a seaplane that lands ahead of you, skims along and lifts off again: overtake it while it's on the water
    ///   (BEAT THE PLANE!).
    /// </summary>
    public class TrackHarbour : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;
        [SerializeField] Sprite ferry, ferryDeck, boom, container, duck, cruiseShip, fireboat, waterArc, seaplane, screenDrop, slideTower;

        const int DeckOrder = -43, DuckOrder = 7, ShadowOrder = 6, ShipOrder = 18, FireboatOrder = 19, JetOrder = 22,
                  ContainerOrder = 24, BoomOrder = 25, FerryOrder = 26, RailOrder = 27, PlaneOrder = 28;

        readonly List<GameObject> _parts = new();
        SomeGame.Car.CarMovement[] _cars = { };
        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.CameraRig.CameraShake _shake;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _cranes.Clear(); _ducks.Clear(); _jets.Clear();
            _ferryRoot = null; _ship = null; _plane = null;
            Track.BranchGate = null;
            if (layout.biome != TrackLayout.Biome.Lagoon) return;
            if (layout.ferry != null && layout.ferry.branch >= 0 && layout.ferry.branch < track.Branches.Count) BuildFerry(layout.ferry);
            foreach (var c in layout.cranes) BuildCrane(path, c);
            foreach (var j in layout.fireJets) BuildJet(path, j);
            // The water park's slide tower at the top of each slide.
            if (slideTower != null)
                foreach (var ride in layout.rides)
                    if (ride.type == TrackLayout.RideType.Flume && ride.points != null && ride.points.Length > 0)
                    {
                        Sprite("SlideTower", slideTower, ride.points[0], 7f, 0f, RailOrder);
                    }
            if (Application.isPlaying)
            {
                _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
                _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
                _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
                _shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
                foreach (var basin in layout.duckBasins) BuildDucks(path, basin, layout.ducks / Mathf.Max(1, layout.duckBasins.Count));
                if (layout.cruiseShip != null && layout.cruiseShip.enabled) BuildShip();
                if (layout.seaplane != null && layout.seaplane.enabled) BuildPlane();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Track.BranchGate = null;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 at, float size, float angle, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (plain != null) sr.sharedMaterial = plain;
            if (sprite != null) go.transform.localScale = Vector3.one * (size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            if (parent == null) _parts.Add(go);
            return sr;
        }

        static float Heading(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        // ------------------------------------------------------------------ the ferry

        Transform _ferryRoot;
        int _ferryBranch;
        TrackLayout.Ferry _ferryData;
        readonly List<(Transform t, Collider2D wall, float angleOpen)> _booms = new();
        readonly List<Transform> _doors = new();
        float _ferryAlong, _ferryOpen = 1f;
        bool _ferryWasOpen = true;
        readonly Dictionary<SomeGame.Car.CarMovement, float> _ferryRide = new();

        void BuildFerry(TrackLayout.Ferry data)
        {
            _ferryData = data;
            _ferryBranch = data.branch;
            _booms.Clear(); _doors.Clear();
            var route = track.Branches[data.branch];
            _ferryAlong = route.Length * data.at;
            Vector2 at = route.PointAt(_ferryAlong), dir = (route.PointAt(_ferryAlong + 1f) - route.PointAt(_ferryAlong - 1f)).normalized;
            var root = new GameObject("Ferry") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(transform, false);
            root.transform.position = at;
            root.transform.rotation = Quaternion.Euler(0f, 0f, Heading(dir));
            _parts.Add(root);
            _ferryRoot = root.transform;
            const float length = 34f;
            var deck = Sprite("FerryDeck", ferryDeck, at, length, Heading(dir), DeckOrder, root.transform);
            deck.transform.localPosition = Vector3.zero; deck.transform.localRotation = Quaternion.identity;
            var hull = Sprite("FerryHull", ferry, at, length, Heading(dir), FerryOrder, root.transform);
            hull.transform.localPosition = Vector3.zero; hull.transform.localRotation = Quaternion.identity;
            float deckHalf = 3.2f, hullHalf = 6.6f;
            if (Application.isPlaying)
            {
                // The hull's sides: walls either side of the car deck.
                foreach (float side in new[] { -1f, 1f })
                {
                    var wall = new GameObject("FerrySide", typeof(BoxCollider2D)) { hideFlags = HideFlags.DontSave };
                    wall.transform.SetParent(root.transform, false);
                    wall.transform.localPosition = new Vector3(side * (deckHalf + hullHalf) * 0.5f, 0f, 0f);
                    wall.GetComponent<BoxCollider2D>().size = new Vector2(hullHalf - deckHalf, length * 0.92f);
                }
            }
            // The bow door (it swings shut across the deck) and a boom across the branch before the ferry. Only the way
            // in closes: a boat already aboard can always drive off at the stern.
            foreach (float end in new[] { -1f })
            {
                var door = new GameObject("FerryDoor", typeof(SpriteRenderer)) { hideFlags = HideFlags.DontSave };
                door.transform.SetParent(root.transform, false);
                door.transform.localPosition = new Vector3(0f, end * length * 0.47f, 0f);
                var sr = door.GetComponent<SpriteRenderer>();
                sr.sprite = boom;
                sr.sortingOrder = FerryOrder + 1;
                if (plain != null) sr.sharedMaterial = plain;
                door.transform.localScale = new Vector3(deckHalf * 2.1f / Mathf.Max(0.01f, boom != null ? boom.bounds.size.x : 1f), 2.2f, 1f);
                _doors.Add(door.transform);
                float a = Mathf.Clamp(_ferryAlong + end * (length * 0.5f + 4f), 1f, route.Length - 1f);
                Vector2 p = route.PointAt(a), d2 = (route.PointAt(a + 0.5f) - route.PointAt(a - 0.5f)).normalized, n = new(-d2.y, d2.x);
                float half = route.HalfWidth + 0.8f;
                var pivot = new GameObject("Boom") { hideFlags = HideFlags.DontSave };
                pivot.transform.SetParent(transform, false);
                pivot.transform.position = p - n * half;
                float across = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
                pivot.transform.rotation = Quaternion.Euler(0f, 0f, across);
                _parts.Add(pivot);
                var arm = Sprite("BoomArm", boom, p, half * 2f, 0f, BoomOrder, pivot.transform);
                arm.transform.localPosition = new Vector3(half, 0f, 0f);
                arm.transform.localRotation = Quaternion.identity;
                Collider2D wall = null;
                if (Application.isPlaying)
                {
                    var w = new GameObject("BoomWall", typeof(BoxCollider2D)) { hideFlags = HideFlags.DontSave };
                    w.transform.SetParent(transform, false);
                    w.transform.position = p;
                    w.transform.rotation = Quaternion.Euler(0f, 0f, across);
                    w.GetComponent<BoxCollider2D>().size = new Vector2(half * 2f, 0.6f);
                    _parts.Add(w);
                    wall = w.GetComponent<Collider2D>();
                }
                _booms.Add((pivot.transform, wall, across + 80f));
            }
            // Rivals only head for the ferry if its doors will still be open when they get there.
            Track.BranchGate = i => i != _ferryBranch || FerryOpenFor(2.5f);
        }

        bool FerryOpen => FerryOpenFor(0f);

        /// <summary>Docked with its doors open for at least these many more seconds.</summary>
        bool FerryOpenFor(float seconds)
        {
            if (_ferryData == null) return true;
            float cycle = _ferryData.docked + _ferryData.away;
            return Mathf.Repeat(Track.Clock, cycle) < _ferryData.docked - seconds;
        }

        void UpdateFerry(float dt)
        {
            if (_ferryRoot == null) return;
            bool open = FerryOpen;
            _ferryOpen = Mathf.MoveTowards(_ferryOpen, open ? 1f : 0f, dt / 0.8f);
            if (open != _ferryWasOpen)
            {
                _ferryWasOpen = open;
                if (!open) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.shipHorn, 0.5f);
            }
            // The ship rocks a little; the doors swing; the booms rise and drop.
            _ferryRoot.localScale = Vector3.one * (1f + 0.006f * Mathf.Sin(Time.time * 1.3f));
            foreach (var door in _doors) door.localScale = new Vector3(door.localScale.x, Mathf.Lerp(2.2f, 0.01f, _ferryOpen), 1f);
            foreach (var (t, wall, angleOpen) in _booms)
            {
                float closedAngle = angleOpen - 80f;
                t.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(closedAngle, angleOpen, _ferryOpen));
                if (wall != null) wall.enabled = _ferryOpen < 0.5f;
            }
            // Through the ferry: FERRY HOP!
            if (_race == null || _race.Player == null) return;
            var sensor = _race.Player.GetComponent<TrackSensor>();
            var car = _race.Player.GetComponent<SomeGame.Car.CarMovement>();
            if (sensor == null || car == null) return;
            bool on = sensor.Branch == _ferryBranch && Mathf.Abs(sensor.BranchAlong - _ferryAlong) < 14f;
            if (on && !_ferryRide.ContainsKey(car)) _ferryRide[car] = Time.time;
            if (!on && _ferryRide.TryGetValue(car, out float since))
            {
                _ferryRide.Remove(car);
                if (sensor.Branch == _ferryBranch && sensor.BranchAlong > _ferryAlong && _combo != null)
                {
                    _combo.Announce("FERRY HOP!", new Color(0.35f, 0.85f, 1f));
                    car.GiveBoost(0.5f);
                }
            }
        }

        // ------------------------------------------------------------------ cranes

        readonly List<(TrackLayout.Crane data, Transform box, SpriteRenderer shadow, Rigidbody2D body, Vector2 centre, Vector2 across, float reach, Transform trolley)> _cranes = new();

        void BuildCrane(TrackPath path, TrackLayout.Crane c)
        {
            Vector2 centre = path.PointAt(c.distance), along = path.TangentAt(c.distance), across = path.NormalAt(c.distance);
            float reach = track.OffRoadAt(c.distance) + 7f;
            float angle = Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg;
            // The gantry: two rails across the channel on legs (drawn above everything: it is high up), the trolley.
            var mv = new List<Vector3>(); var mu = new List<Vector2>(); var mt = new List<int>(); var mc = new List<Color32>();
            foreach (float s in new[] { -2.2f, 2.2f })
            {
                TrackMeshes.AddQuad(mv, mu, mt, centre + along * s, across * reach, along * 0.35f, Vector2.one);
                for (int k = 0; k < 4; k++) mc.Add(new Color32(242, 165, 65, 255));
            }
            foreach (float e in new[] { -1f, 1f })
                foreach (float s in new[] { -2.2f, 2.2f })
                {
                    TrackMeshes.AddQuad(mv, mu, mt, centre + across * (reach * e) + along * s, across * 0.7f, along * 0.7f, Vector2.one);
                    for (int k = 0; k < 4; k++) mc.Add(new Color32(60, 64, 70, 255));
                }
            var rails = new GameObject("CraneRails", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            rails.transform.SetParent(transform, false);
            var mf = rails.GetComponent<MeshFilter>();
            var mr = rails.GetComponent<MeshRenderer>();
            mr.sharedMaterial = plain;
            mr.sortingOrder = RailOrder;
            Assign(mf, TrackMeshes.Quads("CraneRails", mv, mu, mt, mc));
            _parts.Add(rails);
            var trolley = Sprite("Trolley", boom, centre, 5.6f, angle + 90f, RailOrder + 1);
            trolley.color = new Color(0.25f, 0.27f, 0.3f);
            var box = Sprite("Container", container, centre, 5.4f, angle - 90f, ContainerOrder);
            var shadow = Sprite("ContainerShadow", container, centre, 5.4f, angle - 90f, ShadowOrder);
            shadow.color = new Color(0f, 0.12f, 0.2f, 0.35f);
            Rigidbody2D body = null;
            if (Application.isPlaying)
            {
                body = box.gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                var col = box.gameObject.AddComponent<BoxCollider2D>();
                col.size = container != null ? (Vector2)container.bounds.size * 0.9f : Vector2.one;
            }
            _cranes.Add((c, box.transform, shadow, body, centre, across, reach, trolley.transform));
        }

        void UpdateCranes()
        {
            float now = Track.Clock;
            foreach (var (data, box, shadow, body, centre, across, reach, trolley) in _cranes)
            {
                // Across and back: a swing over, a pause on each quay.
                float u = Mathf.Repeat((now + data.offset) / data.period, 1f);
                float x = u < 0.4f ? Mathf.SmoothStep(-1f, 1f, u / 0.4f) : u < 0.5f ? 1f : u < 0.9f ? Mathf.SmoothStep(1f, -1f, (u - 0.5f) / 0.4f) : -1f;
                Vector2 p = centre + across * (x * (reach - 2f));
                float sway = Mathf.Sin(now * 2.2f) * 4f * (u < 0.4f || (u > 0.5f && u < 0.9f) ? 1f : 0.3f);
                if (body != null) body.MovePosition(p);
                box.position = p;
                box.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg - 90f + sway);
                shadow.transform.position = p + new Vector2(1.2f, -1.6f);
                shadow.transform.rotation = box.rotation;
                trolley.position = p;
            }
        }

        // ------------------------------------------------------------------ rubber ducks

        readonly List<(Rigidbody2D body, Transform t, float phase, float d, float lat)> _ducks = new();
        float _duckHits, _duckHitAt;

        void BuildDucks(TrackPath path, Vector2 basin, int count)
        {
            if (duck == null) return;
            var random = new System.Random(77);
            float span = Mathf.Repeat(basin.y - basin.x, path.Length);
            for (int i = 0; i < count; i++)
            {
                float d = basin.x + (float)random.NextDouble() * span;
                float w = track.OffRoadAt(d) - 0.6f;
                float lat = ((float)random.NextDouble() * 2f - 1f) * w;
                Vector2 p = path.PointAt(d) + path.NormalAt(d) * lat;
                var sr = Sprite("Duck", duck, p, 0.95f, (float)random.NextDouble() * 360f, DuckOrder);
                var body = sr.gameObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.mass = 0.04f;
                body.linearDamping = 2.5f;
                body.angularDamping = 3f;
                var col = sr.gameObject.AddComponent<CircleCollider2D>();
                col.radius = 0.38f / sr.transform.localScale.x;
                _ducks.Add((body, sr.transform, (float)random.NextDouble() * 6f, d, lat));
            }
        }

        void UpdateDucks(float dt)
        {
            float now = Time.time;
            var path = track.Path;
            SomeGame.Car.CarMovement player = _race != null && _race.Player != null ? _race.Player.GetComponent<SomeGame.Car.CarMovement>() : null;
            foreach (var (body, t, phase, d, lat) in _ducks)
            {
                if (body == null) continue;
                // Bob; drift slowly back towards where they float (so the basin never empties for good).
                t.localScale = Vector3.one * (t.localScale.x) ;
                Vector2 home = path.PointAt(d) + path.NormalAt(d) * lat;
                Vector2 to = home - body.position;
                body.AddForce(to * 0.02f);
                float speed = body.linearVelocity.magnitude;
                if (player != null && speed > 4f && (body.position - player.Body.position).sqrMagnitude < 4f && now - _duckHitAt > 0.05f)
                {
                    _duckHitAt = now;
                    _duckHits = Mathf.Max(0f, _duckHits - 0f) + 1f;
                    if (_duckHits >= 8f && _combo != null) { _combo.Announce("DUCK HUNT!", new Color(1f, 0.85f, 0.2f)); _duckHits = -20f; }
                }
            }
            _duckHits = Mathf.MoveTowards(_duckHits, 0f, dt * 2f);
        }

        // ------------------------------------------------------------------ the cruise ship

        Transform _ship;
        Rigidbody2D _shipBody;
        float _hornAt = -100f;

        void BuildShip()
        {
            var sr = Sprite("CruiseShip", cruiseShip, Vector2.zero, 64f, 0f, ShipOrder);
            _ship = sr.transform;
            _shipBody = sr.gameObject.AddComponent<Rigidbody2D>();
            _shipBody.bodyType = RigidbodyType2D.Kinematic;
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = (Vector2)cruiseShip.bounds.size * new Vector2(0.88f, 0.94f);
            var shadow = Sprite("CruiseShadow", cruiseShip, Vector2.zero, 64f, 0f, ShadowOrder, sr.transform);
            shadow.transform.localPosition = new Vector3(1.2f / sr.transform.localScale.x, -1.8f / sr.transform.localScale.x, 0f);
            shadow.color = new Color(0f, 0.12f, 0.2f, 0.35f);
        }

        void UpdateShip()
        {
            if (_ship == null) return;
            var s = track.Layout.cruiseShip;
            var path = track.Path;
            // A straight course out at sea beside the stretch, from well before it to well after (it comes into view and
            // steams out of it again, then starts over out of sight).
            Vector2 a = path.PointAt(s.from) + path.NormalAt(s.from) * s.lateral, b = path.PointAt(s.to) + path.NormalAt(s.to) * s.lateral;
            Vector2 dir = (b - a).normalized;
            a -= dir * 90f; b += dir * 90f;
            float span = Vector2.Distance(a, b);
            Vector2 p = a + dir * Mathf.Repeat(Track.Clock * s.speed + 60f, span);
            float angle = Heading(dir);
            _shipBody.MovePosition(p);
            _shipBody.MoveRotation(angle);
            _ship.position = p;
            _ship.rotation = Quaternion.Euler(0f, 0f, angle);
            // Its horn now and then when the player is close: the screen shakes.
            if (_race != null && _race.Player != null && Time.time - _hornAt > 14f && ((Vector2)_race.Player.transform.position - p).sqrMagnitude < 40f * 40f)
            {
                _hornAt = Time.time;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.shipHorn, 0.8f);
                if (_shake != null) _shake.Shake(0.45f);
            }
        }

        // ------------------------------------------------------------------ fireboats

        readonly List<(TrackLayout.FireJet data, SpriteRenderer arc, Vector2 from, Vector2 to, Vector2 across, ParticleSystem spray)> _jets = new();
        readonly List<(UnityEngine.UI.Image image, float born)> _drops = new();
        Canvas _dropCanvas;
        float _pushedAt;

        void BuildJet(TrackPath path, TrackLayout.FireJet j)
        {
            Vector2 n = path.NormalAt(j.distance) * Mathf.Sign(j.side), centre = path.PointAt(j.distance);
            float out_ = track.OffRoadAt(j.distance) + 5.5f;
            Vector2 boatAt = centre + n * out_;
            Sprite("Fireboat", fireboat, boatAt, 9f, Heading(path.TangentAt(j.distance)), FireboatOrder);
            Vector2 from = boatAt - n * 1.5f, to = centre - n * (track.OffRoadAt(j.distance) + 2f);
            var arc = Sprite("WaterArc", waterArc, (from + to) * 0.5f, 1f, Heading(to - from), JetOrder);
            if (waterArc != null)
            {
                var size = waterArc.bounds.size;
                arc.transform.localScale = new Vector3(4.6f / size.x, Vector2.Distance(from, to) / size.y, 1f);
            }
            ParticleSystem spray = null;
            if (Application.isPlaying)
            {
                var go = new GameObject("JetSpray") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.transform.position = to;
                spray = go.AddComponent<ParticleSystem>();
                spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = spray.main;
                main.startLifetime = 0.6f; main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f); main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                main.startColor = new Color(0.9f, 0.97f, 1f, 0.85f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var shape = spray.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.6f;
                var em = spray.emission; em.rateOverTime = 0f;
                var r = go.GetComponent<ParticleSystemRenderer>();
                var foam = Resources.Load<Material>("BoatFoam");
                if (foam != null) r.sharedMaterial = foam;
                r.sortingOrder = JetOrder;
                spray.Play();
                _parts.Add(go);
            }
            _jets.Add((j, arc, from, to, n, spray));
        }

        void UpdateJets(float dt)
        {
            float now = Track.Clock;
            foreach (var (data, arc, from, to, across, spray) in _jets)
            {
                float u = Mathf.Repeat((now + data.offset) / data.period, 1f);
                bool on = u < 0.45f;
                float k = on ? Mathf.Clamp01(Mathf.Min(u, 0.45f - u) / 0.06f) : 0f;
                arc.color = new Color(1f, 1f, 1f, k);
                if (spray != null) { var em = spray.emission; em.rateOverTime = 60f * k; }
                if (k < 0.5f) continue;
                // Boats under the jet are shoved aside (and the player gets a faceful).
                foreach (var car in _cars)
                {
                    if (car == null || car.IsAirborne) continue;
                    Vector2 p = car.Body.position;
                    Vector2 axis = (to - from).normalized;
                    float t = Vector2.Dot(p - from, axis), len = Vector2.Distance(from, to);
                    if (t < 0f || t > len + 1f) continue;
                    float off = Mathf.Abs(Vector2.Dot(p - from, new Vector2(-axis.y, axis.x)));
                    if (off > 1.8f) continue;
                    car.Body.linearVelocity += axis * 14f * dt;
                    if (_race != null && _race.Player != null && car.gameObject == _race.Player.gameObject && Time.time - _pushedAt > 0.5f)
                    {
                        _pushedAt = Time.time;
                        Splash(8);
                    }
                }
            }
            UpdateDrops();
        }

        // Drops of water running down the screen.
        void Splash(int count)
        {
            if (screenDrop == null) return;
            if (_dropCanvas == null)
            {
                var go = new GameObject("ScreenDrops", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler)) { hideFlags = HideFlags.DontSave };
                _dropCanvas = go.GetComponent<Canvas>();
                _dropCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _dropCanvas.sortingOrder = 15;
                var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1170f, 2532f);
                _parts.Add(go);
            }
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Drop", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_dropCanvas.transform, false);
                rt.anchorMin = rt.anchorMax = new Vector2(Random.value, Random.Range(0.25f, 0.95f));
                rt.sizeDelta = Vector2.one * Random.Range(70f, 190f);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = screenDrop;
                image.raycastTarget = false;
                _drops.Add((image, Time.time));
            }
        }

        void UpdateDrops()
        {
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var (image, born) = _drops[i];
                float age = (Time.time - born) / 1.6f;
                if (image == null || age >= 1f) { if (image != null) Destroy(image.gameObject); _drops.RemoveAt(i); continue; }
                image.rectTransform.anchoredPosition += new Vector2(0f, -60f * Time.deltaTime * (0.3f + age));
                image.color = new Color(1f, 1f, 1f, 1f - age * age);
            }
        }

        // ------------------------------------------------------------------ the seaplane

        Transform _plane;
        SpriteRenderer _planeShadow;
        int _planeLap = -1;
        float _planeStart = -100f;
        bool _planeBeaten;
        const float PlaneAir = 1.6f, PlaneSkimSpeed = 13f, PlaneLandSpeed = 22f;

        void BuildPlane()
        {
            var sr = Sprite("Seaplane", seaplane, Vector2.zero, 9f, 0f, PlaneOrder);
            _plane = sr.transform;
            _planeShadow = Sprite("SeaplaneShadow", seaplane, Vector2.zero, 9f, 0f, ShadowOrder);
            _planeShadow.color = new Color(0f, 0.12f, 0.2f, 0.35f);
            _plane.gameObject.SetActive(false);
            _planeShadow.gameObject.SetActive(false);
        }

        void UpdatePlane()
        {
            if (_plane == null || _race == null || _race.Player == null) return;
            var s = track.Layout.seaplane;
            var path = track.Path;
            var sensor = _race.Player.GetComponent<TrackSensor>();
            float pd = sensor.Current.Distance;
            float now = Time.time;
            // Each lap, as the player comes up to it, it glides down from behind, lands ahead and skims along.
            float trigger = s.from - 30f;
            if (pd > trigger && pd < s.from && now - _planeStart > 20f)
            {
                _planeStart = now;
                _planeBeaten = false;
            }
            float t = now - _planeStart;
            float skim = Mathf.Repeat(s.to - s.from, path.Length) / PlaneSkimSpeed;
            bool active = t >= 0f && t < PlaneAir + skim + PlaneAir;
            _plane.gameObject.SetActive(active);
            _planeShadow.gameObject.SetActive(active);
            if (!active) return;
            float d, height;
            if (t < PlaneAir) { d = s.from - PlaneLandSpeed * (PlaneAir - t); height = 1f - t / PlaneAir; }
            else if (t < PlaneAir + skim) { d = s.from + (t - PlaneAir) * PlaneSkimSpeed; height = 0f; }
            else { float u = t - PlaneAir - skim; d = s.to + u * PlaneLandSpeed; height = u / PlaneAir; }
            Vector2 p = path.PointAt(d) + path.NormalAt(d) * 2.4f, dir = path.TangentAt(d);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // (its nose points right in the picture)
            _plane.position = p + new Vector2(-0.6f, 2.2f) * height;
            _plane.rotation = Quaternion.Euler(0f, 0f, angle);
            _plane.localScale = Vector3.one * (9f / Mathf.Max(seaplane.bounds.size.x, seaplane.bounds.size.y)) * (1f + 0.35f * height);
            _plane.GetComponent<SpriteRenderer>().sortingOrder = height > 0.05f ? PlaneOrder : DuckOrder + 1;
            _planeShadow.transform.position = p;
            _planeShadow.transform.rotation = _plane.rotation;
            _planeShadow.transform.localScale = Vector3.one * (9f / Mathf.Max(seaplane.bounds.size.x, seaplane.bounds.size.y)) * 0.9f;
            // Overtaken on the water: BEAT THE PLANE!
            if (!_planeBeaten && height <= 0f && path.DeltaDistance(d, pd) > 3f && path.DeltaDistance(d, pd) < 20f)
            {
                _planeBeaten = true;
                if (_combo != null) _combo.Announce("BEAT THE PLANE!", new Color(1f, 0.82f, 0.25f));
                _race.Player.GetComponent<SomeGame.Car.CarMovement>()?.GiveBoost(0.7f);
            }
        }

        // ------------------------------------------------------------------ the last lap: sunset

        MeshRenderer _sunset;
        float _sunsetLevel;

        void UpdateSunset(float dt)
        {
            if (!track.Layout.lastLapShow || _race == null || _race.Player == null) return;
            bool last = _race.Player.TotalLaps > 1 && _race.Player.CurrentLap >= _race.Player.TotalLaps;
            _sunsetLevel = Mathf.MoveTowards(_sunsetLevel, last ? 1f : 0f, dt / 4f);
            if (_sunsetLevel <= 0f) { if (_sunset != null) _sunset.enabled = false; return; }
            var cam = Camera.main;
            if (cam == null) return;
            if (_sunset == null)
            {
                var go = new GameObject("Sunset", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(cam.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 5f);
                var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>();
                TrackMeshes.AddQuad(v, u, t, Vector2.zero, Vector2.right * 400f, Vector2.up * 400f, Vector2.one);
                // Warm at the top of the screen, a rosy glow lower down.
                var c = new List<Color32> { new(255, 120, 90, 255), new(255, 120, 90, 255), new(255, 170, 60, 255), new(255, 170, 60, 255) };
                Assign(go.GetComponent<MeshFilter>(), TrackMeshes.Quads("Sunset", v, u, t, c));
                _sunset = go.GetComponent<MeshRenderer>();
                _sunset.sharedMaterial = plain;
                _sunset.sortingOrder = 60;
                _parts.Add(go);
            }
            _sunset.enabled = true;
            byte a = (byte)(255f * 0.2f * _sunsetLevel);
            _sunset.GetComponent<MeshFilter>().sharedMesh.colors32 = new Color32[] { new(255, 120, 90, a), new(255, 120, 90, a), new(255, 170, 60, a), new(255, 170, 60, a) };
        }

        void Update()
        {
            if (!Application.isPlaying || track == null || track.Layout == null || track.Layout.biome != TrackLayout.Biome.Lagoon) return;
            float dt = Time.deltaTime;
            UpdateFerry(dt);
            UpdateCranes();
            UpdateDucks(dt);
            UpdateShip();
            UpdateJets(dt);
            UpdatePlane();
            UpdateSunset(dt);
        }
    }
}
