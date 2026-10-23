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
        bool _captioned;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text positionLabel;
        [Tooltip("Shows the last lap time for a few seconds after each lap.")]
        [SerializeField] TMP_Text lapFlashLabel;
        [Tooltip("Optional background shown and hidden together with the lap flash.")]
        [SerializeField] GameObject lapFlashRoot;
        [SerializeField, Min(0f)] float lapFlashSeconds = 2.5f;
        [Tooltip("Blinking warning: WRONG WAY or BACK ON THE ROAD! (off-road for too long).")]
        [SerializeField, FormerlySerializedAs("wrongWayLabel")] TMP_Text warningLabel;

        static readonly string Muted = Theme.Html(Theme.Stone);

        float _hideFlashAt;

        GameObject LapFlash => lapFlashRoot != null ? lapFlashRoot : lapFlashLabel.gameObject;

        void OnEnable() => race.PlayerLapCompleted += FlashLap;
        void OnDisable() => race.PlayerLapCompleted -= FlashLap;

        void Start()
        {
            LapFlash.SetActive(false);
            if (warningLabel != null) warningLabel.gameObject.SetActive(false);
        }

        void Update()
        {
            var player = race.Player;
            if (!_captioned)
            {
                _captioned = true;
                var caption = lapLabel.transform.parent.Find("Caption")?.GetComponent<TMP_Text>();
                if (caption != null && race.Sectioned) caption.text = "SECTION";
            }
            lapLabel.text = $"{player.CurrentLap}<size=60%><color={Muted}>/{race.Laps}</color></size>";
            timeLabel.text = TimeFormat.Clock(race.RaceTime); // hundredths only on the results screen
            int position = race.PositionOf(player);
            positionLabel.text = $"{TimeFormat.Ordinal(position).ToUpperInvariant()}<size=60%><color={Muted}>/{race.Standings.Count}</color></size>";
            positionLabel.color = position == 1 ? Theme.Amber : Theme.White;

            if (warningLabel != null)
            {
                string warning = race.State != RaceState.Racing ? null
                    : player.IsWrongWay ? "WRONG WAY"
                    : player.OffRoadShare > 0.35f ? "BACK ON THE ROAD!"
                    : null;
                bool show = warning != null;
                if (warningLabel.gameObject.activeSelf != show) warningLabel.gameObject.SetActive(show);
                if (show)
                {
                    warningLabel.text = warning;
                    warningLabel.alpha = Mathf.PingPong(Time.time * 3f, 1f) * 0.6f + 0.4f;
                }
            }

            if (LapFlash.activeSelf && Time.time >= _hideFlashAt)
                LapFlash.SetActive(false);
        }

        void FlashLap(float lapTime, bool newBest)
        {
            string what = race.Sectioned ? $"SECTION {Mathf.Max(1, race.Player.LapsCompleted)}" : "LAP";
            lapFlashLabel.text = $"{what}  {TimeFormat.Race(lapTime)}{(newBest ? "  ·  BEST" : "")}";
            lapFlashLabel.color = newBest ? Theme.Orange : Theme.Ink;
            LapFlash.SetActive(true);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.lap, 0.7f);
            _hideFlashAt = Time.time + lapFlashSeconds;
        }
    }
}
