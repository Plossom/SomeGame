using System;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Arcade top-down car handling on a Rigidbody2D, shared by the player and AI cars.
    /// Reads an <see cref="ICarInput"/> on the same GameObject and all tuning from <see cref="CarStats"/>.
    /// The sprite faces +Y (transform.up is forward).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CarMovement : MonoBehaviour
    {
        [SerializeField] CarStats stats;
        [Tooltip("Optional. Without a sensor the car never counts as off-road.")]
        [SerializeField] TrackSensor sensor;

        Rigidbody2D _body;
        ICarInput _input;
        float _hitTimer;

        /// <summary>Raised on every collision with the closing speed of the impact.</summary>
        public event Action<float> Collided;

        public CarStats Stats
        {
            get => stats;
            set { stats = value; ApplyStats(); }
        }

        /// <summary>When false the car ignores its input and coasts (countdown, finished).</summary>
        public bool ControlsEnabled { get; set; } = true;

        public Rigidbody2D Body => _body;
        public float ForwardSpeed { get; private set; }
        public float SidewaysSpeed { get; private set; }
        public bool IsSliding => Mathf.Abs(SidewaysSpeed) > stats.slideThreshold;
        public bool IsOffRoad => sensor != null && sensor.IsOffRoad;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _input = GetComponent<ICarInput>();
            if (sensor == null) sensor = GetComponent<TrackSensor>();
            ApplyStats();
        }

        void ApplyStats()
        {
            if (_body == null || stats == null) return;
            _body.mass = stats.mass;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 forward = transform.up, right = transform.right;
            Vector2 velocity = _body.linearVelocity;
            float forwardSpeed = Vector2.Dot(velocity, forward);
            float sidewaysSpeed = Vector2.Dot(velocity, right);

            bool offRoad = IsOffRoad;
            bool hasControl = ControlsEnabled && _input != null;
            float throttle = hasControl ? Mathf.Clamp01(_input.Throttle) : 0f;
            Vector2 steer = hasControl ? _input.SteerDirection : Vector2.zero;
            _hitTimer = Mathf.Max(0f, _hitTimer - dt);

            Steer(steer, forwardSpeed, dt);

            // Longitudinal: accelerate up to the surface's top speed, brake hard above it, coast otherwise.
            float maxSpeed = stats.topSpeed * (offRoad ? stats.offRoadSlowdown : 1f);
            if (forwardSpeed > maxSpeed)
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, maxSpeed, (offRoad ? stats.offRoadDeceleration : stats.drag) * dt);
            else if (throttle > 0f)
                forwardSpeed = Mathf.Min(maxSpeed, forwardSpeed + stats.acceleration * throttle * dt);
            else
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, stats.drag * (offRoad ? 2f : 1f) * dt);

            // Lateral: grip bleeds off sideways velocity; less grip at speed lets the car slide in fast corners.
            float speedRatio = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.01f, stats.topSpeed));
            float grip = stats.grip * (1f - stats.highSpeedGripLoss * speedRatio) * (offRoad ? stats.offRoadGrip : 1f);
            sidewaysSpeed *= Mathf.Exp(-grip * dt);

            _body.linearVelocity = forward * forwardSpeed + right * sidewaysSpeed;
            ForwardSpeed = forwardSpeed;
            SidewaysSpeed = sidewaysSpeed;
        }

        void Steer(Vector2 direction, float forwardSpeed, float dt)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                // No steering input: let collision spin die out instead of snapping straight.
                _body.angularVelocity = Mathf.MoveTowards(_body.angularVelocity, 0f, stats.turnRate * 4f * dt);
                return;
            }

            float target = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            float delta = Mathf.DeltaAngle(_body.rotation, target);
            float speedFactor = Mathf.Lerp(stats.standstillTurnFactor, 1f,
                Mathf.Clamp01(Mathf.Abs(forwardSpeed) / stats.fullTurnSpeed));
            float recovery = _hitTimer > 0f ? 0.3f : 1f;
            float maxStep = stats.turnRate * speedFactor * recovery * dt;
            _body.angularVelocity = Mathf.Clamp(delta, -maxStep, maxStep) / dt;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            float impact = collision.relativeVelocity.magnitude;
            Collided?.Invoke(impact);

            if (collision.rigidbody == null || !collision.rigidbody.TryGetComponent(out CarMovement other)) return;
            Vector2 toOther = (other._body.position - _body.position).normalized;
            float closingSpeed = Vector2.Dot(_body.linearVelocity - other._body.linearVelocity, toOther);
            if (closingSpeed > 1f)
                other.TakeHit(toOther * (closingSpeed * stats.ramForce * other._body.mass));
        }

        void TakeHit(Vector2 impulse)
        {
            _body.AddForce(impulse, ForceMode2D.Impulse);
            _hitTimer = stats.hitRecoveryTime;
        }
    }
}
