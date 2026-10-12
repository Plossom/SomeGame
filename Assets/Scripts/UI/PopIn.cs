using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Makes a window open smoothly: whenever its panel is shown, the dark backdrop fades in and the card
    /// pops up from slightly smaller with a soft overshoot while it fades in. Uses unscaled time (works
    /// while the race is paused).
    /// </summary>
    public class PopIn : MonoBehaviour
    {
        [SerializeField] RectTransform card;
        [SerializeField] UnityEngine.UI.Graphic backdrop;
        [SerializeField, Min(0.05f)] float duration = 0.32f;

        CanvasGroup _group;
        float _backdropAlpha = -1f, _start;

        void OnEnable()
        {
            if (card == null) return;
            if (_group == null) { _group = card.GetComponent<CanvasGroup>(); if (_group == null) _group = card.gameObject.AddComponent<CanvasGroup>(); }
            if (backdrop != null && _backdropAlpha < 0f) _backdropAlpha = backdrop.color.a;
            _start = Time.unscaledTime;
            Apply(0f);
        }

        void Update()
        {
            if (card == null) return;
            float t = (Time.unscaledTime - _start) / duration;
            Apply(Mathf.Clamp01(t));
        }

        void Apply(float t)
        {
            const float s = 1.6f;
            float u = t - 1f;
            float pop = 1f + u * u * ((s + 1f) * u + s);
            card.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, pop);
            _group.alpha = Mathf.Clamp01(t * 2.5f);
            if (backdrop != null)
            {
                var c = backdrop.color;
                c.a = _backdropAlpha * Mathf.Clamp01(t * 1.8f);
                backdrop.color = c;
            }
        }
    }
}
