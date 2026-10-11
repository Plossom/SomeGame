using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.UI
{
    /// <summary>
    /// A thick line through points in its own rect (local UI units), each point with its own opacity so
    /// the line can fade out at its ends. Draws a track's outline on the map preview and the road ahead
    /// in the race.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TrackLine : MaskableGraphic
    {
        [SerializeField, Min(0.5f)] float width = 16f;
        [Tooltip("Joins the last point back to the first (a whole circuit).")]
        [SerializeField] bool closed;

        readonly List<Vector2> _points = new();
        readonly List<float> _alphas = new();

        public float Width
        {
            get => width;
            set { width = value; SetVerticesDirty(); }
        }

        public bool Closed
        {
            get => closed;
            set { closed = value; SetVerticesDirty(); }
        }

        /// <summary>Sets the line; <paramref name="alphas"/> (optional) fades each point.</summary>
        public void SetPoints(IReadOnlyList<Vector2> points, IReadOnlyList<float> alphas = null)
        {
            _points.Clear();
            _alphas.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                _points.Add(points[i]);
                _alphas.Add(alphas != null && i < alphas.Count ? alphas[i] : 1f);
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = _points.Count;
            if (n < 2) return;
            float half = width * 0.5f;
            int count = closed ? n + 1 : n;
            for (int k = 0; k < count; k++)
            {
                int i = k % n;
                // Direction from the neighbours, so the line bends smoothly at each point.
                Vector2 prev = closed || i > 0 ? _points[(i - 1 + n) % n] : _points[i];
                Vector2 next = closed || i < n - 1 ? _points[(i + 1) % n] : _points[i];
                Vector2 dir = next - prev;
                if (dir.sqrMagnitude < 1e-8f) dir = Vector2.up;
                dir.Normalize();
                Vector2 normal = new Vector2(-dir.y, dir.x) * half;
                Color32 c = color;
                c.a = (byte)(c.a * Mathf.Clamp01(_alphas[i]));
                vh.AddVert(_points[i] - normal, c, Vector4.zero);
                vh.AddVert(_points[i] + normal, c, Vector4.zero);
                if (k == 0) continue;
                int v = k * 2;
                vh.AddTriangle(v - 2, v - 1, v + 1);
                vh.AddTriangle(v - 2, v + 1, v);
            }
        }
    }
}
