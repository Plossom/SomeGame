using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The Winter Cup's life on the ground (nothing on the screen itself): powder snow sprayed up behind every
    /// vehicle (more at speed, a big plume while drifting, a puff on landing), pale ruts instead of dark tyre
    /// marks, snow glittering in the sun and fine snow blowing across the ground in gusts. Added by the
    /// <see cref="TrackRenderer"/> on snow tracks in Play mode.
    /// </summary>
    public class WinterFx : MonoBehaviour
    {
        Material _particles;
        bool _snowmobiles;
        readonly List<(SomeGame.Car.CarMovement car, ParticleSystem spray)> _sprays = new();
        ParticleSystem _glitter, _gusts;
        Camera _camera;
        float _gustUntil, _nextGust;
        Vector2 _wind = new(1f, -0.15f);

        /// <summary>0 calm .. 1 a storm: gusts come more often and blow harder (set by the race, e.g. the fjord breaking up).</summary>
        public static float Storm;

        static readonly Color Powder = new(0.94f, 0.97f, 1f, 0.9f);
        static readonly Color Rut = new(0.55f, 0.66f, 0.8f, 0.5f);

        public void Init(Material particles, bool snowmobiles)
        {
            _particles = particles;
            _snowmobiles = snowmobiles;
        }

        void Start()
        {
            foreach (var car in FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (_particles == null)
                {
                    var any = car.GetComponentInChildren<ParticleSystemRenderer>(true);
                    if (any != null) _particles = any.sharedMaterial;
                }
                foreach (var marks in car.GetComponents<SomeGame.Car.TyreMarks>()) marks.Tint = Rut;
            }
            foreach (var car in FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                car.Landed += () => Puff(car);
                _sprays.Add((car, Spray(car.transform)));
            }
            _camera = Camera.main;
            if (_camera != null)
            {
                _glitter = Glitter(_camera.transform);
                _gusts = Gusts(_camera.transform);
            }
            _nextGust = Time.time + Random.Range(2f, 5f);
        }

        void OnDestroy()
        {
            if (_glitter != null) Destroy(_glitter.gameObject);
            if (_gusts != null) Destroy(_gusts.gameObject);
        }

        void Update()
        {
            foreach (var (car, spray) in _sprays)
            {
                if (car == null || spray == null) continue;
                var emission = spray.emission;
                bool show = car.isActiveAndEnabled && !car.IsAirborne && car.gameObject.activeInHierarchy;
                float speed = car.Body.linearVelocity.magnitude;
                float top = car.Stats != null ? car.Stats.topSpeed : 18f;
                float rate = show ? Mathf.Clamp01((speed - 5f) / top) * (_snowmobiles ? 10f : 6f) : 0f;
                if (show && car.IsDrifting) rate += _snowmobiles ? 18f : 10f; // a plume in the drift
                if (show && car.IsOffRoad) rate += 8f;                       // deep snow off the road
                emission.rateOverTime = rate;
            }
            if (_camera == null || _glitter == null) return;
            // The glitter and the gusts cover what the camera sees.
            float h = _camera.orthographicSize * 2f, w = h * _camera.aspect;
            var shape = _glitter.shape;
            shape.scale = new Vector3(w * 1.1f, h * 1.1f, 0f);
            var glitter = _glitter.emission;
            glitter.rateOverTime = w * h * 0.06f;
            // Now and then a gust: fine snow streams across the ground for a few seconds.
            if (Time.time >= _nextGust)
            {
                _gustUntil = Time.time + Random.Range(2.5f, 4.5f) * (1f + Storm);
                _nextGust = _gustUntil + Random.Range(4f, 9f) * (1f - 0.8f * Storm);
                float a = Random.Range(-25f, 25f) * Mathf.Deg2Rad;
                _wind = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Random.value < 0.5f ? 1f : -1f);
                var velocity = _gusts.velocityOverLifetime;
                velocity.x = new ParticleSystem.MinMaxCurve(_wind.x * 10f, _wind.x * 16f);
                velocity.y = new ParticleSystem.MinMaxCurve(_wind.y * 10f, _wind.y * 16f);
            }
            var gustShape = _gusts.shape;
            gustShape.scale = new Vector3(w * 1.3f, h * 1.2f, 0f);
            var gusts = _gusts.emission;
            float strength = Time.time < _gustUntil ? Mathf.Sin(Mathf.InverseLerp(_gustUntil - 4.5f, _gustUntil, Time.time) * Mathf.PI) : 0f;
            gusts.rateOverTime = Mathf.Max(0f, strength) * w * h * 0.035f * (1f + 2f * Storm);
        }

        // Powder kicked up behind the vehicle, drifting back and spreading.
        ParticleSystem Spray(Transform car)
        {
            var system = Create("SnowSpray", car, new Vector3(0f, -1.3f, 0f), ParticleSystemSimulationSpace.World, 12, 200);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = Powder;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.35f;
            shape.rotation = new Vector3(90f, 0f, 0f); // backwards (the car faces +y)
            Grow(system, 1.8f);
            Fade(system, 0.6f);
            return system;
        }

        void Puff(SomeGame.Car.CarMovement car)
        {
            foreach (var (c, spray) in _sprays)
                if (c == car && spray != null) spray.Emit(_snowmobiles ? 10 : 6);
        }

        // Tiny sparkles winking on the snow all over the view.
        ParticleSystem Glitter(Transform cam)
        {
            var system = Create("SnowGlitter", cam, new Vector3(0f, 0f, 10f), ParticleSystemSimulationSpace.World, -60, 400);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new Color(1f, 1f, 1f, 1f);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            var twinkle = new Gradient();
            twinkle.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = twinkle;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.2f)));
            return system;
        }

        // Fine snow streaming across the ground in a gust (long, faint streaks).
        ParticleSystem Gusts(Transform cam)
        {
            var system = Create("SnowGusts", cam, new Vector3(0f, 0f, 10f), ParticleSystemSimulationSpace.World, 20, 500);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            main.startColor = new Color(1f, 1f, 1f, 0.55f);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(10f, 16f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2f, -1f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.4f;
            Fade(system, 0.6f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 3f;
            return system;
        }

        ParticleSystem Create(string name, Transform parent, Vector3 local, ParticleSystemSimulationSpace space, int order, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = space;
            main.maxParticles = max;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particles;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Play();
            return system;
        }

        static void Fade(ParticleSystem system, float alpha)
        {
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha, 0.3f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
        }

        static void Grow(ParticleSystem system, float to)
        {
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, to));
        }
    }
}
