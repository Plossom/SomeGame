using System.Collections.Generic;
using SomeGame.Car;
using Unity.Netcode;
using UnityEngine;

namespace SomeGame.Online
{
    /// <summary>
    /// Moves a car driven on another phone: keeps the last states the room sent and shows the car a
    /// little in the past (smoothly between two states), guessing ahead from its speed when a packet is late.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class NetCarPuppet : MonoBehaviour
    {
        const double Delay = 0.12;       // seconds shown in the past (a little more than two packets)
        const double MaxGuess = 0.3;     // how far ahead it may guess without news

        readonly List<CarState> _buffer = new();
        CarMovement _car;
        SomeGame.Track.TrackSensor _sensor;
        SpriteRenderer[] _sprites;
        float _vanish = -1f;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _sensor = GetComponent<SomeGame.Track.TrackSensor>();
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        }

        public void Push(CarState state)
        {
            if (_buffer.Count > 0 && state.Stamp <= _buffer[^1].Stamp) return; // old or repeated
            _buffer.Add(state);
            if (_buffer.Count > 30) _buffer.RemoveAt(0);
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
            if (_buffer.Count == 0 || NetworkManager.Singleton == null) return;
            double t = NetworkManager.Singleton.ServerTime.Time - Delay;
            // Drop states older than the pair around t.
            while (_buffer.Count > 2 && _buffer[1].Stamp <= t) _buffer.RemoveAt(0);

            CarState a = _buffer[0], b = _buffer.Count > 1 ? _buffer[1] : a;
            Vector2 position;
            float rotation;
            CarState now;
            if (t <= a.Stamp || _buffer.Count == 1)
            {
                // Only news from the past: guess ahead along its speed (a little).
                float ahead = (float)System.Math.Clamp(t - a.Stamp, 0, MaxGuess);
                position = a.Position + a.Velocity * ahead;
                rotation = a.Rotation;
                now = a;
            }
            else if (t >= b.Stamp)
            {
                float ahead = (float)System.Math.Clamp(t - b.Stamp, 0, MaxGuess);
                position = b.Position + b.Velocity * ahead;
                rotation = b.Rotation;
                now = b;
            }
            else
            {
                float k = (float)((t - a.Stamp) / (b.Stamp - a.Stamp));
                position = Vector2.Lerp(a.Position, b.Position, k);
                rotation = Mathf.LerpAngle(a.Rotation, b.Rotation, k);
                now = k < 0.5f ? a : b;
            }
            Vector2 velocity = Vector2.Lerp(a.Velocity, b.Velocity, 0.5f);
            float air = now.Air / 255f;
            if (now.Has(CarState.Airborne)) air = Mathf.Max(air, 0.02f);

            if (_sensor != null) _sensor.Elevated = now.Has(CarState.Elevated);
            if (_vanish < 0f)
            {
                bool show = !now.Has(CarState.Hidden);
                foreach (var s in _sprites) if (s != null && s.name == "Visual") s.enabled = show;
            }
            _car.DrivePuppet(position, rotation, velocity, now.Drift, now.Tier, air, now.Has(CarState.Boost), Time.fixedDeltaTime);
        }
    }
}
