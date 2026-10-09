using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws the special ground from the track data: rivers and lakes with sandy banks (drawn over the
    /// road where a river crosses it), the kicker ramps before each crossing, the big shortcut ramps
    /// with their sandy approach, and the oil puddles.
    /// </summary>
    public class TrackFeatures : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter water;
        [SerializeField] MeshFilter banks;
        [SerializeField] MeshFilter ramps;
        [SerializeField] MeshFilter oil;
        [Tooltip("Lakes are drawn under the road (it crosses them on causeways); rivers over it.")]
        [SerializeField] MeshFilter lakeWater;
        [SerializeField] MeshFilter lakeBanks;
        [Tooltip("White arrows on the road shortly before every ramp.")]
        [SerializeField] MeshFilter arrows;
        [SerializeField, Min(0.5f)] float arrowSize = 1.8f;
        [Tooltip("World units per repeat of the water texture.")]
        [SerializeField, Min(0.1f)] float waterTile = 8f;
        [SerializeField, Min(0f)] float bankWidth = 1.6f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            var w = new MeshData();
            var b = new MeshData();
            foreach (var river in layout.rivers) River(river, w, b);
            var lw = new MeshData();
            var lb = new MeshData();
            foreach (var lake in layout.lakes) Lake(lake, lw, lb);
            Assign(water, w.ToMesh("Water"));
            if (lakeWater != null) Assign(lakeWater, lw.ToMesh("LakeWater"));
            if (lakeBanks != null) Assign(lakeBanks, lb.ToMesh("LakeBanks"));

            // Approach arrows: in line with each kicker, and leading off the road onto each corner-cut ramp.
            var a = new MeshData();
            foreach (var jump in track.Jumps)
                foreach (float back in new[] { 8f, 15f })
                {
                    float d = jump.RampStart - back;
                    Vector2 fwd = path.TangentAt(d);
                    Arrow(a, path.PointAt(d) + path.NormalAt(d) * jump.Lateral, fwd, arrowSize);
                }
            foreach (var cut in track.Shortcuts)
            {
                Vector2 roadPoint = path.PointAt(cut.From);
                float side = Mathf.Sign(Vector2.Dot(path.NormalAt(cut.From), cut.Centre - roadPoint));
                for (int k = 0; k < 2; k++)
                {
                    float d = cut.From - 5f - k * 6f;
                    float lateral = side * Mathf.Max(0f, track.HalfWidthAt(d) - 1.6f - k * 0.8f);
                    Vector2 fwd = Vector2.Lerp(cut.Direction, path.TangentAt(d), k * 0.5f).normalized;
                    Arrow(a, path.PointAt(d) + path.NormalAt(d) * lateral, fwd, arrowSize);
                }
            }
            if (arrows != null) Assign(arrows, a.ToMesh("Arrows"));

            var r = new MeshData();
            var pads = b;
            foreach (var jump in track.Jumps)
            {
                float length = path.DeltaDistance(jump.RampStart, jump.Lip);
                float mid = jump.RampStart + length * 0.5f;
                Vector2 forward = path.TangentAt(mid), side = path.NormalAt(mid);
                Vector2 centre = path.PointAt(mid) + side * jump.Lateral;
                r.Quad(centre, side * (layout.rampWidth * 0.5f), forward * (length * 0.5f), new Rect(0f, 0f, 1f, 1f));
            }
            foreach (var ramp in track.Shortcuts)
            {
                Vector2 across = new Vector2(ramp.Direction.y, -ramp.Direction.x);
                r.Quad(ramp.Centre, across * (ramp.Width * 0.5f), ramp.Direction * (ramp.Length * 0.5f), new Rect(0f, 0f, 1f, 1f));
                // Sandy approach from the road to the ramp.
                Vector2 padCentre = ramp.Centre - ramp.Direction * (ramp.Length * 0.5f + 1.4f);
                Vector2 hx = across * (ramp.Width * 0.5f + 1.4f), hy = ramp.Direction * 2f;
                pads.QuadPoints(padCentre - hx - hy, padCentre + hx - hy, padCentre + hx + hy, padCentre - hx + hy,
                    new Vector2(0.2f, 0f), new Vector2(0.2f, 1f), new Vector2(0.2f, 1f), new Vector2(0.2f, 0f));
            }
            Assign(ramps, r.ToMesh("Ramps"));
            Assign(banks, b.ToMesh("Banks"));

            var o = new MeshData();
            foreach (var spot in layout.oil)
            {
                Vector2 centre = path.PointAt(spot.distance) + path.NormalAt(spot.distance) * spot.lateral;
                float angle = spot.distance * 1.7f; // varied, but stable for each puddle
                Vector2 x = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (spot.radius * 1.15f);
                o.Quad(centre, x, new Vector2(-x.y, x.x), new Rect(0f, 0f, 1f, 1f));
            }
            Assign(oil, o.ToMesh("Oil"));
        }

        static void Arrow(MeshData mesh, Vector2 centre, Vector2 forward, float size)
        {
            Vector2 right = new Vector2(forward.y, -forward.x);
            mesh.Quad(centre, right * (size * 0.5f), forward * (size * 0.5f), new Rect(0f, 0f, 1f, 1f));
        }

        void River(TrackLayout.River river, MeshData waterMesh, MeshData bankMesh)
        {
            var pts = river.points;
            if (pts.Length < 2) return;
            float half = river.width * 0.5f;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 dir = (pts[Mathf.Min(i + 1, pts.Length - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                Vector2 n = new(-dir.y, dir.x);
                Vector2 p = pts[i];
                // Water: one strip; banks: a strip on each side fading out into the grass (u 0 → 1).
                waterMesh.StripPoint(p - n * half, p + n * half, WorldUv(p - n * half), WorldUv(p + n * half), i > 0);
                bankMesh.StripPoint(p + n * (half - 0.05f), p + n * (half + bankWidth), new Vector2(0f, i), new Vector2(0.96f, i), i > 0, 0);
                bankMesh.StripPoint(p - n * (half - 0.05f), p - n * (half + bankWidth), new Vector2(0f, i), new Vector2(0.96f, i), i > 0, 1);
            }
        }

        void Lake(TrackLayout.Lake lake, MeshData waterMesh, MeshData bankMesh)
        {
            const int segments = 48;
            var rot = Quaternion.Euler(0f, 0f, lake.angle);
            Vector2 Edge(float a, float grow) =>
                lake.center + (Vector2)(rot * new Vector2(Mathf.Cos(a) * (lake.radii.x + grow), Mathf.Sin(a) * (lake.radii.y + grow)));
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector2 p0 = Edge(a0, 0f), p1 = Edge(a1, 0f);
                waterMesh.Triangle(lake.center, p1, p0, WorldUv(lake.center), WorldUv(p1), WorldUv(p0));
                Vector2 q0 = Edge(a0, bankWidth), q1 = Edge(a1, bankWidth);
                bankMesh.QuadPoints(p0, p1, q1, q0, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.96f, 1f), new Vector2(0.96f, 0f));
            }
        }

        Vector2 WorldUv(Vector2 p) => p / waterTile;

        /// <summary>Small mesh builder for strips, quads and triangles.</summary>
        sealed class MeshData
        {
            readonly List<Vector3> _v = new();
            readonly List<Vector2> _uv = new();
            readonly List<int> _t = new();
            readonly int[] _lastStrip = { -1, -1 };

            public void StripPoint(Vector2 a, Vector2 b, Vector2 uvA, Vector2 uvB, bool connect, int strip = 0)
            {
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _uv.Add(uvA); _uv.Add(uvB);
                int last = _lastStrip[strip];
                if (connect && last >= 0) _t.AddRange(new[] { last, i, last + 1, last + 1, i, i + 1 });
                _lastStrip[strip] = i;
            }

            public void Quad(Vector2 centre, Vector2 halfRight, Vector2 halfUp, Rect uv)
            {
                QuadPoints(centre - halfRight - halfUp, centre + halfRight - halfUp, centre + halfRight + halfUp, centre - halfRight + halfUp,
                    new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
            }

            public void QuadPoints(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
                _uv.Add(ua); _uv.Add(ub); _uv.Add(uc); _uv.Add(ud);
                _t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            public void Triangle(Vector2 a, Vector2 b, Vector2 c, Vector2 ua, Vector2 ub, Vector2 uc)
            {
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(c);
                _uv.Add(ua); _uv.Add(ub); _uv.Add(uc);
                _t.AddRange(new[] { i, i + 1, i + 2 });
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
                mesh.SetVertices(_v);
                mesh.SetUVs(0, _uv);
                var colors = new Color32[_v.Count];
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                mesh.colors32 = colors;
                // Double-sided winding is not guaranteed for every shape: draw both faces.
                var both = new List<int>(_t);
                for (int i = 0; i < _t.Count; i += 3) both.AddRange(new[] { _t[i], _t[i + 2], _t[i + 1] });
                mesh.SetTriangles(both, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
