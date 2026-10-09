using System;
using System.Collections;
using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SomeGame.Race
{
    public enum RaceState { Countdown, Racing, Finished }

    /// <summary>
    /// Runs one race: puts the cars on the grid, counts down 3-2-1-GO, times laps, ranks the cars
    /// by track progress and ends the race when the player finishes.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        [SerializeField] SomeGame.Track.Track track;
        [SerializeField] CarMovement player;
        [Tooltip("Rival cars; they start ahead of the player on the grid.")]
        [SerializeField] List<CarMovement> rivals = new();
        [SerializeField] FloatingJoystick joystick;
        [SerializeField, Min(1)] int laps = 3;
        [SerializeField, Min(0.1f)] float countdownStepSeconds = 1f;

        readonly List<RaceProgress> _cars = new();
        readonly List<RaceProgress> _standings = new();
        readonly List<RaceProgress> _finishOrder = new();
        float _startTime;

        /// <summary>3, 2, 1 during the countdown, then 0 for GO.</summary>
        public event Action<int> CountdownTick;
        public event Action RaceStarted;
        /// <summary>The player completed a lap: (lap time, is a new best lap).</summary>
        public event Action<float, bool> PlayerLapCompleted;
        public event Action PlayerFinished;

        public RaceState State { get; private set; } = RaceState.Countdown;
        public int Laps => laps;
        public RaceProgress Player { get; private set; }
        public IReadOnlyList<RaceProgress> Standings => _standings;
        public string TrackId => track.Layout.name;
        public float? BestLap => BestLapStore.Get(TrackId);

        /// <summary>Seconds since GO; frozen at the player's finish time.</summary>
        public float RaceTime => State switch
        {
            RaceState.Countdown => 0f,
            RaceState.Finished => Player.FinishTime - _startTime,
            _ => Time.time - _startTime,
        };

        /// <summary>Race time plus the player's corner-cut penalties.</summary>
        public float PlayerTotalTime => RaceTime + Player.PenaltySeconds;

        /// <summary>The player's final place, including penalties. Valid once PlayerFinished fired.</summary>
        public int FinalPosition { get; private set; }

        /// <summary>The race from the map, or null when the scene was opened directly.</summary>
        public LevelDefinition Level { get; private set; }

        /// <summary>Stars earned in this race (0 unless won). Valid once PlayerFinished fired.</summary>
        public int StarsEarned { get; private set; }

        /// <summary>1-based position of a car in the current standings.</summary>
        public int PositionOf(RaceProgress car) => _standings.IndexOf(car) + 1;

        void Awake()
        {
            Level = GameSession.CurrentLevel;
            int rivalCount = rivals.Count;
            if (Level != null)
            {
                track.SetLayout(Level.track);
                laps = Level.laps;
                rivalCount = Mathf.Min(Level.rivalCount, rivals.Count);
            }

            // Grid: rivals in front, the player at the back.
            int slot = 0;
            for (int i = 0; i < rivals.Count; i++)
            {
                if (i >= rivalCount)
                {
                    rivals[i].gameObject.SetActive(false);
                    continue;
                }
                if (Level != null) ScaleRival(rivals[i]);
                Register(rivals[i], slot++, $"Rival {i + 1}");
            }
            Player = Register(player, slot, "You");
            Player.DetectCuts = true;
        }

        // Rivals share their CarStats assets, so each gets a scaled runtime copy.
        void ScaleRival(CarMovement rival)
        {
            var stats = Instantiate(rival.Stats);
            stats.topSpeed *= Level.rivalSpeedScale;
            stats.acceleration *= Level.rivalSpeedScale;
            rival.Stats = stats;
            if (rival.TryGetComponent(out AIDriverInput ai)) ai.CornerGrip *= Level.rivalCornerScale;
        }

        RaceProgress Register(CarMovement car, int gridSlot, string displayName)
        {
            var pose = track.GridSlot(gridSlot);
            car.transform.SetPositionAndRotation(pose.position, pose.rotation);
            car.Body.position = pose.position;
            car.Body.rotation = pose.rotation.eulerAngles.z;
            car.ControlsEnabled = false;

            var progress = car.GetComponent<RaceProgress>();
            progress.DisplayName = displayName;
            progress.Finished += OnCarFinished;
            _cars.Add(progress);
            _standings.Add(progress);
            return progress;
        }

        void Start() => StartCoroutine(Countdown());

        IEnumerator Countdown()
        {
            State = RaceState.Countdown;
            for (int n = 3; n > 0; n--)
            {
                CountdownTick?.Invoke(n);
                yield return new WaitForSeconds(countdownStepSeconds);
            }

            _startTime = Time.time;
            foreach (var car in _cars)
            {
                car.Begin(laps, _startTime);
                car.GetComponent<CarMovement>().ControlsEnabled = true;
            }
            Player.LapCompleted += OnPlayerLap;
            State = RaceState.Racing;
            CountdownTick?.Invoke(0);
            RaceStarted?.Invoke();
        }

        void Update()
        {
            if (State != RaceState.Racing) return;
            _standings.Sort(CompareStandings);
        }

        int CompareStandings(RaceProgress a, RaceProgress b)
        {
            int fa = _finishOrder.IndexOf(a), fb = _finishOrder.IndexOf(b);
            if (fa >= 0 || fb >= 0)
                return (fa < 0 ? int.MaxValue : fa).CompareTo(fb < 0 ? int.MaxValue : fb);
            return b.Progress.CompareTo(a.Progress);
        }

        void OnPlayerLap(RaceProgress car, float lapTime)
        {
            // Laps with a corner cut never count as best lap.
            bool clean = car.LapClean.Count > 0 && car.LapClean[car.LapClean.Count - 1];
            bool newBest = clean && BestLapStore.Submit(TrackId, lapTime);
            PlayerLapCompleted?.Invoke(lapTime, newBest);
        }

        void OnCarFinished(RaceProgress car)
        {
            _finishOrder.Add(car);
            _standings.Sort(CompareStandings);
            if (car != Player) return;

            State = RaceState.Finished;
            player.ControlsEnabled = false;
            if (joystick != null) joystick.Interactable = false;
            StartCoroutine(ClassifyPlayer());
        }

        // With penalties the player's real result is finish time + penalty. Rivals finishing within
        // that window still beat the player, so wait it out before showing the result.
        IEnumerator ClassifyPlayer()
        {
            float adjusted = Player.FinishTime + Player.PenaltySeconds;
            while (Time.time < adjusted) yield return null;

            int ahead = 0;
            foreach (var car in _cars)
                if (car != Player && car.IsFinished && car.FinishTime < adjusted) ahead++;
            FinalPosition = ahead + 1;
            bool won = FinalPosition == 1;
            StarsEarned = Level != null
                ? ProgressStore.Record(Level, won, PlayerTotalTime)
                : 0;
            PlayerFinished?.Invoke();
        }

        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
