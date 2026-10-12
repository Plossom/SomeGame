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

        float _landedAt = -10f;

        void Awake()
        {
            if (joystick == null) joystick = FindAnyObjectByType<FloatingJoystick>();
            _sensor = GetComponent<SomeGame.Track.TrackSensor>();
            _car = GetComponent<SomeGame.Car.CarMovement>();
            if (_car != null) _car.Landed += () => _landedAt = Time.time;
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
            float strength = 0.28f;
            // Just landed: no help for a moment, then it fades back in (no tug right after a jump).
            float sinceLanding = Time.time - _landedAt;
            if (sinceLanding < 1.1f) strength *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1.1f, sinceLanding));
            if (strength <= 0f) return aim;
            float window = 45f;
            if (_car.IsDrifting) strength *= 0.4f; // a drift is meant to point across the road
            Vector2 position = _car.Body.position;
            float speed = Mathf.Max(0f, _car.ForwardSpeed);
            float look = 5f + speed * 0.35f;
            float turn = 0f; // how the road bends ahead (degrees; positive = left)
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
                // Oil ahead on your line: aim round it, on whichever side needs the smaller move.
                foreach (var spot in track.Layout.oil)
                {
                    float to = path.DeltaDistance(d, spot.distance);
                    if (to < -1f || to > 22f) continue;
                    float clear = spot.radius + 1.6f;
                    if (Mathf.Abs(keep - spot.lateral) >= clear) continue;
                    float limit = track.HalfWidthAt(spot.distance) - 0.9f;
                    float left = spot.lateral + clear, right = spot.lateral - clear;
                    float dodge = left > limit ? right : right < -limit ? left : (Mathf.Abs(left - keep) < Mathf.Abs(right - keep) ? left : right);
                    float k = Mathf.InverseLerp(22f, 7f, to);
                    keep = Mathf.Lerp(keep, Mathf.Clamp(dodge, -limit, limit), k);

                    window = Mathf.Max(window, 55f);
                }
                // In a bend, look further up the road: the aim then already lies across the inside of the
                // corner, like a racing line, instead of hugging the road's curve.
                turn = path.TurnAtDistance(d + look, 8);
                float bendLook = look * Mathf.Lerp(1f, 1.8f, Mathf.InverseLerp(10f, 45f, Mathf.Abs(turn)));
                target = path.PointAt(d + bendLook) + path.NormalAt(d + bendLook) * keep;
                // A ramp ahead: the aim slides over to a line straight through the ramp and on along the road
                // beyond it (at normal strength: guided, not yanked).
                foreach (var jump in track.Jumps)
                {
                    float toLip = path.DeltaDistance(d, jump.Lip);
                    if (toLip <= 0f || toLip > 30f) continue;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 10f, toLip));
                    Vector2 through = path.PointAt(jump.Lip + 6f) + path.NormalAt(jump.Lip + 6f) * jump.Lateral;
                    target = Vector2.Lerp(target, through, k);
                    turn *= 1f - k; // the line through a ramp is straight
                    window = Mathf.Max(window, 50f);
                }
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

            // Steering clearly at a jump pad or a ramp beside the road: that is on purpose, no help.
            foreach (var pad in track.Layout.bouncers)
                if (Aiming(aim, position, pad.position)) return aim;
            foreach (var cut in track.Shortcuts)
                if (Aiming(aim, position, cut.Centre)) return aim;

            Vector2 road = (target - position).normalized;
            Vector2 result = aim;
            float angle = Vector2.Angle(aim, road);
            if (angle < window)
            {
                // Full help for small misses, fading out only towards the edge of the window.
                float w = strength * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(window * 0.5f, window, angle)));
                // Aiming further into the corner than the road (cutting it): hardly pull back.
                if (Mathf.Abs(turn) > 10f && Mathf.Sign(Vector2.SignedAngle(road, aim)) == Mathf.Sign(turn)) w *= 0.2f;
                result = Vector3.Slerp(aim, road, w);
            }

            // Near the edge, steering further out is softened.
            // The inside of a bend is free (cutting the corner); only the outside and straights are guarded.
            if (Mathf.Abs(turn) > 10f && Mathf.Sign(lateral) == Mathf.Sign(turn)) guardEdge = false;
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

        static bool Aiming(Vector2 aim, Vector2 from, Vector2 at)
        {
            Vector2 to = at - from;
            float distance = to.magnitude;
            return distance < 32f && distance > 0.5f && Vector2.Angle(aim, to) < Mathf.Lerp(28f, 14f, distance / 32f);
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
