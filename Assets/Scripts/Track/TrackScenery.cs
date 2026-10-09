using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Scatters scenery (trees, or neon light pylons) on the ground, keeping clear of the road.
    /// Each item gets one of <see cref="colors"/>. Pure scenery, no colliders.
    /// </summary>
    public class TrackScenery : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter trees;
        [SerializeField, Min(0)] int treeCount = 40;
        [SerializeField] int seed = 12345;
        [Tooltip("Minimum gap between a tree's centre and the kerb edge.")]
        [SerializeField, Min(0f)] float roadClearance = 3f;
        [SerializeField] Vector2 sizeRange = new(2.2f, 3.6f);
        [Tooltip("Each item picks one of these tints at random. Empty = untinted.")]
        [SerializeField] Color[] colors = { Color.white };

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            var random = new System.Random(seed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            Rect bounds = path.Bounds();
            float margin = layout.boundaryMargin - 2f;
            var area = Rect.MinMaxRect(bounds.xMin - margin, bounds.yMin - margin, bounds.xMax + margin, bounds.yMax + margin);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var placed = new List<Vector2>();
            var tints = new List<Color32>();

            for (int attempt = 0; attempt < treeCount * 20 && placed.Count < treeCount; attempt++)
            {
                var p = new Vector2(Range(area.xMin, area.xMax), Range(area.yMin, area.yMax));
                float size = Range(sizeRange.x, sizeRange.y);
                if (Mathf.Abs(path.Project(p).Lateral) < layout.OffRoadDistance + roadClearance + size * 0.5f) continue;
                if (placed.Exists(q => (q - p).sqrMagnitude < size * size)) continue;

                placed.Add(p);
                float angle = Range(0f, Mathf.PI * 2f);
                var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (size * 0.5f);
                TrackMeshes.AddQuad(vertices, uvs, triangles, p, right, new Vector2(-right.y, right.x), Vector2.one);
                Color32 tint = colors is { Length: > 0 } ? colors[random.Next(colors.Length)] : Color.white;
                for (int k = 0; k < 4; k++) tints.Add(tint);
            }

            Assign(trees, TrackMeshes.Quads("Scenery", vertices, uvs, triangles, tints));
        }
    }
}
