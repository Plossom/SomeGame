using System;
using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The race info window on the map (the ⓘ button by the records): the whole track from above, as on
    /// the loading screen, with the race's description, its difficulty, laps and rivals, and what waits
    /// on the track (jumps, oil, shortcuts...). START in the window enters the race.
    /// </summary>
    public class RaceInfoWindow : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Button closeArea;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] TMP_Text chapterLabel;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] UnityEngine.UI.Image trackPicture;
        [SerializeField] TMP_Text descriptionLabel;
        [Tooltip("Difficulty pips, easy to hard.")]
        [SerializeField] UnityEngine.UI.Image[] difficultyPips;
        [SerializeField] TMP_Text lapsLabel;
        [SerializeField] TMP_Text rivalsLabel;
        [SerializeField] TMP_Text featuresLabel;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFace;
        [SerializeField] UnityEngine.UI.Image startShadow;
        [SerializeField] TMP_Text startLabel;

        Action _onStart;

        public bool IsOpen => panel.activeSelf;

        void Awake()
        {
            closeArea.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            startButton.onClick.AddListener(() => { Close(); _onStart?.Invoke(); });
            panel.SetActive(false);
        }

        public void Show(LevelDefinition level, bool canEnter, Action onStart)
        {
            if (level == null) return;
            _onStart = onStart;
            chapterLabel.text = level.chapter;
            titleLabel.text = level.displayName.ToUpperInvariant();
            trackPicture.sprite = level.preview;
            trackPicture.enabled = level.preview != null;
            descriptionLabel.text = level.description;
            for (int i = 0; i < difficultyPips.Length; i++)
                difficultyPips[i].color = i < level.difficulty ? Theme.Orange : Theme.WithAlpha(Theme.StoneDark, 0.35f);
            lapsLabel.text = level.laps.ToString();
            rivalsLabel.text = level.rivalCount.ToString();
            featuresLabel.text = Features(level);

            startButton.interactable = canEnter;
            startFace.color = canEnter ? Theme.Orange : Theme.Stone;
            startShadow.color = canEnter ? Theme.OrangeDeep : Theme.StoneDark;
            startLabel.text = canEnter ? "START" : "LOCKED";
            startLabel.color = canEnter ? Theme.White : Theme.StoneDark;
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

        // What waits on the track, from its layout: one line each.
        static string Features(LevelDefinition level)
        {
            var track = level.track;
            if (track == null) return "";
            var lines = new List<string>();
            void Add(bool has, string text) { if (has) lines.Add("•  " + text); }
            Add(track.waterWorld, "Islands in a lagoon");
            Add(track.rivers.Count == 1, "A river to jump");
            Add(track.rivers.Count > 1, $"{track.rivers.Count} streams to jump");
            Add(track.lakes.Count > 0, "Lake crossings");
            Add(track.gaps.Count > 0, "Gaps in the road");
            Add(track.bouncers.Count > 0, "Jump pads");
            Add(track.geysers.Count > 0, "Leaping fish");
            Add(track.sweepers.Count > 0, "Rolling logs and bales");
            Add(track.oil.Count > 0, "Oil slicks");
            Add(track.shortcuts.Count > 0, "Hidden shortcuts");
            if (lines.Count == 0) lines.Add("•  Just road: drive and drift!");
            return string.Join("\n", lines);
        }
    }
}
