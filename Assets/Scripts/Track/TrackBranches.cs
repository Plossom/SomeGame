using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws the other ways round (<see cref="TrackLayout.branches"/>): road branches with the road surface
    /// and white edge lines (drawn just under the main road, so the junctions blend into it), ice
    /// branches as a frozen strip with soft edges, and roof rows as snowy roofs (one house per texture
    /// repeat) standing above the ground with a shadow.
    /// </summary>
    public class TrackBranches : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter roads;
        [SerializeField] MeshFilter edges;
        [SerializeField] MeshFilter ice;
        [SerializeField] MeshFilter roofs;
        [SerializeField] MeshFilter roofShadows;
        [Tooltip("Patches of glare ice on the road (TrackLayout.icePatches), drawn over the road.")]
        [SerializeField] MeshFilter icePatches;
        [Tooltip("Length of one house along a roof row (one roof texture repeat).")]
        [SerializeField, Min(1f)] float houseLength = 12f;
        [SerializeField] Vector2 roofShadowOffset = new(0.9f, -1.2f);
        [Tooltip("Road surface of the meadow and of the snow biome.")]
        [SerializeField] Material meadowRoad;
        [SerializeField] Material snowRoad;
        [SerializeField, Min(0.1f)] float roadTile = 4f;
        [SerializeField, Min(0.01f)] float edgeLineWidth = 0.28f;
        [Tooltip("How far the ice reaches beyond the branch's width on each side (it fades out there).")]
        [SerializeField, Min(0f)] float iceMargin = 2.5f;
        [SerializeField, Min(0.1f)] float iceTile = 10f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            var roadData = new MeshData();
            var edgeData = new MeshData();
            var iceData = new MeshData();
            var roofData = new MeshData();
            var shadowData = new MeshData();
            foreach (var branch in track.Branches)
            {
                float hw = branch.HalfWidth;
                if (branch.Kind == TrackLayout.BranchKind.Roof)
                {
                    Strip(roofData, branch.Points, -hw - 0.6f, hw + 0.6f, 1f, houseLength);
                    var shifted = new List<Vector2>(branch.Points.Count);
                    foreach (var p in branch.Points) shifted.Add(p + roofShadowOffset);
                    Strip(shadowData, shifted, -hw - 0.6f, hw + 0.6f, 1f, 4f, new Color(0.05f, 0.1f, 0.2f, 0.35f));
                    continue;
                }
                if (branch.Kind == TrackLayout.BranchKind.Ice)
                {
                    Strip(iceData, branch.Points, -hw - iceMargin, hw + iceMargin, 1f, iceTile);
                    continue;
                }
                float outer = hw + layout.kerbWidth;
                Strip(roadData, branch.Points, -outer, outer, outer * 2f / roadTile, roadTile);
                Strip(edgeData, branch.Points, outer - edgeLineWidth, outer, 1f, 4f);
                Strip(edgeData, branch.Points, -outer, -outer + edgeLineWidth, 1f, 4f);
            }
            Assign(roads, roadData.ToMesh("BranchRoads"));
            Assign(edges, edgeData.ToMesh("BranchEdges"));
            Assign(ice, iceData.ToMesh("BranchIce"));
            Assign(roofs, roofData.ToMesh("Roofs"));
            Assign(roofShadows, shadowData.ToMesh("RoofShadows"));
            if (icePatches != null)
            {
                var patchData = new MeshData();
                if (layout.icePatches != null)
                    foreach (var patch in layout.icePatches)
                    {
                        float a = patch.angle * Mathf.Deg2Rad;
                        Vector2 along = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), across = new(-along.y, along.x);
                        // The texture's oval fills about 90 % of it, so the quad is a little bigger than the patch.
                        Vector2 hx = along * (patch.size.x * 0.56f), hy = across * (patch.size.y * 0.56f);
                        patchData.Quad(patch.position - hx - hy, patch.position + hx - hy, patch.position + hx + hy, patch.position - hx + hy);
                    }
                Assign(icePatches, patchData.ToMesh("IcePatches"));
            }

            var roadRenderer = roads != null ? roads.GetComponent<MeshRenderer>() : null;
            var material = layout.roadMaterial != null ? layout.roadMaterial : layout.biome == TrackLayout.Biome.Snow && snowRoad != null ? snowRoad : meadowRoad;
            if (roadRenderer != null && material != null) roadRenderer.sharedMaterial = material;
        }

        // A strip along an open polyline between two sideways offsets; u runs across (0..uMax), v along.
        static void Strip(MeshData data, IReadOnlyList<Vector2> points, float lateralA, float lateralB, float uMax, float vLength, Color? color = null)
        {
            int n = points.Count;
            if (n < 2) return;
            int first = data.Vertices.Count;
            float along = 0f;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) along += Vector2.Distance(points[i - 1], points[i]);
                Vector2 prev = points[Mathf.Max(0, i - 1)], next = points[Mathf.Min(n - 1, i + 1)];
                Vector2 dir = (next - prev).normalized, left = new(-dir.y, dir.x);
                data.Vertices.Add(points[i] + left * lateralA);
                data.Vertices.Add(points[i] + left * lateralB);
                data.Uvs.Add(new Vector2(0f, along / vLength));
                data.Uvs.Add(new Vector2(uMax, along / vLength));
                Color32 c = color ?? Color.white;
                data.Colors.Add(c);
                data.Colors.Add(c);
                if (i == 0) continue;
                int a = first + (i - 1) * 2;
                data.Triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
        }

        sealed class MeshData
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<Vector2> Uvs = new();
            public readonly List<int> Triangles = new();
            public readonly List<Color32> Colors = new();

            public Mesh ToMesh(string name) => TrackMeshes.Quads(name, Vertices, Uvs, Triangles, Colors);

            /// <summary>A quad through four corners with the whole texture on it (drawn from both sides).</summary>
            public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                int i = Vertices.Count;
                Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
                Uvs.Add(new Vector2(0f, 0f)); Uvs.Add(new Vector2(1f, 0f)); Uvs.Add(new Vector2(1f, 1f)); Uvs.Add(new Vector2(0f, 1f));
                for (int k = 0; k < 4; k++) Colors.Add(new Color32(255, 255, 255, 255));
                Triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
            }
        }
    }
}
