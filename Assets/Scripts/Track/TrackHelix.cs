using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The helix inside the mountain (<see cref="TrackLayout.helix"/>): the road climbs round and round one small circle,
    /// the loops stacked on top of each other. Every car is on a level (by how far along the climb it is): it only
    /// meets the walls of its level (each loop is its own cave) and the cars on it, and while you are inside, the cars on
    /// the other levels are hidden. Inside, the mountain is rock all round (its colour changing level by level as you
    /// climb); from outside, the mountain's top - rock and a snowy peak - covers it all, and fades away as you drive in.
    /// </summary>
    public class TrackHelix : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;

        const int InteriorOrder = -59, RoofOrder = 90, RoofShadowOrder = -58, CoverOrder = -43;
        static readonly Color Deep = new Color(0.16f, 0.17f, 0.22f).linear, High = new Color(0.42f, 0.4f, 0.4f).linear,
            Rock = new Color(0.46f, 0.47f, 0.52f).linear, RockDark = new Color(0.3f, 0.31f, 0.36f).linear,
            Snow = new Color(0.96f, 0.97f, 1f).linear, SnowShade = new Color(0.78f, 0.84f, 0.93f).linear;

        readonly List<GameObject> _parts = new();
        Mesh _interior, _roof;
        Color[] _interiorBase, _roofBase;
        float _roofAlpha = 1f, _tint = -1f;

        SomeGame.Car.CarMovement[] _cars;
        SomeGame.Car.CarMovement _player;
        SomeGame.UI.ComboDisplay _combo;
        readonly Dictionary<SomeGame.Car.CarMovement, int> _level = new();
        readonly Dictionary<SomeGame.Car.CarMovement, bool> _shown = new();
        int _playerLevel = int.MinValue;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            var h = layout.helix;
            if (h == null || !h.enabled) return;
            // Inside: rock all round the tunnels (no snow, no trees in a mountain).
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            const int n = 48;
            for (int k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                float r0 = h.mountainRadius * Rag(k, 1f), r1 = h.mountainRadius * Rag(k + 1, 1f);
                Tri(h.centre, h.centre + Dir(a0) * r0, h.centre + Dir(a1) * r1, Deep, Deep, Deep, v, c, t);
            }
            _interior = Mesh("HelixInterior", v, c, t);
            _interiorBase = c.ToArray();
            Part("HelixInterior", _interior, InteriorOrder);
            // The mountain's top: rock out to a ragged foot, ridges up to a snowy peak; its shadow on the snow.
            v = new List<Vector3>(); c = new List<Color>(); t = new List<int>();
            var sv = new List<Vector3>(); var sc = new List<Color>(); var st = new List<int>();
            Vector2 peak = h.centre + new Vector2(-0.12f, 0.1f) * h.mountainRadius;
            for (int k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                Vector2 f0 = h.centre + Dir(a0) * h.mountainRadius * Rag(k, 1.08f), f1 = h.centre + Dir(a1) * h.mountainRadius * Rag(k + 1, 1.08f);
                Vector2 m0 = Vector2.Lerp(peak, f0, 0.52f + 0.08f * Mathf.Sin(k * 1.7f)), m1 = Vector2.Lerp(peak, f1, 0.52f + 0.08f * Mathf.Sin((k + 1) * 1.7f));
                // light on the upper left, shade on the lower right
                float lit = 0.5f + 0.5f * Vector2.Dot(Dir((a0 + a1) * 0.5f), new Vector2(-0.6f, 0.8f));
                Color rock = Color.Lerp(RockDark, Rock, lit), snow = Color.Lerp(SnowShade, Snow, lit);
                bool ridge = k % 6 == 0;
                Tri(m0, f0, f1, rock, rock, rock, v, c, t);
                Tri(m0, f1, m1, rock, rock, rock, v, c, t);
                Tri(peak, m0, m1, snow, ridge ? SnowShade : snow, snow, v, c, t);
                Vector2 so = new Vector2(4f, -5f);
                var shade = new Color(0f, 0.04f, 0.12f, 0.35f);
                int i = sv.Count;
                sv.Add(h.centre + so); sv.Add(f0 + so); sv.Add(f1 + so); sc.Add(shade); sc.Add(shade); sc.Add(shade);
                st.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 });
            }
            _roof = Mesh("MountainTop", v, c, t);
            _roofBase = c.ToArray();
            Part("MountainTop", _roof, RoofOrder);
            Part("MountainShadow", Mesh("MountainShadow", sv, sc, st), RoofShadowOrder);
            _roofAlpha = 1f;
            _tint = -1f;
            _entryCover = Cover(path, h, h.entry, h.from);
            _exitCover = Cover(path, h, h.to, h.exit);
            _tunnelsFor = int.MinValue;
        }

        static Vector2 Dir(float a) => new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        static float Rag(int k, float scale) => scale * (1f + 0.07f * Mathf.Sin(k * 2.3f) + 0.04f * Mathf.Sin(k * 5.1f + 1f));

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
            foreach (var car in _cars) if (car.GetComponent<SomeGame.Input.PlayerCarInput>() != null) _player = car;
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
        }

        int LevelOf(SomeGame.Car.CarMovement car, out bool inside)
        {
            var sensor = car.GetComponent<TrackSensor>();
            float d = sensor != null ? sensor.Current.Distance : 0f;
            var level = track.HelixLevel(d);
            inside = level.HasValue;
            var h = track.Layout.helix;
            return level ?? (d < h.entry ? -1 : h.turns);
        }

        // Each car on its level: its walls and its cars only.
        void FixedUpdate()
        {
            if (!Application.isPlaying || _cars == null || track.Layout.helix == null || !track.Layout.helix.enabled) return;
            var mountain = GetComponent<TrackMountain>();
            bool changed = false;
            foreach (var car in _cars)
            {
                if (car == null) continue;
                int level = LevelOf(car, out _);
                if (_level.TryGetValue(car, out int was) && was == level) continue;
                _level[car] = level;
                changed = true;
                var collider = car.GetComponent<Collider2D>();
                if (collider == null || mountain == null) continue;
                foreach (var (cave, walls, _) in mountain.CaveWalls)
                {
                    if (cave.level == -99 || walls == null) continue;
                    foreach (var edge in walls.GetComponents<Collider2D>()) Physics2D.IgnoreCollision(collider, edge, cave.level != level);
                }
            }
            if (!changed) return;
            for (int a = 0; a < _cars.Length; a++)
                for (int b = a + 1; b < _cars.Length; b++)
                {
                    var ca = _cars[a] != null ? _cars[a].GetComponent<Collider2D>() : null;
                    var cb = _cars[b] != null ? _cars[b].GetComponent<Collider2D>() : null;
                    if (ca == null || cb == null) continue;
                    Physics2D.IgnoreCollision(ca, cb, _level[_cars[a]] != _level[_cars[b]]);
                }
        }

        void Update()
        {
            if (!Application.isPlaying || _player == null || track.Layout.helix == null || !track.Layout.helix.enabled) return;
            var h = track.Layout.helix;
            int me = LevelOf(_player, out bool meInside);
            // Climbing: a banner at each new level, and at the top.
            if (me != _playerLevel)
            {
                if (_playerLevel != int.MinValue && _combo != null && meInside && me > _playerLevel)
                {
                    if (me >= 0 && me < h.turns) _combo.Banner($"LEVEL {me + 1}/{h.turns}", new Color(0.7f, 0.85f, 1f));
                    else if (me == h.turns) _combo.Banner("THE SUMMIT!", new Color(1f, 0.85f, 0.35f));
                }
                _playerLevel = me;
            }
            // The cars on the other levels are out of sight (inside the rock above or below you).
            foreach (var car in _cars)
            {
                if (car == null || car == _player) continue;
                int level = LevelOf(car, out bool inside);
                bool show = !(meInside && inside && level != me);
                if (_shown.TryGetValue(car, out bool was) && was == show) continue;
                _shown[car] = show;
                foreach (var r in car.GetComponentsInChildren<Renderer>(true)) r.enabled = show;
            }
            // The tunnel in only on the bottom level, the tunnel out only at the top (the loops above and below have plain walls there).
            ShowTunnels(meInside ? me : -1);
            // The mountain's top: gone while you are inside it.
            float target = meInside ? 0f : 1f;
            float alpha = Mathf.MoveTowards(_roofAlpha, target, Time.deltaTime * 2.5f);
            if (alpha != _roofAlpha && _roof != null)
            {
                _roofAlpha = alpha;
                var cols = new Color[_roofBase.Length];
                for (int i = 0; i < cols.Length; i++) { cols[i] = _roofBase[i]; cols[i].a = _roofBase[i].a * alpha; }
                _roof.colors = cols;
            }
            // The rock round you lightens, level by level, as you climb (smoothly, all the way up).
            var sensor = _player.GetComponent<TrackSensor>();
            float climb = sensor != null ? Mathf.Clamp01((sensor.Current.Distance - h.from) / Mathf.Max(1f, h.to - h.from)) : 0f;
            if (Mathf.Abs(climb - _tint) > 0.004f && _interior != null)
            {
                _tint = climb;
                var col = Color.Lerp(Deep, High, climb);
                var cols = new Color[_interiorBase.Length];
                for (int i = 0; i < cols.Length; i++) cols[i] = col;
                _interior.colors = cols;
            }
        }

        int _tunnelsFor = int.MinValue;

        void ShowTunnels(int level)
        {
            if (level == _tunnelsFor) return;
            _tunnelsFor = level;
            var h = track.Layout.helix;
            bool inShown = level <= 0, outShown = level < 0 || level >= h.turns - 1;
            var mountain = GetComponent<TrackMountain>();
            if (mountain != null)
                foreach (var (cave, _, visuals) in mountain.CaveWalls)
                {
                    if (visuals == null) continue;
                    if (cave.level == -1) visuals.SetActive(inShown);
                    else if (cave.level == h.turns) visuals.SetActive(outShown);
                }
            if (_entryCover != null) _entryCover.SetActive(!inShown);
            if (_exitCover != null) _exitCover.SetActive(!outShown);
        }

        GameObject _entryCover, _exitCover;

        // Rock over the road of the tunnel in (or out) where it leaves the circle: seen from the other levels it is not there.
        GameObject Cover(TrackPath path, TrackLayout.Helix h, float from, float to)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (float d = from; d < to; d += 1f)
            {
                float w = track.OffRoadAt(d) + 1.4f;
                Vector2 a = path.PointAt(d), b = path.PointAt(d + 1.2f), n0 = path.NormalAt(d), n1 = path.NormalAt(d + 1.2f);
                Vector2[] q = { a - n0 * w, b - n1 * w, b + n1 * w, a + n0 * w };
                // (where it still runs round the circle - or overlaps the circle's road - it is the loops' road too: leave it)
                bool onRing = false;
                foreach (var p in new[] { a, b }) if (Mathf.Abs((p - h.centre).magnitude - h.radius) < 2f * track.OffRoadAt(h.from) + 1f) onRing = true;
                if (onRing) continue;
                int i = v.Count;
                foreach (var p in q) { v.Add(p); c.Add(Deep); }
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            var go = new GameObject("TunnelCover", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), Mesh("TunnelCover", v, c, t));
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = plain;
            renderer.sortingOrder = CoverOrder;
            go.SetActive(false);
            _parts.Add(go);
            return go;
        }

        void Part(string name, Mesh mesh, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = plain;
            renderer.sortingOrder = order;
            _parts.Add(go);
        }

        static void Tri(Vector2 a, Vector2 b, Vector2 cc, Color ca, Color cb, Color ccol, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc);
            c.Add(ca); c.Add(cb); c.Add(ccol);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 });
        }

        static Mesh Mesh(string name, List<Vector3> v, List<Color> c, List<int> t)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
