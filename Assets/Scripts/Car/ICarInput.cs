using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// What a driver wants: the player (joystick/keys) and the AI both implement this,
    /// and <see cref="CarMovement"/> turns it into motion.
    /// </summary>
    public interface ICarInput
    {
        /// <summary>
        /// World-space direction the car should face (absolute steering, north is up).
        /// Zero means "no preference": keep the current heading.
        /// </summary>
        Vector2 SteerDirection { get; }

        /// <summary>0 = coast, 1 = full throttle.</summary>
        float Throttle { get; }
    }
}
