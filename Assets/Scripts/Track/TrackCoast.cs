using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// A rocky coast at night in a gale (Water Cup race 3, <see cref="TrackLayout.coast"/>):
    /// - the dark: a night over everything, cut open by the big lighthouse's beam sweeping round the bay and by the small
    ///   lighthouses all round the course (a pool of light round each, a short turning beam); no lights on the boats; lightning now and then lights up the whole bay (thunder follows);
    /// - rain blowing across the screen;
    /// - the tide, rising lap by lap: the flats are dry sand on the first lap, wet on the second, and flooded on the last,
    ///   when the branch across them opens; some rocks stand out of the water only until the tide covers them;
    /// - the lock staircase up the cliff: mitre gates opening and shutting in a rhythm (a red or green lamp on each), the
    ///   water lifting the boats a step at each lock, up to the cliff-top pond;
    /// - the waterfall where the pond spills over the cliff edge (the Leap of Faith: a drop gap);
    /// - a wreck lying along the channel: sail in through its broken bow and out through its stern;
    /// - on the last lap the storm breaks: the rain stops, the dark lifts to moonlight and the beam turns gold.
    /// </summary>
    public class TrackCoast : TrackDerivedBehaviour
    {
        [SerializeField] Material plain, sand, foamParticles;
        [SerializeField] Sprite wreck, rock;

        const int FlatsOrder = -44, LockWallOrder = -42, LockTintOrder = -47, FallOrder = -41, RockOrder = 4, WreckOrder = 5,
                  GateOrder = 20, LampOrder = 21, RainOrder = 59, NightOrder = 58;

        readonly List<GameObject> _parts = new();
        SomeGame.Race.RaceManager _race;
        SomeGame.CameraRig.CameraShake _shake;
        TrackLayout.Coast _coast;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _gates.Clear(); _rocks.Clear();
            _flats = null; _flatsCollider = null; _night = null; _rainMesh = null;
            Release();
            _coast = layout.coast;
            if (layout.biome != TrackLayout.Biome.Lagoon || _coast == null || !_coast.enabled) return;
            BuildFlats();
            foreach (var r in _coast.rocks) BuildRock(r);
            // The small lighthouses are solid (out in the bay they stand on their own rocks).
            if (Application.isPlaying && _coast.lights != null)
                foreach (var lamp in _coast.lights)
                {
                    var go = new GameObject("LighthouseBase", typeof(CircleCollider2D)) { hideFlags = HideFlags.DontSave };
                    go.transform.SetParent(transform, false);
                    go.transform.position = lamp;
                    go.GetComponent<CircleCollider2D>().radius = 2f;
                    _parts.Add(go);
                }
            BuildLocks(path);
            if (_coast.wreck >= 0f) BuildWreck(path);
            foreach (var gap in layout.gaps) if (gap.drop) BuildFall(path, gap);
            if (!Application.isPlaying) return;
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            _nextStrike = Time.time + 6f;
            SomeGame.Car.CarHop.Lift = Lift;
            SomeGame.Track.Track.Obstacle = GateShut;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Release();
        }

        void Release()
        {
            if (SomeGame.Car.CarHop.Lift == (System.Func<Vector2, float>)Lift) SomeGame.Car.CarHop.Lift = null;
            if (SomeGame.Track.Track.Obstacle == (System.Func<Vector2, bool>)GateShut) SomeGame.Track.Track.Obstacle = null;
        }

        GameObject Part(string name, Material material, int order, Mesh mesh, Transform parent = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent != null ? parent : transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            if (parent == null) _parts.Add(go);
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

        static Mesh Quads(string name, List<Vector3> v, List<Vector2> u, List<int> t, List<Color32> c) => TrackMeshes.Quads(name, v, u, t, c);

        // A quad from four corners, one colour.
        static void Quad(List<Vector3> v, List<Vector2> u, List<int> t, List<Color32> c, Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color32 col)
        {
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(d); v.Add(cc);
            u.Add(Vector2.zero); u.Add(Vector2.right); u.Add(Vector2.up); u.Add(Vector2.one);
            for (int k = 0; k < 4; k++) c.Add(col);
            t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
        }

        bool LastLap => _race != null && _race.Player != null && _race.Player.TotalLaps > 1 && _race.Player.CurrentLap >= _race.Player.TotalLaps;

        // ------------------------------------------------------------------ the tide

        MeshFilter _flats;
        Collider2D _flatsCollider;
        float _tide;
        readonly List<(TrackLayout.TideRock data, SpriteRenderer sr, Collider2D col)> _rocks = new();

        void BuildFlats()
        {
            if (_coast.flats == null || _coast.flats.Length < 3) return;
            var shore = TrackLagoon.Smooth(_coast.flats, 6);
            if (TrackLagoon.SignedArea(shore) < 0f) shore.Reverse();
            _flats = Part("TideFlats", sand != null ? sand : plain, FlatsOrder, TrackLagoon.Fill(shore, Color.white, 1f / 4f)).GetComponent<MeshFilter>();
            Part("TideFlatsEdge", plain, FlatsOrder - 1, TrackLagoon.Ring(shore, 0f, 2.5f, new Color(1f, 1f, 1f, 0.5f), new Color(1f, 1f, 1f, 0f), 1f));
            if (Application.isPlaying)
            {
                var go = new GameObject("TideFlatsShore", typeof(PolygonCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.GetComponent<PolygonCollider2D>().points = TrackLagoon.Offset(shore, -0.4f).ToArray();
                _flatsCollider = go.GetComponent<Collider2D>();
                _parts.Add(go);
            }
            ApplyTide(0f);
        }

        void BuildRock(TrackLayout.TideRock data)
        {
            if (rock == null) return;
            var sr = Sprite("SeaRock", rock, data.position, data.radius * 2.6f, Mathf.Repeat(data.position.x * 37f, 360f), RockOrder);
            Collider2D col = null;
            if (Application.isPlaying)
            {
                var c = sr.gameObject.AddComponent<CircleCollider2D>();
                c.radius = data.radius / sr.transform.localScale.x;
                col = c;
            }
            _rocks.Add((data, sr, col));
        }

        // 0 low tide (lap 1) .. 1 high tide (the last lap): the flats go under, the low rocks disappear.
        void ApplyTide(float level)
        {
            if (_flats != null)
            {
                var mesh = _flats.sharedMesh;
                var colors = mesh.colors32;
                float wet = Mathf.Clamp01(level * 2f), gone = Mathf.Clamp01(level * 2f - 1f);
                var col = (Color32)new Color(Mathf.Lerp(1f, 0.62f, wet), Mathf.Lerp(1f, 0.66f, wet), Mathf.Lerp(1f, 0.72f, wet), 1f - gone);
                for (int i = 0; i < colors.Length; i++) colors[i] = col;
                mesh.colors32 = colors;
                if (_flatsCollider != null) _flatsCollider.enabled = gone < 0.5f;
            }
            foreach (var (data, sr, col) in _rocks)
            {
                bool shown = data.untilLap <= 0 || Track.PhaseLap < data.untilLap;
                float a = Mathf.MoveTowards(sr.color.a, shown ? 1f : 0f, Time.deltaTime / 2f);
                if (!Application.isPlaying) a = 1f;
                sr.color = new Color(1f, 1f, 1f, a);
                if (col != null) col.enabled = a > 0.5f;
            }
        }

        // ------------------------------------------------------------------ the lock staircase

        class Gate
        {
            public float Distance, Half;
            public Vector2 Centre, Along, Across;
            public Transform[] Leaves;
            public Rigidbody2D[] Bodies;
            public float[] Closed, Open;
            public MeshFilter Lamp;
            public float Openness;
        }
        readonly List<Gate> _gates = new();

        void BuildLocks(TrackPath path)
        {
            var gates = _coast.gates;
            if (gates == null || gates.Length == 0) return;
            // Stone walls along both sides of the staircase; the water in each lock a little paler, the higher it stands.
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
            float from = gates[0] - 4f, to = gates[^1] + 6f;
            for (float d = from; d < to; d += 1f)
            {
                float half0 = track.OffRoadAt(d), half1 = track.OffRoadAt(d + 1f);
                Vector2 p0 = path.PointAt(d), p1 = path.PointAt(d + 1f), n0 = path.NormalAt(d), n1 = path.NormalAt(d + 1f);
                foreach (float side in new[] { 1f, -1f })
                {
                    Quad(v, u, t, c, p0 + n0 * side * half0, p1 + n1 * side * half1, p1 + n1 * side * (half1 + 1.6f), p0 + n0 * side * (half0 + 1.6f),
                        Mathf.Repeat(d, 3f) < 1f ? new Color32(120, 116, 110, 255) : new Color32(140, 136, 128, 255));
                }
                int step = 0;
                foreach (float g in gates) if (d >= g) step++;
                if (step > 0)
                    Quad(v, u, t, c, p0 - n0 * half0, p1 - n1 * half1, p1 + n1 * half1, p0 + n0 * half0, new Color32(200, 235, 255, (byte)(14 * step)));
            }
            Part("LockWalls", plain, LockWallOrder, Quads("LockWalls", v, u, t, c));
            foreach (float g in gates) BuildGate(path, g);
        }

        void BuildGate(TrackPath path, float distance)
        {
            var gate = new Gate { Distance = distance, Centre = path.PointAt(distance), Along = path.TangentAt(distance), Across = path.NormalAt(distance) };
            gate.Half = track.OffRoadAt(distance);
            gate.Leaves = new Transform[2];
            gate.Bodies = new Rigidbody2D[2];
            gate.Closed = new float[2];
            gate.Open = new float[2];
            float across = Mathf.Atan2(gate.Across.y, gate.Across.x) * Mathf.Rad2Deg, along = Mathf.Atan2(gate.Along.y, gate.Along.x) * Mathf.Rad2Deg;
            float len = gate.Half / Mathf.Cos(18f * Mathf.Deg2Rad) + 0.15f;
            for (int k = 0; k < 2; k++)
            {
                float side = k == 0 ? 1f : -1f;
                // A leaf hinged on the wall, pointing in to the middle (a mitre gate: angled a little upstream), or swung
                // back flat against the wall in the lock.
                var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
                Quad(v, u, t, c, new Vector2(0f, -0.32f), new Vector2(len, -0.32f), new Vector2(len, 0.32f), new Vector2(0f, 0.32f), new Color32(58, 40, 26, 255));
                Quad(v, u, t, c, new Vector2(0.1f, 0.02f), new Vector2(len - 0.1f, 0.02f), new Vector2(len - 0.1f, 0.24f), new Vector2(0.1f, 0.24f), new Color32(122, 86, 52, 255));
                for (float x = 0.8f; x < len - 0.3f; x += 1.2f)
                    Quad(v, u, t, c, new Vector2(x, -0.3f), new Vector2(x + 0.12f, -0.3f), new Vector2(x + 0.12f, 0.3f), new Vector2(x, 0.3f), new Color32(40, 28, 18, 255));
                var leaf = Part("LockGate", plain, GateOrder, Quads("LockGate", v, u, t, c));
                leaf.transform.position = gate.Centre + gate.Across * side * gate.Half;
                gate.Closed[k] = (side > 0f ? across + 180f : across) - side * 18f;
                gate.Open[k] = along + side * 4f;
                leaf.transform.rotation = Quaternion.Euler(0f, 0f, gate.Closed[k]);
                gate.Leaves[k] = leaf.transform;
                if (Application.isPlaying)
                {
                    var body = leaf.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    var box = leaf.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(len, 0.6f);
                    box.offset = new Vector2(len * 0.5f, 0f);
                    gate.Bodies[k] = body;
                }
            }
            // The lamp on the wall where the boats come up to it: red shut, green open.
            var lv = new List<Vector3>(); var lu = new List<Vector2>(); var lt = new List<int>(); var lc = new List<Color32>();
            Vector2 at = gate.Centre - gate.Along * 1.2f + gate.Across * (gate.Half + 0.8f);
            for (int s = 0; s < 2; s++)
            {
                float r = s == 0 ? 0.55f : 0.4f;
                Quad(lv, lu, lt, lc, at + new Vector2(-r, -r), at + new Vector2(r, -r), at + new Vector2(r, r), at + new Vector2(-r, r), s == 0 ? new Color32(20, 22, 26, 255) : new Color32(255, 60, 50, 255));
            }
            gate.Lamp = Part("GateLamp", plain, LampOrder, Quads("GateLamp", lv, lu, lt, lc)).GetComponent<MeshFilter>();
            _gates.Add(gate);
        }

        float GateOpenness(int index)
        {
            float period = _coast.gatePeriod, open = _coast.gateOpen * period, slide = 0.7f;
            // Each gate a little later than the one below it: a wave of gates opening up the staircase.
            float u = Mathf.Repeat(Track.Clock - index * period * 0.33f, period);
            if (u < slide) return u / slide;
            if (u < open) return 1f;
            if (u < open + slide) return 1f - (u - open) / slide;
            return 0f;
        }

        void UpdateGates()
        {
            for (int i = 0; i < _gates.Count; i++)
            {
                var g = _gates[i];
                g.Openness = GateOpenness(i);
                float e = Mathf.SmoothStep(0f, 1f, g.Openness);
                for (int k = 0; k < 2; k++)
                {
                    float a = Mathf.LerpAngle(g.Closed[k], g.Open[k], e);
                    if (g.Bodies[k] != null) g.Bodies[k].MoveRotation(a);
                    else g.Leaves[k].rotation = Quaternion.Euler(0f, 0f, a);
                }
                var colors = g.Lamp.sharedMesh.colors32;
                var lamp = g.Openness > 0.6f ? new Color32(70, 230, 110, 255) : new Color32(255, 60, 50, 255);
                for (int k = 4; k < 8; k++) colors[k] = lamp;
                g.Lamp.sharedMesh.colors32 = colors;
            }
        }

        // Rivals wait at a shut gate (Track.Obstacle).
        bool GateShut(Vector2 p)
        {
            if (_gates.Count == 0) return false;
            var on = track.Path.Project(p);
            foreach (var g in _gates)
                if (g.Openness < 0.6f && Mathf.Abs(track.Path.DeltaDistance(g.Distance, on.Distance)) < 1.5f && Mathf.Abs(on.Lateral) < g.Half + 1f) return true;
            return false;
        }

        // How high the water has lifted a boat: a step at each lock, up to the top; back at sea level past the cliff edge.
        float Lift(Vector2 p)
        {
            var gates = _coast.gates;
            if (gates == null || gates.Length == 0) return 0f;
            var path = track.Path;
            var on = path.Project(p);
            float into = path.DeltaDistance(gates[0] - 1f, on.Distance), span = path.DeltaDistance(gates[0] - 1f, _coast.top);
            if (into < 0f || into > span || Mathf.Abs(on.Lateral) > track.OffRoadAt(on.Distance) + 2f) return 0f;
            float level = 0f;
            foreach (float g in gates) level += Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(g, g + 7f, on.Distance));
            return level / gates.Length * 0.8f;
        }

        // ------------------------------------------------------------------ the wreck and the waterfall

        void BuildWreck(TrackPath path)
        {
            float d = _coast.wreck;
            Vector2 at = path.PointAt(d), along = path.TangentAt(d), across = path.NormalAt(d);
            float heading = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg - 90f;
            float half = track.OffRoadAt(d);
            float length = 26f;
            if (wreck != null)
            {
                var sr = Sprite("Wreck", wreck, at, length, heading, WreckOrder);
                // Its width to fit round the channel.
                float w = wreck.bounds.size.x * sr.transform.localScale.x, want = (half + 0.6f) * 2f / (112f / 160f);
                sr.transform.localScale = new Vector3(sr.transform.localScale.x * want / w, sr.transform.localScale.y, 1f);
            }
            if (!Application.isPlaying) return;
            foreach (float side in new[] { 1f, -1f })
            {
                var go = new GameObject("WreckSide", typeof(BoxCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.transform.SetPositionAndRotation(at + across * side * (half + 1f), Quaternion.Euler(0f, 0f, heading));
                go.GetComponent<BoxCollider2D>().size = new Vector2(1.4f, length * 0.62f);
                _parts.Add(go);
            }
        }

        // The pond spilling over the cliff edge: foam at the lip, white water streaming down the face, spray at the foot.
        void BuildFall(TrackPath path, TrackLayout.Gap gap)
        {
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
            float len = Mathf.Repeat(gap.to - gap.from, path.Length);
            Vector2 lip = path.PointAt(gap.from), along = path.TangentAt(gap.from), across = path.NormalAt(gap.from);
            float half = track.OffRoadAt(gap.from) + 1.5f;
            var random = new System.Random(5);
            for (int i = 0; i < 40; i++)
            {
                float x = Mathf.Lerp(-half, half, (i + 0.5f) / 40f), w = 0.2f + (float)random.NextDouble() * 0.3f;
                float down = len * (0.5f + (float)random.NextDouble() * 0.35f);
                Vector2 a = lip + across * x, b = a + along * down;
                int s = v.Count;
                v.Add(a - across * w); v.Add(a + across * w); v.Add(b - across * w * 1.6f); v.Add(b + across * w * 1.6f);
                for (int k = 0; k < 4; k++) u.Add(Vector2.zero);
                c.Add(new Color32(240, 250, 255, 230)); c.Add(new Color32(240, 250, 255, 230)); c.Add(new Color32(240, 250, 255, 0)); c.Add(new Color32(240, 250, 255, 0));
                t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
            }
            // A dark band: the cliff face under the falling water.
            Quad(v, u, t, c, lip - across * half, lip + across * half, lip + across * half + along * len * 0.9f, lip - across * half + along * len * 0.9f, new Color32(20, 26, 32, 50));
            Quad(v, u, t, c, lip - across * half - along * 0.6f, lip + across * half - along * 0.6f, lip + across * half + along * 0.4f, lip - across * half + along * 0.4f, new Color32(255, 255, 255, 235));
            Part("Waterfall", plain, FallOrder, Quads("Waterfall", v, u, t, c));
            if (!Application.isPlaying || foamParticles == null) return;
            var go = new GameObject("FallSpray") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.transform.position = path.PointAt(gap.to - len * 0.15f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 1.4f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f); main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.6f);
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(half * 2f, 3f, 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg);
            var em = ps.emission; em.rateOverTime = 40f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = foamParticles;
            r.sortingOrder = FallOrder + 1;
            ps.Play();
            _parts.Add(go);
        }

        // ------------------------------------------------------------------ the night, the beam, rain and lightning

        MeshFilter _night;
        Vector3[] _nightLocal;
        Color32[] _nightColors;
        float _nightSize = -1f;
                float _break, _flash, _nextStrike, _thunderAt = -1f;
        const int Cols = 40, Rows = 84;
        static readonly Color NightColor = new(0.01f, 0.03f, 0.08f), MoonColor = new(0.05f, 0.11f, 0.26f);

        void BuildNight(Camera cam)
        {
            if (_night != null) { Destroy(_night.gameObject); _night = null; }
            // Built a size up (the camera zooms out a little at speed), so it is only rebuilt for a real zoom.
            float size = cam.orthographicSize * 1.3f;
            float h = size + 4f, w = size * cam.aspect + 4f;
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
            for (int y = 0; y <= Rows; y++)
                for (int x = 0; x <= Cols; x++)
                {
                    v.Add(new Vector3(Mathf.Lerp(-w, w, x / (float)Cols), Mathf.Lerp(-h, h, y / (float)Rows), 0f));
                    u.Add(Vector2.zero);
                    c.Add(new Color32(0, 0, 0, 0));
                }
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    int a = y * (Cols + 1) + x;
                    t.AddRange(new[] { a, a + Cols + 1, a + 1, a + 1, a + Cols + 1, a + Cols + 2 });
                }
            var go = Part("Night", plain, NightOrder, Quads("Night", v, u, t, c), cam.transform);
            go.transform.localPosition = new Vector3(0f, 0f, 5f);
            go.transform.localRotation = Quaternion.identity;
            _parts.Add(go);
            _night = go.GetComponent<MeshFilter>();
            _nightLocal = v.ToArray();
            _nightColors = c.ToArray();
            _nightSize = size;
        }

        // Rain: thin streaks blowing across the screen (a mesh on the camera, moved every frame).
        MeshFilter _rainMesh;
        Vector3[] _rainVerts;
        Vector2[] _rainDrops;
        Color32[] _rainColors;
        const int Drops = 150;
        static readonly Vector2 RainWind = new(7f, -15f);

        void BuildRain(Camera cam)
        {
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
            _rainDrops = new Vector2[Drops];
            float h = cam.orthographicSize + 2f, w = cam.orthographicSize * cam.aspect + 2f;
            for (int i = 0; i < Drops; i++)
            {
                _rainDrops[i] = new Vector2(Random.Range(-w, w), Random.Range(-h, h));
                Quad(v, u, t, c, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color32(200, 220, 255, 0));
            }
            var go = Part("Rain", plain, RainOrder, Quads("Rain", v, u, t, c), cam.transform);
            go.transform.localPosition = new Vector3(0f, 0f, 4f);
            go.transform.localRotation = Quaternion.identity;
            _parts.Add(go);
            _rainMesh = go.GetComponent<MeshFilter>();
            _rainVerts = v.ToArray();
            _rainColors = c.ToArray();
        }

        void UpdateRain(Camera cam, float dt, float amount)
        {
            if (_rainMesh == null) return;
            float h = cam.orthographicSize + 2f, w = cam.orthographicSize * cam.aspect + 2f;
            Vector2 dir = RainWind.normalized, side = new Vector2(-dir.y, dir.x) * 0.035f, len = dir * 1.1f;
            byte a = (byte)(110f * amount);
            for (int i = 0; i < Drops; i++)
            {
                var p = _rainDrops[i] + RainWind * dt * (0.8f + (i % 5) * 0.1f);
                if (p.y < -h) p = new Vector2(Random.Range(-w - 6f, w), h);
                if (p.x > w) p.x -= 2f * w;
                _rainDrops[i] = p;
                int k = i * 4;
                // (Quad's corner order: a, b, d, c)
                _rainVerts[k] = p - side; _rainVerts[k + 1] = p + side; _rainVerts[k + 2] = p - side - len; _rainVerts[k + 3] = p + side - len;
                var col = new Color32(205, 225, 255, a);
                _rainColors[k] = col; _rainColors[k + 1] = col; _rainColors[k + 2] = new Color32(205, 225, 255, 0); _rainColors[k + 3] = new Color32(205, 225, 255, 0);
            }
            var mesh = _rainMesh.sharedMesh;
            mesh.vertices = _rainVerts;
            mesh.colors32 = _rainColors;
            mesh.RecalculateBounds();
        }

        void UpdateNight(float dt)
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (_night == null || cam.orthographicSize > _nightSize || cam.orthographicSize < _nightSize * 0.5f) BuildNight(cam);
            if (_rainMesh == null) BuildRain(cam);
            _break = Mathf.MoveTowards(_break, LastLap ? 1f : 0f, dt / 5f);
            // Lightning: two quick flashes, thunder a moment later (not once the storm has broken).
            if (_break < 0.1f && Time.time > _nextStrike)
            {
                _nextStrike = Time.time + Random.Range(6f, 13f);
                _flash = 1f;
                _thunderAt = Time.time + Random.Range(0.3f, 0.8f);
            }
            if (_thunderAt > 0f && Time.time > _thunderAt)
            {
                _thunderAt = -1f;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.thunder, 0.6f);
                if (_shake != null) _shake.Shake(0.2f);
            }
            float flash = _flash > 0f ? (_flash > 0.75f || (_flash > 0.45f && _flash < 0.6f) ? 1f : _flash * 0.5f) : 0f;
            _flash = Mathf.Max(0f, _flash - dt * 2.2f);
            UpdateRain(cam, dt, 1f - _break);

            float darkness = Mathf.Lerp(_coast.darkness, _coast.darkness * 0.55f, _break) * (1f - flash * 0.5f);
            Color night = Color.Lerp(NightColor, MoonColor, _break);
            Color beamColor = Color.Lerp(new Color(1f, 0.97f, 0.8f), new Color(1f, 0.78f, 0.25f), _break);
            float wash = Mathf.Lerp(0.1f, 0.24f, _break);
            Vector2 lighthouse = _coast.lighthouse;
            float beamAngle = Track.Clock / _coast.beamPeriod * 360f;
            // The small lighthouses near the camera (no lights on the boats: only the lighthouses light the sea).
            Vector2 camPos = cam.transform.position;
            float reach = cam.orthographicSize * 2f + 30f;
            var lamps = new List<(Vector2 p, float angle)>();
            if (_coast.lights != null)
                for (int k = 0; k < _coast.lights.Length; k++)
                {
                    var lp = _coast.lights[k];
                    if ((lp - camPos).sqrMagnitude > reach * reach) continue;
                    lamps.Add((lp, Track.Clock / (4.5f + (k % 3) * 0.8f) * 360f * (k % 2 == 0 ? 1f : -1f) + k * 47f));
                }
            var tr = _night.transform;
            for (int i = 0; i < _nightLocal.Length; i++)
            {
                Vector2 p = tr.TransformPoint(_nightLocal[i]);
                // The beam: a wedge turning round the lighthouse (a glow round its lamp).
                Vector2 q = p - lighthouse;
                float r = q.magnitude;
                float diff = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg, beamAngle));
                float halfAngle = Mathf.Max(4.5f, Mathf.Atan2(2.5f, Mathf.Max(0.1f, r)) * Mathf.Rad2Deg);
                float beam = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(halfAngle * 0.5f, halfAngle, diff))) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(110f, 180f, r)));
                beam = Mathf.Max(beam, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 12f, r)));
                // The small lighthouses: a pool of light round each, and its short turning beam.
                float lamp = 0f;
                foreach (var (lp, la) in lamps)
                {
                    Vector2 o = p - lp;
                    float d = o.magnitude;
                    if (d > 50f) continue;
                    float pool = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7f, 17f, d));
                    float off = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(o.y, o.x) * Mathf.Rad2Deg, la));
                    float ray = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 9f, off))) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(25f, 48f, d)));
                    lamp = Mathf.Max(lamp, Mathf.Max(pool, ray * 0.85f));
                }
                float glow = Mathf.Max(beam, lamp);
                float lit = Mathf.Clamp01(glow * 0.85f);
                float alpha = darkness * (1f - lit) + glow * wash;
                Color col = Color.Lerp(night, beamColor, Mathf.Clamp01(glow * wash / Mathf.Max(0.01f, alpha)));
                _nightColors[i] = new Color(col.r, col.g, col.b, Mathf.Clamp01(alpha));
            }
            _night.sharedMesh.colors32 = _nightColors;
        }

        void Update()
        {
            if (!Application.isPlaying || track == null || _coast == null || !_coast.enabled || track.Layout == null || track.Layout.biome != TrackLayout.Biome.Lagoon) return;
            float dt = Time.deltaTime;
            float target = Mathf.Clamp01((Track.PhaseLap - 1) / 2f);
            _tide = Mathf.MoveTowards(_tide, target, dt / 4f);
            ApplyTide(_tide);
            UpdateGates();
            UpdateNight(dt);
        }
    }
}
