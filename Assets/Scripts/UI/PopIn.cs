using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Makes a window open and close smoothly: whenever its panel is shown, the dark backdrop fades in and the card
    /// pops up from slightly smaller with a soft overshoot while it fades in; closed with <see cref="Hide"/>, the card
    /// dips in a touch, shrinks away and fades out with the backdrop before the panel goes. Uses unscaled time (works
    /// while the race is paused).
    /// </summary>
    public class PopIn : MonoBehaviour
    {
        [SerializeField] RectTransform card;
        [SerializeField] UnityEngine.UI.Graphic backdrop;
        [SerializeField, Min(0.05f)] float duration = 0.32f;
        [SerializeField, Min(0.05f)] float closeDuration = 0.2f;

        CanvasGroup _group;
        float _backdropAlpha = -1f, _start;
        bool _closing;
        System.Action _closed;

        /// <summary>True while it is on its way out (still active, but as good as closed).</summary>
        public bool Closing => _closing && isActiveAndEnabled;

        /// <summary>Shows a window (popping it in; a window on its way out comes back).</summary>
        public static void Show(GameObject panel)
        {
            if (panel == null) return;
            var pop = panel.GetComponent<PopIn>();
            if (pop != null && pop._closing && panel.activeSelf) pop.Open();
            else panel.SetActive(true);
        }

        /// <summary>Closes a window with its close animation (a window without one gets it), then deactivates it.</summary>
        public static void Hide(GameObject panel, System.Action closed = null)
        {
            if (panel == null) return;
            if (!panel.activeInHierarchy)
            {
                panel.SetActive(false);
                closed?.Invoke();
                return;
            }
            var pop = panel.GetComponent<PopIn>();
            if (pop == null) pop = panel.AddComponent<PopIn>();
            pop.Close(closed);
        }

        /// <summary>True if the window is open (shown and not on its way out).</summary>
        public static bool IsShown(GameObject panel)
        {
            if (panel == null || !panel.activeSelf) return false;
            var pop = panel.GetComponent<PopIn>();
            return pop == null || !pop._closing;
        }

        RectTransform Card => card != null ? card : transform as RectTransform;

        void OnEnable() => Open();

        void OnDisable()
        {
            // Put everything back for the next time it opens.
            if (!_closing) return;
            _closing = false;
            Apply(1f);
        }

        void Open()
        {
            if (Card == null) return;
            if (_group == null) { _group = Card.GetComponent<CanvasGroup>(); if (_group == null) _group = Card.gameObject.AddComponent<CanvasGroup>(); }
            if (backdrop != null && _backdropAlpha < 0f) _backdropAlpha = backdrop.color.a;
            _closing = false;
            _closed = null;
            _group.interactable = true;
            _start = Time.unscaledTime;
            Apply(0f);
        }

        void Close(System.Action closed)
        {
            if (Card == null) { gameObject.SetActive(false); closed?.Invoke(); return; }
            if (_group == null) Open();
            _closed += closed;
            if (_closing) return;
            _closing = true;
            _group.interactable = false; // no double taps on its way out
            _start = Time.unscaledTime;
        }

        void Update()
        {
            if (Card == null) return;
            if (!_closing)
            {
                Apply(Mathf.Clamp01((Time.unscaledTime - _start) / duration));
                return;
            }
            float t = Mathf.Clamp01((Time.unscaledTime - _start) / closeDuration);
            ApplyClose(t);
            if (t < 1f) return;
            var closed = _closed;
            _closed = null;
            gameObject.SetActive(false); // (OnDisable resets the look)
            closed?.Invoke();
        }

        void Apply(float t)
        {
            const float s = 1.6f;
            float u = t - 1f;
            float pop = 1f + u * u * ((s + 1f) * u + s);
            Card.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, pop);
            _group.alpha = Mathf.Clamp01(t * 2.5f);
            SetBackdrop(Mathf.Clamp01(t * 1.8f));
        }

        // Out: a little dip (it swells a touch first), then it shrinks away and fades, the backdrop with it.
        void ApplyClose(float t)
        {
            float scale = t < 0.25f ? Mathf.Lerp(1f, 1.04f, t / 0.25f) : Mathf.Lerp(1.04f, 0.8f, Mathf.SmoothStep(0f, 1f, (t - 0.25f) / 0.75f));
            Card.localScale = Vector3.one * scale;
            _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.85f));
            SetBackdrop(1f - t);
        }

        void SetBackdrop(float k)
        {
            if (backdrop == null) return;
            var c = backdrop.color;
            c.a = _backdropAlpha * k;
            backdrop.color = c;
        }
    }
}
