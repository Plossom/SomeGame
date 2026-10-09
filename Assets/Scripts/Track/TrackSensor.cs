using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Tracks where a car is relative to the circuit each physics step: distance along the lap,
    /// sideways offset and whether it is on grass. Shared by movement, race progress and the AI.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody2D))]
    public class TrackSensor : MonoBehaviour
    {
        [SerializeField] Track track;
        [Tooltip("Centre-line samples searched either side of the last known position.")]
        [SerializeField, Min(2)] int searchWindow = 14;

        Rigidbody2D _body;
        int _hint = -1;

        public Track Track => track;
        public TrackPoint Current { get; private set; }
        public bool IsOffRoad { get; private set; }

        /// <summary>
        /// True for the step in which the car was re-located on a different part of the circuit
        /// (e.g. after cutting across the grass). Progress logic must not count distance over a jump.
        /// </summary>
        public bool Jumped { get; private set; }

        public void SetTrack(Track value)
        {
            track = value;
            _hint = -1;
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (track == null) track = FindAnyObjectByType<Track>();
        }

        void Start() => Sample();

        void FixedUpdate() => Sample();

        /// <summary>Re-samples immediately, e.g. after the car was teleported to the grid.</summary>
        public void Sample()
        {
            if (track == null) return;
            var path = track.Path;
            Vector2 position = _body.position;
            Jumped = false;

            TrackPoint point;
            if (_hint < 0)
            {
                point = path.Project(position);
            }
            else
            {
                point = path.Project(position, _hint, searchWindow);
                // Lost the local match (cut across grass to another section): re-acquire globally.
                if (Mathf.Abs(point.Lateral) > track.Layout.roadWidth)
                {
                    var global = path.Project(position);
                    if (Mathf.Abs(global.Lateral) < Mathf.Abs(point.Lateral) - 0.5f)
                    {
                        point = global;
                        Jumped = true;
                    }
                }
            }

            _hint = point.Segment;
            Current = point;
            IsOffRoad = track.IsOffRoad(point.Lateral);
        }
    }
}
