using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// A <see cref="TrackLayout.Branch"/> turned into a drivable line: a smooth open curve that leaves the
    /// main road at <see cref="From"/> along its direction, runs through the branch's points and rejoins at
    /// <see cref="To"/>. Positions on it map to lap distances between From and To, so race progress runs
    /// on smoothly whichever way a car goes.
    /// </summary>
    public sealed class BranchRoute
    {
        public TrackLayout.BranchKind Kind { get; }
        public float From { get; }
        public float To { get; }
        public float HalfWidth { get; }
        /// <summary>Lap distance covered from <see cref="From"/> to <see cref="To"/> along the main road.</summary>
        public float Span { get; }
        public float Length { get; }
        public IReadOnlyList<Vector2> Points => _points;

        readonly List<Vector2> _points = new();
        readonly List<float> _distances = new();
        readonly TrackPath _main;

        public BranchRoute(TrackLayout.Branch branch, TrackPath main, float spacing = 1f)
        {
            _main = main;
            Kind = branch.kind;
            From = main.WrapDistance(branch.from);
            To = main.WrapDistance(branch.to);
            HalfWidth = branch.width * 0.5f;
            Span = main.WrapDistance(To - From);

            // Leave and rejoin along the main road's direction: the end tangents come from extra points
            // a little before the start and after the end. A roof is only the row of roofs itself: cars
            // get up by a jump pad and fly back down to the road at its end.
            var control = new List<Vector2>();
            bool roof = Kind == TrackLayout.BranchKind.Roof;
            if (!roof) control.Add(main.PointAt(From));
            if (branch.points != null) control.AddRange(branch.points);
            if (!roof) control.Add(main.PointAt(To));
            if (control.Count < 2) control.Add(control.Count == 1 ? control[0] + Vector2.up : main.PointAt(To));
            Vector2 before = roof ? control[0] * 2f - control[1] : control[0] - main.TangentAt(From) * 12f;
            Vector2 after = roof ? control[^1] * 2f - control[^2] : control[^1] + main.TangentAt(To) * 12f;
            for (int i = 0; i < control.Count - 1; i++)
            {
                Vector2 p0 = i == 0 ? before : control[i - 1], p1 = control[i], p2 = control[i + 1];
                Vector2 p3 = i + 2 < control.Count ? control[i + 2] : after;
                int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / spacing));
                for (int s = 0; s < steps; s++) Add(CatmullRom(p0, p1, p2, p3, s / (float)steps));
            }
            Add(control[^1]);
            Length = _distances[^1];
        }

        void Add(Vector2 p)
        {
            _distances.Add(_points.Count == 0 ? 0f : _distances[^1] + Vector2.Distance(_points[^1], p));
            _points.Add(p);
        }

        /// <summary>Lap distance for a distance along the branch.</summary>
        public float MainDistance(float along) => _main.WrapDistance(From + Span * Mathf.Clamp01(along / Mathf.Max(0.01f, Length)));

        public Vector2 PointAt(float along)
        {
            int i = Segment(along);
            float t = Mathf.InverseLerp(_distances[i], _distances[i + 1], along);
            return Vector2.Lerp(_points[i], _points[i + 1], t);
        }

        public Vector2 TangentAt(float along)
        {
            int i = Segment(along);
            return (_points[i + 1] - _points[i]).normalized;
        }

        int Segment(float along)
        {
            along = Mathf.Clamp(along, 0f, Length);
            int i = _distances.BinarySearch(along);
            if (i < 0) i = ~i - 1;
            return Mathf.Clamp(i, 0, _points.Count - 2);
        }

        /// <summary>Nearest point on the branch: distance along it and signed sideways offset (positive = left).</summary>
        public void Project(Vector2 position, out float along, out float lateral)
        {
            along = 0f;
            lateral = float.MaxValue;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _points.Count - 1; i++)
            {
                Vector2 a = _points[i], ab = _points[i + 1] - a;
                float lenSqr = ab.sqrMagnitude;
                float t = lenSqr > 0f ? Mathf.Clamp01(Vector2.Dot(position - a, ab) / lenSqr) : 0f;
                Vector2 closest = a + ab * t;
                float sqr = (position - closest).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                along = _distances[i] + Mathf.Sqrt(lenSqr) * t;
                Vector2 dir = lenSqr > 0f ? ab / Mathf.Sqrt(lenSqr) : Vector2.up;
                lateral = (Vector2.Dot(position - closest, new Vector2(-dir.y, dir.x)) < 0f ? -1f : 1f) * Mathf.Sqrt(sqr);
            }
        }

        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
