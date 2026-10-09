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
