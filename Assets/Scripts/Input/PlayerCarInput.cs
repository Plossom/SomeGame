using SomeGame.Car;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SomeGame.Input
{
    /// <summary>
    /// Player driver. Floating joystick: the car turns toward the stick direction and drives at full
    /// throttle once the stick leaves the dead zone; lifting the thumb coasts.
    /// Keyboard (WASD / arrows) works the same way with 8 directions, for testing in the Editor.
    /// </summary>
    public class PlayerCarInput : MonoBehaviour, ICarInput
    {
        [Tooltip("Found in the scene automatically when left empty.")]
        [SerializeField] FloatingJoystick joystick;

        public Vector2 SteerDirection { get; private set; }
        public float Throttle { get; private set; }

        void Awake()
        {
            if (joystick == null) joystick = FindAnyObjectByType<FloatingJoystick>();
        }

        void Update()
        {
            if (joystick != null && joystick.IsPastDeadZone)
            {
                SteerDirection = joystick.Value.normalized;
                Throttle = 1f;
                return;
            }

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
