using System;
using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// One race of the <see cref="RaceDeck"/>: a big card with the race's number on a medallion, its name, the
    /// whole track from above, difficulty, the places that earn stars (earned ones lit), your records and
    /// START; the cross on its corner closes the deck. Off to the side of the deck it is frosted over and its picture
    /// blurred (<see cref="SetFocus"/>). A race still locked keeps its track a secret: a grey plate with a lock and
    /// the stars it needs.
    /// </summary>
    public class RaceCard : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [Tooltip("Tapping a card at the side of the deck brings it to the middle.")]
        [SerializeField] UnityEngine.UI.Button focusButton;
        [SerializeField] UnityEngine.UI.Image veil;
        [Tooltip("The frost over what sticks out past the card's edge (the number medallion, the trophy, the cross).")]
        [SerializeField] UnityEngine.UI.Image[] edgeVeils;
        [SerializeField] RectTransform glow;
        [SerializeField] TMP_Text numberLabel, chapterLabel, titleLabel, recordsLabel;
        [SerializeField] UnityEngine.UI.Image picture;
        [SerializeField] UnityEngine.UI.RawImage blurredPicture;
        [Tooltip("A band of light that sweeps across the picture now and then (on the card in the middle).")]
        [SerializeField] RectTransform shine;
        [SerializeField] GameObject lockCover;
        [SerializeField] TMP_Text lockLabel;
        [SerializeField] GameObject wonBadge;
        [SerializeField] UnityEngine.UI.Image[] difficultyPips;
        [Tooltip("The three star medals (a face, a rim, the stars S0.., the places): gold once the race has earned them.")]
        [SerializeField] RectTransform[] starGroups;
        [SerializeField] TMP_Text[] starTimes;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFace, startShadow;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] RectTransform startRing;

        float _focusedAt = -10f, _pressedAt = -10f, _settledAt = -10f;
        bool _focused, _settled, _canEnter;
        readonly bool[] _earned = new bool[3];

        public int Index { get; private set; }
        public UnityEngine.UI.Button FocusButton => focusButton;
        public UnityEngine.UI.Button StartButton => startButton;
        public UnityEngine.UI.Button CloseButton => closeButton;

        public void Bind(LevelDefinition level, int index, bool canEnter, CupDefinition cup)
        {
            Index = index;
            _canEnter = canEnter;
            name = $"Card{index + 1}";
            numberLabel.text = (index + 1).ToString();
            chapterLabel.text = cup != null ? $"{cup.displayName} · RACE {index + 1}" : level.chapter;
            titleLabel.text = level.displayName.ToUpperInvariant();

            var best = ProgressStore.BestTimeOf(level);
            var bestPlace = ProgressStore.BestPlaceOf(level);
            var bestLap = level.track != null ? BestLapStore.Get(level.track.name) : null;
            string ink = Theme.Html(Theme.Ink), lapWord = level.Sectioned ? "SECTION" : "LAP";
            var parts = new List<string>();
            if (bestPlace.HasValue) parts.Add($"BEST <color={ink}>{TimeFormat.Ordinal(bestPlace.Value).ToUpperInvariant()}</color>");
            if (best.HasValue) parts.Add($"TIME <color={ink}>{TimeFormat.Race(best.Value)}</color>");
            if (bestLap.HasValue) parts.Add($"{lapWord} <color={ink}>{TimeFormat.Race(bestLap.Value)}</color>");
            recordsLabel.text = parts.Count > 0 ? string.Join("    ", parts) : "NO RACE YET";

            // A locked race keeps its track a secret (a grey plate in a grey frame).
            var preview = canEnter ? level.preview : null;
            if (picture.transform.parent.TryGetComponent(out UnityEngine.UI.Image frame)) frame.color = canEnter ? Theme.Ink : Theme.StoneDark;
            picture.sprite = preview;
            picture.enabled = preview != null;
            if (blurredPicture != null)
            {
                blurredPicture.texture = preview != null ? UiBlur.Blurred(preview.texture) : null;
                blurredPicture.enabled = blurredPicture.texture != null;
            }
            for (int i = 0; i < difficultyPips.Length; i++)
                difficultyPips[i].color = i < level.difficulty ? Theme.Orange : Theme.WithAlpha(Theme.StoneDark, 0.3f);

            // The places for 1, 2 and 3 stars; lit up as far as this race's stars go.
            int stars = ProgressStore.StarsOf(level);
            for (int i = 0; i < starTimes.Length; i++)
            {
                starTimes[i].text = i < LevelDefinition.StarPlaces.Length ? LevelDefinition.StarPlaces[i] : "-";
                bool earned = stars >= i + 1;
                if (i < _earned.Length) _earned[i] = earned;
                starTimes[i].color = earned ? Theme.Ink : Theme.WithAlpha(Theme.InkSoft, 0.55f);
                foreach (var star in Stars(i)) star.color = earned ? Theme.Amber : Theme.WithAlpha(Theme.StoneDark, 0.3f);
                var medal = starGroups != null && i < starGroups.Length ? starGroups[i] : null;
                if (medal == null) continue;
                var face = medal.Find("Face")?.GetComponent<UnityEngine.UI.Image>();
                if (face != null) face.color = earned ? Color.white : Theme.CreamDark;
                var rim = medal.Find("Rim");
                if (rim != null) rim.gameObject.SetActive(earned);
            }
            if (wonBadge != null) wonBadge.SetActive(ProgressStore.HasWon(level));

            lockCover.SetActive(!canEnter);
            if (!canEnter)
            {
                int have = Mathf.Min(ProgressStore.TotalStars, level.starsRequired);
                lockLabel.text = $"<size=130%>{have} / {level.starsRequired}</size>\nSTARS TO UNLOCK";
            }
            startFace.color = canEnter ? Theme.Orange : Theme.Stone;
            startShadow.color = canEnter ? Theme.OrangeDeep : Theme.StoneDark;
            startLabel.text = canEnter ? "START" : "LOCKED";
            startLabel.color = canEnter ? Theme.White : Theme.StoneDark;
        }

        IEnumerable<UnityEngine.UI.Image> Stars(int group) =>
            starGroups != null && group < starGroups.Length && starGroups[group] != null
                ? System.Linq.Enumerable.Where(starGroups[group].GetComponentsInChildren<UnityEngine.UI.Image>(true), image => image.name.StartsWith("S"))
                : Array.Empty<UnityEngine.UI.Image>();

        /// <summary>
        /// How far from the middle of the deck the card is (0 in the middle, 1 one place off): the frost, the blurred
        /// picture, and whether its buttons work. <paramref name="settled"/>: the deck has come to rest.
        /// </summary>
        public void SetFocus(float away, bool settled)
        {
            float a = Mathf.Clamp01(Mathf.Abs(away));
            var frost = Theme.WithAlpha(Theme.Cream, 0.62f * Mathf.SmoothStep(0f, 1f, a));
            if (veil != null) veil.color = frost;
            if (edgeVeils != null) foreach (var edge in edgeVeils) if (edge != null) edge.color = frost;
            if (blurredPicture != null && blurredPicture.enabled) blurredPicture.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.6f));
            bool focused = a < 0.08f;
            startButton.interactable = focused && _canEnter;
            focusButton.interactable = !focused;
            closeButton.interactable = focused;
            if (focused && !_focused) _focusedAt = Time.unscaledTime;
            _focused = focused;
            // Come to rest in the middle (not just passing through): the earned stars give a little bump.
            bool rest = focused && settled;
            if (rest && !_settled) _settledAt = Time.unscaledTime;
            _settled = rest;
            if (glow != null)
            {
                glow.gameObject.SetActive(a < 0.6f);
                glow.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 2.4f));
                var image = glow.GetComponent<UnityEngine.UI.Image>();
                if (image != null) image.color = Theme.WithAlpha(Theme.Amber, 0.32f * (1f - a / 0.6f));
            }
        }

        public void SetAlpha(float alpha) => group.alpha = alpha;

        /// <summary>START pressed: it dips, and says LOADING.</summary>
        public void Pressed()
        {
            _pressedAt = Time.unscaledTime;
            startLabel.text = "LOADING";
        }

        void Update()
        {
            float t = Time.unscaledTime;
            // Come to rest in the middle: the earned stars bump up one after another (from their full size, so
            // nothing jumps when a card arrives).
            for (int i = 0; starGroups != null && i < starGroups.Length; i++)
            {
                if (starGroups[i] == null) continue;
                float k = Mathf.Clamp01((t - _settledAt - 0.1f - i * 0.1f) / 0.32f);
                float bump = _settled && i < _earned.Length && _earned[i] ? Mathf.Sin(k * Mathf.PI) * (1f - k * 0.4f) : 0f;
                foreach (var star in Stars(i)) star.rectTransform.localScale = Vector3.one * (1f + 0.35f * bump);
            }
            // The ring round START breathes; the light sweeps over the picture every few seconds.
            if (startRing != null)
            {
                startRing.gameObject.SetActive(_focused && _canEnter);
                float c = Mathf.Repeat(t, 1.4f) / 1.4f;
                startRing.localScale = Vector3.one * Mathf.Lerp(1f, 1.18f, c);
                var ring = startRing.GetComponent<UnityEngine.UI.Image>();
                if (ring != null) ring.color = Theme.WithAlpha(Theme.Orange, 0.55f * (1f - c));
            }
            if (shine != null)
            {
                shine.gameObject.SetActive(_focused);
                float s = Mathf.Repeat(t - _focusedAt - 0.25f, 3.6f);
                var frame = (RectTransform)shine.parent;
                float w = frame.rect.width + frame.rect.height;
                shine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.6f, w * 0.6f, Mathf.Clamp01(s / 0.7f)), 0f);
            }
            float press = Mathf.Clamp01((t - _pressedAt) / 0.35f);
            startButton.transform.localScale = Vector3.one * (press < 1f ? 1f - 0.12f * Mathf.Sin(press * Mathf.PI) : 1f);
        }

    }
}
