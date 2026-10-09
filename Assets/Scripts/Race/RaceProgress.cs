using System;
using System.Collections.Generic;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// Per-car checkpoint and lap tracking, derived from the track data via the car's
    /// <see cref="TrackSensor"/>. Checkpoints count only in order; checkpoint 0 is the start line.
    /// Cars start just behind the line, so the first crossing begins lap 1 and is not a lap.
    /// </summary>
    [RequireComponent(typeof(TrackSensor))]
    public class RaceProgress : MonoBehaviour
    {
        [Tooltip("Seconds of driving against the track direction before Wrong Way is shown.")]
        [SerializeField, Min(0f)] float wrongWayDelay = 0.8f;
        [Tooltip("A checkpoint only counts when passed between its posts: within this distance of the " +
                 "centre line beyond the kerb (units).")]
        [SerializeField, Min(0f)] float gateMargin = 0.5f;
        [Tooltip("How far past the next checkpoint (units) the car must be before Missed Checkpoint shows.")]
        [SerializeField, Min(0f)] float missedCheckpointMargin = 6f;

        TrackSensor _sensor;
        Rigidbody2D _body;
        float _wrongWayTimer;
        float _lastDistance;
        float _lapStartTime;
        bool _tracking;
        readonly List<float> _lapTimes = new();

        public event Action<RaceProgress, float> LapCompleted;
        public event Action<RaceProgress> Finished;

        public string DisplayName { get; set; }
        public int TotalLaps { get; private set; } = 3;
        public int CheckpointsPassed { get; private set; }
        public bool IsFinished { get; private set; }
        public float FinishTime { get; private set; }
        public IReadOnlyList<float> LapTimes => _lapTimes;
        /// <summary>True when the car is clearly beyond the checkpoint it still has to pass (took a shortcut).</summary>
        public bool MissedCheckpoint =>
            _tracking && !IsFinished &&
            Circuit.Path.DeltaDistance(Circuit.CheckpointDistance(NextCheckpoint), _sensor.Current.Distance) > missedCheckpointMargin;

        /// <summary>True while the car has been driving against the track direction for a moment.</summary>
        public bool IsWrongWay => _wrongWayTimer >= wrongWayDelay;

        SomeGame.Track.Track Circuit => _sensor.Track;
        int CheckpointCount => Circuit.CheckpointCount;
        public int NextCheckpoint => CheckpointsPassed % CheckpointCount;
        public int LapsCompleted => CheckpointsPassed <= 0 ? 0 : (CheckpointsPassed - 1) / CheckpointCount;
        public int CurrentLap => Mathf.Clamp(LapsCompleted + 1, 1, TotalLaps);

        /// <summary>Monotonic race progress used for ranking (checkpoints plus fraction to the next one).</summary>
        public float Progress
        {
            get
            {
                var path = Circuit.Path;
                float from = Circuit.CheckpointDistance(NextCheckpoint - 1);
                float span = path.Length / CheckpointCount;
                float fraction = Mathf.Repeat(_sensor.Current.Distance - from, path.Length) / span;
                return CheckpointsPassed + Mathf.Clamp(fraction, 0f, 0.999f);
            }
        }

        void Awake()
        {
            _sensor = GetComponent<TrackSensor>();
            _body = GetComponent<Rigidbody2D>();
        }

        /// <summary>Starts counting from the current position. Call at GO.</summary>
        public void Begin(int totalLaps, float startTime)
        {
            TotalLaps = totalLaps;
            CheckpointsPassed = 0;
            IsFinished = false;
            _lapTimes.Clear();
            _lapStartTime = startTime;
            _sensor.Sample();
            _lastDistance = _sensor.Current.Distance;
            _tracking = true;
        }

        void FixedUpdate()
        {
            if (!_tracking || IsFinished)
            {
                _wrongWayTimer = 0f;
                return;
            }
            var path = Circuit.Path;
            float distance = _sensor.Current.Distance;
            UpdateWrongWay(path.TangentAt(distance), Time.fixedDeltaTime);

            float moved = path.DeltaDistance(_lastDistance, distance);

            // A jump means the car was re-located (cut across grass); it does not pass checkpoints.
            if (!_sensor.Jumped)
            {
                float toCheckpoint = path.DeltaDistance(_lastDistance, Circuit.CheckpointDistance(NextCheckpoint));
                bool betweenPosts = Mathf.Abs(_sensor.Current.Lateral) <= Circuit.OffRoadAt(distance) + gateMargin;
                if (moved > 0f && toCheckpoint > 0f && toCheckpoint <= moved && betweenPosts)
                    PassCheckpoint();
            }
            _lastDistance = distance;
        }

        /// <summary>
        /// After a shortcut jump: counts the checkpoints between the take-off and the landing spot (the
        /// car flew over them), so cutting the corner this way is allowed.
        /// </summary>
        public void CreditFlight(float from, float to)
        {
            if (!_tracking || IsFinished) return;
            var path = Circuit.Path;
            float span = path.DeltaDistance(from, to);
            for (int guard = 0; guard < CheckpointCount && !IsFinished; guard++)
            {
                float toCheckpoint = path.DeltaDistance(from, Circuit.CheckpointDistance(NextCheckpoint));
                if (toCheckpoint <= 0f || toCheckpoint > span) break;
                PassCheckpoint();
            }
            _sensor.Sample();
            _lastDistance = _sensor.Current.Distance;
        }

        void UpdateWrongWay(Vector2 trackDirection, float dt)
        {
            Vector2 velocity = _body.linearVelocity;
            float speed = velocity.magnitude;
            bool backwards = speed > 2f && Vector2.Dot(velocity, trackDirection) < -0.5f * speed;
            _wrongWayTimer = backwards
                ? _wrongWayTimer + dt
                : Mathf.Max(0f, Mathf.Min(_wrongWayTimer, wrongWayDelay) - dt * 2f);
        }

        void PassCheckpoint()
        {
            bool crossedLine = NextCheckpoint == 0;
            CheckpointsPassed++;
            if (!crossedLine || CheckpointsPassed == 1) return;

            float now = Time.time;
            float lapTime = now - _lapStartTime;
            _lapStartTime = now;
            _lapTimes.Add(lapTime);
            LapCompleted?.Invoke(this, lapTime);

            if (LapsCompleted >= TotalLaps)
            {
                IsFinished = true;
                FinishTime = now;
                Finished?.Invoke(this);
            }
        }
    }
}
