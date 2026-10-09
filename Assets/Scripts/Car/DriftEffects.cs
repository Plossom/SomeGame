using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Shows drift charge with sparks at the rear wheels whose colour steps up with the charge tier,
    /// and a flame burst from the back of the car while a boost runs. Purely visual.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class DriftEffects : MonoBehaviour
    {
        [SerializeField] Material particleMaterial;
        [Tooltip("Rear wheel positions in local space (car faces +Y).")]
        [SerializeField] Vector2[] wheels = { new(-0.4f, -0.6f), new(0.4f, -0.6f) };
        [SerializeField] Vector2 exhaust = new(0f, -0.95f);
        [SerializeField] int sortingOrder = 12;

        [Header("Spark colour per charge tier (0 = charging, 1-3 = boost levels)")]
        [SerializeField] Color[] tierColors =
        {
            new(1f, 1f, 1f, 0.55f),
            new(0.35f, 0.65f, 1f),
            new(1f, 0.6f, 0.1f),
            new(0.9f, 0.35f, 1f),
        };
        [SerializeField] float[] tierRates = { 14f, 45f, 60f, 80f };
        [SerializeField] float[] tierSizes = { 0.18f, 0.27f, 0.33f, 0.42f };
        [Tooltip("Sparks burst out when the charge reaches a new tier.")]
        [SerializeField, Min(0)] int tierUpBurst = 14;

        [Header("Boost flame")]
        [SerializeField] Color flameStart = new(1f, 0.95f, 0.55f);
        [SerializeField] Color flameEnd = new(1f, 0.3f, 0.05f, 0f);
        [SerializeField] Vector2 flameRate = new(40f, 140f);
        [SerializeField] Vector2 flameSize = new(0.3f, 0.55f);

        CarMovement _car;
        ParticleSystem[] _sparks;
        ParticleSystem _flame;
        int _lastTier = -1;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _sparks = new ParticleSystem[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
                _sparks[i] = CreateSystem($"DriftSparks{i}", wheels[i], 0.3f, new Vector2(1.5f, 4f), 35f, Color.white, new Color(1f, 1f, 1f, 0f));
            _flame = CreateSystem("BoostFlame", exhaust, 0.35f, new Vector2(3f, 6f), 18f, flameStart, flameEnd);
            var flameMain = _flame.main;
            flameMain.startSize = new ParticleSystem.MinMaxCurve(flameSize.x, flameSize.y);
        }

        void LateUpdate()
        {
            int tier = _car.IsDrifting ? _car.DriftTier : -1;
            foreach (var sparks in _sparks)
            {
                var emission = sparks.emission;
                emission.rateOverTime = tier >= 0 ? tierRates[tier] : 0f;
                if (tier < 0) continue;
                var main = sparks.main;
                main.startColor = tierColors[tier];
                main.startSize = new ParticleSystem.MinMaxCurve(tierSizes[tier] * 0.6f, tierSizes[tier]);
                if (tier > _lastTier && tier > 0) sparks.Emit(tierUpBurst);
            }
            _lastTier = tier;

            var flameEmission = _flame.emission;
            flameEmission.rateOverTime = _car.IsBoosting ? Mathf.Lerp(flameRate.x, flameRate.y, _car.BoostStrength) : 0f;
        }

        ParticleSystem CreateSystem(string name, Vector2 localPosition, float lifetime, Vector2 speed, float spread, Color start, Color end)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = start;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            // Cone pointing backwards out of the car (-Y in local space).
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = spread;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

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
