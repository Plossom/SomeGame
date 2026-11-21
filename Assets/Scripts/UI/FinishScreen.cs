using System.Collections;
using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Results screen shown when the player finishes: position, stars, total time, best lap, the
    /// time needed for the next star, Retry, MAP (back to the map with this race's card open) and NEXT
    /// (straight into the cup's next race; after the cup's last race, or one still locked, only MAP).
    /// It celebrates: light rays turning behind the place, a ring bursting out of it, the earned stars
    /// slamming in with sparks, the total time counting up, confetti on a podium (raining on after a win).
    /// </summary>
    public class FinishScreen : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text raceLabel;
        [SerializeField] TMP_Text positionLabel;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] GameObject newBestTag;
        [Tooltip("Three star images, filled left to right.")]
        [SerializeField] UnityEngine.UI.Image[] stars;
        [SerializeField] TMP_Text totalValue;
        [SerializeField] TMP_Text bestLapValue;
        [Tooltip("Your all-time best total time for this race (marked NEW when this run set it).")]
        [SerializeField] TMP_Text bestTimeValue;
        [SerializeField] TMP_Text nextStarLabel;
        [SerializeField] TMP_Text nextStarValue;
        [Tooltip("The big button: NEXT race, or MAP when there is none. Hidden when the race scene was opened directly.")]
        [SerializeField] UnityEngine.UI.Button continueButton;
        [SerializeField] TMP_Text continueLabel;
        [Tooltip("The small MAP button beside Retry, shown when the big one says NEXT.")]
        [SerializeField] UnityEngine.UI.Button mapButton;
        [SerializeField] UnityEngine.UI.Button retryButton;

        [Header("Effects")]
        [Tooltip("Light rays turning behind the place.")]
        [SerializeField] UnityEngine.UI.Image rays;
        [SerializeField] UnityEngine.UI.Image glow;
        [Tooltip("Where the bursts, sparks and confetti fly (over everything, no raycasts).")]
        [SerializeField] RectTransform fxLayer;
        [SerializeField] Sprite confettiSprite;
        [SerializeField] Sprite sparkSprite;
        [SerializeField] Sprite ringSprite;

        static readonly Color Silver = new(0.55f, 0.62f, 0.7f), Bronze = new(0.78f, 0.47f, 0.24f);

        bool _newBest;
        LevelDefinition _next;
        bool _won, _podium;
        float _shownAt;
        Theme.Palette _palette;

        // Sparks, rings and confetti: each a picture flying, spinning and fading.
        class Bit
        {
            public RectTransform rt;
            public UnityEngine.UI.Image image;
            public Vector2 velocity;
            public float spin, age, life, gravity, drag, grow, flutter, size;
            public bool ring, confetti;
        }
        readonly List<Bit> _bits = new();
        readonly Stack<Bit> _spare = new();
        float _confettiUntil, _confettiRate, _confettiDebt;

        void Awake()
        {
            panel.SetActive(false);
            retryButton.onClick.AddListener(race.Restart);
            continueButton.onClick.AddListener(() =>
            {
                if (_next != null) GameSession.StartRace(_next);
                else GameSession.ReturnToMapCards();
            });
            if (mapButton != null) mapButton.onClick.AddListener(GameSession.ReturnToMapCards);
        }

        /// <summary>The cup's race after <paramref name="level"/>, if it is open to you; null after the cup's last race.</summary>
        static LevelDefinition NextOf(LevelDefinition level)
        {
            var list = CupList.Instance;
            if (list == null || level == null) return null;
            foreach (var cup in list.cups)
            {
                if (cup == null || !cup.HasRaces) continue;
                int i = cup.races.IndexOf(level);
                if (i < 0) continue;
                return i + 1 < cup.races.Count && ProgressStore.CanEnter(cup.races, i + 1) ? cup.races[i + 1] : null;
            }
            return null;
        }

        void OnEnable()
        {
            race.PlayerFinished += Show;
            race.PlayerLapCompleted += OnLap;
        }

        void OnDisable()
        {
            race.PlayerFinished -= Show;
            race.PlayerLapCompleted -= OnLap;
        }

        // The jingle; a ring bursts out of the place; each earned star slams in with a ding and sparks (as it
        // pops in); the total time counts up.
        IEnumerator Celebrate(bool won, int earnedStars)
        {
            var library = SomeGame.Audio.AudioLibrary.Instance;
            if (library != null) SomeGame.Audio.GameAudio.Play(won ? library.win : library.lose, 0.8f);
            StartCoroutine(CountUp(totalValue, race.RaceTime, 0.25f, 0.8f));
            yield return new WaitForSecondsRealtime(0.12f);
            Vector2 place = Local(positionLabel.rectTransform);
            Ring(place, _podium ? _palette.Amber : Theme.White, 900f, 0.6f);
            if (_podium) Sparks(place, won ? 22 : 12, won ? 900f : 650f);
            for (int i = 0; i < earnedStars && i < stars.Length; i++)
            {
                yield return new WaitForSecondsRealtime(i == 0 ? 0.23f : 0.18f);
                if (library != null) SomeGame.Audio.GameAudio.Play(library.star, 0.7f);
                var star = (RectTransform)stars[i].transform.parent;
                StartCoroutine(Punch(star));
                Vector2 at = Local(star);
                Ring(at, _palette.Amber, 420f, 0.45f);
                Sparks(at, 10, 520f);
            }
        }

        // The time rolls up from zero, easing out.
        IEnumerator CountUp(TMP_Text label, float value, float delay, float duration)
        {
            label.text = TimeFormat.Race(0f);
            yield return new WaitForSecondsRealtime(delay);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                label.text = TimeFormat.Race(value * (1f - (1f - t) * (1f - t) * (1f - t)));
                yield return null;
            }
            label.text = TimeFormat.Race(value);
        }

        // A star lands: big, then settles with a little bounce.
        IEnumerator Punch(RectTransform rt)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                rt.localScale = Vector3.one * (1f + 0.45f * Mathf.Exp(-6f * t) * Mathf.Cos(t * 11f));
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime - _shownAt;
            // The rays turn slowly and breathe; the glow pulses.
            if (rays != null)
            {
                rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -now * 14f);
                float k = Mathf.Clamp01(now / 0.6f);
                rays.rectTransform.localScale = Vector3.one * (0.6f + 0.4f * (1f - (1f - k) * (1f - k)) + 0.03f * Mathf.Sin(now * 2f));
                var c = rays.color; c.a = (_podium ? 0.3f : 0.08f) * k; rays.color = c;
            }
            if (glow != null)
            {
                var c = glow.color; c.a = (_podium ? 0.55f : 0.3f) * Mathf.Clamp01(now / 0.5f) * (0.85f + 0.15f * Mathf.Sin(now * 3f)); glow.color = c;
            }
            // Confetti keeps coming while it lasts (for ever after a win); a sparkle now and then round the place.
            if (Time.unscaledTime < _confettiUntil)
            {
                _confettiDebt += _confettiRate * dt;
                while (_confettiDebt >= 1f) { _confettiDebt -= 1f; Confetti(); }
            }
            if (_won && now > 1f && Random.value < dt * 3f)
            {
                var r = positionLabel.rectTransform.rect;
                Vector2 at = Local(positionLabel.rectTransform) + new Vector2(Random.Range(-0.4f, 0.4f) * r.width, Random.Range(-0.35f, 0.35f) * r.height);
                var b = Spawn(sparkSprite, Random.Range(50f, 90f), Random.value < 0.5f ? Color.white : _palette.Amber, at);
                b.life = 0.7f; b.grow = 1f; b.drag = 0f; b.gravity = 0f; b.spin = 120f;
            }
            for (int i = _bits.Count - 1; i >= 0; i--)
            {
                var b = _bits[i];
                b.age += dt;
                float k = b.age / b.life;
                if (k >= 1f)
                {
                    b.rt.gameObject.SetActive(false);
                    _spare.Push(b);
                    _bits.RemoveAt(i);
                    continue;
                }
                b.velocity *= Mathf.Exp(-b.drag * dt);
                b.velocity.y -= b.gravity * dt;
                b.rt.anchoredPosition += b.velocity * dt;
                b.rt.localRotation = Quaternion.Euler(0f, 0f, b.rt.localEulerAngles.z + b.spin * dt);
                var col = b.image.color;
                if (b.ring)
                {
                    b.rt.sizeDelta = Vector2.one * Mathf.Lerp(b.size * 0.2f, b.size, 1f - (1f - k) * (1f - k));
                    col.a = 0.9f * (1f - k);
                }
                else if (b.confetti)
                {
                    // Tumbling: it turns its edge to you and back.
                    b.rt.localScale = new Vector3(Mathf.Abs(Mathf.Cos(b.age * b.flutter)) * 0.85f + 0.15f, 1f, 1f);
                    b.velocity.x += Mathf.Sin(b.age * 2.3f + b.flutter) * 60f * dt;
                    col.a = Mathf.Clamp01((1f - k) * 5f);
                }
                else
                {
                    float s = b.grow > 0f ? Mathf.Sin(k * Mathf.PI) : 1f - k * 0.6f;
                    b.rt.localScale = Vector3.one * s;
                    col.a = b.grow > 0f ? s : 1f - k * k;
                }
                b.image.color = col;
            }
        }

        Vector2 Local(RectTransform rt) => fxLayer.InverseTransformPoint(rt.TransformPoint(rt.rect.center));

        Bit Spawn(Sprite sprite, float size, Color color, Vector2 at)
        {
            Bit b;
            if (_spare.Count > 0) b = _spare.Pop();
            else
            {
                var go = new GameObject("Fx", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                b = new Bit { rt = (RectTransform)go.transform, image = go.GetComponent<UnityEngine.UI.Image>() };
                b.rt.SetParent(fxLayer, false);
                b.image.raycastTarget = false;
            }
            b.rt.gameObject.SetActive(true);
            b.rt.SetAsLastSibling();
            b.image.sprite = sprite;
            b.image.color = color;
            b.image.preserveAspect = true;
            b.rt.sizeDelta = Vector2.one * size;
            b.rt.anchoredPosition = at;
            b.rt.localScale = Vector3.one;
            b.rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            b.size = size; b.age = 0f; b.ring = b.confetti = false; b.grow = 0f; b.spin = 0f; b.drag = 0f; b.gravity = 0f; b.velocity = Vector2.zero;
            _bits.Add(b);
            return b;
        }

        void Ring(Vector2 at, Color color, float size, float life)
        {
            if (ringSprite == null) return;
            var b = Spawn(ringSprite, size, color, at);
            b.ring = true; b.life = life;
        }

        void Sparks(Vector2 at, int count, float speed)
        {
            if (sparkSprite == null) return;
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
                var b = Spawn(sparkSprite, Random.Range(40f, 80f), i % 3 == 0 ? Color.white : _palette.Amber, at);
                b.velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.55f, 1f);
                b.drag = 3.5f; b.gravity = 300f; b.spin = Random.Range(-300f, 300f); b.life = Random.Range(0.6f, 0.9f);
            }
        }

        static readonly Color[] ConfettiColors =
        {
            new(1f, 0.48f, 0.1f), new(1f, 0.83f, 0.28f), new(0.17f, 0.58f, 0.91f), new(0.49f, 0.88f, 0.48f), new(0.9f, 0.3f, 0.6f), Color.white,
        };

        // A scrap of confetti dropping in from above the screen.
        void Confetti()
        {
            if (confettiSprite == null) return;
            var r = fxLayer.rect;
            var b = Spawn(confettiSprite, Random.Range(34f, 56f), ConfettiColors[Random.Range(0, ConfettiColors.Length)],
                new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + 60f));
            b.image.preserveAspect = false;
            b.rt.sizeDelta = new Vector2(b.size * 0.5f, b.size);
            b.confetti = true;
            b.velocity = new Vector2(Random.Range(-80f, 80f), -Random.Range(260f, 420f));
            b.spin = Random.Range(-200f, 200f); b.flutter = Random.Range(3f, 7f);
            b.life = (r.height + 160f) / 300f;
        }

        void OnLap(float lapTime, bool newBest) => _newBest |= newBest;

        void Show()
        {
            if (GameSession.Online) return; // online races show the room's results (OnlineResults)
            var level = race.Level;
            int position = race.FinalPosition;
            bool won = position == 1;
            _won = won;
            _podium = position <= 3;
            _palette = Theme.OfLevel(level);
            _shownAt = Time.unscaledTime;
            // In the colours of the race's cup (built in the Valley colours).
            var palette = Theme.OfLevel(level);
            Theme.RecolorAll(transform, Theme.Valley, palette);
            Theme.Hills(transform, palette);
            // A race in sections has no best lap (every section is different): the next-star row moves up into its place.
            var lapRow = (RectTransform)bestLapValue.transform.parent;
            var nextRow = (RectTransform)nextStarValue.transform.parent;
            if (race.Sectioned && lapRow.gameObject.activeSelf)
            {
                lapRow.gameObject.SetActive(false);
                nextRow.anchoredPosition = lapRow.anchoredPosition;
            }

            raceLabel.text = level != null ? level.chapter : "TEST RACE";
            positionLabel.text = TimeFormat.Ordinal(position).ToUpperInvariant();
            positionLabel.color = won ? palette.Orange : position == 2 ? Silver : position == 3 ? Bronze : palette.Ink;
            subtitle.text = won ? "PLACE · WINNER" : "PLACE";
            newBestTag.SetActive(false); // the best-lap row says NEW BEST instead

            int earned = race.StarsEarned;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(level != null);
                stars[i].color = i < earned ? palette.Amber : palette.CreamDark;
            }

            totalValue.text = TimeFormat.Race(race.RaceTime);
            float bestLap = float.MaxValue;
            foreach (var lap in race.Player.LapTimes) bestLap = Mathf.Min(bestLap, lap);
            bestLapValue.text = bestLap >= float.MaxValue ? "-"
                : _newBest ? $"<size=60%>NEW BEST  </size>{TimeFormat.Race(bestLap)}"
                : TimeFormat.Race(bestLap);
            bestLapValue.color = _newBest ? palette.Amber : Theme.White;
            if (bestTimeValue != null)
            {
                var best = level != null ? ProgressStore.BestTimeOf(level) : null;
                bool isNew = race.IsNewBestTime;
                bestTimeValue.text = !best.HasValue ? "-"
                    : isNew ? $"<size=60%>NEW BEST  </size>{TimeFormat.Race(best.Value)}"
                    : TimeFormat.Race(best.Value);
                bestTimeValue.color = isNew ? palette.Amber : Theme.White;
            }

            if (level == null)
            {
                nextStarLabel.text = "No stars in test races";
                nextStarValue.text = "";
            }
            else if (earned >= 3)
            {
                nextStarLabel.text = "All stars";
                nextStarValue.text = "3/3";
            }
            else
            {
                // What the next star takes: a top-five, a podium, a win.
                nextStarLabel.text = earned == 0 ? "1 star for" : $"{earned + 1} stars for";
                nextStarValue.text = earned == 0 ? "TOP 5" : earned == 1 ? "TOP 3" : "A WIN";
            }

            // NEXT into the cup's next race (MAP beside Retry), or only MAP after its last.
            _next = NextOf(level);
            continueButton.gameObject.SetActive(level != null);
            if (continueLabel != null) continueLabel.text = _next != null ? "NEXT" : "MAP";
            if (mapButton != null) mapButton.gameObject.SetActive(level != null && _next != null);

            if (rays != null) rays.color = Theme.WithAlpha(_podium ? palette.Orange : palette.Ink, 0f);
            if (glow != null) glow.color = Theme.WithAlpha(_podium ? palette.Amber : Theme.White, 0f);
            _confettiRate = won ? 28f : _podium ? 20f : 0f;
            _confettiUntil = won ? float.MaxValue : _podium ? Time.unscaledTime + 2.5f : 0f;
            _confettiDebt = 0f;
            panel.SetActive(true);
            // A first handful already falling.
            if (_podium && confettiSprite != null) for (int i = 0; i < (won ? 40 : 20); i++) { Confetti(); _bits[^1].rt.anchoredPosition -= new Vector2(0f, Random.Range(0f, fxLayer.rect.height * 0.6f)); }
            StartCoroutine(Celebrate(won, level != null ? earned : 0));
        }
    }
}
