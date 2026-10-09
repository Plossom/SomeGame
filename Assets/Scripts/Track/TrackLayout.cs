using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The single source of truth for a circuit: an ordered, closed loop of waypoints plus a road width.
    /// Road mesh, off-road detection, checkpoints, lap counting and the AI line are all derived from it.
    /// Waypoint 0 is the start/finish line; the race runs in waypoint order.
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/Track Layout", fileName = "TrackLayout")]
    public class TrackLayout : ScriptableObject
    {
        [Tooltip("Closed loop of control points (world units). The spline passes through each one.")]
        public Vector2[] waypoints =
        {
            new(0, -8), new(0, 30), new(20, 30), new(20, -8),
        };

        [Min(1f)] public float roadWidth = 7f;
        [Tooltip("Kerb strip outside each road edge. Kerbs count as road (no off-road slowdown).")]
        [Min(0f)] public float kerbWidth = 0.8f;
        [Tooltip("Distance between samples on the smoothed centre line.")]
        [Min(0.25f)] public float sampleSpacing = 1f;
        [Tooltip("Checkpoints evenly spaced along the lap, checkpoint 0 is the start/finish line.")]
        [Min(2)] public int checkpointCount = 6;
        [Tooltip("Grass margin around the track before the invisible boundary wall.")]
        [Min(5f)] public float boundaryMargin = 25f;

        [Header("Water, jumps and oil")]
        [Tooltip("The whole world is water: the road is a causeway, and anything off the road is a splash.")]
        public bool waterWorld;
        [Tooltip("Gaps in the road over water (lap distances), each with a kicker ramp just before it.")]
        public List<Gap> gaps = new();
        [Tooltip("Rivers (open polylines). Where a river crosses the road there is a jump: a ramp before the water.")]
        public List<River> rivers = new();
        [Tooltip("Lakes (ellipses) beside the track. Driving in means a splash and a restart nearby.")]
        public List<Lake> lakes = new();
        [Tooltip("Oil puddles on the road: a car that drives through loses grip for a moment.")]
        public List<OilSpot> oil = new();
        [Tooltip("Length of the small take-off ramp (kicker) before each river crossing.")]
        [Min(1f)] public float rampLength = 3f;
        [Tooltip("Width of the kicker. Only cars that hit it fly over the river.")]
        [Min(1f)] public float rampWidth = 2.8f;

        [Tooltip("Small ramps beside the road on the inside of a corner: driven straight at speed, they hop a car across the corner.")]
        public List<Shortcut> shortcuts = new();

        [Tooltip("Scenery for this track. Empty = the scene's default scenery.")]
        public List<TrackScenery.Kind> scenery = new();

        [Serializable]
        public class Gap
        {
            [Tooltip("Lap distance where the road stops (the ramp's lip).")]
            public float from;
            [Tooltip("Lap distance where the road starts again.")]
            public float to;
            [Tooltip("Sideways position of the kicker (positive = left of the centre line).")]
            public float rampLateral;
        }

        [Serializable]
        public class Shortcut
        {
            [Tooltip("Lap distance where the ramp stands beside the road.")]
            public float from;
            [Tooltip("Lap distance where the jump lands (the ramp points there, along the road's direction at the landing).")]
            public float to;
            [Tooltip("Which side of the road the ramp is on: +1 left, -1 right (the inside of the corner).")]
            public float side = 1f;
            [Min(1f)] public float length = 4f;
            [Min(1f)] public float width = 4.2f;
            [Tooltip("Speed at which the jump lands exactly on target; slower cars land short.")]
            [Min(1f)] public float designSpeed = 15f;
        }

        [Serializable]
        public class River
        {
            public Vector2[] points = { new(-40, 0), new(40, 0) };
            [Min(1f)] public float width = 6f;
            [Tooltip("Sideways position of the kicker at each crossing, in lap order (positive = left of the centre line).")]
            public float[] rampLaterals = { 0f };
        }

        [Serializable]
        public class Lake
        {
            public Vector2 center;
            public Vector2 radii = new(8f, 5f);
            [Tooltip("Rotation in degrees.")]
            public float angle;
        }

        [Serializable]
        public class OilSpot
        {
            [Tooltip("Distance along the lap from the start line.")]
            public float distance;
            [Tooltip("Sideways offset from the centre line (positive = left).")]
            public float lateral;
            [Min(0.3f)] public float radius = 1.3f;
        }

        /// <summary>Raised when the layout is edited in the Inspector, so the track can rebuild.</summary>
        public event System.Action Changed;

        void OnValidate() => Changed?.Invoke();

        public float HalfWidth => roadWidth * 0.5f;
        /// <summary>Lateral distance from the centre line beyond which a car is on grass.</summary>
        public float OffRoadDistance => HalfWidth + kerbWidth;
    }
}
