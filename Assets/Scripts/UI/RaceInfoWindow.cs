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
        [Tooltip("Arrows either side of START: the previous / next race (hidden at the first / last).")]
        [SerializeField] UnityEngine.UI.Button previousButton;
        [SerializeField] UnityEngine.UI.Button nextButton;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFace;
        [SerializeField] UnityEngine.UI.Image startShadow;
        [SerializeField] TMP_Text startLabel;

        Action _onStart, _onPrevious, _onNext;
        float _shownAt = -10f;
        int _slide;
        readonly List<GameObject> _badges = new();

        public bool IsOpen => panel.activeSelf;

        void Awake()
        {
            closeArea.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            startButton.onClick.AddListener(() => { Close(); _onStart?.Invoke(); });
            if (previousButton != null) previousButton.onClick.AddListener(() => { _slide = -1; _onPrevious?.Invoke(); });
            if (nextButton != null) nextButton.onClick.AddListener(() => { _slide = 1; _onNext?.Invoke(); });
            if (featureTemplate != null) featureTemplate.gameObject.SetActive(false);
            panel.SetActive(false);
        }

        /// <param name="onPrevious">Shows the previous race (null at the first: the arrow is hidden).</param>
        /// <param name="onNext">Shows the next race (null at the last).</param>
        public void Show(LevelDefinition level, bool canEnter, Action onStart, Action onPrevious = null, Action onNext = null)
        {
            if (level == null) return;
            _onStart = onStart;
            _onPrevious = onPrevious;
            _onNext = onNext;
            if (previousButton != null) previousButton.gameObject.SetActive(onPrevious != null);
            if (nextButton != null) nextButton.gameObject.SetActive(onNext != null);
            _shownAt = Time.unscaledTime;
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

        public void Close()
        {
            panel.SetActive(false);
            _slide = 0;
        }

        // Paging to another race: the picture slides in from the side of the arrow and fades up.
        void Update()
        {
            if (_slide == 0 || trackPicture == null) return;
            float t = Mathf.Clamp01((Time.unscaledTime - _shownAt) / 0.25f);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            var rt = trackPicture.rectTransform;
            rt.anchoredPosition = new Vector2(_slide * 60f * (1f - e), 0f);
            var c = trackPicture.color;
            c.a = e;
            trackPicture.color = c;
            if (t >= 1f) _slide = 0;
        }

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
