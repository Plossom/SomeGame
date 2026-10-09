using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>Result of projecting a world position onto the centre line.</summary>
    public struct TrackPoint
    {
        /// <summary>Index of the centre-line segment (sample i to i+1).</summary>
        public int Segment;
        /// <summary>Distance along the lap from the start line, in [0, Length).</summary>
        public float Distance;
        /// <summary>Signed distance from the centre line; positive is left of the driving direction.</summary>
        public float Lateral;
        public Vector2 Position;
    }

    /// <summary>
    /// Smoothed, evenly sampled closed centre line built from a <see cref="TrackLayout"/>
    /// (centripetal Catmull-Rom through the waypoints). Pure data, no Unity objects.
    /// </summary>
    public sealed class TrackPath
    {
        readonly Vector2[] _points;
        readonly float[] _distances;

        public float Length { get; }
        public int Count => _points.Length;
        public Vector2 this[int i] => _points[Wrap(i)];
        public float DistanceAt(int i) => _distances[Wrap(i)];

        public TrackPath(IReadOnlyList<Vector2> waypoints, float spacing)
        {
            var samples = new List<Vector2>();
            int n = waypoints.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = waypoints[(i - 1 + n) % n], p1 = waypoints[i];
                Vector2 p2 = waypoints[(i + 1) % n], p3 = waypoints[(i + 2) % n];
                int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / spacing));
                for (int s = 0; s < steps; s++)
                    samples.Add(CatmullRom(p0, p1, p2, p3, s / (float)steps));
            }

            _points = samples.ToArray();
            _distances = new float[_points.Length];
            float d = 0f;
            for (int i = 0; i < _points.Length; i++)
            {
                _distances[i] = d;
                d += Vector2.Distance(_points[i], _points[(i + 1) % _points.Length]);
            }
            Length = d;
        }

        public int Wrap(int i) => ((i % _points.Length) + _points.Length) % _points.Length;
        public float WrapDistance(float d) => Mathf.Repeat(d, Length);

        /// <summary>Shortest signed distance from a to b along the loop, in (-Length/2, Length/2].</summary>
        public float DeltaDistance(float from, float to)
        {
            float delta = Mathf.Repeat(to - from, Length);
            return delta > Length * 0.5f ? delta - Length : delta;
        }

        public int SegmentAt(float distance)
        {
            distance = WrapDistance(distance);
            int lo = 0, hi = _distances.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (_distances[mid] <= distance) lo = mid; else hi = mid - 1;
            }
            return lo;
        }

        public Vector2 PointAt(float distance)
        {
            distance = WrapDistance(distance);
            int i = SegmentAt(distance);
            float segLength = SegmentLength(i);
            float t = segLength > 0f ? (distance - _distances[i]) / segLength : 0f;
            return Vector2.Lerp(_points[i], this[i + 1], t);
        }

        public Vector2 TangentAt(float distance) => SegmentDirection(SegmentAt(distance));

        /// <summary>Left-hand normal at a distance along the lap.</summary>
        public Vector2 NormalAt(float distance) => Left(TangentAt(distance));

        /// <summary>Smoothed left-hand normal at sample i (average of the two adjacent segments).</summary>
        public Vector2 SampleNormal(int i) =>
            Left((SegmentDirection(i - 1) + SegmentDirection(i)).normalized);

        public Vector2 SegmentDirection(int i) => (this[i + 1] - this[i]).normalized;

        /// <summary>
        /// How much the road turns around sample i, in degrees over ±<paramref name="span"/> samples.
        /// Positive turns left (toward the left-hand normal), negative turns right.
        /// </summary>
        public float TurnAt(int i, int span) => Vector2.SignedAngle(SegmentDirection(i - span), SegmentDirection(i + span));

        public float TurnAtDistance(float distance, int span) => TurnAt(SegmentAt(distance), span);
        public float SegmentLength(int i) => Vector2.Distance(this[i], this[i + 1]);

        /// <summary>
        /// Projects a position onto the centre line. With a hint, only segments within
        /// <paramref name="window"/> of it are searched, which keeps cars from snapping to a
        /// nearby but different part of the circuit.
        /// </summary>
        public TrackPoint Project(Vector2 position, int hint = -1, int window = 0)
        {
            int start = 0, count = _points.Length;
            if (hint >= 0 && window > 0 && window * 2 + 1 < _points.Length)
            {
                start = hint - window;
                count = window * 2 + 1;
            }

            var best = new TrackPoint();
            float bestSqr = float.MaxValue;
            for (int k = 0; k < count; k++)
            {
                int i = Wrap(start + k);
                Vector2 a = _points[i], ab = this[i + 1] - a;
                float lenSqr = ab.sqrMagnitude;
                float t = lenSqr > 0f ? Mathf.Clamp01(Vector2.Dot(position - a, ab) / lenSqr) : 0f;
                Vector2 closest = a + ab * t;
                float sqr = (position - closest).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best.Segment = i;
                best.Position = closest;
                best.Distance = WrapDistance(_distances[i] + Mathf.Sqrt(lenSqr) * t);
                Vector2 dir = lenSqr > 0f ? ab / Mathf.Sqrt(lenSqr) : Vector2.up;
                float side = Vector2.Dot(position - closest, Left(dir));
                best.Lateral = (side < 0f ? -1f : 1f) * Mathf.Sqrt(sqr);
            }
            return best;
        }

        public Rect Bounds()
        {
            Vector2 min = _points[0], max = _points[0];
            foreach (var p in _points)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Vector2 Left(Vector2 v) => new(-v.y, v.x);

        // Centripetal Catmull-Rom (Barry-Goldman), avoids cusps and overshoot on uneven spacing.
        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
        {
            float t0 = 0f;
            float t1 = t0 + Knot(p0, p1);
            float t2 = t1 + Knot(p1, p2);
            float t3 = t2 + Knot(p2, p3);
            float t = Mathf.Lerp(t1, t2, u);

            Vector2 a1 = Lerp(p0, p1, t0, t1, t);
            Vector2 a2 = Lerp(p1, p2, t1, t2, t);
            Vector2 a3 = Lerp(p2, p3, t2, t3, t);
            Vector2 b1 = Lerp(a1, a2, t0, t2, t);
            Vector2 b2 = Lerp(a2, a3, t1, t3, t);
            return Lerp(b1, b2, t1, t2, t);
        }

        static float Knot(Vector2 a, Vector2 b) => Mathf.Max(1e-4f, Mathf.Sqrt(Vector2.Distance(a, b)));

        static Vector2 Lerp(Vector2 a, Vector2 b, float ta, float tb, float t) =>
            (tb - t) / (tb - ta) * a + (t - ta) / (tb - ta) * b;
    }
}
