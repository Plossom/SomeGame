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
        [Tooltip("The frost over what sticks out past the card's edge (the number medallion, the cross).")]
        [SerializeField] UnityEngine.UI.Image[] edgeVeils;
        [SerializeField] RectTransform glow;
        [SerializeField] TMP_Text numberLabel, chapterLabel, titleLabel, recordsLabel;
        [SerializeField] UnityEngine.UI.Image picture;
        [SerializeField] UnityEngine.UI.RawImage blurredPicture;
        [Tooltip("A band of light that sweeps across the picture now and then (on the card in the middle).")]
        [SerializeField] RectTransform shine;
        [SerializeField] GameObject lockCover;
        [SerializeField] TMP_Text lockLabel;
        [SerializeField] UnityEngine.UI.Image[] difficultyPips;
        [Tooltip("The three star badges on the track (a face, a rim, the stars S0.., the place): gold once earned.")]
        [SerializeField] RectTransform[] starGroups;
        [SerializeField] TMP_Text[] starTimes;
        [Tooltip("The track's gold fill (up to the last star earned) and the ring on the next star to win.")]
        [SerializeField] RectTransform starFill, starNext;
        [SerializeField] Sprite sparkle;
        const float FillFirst = 60f, NodeGap = 250f;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFace, startShadow;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] RectTransform startRing;
        [Tooltip("The race's rival leaning in over the picture: their face, a speech bubble, and their gadget as the prize.")]
        [SerializeField] CanvasGroup rival;
        [SerializeField] UnityEngine.UI.Image rivalPicture;
        [SerializeField] TMP_Text rivalLine, rivalPrize;
        [Tooltip("Their speech bubble: shown for a while when the card comes to rest, again when their face is tapped.")]
        [SerializeField] CanvasGroup rivalBubble, rivalPrizeGroup;
        [SerializeField] UnityEngine.UI.Button rivalFace;
        const float BubbleSeconds = 4.5f;
        float _bubbleAt = -100f;

        float _focusedAt = -10f, _pressedAt = -10f, _settledAt = -10f;
        bool _focused, _settled, _canEnter, _hasRival;
        readonly bool[] _earned = new bool[3];
        // Stars: how many the race has, and how many the card shows (new ones fill in once it comes to rest).
        int _stars, _starsFrom;
        float _starsShown, _starLandedAt = -10f;
        int _starLanded = -1;
        LevelDefinition _level;
        readonly List<(RectTransform rt, UnityEngine.UI.Image image, Vector2 velocity, float born)> _sparks = new();

        public int Index { get; private set; }
        public UnityEngine.UI.Button FocusButton => focusButton;
        public UnityEngine.UI.Button StartButton => startButton;
        public UnityEngine.UI.Button CloseButton => closeButton;


        void Awake()
        {
            if (rivalFace != null) rivalFace.onClick.AddListener(() =>
            {
                _bubbleAt = Time.unscaledTime;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
            });
        }

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
            string ink = Theme.Html(Theme.Ink);
            var parts = new List<string>();
            if (bestPlace.HasValue) parts.Add($"BEST <color={ink}>{TimeFormat.Ordinal(bestPlace.Value).ToUpperInvariant()}</color>");
            if (best.HasValue) parts.Add($"TIME <color={ink}>{TimeFormat.Race(best.Value)}</color>");
            if (bestLap.HasValue && !level.Sectioned) parts.Add($"LAP <color={ink}>{TimeFormat.Race(bestLap.Value)}</color>");
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

            // The places for 1, 2 and 3 stars on the track, lit as far as this race's stars go; stars won since the
            // card was last seen come in when it comes to rest.
            _level = level;
            _stars = ProgressStore.StarsOf(level);
            _starsFrom = canEnter ? ProgressStore.StarsBeforeNew(level) : _stars;
            _starsShown = _starsFrom;
            _starLanded = -1;
            for (int i = 0; i < starTimes.Length; i++)
                starTimes[i].text = i < LevelDefinition.StarPlaces.Length ? LevelDefinition.StarPlaces[i] : "-";
            ShowStars();

            // Their rival leans in (once met; a silhouette with a teaser before).
            var character = RivalCharacter.For(level);
            _hasRival = character != null && canEnter && rival != null;
            if (rival != null) rival.gameObject.SetActive(_hasRival);
            if (_hasRival)
            {
                bool met = RivalProgress.HasMet(character.name);
                bool beaten = RivalProgress.IsBeaten(character.name);
                rivalPicture.sprite = character.portrait;
                rivalPicture.color = met ? Color.white : new Color(0.08f, 0.1f, 0.12f, 0.95f);
                rivalLine.text = met ? character.cardLine : "Someone is waiting for you here...";
                var gadget = Garage.Find(character.gadget, Garage.Slot.Gadget);
                rivalPrize.text = beaten ? "BEATEN!  GADGET WON" : $"WIN  {gadget?.Name}";
            }

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

        // The badges lit as far as the stars shown; the fill up to the last one; the ring on the next.
        void ShowStars()
        {
            int lit = Mathf.FloorToInt(_starsShown + 0.001f);
            for (int i = 0; i < starTimes.Length; i++)
            {
                bool earned = lit >= i + 1;
                if (i < _earned.Length) _earned[i] = earned;
                starTimes[i].color = earned ? Theme.Ink : Theme.WithAlpha(Theme.InkSoft, 0.55f);
                foreach (var star in Stars(i)) star.color = earned ? Color.white : Theme.WithAlpha(Theme.StoneDark, 0.55f);
                var medal = starGroups != null && i < starGroups.Length ? starGroups[i] : null;
                if (medal == null) continue;
                var face = medal.Find("Face")?.GetComponent<UnityEngine.UI.Image>();
                if (face != null) face.color = earned ? Theme.Amber : Theme.CreamDark;
                var rim = medal.Find("Rim")?.GetComponent<UnityEngine.UI.Image>();
                if (rim != null) rim.color = earned ? Theme.OrangeDeep : Theme.WithAlpha(Theme.StoneDark, 0.35f);
            }
            if (starFill != null)
            {
                float width = _starsShown <= 0f ? 0f : _starsShown < 1f ? FillFirst * _starsShown : FillFirst + (_starsShown - 1f) * NodeGap;
                starFill.sizeDelta = new Vector2(Mathf.Max(width, 24f), starFill.sizeDelta.y);
                starFill.gameObject.SetActive(width > 1f);
            }
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
            if (rival != null && _hasRival) rival.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a * 2.5f));
            if (focused && !_focused) _focusedAt = Time.unscaledTime;
            _focused = focused;
            // Come to rest in the middle (not just passing through): the earned stars give a little bump.
            bool rest = focused && settled;
            if (rest && !_settled) { _settledAt = Time.unscaledTime; _bubbleAt = _settledAt + 0.35f; }
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

        // Sparkles bursting out of a star badge that was just won.
        void Burst(RectTransform badge)
        {
            if (sparkle == null || badge == null) return;
            for (int i = 0; i < 12; i++)
            {
                var go = new GameObject("Spark", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(badge.parent, false);
                rt.sizeDelta = Vector2.one * UnityEngine.Random.Range(26f, 52f);
                rt.anchoredPosition = badge.anchoredPosition + new Vector2(0f, 22f);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = sparkle;
                image.raycastTarget = false;
                image.color = i % 3 == 0 ? Color.white : Theme.Amber;
                float a = (i / 12f + UnityEngine.Random.Range(-0.03f, 0.03f)) * Mathf.PI * 2f;
                _sparks.Add((rt, image, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(380f, 620f), Time.unscaledTime));
            }
        }

        /// <summary>START pressed: it dips, and says LOADING.</summary>
        public void Pressed()
        {
            _pressedAt = Time.unscaledTime;
            startLabel.text = "LOADING";
        }

        void Update()
        {
            float t = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            // New stars: once the card is at rest the gold fills along the track to each one in turn; it slams in
            // with a burst of sparkles.
            if (_settled && _starsShown < _stars && t > _settledAt + 0.35f)
            {
                _starsShown = Mathf.MoveTowards(_starsShown, _stars, dt / 0.45f);
                int reached = Mathf.FloorToInt(_starsShown + 0.001f);
                if (reached - 1 > _starLanded && reached >= 1)
                {
                    _starLanded = reached - 1;
                    _starLandedAt = t;
                    Burst(starGroups[_starLanded]);
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.7f);
                }
                ShowStars();
                if (_starsShown >= _stars && _level != null) ProgressStore.NewStarsShown(_level);
            }
            // Come to rest in the middle: the earned stars bump up one after another (from their full size, so
            // nothing jumps when a card arrives); one just won slams in big.
            for (int i = 0; starGroups != null && i < starGroups.Length; i++)
            {
                if (starGroups[i] == null) continue;
                float k = Mathf.Clamp01((t - _settledAt - 0.1f - i * 0.1f) / 0.32f);
                float bump = _settled && i < _earned.Length && _earned[i] && i < _starsFrom ? Mathf.Sin(k * Mathf.PI) * (1f - k * 0.4f) : 0f;
                foreach (var star in Stars(i)) star.rectTransform.localScale = Vector3.one * (1f + 0.35f * bump);
                float slam = i == _starLanded ? Mathf.Clamp01((t - _starLandedAt) / 0.45f) : 1f;
                starGroups[i].localScale = Vector3.one * (slam < 1f ? 1f + 0.45f * Mathf.Sin(slam * Mathf.PI) * (1f - slam) + 0.1f * Mathf.Sin(slam * Mathf.PI * 3f) * (1f - slam) : 1f);
            }
            // The next star to win: a ring breathing round its badge.
            if (starNext != null)
            {
                int next = Mathf.FloorToInt(_starsShown + 0.001f);
                bool show = _canEnter && next < 3 && starGroups != null && next < starGroups.Length;
                starNext.gameObject.SetActive(show);
                if (show)
                {
                    starNext.anchoredPosition = starGroups[next].anchoredPosition + new Vector2(0f, 22f);
                    float c = Mathf.Repeat(t, 1.6f) / 1.6f;
                    starNext.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.25f, c);
                    var ring = starNext.GetComponent<UnityEngine.UI.Image>();
                    ring.color = Theme.WithAlpha(Theme.Orange, 0.8f * (1f - c));
                }
            }
            // The sparkles fly out and fade.
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var (rt, image, velocity, born) = _sparks[i];
                float age = (t - born) / 0.6f;
                if (age >= 1f || rt == null) { if (rt != null) Destroy(rt.gameObject); _sparks.RemoveAt(i); continue; }
                rt.anchoredPosition += velocity * dt * (1f - age);
                rt.localScale = Vector3.one * (1f - age * 0.6f);
                rt.localRotation = Quaternion.Euler(0f, 0f, age * 180f);
                var c = image.color; c.a = 1f - age * age; image.color = c;
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
            // The rival leans in when the card comes to rest, and sways a little.
            if (rival != null && _hasRival)
            {
                float lean = _settled ? Mathf.Clamp01((t - _settledAt - 0.15f) / 0.4f) : 0f;
                rival.transform.localScale = Vector3.one * (Mathf.Lerp(0.85f, 1f, lean) + 0.1f * Mathf.Sin(lean * Mathf.PI));
                rival.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 4f, lean) + 1.5f * Mathf.Sin(t * 1.7f));
                // The speech bubble: pops up, stays a while, fades away (a tap on the face brings it back).
                if (rivalBubble != null)
                {
                    float b = t - _bubbleAt;
                    float shown = b < 0f ? 0f : Mathf.Clamp01(b / 0.2f) * (1f - Mathf.Clamp01((b - BubbleSeconds) / 0.4f));
                    rivalBubble.alpha = shown;
                    if (rivalPrizeGroup != null) rivalPrizeGroup.alpha = shown;
                    rivalBubble.transform.localScale = Vector3.one * (b < 0f ? 0.6f : Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(b / 0.2f)) + 0.06f * Mathf.Sin(Mathf.Clamp01(b / 0.35f) * Mathf.PI));
                }
            }
            float press = Mathf.Clamp01((t - _pressedAt) / 0.35f);
            startButton.transform.localScale = Vector3.one * (press < 1f ? 1f - 0.12f * Mathf.Sin(press * Mathf.PI) : 1f);
        }

    }
}
