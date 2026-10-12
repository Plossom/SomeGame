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
    /// Smart steering (<see cref="ControlSettings.SteerAssist"/>) bends the aim gently toward the road
    /// ahead while you roughly follow it, softens steering off the edge and lines the car up with ramps.
    /// </summary>
    public class PlayerCarInput : MonoBehaviour, ICarInput
    {
        [Tooltip("Found in the scene automatically when left empty.")]
        [SerializeField] FloatingJoystick joystick;

        public Vector2 SteerDirection { get; private set; }
        public float Throttle { get; private set; }
        public bool DriftHeld { get; private set; }

        SomeGame.Track.TrackSensor _sensor;
        SomeGame.Car.CarMovement _car;

        void Awake()
        {
            if (joystick == null) joystick = FindAnyObjectByType<FloatingJoystick>();
            _sensor = GetComponent<SomeGame.Track.TrackSensor>();
            _car = GetComponent<SomeGame.Car.CarMovement>();
        }

        void Update()
        {
            DriftHeld = SecondFingerDown() || DriftKeyDown();

            if (joystick != null && joystick.IsPastDeadZone)
            {
                SteerDirection = Assisted(joystick.Value.normalized);
                Throttle = 1f;
                return;
            }

            Vector2 keys = ReadKeyboard();
            SteerDirection = keys == Vector2.zero ? keys : Assisted(keys);
            Throttle = keys == Vector2.zero ? 0f : 1f;
        }

        // Smart steering. Only when the aim roughly follows the road (a clear steer elsewhere always wins).
        Vector2 Assisted(Vector2 aim)
        {
            var level = ControlSettings.SteerAssist;
            if (level == ControlSettings.Assist.Off || _sensor == null || _car == null || _sensor.Track == null || _car.IsAirborne) return aim;
            var track = _sensor.Track;
            var path = track.Path;
            float strength = level == ControlSettings.Assist.Light ? 0.45f : 0.75f;
            float window = level == ControlSettings.Assist.Light ? 45f : 60f;
            if (_car.IsDrifting) strength *= 0.4f; // a drift is meant to point across the road
            Vector2 position = _car.Body.position;
            float speed = Mathf.Max(0f, _car.ForwardSpeed);
            float look = 5f + speed * 0.35f;
            float lateral = _sensor.Current.Lateral;
            Vector2 target, tangent;
            float half;
            bool guardEdge = true;

            if (_sensor.Branch >= 0)
            {
                var route = track.Branches[_sensor.Branch];
                float along = _sensor.BranchAlong + look;
                target = along <= route.Length ? route.PointAt(along) : path.PointAt(route.To + along - route.Length);
                tangent = route.TangentAt(_sensor.BranchAlong);
                half = route.HalfWidth;
            }
            else
            {
                float d = _sensor.Current.Distance;
                half = track.HalfWidthAt(d);
                float keep = Mathf.Clamp(lateral, -half * 0.6f, half * 0.6f); // stay on your own line
                // A ramp ahead: line up with it.
                foreach (var jump in track.Jumps)
                {
                    float toLip = path.DeltaDistance(d, jump.Lip);
                    if (toLip <= 0f || toLip > 24f) continue;
                    float k = Mathf.InverseLerp(24f, 8f, toLip);
                    keep = Mathf.Lerp(keep, jump.Lateral, k);
                    strength = Mathf.Max(strength, Mathf.Lerp(strength, 0.85f, k));
                    window = Mathf.Max(window, 60f);
                }
                target = path.PointAt(d + look) + path.NormalAt(d + look) * keep;
                tangent = path.TangentAt(d);
                // Leave room for the things beside the road that you are meant to drive onto.
                float side = Mathf.Sign(lateral);
                if (track.BankAt(d, side * (track.OffRoadAt(d) + 1f), out _) != null) guardEdge = false;
                foreach (var cut in track.Layout.shortcuts)
                {
                    float to = path.DeltaDistance(d, cut.from);
                    if (to > -6f && to < 30f && Mathf.Sign(cut.side) == side) guardEdge = false;
                }
            }

            Vector2 road = (target - position).normalized;
            Vector2 result = aim;
            float angle = Vector2.Angle(aim, road);
            if (angle < window)
            {
                // Full help for small misses, fading out only towards the edge of the window.
                float w = strength * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(window * 0.5f, window, angle)));
                result = Vector3.Slerp(aim, road, w);
            }

            // Near the edge, steering further out is softened.
            if (guardEdge)
            {
                Vector2 outward = new Vector2(-tangent.y, tangent.x) * Mathf.Sign(lateral);
                float edge = Mathf.InverseLerp(half * 0.6f, half + track.Layout.kerbWidth, Mathf.Abs(lateral));
                float outwardPart = Vector2.Dot(result, outward);
                if (edge > 0f && outwardPart > 0f)
                    result -= outward * outwardPart * Mathf.Clamp01(edge * strength * 1.5f);
            }
            return result.sqrMagnitude > 0.0001f ? result.normalized : aim;
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
