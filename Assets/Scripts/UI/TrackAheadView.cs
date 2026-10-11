using SomeGame.Race;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// A small, minimal view of the road around the car in the race HUD: the centre line as a soft
    /// ribbon, bright ahead and fading out behind and in the distance, with the car as a slim arrow in the
    /// middle. It is fixed the same way up as the screen (like the race camera), so the next bend points
    /// exactly where you drag your thumb to steer (the car drives where you point). The arrow turns with
    /// the direction the car is moving, smoothed so it stays calm while drifting.
    /// </summary>
    public class TrackAheadView : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] TrackLine road;
        [SerializeField] RectTransform arrow;
        [Tooltip("Track units drawn ahead of the car and behind it.")]
        [SerializeField] float ahead = 90f, behind = 25f;
        [Tooltip("Track units from the car to the panel's edge (half the panel's shorter side).")]
        [SerializeField, Min(5f)] float viewRadius = 48f;
        [Tooltip("How quickly the arrow turns after the car (higher = snappier).")]
        [SerializeField, Min(0.1f)] float turnRate = 8f;
        [SerializeField, Min(0.2f)] float step = 1.5f;

        readonly System.Collections.Generic.List<Vector2> _points = new();
        readonly System.Collections.Generic.List<float> _alphas = new();
        TrackSensor _sensor;
        Rigidbody2D _body;
        float _heading;
        bool _hasHeading;

        void LateUpdate()
        {
            if (!Bind()) return;
            var track = _sensor.Track;
            var path = track.Path;
            Vector2 car = _body.position;

            // The arrow: where the car is going (not where its nose points mid-drift), smoothed.
            Vector2 velocity = _body.linearVelocity;
            Vector2 dir = velocity.sqrMagnitude > 9f ? velocity : (Vector2)_body.transform.up;
            float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _heading = _hasHeading ? Mathf.LerpAngle(_heading, target, 1f - Mathf.Exp(-turnRate * Time.deltaTime)) : target;
            _hasHeading = true;
            arrow.anchoredPosition = Vector2.zero;
            arrow.localRotation = Quaternion.Euler(0f, 0f, _heading - 90f); // the arrow sprite points up

            var rect = ((RectTransform)transform).rect;
            float scale = Mathf.Min(rect.width, rect.height) * 0.5f / viewRadius; // panel units per track unit

            float from = _sensor.Current.Distance;
            _points.Clear();
            _alphas.Clear();
            for (float d = -behind; d <= ahead; d += step)
            {
                _points.Add((path.PointAt(path.WrapDistance(from + d)) - car) * scale);
                // Behind the car: dim and fading out. Ahead: bright, fading only in the distance.
                float a = d < 0f ? 0.35f * Mathf.InverseLerp(-behind, 0f, d) : 1f - Mathf.InverseLerp(ahead * 0.6f, ahead, d);
                _alphas.Add(a);
            }
            road.Width = Mathf.Clamp(track.Layout.roadWidth * scale * 1.2f, 22f, 40f);
            road.SetPoints(_points, _alphas);
        }

        bool Bind()
        {
            if (_sensor != null && _body != null) return true;
            if (race == null || race.Player == null) return false;
            _sensor = race.Player.GetComponent<TrackSensor>();
            _body = race.Player.GetComponent<Rigidbody2D>();
            return _sensor != null && _body != null && _sensor.Track != null;
        }
    }
}
