using System.Text;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>Results panel shown when the player finishes: place, stars, times, Continue and Retry.</summary>
    public class FinishScreen : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text details;
        [SerializeField] UnityEngine.UI.Button restartButton;
        [Tooltip("Back to the map. Hidden when the race scene was opened directly.")]
        [SerializeField] UnityEngine.UI.Button continueButton;
        [Tooltip("Three star images, filled left to right.")]
        [SerializeField] UnityEngine.UI.Image[] stars;
        [SerializeField] TMP_Text starInfo;
        [SerializeField] Color starOn = new(1f, 0.82f, 0.2f);
        [SerializeField] Color starOff = new(1f, 1f, 1f, 0.15f);

        bool _newBest;

        void Awake()
        {
            panel.SetActive(false);
            restartButton.onClick.AddListener(race.Restart);
            if (continueButton != null) continueButton.onClick.AddListener(GameSession.ReturnToMap);
        }

        void OnEnable()
        {
            race.PlayerFinished += Show;
            race.PlayerLapCompleted += OnLap;
        }

        void OnDisable()
        {
            race.PlayerFinished -= Show;
            race.PlayerLapCompleted -= OnLap;
        }

        void OnLap(float lapTime, bool newBest) => _newBest |= newBest;

        void Show()
        {
            int position = race.FinalPosition;
            title.text = $"{TimeFormat.Ordinal(position)} PLACE";

            var text = new StringBuilder();
            text.AppendLine($"Total  {TimeFormat.Race(race.RaceTime)}");
            var laps = race.Player.LapTimes;
            for (int i = 0; i < laps.Count; i++)
                text.AppendLine($"Lap {i + 1}  {TimeFormat.Race(laps[i])}");
            if (race.BestLap.HasValue)
                text.Append($"Best lap  {TimeFormat.Race(race.BestLap.Value)}{(_newBest ? "  NEW!" : "")}");
            details.text = text.ToString();

            ShowStars();
            panel.SetActive(true);
        }

        void ShowStars()
        {
            var level = race.Level;
            bool hasLevel = level != null;
            if (continueButton != null) continueButton.gameObject.SetActive(hasLevel);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(hasLevel);
                stars[i].color = i < race.StarsEarned ? starOn : starOff;
            }
            if (starInfo == null) return;
            starInfo.gameObject.SetActive(hasLevel);
            if (!hasLevel) return;
            starInfo.text = race.FinalPosition == 1
                ? $"Star times  {Join(level.starTimes)}"
                : "Win the race to earn stars";
        }

        static string Join(float[] times)
        {
            var parts = new string[times.Length];
            for (int i = 0; i < times.Length; i++) parts[i] = TimeFormat.Race(times[i]);
            return string.Join("  ·  ", parts);
        }

    }
}
