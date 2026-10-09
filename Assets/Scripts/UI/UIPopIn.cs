using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Pops the element in (scale overshoot + fade) whenever it becomes active, after an optional
    /// delay, so screens can stagger their elements. Unscaled time.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIPopIn : MonoBehaviour
    {
        [SerializeField, Min(0f)] float delay;
        [SerializeField, Min(0.01f)] float duration = 0.35f;
        [SerializeField, Min(0f)] float startScale = 0.6f;

        CanvasGroup _group;
        float _start;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            _start = Time.unscaledTime + delay;
            Apply(0f);
        }

        void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _start) / duration);
            Apply(t);
        }

        void Apply(float t)
        {
            // Ease-out-back: overshoot slightly past 1, then settle.
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float eased = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, eased);
            _group.alpha = Mathf.Clamp01(t * 2f);
        }
    }
}
