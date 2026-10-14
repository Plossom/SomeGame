using SomeGame.Race;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.UI
{
    /// <summary>
    /// A short tour of the map after the tutorial (MAP on its last card): the screen dims except for a
    /// spotlight that glides from one part to the next (race info, star times, records, the race stops,
    /// swiping between cups, the cup list, stars and trophies, online, the menu, START), each with a card
    /// saying what it is. Tap anywhere or NEXT to go on, SKIP to end it.
    /// </summary>
    public class MapTour : MonoBehaviour
    {
        public const string PendingKey = "Tour.Pending";

        [SerializeField] MapScreen screen;
        [SerializeField] GameObject root;
        [SerializeField] RectTransform area;
        [Tooltip("The dim around the spotlight: top, bottom, left, right.")]
        [SerializeField] RectTransform[] dims;
        [SerializeField] RectTransform frame;
        [SerializeField] RectTransform card;
        [SerializeField] TMP_Text stepLabel, titleLabel, bodyLabel, nextLabel;
        [SerializeField] Button nextButton, skipButton, tapArea;
        [SerializeField] RectTransform finger;
        [Tooltip("What each step shows, in order (an empty entry = the selected race's stop).")]
        [SerializeField] RectTransform[] targets;

        static readonly (string title, string body)[] Steps =
        {
            ("THE RACE", "Tap the race name (or the <b>i</b>) to see the whole track, how hard it is and what waits on it."),
            ("STAR TIMES", "Finish faster than these times to earn 1, 2 or 3 stars."),
            ("YOUR RECORDS", "Your best time and your best lap on this race."),
            ("THE RACES", "Tap a stop on the map to pick a race. New races open as you collect stars."),
            ("SWITCH CUPS", "Swipe left or right anywhere on the map to go to the next cup."),
            ("THE CUPS", "Or open this list and tap a cup to jump to it."),
            ("STARS & TROPHIES", "Everything you have won. Enough stars and trophies unlock new cups."),
            ("PLAY ONLINE", "Race your friends: open a room and share its code. Up to 8 racers!"),
            ("MENU", "Sound, controls, the tutorial and more."),
            ("READY?", "Tap <b>START</b> to race the selected track. Have fun!"),
        };

        int _step = -1;
        Rect _hole, _holeFrom;
        float _stepAt;
        bool _waiting;

        void Awake()
        {
            root.SetActive(false);
            nextButton.onClick.AddListener(Next);
            tapArea.onClick.AddListener(Next);
            skipButton.onClick.AddListener(End);
        }

        void Start() => _waiting = PlayerPrefs.GetInt(PendingKey, 0) == 1;

        /// <summary>Starts the tour now (also from code for tests).</summary>
        public void Play()
        {
            _waiting = false;
            root.SetActive(true);
            _hole = new Rect(area.rect.center, Vector2.zero);
            _step = -1;
            Next();
        }

        void Next()
        {
            if (Time.unscaledTime - _stepAt < 0.25f && _step >= 0) return; // a double tap is one step
            _step++;
            if (_step >= Steps.Length) { End(); return; }
            _stepAt = Time.unscaledTime;
            _holeFrom = _hole;
            stepLabel.text = $"MAP TOUR · {_step + 1}/{Steps.Length}";
            titleLabel.text = Steps[_step].title;
            bodyLabel.text = Steps[_step].body;
            nextLabel.text = _step == Steps.Length - 1 ? "GOT IT" : "NEXT";
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.6f);
        }

        void End()
        {
            PlayerPrefs.SetInt(PendingKey, 0);
            PlayerPrefs.Save();
            root.SetActive(false);
            _step = -1;
        }

        RectTransform Target(int step)
        {
            var t = step < targets.Length ? targets[step] : null;
            return t != null ? t : screen.SelectedStop;
        }

        // A target's rectangle in the tour's own space, with some room around it.
        Rect RectOf(RectTransform target)
        {
            if (target == null) return new Rect(area.rect.center, Vector2.zero);
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = area.InverseTransformPoint(corners[0]), max = area.InverseTransformPoint(corners[2]);
            const float pad = 22f;
            return Rect.MinMaxRect(min.x - pad, min.y - pad, max.x + pad, max.y + pad);
        }

        void Update()
        {
            if (_waiting)
            {
                if (screen != null && !screen.Busy && !GameSession.Loading) Play();
                return;
            }
            if (_step < 0) return;
            float t = Time.unscaledTime - _stepAt;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.45f), 3f);
            var to = RectOf(Target(_step));
            _hole = new Rect(Vector2.Lerp(_holeFrom.position, to.position, e), Vector2.Lerp(_holeFrom.size, to.size, e));
            Layout(_hole);

            // The frame breathes; the card pops in for each step.
            frame.localScale = Vector3.one * (1f + 0.02f * Mathf.Sin(Time.unscaledTime * 4f));
            float pop = Mathf.Clamp01((t - 0.15f) / 0.35f);
            const float s = 1.7f;
            float u = pop - 1f;
            card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, 1f + u * u * ((s + 1f) * u + s));
            card.GetComponent<CanvasGroup>().alpha = pop;

            // The card sits under the spotlight when it is in the upper half, else above it.
            var r = area.rect;
            bool below = to.center.y > r.center.y;
            float y = below ? to.yMin - 30f : to.yMax + 30f;
            float h = card.rect.height;
            y = below ? Mathf.Max(y, r.yMin + h + 60f) : Mathf.Min(y, r.yMax - h - 60f);
            card.pivot = new Vector2(0.5f, below ? 1f : 0f);
            card.anchoredPosition = new Vector2(0f, y - r.center.y);

            // The swipe step: a finger sweeps across the map.
            bool swipe = _step == 4;
            finger.gameObject.SetActive(swipe);
            if (swipe)
            {
                float c = Mathf.Repeat(Time.unscaledTime, 1.6f) / 1.6f;
                float x = Mathf.Lerp(260f, -260f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.75f, c)));
                finger.anchoredPosition = to.center - r.center + new Vector2(x, -40f);
                finger.GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Sin(Mathf.Clamp01(c / 0.9f) * Mathf.PI));
            }
        }

        // The four dim panels around the hole, and the frame on it.
        void Layout(Rect hole)
        {
            var r = area.rect;
            Set(dims[0], new Rect(r.xMin, hole.yMax, r.width, Mathf.Max(0f, r.yMax - hole.yMax)));
            Set(dims[1], new Rect(r.xMin, r.yMin, r.width, Mathf.Max(0f, hole.yMin - r.yMin)));
            Set(dims[2], new Rect(r.xMin, hole.yMin, Mathf.Max(0f, hole.xMin - r.xMin), hole.height));
            Set(dims[3], new Rect(hole.xMax, hole.yMin, Mathf.Max(0f, r.xMax - hole.xMax), hole.height));
            Set(frame, hole);
        }

        void Set(RectTransform rt, Rect rect)
        {
            var c = area.rect.center;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = rect.center - c;
            rt.sizeDelta = rect.size;
        }
    }
}
