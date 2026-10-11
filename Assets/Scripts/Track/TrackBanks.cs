using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws the banked walls (<see cref="TrackLayout.banks"/>) on the outside of corners: a strip from the
    /// kerb outwards, shaded from its base to a snowy crest, with chevrons pointing along it. It narrows in
    /// at both ends so the wall rises out of the ground and sinks back.
    /// </summary>
    public class TrackBanks : TrackDerivedBehaviour
    {
        [SerializeField] MeshFilter walls;
        [Tooltip("Length of one chevron along the wall (one texture repeat).")]
        [SerializeField, Min(0.5f)] float tile = 4f;
        [Tooltip("Metres at each end over which the wall rises to its full width.")]
        [SerializeField, Min(0f)] float taper = 6f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            var meshes = new List<CombineInstance>();
            foreach (var bank in layout.banks)
            {
                float span = path.WrapDistance(bank.to - bank.from);
                float Width(float d)
                {
                    float into = path.WrapDistance(d - bank.from), left = span - into;
                    return bank.width * Mathf.SmoothStep(0f, 1f, Mathf.Min(into, left) / Mathf.Max(0.01f, taper));
                }
                bool Keep(float d) => path.WrapDistance(d - bank.from) <= span;
                float sign = bank.side >= 0f ? 1f : -1f;
                var mesh = TrackMeshes.Strip(path, d => sign * (track.OffRoadAt(d) - 0.05f), d => sign * (track.OffRoadAt(d) + Width(d)),
                    1f, tile, "Bank", Color.white, Keep);
                meshes.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.identity });
            }
            var combined = new Mesh { name = "Banks", hideFlags = HideFlags.DontSave };
            combined.CombineMeshes(meshes.ToArray());
            foreach (var m in meshes)
            {
                if (Application.isPlaying) Destroy(m.mesh); else DestroyImmediate(m.mesh);
            }
            Assign(walls, combined);
        }
    }
}
