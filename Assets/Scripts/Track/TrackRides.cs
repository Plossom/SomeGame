using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The freestyle park's toys (<see cref="TrackLayout.rides"/>, <see cref="TrackLayout.pinball"/>,
    /// <see cref="TrackLayout.rinks"/>, <see cref="TrackLayout.slalom"/>):
    /// - the ice flume: a see-through tube of ice beside the road; steer into its mouth and you shoot along it
    ///   (twisting through the trees, high above them), popping out further on with a boost;
    /// - the grind rail: hit its kicker lined up and fast enough and you grind along the rail in a shower of
    ///   sparks (over the gorge), dropping off further on with a boost;
    /// - the pinball rink: an ice rink inside hockey boards, full of bumpers that bounce you off with a boost;
    /// - the slalom: gates down the slope; every gate in a row gives a bigger boost, a missed gate ends the chain.
    /// Rivals take the flume and the rail too (on some laps).
    /// </summary>
    public class TrackRides : TrackDerivedBehaviour
    {
        [SerializeField] Material plain, iceFloor, particleMaterial;
        [SerializeField] Sprite bumperSprite, gateRedSprite, gateBlueSprite, glowSprite;

        const int ShadowOrder = -44, RinkOrder = -46, KickerOrder = -45, BoardOrder = -40, PostOrder = 6, RailOrder = 8,
                  GateOrder = 10, BumperOrder = 11, TubeOrder = 25, MouthOrder = 26;
        const float TubeHalf = 2.3f;
        // How far off the entrance's middle a car may be (the rail's kicker is narrow), the speed it needs, and how
        // straight it must be heading (no swerving in sideways).
        const float RailHalf = 1.5f, FlumeHalf = 1.3f, RailSpeed = 12f, FlumeSpeed = 8f, MaxEntryAngle = 22f, RailEntryAngle = 28f;

        static Color Lin(float r, float g, float b, float a = 1f) { var c = new Color(r, g, b).linear; c.a = a; return c; }
        static readonly Color TubeEdge = Lin(0.32f, 0.6f, 0.82f, 0.9f), TubeMid = Lin(0.72f, 0.9f, 1f, 0.42f), TubeCore = Lin(0.88f, 0.97f, 1f, 0.22f),
            TubeRing = Lin(0.38f, 0.66f, 0.88f, 0.7f), Shadow = new(0f, 0.05f, 0.15f, 0.18f), Steel = Lin(0.34f, 0.37f, 0.43f),
            SteelLight = Lin(0.82f, 0.86f, 0.92f), Board = Lin(0.97f, 0.98f, 1f), BoardRed = Lin(0.85f, 0.22f, 0.22f), BoardBlue = Lin(0.2f, 0.42f, 0.78f),
            Hazard = Lin(0.96f, 0.76f, 0.19f), HazardDark = Lin(0.16f, 0.16f, 0.18f), Mouth = Lin(0.08f, 0.2f, 0.32f);

        readonly List<GameObject> _parts = new();

        class RideView
        {
            public TrackLayout.Ride Data;
            public List<Vector2> Course;
            public Pose Arrival;
        }
        readonly List<RideView> _rides = new();
        readonly List<(Transform t, float scale, float hitAt)> _bumpers = new();
        readonly Dictionary<SomeGame.Car.CarMovement, float> _bumpedAt = new();
        readonly List<(SpriteRenderer a, SpriteRenderer b)> _gates = new();
        SomeGame.Car.CarMovement[] _cars;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.Race.RaceManager _race;
        ParticleSystem _sparks, _pop;

        // The player's slalom: the next gate, the chain so far, where they were last frame.
        int _nextGate = -1, _chain;
        float _lastDistance = -1f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _rides.Clear(); _bumpers.Clear(); _gates.Clear();
            if (layout.rides != null) foreach (var ride in layout.rides) BuildRide(path, layout, ride);
            if (layout.rinks != null) foreach (var rink in layout.rinks) BuildRink(path, layout, rink);
            if (layout.pinball != null) foreach (var b in layout.pinball) BuildBumper(b);
            if (layout.slalom != null) for (int i = 0; i < layout.slalom.Count; i++) BuildGate(path, layout.slalom[i], i);
            if (Application.isPlaying)
            {
                _sparks = Particles("GrindSparks", new Color(1f, 0.85f, 0.45f, 1f), 0.25f, new Vector2(3f, 8f));
                _pop = Particles("BumperPop", new Color(1f, 1f, 1f, 0.9f), 0.4f, new Vector2(4f, 9f));
            }
        }

        // ------------------------------------------------------------------ rides

        void BuildRide(TrackPath path, TrackLayout layout, TrackLayout.Ride ride)
        {
            Vector2 entry = path.PointAt(ride.from) + path.NormalAt(ride.from) * ride.lateral;
            Vector2 exit = path.PointAt(ride.to) + path.NormalAt(ride.to) * ride.toLateral;
            var control = new List<Vector2> { entry };
            if (ride.points != null) control.AddRange(ride.points);
            control.Add(exit);
            Vector2 before = entry - path.TangentAt(ride.from) * 10f, after = exit + path.TangentAt(ride.to) * 10f;
            var course = Smooth(control, before, after, 1f);
            Vector2 tan = path.TangentAt(ride.to);
            var view = new RideView
            {
                Data = ride, Course = course,
                Arrival = new Pose(exit + tan * 2f, Quaternion.Euler(0f, 0f, Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg - 90f)),
            };
            _rides.Add(view);
            if (ride.type == TrackLayout.RideType.Flume) BuildFlume(path, ride, course);
            else BuildRail(path, layout, ride, course);
        }

        // A tube of clear ice: blue edges, see-through in the middle (you see the cars inside), a glint along it,
        // hoops every few metres; posts and a shadow underneath; a dark mouth at each end.
        void BuildFlume(TrackPath path, TrackLayout.Ride ride, List<Vector2> course)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            var sv = new List<Vector3>(); var sc = new List<Color>(); var st = new List<int>();
            float[] lanes = { -1f, -0.6f, 0f, 0.6f, 1f };
            Color[] cols = { TubeEdge, TubeMid, TubeCore, TubeMid, TubeEdge };
            float along = 0f;
            for (int i = 0; i < course.Count; i++)
            {
                Vector2 dir = Dir(course, i), n = new(-dir.y, dir.x), p = course[i];
                if (i > 0) along += Vector2.Distance(course[i - 1], p);
                int k = v.Count;
                for (int q = 0; q < lanes.Length; q++) { v.Add(p + n * lanes[q] * TubeHalf); c.Add(cols[q]); }
                if (i > 0)
                    for (int q = 0; q < lanes.Length - 1; q++)
                    {
                        int a = k - lanes.Length + q;
                        t.AddRange(new[] { a, k + q, a + 1, a + 1, k + q, k + q + 1 });
                    }
                Vector2 shadowOffset = new(1.4f, -1.8f);
                int s = sv.Count;
                sv.Add(p + n * TubeHalf + shadowOffset); sv.Add(p - n * TubeHalf + shadowOffset);
                sc.Add(Shadow); sc.Add(Shadow);
                if (i > 0) st.AddRange(new[] { s - 2, s, s - 1, s - 1, s, s + 1 });
            }
            // The glint and the hoops.
            for (int i = 1; i < course.Count; i++)
            {
                Vector2 n0 = Left(Dir(course, i - 1)), n1 = Left(Dir(course, i));
                AddQuad(course[i - 1] + n0 * TubeHalf * 0.45f, course[i] + n1 * TubeHalf * 0.45f, course[i] + n1 * TubeHalf * 0.3f, course[i - 1] + n0 * TubeHalf * 0.3f,
                    new Color(1f, 1f, 1f, 0.55f), v, c, t);
            }
            for (float a = 2f; a < along - 2f; a += 3.5f)
            {
                Vector2 p = At(course, a, out Vector2 d), n = Left(d);
                AddQuad(p - n * TubeHalf - d * 0.18f, p - n * TubeHalf + d * 0.18f, p + n * TubeHalf + d * 0.18f, p + n * TubeHalf - d * 0.18f, TubeRing, v, c, t);
                if (Mathf.Repeat(a, 7f) < 3.5f)
                {
                    // A post under the tube.
                    var post = new Vector2(0.35f, 0.35f);
                    AddQuad(p - post + new Vector2(0.7f, -0.9f), p + new Vector2(post.x, -post.y) + new Vector2(0.7f, -0.9f), p + post + new Vector2(0.7f, -0.9f), p + new Vector2(-post.x, post.y) + new Vector2(0.7f, -0.9f), Steel, sv, sc, st);
                }
            }
            Part("FlumeShadow", MakeMesh("FlumeShadow", sv, sc, st), plain, ShadowOrder);
            Part("Flume", MakeMesh("Flume", v, c, t), plain, TubeOrder);
            // The mouths: a dark opening in a thick ice rim.
            var mv = new List<Vector3>(); var mc = new List<Color>(); var mt = new List<int>();
            foreach (bool start in new[] { true, false })
            {
                int i = start ? 0 : course.Count - 1;
                Vector2 p = course[i], d = Dir(course, i) * (start ? 1f : -1f), n = Left(d);
                Disc(p + d * 0.6f, n, d, TubeHalf * 1.25f, TubeHalf * 0.75f, TubeEdge, mv, mc, mt);
                Disc(p + d * 0.9f, n, d, TubeHalf * 0.85f, TubeHalf * 0.55f, Mouth, mv, mc, mt);
            }
            Part("FlumeMouths", MakeMesh("FlumeMouths", mv, mc, mt), plain, MouthOrder);
        }

        // A steel rail on posts with a striped kicker leading up to it.
        void BuildRail(TrackPath path, TrackLayout layout, TrackLayout.Ride ride, List<Vector2> course)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            var sv = new List<Vector3>(); var sc = new List<Color>(); var st = new List<int>();
            float along = 0f;
            for (int i = 1; i < course.Count; i++)
            {
                Vector2 a = course[i - 1], b = course[i], n0 = Left(Dir(course, i - 1)), n1 = Left(Dir(course, i));
                along += Vector2.Distance(a, b);
                AddQuad(a - n0 * 0.28f, b - n1 * 0.28f, b + n1 * 0.28f, a + n0 * 0.28f, Steel, v, c, t);
                AddQuad(a + n0 * 0.04f, b + n1 * 0.04f, b + n1 * 0.15f, a + n0 * 0.15f, SteelLight, v, c, t);
                Vector2 so = new(0.9f, -1.2f);
                AddQuad(a - n0 * 0.3f + so, b - n1 * 0.3f + so, b + n1 * 0.3f + so, a + n0 * 0.3f + so, Shadow, sv, sc, st);
            }
            for (float a = 1.5f; a < along - 1f; a += 3f)
            {
                Vector2 p = At(course, a, out Vector2 d), n = Left(d);
                AddQuad(p - n * 0.22f - d * 0.22f + new Vector2(0.5f, -0.7f), p - n * 0.22f + d * 0.22f + new Vector2(0.5f, -0.7f), p + n * 0.22f + d * 0.22f + new Vector2(0.5f, -0.7f), p + n * 0.22f - d * 0.22f + new Vector2(0.5f, -0.7f), Steel, sv, sc, st);
            }
            Part("RailShadow", MakeMesh("RailShadow", sv, sc, st), plain, ShadowOrder);
            Part("Rail", MakeMesh("Rail", v, c, t), plain, RailOrder);
            // The kicker: hazard stripes on a short ramp leading onto the rail.
            var kv = new List<Vector3>(); var kc = new List<Color>(); var kt = new List<int>();
            Vector2 tan = path.TangentAt(ride.from), nn = path.NormalAt(ride.from);
            Vector2 lip = path.PointAt(ride.from) + nn * ride.lateral;
            for (int q = 0; q < 6; q++)
            {
                Vector2 p0 = lip - tan * (4f - q * 0.67f), p1 = lip - tan * (4f - (q + 1) * 0.67f);
                Color col = q % 2 == 0 ? Hazard : HazardDark;
                AddQuad(p0 - nn * RailHalf, p1 - nn * RailHalf, p1 + nn * RailHalf, p0 + nn * RailHalf, col, kv, kc, kt);
            }
            Part("RailKicker", MakeMesh("RailKicker", kv, kc, kt), plain, KickerOrder);
        }

        // ------------------------------------------------------------------ rink, bumpers, gates

        void BuildRink(TrackPath path, TrackLayout layout, Vector2 span)
        {
            var fv = new List<Vector3>(); var fuv = new List<Vector2>(); var ft = new List<int>();
            var bv = new List<Vector3>(); var bc = new List<Color>(); var bt = new List<int>();
            float length = path.DeltaDistance(span.x, span.y);
            int steps = Mathf.Max(2, Mathf.CeilToInt(length));
            var sides = new List<Vector2>[2] { new(), new() };
            for (int i = 0; i <= steps; i++)
            {
                float d = span.x + length * i / steps;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                float w = track.OffRoadAt(d);
                int k = fv.Count;
                fv.Add(p + n * w); fv.Add(p - n * w); fuv.Add((p + n * w) / 8f); fuv.Add((p - n * w) / 8f);
                if (i > 0) ft.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 });
                sides[0].Add(p + n * (w + 0.35f)); sides[1].Add(p - n * (w + 0.35f));
            }
            var floor = new Mesh { name = "Rink", hideFlags = HideFlags.DontSave };
            floor.SetVertices(fv); floor.SetUVs(0, fuv);
            var white = new Color[fv.Count];
            for (int i = 0; i < white.Length; i++) white[i] = new Color(1f, 1f, 1f, 0.9f);
            floor.colors = white;
            floor.SetTriangles(BothSides(ft), 0);
            floor.RecalculateBounds();
            Part("Rink", floor, iceFloor != null ? iceFloor : plain, RinkOrder);
            // The boards: white with a red and a blue stripe, solid.
            var colliders = new GameObject("RinkBoards") { hideFlags = HideFlags.DontSave };
            colliders.transform.SetParent(transform, false);
            _parts.Add(colliders);
            for (int side = 0; side < 2; side++)
            {
                var line = sides[side];
                for (int i = 1; i < line.Count; i++)
                {
                    Vector2 a = line[i - 1], b = line[i], d = (b - a).normalized, n = new(-d.y, d.x);
                    AddQuad(a - n * 0.45f, b - n * 0.45f, b + n * 0.45f, a + n * 0.45f, Board, bv, bc, bt);
                    AddQuad(a - n * 0.12f, b - n * 0.12f, b + n * 0.02f, a + n * 0.02f, i % 6 < 3 ? BoardRed : BoardBlue, bv, bc, bt);
                }
                var edge = colliders.AddComponent<EdgeCollider2D>();
                edge.points = line.ToArray();
                edge.edgeRadius = 0.4f;
            }
            Part("RinkBoards", MakeMesh("RinkBoards", bv, bc, bt), plain, BoardOrder);
        }

        void BuildBumper(TrackLayout.PinballBumper bumper)
        {
            var sr = Sprite("PinballBumper", bumperSprite, bumper.position, bumper.radius * 2.3f, 0f, BumperOrder);
            var circle = sr.gameObject.AddComponent<CircleCollider2D>();
            circle.radius = bumper.radius / Mathf.Max(0.01f, sr.transform.localScale.x);
            _bumpers.Add((sr.transform, sr.transform.localScale.x, -10f));
        }

        void BuildGate(TrackPath path, TrackLayout.SlalomGate gate, int index)
        {
            Vector2 p = path.PointAt(gate.distance) + path.NormalAt(gate.distance) * gate.lateral, n = path.NormalAt(gate.distance);
            var sprite = index % 2 == 0 ? gateRedSprite : gateBlueSprite;
            float angle = Mathf.Atan2(path.TangentAt(gate.distance).y, path.TangentAt(gate.distance).x) * Mathf.Rad2Deg - 90f;
            var a = Sprite("GatePole", sprite, p + n * gate.width * 0.5f, 2.3f, angle, GateOrder);
            var b = Sprite("GatePole", sprite, p - n * gate.width * 0.5f, 2.3f, angle, GateOrder);
            _gates.Add((a, b));
        }

        // ------------------------------------------------------------------ Play mode

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || _cars == null || track == null) return;
            var path = track.Path;
            foreach (var car in _cars)
            {
                if (car == null || !car.isActiveAndEnabled) continue;
                if (car.IsRiding)
                {
                    if (car.CurrentRide == SomeGame.Car.RideKind.Rail && _sparks != null)
                    {
                        _sparks.transform.position = car.Body.position - (Vector2)car.transform.up * 0.8f;
                        _sparks.Emit(3);
                    }
                    continue;
                }
                if (car.IsAirborne) continue;
                var sensor = car.GetComponent<TrackSensor>();
                if (sensor == null || sensor.Branch >= 0) continue;
                // Into a ride's entrance?
                float d = sensor.Current.Distance, lat = sensor.Current.Lateral, speed = Vector2.Dot(car.Body.linearVelocity, path.TangentAt(d));
                foreach (var ride in _rides)
                {
                    float to = path.DeltaDistance(d, ride.Data.from);
                    bool rail = ride.Data.type == TrackLayout.RideType.Rail;
                    // Rivals line up by script, not by thumb: a little more room for them.
                    bool rival = car.GetComponent<SomeGame.Input.PlayerCarInput>() == null;
                    float window = (rail ? RailHalf : FlumeHalf) + (rival ? 0.9f : 0f);
                    if (to < -0.5f || to > 1.5f || Mathf.Abs(lat - ride.Data.lateral) > window) continue;
                    if (speed < (rail ? RailSpeed : FlumeSpeed) * (rival ? 0.8f : 1f)) continue;
                    if (Vector2.Angle(car.Body.linearVelocity, path.TangentAt(d)) > (rail ? RailEntryAngle : MaxEntryAngle) * (rival ? 1.8f : 1f)) continue;
                    BoardRide(car, ride);
                    break;
                }
                // Bumpers: bounce off with a boost.
                for (int b = 0; b < _bumpers.Count && b < track.Layout.pinball.Count; b++)
                {
                    var t = _bumpers[b].t;
                    float r = track.Layout.pinball[b].radius;
                    Vector2 away = car.Body.position - (Vector2)t.position;
                    if (away.magnitude > r + 1.8f) continue; // touching it (the car's nose reaches this far)
                    if (_bumpedAt.TryGetValue(car, out float last) && Time.time - last < 0.35f) continue;
                    _bumpedAt[car] = Time.time;
                    Bump(car, t, away.normalized);
                }
            }
        }

        void BoardRide(SomeGame.Car.CarMovement car, RideView ride)
        {
            var progress = car.GetComponent<SomeGame.Race.RaceProgress>();
            bool rail = ride.Data.type == TrackLayout.RideType.Rail;
            float top = car.Stats != null ? car.Stats.topSpeed : 18f;
            float speed = rail ? Mathf.Max(ride.Data.speed, car.Body.linearVelocity.magnitude * 1.1f) : ride.Data.speed;
            var path = new List<Vector2>(ride.Course);
            bool ok = car.RideLift(path, ride.Arrival, speed, top * 1.1f, () =>
            {
                if (progress != null) progress.LiftArrived(ride.Data.from, ride.Data.to);
                car.GiveBoost(0.8f);
                if (car.GetComponent<SomeGame.Input.PlayerCarInput>() != null && _combo != null)
                    _combo.Announce(rail ? "RAIL GRIND!" : "FLUME RIDE!", rail ? new Color(1f, 0.75f, 0.3f) : new Color(0.5f, 0.85f, 1f));
            }, rail ? SomeGame.Car.RideKind.Rail : SomeGame.Car.RideKind.Flume);
            if (!ok || car.GetComponent<SomeGame.Input.PlayerCarInput>() == null) return;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.8f);
        }

        void Bump(SomeGame.Car.CarMovement car, Transform bumper, Vector2 away)
        {
            Vector2 v = car.Body.linearVelocity;
            float speed = Mathf.Max(v.magnitude, 13f);
            // Reflect off the bumper, mostly away from it, and kick it up.
            Vector2 reflected = Vector2.Reflect(v.sqrMagnitude > 0.01f ? v.normalized : away, away);
            Vector2 dir = (reflected + away * 0.8f).normalized;
            car.Body.linearVelocity = dir * speed * 1.05f;
            car.Body.position += away * 0.3f;
            car.GiveBoost(0.4f);
            for (int i = 0; i < _bumpers.Count; i++)
                if (_bumpers[i].t == bumper) _bumpers[i] = (_bumpers[i].t, _bumpers[i].scale, Time.time);
            if (_pop != null) { _pop.transform.position = bumper.position; _pop.Emit(18); }
            if (car.GetComponent<SomeGame.Input.PlayerCarInput>() == null) return;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.bumper, 0.9f);
            if (_combo != null) _combo.Announce("BUMPER!", new Color(1f, 0.4f, 0.6f));
            var shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            if (shake != null) shake.Shake(0.2f);
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            // Bumpers squash and spring back when hit.
            for (int i = 0; i < _bumpers.Count; i++)
            {
                var (t, scale, hitAt) = _bumpers[i];
                if (t == null) continue;
                float since = Time.time - hitAt;
                float k = since < 0.35f ? 1f + 0.25f * Mathf.Sin(since / 0.35f * Mathf.PI) * (1f - since / 0.35f) : 1f;
                t.localScale = Vector3.one * scale * k;
            }
            Slalom();
        }

        // The player's slalom chain: through a gate, the chain grows and so does the boost; past one outside it, the chain ends.
        void Slalom()
        {
            var gates = track.Layout.slalom;
            if (gates == null || gates.Count == 0 || _race == null || _race.Player == null) return;
            var sensor = _race.Player.GetComponent<TrackSensor>();
            var car = _race.Player.GetComponent<SomeGame.Car.CarMovement>();
            if (sensor == null || car == null || sensor.Branch >= 0) return;
            var path = track.Path;
            float d = sensor.Current.Distance;
            if (_lastDistance >= 0f)
                for (int i = 0; i < gates.Count; i++)
                {
                    var g = gates[i];
                    bool crossed = path.DeltaDistance(_lastDistance, g.distance) > 0f && path.DeltaDistance(g.distance, d) >= 0f
                        && path.DeltaDistance(_lastDistance, d) < 10f;
                    if (!crossed) continue;
                    bool through = Mathf.Abs(sensor.Current.Lateral - g.lateral) <= g.width * 0.5f;
                    bool inOrder = i == 0 || _nextGate == i;
                    if (through)
                    {
                        _chain = inOrder ? _chain + 1 : 1;
                        _nextGate = i + 1;
                        car.GiveBoost(Mathf.Min(1f, 0.25f + 0.12f * _chain));
                        Flash(i);
                        if (_combo != null)
                        {
                            bool last = i == gates.Count - 1;
                            if (last && _chain == gates.Count) _combo.Announce("PERFECT SLALOM!", new Color(1f, 0.85f, 0.3f));
                            else _combo.Announce($"GATE x{_chain}!", i % 2 == 0 ? new Color(1f, 0.45f, 0.4f) : new Color(0.45f, 0.65f, 1f));
                        }
                        SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.dashPanel, 0.5f + 0.06f * _chain);
                    }
                    else
                    {
                        _chain = 0;
                        _nextGate = -1;
                    }
                }
            _lastDistance = d;
        }

        void Flash(int gate)
        {
            if (gate < 0 || gate >= _gates.Count || _pop == null) return;
            foreach (var sr in new[] { _gates[gate].a, _gates[gate].b })
                if (sr != null) { _pop.transform.position = sr.transform.position; _pop.Emit(6); }
        }

        // ------------------------------------------------------------------ helpers

        static Vector2 Left(Vector2 d) => new(-d.y, d.x);

        static Vector2 Dir(List<Vector2> pts, int i) =>
            (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;

        static Vector2 At(List<Vector2> pts, float along, out Vector2 dir)
        {
            for (int i = 1; i < pts.Count; i++)
            {
                float seg = Vector2.Distance(pts[i - 1], pts[i]);
                if (along <= seg || i == pts.Count - 1)
                {
                    dir = (pts[i] - pts[i - 1]).normalized;
                    return Vector2.Lerp(pts[i - 1], pts[i], Mathf.Clamp01(along / Mathf.Max(0.001f, seg)));
                }
                along -= seg;
            }
            dir = Vector2.up;
            return pts[^1];
        }

        static List<Vector2> Smooth(List<Vector2> control, Vector2 before, Vector2 after, float spacing)
        {
            var list = new List<Vector2>();
            for (int i = 0; i < control.Count - 1; i++)
            {
                Vector2 p0 = i == 0 ? before : control[i - 1], p1 = control[i], p2 = control[i + 1], p3 = i + 2 < control.Count ? control[i + 2] : after;
                int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / spacing));
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps, t2 = t * t, t3 = t2 * t;
                    list.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
                }
            }
            list.Add(control[^1]);
            return list;
        }

        // A filled oval (a mouth seen from above): `across` wide along n, `depth` along d.
        static void Disc(Vector2 centre, Vector2 n, Vector2 d, float across, float depth, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            const int seg = 28;
            int k = v.Count;
            v.Add(centre); c.Add(color);
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * 2f * i / seg;
                v.Add(centre + n * Mathf.Cos(a) * across + d * Mathf.Sin(a) * depth);
                c.Add(color);
                if (i > 0) t.AddRange(new[] { k, k + i, k + i + 1 });
            }
        }

        static void AddQuad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) c.Add(color);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        static List<int> BothSides(List<int> t)
        {
            var both = new List<int>(t);
            for (int i = 0; i < t.Count; i += 3) both.AddRange(new[] { t[i], t[i + 2], t[i + 1] });
            return both;
        }

        static Mesh MakeMesh(string name, List<Vector3> v, List<Color> c, List<int> t)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(BothSides(t), 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void Part(string name, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material != null ? material : plain;
            r.sortingOrder = order;
            _parts.Add(go);
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 position, float size, float angle, int order)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, sprite.bounds.size.x));
            _parts.Add(go);
            return sr;
        }

        ParticleSystem Particles(string name, Color color, float size, Vector2 speed)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _parts.Add(go);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = TubeOrder - 1;
            system.Play();
            return system;
        }
    }
}
