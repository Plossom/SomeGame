using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.UI
{
    /// <summary>
    /// The little demo under the tutorial's hint card: white fingers acting out the current step on a
    /// see-through plate, with a mini car showing what happens (steer, hop, drift, charge, boost, jump).
    /// It loops while the step is shown. Nothing in it catches touches.
    /// </summary>
    public class TutorialDemo : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform finger1, finger2;
        [SerializeField] RectTransform ring, knob;
        [SerializeField] RectTransform press, ripple;
        [SerializeField] RectTransform car, carShadow;
        [SerializeField] Image[] sparks;
        [SerializeField] RectTransform[] speedLines;
        [SerializeField] RectTransform ramp;

        static readonly Color Blue = new(0.31f, 0.64f, 1f), Orange = new(1f, 0.6f, 0.24f), Purple = new(0.71f, 0.42f, 1f);
        static readonly Vector2 RingAt = new(-210f, -30f), TapAt = new(30f, -40f), CarAt = new(230f, 0f);

        int _step = -1;
        SomeGame.Input.ControlSettings.DriftMode _mode;
        float _start;
        Image _rippleImage, _pressImage;

        public void Play(int step, SomeGame.Input.ControlSettings.DriftMode mode)
        {
            _step = step;
            _mode = mode;
            _start = Time.unscaledTime;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _step = -1;
            gameObject.SetActive(false);
        }

        void Awake()
        {
            _rippleImage = ripple.GetComponent<Image>();
            _pressImage = press.GetComponent<Image>();
        }

        void Update()
        {
            if (_step < 0) return;
            float t = Time.unscaledTime - _start;
            group.alpha = Mathf.Clamp01(t / 0.3f);
            // Defaults: one finger on the stick, nothing else.
            ring.gameObject.SetActive(true);
            finger2.gameObject.SetActive(false);
            press.gameObject.SetActive(false);
            ripple.gameObject.SetActive(false);
            foreach (var s in sparks) s.gameObject.SetActive(false);
            foreach (var l in speedLines) l.gameObject.SetActive(false);
            ramp.gameObject.SetActive(false);
            car.localScale = Vector3.one;
            car.anchoredPosition = CarAt;
            carShadow.anchoredPosition = CarAt + new Vector2(6f, -8f);
            carShadow.localScale = Vector3.one;

            switch (_step)
            {
                case 0: Steer(t); break;
                case 1: Hop(t); break;
                case 2: Drift(t, false); break;
                case 3: Drift(t, true); break;
                case 4: Boost(t); break;
                default: Jump(t); break;
            }
        }

        // The stick: the knob at an angle from the ring's centre, the first finger's tip on it.
        void Stick(float angleDeg, float reach = 62f)
        {
            float a = (angleDeg + 90f) * Mathf.Deg2Rad;
            Vector2 k = RingAt + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * reach;
            knob.anchoredPosition = k;
            finger1.anchoredPosition = k;
        }

        void CarHeading(float angleDeg) => car.localRotation = carShadow.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

        void Tap(Vector2 at, float k)
        {
            ripple.gameObject.SetActive(k > 0f && k < 1f);
            ripple.anchoredPosition = at;
            ripple.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.5f, k);
            _rippleImage.color = new Color(1f, 1f, 1f, 0.9f * (1f - k));
        }

        void Steer(float t)
        {
            float angle = Mathf.Sin(t * 1.6f) * 55f;
            Stick(angle);
            CarHeading(angle);
        }

        void Hop(float t)
        {
            Stick(Mathf.Sin(t * 1.2f) * 12f);
            CarHeading(Mathf.Sin(t * 1.2f) * 12f);
            float c = Mathf.Repeat(t, 1.6f);
            bool down = c > 0.4f && c < 0.65f;
            finger2.gameObject.SetActive(true);
            finger2.anchoredPosition = TapAt + new Vector2(0f, down ? -6f : 18f);
            finger2.localScale = Vector3.one * (down ? 0.94f : 1f);
            Tap(TapAt, Mathf.InverseLerp(0.45f, 1f, c));
            float hop = c > 0.45f && c < 0.85f ? Mathf.Sin(Mathf.InverseLerp(0.45f, 0.85f, c) * Mathf.PI) : 0f;
            car.localScale = Vector3.one * (1f + 0.3f * hop);
            carShadow.anchoredPosition = CarAt + new Vector2(6f + 12f * hop, -8f - 16f * hop);
            carShadow.localScale = Vector3.one * (1f - 0.2f * hop);
        }

        // Steer a little into the bend, second finger presses and stays down: the car slides round sideways.
        void Drift(float t, bool charge)
        {
            float period = charge ? 3.2f : 2.6f;
            float c = Mathf.Repeat(t, period);
            bool held = c > 0.4f && c < period - 0.35f;
            if (_mode == SomeGame.Input.ControlSettings.DriftMode.Manual)
            {
                Stick(-28f);
                finger2.gameObject.SetActive(true);
                finger2.anchoredPosition = TapAt + new Vector2(0f, held ? -6f : 18f);
                finger2.localScale = Vector3.one * (held ? 0.94f : 1f);
                press.gameObject.SetActive(held);
                press.anchoredPosition = TapAt;
                _pressImage.color = new Color(1f, 1f, 1f, 0.45f + 0.15f * Mathf.Sin(t * 8f));
            }
            else
            {
                // Auto: into the curve and hold.
                float stick = c < 0.4f ? Mathf.Lerp(0f, -40f, c / 0.4f) : held ? -40f : Mathf.Lerp(-40f, 0f, (c - (period - 0.35f)) / 0.35f);
                Stick(stick);
            }
            float slide = held ? Mathf.Clamp01((c - 0.4f) / 0.3f) : 0f;
            CarHeading(-28f - 38f * slide);
            car.anchoredPosition = CarAt + new Vector2(Mathf.Sin(c * 2f) * 12f * slide, 0f);
            carShadow.anchoredPosition = car.anchoredPosition + new Vector2(6f, -8f);
            if (!charge || !held) return;
            // Sparks behind the car: blue, then orange, then purple, growing.
            float k = Mathf.Clamp01((c - 0.7f) / (period - 1.3f));
            Color colour = k < 0.33f ? Blue : k < 0.66f ? Orange : Purple;
            for (int i = 0; i < sparks.Length; i++)
            {
                var s = sparks[i];
                s.gameObject.SetActive(true);
                float flicker = Mathf.Repeat(t * 7f + i * 0.37f, 1f);
                Vector2 back = (Vector2)(car.localRotation * Vector3.down) * (78f + 22f * flicker);
                Vector2 side = (Vector2)(car.localRotation * Vector3.right) * (i - (sparks.Length - 1) * 0.5f) * 30f;
                s.rectTransform.anchoredPosition = car.anchoredPosition + back + side;
                s.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, k) * (1f - flicker * 0.5f);
                s.color = new Color(colour.r, colour.g, colour.b, 1f - flicker);
            }
        }

        // The drift finger lifts: the car straightens and dashes forward.
        void Boost(float t)
        {
            float c = Mathf.Repeat(t, 2.8f);
            if (c < 1.5f) { Drift(c, true); return; }
            if (_mode == SomeGame.Input.ControlSettings.DriftMode.Manual)
            {
                Stick(0f);
                finger2.gameObject.SetActive(true);
                float lift = Mathf.Clamp01((c - 1.5f) / 0.25f);
                finger2.anchoredPosition = TapAt + new Vector2(0f, -6f + 40f * lift);
                Tap(TapAt, Mathf.InverseLerp(1.5f, 2f, c));
            }
            else Stick(Mathf.Lerp(-40f, 0f, Mathf.Clamp01((c - 1.5f) / 0.2f))); // straighten the thumb
            float go = Mathf.Clamp01((c - 1.55f) / 0.3f);
            CarHeading(Mathf.Lerp(-66f, 0f, go));
            float dash = Mathf.InverseLerp(1.6f, 2.6f, c);
            car.anchoredPosition = CarAt + new Vector2(0f, 120f * Mathf.Sin(dash * Mathf.PI * 0.5f) - 20f);
            carShadow.anchoredPosition = car.anchoredPosition + new Vector2(6f, -8f);
            for (int i = 0; i < speedLines.Length; i++)
            {
                var line = speedLines[i];
                line.gameObject.SetActive(dash > 0f && dash < 1f);
                line.anchoredPosition = car.anchoredPosition + new Vector2((i - 1) * 30f, -105f - 36f * Mathf.Repeat(t * 4f + i * 0.3f, 1f));
            }
        }

        // Straight at the ramp with the white arrows: up and over, the shadow left behind on the ground.
        void Jump(float t)
        {
            Stick(0f);
            CarHeading(0f);
            ramp.gameObject.SetActive(true);
            ramp.anchoredPosition = CarAt + new Vector2(0f, -10f);
            float c = Mathf.Repeat(t, 2.4f);
            float y = Mathf.Lerp(-150f, 170f, Mathf.Clamp01(c / 1.8f));
            car.anchoredPosition = CarAt + new Vector2(0f, y);
            float air = y > 20f && y < 150f ? Mathf.Sin(Mathf.InverseLerp(20f, 150f, y) * Mathf.PI) : 0f;
            car.localScale = Vector3.one * (1f + 0.45f * air);
            carShadow.anchoredPosition = CarAt + new Vector2(6f + 18f * air, y - 8f - 26f * air);
            carShadow.localScale = Vector3.one * (1f - 0.25f * air);
            group.alpha = Mathf.Min(group.alpha, c > 2.1f ? 1f - (c - 2.1f) / 0.3f : 1f);
        }
    }
}
