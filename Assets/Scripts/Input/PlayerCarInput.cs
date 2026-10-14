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

        /// <summary>Development: a stick direction set from code (automated tests); null = the real stick.</summary>
        [System.NonSerialized] public Vector2? ScriptedStick;

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
            bool manual = SecondFingerDown() || DriftKeyDown(); // the drift finger works in every mode

            Vector2 raw;
            if (ScriptedStick.HasValue)
            {
                raw = ScriptedStick.Value; // automated tests
                SteerDirection = raw == Vector2.zero ? raw : Assisted(raw);
                Throttle = raw == Vector2.zero ? 0f : 1f;
            }
            else if (joystick != null && joystick.IsPastDeadZone)
            {
                raw = joystick.Value.normalized;
                SteerDirection = Assisted(raw);
                Throttle = 1f;
            }
            else
            {
                raw = ReadKeyboard();
                SteerDirection = raw == Vector2.zero ? raw : Assisted(raw);
                Throttle = raw == Vector2.zero ? 0f : 1f;
            }
            DriftHeld = manual || (ControlSettings.Drift == ControlSettings.DriftMode.Auto && AutoDrift(raw));
            // An auto drift drives a clean line through the curve: its curve follows the road's.
            if (_car != null)
            {
                bool guided = _autoDrift && !manual && _car.IsDrifting && raw != Vector2.zero;
                _car.DriftTurnOverride = guided ? DriftTurn(_car.DriftDirection) : null;
                if (guided) SteerDirection = ((Vector2)Vector3.Slerp(raw, DriftLine(_car.DriftDirection), autoLine)).normalized;
            }
        }

        // Smart steering. Only when the aim roughly follows the road (a clear steer elsewhere always wins).
        // How many degrees to turn the aim (signed), before smoothing.
        float Correction(Vector2 aim)
        {
            var level = ControlSettings.SteerAssist;
            if (level == ControlSettings.Assist.Off || _sensor == null || _car == null || _sensor.Track == null || _car.IsAirborne) return 0f;
            var track = _sensor.Track;
            var path = track.Path;
            // Very light: a small share of a small miss, a bit more at speed (where errors matter most).
            float speedNow = Mathf.Max(0f, _car.ForwardSpeed);
            float strength = 0.16f * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(speedNow / Mathf.Max(1f, _car.Stats.topSpeed)));
            // Just landed: no help for a moment, then it fades back in (no tug right after a jump).
            float sinceLanding = Time.time - _landedAt;
            if (sinceLanding < 1.1f) strength *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1.1f, sinceLanding));
            if (strength <= 0f) return 0f;
            float window = 32f;
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
                if (Aiming(aim, position, pad.position)) return 0f;
            foreach (var cut in track.Shortcuts)
                if (Aiming(aim, position, cut.Centre)) return 0f;

            Vector2 road = (target - position).normalized;
            float correction = 0f;
            float angle = Vector2.Angle(aim, road);
            if (angle < window)
            {
                // Help fades out towards the edge of the window: bigger misses are on purpose.
                float w = strength * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(window * 0.4f, window, angle)));
                // Aiming further into the corner than the road (cutting it): hardly pull back.
                if (Mathf.Abs(turn) > 10f && Mathf.Sign(Vector2.SignedAngle(road, aim)) == Mathf.Sign(turn)) w *= 0.2f;
                correction = Vector2.SignedAngle(aim, road) * w;
            }

            // Only when the car is really about to leave the road (within about half a second), steering
            // further out is eased off a little. The inside of a bend is free (cutting the corner).
            if (Mathf.Abs(turn) > 10f && Mathf.Sign(lateral) == Mathf.Sign(turn)) guardEdge = false;
            if (guardEdge)
            {
                Vector2 outward = new Vector2(-tangent.y, tangent.x) * Mathf.Sign(lateral);
                float towardEdge = Vector2.Dot(_car.Body.linearVelocity, outward);
                float room = half + track.Layout.kerbWidth - Mathf.Abs(lateral);
                if (towardEdge > 0.5f)
                {
                    float soon = 1f - Mathf.Clamp01(room / towardEdge / 0.5f); // 0 = half a second away or more
                    Vector2 rotated = Quaternion.Euler(0f, 0f, correction) * aim;
                    float outwardPart = Vector2.Dot(rotated, outward);
                    if (soon > 0f && outwardPart > 0f)
                    {
                        Vector2 eased = (rotated - outward * outwardPart * soon * 0.35f).normalized;
                        correction += Vector2.SignedAngle(rotated, eased);
                    }
                }
            }
            return Mathf.Clamp(correction, -10f, 10f);
        }

        float _correction;
        Vector2 _smoothAim;

        // The aim after the help: thumb wobbles smoothed a touch, the correction eased in and out.
        Vector2 Assisted(Vector2 aim)
        {
            if (ControlSettings.SteerAssist == ControlSettings.Assist.Off) { _correction = 0f; return aim; }
            float dt = Time.deltaTime;
            // Small wobbles of the thumb are smoothed; real steering moves pass straight through.
            if (_smoothAim.sqrMagnitude > 0.5f && Vector2.Angle(_smoothAim, aim) < 20f)
                aim = ((Vector2)Vector3.Slerp(_smoothAim, aim, 1f - Mathf.Exp(-dt / 0.045f))).normalized;
            _smoothAim = aim;
            float target = Correction(aim);
            _correction = Mathf.Lerp(_correction, target, 1f - Mathf.Exp(-dt / 0.12f));
            return Mathf.Abs(_correction) < 0.05f ? aim : (Vector2)(Quaternion.Euler(0f, 0f, _correction) * aim);
        }

        static bool Aiming(Vector2 aim, Vector2 from, Vector2 at)
        {
            Vector2 to = at - from;
            float distance = to.magnitude;
            return distance < 32f && distance > 0.5f && Vector2.Angle(aim, to) < Mathf.Lerp(28f, 14f, distance / 32f);
        }

        // ------------------------------------------------------------------ auto drifting

        [Header("Auto drifting")]
        [Tooltip("How far (degrees) the stick must point into a turn, past the direction of travel, to start a drift where the road bends that way (and anywhere else).")]
        [SerializeField] float autoStartInCurve = 14f, autoStartAnywhere = 45f;
        [Tooltip("During an auto drift: how much the drift follows the road's line rather than the thumb (0..1).")]
        [SerializeField, Range(0f, 1f)] float autoLine = 0.75f;
        [Tooltip("Seconds the stick must be held that far into the turn.")]
        [SerializeField] float autoStartTime = 0.1f;
        [Tooltip("How much the road must turn over the stretch ahead (degrees) to count as a curve worth drifting.")]
        [SerializeField] float curveTurn = 32f;
        [Tooltip("Length of road ahead (m) over which the turn is measured: long enough that wide, sweeping curves count too.")]
        [SerializeField] float curveLength = 30f;

        float _intoTurnFor, _endingFor, _startedAt, _endedAt = -10f;
        bool _autoDrift;

        // Auto: where the road bends, steer into the curve at speed and the car drifts by itself. It holds the
        // drift through the curve and lets go (boost) at its exit, when you clearly steer out, or just before
        // the drift would carry the car off the outside of the road.
        // How much the road turns (signed, degrees; positive = left) from here over the next stretch.
        static float Bend(SomeGame.Track.TrackPath path, float from, float length) =>
            Vector2.SignedAngle(path.TangentAt(from), path.TangentAt(from + length));

        // How sharply an auto drift should curve (deg/s toward its side): as much as the road turns here,
        // corrected towards the car's line (a little inside of the middle) and the road's direction.
        float DriftTurn(int side)
        {
            var track = _sensor.Track;
            var path = track.Path;
            float d = _sensor.Current.Distance;
            Vector2 velocity = _car.Body.linearVelocity;
            float speed = velocity.magnitude;
            float roadRate = Vector2.SignedAngle(path.TangentAt(d), path.TangentAt(d + 6f)) / 6f * speed; // deg/s, left positive
            float headingError = Vector2.SignedAngle(path.TangentAt(d), velocity);                       // left positive
            float half = track.HalfWidthAt(d);
            float wantLateral = side * half * 0.2f;
            float lateral = _sensor.Current.Lateral;
            float edgeGain = Mathf.Lerp(6f, 18f, Mathf.InverseLerp(half * 0.5f, half, Mathf.Abs(lateral))); // push back harder near an edge
            float rate = roadRate - headingError * 2.5f + (wantLateral - lateral) * edgeGain;
            return Mathf.Clamp(rate * side, 4f, _car.Stats.driftTightTurnRate);
        }

        // Where an auto drift aims: the road a little way ahead, slightly towards the inside of the curve.
        Vector2 DriftLine(int side)
        {
            var track = _sensor.Track;
            var path = track.Path;
            float speed = _car.Body.linearVelocity.magnitude;
            float at = _sensor.Current.Distance + 6f + speed * 0.25f;
            // A little inside of the middle, pulled back towards the middle when the car is near an edge.
            float half = track.HalfWidthAt(at);
            // The further out the car is, the harder the line pulls it back in.
            float now = _sensor.Current.Lateral;
            float lateral = Mathf.Clamp(side * half * 0.15f - now * 1.1f, -half * 0.7f, half * 0.7f);
            Vector2 target = path.PointAt(at) + path.NormalAt(at) * lateral;
            return (target - _car.Body.position).normalized;
        }

        bool AutoDrift(Vector2 stick)
        {
            if (_car == null || _sensor == null || _sensor.Track == null) return false;
            float dt = Time.deltaTime;
            Vector2 velocity = _car.Body.linearVelocity;
            if (stick == Vector2.zero || velocity.magnitude < 3f || _car.IsAirborne || _car.IsSlipping)
            {
                _intoTurnFor = 0f;
                _autoDrift = false;
                return false;
            }
            var track = _sensor.Track;
            var path = track.Path;
            float d = _sensor.Current.Distance, speed = velocity.magnitude;
            float angle = Vector2.SignedAngle(velocity, stick); // positive = left of the direction of travel

            if (_autoDrift)
            {
                if (!_car.IsDrifting)
                {
                    // The drift did not start (or was lost): give up after a moment.
                    if (Time.time - _startedAt > 0.4f) _autoDrift = false;
                    return _autoDrift;
                }
                int side = _car.DriftDirection;
                // Clearly steering out of the curve (compared with the drift's line) ends it.
                bool steeringOut = Vector2.SignedAngle(DriftLine(side), stick) * side < -35f;
                // The curve is over when the road ahead hardly turns any more.
                bool curveOver = Bend(path, d, curveLength * 0.6f) * side < 7f;
                // About to leave the road (on either side): let go now, before the grass ends it without a boost.
                float lateral = _sensor.Current.Lateral, outer = track.OffRoadAt(d);
                Vector2 roadLeft = path.NormalAt(d);
                float sideways = Vector2.Dot(_car.Body.linearVelocity, roadLeft) * Mathf.Sign(lateral); // towards the nearer edge
                bool atEdge = Mathf.Abs(lateral) + Mathf.Max(0f, sideways) * 0.3f > outer - 0.4f; // within about 0.3 s of the edge
                if (atEdge) { _autoDrift = false; _endedAt = Time.time; return false; }
                // Otherwise hold it at least until it has earned a boost.
                float held = Time.time - _startedAt;
                bool charged = _car.DriftTier >= 1; // the sparks are showing: letting go now gives a boost
                // Fully charged: cash the boost in at the next easier part (or soon anyway), so long curves
                // get several drifts and boosts instead of one slow endless drift.
                bool full = _car.DriftTier >= 3 && (Bend(path, d, 15f) * side < 30f || held > _car.Stats.driftMaxCharge + 1f);
                _endingFor = charged && (steeringOut || curveOver || full) ? _endingFor + dt : 0f;
                if (_endingFor > 0.1f) { _autoDrift = false; _endedAt = Time.time; } // let go: boost
                return _autoDrift;
            }

            if (_car.ForwardSpeed < _car.Stats.driftMinSpeed) { _intoTurnFor = 0f; return false; }
            // A moment to settle after a drift before the next one (no restarting straight after the boost).
            if (_car.IsBoosting || Time.time - _endedAt < 0.6f) { _intoTurnFor = 0f; return false; }
            // Not right at the edge of the road (it would have to end at once).
            if (Mathf.Abs(_sensor.Current.Lateral) > track.OffRoadAt(d) * 0.65f) { _intoTurnFor = 0f; return false; }
            float bend = Bend(path, d, curveLength);
            bool curveThatWay = Mathf.Abs(bend) >= curveTurn && Mathf.Sign(bend) == Mathf.Sign(angle);
            float needed = curveThatWay ? autoStartInCurve : autoStartAnywhere;
            _intoTurnFor = Mathf.Abs(angle) >= needed ? _intoTurnFor + dt : 0f;
            if (_intoTurnFor >= autoStartTime)
            {
                _autoDrift = true;
                _intoTurnFor = _endingFor = 0f;
                _startedAt = Time.time;
            }
            return _autoDrift;
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
