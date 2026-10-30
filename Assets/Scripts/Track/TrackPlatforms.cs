using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Wooden things and holes: platforms of planks out over the void (<see cref="TrackLayout.platforms"/>), holes in
    /// the ice over the underground river (<see cref="TrackLayout.holes"/> - water, even on the road) and hatches
    /// (<see cref="TrackLayout.hatches"/>) - a trapdoor in the ground with its doors flung open either side of the hole
    /// you drop into.
    /// </summary>
    public class TrackPlatforms : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;

        const int DeckOrder = -45, HoleOrder = -45, DoorOrder = -44;
        static readonly Color Plank = new Color(0.62f, 0.42f, 0.24f).linear, PlankDark = new Color(0.5f, 0.33f, 0.18f).linear,
            Seam = new Color(0.22f, 0.14f, 0.08f).linear, Beam = new Color(0.36f, 0.23f, 0.12f).linear, Nail = new Color(0.8f, 0.8f, 0.82f).linear,
            Water = new Color(0.06f, 0.2f, 0.3f).linear, WaterLight = new Color(0.16f, 0.42f, 0.55f).linear,
            IceRim = new Color(0.93f, 0.97f, 1f).linear, IceBlue = new Color(0.68f, 0.86f, 0.97f).linear, Pit = new Color(0.04f, 0.05f, 0.08f).linear;

        readonly List<GameObject> _parts = new();

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            if (layout.platforms != null) foreach (var span in layout.platforms) Deck(path, span, v, c, t);
            if (v.Count > 0) Part("Platforms", Mesh("Platforms", v, c, t), DeckOrder);
            v = new List<Vector3>(); c = new List<Color>(); t = new List<int>();
            if (layout.holes != null) foreach (var hole in layout.holes) Hole(path, hole, v, c, t);
            if (v.Count > 0) Part("IceHoles", Mesh("IceHoles", v, c, t), HoleOrder);
            v = new List<Vector3>(); c = new List<Color>(); t = new List<int>();
            if (layout.hatches != null) foreach (float at in layout.hatches) Hatch(path, layout, at, v, c, t);
            if (v.Count > 0) Part("Hatches", Mesh("Hatches", v, c, t), DoorOrder);
        }

        // Planks across the road (alternating shades, dark seams, a nail at each end), a beam along each side.
        void Deck(TrackPath path, TrackLayout.Span span, List<Vector3> v, List<Color> c, List<int> t)
        {
            float length = path.DeltaDistance(span.from, span.to);
            int k = 0;
            for (float s = 0f; s < length; s += 0.9f, k++)
            {
                float d0 = span.from + s, d1 = span.from + Mathf.Min(length, s + 0.8f);
                float w = track.OffRoadAt(d0) + 0.2f;
                Vector2 a = path.PointAt(d0), b = path.PointAt(d1), n0 = path.NormalAt(d0), n1 = path.NormalAt(d1);
                Quad(a - n0 * w, b - n1 * w, b + n1 * w, a + n0 * w, k % 3 == 1 ? PlankDark : Plank, v, c, t);
                Vector2 mn = path.NormalAt(span.from + s + 0.8f);
                Quad(b - n1 * w, path.PointAt(Mathf.Min(span.from + length, d1 + 0.1f)) - mn * w, path.PointAt(Mathf.Min(span.from + length, d1 + 0.1f)) + mn * w, b + n1 * w, Seam, v, c, t);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 nail = Vector2.Lerp(a, b, 0.5f) + Vector2.Lerp(n0, n1, 0.5f) * side * (w - 0.5f);
                    Quad(nail + new Vector2(-0.09f, -0.09f), nail + new Vector2(0.09f, -0.09f), nail + new Vector2(0.09f, 0.09f), nail + new Vector2(-0.09f, 0.09f), Nail, v, c, t);
                }
            }
            foreach (float side in new[] { -1f, 1f })
                for (float s = 0f; s < length; s += 1f)
                {
                    float d0 = span.from + s, d1 = span.from + Mathf.Min(length, s + 1f);
                    float w0 = track.OffRoadAt(d0) + 0.2f, w1 = track.OffRoadAt(d1) + 0.2f;
                    Vector2 a = path.PointAt(d0), b = path.PointAt(d1), n0 = path.NormalAt(d0) * side, n1 = path.NormalAt(d1) * side;
                    Quad(a + n0 * w0, b + n1 * w1, b + n1 * (w1 + 0.55f), a + n0 * (w0 + 0.55f), Beam, v, c, t);
                }
        }

        // Dark water in a ragged ring of ice, a lighter ripple inside.
        void Hole(TrackPath path, TrackLayout.Hole hole, List<Vector3> v, List<Color> c, List<int> t)
        {
            Vector2 centre = path.PointAt(hole.distance) + path.NormalAt(hole.distance) * hole.lateral;
            const int n = 18;
            float seed = hole.distance * 0.37f + hole.lateral;
            float Rag(int k) => 1f + 0.12f * Mathf.Sin(k * 2.1f + seed) + 0.06f * Mathf.Sin(k * 5.3f + seed * 2f);
            for (int k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                float r0 = hole.radius * Rag(k), r1 = hole.radius * Rag(k + 1);
                Quad(centre + d0 * r0, centre + d0 * (r0 + 0.45f), centre + d1 * (r1 + 0.45f), centre + d1 * r1, (k % 2 == 0) ? IceRim : IceBlue, v, c, t);
                Tri(centre, centre + d0 * r0, centre + d1 * r1, Water, v, c, t);
                // a lighter ring of open water inside the ice, darker again in the middle
                Quad(centre + d0 * r0 * 0.55f, centre + d0 * r0 * 0.8f, centre + d1 * r1 * 0.8f, centre + d1 * r1 * 0.55f, WaterLight, v, c, t);
            }
        }

        // A dark pit across the road at the gap, framed in wood, and the two doors flung open, lying either side.
        void Hatch(TrackPath path, TrackLayout layout, float at, List<Vector3> v, List<Color> c, List<int> t)
        {
            float to = at;
            foreach (var gap in layout.gaps)
                if (Mathf.Abs(path.DeltaDistance(gap.from, at)) < 1f) { to = gap.to; break; }
            float length = Mathf.Max(2f, path.DeltaDistance(at, to));
            float w = track.OffRoadAt(at) + 0.3f;
            Vector2 a = path.PointAt(at), b = path.PointAt(at + length), n = path.NormalAt(at), tan = path.TangentAt(at);
            Quad(a - n * w, b - n * w, b + n * w, a + n * w, Pit, v, c, t);
            // the frame
            foreach (var (p, q) in new[] { (a - n * (w + 0.5f), a + n * (w + 0.5f)), (b - n * (w + 0.5f), b + n * (w + 0.5f)) })
                Quad(p - tan * 0.5f, q - tan * 0.5f, q + tan * 0.5f, p + tan * 0.5f, Beam, v, c, t);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 e0 = a + n * side * (w + 0.5f), e1 = b + n * side * (w + 0.5f);
                Quad(e0, e1, e1 + n * side * 0.5f, e0 + n * side * 0.5f, Beam, v, c, t);
                // a door, flung open: lying beside the hole, its planks running along it
                Vector2 h0 = e0 + n * side * 0.6f, h1 = e1 + n * side * 0.6f;
                float doorW = w * 0.95f;
                for (int k = 0; k < 4; k++)
                {
                    float u0 = k / 4f, u1 = (k + 1) / 4f - 0.03f;
                    Quad(h0 + n * side * doorW * u0, h1 + n * side * doorW * u0, h1 + n * side * doorW * u1, h0 + n * side * doorW * u1, k % 2 == 0 ? Plank : PlankDark, v, c, t);
                }
                Vector2 bar0 = Vector2.Lerp(h0, h1, 0.25f), bar1 = Vector2.Lerp(h0, h1, 0.75f);
                foreach (var bar in new[] { bar0, bar1 })
                    Quad(bar - tan * 0.2f, bar + tan * 0.2f, bar + tan * 0.2f + n * side * doorW, bar - tan * 0.2f + n * side * doorW, Beam, v, c, t);
            }
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

        static void Quad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) c.Add(color);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        static void Tri(Vector2 a, Vector2 b, Vector2 cc, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc);
            for (int k = 0; k < 3; k++) c.Add(color);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 });
        }

        static Mesh Mesh(string name, List<Vector3> v, List<Color> c, List<int> t)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
