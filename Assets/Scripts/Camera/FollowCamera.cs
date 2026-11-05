using UnityEngine;

namespace SomeGame.CameraRig
{
    /// <summary>
    /// Orthographic follow camera. Never rotates (north stays up), smoothly follows the target and
    /// looks ahead in the direction of travel. Zoom is the single <see cref="orthographicSize"/> value.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] Rigidbody2D target;
        [Tooltip("Zoom: half the visible height in world units.")]
        [SerializeField, Min(1f)] float orthographicSize = 16f;
        [Tooltip("Seconds to catch up with the target (lower = tighter).")]
        [SerializeField, Min(0f)] float followSmoothTime = 0.12f;
        [Tooltip("Look this many seconds ahead along the velocity.")]
        [SerializeField, Min(0f)] float lookAheadTime = 0.35f;
        [SerializeField, Min(0f)] float maxLookAhead = 4.5f;
        [Tooltip("Scales sideways look-ahead; the portrait screen is narrow, so less lead left/right than up/down.")]
        [SerializeField, Range(0f, 1f)] float horizontalLookAheadFactor = 0.7f;
        [Tooltip("Constant camera offset from the car (world units).")]
        [SerializeField] Vector2 framingOffset = Vector2.zero;
        [Tooltip("The car never sits lower than this fraction of the screen height (0 = bottom, 0.5 = centre), " +
                 "so look-ahead while driving up the screen does not put it under the thumb.")]
        [SerializeField, Range(0f, 0.5f)] float minCarScreenHeight = 0.44f;
        [SerializeField, Min(0f)] float lookAheadSmoothTime = 0.5f;

        [Tooltip("How much further the camera pulls back at top speed, and again while boosting (a sense of speed).")]
        [SerializeField, Range(0f, 0.3f)] float speedZoom = 0.07f;
        [SerializeField, Range(0f, 0.3f)] float boostZoom = 0.06f;
        [Tooltip("Zoom while riding a gondola (the view widens to show the mountain).")]
        [SerializeField, Range(1f, 2f)] float rideZoom = 1.45f;

        Camera _camera;
        Vector2 _focus, _focusVelocity, _lookAhead, _lookAheadVelocity;
        SomeGame.Car.CarMovement _targetCar;
        float _speedZoom = 1f, _speedZoomVelocity;

        /// <summary>Extra offset added after smoothing (camera shake).</summary>
        public Vector2 Offset { get; set; }

        public float OrthographicSize
        {
            get => orthographicSize;
            set { orthographicSize = value; _camera.orthographicSize = value; }
        }

        public void SetTarget(Rigidbody2D body)
        {
            target = body;
            Snap();
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = orthographicSize;
        }

        void Start() => Snap();

        void OnValidate()
        {
            if (_camera != null) _camera.orthographicSize = orthographicSize;
        }

        /// <summary>Jumps straight to the target without smoothing.</summary>
        public void Snap()
        {
            if (target == null) return;
            _focus = target.transform.position;
            _focusVelocity = _lookAhead = _lookAheadVelocity = Vector2.zero;
            Apply();
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            Vector2 desiredLookAhead = Vector2.ClampMagnitude(target.linearVelocity * lookAheadTime, maxLookAhead);
            desiredLookAhead.x *= horizontalLookAheadFactor;
            _lookAhead = Vector2.SmoothDamp(_lookAhead, desiredLookAhead, ref _lookAheadVelocity, lookAheadSmoothTime, Mathf.Infinity, dt);
            // Faster (and boosting) the camera eases back a little, so speed is felt.
            if (_targetCar == null || _targetCar.Body != target) _targetCar = target.GetComponent<SomeGame.Car.CarMovement>();
            float wanted = 1f;
            if (_targetCar != null && _targetCar.Stats != null)
            {
                float fraction = Mathf.Clamp01(target.linearVelocity.magnitude / Mathf.Max(1f, _targetCar.Stats.topSpeed));
                wanted = 1f + speedZoom * fraction * fraction + (_targetCar.IsBoosting ? boostZoom : 0f);
                // Up on the gondola: look out over the mountain; shooting along a flume: a little wider for the speed.
                if (_targetCar.IsRiding)
                    wanted = _targetCar.CurrentRide == SomeGame.Car.RideKind.Lift ? rideZoom : _targetCar.CurrentRide == SomeGame.Car.RideKind.Flume ? 1.25f : wanted;
            }
            _speedZoom = Mathf.SmoothDamp(_speedZoom, wanted, ref _speedZoomVelocity, 0.6f, Mathf.Infinity, dt);
            _focus = Vector2.SmoothDamp(_focus, (Vector2)target.transform.position, ref _focusVelocity, followSmoothTime, Mathf.Infinity, dt);
            Apply();
        }

        void Apply()
        {
            Vector2 p = _focus + _lookAhead + framingOffset;
            // Keep the car at or above minCarScreenHeight: limit how far the camera sits above it.
            float carY = target != null ? target.transform.position.y : p.y;
            float size = orthographicSize * SomeGame.Input.ControlSettings.CameraZoomFactor * _speedZoom; // the player's zoom setting, eased back with speed
            p.y = Mathf.Min(p.y, carY + size * (1f - 2f * minCarScreenHeight));
            p += Offset;
            transform.SetPositionAndRotation(new Vector3(p.x, p.y, transform.position.z), Quaternion.identity);
            _camera.orthographicSize = size;
        }
    }
}
