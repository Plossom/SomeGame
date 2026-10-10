using System;
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

        struct Step
        {
            public string title, body;
            public Func<bool> done;
        }

        Step[] _steps;
        int _index = -1;
        float _steerTime, _nextAt = -1f;
        bool _hopped, _drifted, _boosted, _jumped;
        CarMovement _car;
        SomeGame.Input.PlayerCarInput _input;

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
            _car.Hopped += () => _hopped = true;
            _car.DriftStarted += () => _drifted = true;
            _car.BoostStarted += _ => _boosted = true;
            _car.Launched += _ => _jumped = true;
            skipButton.onClick.AddListener(Finish);
            doneButton.onClick.AddListener(GameSession.ReturnToMap);

            const string second = "Tap a <b>second finger</b> anywhere";
            _steps = new[]
            {
                new Step { title = "STEER", body = "Touch the screen and drag: the car drives where you point. Lift your thumb to roll.",
                    done = () => _steerTime > 2f },
                new Step { title = "HOP", body = second + " while you steer: the car hops.", done = () => _hopped },
                new Step { title = "DRIFT", body = "Steer a little into the curve, then hop again and <b>keep holding</b>: you drift round the corner.",
                    done = () => _drifted },
                new Step { title = "CHARGE", body = "Keep drifting: the sparks turn <color=#4FA3FF>blue</color>, <color=#FF9A3D>orange</color>, then <color=#B66BFF>purple</color>.",
                    done = () => _car.IsDrifting && _car.DriftTier >= 1 },
                new Step { title = "BOOST!", body = "Now let go: the longer the drift, the bigger the boost.", done = () => _boosted },
                new Step { title = "JUMP", body = "Drive straight over the <b>ramp with the white arrows</b> at full speed to fly across the stream.",
                    done = () => _jumped },
            };
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
        }

        void Finish()
        {
            _index = _steps.Length;
            card.SetActive(false);
            doneCard.SetActive(true);
        }
    }
}
