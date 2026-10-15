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

        /// <summary>The branch a rockslide has closed this race (-1 = none).</summary>
        public int ClosedBranch { get; set; } = -1;
        /// <summary>The branch everyone must take this race, because the rockslide closed the road beside it (-1 = none).</summary>
        public int ForcedBranch { get; set; } = -1;

        /// <summary>
        /// Picks this race's surprises (which way the rockslide closes, the train timetable). Set by the race
        /// before the track starts; online every phone gets the same number, so all see the same race.
        /// </summary>
        public static int SurpriseSeed { get; set; }
        /// <summary>Sections of a journey track (1 for a circuit raced in laps).</summary>
        public int Sections => layout.sections > 1 && layout.checkpointCount % layout.sections == 0 ? layout.sections : 1;
        /// <summary>Checkpoints from one lap line (or section line) to the next.</summary>
        public int CheckpointsPerLap => CheckpointCount / Sections;

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
            float shift = line.kind == TrackLayout.CrossingKind.Sheep ? SeedShift(line.distance) * line.period : 0f;
            float t = Mathf.Repeat(time + line.offset + shift, line.period), half = line.period * 0.5f;
            bool back = t >= half;
            float walk = Mathf.Clamp01((back ? t - half : t) / line.crossing);
            float reach = OffRoadAt(line.distance) + 2.5f;
            Vector2 centre = Path.PointAt(line.distance), across = Path.NormalAt(line.distance) * (back ? 1f : -1f);
            const float spacing = 1.6f;
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
                foreach (var (position, _) in _penguinScratch)
                    if ((position - p).sqrMagnitude < radius * radius) return true;
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
        public int BranchAt(Vector2 position, out float along, out float lateral, bool roofs = true, bool ground = true)
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
                branches[i].Project(position, out float a, out float l);
                if (Mathf.Abs(l) > branches[i].HalfWidth + layout.kerbWidth || Mathf.Abs(l) >= best) continue;
                best = Mathf.Abs(l);
                found = i;
                along = a;
                lateral = l;
            }
            return found;
        }

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
        public bool IsWater(Vector2 p)
        {
            if (RiverDistance(p) < 0f) return true;
            bool lake = LakeDistance(p) < 0f;
            if (!lake && !layout.waterWorld && layout.gaps.Count == 0) return false;
            var point = Path.Project(p);
            bool onRoad = Mathf.Abs(point.Lateral) <= OffRoadAt(point.Distance) + 0.15f && !InGap(point.Distance);
            if (onRoad) return false;
            return lake || layout.waterWorld || InGap(point.Distance) && Mathf.Abs(point.Lateral) <= OffRoadAt(point.Distance) + 0.15f;
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
                float into = Path.DeltaDistance(gap.from, distance);
                if (into >= 0f && into <= Path.DeltaDistance(gap.from, gap.to)) return true;
            }
            return false;
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
            return false;
        }

        /// <summary>The jump whose ramp contains this lap distance and sideways offset, or null.</summary>
        public Jump? RampAt(float distance, float lateral)
        {
            foreach (var jump in Jumps)
            {
                float into = Path.DeltaDistance(jump.RampStart, distance);
                if (into >= 0f && into <= jump.Lip - jump.RampStart && Mathf.Abs(lateral - jump.Lateral) <= layout.rampWidth * 0.5f)
                    return jump;
            }
            return null;
        }

        List<Jump> FindJumps()
        {
            var jumps = new List<Jump>();
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
                        jumps.Add(new Jump { RampStart = path.WrapDistance(enter - 1f - layout.rampLength), Lip = path.WrapDistance(enter - 1f), FarBank = path.WrapDistance(d + 1f), Lateral = lateral });
                        crossing++;
                    }
                    wasIn = isIn;
                }
            }
            foreach (var gap in layout.gaps)
                jumps.Add(new Jump
                {
                    RampStart = path.WrapDistance(gap.from - layout.rampLength), Lip = path.WrapDistance(gap.from),
                    FarBank = path.WrapDistance(gap.to), Lateral = gap.rampLateral,
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

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
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
