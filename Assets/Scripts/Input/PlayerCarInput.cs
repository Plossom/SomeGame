using SomeGame.Car;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SomeGame.Input
{
    /// <summary>
    /// Player driver: WASD / arrow keys pick an absolute direction (8-way) at full throttle.
    /// </summary>
    public class PlayerCarInput : MonoBehaviour, ICarInput
    {
        public Vector2 SteerDirection { get; private set; }
        public float Throttle { get; private set; }

        void Update()
        {
            Vector2 keys = ReadKeyboard();
            SteerDirection = keys;
            Throttle = keys == Vector2.zero ? 0f : 1f;
        }

        static Vector2 ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y).normalized;
        }
    }
}
