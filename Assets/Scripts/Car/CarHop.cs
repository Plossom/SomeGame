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
        [Tooltip("Sprite scale and extra shadow offset while up on the roofs.")]
        [SerializeField, Min(1f)] float roofScale = 1.14f;
        [SerializeField] Vector2 roofShadowLift = new(0.6f, -0.75f);
        [Tooltip("How much narrower the car looks riding a banked wall (tilted on its side).")]
        [SerializeField, Range(0f, 0.6f)] float wallSquash = 0.3f;

        CarMovement _car;
        SomeGame.Track.TrackSensor _sensor;
        float _time = -1f;
        float _elevation;
        float _tilt;
        Vector3 _visualScale = Vector3.one;

        /// <summary>True while the car is in the air (visually).</summary>
        public bool IsHopping => _time >= 0f;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _sensor = GetComponent<SomeGame.Track.TrackSensor>();
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

        // Up on the roofs the car is a bit bigger (closer to the camera) and its shadow falls further away.
        Vector3 BaseScale => Vector3.Scale(_visualScale * Mathf.Lerp(1f, roofScale, _elevation), new Vector3(1f - wallSquash * _tilt, 1f, 1f));
        Vector2 BaseShadow => shadowOffset + roofShadowLift * _elevation;

        void Update()
        {
            bool up = _sensor != null && (_sensor.Elevated || _sensor.Upstairs);
            _elevation = Mathf.MoveTowards(_elevation, up ? 1f : 0f, Time.deltaTime * 4f);
            // On a banked wall the car leans on its side, more the higher up it rides.
            float lean = _sensor != null && _sensor.OnBank ? Mathf.Lerp(0.5f, 1f, _sensor.BankHeight) : 0f;
            _tilt = Mathf.MoveTowards(_tilt, lean, Time.deltaTime * 5f);
            if (_car.IsAirborne)
            {
                float h = _car.AirHeight;
                if (visual != null) visual.localScale = BaseScale * Mathf.Lerp(1f, jumpScale, h);
                if (shadow != null) shadow.localPosition = BaseShadow + jumpShadowLift * h;
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
            if (visual != null) visual.localScale = BaseScale * Mathf.Lerp(1f, peakScale, height);
            if (shadow != null) shadow.localPosition = BaseShadow + shadowLift * height;
        }
    }
}
