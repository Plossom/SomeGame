using System.Collections.Generic;
using SomeGame.Race;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// One map road, in canvas units of the 1170 x 2532 map picture (origin at the bottom left): a smooth
    /// curve through control points, with the race stops spaced evenly between two of them.
    /// </summary>
    public sealed class MapRoad
    {
        readonly Vector2[] _points;
        readonly int _firstStopPoint, _lastStopPoint;

        /// <summary>Race stops on this road (the cup's races, or its races and a "coming soon" stop).</summary>
        public int Stops { get; }

        /// <summary>Nudges single stops along the road (by stop index; negative: further down, towards the bottom).</summary>
        public float[] StopShifts { get; set; }
        List<Vector2> _samples;
        List<float> _distances;

        /// <param name="points">The road from the bottom of the screen; it ends at the last stop.</param>
        /// <param name="firstStopPoint">Control point of race 1.</param>
        /// <param name="lastStopPoint">Control point of the last stop.</param>
        /// <param name="stops">Race stops spaced evenly between them.</param>
        public MapRoad(Vector2[] points, int firstStopPoint, int lastStopPoint, int stops)
        {
            _points = points;
            _firstStopPoint = firstStopPoint;
            _lastStopPoint = lastStopPoint;
            Stops = stops;
        }

        /// <summary>The road as a dense polyline.</summary>
        public IReadOnlyList<Vector2> Samples { get { Build(); return _samples; } }

        public float Length { get { Build(); return _distances[^1]; } }

        /// <summary>Road distance from the start to stop <paramref name="index"/>.</summary>
        public float StopDistance(int index)
        {
            Build();
            float first = _distances[_firstStopPoint * Steps], last = _distances[_lastStopPoint * Steps];
            int stops = Stops;
            float d = Mathf.Lerp(first, last, stops > 1 ? index / (float)(stops - 1) : 0f);
            if (StopShifts != null && index < StopShifts.Length) d += StopShifts[index];
            return Mathf.Max(0f, d);
        }

        public Vector2 StopPosition(int index) => PointAt(StopDistance(index), out _);

        public Vector2 PointAt(float distance, out Vector2 tangent)
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

        void Build()
        {
            if (_samples != null) return;
            _samples = new List<Vector2>();
            _distances = new List<float>();
            for (int i = 0; i < _points.Length - 1; i++)
            {
                Vector2 p0 = _points[Mathf.Max(0, i - 1)], p1 = _points[i], p2 = _points[i + 1], p3 = _points[Mathf.Min(_points.Length - 1, i + 2)];
                for (int k = 0; k < Steps; k++) Add(CatmullRom(p0, p1, p2, p3, k / (float)Steps));
            }
            Add(_points[^1]);
        }

        void Add(Vector2 p)
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

    /// <summary>
    /// The map roads of the cups. Shared by the painters that bake the landscapes and by
    /// <see cref="MapScreen"/>, which places the race stops, the progress line and the car on the road of
    /// the cup it shows (<see cref="Current"/>).
    /// </summary>
    public static class MapLayout
    {
        public static readonly Vector2 Size = new(1170f, 2532f);

        // The landscape pictures are stretched upward by MapArtGenerator.Stretch; these points include it.

        /// <summary>The forest road: switchbacks up from the bottom (like the mountain road), past the lake to the last race.</summary>
        public static readonly MapRoad Valley = new(new Vector2[]
        {
            new(40f, -184f), new(170f, 330f), new(470f, 400f), new(830f, 520f), new(860f, 660f), new(560f, 770f),
            new(230f, 860f), new(260f, 1010f), new(600f, 1060f), new(880f, 1150f), new(800f, 1346f), new(500f, 1483f),
            new(410f, 1656f),
        }, 1, 12, 8) { StopShifts = new[] { 0f, 0f, 0f, -55f, 60f } }; // 4 and 5 apart round the left bend

        /// <summary>The mountain road: comes in from the bottom left and climbs in switchbacks towards the peaks.</summary>
        public static readonly MapRoad Winter = new(new Vector2[]
        {
            new(50f, -160f), new(270f, 300f), new(690f, 430f), new(920f, 620f), new(690f, 800f), new(290f, 900f),
            new(210f, 1060f), new(490f, 1190f), new(840f, 1290f), new(870f, 1450f), new(530f, 1560f),
        }, 1, 10, 8);

        public static MapRoad Of(CupLook look) => look == CupLook.Winter ? Winter : Valley;

        /// <summary>The road of the map being shown (or painted).</summary>
        public static MapRoad Current { get; set; } = Valley;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Current = Valley;

        public static int StopCount => Current.Stops;

        public static IReadOnlyList<Vector2> Samples => Current.Samples;
        public static float Length => Current.Length;
        public static float StopDistance(int index) => Current.StopDistance(index);
        public static Vector2 StopPosition(int index) => Current.StopPosition(index);
        public static Vector2 PointAt(float distance, out Vector2 tangent) => Current.PointAt(distance, out tangent);

        /// <summary>Road width at a height on the map: narrower further up (into the distance).</summary>
        public static float WidthAt(float y) => Mathf.Lerp(78f, 30f, Mathf.InverseLerp(920f, 1700f, y));
    }
}
