using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// Racing moments on the player's car, announced like tricks (they count in combos):
    /// <list type="bullet">
    /// <item>ROCKET START: put your thumb down just before GO (on "1", not earlier) and you shoot off with a boost.
    /// Pressing too early (already on "2") gets no boost.</item>
    /// <item>SLIPSTREAM: tuck in close behind a rival at speed; wind streaks build up round the car and after a
    /// moment you slingshot past with a boost.</item>
    /// <item>OVERTAKE: every place gained pops up.</item>
    /// </list>
    /// Added to the player's car by the <see cref="RaceManager"/>.
    /// </summary>
    public class RaceFeel : MonoBehaviour
    {
        const float SlipRange = 11f;        // metres behind a rival
        const float SlipAngle = 16f;        // degrees off its line
        const float SlipCharge = 1.1f;      // seconds tucked in for the boost
        const float SlipCooldown = 2.5f;

        RaceManager _race;
        CarMovement _car;
        ICarInput _input;
        RaceProgress _progress;
        SomeGame.UI.ComboDisplay _combo;
        ParticleSystem _wind;

        float _goAt = -1f, _pressedAt = -1f;
        bool _pressedEarly;
        float _slip, _slipReadyAt;
        int _place = -1;
        float _placeAnnouncedAt = -10f;

        static readonly Color RocketColor = new(1f, 0.42f, 0.2f);
        static readonly Color SlipColor = new(0.55f, 0.9f, 1f);
        static readonly Color OvertakeColor = new(0.45f, 0.95f, 0.45f);

        public void Init(RaceManager race)
        {
            _race = race;
            _car = GetComponent<CarMovement>();
            _input = GetComponent<ICarInput>();
            _progress = GetComponent<RaceProgress>();
            race.CountdownTick += OnTick;
        }

        void OnDestroy()
        {
            if (_race != null) _race.CountdownTick -= OnTick;
        }

        void Start()
        {
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _wind = CreateWind();
        }

        void OnTick(int n)
        {
            if (n == 1) _goAt = Time.time + 1f; // GO comes one step after "1"
            if (n != 0) return;
            // GO: thumb down since just before it (not since "2" or earlier) is a rocket start.
            bool pressing = _input != null && _input.Throttle > 0f;
            if (pressing && !_pressedEarly && _pressedAt >= 0f)
            {
                _car.GiveBoost(0.8f);
                Announce("ROCKET START!", RocketColor);
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.boost, 0.9f);
            }
            _goAt = -1f;
        }

        void Update()
        {
            if (_race == null || _car == null) return;
            if (_race.State == RaceState.Countdown) WatchStart();
            else if (_race.State == RaceState.Racing && (_progress == null || !_progress.IsFinished))
            {
                Slipstream();
                Overtakes();
            }
            else SetWind(0f);
        }

        // Before GO: remember when the thumb went down; down before the window (and kept down) is too early.
        void WatchStart()
        {
            bool pressing = _input != null && _input.Throttle > 0f;
            if (!pressing)
            {
                _pressedAt = -1f;
                _pressedEarly = false;
                return;
            }
            if (_pressedAt >= 0f) return;
            _pressedAt = Time.time;
            _pressedEarly = _goAt < 0f; // before "1" showed
        }

        void Slipstream()
        {
            float top = _car.Stats != null ? _car.Stats.topSpeed : 18f;
            bool fast = _car.ForwardSpeed > top * 0.6f && !_car.IsAirborne;
            bool behind = false;
            if (fast)
                foreach (var other in _race.Standings)
                {
                    if (other == null || other == _progress) continue;
                    Vector2 toOther = (Vector2)(other.transform.position - transform.position);
                    float distance = toOther.magnitude;
                    if (distance < 2.5f || distance > SlipRange) continue;
                    if (Vector2.Angle(transform.up, toOther) > SlipAngle) continue;
                    if (Vector2.Angle(transform.up, other.transform.up) > 30f) continue; // going the same way
                    behind = true;
                    break;
                }
            bool ready = Time.time >= _slipReadyAt;
            _slip = behind && ready ? _slip + Time.deltaTime : Mathf.Max(0f, _slip - Time.deltaTime * 2f);
            SetWind(ready ? Mathf.Clamp01(_slip / SlipCharge) : 0f);
            if (_slip < SlipCharge) return;
            _slip = 0f;
            _slipReadyAt = Time.time + SlipCooldown;
            _car.GiveBoost(0.6f);
            Announce("SLIPSTREAM!", SlipColor);
        }

        void Overtakes()
        {
            int place = _race.PositionOf(_progress);
            if (_race.RaceTime < 3f) { _place = place; return; } // places shuffle as the grid gets going
            if (_place < 0) { _place = place; return; }
            if (place < _place && Time.time - _placeAnnouncedAt > 1.2f)
            {
                Announce(place == 1 ? "TAKE THE LEAD!" : "OVERTAKE!", OvertakeColor);
                _placeAnnouncedAt = Time.time;
            }
            _place = place;
        }

        void Announce(string text, Color color)
        {
            if (_combo != null) _combo.Announce(text, color);
        }

        // Wind streaks streaming past the car while it is in a slipstream (more as it charges).
        void SetWind(float amount)
        {
            if (_wind == null) return;
            var emission = _wind.emission;
            emission.rateOverTime = amount * 40f;
        }

        ParticleSystem CreateWind()
        {
            var source = GetComponentInChildren<ParticleSystemRenderer>(true);
            var go = new GameObject("SlipstreamWind");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = new Color(1f, 1f, 1f, 0.7f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 60;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.6f, 0.5f, 0f);
            // Streaks rush back along the car.
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-22f, -16f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 2f;
            if (source != null) renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingOrder = 14;
            system.Play();
            return system;
        }
    }
}
