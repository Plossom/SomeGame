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
        [Tooltip("Sprite scale while up on the roofs (closer to the camera).")]
        [SerializeField, Min(1f)] float roofScale = 1.14f;
        [Tooltip("How much narrower the car looks riding a banked wall (tilted on its side).")]
        [SerializeField, Range(0f, 0.6f)] float wallSquash = 0.3f;

        CarMovement _car;
        SomeGame.Track.TrackSensor _sensor;
        float _time = -1f;
        float _elevation;
        float _tilt;
        Vector3 _visualScale = Vector3.one, _shadowScale = Vector3.one;

        /// <summary>True while the car is in the air (visually).</summary>
        public bool IsHopping => _time >= 0f;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _sensor = GetComponent<SomeGame.Track.TrackSensor>();
            if (visual != null) _visualScale = visual.localScale;
            if (shadow != null) _shadowScale = shadow.localScale;
        }
        void OnEnable()
        {
            _car.Hopped += Hop;
            _car.TrickStarted += OnTrick;
        }

        float _trickAt = -10f;

        // A trick: one full spin in the air (and a whoosh for the player's car).
        void OnTrick()
        {
            _trickAt = Time.time;
            if (GetComponent<SomeGame.Input.PlayerCarInput>() != null)
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.trick, 0.8f);
        }

        void OnDisable()
        {
            _car.Hopped -= Hop;
            _car.TrickStarted -= OnTrick;
            _time = -1f;
            Apply(0f);
        }

        void Hop() => _time = 0f;

        // Up on the roofs the car is a bit bigger (closer to the camera); it rests on the roof, so its shadow
        // falls right under it as on the road.
        Vector3 BaseScale => Vector3.Scale(_visualScale * Mathf.Lerp(1f, roofScale, _elevation), new Vector3(1f - wallSquash * _tilt, 1f, 1f));
        Vector2 BaseShadow => shadowOffset;

        void Update()
        {
            // Upstairs in a house the car rests on that floor, so it looks the same as downstairs.
            bool up = _sensor != null && (_sensor.Elevated || _sensor.Aboard || _sensor.OnViaduct); // on the roofs, on a train, on a viaduct
            _elevation = Mathf.MoveTowards(_elevation, up ? 1f : 0f, Time.deltaTime * 4f);
            // On a banked wall the car leans on its side, more the higher up it rides.
            float lean = _sensor != null && _sensor.OnBank ? Mathf.Lerp(0.5f, 1f, _sensor.BankHeight) : 0f;
            _tilt = Mathf.MoveTowards(_tilt, lean, Time.deltaTime * 5f);
            if (_car.IsAirborne)
            {
                float h = _car.AirHeight;
                // Fallen into a chasm: the car shrinks away into the depths, tumbling, its shadow gone.
                float fall = _car.FallDepth;
                if (visual != null) visual.localScale = BaseScale * Mathf.Lerp(1f, jumpScale, h) * Mathf.Lerp(1f, 0.25f, fall);
                if (shadow != null)
                {
                    shadow.localPosition = BaseShadow + jumpShadowLift * h;
                    shadow.localScale = _shadowScale * (1f - fall);
                }
                // The trick: a spin turns the car round once (left or right); a flip squeezes it end over end
                // (front: its nose dips away first; back: its tail), showing the underside half way.
                float spin = Mathf.Clamp01((Time.time - _trickAt) / Mathf.Max(0.05f, _car.TrickSpin));
                float eased = 1f - (1f - spin) * (1f - spin);
                var kind = _car.LastTrick;
                bool flip = spin < 1f && (kind == TrickKind.FrontFlip || kind == TrickKind.BackFlip);
                float turn = spin < 1f && !flip ? 360f * eased * (kind == TrickKind.SpinLeft ? 1f : -1f) : 0f;
                if (visual != null)
                {
                    visual.localRotation = Quaternion.Euler(0f, 0f, turn + fall * (_car.IsPlunging ? 25f : 150f));
                    if (flip)
                    {
                        var sc = visual.localScale;
                        visual.localScale = new Vector3(sc.x, sc.y * Mathf.Cos(eased * Mathf.PI * 2f), sc.z);
                        visual.localPosition = new Vector3(0f, Mathf.Sin(eased * Mathf.PI) * (kind == TrickKind.FrontFlip ? 0.6f : -0.6f), 0f);
                    }
                    else if (visual.localPosition != Vector3.zero) visual.localPosition = Vector3.zero;
                }
                _time = -1f;
                return;
            }
            if (visual != null && visual.localRotation != Quaternion.identity) visual.localRotation = Quaternion.identity;
            if (visual != null && visual.localPosition != Vector3.zero) visual.localPosition = Vector3.zero;
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
            if (shadow != null && shadow.localScale != _shadowScale) shadow.localScale = _shadowScale;
            if (visual != null) visual.localScale = BaseScale * Mathf.Lerp(1f, peakScale, height);
            if (shadow != null) shadow.localPosition = BaseShadow + shadowLift * height;
        }
    }
}
