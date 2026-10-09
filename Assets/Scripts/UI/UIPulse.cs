using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Gentle looping pulse for highlights (START ring, selected race). Scales the object and fades
    /// an optional graphic. Uses unscaled time so it keeps moving while the game is paused.
    /// </summary>
    public class UIPulse : MonoBehaviour
    {
        [SerializeField, Min(0f)] float speed = 1.6f;
        [Tooltip("Scale at the low and high point of the pulse.")]
        [SerializeField] Vector2 scale = new(1f, 1.08f);
        [Tooltip("Optional graphic whose alpha follows the pulse.")]
        [SerializeField] UnityEngine.UI.Graphic fade;
        [SerializeField] Vector2 alpha = new(0.35f, 0.9f);
        [Tooltip("Expanding ring mode: grows from scale.x to scale.y and fades out, then restarts.")]
        [SerializeField] bool ripple;

        void Update()
        {
            float t = ripple
                ? Mathf.Repeat(Time.unscaledTime * speed, 1f)
                : (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = Vector3.one * Mathf.Lerp(scale.x, scale.y, ripple ? Mathf.SmoothStep(0f, 1f, t) : t);
            if (fade == null) return;
            var c = fade.color;
            c.a = ripple ? Mathf.Lerp(alpha.y, 0f, t) : Mathf.Lerp(alpha.x, alpha.y, t);
            fade.color = c;
        }
    }
}
