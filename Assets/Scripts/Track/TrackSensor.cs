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
        /// <summary>The branch (other way round) the car is on, or -1 on the main road.</summary>
        public int Branch { get; private set; } = -1;
        /// <summary>Distance along the branch the car is on.</summary>
        public float BranchAlong { get; private set; }
        /// <summary>Direction of the road under the car (the branch's on a branch).</summary>
        public Vector2 RoadDirection { get; private set; } = Vector2.up;
        /// <summary>Riding on top of a train (set by TrackTrain): counts as road, the ground's rules do not apply.</summary>
        public bool Aboard { get; set; }
        /// <summary>Up on the roofs (set by CarTerrain when the car lands on one): only roofs count as road.</summary>
        public bool Elevated { get; set; }
        /// <summary>On an upper floor of a house (only drawn higher; no rooftop rules).</summary>
        public bool Upstairs { get; private set; }
        /// <summary>On a roof branch (up on the roofs).</summary>
        public bool OnRoof => Branch >= 0 && track.Branches[Branch].Kind == TrackLayout.BranchKind.Roof;
        /// <summary>On a banked wall: how far up it (0 = not on a wall, up to 1 at the top).</summary>
        public float BankHeight { get; private set; }
        public bool OnBank => BankHeight > 0f;
        /// <summary>True on an ice branch (frozen lake), an ice floor or a patch of ice on the road: little grip.</summary>
        public bool OnIce => Branch >= 0 && track.Branches[Branch].Kind == TrackLayout.BranchKind.Ice
            || track != null && (track.OnIceFloor(_body.position) || track.OnIcePatch(_body.position));

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
                if (Mathf.Abs(point.Lateral) > track.WidthAt(point.Distance))
                {
                    var global = path.Project(position);
                    if (Mathf.Abs(global.Lateral) < Mathf.Abs(point.Lateral) - 0.5f)
                    {
                        point = global;
                        Jumped = true;
                    }
                }
            }

            // Off the main road but on a branch: the branch is the road, and progress runs along it.
            int previous = Branch;
            Branch = -1;
            RoadDirection = path.TangentAt(point.Distance);
            if (track.Branches.Count > 0 && (Elevated || Mathf.Abs(point.Lateral) > track.HalfWidthAt(point.Distance)))
            {
                int branch = track.BranchAt(position, out float along, out float lateral, roofs: Elevated, ground: !Elevated, keep: previous);
                if (branch >= 0)
                {
                    var route = track.Branches[branch];
                    Branch = branch;
                    BranchAlong = along;
                    float distance = route.MainDistance(along);
                    point = new TrackPoint { Segment = path.SegmentAt(distance), Distance = distance, Lateral = lateral, Position = route.PointAt(along) };
                    RoadDirection = route.TangentAt(along);
                    Jumped = false;
                }
            }

            _hint = point.Segment;
            Current = point;
            Upstairs = false;
            if (track.Layout.upstairs != null && Branch < 0)
                foreach (var span in track.Layout.upstairs)
                {
                    float into = path.DeltaDistance(span.from, point.Distance);
                    if (into >= 0f && into <= path.DeltaDistance(span.from, span.to)) { Upstairs = true; break; }
                }
            float bankHeight = 0f;
            bool onBank = Branch < 0 && !Elevated && track.BankAt(point.Distance, point.Lateral, out bankHeight) != null;
            BankHeight = onBank ? Mathf.Max(0.01f, bankHeight) : 0f;
            IsOffRoad = Branch < 0 && !onBank && !Aboard && track.IsOffRoad(point.Distance, point.Lateral);
        }
    }
}
