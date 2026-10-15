using SomeGame.Car;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Input
{
    /// <summary>
    /// AI driver: steers at a point ahead on the track centre line (plus its own lane offset) and
    /// brakes for corners: from the curvature of the track ahead it works out the fastest speed it
    /// can still slow down from in time. Speed differences between rivals come from their
    /// <see cref="CarStats"/> assets and <see cref="cornerGrip"/>. On tracks with other ways round each
    /// rival has a taste for adventure: lap by lap it may take a branch (ice, rooftops via the jump pad)
    /// or ride a banked wall; otherwise it keeps to the road and steers round the jump pad.
    /// </summary>
    [RequireComponent(typeof(TrackSensor), typeof(CarMovement))]
    public class AIDriverInput : MonoBehaviour, ICarInput
    {
        [Tooltip("Preferred sideways offset from the centre line (positive = left).")]
        [SerializeField] float laneOffset;
        [Tooltip("Steering target distance ahead: base + speed * factor.")]
        [SerializeField, Min(0f)] float lookAheadBase = 4f;
        [SerializeField, Min(0f)] float lookAheadPerSpeed = 0.3f;
        [Tooltip("Sideways acceleration the AI trusts in corners (units/s²). Higher = faster, riskier cornering.")]
        [SerializeField, Min(1f)] float cornerGrip = 24f;
        [Tooltip("How far ahead to read the track for corners (units).")]
        [SerializeField, Min(1f)] float cornerScanDistance = 45f;
        [Tooltip("Fraction of the car's brake power the AI plans with (leaves a safety margin).")]
        [SerializeField, Range(0.1f, 1f)] float brakeMargin = 0.75f;

        [Tooltip("Distance before a jump at which the car is lined up with the kicker ramp.")]
        [SerializeField, Min(1f)] float rampLineUp = 20f;
        [Tooltip("Distance ahead at which the car starts steering around an oil puddle.")]
        [SerializeField, Min(1f)] float oilLookAhead = 22f;

        [Tooltip("How often this rival takes another way round or rides a wall (0 = never, 1 = always).")]
        [SerializeField, Range(0f, 1f)] float adventurous = 0.5f;
        [Tooltip("Speed the AI allows itself on ice (it slides).")]
        [SerializeField, Min(1f)] float iceSpeed = 15f;

        TrackSensor _sensor;
        CarMovement _car;
        SomeGame.Race.RaceProgress _progress;
        int _seed;

        public Vector2 SteerDirection { get; private set; }
        public float Throttle { get; private set; }
        public bool DriftHeld => false;

        public float CornerGrip
        {
            get => cornerGrip;
            set => cornerGrip = value;
        }

        public float LaneOffset
        {
            get => laneOffset;
            set => laneOffset = value;
        }

        public float Adventurous
        {
            get => adventurous;
            set => adventurous = Mathf.Clamp01(value);
        }

        /// <summary>Makes this rival's route choices its own (rivals with the same taste still differ).</summary>
        public int Seed
        {
            get => _seed;
            set => _seed = value;
        }

        void Awake()
        {
            _sensor = GetComponent<TrackSensor>();
            _car = GetComponent<CarMovement>();
            _progress = GetComponent<SomeGame.Race.RaceProgress>();
        }

        // Whether this rival goes for choice <paramref name="what"/> (a branch, a wall) on this lap.
        bool Takes(int what)
        {
            var track = _sensor != null ? _sensor.Track : null;
            if (track != null && track.ClosedBranch == what) return false; // the rockslide closed it
            if (track != null && track.ForcedBranch == what) return true;  // the rockslide closed the road beside it
            int lap = _progress != null ? _progress.CurrentLap : 1;
            uint h = (uint)(_seed * 73856093 ^ (what + 1) * 19349663 ^ lap * 83492791);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0xFFFF) / 65535f < adventurous;
        }

        void Update()
        {
            if (_sensor.Track == null) return;
            var path = _sensor.Track.Path;
            float distance = _sensor.Current.Distance;
            float speed = Mathf.Max(0f, _car.ForwardSpeed);

            float look = lookAheadBase + speed * lookAheadPerSpeed;
            float ahead = distance + look;
            Vector2 aim = path.PointAt(ahead) + path.NormalAt(ahead) * LateralFor(path, distance);
            float targetSpeed = TargetSpeed(path, distance);

            var track = _sensor.Track;
            if (_sensor.Branch >= 0)
            {
                // On a branch: follow it, then the road after it rejoins.
                var route = track.Branches[_sensor.Branch];
                float a = _sensor.BranchAlong + look;
                aim = a <= route.Length ? route.PointAt(a) : path.PointAt(route.To + a - route.Length);
                if (route.Kind == TrackLayout.BranchKind.Ice) targetSpeed = Mathf.Min(targetSpeed, iceSpeed);
            }
            else if (!_sensor.Elevated)
            {
                // A branch coming up that this rival takes this lap: head into it (rooftops: onto the jump pad).
                for (int i = 0; i < track.Branches.Count; i++)
                {
                    var route = track.Branches[i];
                    float to = path.DeltaDistance(distance, route.From);
                    if (!Takes(i)) continue;
                    if (route.Kind == TrackLayout.BranchKind.Roof)
                    {
                        if (to < -2f || to > 30f) continue;
                        var pad = PadFor(route);
                        if (pad != null) aim = pad.position;
                        break;
                    }
                    if (to < -30f || to > 30f) continue;
                    if (to > 0f)
                    {
                        if (to < look) aim = route.PointAt(look - to);
                    }
                    else
                    {
                        // Just past the turn-off, still close to the road: keep following the branch.
                        route.Project(_car.Body.position, out float along, out float off);
                        if (Mathf.Abs(off) > route.HalfWidth + 3f) continue; // missed it: stay on the road
                        aim = route.PointAt(along + look);
                    }
                    break;
                }
            }
            SteerDirection = (aim - _car.Body.position).normalized;
            Throttle = speed < targetSpeed - 0.3f ? 1f : speed > targetSpeed + 1.5f ? -1f : 0f;
        }

        // The jump pad that throws cars onto a row of roofs (the pad aimed at its first roof).
        TrackLayout.Bouncer PadFor(BranchRoute roof)
        {
            TrackLayout.Bouncer best = null;
            float bestSqr = 64f;
            foreach (var pad in _sensor.Track.Layout.bouncers)
            {
                if (!pad.HasTarget) continue;
                float sqr = (pad.target - roof.Points[0]).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = pad; }
            }
            return best;
        }

        // A banked wall this rival rides this lap, if the lap distance is on (or just before) one.
        TrackLayout.Bank RiddenBank(TrackPath path, float distance, float before)
        {
            var banks = _sensor.Track.Layout.banks;
            for (int i = 0; i < banks.Count; i++)
                if (path.DeltaDistance(banks[i].from - before, distance) >= 0f && path.DeltaDistance(distance, banks[i].to) >= 0f && Takes(1000 + i))
                    return banks[i];
            return null;
        }

        // The car's own lane, except before a jump (it lines up with the kicker ramp) and near oil (it goes round).
        float LateralFor(TrackPath path, float distance)
        {
            // On a water track the edge of the road is the water: stay nearer the middle.
            // Narrow road: shrink the lane with it.
            var track = _sensor.Track;
            float narrow = Mathf.Clamp01(track.HalfWidthAt(distance + 6f) / track.Layout.HalfWidth);
            float lane = (track.Layout.waterWorld ? laneOffset * 0.4f : laneOffset) * narrow;
            float lateral = lane;
            foreach (var jump in _sensor.Track.Jumps)
            {
                float toLip = path.DeltaDistance(distance, jump.Lip);
                if (toLip < -1f || toLip > rampLineUp * 1.6f) continue;
                float blend = Mathf.InverseLerp(rampLineUp * 1.6f, rampLineUp, toLip);
                lateral = Mathf.Lerp(lane, jump.Lateral, blend);
            }
            // Ride a banked wall: up on it, from a little before it starts.
            var bank = RiddenBank(path, distance, 12f);
            if (bank != null)
            {
                float into = Mathf.InverseLerp(bank.from - 12f, bank.from, distance);
                lateral = Mathf.Lerp(lateral, bank.side * (track.OffRoadAt(distance) + 1.8f), Mathf.Clamp01(into));
            }
            // Keep off a jump pad unless taking the rooftops this lap.
            for (int i = 0; i < track.Branches.Count; i++)
            {
                var route = track.Branches[i];
                if (route.Kind != TrackLayout.BranchKind.Roof || Takes(i)) continue;
                var pad = PadFor(route);
                if (pad == null) continue;
                var at = path.Project(pad.position);
                float to = path.DeltaDistance(distance, at.Distance);
                if (to < -1f || to > oilLookAhead) continue;
                float clear = pad.radius + 1.6f;
                if (Mathf.Abs(lateral - at.Lateral) >= clear) continue;
                float limit = track.HalfWidthAt(at.Distance) - 0.9f;
                float dodge = Mathf.Clamp(at.Lateral > 0f ? at.Lateral - clear : at.Lateral + clear, -limit, limit);
                lateral = Mathf.Lerp(lateral, dodge, Mathf.InverseLerp(oilLookAhead, oilLookAhead * 0.4f, to));
            }
            // Steer around oil puddles ahead: pass on whichever side needs the smaller move.
            var layout = _sensor.Track.Layout;
            foreach (var spot in layout.oil)
            {
                float to = path.DeltaDistance(distance, spot.distance);
                if (to < -1f || to > oilLookAhead) continue;
                float clear = spot.radius + 1.5f;
                if (Mathf.Abs(lateral - spot.lateral) >= clear) continue;
                float limit = _sensor.Track.HalfWidthAt(spot.distance) - 0.9f;
                float left = spot.lateral + clear, right = spot.lateral - clear;
                float dodge = left > limit ? right : right < -limit ? left : (Mathf.Abs(left - lateral) < Mathf.Abs(right - lateral) ? left : right);
                lateral = Mathf.Lerp(lateral, Mathf.Clamp(dodge, -limit, limit), Mathf.InverseLerp(oilLookAhead, oilLookAhead * 0.4f, to));
            }
            return lateral;
        }

        /// <summary>Fastest speed from which every corner in the scan range can still be taken.</summary>
        float TargetSpeed(TrackPath path, float distance)
        {
            const float window = 6f; // track length over which curvature is measured
            float brake = _car.Stats.brakeDeceleration * brakeMargin;
            float target = _car.Stats.topSpeed;
            for (float d = 0f; d <= cornerScanDistance; d += 2f)
            {
                float at = distance + d;
                float turn = Vector2.Angle(path.TangentAt(at - window * 0.5f), path.TangentAt(at + window * 0.5f)) * Mathf.Deg2Rad;
                float curvature = turn / window;
                if (curvature < 0.002f) continue;
                // On a ridden wall the corner holds much more speed.
                float grip = RiddenBank(path, at, 0f) != null ? cornerGrip * _car.Stats.wallGrip : cornerGrip;
                float cornerSpeed = Mathf.Sqrt(grip / curvature);
                target = Mathf.Min(target, Mathf.Sqrt(cornerSpeed * cornerSpeed + 2f * brake * d));
            }
            return target;
        }
    }
}
