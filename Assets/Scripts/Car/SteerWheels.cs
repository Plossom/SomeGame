using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Turns the front wheels (or a snowmobile's skis) with the steering, always into the turn (drifts
    /// too). The player's car follows the joystick; the rivals (whose AI steers in small steps) and online
    /// cars turn them by how fast they turn.
    /// </summary>
    public class SteerWheels : MonoBehaviour
    {
        [Tooltip("The front wheels (child sprites), turned about their own centre.")]
        [SerializeField] Transform[] wheels;
        [SerializeField, Range(0f, 60f)] float maxAngle = 32f;
        [Tooltip("Wheel angle per degree between the heading and where the player steers.")]
        [SerializeField, Min(0f)] float steerGain = 0.6f;
        [Tooltip("Wheel angle (degrees) per degree per second the car turns (rivals and online cars).")]
        [SerializeField, Min(0f)] float turnGain = 0.4f;
        [Tooltip("Seconds the wheels take to follow.")]
        [SerializeField, Min(0.01f)] float smoothTime = 0.1f;

        CarMovement _car;
        ICarInput _input;
        float _lastHeading, _angle, _velocity;
        bool _started;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _input = GetComponent<ICarInput>();
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || wheels == null) return;
            float heading = transform.eulerAngles.z;
            if (!_started)
            {
                _lastHeading = heading;
                _started = true;
            }
            float yawRate = Mathf.DeltaAngle(_lastHeading, heading) / dt;
            _lastHeading = heading;

            float target = yawRate * turnGain;
            bool driven = _input is SomeGame.Input.PlayerCarInput && _car != null && !_car.IsPuppet && _car.ControlsEnabled;
            if (driven)
            {
                Vector2 steer = _input.SteerDirection;
                target = steer.sqrMagnitude > 0.0001f
                    ? Mathf.DeltaAngle(heading, Mathf.Atan2(steer.y, steer.x) * Mathf.Rad2Deg - 90f) * steerGain
                    : 0f;
            }
            target = Mathf.Clamp(target, -maxAngle, maxAngle);
            _angle = Mathf.SmoothDamp(_angle, target, ref _velocity, smoothTime, Mathf.Infinity, dt);
            var rotation = Quaternion.Euler(0f, 0f, _angle);
            foreach (var wheel in wheels)
                if (wheel != null) wheel.localRotation = rotation;
        }
    }
}
