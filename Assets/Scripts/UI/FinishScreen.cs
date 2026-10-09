using System.Text;
using SomeGame.Input;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>Results panel shown when the player finishes, with Restart and the joystick setting.</summary>
    public class FinishScreen : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text details;
        [SerializeField] UnityEngine.UI.Button restartButton;
        [SerializeField] UnityEngine.UI.Button joystickButton;
        [SerializeField] TMP_Text joystickButtonLabel;

        bool _newBest;

        void Awake()
        {
            panel.SetActive(false);
            restartButton.onClick.AddListener(race.Restart);
            joystickButton.onClick.AddListener(() => ControlSettings.InvisibleJoystick = !ControlSettings.InvisibleJoystick);
        }

        void OnEnable()
        {
            race.PlayerFinished += Show;
            race.PlayerLapCompleted += OnLap;
            ControlSettings.Changed += RefreshJoystickLabel;
            RefreshJoystickLabel();
        }

        void OnDisable()
        {
            race.PlayerFinished -= Show;
            race.PlayerLapCompleted -= OnLap;
            ControlSettings.Changed -= RefreshJoystickLabel;
        }

        void OnLap(float lapTime, bool newBest) => _newBest |= newBest;

        void Show()
        {
            int position = race.FinalPosition;
            title.text = $"{TimeFormat.Ordinal(position)} PLACE";

            var text = new StringBuilder();
            float penalty = race.Player.PenaltySeconds;
            text.AppendLine($"Total  {TimeFormat.Race(race.PlayerTotalTime)}");
            if (penalty > 0f)
                text.AppendLine($"<size=75%><color=#FF6A5A>incl. +{penalty:0.0} s corner cuts</color></size>");
            var laps = race.Player.LapTimes;
            var clean = race.Player.LapClean;
            for (int i = 0; i < laps.Count; i++)
            {
                bool cut = i < clean.Count && !clean[i];
                text.AppendLine($"Lap {i + 1}  {TimeFormat.Race(laps[i])}{(cut ? "<size=70%><color=#FF6A5A>  cut</color></size>" : "")}");
            }
            if (race.BestLap.HasValue)
                text.Append($"Best lap  {TimeFormat.Race(race.BestLap.Value)}{(_newBest ? "  NEW!" : "")}");
            details.text = text.ToString();

            panel.SetActive(true);
        }

        void RefreshJoystickLabel() =>
            joystickButtonLabel.text = ControlSettings.InvisibleJoystick ? "Joystick: Invisible" : "Joystick: Visible";
    }
}
