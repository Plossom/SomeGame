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
        [Header("Corner cutting (only when DetectCuts is on, i.e. the player)")]
        [Tooltip("A trip onto the grass is a cut when it gains this much more track distance than the car " +
                 "actually drove (units). Running wide never gains distance, so it is never penalised.")]
        [SerializeField, Min(0f)] float cutTolerance = 2.5f;
        [Tooltip("Fixed seconds added for each cut.")]
        [SerializeField, Min(0f)] float cutPenaltySeconds = 1f;
        [Tooltip("Plus the distance gained by the cut converted to time at this speed (units/s). Kept well " +
                 "below the average race speed, so a cut always costs more than it saves.")]
        [SerializeField, Min(1f)] float cutPenaltySpeed = 10f;
        [Tooltip("A checkpoint only counts when passed between its posts: within this distance of the " +
                 "centre line beyond the kerb (units).")]
        [SerializeField, Min(0f)] float gateMargin = 0.5f;
        [Tooltip("How far past the next checkpoint (units) the car must be before Missed Checkpoint shows.")]
        [SerializeField, Min(0f)] float missedCheckpointMargin = 6f;

        TrackSensor _sensor;
        Rigidbody2D _body;
        float _wrongWayTimer;
        bool _onGrass;
        float _grassProgress, _grassTravelled;
        bool _lapHadCut;
        readonly List<bool> _lapClean = new();
        float _lastDistance;
        float _lapStartTime;
        bool _tracking;
        readonly List<float> _lapTimes = new();

        public event Action<RaceProgress, float> LapCompleted;
        public event Action<RaceProgress> Finished;
        /// <summary>A corner cut was penalised: (car, penalty seconds).</summary>
        public event Action<RaceProgress, float> CornerCut;

        public string DisplayName { get; set; }
        public int TotalLaps { get; private set; } = 3;
        public int CheckpointsPassed { get; private set; }
        public bool IsFinished { get; private set; }
        public float FinishTime { get; private set; }
        public IReadOnlyList<float> LapTimes => _lapTimes;
        /// <summary>Per lap: true if the lap had no corner cut (only clean laps count as best lap).</summary>
        public IReadOnlyList<bool> LapClean => _lapClean;
        /// <summary>When on, corner cuts are detected and penalised. Set for the player.</summary>
        public bool DetectCuts { get; set; }
        /// <summary>Total penalty seconds collected for corner cuts.</summary>
        public float PenaltySeconds { get; private set; }

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
            PenaltySeconds = 0f;
            _onGrass = _lapHadCut = false;
            _lapTimes.Clear();
            _lapClean.Clear();
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
            if (DetectCuts) TrackCornerCut(moved);

            // A jump means the car was re-located (cut across grass); it does not pass checkpoints.
            if (!_sensor.Jumped)
            {
                float toCheckpoint = path.DeltaDistance(_lastDistance, Circuit.CheckpointDistance(NextCheckpoint));
                bool betweenPosts = Mathf.Abs(_sensor.Current.Lateral) <= Circuit.Layout.OffRoadDistance + gateMargin;
                if (moved > 0f && toCheckpoint > 0f && toCheckpoint <= moved && betweenPosts)
                    PassCheckpoint();
            }
            _lastDistance = distance;
        }

        // Each trip onto the grass compares track distance gained with distance actually driven.
        // Cutting the inside of a corner gains more track than it drives; that is a cut.
        void TrackCornerCut(float moved)
        {
            if (_sensor.IsOffRoad)
            {
                if (!_onGrass)
                {
                    _onGrass = true;
                    _grassProgress = _grassTravelled = 0f;
                }
                _grassProgress += moved;
                _grassTravelled += _body.linearVelocity.magnitude * Time.fixedDeltaTime;
                return;
            }

            if (!_onGrass) return;
            _onGrass = false;
            float gained = _grassProgress - _grassTravelled;
            if (gained <= cutTolerance) return;

            float penalty = cutPenaltySeconds + gained / cutPenaltySpeed;
            PenaltySeconds += penalty;
            _lapHadCut = true;
            CornerCut?.Invoke(this, penalty);
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
            _lapClean.Add(!_lapHadCut);
            _lapHadCut = false;
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
