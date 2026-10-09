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

        public float CheckpointDistance(int index) =>
            Mathf.Repeat(index, layout.checkpointCount) * Path.Length / layout.checkpointCount;

        public bool IsOffRoad(float lateral) => Mathf.Abs(lateral) > layout.OffRoadDistance;

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

        /// <summary>Every river crossing, in lap order (derived from the rivers and the centre line).</summary>
        public IReadOnlyList<Jump> Jumps => _jumps ??= FindJumps();

        /// <summary>Distance from a point to the nearest water edge (negative inside water).</summary>
        public float WaterDistance(Vector2 p)
        {
            float d = float.MaxValue;
            foreach (var river in layout.rivers)
            {
                var pts = river.points;
                for (int i = 1; i < pts.Length; i++)
                    d = Mathf.Min(d, DistanceToSegment(p, pts[i - 1], pts[i]) - river.width * 0.5f);
            }
            foreach (var lake in layout.lakes)
            {
                Vector2 local = Quaternion.Euler(0f, 0f, -lake.angle) * (p - lake.center);
                float k = new Vector2(local.x / lake.radii.x, local.y / lake.radii.y).magnitude;
                d = Mathf.Min(d, (k - 1f) * Mathf.Min(lake.radii.x, lake.radii.y));
            }
            return d;
        }

        public bool IsWater(Vector2 p) => WaterDistance(p) < 0f;

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

        /// <summary>A safe place to put a car back on the road: on the centre line, some way before a lap distance.</summary>
        public Pose RespawnBefore(float distance, float back = 8f)
        {
            float d = distance - back;
            // Not on a ramp or in water: keep stepping back.
            for (int i = 0; i < 20; i++)
            {
                bool blocked = IsWater(Path.PointAt(d)) || IsOil(Path.PointAt(d));
                foreach (var jump in Jumps)
                    if (Path.DeltaDistance(jump.RampStart - 3f, d) >= 0f && Path.DeltaDistance(d, jump.FarBank) >= 0f) blocked = true;
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
                Vector2 p = path.PointAt(d), n = path.NormalAt(d) * layout.OffRoadDistance;
                Gizmos.DrawLine(p - n, p + n);
            }

            Gizmos.color = Color.magenta;
            foreach (var w in layout.waypoints)
                Gizmos.DrawWireSphere(w, 0.6f);
        }
    }
}
