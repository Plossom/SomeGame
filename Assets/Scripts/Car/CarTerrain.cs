using System;
using System.Collections;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// What the ground under the car does: a kicker ramp before a river launches the car into the
    /// air, water (missing the ramp, landing short, driving into a lake) means a splash and a restart a
    /// little way back, and an oil puddle makes the car slide. Also plays the splash and landing dust.
    /// </summary>
    [RequireComponent(typeof(CarMovement), typeof(TrackSensor))]
    public class CarTerrain : MonoBehaviour
    {
        [SerializeField] Material particleMaterial;
        [Tooltip("Cars slower than this along the road just roll off the ramp into the water.")]
        [SerializeField, Min(0f)] float minLaunchSpeed = 4f;
        [Tooltip("How far back along the road a car restarts after a splash.")]
        [SerializeField, Min(0f)] float respawnBack = 6f;
        [Tooltip("Seconds the car blinks after a restart.")]
        [SerializeField, Min(0f)] float blinkSeconds = 1.2f;
        [SerializeField] int sortingOrder = 13;

        CarMovement _car;
        TrackSensor _sensor;
        ParticleSystem _splash, _dust;
        SpriteRenderer[] _sprites;

        /// <summary>The car fell into water and is being put back on the road.</summary>
        public event Action Splashed;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _sensor = GetComponent<TrackSensor>();
            _sprites = GetComponentsInChildren<SpriteRenderer>();
            _splash = CreateBurst("Splash", new Color(0.85f, 0.95f, 1f, 0.9f), new Vector2(3f, 7f), new Vector2(0.25f, 0.55f), 0.7f);
            _dust = CreateBurst("LandingDust", new Color(0.93f, 0.86f, 0.7f, 0.7f), new Vector2(1.5f, 4f), new Vector2(0.4f, 0.8f), 0.6f);
        }

        void OnEnable() => _car.Landed += OnLanded;
        void OnDisable() => _car.Landed -= OnLanded;

        void FixedUpdate()
        {
            var track = _sensor.Track;
            if (track == null || _car.IsAirborne) return;
            Vector2 position = _car.Body.position;
            var point = _sensor.Current;

            // Take-off: at the end of a ramp, moving forward along the road.
            var ramp = track.RampAt(point.Distance, point.Lateral);
            if (ramp.HasValue)
            {
                float toLip = track.Path.DeltaDistance(point.Distance, ramp.Value.Lip);
                float along = Vector2.Dot(_car.Body.linearVelocity, track.Path.TangentAt(point.Distance));
                if (toLip < 0.8f && along > minLaunchSpeed)
                {
                    var stats = _car.Stats;
                    _car.Launch(stats.airTimeBase + stats.airTimePerSpeed * along);
                    return;
                }
            }

            if (track.IsWater(position))
            {
                Splash(track);
                return;
            }
            if (!_car.IsSlipping && track.IsOil(position))
                _car.Slip(_car.Stats.oilSlideSeconds, (UnityEngine.Random.value < 0.5f ? -1f : 1f) * _car.Stats.oilSpin);
        }

        void OnLanded()
        {
            var track = _sensor.Track;
            _sensor.Sample();
            if (track != null && track.IsWater(_car.Body.position)) Splash(track);
            else _dust.Emit(18);
        }

        void Splash(SomeGame.Track.Track track)
        {
            _splash.transform.position = _car.Body.position;
            _splash.Emit(40);
            _car.Respawn(track.RespawnBefore(_sensor.Current.Distance, respawnBack));
            Splashed?.Invoke();
            StopAllCoroutines();
            StartCoroutine(Blink());
        }

        IEnumerator Blink()
        {
            for (float t = 0f; t < blinkSeconds; t += Time.deltaTime)
            {
                bool visible = Mathf.Repeat(t * 8f, 1f) < 0.6f;
                foreach (var s in _sprites) if (s != null) s.enabled = visible;
                yield return null;
            }
            foreach (var s in _sprites) if (s != null) s.enabled = true;
        }

        ParticleSystem CreateBurst(string name, Color color, Vector2 speed, Vector2 size, float lifetime)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.3f;
            limit.dampen = 0.12f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;
            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.8f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = sortingOrder;
            system.Play();
            return system;
        }
    }
}
