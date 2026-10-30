using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Scene entry point for the circuit. Owns the <see cref="TrackPath"/> built from the layout and
    /// answers every track question (position along the lap, off-road, checkpoints, spawn points).
    /// </summary>
    [ExecuteAlways]
    public class Track : MonoBehaviour
    {
        [SerializeField] TrackLayout layout;

        TrackPath _path;

        /// <summary>Raised after the path is rebuilt (layout edited in the Inspector).</summary>
        public event Action Rebuilt;

        public TrackLayout Layout => layout;
        public TrackPath Path => _path ??= Build();
        public float Length => Path.Length;
        public int CheckpointCount => layout.checkpointCount;

        /// <summary>
        /// Picks this race's surprises (the train timetable, when the herds and the cat cross). Set by the race
        /// before the track starts; online every phone gets the same number, so all see the same race.
        /// </summary>
        public static int SurpriseSeed { get; set; }
        /// <summary>Sections of a journey track (1 for a circuit raced in laps).</summary>
        public int Sections => SectionCheckpoints.Length;

        int[] _sectionCheckpoints;

        /// <summary>
        /// The checkpoints that are lap lines: just the start line for a circuit; for a journey also each section
        /// line (evenly spaced, or at the layout's own section line distances).
        /// </summary>
        public int[] SectionCheckpoints => _sectionCheckpoints ??= FindSectionCheckpoints();

        int[] FindSectionCheckpoints()
        {
            int count = CheckpointCount;
            var lines = new List<int> { 0 };
            if (layout.sectionLines is { Length: > 0 })
            {
                foreach (float d in layout.sectionLines)
                {
                    int k = Mathf.Clamp(Mathf.RoundToInt(d / Path.Length * count), 1, count - 1);
                    if (!lines.Contains(k)) lines.Add(k);
                }
                lines.Sort();
            }
            else if (layout.sections > 1 && count % layout.sections == 0)
                for (int k = 1; k < layout.sections; k++) lines.Add(k * count / layout.sections);
            return lines.ToArray();
        }

        /// <summary>True if checkpoint <paramref name="index"/> is the start line or a section line.</summary>
        public bool IsLapLine(int index) => System.Array.IndexOf(SectionCheckpoints, ((index % CheckpointCount) + CheckpointCount) % CheckpointCount) >= 0;

        /// <summary>Lap (or section) lines crossed after the start, for a car that has passed this many checkpoints.</summary>
        public int LinesPassed(int checkpointsPassed)
        {
            if (checkpointsPassed <= 1) return 0;
            int count = CheckpointCount, loops = (checkpointsPassed - 1) / count, rest = (checkpointsPassed - 1) % count;
            int lines = loops * Sections;
            foreach (int k in SectionCheckpoints) if (k >= 1 && k <= rest) lines++;
            return lines;
        }

        /// <summary>True if a point is inside one of the rooms of a house the track runs through.</summary>
        public bool InRoom(Vector2 p)
        {
            if (layout.rooms == null) return false;
            foreach (var room in layout.rooms)
                if (p.x >= room.min.x && p.x <= room.max.x && p.y >= room.min.y && p.y <= room.max.y) return true;
            return false;
        }

        /// <summary>True on the ice floor of a room (an ice palace hall): little grip, like a frozen lake.</summary>
        public bool OnIceFloor(Vector2 p)
        {
            if (layout.rooms == null) return false;
            foreach (var room in layout.rooms)
                if (room.floor == TrackLayout.FloorKind.Ice && p.x >= room.min.x && p.x <= room.max.x && p.y >= room.min.y && p.y <= room.max.y) return true;
            return false;
        }

        /// <summary>Top-speed factor at a lap distance: above 1 running down a slope.</summary>
        public float SlopeAt(float distance)
        {
            if (layout.slopes == null) return 1f;
            foreach (var slope in layout.slopes)
            {
                float into = Path.DeltaDistance(slope.from, distance);
                if (into >= 0f && into <= Path.DeltaDistance(slope.from, slope.to)) return slope.speed;
            }
            return 1f;
        }

        /// <summary>
        /// In the helix the loops lie on top of each other, so the nearest road is any of them: the one nearest (along
        /// the lap) to <paramref name="near"/> - where the car was - is the one it is on.
        /// </summary>
        public TrackPoint HelixResolve(TrackPoint point, Vector2 position, float near)
        {
            var h = layout.helix;
            if (h == null || !h.enabled || h.Loop <= 1f) return point;
            if (point.Distance < h.from - 1f || point.Distance > h.to + 1f) return point;
            float best = point.Distance, bestErr = Mathf.Abs(Path.DeltaDistance(near, best));
            for (int k = -h.turns; k <= h.turns; k++)
            {
                float d = point.Distance + k * h.Loop;
                if (k == 0 || d < h.from - 1f || d > h.to + 1f) continue;
                float e = Mathf.Abs(Path.DeltaDistance(near, d));
                if (e < bestErr) { bestErr = e; best = d; }
            }
            if (best == point.Distance) return point;
            int segment = Mathf.Clamp(Mathf.RoundToInt(best / Path.Length * Path.Count), 0, Path.Count - 1);
            return Path.Project(position, segment, 12);
        }

        /// <summary>The helix level at a lap distance: -1 in the entry tunnel, 0.. up the loops, turns in the exit tunnel; null outside.</summary>
        public int? HelixLevel(float distance)
        {
            var h = layout.helix;
            if (h == null || !h.enabled) return null;
            if (distance < h.entry || distance > h.exit) return null;
            if (distance < h.from) return -1;
            if (distance >= h.to) return h.turns;
            return Mathf.Clamp(Mathf.FloorToInt((distance - h.from) / h.Loop), 0, h.turns - 1);
        }

        /// <summary>True in a hole in the ice (water, even on the road).</summary>
        public bool HoleAt(Vector2 p)
        {
            if (layout.holes == null || layout.holes.Count == 0) return false;
            foreach (var h in layout.holes)
            {
                Vector2 c = Path.PointAt(h.distance) + Path.NormalAt(h.distance) * h.lateral;
                if ((p - c).sqrMagnitude <= h.radius * h.radius) return true;
            }
            return false;
        }

        /// <summary>The turntable under a point, if any.</summary>
        public TrackLayout.Turntable TurntableAt(Vector2 p)
        {
            if (layout.turntables == null) return null;
            foreach (var t in layout.turntables)
                if ((p - t.position).sqrMagnitude <= t.radius * t.radius) return t;
            return null;
        }

        /// <summary>True on a patch of glare ice on the road: little grip.</summary>
        public bool OnIcePatch(Vector2 p)
        {
            if (layout.icePatches == null) return false;
            foreach (var patch in layout.icePatches)
                if (patch.Contains(p)) return !ChasmAt(p);
            return false;
        }

        /// <summary>Lap distance of section line <paramref name="k"/> (0 = the start/finish line).</summary>
        public float SectionLineDistance(int k) => CheckpointDistance(SectionCheckpoints[k]);

        public float CheckpointDistance(int index) =>
            Mathf.Repeat(index, layout.checkpointCount) * Path.Length / layout.checkpointCount;

        public bool IsOffRoad(float distance, float lateral) => Mathf.Abs(lateral) > OffRoadAt(distance);

        /// <summary>Road width at a lap distance (follows the layout's width keys, smoothly blended).</summary>
        public float WidthAt(float distance)
        {
            var keys = layout.widths;
            if (keys == null || keys.Count == 0) return layout.roadWidth;
            if (keys.Count == 1) return keys[0].width;
            float d = Path.WrapDistance(distance);
            // Find the keys around d (keys are in lap order; wrap around the start line).
            int next = keys.FindIndex(k => k.distance > d);
            if (next < 0) next = 0;
            int prev = (next - 1 + keys.Count) % keys.Count;
            float span = Mathf.Repeat(keys[next].distance - keys[prev].distance, Path.Length);
            float t = span <= 0.01f ? 1f : Mathf.Repeat(d - keys[prev].distance, Path.Length) / span;
            return Mathf.Lerp(keys[prev].width, keys[next].width, Mathf.SmoothStep(0f, 1f, t));
        }

        public float HalfWidthAt(float distance) => WidthAt(distance) * 0.5f;

        /// <summary>Distance from the centre line beyond which a car is off the road (road plus kerb).</summary>
        public float OffRoadAt(float distance) => HalfWidthAt(distance) + layout.kerbWidth;

        /// <summary>A river crossing: a kicker ramp on the road right before the water.</summary>
        public struct Jump
        {
            /// <summary>Lap distance where the ramp starts and where its lip (the river bank) is.</summary>
            public float RampStart, Lip;
            /// <summary>Lap distance of the far bank.</summary>
            public float FarBank;
            /// <summary>Sideways centre of the ramp.</summary>
            public float Lateral;
            /// <summary>The layout gap this jump crosses, or -1 (a river).</summary>
            public int Gap;
            /// <summary>Width of the take-off (the kicker, or the whole road at a cliff drop).</summary>
            public float Width;
        }

        /// <summary>The race clock the drawbridges keep time by (the same for every player); Time.time until a race sets it.</summary>
        public static System.Func<float> ClockSource;
        public static float Clock => ClockSource != null ? ClockSource() : Time.time;

        /// <summary>How far a drawbridge stands up now: 0 down (drive across) .. 1 up (a ramp).</summary>
        public static float BridgeLift(TrackLayout.Gap gap, float time)
        {
            if (!gap.IsDrawbridge) return 1f;
            float t = Mathf.Repeat(time + gap.offset, gap.period);
            const float Move = 0.8f; // seconds to rise or to come down
            float up = Mathf.Clamp01(t / Move) * Mathf.Clamp01((gap.raised + Move - t) / Move);
            return Mathf.SmoothStep(0f, 1f, up);
        }

        /// <summary>Seconds until this drawbridge starts rising (0 while it is moving or up).</summary>
        public static float BridgeRisesIn(TrackLayout.Gap gap, float time)
        {
            float t = Mathf.Repeat(time + gap.offset, gap.period);
            return t < gap.raised + 0.8f ? 0f : gap.period - t;
        }

        public bool GapOpen(TrackLayout.Gap gap)
        {
            if (gap.collapse) return Clock >= CollapseAt(layout.gaps.IndexOf(gap)); // an ice bridge until it falls
            return BridgeLift(gap, Clock) > 0.6f;
        }

        float[] _collapseAt;

        /// <summary>Race-clock time an ice bridge (a gap marked collapse) falls; infinity while it stands.</summary>
        public float CollapseAt(int gap)
        {
            if (_collapseAt == null || gap < 0 || gap >= _collapseAt.Length) return float.PositiveInfinity;
            return _collapseAt[gap];
        }

        /// <summary>Sets an ice bridge falling at a time (once).</summary>
        public void Collapse(int gap, float at)
        {
            if (layout == null || gap < 0 || gap >= layout.gaps.Count) return;
            if (_collapseAt == null || _collapseAt.Length != layout.gaps.Count)
            {
                _collapseAt = new float[layout.gaps.Count];
                for (int i = 0; i < _collapseAt.Length; i++) _collapseAt[i] = float.PositiveInfinity;
            }
            if (float.IsPositiveInfinity(_collapseAt[gap])) _collapseAt[gap] = at;
        }

        /// <summary>Ice bridges standing again, the fallen trees cleared (a new race).</summary>
        public void ResetAvalanche()
        {
            _collapseAt = null;
            Logs.Clear();
        }

        /// <summary>Trunks lying across the road (lap distance, sideways centre, half length): rivals steer round them.</summary>
        public static readonly List<Vector3> Logs = new();

        /// <summary>True on a viaduct (the upper road over another).</summary>
        public bool OnViaduct(float distance)
        {
            if (layout == null || layout.viaducts == null) return false;
            foreach (var span in layout.viaducts)
                if (Path.DeltaDistance(span.from, distance) >= 0f && Path.DeltaDistance(distance, span.to) >= 0f) return true;
            return false;
        }

        /// <summary>Index of the gap whose water is under this lap distance right now (a lowered drawbridge has none), or -1.</summary>
        public int OpenGapAt(float distance)
        {
            for (int i = 0; i < layout.gaps.Count; i++)
            {
                var gap = layout.gaps[i];
                if (gap.hidden || gap.kicker) continue; // the unraced way back / a table top: nothing to fall into
                float from = gap.IsDrawbridge ? gap.from + DrawbridgeRamp : gap.from;
                float into = Path.DeltaDistance(from, distance);
                if (into >= 0f && into <= Path.DeltaDistance(from, gap.to) && GapOpen(gap)) return i;
            }
            return -1;
        }

        /// <summary>Length of a raised drawbridge leaf that works as a ramp.</summary>
        public const float DrawbridgeRamp = 3f;

        List<(float from, float to)> _bridges;

        /// <summary>Lap distance spans where a bridge carries the road over a river.</summary>
        public IReadOnlyList<(float from, float to)> Bridges { get { if (_jumps == null) _jumps = FindJumps(); return _bridges; } }

        /// <summary>True where a bridge carries the road over a river.</summary>
        public bool OnBridge(float distance)
        {
            if (_jumps == null) _jumps = FindJumps();
            foreach (var (from, to) in _bridges)
                if (Path.DeltaDistance(from, distance) >= 0f && Path.DeltaDistance(distance, to) >= 0f) return true;
            return false;
        }

        /// <summary>The ring a car at this point (in the air) flies through, or -1.</summary>
        public int RingAt(Vector2 p, float radius = 2.2f)
        {
            for (int i = 0; i < layout.rings.Count; i++)
            {
                var ring = layout.rings[i];
                Vector2 c = Path.PointAt(ring.distance) + Path.NormalAt(ring.distance) * ring.lateral;
                if ((p - c).sqrMagnitude < radius * radius) return i;
            }
            return -1;
        }

        List<Jump> _jumps;
        List<ShortcutRamp> _shortcuts;

        /// <summary>A big shortcut ramp beside the road, pointing across a corner at a landing spot.</summary>
        public struct ShortcutRamp
        {
            public Vector2 Centre, Direction;
            public float Length, Width;
            /// <summary>Lap distances of the ramp and of the landing spot.</summary>
            public float From, To;
            /// <summary>Time in the air at the ramp's design speed.</summary>
            public float AirTime;
            public Vector2 Lip => Centre + Direction * (Length * 0.5f);

            /// <summary>Position in the ramp's frame: x across (0 = centre line), y along (0 = middle, + toward the lip).</summary>
            public Vector2 Local(Vector2 p)
            {
                Vector2 d = p - Centre;
                return new Vector2(Vector2.Dot(d, new Vector2(Direction.y, -Direction.x)), Vector2.Dot(d, Direction));
            }
        }

        public IReadOnlyList<ShortcutRamp> Shortcuts => _shortcuts ??= FindShortcuts();

        List<ShortcutRamp> FindShortcuts()
        {
            var list = new List<ShortcutRamp>();
            foreach (var s in layout.shortcuts)
            {
                // The ramp starts on the kerb, so a car can drive straight off the road onto it.
                Vector2 roadEdge = Path.PointAt(s.from) + Path.NormalAt(s.from) * (Mathf.Sign(s.side) * (OffRoadAt(s.from) - 0.4f));
                Vector2 target = Path.PointAt(s.to);
                Vector2 dir = (target - roadEdge).normalized;
                Vector2 centre = roadEdge + dir * (s.length * 0.5f);
                Vector2 lip = centre + dir * (s.length * 0.5f);
                list.Add(new ShortcutRamp
                {
                    Centre = centre, Direction = dir, Length = s.length, Width = s.width,
                    From = s.from, To = s.to, AirTime = Vector2.Distance(lip, target) / s.designSpeed,
                });
            }
            return list;
        }

        /// <summary>Every river crossing, in lap order (derived from the rivers and the centre line).</summary>
        public IReadOnlyList<Jump> Jumps => _jumps ??= FindJumps();

        /// <summary>Where each penguin of a crossing is at a time (on or beside the road), and which way it walks.</summary>
        public void PenguinPositions(TrackLayout.Penguins line, float time, List<(Vector2 position, Vector2 heading)> into)
        {
            into.Clear();
            // Each race the herd keeps its own rhythm, shifted by the race's surprise seed (sheep).
            float shift = line.kind != TrackLayout.CrossingKind.Penguins ? SeedShift(line.distance) * line.period : 0f;
            float t = Mathf.Repeat(time + line.offset + shift, line.period), half = line.period * 0.5f;
            bool back = t >= half;
            float walk = Mathf.Clamp01((back ? t - half : t) / line.crossing);
            float reach = OffRoadAt(line.distance) + 2.5f;
            Vector2 centre = Path.PointAt(line.distance), across = Path.NormalAt(line.distance) * (back ? 1f : -1f);
            float spacing = line.kind == TrackLayout.CrossingKind.Reindeer ? 2.8f : 1.6f; // reindeer walk further apart
            for (int k = 0; k < line.count; k++)
            {
                // The leader walks the whole way; the others follow in a row behind it.
                float lateral = Mathf.Lerp(-reach, reach + spacing * (line.count - 1), walk) - k * spacing;
                lateral = Mathf.Clamp(lateral, -reach - spacing * k, reach);
                Vector2 along = Path.TangentAt(line.distance) * ((k % 2 == 0 ? 0.35f : -0.35f));
                into.Add((centre + across * lateral + along, across));
            }
        }

        /// <summary>A fraction 0..1 from the race's surprise seed and a key (e.g. a lap distance): the same on every phone.</summary>
        public static float SeedShift(float key)
        {
            uint h = (uint)(SurpriseSeed * 73856093) ^ (uint)(Mathf.RoundToInt(key * 10f) * 19349663);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0xFFFF) / 65535f;
        }

        readonly List<(Vector2 position, Vector2 heading)> _penguinScratch = new();

        /// <summary>True if a car at <paramref name="p"/> bumps into a penguin.</summary>
        public bool PenguinHit(Vector2 p, float time, float radius = 1.3f)
        {
            if (layout == null || layout.penguins == null) return false;
            foreach (var line in layout.penguins)
            {
                if ((Path.PointAt(line.distance) - p).sqrMagnitude > 400f) continue;
                PenguinPositions(line, time, _penguinScratch);
                float r = line.kind switch { TrackLayout.CrossingKind.Cat => radius * 1.9f, TrackLayout.CrossingKind.Reindeer => radius * 1.5f, _ => radius }; // the cat and the reindeer are bigger
                foreach (var (position, _) in _penguinScratch)
                    if ((position - p).sqrMagnitude < r * r) return true;
            }
            return false;
        }

        /// <summary>The dash panel under a lap distance and sideways offset, if any.</summary>
        public TrackLayout.DashPanel DashPanelAt(float distance, float lateral)
        {
            if (layout == null || layout.dashPanels == null) return null;
            foreach (var panel in layout.dashPanels)
                if (Mathf.Abs(Path.DeltaDistance(panel.distance, distance)) <= panel.length * 0.5f && Mathf.Abs(lateral - panel.lateral) <= panel.width * 0.5f)
                    return panel;
            return null;
        }

        /// <summary>
        /// The banked wall at a lap distance and sideways offset, if the car is on one; <paramref name="height"/>
        /// is how far up the wall it is (0 at the kerb, 1 at the top).
        /// </summary>
        public TrackLayout.Bank BankAt(float distance, float lateral, out float height)
        {
            height = 0f;
            if (layout == null || layout.banks == null) return null;
            foreach (var bank in layout.banks)
            {
                if (Path.DeltaDistance(bank.from, distance) < 0f || Path.DeltaDistance(distance, bank.to) < 0f) continue;
                if (lateral * bank.side <= 0f) continue;
                float up = Mathf.Abs(lateral) - OffRoadAt(distance) + 0.3f;
                if (up < 0f || up > bank.width) continue;
                height = Mathf.Clamp01(up / bank.width);
                return bank;
            }
            return null;
        }

        /// <summary>The other ways round (<see cref="TrackLayout.branches"/>) as drivable lines.</summary>
        public IReadOnlyList<BranchRoute> Branches => _branches ??= BuildBranches();
        List<BranchRoute> _branches;

        List<BranchRoute> BuildBranches()
        {
            var list = new List<BranchRoute>();
            if (layout != null && layout.branches != null)
                foreach (var branch in layout.branches) list.Add(new BranchRoute(branch, Path, layout.sampleSpacing));
            return list;
        }

        /// <summary>
        /// The branch a position is on (within its width plus the kerb), or -1: index, distance along it
        /// and sideways offset. If several overlap, the nearest centre line wins. <paramref name="roofs"/> and
        /// <paramref name="ground"/> choose which branches count (a car on the street is not on a roof).
        /// </summary>
        public int BranchAt(Vector2 position, out float along, out float lateral, bool roofs = true, bool ground = true, int keep = -1)
        {
            int found = -1;
            along = 0f;
            lateral = 0f;
            float best = float.MaxValue;
            var branches = Branches;
            for (int i = 0; i < branches.Count; i++)
            {
                bool roof = branches[i].Kind == TrackLayout.BranchKind.Roof;
                if (roof ? !roofs : !ground) continue;
                if (i != keep && !BranchOpen(i)) continue; // still closed this lap (a car already on it keeps it)
                branches[i].Project(position, out float a, out float l);
                if (Mathf.Abs(l) > branches[i].HalfWidth + layout.kerbWidth || Mathf.Abs(l) >= best) continue;
                best = Mathf.Abs(l);
                found = i;
                along = a;
                lateral = l;
            }
            return found;
        }

        // ------------------------------------------------------------------ phases: the track changes lap by lap

        /// <summary>Something big in the way at a point (a passing train): rivals stop for it. Set by the track's set pieces.</summary>
        public static System.Func<Vector2, bool> Obstacle;

        /// <summary>The lap the race leader is on (set by <see cref="FjordIce"/>; 1 outside a race). Branches open and the fjord breaks up by it.</summary>
        public static int PhaseLap = 1;
        /// <summary>Race-clock time the fjord started cracking (the icebreaker sails from then).</summary>
        public static float CrackStart = float.PositiveInfinity;
        /// <summary>Race-clock time the fjord broke up (open water from then).</summary>
        public static float BreakStart = float.PositiveInfinity;

        /// <summary>Whether a branch is open on the current lap.</summary>
        public bool BranchOpen(int i) =>
            layout == null || layout.branches == null || i < 0 || i >= layout.branches.Count || layout.branches[i].openFromLap <= PhaseLap;

        bool FjordOn => layout != null && layout.fjord != null && layout.fjord.enabled;

        /// <summary>The fjord is cracked (the panels crumble, the icebreaker sails) / broken up (open water and floes).</summary>
        public bool FjordCracked => FjordOn && Clock >= CrackStart;
        public bool FjordBroken => FjordOn && Clock >= BreakStart;

        const float PanelSink = 0.8f, PanelRefreeze = 3.5f;
        float[] _panelCrack;

        /// <summary>The crumbling panel of the ice road under a point, or -1.</summary>
        public int PanelAt(Vector2 p)
        {
            var f = layout.fjord;
            if (!FjordOn || f.panels == null || f.panels.Length == 0 || f.iceRoad < 0 || f.iceRoad >= Branches.Count) return -1;
            var road = Branches[f.iceRoad];
            road.Project(p, out float along, out float lateral);
            if (Mathf.Abs(lateral) > road.HalfWidth + layout.kerbWidth + 0.5f) return -1;
            for (int i = 0; i < f.panels.Length; i++)
                if (along >= f.panels[i].x && along <= f.panels[i].y) return i;
            return -1;
        }

        /// <summary>0 whole, 1 cracking (about to sink), 2 sunk (open water), for a panel now.</summary>
        public int PanelState(int i)
        {
            if (_panelCrack == null || i < 0 || i >= _panelCrack.Length) return 0;
            float since = Clock - _panelCrack[i];
            return since < 0f ? 0 : since < PanelSink ? 1 : since < PanelSink + PanelRefreeze ? 2 : 0;
        }

        /// <summary>Seconds since the panel last cracked (large when never).</summary>
        public float PanelAge(int i) => _panelCrack == null || i < 0 || i >= _panelCrack.Length ? 1000f : Clock - _panelCrack[i];

        /// <summary>A car drove onto a whole panel while the fjord is cracked: it cracks, and sinks a moment later. True if it cracked now.</summary>
        public bool CrackPanel(int i)
        {
            if (!FjordCracked || FjordBroken || i < 0) return false;
            var f = layout.fjord;
            if (_panelCrack == null || _panelCrack.Length != f.panels.Length)
            {
                _panelCrack = new float[f.panels.Length];
                for (int k = 0; k < _panelCrack.Length; k++) _panelCrack[k] = -1000f;
            }
            if (PanelState(i) != 0) return false;
            _panelCrack[i] = Clock;
            return true;
        }

        /// <summary>Resets the fjord for a new race.</summary>
        public void ResetFjord()
        {
            _panelCrack = null;
            PhaseLap = 1;
            CrackStart = BreakStart = float.PositiveInfinity;
        }

        /// <summary>Where the icebreaker is (0 = shipFrom .. 1 = shipTo) and which way it sails (+1 out, -1 back, 0 waiting).</summary>
        public float ShipAlong(out int direction)
        {
            var f = layout.fjord;
            direction = 0;
            float t = Clock - CrackStart;
            if (!FjordOn || t < 0f || float.IsInfinity(t)) return 0f;
            float travel = Vector2.Distance(f.shipFrom, f.shipTo) / Mathf.Max(0.5f, f.shipSpeed), cycle = 2f * (travel + f.shipWait);
            float c = Mathf.Repeat(t, cycle);
            if (c < travel) { direction = 1; return c / travel; }
            if (c < travel + f.shipWait) return 1f;
            if (c < 2f * travel + f.shipWait) { direction = -1; return 1f - (c - travel - f.shipWait) / travel; }
            return 0f;
        }

        /// <summary>Seconds since the icebreaker last passed this point of its line (0..1), or infinity.</summary>
        public float SinceShipPassed(float u)
        {
            var f = layout.fjord;
            float t = Clock - CrackStart;
            if (!FjordOn || t < 0f || float.IsInfinity(t)) return float.PositiveInfinity;
            float travel = Vector2.Distance(f.shipFrom, f.shipTo) / Mathf.Max(0.5f, f.shipSpeed), cycle = 2f * (travel + f.shipWait);
            float k = Mathf.Floor(t / cycle) * cycle, best = float.PositiveInfinity;
            foreach (float start in new[] { k - cycle, k })
                foreach (float pass in new[] { start + u * travel, start + travel + f.shipWait + (1f - u) * travel })
                    if (pass <= t && pass >= 0f) best = Mathf.Min(best, t - pass);
            return best;
        }

        /// <summary>Open water in the icebreaker's channel at a point (freshly cut, not frozen over yet).</summary>
        public bool InChannel(Vector2 p)
        {
            var f = layout.fjord;
            if (!FjordCracked) return false;
            Vector2 a = f.shipFrom, ab = f.shipTo - f.shipFrom;
            float u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.01f, ab.sqrMagnitude));
            if (Vector2.Distance(p, a + ab * u) > ChannelHalfWidth) return false;
            return SinceShipPassed(u) < f.refreeze;
        }

        public const float ChannelHalfWidth = 3.4f;

        /// <summary>Where a drifting floe is now.</summary>
        public Vector2 FloePosition(int i)
        {
            var floe = layout.fjord.floes[i];
            float a = (Clock / floe.period + floe.phase) * Mathf.PI * 2f;
            return floe.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * floe.drift;
        }

        /// <summary>The floe under a point (once the fjord has broken up), or -1.</summary>
        public int FloeAt(Vector2 p, float shrink = 0.92f)
        {
            if (!FjordBroken || layout.fjord.floes == null) return -1;
            for (int i = 0; i < layout.fjord.floes.Count; i++)
            {
                float r = layout.fjord.floes[i].radius * shrink;
                if ((p - FloePosition(i)).sqrMagnitude < r * r) return i;
            }
            return -1;
        }

        /// <summary>Water of the fjord at a point now: the broken fjord except its floes, the icebreaker's fresh channel, a sunk panel.</summary>
        public bool FjordWater(Vector2 p)
        {
            if (!FjordOn || !layout.fjord.Contains(p)) return false;
            if (FjordBroken) return FloeAt(p) < 0;
            if (!FjordCracked) return false;
            return InChannel(p) || PanelState(PanelAt(p)) == 2;
        }

        /// <summary>Water that will freeze over again soon (the channel, a sunk panel): rivals wait for it.</summary>
        public bool FjordBlocked(Vector2 p) => FjordOn && FjordCracked && layout.fjord.Contains(p)
            && ((p - ShipPosition).sqrMagnitude < 8f * 8f || !FjordBroken && (InChannel(p) || PanelState(PanelAt(p)) == 2));

        /// <summary>Where the icebreaker is now.</summary>
        public Vector2 ShipPosition => Vector2.Lerp(layout.fjord.shipFrom, layout.fjord.shipTo, ShipAlong(out _));

        /// <summary>Distance from a point to the nearest water edge, rivers and lakes (negative inside water).</summary>
        public float WaterDistance(Vector2 p) => Mathf.Min(RiverDistance(p), LakeDistance(p));

        public float RiverDistance(Vector2 p)
        {
            float d = float.MaxValue;
            foreach (var river in layout.rivers)
            {
                var pts = river.points;
                for (int i = 1; i < pts.Length; i++)
                    d = Mathf.Min(d, DistanceToSegment(p, pts[i - 1], pts[i]) - river.width * 0.5f);
            }
            return d;
        }

        public float LakeDistance(Vector2 p)
        {
            float d = float.MaxValue;
            foreach (var lake in layout.lakes)
            {
                Vector2 local = Quaternion.Euler(0f, 0f, -lake.angle) * (p - lake.center);
                float k = new Vector2(local.x / lake.radii.x, local.y / lake.radii.y).magnitude;
                d = Mathf.Min(d, (k - 1f) * Mathf.Min(lake.radii.x, lake.radii.y));
            }
            return d;
        }

        /// <summary>
        /// Water under a point: rivers always cover the road (they are jumped); lakes and the water of a
        /// water track only count off the road, so the road can cross them on a causeway. Gaps in the road
        /// are water too.
        /// </summary>
        public bool IsWater(Vector2 p) => IsWater(p, null);

        /// <summary>
        /// As <see cref="IsWater(Vector2)"/>, measured from <paramref name="near"/> - where a car's own sensor puts it on
        /// the road - instead of the nearest road anywhere (where the road crosses under a jump, the nearest road can be
        /// the jump's gap).
        /// </summary>
        public bool IsWater(Vector2 p, TrackPoint? near)
        {
            if (HoleAt(p)) return true;
            if (FjordOn && FjordWater(p)) return true;
            bool river = RiverDistance(p) < 0f;
            if (river && Jumps != null && _bridges.Count == 0) return true;
            bool lake = LakeDistance(p) < 0f || ChasmAt(p);
            if (!river && !lake && !layout.waterWorld && layout.gaps.Count == 0) return false;
            var point = near ?? Path.Project(p);
            bool roadHere = Mathf.Abs(point.Lateral) <= OffRoadAt(point.Distance) + 0.15f;
            // A river is water everywhere except where a bridge carries the road over it.
            if (river && !(roadHere && OnBridge(point.Distance))) return true;
            bool gap = OpenGapAt(point.Distance) >= 0;
            if (roadHere && !gap) return false;
            return river || lake || layout.waterWorld || gap && roadHere;
        }

        /// <summary>True inside a chasm's outline (whether or not the road crosses there).</summary>
        public bool ChasmAt(Vector2 p)
        {
            if (layout.chasms == null) return false;
            foreach (var chasm in layout.chasms)
                if (chasm.points != null && chasm.points.Length >= 3 && chasm.Contains(p)) return true;
            return false;
        }

        /// <summary>The gummiboat under a point, if any.</summary>
        public TrackLayout.Bouncer BouncerAt(Vector2 p)
        {
            foreach (var b in layout.bouncers)
                if ((p - b.position).sqrMagnitude < b.radius * b.radius) return b;
            return null;
        }

        /// <summary>
        /// Where a jumping fish is at a time: its point over the ground and its height (0..1), or null
        /// while it is under water. It leaps from beside the road on one side to the other side.
        /// </summary>
        public (Vector2 ground, float height, Vector2 direction)? FishAt(TrackLayout.Geyser fish, float time)
        {
            float t = Mathf.Repeat(time + fish.offset, fish.period);
            if (t >= fish.active) return null;
            float u = t / fish.active;
            float reach = OffRoadAt(fish.distance) + 2.2f;
            Vector2 normal = Path.NormalAt(fish.distance) * Mathf.Sign(fish.side);
            Vector2 centre = Path.PointAt(fish.distance);
            Vector2 ground = centre + normal * Mathf.Lerp(reach, -reach, u);
            return (ground, Mathf.Sin(u * Mathf.PI), -normal);
        }

        /// <summary>True if a jumping fish hits a car at this point now.</summary>
        public bool FishHit(Vector2 p, float time, float radius = 1.5f)
        {
            foreach (var fish in layout.geysers)
            {
                var at = FishAt(fish, time);
                if (at.HasValue && (at.Value.ground - p).sqrMagnitude < radius * radius) return true;
            }
            return false;
        }

        /// <summary>True where the road is missing (a gap over water).</summary>
        public bool InGap(float distance)
        {
            foreach (var gap in layout.gaps)
            {
                if (gap.kicker) continue; // a table top: the road carries on under the flight
                float into = Path.DeltaDistance(gap.from, distance);
                if (into >= 0f && into <= Path.DeltaDistance(gap.from, gap.to)) return true;
            }
            return false;
        }

        public Vector2 CowPosition(TrackLayout.Cow cow) => Path.PointAt(cow.distance) + Path.NormalAt(cow.distance) * cow.lateral;

        /// <summary>The cow a car at this point bumps into, or -1.</summary>
        public int CowAt(Vector2 p, float carRadius = 0.9f)
        {
            for (int i = 0; i < layout.cows.Count; i++)
            {
                float r = layout.cows[i].radius + carRadius;
                if ((p - CowPosition(layout.cows[i])).sqrMagnitude < r * r) return i;
            }
            return -1;
        }

        /// <summary>The oil puddle under a point, if any.</summary>
        public bool IsOil(Vector2 p)
        {
            foreach (var spot in layout.oil)
            {
                Vector2 c = Path.PointAt(spot.distance) + Path.NormalAt(spot.distance) * spot.lateral;
                if ((p - c).sqrMagnitude < spot.radius * spot.radius) return true;
            }
            return false;
        }

        /// <summary>
        /// True if an oil puddle is under or just ahead of a car standing on the centre line at this lap
        /// distance (the car is wide and drives off forward, so its whole footprint and the next metres count).
        /// </summary>
        public bool OilNear(float distance, float ahead = 2f, float carHalfWidth = 1.4f)
        {
            foreach (var spot in layout.oil)
            {
                float along = Path.DeltaDistance(distance, spot.distance);
                if (along < -2.5f - spot.radius || along > ahead + spot.radius) continue;
                if (Mathf.Abs(spot.lateral) < carHalfWidth + spot.radius + 0.3f) return true;
            }
            foreach (var cow in layout.cows)
            {
                float along = Path.DeltaDistance(distance, cow.distance);
                if (along < -2.5f - cow.radius || along > ahead + cow.radius) continue;
                if (Mathf.Abs(cow.lateral) < carHalfWidth + cow.radius + 0.3f) return true;
            }
            return false;
        }

        /// <summary>The jump whose ramp contains this lap distance and sideways offset, or null.</summary>
        public Jump? RampAt(float distance, float lateral)
        {
            foreach (var jump in Jumps)
            {
                if (jump.Gap >= 0 && !GapOpen(layout.gaps[jump.Gap])) continue; // a drawbridge that is down
                float into = Path.DeltaDistance(jump.RampStart, distance);
                if (into >= 0f && into <= Path.DeltaDistance(jump.RampStart, jump.Lip) && Mathf.Abs(lateral - jump.Lateral) <= jump.Width * 0.5f)
                    return jump;
            }
            return null;
        }

        List<Jump> FindJumps()
        {
            var jumps = new List<Jump>();
            _bridges = new List<(float from, float to)>();
            var path = Path;
            foreach (var river in layout.rivers)
            {
                int crossing = 0;
                // Walk the centre line; a run of samples inside this river is one crossing.
                bool wasIn = InRiver(river, path[0]);
                float enter = 0f;
                for (int i = 1; i <= path.Count; i++)
                {
                    bool isIn = InRiver(river, path[i]);
                    float d = i == path.Count ? path.Length : path.DistanceAt(i);
                    if (isIn && !wasIn) enter = d;
                    if (!isIn && wasIn)
                    {
                        float lateral = river.rampLaterals is { Length: > 0 } ? river.rampLaterals[Mathf.Min(crossing, river.rampLaterals.Length - 1)] : 0f;
                        if (river.bridged != null && System.Array.IndexOf(river.bridged, crossing) >= 0)
                            _bridges.Add((path.WrapDistance(enter - 2f), path.WrapDistance(d + 2f)));
                        else
                            jumps.Add(new Jump { RampStart = path.WrapDistance(enter - 1f - layout.rampLength), Lip = path.WrapDistance(enter - 1f), FarBank = path.WrapDistance(d + 1f), Lateral = lateral, Gap = -1, Width = layout.rampWidth });
                        crossing++;
                    }
                    wasIn = isIn;
                }
            }
            for (int g = 0; g < layout.gaps.Count; g++)
            {
                var gap = layout.gaps[g];
                if (gap.lift || gap.hidden) continue; // a cable car carries cars across / never raced: no ramp
                // A drawbridge's raised leaf is the ramp itself (over the water, from its hinge).
                // A cliff drop takes off across the whole road.
                jumps.Add(gap.IsDrawbridge
                    ? new Jump { RampStart = path.WrapDistance(gap.from), Lip = path.WrapDistance(gap.from + DrawbridgeRamp), FarBank = path.WrapDistance(gap.to + 1.5f), Lateral = 0f, Gap = g, Width = layout.rampWidth }
                    : new Jump
                    {
                        RampStart = path.WrapDistance(gap.from - layout.rampLength), Lip = path.WrapDistance(gap.from),
                        FarBank = path.WrapDistance(gap.to), Lateral = gap.drop || gap.plunge || (gap.kicker && gap.width <= 0f) ? 0f : gap.rampLateral, Gap = g,
                        Width = gap.kicker && gap.width > 0f ? gap.width : gap.drop || gap.plunge || gap.kicker ? OffRoadAt(gap.from) * 2f : layout.rampWidth,
                    });
            }
            // Forest logs across the road: a little ramp right before each one.
            if (layout.logJumps != null)
                foreach (float log in layout.logJumps)
                    jumps.Add(new Jump
                    {
                        RampStart = path.WrapDistance(log - 1.4f - layout.rampLength), Lip = path.WrapDistance(log - 1.4f),
                        FarBank = path.WrapDistance(log + 1.4f), Lateral = 0f, Gap = -1, Width = layout.rampWidth,
                    });
            jumps.Sort((a, b) => a.RampStart.CompareTo(b.RampStart));
            return jumps;
        }

        static bool InRiver(TrackLayout.River river, Vector2 p)
        {
            for (int i = 1; i < river.points.Length; i++)
                if (DistanceToSegment(p, river.points[i - 1], river.points[i]) < river.width * 0.5f + 0.5f) return true;
            return false;
        }

        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>A place on the road just past the water ahead of a lap distance (to unstick rivals).</summary>
        public Pose RespawnAfterWater(float distance)
        {
            float d = distance;
            foreach (var jump in Jumps)
            {
                float to = Path.DeltaDistance(distance, jump.RampStart);
                if (to > -25f && to < 40f) { d = jump.FarBank + 3f; break; }
            }
            for (int i = 0; i < 40 && (IsWater(Path.PointAt(d)) || InGap(d) || OilNear(d)); i++) d += 2f;
            Vector2 forward = Path.TangentAt(d);
            return new Pose(Path.PointAt(d), Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f));
        }

        /// <summary>
        /// A safe place to put a car back on the road: on the centre line, some way before a lap distance,
        /// not in water or oil, and with enough run-up before the next ramp to reach take-off speed.
        /// </summary>
        public Pose RespawnBefore(float distance, float back = 10f, float runUp = 30f)
        {
            float d = distance - back;
            for (int i = 0; i < 40; i++)
            {
                bool blocked = IsWater(Path.PointAt(d)) || OilNear(d);
                foreach (var jump in Jumps)
                    if (Path.DeltaDistance(jump.RampStart - runUp, d) >= 0f && Path.DeltaDistance(d, jump.FarBank) >= 0f) blocked = true;
                if (!blocked) break;
                d -= 2f;
            }
            Vector2 forward = Path.TangentAt(d);
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;
            return new Pose(Path.PointAt(d), Quaternion.Euler(0f, 0f, angle));
        }

        /// <summary>Grid slot behind the start line, alternating left and right.</summary>
        public Pose GridSlot(int index, float rowSpacing = 2.6f, float firstRowOffset = 3f)
        {
            float distance = -(firstRowOffset + index * rowSpacing);
            float lateral = (index % 2 == 0 ? 1f : -1f) * layout.roadWidth * 0.22f;
            Vector2 position = Path.PointAt(distance) + Path.NormalAt(distance) * lateral;
            Vector2 forward = Path.TangentAt(distance);
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;
            return new Pose(position, Quaternion.Euler(0f, 0f, angle));
        }

        /// <summary>Switches to another layout at runtime (level loading) and rebuilds everything derived from it.</summary>
        public void SetLayout(TrackLayout value)
        {
            if (value == null || value == layout) return;
            if (layout != null) layout.Changed -= Invalidate;
            layout = value;
            if (isActiveAndEnabled) layout.Changed += Invalidate;
            Invalidate();
        }

        void OnEnable()
        {
            if (layout != null) layout.Changed += Invalidate;
        }

        void OnDisable()
        {
            if (layout != null) layout.Changed -= Invalidate;
        }

        void OnValidate() => Invalidate();

        void Invalidate()
        {
            _path = null;
            _jumps = null;
            _sectionCheckpoints = null;
            _shortcuts = null;
            _branches = null;
            Rebuilt?.Invoke();
        }

        TrackPath Build()
        {
            if (layout == null || layout.waypoints == null || layout.waypoints.Length < 3)
                throw new InvalidOperationException($"{name}: Track needs a TrackLayout with at least 3 waypoints.");
            return new TrackPath(layout.waypoints, layout.sampleSpacing);
        }

        void OnDrawGizmosSelected()
        {
            if (layout == null) return;
            var path = Path;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < path.Count; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);

            Gizmos.color = Color.cyan;
            for (int k = 0; k < layout.checkpointCount; k++)
            {
                float d = CheckpointDistance(k);
                Vector2 p = path.PointAt(d), n = path.NormalAt(d) * OffRoadAt(d);
                Gizmos.DrawLine(p - n, p + n);
            }

            Gizmos.color = Color.magenta;
            foreach (var w in layout.waypoints)
                Gizmos.DrawWireSphere(w, 0.6f);
        }
    }
}
