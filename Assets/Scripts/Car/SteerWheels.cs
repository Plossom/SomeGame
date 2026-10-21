using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Turns the front wheels (or a snowmobile's skis) with the steering, always into the turn (drifts
    /// too). Read from how fast the car turns, so it works the same for the player, the rivals and online
    /// puppets.
    /// </summary>
    public class SteerWheels : MonoBehaviour
    {
        [Tooltip("The front wheels (child sprites), turned about their own centre.")]
        [SerializeField] Transform[] wheels;
        [SerializeField, Range(0f, 60f)] float maxAngle = 30f;
        [Tooltip("Wheel angle (degrees) per degree per second the car turns.")]
        [SerializeField, Min(0f)] float turnGain = 0.22f;
        [Tooltip("Seconds the wheels take to follow.")]
        [SerializeField, Min(0.01f)] float smoothTime = 0.06f;

        float _lastHeading, _angle, _velocity;
        bool _started;

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
            target = Mathf.Clamp(target, -maxAngle, maxAngle);
            _angle = Mathf.SmoothDamp(_angle, target, ref _velocity, smoothTime, Mathf.Infinity, dt);
            var rotation = Quaternion.Euler(0f, 0f, _angle);
            foreach (var wheel in wheels)
                if (wheel != null) wheel.localRotation = rotation;
        }
    }
}
