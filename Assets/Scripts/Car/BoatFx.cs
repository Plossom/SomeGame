using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// A boat on the water (Water Cup): a V of foam spreading out behind it and a white churn straight behind the
    /// motor, spray off the bow in hard turns and boosts, a splash when it lands from a jump; less grip than a car
    /// (it glides and swings wide). Tyre marks are off. Added to every racer by the track renderer on a boat track.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class BoatFx : MonoBehaviour
    {
        const float Grip = 0.62f;

        CarMovement _car;
        ParticleSystem _wake, _spray;
        Vector2 _last;
        float _travelled;

        public static void Fit(CarMovement car, bool boat)
        {
            var fx = car.GetComponent<BoatFx>();
            if (boat && fx == null) car.gameObject.AddComponent<BoatFx>();
            if (!boat && fx != null) Destroy(fx);
            if (!boat) car.GripScale = 1f;
            foreach (var marks in car.GetComponents<TyreMarks>()) marks.enabled = !boat;
        }

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _car.GripScale = Grip;
            var material = Resources.Load<Material>("BoatFoam");
            _wake = System("Wake", material, 2, 260);
            _spray = System("Spray", material, 14, 120);
            _last = _car.Body.position;
            _car.Landed += OnLanded;
        }

        void OnDestroy()
        {
            if (_car != null) { _car.Landed -= OnLanded; _car.GripScale = 1f; }
            if (_wake != null) Destroy(_wake.gameObject);
            if (_spray != null) Destroy(_spray.gameObject);
        }

        ParticleSystem System(string name, Material material, int order, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startLifetime = 1f;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = false;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0.3f, 0.4f), new GradientAlphaKey(0f, 1f) });
            var colour = system.colorOverLifetime;
            colour.enabled = true;
            colour.color = fade;
            var slow = system.limitVelocityOverLifetime;
            slow.enabled = true;
            slow.limit = 0.2f;
            slow.dampen = 0.08f;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            system.Play();
            return system;
        }

        void Update()
        {
            if (_car == null || _wake == null) return;
            Vector2 p = _car.Body.position;
            _travelled += Vector2.Distance(p, _last);
            _last = p;
            if (_car.IsAirborne) { _travelled = 0f; return; }
            Vector2 forward = transform.up, right = transform.right;
            float speed = _car.Body.linearVelocity.magnitude;
            // The wake: a pair of foam puffs every little stretch, spreading sideways (a V), and the churn behind.
            while (_travelled > 0.3f && speed > 1.5f)
            {
                _travelled -= 0.3f;
                Vector2 stern = p - forward * 0.85f;
                float spread = Mathf.Lerp(0.7f, 1.8f, Mathf.Clamp01(speed / 20f));
                for (int side = -1; side <= 1; side += 2)
                    Emit(_wake, stern + right * (0.4f * side), right * (spread * side) - forward * 0.3f, Random.Range(0.65f, 0.85f), 1.15f, WakeAlpha);
                Emit(_wake, stern - forward * 0.2f, -forward * 0.6f + right * Random.Range(-0.3f, 0.3f), Random.Range(0.8f, 1.05f), 0.7f, WakeAlpha);
            }
            if (speed <= 1.5f) _travelled = 0f;
            // Spray off the bow: in hard turns (sideways speed), drifts and boosts.
            float slide = Mathf.Abs(_car.SidewaysSpeed);
            float sprayRate = (_car.IsBoosting ? 50f : 0f) + (_car.IsDrifting ? 30f : 0f) + Mathf.Clamp01((slide - 2f) / 6f) * 40f;
            int sprayCount = Mathf.FloorToInt(sprayRate * Time.deltaTime + Random.value);
            for (int i = 0; i < sprayCount && speed > 4f; i++)
            {
                int side = _car.SidewaysSpeed > 0.5f ? -1 : _car.SidewaysSpeed < -0.5f ? 1 : (Random.value < 0.5f ? -1 : 1);
                Vector2 bow = p + forward * Random.Range(0.1f, 0.7f) + right * (0.45f * side);
                Emit(_spray, bow, right * (side * Random.Range(3f, 6f)) + forward * Random.Range(0f, 2f), Random.Range(0.18f, 0.32f), Random.Range(0.3f, 0.5f));
            }
        }

        void OnLanded()
        {
            if (_spray == null) return;
            Vector2 p = _car.Body.position;
            for (int i = 0; i < 26; i++)
            {
                float a = i / 26f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Emit(_spray, p + dir * 0.5f, dir * Random.Range(3f, 7f), Random.Range(0.2f, 0.4f), Random.Range(0.35f, 0.6f));
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Emit(_wake, p + dir * 0.6f, dir * 1.6f, 0.9f, 1.2f);
            }
        }

        // The wake a little see-through (it reads as foam, not a white trail).
        const float WakeAlpha = 0.75f;

        static void Emit(ParticleSystem system, Vector2 position, Vector2 velocity, float size, float life, float alpha = 1f)
        {
            var e = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startSize = size,
                startLifetime = life,
                startColor = new Color(1f, 1f, 1f, alpha),
                rotation = Random.Range(0f, 360f),
            };
            system.Emit(e, 1);
        }
    }
}
