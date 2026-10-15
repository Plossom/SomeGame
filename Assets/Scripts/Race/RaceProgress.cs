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
        SomeGame.Car.CarMovement _car;
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
            _tracking && !IsFinished && !(_car != null && _car.IsAirborne) && // mid-jump: the landing credits what was flown over
            Circuit.Path.DeltaDistance(Circuit.CheckpointDistance(NextCheckpoint), _sensor.Current.Distance) > missedCheckpointMargin;

        /// <summary>True while the car has been driving against the track direction for a moment.</summary>
        public bool IsWrongWay => _wrongWayTimer >= wrongWayDelay;

        SomeGame.Track.Track Circuit => _sensor.Track;
        int CheckpointCount => Circuit.CheckpointCount;
        public int NextCheckpoint => CheckpointsPassed % CheckpointCount;
        /// <summary>Laps (or sections) done. A section line counts like the start/finish line.</summary>
        public int LapsCompleted => CheckpointsPassed <= 0 ? 0 : (CheckpointsPassed - 1) / Circuit.CheckpointsPerLap;
        public int CurrentLap => Mathf.Clamp(LapsCompleted + 1, 1, TotalLaps);

        /// <summary>Monotonic race progress used for ranking (checkpoints plus fraction to the next one).</summary>
        public float Progress
        {
            get
            {
                var path = Circuit.Path;
                float from = Circuit.CheckpointDistance(NextCheckpoint - 1);
                float span = path.Length / CheckpointCount;
                // Signed: put back behind the last checkpoint (after a splash) counts as behind it, not as
                // almost at the next one, so the ranking stays right.
                float fraction = path.DeltaDistance(from, _sensor.Current.Distance) / span;
                return CheckpointsPassed + Mathf.Clamp(fraction, -0.999f, 0.999f);
            }
        }

        void Awake()
        {
            _sensor = GetComponent<TrackSensor>();
            _body = GetComponent<Rigidbody2D>();
            _car = GetComponent<SomeGame.Car.CarMovement>();
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

        /// <summary>
        /// Online: a car driven on another phone. Its checkpoints and finish come from that phone
        /// (<see cref="ApplyRemote"/>); only the fraction to the next checkpoint is measured here.
        /// </summary>
        public bool Remote { get; set; }

        /// <summary>Online: the other phone's count of checkpoints, and its finish (seconds after GO).</summary>
        public void ApplyRemote(int checkpoints, bool finished, float finishSeconds, float startTime)
        {
            if (!_tracking || IsFinished) return;
            while (CheckpointsPassed < checkpoints)
            {
                bool crossedLine = NextCheckpoint % Circuit.CheckpointsPerLap == 0;
                CheckpointsPassed++;
                if (crossedLine && CheckpointsPassed > 1) _lapTimes.Add(0f);
            }
            if (!finished) return;
            IsFinished = true;
            FinishTime = startTime + finishSeconds;
            Finished?.Invoke(this);
        }

        void FixedUpdate()
        {
            if (Remote) return;
            if (!_tracking || IsFinished)
            {
                _wrongWayTimer = 0f;
                return;
            }
            var path = Circuit.Path;
            float distance = _sensor.Current.Distance;
            UpdateWrongWay(_sensor.RoadDirection, Time.fixedDeltaTime); // the branch's direction on a branch

            float moved = path.DeltaDistance(_lastDistance, distance);

            // A jump means the car was re-located (cut across grass); it does not pass checkpoints.
            if (!_sensor.Jumped)
            {
                float toCheckpoint = path.DeltaDistance(_lastDistance, Circuit.CheckpointDistance(NextCheckpoint));
                // Riding a banked wall or taking another way round counts as passing between the posts.
                bool betweenPosts = Mathf.Abs(_sensor.Current.Lateral) <= Circuit.OffRoadAt(distance) + gateMargin || _sensor.OnBank || _sensor.Branch >= 0;
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
            bool crossedLine = NextCheckpoint % Circuit.CheckpointsPerLap == 0; // the start line, or a section line
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
