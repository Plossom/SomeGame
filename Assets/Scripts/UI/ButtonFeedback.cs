using UnityEngine;
using UnityEngine.EventSystems;

namespace SomeGame.UI
{
    /// <summary>Squashes a button slightly while it is pressed. Unscaled time, works while paused.</summary>
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.5f, 1f)] float pressedScale = 0.92f;
        [SerializeField, Min(1f)] float speed = 18f;

        float _target = 1f;
        Vector3 _baseScale = Vector3.one;

        void Awake() => _baseScale = transform.localScale;

        public void OnPointerDown(PointerEventData eventData) => _target = pressedScale;
        public void OnPointerUp(PointerEventData eventData) => _target = 1f;
        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        void OnDisable()
        {
            _target = 1f;
            transform.localScale = _baseScale;
        }

        void Update()
        {
            float current = transform.localScale.x / Mathf.Max(0.0001f, _baseScale.x);
            float next = Mathf.MoveTowards(current, _target, speed * Time.unscaledDeltaTime);
            transform.localScale = _baseScale * next;
        }
    }
}
