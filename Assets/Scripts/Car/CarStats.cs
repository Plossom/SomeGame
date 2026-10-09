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
        [Tooltip("Minimum forward speed to start a drift (units/s).")]
        [Min(0f)] public float driftMinSpeed = 7f;
        [Tooltip("Stick must point at least this many degrees off the car's heading to pick a drift side.")]
        [Range(0f, 90f)] public float driftStartAngle = 8f;
        [Tooltip("How fast the direction of travel follows the stick into the drift side (degrees/s).")]
        [Min(0f)] public float driftTurnRate = 200f;
        [Tooltip("How fast it follows the stick toward the other side, i.e. widening the drift (degrees/s).")]
        [Min(0f)] public float driftCounterTurnRate = 70f;
        [Tooltip("How far the car body points into the corner relative to its direction of travel (degrees).")]
        [Range(0f, 80f)] public float driftAngle = 32f;
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
    }
}
