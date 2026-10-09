using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Fakes height for the top-down car: a small hop whenever the drift button is pressed, and a big
    /// arc while the car flies off a ramp. The sprite scales up and back down while its shadow drifts
    /// away and returns. Only moves visuals, never the physics body.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class CarHop : MonoBehaviour
    {
        [Tooltip("The car sprite (child object), scaled to fake height.")]
        [SerializeField] Transform visual;
        [Tooltip("The drop shadow (child object), offset further while in the air.")]
        [SerializeField] Transform shadow;
        [SerializeField, Min(0.01f)] float duration = 0.28f;
        [Tooltip("Sprite scale at the top of the drift hop.")]
        [SerializeField, Min(1f)] float peakScale = 1.08f;
        [Tooltip("Sprite scale at the top of a ramp jump.")]
        [SerializeField, Min(1f)] float jumpScale = 1.3f;
        [Tooltip("Shadow offset at the top of a ramp jump (the hop uses shadowLift).")]
        [SerializeField] Vector2 jumpShadowLift = new(0.8f, -1f);
        [SerializeField] Vector2 shadowOffset = new(0.08f, -0.1f);
        [Tooltip("Extra shadow offset at the top of the hop.")]
        [SerializeField] Vector2 shadowLift = new(0.25f, -0.3f);

        CarMovement _car;
        float _time = -1f;
        Vector3 _visualScale = Vector3.one;

        /// <summary>True while the car is in the air (visually).</summary>
        public bool IsHopping => _time >= 0f;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            if (visual != null) _visualScale = visual.localScale;
        }
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
            if (_car.IsAirborne)
            {
                float h = _car.AirHeight;
                if (visual != null) visual.localScale = _visualScale * Mathf.Lerp(1f, jumpScale, h);
                if (shadow != null) shadow.localPosition = shadowOffset + jumpShadowLift * h;
                _time = -1f;
                return;
            }
            if (_time < 0f)
            {
                Apply(0f);
                return;
            }
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
            if (visual != null) visual.localScale = _visualScale * Mathf.Lerp(1f, peakScale, height);
            if (shadow != null) shadow.localPosition = shadowOffset + shadowLift * height;
        }
    }
}
