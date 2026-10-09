using SomeGame.Car;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Input
{
    /// <summary>
    /// AI driver: steers at a point ahead on the track centre line (plus its own lane offset) and
    /// lifts off the throttle when a sharp corner is coming. Speed differences between rivals come
    /// from their <see cref="CarStats"/> assets.
    /// </summary>
    [RequireComponent(typeof(TrackSensor), typeof(CarMovement))]
    public class AIDriverInput : MonoBehaviour, ICarInput
    {
        [Tooltip("Preferred sideways offset from the centre line (positive = left).")]
        [SerializeField] float laneOffset;
        [Tooltip("Steering target distance ahead: base + speed * factor.")]
        [SerializeField, Min(0f)] float lookAheadBase = 4f;
        [SerializeField, Min(0f)] float lookAheadPerSpeed = 0.3f;
        [Tooltip("How far ahead to look for corners when deciding to lift off.")]
        [SerializeField, Min(1f)] float cornerScanDistance = 26f;
        [Tooltip("Speed the AI aims for in the tightest corners, as a fraction of its top speed.")]
        [SerializeField, Range(0.1f, 1f)] float tightCornerSpeedFactor = 0.5f;
        [Tooltip("Turn angle (degrees) over the scan distance that counts as the tightest corner.")]
        [SerializeField, Min(1f)] float tightCornerAngle = 150f;

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
            Vector2 target = path.PointAt(ahead) + path.NormalAt(ahead) * laneOffset;
            SteerDirection = (target - _car.Body.position).normalized;

            // Biggest change of direction between here and the scan distance ahead.
            Vector2 now = path.TangentAt(distance);
            float sharpest = 0f;
            for (float d = 4f; d <= cornerScanDistance; d += 4f)
                sharpest = Mathf.Max(sharpest, Vector2.Angle(now, path.TangentAt(distance + d)));

            float topSpeed = _car.Stats.topSpeed;
            float allowed = Mathf.Lerp(topSpeed, topSpeed * tightCornerSpeedFactor, sharpest / tightCornerAngle);
            Throttle = speed < allowed ? 1f : 0f;
        }
    }
}
