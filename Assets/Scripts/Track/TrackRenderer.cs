using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>Draws grass, road, kerbs and start line from the track data, and fences the play area.</summary>
    public class TrackRenderer : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter grass;
        [SerializeField] MeshFilter road;
        [SerializeField] MeshFilter kerbs;
        [SerializeField] MeshFilter startLine;
        [Tooltip("Invisible wall around the grass so cars cannot leave the map. Set up in Play mode only.")]
        [SerializeField] EdgeCollider2D boundary;

        [Header("Texture tiling (world units per texture repeat)")]
        [SerializeField, Min(0.1f)] float roadTile = 4f;
        [SerializeField, Min(0.1f)] float grassTile = 4f;
        [Tooltip("Approximate length of one red + white kerb block pair; adjusted so the loop closes cleanly.")]
        [SerializeField, Min(0.1f)] float kerbCycle = 2.4f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            float hw = layout.HalfWidth, outer = hw + layout.kerbWidth;

            Assign(road, TrackMeshes.Strip(path, -hw - 0.05f, hw + 0.05f, layout.roadWidth / roadTile, roadTile, "Road"));
            Assign(kerbs, BuildKerbs(path, hw, outer));
            Assign(startLine, BuildStartLine(path, hw));

            Rect area = Expand(path.Bounds(), layout.boundaryMargin);
            Assign(grass, BuildGrass(area));
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

        Mesh BuildKerbs(TrackPath path, float inner, float outer)
        {
            float cycle = path.Length / Mathf.Max(1, Mathf.Round(path.Length / kerbCycle));
            var left = TrackMeshes.Strip(path, inner, outer, 1f, cycle, "KerbLeft");
            var right = TrackMeshes.Strip(path, -inner, -outer, 1f, cycle, "KerbRight");
            var combined = new Mesh { name = "Kerbs", hideFlags = HideFlags.DontSave };
            combined.CombineMeshes(new[]
            {
                new CombineInstance { mesh = left, transform = Matrix4x4.identity },
                new CombineInstance { mesh = right, transform = Matrix4x4.identity },
            });
            DestroyTemp(left);
            DestroyTemp(right);
            return combined;
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

        Mesh BuildGrass(Rect area)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            TrackMeshes.AddQuad(vertices, uvs, triangles, area.center,
                new Vector2(area.width * 0.5f, 0f), new Vector2(0f, area.height * 0.5f),
                new Vector2(area.width / grassTile, area.height / grassTile));
            return TrackMeshes.Quads("Grass", vertices, uvs, triangles);
        }

        static Rect Expand(Rect r, float margin) =>
            Rect.MinMaxRect(r.xMin - margin, r.yMin - margin, r.xMax + margin, r.yMax + margin);

        static void DestroyTemp(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
