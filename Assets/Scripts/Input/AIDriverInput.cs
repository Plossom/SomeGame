using SomeGame.Car;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Input
{
    /// <summary>
    /// AI driver: steers at a point ahead on the track centre line (plus its own lane offset) and
    /// brakes for corners: from the curvature of the track ahead it works out the fastest speed it
    /// can still slow down from in time. Speed differences between rivals come from their
    /// <see cref="CarStats"/> assets and <see cref="cornerGrip"/>.
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

        TrackSensor _sensor;
        CarMovement _car;

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

        void Awake()
        {
            _sensor = GetComponent<TrackSensor>();
            _car = GetComponent<CarMovement>();
        }

        void Update()
        {
            if (_sensor.Track == null) return;
            var path = _sensor.Track.Path;
            float distance = _sensor.Current.Distance;
            float speed = Mathf.Max(0f, _car.ForwardSpeed);

            float ahead = distance + lookAheadBase + speed * lookAheadPerSpeed;
            Vector2 aim = path.PointAt(ahead) + path.NormalAt(ahead) * LateralFor(path, distance);
            SteerDirection = (aim - _car.Body.position).normalized;

            float targetSpeed = TargetSpeed(path, distance);
            Throttle = speed < targetSpeed - 0.3f ? 1f : speed > targetSpeed + 1.5f ? -1f : 0f;
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
                float cornerSpeed = Mathf.Sqrt(cornerGrip / curvature);
                target = Mathf.Min(target, Mathf.Sqrt(cornerSpeed * cornerSpeed + 2f * brake * d));
            }
            return target;
        }
    }
}
