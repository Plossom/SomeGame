using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Small visual hop whenever the drift button is pressed: the car sprite scales up and back down
    /// while its shadow drifts away and returns. Only moves visuals, never the physics body.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class CarHop : MonoBehaviour
    {
        [Tooltip("The car sprite (child object), scaled to fake height.")]
        [SerializeField] Transform visual;
        [Tooltip("The drop shadow (child object), offset further while in the air.")]
        [SerializeField] Transform shadow;
        [SerializeField, Min(0.01f)] float duration = 0.28f;
        [Tooltip("Sprite scale at the top of the hop.")]
        [SerializeField, Min(1f)] float peakScale = 1.22f;
        [SerializeField] Vector2 shadowOffset = new(0.08f, -0.1f);
        [Tooltip("Extra shadow offset at the top of the hop.")]
        [SerializeField] Vector2 shadowLift = new(0.25f, -0.3f);

        CarMovement _car;
        float _time = -1f;

        /// <summary>True while the car is in the air (visually).</summary>
        public bool IsHopping => _time >= 0f;

        void Awake() => _car = GetComponent<CarMovement>();
        void OnEnable() => _car.Hopped += Hop;

        void OnDisable()
        {
            _car.Hopped -= Hop;
            _time = -1f;
            Apply(0f);
        }

        void Hop() => _time = 0f;

        void Update()
        {
            if (_time < 0f) return;
            _time += Time.deltaTime;
            float t = _time / duration;
            if (t >= 1f)
            {
                _time = -1f;
                Apply(0f);
                return;
            }
            Apply(Mathf.Sin(t * Mathf.PI)); // up and down
        }

        void Apply(float height)
        {
            if (visual != null) visual.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, height);
            if (shadow != null) shadow.localPosition = shadowOffset + shadowLift * height;
        }
    }
}
