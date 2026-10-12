using System;
using System.Collections.Generic;
using SomeGame.Race;
using SomeGame.Track;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The race info window on the map (the ⓘ button by the records), with as little text as possible:
    /// the race name, the whole track from above (as on the loading screen), its difficulty as pips and a
    /// row of round badges showing what waits on the track (ramps, jump pads, oil, fish, penguins...),
    /// each with the game's own picture of it. START in the window enters the race.
    /// </summary>
    public class RaceInfoWindow : MonoBehaviour
    {
        [Serializable]
        public struct FeatureIcon
        {
            public string key;
            public Sprite sprite;
            [Tooltip("Tint of the picture (white pictures need a dark one). Clear = no tint.")]
            public Color tint;
        }

        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Button closeArea;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] TMP_Text chapterLabel;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] UnityEngine.UI.Image trackPicture;
        [Tooltip("Difficulty pips, easy to hard.")]
        [SerializeField] UnityEngine.UI.Image[] difficultyPips;
        [Tooltip("Row the feature badges go into, and the inactive badge to copy (its child \"Icon\" shows the picture).")]
        [SerializeField] RectTransform featureRow;
        [SerializeField] RectTransform featureTemplate;
        [SerializeField] FeatureIcon[] icons;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFace;
        [SerializeField] UnityEngine.UI.Image startShadow;
        [SerializeField] TMP_Text startLabel;

        Action _onStart;
        readonly List<GameObject> _badges = new();

        public bool IsOpen => panel.activeSelf;

        void Awake()
        {
            closeArea.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            startButton.onClick.AddListener(() => { Close(); _onStart?.Invoke(); });
            if (featureTemplate != null) featureTemplate.gameObject.SetActive(false);
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
            for (int i = 0; i < difficultyPips.Length; i++)
                difficultyPips[i].color = i < level.difficulty ? Theme.Orange : Theme.WithAlpha(Theme.StoneDark, 0.35f);
            ShowFeatures(level.track);

            startButton.interactable = canEnter;
            startFace.color = canEnter ? Theme.Orange : Theme.Stone;
            startShadow.color = canEnter ? Theme.OrangeDeep : Theme.StoneDark;
            startLabel.text = canEnter ? "START" : "LOCKED";
            startLabel.color = canEnter ? Theme.White : Theme.StoneDark;
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

        void ShowFeatures(TrackLayout track)
        {
            foreach (var badge in _badges) Destroy(badge);
            _badges.Clear();
            if (track == null || featureRow == null || featureTemplate == null) return;
            foreach (var key in Features(track))
            {
                var entry = Array.Find(icons, i => i.key == key);
                var sprite = entry.sprite;
                if (sprite == null) continue;
                var badge = Instantiate(featureTemplate, featureRow);
                badge.gameObject.SetActive(true);
                badge.name = key;
                var icon = badge.Find("Mask/Icon")?.GetComponent<UnityEngine.UI.Image>();
                if (icon != null)
                {
                    icon.sprite = sprite;
                    icon.color = entry.tint.a > 0f ? entry.tint : Color.white;
                }
                _badges.Add(badge.gameObject);
            }
        }

        // What waits on the track, from its layout (keys of the badge pictures), at most seven.
        static List<string> Features(TrackLayout track)
        {
            var keys = new List<string>();
            void Add(bool has, string key) { if (has && keys.Count < 7) keys.Add(key); }
            bool Has(TrackLayout.BranchKind kind) => track.branches.Exists(b => b.kind == kind);
            Add(Has(TrackLayout.BranchKind.Roof), "roof");
            Add(track.banks.Count > 0, "wall");
            Add(Has(TrackLayout.BranchKind.Ice), "ice");
            Add(track.rivers.Count > 0 || track.gaps.Count > 0, "ramp");
            Add(track.bouncers.Count > 0, track.waterWorld || track.gaps.Count > 0 || track.rivers.Count > 0 ? "gummiboat" : "pad");
            Add(track.dashPanels.Count > 0, "dash");
            Add(track.penguins.Count > 0, "penguin");
            Add(track.geysers.Count > 0, "fish");
            Add(track.sweepers.Exists(s => s.kind == TrackLayout.SweeperKind.Snowball), "snowball");
            Add(track.sweepers.Exists(s => s.kind == TrackLayout.SweeperKind.Log), "log");
            Add(track.sweepers.Exists(s => s.kind == TrackLayout.SweeperKind.HayBale), "hay");
            Add(track.oil.Count > 0, "oil");
            Add(track.shortcuts.Count > 0, "shortcut");
            return keys;
        }
    }
}
