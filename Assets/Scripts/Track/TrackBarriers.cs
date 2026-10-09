using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Solid barrier walls on both sides of the track, a grass run-off outside the kerbs, generated
    /// from the track data. On the inside of tight corners the wall pulls in so it never folds over.
    /// </summary>
    public class TrackBarriers : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter walls;
        [Tooltip("Collider along the inner face of the left wall. Points are set in Play mode only.")]
        [SerializeField] EdgeCollider2D leftCollider;
        [SerializeField] EdgeCollider2D rightCollider;
        [Tooltip("Length of one texture repeat along the wall (world units).")]
        [SerializeField, Min(0.1f)] float textureLength = 1.2f;
        [Tooltip("Closest the wall may get to the centre line in tight corners, beyond the kerb.")]
        [SerializeField, Min(0f)] float minimumRunOff = 1f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            float minimum = layout.OffRoadDistance + minimumRunOff;
            float[] left = path.SideOffsets(1, layout.BarrierDistance, minimum);
            float[] right = path.SideOffsets(-1, layout.BarrierDistance, minimum);
            float width = layout.barrierWidth;
            float repeat = path.Length / Mathf.Max(1f, Mathf.Round(path.Length / textureLength));

            var leftMesh = TrackMeshes.Strip(path, i => left[i], i => left[i] + width, 1f, repeat, "BarrierLeft");
            var rightMesh = TrackMeshes.Strip(path, i => -right[i], i => -(right[i] + width), 1f, repeat, "BarrierRight");
            var combined = new Mesh { name = "Barriers", hideFlags = HideFlags.DontSave };
            combined.CombineMeshes(new[]
            {
                new CombineInstance { mesh = leftMesh, transform = Matrix4x4.identity },
                new CombineInstance { mesh = rightMesh, transform = Matrix4x4.identity },
            });
            DestroyTemp(leftMesh);
            DestroyTemp(rightMesh);
            Assign(walls, combined);

            if (!Application.isPlaying) return;
            SetCollider(leftCollider, path, left, 1);
            SetCollider(rightCollider, path, right, -1);
        }

        static void SetCollider(EdgeCollider2D edge, TrackPath path, float[] offsets, int side)
        {
            if (edge == null) return;
            var points = new Vector2[path.Count + 1];
            for (int i = 0; i <= path.Count; i++)
            {
                int s = path.Wrap(i);
                points[i] = path[s] + path.SampleNormal(s) * (side * offsets[s]);
            }
            edge.points = points;
        }

        static void DestroyTemp(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
