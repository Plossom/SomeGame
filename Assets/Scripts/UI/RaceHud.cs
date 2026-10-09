using SomeGame.Race;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace SomeGame.UI
{
    /// <summary>Top-of-screen race info: lap, race time, position, and a short lap-time flash.</summary>
    public class RaceHud : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] TMP_Text lapLabel;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text positionLabel;
        [Tooltip("Shows the last lap time for a few seconds after each lap.")]
        [SerializeField] TMP_Text lapFlashLabel;
        [SerializeField, Min(0f)] float lapFlashSeconds = 2.5f;
        [Tooltip("Blinking warning: WRONG WAY or MISSED CHECKPOINT.")]
        [SerializeField, FormerlySerializedAs("wrongWayLabel")] TMP_Text warningLabel;

        float _hideFlashAt;

        void OnEnable() => race.PlayerLapCompleted += FlashLap;
        void OnDisable() => race.PlayerLapCompleted -= FlashLap;

        void Start()
        {
            lapFlashLabel.gameObject.SetActive(false);
            if (warningLabel != null) warningLabel.gameObject.SetActive(false);
        }

        void Update()
        {
            var player = race.Player;
            lapLabel.text = $"{player.CurrentLap}<size=55%><color={NeonTheme.Html(NeonTheme.Dim)}>/{race.Laps}</color></size>";
            timeLabel.text = TimeFormat.Race(race.RaceTime);
            int position = race.PositionOf(player);
            positionLabel.text = $"P{position}<size=50%><color={NeonTheme.Html(NeonTheme.Dim)}>/{race.Standings.Count}</color></size>";
            positionLabel.color = position == 1 ? NeonTheme.Lime : NeonTheme.Text;

            if (warningLabel != null)
            {
                string warning = race.State != RaceState.Racing ? null
                    : player.IsWrongWay ? "WRONG WAY"
                    : player.MissedCheckpoint ? "MISSED CHECKPOINT\n<size=60%>GO BACK</size>"
                    : null;
                bool show = warning != null;
                if (warningLabel.gameObject.activeSelf != show) warningLabel.gameObject.SetActive(show);
                if (show)
                {
                    warningLabel.text = warning;
                    warningLabel.alpha = Mathf.PingPong(Time.time * 3f, 1f) * 0.6f + 0.4f;
                }
            }

            if (lapFlashLabel.gameObject.activeSelf && Time.time >= _hideFlashAt)
                lapFlashLabel.gameObject.SetActive(false);
        }

        void FlashLap(float lapTime, bool newBest)
        {
            lapFlashLabel.text = $"LAP  {TimeFormat.Race(lapTime)}{(newBest ? "  //  BEST" : "")}";
            lapFlashLabel.color = newBest ? NeonTheme.Lime : NeonTheme.Cyan;
            lapFlashLabel.gameObject.SetActive(true);
            _hideFlashAt = Time.time + lapFlashSeconds;
        }
    }
}
