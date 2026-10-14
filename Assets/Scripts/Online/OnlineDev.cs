using System;
using UnityEngine;

namespace SomeGame.Online
{
    /// <summary>
    /// Development: command line switches for testing a room with a second copy of the game on one
    /// computer. "-onlineLocal" connects over 127.0.0.1, "-autoJoin CODE" joins at the map, "-autoReady"
    /// taps READY, "-autoDrive" lets your car drive itself round the track. Off in release builds.
    /// </summary>
    public static class OnlineDev
    {
        public static string AutoJoin => Value("-autoJoin");
        public static bool AutoReady => Has("-autoReady");
        public static bool AutoDrive => Has("-autoDrive") || ForceAutoDrive;

        /// <summary>Set from the Editor for tests.</summary>
        public static bool ForceAutoDrive;

        static bool Has(string flag)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0;
#else
            return false;
#endif
        }

        static string Value(string flag)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
#else
            return null;
#endif
        }
    }

    /// <summary>Development: steers your car along the road ahead (tests with "-autoDrive").</summary>
    public class OnlineAutoPilot : MonoBehaviour
    {
        SomeGame.Input.PlayerCarInput _input;
        SomeGame.Track.TrackSensor _sensor;
        SomeGame.Car.CarMovement _car;

        void Awake()
        {
            _input = GetComponent<SomeGame.Input.PlayerCarInput>();
            _sensor = GetComponent<SomeGame.Track.TrackSensor>();
            _car = GetComponent<SomeGame.Car.CarMovement>();
        }

        void Update()
        {
            if (_input == null || _sensor == null || _sensor.Track == null) return;
            var path = _sensor.Track.Path;
            float d = _sensor.Current.Distance;
            Vector2 target = path.PointAt(d + 9f + Mathf.Max(0f, _car.ForwardSpeed) * 0.3f);
            _input.ScriptedStick = (target - _car.Body.position).normalized;
        }
    }
}

#if UNITY_EDITOR
namespace SomeGame.Online
{
    /// <summary>Editor test: how evenly a car moves on screen (frame-to-frame speed changes).</summary>
    public class MotionProbe : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Transform Target;
        readonly System.Collections.Generic.List<float> _speeds = new();
        UnityEngine.Vector3 _last;
        bool _started;

        void LateUpdate()
        {
            if (Target == null) return;
            var p = Target.position;
            if (_started && UnityEngine.Time.deltaTime > 0f) _speeds.Add((p - _last).magnitude / UnityEngine.Time.deltaTime);
            _last = p;
            _started = true;
        }

        /// <summary>Mean speed, and mean size of the change in speed between frames (lower = smoother).</summary>
        public string Report()
        {
            if (_speeds.Count < 3) return "no data";
            float mean = 0f, jerk = 0f, worst = 0f;
            for (int i = 0; i < _speeds.Count; i++) mean += _speeds[i];
            for (int i = 1; i < _speeds.Count; i++) { float d = UnityEngine.Mathf.Abs(_speeds[i] - _speeds[i - 1]); jerk += d; worst = UnityEngine.Mathf.Max(worst, d); }
            return $"frames={_speeds.Count} speed={mean / _speeds.Count:0.0} jitter={jerk / (_speeds.Count - 1):0.00} worst={worst:0.0}";
        }
    }
}
#endif
