using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SomeGame.City
{
    /// <summary>Collects quads, boxes and prisms with vertex colours into one mesh per material.</summary>
    public sealed class CityMeshBatch
    {
        readonly List<Vector3> _vertices = new();
        readonly List<Vector2> _uvs = new();
        readonly List<Color32> _colors = new();
        readonly List<int> _triangles = new();

        public int VertexCount => _vertices.Count;

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color,
            Vector2 uvA = default, Vector2 uvB = default, Vector2 uvC = default, Vector2 uvD = default)
        {
            int i = _vertices.Count;
            _vertices.Add(a); _vertices.Add(b); _vertices.Add(c); _vertices.Add(d);
            if (uvA == default && uvB == default && uvC == default && uvD == default)
            {
                uvA = new Vector2(0, 0); uvB = new Vector2(1, 0); uvC = new Vector2(1, 1); uvD = new Vector2(0, 1);
            }
            _uvs.Add(uvA); _uvs.Add(uvB); _uvs.Add(uvC); _uvs.Add(uvD);
            // Colours are authored as seen on screen; the project renders in linear space.
            Color32 c32 = color.linear;
            _colors.Add(c32); _colors.Add(c32); _colors.Add(c32); _colors.Add(c32);
            _triangles.Add(i); _triangles.Add(i + 2); _triangles.Add(i + 1);
            _triangles.Add(i); _triangles.Add(i + 3); _triangles.Add(i + 2);
        }

        /// <summary>A flat rectangle on the plane y, from (x0,z0) to (x1,z1).</summary>
        public void Flat(float x0, float z0, float x1, float z1, float y, Color color, Vector2 uvScale = default)
        {
            Vector2 s = uvScale == default ? Vector2.one : uvScale;
            Quad(new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), color,
                Vector2.zero, new Vector2(s.x, 0), s, new Vector2(0, s.y));
        }

        /// <summary>A thin flat strip along a line segment on the ground (markings, neon edges).</summary>
        public void Strip(Vector3 from, Vector3 to, float width, Color color)
        {
            Vector3 dir = (to - from).normalized;
            Vector3 side = new Vector3(-dir.z, 0f, dir.x) * (width * 0.5f);
            Quad(from - side, from + side, to + side, to - side, color);
        }

        /// <summary>Axis-aligned box: four walls (UV in world metres / tile) and a top.</summary>
        public void Box(Vector3 min, Vector3 max, Color wallColor, Color topColor, float uvTile = 12f, float uOffset = 0f, bool top = true)
        {
            float w = max.x - min.x, d = max.z - min.z, h = max.y - min.y;
            float u = uOffset;
            Wall(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), h, wallColor, ref u, uvTile); // south
            Wall(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), h, wallColor, ref u, uvTile); // east
            Wall(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z), h, wallColor, ref u, uvTile); // north
            Wall(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z), h, wallColor, ref u, uvTile); // west
            if (top) Flat(min.x, min.z, max.x, max.z, max.y, topColor, new Vector2(w / uvTile, d / uvTile));
        }

        void Wall(Vector3 a, Vector3 b, float height, Color color, ref float u, float tile)
        {
            float len = Vector3.Distance(a, b) / tile, v = height / tile;
            Quad(a, b, b + Vector3.up * height, a + Vector3.up * height, color,
                new Vector2(u, 0), new Vector2(u + len, 0), new Vector2(u + len, v), new Vector2(u, v));
            u += len;
        }

        /// <summary>
        /// Prism / cone around a vertical axis: <paramref name="sides"/> faces, bottom radius r0 at y0,
        /// top radius r1 at y1 (0 = pointed). Each face gets its own colour from <paramref name="faceColor"/>.
        /// </summary>
        public void Prism(Vector3 baseCenter, float r0, float r1, float height, int sides, System.Func<int, float, Color> faceColor,
            Color topColor, float rotation = 0f)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = rotation + i * Mathf.PI * 2f / sides, a1 = rotation + (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = new(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = baseCenter + d0 * r0, b1 = baseCenter + d1 * r0;
                Vector3 t0 = baseCenter + Vector3.up * height + d0 * r1, t1 = baseCenter + Vector3.up * height + d1 * r1;
                float facing = Vector3.Dot((d0 + d1).normalized, new Vector3(-0.4f, 0f, -0.9f).normalized); // toward the camera/light
                Quad(b0, b1, t1, t0, faceColor(i, facing));
                if (r1 > 0.001f)
                    Quad(baseCenter + Vector3.up * height, t0, t1, baseCenter + Vector3.up * height, topColor);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(_vertices);
            mesh.SetUVs(0, _uvs);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
