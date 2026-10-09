using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SomeGame.City
{
    /// <summary>
    /// Perspective camera over the city (straight down by default). One finger (or the mouse) drags the map, two fingers
    /// pinch to zoom (mouse wheel in the Editor), a short tap reports the ground point it hit.
    /// <see cref="FocusOn"/> glides to a point.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CityCamera : MonoBehaviour
    {
        [SerializeField, Range(20f, 90f)] float pitch = 90f;
        [SerializeField] Vector2 distanceRange = new(120f, 600f);
        [SerializeField, Min(1f)] float distance = 280f;
        [Tooltip("The focus point is kept inside these bounds (world X/Z).")]
        [SerializeField] Rect bounds = new(-120, -90, 240, 200);
        [SerializeField, Min(0.01f)] float focusSmoothTime = 0.45f;
        [SerializeField, Min(0f)] float tapMaxMove = 18f;
        [SerializeField, Min(0f)] float tapMaxSeconds = 0.35f;

        UnityEngine.Camera _camera;
        Vector3 _focus;
        Vector3? _target;
        float? _targetDistance;
        Vector3 _focusVelocity;
        float _distanceVelocity;

        bool _dragging, _overUI;
        Vector2 _pressPosition, _lastPosition;
        float _pressTime;
        float _lastPinch;
        bool _pinching;

        /// <summary>A tap on the city: the ground point under the finger.</summary>
        public event Action<Vector3> Tapped;

        /// <summary>When false, gestures are ignored (a panel is open).</summary>
        public bool InputEnabled { get; set; } = true;

        public Rect Bounds { get => bounds; set => bounds = value; }

        void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _focus = new Vector3(bounds.center.x, 0f, bounds.center.y);
        }

        public void Jump(Vector3 focus, float dist)
        {
            _focus = Clamp(focus);
            distance = dist;
            _target = null;
            _targetDistance = null;
            Apply();
        }

        /// <summary>Glide to a point; <paramref name="screenOffset"/> shifts it up the screen (for panels at the bottom).</summary>
        public void FocusOn(Vector3 point, float? dist = null)
        {
            _target = Clamp(point);
            _targetDistance = dist;
        }

        void Update()
        {
            HandleInput();
            if (_target.HasValue)
            {
                _focus = Vector3.SmoothDamp(_focus, _target.Value, ref _focusVelocity, focusSmoothTime);
                if ((_focus - _target.Value).sqrMagnitude < 0.01f) _target = null;
            }
            if (_targetDistance.HasValue)
            {
                distance = Mathf.SmoothDamp(distance, _targetDistance.Value, ref _distanceVelocity, focusSmoothTime);
                if (Mathf.Abs(distance - _targetDistance.Value) < 0.05f) _targetDistance = null;
            }
            Apply();
        }

        void Apply()
        {
            var rotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.SetPositionAndRotation(_focus - rotation * Vector3.forward * distance, rotation);
        }

        void HandleInput()
        {
            var touch = Touchscreen.current;
            int touches = 0;
            Vector2 p0 = default, p1 = default;
            if (touch != null)
            {
                foreach (var t in touch.touches)
                {
                    if (!t.press.isPressed) continue;
                    if (touches == 0) p0 = t.position.ReadValue(); else if (touches == 1) p1 = t.position.ReadValue();
                    touches++;
                }
            }

            if (touches >= 2)
            {
                float pinch = Vector2.Distance(p0, p1);
                if (!_pinching) { _pinching = true; _lastPinch = pinch; _dragging = false; }
                else if (InputEnabled && !_overUI)
                {
                    Zoom(_lastPinch / Mathf.Max(1f, pinch));
                    _lastPinch = pinch;
                }
                return;
            }
            _pinching = false;

            bool down = touches == 1;
            Vector2 position = p0;
            var mouse = Mouse.current;
            if (!down && mouse != null && mouse.leftButton.isPressed)
            {
                down = true;
                position = mouse.position.ReadValue();
            }
            if (mouse != null && InputEnabled)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) Zoom(wheel > 0 ? 0.9f : 1.1f);
            }

            if (down && !_dragging)
            {
                _dragging = true;
                _overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                _pressPosition = _lastPosition = position;
                _pressTime = Time.unscaledTime;
                return;
            }

            if (down && _dragging)
            {
                if (InputEnabled && !_overUI && Ground(_lastPosition, out var a) && Ground(position, out var b))
                {
                    _focus = Clamp(_focus + (a - b));
                    _target = null;
                }
                _lastPosition = position;
                return;
            }

            if (!down && _dragging)
            {
                _dragging = false;
                bool tap = Vector2.Distance(position == default ? _lastPosition : _lastPosition, _pressPosition) < tapMaxMove &&
                           Time.unscaledTime - _pressTime < tapMaxSeconds;
                if (tap && InputEnabled && !_overUI && Ground(_lastPosition, out var hit)) Tapped?.Invoke(hit);
            }
        }

        void Zoom(float factor)
        {
            distance = Mathf.Clamp(distance * factor, distanceRange.x, distanceRange.y);
            _targetDistance = null;
        }

        bool Ground(Vector2 screen, out Vector3 point)
        {
            var ray = _camera.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = default;
            return false;
        }

        Vector3 Clamp(Vector3 p) =>
            new(Mathf.Clamp(p.x, bounds.xMin, bounds.xMax), 0f, Mathf.Clamp(p.z, bounds.yMin, bounds.yMax));
    }
}
