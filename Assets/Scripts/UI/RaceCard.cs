using System;
using System.Collections.Generic;
using SomeGame.Race;
using SomeGame.Track;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// One race of the <see cref="RaceDeck"/>: a big card with the race's number on a medallion, its name, the
    /// whole track from above, difficulty, laps or sections, the star times (earned ones lit), your records, what
    /// waits on the track, and START. Off to the side of the deck it is frosted over and its picture blurred
    /// (<see cref="SetFocus"/>). A race still locked shows a lock and the stars it needs.
    /// </summary>
    public class RaceCard : MonoBehaviour
    {
        [Serializable]
        public struct FeatureIcon
        {
            public string key;
            public Sprite sprite;
            [Tooltip("Tint of the picture (white pictures need a dark one). Clear = no tint.")]
            public Color tint;
        }

        [SerializeField] CanvasGroup group;
        [Tooltip("Tapping a card at the side of the deck brings it to the middle.")]
        [SerializeField] UnityEngine.UI.Button focusButton;
        [SerializeField] UnityEngine.UI.Image veil;
        [SerializeField] RectTransform glow;
        [SerializeField] TMP_Text numberLabel, chapterLabel, titleLabel, recordsLabel, lapsLabel;
        [SerializeField] UnityEngine.UI.Image picture;
        [SerializeField] UnityEngine.UI.RawImage blurredPicture;
        [Tooltip("A band of light that sweeps across the picture now and then (on the card in the middle).")]
        [SerializeField] RectTransform shine;
        [SerializeField] GameObject lockCover;
        [SerializeField] TMP_Text lockLabel;
        [SerializeField] GameObject wonBadge;
        [SerializeField] UnityEngine.UI.Image[] difficultyPips;
        [Tooltip("The three star-time groups: their star icons light up when your best time beats the time.")]
        [SerializeField] RectTransform[] starGroups;
        [SerializeField] TMP_Text[] starTimes;
        [SerializeField] RectTransform featureRow;
        [SerializeField] RectTransform featureTemplate;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFace, startShadow;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] RectTransform startRing;

        readonly List<GameObject> _badges = new();
        float _focusedAt = -10f, _pressedAt = -10f;
        bool _focused, _canEnter;

        public int Index { get; private set; }
        public UnityEngine.UI.Button FocusButton => focusButton;
        public UnityEngine.UI.Button StartButton => startButton;

        public void Bind(LevelDefinition level, int index, bool canEnter, CupDefinition cup, FeatureIcon[] icons)
        {
            Index = index;
            _canEnter = canEnter;
            name = $"Card{index + 1}";
            numberLabel.text = (index + 1).ToString();
            chapterLabel.text = cup != null ? $"{cup.displayName} · RACE {index + 1}" : level.chapter;
            titleLabel.text = level.displayName.ToUpperInvariant();
            lapsLabel.text = level.LapsText.ToUpperInvariant();

            var best = ProgressStore.BestTimeOf(level);
            var bestLap = level.track != null ? BestLapStore.Get(level.track.name) : null;
            string lapWord = level.Sectioned ? "BEST SECTION" : "BEST LAP";
            recordsLabel.text = best.HasValue
                ? $"BEST  <color={Theme.Html(Theme.Ink)}>{TimeFormat.Race(best.Value)}</color>" + (bestLap.HasValue ? $"     {lapWord}  <color={Theme.Html(Theme.Ink)}>{TimeFormat.Race(bestLap.Value)}</color>" : "")
                : "NO TIME YET";

            picture.sprite = level.preview;
            picture.enabled = level.preview != null;
            if (blurredPicture != null)
            {
                blurredPicture.texture = level.preview != null ? UiBlur.Blurred(level.preview.texture) : null;
                blurredPicture.enabled = blurredPicture.texture != null;
            }
            for (int i = 0; i < difficultyPips.Length; i++)
                difficultyPips[i].color = i < level.difficulty ? Theme.Orange : Theme.WithAlpha(Theme.StoneDark, 0.3f);

            for (int i = 0; i < starTimes.Length; i++)
            {
                bool has = i < level.starTimes.Length;
                starTimes[i].text = has ? TimeFormat.Race(level.starTimes[i]) : "-";
                bool earned = best.HasValue && has && best.Value <= level.starTimes[i];
                starTimes[i].color = earned ? Theme.White : Theme.WithAlpha(Theme.Stone, 0.6f);
                foreach (var icon in Stars(i)) icon.color = earned ? Theme.Amber : Theme.WithAlpha(Theme.Stone, 0.35f);
            }
            if (wonBadge != null) wonBadge.SetActive(ProgressStore.HasWon(level));
            ShowFeatures(level.track, icons);

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
                ? starGroups[group].GetComponentsInChildren<UnityEngine.UI.Image>(true)
                : Array.Empty<UnityEngine.UI.Image>();

        /// <summary>
        /// How far from the middle of the deck the card is (0 in the middle, 1 one place off): the frost, the blurred
        /// picture, and whether its buttons work.
        /// </summary>
        public void SetFocus(float away)
        {
            float a = Mathf.Clamp01(Mathf.Abs(away));
            if (veil != null) veil.color = Theme.WithAlpha(Theme.Cream, 0.62f * Mathf.SmoothStep(0f, 1f, a));
            if (blurredPicture != null && blurredPicture.enabled) blurredPicture.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.6f));
            bool focused = a < 0.08f;
            startButton.interactable = focused && _canEnter;
            focusButton.interactable = !focused;
            if (focused && !_focused) _focusedAt = Time.unscaledTime;
            _focused = focused;
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
            // Brought to the middle: the earned stars pop in one after another.
            for (int i = 0; starGroups != null && i < starGroups.Length; i++)
            {
                if (starGroups[i] == null) continue;
                float k = Mathf.Clamp01((t - _focusedAt - 0.08f - i * 0.09f) / 0.3f);
                starGroups[i].localScale = Vector3.one * (_focused ? Pop(k) : 1f);
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

        static float Pop(float t)
        {
            const float s = 2.2f;
            if (t <= 0f) return 0.6f;
            float u = Mathf.Clamp01(t) - 1f;
            return Mathf.LerpUnclamped(0.6f, 1f, 1f + u * u * ((s + 1f) * u + s));
        }

        void ShowFeatures(TrackLayout track, FeatureIcon[] icons)
        {
            foreach (var badge in _badges) Destroy(badge);
            _badges.Clear();
            if (track == null || featureRow == null || featureTemplate == null || icons == null) return;
            featureTemplate.gameObject.SetActive(false);
            foreach (var key in Features(track))
            {
                var entry = Array.Find(icons, i => i.key == key);
                if (entry.sprite == null) continue;
                var badge = Instantiate(featureTemplate, featureRow);
                badge.gameObject.SetActive(true);
                badge.name = key;
                var icon = badge.Find("Mask/Icon")?.GetComponent<UnityEngine.UI.Image>();
                if (icon != null)
                {
                    icon.sprite = entry.sprite;
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
            // The special attractions first.
            Add(track.dusk, track.biome == TrackLayout.Biome.Snow ? "aurora" : "lantern"); // winter nights have the northern lights, no lanterns
            Add(track.gaps.Exists(g => g.IsDrawbridge), "drawbridge");
            Add(track.rings is { Count: > 0 }, "ring");
            Add(track.waterfalls is { Count: > 0 }, "waterfall");
            Add(track.coasters is { Count: > 0 }, "coaster");
            Add(Has(TrackLayout.BranchKind.Roof), "roof");
            Add(track.banks.Count > 0, "wall");
            Add(Has(TrackLayout.BranchKind.Ice), "ice");
            Add(track.rivers.Count > 0 || track.gaps.Count > 0, "ramp");
            // Pads as the track draws them: gummi boats in the water, bouncy mushrooms on meadow ground.
            bool orange = track.padLook == TrackLayout.PadLook.Orange;
            Add(track.bouncers.Exists(b => Wet(track, b.position)), track.biome == TrackLayout.Biome.Snow ? "floe" : "gummiboat");
            Add(track.bouncers.Exists(b => !Wet(track, b.position)),
                orange ? "pad" : track.padLook == TrackLayout.PadLook.Trampoline ? "trampoline" : track.biome == TrackLayout.Biome.Meadow ? "mushroom" : "pad");
            Add(track.dashPanels.Count > 0, "dash");
            Add(track.penguins.Exists(p => p.kind == TrackLayout.CrossingKind.Reindeer), "reindeer");
            Add(track.penguins.Exists(p => p.kind != TrackLayout.CrossingKind.Reindeer), "penguin");
            Add(track.geysers.Count > 0, "fish");
            Add(track.sweepers.Exists(s => s.kind == TrackLayout.SweeperKind.Snowball), "snowball");
            Add(track.sweepers.Exists(s => s.kind == TrackLayout.SweeperKind.Curling), "curling");
            Add(track.sweepers.Exists(s => s.kind == TrackLayout.SweeperKind.Log), "log");
            Add(track.sweepers.Exists(s => s.kind == TrackLayout.SweeperKind.HayBale), "hay");
            Add(track.oil.Count > 0, "oil");
            Add(track.cows.Count > 0, "cow");
            Add(track.logJumps.Count > 0, "forestlog");
            Add(track.shortcuts.Count > 0, "shortcut");
            return keys;
        }

        static bool Wet(TrackLayout track, Vector2 p)
        {
            if (track.waterWorld) return true;
            foreach (var lake in track.lakes)
            {
                Vector2 local = Quaternion.Euler(0f, 0f, -lake.angle) * (p - lake.center);
                if (new Vector2(local.x / lake.radii.x, local.y / lake.radii.y).sqrMagnitude < 1f) return true;
            }
            foreach (var river in track.rivers)
                for (int i = 1; i < river.points.Length; i++)
                {
                    Vector2 a = river.points[i - 1], ab = river.points[i] - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                    if (Vector2.Distance(p, a + ab * t) < river.width * 0.5f) return true;
                }
            return false;
        }
    }
}
