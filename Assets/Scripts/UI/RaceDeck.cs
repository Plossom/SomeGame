using System;
using System.Collections.Generic;
using SomeGame.Race;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SomeGame.UI
{
    /// <summary>
    /// The races of a cup as a deck of cards over the map. It opens out of the race's stop once the car has driven
    /// there: the card grows out of the stop with a little overshoot and the races before and after slide in at the
    /// sides, frosted over. Swipe sideways and the cards follow the finger, leaning into the swipe; let go and they
    /// glide on and settle on a race with a spring (a tick for every card that passes), and the car drives there on
    /// the map behind. Tap a number in the bar of races under the deck to fly straight to that race. Swipe the deck down, tap beside it or the cross on the card, and the card shrinks back
    /// into its stop. START launches the race: sparks and a ring of light burst from the button, the other cards fly
    /// off, and the card is drawn - it lifts, then sweeps up towards you filling the screen as the backdrop turns to
    /// the loading screen's cream, which then takes over. Lives on the deck's root (always active), so drags and taps anywhere in the deck reach it.
    /// </summary>
    public class RaceDeck : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public struct Entry
        {
            public LevelDefinition level;
            public bool canEnter;
        }

        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Image backdrop;
        [SerializeField] RaceCard cardTemplate;
        [Tooltip("The bar of race numbers: its plate, a badge to copy and the ring on the current race.")]
        [SerializeField] RectTransform strip;
        [SerializeField] RectTransform stripBadge;
        [SerializeField] RectTransform stripRing;
        [Tooltip("Pictures for the launch: the sparks and the ring of light bursting out of START.")]
        [SerializeField] Sprite sparkle, flashRing;
        [Tooltip("The cup's challenge, a card tucked behind its rival's race card (the two swap when tapped).")]
        [SerializeField] DeckChallengeCard challengeCard;

        const float Spacing = 720f;          // between the middle of two cards next to each other
        const float Raise = 90f;             // the cards sit this much above the middle (the bar of race numbers stays)
        const float SideScale = 0.86f, SideTilt = 5f, SideDrop = 46f;
        const float OpenSeconds = 0.6f, CloseSeconds = 0.32f;
        const float Omega = 12f;             // the settling spring (critically damped)
        const float BadgeSpacing = 134f, StripHeight = 172f;
        const float CloseDrag = 260f;        // dragged this far down, the deck closes
        const float LaunchSeconds = 0.95f;   // START: from the press until the loading screen takes over

        enum Phase { Closed, Opening, Open, Closing, Launch }

        readonly List<RaceCard> _cards = new();
        readonly List<RectTransform> _badges = new();
        readonly List<RaceCard> _order = new();
        Phase _phase;
        float _phaseAt;
        float _pos, _vel;
        int _target, _settled, _tick;
        bool _dragging, _vertical, _dragActive;
        float _dragFrom, _dx, _dy, _dyVelocity;
        float _ringPopAt = -10f;
        Vector2 _from;
        Func<int, RectTransform> _stopOf;
        Action<int> _onSelect, _onStart;
        CupDefinition _cup;
        Canvas _canvas;
        bool _launched;
        readonly List<(RectTransform rt, UnityEngine.UI.Image image, Vector2 from, Vector2 velocity, float spin)> _sparks = new();
        UnityEngine.UI.Image _flash;
        int _challengeAt = -1;          // the race whose card the challenge lies behind
        float _swap, _swapTarget;       // 0: the race card in front, 1: the challenge card in front
        float _fan;                     // 0: the card behind hidden right behind, 1: fanned out to the side
        float _fannedAt = -1f;          // when the fan last opened (the teasing nudges count from there)
        int _teased = -1;
        const float TeaseEvery = 4f, TeaseSeconds = 0.75f, TeaseLift = 0.16f;
        const float SwapSeconds = 0.45f, FanTilt = -9f, FanShift = 80f;

        public bool IsOpen => _phase != Phase.Closed;

        void Awake()
        {
            cardTemplate.gameObject.SetActive(false);
            stripBadge.gameObject.SetActive(false);
            if (challengeCard != null)
            {
                challengeCard.Body.onClick.AddListener(() => Swap(1f));
                challengeCard.gameObject.SetActive(false);
            }
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            panel.SetActive(false);
        }

        /// <summary>Opens the deck on race <paramref name="selected"/>, its card growing out of that race's stop.</summary>
        /// <param name="onSelect">A race was settled on (the map drives there).</param>
        /// <param name="onStart">START on a race's card.</param>
        public void Open(IReadOnlyList<Entry> races, int selected, CupDefinition cup, Func<int, RectTransform> stopOf, Action<int> onSelect, Action<int> onStart)
        {
            if (races.Count == 0) return;
            _stopOf = stopOf;
            _onSelect = onSelect;
            _onStart = onStart;
            _cup = cup;
            panel.SetActive(true);
            Build(races);
            // The cup's challenge behind its rival's race.
            _challengeAt = -1;
            _swap = _swapTarget = _fan = 0f;
            if (challengeCard != null && cup != null && cup.challenge != null)
            {
                var rival = string.IsNullOrEmpty(cup.challengeAfterRival) ? null : Resources.Load<RivalCharacter>("Rivals/" + cup.challengeAfterRival);
                for (int i = 0; i < races.Count; i++)
                    if (rival != null && races[i].level == rival.homeRace && races[i].canEnter) _challengeAt = i;
                if (_challengeAt >= 0) challengeCard.Bind(cup);
            }
            if (challengeCard != null) challengeCard.gameObject.SetActive(_challengeAt >= 0);
            _pos = _target = _settled = _tick = Mathf.Clamp(selected, 0, races.Count - 1);
            _vel = _dx = _dy = 0f;
            _dragging = _vertical = false;
            _from = StopPoint(_target);
            _phase = Phase.Opening;
            _phaseAt = Time.unscaledTime;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.45f);
            Layout();
        }

        /// <summary>Closes the deck: the card shrinks back into the stop of the race it is on.</summary>
        public void Close()
        {
            if (_phase != Phase.Open && _phase != Phase.Opening) return;
            _phase = Phase.Closing;
            _phaseAt = Time.unscaledTime;
            _from = StopPoint(_target);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
        }

        Vector2 StopPoint(int index)
        {
            var stop = _stopOf?.Invoke(index);
            var stage = (RectTransform)cardTemplate.transform.parent;
            return stop != null ? (Vector2)stage.InverseTransformPoint(stop.position) : new Vector2(0f, -stage.rect.height * 0.3f);
        }

        void Build(IReadOnlyList<Entry> races)
        {
            foreach (var card in _cards) Destroy(card.gameObject);
            _cards.Clear();
            foreach (var badge in _badges) Destroy(badge.gameObject);
            _badges.Clear();
            for (int i = 0; i < races.Count; i++)
            {
                var card = Instantiate(cardTemplate, cardTemplate.transform.parent);
                card.gameObject.SetActive(true);
                card.Bind(races[i].level, i, races[i].canEnter, _cup);
                card.CloseButton.onClick.AddListener(Close);
                int index = i;
                card.FocusButton.onClick.AddListener(() => { if (index == _challengeAt && _swapTarget > 0f) Swap(0f); else FlyTo(index); });
                card.StartButton.onClick.AddListener(() => StartRace(card));
                _cards.Add(card);

                var b = Instantiate(stripBadge, stripBadge.parent);
                b.gameObject.SetActive(true);
                b.name = $"Race{i + 1}";
                bool won = ProgressStore.HasWon(races[i].level);
                b.Find("Face").GetComponent<UnityEngine.UI.Image>().color = !races[i].canEnter ? Theme.Stone : won ? Theme.Amber : Theme.Cream;
                var number = b.Find("Number").GetComponent<TMPro.TMP_Text>();
                number.text = (i + 1).ToString();
                number.gameObject.SetActive(races[i].canEnter);
                var lockIcon = b.Find("Lock");
                lockIcon.gameObject.SetActive(!races[i].canEnter);
                b.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => FlyTo(index));
                _badges.Add(b);
            }
            stripRing.SetAsLastSibling();
        }

        // Brings the challenge card (1) or its race's card (0) to the front.
        void Swap(float front)
        {
            if (_phase != Phase.Open || _dragging || _target != _challengeAt || Mathf.Abs(_pos - _target) > 0.05f) return;
            if (Mathf.Approximately(_swapTarget, front)) return;
            _swapTarget = front;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.4f);
        }

        void FlyTo(int index)
        {
            if (_phase != Phase.Open || _dragging) return;
            _target = Mathf.Clamp(index, 0, _cards.Count - 1);
        }

        void StartRace(RaceCard card)
        {
            if (_phase != Phase.Open || card.Index != _target || Mathf.Abs(_pos - _target) > 0.05f) return;
            card.Pressed();
            _phase = Phase.Launch;
            _phaseAt = Time.unscaledTime;
            _launched = false;
            _vel = 0f;
            Burst(card);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.6f);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.boost, 0.7f);
        }

        // Sparks and a ring of light bursting out of START.
        void Burst(RaceCard card)
        {
            var stage = (RectTransform)cardTemplate.transform.parent;
            Vector2 at = stage.InverseTransformPoint(card.StartButton.transform.position);
            foreach (var spark in _sparks) if (spark.rt != null) Destroy(spark.rt.gameObject);
            _sparks.Clear();
            UnityEngine.UI.Image Make(string name, Sprite sprite, float size, Color color)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(stage, false);
                rt.sizeDelta = Vector2.one * size;
                rt.anchoredPosition = at;
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = sprite;
                image.color = color;
                image.raycastTarget = false;
                return image;
            }
            if (_flash == null && flashRing != null) _flash = Make("LaunchRing", flashRing, 260f, Theme.Amber);
            if (_flash != null) { _flash.rectTransform.anchoredPosition = at; _flash.rectTransform.SetAsLastSibling(); }
            if (sparkle == null) return;
            for (int i = 0; i < 16; i++)
            {
                float angle = (i / 16f + UnityEngine.Random.Range(-0.02f, 0.02f)) * Mathf.PI * 2f;
                var image = Make("Spark", sparkle, UnityEngine.Random.Range(44f, 84f), i % 3 == 0 ? Color.white : Theme.Amber);
                var v = new Vector2(Mathf.Cos(angle) * 1.5f, Mathf.Sin(angle)) * UnityEngine.Random.Range(260f, 460f);
                _sparks.Add((image.rectTransform, image, at, v, UnityEngine.Random.Range(-360f, 360f)));
            }
        }

        // ------------------------------------------------------------------ touch

        public void OnBeginDrag(PointerEventData e)
        {
            _dragActive = _phase == Phase.Open;
            if (!_dragActive) return;
            _vertical = Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x) * 1.2f && e.delta.y < 0f;
            _dragging = !_vertical;
            _dragFrom = _pos;
            _dx = 0f;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragActive) return;
            Vector2 step = e.delta / (_canvas != null ? _canvas.scaleFactor : 1f);
            float dt = Mathf.Max(1e-4f, Time.unscaledDeltaTime);
            if (_vertical)
            {
                float dy = Mathf.Min(0f, _dy + step.y);
                _dyVelocity = Mathf.Lerp(_dyVelocity, (dy - _dy) / dt, 0.5f);
                _dy = dy;
                return;
            }
            _dx += step.x;
            float raw = _dragFrom - _dx / Spacing, last = _cards.Count - 1;
            // Past the first or the last race the deck only gives a little.
            if (raw < 0f) raw = -Rubber(-raw);
            else if (raw > last) raw = last + Rubber(raw - last);
            _vel = Mathf.Lerp(_vel, (raw - _pos) / dt, 0.5f);
            _pos = raw;
        }

        static float Rubber(float x) => 0.3f * (1f - Mathf.Exp(-x / 0.3f));

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragActive) return;
            _dragActive = false;
            if (_vertical)
            {
                _vertical = false;
                if (_dy < -CloseDrag || _dyVelocity < -1800f) Close();
                return;
            }
            _dragging = false;
            // Where the glide would take it, at the speed it was let go.
            float projected = _pos + Mathf.Clamp(_vel, -12f, 12f) * 0.18f;
            _target = Mathf.Clamp(Mathf.RoundToInt(projected), 0, _cards.Count - 1);
        }

        // A tap beside the cards closes the deck (taps on the cards go to them).
        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging && _phase == Phase.Open) Close();
        }

        // ------------------------------------------------------------------ motion

        void Update()
        {
            if (_phase == Phase.Closed) return;
            float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_phase == Phase.Opening && now - _phaseAt >= OpenSeconds) _phase = Phase.Open;
            if (_phase == Phase.Launch)
            {
                // The card is drawn and fills the screen: now the loading screen takes over.
                if (!_launched && now - _phaseAt >= LaunchSeconds)
                {
                    _launched = true;
                    _onStart?.Invoke(_target);
                }
                LayoutLaunch(Mathf.Clamp01((now - _phaseAt) / LaunchSeconds), now - _phaseAt);
                return;
            }
            if (_phase == Phase.Closing && now - _phaseAt >= CloseSeconds)
            {
                _phase = Phase.Closed;
                panel.SetActive(false);
                return;
            }
            if (!_dragging)
            {
                float a = -Omega * Omega * (_pos - _target) - 2f * Omega * _vel;
                _vel += a * dt;
                _pos += _vel * dt;
                if (Mathf.Abs(_pos - _target) < 0.0015f && Mathf.Abs(_vel) < 0.01f) { _pos = _target; _vel = 0f; }
            }
            if (!_vertical) _dy = Mathf.Lerp(_dy, 0f, 1f - Mathf.Exp(-14f * dt));
            // Off to another race: the challenge tucks itself back behind; resting on its race the two fan out.
            if (_challengeAt >= 0 && (_target != _challengeAt || _dragging)) _swapTarget = 0f;
            _swap = Mathf.MoveTowards(_swap, _swapTarget, dt / SwapSeconds);
            bool fanned = _challengeAt >= 0 && _phase == Phase.Open && !_dragging && _target == _challengeAt && Mathf.Abs(_pos - _target) < 0.05f;
            _fan = Mathf.MoveTowards(_fan, fanned ? 1f : 0f, dt / 0.3f);
            if (_fan >= 1f && _fannedAt < 0f) { _fannedAt = now; _teased = -1; }
            else if (_fan < 1f) _fannedAt = -1f;
            // A tick for every card that passes the middle.
            int near = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, _cards.Count - 1);
            if (near != _tick)
            {
                _tick = near;
                _ringPopAt = now;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.35f);
            }
            // Settled on another race: the map follows.
            if (!_dragging && _pos == _target && _target != _settled)
            {
                _settled = _target;
                _onSelect?.Invoke(_target);
            }
            Layout();
        }

        static float Ease(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

        static float Back(float t)
        {
            const float s = 1.5f;
            float u = Mathf.Clamp01(t) - 1f;
            return 1f + u * u * ((s + 1f) * u + s);
        }

        void Layout()
        {
            float now = Time.unscaledTime;
            float k = _phase == Phase.Opening ? Mathf.Clamp01((now - _phaseAt) / OpenSeconds)
                : _phase == Phase.Closing ? 1f - Mathf.Clamp01((now - _phaseAt) / CloseSeconds) : 1f;
            bool closing = _phase == Phase.Closing;
            backdrop.color = Theme.WithAlpha(Theme.Ink, 0.72f * Ease(closing ? k : k * 1.6f));

            float lean = Mathf.Clamp(_vel * 4f, -8f, 8f);
            bool settled = _phase == Phase.Open && !_dragging && _pos == _target;
            float bob = settled ? Mathf.Sin(now * 1.7f) * 6f : 0f;
            _order.Clear();
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                float off = i - _pos, a = Mathf.Abs(off);
                bool show = a < 2.6f;
                if (card.gameObject.activeSelf != show) card.gameObject.SetActive(show);
                if (!show) continue;
                _order.Add(card);
                float side = Mathf.Sign(off);
                float x = side * (a <= 1f ? a * Spacing : Spacing + (a - 1f) * Spacing * 0.45f);
                float near = Mathf.Min(a, 1f);
                float scale = Mathf.Lerp(1f, SideScale, near) - Mathf.Max(0f, a - 1f) * 0.08f;
                float rot = -off * SideTilt + lean * (1f - near);
                var at = new Vector2(x, Raise - Mathf.Min(a, 1.5f) * SideDrop + _dy * Mathf.Lerp(1f, 0.6f, near) + bob * (1f - near));
                float alpha = 1f;
                var rt = (RectTransform)card.transform;
                if (i == _target && _phase != Phase.Open)
                {
                    // The card in the middle grows out of the stop (and shrinks back into it).
                    float g = closing ? Ease(k) : Back(k);
                    at = Vector2.LerpUnclamped(_from, at, g);
                    scale *= Mathf.LerpUnclamped(0.08f, 1f, g);
                    rot += (1f - k) * (closing ? 6f : -10f);
                    alpha = Mathf.Clamp01(k * (closing ? 3f : 4f));
                }
                else if (_phase != Phase.Open)
                {
                    // The others slide in from the sides, a moment later (and out again).
                    float s = closing ? k : Ease((k - 0.25f) / 0.75f);
                    at.x += side * (1f - s) * 700f;
                    alpha = s;
                }
                rt.anchoredPosition = at;
                rt.localScale = Vector3.one * scale;
                rt.localRotation = Quaternion.Euler(0f, 0f, rot);
                card.SetAlpha(alpha * Mathf.Clamp01(2.6f - a));
                card.SetFocus(off, settled);
            }
            // The card nearest the middle in front, the others behind it by their distance.
            _order.Sort((p, q) => Mathf.Abs(q.Index - _pos).CompareTo(Mathf.Abs(p.Index - _pos)));
            for (int i = 0; i < _order.Count; i++) _order[i].transform.SetSiblingIndex(cardTemplate.transform.GetSiblingIndex() + 1 + i);
            LayoutChallenge(settled);
            LayoutStrip(k, closing);
        }

        // The challenge card and its race's card, like two cards held in a hand: resting on the race they fan out (the
        // one behind tilted out to the right, turning round its bottom), moving on they close up again. Swapping, the
        // front one swings away and tucks in behind while the other comes forward.
        void LayoutChallenge(bool settled)
        {
            if (challengeCard == null || _challengeAt < 0) return;
            var host = _cards[_challengeAt];
            var hostRt = (RectTransform)host.transform;
            float off = _challengeAt - _pos, a = Mathf.Abs(off);
            float show = host.gameObject.activeSelf ? Mathf.Clamp01(1f - a * 1.6f) : 0f;
            var rt = (RectTransform)challengeCard.transform;
            float t = Mathf.SmoothStep(0f, 1f, _swap), fan = Mathf.SmoothStep(0f, 1f, _fan);
            t = Mathf.Max(t, Tease());
            float swing = Mathf.Sin(t * Mathf.PI);
            float scale = hostRt.localScale.x, h = hostRt.rect.height * scale;
            Vector2 Turn(Vector2 v) => (Vector2)(hostRt.localRotation * v) * scale;
            // Tilting round the bottom of the card: the middle moves sideways (and a touch down).
            Vector2 Behind(float tilt) => new Vector2(-Mathf.Sin(tilt * Mathf.Deg2Rad) * h * 0.5f / scale + FanShift, (Mathf.Cos(tilt * Mathf.Deg2Rad) - 1f) * h * 0.5f / scale) * fan;
            Vector2 at = hostRt.anchoredPosition;
            float rot = hostRt.localEulerAngles.z, tilt = FanTilt * fan;
            // The challenge: from behind (fanned) to the front, swinging out to the right on the way.
            rt.anchoredPosition = at + Turn(Vector2.Lerp(Behind(FanTilt), Vector2.zero, t) + new Vector2(swing * 260f, 0f));
            rt.localRotation = Quaternion.Euler(0f, 0f, rot + Mathf.Lerp(tilt, 0f, t) - swing * 6f);
            rt.localScale = hostRt.localScale * Mathf.Lerp(0.97f, 1f, t);
            // The race card: from the front to behind (fanned), swinging out to the left.
            hostRt.anchoredPosition = at + Turn(Vector2.Lerp(Vector2.zero, Behind(FanTilt), t) - new Vector2(swing * 260f, 0f));
            hostRt.localRotation = Quaternion.Euler(0f, 0f, rot + Mathf.Lerp(0f, tilt, t) + swing * 6f);
            hostRt.localScale *= Mathf.Lerp(1f, 0.97f, t);
            // Whichever is in front draws on top (they change places half way).
            int hostIndex = hostRt.GetSiblingIndex();
            rt.SetSiblingIndex(t < 0.5f ? hostIndex : hostIndex + 1);
            float alpha = show * Mathf.Lerp(Mathf.Max(fan, t), 1f, t);
            if (_phase == Phase.Closing) alpha *= Mathf.Clamp01(1f - (Time.unscaledTime - _phaseAt) / CloseSeconds);
            challengeCard.Show(t, alpha);
            // The race card behind is frosted with START off.
            if (t > 0f) host.SetFocus(Mathf.Max(Mathf.Abs(off), 0.75f * t), settled);
        }

        // Resting on the race with the challenge behind: every few seconds the swap starts on its own for a moment
        // (the challenge lifts out a little and settles back), a hint to tap it.
        float Tease()
        {
            if (_fannedAt < 0f || _swap > 0f || _swapTarget > 0f) return 0f;
            float since = Time.unscaledTime - _fannedAt - 1.2f;
            if (since < 0f) return 0f;
            int round = Mathf.FloorToInt(since / TeaseEvery);
            float p = (since - round * TeaseEvery) / TeaseSeconds;
            if (p >= 1f) return 0f;
            if (round != _teased)
            {
                _teased = round;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.15f);
            }
            return TeaseLift * Mathf.Sin(p * Mathf.PI) * (1f - 0.25f * Mathf.Sin(p * Mathf.PI * 2f));
        }

        // START: the other cards fly off, the card lifts (drawn from the deck), then sweeps up towards you filling the
        // screen while the backdrop turns cream like the loading screen; the sparks fly out and fade.
        void LayoutLaunch(float l, float seconds)
        {
            float cream = Ease((l - 0.45f) / 0.4f);
            backdrop.color = Color.Lerp(Theme.WithAlpha(Theme.Ink, 0.72f), Theme.Cream, cream);
            float lift = Ease((l - 0.18f) / 0.25f), zoom = Mathf.Clamp01((l - 0.45f) / 0.45f), away = Ease((l - 0.06f) / 0.35f);
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (!card.gameObject.activeSelf) continue;
                var rt = (RectTransform)card.transform;
                float off = i - _pos;
                if (i == _target)
                {
                    rt.anchoredPosition = new Vector2(0f, Raise + 50f * lift * (1f - zoom) + 120f * zoom * zoom);
                    rt.localScale = Vector3.one * (1f + 0.06f * lift) * Mathf.Lerp(1f, 2.8f, zoom * zoom);
                    rt.localRotation = Quaternion.Euler(0f, 0f, -3f * lift * (1f - zoom));
                    card.SetAlpha(1f - Mathf.Clamp01((l - 0.7f) / 0.25f));
                }
                else
                {
                    var p = rt.anchoredPosition;
                    rt.anchoredPosition = new Vector2(Mathf.Sign(off) * (Spacing + away * 900f), p.y);
                    card.SetAlpha(1f - away);
                }
            }
            if (challengeCard != null && challengeCard.gameObject.activeSelf) challengeCard.Show(0f, 0f);
            LayoutStrip(1f - Ease((l - 0.05f) / 0.3f), true);
            float burst = Mathf.Clamp01(seconds / 0.6f);
            foreach (var (rt, image, from, velocity, spin) in _sparks)
            {
                if (rt == null) continue;
                rt.anchoredPosition = from + velocity * Ease(burst) + new Vector2(0f, -120f * burst * burst);
                rt.localRotation = Quaternion.Euler(0f, 0f, spin * burst);
                var c = image.color; c.a = 1f - burst; image.color = c;
            }
            if (_flash != null)
            {
                float f = Mathf.Clamp01(seconds / 0.45f);
                _flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 3.4f, Ease(f));
                _flash.color = Theme.WithAlpha(Theme.Amber, 0.9f * (1f - f));
            }
        }

        // The bar of race numbers: a row of numbered badges (it slides up with the deck); the ring glides along
        // with the deck.
        void LayoutStrip(float k, bool closing)
        {
            int n = _badges.Count;
            strip.sizeDelta = new Vector2(n * BadgeSpacing + 34f, StripHeight);
            float slide = closing ? k : Ease((k - 0.35f) / 0.65f);
            strip.anchoredPosition = new Vector2(0f, Mathf.Lerp(-260f, 70f, slide));
            for (int i = 0; i < n; i++)
            {
                var b = _badges[i];
                b.anchoredPosition = new Vector2((i - (n - 1) * 0.5f) * BadgeSpacing, 0f);
                float near = Mathf.Clamp01(1f - Mathf.Abs(_pos - i));
                b.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, near);
            }
            float pop = Mathf.Clamp01((Time.unscaledTime - _ringPopAt) / 0.25f);
            stripRing.anchoredPosition = new Vector2((_pos - (n - 1) * 0.5f) * BadgeSpacing, 0f);
            stripRing.localScale = Vector3.one * (1f + 0.14f * Mathf.Sin(pop * Mathf.PI));
        }
    }
}
