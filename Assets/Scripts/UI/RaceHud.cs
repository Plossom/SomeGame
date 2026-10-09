using SomeGame.Race;
using TMPro;
using UnityEngine;

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
        [Tooltip("Blinking warning shown while the player drives the wrong way.")]
        [SerializeField] TMP_Text wrongWayLabel;

        float _hideFlashAt;

        void OnEnable() => race.PlayerLapCompleted += FlashLap;
        void OnDisable() => race.PlayerLapCompleted -= FlashLap;

        void Start()
        {
            lapFlashLabel.gameObject.SetActive(false);
            if (wrongWayLabel != null) wrongWayLabel.gameObject.SetActive(false);
        }

        void Update()
        {
            var player = race.Player;
            lapLabel.text = $"LAP {player.CurrentLap}/{race.Laps}";
            timeLabel.text = TimeFormat.Race(race.RaceTime);
            int position = race.PositionOf(player);
            positionLabel.text = $"{TimeFormat.Ordinal(position)}<size=60%>/{race.Standings.Count}</size>";

            if (wrongWayLabel != null)
            {
                bool show = player.IsWrongWay && race.State == RaceState.Racing;
                if (wrongWayLabel.gameObject.activeSelf != show) wrongWayLabel.gameObject.SetActive(show);
                if (show) wrongWayLabel.alpha = Mathf.PingPong(Time.time * 3f, 1f) * 0.6f + 0.4f;
            }

            if (lapFlashLabel.gameObject.activeSelf && Time.time >= _hideFlashAt)
                lapFlashLabel.gameObject.SetActive(false);
        }

        void FlashLap(float lapTime, bool newBest)
        {
            lapFlashLabel.text = $"Lap {TimeFormat.Race(lapTime)}{(newBest ? "  BEST!" : "")}";
            lapFlashLabel.color = newBest ? new Color(1f, 0.85f, 0.3f) : Color.white;
            lapFlashLabel.gameObject.SetActive(true);
            _hideFlashAt = Time.time + lapFlashSeconds;
        }
    }
}
