using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Results screen shown when the player finishes: position, stars, total time, best lap, the
    /// time needed for the next star, NEXT (back to the map) and Retry.
    /// </summary>
    public class FinishScreen : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text raceLabel;
        [SerializeField] TMP_Text positionLabel;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] GameObject newBestTag;
        [Tooltip("Three star images, filled left to right.")]
        [SerializeField] UnityEngine.UI.Image[] stars;
        [SerializeField] TMP_Text totalValue;
        [SerializeField] TMP_Text bestLapValue;
        [SerializeField] TMP_Text nextStarLabel;
        [SerializeField] TMP_Text nextStarValue;
        [Tooltip("Back to the map. Hidden when the race scene was opened directly.")]
        [SerializeField] UnityEngine.UI.Button continueButton;
        [SerializeField] UnityEngine.UI.Button retryButton;

        bool _newBest;

        void Awake()
        {
            panel.SetActive(false);
            retryButton.onClick.AddListener(race.Restart);
            continueButton.onClick.AddListener(GameSession.ReturnToMap);
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
            var level = race.Level;
            int position = race.FinalPosition;
            bool won = position == 1;

            raceLabel.text = level != null ? level.chapter : "TEST RACE";
            positionLabel.text = $"P{position}";
            positionLabel.color = won ? NeonTheme.Lime : NeonTheme.Text;
            subtitle.text = won ? "FINISHED  //  WINNER" : $"FINISHED  //  {TimeFormat.Ordinal(position).ToUpperInvariant()} PLACE";
            newBestTag.SetActive(_newBest);

            int earned = race.StarsEarned;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(level != null);
                stars[i].color = i < earned ? NeonTheme.Lime : NeonTheme.Faint;
            }

            totalValue.text = TimeFormat.Race(race.RaceTime);
            float bestLap = float.MaxValue;
            foreach (var lap in race.Player.LapTimes) bestLap = Mathf.Min(bestLap, lap);
            bestLapValue.text = bestLap < float.MaxValue ? TimeFormat.Race(bestLap) : "-";

            if (level == null)
            {
                nextStarLabel.text = "NO STARS IN TEST RACES";
                nextStarValue.text = "";
            }
            else if (!won)
            {
                nextStarLabel.text = "WIN TO EARN STARS";
                nextStarValue.text = "";
            }
            else if (earned >= level.starTimes.Length)
            {
                nextStarLabel.text = "ALL STARS";
                nextStarValue.text = $"{earned}/{level.starTimes.Length}";
            }
            else
            {
                nextStarLabel.text = $"{TimeFormat.Ordinal(earned + 1).ToUpperInvariant()} STAR AT";
                nextStarValue.text = TimeFormat.Race(level.starTimes[earned]);
            }

            continueButton.gameObject.SetActive(level != null);
            panel.SetActive(true);
        }
    }
}
