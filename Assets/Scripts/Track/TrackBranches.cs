using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws the other ways round (<see cref="TrackLayout.branches"/>): road branches with the road surface
    /// and white edge lines (drawn just under the main road, so the junctions blend into it), and ice
    /// branches as a frozen strip with soft edges.
    /// </summary>
    public class TrackBranches : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter roads;
        [SerializeField] MeshFilter edges;
        [SerializeField] MeshFilter ice;
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
            foreach (var branch in track.Branches)
            {
                float hw = branch.HalfWidth;
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

            var roadRenderer = roads != null ? roads.GetComponent<MeshRenderer>() : null;
            var material = layout.biome == TrackLayout.Biome.Snow && snowRoad != null ? snowRoad : meadowRoad;
            if (roadRenderer != null && material != null) roadRenderer.sharedMaterial = material;
        }

        // A strip along an open polyline between two sideways offsets; u runs across (0..uMax), v along.
        static void Strip(MeshData data, IReadOnlyList<Vector2> points, float lateralA, float lateralB, float uMax, float vLength)
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

            public Mesh ToMesh(string name) => TrackMeshes.Quads(name, Vertices, Uvs, Triangles);
        }
    }
}
