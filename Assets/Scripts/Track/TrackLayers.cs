using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Viaducts (<see cref="TrackLayout.viaducts"/>): the road up on a stone bridge where it crosses over a road
    /// below. The deck is drawn above the cars down below (they pass under it), the cars up on it above the deck;
    /// cars on different levels pass through each other, and the railings only stop the cars up top. Its shadow
    /// falls on the ground, piers stand under it.
    /// </summary>
    public class TrackLayers : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;

        // Above the cars on the ground, below the cars up on the deck (they are lifted by UpperBoost).
        const int ShadowOrder = -44, DeckOrder = 30, RailOrder = 31, UpperBoost = 40;
        static readonly Color Stone = new Color(0.55f, 0.58f, 0.64f).linear, StoneDark = new Color(0.32f, 0.35f, 0.41f).linear,
            Rail = new Color(0.94f, 0.96f, 1f).linear, RailBlue = new Color(0.25f, 0.45f, 0.75f).linear;

        readonly List<GameObject> _parts = new();
        readonly List<Collider2D> _railings = new();
        SomeGame.Car.CarMovement[] _cars;
        readonly Dictionary<SomeGame.Car.CarMovement, bool> _upper = new();
        readonly Dictionary<SpriteRenderer, int> _baseOrder = new();

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _railings.Clear();
            if (layout.viaducts == null) return;
            foreach (var span in layout.viaducts) BuildViaduct(path, layout, span);
        }

        void BuildViaduct(TrackPath path, TrackLayout layout, TrackLayout.Span span)
        {
            float length = path.DeltaDistance(span.from, span.to);
            int steps = Mathf.Max(2, Mathf.CeilToInt(length));
            var dv = new List<Vector3>(); var duv = new List<Vector2>(); var dt = new List<int>();
            var rv = new List<Vector3>(); var rc = new List<Color>(); var rt = new List<int>();
            var sv = new List<Vector3>(); var sc = new List<Color>(); var st = new List<int>();
            var sides = new List<Vector2>[2] { new(), new() };
            for (int i = 0; i <= steps; i++)
            {
                float d = span.from + length * i / steps;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                float w = track.OffRoadAt(d);
                int k = dv.Count;
                dv.Add(p + n * (w + 0.9f)); dv.Add(p - n * (w + 0.9f));
                duv.Add(new Vector2(0f, d / 4f)); duv.Add(new Vector2(1f, d / 4f));
                if (i > 0) dt.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 });
                // Its shadow, well off to the side (it stands high).
                Vector2 so = new(2.4f, -3.2f);
                int j = sv.Count;
                float fade = Mathf.Clamp01(Mathf.Min(i, steps - i) / 4f);
                var shade = new Color(0f, 0.04f, 0.12f, 0.3f * fade);
                sv.Add(p + n * (w + 1f) + so * fade); sv.Add(p - n * (w + 1f) + so * fade); sc.Add(shade); sc.Add(shade);
                if (i > 0) st.AddRange(new[] { j - 2, j, j - 1, j - 1, j, j + 1 });
                sides[0].Add(p + n * (w + 0.6f)); sides[1].Add(p - n * (w + 0.6f));
            }
            // Piers under it, every few metres, and the stone edges of the deck.
            for (float s = 3f; s < length - 3f; s += 9f)
            {
                float d = span.from + s;
                Vector2 p = path.PointAt(d) + new Vector2(1.2f, -1.6f), n = path.NormalAt(d), t = path.TangentAt(d);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 c = p + n * side * (track.OffRoadAt(d) - 0.5f);
                    Quad(c - t * 0.9f - n * 0.9f, c + t * 0.9f - n * 0.9f, c + t * 0.9f + n * 0.9f, c - t * 0.9f + n * 0.9f, StoneDark, sv, sc, st);
                }
            }
            for (int side = 0; side < 2; side++)
            {
                var line = sides[side];
                for (int i = 1; i < line.Count; i++)
                {
                    Vector2 a = line[i - 1], b = line[i], d = (b - a).normalized, n = new(-d.y, d.x);
                    Quad(a - n * 0.55f, b - n * 0.55f, b + n * 0.55f, a + n * 0.55f, StoneDark, rv, rc, rt);
                    Quad(a - n * 0.32f, b - n * 0.32f, b + n * 0.32f, a + n * 0.32f, i % 4 < 2 ? Rail : RailBlue, rv, rc, rt);
                }
                if (!Application.isPlaying) continue;
                var go = new GameObject("ViaductRailing") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                _parts.Add(go);
                var edge = go.AddComponent<EdgeCollider2D>();
                edge.points = line.ToArray();
                edge.edgeRadius = 0.3f;
                _railings.Add(edge);
            }
            Part("ViaductShadow", Mesh("ViaductShadow", sv, sc, st, null), plain, ShadowOrder);
            var deckMaterial = track.Layout.roadMaterial != null ? track.Layout.roadMaterial : plain;
            var deckColors = new List<Color>(); foreach (var _ in dv) deckColors.Add(Color.white);
            Part("ViaductDeck", Mesh("ViaductDeck", dv, deckColors, dt, duv), deckMaterial, DeckOrder);
            Part("ViaductRails", Mesh("ViaductRails", rv, rc, rt, null), plain, RailOrder);
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
        }

        // Each car on its level: drawn above or below the deck, colliding only with cars on its own level and,
        // up top, with the railings.
        void FixedUpdate()
        {
            if (!Application.isPlaying || _cars == null || track.Layout.viaducts == null || track.Layout.viaducts.Count == 0) return;
            bool changed = false;
            foreach (var car in _cars)
            {
                if (car == null) continue;
                var sensor = car.GetComponent<TrackSensor>();
                bool up = sensor != null && sensor.OnViaduct;
                if (_upper.TryGetValue(car, out bool was) && was == up) continue;
                _upper[car] = up;
                changed = true;
                foreach (var sr in car.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (!_baseOrder.ContainsKey(sr)) _baseOrder[sr] = sr.sortingOrder;
                    sr.sortingOrder = _baseOrder[sr] + (up ? UpperBoost : 0);
                }
                var collider = car.GetComponent<Collider2D>();
                if (collider != null) foreach (var rail in _railings) Physics2D.IgnoreCollision(collider, rail, !up);
            }
            if (!changed) return;
            for (int a = 0; a < _cars.Length; a++)
                for (int b = a + 1; b < _cars.Length; b++)
                {
                    var ca = _cars[a] != null ? _cars[a].GetComponent<Collider2D>() : null;
                    var cb = _cars[b] != null ? _cars[b].GetComponent<Collider2D>() : null;
                    if (ca == null || cb == null) continue;
                    bool ua = _upper.TryGetValue(_cars[a], out bool x) && x, ub = _upper.TryGetValue(_cars[b], out bool y) && y;
                    Physics2D.IgnoreCollision(ca, cb, ua != ub);
                }
        }

        static void Quad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) c.Add(color);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        static Mesh Mesh(string name, List<Vector3> v, List<Color> c, List<int> t, List<Vector2> uv)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            if (uv != null) mesh.SetUVs(0, uv);
            var both = new List<int>(t);
            for (int i = 0; i < t.Count; i += 3) both.AddRange(new[] { t[i], t[i + 2], t[i + 1] });
            mesh.SetTriangles(both, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void Part(string name, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            _parts.Add(go);
        }
    }
}
