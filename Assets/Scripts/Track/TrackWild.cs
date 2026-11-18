using System.Collections.Generic;
using UnityEngine;
using SomeGame.Car;

namespace SomeGame.Track
{
    /// <summary>
    /// The Water Cup's later races (<see cref="TrackLayout.wild"/>):
    /// - ledges: stretches where the channel runs along the top of the rocks (a rim, then a cliff face dropping away
    ///   to the sea far below) - leave the channel there and you fall; on a reef the coral flats either side, and you
    ///   run aground instead;
    /// - props: temples, ships, skull rock and the fortress, placed by hand (overhead ones fade while you are under);
    /// - crocodiles surfacing in the river (bubbles first), stone doors sliding shut across it;
    /// - a ship's cannon fire (a red ring marks where each ball will land), gold coins, plank ramps;
    /// - jellyfish drifting across the channel, manta rays leaping over it;
    /// - lightning striking the channel (a ring warns first; a small burst of light, no flash of the screen), waves
    ///   washing across the ledges, rain.
    /// </summary>
    public class TrackWild : TrackDerivedBehaviour
    {
        [SerializeField] Material plain, scenery, ground, water, sand, foam;
        [SerializeField] Sprite[] sprites = { };

        const int FaceOrder = -45, RimOrder = -44, RingOrder = 3, PropOrder = 4, CoinOrder = 7, ShadowOrder = 6, CrocOrder = 7,
                  DoorOrder = 20, BallOrder = 24, OverheadOrder = 30, BoltOrder = 32, RainOrder = 59, StormOrder = 57;

        readonly List<GameObject> _parts = new();
        CarMovement[] _cars = { };
        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.CameraRig.CameraShake _shake;
        TrackLayout.Wild _wild;
        Sprite _glow;

        Sprite Find(string name)
        {
            foreach (var s in sprites) if (s != null && s.name == name) return s;
            return null;
        }

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _overheads.Clear(); _crocs.Clear(); _doors.Clear(); _coins.Clear(); _planks.Clear(); _jellies.Clear();
            _mantas.Clear(); _strikes.Clear(); _waves.Clear(); _balls.Clear(); _fallen.Clear();
            _rain = null; _storm = null;
            Release();
            _wild = layout.wild;
            if (layout.biome != TrackLayout.Biome.Lagoon || _wild == null || !_wild.enabled) return;
            if (_wild.river) BuildRiver(path, layout);
            foreach (var ledge in _wild.ledges) BuildLedge(path, ledge);
            foreach (var prop in _wild.props) BuildProp(prop);
            foreach (var c in _wild.crocs) BuildCroc(path, c);
            for (int i = 0; i < _wild.doors.Count; i++) BuildDoor(path, _wild.doors[i], i);
            foreach (var c in _wild.coins) BuildCoin(path, c);
            foreach (var p in _wild.planks) BuildPlank(path, p);
            foreach (var j in _wild.jellyfish) BuildJellies(path, j);
            foreach (float m in _wild.mantas) BuildManta(path, m);
            if (!Application.isPlaying) return;
            _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            _glow = Glow();
            if (_doors.Count > 0) Track.Obstacle = DoorShut;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Release();
        }

        void Release()
        {
            if (Track.Obstacle == (System.Func<Vector2, bool>)DoorShut) Track.Obstacle = null;
        }

        // ------------------------------------------------------------------ helpers

