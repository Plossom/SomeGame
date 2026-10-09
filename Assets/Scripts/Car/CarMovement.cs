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
        bool _driftHeldLastStep;
        float _hopTimer;       // > 0 right after the drift button was pressed: the stick may pick a side
        float _driftTightness; // 0 = widest drift, 1 = tightest, smoothed
        float _laggedHeading;  // where the car was pointing a moment ago (smoothed), for steering intent
        float _airTimer, _airDuration;
        float _slipTimer, _slipDuration, _slipDirection, _slipStartSpeed;
        Collider2D _collider;

        /// <summary>Raised on every collision with the closing speed of the impact.</summary>
        public event Action<float> Collided;
        public event Action DriftStarted;
        /// <summary>The drift button was pressed. The car always hops; a drift follows only if conditions allow.</summary>
        public event Action Hopped;
        /// <summary>A drift was released with enough charge; argument is boost strength 0..1.</summary>
        public event Action<float> BoostStarted;
        /// <summary>The car left a ramp; argument is the time in the air.</summary>
        public event Action<float> Launched;
        public event Action Landed;
        /// <summary>The car hit an oil puddle.</summary>
        public event Action Slipped;

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
        public bool IsOffRoad => sensor != null && sensor.IsOffRoad && !OnRamp;
        /// <summary>Set while the car is on a shortcut ramp beside the road: the ramp counts as road.</summary>
        public bool OnRamp { get; set; }

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
        /// <summary>Throttle applied in the last physics step (-1..1; negative = braking, 0 without control).</summary>
        public float Throttle { get; private set; }
        public bool IsBoosting => _boostTimer > 0f;
        public bool IsAirborne => _airTimer > 0f;
        /// <summary>0 at take-off and landing, 1 at the top of the jump.</summary>
        public float AirHeight => IsAirborne ? Mathf.Sin(Mathf.PI * (1f - _airTimer / _airDuration)) : 0f;
        public bool IsSlipping => _slipTimer > 0f;
        /// <summary>Strength (0..1) of the boost currently running.</summary>
        public float BoostStrength { get; private set; }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _input = GetComponent<ICarInput>();
            _collider = GetComponent<Collider2D>();
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
            if (IsAirborne)
            {
                Fly(dt);
                return;
            }
            if (IsSlipping)
            {
                SpinOut(dt);
                return;
            }
            Vector2 forward = transform.up, right = transform.right;
            Vector2 velocity = _body.linearVelocity;
            float forwardSpeed = Vector2.Dot(velocity, forward);
            float sidewaysSpeed = Vector2.Dot(velocity, right);

            bool offRoad = IsOffRoad;
            bool hasControl = ControlsEnabled && _input != null;
            float throttle = hasControl ? Mathf.Clamp(_input.Throttle, -1f, 1f) : 0f;
            Throttle = throttle;
            Vector2 steer = hasControl ? _input.SteerDirection : Vector2.zero;
            bool driftHeld = hasControl && _input.DriftHeld;
            if (driftHeld && !_driftHeldLastStep)
            {
                _hopTimer = stats.driftHopTime;
                Hopped?.Invoke();
            }
            _driftHeldLastStep = driftHeld;
            _hitTimer = Mathf.Max(0f, _hitTimer - dt);

            forwardSpeed += UpdateDrift(driftHeld, steer, throttle, velocity.magnitude, offRoad, dt);

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
            if (throttle < 0f)
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, stats.brakeDeceleration * -throttle * dt);
            else if (forwardSpeed > maxSpeed)
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
        float UpdateDrift(bool held, Vector2 steer, float throttle, float speed, bool offRoad, float dt)
        {
            bool choosingSide = _hopTimer > 0f;
            _hopTimer -= dt;
            _laggedHeading += Mathf.DeltaAngle(_laggedHeading, _body.rotation) * (1f - Mathf.Exp(-dt / stats.driftSteerMemory));

            if (IsDrifting)
            {
                _driftCharge += dt;
                // Lifted the steering thumb, ran onto the grass or got too slow: drift lost, no boost.
                if (!ControlsEnabled || throttle <= 0f || offRoad || speed < stats.driftMinSpeed * stats.driftKeepSpeedFactor)
                    EndDrift();
                else if (!held)
                    return ReleaseDrift();      // let go of the drift finger: boost
                return 0f;
            }

            // A drift can only begin during the hop, on the road, at speed.
            if (!held || !choosingSide || throttle <= 0f || offRoad || speed < stats.driftMinSpeed || steer.sqrMagnitude < 0.0001f)
                return 0f;

            // Stick slightly left or right of where the car was heading a moment ago picks the side
            // (so a press just after turning in still counts); roughly straight is just a hop.
            float intent = Mathf.DeltaAngle(_laggedHeading, HeadingOf(steer));
            if (Mathf.Abs(intent) < stats.driftNeutralAngle) return 0f;

            _driftDirection = intent > 0f ? 1 : -1;
            _driftCharge = 0f;
            _driftTightness = 0f;
            _hopTimer = 0f;
            DriftStarted?.Invoke();
            return 0f;
        }

        // A drift always curves toward its side. The stick only sets how tight: pointing into the
        // corner (relative to the direction of travel) tightens it, straight or outward is the widest
        // line. The body is angled into the corner, more so in a tight drift.
        void DriftMotion(Vector2 steer, float throttle, float maxSpeed, float acceleration, bool offRoad, float dt)
        {
            Vector2 velocity = _body.linearVelocity;
            float speed = velocity.magnitude;
            float travel = speed > 0.1f ? HeadingOf(velocity) : _body.rotation;

            if (speed > maxSpeed)
                speed = Mathf.MoveTowards(speed, maxSpeed, (offRoad ? stats.offRoadDeceleration : stats.drag) * dt);
            else
                speed = Mathf.Min(maxSpeed, speed + acceleration * throttle * dt);

            float inside = steer.sqrMagnitude > 0.0001f
                ? Mathf.DeltaAngle(travel, HeadingOf(steer)) * _driftDirection
                : 0f;
            float tightness = Mathf.Clamp01(inside / stats.driftFullInsideAngle);
            _driftTightness = Mathf.MoveTowards(_driftTightness, tightness, stats.driftTightnessResponse * dt);

            // Entry: the car tilts quickly into the corner (slightly past the drift angle) while its
            // path stays almost straight; the curve fades in as the entry settles.
            float entry = 1f - Mathf.Clamp01(_driftCharge / stats.driftEntryTime);
            float curve = Mathf.Lerp(stats.driftWideTurnRate, stats.driftTightTurnRate, _driftTightness) * (1f - entry);
            travel += _driftDirection * curve * dt;
            float rad = (travel + 90f) * Mathf.Deg2Rad;
            _body.linearVelocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;

            float angle = Mathf.Lerp(stats.driftAngleWide, stats.driftAngleTight, _driftTightness)
                        + stats.driftEntryKick * entry * entry;
            float bodyStep = Mathf.DeltaAngle(_body.rotation, travel + _driftDirection * angle);
            float bodyRate = stats.driftEntryRotationSpeed * (entry > 0f ? 1f : 0.5f);
            _body.angularVelocity = Mathf.Clamp(bodyStep, -bodyRate * dt, bodyRate * dt) / dt;

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

        /// <summary>
        /// Sends the car into the air (off a ramp) for <paramref name="duration"/> seconds. With a
        /// direction, the car is turned to fly that way (big shortcut ramps).
        /// </summary>
        public void Launch(float duration, Vector2? direction = null)
        {
            if (IsAirborne || duration <= 0f) return;
            EndDrift();
            if (direction.HasValue)
            {
                Vector2 dir = direction.Value.normalized;
                _body.rotation = HeadingOf(dir);
                transform.rotation = Quaternion.Euler(0f, 0f, _body.rotation);
                _body.linearVelocity = dir * _body.linearVelocity.magnitude;
            }
            _airDuration = _airTimer = duration;
            _body.angularVelocity = 0f;
            if (_collider != null) _collider.enabled = false; // flies over cars and scenery
            Launched?.Invoke(duration);
        }

        // In the air the car keeps its speed and heading; the stick only nudges it a little.
        void Fly(float dt)
        {
            _airTimer -= dt;
            Vector2 steer = ControlsEnabled && _input != null ? _input.SteerDirection : Vector2.zero;
            if (steer.sqrMagnitude > 0.0001f)
            {
                float delta = Mathf.DeltaAngle(_body.rotation, HeadingOf(steer));
                _body.angularVelocity = Mathf.Clamp(delta, -stats.turnRate * stats.airSteering * dt, stats.turnRate * stats.airSteering * dt) / dt;
            }
            else _body.angularVelocity = 0f;
            float speed = _body.linearVelocity.magnitude;
            _body.linearVelocity = (Vector2)transform.up * speed;
            ForwardSpeed = speed;
            SidewaysSpeed = 0f;
            if (_airTimer > 0f) return;

            _airTimer = 0f;
            if (_collider != null) _collider.enabled = true;
            _body.linearVelocity *= stats.landingSpeedKeep;
            Collided?.Invoke(stats.landingImpact); // a little camera shake
            Landed?.Invoke();
        }

        /// <summary>
        /// Oil: the car spins once all the way round (like a banana peel) while it slides on in the
        /// direction it was going and loses speed; the controls do nothing until the spin is over.
        /// </summary>
        public void Slip(int direction)
        {
            if (IsAirborne || IsSlipping) return;
            EndDrift();
            _boostTimer = 0f;
            _slipDuration = _slipTimer = stats.oilSpinSeconds;
            _slipDirection = direction >= 0 ? 1f : -1f;
            _slipStartSpeed = _body.linearVelocity.magnitude;
            Slipped?.Invoke();
        }

        void SpinOut(float dt)
        {
            _slipTimer -= dt;
            float t = 1f - Mathf.Clamp01(_slipTimer / _slipDuration);
            // Fast at first, easing out at the end of the turn.
            float rate = stats.oilSpinDegrees / _slipDuration * 2f * (1f - t);
            _body.angularVelocity = _slipDirection * rate;
            Vector2 velocity = _body.linearVelocity;
            float speed = Mathf.Lerp(_slipStartSpeed, _slipStartSpeed * stats.oilSpeedKeep, t);
            _body.linearVelocity = velocity.sqrMagnitude > 0.01f ? velocity.normalized * speed : Vector2.zero;
            ForwardSpeed = Vector2.Dot(_body.linearVelocity, transform.up);
            SidewaysSpeed = Vector2.Dot(_body.linearVelocity, transform.right);
            if (_slipTimer <= 0f)
            {
                _slipTimer = 0f;
                _body.angularVelocity = 0f;
            }
        }

        /// <summary>Puts the car back on the road (after a splash), standing still.</summary>
        public void Respawn(Pose pose)
        {
            _airTimer = 0f;
            _slipTimer = 0f;
            _boostTimer = 0f;
            EndDrift();
            if (_collider != null) _collider.enabled = true;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _body.position = pose.position;
            _body.rotation = pose.rotation.eulerAngles.z;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            ForwardSpeed = SidewaysSpeed = 0f;
            if (sensor != null) sensor.Sample();
        }

        /// <summary>Body rotation (degrees) that faces a world direction; the sprite faces +Y.</summary>
        static float HeadingOf(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        void OnCollisionEnter2D(Collision2D collision)
        {
            float impact = collision.relativeVelocity.magnitude;
            Collided?.Invoke(impact);

            // A hard knock against scenery (trees, houses, tyres) also ends a drift.
            if (collision.rigidbody == null && impact > 4f) EndDrift();
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
