using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Draws the layout's ground zones (<see cref="TrackLayout.grounds"/>): wide strips of another ground
    /// (mud, sand, red rock, pebbles, wheat) along stretches of the road, over the grass and under the run-off,
    /// road and scenery. Their outer edges wander a little and fade softly, and they fade in and out at
    /// their ends, so each part of a long journey has its own look.
    /// </summary>
    public class TrackGround : TrackDerivedBehaviour
    {
        [Tooltip("One material per ground kind, in TrackLayout.GroundKind order (tiled textures).")]
        [SerializeField] Material[] materials;
        [SerializeField, Min(0.5f)] float tile = 8f;
        [SerializeField] int sortingOrder = -90;
        [SerializeField, Min(0.25f)] float step = 1.5f;

        readonly List<GameObject> _layers = new();

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _layers) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _layers.Clear();
            if (layout.grounds == null || materials == null) return;
            for (int i = 0; i < layout.grounds.Count; i++)
            {
                var zone = layout.grounds[i];
                int k = (int)zone.kind;
                if (k >= materials.Length || materials[k] == null) continue;
                var go = new GameObject($"Ground{i}_{zone.kind}", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = materials[k];
                renderer.sortingOrder = sortingOrder + i % 5; // later zones lie over earlier ones where they meet
                Assign(go.GetComponent<MeshFilter>(), Strip(path, zone, i));
                _layers.Add(go);
            }
        }

        // Across the strip: the edge, a soft band, the full core, a soft band, the other edge.
        static readonly float[] Across = { -1f, -0.72f, -0.45f, 0f, 0.45f, 0.72f, 1f };
        static readonly float[] Alpha = { 0f, 0.75f, 1f, 1f, 1f, 0.75f, 0f };

        Mesh Strip(TrackPath path, TrackLayout.GroundZone zone, int seed)
        {
            float length = path.Length;
            float span = Mathf.Repeat(zone.to - zone.from, length);
            if (span <= 0f) span = length;
            int rows = Mathf.Max(2, Mathf.CeilToInt(span / step) + 1);
            var vertices = new List<Vector3>(rows * Across.Length);
            var uvs = new List<Vector2>(rows * Across.Length);
            var colors = new List<Color32>(rows * Across.Length);
            var triangles = new List<int>();
            for (int r = 0; r < rows; r++)
            {
                float t = r / (rows - 1f), along = t * span, d = zone.from + along;
                Vector2 centre = path.PointAt(d), normal = path.NormalAt(d);
                float ends = zone.fade > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Min(along, span - along) / zone.fade) : 1f;
                for (int c = 0; c < Across.Length; c++)
                {
                    float side = Across[c];
                    // The edge wanders in and out, differently on each side.
                    float wobble = 0.78f + 0.22f * Mathf.PerlinNoise(d * 0.045f + seed * 13.1f, side > 0 ? 3.7f : 9.2f);
                    Vector2 p = centre + normal * (side * zone.reach * (Mathf.Abs(side) > 0.5f ? wobble : 1f));
                    vertices.Add(p);
                    uvs.Add(p / tile);
                    colors.Add(new Color(1f, 1f, 1f, Alpha[c] * ends));
                }
                if (r == 0) continue;
                int a = (r - 1) * Across.Length, b = r * Across.Length;
                for (int c = 0; c < Across.Length - 1; c++)
                {
                    triangles.Add(a + c); triangles.Add(b + c); triangles.Add(b + c + 1);
                    triangles.Add(a + c); triangles.Add(b + c + 1); triangles.Add(a + c + 1);
                }
            }
            var mesh = new Mesh { name = "Ground" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
