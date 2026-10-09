using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Every tunable handling value for a car. Never hard-code these in movement code;
    /// the skill tree will later produce modified copies of this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/Car Stats", fileName = "CarStats")]
    public class CarStats : ScriptableObject
    {
        [Header("Speed")]
        [Tooltip("Maximum forward speed on the road (units/s).")]
        [Min(0f)] public float topSpeed = 18f;
        [Tooltip("Forward acceleration at full throttle (units/s²).")]
        [Min(0f)] public float acceleration = 11f;
        [Tooltip("Deceleration when the throttle is released (units/s²).")]
        [Min(0f)] public float drag = 5f;
        [Tooltip("Deceleration at full brake (negative throttle; only the AI brakes) (units/s²).")]
        [Min(0f)] public float brakeDeceleration = 18f;

        [Header("Steering")]
        [Tooltip("Maximum rotation speed toward the stick direction (degrees/s).")]
        [Min(0f)] public float turnRate = 220f;
        [Tooltip("Forward speed at which the full turn rate is available (units/s).")]
        [Min(0.01f)] public float fullTurnSpeed = 6f;
        [Tooltip("Fraction of the turn rate available when standing still.")]
        [Range(0f, 1f)] public float standstillTurnFactor = 0.35f;

        [Header("Grip")]
        [Tooltip("How quickly sideways sliding is cancelled (1/s). Higher = more grip, less drift.")]
        [Min(0f)] public float grip = 7f;
        [Tooltip("Fraction of grip lost at top speed, so fast corners slide.")]
        [Range(0f, 1f)] public float highSpeedGripLoss = 0.5f;
        [Tooltip("Sideways speed above which the car counts as sliding (tyre marks).")]
        [Min(0f)] public float slideThreshold = 3f;

        [Header("Off-road (grass)")]
        [Tooltip("Top speed multiplier while on grass.")]
        [Range(0.05f, 1f)] public float offRoadSlowdown = 0.4f;
        [Tooltip("Braking applied on grass while faster than the grass top speed (units/s²).")]
        [Min(0f)] public float offRoadDeceleration = 22f;
        [Tooltip("Grip multiplier on grass.")]
        [Range(0f, 1f)] public float offRoadGrip = 0.6f;

        [Header("Drift (second finger while steering)")]
        [Tooltip("Minimum speed to start a drift (units/s). Drifting only works on the road, never on grass.")]
        [Min(0f)] public float driftMinSpeed = 10f;
        [Tooltip("A drift ends (without boost) when the speed drops below this fraction of driftMinSpeed.")]
        [Range(0f, 1f)] public float driftKeepSpeedFactor = 0.6f;
        [Tooltip("Seconds after pressing the drift button (the hop) in which the stick picks a drift side.")]
        [Min(0f)] public float driftHopTime = 0.4f;
        [Tooltip("Steering intent within this many degrees counts as straight: the car only hops, no drift. " +
                 "Intent = stick direction compared with where the car was heading a moment ago (driftSteerMemory).")]
        [Range(0f, 90f)] public float driftNeutralAngle = 6f;
        [Tooltip("How long a recent steering movement still counts as intent when picking the drift side (seconds, " +
                 "time constant of the lagging heading). Lets you press just after turning in, when the car has " +
                 "already caught up with the stick.")]
        [Min(0.01f)] public float driftSteerMemory = 0.6f;
        [Tooltip("Curve rate while drifting with the stick straight ahead or pointing out of the corner (degrees/s). " +
                 "A drift always curves at least this much toward its side.")]
        [Min(0f)] public float driftWideTurnRate = 30f;
        [Tooltip("Curve rate with the stick pointed fully into the corner (degrees/s).")]
        [Min(0f)] public float driftTightTurnRate = 170f;
        [Tooltip("Stick angle into the corner (relative to the direction of travel) that gives the tightest drift.")]
        [Range(1f, 180f)] public float driftFullInsideAngle = 70f;
        [Tooltip("How fast the drift reacts to stick changes between wide and tight (per second).")]
        [Min(0.1f)] public float driftTightnessResponse = 5f;
        [Tooltip("Body angle into the corner relative to the direction of travel, wide drift (degrees).")]
        [Range(0f, 80f)] public float driftAngleWide = 22f;
        [Tooltip("Body angle into the corner, tight drift (degrees).")]
        [Range(0f, 80f)] public float driftAngleTight = 45f;
        [Tooltip("Extra rotation on drift entry: the car snaps this many degrees past the drift angle, then settles.")]
        [Range(0f, 60f)] public float driftEntryKick = 12f;
        [Tooltip("Seconds the entry kick takes to settle back to the drift angle.")]
        [Min(0.01f)] public float driftEntryTime = 0.3f;
        [Tooltip("Rotation speed of the snap into the drift (degrees/s).")]
        [Min(0f)] public float driftEntryRotationSpeed = 900f;
        [Tooltip("Top speed multiplier while drifting.")]
        [Range(0.1f, 1f)] public float driftSpeedFactor = 0.92f;
        [Tooltip("Seconds of drifting before releasing gives any boost (first spark colour).")]
        [Min(0f)] public float driftMinCharge = 0.6f;
        [Tooltip("Seconds of drifting for the strongest boost (last spark colour).")]
        [Min(0.01f)] public float driftMaxCharge = 2.4f;

        [Header("Boost (released drift)")]
        [Tooltip("Boost length for the weakest / strongest charge (seconds).")]
        [Min(0f)] public float boostMinDuration = 0.5f;
        [Min(0f)] public float boostMaxDuration = 1.5f;
        [Tooltip("Extra top speed as a fraction of topSpeed, weakest / strongest charge.")]
        [Min(0f)] public float boostMinSpeedBonus = 0.15f;
        [Min(0f)] public float boostMaxSpeedBonus = 0.45f;
        [Tooltip("Extra acceleration while boosting (units/s²).")]
        [Min(0f)] public float boostAcceleration = 30f;
        [Tooltip("Instant forward speed added when a full-strength boost fires (units/s).")]
        [Min(0f)] public float boostKick = 4f;

        [Header("Collisions")]
        [Tooltip("Extra impulse given to a car you drive into, per unit of closing speed.")]
        [Min(0f)] public float ramForce = 0.7f;
        [Tooltip("Seconds of reduced steering after being hit hard.")]
        [Min(0f)] public float hitRecoveryTime = 0.35f;
        [Tooltip("Rigidbody mass. Heavier cars push lighter ones more.")]
        [Min(0.01f)] public float mass = 1f;

        [Header("Jumps and oil")]
        [Tooltip("Time in the air off a ramp: base plus this much per unit of speed.")]
        public float airTimeBase = 0.28f;
        public float airTimePerSpeed = 0.03f;
        [Tooltip("How much the stick can turn the car in the air (fraction of the turn rate).")]
        [Range(0f, 1f)] public float airSteering = 0.15f;
        [Tooltip("Speed kept on landing.")]
        [Range(0.5f, 1f)] public float landingSpeedKeep = 0.94f;
        [Tooltip("Impact reported on landing (drives the camera shake).")]
        public float landingImpact = 7f;
        [Tooltip("Oil: how far the car spins round (degrees) and how long that takes.")]
        public float oilSpinDegrees = 360f;
        [Min(0.1f)] public float oilSpinSeconds = 0.9f;
        [Tooltip("Speed left after the oil spin (fraction).")]
        [Range(0f, 1f)] public float oilSpeedKeep = 0.4f;
    }
}
