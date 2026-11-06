#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using SomeGame.Car;
using SomeGame.Input;
using SomeGame.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SomeGame.Recording
{
    /// <summary>
    /// Runs a batch in Play mode: for each run it loads the race, hands the player's car to the autopilot, records
    /// from the countdown to just past the finish and writes the event log. Autopilot runs keep no records, so
    /// the saved game stays as it was.
    /// </summary>
    public class ShortsDirector : MonoBehaviour
    {
        const float MaxRaceSeconds = 300f; // an autopilot stuck for good ends the run
        const float AfterFinishSeconds = 2.5f;
        const float EffectEmphasis = 3f, TrailWidth = 2f;
        static readonly Color SparkColor = new(1f, 0.72f, 0.12f);
        static readonly Color TrailColor = new(0.05f, 0.08f, 0.16f, 0.85f);

        /// <summary>Starts the video of this name (false if it would not start). Set by the Editor side, which has the Recorder.</summary>
        public static Func<string, bool> StartVideo;
        public static Action StopVideo;

        public ShortsOptions Options;
        bool _recording;

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            RaceManager.KeepRecords = false;
            var cup = CupList.Instance.cups[Options.cup];
            foreach (int race in Options.races)
                for (int i = 0; i < Options.runs; i++)
                    yield return Run(cup, race);
            Debug.Log("Shorts: batch done");
            EditorApplication.ExitPlaymode();
        }

        void OnDestroy()
        {
            RaceManager.KeepRecords = true;
            if (_recording) StopVideo();
        }

        IEnumerator Run(CupDefinition cup, int raceNumber)
        {
            var level = cup.races[raceNumber - 1];
            var (name, run) = ShortsOptions.NextRun(cup, raceNumber);
            int seed = Options.randomLines ? UnityEngine.Random.Range(1, 1000000) : 3;
            yield return new WaitUntil(() => !GameSession.Loading);

            RaceManager race = null;
            RaceEventLog log = null;
            void Ready(Scene scene, LoadSceneMode mode)
            {
                if (scene.name != GameSession.RaceScene) return;
                SceneManager.sceneLoaded -= Ready;
                race = FindAnyObjectByType<RaceManager>();
                var car = Autopilot(race, seed, Options.drift);
                log = car.gameObject.AddComponent<RaceEventLog>();
                log.Init(race, car, name, level.displayName, raceNumber, run, seed, Options);
                Camera.main.gameObject.AddComponent<RecordingCamera>().Init(race, car, Options, log);
                if (!Options.showHud) foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) canvas.enabled = false;
            }
            SceneManager.sceneLoaded += Ready;
            GameSession.StartRace(level);
            yield return new WaitUntil(() => race != null);

            _recording = StartVideo(name);
            if (!_recording)
            {
                Debug.LogError("Shorts: the recorder did not start for " + name);
                EditorApplication.ExitPlaymode();
                yield break;
            }
            log.StartClock();
            yield return null;
            if (Options.drift) ShowDrift(race.Player.GetComponent<CarMovement>());
            Debug.Log($"Shorts: recording {name} (seed {seed})");

            while (race.State != RaceState.Finished && race.RaceTime < MaxRaceSeconds) yield return null;
            if (race.State == RaceState.Finished) yield return new WaitForSeconds(AfterFinishSeconds);
            StopVideo();
            _recording = false;
            File.WriteAllText(Path.Combine(ShortsOptions.Folder, name + ".json"), log.ToJson());
            Debug.Log($"Shorts: {name} done, {log.Summary}");
            yield return null;
        }

        // The rivals' driver takes the player's car. The player's input goes for good (the track's set pieces then
        // treat the car like an AI's: it needs no swipes or taps).
        static CarMovement Autopilot(RaceManager race, int seed, bool drift)
        {
            var car = race.Player.GetComponent<CarMovement>();
            if (car.TryGetComponent(out PlayerCarInput player)) DestroyImmediate(player);
            var ai = car.gameObject.AddComponent<AIDriverInput>();
            ai.Seed = seed;
            ai.Adventurous = 0.5f;
            ai.LaneOffset = seed == 3 ? 0f : UnityEngine.Random.Range(-1.5f, 1.5f);
            car.UseInput(drift ? new DriftingDriver(ai, car) : ai);
            return car;
        }

        // Sparks and tyre trails that read on a phone screen, on snow too (the winter tracks pale the trails at the start).
        static void ShowDrift(CarMovement car)
        {
            var sparks = car.GetComponent<DriftEffects>();
            sparks.Emphasis = EffectEmphasis;
            sparks.ChargeColor = SparkColor;
            var marks = car.GetComponent<TyreMarks>();
            marks.Width *= TrailWidth;
            marks.Tint = TrailColor;
        }
    }

    /// <summary>The AI, plus the drift button: held through sharp corners at speed, away from the jumps.</summary>
    public class DriftingDriver : ICarInput
    {
        const float Look = 14f, StartTurn = 28f, StopTurn = 12f, MinSpeed = 11f;

        readonly AIDriverInput _ai;
        readonly CarMovement _car;
        readonly SomeGame.Track.TrackSensor _sensor;
        bool _held;

        public DriftingDriver(AIDriverInput ai, CarMovement car)
        {
            _ai = ai;
            _car = car;
            _sensor = car.GetComponent<SomeGame.Track.TrackSensor>();
        }

        public Vector2 SteerDirection => _ai.SteerDirection;
        public float Throttle => _ai.Throttle;

        public bool DriftHeld
        {
            get
            {
                var track = _sensor.Track;
                if (track == null) return false;
                var path = track.Path;
                float d = _sensor.Current.Distance;
                float turn = Vector2.Angle(path.TangentAt(d), path.TangentAt(d + Look));
                bool safe = _sensor.Branch < 0 && !_car.IsAirborne && _car.ForwardSpeed > MinSpeed && !track.InGap(d + Look);
                foreach (var jump in track.Jumps)
                {
                    float to = path.DeltaDistance(d, jump.RampStart);
                    if (to > -2f && to < Look * 2f) safe = false;
                }
                _held = safe && turn > (_held ? StopTurn : StartTurn);
                return _held;
            }
        }
    }
}
#endif
