using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SomeGame.Input
{
    /// <summary>
    /// Joystick that appears where the thumb touches down. The knob follows the thumb up to
    /// <see cref="maxRadius"/>; releasing hides it. In the Editor the left mouse button acts as touch.
    /// </summary>
    public class FloatingJoystick : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [Tooltip("Ring drawn at the touch-down point. Must be a child of a full-screen RectTransform.")]
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform knob;
        [Tooltip("How far the knob can travel from the centre, in canvas units (reference 1170x2532).")]
        [SerializeField, Min(1f)] float maxRadius = 170f;
        [Tooltip("Fraction of the radius that counts as no input.")]
        [SerializeField, Range(0f, 0.9f)] float deadZone = 0.15f;

        Vector2 _origin;
        bool _pressed;
        bool _ignoringPress;

        /// <summary>Stick deflection, magnitude 0..1, in screen orientation (up is +Y).</summary>
        public Vector2 Value { get; private set; }
        public bool IsHeld => _pressed && !_ignoringPress;
        public bool IsPastDeadZone => IsHeld && Value.magnitude > deadZone;

        /// <summary>When false, new touches are ignored (countdown, finish screen).</summary>
        public bool Interactable { get; set; } = true;

        void OnEnable()
        {
            ControlSettings.Changed += UpdateVisibility;
            UpdateVisibility();
        }

        void OnDisable()
        {
            ControlSettings.Changed -= UpdateVisibility;
            Release();
        }

        void Update()
        {
            bool down = ReadPointer(out Vector2 screenPosition);

            if (down && !_pressed)
            {
                _pressed = true;
                // Touches that start on a button, or while input is locked, never become the stick.
                _ignoringPress = !Interactable || IsOverUI();
                _origin = screenPosition;
            }
            else if (!down && _pressed)
            {
                Release();
            }

            if (!IsHeld)
            {
                UpdateVisibility();
                return;
            }

            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 offset = Vector2.ClampMagnitude((screenPosition - _origin) / scale, maxRadius);
            Value = offset / maxRadius;

            var parent = (RectTransform)background.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, _origin, null, out Vector2 local);
            background.anchoredPosition = local;
            knob.anchoredPosition = offset;
            UpdateVisibility();
        }

        void Release()
        {
            _pressed = false;
            _ignoringPress = false;
            Value = Vector2.zero;
            UpdateVisibility();
        }

        void UpdateVisibility()
        {
            bool show = IsHeld && !ControlSettings.InvisibleJoystick;
            if (background != null && background.gameObject.activeSelf != show)
                background.gameObject.SetActive(show);
        }

        static bool ReadPointer(out Vector2 position)
        {
            var touch = Touchscreen.current?.primaryTouch;
            if (touch != null && touch.press.isPressed)
            {
                position = touch.position.ReadValue();
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }

        static bool IsOverUI()
        {
            var events = EventSystem.current;
            if (events == null) return false;
            if (events.IsPointerOverGameObject()) return true;
            var touch = Touchscreen.current?.primaryTouch;
            return touch != null && events.IsPointerOverGameObject(touch.touchId.ReadValue());
        }
    }
}
