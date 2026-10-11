using SomeGame.Race;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// A small, minimal view of the road just ahead in the race HUD: the centre line as a soft ribbon
    /// that fades out behind and far ahead, and an arrow for the car. It turns with the car (the arrow
    /// always points up), so the next bend shows the way you will have to steer. The turning follows
    /// the direction the car is moving and is smoothed, so it stays calm while drifting.
    /// </summary>
    public class TrackAheadView : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] TrackLine road;
        [SerializeField] RectTransform arrow;
        [Tooltip("Track units shown ahead of the car and behind it.")]
        [SerializeField] float ahead = 85f, behind = 12f;
        [Tooltip("Where the car sits, as a fraction of the panel height from the bottom.")]
        [SerializeField, Range(0f, 1f)] float carHeight = 0.24f;
        [Tooltip("How quickly the view turns after the car (higher = snappier).")]
        [SerializeField, Min(0.1f)] float turnRate = 6f;
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

            // Heading: where the car is going (not where its nose points mid-drift), smoothed.
            Vector2 velocity = _body.linearVelocity;
            Vector2 dir = velocity.sqrMagnitude > 9f ? velocity : (Vector2)_body.transform.up;
            float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _heading = _hasHeading ? Mathf.LerpAngle(_heading, target, 1f - Mathf.Exp(-turnRate * Time.deltaTime)) : target;
            _hasHeading = true;

            var rect = ((RectTransform)transform).rect;
            float scale = rect.height * (1f - carHeight) / ahead; // panel units per track unit
            // The road and arrow sit centred in the panel (their anchors), whatever the panel's pivot.
            Vector2 origin = new(0f, rect.height * (carHeight - 0.5f));
            arrow.anchoredPosition = origin;
            Quaternion toView = Quaternion.Euler(0f, 0f, 90f - _heading); // heading -> straight up

            float from = _sensor.Current.Distance;
            _points.Clear();
            _alphas.Clear();
            for (float d = -behind; d <= ahead; d += step)
            {
                Vector2 local = toView * (path.PointAt(path.WrapDistance(from + d)) - car);
                _points.Add(origin + local * scale);
                float fadeIn = Mathf.InverseLerp(-behind, 0f, d), fadeOut = 1f - Mathf.InverseLerp(ahead * 0.55f, ahead, d);
                _alphas.Add(Mathf.Min(fadeIn, fadeOut));
            }
            road.Width = Mathf.Clamp(track.Layout.roadWidth * scale * 1.5f, 26f, 40f);
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
