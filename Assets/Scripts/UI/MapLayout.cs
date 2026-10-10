using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The forest road on the map screen, in canvas units of the 1170 x 2532 map picture (origin at
    /// the bottom left). Shared by the painter that bakes the landscape and by <see cref="MapScreen"/>,
    /// which places the race stops, the progress line and the car on the same road.
    /// </summary>
    public static class MapLayout
    {
        public static readonly Vector2 Size = new(1170f, 2532f);

        // The road winds up from the bottom of the screen past the race stops and ends at the last one.
        // (The landscape picture is stretched upward by MapArtGenerator.Stretch; these points include it.)
        static readonly Vector2[] Points =
        {
            new(40f, -184f), new(190f, 345f), new(255f, 590f), new(330f, 828f), new(640f, 1012f), new(890f, 1104f),
            new(800f, 1346f), new(500f, 1483f), new(410f, 1656f),
        };

        /// <summary>Index into the control points of each stop: races 1-8 (a last stop past the races shows "more soon").</summary>
        /// <summary>The first and last race sit on these control points; the others are spaced evenly along the road between them.</summary>
        const int FirstStopPoint = 1, LastStopPoint = 8;
        const int Stops = 8;

        public static int StopCount => Stops;

        static List<Vector2> _samples;
        static List<float> _distances;

        /// <summary>The road as a dense polyline.</summary>
        public static IReadOnlyList<Vector2> Samples { get { Build(); return _samples; } }

        public static float Length { get { Build(); return _distances[^1]; } }

        /// <summary>Road distance from the start to stop <paramref name="index"/>.</summary>
        public static float StopDistance(int index)
        {
            Build();
            float first = _distances[FirstStopPoint * Steps], last = _distances[LastStopPoint * Steps];
            return Mathf.Lerp(first, last, Stops > 1 ? index / (float)(Stops - 1) : 0f);
        }

        public static Vector2 StopPosition(int index) => PointAt(StopDistance(index), out _);

        /// <summary>Road width at a height on the map: narrower further up (into the distance).</summary>
        public static float WidthAt(float y) => Mathf.Lerp(78f, 30f, Mathf.InverseLerp(920f, 1700f, y));

        public static Vector2 PointAt(float distance, out Vector2 tangent)
        {
            Build();
            distance = Mathf.Clamp(distance, 0f, Length);
            int i = _distances.BinarySearch(distance);
            if (i < 0) i = Mathf.Max(1, ~i);
            i = Mathf.Clamp(i, 1, _samples.Count - 1);
            float t = Mathf.InverseLerp(_distances[i - 1], _distances[i], distance);
            tangent = (_samples[i] - _samples[i - 1]).normalized;
            return Vector2.Lerp(_samples[i - 1], _samples[i], t);
        }

        const int Steps = 24;

        static void Build()
        {
            if (_samples != null) return;
            _samples = new List<Vector2>();
            _distances = new List<float>();
            for (int i = 0; i < Points.Length - 1; i++)
            {
                Vector2 p0 = Points[Mathf.Max(0, i - 1)], p1 = Points[i], p2 = Points[i + 1], p3 = Points[Mathf.Min(Points.Length - 1, i + 2)];
                for (int k = 0; k < Steps; k++) Add(CatmullRom(p0, p1, p2, p3, k / (float)Steps));
            }
            Add(Points[^1]);
        }

        static void Add(Vector2 p)
        {
            _distances.Add(_samples.Count == 0 ? 0f : _distances[^1] + Vector2.Distance(_samples[^1], p));
            _samples.Add(p);
        }

        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
