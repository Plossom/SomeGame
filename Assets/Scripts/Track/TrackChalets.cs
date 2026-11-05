using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The luxury chalets of the winter finale (the rooms of houses with a <see cref="TrackLayout.RoofKind.Chalet"/> roof) and
    /// their surprises. Indoors the snowy road becomes a red runner carpet (or polished marble with gold inlays); sliding
    /// doors shut across the road in a rhythm (<see cref="TrackLayout.slidingDoors"/>); indoor pools are jumped
    /// (<see cref="TrackLayout.pools"/>); on a bowling lane balls roll at you and the pins wait at the end - drive through
    /// them: STRIKE! (<see cref="TrackLayout.bowling"/>); giant balls roll about a billiard hall, bouncing off its walls and off
    /// the cars (<see cref="TrackLayout.billiards"/>); a glass floor over the abyss has cracked panes that shatter under you a
    /// moment after you drive onto them (<see cref="TrackLayout.glassFloors"/>); and a panoramic window bursts into shards as
    /// the cars fly out through it. Tells the rivals how to get by (doors, balls, cracked glass).
    /// </summary>
    public class TrackChalets : TrackDerivedBehaviour, Track.IGuide
    {
        [SerializeField] Material plain;
        [SerializeField] Material particleMaterial;

        const int RunnerOrder = -47, InlayOrder = -46, GlassOrder = -46, CrackOrder = -45, DoorOrder = 46, LampOrder = 47, // doors over the roof: seen from outside
            PinOrder = 6, BallOrder = 7;
        const float PaneLength = 3f, PaneWidth = 2.6f, CrackTime = 0.35f, BrokenTime = 7f, DoorSlide = 0.45f, DoorThickness = 0.55f,
            PinRadius = 0.3f, PinSpacing = 0.72f, BowlRadius = 0.85f, CarRadius = 0.95f;

        // Vertex colours are linear (the project renders in linear space): these are the colours as seen on screen.
        static Color L(string hex, float a = 1f) { ColorUtility.TryParseHtmlString("#" + hex, out var c); c = c.linear; c.a = a; return c; }
        static readonly Color Crimson = L("A3203A"), CrimsonDeep = L("7A1428"), Gold = L("E2B547"), GoldDark = L("A87A22"), RunnerEdge = L("3A0C16"),
            MarbleBand = L("1F2C2B"), Depth = L("232D45"), DepthLight = L("33415F"), Steel = L("8C99A8"), SteelDark = L("4E5966"),
            Glass = L("CFEAF7", 0.22f), GlassShine = L("FFFFFF", 0.32f), Crack = L("FFFFFF", 0.9f),
            Stone = L("EFE8DA"), StoneDark = L("C9BFAE"), Water = L("2B97D4"), WaterDeep = L("1A6AAE"), WaterLight = L("74CDF2", 0.75f),
            RopeRed = L("D8323A"), RopeWhite = L("F5F5F5"), Maple = L("E3B97E"), MapleDark = L("C99A5C"), Gutter = L("3C3F47"), LaneMark = L("9A5B2A"),
            PinWhite = L("FBFAF6"), PinRed = L("D8323A"), Outline = L("16283A"), BallBlue = L("2A3E9C"), BallShine = L("8FA3FF", 0.8f),
            Felt = L("2F7D4F"), FeltDark = L("24663F"), Chalk = L("E8F3EC", 0.55f), Wood = L("5A3418"), WoodLight = L("8A5A30"),
            DoorGlass = L("A8D2E8", 0.85f), Lamp = L("36D16A"), LampOff = L("D8323A"), Shadow = new(0.02f, 0.04f, 0.12f, 0.28f);
        static readonly Color[] BilliardColors = { L("F2C230"), L("2D5BD3"), L("D8323A"), L("6A3FB5"), L("F07A22"), L("2F9A4F"), L("8A1E2E"), L("1E1E22") };

        readonly List<GameObject> _parts = new();
        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        CarMovement[] _cars;
        readonly Dictionary<CarMovement, TrackSensor> _sensors = new();
        ParticleSystem _shards, _pinBurst;

        // ------------------------------------------------------------------ data at runtime

        class Door
        {
            public TrackLayout.SlidingDoor Data;
            public Vector2 Centre, Across, Along;
            public float Half;
            public Transform Left, Right;
            public Rigidbody2D LeftBody, RightBody;
            public MeshFilter Lamp;
            public float Open = 1f;
            public bool Held;
        }

        class Lane
        {
            public TrackLayout.Bowling Data;
            public float Lateral;
            public float NextBall;
            public Transform Pins;
            public float PinsDownAt = -100f;
            public readonly Transform[] PinParts = new Transform[10];
            public readonly Vector2[] PinFly = new Vector2[10];
        }

        class Ball
        {
            public Lane Lane;
            public float Along; // lap distance
            public Transform View;
            public readonly HashSet<CarMovement> Hit = new();
        }

        class Billiard
        {
            public TrackLayout.Billiards Hall;
            public Vector2 Position, Velocity;
            public Transform View;
        }

        class Pane
        {
            public TrackLayout.GlassFloor Floor;
            public int Row, Column, Columns;
            public float From, To, Left, Right; // lap distances, laterals (left > right)
            public bool Cracked;
            public float BreakAt = float.PositiveInfinity, BrokenUntil = -1f;
            public bool Broken => BrokenUntil > Track.Clock;
            public bool Breaking => !float.IsPositiveInfinity(BreakAt);
        }

        class Window
        {
            public Vector2 Point;
            public float LastBurst = -10f;
            public bool Shattered;
        }

        readonly List<Door> _doors = new();
        readonly List<Lane> _lanes = new();
        readonly List<Ball> _balls = new();
        readonly List<Billiard> _billiards = new();
        readonly List<Pane> _panes = new();
        readonly List<Window> _windows = new();
        MeshFilter _glassMesh, _crackMesh;
        bool _glassDirty;
        Rect _glassBounds;
        Mesh _ballMesh;
        readonly Mesh[] _billiardMeshes = new Mesh[16];

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Track.Guides.Contains(this)) Track.Guides.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Track.Guides.Remove(this);
            if (Track.GlassHole == (System.Func<Vector2, bool>)GlassHoleAt) Track.GlassHole = null;
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
        }

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var ball in _balls) if (ball.View != null) _parts.Add(ball.View.gameObject);
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _doors.Clear(); System.Array.Clear(_billiardMeshes, 0, _billiardMeshes.Length); _ballMesh = null; _lanes.Clear(); _balls.Clear(); _billiards.Clear(); _panes.Clear(); _windows.Clear();
            _glassMesh = _crackMesh = null;
            if (Track.GlassHole == (System.Func<Vector2, bool>)GlassHoleAt) Track.GlassHole = null;
            bool any = layout.houses != null && layout.houses.Exists(h => h.roof == TrackLayout.RoofKind.Chalet)
                       || layout.slidingDoors is { Count: > 0 } || layout.pools is { Count: > 0 } || layout.bowling is { Count: > 0 }
                       || layout.billiards is { Count: > 0 } || layout.glassFloors is { Count: > 0 };
            if (!any) return;
            BuildRunners(path, layout);
            if (layout.pools != null) foreach (var pool in layout.pools) BuildPool(pool);
            if (layout.slidingDoors != null) foreach (var door in layout.slidingDoors) BuildDoor(path, door);
            if (layout.bowling != null) foreach (var lane in layout.bowling) BuildBowling(path, lane);
            if (layout.billiards != null) foreach (var hall in layout.billiards) BuildBilliards(hall);
            if (layout.glassFloors is { Count: > 0 }) BuildGlass(path, layout);
            FindWindows(path, layout);
            if (Application.isPlaying && particleMaterial != null)
            {
                _shards = Particles("GlassShards", new Color(0.85f, 0.95f, 1f, 0.95f), 0.35f, new Vector2(3f, 9f), 0.5f, 1.1f);
                _pinBurst = Particles("PinBurst", new Color(1f, 1f, 1f, 0.9f), 0.3f, new Vector2(2f, 6f), 0.25f, 0.6f);
            }
        }

        // ------------------------------------------------------------------ chalet rooms

        bool IsChaletRoom(TrackLayout layout, TrackLayout.Room room)
        {
            Vector2 c = (room.min + room.max) * 0.5f;
            foreach (var house in layout.houses)
                if (house.roof == TrackLayout.RoofKind.Chalet && c.x >= house.min.x && c.x <= house.max.x && c.y >= house.min.y && c.y <= house.max.y)
                    return true;
            return false;
        }

        TrackLayout.Room ChaletRoomAt(TrackLayout layout, Vector2 p)
        {
            if (layout.rooms == null) return null;
            foreach (var room in layout.rooms)
                if (p.x >= room.min.x && p.x <= room.max.x && p.y >= room.min.y && p.y <= room.max.y && IsChaletRoom(layout, room)) return room;
            return null;
        }

        static bool InRect(Vector2 p, Vector2 min, Vector2 max, float grow = 0f) =>
            p.x >= min.x - grow && p.x <= max.x + grow && p.y >= min.y - grow && p.y <= max.y + grow;

        // Something of its own drawn on the road here (a pool, a lane, the billiard felt, glass): no runner.
        bool Covered(TrackPath path, TrackLayout layout, float d, Vector2 p)
        {
            if (track.InGap(d)) return true;
            if (track.TurntableAt(p) != null) return true;
            if (layout.pools != null) foreach (var pool in layout.pools) if (InRect(p, pool.min, pool.max)) return true;
            if (layout.billiards != null) foreach (var hall in layout.billiards) if (InRect(p, hall.min, hall.max)) return true;
            if (layout.bowling != null)
                foreach (var b in layout.bowling)
                    if (path.DeltaDistance(b.from, d) >= 0f && path.DeltaDistance(d, b.to + 3f) >= 0f) return true;
            if (layout.glassFloors != null)
                foreach (var g in layout.glassFloors)
                    if (path.DeltaDistance(g.from, d) >= 0f && path.DeltaDistance(d, g.to) >= 0f) return true;
            return false;
        }

        // Indoors, the road is a red runner with gold borders and a gold diamond now and then; on marble it is the polished
        // marble itself between dark bands with a gold inlay.
        void BuildRunners(TrackPath path, TrackLayout layout)
        {
            var runner = new Kit();
            var inlay = new Kit();
            var marble = new List<Vector3>(); var marbleUv = new List<Vector2>(); var marbleTris = new List<int>();
            var houses = GetComponent<TrackHouses>();
            float tile = houses != null ? houses.FloorTile : 6f;
            const float step = 0.5f;
            int k = 0;
            for (float d = 0f; d < path.Length; d += step, k++)
            {
                float d1 = Mathf.Min(path.Length, d + step);
                Vector2 mid = path.PointAt(d + step * 0.5f);
                var room = ChaletRoomAt(layout, mid);
                if (room == null || Covered(path, layout, d + step * 0.5f, mid)) continue;
                if (room.floor == TrackLayout.FloorKind.Glass) continue;
                Vector2 a = path.PointAt(d), b = path.PointAt(d1), na = path.NormalAt(d), nb = path.NormalAt(d1);
                float wa = track.OffRoadAt(d) + 0.1f, wb = track.OffRoadAt(d1) + 0.1f;
                void Band(Kit kit, float from, float to, Color color) =>
                    kit.Quad(a + na * from * wa, b + nb * from * wb, b + nb * to * wb, a + na * to * wa, color);
                void Strip(Kit kit, float fromM, float toM, Color color) =>
                    kit.Quad(a + na * fromM, b + nb * fromM, b + nb * toM, a + na * toM, color);
                if (room.floor == TrackLayout.FloorKind.Marble)
                {
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Strip(runner, s * wa, s * (wa - 0.65f), MarbleBand);
                        Strip(inlay, s * (wa - 0.65f), s * (wa - 0.8f), Gold);
                    }
                    int i = marble.Count;
                    float ia = wa - 0.7f, ib = wb - 0.7f;
                    foreach (var q in new[] { a - na * ia, b - nb * ib, b + nb * ib, a + na * ia }) { marble.Add(q); marbleUv.Add(q / tile); }
                    marbleTris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
                    continue;
                }
                Band(runner, -1f, 1f, Crimson);
                foreach (float s in new[] { -1f, 1f })
                {
                    Strip(runner, s * wa, s * (wa - 0.18f), RunnerEdge);
                    Strip(inlay, s * (wa - 0.3f), s * (wa - 0.62f), Gold);
                    Strip(runner, s * (wa - 0.62f), s * (wa - 0.95f), CrimsonDeep);
                }
                // A gold diamond in the middle every 3 m.
                if (k % 6 == 0)
                {
                    Vector2 t = path.TangentAt(d), n = path.NormalAt(d), c = path.PointAt(d);
                    inlay.Quad(c - t * 0.55f, c + n * 0.38f, c + t * 0.55f, c - n * 0.38f, Gold);
                    inlay.Quad(c - t * 0.28f, c + n * 0.19f, c + t * 0.28f, c - n * 0.19f, Crimson);
                }
            }
            if (runner.Count > 0) Part("ChaletRunner", runner.Build("ChaletRunner"), plain, RunnerOrder);
            if (inlay.Count > 0) Part("ChaletInlay", inlay.Build("ChaletInlay"), plain, InlayOrder);
            var marbleMaterial = houses != null ? houses.FloorMaterial(TrackLayout.FloorKind.Marble) : null;
            if (marble.Count > 0 && marbleMaterial != null)
            {
                var mesh = new Mesh { name = "ChaletMarble", hideFlags = HideFlags.DontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(marble); mesh.SetUVs(0, marbleUv); mesh.SetTriangles(marbleTris, 0); mesh.RecalculateBounds();
                Part("ChaletMarble", mesh, marbleMaterial, RunnerOrder);
            }
        }

        // ------------------------------------------------------------------ pools

        // A stone deck round blue water, light bands rippling across it, a lane rope down the middle, a ladder.
        void BuildPool(TrackLayout.Pool pool)
        {
            var kit = new Kit();
            Vector2 min = pool.min, max = pool.max, size = max - min;
            kit.Rect(min - Vector2.one * 1.1f, max + Vector2.one * 1.1f, Stone);
            for (float x = min.x - 1.1f; x < max.x + 1.1f; x += 1.1f)
                kit.Rect(new Vector2(x, min.y - 1.1f), new Vector2(x + 0.06f, max.y + 1.1f), StoneDark);
            kit.Rect(min - Vector2.one * 0.15f, max + Vector2.one * 0.15f, StoneDark);
            kit.Quad(min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y), WaterDeep, WaterDeep, Water, Water);
            var random = new System.Random((int)(min.x * 13 + min.y * 7));
            for (int i = 0; i < Mathf.RoundToInt(size.x * size.y / 6f); i++)
            {
                Vector2 c = min + new Vector2((float)random.NextDouble() * size.x, (float)random.NextDouble() * size.y);
                float w = 0.6f + (float)random.NextDouble() * 1.4f;
                if (c.x - w < min.x || c.x + w > max.x || c.y < min.y + 0.3f || c.y > max.y - 0.3f) continue;
                kit.Quad(c + new Vector2(-w, -0.06f), c + new Vector2(w, 0.04f), c + new Vector2(w, 0.14f), c + new Vector2(-w, 0.06f), WaterLight);
            }
            bool wide = size.x >= size.y;
            for (float u = 0f; u < (wide ? size.x : size.y); u += 0.9f)
            {
                Color rope = Mathf.FloorToInt(u / 0.9f) % 2 == 0 ? RopeRed : RopeWhite;
                if (wide) kit.Rect(new Vector2(min.x + u, min.y + size.y * 0.5f - 0.1f), new Vector2(Mathf.Min(max.x, min.x + u + 0.8f), min.y + size.y * 0.5f + 0.1f), rope);
                else kit.Rect(new Vector2(min.x + size.x * 0.5f - 0.1f, min.y + u), new Vector2(min.x + size.x * 0.5f + 0.1f, Mathf.Min(max.y, min.y + u + 0.8f)), rope);
            }
            // The ladder at a corner.
            Vector2 l = new(max.x - 1.6f, max.y);
            kit.Rect(l + new Vector2(-0.5f, -1.2f), l + new Vector2(-0.38f, 0.5f), Steel);
            kit.Rect(l + new Vector2(0.38f, -1.2f), l + new Vector2(0.5f, 0.5f), Steel);
            for (int r = 0; r < 3; r++) kit.Rect(l + new Vector2(-0.4f, -1.1f + r * 0.4f), l + new Vector2(0.4f, -1.0f + r * 0.4f), SteelDark);
            Part("Pool", kit.Build("Pool"), plain, RunnerOrder);
        }

        // ------------------------------------------------------------------ sliding doors

        void BuildDoor(TrackPath path, TrackLayout.SlidingDoor data)
        {
            var door = new Door { Data = data };
            door.Centre = path.PointAt(data.distance);
            door.Along = path.TangentAt(data.distance);
            door.Across = path.NormalAt(data.distance);
            door.Half = track.OffRoadAt(data.distance) + 1.2f;
            var root = new GameObject("SlidingDoor") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var leaf = LeafMesh(door.Half);
            door.Left = Leaf(root, "Left", leaf, out door.LeftBody);
            door.Right = Leaf(root, "Right", leaf, out door.RightBody);
            // A lamp over the doorway on the side the cars come from: green open, red shut.
            var lamp = new Kit();
            Vector2 at = door.Centre - door.Along * 1.1f + door.Across * (door.Half - 0.4f);
            lamp.Disc(at, 0.42f, Outline, 14);
            lamp.Disc(at, 0.32f, Color.white, 14);
            var lampPart = Part("DoorLamp", lamp.Build("DoorLamp"), plain, LampOrder);
            door.Lamp = lampPart.GetComponent<MeshFilter>();
            _doors.Add(door);
            Place(door, 1f, snap: true);
        }

        // A wooden leaf with three glass panes and a gold handle at its meeting edge, centred on its origin, lying along x.
        Mesh LeafMesh(float half)
        {
            var kit = new Kit();
            float w = half * 0.5f, h = DoorThickness * 0.5f;
            kit.Rect(new Vector2(-w - 0.05f, -h - 0.08f), new Vector2(w + 0.05f, h + 0.08f), Outline);
            kit.Rect(new Vector2(-w, -h), new Vector2(w, h), Wood);
            for (int i = 0; i < 3; i++)
            {
                float x0 = -w + 0.25f + i * (2f * w - 0.5f) / 3f, x1 = x0 + (2f * w - 0.5f) / 3f - 0.2f;
                kit.Rect(new Vector2(x0, -h * 0.45f), new Vector2(x1, h * 0.45f), DoorGlass);
            }
            kit.Rect(new Vector2(-w, h * 0.55f), new Vector2(w, h), WoodLight);
            kit.Disc(new Vector2(-w + 0.35f, 0f), 0.16f, Gold, 10);
            return kit.Build("DoorLeaf");
        }

        Transform Leaf(Transform root, string name, Mesh mesh, out Rigidbody2D body)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(root, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = plain;
            r.sortingOrder = DoorOrder;
            body = null;
            if (Application.isPlaying)
            {
                body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(mesh.bounds.size.x, DoorThickness);
            }
            return go.transform;
        }

        static float Openness(TrackLayout.SlidingDoor d, float time)
        {
            float u = Mathf.Repeat(time + d.phase, d.period), open = d.open * d.period;
            if (u < DoorSlide) return u / DoorSlide;
            if (u < open) return 1f;
            if (u < open + DoorSlide) return 1f - (u - open) / DoorSlide;
            return 0f;
        }

        // Seconds from now until the door has been fully open for a moment (0 if it is open now and stays open a while).
        float UntilOpen(TrackLayout.SlidingDoor d, float time, float need)
        {
            for (float s = 0f; s < d.period * 2f; s += 0.05f)
            {
                bool ok = true;
                for (float x = 0f; x <= need; x += 0.1f) if (Openness(d, time + s + x) < 0.85f) { ok = false; break; }
                if (ok) return s;
            }
            return d.period;
        }

        void Place(Door door, float open, bool snap = false)
        {
            float w = door.Half * 0.5f;
            float angle = Mathf.Atan2(door.Across.y, door.Across.x) * Mathf.Rad2Deg;
            // Shut, each leaf covers its half of the doorway; open, it has slid away into the wall beside it.
            Vector2 left = door.Centre + door.Across * (w + open * door.Half * 0.95f);
            Vector2 right = door.Centre - door.Across * (w + open * door.Half * 0.95f);
            var rotL = Quaternion.Euler(0f, 0f, angle);
            var rotR = Quaternion.Euler(0f, 0f, angle + 180f);
            if (snap && door.LeftBody != null)
            {
                door.LeftBody.position = left; door.LeftBody.rotation = angle;
                door.RightBody.position = right; door.RightBody.rotation = angle + 180f;
            }
            if (door.LeftBody != null && Application.isPlaying && !snap)
            {
                door.LeftBody.MovePosition(left); door.LeftBody.MoveRotation(angle);
                door.RightBody.MovePosition(right); door.RightBody.MoveRotation(angle + 180f);
            }
            else
            {
                door.Left.SetPositionAndRotation(left, rotL);
                door.Right.SetPositionAndRotation(right, rotR);
            }
        }

        void UpdateDoors(float time)
        {
            foreach (var door in _doors)
            {
                float target = Openness(door.Data, time);
                // A car in the doorway holds the doors open (they have a sensor, like real ones).
                door.Held = false;
                foreach (var car in Cars())
                {
                    Vector2 rel = car.Body.position - door.Centre;
                    if (Mathf.Abs(Vector2.Dot(rel, door.Along)) < DoorThickness * 0.5f + CarRadius + 0.2f && Mathf.Abs(Vector2.Dot(rel, door.Across)) < door.Half)
                    { door.Held = true; break; }
                }
                if (door.Held && door.Open > 0.6f) target = Mathf.Max(target, door.Open);
                door.Open = target;
                Place(door, door.Open);
                if (door.Lamp != null)
                {
                    bool shutting = Openness(door.Data, time + 0.8f) < 0.99f;
                    Color c = door.Open > 0.99f && !shutting ? Lamp : door.Open > 0.99f ? (Mathf.Repeat(time * 6f, 1f) < 0.5f ? LampOff : Lamp) : LampOff;
                    var mesh = door.Lamp.sharedMesh;
                    var colors = new List<Color>();
                    mesh.GetColors(colors);
                    for (int i = colors.Count / 2; i < colors.Count; i++) colors[i] = c;
                    mesh.SetColors(colors);
                }
            }
        }

        // ------------------------------------------------------------------ bowling

        void BuildBowling(TrackPath path, TrackLayout.Bowling data)
        {
            var kit = new Kit();
            float length = path.DeltaDistance(data.from, data.to);
            const float laneHalf = 1.15f, gutter = 0.42f;
            // The lanes: maple boards between dark gutters, arrows a third of the way down, the foul line.
            for (float s = 0f; s < length + 3f; s += 0.5f)
            {
                float d0 = data.from + s, d1 = data.from + Mathf.Min(length + 3f, s + 0.5f);
                Vector2 a = path.PointAt(d0), b = path.PointAt(d1), na = path.NormalAt(d0), nb = path.NormalAt(d1);
                float w = track.OffRoadAt(d0) + 0.1f;
                kit.Quad(a - na * w, b - nb * w, b + nb * w, a + na * w, Gutter);
                foreach (float lane in data.lanes)
                {
                    int board = 0;
                    for (float x = -laneHalf; x < laneHalf - 0.01f; x += 0.23f, board++)
                        kit.Quad(a + na * (lane + x), b + nb * (lane + x), b + nb * (lane + x + 0.23f), a + na * (lane + x + 0.23f), board % 2 == 0 ? Maple : MapleDark);
                    foreach (float side in new[] { -1f, 1f })
                        kit.Quad(a + na * (lane + side * laneHalf), b + nb * (lane + side * laneHalf), b + nb * (lane + side * (laneHalf + gutter)), a + na * (lane + side * (laneHalf + gutter)), Outline);
                }
            }
            foreach (float lane in data.lanes)
            {
                for (int i = -2; i <= 2; i++)
                {
                    float d = data.from + length * 0.62f + Mathf.Abs(i) * 0.8f;
                    Vector2 c = path.PointAt(d) + path.NormalAt(d) * (lane + i * 0.42f), t = path.TangentAt(d), n = path.NormalAt(d);
                    kit.Tri(c + t * 0.45f, c - t * 0.25f + n * 0.14f, c - t * 0.25f - n * 0.14f, LaneMark);
                }
                Vector2 f = path.PointAt(data.from + 0.6f), fn = path.NormalAt(data.from + 0.6f), ft = path.TangentAt(data.from + 0.6f);
                kit.Quad(f + fn * (lane - laneHalf) - ft * 0.08f, f + fn * (lane - laneHalf) + ft * 0.08f, f + fn * (lane + laneHalf) + ft * 0.08f, f + fn * (lane + laneHalf) - ft * 0.08f, PinRed);
            }
            Part("BowlingLanes", kit.Build("BowlingLanes"), plain, RunnerOrder);

            // The pins: ten on each lane, the head pin nearest the drivers.
            var pin = PinMesh();
            foreach (float lateral in data.lanes)
            {
                var lane = new Lane { Data = data, Lateral = lateral, NextBall = Random.value * data.every };
                var root = new GameObject("Pins") { hideFlags = HideFlags.DontSave }.transform;
                root.SetParent(transform, false);
                Vector2 t = path.TangentAt(data.to), n = path.NormalAt(data.to);
                root.position = path.PointAt(data.to) + n * lateral;
                root.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg - 90f);
                _parts.Add(root.gameObject);
                int k = 0;
                for (int row = 0; row < 4; row++)
                    for (int i = 0; i <= row; i++, k++)
                    {
                        var go = new GameObject("Pin", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
                        go.transform.SetParent(root, false);
                        go.transform.localPosition = new Vector3((i - row * 0.5f) * PinSpacing, row * PinSpacing * 0.87f, 0f);
                        Assign(go.GetComponent<MeshFilter>(), pin);
                        var r = go.GetComponent<MeshRenderer>();
                        r.sharedMaterial = plain;
                        r.sortingOrder = PinOrder;
                        lane.PinParts[k] = go.transform;
                    }
                lane.Pins = root;
                _lanes.Add(lane);
            }
            if (_ballMesh == null) _ballMesh = BowlingBallMesh();
        }

        Mesh PinMesh()
        {
            var kit = new Kit();
            kit.Disc(new Vector2(0.06f, -0.06f), PinRadius + 0.05f, Shadow, 14);
            kit.Disc(Vector2.zero, PinRadius + 0.05f, Outline, 14);
            kit.Disc(Vector2.zero, PinRadius, PinWhite, 14);
            kit.Ring(Vector2.zero, PinRadius * 0.55f, PinRadius * 0.78f, PinRed, 14);
            kit.Disc(new Vector2(-0.07f, 0.07f), PinRadius * 0.3f, Color.white, 8);
            return kit.Build("Pin");
        }

        Mesh BowlingBallMesh()
        {
            var kit = new Kit();
            kit.Disc(new Vector2(0.18f, -0.18f), BowlRadius, Shadow, 20);
            kit.Disc(Vector2.zero, BowlRadius + 0.06f, Outline, 20);
            kit.Disc(Vector2.zero, BowlRadius, BallBlue, 20);
            kit.Disc(new Vector2(-0.25f, 0.28f), BowlRadius * 0.35f, BallShine, 12);
            kit.Disc(new Vector2(0.12f, 0.22f), 0.11f, Outline, 8);
            kit.Disc(new Vector2(0.32f, 0.05f), 0.11f, Outline, 8);
            kit.Disc(new Vector2(0.1f, -0.25f), 0.14f, Outline, 8);
            return kit.Build("BowlingBall");
        }

        void UpdateBowling(float dt)
        {
            if (_lanes.Count == 0) return;
            var path = track.Path;
            float time = Track.Clock;
            foreach (var lane in _lanes)
            {
                // A new ball rolls out of the pin deck now and then.
                if (Racing && time >= lane.NextBall)
                {
                    lane.NextBall = time + lane.Data.every * Random.Range(0.75f, 1.3f);
                    var view = new GameObject("BowlingBall", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
                    view.transform.SetParent(transform, false);
                    view.GetComponent<MeshFilter>().sharedMesh = _ballMesh;
                    var r = view.GetComponent<MeshRenderer>();
                    r.sharedMaterial = plain;
                    r.sortingOrder = BallOrder;
                    _balls.Add(new Ball { Lane = lane, Along = lane.Data.to - 3.5f, View = view.transform });
                }
                // Knocked-down pins fly apart, then stand up again.
                float since = time - lane.PinsDownAt;
                for (int k = 0; k < 10; k++)
                {
                    var p = lane.PinParts[k];
                    int row = k < 1 ? 0 : k < 3 ? 1 : k < 6 ? 2 : 3, i = k - row * (row + 1) / 2;
                    Vector3 home = new((i - row * 0.5f) * PinSpacing, row * PinSpacing * 0.87f, 0f);
                    if (since < 3f)
                    {
                        float u = Mathf.Clamp01(since / 0.7f);
                        p.localPosition = home + (Vector3)(lane.PinFly[k] * (1f - (1f - u) * (1f - u)));
                        p.localRotation = Quaternion.Euler(0f, 0f, since * 720f * (k % 2 == 0 ? 1f : -1f));
                        p.localScale = Vector3.one * (since < 2.6f ? 1f : Mathf.Clamp01((3f - since) / 0.4f));
                    }
                    else
                    {
                        p.localPosition = home;
                        p.localRotation = Quaternion.identity;
                        p.localScale = Vector3.one * Mathf.Clamp01((since - 3f) / 0.3f);
                    }
                }
            }
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var ball = _balls[i];
                ball.Along -= ball.Lane.Data.speed * dt;
                if (path.DeltaDistance(ball.Lane.Data.from - 1f, ball.Along) < 0f)
                {
                    Destroy(ball.View.gameObject);
                    _balls.RemoveAt(i);
                    continue;
                }
                Vector2 at = path.PointAt(ball.Along) + path.NormalAt(ball.Along) * ball.Lane.Lateral;
                ball.View.position = at;
                ball.View.rotation = Quaternion.Euler(0f, 0f, ball.Along * 70f);
                foreach (var car in Cars())
                {
                    if (car.IsAirborne || ball.Hit.Contains(car)) continue;
                    if ((car.Body.position - at).sqrMagnitude > (BowlRadius + CarRadius) * (BowlRadius + CarRadius)) continue;
                    ball.Hit.Add(car);
                    Vector2 n = path.NormalAt(ball.Along);
                    car.Slip(Vector2.Dot(car.Body.position - at, n) >= 0f ? 1 : -1);
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.bumper, 0.8f);
                    if (IsPlayer(car)) Banner("GUTTER BALL!", new Color(0.55f, 0.65f, 1f));
                }
            }
            // Through the pins: STRIKE!
            foreach (var lane in _lanes)
            {
                if (time - lane.PinsDownAt < 3.3f) continue;
                Vector2 pins = (Vector2)lane.Pins.position + (Vector2)(lane.Pins.up * PinSpacing * 1.3f);
                foreach (var car in Cars())
                {
                    if (car.IsAirborne || (car.Body.position - pins).sqrMagnitude > 2.3f * 2.3f) continue;
                    lane.PinsDownAt = time;
                    Vector2 fwd = car.Body.linearVelocity.sqrMagnitude > 1f ? car.Body.linearVelocity.normalized : (Vector2)lane.Pins.up;
                    for (int k = 0; k < 10; k++)
                    {
                        Vector2 local = (Vector2)lane.PinParts[k].localPosition;
                        Vector2 dir = (Vector2)lane.Pins.InverseTransformDirection(fwd) * 2.2f + local.normalized * 1.6f + Random.insideUnitCircle;
                        lane.PinFly[k] = dir * Random.Range(1.4f, 2.4f);
                    }
                    if (_pinBurst != null) { _pinBurst.transform.position = pins; _pinBurst.Emit(30); }
                    car.GiveBoost(0.5f);
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.crash, 0.7f);
                    if (IsPlayer(car)) Banner("STRIKE!", new Color(1f, 0.82f, 0.3f));
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ billiards

        void BuildBilliards(TrackLayout.Billiards hall)
        {
            var kit = new Kit();
            kit.Rect(hall.min, hall.max, Felt);
            // Darker felt in a border, chalk diamonds along it, a chalk line where the road runs (drawn by the road edges).
            kit.Rect(hall.min, new Vector2(hall.max.x, hall.min.y + 0.9f), FeltDark);
            kit.Rect(new Vector2(hall.min.x, hall.max.y - 0.9f), hall.max, FeltDark);
            kit.Rect(hall.min, new Vector2(hall.min.x + 0.9f, hall.max.y), FeltDark);
            kit.Rect(new Vector2(hall.max.x - 0.9f, hall.min.y), hall.max, FeltDark);
            for (float x = hall.min.x + 3f; x < hall.max.x - 2f; x += 4f)
                foreach (float y in new[] { hall.min.y + 0.45f, hall.max.y - 0.45f })
                    kit.Quad(new Vector2(x - 0.25f, y), new Vector2(x, y + 0.2f), new Vector2(x + 0.25f, y), new Vector2(x, y - 0.2f), Chalk);
            var path = track.Path;
            for (float d = 0f; d < path.Length; d += 0.5f)
            {
                Vector2 p = path.PointAt(d + 0.25f);
                if (!InRect(p, hall.min, hall.max)) continue;
                Vector2 a = path.PointAt(d), b = path.PointAt(d + 0.5f), na = path.NormalAt(d), nb = path.NormalAt(d + 0.5f);
                float w = track.OffRoadAt(d);
                foreach (float s in new[] { -1f, 1f })
                    kit.Quad(a + na * s * (w - 0.08f), b + nb * s * (w - 0.08f), b + nb * s * (w + 0.08f), a + na * s * (w + 0.08f), Chalk);
            }
            Part("BilliardFelt", kit.Build("BilliardFelt"), plain, RunnerOrder);
            var random = new System.Random((int)(hall.min.x * 31 + hall.min.y * 17));
            for (int i = 0; i < hall.balls; i++)
            {
                int colour = i % BilliardColors.Length;
                bool stripe = i % 2 == 1 && colour != 7; // the black eight is never striped
                int key = colour + (stripe ? 8 : 0);
                if (_billiardMeshes[key] == null) _billiardMeshes[key] = BilliardMesh(BilliardColors[colour], hall.radius, stripe);
                var view = new GameObject("Billiard", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
                view.transform.SetParent(transform, false);
                view.GetComponent<MeshFilter>().sharedMesh = _billiardMeshes[key];
                var r = view.GetComponent<MeshRenderer>();
                r.sharedMaterial = plain;
                r.sortingOrder = BallOrder;
                _parts.Add(view);
                float a = (float)random.NextDouble() * Mathf.PI * 2f;
                var ball = new Billiard
                {
                    Hall = hall,
                    Position = new Vector2(Mathf.Lerp(hall.min.x + 2f, hall.max.x - 2f, (float)random.NextDouble()), Mathf.Lerp(hall.min.y + 2f, hall.max.y - 2f, (float)random.NextDouble())),
                    Velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * hall.speed,
                    View = view.transform,
                };
                view.transform.position = ball.Position;
                _billiards.Add(ball);
            }
        }

        Mesh BilliardMesh(Color colour, float radius, bool stripe)
        {
            var kit = new Kit();
            kit.Disc(new Vector2(0.25f, -0.25f), radius, Shadow, 22);
            kit.Disc(Vector2.zero, radius + 0.07f, Outline, 22);
            kit.Disc(Vector2.zero, radius, stripe ? PinWhite : colour, 22);
            if (stripe) kit.Rect(new Vector2(-radius * 0.92f, -radius * 0.42f), new Vector2(radius * 0.92f, radius * 0.42f), colour);
            kit.Disc(Vector2.zero, radius * 0.42f, PinWhite, 16);
            kit.Disc(Vector2.zero, radius * 0.16f, Outline, 10);
            kit.Disc(new Vector2(-radius * 0.38f, radius * 0.4f), radius * 0.22f, new Color(1f, 1f, 1f, 0.55f), 10);
            return kit.Build("Billiard");
        }

        void UpdateBilliards(float dt)
        {
            if (_billiards.Count == 0) return;
            foreach (var b in _billiards)
            {
                var hall = b.Hall;
                float r = hall.radius;
                b.Position += b.Velocity * dt;
                // Off the cushions.
                if (b.Position.x < hall.min.x + r) { b.Position.x = hall.min.x + r; b.Velocity.x = Mathf.Abs(b.Velocity.x); }
                if (b.Position.x > hall.max.x - r) { b.Position.x = hall.max.x - r; b.Velocity.x = -Mathf.Abs(b.Velocity.x); }
                if (b.Position.y < hall.min.y + r) { b.Position.y = hall.min.y + r; b.Velocity.y = Mathf.Abs(b.Velocity.y); }
                if (b.Position.y > hall.max.y - r) { b.Position.y = hall.max.y - r; b.Velocity.y = -Mathf.Abs(b.Velocity.y); }
            }
            // Off each other.
            for (int i = 0; i < _billiards.Count; i++)
                for (int j = i + 1; j < _billiards.Count; j++)
                {
                    var a = _billiards[i]; var b = _billiards[j];
                    if (a.Hall != b.Hall) continue;
                    Vector2 d = b.Position - a.Position;
                    float min = a.Hall.radius * 2f;
                    if (d.sqrMagnitude >= min * min || d.sqrMagnitude < 1e-4f) continue;
                    Vector2 n = d.normalized;
                    float overlap = min - d.magnitude;
                    a.Position -= n * overlap * 0.5f; b.Position += n * overlap * 0.5f;
                    float va = Vector2.Dot(a.Velocity, n), vb = Vector2.Dot(b.Velocity, n);
                    if (va - vb <= 0f) continue;
                    a.Velocity += n * (vb - va); b.Velocity += n * (va - vb);
                }
            // Off the cars (the car is knocked aside too).
            foreach (var b in _billiards)
            {
                float r = b.Hall.radius;
                foreach (var car in Cars())
                {
                    if (car.IsAirborne) continue;
                    Vector2 d = car.Body.position - b.Position;
                    float min = r + CarRadius;
                    if (d.sqrMagnitude >= min * min || d.sqrMagnitude < 1e-4f) continue;
                    Vector2 n = d.normalized;
                    car.Body.position = b.Position + n * min;
                    float into = Vector2.Dot(b.Velocity - car.Body.linearVelocity, n);
                    if (into > 0f || Vector2.Dot(car.Body.linearVelocity, -n) > 2f)
                    {
                        car.Bump(n, 5f);
                        car.Body.linearVelocity += n * Mathf.Max(4f, into * 0.8f);
                        SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.bumper, 0.6f);
                    }
                    b.Velocity = Vector2.Reflect(b.Velocity, -n);
                    if (Vector2.Dot(b.Velocity, -n) < 0f) b.Velocity = -n * b.Velocity.magnitude;
                }
                // Always rolling at the hall's pace.
                b.Velocity = b.Velocity.sqrMagnitude > 0.01f ? b.Velocity.normalized * b.Hall.speed : Random.insideUnitCircle.normalized * b.Hall.speed;
                b.View.position = b.Position;
            }
        }

        // ------------------------------------------------------------------ glass floors

        void BuildGlass(TrackPath path, TrackLayout layout)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (var floor in layout.glassFloors)
            {
                float length = path.DeltaDistance(floor.from, floor.to);
                int rows = Mathf.Max(1, Mathf.RoundToInt(length / PaneLength));
                float rowLength = length / rows;
                for (int row = 0; row < rows; row++)
                {
                    float d0 = floor.from + row * rowLength, d1 = d0 + rowLength;
                    float w = track.OffRoadAt(d0 + rowLength * 0.5f);
                    int columns = Mathf.Max(2, Mathf.RoundToInt(2f * w / PaneWidth));
                    int crackedInRow = 0;
                    var rowPanes = new List<Pane>();
                    for (int col = 0; col < columns; col++)
                    {
                        float left = w - col * 2f * w / columns, right = left - 2f * w / columns;
                        float h = Hash(floor.seed * 7919 + row * 131 + col * 17);
                        bool cracked = row > 0 && row < rows - 1 && h < floor.cracked;
                        var pane = new Pane { Floor = floor, Row = row, Column = col, Columns = columns, From = d0, To = d1, Left = left, Right = right, Cracked = cracked };
                        if (cracked) crackedInRow++;
                        rowPanes.Add(pane);
                    }
                    // Never a whole row cracked: one pane always holds.
                    if (crackedInRow == columns) rowPanes[(int)(Hash(floor.seed + row * 3) * columns) % columns].Cracked = false;
                    _panes.AddRange(rowPanes);
                    for (float s = d0; s <= d1; s += 2f)
                    {
                        Vector2 p = path.PointAt(s);
                        xMin = Mathf.Min(xMin, p.x - w - 2f); xMax = Mathf.Max(xMax, p.x + w + 2f);
                        yMin = Mathf.Min(yMin, p.y - w - 2f); yMax = Mathf.Max(yMax, p.y + w + 2f);
                    }
                }
            }
            _glassBounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            _glassMesh = Part("GlassFloor", new Mesh(), plain, GlassOrder).GetComponent<MeshFilter>();
            _crackMesh = Part("GlassCracks", new Mesh(), plain, CrackOrder).GetComponent<MeshFilter>();
            RebuildGlass();
            if (Application.isPlaying) Track.GlassHole = GlassHoleAt;
        }

        static float Hash(int n)
        {
            uint h = (uint)n * 2654435761u;
            h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        // The abyss far below (dark, faint streaks of rock and mist), the panes over it in steel frames, cracks on the
        // cracked ones; a shattered pane shows the open drop with jagged shards round its frame.
        void RebuildGlass()
        {
            var path = track.Path;
            var glass = new Kit();
            var cracks = new Kit();
            foreach (var pane in _panes)
            {
                Vector2 Corner(float d, float lat) => path.PointAt(d) + path.NormalAt(d) * lat;
                Vector2 a = Corner(pane.From, pane.Right), b = Corner(pane.To, pane.Right), c = Corner(pane.To, pane.Left), e = Corner(pane.From, pane.Left);
                glass.Quad(a, b, c, e, Depth);
                float seed = pane.Row * 3.7f + pane.Column * 1.3f;
                Vector2 mid = (a + c) * 0.5f;
                glass.Quad(Vector2.Lerp(a, e, 0.2f + 0.3f * Frac(seed)), Vector2.Lerp(b, c, 0.1f + 0.3f * Frac(seed * 1.7f)),
                    Vector2.Lerp(b, c, 0.25f + 0.3f * Frac(seed * 1.7f)), Vector2.Lerp(a, e, 0.32f + 0.3f * Frac(seed)), DepthLight);
                if (pane.Broken)
                {
                    // Jagged shards still stuck in the frame.
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2 p0 = k == 0 ? a : k == 1 ? b : k == 2 ? c : e, p1 = k == 0 ? b : k == 1 ? c : k == 2 ? e : a;
                        for (int s = 0; s < 3; s++)
                        {
                            Vector2 q0 = Vector2.Lerp(p0, p1, s / 3f), q1 = Vector2.Lerp(p0, p1, (s + 1) / 3f);
                            Vector2 tip = Vector2.Lerp((q0 + q1) * 0.5f, mid, 0.18f + 0.15f * Frac(seed + k * 2.1f + s));
                            cracks.Tri(q0, q1, tip, Glass);
                        }
                    }
                }
                else
                {
                    Color tint = pane.Breaking ? new Color(1f, 1f, 1f, 0.35f) : Glass;
                    glass.Quad(a, b, c, e, tint);
                    glass.Quad(Vector2.Lerp(a, b, 0.1f), Vector2.Lerp(a, b, 0.24f), Vector2.Lerp(e, c, 0.56f), Vector2.Lerp(e, c, 0.42f), GlassShine);
                    if (pane.Cracked)
                    {
                        // A star of cracks from a point off the middle.
                        Vector2 hit = Vector2.Lerp(Vector2.Lerp(a, b, 0.3f + 0.4f * Frac(seed * 2.3f)), Vector2.Lerp(e, c, 0.3f + 0.4f * Frac(seed * 2.3f)), 0.3f + 0.4f * Frac(seed * 3.1f));
                        int rays = pane.Breaking ? 9 : 6;
                        for (int r = 0; r < rays; r++)
                        {
                            float ang = r * Mathf.PI * 2f / rays + Frac(seed + r) * 0.6f;
                            Vector2 dir = new(Mathf.Cos(ang), Mathf.Sin(ang));
                            float len = 0.7f + 0.8f * Frac(seed * 1.3f + r * 0.7f);
                            Vector2 bend = hit + dir * len * 0.5f + new Vector2(-dir.y, dir.x) * 0.12f;
                            cracks.Line(hit, bend, 0.05f, Crack);
                            cracks.Line(bend, hit + dir * len, 0.04f, Crack);
                        }
                        cracks.Ring(hit, 0.22f, 0.27f, Crack, 10);
                    }
                }
                // The steel frame.
                cracks.Line(a, b, 0.09f, Steel);
                cracks.Line(e, c, 0.09f, Steel);
                cracks.Line(a, e, 0.09f, Steel);
                cracks.Line(b, c, 0.09f, Steel);
            }
            var gm = glass.Build("GlassFloor"); var cm = cracks.Build("GlassCracks");
            if (_glassMesh.sharedMesh != null) DestroyImmediateOrNot(_glassMesh.sharedMesh);
            if (_crackMesh.sharedMesh != null) DestroyImmediateOrNot(_crackMesh.sharedMesh);
            _glassMesh.sharedMesh = gm;
            _crackMesh.sharedMesh = cm;
            _glassDirty = false;
        }

        static void DestroyImmediateOrNot(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        static float Frac(float x) => Mathf.Repeat(Mathf.Sin(x * 12.9898f) * 43758.5453f, 1f);

        Pane PaneAt(float d, float lateral)
        {
            var path = track.Path;
            foreach (var pane in _panes)
                if (path.DeltaDistance(pane.From, d) >= 0f && path.DeltaDistance(d, pane.To) >= 0f && lateral <= pane.Left && lateral >= pane.Right)
                    return pane;
            return null;
        }

        bool GlassHoleAt(Vector2 p)
        {
            if (_panes.Count == 0 || !_glassBounds.Contains(p)) return false;
            var point = track.Path.Project(p);
            var pane = PaneAt(point.Distance, point.Lateral);
            return pane != null && pane.Broken;
        }

        void UpdateGlass()
        {
            if (_panes.Count == 0) return;
            float time = Track.Clock;
            foreach (var car in Cars())
            {
                if (car.IsAirborne || !_glassBounds.Contains(car.Body.position)) continue;
                var s = Sensor(car);
                if (s == null) continue;
                var pane = PaneAt(s.Current.Distance, s.Current.Lateral);
                if (pane == null || !pane.Cracked || pane.Broken || pane.Breaking) continue;
                pane.BreakAt = time + CrackTime;
                _glassDirty = true;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 0.6f);
            }
            foreach (var pane in _panes)
            {
                if (pane.Breaking && time >= pane.BreakAt)
                {
                    pane.BreakAt = float.PositiveInfinity;
                    pane.BrokenUntil = time + BrokenTime;
                    _glassDirty = true;
                    var path = track.Path;
                    Vector2 c = path.PointAt((pane.From + pane.To) * 0.5f) + path.NormalAt((pane.From + pane.To) * 0.5f) * (pane.Left + pane.Right) * 0.5f;
                    if (_shards != null) { _shards.transform.position = c; _shards.Emit(26); }
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 1f);
                }
                else if (pane.BrokenUntil > 0f && time >= pane.BrokenUntil)
                {
                    pane.BrokenUntil = -1f; // a new pane: cracked again
                    _glassDirty = true;
                }
            }
            if (_glassDirty) RebuildGlass();
        }

        // ------------------------------------------------------------------ panoramic windows

        // A jump that takes off (or lands) at a chalet's wall goes through a panoramic window: it bursts as the cars fly through.
        void FindWindows(TrackPath path, TrackLayout layout)
        {
            foreach (var gap in layout.gaps)
                foreach (float at in new[] { gap.from, gap.to })
                {
                    Vector2 p = path.PointAt(at);
                    foreach (var room in layout.rooms)
                    {
                        if (!IsChaletRoom(layout, room)) continue;
                        if (!InRect(p, room.min, room.max, 4f) || InRect(p, room.min, room.max, -4f)) continue;
                        // The wall crossing nearest to it.
                        for (float s = -5f; s <= 5f; s += 0.25f)
                        {
                            bool a = InRect(path.PointAt(at + s), room.min, room.max), b = InRect(path.PointAt(at + s + 0.25f), room.min, room.max);
                            if (a == b) continue;
                            _windows.Add(new Window { Point = path.PointAt(at + s) });
                            break;
                        }
                    }
                }
        }

        void UpdateWindows()
        {
            if (_windows.Count == 0 || _shards == null) return;
            foreach (var w in _windows)
                foreach (var car in Cars())
                {
                    if (!car.IsAirborne || (car.Body.position - w.Point).sqrMagnitude > 3f * 3f || Time.time - w.LastBurst < 0.6f) continue;
                    w.LastBurst = Time.time;
                    _shards.transform.position = car.Body.position;
                    _shards.Emit(w.Shattered ? 10 : 45);
                    if (!w.Shattered) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 1f);
                    w.Shattered = true;
                }
        }

        // ------------------------------------------------------------------ the loop

        bool Racing => _race != null && _race.State == SomeGame.Race.RaceState.Racing;

        IEnumerable<CarMovement> Cars()
        {
            if (_cars == null) _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            foreach (var car in _cars)
                if (car != null && car.isActiveAndEnabled && !car.IsPuppet && !car.IsCarried) yield return car;
        }

        TrackSensor Sensor(CarMovement car)
        {
            if (!_sensors.TryGetValue(car, out var s)) _sensors[car] = s = car.GetComponent<TrackSensor>();
            return s;
        }

        bool IsPlayer(CarMovement car) => _race != null && _race.Player != null && _race.Player.gameObject == car.gameObject;

        void Banner(string text, Color color)
        {
            if (_combo == null) _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            if (_combo != null) _combo.Banner(text, color);
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying) return;
            float time = Track.Clock;
            UpdateDoors(time);
            UpdateBilliards(Time.fixedDeltaTime);
            UpdateBowling(Time.fixedDeltaTime);
            UpdateGlass();
            UpdateWindows();
        }

        // ------------------------------------------------------------------ the rivals

        public void Guide(CarMovement car, int seed, float distance, ref float lateral, ref float speed)
        {
            var path = track.Path;
            float v = Mathf.Max(6f, car.ForwardSpeed);
            float time = Track.Clock;
            // Sliding doors: arrive while they stand open (or wait for them).
            foreach (var door in _doors)
            {
                float to = path.DeltaDistance(distance, door.Data.distance);
                if (to < 0.5f || to > 40f) continue;
                float arrive = to / v;
                float wait = UntilOpen(door.Data, time + arrive, 0.5f);
                if (wait <= 0.01f) continue;
                float until = UntilOpen(door.Data, time, 0.5f + to / 20f);
                // Slow enough to get there just as it opens; right in front of a shut door, stop.
                speed = Mathf.Min(speed, to < 5f ? 0f : Mathf.Max(3f, (to - 2f) / Mathf.Max(0.3f, until)));
            }
            // Bowling balls rolling at you: change lanes.
            foreach (var lane in _lanes)
            {
                float into = path.DeltaDistance(lane.Data.from - 15f, distance);
                if (into < 0f || path.DeltaDistance(distance, lane.Data.to + 2f) < 0f) continue;
                float best = lateral, bestRoom = -1f;
                foreach (var other in _lanes)
                {
                    if (other.Data != lane.Data) continue;
                    float room = 999f;
                    foreach (var ball in _balls)
                    {
                        if (ball.Lane != other) continue;
                        float gap = path.DeltaDistance(distance, ball.Along);
                        if (gap < -2f) continue;
                        // where it meets us: closing at our speed plus its own
                        room = Mathf.Min(room, gap);
                    }
                    if (room > bestRoom + 4f || Mathf.Abs(room - bestRoom) <= 4f && Mathf.Abs(other.Lateral - lateral) < Mathf.Abs(best - lateral))
                    { bestRoom = room; best = other.Lateral; }
                }
                lateral = best;
            }
            // Billiard balls: round them, by where they will be when we get there.
            foreach (var b in _billiards)
            {
                if (!InRect(car.Body.position, b.Hall.min, b.Hall.max, 25f)) continue;
                var at = path.Project(b.Position);
                float to = path.DeltaDistance(distance, at.Distance);
                if (to < -3f || to > 22f) continue;
                Vector2 future = b.Position + b.Velocity * Mathf.Clamp(to / v, 0f, 1.5f);
                var f = path.Project(future);
                float clear = b.Hall.radius + CarRadius + 0.8f;
                if (Mathf.Abs(lateral - f.Lateral) >= clear) continue;
                float limit = track.HalfWidthAt(f.Distance) - 0.6f;
                float left = f.Lateral + clear, right = f.Lateral - clear;
                lateral = left > limit ? right : right < -limit ? left : Mathf.Abs(left - lateral) < Mathf.Abs(right - lateral) ? left : right;
                lateral = Mathf.Clamp(lateral, -limit, limit);
            }
            // Glass floor: keep to panes that hold (a careless rival does not look - and breaks them for the others).
            if (_panes.Count > 0 && (seed & 3) != 0)
            {
                Pane bestPane = null;
                float bestScore = float.MaxValue;
                foreach (var pane in _panes)
                {
                    // The row a few metres ahead: the pane to aim for.
                    float to = path.DeltaDistance(distance, (pane.From + pane.To) * 0.5f);
                    if (to < 3f || to > 11f) continue;
                    if (bestPane != null && bestPane.Row != pane.Row) continue;
                    if (pane.Cracked || pane.Broken) continue;
                    float score = Mathf.Abs((pane.Left + pane.Right) * 0.5f - lateral);
                    if (bestPane == null || score < bestScore) { bestPane = pane; bestScore = score; }
                }
                if (bestPane != null)
                {
                    lateral = (bestPane.Left + bestPane.Right) * 0.5f;
                    speed = Mathf.Min(speed, 17f);
                }
            }
        }

        // ------------------------------------------------------------------ helpers

        MeshRenderer Part(string name, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            _parts.Add(go);
            return r;
        }

        ParticleSystem Particles(string name, Color color, float size, Vector2 speed, float lifeMin, float lifeMax)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _parts.Add(go);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.4f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.6f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.15f;
            limit.limit = 0.5f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = BallOrder + 6;
            system.Play();
            return system;
        }

        /// <summary>A little vertex-colour mesh builder.</summary>
        class Kit
        {
            readonly List<Vector3> _v = new();
            readonly List<Color> _c = new();
            readonly List<int> _t = new();
            public int Count => _v.Count;

            public void Quad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color) => Quad(a, b, cc, d, color, color, color, color);

            public void Quad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color ca, Color cb, Color ccc, Color cd)
            {
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(cc); _v.Add(d);
                _c.Add(ca); _c.Add(cb); _c.Add(ccc); _c.Add(cd);
                _t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            public void Rect(Vector2 min, Vector2 max, Color color) => Quad(min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y), color);

            public void Tri(Vector2 a, Vector2 b, Vector2 cc, Color color)
            {
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(cc);
                _c.Add(color); _c.Add(color); _c.Add(color);
                _t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 });
            }

            public void Line(Vector2 a, Vector2 b, float width, Color color)
            {
                Vector2 n = (b - a).sqrMagnitude > 1e-6f ? new Vector2(-(b - a).y, (b - a).x).normalized * (width * 0.5f) : Vector2.zero;
                Quad(a - n, b - n, b + n, a + n, color);
            }

            public void Disc(Vector2 centre, float radius, Color color, int n)
            {
                int i = _v.Count;
                _v.Add(centre); _c.Add(color);
                for (int k = 0; k <= n; k++)
                {
                    float a = k * Mathf.PI * 2f / n;
                    _v.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius); _c.Add(color);
                    if (k > 0) _t.AddRange(new[] { i, i + k, i + k + 1, i, i + k + 1, i + k });
                }
            }

            public void Ring(Vector2 centre, float inner, float outer, Color color, int n)
            {
                for (int k = 0; k < n; k++)
                {
                    float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                    Vector2 d0 = new(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new(Mathf.Cos(a1), Mathf.Sin(a1));
                    Quad(centre + d0 * inner, centre + d0 * outer, centre + d1 * outer, centre + d1 * inner, color);
                }
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(_v);
                mesh.SetColors(_c);
                mesh.SetTriangles(_t, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
