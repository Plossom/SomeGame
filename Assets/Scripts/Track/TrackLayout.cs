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
        [Tooltip("Optional road width changes along the lap (lap distance, width); blended smoothly between keys. Empty = roadWidth everywhere.")]
        public List<WidthKey> widths = new();

        [Serializable]
        public struct WidthKey
        {
            public float distance;
            [Min(1f)] public float width;
        }
        [Tooltip("Kerb strip outside each road edge. Kerbs count as road (no off-road slowdown).")]
        [Min(0f)] public float kerbWidth = 0.8f;
        [Tooltip("Distance between samples on the smoothed centre line.")]
        [Min(0.25f)] public float sampleSpacing = 1f;
        [Tooltip("Checkpoints evenly spaced along the lap, checkpoint 0 is the start/finish line.")]
        [Min(2)] public int checkpointCount = 6;
        [Tooltip("1 = a circuit raced in laps. 3 = one long journey in three sections: the loop is split into equal parts " +
                 "and each section line counts like a lap line (Mario Kart style). checkpointCount must be a multiple of it.")]
        [Min(1)] public int sections = 1;
        [Tooltip("Grass margin around the track before the invisible boundary wall.")]
        [Min(5f)] public float boundaryMargin = 25f;

        [Header("Journey surprises")]
        [Tooltip("Level crossings: a little mine train rolls across the road now and then (lights blink, barriers drop first). Its timetable differs every race.")]
        public List<Train> trains = new();
        [Tooltip("A rockslide closes one of two ways each race, at random: either this branch or the main road beside it.")]
        public List<Rockslide> rockslides = new();
        [Tooltip("Landmarks with moving parts (a windmill whose sails turn).")]
        public List<Prop> props = new();

        [Serializable]
        public class Train
        {
            [Tooltip("Lap distance of the level crossing.")]
            public float distance;
            [Tooltip("Average seconds between trains.")]
            [Min(4f)] public float period = 16f;
            [Min(0)] public int wagons = 3;
            [Tooltip("Speed of the train (units per second).")]
            [Min(2f)] public float speed = 13f;
            [Tooltip("How far the rails reach from the road on each side.")]
            [Min(10f)] public float reach = 45f;
        }

        [Serializable]
        public class Rockslide
        {
            [Tooltip("Index of the branch that is the other way through.")]
            public int branch;
        }

        public enum PropKind { Windmill }

        [Serializable]
        public class Prop
        {
            public PropKind kind;
            public Vector2 position;
            [Min(1f)] public float size = 12f;
        }

        [Tooltip("The banked walls' look on this track (empty = the usual ice wall).")]
        public Material bankMaterial;
        [Tooltip("This track's own scenery sheet (empty = the cup's usual one). The scenery kinds' uv point into it.")]
        public Material sceneryMaterial;

        [Header("Ground")]
        [Tooltip("Stretches of other ground along the road (mud, sand, red rock, pebbles...), soft-edged, under the road and scenery.")]
        public List<GroundZone> grounds = new();

        public enum GroundKind { Mud, Sand, RedRock, Pebbles, Wheat }

        [Serializable]
        public class GroundZone
        {
            public GroundKind kind;
            [Tooltip("Lap distances where it starts and ends.")]
            public float from, to;
            [Tooltip("How far it reaches out from the centre line on each side.")]
            [Min(2f)] public float reach = 30f;
            [Tooltip("Distance over which it fades in and out at its ends.")]
            [Min(0f)] public float fade = 25f;
        }

        [Header("Water, jumps and oil")]
        [Tooltip("The whole world is water: the road is a causeway, and anything off the road is a splash.")]
        public bool waterWorld;
        [Tooltip("Look of the land around the road: green meadows (Valley Cup) or snow (Winter Cup).")]
        public Biome biome;
        [Tooltip("Gaps in the road over water (lap distances), each with a kicker ramp just before it.")]
        public List<Gap> gaps = new();
        [Tooltip("The road stands a little above the ground and casts a shadow (always on water tracks).")]
        public bool elevatedRoad;

        [Header("Hazards")]
        [Tooltip("Gummiboats: bouncy inflatable boats on the water. Land on one (or drive onto it) and it catapults you on.")]
        public List<Bouncer> bouncers = new();
        [Tooltip("Fish that leap across the road in a rhythm (from the water, or from a pond beside the road); a hit spins the car round.")]
        public List<Geyser> geysers = new();
        [Tooltip("Obstacles that roll back and forth across the road (logs, hay bales).")]
        public List<Sweeper> sweepers = new();
        [Tooltip("How busy the sky is: birds and balloons flying over (0 = none, 1 = lots).")]
        [Range(0f, 1f)] public float skyLife = 0.3f;

        [Serializable]
        public class Bouncer
        {
            public Vector2 position;
            [Min(0.5f)] public float radius = 2.4f;
            [Tooltip("Time in the air after a bounce.")]
            [Min(0.1f)] public float airTime = 0.8f;
            [Tooltip("Speed multiplier on the bounce.")]
            [Min(0.5f)] public float speedBoost = 1.1f;
            [Tooltip("Jump pad: catapults the car towards this point (the next pad, or the road). Zero = the car keeps its heading.")]
            public Vector2 target;
            public bool HasTarget => target != Vector2.zero;
        }

        /// <summary>A jumping fish (kept as "Geyser" so existing track data loads).</summary>
        [Serializable]
        public class Geyser
        {
            [Tooltip("Lap distance where the fish crosses the road.")]
            public float distance;
            [Tooltip("Which side it jumps from: +1 left, -1 right.")]
            public float side = 1f;
            [Min(0.5f)] public float period = 3f;
            [Tooltip("Seconds each leap takes from one side to the other.")]
            [Min(0.1f)] public float active = 1f;
            [Tooltip("Shifts its rhythm (seconds).")]
            public float offset;
        }

        public enum Biome { Meadow, Snow }

        public enum SweeperKind { Log, HayBale, Snowball, Boulder }

        public enum CrossingKind { Penguins, Sheep }

        /// <summary>A little line of penguins that waddles across the road, waits, and waddles back.</summary>
        [Serializable]
        public class Penguins
        {
            [Tooltip("What crosses the road: a line of penguins, or a herd of sheep.")]
            public CrossingKind kind;
            [Tooltip("Lap distance where they cross.")]
            public float distance;
            [Min(1)] public int count = 3;
            [Tooltip("Seconds for one crossing there and back, waits included.")]
            [Min(1f)] public float period = 8f;
            [Tooltip("Seconds one crossing takes.")]
            [Min(0.5f)] public float crossing = 3f;
            [Tooltip("Shifts their rhythm (seconds).")]
            public float offset;
        }

        /// <summary>A dash panel on the road: drive over it for a boost.</summary>
        [Serializable]
        public class DashPanel
        {
            public float distance;
            [Tooltip("Sideways position (positive = left of the centre line).")]
            public float lateral;
            [Min(1f)] public float length = 4f;
            [Min(1f)] public float width = 3.2f;
        }

        [Serializable]
        public class Sweeper
        {
            public SweeperKind kind;
            [Tooltip("Lap distance where it crosses the road.")]
            public float distance;
            [Tooltip("Seconds for one trip across and back.")]
            [Min(0.5f)] public float period = 4f;
            [Tooltip("Shifts its rhythm (seconds).")]
            public float offset;
        }
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

        [Tooltip("Other ways round: pieces of road (or ice, or rooftops) that leave the main road and rejoin it further on.")]
        public List<Branch> branches = new();
        [Tooltip("Banked walls on the outside of corners: ride them fast to keep full speed through the corner.")]
        public List<Bank> banks = new();
        [Tooltip("Penguins crossing the road (bump one and you spin).")]
        public List<Penguins> penguins = new();
        [Tooltip("Dash panels on the road: a boost for driving over them.")]
        public List<DashPanel> dashPanels = new();

        [Tooltip("Scenery for this track. Empty = the scene's default scenery.")]
        public List<TrackScenery.Kind> scenery = new();

        [Tooltip("Scenery placed in the Editor (computed automatically when the track or its scenery settings change), so a race loads instantly.")]
        [HideInInspector] public List<SceneryItem> bakedScenery = new();
        [HideInInspector] public string bakedSignature;

        [Serializable]
        public struct SceneryItem
        {
            public Vector2 position;
            public Vector2 right;
            public Rect uv;
            public Color32 tint;
        }

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

        /// <summary>
        /// A banked wall on the outside of a corner, from the kerb outwards: driven fast it is road with extra
        /// grip (ride the wall round the corner at full speed); too slow, the car slides back down.
        /// </summary>
        [Serializable]
        public class Bank
        {
            [Tooltip("Lap distances where the wall starts and ends.")]
            public float from, to;
            [Tooltip("Which side of the road: +1 left, -1 right (the outside of the corner).")]
            public float side = 1f;
            [Min(1f)] public float width = 4.5f;
        }

        public enum BranchKind { Road, Ice, Roof }

        /// <summary>
        /// Another way round: leaves the main road at lap distance <see cref="from"/>, runs through
        /// <see cref="points"/> and rejoins at <see cref="to"/>. Driving on it counts as road (with its own
        /// surface) and as progress from <see cref="from"/> to <see cref="to"/>, so checkpoints and
        /// positions keep working whichever way a car takes.
        /// </summary>
        [Serializable]
        public class Branch
        {
            public BranchKind kind;
            [Tooltip("Lap distance where it leaves the main road.")]
            public float from;
            [Tooltip("Lap distance where it rejoins the main road (further on).")]
            public float to;
            [Tooltip("Points the branch runs through between leaving and rejoining (world positions).")]
            public Vector2[] points = { };
            [Min(2f)] public float width = 7f;
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
