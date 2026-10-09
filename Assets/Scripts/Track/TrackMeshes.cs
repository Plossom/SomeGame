using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>Builds the procedural meshes for road, kerbs, grass, start line and scenery.</summary>
    public static class TrackMeshes
    {
        /// <summary>
        /// A strip following the closed centre line between two lateral offsets.
        /// U runs 0..uMax across the strip, V is distance along the lap divided by vLength.
        /// </summary>
        public static Mesh Strip(TrackPath path, float lateralA, float lateralB, float uMax, float vLength, string name) =>
            Strip(path, lateralA, lateralB, uMax, vLength, name, Color.white);

        /// <summary>Strip tinted with a vertex colour (unlit sprite materials multiply it in).</summary>
        public static Mesh Strip(TrackPath path, float lateralA, float lateralB, float uMax, float vLength, string name, Color color)
        {
            int n = path.Count;
            var vertices = new Vector3[(n + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                Vector2 p = path[i], normal = path.SampleNormal(i);
                float v = (i == n ? path.Length : path.DistanceAt(i)) / vLength;
                vertices[i * 2] = p + normal * lateralA;
                vertices[i * 2 + 1] = p + normal * lateralB;
                uvs[i * 2] = new Vector2(0f, v);
                uvs[i * 2 + 1] = new Vector2(uMax, v);
                if (i == n) break;
                int t = i * 6, a = i * 2;
                triangles[t] = a; triangles[t + 1] = a + 2; triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1; triangles[t + 4] = a + 2; triangles[t + 5] = a + 3;
            }
            return Create(name, vertices, uvs, triangles, color);
        }

        /// <summary>
        /// Like <see cref="Strip(TrackPath, float, float, float, float, string, Color)"/>, but only for the
        /// stretches where <paramref name="keep"/> is true for the distance (e.g. leaving out gaps in the road).
        /// </summary>
        public static Mesh Strip(TrackPath path, float lateralA, float lateralB, float uMax, float vLength, string name, Color color,
            System.Func<float, bool> keep, Vector2 offset = default) =>
            Strip(path, _ => lateralA, _ => lateralB, uMax, vLength, name, color, keep, offset);

        /// <summary>A strip whose two edges may change their sideways offset along the lap (road width changes).</summary>
        public static Mesh Strip(TrackPath path, System.Func<float, float> lateralA, System.Func<float, float> lateralB, float uMax, float vLength,
            string name, Color color, System.Func<float, bool> keep, Vector2 offset = default)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i < path.Count; i++)
            {
                float d0 = path.DistanceAt(i), d1 = d0 + path.SegmentLength(i);
                if (!keep((d0 + d1) * 0.5f)) continue;
                int a = vertices.Count;
                for (int k = 0; k <= 1; k++)
                {
                    Vector2 p = path[i + k], normal = path.SampleNormal(i + k);
                    float d = k == 0 ? d0 : d1;
                    float v = d / vLength;
                    vertices.Add(p + normal * lateralA(d) + offset);
                    vertices.Add(p + normal * lateralB(d) + offset);
                    uvs.Add(new Vector2(0f, v));
                    uvs.Add(new Vector2(uMax, v));
                }
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            var colors = new List<Color32>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++) colors.Add(color);
            return Quads(name, vertices, uvs, triangles, colors);
        }

        /// <summary>A quad defined by its centre, half extents along two axes, and UV range.</summary>
        public static void AddQuad(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles,
            Vector2 centre, Vector2 halfRight, Vector2 halfUp, Vector2 uvMax)
        {
            int start = vertices.Count;
            vertices.Add(centre - halfRight - halfUp);
            vertices.Add(centre + halfRight - halfUp);
            vertices.Add(centre - halfRight + halfUp);
            vertices.Add(centre + halfRight + halfUp);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(uvMax.x, 0f));
            uvs.Add(new Vector2(0f, uvMax.y));
            uvs.Add(uvMax);
            triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 });
        }

        public static Mesh Quads(string name, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles,
            List<Color32> colors = null)
        {
            var mesh = Create(name, vertices.ToArray(), uvs.ToArray(), triangles.ToArray(), Color.white);
            if (colors != null && colors.Count == vertices.Count) mesh.colors32 = colors.ToArray();
            return mesh;
        }

        static Mesh Create(string name, Vector3[] vertices, Vector2[] uvs, int[] triangles, Color color)
        {
            var colors = new Color32[vertices.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = color;

            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (vertices.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
