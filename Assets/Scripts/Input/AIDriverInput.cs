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

        TrackSensor _sensor;
        CarMovement _car;

        public Vector2 SteerDirection { get; private set; }
        public float Throttle { get; private set; }
        public bool DriftHeld => false;

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
            Vector2 aim = path.PointAt(ahead) + path.NormalAt(ahead) * laneOffset;
            SteerDirection = (aim - _car.Body.position).normalized;

            float targetSpeed = TargetSpeed(path, distance);
            Throttle = speed < targetSpeed - 0.3f ? 1f : speed > targetSpeed + 1.5f ? -1f : 0f;
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
