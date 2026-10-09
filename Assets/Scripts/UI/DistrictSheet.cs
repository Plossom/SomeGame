using System;
using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Bottom sheet for a district (Grand Prix): name, tagline, what it adds, star progress, the
    /// race cards and START. Locked or coming-soon districts show what is needed instead.
    /// Slides in and out with unscaled time.
    /// </summary>
    public class DistrictSheet : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField, Min(0.01f)] float slideSeconds = 0.3f;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] UnityEngine.UI.Image accentBar;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text tagline;
        [SerializeField] TMP_Text feature;
        [SerializeField] UnityEngine.UI.Image featureFill;
        [SerializeField] UnityEngine.UI.Image featureOutline;
        [SerializeField] TMP_Text progress;

        [Header("Open district")]
        [SerializeField] GameObject racesGroup;
        [SerializeField] RectTransform cardsContent;
        [SerializeField] RaceCard cardTemplate;
        [SerializeField] TMP_Text raceInfo;
        [SerializeField] TMP_Text[] starTimes;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFill;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] GameObject startRing;

        [Header("Locked district")]
        [SerializeField] GameObject lockedGroup;
        [SerializeField] TMP_Text lockedTitle;
        [SerializeField] TMP_Text lockedText;
        [SerializeField] UnityEngine.UI.Image lockedBarFill;

        readonly List<RaceCard> _cards = new();
        DistrictDefinition _district;
        int _selected;
        float _shown, _target;

        public event Action Closed;
        public bool IsOpen => _target > 0.5f;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            startButton.onClick.AddListener(StartSelected);
            cardTemplate.gameObject.SetActive(false);
            _shown = _target = 0f;
            Apply();
        }

        public void Show(DistrictDefinition district)
        {
            _district = district;
            _selected = DefaultRace(district);
            Refresh();
            _target = 1f;
        }

        public void Hide()
        {
            if (_target < 0.5f) return;
            _target = 0f;
            Closed?.Invoke();
        }

        void Update()
        {
            if (Mathf.Approximately(_shown, _target)) return;
            _shown = Mathf.MoveTowards(_shown, _target, Time.unscaledDeltaTime / slideSeconds);
            Apply();
        }

        void Apply()
        {
            float eased = 1f - Mathf.Pow(1f - _shown, 3f);
            float height = panel.rect.height;
            panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(-height - 40f, 0f, eased));
            panel.gameObject.SetActive(_shown > 0.001f);
        }

        static int DefaultRace(DistrictDefinition district)
        {
            int last = 0;
            for (int i = 0; i < district.races.Count; i++)
            {
                if (!ProgressStore.CanEnter(district, i)) break;
                last = i;
                if (!ProgressStore.HasWon(district.races[i])) return i;
            }
            return last;
        }

        void Refresh()
        {
            var d = _district;
            Color accent = d.accent;
            bool unlocked = ProgressStore.IsUnlocked(d);
            accentBar.color = accent;
            title.text = d.displayName;
            tagline.text = d.tagline;
            feature.text = d.feature;
            feature.color = accent;
            featureFill.color = NeonTheme.WithAlpha(accent, 0.12f);
            featureOutline.color = NeonTheme.WithAlpha(accent, 0.7f);
            progress.text = d.ComingSoon ? "—" : $"{ProgressStore.StarsIn(d)}<size=60%><color={NeonTheme.Html(NeonTheme.Dim)}> / {d.MaxStars}</color></size>";

            racesGroup.SetActive(unlocked);
            lockedGroup.SetActive(!unlocked);
            if (!unlocked)
            {
                int have = ProgressStore.TotalStars;
                lockedTitle.text = d.ComingSoon ? "COMING SOON" : "LOCKED";
                lockedText.text = d.ComingSoon
                    ? $"This district opens in a future update.  <color={NeonTheme.Html(NeonTheme.Lime)}>{Mathf.Min(have, d.starsRequired)} / {d.starsRequired}</color> stars"
                    : $"Collect <color={NeonTheme.Html(NeonTheme.Lime)}>{d.starsRequired}</color> stars to unlock.  You have {have}.";
                lockedBarFill.fillAmount = d.starsRequired > 0 ? Mathf.Clamp01(have / (float)d.starsRequired) : 1f;
                lockedBarFill.color = accent;
                return;
            }

            while (_cards.Count < d.races.Count)
            {
                var card = Instantiate(cardTemplate, cardsContent);
                card.gameObject.SetActive(true);
                _cards.Add(card);
            }
            for (int i = 0; i < _cards.Count; i++)
            {
                bool exists = i < d.races.Count;
                _cards[i].gameObject.SetActive(exists);
                if (!exists) continue;
                var race = d.races[i];
                int index = i;
                _cards[i].Bind(i, race.displayName, ProgressStore.StarsOf(race), ProgressStore.CanEnter(d, i), ProgressStore.HasWon(race),
                    i == _selected, accent, () => { _selected = index; Refresh(); });
            }

            var level = d.races[_selected];
            var best = ProgressStore.BestTimeOf(level);
            raceInfo.text = $"<color={NeonTheme.Html(NeonTheme.Text)}>{level.displayName.ToUpperInvariant()}</color>\n" +
                            $"{level.laps} LAPS  //  {level.rivalCount} RIVALS  //  BEST {(best.HasValue ? TimeFormat.Race(best.Value) : "—")}";
            for (int i = 0; i < starTimes.Length; i++)
                starTimes[i].text = i < level.starTimes.Length ? TimeFormat.Race(level.starTimes[i]) : "-";

            bool canEnter = ProgressStore.CanEnter(d, _selected);
            startButton.interactable = canEnter;
            startFill.color = canEnter ? NeonTheme.Magenta : NeonTheme.PanelRaised;
            startLabel.text = canEnter ? "START" : "LOCKED";
            startLabel.color = canEnter ? NeonTheme.Background : NeonTheme.Dim;
            startRing.SetActive(canEnter);
        }

        void StartSelected()
        {
            if (_district != null && ProgressStore.CanEnter(_district, _selected))
                GameSession.StartRace(_district.races[_selected], _district);
        }
    }
}
