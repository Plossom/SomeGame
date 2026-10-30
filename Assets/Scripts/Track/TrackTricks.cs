using System.Collections.Generic;
using SomeGame.Car;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.Track
{
    /// <summary>
    /// Trick jumps (<see cref="TrackLayout.Gap.trickJump"/>): you only make it across if you pull off the tricks.
    /// Off the lip time slows right down and a row of arrows appears - swipe them, fast and in order (left and right
    /// spin, up a front flip, down a back flip). All of them before the time runs out: PERFECT!, a big boost, and
    /// you land on the far side. One wrong, or too slow: you come down short, into the chasm. Rivals try too
    /// (most of the time they make it).
    /// </summary>
    public class TrackTricks : MonoBehaviour
    {
        [SerializeField] Track track;

        const float SlowScale = 0.4f, RivalSkill = 0.74f;
        const int MaxTricks = 10;

        class Attempt
        {
            public CarMovement Car;
            public int Gap;
            public List<TrickKind> Sequence;
            public int Done;
            public bool Player, Failed, Succeeded, Ended;
            public float NextShow; // rivals: when they do their next trick
            public float Total;    // the flight's length (game seconds), for the bar and a rival's fall
        }

        readonly List<Attempt> _attempts = new();
        readonly Dictionary<CarMovement, System.Action<float>> _launchHandlers = new();
        CarMovement[] _cars;
        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.Input.PlayerCarInput _playerInput;
        Attempt _shown;
        float _scaleBefore = 1f;
        bool _slow;

        // The arrows on screen.
        Canvas _canvas;
        readonly List<Image> _arrows = new();
        Image _bar, _barBack;
        TextMeshProUGUI _caption;
        Sprite _arrowSprite, _square;
        float _shakeAt = -10f;

        void Start()
        {
            if (track == null) track = FindAnyObjectByType<Track>();
            if (track == null || track.Layout == null || !track.Layout.gaps.Exists(g => g.trickJump)) { enabled = false; return; }
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            foreach (var car in _cars)
            {
                var c = car;
                System.Action<float> handler = air => OnLaunched(c);
                c.Launched += handler;
                _launchHandlers[c] = handler;
                var input = c.GetComponent<SomeGame.Input.PlayerCarInput>();
                if (input != null) { _playerInput = input; input.TrickSwiped += OnSwipe; }
            }
            BuildUi();
        }

        void OnDestroy()
        {
            foreach (var kv in _launchHandlers) if (kv.Key != null) kv.Key.Launched -= kv.Value;
            if (_playerInput != null) _playerInput.TrickSwiped -= OnSwipe;
            Restore();
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        // Off the lip of a trick jump: the tricks to do.
        void OnLaunched(CarMovement car)
        {
            if (car.IsCarried || car.IsRiding) return;
            var sensor = car.GetComponent<TrackSensor>();
            if (sensor == null) return;
            var gaps = track.Layout.gaps;
            for (int g = 0; g < gaps.Count; g++)
            {
                var gap = gaps[g];
                if (!gap.trickJump) continue;
                float to = track.Path.DeltaDistance(sensor.Current.Distance, gap.from);
                if (to < -2f || to > 4f) continue;
                _attempts.RemoveAll(a => a.Car == car);
                var attempt = new Attempt { Car = car, Gap = g, Sequence = new List<TrickKind>(), Total = Mathf.Max(0.1f, car.AirTimeLeft),
                    Player = car.GetComponent<SomeGame.Input.PlayerCarInput>() != null };
                int count = Mathf.Clamp(gap.tricks, 1, MaxTricks);
                for (int k = 0; k < count; k++) attempt.Sequence.Add((TrickKind)Random.Range(0, 4));
                if (attempt.Player)
                {
                    _shown = attempt;
                    Slow(true);
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.7f);
                }
                else
                {
                    // A rival's skill decides this one; it shows its tricks on the way over.
                    attempt.Failed = Random.value > RivalSkill;
                    attempt.NextShow = Time.time + 0.15f;
                }
                _attempts.Add(attempt);
                return;
            }
        }

        void OnSwipe(TrickKind kind)
        {
            var a = _shown;
            if (a == null || a.Ended || a.Failed || a.Succeeded) return;
            if (kind == a.Sequence[a.Done])
            {
                a.Done++;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.trick, 0.5f + 0.08f * a.Done);
                if (a.Done >= a.Sequence.Count) Succeed(a);
            }
            else Fail(a);
        }

        void Succeed(Attempt a)
        {
            a.Succeeded = true;
            if (a.Player)
            {
                if (_combo != null) _combo.Banner("PERFECT!", new Color(1f, 0.85f, 0.3f));
                Slow(false);
            }
        }

        // Wrong, or too slow: the flight ends here - down into the chasm.
        void Fail(Attempt a)
        {
            a.Failed = true;
            a.Ended = true;
            a.Car.CutFlight(0.15f);
            if (a.Player)
            {
                _shakeAt = Time.unscaledTime;
                if (_combo != null) _combo.Banner("MISSED!", new Color(1f, 0.4f, 0.35f));
                Slow(false);
            }
        }

        void Update()
        {
            for (int i = _attempts.Count - 1; i >= 0; i--)
            {
                var a = _attempts[i];
                if (a.Car == null || (!a.Car.IsAirborne && !a.Ended))
                {
                    // Landed: a success earns a big boost on top of the tricks' own.
                    if (a.Car != null && a.Succeeded) a.Car.GiveBoost(1f);
                    if (a == _shown) { _shown = null; Slow(false); }
                    _attempts.RemoveAt(i);
                    continue;
                }
                if (a.Ended || a.Succeeded) continue;
                if (a.Player)
                {
                    // Out of time with tricks still to do: it comes down short.
                    if (a.Car.AirTimeLeft < 0.35f) Fail(a);
                    continue;
                }
                // A rival: its tricks one after another, and down into the chasm half way over if this one fails.
                if (a.Failed)
                {
                    if (a.Car.AirTimeLeft < 0.5f * a.Total) { a.Ended = true; a.Car.CutFlight(0.15f); }
                    continue;
                }
                if (Time.time >= a.NextShow && a.Done < a.Sequence.Count)
                {
                    a.Car.DoTrick(a.Sequence[a.Done]);
                    a.Done++;
                    a.NextShow = Time.time + 0.34f;
                    if (a.Done >= a.Sequence.Count) a.Succeeded = true;
                }
            }
            // While the arrows are up, swipes are the tricks (on any other jump a tap spins).
            if (_playerInput != null) _playerInput.SwipeMode = _shown != null && !_shown.Ended && !_shown.Failed && !_shown.Succeeded;
            DrawUi();
        }

        void Slow(bool on)
        {
            if (on && !_slow)
            {
                _slow = true;
                _scaleBefore = Time.timeScale;
                Time.timeScale = _scaleBefore * SlowScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
            else if (!on) Restore();
        }

        void Restore()
        {
            if (!_slow) return;
            _slow = false;
            Time.timeScale = _scaleBefore;
            Time.fixedDeltaTime = 0.02f;
        }

        // ------------------------------------------------------------------ the arrows on screen

        void BuildUi()
        {
            var go = new GameObject("TrickJumpHud");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 6;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170, 2532);
            scaler.matchWidthOrHeight = 0.5f;
            _arrowSprite = ArrowSprite();
            _square = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            for (int k = 0; k < MaxTricks; k++)
            {
                var img = Img(go.transform, "Arrow", _arrowSprite, new Vector2(170f, 170f));
                img.gameObject.SetActive(false);
                _arrows.Add(img);
            }
            _barBack = Img(go.transform, "TimeBack", _square, new Vector2(700f, 26f));
            _barBack.color = new Color(0.08f, 0.12f, 0.25f, 0.6f);
            _barBack.rectTransform.anchoredPosition = new Vector2(0f, 190f);
            _bar = Img(go.transform, "Time", _square, new Vector2(700f, 26f));
            _bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            _bar.rectTransform.anchoredPosition = new Vector2(-350f, 190f);
            var label = new GameObject("Caption", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            _caption = label.AddComponent<TextMeshProUGUI>();
            var any = FindAnyObjectByType<TMP_Text>();
            if (any != null) _caption.font = any.font;
            _caption.fontSize = 64f;
            _caption.fontStyle = FontStyles.Italic;
            _caption.alignment = TextAlignmentOptions.Center;
            _caption.raycastTarget = false;
            _caption.fontMaterial = new Material(_caption.fontSharedMaterial);
            _caption.outlineWidth = 0.25f;
            _caption.outlineColor = new Color32(20, 30, 60, 255);
            _caption.rectTransform.sizeDelta = new Vector2(1000f, 100f);
            _caption.rectTransform.anchoredPosition = new Vector2(0f, 480f);
            _caption.text = "";
            _barBack.gameObject.SetActive(false);
            _bar.gameObject.SetActive(false);
        }

        static Image Img(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = size;
            return img;
        }

        void DrawUi()
        {
            if (_canvas == null) return;
            var a = _shown;
            // A miss stays on screen for a moment (the wrong arrow in red), then goes.
            bool show = a != null && !(a.Ended && !a.Failed) && !(a.Failed && Time.unscaledTime - _shakeAt > 0.9f);
            if (!show)
            {
                foreach (var img in _arrows) img.gameObject.SetActive(false);
                _bar.gameObject.SetActive(false);
                _barBack.gameObject.SetActive(false);
                _caption.text = "";
                return;
            }
            int n = a.Sequence.Count;
            // More than six: two rows (the first on top).
            int perRow = n > 6 ? (n + 1) / 2 : n;
            _caption.rectTransform.anchoredPosition = new Vector2(0f, n > perRow ? 680f : 480f); // above the arrows
            float spacing = 190f, shake = Time.unscaledTime - _shakeAt < 0.4f ? Mathf.Sin(Time.unscaledTime * 80f) * 14f : 0f;
            for (int k = 0; k < _arrows.Count; k++)
            {
                var img = _arrows[k];
                img.gameObject.SetActive(k < n);
                if (k >= n) continue;
                int row = k / perRow, col = k % perRow, inRow = Mathf.Min(perRow, n - row * perRow);
                img.rectTransform.anchoredPosition = new Vector2((col - (inRow - 1) * 0.5f) * spacing + shake, n > perRow ? (row == 0 ? 530f : 330f) : 330f);
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, a.Sequence[k] switch
                {
                    TrickKind.SpinLeft => 180f, TrickKind.SpinRight => 0f, TrickKind.FrontFlip => 90f, _ => -90f,
                });
                bool done = k < a.Done, current = k == a.Done && !a.Failed;
                float pulse = current ? 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 14f) : 1f;
                img.rectTransform.localScale = Vector3.one * (done ? 0.85f : pulse);
                img.color = a.Failed && k == a.Done ? new Color(1f, 0.3f, 0.25f) : done ? new Color(0.45f, 0.95f, 0.5f) : current ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 1f, 1f, 0.85f);
            }
            // The time left, as a bar shrinking towards the landing.
            float left = Mathf.Clamp01((a.Car.AirTimeLeft - 0.35f) / Mathf.Max(0.01f, a.Total - 0.35f));
            _barBack.gameObject.SetActive(!a.Failed && !a.Succeeded);
            _bar.gameObject.SetActive(!a.Failed && !a.Succeeded);
            _bar.rectTransform.sizeDelta = new Vector2(700f * left, 26f);
            _bar.color = Color.Lerp(new Color(1f, 0.35f, 0.3f), new Color(0.45f, 0.9f, 1f), left);
            _caption.text = a.Failed ? "" : a.Succeeded ? "" : "SWIPE THE TRICKS!";
        }

        // A thick white arrow pointing right, with a dark rim (tinted per state; turned for each direction).
        static Sprite ArrowSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    // Shaft from 18 to 70, head from 62 to 112.
                    float shaft = Mathf.Max(Mathf.Max(18f - px, px - 72f), Mathf.Abs(py - 64f) - 15f);
                    float head = Mathf.Max(62f - px, Mathf.Abs(py - 64f) - (112f - px) * 0.95f);
                    float d = Mathf.Min(shaft, head);
                    float fill = Mathf.Clamp01(0.5f - d), rim = Mathf.Clamp01(0.5f - (d - 7f)) - fill;
                    var c = Color.white * fill + new Color(0.08f, 0.12f, 0.25f, 1f) * rim;
                    c.a = Mathf.Clamp01(fill + rim);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