        GameObject Mesh(string name, Material material, int order, Mesh mesh, Transform parent = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent != null ? parent : transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            _parts.Add(go);
            return go;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 at, float size, float angle, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, angle));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (plain != null) sr.sharedMaterial = plain;
            if (sprite != null) go.transform.localScale = Vector3.one * (size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            _parts.Add(go);
            return sr;
        }

        static float Heading(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        static Sprite Glow()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        bool IsPlayer(CarMovement car) => _race != null && _race.Player != null && car.gameObject == _race.Player.gameObject;

        void Say(CarMovement car, string text, Color color)
        {
            if (_combo != null && IsPlayer(car)) _combo.Announce(text, color);
        }

        static bool Free(CarMovement car) => car != null && !car.IsAirborne && !car.IsCarried && !car.IsPuppet && !car.IsRiding;

        // ------------------------------------------------------------------ the river world

        const int GroundOrder = -98, RiverOrder = -97, BankOrder = -96, ShoreFoamOrder = -95;

        // The land everywhere, the river a ribbon of water along the course, sandy banks fading into the jungle, foam
        // at the water's edge, and the shores solid.
        void BuildRiver(TrackPath path, TrackLayout layout)
        {
            var b = path.Bounds();
            float pad = layout.boundaryMargin + 60f;
            var gv = new List<Vector3>(); var gu = new List<Vector2>(); var gt = new List<int>(); var gc = new List<Color32>();
            Vector2 centre = b.center, half = b.size * 0.5f + Vector2.one * pad;
            TrackMeshes.AddQuad(gv, gu, gt, centre, new Vector2(half.x, 0f), new Vector2(0f, half.y), Vector2.one);
            for (int k = 0; k < 4; k++) { gu[k] = (Vector2)gv[k] / 4f; gc.Add(new Color32(255, 255, 255, 255)); }
            Mesh("JungleGround", ground != null ? ground : plain, GroundOrder, TrackMeshes.Quads("JungleGround", gv, gu, gt, gc));
            float margin = _wild.riverMargin;
            Ribbon("RiverWater", water, RiverOrder, path, d => -(track.OffRoadAt(d) + margin), d => track.OffRoadAt(d) + margin, Color.white, Color.white, 1f / 8f);
            foreach (float side in new[] { 1f, -1f })
            {
                Ribbon("RiverBank", sand, BankOrder, path, d => side * (track.OffRoadAt(d) + margin), d => side * (track.OffRoadAt(d) + margin + 3f),
                    Color.white, new Color(1f, 1f, 1f, 0f), 1f / 4f);
                Ribbon("RiverFoam", foam, ShoreFoamOrder, path, d => side * (track.OffRoadAt(d) + margin - 0.6f), d => side * (track.OffRoadAt(d) + margin + 0.3f),
                    new Color(1f, 1f, 1f, 0f), Color.white, 1f / 4f);
            }
            if (!Application.isPlaying) return;
            foreach (float side in new[] { 1f, -1f })
            {
                var pts = new List<Vector2>();
                for (float d = 0f; d <= path.Length; d += 1f) pts.Add(path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + margin + 0.4f));
                var go = new GameObject("RiverShore", typeof(EdgeCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.GetComponent<EdgeCollider2D>().points = pts.ToArray();
                _parts.Add(go);
            }
        }

        // A strip along the whole lap from one sideways offset to another (functions of the lap distance), world UVs.
        void Ribbon(string name, Material material, int order, TrackPath path, System.Func<float, float> from, System.Func<float, float> to, Color cFrom, Color cTo, float uvScale)
        {
            if (material == null) return;
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
            int steps = Mathf.CeilToInt(path.Length);
            for (int i = 0; i <= steps; i++)
            {
                float d = path.Length * i / steps;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                Vector2 a = p + n * from(d), bb = p + n * to(d);
                v.Add(a); v.Add(bb);
                u.Add(a * uvScale); u.Add(bb * uvScale);
                c.Add(cFrom); c.Add(cTo);
                if (i == steps) break;
                int s = i * 2;
                t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s, s + 1, s + 2, s + 1, s + 3, s + 2 });
            }
            Mesh(name, material, order, TrackMeshes.Quads(name, v, u, t, c));
        }

        // ------------------------------------------------------------------ ledges

        void BuildLedge(TrackPath path, TrackLayout.Ledge ledge)
        {
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var rimC = new List<Color32>();
            var fv = new List<Vector3>(); var fu = new List<Vector2>(); var ft = new List<int>(); var faceC = new List<Color32>();
            float len = Mathf.Repeat(ledge.to - ledge.from, path.Length);
            Color rimTop = ledge.reef ? new Color(0.98f, 0.86f, 0.62f) : new Color(0.56f, 0.53f, 0.49f);
            Color faceTop = ledge.reef ? new Color(0.36f, 0.86f, 0.8f) : new Color(0.24f, 0.24f, 0.26f);
            Color faceBottom = ledge.reef ? new Color(0.45f, 0.92f, 0.85f) : new Color(0.08f, 0.12f, 0.18f);
            foreach (float side in new[] { 1f, -1f })
            {
                if (ledge.side != 0f && Mathf.Sign(ledge.side) != side) continue;
                for (float a = 0f; a < len; a += 1f)
                {
                    float d0 = ledge.from + a, d1 = ledge.from + Mathf.Min(len, a + 1f);
                    // (fading in and out at the ends, so the ledge never starts with a hard edge)
                    float k0 = Mathf.Clamp01(Mathf.Min(a, len - a) / 3f), k1 = Mathf.Clamp01(Mathf.Min(a + 1f, len - a - 1f) / 3f);
                    Vector2 p0 = path.PointAt(d0), p1 = path.PointAt(d1), n0 = path.NormalAt(d0) * side, n1 = path.NormalAt(d1) * side;
                    float r0 = track.OffRoadAt(d0), r1 = track.OffRoadAt(d1);
                    Strip(v, u, t, rimC, p0 + n0 * r0, p1 + n1 * r1, p1 + n1 * (r1 + 0.9f), p0 + n0 * (r0 + 0.9f), Fade(rimTop, k0), Fade(rimTop, k1), Fade(rimTop, k1), Fade(rimTop, k0));
                    float depth = ledge.reef ? 7f : 5.5f;
                    Strip(fv, fu, ft, faceC, p0 + n0 * (r0 + 0.9f), p1 + n1 * (r1 + 0.9f), p1 + n1 * (r1 + depth), p0 + n0 * (r0 + depth),
                        Fade(faceTop, k0), Fade(faceTop, k1), Fade(faceBottom, 0f), Fade(faceBottom, 0f));
                }
            }
            Mesh("LedgeFace", plain, FaceOrder, TrackMeshes.Quads("LedgeFace", fv, fu, ft, faceC));
            Mesh("LedgeRim", plain, RimOrder, TrackMeshes.Quads("LedgeRim", v, u, t, rimC));
            // Coral over the reef flats.
            if (ledge.reef && scenery != null)
            {
                var cv = new List<Vector3>(); var cu = new List<Vector2>(); var ct = new List<int>(); var cc = new List<Color32>();
                var random = new System.Random((int)(ledge.from * 13f));
                string[] cells = { "Coral", "CoralPink", "Seaweed" };
                for (float a = 2f; a < len - 2f; a += 2.2f)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        if (ledge.side != 0f && Mathf.Sign(ledge.side) != side) continue;
                        float d = ledge.from + a;
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + 2.2f + (float)random.NextDouble() * 3f);
                        float size = 2.4f + (float)random.NextDouble() * 1.6f, ang = (float)random.NextDouble() * 6.28f;
                        Vector2 r = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * size * 0.5f;
                        int first = cv.Count;
                        TrackMeshes.AddQuad(cv, cu, ct, p, r, new Vector2(-r.y, r.x), Vector2.one);
                        var uv = LagoonSheet.Cell(cells[random.Next(cells.Length)]);
                        for (int k = first; k < cu.Count; k++) cu[k] = new Vector2(uv.x + cu[k].x * uv.width, uv.y + cu[k].y * uv.height);
                        for (int k = 0; k < 4; k++) cc.Add(new Color32(255, 255, 255, 255));
                    }
                Mesh("ReefCoral", scenery, RimOrder + 1, TrackMeshes.Quads("ReefCoral", cv, cu, ct, cc));
            }
        }

        static Color32 Fade(Color c, float k) => new Color(c.r, c.g, c.b, c.a * k);

        static void Strip(List<Vector3> v, List<Vector2> u, List<int> t, List<Color32> c, Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color32 ca, Color32 cb, Color32 ccol, Color32 cd)
        {
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(d); v.Add(cc);
            u.Add(Vector2.zero); u.Add(Vector2.right); u.Add(Vector2.up); u.Add(Vector2.one);
            c.Add(ca); c.Add(cb); c.Add(cd); c.Add(ccol);
            t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s, s + 1, s + 2, s + 1, s + 3, s + 2 });
        }

        readonly Dictionary<CarMovement, float> _fallen = new();

        void UpdateLedges()
        {
            if (_wild.ledges.Count == 0) return;
            var path = track.Path;
            foreach (var car in _cars)
            {
                if (!Free(car)) continue;
                var sensor = car.GetComponent<TrackSensor>();
                if (sensor == null || sensor.Branch >= 0) continue;
                if (_fallen.TryGetValue(car, out float at) && Time.time - at < 0.6f) continue;
                float d = sensor.Current.Distance, lat = sensor.Current.Lateral;
                foreach (var ledge in _wild.ledges)
                {
                    float into = path.DeltaDistance(ledge.from, d);
                    if (into < 0f || into > Mathf.Repeat(ledge.to - ledge.from, path.Length)) continue;
                    if (ledge.side != 0f && Mathf.Sign(lat) != Mathf.Sign(ledge.side)) continue;
                    if (Mathf.Abs(lat) < track.OffRoadAt(d) + 0.35f) continue;
                    _fallen[car] = Time.time;
                    var pose = track.RespawnBefore(d, 3f);
                    float top = car.Stats != null ? car.Stats.topSpeed : 18f;
                    if (!car.CarryBack(pose, top * 0.4f, fromWater: true, fall: !ledge.reef)) car.Respawn(pose);
                    if (ledge.reef) Say(car, "AGROUND!", new Color(1f, 0.6f, 0.35f));
                    else
                    {
                        Say(car, "OVER THE EDGE!", new Color(1f, 0.45f, 0.4f));
                        if (IsPlayer(car)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.splash, 0.7f);
                    }
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ props

        readonly List<(SpriteRenderer sr, float radius)> _overheads = new();

        void BuildProp(TrackLayout.Piece prop)
        {
            var sprite = Find(prop.sprite);
            if (sprite == null) return;
            var sr = Sprite(prop.sprite, sprite, prop.position, prop.size, prop.angle, prop.overhead ? OverheadOrder : PropOrder);
            if (!prop.overhead)
            {
                var shadow = Sprite(prop.sprite + "Shadow", sprite, prop.position + new Vector2(0.8f, -1f), prop.size, prop.angle, PropOrder - 1);
                shadow.color = new Color(0f, 0.08f, 0.12f, 0.3f);
            }
            else _overheads.Add((sr, prop.size * 0.45f));
        }

        void UpdateOverheads(float dt)
        {
            if (_overheads.Count == 0 || _race == null || _race.Player == null) return;
            Vector2 p = _race.Player.transform.position;
            foreach (var (sr, radius) in _overheads)
            {
                bool under = ((Vector2)sr.transform.position - p).sqrMagnitude < radius * radius;
                var c = sr.color;
                c.a = Mathf.MoveTowards(c.a, under ? 0.35f : 1f, dt * 3f);
                sr.color = c;
            }
        }

        // ------------------------------------------------------------------ crocodiles

        readonly List<(SpriteRenderer croc, SpriteRenderer ripple, float d, float lat, float phase)> _crocs = new();
        const float CrocCycle = 6.5f;

        void BuildCroc(TrackPath path, Vector2 c)
        {
            var sprite = Find("Crocodile");
            var ring = Find("TargetRing");
            if (sprite == null) return;
            var croc = Sprite("Crocodile", sprite, path.PointAt(c.x), 3.2f, 0f, CrocOrder);
            var ripple = Sprite("CrocRipple", ring, path.PointAt(c.x), 2.6f, 0f, RingOrder);
            ripple.color = new Color(1f, 1f, 1f, 0f);
            _crocs.Add((croc, ripple, c.x, c.y, Mathf.Repeat(c.x * 0.37f, CrocCycle)));
        }

        void UpdateCrocs()
        {
            var path = track.Path;
            float now = Track.Clock;
            foreach (var (croc, ripple, d, lat, phase) in _crocs)
            {
                float u = Mathf.Repeat(now + phase, CrocCycle);
                // Drifting across the channel and back; bubbles for a second, then up for a while, then under again.
                float half = track.OffRoadAt(d) - 1f;
                float l = Mathf.Clamp(lat + Mathf.Sin((now + phase) * 0.6f) * half * 0.6f, -half, half);
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * l;
                Vector2 dir = path.NormalAt(d) * Mathf.Sign(Mathf.Cos((now + phase) * 0.6f));
                croc.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, Heading(dir)));
                ripple.transform.position = at;
                bool warn = u > 1.5f && u < 2.6f, up = u >= 2.6f && u < 5.4f;
                ripple.color = new Color(1f, 1f, 1f, warn ? 0.5f + 0.4f * Mathf.Sin(now * 14f) : 0f);
                ripple.transform.localScale = Vector3.one * (2.6f / Mathf.Max(0.01f, ripple.sprite != null ? ripple.sprite.bounds.size.x : 1f)) * (warn ? 0.8f + 0.2f * Mathf.Repeat(now * 2f, 1f) : 1f);
                float rise = up ? Mathf.Clamp01(Mathf.Min(u - 2.6f, 5.4f - u) / 0.35f) : 0f;
                croc.color = new Color(1f, 1f, 1f, rise);
                if (rise < 0.6f) continue;
                foreach (var car in _cars)
                {
                    if (!Free(car) || car.IsSlipping) continue;
                    if ((car.Body.position - at).sqrMagnitude > 1.9f * 1.9f) continue;
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    car.Bump((car.Body.position - at).normalized, 5f);
                    Say(car, "SNAP!", new Color(0.55f, 0.9f, 0.4f));
                    if (IsPlayer(car)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.crash, 0.6f);
                }
            }
        }

        // ------------------------------------------------------------------ stone doors

        class Door { public float D, Half; public Vector2 Centre, Across; public Rigidbody2D Left, Right; public Transform LeftT, RightT; public float Open; public int Index; }
        readonly List<Door> _doors = new();

        void BuildDoor(TrackPath path, float d, int index)
        {
            var sprite = Find("StoneDoor");
            var door = new Door { D = d, Centre = path.PointAt(d), Across = path.NormalAt(d), Half = track.OffRoadAt(d) + 0.4f, Index = index };
            float angle = Mathf.Atan2(door.Across.y, door.Across.x) * Mathf.Rad2Deg;
            foreach (float side in new[] { 1f, -1f })
            {
                var sr = Sprite("StoneDoor", sprite, door.Centre + door.Across * side * door.Half * 0.5f, door.Half, angle, DoorOrder);
                if (sprite != null) sr.transform.localScale = new Vector3(door.Half / sprite.bounds.size.x, 1.1f / sprite.bounds.size.y, 1f);
                Rigidbody2D body = null;
                if (Application.isPlaying)
                {
                    body = sr.gameObject.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    var box = sr.gameObject.AddComponent<BoxCollider2D>();
                    box.size = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
                }
                if (side > 0f) { door.Left = body; door.LeftT = sr.transform; } else { door.Right = body; door.RightT = sr.transform; }
            }
            _doors.Add(door);
        }

        float DoorOpenness(int index)
        {
            float period = _wild.doorPeriod, open = 0.55f * period, slide = 0.6f;
            float u = Mathf.Repeat(Track.Clock - index * period * 0.37f, period);
            if (u < slide) return u / slide;
            if (u < open) return 1f;
            if (u < open + slide) return 1f - (u - open) / slide;
            return 0f;
        }

        void UpdateDoors()
        {
            foreach (var door in _doors)
            {
                door.Open = DoorOpenness(door.Index);
                float e = Mathf.SmoothStep(0f, 1f, door.Open);
                // Shut, each leaf covers its half; open, it has slid back into the bank.
                foreach (float side in new[] { 1f, -1f })
                {
                    Vector2 at = door.Centre + door.Across * side * (door.Half * 0.5f + e * door.Half * 1.05f);
                    var body = side > 0f ? door.Left : door.Right;
                    if (body != null) body.MovePosition(at); else (side > 0f ? door.LeftT : door.RightT).position = at;
                }
            }
        }

        bool DoorShut(Vector2 p)
        {
            if (_doors.Count == 0) return false;
            var on = track.Path.Project(p);
            foreach (var door in _doors)
                if (door.Open < 0.6f && Mathf.Abs(track.Path.DeltaDistance(door.D, on.Distance)) < 1.5f) return true;
            return false;
        }

        // ------------------------------------------------------------------ cannon fire

        readonly List<(Transform ball, SpriteRenderer ring, Vector2 from, Vector2 to, float fired, float lands)> _balls = new();
        float _nextShot;

        void UpdateCannon()
        {
            if (_wild.cannonAims == Vector2.zero || _race == null || _race.Player == null) return;
            var path = track.Path;
            float pd = _race.Player.GetComponent<TrackSensor>().Current.Distance;
            bool inRange = path.DeltaDistance(_wild.cannonAims.x, pd) >= -15f && path.DeltaDistance(pd, _wild.cannonAims.y) >= 0f;
            if (inRange && Time.time >= _nextShot)
            {
                _nextShot = Time.time + _wild.cannonEvery * Random.Range(0.8f, 1.2f);
                float d = pd + Random.Range(9f, 20f);
                float half = track.HalfWidthAt(d);
                Vector2 to = path.PointAt(d) + path.NormalAt(d) * Random.Range(-half, half);
                var ring = Sprite("CannonTarget", Find("TargetRing"), to, 3.6f, 0f, RingOrder);
                var ball = Sprite("Cannonball", Find("Cannonball"), _wild.cannon, 0.9f, 0f, BallOrder);
                float flight = 1.15f;
                _balls.Add((ball.transform, ring, _wild.cannon, to, Time.time, Time.time + flight));
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.thunder, 0.25f);
            }
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var (ball, ring, from, to, fired, lands) = _balls[i];
                float k = Mathf.InverseLerp(fired, lands, Time.time);
                if (ball != null)
                {
                    ball.position = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 3f;
                    ball.localScale = Vector3.one * (0.9f / Mathf.Max(0.01f, Find("Cannonball") != null ? Find("Cannonball").bounds.size.x : 1f)) * (1f + Mathf.Sin(k * Mathf.PI) * 1.2f);
                }
                if (ring != null) ring.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * Mathf.Sin(Time.time * 16f));
                if (k < 1f) continue;
                // Splash: anyone near is knocked aside and spun.
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    Vector2 o = car.Body.position - to;
                    if (o.sqrMagnitude > 2.6f * 2.6f) continue;
                    car.Bump(o.sqrMagnitude > 0.01f ? o.normalized : Vector2.up, 8f);
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    Say(car, "KABOOM!", new Color(1f, 0.5f, 0.3f));
                }
                Burst(to, new Color(0.9f, 0.97f, 1f), 4.5f);
                if (_race.Player != null && ((Vector2)_race.Player.transform.position - to).sqrMagnitude < 15f * 15f)
                {
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.splash, 0.6f);
                    if (_shake != null) _shake.Shake(0.15f);
                }
                if (ball != null) Destroy(ball.gameObject);
                if (ring != null) Destroy(ring.gameObject);
                _balls.RemoveAt(i);
            }
        }

        // A short burst of light or spray: a soft disc that grows and fades.
        readonly List<(SpriteRenderer sr, float born, float size)> _bursts = new();

        void Burst(Vector2 at, Color color, float size)
        {
            if (_glow == null) return;
            var sr = Sprite("Burst", _glow, at, size, 0f, BoltOrder - 1);
            sr.color = color;
            _bursts.Add((sr, Time.time, size));
        }

        void UpdateBursts()
        {
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                var (sr, born, size) = _bursts[i];
                float k = (Time.time - born) / 0.45f;
                if (sr == null || k >= 1f) { if (sr != null) Destroy(sr.gameObject); _bursts.RemoveAt(i); continue; }
                sr.transform.localScale = Vector3.one * size * (0.6f + 0.6f * k) / Mathf.Max(0.01f, _glow.bounds.size.x);
                var c = sr.color; c.a = 1f - k; sr.color = c;
            }
        }

        // ------------------------------------------------------------------ coins and plank ramps

        readonly List<(SpriteRenderer sr, Vector2 at, float hiddenUntil)> _coins = new();
        int _coinRun;
        float _coinAt;

        void BuildCoin(TrackPath path, Vector2 c)
        {
            Vector2 at = path.PointAt(c.x) + path.NormalAt(c.x) * c.y;
            _coins.Add((Sprite("Coin", Find("Coin"), at, 1.3f, 0f, CoinOrder), at, 0f));
        }

        void UpdateCoins()
        {
            for (int i = 0; i < _coins.Count; i++)
            {
                var (sr, at, hidden) = _coins[i];
                if (sr == null) continue;
                bool shown = Time.time >= hidden;
                sr.enabled = shown;
                if (!shown) continue;
                float spin = Mathf.Abs(Mathf.Cos(Time.time * 3f + at.x));
                var s = sr.transform.localScale; sr.transform.localScale = new Vector3(Mathf.Max(0.15f, spin) * Mathf.Abs(s.y), s.y, 1f);
                foreach (var car in _cars)
                {
                    if (car == null || car.IsPuppet || car.IsCarried) continue;
                    if ((car.Body.position - at).sqrMagnitude > 1.5f * 1.5f) continue;
                    _coins[i] = (sr, at, Time.time + 6f);
                    car.GiveBoost(0.2f);
                    if (IsPlayer(car))
                    {
                        _coinRun = Time.time - _coinAt < 2.5f ? _coinRun + 1 : 1;
                        _coinAt = Time.time;
                        SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.35f);
                        if (_coinRun == 5) { car.GiveBoost(0.7f); Say(car, "TREASURE!", new Color(1f, 0.85f, 0.25f)); }
                    }
                    break;
                }
            }
        }

        readonly List<(Vector2 at, Vector2 along, Vector2 across)> _planks = new();
        readonly Dictionary<CarMovement, float> _planked = new();

        void BuildPlank(TrackPath path, Vector2 p)
        {
            Vector2 at = path.PointAt(p.x) + path.NormalAt(p.x) * p.y, along = path.TangentAt(p.x);
            Sprite("PlankRamp", Find("PlankRamp"), at, 4.6f, Heading(along), RingOrder + 1);
            _planks.Add((at, along, new Vector2(-along.y, along.x)));
        }

        void UpdatePlanks()
        {
            foreach (var (at, along, across) in _planks)
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    if (_planked.TryGetValue(car, out float t) && Time.time - t < 1f) continue;
                    Vector2 o = car.Body.position - at;
                    if (Mathf.Abs(Vector2.Dot(o, along)) > 1.8f || Mathf.Abs(Vector2.Dot(o, across)) > 1.7f) continue;
                    if (Vector2.Dot(car.Body.linearVelocity, along) < 6f) continue;
                    _planked[car] = Time.time;
                    car.Launch(0.95f);
                    car.GiveBoost(0.45f);
                    Say(car, "PLANK JUMP!", new Color(1f, 0.75f, 0.35f));
                    if (IsPlayer(car)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.jump, 0.7f);
                }
        }

        // ------------------------------------------------------------------ jellyfish and manta rays

        readonly List<(SpriteRenderer sr, float d, float phase, float speed)> _jellies = new();
        readonly Dictionary<CarMovement, float> _stung = new();

        void BuildJellies(TrackPath path, Vector2 span)
        {
            float len = Mathf.Repeat(span.y - span.x, path.Length);
            int count = Mathf.Max(3, Mathf.RoundToInt(len / 6f));
            var random = new System.Random((int)(span.x * 7f));
            for (int i = 0; i < count; i++)
            {
                float d = span.x + len * (i + 0.5f) / count;
                _jellies.Add((Sprite("Jellyfish", Find("Jellyfish"), path.PointAt(d), 1.6f, 0f, CrocOrder), d, (float)random.NextDouble() * 6.28f, 0.35f + (float)random.NextDouble() * 0.4f));
            }
        }

        void UpdateJellies()
        {
            var path = track.Path;
            float now = Track.Clock;
            foreach (var (sr, d, phase, speed) in _jellies)
            {
                if (sr == null) continue;
                float half = track.OffRoadAt(d) - 0.5f;
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * (Mathf.Sin(now * speed + phase) * half);
                sr.transform.position = at;
                float pulse = 1f + 0.12f * Mathf.Sin(now * 4f + phase);
                sr.transform.localScale = Vector3.one * (1.6f / Mathf.Max(0.01f, sr.sprite != null ? sr.sprite.bounds.size.x : 1f)) * pulse;
                foreach (var car in _cars)
                {
                    if (!Free(car) || car.IsSlipping) continue;
                    if ((car.Body.position - at).sqrMagnitude > 1.2f * 1.2f) continue;
                    if (_stung.TryGetValue(car, out float t) && Time.time - t < 1.5f) continue;
                    _stung[car] = Time.time;
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    Say(car, "ZAP!", new Color(1f, 0.55f, 0.85f));
                }
            }
        }

        readonly List<(SpriteRenderer sr, SpriteRenderer shadow, float d, float offset)> _mantas = new();
        const float MantaCycle = 5.5f, MantaFlight = 1.6f;

        void BuildManta(TrackPath path, float d)
        {
            var sprite = Find("MantaRay");
            var sr = Sprite("MantaRay", sprite, path.PointAt(d), 4.2f, 0f, BallOrder);
            var shadow = Sprite("MantaShadow", sprite, path.PointAt(d), 4.2f, 0f, ShadowOrder);
            shadow.color = new Color(0f, 0.1f, 0.2f, 0.35f);
            _mantas.Add((sr, shadow, d, Mathf.Repeat(d * 0.21f, MantaCycle)));
        }

        void UpdateMantas()
        {
            var path = track.Path;
            float now = Track.Clock;
            foreach (var (sr, shadow, d, offset) in _mantas)
            {
                float u = Mathf.Repeat(now + offset, MantaCycle);
                bool flying = u < MantaFlight;
                sr.enabled = shadow.enabled = flying;
                if (!flying) continue;
                float k = u / MantaFlight, h = Mathf.Sin(k * Mathf.PI);
                float side = Mathf.Repeat(Mathf.Floor((now + offset) / MantaCycle), 2f) < 1f ? 1f : -1f;
                float reach = track.OffRoadAt(d) + 3f;
                Vector2 n = path.NormalAt(d) * side, at = path.PointAt(d) + n * Mathf.Lerp(reach, -reach, k);
                sr.transform.SetPositionAndRotation(at + new Vector2(-0.5f, 1.6f) * h, Quaternion.Euler(0f, 0f, Heading(-n)));
                sr.transform.localScale = Vector3.one * (4.2f / Mathf.Max(0.01f, sr.sprite != null ? sr.sprite.bounds.size.x : 1f)) * (1f + 0.4f * h);
                shadow.transform.SetPositionAndRotation(at, sr.transform.rotation);
                if (h > 0.45f) continue;
                // Low over the water (taking off, diving in): it knocks you aside.
                foreach (var car in _cars)
                {
                    if (!Free(car) || car.IsSlipping) continue;
                    if ((car.Body.position - at).sqrMagnitude > 2f * 2f) continue;
                    car.Slip(side > 0f ? 1 : -1);
                    Say(car, "SPLAT!", new Color(0.6f, 0.75f, 1f));
                }
            }
        }

        // ------------------------------------------------------------------ lightning and waves

        readonly List<(SpriteRenderer ring, Vector2 at, float strikes)> _strikes = new();
        float _nextStrike;

        bool PlayerIn(List<Vector2> spans, out float pd)
        {
            pd = 0f;
            if (_race == null || _race.Player == null || spans.Count == 0) return false;
            pd = _race.Player.GetComponent<TrackSensor>().Current.Distance;
            foreach (var s in spans)
            {
                float into = track.Path.DeltaDistance(s.x, pd);
                if (into >= -10f && into <= Mathf.Repeat(s.y - s.x, track.Path.Length)) return true;
            }
            return false;
        }

        void UpdateLightning()
        {
            var path = track.Path;
            if (PlayerIn(_wild.lightning, out float pd) && Time.time >= _nextStrike)
            {
                _nextStrike = Time.time + Random.Range(2.2f, 3.6f);
                float d = pd + Random.Range(10f, 20f);
                float half = track.HalfWidthAt(d);
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * Random.Range(-half, half);
                var ring = Sprite("StrikeRing", Find("TargetRing"), at, 4.4f, 0f, RingOrder);
                ring.color = new Color(1f, 0.95f, 0.5f, 0.8f);
                _strikes.Add((ring, at, Time.time + 1.1f));
            }
            for (int i = _strikes.Count - 1; i >= 0; i--)
            {
                var (ring, at, strikes) = _strikes[i];
                if (ring != null) ring.color = new Color(1f, 0.95f, 0.5f, 0.5f + 0.5f * Mathf.Sin(Time.time * 18f));
                if (Time.time < strikes) continue;
                var bolt = Sprite("Bolt", Find("Bolt"), at + Vector2.up * 4f, 8f, 0f, BoltOrder);
                Destroy(bolt.gameObject, 0.22f);
                Burst(at, new Color(1f, 0.98f, 0.8f), 7f);
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    if ((car.Body.position - at).sqrMagnitude > 2.6f * 2.6f) continue;
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    Say(car, "ZAPPED!", new Color(1f, 0.95f, 0.45f));
                }
                if (_race.Player != null && ((Vector2)_race.Player.transform.position - at).sqrMagnitude < 25f * 25f)
                {
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.thunder, 0.55f);
                    if (_shake != null) _shake.Shake(0.2f);
                }
                if (ring != null) Destroy(ring.gameObject);
                _strikes.RemoveAt(i);
            }
        }

        // A wave: a band of foam rolling across the channel from one side, shoving everything in it the other way.
        readonly List<(MeshFilter mesh, float d, float side, float born)> _waves = new();
        float _nextWave;
        const float WaveWarn = 0.9f, WaveCross = 1.4f, WaveLength = 9f;

        void UpdateWaves(float dt)
        {
            var path = track.Path;
            if (PlayerIn(_wild.waves, out float pd) && Time.time >= _nextWave)
            {
                _nextWave = Time.time + Random.Range(3.2f, 4.8f);
                var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
                for (int k = 0; k < 16; k++) { v.Add(Vector3.zero); u.Add(Vector2.zero); c.Add(new Color32(255, 255, 255, 0)); }
                for (int k = 0; k < 3; k++) { int s = k * 4; t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 }); }
                var go = Mesh("Wave", plain, BoltOrder - 2, TrackMeshes.Quads("Wave", v, u, t, c));
                _waves.Add((go.GetComponent<MeshFilter>(), pd + Random.Range(8f, 14f), Random.value < 0.5f ? 1f : -1f, Time.time));
            }
            for (int i = _waves.Count - 1; i >= 0; i--)
            {
                var (mf, d, side, born) = _waves[i];
                float age = Time.time - born;
                if (mf == null || age > WaveWarn + WaveCross) { if (mf != null) Destroy(mf.gameObject); _waves.RemoveAt(i); continue; }
                Vector2 centre = path.PointAt(d), along = path.TangentAt(d), n = path.NormalAt(d) * side;
                float reach = track.OffRoadAt(d) + 2.5f;
                // Warning: the crest gathers at the edge it comes from; then it rolls across.
                float k = age < WaveWarn ? 0f : (age - WaveWarn) / WaveCross;
                float pos = Mathf.Lerp(reach, -reach, k);
                byte alpha = (byte)(age < WaveWarn ? 120 + 100 * Mathf.Sin(age * 20f) : 230 * (1f - k * k));
                var mesh = mf.sharedMesh;
                var verts = new Vector3[16]; var cols = new Color32[16];
                for (int q = 0; q < 3; q++)
                {
                    float w = q == 0 ? 1.4f : q == 1 ? 0.6f : 0.3f, back = q == 0 ? 0f : q == 1 ? 1.3f : 2.2f;
                    Vector2 mid = centre + n * (pos + back * Mathf.Sign(reach));
                    int s = q * 4;
                    verts[s] = mid - along * WaveLength * 0.5f - n * w * 0.5f; verts[s + 1] = mid + along * WaveLength * 0.5f - n * w * 0.5f;
                    verts[s + 2] = mid - along * WaveLength * 0.5f + n * w * 0.5f; verts[s + 3] = mid + along * WaveLength * 0.5f + n * w * 0.5f;
                    byte a = (byte)(alpha * (q == 0 ? 1f : q == 1 ? 0.6f : 0.35f));
                    for (int z = 0; z < 4; z++) cols[s + z] = new Color32(240, 250, 255, a);
                }
                mesh.vertices = verts;
                mesh.colors32 = cols;
                mesh.RecalculateBounds();
                if (age < WaveWarn) continue;
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    Vector2 o = car.Body.position - centre;
                    if (Mathf.Abs(Vector2.Dot(o, along)) > WaveLength * 0.5f) continue;
                    if (Mathf.Abs(Vector2.Dot(o, n) - pos) > 1.6f) continue;
                    car.Body.linearVelocity += -n * 22f * dt;
                    if (IsPlayer(car) && age - WaveWarn < 0.1f) Say(car, "WAVE!", new Color(0.6f, 0.85f, 1f));
                }
            }
        }

        // ------------------------------------------------------------------ rain and the storm's gloom

        MeshFilter _rain;
        MeshRenderer _storm;
        Vector3[] _rainVerts;
        Vector2[] _drops;
        Color32[] _rainColors;
        const int Drops = 140;
        static readonly Vector2 Wind = new(6f, -15f);

        void UpdateRain(float dt)
        {
            var cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize + 3f, w = cam.orthographicSize * cam.aspect + 3f;
            if (_rain == null)
            {
                var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
                _drops = new Vector2[Drops];
                for (int i = 0; i < Drops; i++)
                {
                    _drops[i] = new Vector2(Random.Range(-w, w), Random.Range(-h, h));
                    for (int k = 0; k < 4; k++) { v.Add(Vector3.zero); u.Add(Vector2.zero); c.Add(new Color32(0, 0, 0, 0)); }
                    int s = i * 4; t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
                }
                var go = Mesh("Rain", plain, RainOrder, TrackMeshes.Quads("Rain", v, u, t, c), cam.transform);
                go.transform.localPosition = new Vector3(0f, 0f, 4f);
                _rain = go.GetComponent<MeshFilter>();
                _rainVerts = v.ToArray();
                _rainColors = c.ToArray();
                // A grey-blue gloom over the storm (not darkness: everything stays easy to see).
                var gv = new List<Vector3>(); var gu = new List<Vector2>(); var gt = new List<int>(); var gc = new List<Color32>();
                TrackMeshes.AddQuad(gv, gu, gt, Vector2.zero, Vector2.right * 400f, Vector2.up * 400f, Vector2.one);
                for (int k = 0; k < 4; k++) gc.Add(new Color32(30, 45, 70, 40));
                var gloom = Mesh("StormGloom", plain, StormOrder, TrackMeshes.Quads("StormGloom", gv, gu, gt, gc), cam.transform);
                gloom.transform.localPosition = new Vector3(0f, 0f, 5f);
                _storm = gloom.GetComponent<MeshRenderer>();
            }
            Vector2 dir = Wind.normalized, side = new Vector2(-dir.y, dir.x) * 0.035f, len = dir * 1.1f;
            for (int i = 0; i < Drops; i++)
            {
                var p = _drops[i] + Wind * dt * (0.8f + (i % 5) * 0.1f);
                if (p.y < -h) p = new Vector2(Random.Range(-w - 6f, w), h);
                if (p.x > w) p.x -= 2f * w;
                _drops[i] = p;
                int k = i * 4;
                _rainVerts[k] = p - side; _rainVerts[k + 1] = p + side; _rainVerts[k + 2] = p - side - len; _rainVerts[k + 3] = p + side - len;
                _rainColors[k] = _rainColors[k + 1] = new Color32(210, 228, 255, 120);
                _rainColors[k + 2] = _rainColors[k + 3] = new Color32(210, 228, 255, 0);
            }
            var mesh = _rain.sharedMesh;
            mesh.vertices = _rainVerts;
            mesh.colors32 = _rainColors;
            mesh.RecalculateBounds();
        }

        // ------------------------------------------------------------------

        void Update()
        {
            if (!Application.isPlaying || _wild == null || !_wild.enabled || track == null || track.Layout == null || track.Layout.biome != TrackLayout.Biome.Lagoon) return;
            float dt = Time.deltaTime;
            UpdateLedges();
            UpdateOverheads(dt);
            UpdateCrocs();
            UpdateDoors();
            UpdateCannon();
            UpdateCoins();
            UpdatePlanks();
            UpdateJellies();
            UpdateMantas();
            UpdateLightning();
            UpdateWaves(dt);
            UpdateBursts();
            if (_wild.rain) UpdateRain(dt);
        }
    }
}
