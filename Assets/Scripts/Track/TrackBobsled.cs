using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Bobsled channels (<see cref="TrackLayout.bobsleds"/>): a floor of glossy ice between two curved ice walls with a
    /// striped lip. On the ice a bend carries the cars outwards and up the outer wall - the faster, the higher - while
    /// the wall pushes them back down. Ride it high and clean through a bend and you come out of it with a boost
    /// ("CARVE!"); too fast and you fly over the lip into the depths. Red chevron boards before the tight bends: brake.
    /// </summary>
    public class TrackBobsled : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;
        [SerializeField] Material particleMaterial;

        const int FloorOrder = -46, WallOrder = -45, LipOrder = -44, BoardOrder = 6;
        /// <summary>Push back down the wall per unit of height (m/s²), and how much of a bend's pull the ice lets through.</summary>
        public const float WallPush = 8.5f, Slip = 0.55f;

        static readonly Color Floor = MeshKit.Hex("E3F3FC"), FloorStreak = MeshKit.Hex("FFFFFF", 0.7f), WallLow = MeshKit.Hex("D5ECF8"),
            WallHigh = MeshKit.Hex("9FCDE8"), WallShade = MeshKit.Hex("7DB4D6"), LipRed = MeshKit.Hex("E0342B"), LipBlue = MeshKit.Hex("1E5BC6"),
            LipWhite = MeshKit.Hex("FFFFFF"), Board = MeshKit.Hex("E0342B"), BoardEdge = MeshKit.Hex("FFFFFF"), Post = MeshKit.Hex("30343C"),
            Shadow = new Color(0.02f, 0.04f, 0.12f, 0.3f);

        readonly List<GameObject> _parts = new();
        CarMovement[] _cars;
        readonly Dictionary<CarMovement, TrackSensor> _sensors = new();
        readonly Dictionary<CarMovement, float> _carve = new();
        SomeGame.Race.RaceManager _race;
        ParticleSystem _spray;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (track != null && !track.Hazards.Contains(this)) track.Hazards.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (track != null) track.Hazards.Remove(this);
        }

        /// <summary>Signed bend at a lap distance (radians per metre; + bends left).</summary>
        public static float Bend(TrackPath path, float d)
        {
            Vector2 a = path.TangentAt(d - 3f), b = path.TangentAt(d + 3f);
            return Mathf.Atan2(a.x * b.y - a.y * b.x, Vector2.Dot(a, b)) / 6f;
        }

        TrackLayout.Bobsled At(float d)
        {
            if (track.Layout.bobsleds == null) return null;
            foreach (var b in track.Layout.bobsleds)
                if (track.Path.DeltaDistance(b.from, d) >= 0f && track.Path.DeltaDistance(d, b.to) >= 0f) return b;
            return null;
        }

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            if (layout.bobsleds == null || layout.bobsleds.Count == 0) return;
            var root = new GameObject("Bobsled") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var floor = new MeshKit();
            var walls = new MeshKit();
            var lip = new MeshKit();
            var boards = new MeshKit();
            foreach (var b in layout.bobsleds)
            {
                float length = path.DeltaDistance(b.from, b.to), fh = b.floor * 0.5f, top = fh + b.wall;
                int k = 0;
                for (float s = 0f; s < length - 0.01f; s += 1f, k++)
                {
                    float d0 = b.from + s, d1 = b.from + Mathf.Min(length, s + 1f);
                    Vector2 a = path.PointAt(d0), c = path.PointAt(d1), na = path.NormalAt(d0), nc = path.NormalAt(d1);
                    // the floor, with long streaks of polish
                    floor.Quad(a - na * fh, c - nc * fh, c + nc * fh, a + na * fh, Floor);
                    foreach (float st in new[] { -1.4f, 0.3f, 1.7f })
                        if ((k + (int)(st * 3f)) % 5 < 3) floor.Quad(a + na * (st - 0.05f), c + nc * (st - 0.05f), c + nc * (st + 0.05f), a + na * (st + 0.05f), FloorStreak);
                    // the walls: low and pale at the floor, deeper blue up to the lip; the outer wall of a bend shaded deeper
                    float bend = Bend(path, d0);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        bool outer = bend * side < -0.004f;
                        Color hi = outer ? WallShade : WallHigh;
                        walls.Quad(a + na * side * fh, c + nc * side * fh, c + nc * side * top, a + na * side * top, WallLow, WallLow, hi, hi);
                        // the lip: red, white and blue
                        Color stripe = (k / 2) % 3 == 0 ? LipRed : (k / 2) % 3 == 1 ? LipWhite : LipBlue;
                        lip.Quad(a + na * side * top, c + nc * side * top, c + nc * side * (top + 0.45f), a + na * side * (top + 0.45f), stripe);
                        // its shadow falling outwards
                        lip.Quad(a + na * side * (top + 0.45f), c + nc * side * (top + 0.45f), c + nc * side * (top + 2.2f), a + na * side * (top + 2.2f), Shadow, Shadow, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0));
                    }
                }
                // Red chevron boards before every bend too tight to take flat out: brake.
                float flat = 25f; // about the fastest a car comes down the channel
                float lastBoard = -1000f;
                for (float s = 0f; s < length; s += 2f)
                {
                    float d = b.from + s, bend = Bend(path, d);
                    float height = flat * flat * Mathf.Abs(bend) * Slip / WallPush;
                    if (height < 0.95f || s - lastBoard < 70f) continue;
                    lastBoard = s;
                    float at = Mathf.Max(b.from + 4f, d - 32f), side = -Mathf.Sign(bend); // on the outside
                    Vector2 p = path.PointAt(at) + path.NormalAt(at) * side * (top + 2.6f), t = path.TangentAt(at), n = path.NormalAt(at);
                    boards.Disc(p + new Vector2(0.5f, -0.6f), new Vector2(1.9f, 1.2f), 0f, Shadow, new Color(0, 0, 0, 0), 16);
                    boards.Quad(p - t * 0.4f - n * 1.6f, p + t * 0.4f - n * 1.6f, p + t * 0.4f + n * 1.6f, p - t * 0.4f + n * 1.6f, BoardEdge);
                    boards.Quad(p - t * 0.3f - n * 1.5f, p + t * 0.3f - n * 1.5f, p + t * 0.3f + n * 1.5f, p - t * 0.3f + n * 1.5f, Board);
                    // chevrons pointing the way the bend goes
                    for (int q = -1; q <= 1; q++)
                    {
                        Vector2 c = p + n * (q * 0.95f);
                        Vector2 tip = c + n * (-side) * 0.35f;
                        boards.Line(c - t * 0.22f + n * side * 0.2f, tip, 0.12f, BoardEdge);
                        boards.Line(c + t * 0.22f + n * side * 0.2f, tip, 0.12f, BoardEdge);
                    }
                    boards.Disc(p - n * 1.8f, 0.18f, Post, 8);
                    boards.Disc(p + n * 1.8f, 0.18f, Post, 8);
                }
            }
            MeshKit.Part(root, "BobFloor", floor.Build("BobFloor"), plain, FloorOrder, out _);
            MeshKit.Part(root, "BobWalls", walls.Build("BobWalls"), plain, WallOrder, out _);
            MeshKit.Part(root, "BobLip", lip.Build("BobLip"), plain, LipOrder, out _);
            MeshKit.Part(root, "BobBoards", boards.Build("BobBoards"), plain, BoardOrder, out _);
            if (Application.isPlaying && particleMaterial != null)
                _spray = MeshKit.Burst(root, "BobSpray", particleMaterial, BoardOrder + 1, new Color(0.92f, 0.98f, 1f, 0.8f), new Vector2(1f, 3f), new Vector2(0.15f, 0.35f), 0.45f, 0.3f);
        }

        protected override void Start()
        {
            base.Start();
            if (Application.isPlaying) _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || track.Layout.bobsleds == null || track.Layout.bobsleds.Count == 0) return;
            if (_cars == null) _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            float dt = Time.fixedDeltaTime;
            var path = track.Path;
            foreach (var car in _cars)
            {
                if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                if (!_sensors.TryGetValue(car, out var sensor)) _sensors[car] = sensor = car.GetComponent<TrackSensor>();
                if (sensor == null) continue;
                var at = sensor.Current;
                var b = At(at.Distance);
                if (b == null) { _carve.Remove(car); continue; }
                float fh = b.floor * 0.5f, bend = Bend(path, at.Distance);
                Vector2 n = path.NormalAt(at.Distance), v = car.Body.linearVelocity;
                float speed = v.magnitude;
                // the bend carries the car outwards on the ice...
                car.Body.linearVelocity += n * (-Mathf.Sign(bend) * speed * speed * Mathf.Abs(bend) * Slip * dt);
                // ...and the wall pushes it back down, the harder the higher it is
                float height = (Mathf.Abs(at.Lateral) - fh) / b.wall;
                if (height > 0f)
                {
                    car.Body.linearVelocity -= n * (Mathf.Sign(at.Lateral) * WallPush * height * dt);
                    if (Random.value < dt * 30f * Mathf.Clamp01(height)) MeshKit.Emit(_spray, car.Body.position + n * Mathf.Sign(at.Lateral) * 0.9f, 1);
                }
                // a clean high ride through a bend: CARVE!
                _carve.TryGetValue(car, out float ride);
                bool carving = height > 0.45f && height < 0.98f && Mathf.Abs(bend) > 0.008f;
                if (carving) ride += speed * dt;
                else if (Mathf.Abs(bend) < 0.004f || height <= 0.2f)
                {
                    if (ride > 22f)
                    {
                        car.GiveBoost(0.55f);
                        if (_race != null && _race.Player != null && _race.Player.gameObject == car.gameObject)
                        {
                            var combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
                            if (combo != null) combo.Banner("CARVE!", new Color(0.55f, 0.85f, 1f));
                        }
                    }
                    ride = 0f;
                }
                _carve[car] = ride;
            }
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near) => false; // over the lip is off the road, into the chasm below

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            if (At(distance + 20f) == null && At(distance) == null) return;
            // Slow enough for the tightest bend in the next stretch to keep it below the lip (a weak rival cuts it fine - too fine).
            float worst = 0f;
            for (float x = 0f; x <= 45f; x += 3f)
                if (At(distance + x) != null) worst = Mathf.Max(worst, Mathf.Abs(Bend(path, distance + x)));
            if (worst < 0.003f) return;
            float safe = Mathf.Sqrt(0.8f * WallPush / (worst * Slip)) * Mathf.Lerp(1.16f, 0.96f, skill);
            speed = Mathf.Min(speed, safe);
            lateral *= 0.3f;
        }
    }
}
