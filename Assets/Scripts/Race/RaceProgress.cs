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
        TrackSensor _sensor;
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

        void Awake() => _sensor = GetComponent<TrackSensor>();

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
            if (!_tracking || IsFinished) return;
            var path = Circuit.Path;
            float distance = _sensor.Current.Distance;

            // A jump means the car was re-located (cut across grass); it does not pass checkpoints.
            if (!_sensor.Jumped)
            {
                float moved = path.DeltaDistance(_lastDistance, distance);
                float toCheckpoint = path.DeltaDistance(_lastDistance, Circuit.CheckpointDistance(NextCheckpoint));
                if (moved > 0f && toCheckpoint > 0f && toCheckpoint <= moved)
                    PassCheckpoint();
            }
            _lastDistance = distance;
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
