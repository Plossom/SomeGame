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
        [Tooltip("Optional: lap distances of the section lines after the start, for sections of different lengths " +
                 "(each snaps to the nearest checkpoint). Empty = equal parts.")]
        public float[] sectionLines = { };
        [Tooltip("Grass margin around the track before the invisible boundary wall.")]
        [Min(5f)] public float boundaryMargin = 25f;

        [Header("Houses (the track runs through them)")]
        [Tooltip("Rooms: each has its own floor; walls are drawn round every room, with a doorway where the road crosses.")]
        public List<Room> rooms = new();
        [Tooltip("Whole houses: their roof shows from outside and fades away when you drive in.")]
        public List<House> houses = new();
        [Tooltip("Stretches of the lap on an upper floor (the car looks higher up there).")]
        public List<Span> upstairs = new();
        [Tooltip("Staircases on the road (steps drawn across it).")]
        public List<Span> stairs = new();
        [Tooltip("Furniture and other pieces placed by hand (cells of the track's scenery sheet).")]
        public List<Decor> decor = new();

        [Serializable]
        public class Decor
        {
            public string cell;
            [Tooltip("The cell's area of the scenery sheet (0-1 UV).")]
            public Rect uv;
            public Vector2 position;
            [Min(0.5f)] public float size = 10f;
            [Tooltip("Degrees.")]
            public float angle;
            [Tooltip("Lies flat under everything else (a rug).")]
            public bool flat;
        }

        [Tooltip("Logs across the road with a little ramp before them: jump them, or bump into them (forest).")]
        public List<float> logJumps = new();

        /// <summary>A cow resting on the road: bump into it and you spin (like oil).</summary>
        [Serializable]
        public class Cow
        {
            public float distance;
            [Tooltip("Sideways offset from the centre line (positive = left).")]
            public float lateral;
            [Tooltip("Degrees, relative to the driving direction.")]
            public float angle;
            [Min(0.5f)] public float radius = 1.2f;
        }
        /// <summary>A roller coaster: a closed loop of rails on stilts (it may cross over the road), a train racing round.</summary>
        [Serializable]
        public class Coaster
        {
            public Vector2[] points = { };
            [Min(1)] public int cars = 4;
            [Tooltip("Metres per second (faster downhill, slower on the climbs).")]
            [Min(1f)] public float speed = 16f;
        }
        /// <summary>A golden ring in the air over a jump: fly through it for a boost.</summary>
        [Serializable]
        public class Ring
        {
            public float distance;
            public float lateral;
        }
        [Tooltip("Boost rings in the air over the jumps.")]
        public List<Ring> rings = new();
        /// <summary>A waterfall pouring across the road: you drive straight through it.</summary>
        [Serializable]
        public class Waterfall
        {
            public float distance;
            [Tooltip("Which side it falls from (+1 left, -1 right).")]
            public float side = 1f;
        }
        public List<Waterfall> waterfalls = new();
        [Tooltip("The race runs from afternoon into the night: sunset after the first section line, night after the second (lanterns, headlights, fireflies; on snow the northern lights instead of lanterns).")]
        public bool dusk;

        [Tooltip("Roller coasters over the fairground.")]
        public List<Coaster> coasters = new();
        [Tooltip("Strings of pennants along both sides of the road on these stretches (lap distances from, to).")]
        public List<Vector2> buntings = new();
        public enum PadLook { Auto, Trampoline, Orange }
        [Tooltip("How the jump pads look (Auto: mushrooms in the meadows, jump pads elsewhere, gummi boats in the water; " +
                 "Orange: the orange jump pads on dry ground too, gummi boats in the water).")]
        public PadLook padLook;
        public enum BoatKind { Sailboat, Rowboat, SwanBoat, Buoy, Ducks, Iceberg, Trawler, Icebreaker, SealFloe }
        /// <summary>Something on the lake: a boat, a buoy, a duck family.</summary>
        [Serializable]
        public class Boat
        {
            public BoatKind kind;
            public Vector2 position;
            [Tooltip("Degrees.")]
            public float angle;
            [Tooltip("Goes slowly round a circle this big (0 = stays where it is, bobbing).")]
            public float drift;
        }
        [Tooltip("Boats, buoys and ducks on the lakes.")]
        public List<Boat> boats = new();
        [Tooltip("Fireworks over the scenery, away from the road.")]
        public bool fireworks;

        [Tooltip("Cows chilling on the road (farm).")]
        public List<Cow> cows = new();
        [Tooltip("Wooden fences along both sides of the road on these stretches (lap distances from, to).")]
        public List<Vector2> fences = new();

        public enum FloorKind { Parquet, Tiles, Carpet, Planks, Sawdust, Cobbles, Ice, Rock }
        public enum RoofKind { Red, Blue, Circus, Slate, Ice, Gingerbread, Cave }

        [Serializable]
        public class Room
        {
            public Vector2 min, max;
            public FloorKind floor;
            [Tooltip("An upstairs room (drawn raised).")]
            public bool upper;
        }

        [Serializable]
        public class House
        {
            public Vector2 min, max;
            public RoofKind roof;
        }

        [Serializable]
        public struct Span
        {
            public float from, to;
        }

        [Header("Journey surprises")]
        [Tooltip("Level crossings: a little mine train rolls across the road now and then (lights blink, barriers drop first). Its timetable differs every race.")]
        public List<Train> trains = new();
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
            [Tooltip("A wooden toy train on wooden rails (in the house).")]
            public bool toy;
        }

        public enum PropKind { Windmill, Carousel, SwingRide, Teacups }

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
        [Tooltip("This track's own road surface (empty = the biome's usual asphalt).")]
        public Material roadMaterial;
        [Tooltip("What everyone races in on this track.")]
        public Vehicle vehicle;
        [Tooltip("No snowfall over the screen on this snow track.")]
        public bool noSnowfall;

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

        public enum Vehicle { Car, Snowmobile }

        public enum SweeperKind { Log, HayBale, Snowball, Boulder, Vacuum, Ball, Curling }

        public enum CrossingKind { Penguins, Sheep, Cat, Reindeer }

        /// <summary>A little line of penguins that waddles across the road, waits, and waddles back.</summary>
        [Serializable]
        public class Penguins
        {
            [Tooltip("What crosses the road: a line of penguins, a herd of sheep, or the house cat (count 1).")]
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
        [Tooltip("Patches of glare ice on the road (world positions): slippery, little grip like a frozen lake.")]
        public List<IcePatch> icePatches = new();
        [Tooltip("Downhill stretches (lap distances): cars run faster than their top speed there.")]
        public List<Slope> slopes = new();

        [Serializable]
        public class Slope
        {
            public float from, to;
            [Tooltip("Top speed there, as a share of the normal top speed.")]
            [Range(1f, 1.6f)] public float speed = 1.25f;
        }

        [Tooltip("Heaps of snow on the road: blast through one and it bursts into powder (you lose a little speed); it builds up again.")]
        public List<SnowPile> snowPiles = new();

        [Tooltip("Chasms (closed outlines, world positions): off the road inside one you fall into the depths and are carried back. " +
                 "The road may run across one on a ledge or jump it through a gap.")]
        public List<Chasm> chasms = new();
        [Serializable]
        public class Chasm
        {
            public Vector2[] points = { };
            /// <summary>Inside the outline (even-odd).</summary>
            public bool Contains(Vector2 p)
            {
                bool inside = false;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2 a = points[i], b = points[j];
                    if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                return inside;
            }
        }
        [Tooltip("Caves: stretches of road (the main road, or a branch) running through the mountain: rock walls on both sides, " +
                 "the mountain on top (it fades away while you are inside), dark with glowing ice crystals and headlights on.")]
        public List<Cave> caves = new();
        [Serializable]
        public class Cave
        {
            [Tooltip("-1 = on the main road (from/to are lap distances); else the branch's index (from/to along the branch, to <= 0 = its end).")]
            public int branch = -1;
            public float from, to;
            [Tooltip("Ice stalagmites standing in the cave (solid), as (along, lateral) pairs.")]
            public Vector2[] stalagmites = { };
        }
        [Serializable]
        public class SnowPile
        {
            [Tooltip("Lap distance.")]
            public float distance;
            [Tooltip("Sideways from the centre line (positive = left).")]
            public float lateral;
            [Min(0.5f)] public float radius = 1.6f;
        }

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
            /// <summary>A tree crown over the road's edge, drawn above the cars.</summary>
            public bool canopy;
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
            [Tooltip("A drawbridge over the gap: seconds per cycle (0 = an open gap, always jumped).")]
            [Min(0f)] public float period;
            [Tooltip("Drawbridge: seconds of each cycle it stands up (then it is a ramp; down, you drive across).")]
            [Min(0f)] public float raised;
            [Tooltip("Drawbridge: shifts its rhythm (seconds).")]
            public float offset;
            [Tooltip("A cable car carries cars across (no ramp): drive into its valley station before the gap.")]
            public bool lift;
            [Tooltip("A cliff drop: the whole width of the road is the take-off (no narrow kicker), you sail off the edge down to the road below.")]
            public bool drop;
            public bool IsDrawbridge => period > 0f;
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
            [Tooltip("Crossings (in lap order, from 0) where a bridge carries the road over: no jump, no water on the road.")]
            public int[] bridged = { };
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
        public class IcePatch
        {
            public Vector2 position;
            [Tooltip("Length (along the road) and width of the patch.")]
            public Vector2 size = new(10f, 7f);
            [Tooltip("Direction of its length (degrees, 0 = east, 90 = north).")]
            public float angle;

            /// <summary>Inside the patch's oval.</summary>
            public bool Contains(Vector2 p)
            {
                Vector2 d = p - position;
                float a = angle * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
                float along = d.x * c + d.y * s, across = -d.x * s + d.y * c;
                float u = along / (size.x * 0.5f), v = across / (size.y * 0.5f);
                return u * u + v * v <= 1f;
            }
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
