using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Scatters scenery (trees, bushes, chalets, fields, tyre piles...) around the track, keeping clear of
    /// the road and of the sandy run-off in the corners. Each kind takes its picture from a cell of one
    /// atlas texture, so everything is a single mesh. Solid kinds also get a collider in Play mode, so
    /// cars bump into them instead of driving through.
    /// </summary>
    public class TrackScenery : TrackDerivedBehaviour
    {
        public enum Solid { None, Circle, Box }

        [Serializable]
        public class Kind
        {
            public string name = "Tree";
            [Tooltip("Area of the atlas texture holding the picture (0-1 UV).")]
            public Rect uv = new(0f, 0f, 0.5f, 0.5f);
            [Min(0)] public int count = 40;
            public Vector2 sizeRange = new(2.2f, 3.6f);
            [Tooltip("Distance from the outer kerb edge: min and max (0 = no limit).")]
            public Vector2 clearance = new(3f, 0f);
            [Tooltip("Turned to follow the road (houses), instead of a random angle.")]
            public bool alignToRoad;
            [Tooltip("Clustered in forests instead of spread evenly.")]
            public bool clustered;
            [Tooltip("Only in corners, on the sand (tyre piles).")]
            public bool cornerOnly;
            [Tooltip("Turned to a multiple of 90 degrees instead of a random angle (fields, barns).")]
            public bool squareAngle;
            [Tooltip("Collider shape in Play mode (None = cars drive over it).")]
            public Solid solid;
            [Tooltip("Collider size as a fraction of the item size: circle radius in x, or box half width / half length.")]
            public Vector2 solidSize = new(0.35f, 0.35f);
            [Tooltip("Each item picks one of these tints at random.")]
            public Color[] tints = { Color.white };
        }

        [SerializeField] MeshFilter trees;
        [SerializeField] int seed = 12345;
        [Tooltip("Scenery also fills this far beyond the play area, so the world has no visible edge.")]
        [SerializeField, Min(0f)] float overscan = 14f;
        [SerializeField] Kind[] kinds = { new() };

        [Header("Run-off (match TrackRenderer)")]
        [Tooltip("Extra clearance on the outside / inside of the tightest corners, where the sand run-off is.")]
        [SerializeField, Min(0f)] float cornerRunoff = 5f;
        [SerializeField, Min(0f)] float cornerRunoffInside = 2.5f;
        [SerializeField, Min(1f)] float runoffFullTurn = 70f;
        [SerializeField, Min(1)] int runoffSpan = 9;
        [Tooltip("Road turn (degrees over the span) that counts as a corner for tyre walls.")]
        [SerializeField] float cornerTurn = 35f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            var random = new System.Random(seed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            Rect bounds = path.Bounds();
            float margin = layout.boundaryMargin + overscan;
            var area = Rect.MinMaxRect(bounds.xMin - margin, bounds.yMin - margin, bounds.xMax + margin, bounds.yMax + margin);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var tints = new List<Color32>();
            var placed = new List<(Vector2 p, float r)>();
            var items = new List<(Vector2 p, Vector2 right, Rect uv, Color32 tint)>();
            var solids = new List<Vector2[]>();

            // A track can bring its own scenery (e.g. boats and buoys for a water track).
            var useKinds = layout.scenery is { Count: > 0 } ? layout.scenery.ToArray() : kinds;
            foreach (var kind in useKinds)
            {
                int done = 0;
                for (int attempt = 0; attempt < kind.count * 30 && done < kind.count; attempt++)
                {
                    var p = new Vector2(Range(area.xMin, area.xMax), Range(area.yMin, area.yMax));
                    if (kind.clustered && Mathf.PerlinNoise(p.x * 0.045f + seed % 100, p.y * 0.045f) < 0.45f) continue;
                    float size = Range(kind.sizeRange.x, kind.sizeRange.y);
                    var projection = path.Project(p);
                    float fromKerb = Mathf.Abs(projection.Lateral) - layout.OffRoadDistance - size * 0.5f;
                    float turn = path.TurnAtDistance(projection.Distance, runoffSpan);
                    bool outside = turn * projection.Lateral < 0f; // left turn: the right side is outside
                    if (kind.cornerOnly && Mathf.Abs(turn) < cornerTurn) continue;
                    if (!kind.cornerOnly)
                        fromKerb -= (outside ? cornerRunoff : cornerRunoffInside) * Mathf.SmoothStep(0f, 1f, Mathf.Abs(turn) / runoffFullTurn);
                    if (fromKerb < kind.clearance.x) continue;
                    if (kind.clearance.y > 0f && fromKerb > kind.clearance.y) continue;
                    if (!layout.waterWorld && track.WaterDistance(p) < size * 0.5f + 1.2f) continue;
                    if (InShortcutPath(p, size)) continue;
                    float r = size * 0.45f;
                    if (placed.Exists(q => (q.p - p).sqrMagnitude < (q.r + r) * (q.r + r))) continue;

                    placed.Add((p, r));
                    done++;
                    float angle = kind.alignToRoad
                        ? Mathf.Atan2(path.TangentAt(projection.Distance).y, path.TangentAt(projection.Distance).x)
                        : kind.squareAngle ? random.Next(4) * Mathf.PI * 0.5f + Range(-0.06f, 0.06f)
                        : Range(0f, Mathf.PI * 2f);
                    var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (size * 0.5f);
                    Color32 tint = kind.tints is { Length: > 0 } ? kind.tints[random.Next(kind.tints.Length)] : Color.white;
                    items.Add((p, right, kind.uv, tint));
                    if (kind.solid != Solid.None) solids.Add(Outline(kind, p, right, size));
                }
            }

            // Draw top-down from the back so overlapping crowns read naturally.
            items.Sort((a, b) => b.p.y.CompareTo(a.p.y));
            foreach (var (p, right, uv, tint) in items)
            {
                int first = vertices.Count;
                TrackMeshes.AddQuad(vertices, uvs, triangles, p, right, new Vector2(-right.y, right.x), Vector2.one);
                for (int k = first; k < uvs.Count; k++)
                    uvs[k] = new Vector2(uv.x + uvs[k].x * uv.width, uv.y + uvs[k].y * uv.height);
                for (int k = 0; k < 4; k++) tints.Add(tint);
            }

            Assign(trees, TrackMeshes.Quads("Scenery", vertices, uvs, triangles, tints));
            if (Application.isPlaying) BuildColliders(solids);
        }

        // The corridor a shortcut jump flies through stays clear, so a short landing is never inside a tree.
        bool InShortcutPath(Vector2 p, float size)
        {
            foreach (var ramp in track.Shortcuts)
            {
                Vector2 a = ramp.Centre - ramp.Direction * (ramp.Length * 0.5f + 3.5f), b = track.Path.PointAt(ramp.To);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                if (Vector2.Distance(p, a + ab * t) < ramp.Width * 0.5f + 2f + size * 0.5f) return true;
            }
            return false;
        }

        // A collider outline in world space: an octagon for round things, a rotated box for buildings.
        static Vector2[] Outline(Kind kind, Vector2 p, Vector2 right, float size)
        {
            Vector2 x = right.normalized, y = new(-x.y, x.x);
            if (kind.solid == Solid.Box)
            {
                Vector2 hx = x * (kind.solidSize.x * size), hy = y * (kind.solidSize.y * size);
                return new[] { p - hx - hy, p + hx - hy, p + hx + hy, p - hx + hy };
            }
            float r = kind.solidSize.x * size;
            var points = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                points[i] = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return points;
        }

        // All solid props share one static polygon collider (one path each) on a child object.
        void BuildColliders(List<Vector2[]> solids)
        {
            var child = transform.Find("Obstacles");
            if (child == null)
            {
                child = new GameObject("Obstacles").transform;
                child.SetParent(transform, false);
            }
            child.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            child.localScale = Vector3.one;
            var polygon = child.GetComponent<PolygonCollider2D>();
            if (polygon == null) polygon = child.gameObject.AddComponent<PolygonCollider2D>();
            polygon.pathCount = solids.Count;
            for (int i = 0; i < solids.Count; i++) polygon.SetPath(i, solids[i]);
        }
    }
}
