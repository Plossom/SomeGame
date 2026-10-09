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

        [Header("Collisions")]
        [Tooltip("Extra impulse given to a car you drive into, per unit of closing speed.")]
        [Min(0f)] public float ramForce = 0.7f;
        [Tooltip("Seconds of reduced steering after being hit hard.")]
        [Min(0f)] public float hitRecoveryTime = 0.35f;
        [Tooltip("Rigidbody mass. Heavier cars push lighter ones more.")]
        [Min(0.01f)] public float mass = 1f;
    }
}
