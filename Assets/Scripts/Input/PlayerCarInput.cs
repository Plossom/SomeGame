using SomeGame.Car;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SomeGame.Input
{
    /// <summary>
    /// Player driver. Floating joystick: the car turns toward the stick direction and drives at full
    /// throttle once the stick leaves the dead zone; lifting the thumb coasts. A second finger on the
    /// screen while steering holds a drift.
    /// Keyboard (WASD / arrows, Space to drift) and the right mouse button (drift) work in the Editor.
    /// </summary>
    public class PlayerCarInput : MonoBehaviour, ICarInput
    {
        [Tooltip("Found in the scene automatically when left empty.")]
        [SerializeField] FloatingJoystick joystick;

        public Vector2 SteerDirection { get; private set; }
        public float Throttle { get; private set; }
        public bool DriftHeld { get; private set; }

        void Awake()
        {
            if (joystick == null) joystick = FindAnyObjectByType<FloatingJoystick>();
        }

        void Update()
        {
            DriftHeld = SecondFingerDown() || DriftKeyDown();

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

        static bool SecondFingerDown()
        {
            var screen = Touchscreen.current;
            if (screen == null) return false;
            int pressed = 0;
            foreach (var touch in screen.touches)
                if (touch.press.isPressed && ++pressed >= 2) return true;
            return false;
        }

        static bool DriftKeyDown()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            return (keyboard != null && keyboard.spaceKey.isPressed) || (mouse != null && mouse.rightButton.isPressed);
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
