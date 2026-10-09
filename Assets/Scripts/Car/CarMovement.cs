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
        int _driftDirection;   // +1 drifting left (counter-clockwise), -1 right, 0 not drifting
        float _driftCharge;    // seconds spent in the current drift
        float _boostTimer, _boostDuration;

        /// <summary>Raised on every collision with the closing speed of the impact.</summary>
        public event Action<float> Collided;
        public event Action DriftStarted;
        /// <summary>A drift was released with enough charge; argument is boost strength 0..1.</summary>
        public event Action<float> BoostStarted;

        public CarStats Stats
        {
            get => stats;
            set { stats = value; ApplyStats(); }
        }

        /// <summary>When false the car ignores its input and coasts (countdown, finished).</summary>
        public bool ControlsEnabled { get; set; } = true;

        // Resolved lazily: other scripts (RaceManager) may use it before this Awake has run,
        // and Awake order between objects differs between the Editor and device builds.
        public Rigidbody2D Body => _body != null ? _body : _body = GetComponent<Rigidbody2D>();
        public float ForwardSpeed { get; private set; }
        public float SidewaysSpeed { get; private set; }
        public bool IsSliding => Mathf.Abs(SidewaysSpeed) > stats.slideThreshold;
        public bool IsOffRoad => sensor != null && sensor.IsOffRoad;

        public bool IsDrifting => _driftDirection != 0;
        /// <summary>+1 drifting to the left, -1 to the right, 0 when not drifting.</summary>
        public int DriftDirection => _driftDirection;
        /// <summary>
        /// Charge level of the current drift: 0 = no boost yet, 1 = weak, 2 = medium, 3 = full.
        /// </summary>
        public int DriftTier
        {
            get
            {
                if (!IsDrifting || _driftCharge < stats.driftMinCharge) return 0;
                if (_driftCharge >= stats.driftMaxCharge) return 3;
                return _driftCharge >= (stats.driftMinCharge + stats.driftMaxCharge) * 0.5f ? 2 : 1;
            }
        }
        public bool IsBoosting => _boostTimer > 0f;
        /// <summary>Strength (0..1) of the boost currently running.</summary>
        public float BoostStrength { get; private set; }

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
            bool driftHeld = hasControl && _input.DriftHeld;
            _hitTimer = Mathf.Max(0f, _hitTimer - dt);

            forwardSpeed += UpdateDrift(driftHeld, steer, throttle, forwardSpeed, dt);

            // Boost: raises top speed and acceleration, fading out over the last third.
            float boost = 0f;
            if (IsBoosting)
            {
                _boostTimer -= dt;
                boost = Mathf.Clamp01(_boostTimer / (_boostDuration * 0.33f));
                throttle = 1f;
            }
            float boostBonus = Mathf.Lerp(stats.boostMinSpeedBonus, stats.boostMaxSpeedBonus, BoostStrength) * boost;

            // Longitudinal: accelerate up to the surface's top speed, brake hard above it, coast otherwise.
            float maxSpeed = stats.topSpeed * (offRoad ? stats.offRoadSlowdown : 1f)
                           * (IsDrifting ? stats.driftSpeedFactor : 1f) * (1f + boostBonus);
            float acceleration = stats.acceleration + stats.boostAcceleration * boost;

            if (IsDrifting)
            {
                DriftMotion(steer, throttle, maxSpeed, acceleration, offRoad, dt);
                return;
            }

            Steer(steer, forwardSpeed, dt);
            if (forwardSpeed > maxSpeed)
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, maxSpeed, (offRoad ? stats.offRoadDeceleration : stats.drag) * dt);
            else if (throttle > 0f)
                forwardSpeed = Mathf.Min(maxSpeed, forwardSpeed + acceleration * throttle * dt);
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

            float delta = Mathf.DeltaAngle(_body.rotation, HeadingOf(direction));
            float speedFactor = Mathf.Lerp(stats.standstillTurnFactor, 1f,
                Mathf.Clamp01(Mathf.Abs(forwardSpeed) / stats.fullTurnSpeed));
            float recovery = _hitTimer > 0f ? 0.3f : 1f;
            float maxStep = stats.turnRate * speedFactor * recovery * dt;
            _body.angularVelocity = Mathf.Clamp(delta, -maxStep, maxStep) / dt;
        }

        /// <summary>
        /// Starts, charges and ends drifts. Returns instant forward speed to add (boost kick).
        /// </summary>
        float UpdateDrift(bool held, Vector2 steer, float throttle, float forwardSpeed, float dt)
        {
            if (IsDrifting)
            {
                _driftCharge += dt;
                if (!ControlsEnabled || throttle <= 0f || forwardSpeed < stats.driftMinSpeed * 0.5f)
                    EndDrift();                 // lifted the steering thumb or stalled: no boost
                else if (!held)
                    return ReleaseDrift();      // let go of the drift finger: boost
                return 0f;
            }

            if (!held || throttle <= 0f || forwardSpeed < stats.driftMinSpeed || steer.sqrMagnitude < 0.0001f)
                return 0f;

            // The side is picked by where the stick points relative to the car.
            float delta = Mathf.DeltaAngle(_body.rotation, HeadingOf(steer));
            if (Mathf.Abs(delta) < stats.driftStartAngle) return 0f;

            _driftDirection = delta > 0f ? 1 : -1;
            _driftCharge = 0f;
            DriftStarted?.Invoke();
            return 0f;
        }

        // While drifting the stick steers the direction of travel (fast into the drift side, slower
        // out of it to widen the line) and the body stays angled into the corner by driftAngle.
        void DriftMotion(Vector2 steer, float throttle, float maxSpeed, float acceleration, bool offRoad, float dt)
        {
            Vector2 velocity = _body.linearVelocity;
            float speed = velocity.magnitude;
            float travel = speed > 0.1f ? HeadingOf(velocity) : _body.rotation;

            if (speed > maxSpeed)
                speed = Mathf.MoveTowards(speed, maxSpeed, (offRoad ? stats.offRoadDeceleration : stats.drag) * dt);
            else
                speed = Mathf.Min(maxSpeed, speed + acceleration * throttle * dt);

            if (steer.sqrMagnitude > 0.0001f)
            {
                float delta = Mathf.DeltaAngle(travel, HeadingOf(steer));
                float rate = Mathf.Sign(delta) == _driftDirection ? stats.driftTurnRate : stats.driftCounterTurnRate;
                travel += Mathf.Clamp(delta, -rate * dt, rate * dt);
            }

            float rad = (travel + 90f) * Mathf.Deg2Rad;
            _body.linearVelocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;

            float bodyTarget = travel + _driftDirection * stats.driftAngle;
            float bodyStep = Mathf.DeltaAngle(_body.rotation, bodyTarget);
            float maxBodyStep = stats.driftTurnRate * 2f * dt;
            _body.angularVelocity = Mathf.Clamp(bodyStep, -maxBodyStep, maxBodyStep) / dt;

            ForwardSpeed = Vector2.Dot(_body.linearVelocity, transform.up);
            SidewaysSpeed = Vector2.Dot(_body.linearVelocity, transform.right);
        }

        float ReleaseDrift()
        {
            float charge = _driftCharge;
            EndDrift();
            if (charge < stats.driftMinCharge) return 0f;

            BoostStrength = Mathf.InverseLerp(stats.driftMinCharge, stats.driftMaxCharge, charge);
            _boostDuration = Mathf.Lerp(stats.boostMinDuration, stats.boostMaxDuration, BoostStrength);
            _boostTimer = _boostDuration;
            BoostStarted?.Invoke(BoostStrength);
            return stats.boostKick * Mathf.Lerp(0.5f, 1f, BoostStrength);
        }

        void EndDrift()
        {
            _driftDirection = 0;
            _driftCharge = 0f;
        }

        /// <summary>Body rotation (degrees) that faces a world direction; the sprite faces +Y.</summary>
        static float HeadingOf(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

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
            EndDrift(); // getting rammed spins you out of a drift, charge is lost
        }
    }
}
