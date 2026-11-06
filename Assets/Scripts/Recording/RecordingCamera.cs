#if UNITY_EDITOR
using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.CameraRig;
using SomeGame.Race;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Recording
{
    /// <summary>
    /// The camera of a recorded run: top-down, turned so the car's direction of travel points up the screen, a
    /// little ahead of the car, and zooming by itself. Overview shows a fork coming (it wins over close, since most
    /// forks lie right behind a ramp), close goes in before jumps and near-misses, normal is the gameplay distance.
    /// Big jumps also slow time around the landing; landings shake the view lightly.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RecordingCamera : MonoBehaviour
    {
        enum Zoom { Normal, Close, Overview }

        const float VideoStep = 1f / ShortsOptions.FrameRate; // zooms and shakes keep video time, through slow motion too
        const float HeadingSmooth = 0.35f, ZoomSmooth = 0.3f;
        const float NormalLead = 0.25f, OverviewLead = 0.7f;  // shares of the view height ahead of the car
        const float CloseScale = 0.62f, OverviewMinScale = 1.25f, OverviewMaxScale = 3f;
        const float ForkWarning = 2f;      // seconds before a fork: in overview by 1.5 s before it
        const float ForkHold = 12f;        // metres into the fork, then back to normal
        const float JumpWarning = 1f;      // seconds before a ramp
        const float NearWarning = 0.9f;    // seconds before a predicted near-miss
        const float NearLook = 16f, NearGap = 1.2f, NearMinSpeed = 5f, NearMaxSize = 6f, NearGrace = 3f;
        const int CloseMinFrames = 60, CloseAfterFrames = 24, CloseGapFrames = 48;
        const float SlowScale = 0.3f;
        const int SlowFrames = 90;         // 1.5 seconds of video
        const float SlowCooldown = 6f;     // seconds of game time between slow motions
        const float ShakeMinAir = 0.4f;
        const int ShakeFrames = 24;
        const float ShakeSize = 0.3f;      // metres at the normal zoom

        CarMovement _car;
        TrackSensor _sensor;
        ShortsOptions _options;
        RaceEventLog _log;
        RaceManager _race;
        Camera _camera;
        Transform _night;
        readonly List<Collider2D> _found = new();

        float _normal, _size, _sizeVelocity, _lead = NormalLead, _leadVelocity;
        float _heading, _headingVelocity;
        Zoom _zoom = Zoom.Normal;
        int _fork = -1, _closeUntil;
        float _overview;
        float _airPlanned;
        bool _slowPending;
        int _slowLeft, _shakeLeft;
        float _slowReadyAt, _baseFixedDelta;

        public void Init(RaceManager race, CarMovement car, ShortsOptions options, RaceEventLog log)
        {
            _race = race;
            _car = car;
            _sensor = car.GetComponent<TrackSensor>();
            _options = options;
            _log = log;
            _camera = GetComponent<Camera>();
            _baseFixedDelta = Time.fixedDeltaTime;
            car.Launched += OnLaunched;
            car.Landed += OnLanded;
            if (options.camera == ShortsCamera.Gameplay) return;
            var follow = GetComponent<FollowCamera>();
            follow.enabled = false;
            _normal = _size = follow.OrthographicSize * SomeGame.Input.ControlSettings.CameraZoomFactor;
            _heading = Angle(car.transform.up);
            Application.onBeforeRender += CoverNight;
        }

        void OnDestroy()
        {
            Application.onBeforeRender -= CoverNight;
            EndSlow();
            if (_car == null) return;
            _car.Launched -= OnLaunched;
            _car.Landed -= OnLanded;
        }

        // The winter night tint is an upright rectangle round the camera: turned, the view would reach past it.
        void CoverNight()
        {
            if (_night == null)
                foreach (var sprite in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                    if (sprite.name == "Night") { _night = sprite.transform; break; }
            if (_night == null) return;
            float across = _camera.orthographicSize * 4.5f;
            _night.localScale = new Vector3(across, across, 1f);
        }

        void OnLaunched(float air)
        {
            _airPlanned = air;
            HoldClose(CloseMinFrames);
            if (air >= _options.bigJumpAir) _slowPending = _options.slowMotion && Time.time >= _slowReadyAt;
        }

        void OnLanded()
        {
            if (_slowPending) BeginSlow();
            if (_airPlanned >= ShakeMinAir) _shakeLeft = ShakeFrames;
            HoldClose(CloseAfterFrames);
        }

        void HoldClose(int frames) => _closeUntil = Mathf.Max(_closeUntil, Time.frameCount + frames);

        void BeginSlow()
        {
            _slowPending = false;
            _slowLeft = SlowFrames;
            Time.timeScale = SlowScale;
            Time.fixedDeltaTime = _baseFixedDelta * SlowScale; // smooth physics, not a third of the steps
            _log.Add("slowmo_start");
        }

        void EndSlow()
        {
            if (_slowLeft <= 0) return;
            _slowLeft = 0;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDelta;
            _slowReadyAt = Time.time + SlowCooldown;
            if (_log != null) _log.Add("slowmo_end");
        }

        void LateUpdate()
        {
            if (_slowPending && _car.IsAirborne && _car.AirTimeLeft <= SlowFrames * VideoStep * SlowScale * 0.5f) BeginSlow();
            if (_slowLeft == 1) EndSlow(); else if (_slowLeft > 1) _slowLeft--;
            if (_options.camera == ShortsCamera.Gameplay) return;

            Choose();
            float size = _zoom == Zoom.Overview ? _overview : _zoom == Zoom.Close ? _normal * CloseScale : _normal;
            _size = Mathf.SmoothDamp(_size, size, ref _sizeVelocity, ZoomSmooth, Mathf.Infinity, VideoStep);
            _lead = Mathf.SmoothDamp(_lead, _zoom == Zoom.Overview ? OverviewLead : NormalLead, ref _leadVelocity, ZoomSmooth, Mathf.Infinity, VideoStep);

            _heading = Mathf.SmoothDampAngle(_heading, Angle(Travel()), ref _headingVelocity, HeadingSmooth, Mathf.Infinity, Time.deltaTime);
            float rad = _heading * Mathf.Deg2Rad;
            Vector2 forward = new(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 p = (Vector2)_car.transform.position + forward * (_lead * _size);
            if (_shakeLeft > 0)
            {
                float fade = _shakeLeft-- / (float)ShakeFrames, t = Time.frameCount * 0.45f;
                p += new Vector2(Mathf.PerlinNoise(t, 3.7f) - 0.5f, Mathf.PerlinNoise(9.1f, t) - 0.5f) * (2f * ShakeSize * fade * _size / _normal);
            }
            transform.SetPositionAndRotation(new Vector3(p.x, p.y, transform.position.z), Quaternion.Euler(0f, 0f, _heading - 90f));
            _camera.orthographicSize = _size;
        }

        // Where the car is going: along its velocity; slow, thrown about or carried, along the road.
        Vector2 Travel()
        {
            Vector2 velocity = _car.Body.linearVelocity, road = _sensor.RoadDirection;
            return velocity.sqrMagnitude > 25f && !_car.IsCarried && Vector2.Dot(velocity, road) > 0f ? velocity : road;
        }

        void Choose()
        {
            bool fork = Fork(); // always: it notes where the fork is
            if (Jumping() | NearMissComing()) HoldClose(CloseGapFrames); // no quick out-and-in between two reasons
            var want = fork ? Zoom.Overview : Time.frameCount < _closeUntil ? Zoom.Close : Zoom.Normal;
            if (want == _zoom) return;
            _zoom = want;
            _log.Add("zoom", ("level", want.ToString().ToLowerInvariant()));
        }

        bool Fork()
        {
            var track = _sensor.Track;
            var splits = track.Layout.splits;
            if (splits == null) return false;
            var path = track.Path;
            float d = _sensor.Current.Distance;
            if (_fork >= 0)
            {
                float into = path.DeltaDistance(splits[_fork].from, d);
                if (into < ForkHold || _car.IsAirborne) return true;
                _fork = -1;
            }
            float speed = Mathf.Max(8f, _car.ForwardSpeed);
            for (int i = 0; i < splits.Count; i++)
            {
                float to = path.DeltaDistance(d, splits[i].from);
                if (to < 0f || to / speed > ForkWarning) continue;
                _fork = i;
                _overview = OverviewSize(splits[i], d);
                return true;
            }
            return false;
        }

        // Big enough to hold the road from the car to the end of the fork, the car low in the picture.
        float OverviewSize(TrackLayout.Split split, float d)
        {
            var path = _sensor.Track.Path;
            float length = path.DeltaDistance(d, split.to);
            Vector2 start = path.PointAt(d), along = (path.PointAt(split.to) - start).normalized;
            float wide = 0f;
            for (float x = 0f; x <= length; x += 4f)
            {
                float at = d + x;
                float side = Mathf.Abs(Vector2.Dot(path.PointAt(at) - start, new Vector2(-along.y, along.x)));
                wide = Mathf.Max(wide, side + _sensor.Track.OffRoadAt(at) + 2f);
            }
            float high = length / (1f + OverviewLead);
            float size = Mathf.Max(high, wide * ShortsOptions.Height / ShortsOptions.Width);
            return Mathf.Clamp(size, _normal * OverviewMinScale, _normal * OverviewMaxScale);
        }

        bool Jumping()
        {
            if (_car.IsAirborne && !_car.IsCarried) return true;
            var track = _sensor.Track;
            float d = _sensor.Current.Distance, reach = Mathf.Max(8f, _car.ForwardSpeed) * JumpWarning;
            foreach (var jump in track.Jumps)
            {
                float to = track.Path.DeltaDistance(d, jump.RampStart);
                if (to >= 0f && to <= reach) return true;
            }
            return false;
        }

        // Rivals, small solid things and holes the car is about to pass very close to, from where everything is heading.
        bool NearMissComing()
        {
            if (_car.IsAirborne || _race.RaceTime < NearGrace) return false;
            Vector2 at = _car.Body.position, velocity = _car.Body.linearVelocity;
            _found.Clear();
            Physics2D.OverlapCircle(at, NearLook, ContactFilter2D.noFilter, _found);
            bool near = false;
            foreach (var other in _found)
            {
                if (other.attachedRigidbody == _car.Body || other.isTrigger) continue;
                var size = other.bounds.size;
                bool car = other.GetComponentInParent<RaceProgress>() != null;
                if (!car && Mathf.Max(size.x, size.y) > NearMaxSize) continue;
                Vector2 moving = other.attachedRigidbody != null ? other.attachedRigidbody.linearVelocity : Vector2.zero;
                near |= Passes((Vector2)other.bounds.center - at, moving - velocity, Mathf.Max(size.x, size.y) * 0.5f);
            }
            var holes = _sensor.Track.Layout.holes;
            var path = _sensor.Track.Path;
            if (holes != null)
                foreach (var hole in holes)
                    near |= Passes(path.PointAt(hole.distance) + path.NormalAt(hole.distance) * hole.lateral - at, -velocity, hole.radius);
            return near;
        }

        bool Passes(Vector2 offset, Vector2 relative, float radius)
        {
            float speed = relative.magnitude;
            if (speed < NearMinSpeed) return false;
            float t = -Vector2.Dot(offset, relative) / (speed * speed);
            if (t <= 0f || t > NearWarning || (offset + relative * t).magnitude - radius - 1f > NearGap) return false;
            HoldClose(Mathf.CeilToInt((t + 0.4f) / VideoStep));
            return true;
        }

        static float Angle(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
}
#endif
