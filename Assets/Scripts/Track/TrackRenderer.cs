using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws grass, road, white edge lines, red-and-white kerbs and sandy run-off in the corners, and the
    /// start line from the track data, and fences the play area.
    /// </summary>
    public class TrackRenderer : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter grass;
        [SerializeField] MeshFilter road;
        [SerializeField] MeshFilter kerbs;
        [SerializeField] MeshFilter startLine;
        [Tooltip("White line along both road edges.")]
        [SerializeField] MeshFilter edgeLines;
        [SerializeField, Min(0.01f)] float edgeLineWidth = 0.28f;
        [Tooltip("Optional sandy run-off beyond the kerbs in the corners (wider on the outside).")]
        [SerializeField] MeshFilter runoff;
        [Tooltip("Optional dashed centre line.")]
        [SerializeField] MeshFilter centerLine;
        [Tooltip("Invisible wall around the grass so cars cannot leave the map. Set up in Play mode only.")]
        [SerializeField] EdgeCollider2D boundary;

        [Header("Texture tiling (world units per texture repeat)")]
        [SerializeField, Min(0.1f)] float roadTile = 4f;
        [SerializeField, Min(0.1f)] float grassTile = 4f;
        [Tooltip("Approximate length of one edge texture repeat; adjusted so the loop closes cleanly.")]
        [SerializeField, Min(0.1f)] float kerbCycle = 2.4f;
        [Tooltip("Vertex colour of the edge strip on the left / right of the driving direction.")]
        [SerializeField] Color leftEdgeColor = Color.white;
        [SerializeField] Color rightEdgeColor = Color.white;
        [Tooltip("Length of one centre-line dash plus gap; adjusted so the loop closes cleanly.")]
        [SerializeField, Min(0.1f)] float centerLineCycle = 3f;
        [SerializeField, Min(0.01f)] float centerLineWidth = 0.22f;
        [Tooltip("Grass is drawn this far beyond the invisible wall, so the world has no visible edge.")]
        [SerializeField, Min(0f)] float grassOverscan = 30f;
        [Header("Corners")]
        [Tooltip("Road turn (degrees over the sampled span) from which a stretch counts as a corner and gets kerbs.")]
        [SerializeField, Min(1f)] float kerbTurn = 22f;
        [Tooltip("Run-off width on the outside / inside of the tightest corners (none on straights).")]
        [SerializeField, Min(0f)] float runoffCorner = 5f;
        [SerializeField, Min(0f)] float runoffInside = 2.5f;
        [Tooltip("Road turn (degrees over the sampled span) that gets the full corner width.")]
        [SerializeField, Min(1f)] float runoffFullTurn = 70f;
        [SerializeField, Min(1)] int runoffSpan = 9;
        [SerializeField, Min(0.1f)] float runoffTile = 4f;

        [Tooltip("Grass tint range for the large meadow patches.")]
        [SerializeField] Color meadowDark = new(0.8f, 0.88f, 0.8f);
        [SerializeField] Color meadowLight = Color.white;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            float hw = layout.HalfWidth, outer = hw + layout.kerbWidth;

            Assign(road, TrackMeshes.Strip(path, -outer - 0.02f, outer + 0.02f, outer * 2f / roadTile, roadTile, "Road"));
            Assign(kerbs, BuildKerbs(path, hw, outer));
            if (edgeLines != null) Assign(edgeLines, BuildEdgeLines(path, outer));
            Assign(startLine, BuildStartLine(path, hw));
            if (runoff != null) Assign(runoff, BuildRunoff(path, outer));
            if (centerLine != null)
            {
                float cycle = path.Length / Mathf.Max(1, Mathf.Round(path.Length / centerLineCycle));
                Assign(centerLine, TrackMeshes.Strip(path, -centerLineWidth * 0.5f, centerLineWidth * 0.5f, 1f, cycle, "CenterLine"));
            }

            Rect area = Expand(path.Bounds(), layout.boundaryMargin);
            Assign(grass, BuildGrass(Expand(area, grassOverscan)));
            if (boundary != null && Application.isPlaying)
            {
                Rect wall = Expand(area, -1f);
                boundary.points = new[]
                {
                    new Vector2(wall.xMin, wall.yMin), new Vector2(wall.xMax, wall.yMin),
                    new Vector2(wall.xMax, wall.yMax), new Vector2(wall.xMin, wall.yMax),
                    new Vector2(wall.xMin, wall.yMin),
                };
            }
        }

        // Red-and-white kerbs on both sides, only where the road turns enough.
        Mesh BuildKerbs(TrackPath path, float inner, float outer)
        {
            int n = path.Count;
            float cycle = path.Length / Mathf.Max(1, Mathf.Round(path.Length / kerbCycle));
            var corner = new bool[n];
            for (int i = 0; i < n; i++) corner[i] = Mathf.Abs(path.TurnAt(i, runoffSpan)) >= kerbTurn;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1f : -1f;
                for (int i = 0; i < n; i++)
                {
                    if (!corner[i]) continue;
                    int a = vertices.Count;
                    for (int k = 0; k <= 1; k++)
                    {
                        Vector2 p = path[i + k], normal = path.SampleNormal(i + k) * sign;
                        float d = path.DistanceAt(i);
                        if (k == 1) d += path.SegmentLength(i);
                        vertices.Add(p + normal * inner);
                        vertices.Add(p + normal * outer);
                        uvs.Add(new Vector2(0f, d / cycle));
                        uvs.Add(new Vector2(1f, d / cycle));
                    }
                    if (side == 0) triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                    else triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            return TrackMeshes.Quads("Kerbs", vertices, uvs, triangles);
        }

        Mesh BuildEdgeLines(TrackPath path, float outer)
        {
            float inner = outer - edgeLineWidth;
            var left = TrackMeshes.Strip(path, inner, outer, 1f, 4f, "EdgeLeft");
            var right = TrackMeshes.Strip(path, -inner, -outer, 1f, 4f, "EdgeRight");
            var combined = new Mesh { name = "EdgeLines", hideFlags = HideFlags.DontSave };
            combined.CombineMeshes(new[]
            {
                new CombineInstance { mesh = left, transform = Matrix4x4.identity },
                new CombineInstance { mesh = right, transform = Matrix4x4.identity },
            });
            DestroyTemp(left);
            DestroyTemp(right);
            return combined;
        }

        // Sand strips from the kerb outwards in the corners: wide on the outside (where a car that runs wide
        // ends up), narrower on the inside, none on straights. U runs 0 at the kerb to 1 at the outer edge.
        Mesh BuildRunoff(TrackPath path, float kerbEdge)
        {
            int n = path.Count;
            var widths = new float[2, n];
            for (int i = 0; i < n; i++)
            {
                float turn = path.TurnAt(i, runoffSpan);
                float extra = runoffCorner * Mathf.SmoothStep(0f, 1f, Mathf.Abs(turn) / runoffFullTurn);
                float inside = extra * runoffInside / Mathf.Max(0.01f, runoffCorner);
                widths[0, i] = turn < 0f ? extra : inside; // left side is outside in a right turn
                widths[1, i] = turn > 0f ? extra : inside;
            }
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1f : -1f;
                int start = vertices.Count;
                for (int i = 0; i <= n; i++)
                {
                    // Smooth the width along the road so the sand flows in and out of corners.
                    float w = 0f;
                    for (int k = -4; k <= 4; k++) w += widths[side, path.Wrap(i + k)];
                    w /= 9f;
                    if (w < 0.05f) w = 0f;
                    Vector2 p = path[i], normal = path.SampleNormal(i) * sign;
                    float v = (i == n ? path.Length : path.DistanceAt(i)) / runoffTile;
                    vertices.Add(p + normal * (kerbEdge - 0.05f));
                    vertices.Add(p + normal * (kerbEdge + w));
                    uvs.Add(new Vector2(0f, v));
                    uvs.Add(new Vector2(1f, v));
                    if (i == n) break;
                    int a = start + i * 2;
                    if (side == 0) triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                    else triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            return TrackMeshes.Quads("Runoff", vertices, uvs, triangles);
        }

        Mesh BuildStartLine(TrackPath path, float hw)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector2 forward = path.TangentAt(0f), normal = path.NormalAt(0f);
            TrackMeshes.AddQuad(vertices, uvs, triangles, path.PointAt(0f), normal * hw, forward * 0.5f,
                new Vector2(hw, 1f));
            return TrackMeshes.Quads("StartLine", vertices, uvs, triangles);
        }

        // A grid so the grass can carry large, soft light and dark meadow patches in its vertex colours.
        Mesh BuildGrass(Rect area)
        {
            const float cell = 3f;
            int nx = Mathf.CeilToInt(area.width / cell), ny = Mathf.CeilToInt(area.height / cell);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++)
                {
                    var p = new Vector2(area.xMin + i * cell, area.yMin + j * cell);
                    vertices.Add(p);
                    uvs.Add(new Vector2(p.x / grassTile, p.y / grassTile));
                    float t = Mathf.PerlinNoise(p.x * 0.035f + 100f, p.y * 0.035f) * 0.7f + Mathf.PerlinNoise(p.x * 0.11f, p.y * 0.11f + 50f) * 0.3f;
                    colors.Add(Color.Lerp(meadowDark, meadowLight, Mathf.SmoothStep(0f, 1f, t)));
                }
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            return TrackMeshes.Quads("Grass", vertices, uvs, triangles, colors);
        }

        static Rect Expand(Rect r, float margin) =>
            Rect.MinMaxRect(r.xMin - margin, r.yMin - margin, r.xMax + margin, r.yMax + margin);

        static void DestroyTemp(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
