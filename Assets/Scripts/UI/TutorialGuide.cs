using System;
using System.Collections;
using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The practice drive: shows one hint at a time on a card at the top and moves on once the player has
    /// done it (steer, hop, drift, charge, boost, jump). At the end a "You're ready" card sends the player
    /// to the map. Only active when the race is the tutorial level; then the normal race HUD is hidden.
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] GameObject card;
        [SerializeField] TMP_Text stepLabel;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] UnityEngine.UI.Button skipButton;
        [SerializeField] GameObject doneCard;
        [SerializeField] UnityEngine.UI.Button doneButton;
        [Tooltip("Race HUD objects hidden in the practice drive.")]
        [SerializeField] GameObject[] hideInTutorial;
        [Tooltip("Seconds a finished step stays ticked before the next one shows.")]
        [SerializeField, Min(0f)] float stepPause = 0.9f;
        [Tooltip("Full-screen cover faded in while the car is moved to the spot for the next step.")]
        [SerializeField] CanvasGroup fade;
        [Tooltip("White fingers acting out the current step under the hint card.")]
        [SerializeField] TutorialDemo demo;
        [Tooltip("Road before a curve (drift step) and before the ramp (jump step) the car is put at.")]
        [SerializeField, Min(5f)] float curveLead = 34f, rampLead = 50f;

        // Where a step is best practised: the car is moved there when the step starts.
        enum Spot { Anywhere, BeforeCurve, BeforeRamp }

        struct Step
        {
            public string title, body;
            public Func<bool> done;
            public Spot spot;
            public int demo;
        }

        Step[] _steps;
        int _index = -1;
        float _steerTime, _nextAt = -1f;
        bool _hopped, _drifted, _boosted, _jumped;
        CarMovement _car;
        SomeGame.Input.PlayerCarInput _input;
        SomeGame.Track.TrackSensor _sensor;
        RaceProgress _progress;
        List<float> _curves;

        void Start()
        {
            bool tutorial = race.Level != null && race.Level.isTutorial;
            card.SetActive(false);
            doneCard.SetActive(false);
            if (!tutorial)
            {
                enabled = false;
                return;
            }
            // Hide (but keep running) the race HUD: its warnings and lap flash still work.
            foreach (var go in hideInTutorial)
            {
                if (go == null) continue;
                var group = go.GetComponent<CanvasGroup>();
                if (group == null) group = go.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
            _car = race.Player.GetComponent<CarMovement>();
            _input = race.Player.GetComponent<SomeGame.Input.PlayerCarInput>();
            _sensor = race.Player.GetComponent<SomeGame.Track.TrackSensor>();
            _progress = race.Player;
            if (fade != null) { fade.alpha = 0f; fade.blocksRaycasts = false; }
            _car.Hopped += () => _hopped = true;
            _car.DriftStarted += () => _drifted = true;
            _car.BoostStarted += _ => _boosted = true;
            _car.Launched += _ => _jumped = true;
            skipButton.onClick.AddListener(Finish);
            doneButton.onClick.AddListener(GameSession.ReturnToMap);

            const string second = "Tap a <b>second finger</b> anywhere";
            bool drifting = SomeGame.Input.ControlSettings.Drifting;
            var steps = new System.Collections.Generic.List<Step>
            {
                new Step { title = "STEER", body = "Touch the screen and drag: the car drives where you point. Lift your thumb to roll.",
                    done = () => _steerTime > 2f, demo = 0 },
            };
            // Drifting can be switched off in the menu: then the practice skips it.
            if (drifting)
            {
                steps.Add(new Step { title = "HOP", body = second + " while you steer: the car hops.", done = () => _hopped, demo = 1 });
                steps.Add(new Step { title = "DRIFT", body = "Steer a little into the curve, then hop again and <b>keep holding</b>: you drift round the corner.",
                    done = () => _drifted, spot = Spot.BeforeCurve, demo = 2 });
                steps.Add(new Step { title = "CHARGE", body = "Keep drifting: the sparks turn <color=#4FA3FF>blue</color>, <color=#FF9A3D>orange</color>, then <color=#B66BFF>purple</color>.",
                    done = () => _car.IsDrifting && _car.DriftTier >= 1, demo = 3 });
                steps.Add(new Step { title = "BOOST!", body = "Now let go: the longer the drift, the bigger the boost.", done = () => _boosted, demo = 4 });
            }
            steps.Add(new Step { title = "JUMP", body = "Drive straight over the <b>ramp with the white arrows</b> at full speed to fly across the stream.",
                done = () => _jumped, spot = Spot.BeforeRamp, demo = 5 });
            _steps = steps.ToArray();
        }

        void Update()
        {
            if (_steps == null) return;
            if (race.State != RaceState.Racing) return;
            if (_index < 0) Show(0);
            if (_index >= _steps.Length) return;

            if (_input != null && _input.SteerDirection.sqrMagnitude > 0.01f && _car.ForwardSpeed > 4f) _steerTime += Time.deltaTime;

            if (_nextAt > 0f)
            {
                if (Time.time >= _nextAt) { _nextAt = -1f; Show(_index + 1); }
                return;
            }
            if (_steps[_index].done())
            {
                titleLabel.text = _steps[_index].title + "  <color=#FF7A1A>DONE!</color>";
                _nextAt = Time.time + stepPause;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.6f);
            }
        }

        void Show(int index)
        {
            _index = index;
            if (index >= _steps.Length)
            {
                Finish();
                return;
            }
            // Only events that happen while a step is shown count for it.
            _hopped = _drifted = _boosted = _jumped = false;
            card.SetActive(true);
            stepLabel.text = $"PRACTICE  ·  {index + 1}/{_steps.Length}";
            titleLabel.text = _steps[index].title;
            bodyLabel.text = _steps[index].body;
            if (demo != null) demo.Play(_steps[index].demo);
            if (_steps[index].spot != Spot.Anywhere) StartCoroutine(MoveTo(_steps[index].spot));
        }

        // Puts the car on the road just before the next curve or ramp (behind a quick fade), unless it is
        // already heading into one, so the step can be tried straight away.
        IEnumerator MoveTo(Spot spot)
        {
            if (_sensor == null || _sensor.Track == null) yield break;
            var track = _sensor.Track;
            var path = track.Path;
            float lead = spot == Spot.BeforeRamp ? rampLead : curveLead;
            var targets = new List<float>();
            if (spot == Spot.BeforeRamp) foreach (var jump in track.Jumps) targets.Add(jump.RampStart);
            else targets.AddRange(Curves(path));
            if (targets.Count == 0) yield break;

            float current = _sensor.Current.Distance;
            foreach (float t in targets)
            {
                float ahead = path.WrapDistance(t - current);
                if (ahead >= lead * 0.6f && ahead <= lead * 1.8f) yield break; // already on the way in
            }
            // The next spot ahead whose run-up is clear (not blocked by water or a ramp in between).
            Pose? best = null;
            float bestAhead = float.MaxValue;
            foreach (float t in targets)
            {
                var pose = track.RespawnBefore(t, lead, 30f);
                float at = path.Project(pose.position).Distance;
                if (path.WrapDistance(t - at) > lead + 8f) continue;
                float ahead = path.WrapDistance(at - current);
                if (ahead < bestAhead) { bestAhead = ahead; best = pose; }
            }
            if (best == null) yield break;

            yield return Fade(1f, 0.15f);
            float from = _sensor.Current.Distance;
            _car.Respawn(best.Value);
            _sensor.Sample();
            _progress.CreditFlight(from, _sensor.Current.Distance); // moved ahead: counts the checkpoints skipped
            var follow = Camera.main != null ? Camera.main.GetComponent<SomeGame.CameraRig.FollowCamera>() : null;
            if (follow != null) follow.Snap();
            yield return Fade(0f, 0.3f);
        }

        // Where the curves begin: the road starts to turn after running straight.
        List<float> Curves(SomeGame.Track.TrackPath path)
        {
            if (_curves != null) return _curves;
            _curves = new List<float>();
            bool wasTurning = true;
            for (float d = 0f; d < path.Length; d += 1f)
            {
                bool turning = Mathf.Abs(path.TurnAtDistance(d, 4)) > 10f;
                if (turning && !wasTurning) _curves.Add(d);
                wasTurning = turning;
            }
            return _curves;
        }

        IEnumerator Fade(float to, float duration)
        {
            if (fade == null) yield break;
            float start = fade.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(start, to, t / duration);
                yield return null;
            }
            fade.alpha = to;
        }

        void Finish()
        {
            if (demo != null) demo.Hide();
            _index = _steps.Length;
            card.SetActive(false);
            doneCard.SetActive(true);
        }
    }
}
