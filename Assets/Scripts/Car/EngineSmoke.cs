using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Exhaust smoke from the back of the car: soft puffs that grow and fade behind it. It only shows
    /// while the car accelerates hard from low speed (the start, slow corners, after a crash) and fades
    /// out as the car gets up to speed; a boost adds a thick trail. Purely visual.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class EngineSmoke : MonoBehaviour
    {
        [SerializeField] Material particleMaterial;
        [Tooltip("Exhaust pipe positions in local space (car faces +Y).")]
        [SerializeField] Vector2[] exhausts = { new(-0.32f, -1.12f), new(0.32f, -1.12f) };
        [SerializeField] int sortingOrder = 8;

        [Header("Amount")]
        [Tooltip("Puffs per second per pipe at full intensity.")]
        [SerializeField, Min(0f)] float maxRate = 30f;
        [Tooltip("Speed (fraction of top speed) at which the smoke has faded out completely.")]
        [Range(0.1f, 1f)] [SerializeField] float fadeOutSpeed = 0.8f;
        [Tooltip("Extra puffs per world unit travelled at full intensity, so a fast car leaves a continuous trail instead of dots.")]
        [SerializeField, Min(0f)] float ratePerUnit = 2.5f;
        [Tooltip("Intensity added while a boost runs.")]
        [SerializeField, Min(0f)] float boostIntensity = 1f;
        [Tooltip("How quickly the smoke follows throttle changes.")]
        [SerializeField, Min(0.1f)] float response = 6f;

        [Header("Look")]
        [SerializeField] Color lightSmoke = new(1f, 1f, 0.98f, 0.45f);
        [SerializeField] Color heavySmoke = new(0.82f, 0.82f, 0.8f, 0.8f);
        [SerializeField] Vector2 lifetime = new(0.6f, 1.2f);
        [SerializeField] Vector2 startSize = new(0.4f, 0.65f);
        [Tooltip("Size multiplier at the end of a puff's life.")]
        [SerializeField, Min(1f)] float growth = 3.5f;

        CarMovement _car;
        ParticleSystem[] _systems;
        float _intensity;

        /// <summary>Current smoke intensity (0 = none, 1 = full throttle from standstill, more while boosting).</summary>
        public float Intensity => _intensity;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _systems = new ParticleSystem[exhausts.Length];
            for (int i = 0; i < exhausts.Length; i++) _systems[i] = CreateSystem($"ExhaustSmoke{i}", exhausts[i]);
        }

        void LateUpdate()
        {
            float throttle = Mathf.Clamp01(_car.Throttle);
            float speed01 = _car.Stats != null ? Mathf.Clamp01(_car.ForwardSpeed / _car.Stats.topSpeed) : 0f;
            float lowSpeed = 1f - Mathf.Clamp01(speed01 / fadeOutSpeed);
            float target = throttle * lowSpeed;
            if (_car.IsBoosting) target += boostIntensity * Mathf.Max(0.5f, _car.BoostStrength);
            _intensity = Mathf.MoveTowards(_intensity, target, response * Time.deltaTime);

            float heavy = Mathf.Clamp01(_intensity);
            foreach (var system in _systems)
            {
                var emission = system.emission;
                emission.rateOverTime = maxRate * _intensity;
                emission.rateOverDistance = ratePerUnit * _intensity;
                var main = system.main;
                main.startColor = Color.Lerp(lightSmoke, heavySmoke, heavy * heavy);
                main.startSize = new ParticleSystem.MinMaxCurve(startSize.x * (0.8f + heavy * 0.5f), startSize.y * (0.8f + heavy * 0.5f));
            }
        }

        ParticleSystem CreateSystem(string name, Vector2 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            // Puffs leave backwards (-Y in local space) in a narrow cone and slow down quickly.
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.04f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.2f;
            limit.dampen = 0.08f;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.5f, 0.55f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, growth));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            system.Play();
            return system;
        }
    }
}
