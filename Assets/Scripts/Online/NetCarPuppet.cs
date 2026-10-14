using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Online
{
    /// <summary>
    /// Moves a car driven on another phone. The car is shown a little in the past on its own steady
    /// timeline (the sender's clock, followed at a gently adjusted pace), so late or bunched packets do
    /// not make it jerk. Between two states it follows a curve through both positions and speeds; when news
    /// is late it carries on along its speed for a moment. The delay grows when packets arrive unevenly.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class NetCarPuppet : MonoBehaviour
    {
        const double MinDelay = 0.08, MaxDelay = 0.3;
        const double MaxGuess = 0.25;    // how far ahead it may guess without news

        readonly List<CarState> _buffer = new();
        CarMovement _car;
        SomeGame.Track.TrackSensor _sensor;
        SpriteRenderer[] _sprites;
        float _vanish = -1f;

        double _render = double.NaN;     // the moment of the sender's timeline being shown
        double _newest = double.NegativeInfinity;
        double _offset = double.NaN;     // the quickest recent travel time (arrival minus stamp)
        double _delay = 0.12;
        readonly Queue<(double at, double offset)> _arrivals = new();

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _sensor = GetComponent<SomeGame.Track.TrackSensor>();
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        }

        public void Push(CarState state)
        {
            if (state.Stamp <= _newest) return; // old or repeated
            _newest = state.Stamp;
            _buffer.Add(state);
            if (_buffer.Count > 40) _buffer.RemoveAt(0);

            // Travel times over the last two seconds: the quickest is the base (steady), the spread up to
            // the slowest few sets how far behind the car is shown so late packets are still in time.
            double now = Time.unscaledTimeAsDouble;
            _arrivals.Enqueue((now, now - state.Stamp));
            while (_arrivals.Count > 0 && now - _arrivals.Peek().at > 2.0) _arrivals.Dequeue();
            double quickest = double.MaxValue, slowest = double.MinValue;
            var sorted = new List<double>(_arrivals.Count);
            foreach (var (_, o) in _arrivals) { sorted.Add(o); quickest = System.Math.Min(quickest, o); }
            sorted.Sort();
            slowest = sorted[(int)((sorted.Count - 1) * 0.9)];
            _offset = double.IsNaN(_offset) ? quickest : _offset + (quickest - _offset) * 0.1;
            double wanted = System.Math.Clamp(1.0 / 30.0 + (slowest - quickest) + 0.025, MinDelay, MaxDelay);
            _delay += (wanted - _delay) * (wanted > _delay ? 0.2 : 0.02); // grows quickly, shrinks slowly
            if (double.IsNaN(_render)) _render = state.Stamp - _delay;
        }

        /// <summary>The driver left: the car fades out and is gone.</summary>
        public void Vanish()
        {
            if (_vanish < 0f) _vanish = 0f;
        }

        void Update()
        {
            if (_vanish < 0f) return;
            _vanish += Time.deltaTime;
            foreach (var s in _sprites)
            {
                if (s == null) continue;
                var c = s.color;
                c.a = Mathf.Clamp01(1f - _vanish / 0.6f);
                s.color = c;
            }
            if (_vanish >= 0.6f) gameObject.SetActive(false);
        }

        void FixedUpdate()
        {
            if (_buffer.Count == 0) return;
            float dt = Time.fixedDeltaTime;

            // Advance the shown moment at about real speed, a little faster or slower to stay `_delay` behind.
            double target = Time.fixedUnscaledTimeAsDouble - _offset - _delay;
            double error = target - _render;
            if (System.Math.Abs(error) > 0.5) _render = target; // far off (a pause, a hiccup): jump
            else _render += dt * (1.0 + System.Math.Clamp(error * 0.5, -0.04, 0.04)); // a barely noticeable pace change

            while (_buffer.Count > 2 && _buffer[1].Stamp <= _render) _buffer.RemoveAt(0);
            CarState a = _buffer[0], b = _buffer.Count > 1 ? _buffer[1] : a;
            Vector2 position, velocity;
            float rotation;
            CarState now;
            if (_buffer.Count == 1 || _render >= b.Stamp || b.Stamp - a.Stamp < 1e-4)
            {
                var last = _buffer[^1];
                float ahead = (float)System.Math.Clamp(_render - last.Stamp, 0, MaxGuess);
                position = last.Position + last.Velocity * ahead;
                velocity = last.Velocity;
                rotation = last.Rotation;
                now = last;
            }
            else if (_render <= a.Stamp)
            {
                position = a.Position;
                velocity = a.Velocity;
                rotation = a.Rotation;
                now = a;
            }
            else
            {
                float span = (float)(b.Stamp - a.Stamp);
                float t = (float)((_render - a.Stamp) / span);
                // Hermite curve through both positions with both speeds: round, not zig-zag.
                float t2 = t * t, t3 = t2 * t;
                position = (2f * t3 - 3f * t2 + 1f) * a.Position + (t3 - 2f * t2 + t) * span * a.Velocity
                         + (-2f * t3 + 3f * t2) * b.Position + (t3 - t2) * span * b.Velocity;
                // A teleport (put back after a splash) is not a curve.
                if ((b.Position - a.Position).sqrMagnitude > 64f) position = t < 0.5f ? a.Position : b.Position;
                velocity = Vector2.Lerp(a.Velocity, b.Velocity, t);
                rotation = Mathf.LerpAngle(a.Rotation, b.Rotation, t);
                now = t < 0.5f ? a : b;
            }

            float air = now.Air / 255f;
            if (now.Has(CarState.Airborne)) air = Mathf.Max(air, 0.02f);
            if (_sensor != null) _sensor.Elevated = now.Has(CarState.Elevated);
            if (_vanish < 0f)
            {
                bool show = !now.Has(CarState.Hidden);
                foreach (var s in _sprites) if (s != null && s.name == "Visual") s.enabled = show;
            }
            _car.DrivePuppet(position, rotation, velocity, now.Drift, now.Tier, air, now.Has(CarState.Boost), dt);
        }
    }
}
