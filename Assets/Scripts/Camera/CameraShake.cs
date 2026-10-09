using SomeGame.Car;
using UnityEngine;

namespace SomeGame.CameraRig
{
    /// <summary>Short positional shake on hard collisions of the watched car. Never rotates the camera.</summary>
    [RequireComponent(typeof(FollowCamera))]
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] CarMovement source;
        [Tooltip("Impact speed (units/s) below which nothing shakes.")]
        [SerializeField, Min(0f)] float minImpact = 5f;
        [Tooltip("Impact speed that gives the strongest shake.")]
        [SerializeField, Min(0.1f)] float maxImpact = 18f;
        [Tooltip("Largest shake offset in world units.")]
        [SerializeField, Min(0f)] float maxOffset = 0.6f;
        [Tooltip("How fast the shake dies away (trauma per second).")]
        [SerializeField, Min(0.1f)] float decay = 2.5f;
        [SerializeField, Min(1f)] float frequency = 25f;

        FollowCamera _camera;
        float _trauma;

        void Awake() => _camera = GetComponent<FollowCamera>();

        void OnEnable()
        {
            if (source != null) source.Collided += OnCollided;
        }

        void OnDisable()
        {
            if (source != null) source.Collided -= OnCollided;
            _trauma = 0f;
            if (_camera != null) _camera.Offset = Vector2.zero;
        }

        void OnCollided(float impact)
        {
            if (impact < minImpact) return;
            Shake(Mathf.InverseLerp(minImpact, maxImpact, impact) * 0.7f + 0.3f);
        }

        /// <summary>Adds shake, 0..1.</summary>
        public void Shake(float amount) => _trauma = Mathf.Clamp01(Mathf.Max(_trauma, amount));

        void Update()
        {
            if (_trauma <= 0f)
            {
                _camera.Offset = Vector2.zero;
                return;
            }

            float t = Time.time * frequency;
            float strength = _trauma * _trauma * maxOffset;
            _camera.Offset = new Vector2(Mathf.PerlinNoise(t, 0.1f) * 2f - 1f, Mathf.PerlinNoise(0.7f, t) * 2f - 1f) * strength;
            _trauma = Mathf.Max(0f, _trauma - decay * Time.deltaTime);
        }
    }
}
