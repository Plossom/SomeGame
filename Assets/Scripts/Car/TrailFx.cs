using SomeGame.Race;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// The trail from the garage: sparkles, fire, snowflakes or confetti streaming out behind the car while it
    /// drifts or boosts (more while boosting). Purely visual; added by <see cref="CarLook"/>.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class TrailFx : MonoBehaviour
    {
        CarMovement _car;
        ParticleSystem _system;
        Garage.Part _trail;

        static readonly Color[] Confetti =
        {
            new(1f, 0.36f, 0.42f), new(1f, 0.82f, 0.26f), new(0.36f, 0.84f, 0.5f), new(0.3f, 0.66f, 1f), new(0.78f, 0.5f, 1f),
        };

        public void Use(Garage.Part trail, Color paint)
        {
            _car = GetComponent<CarMovement>();
            _trail = trail;
            if (_system == null) _system = Create();
            var art = GarageArt.Instance?.Get(trail);
            var sheet = _system.textureSheetAnimation;
            sheet.enabled = art != null && art.full != null;
            if (sheet.enabled)
            {
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                while (sheet.spriteCount > 0) sheet.RemoveSprite(0);
                sheet.AddSprite(art.full);
            }
            var main = _system.main;
            main.startColor = trail.Id switch
            {
                "flame" => new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.1f)),
                "flake" => new ParticleSystem.MinMaxGradient(Color.white, new Color(0.75f, 0.9f, 1f)),
                "confetti" => new ParticleSystem.MinMaxGradient(Confetti[0], Confetti[3]) { mode = ParticleSystemGradientMode.RandomColor, gradient = ConfettiGradient() },
"winterconfetti" => new ParticleSystem.MinMaxGradient(Color.white, new Color(0.45f, 0.8f, 1f)),
                "aurora" => new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.7f), new Color(0.7f, 0.5f, 1f)),
                "bubbles" => new ParticleSystem.MinMaxGradient(Color.white, new Color(0.7f, 0.95f, 1f)),
                "rainbow" => new ParticleSystem.MinMaxGradient(Confetti[0], Confetti[3]) { mode = ParticleSystemGradientMode.RandomColor, gradient = ConfettiGradient() },
                "goldwake" => new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.55f), new Color(1f, 0.78f, 0.2f)),
                _ => new ParticleSystem.MinMaxGradient(Color.white, Color.Lerp(paint, Color.white, 0.5f)),
            };
            main.startSize = trail.Id == "confetti" ? new ParticleSystem.MinMaxCurve(0.14f, 0.24f) : new ParticleSystem.MinMaxCurve(0.2f, 0.42f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }

        static Gradient ConfettiGradient()
        {
            var g = new Gradient { mode = GradientMode.Fixed };
            var keys = new GradientColorKey[Confetti.Length];
            for (int i = 0; i < Confetti.Length; i++) keys[i] = new GradientColorKey(Confetti[i], (i + 1f) / Confetti.Length);
            g.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        void LateUpdate()
        {
            if (_system == null || _car == null) return;
            float rate = _car.IsAirborne ? 0f : _car.IsBoosting ? 40f : _car.IsDrifting ? 18f : 0f;
            var emission = _system.emission;
            emission.rateOverTime = rate;
        }

        ParticleSystem Create()
        {
            var go = new GameObject("GarageTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1f, 0.2f, 0f);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var colour = system.colorOverLifetime;
            colour.enabled = true;
            colour.color = fade;
            var spin = system.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (TryGetComponent(out DriftEffects drift)) renderer.sharedMaterial = drift.ParticleMaterial;
            renderer.sortingOrder = 9;
            system.Play();
            return system;
        }
    }
}
