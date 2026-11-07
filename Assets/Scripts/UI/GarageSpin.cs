using UnityEngine;
using UnityEngine.EventSystems;

namespace SomeGame.UI
{
    /// <summary>
    /// The garage's stage turns slowly on its own; drag across it to spin the car round (it coasts and settles
    /// back to the slow turn).
    /// </summary>
    public class GarageSpin : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] RectTransform target;
        [SerializeField] float idleSpeed = 14f;

        float _speed, _angle;
        bool _dragging;

        void OnEnable() => _speed = idleSpeed;

        public void OnBeginDrag(PointerEventData eventData) => _dragging = true;

        public void OnDrag(PointerEventData eventData)
        {
            float degrees = -eventData.delta.x * 0.35f;
            _angle += degrees;
            _speed = degrees / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        }

        public void OnEndDrag(PointerEventData eventData) => _dragging = false;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!_dragging)
            {
                _speed = Mathf.Lerp(_speed, idleSpeed, 1f - Mathf.Exp(-1.6f * dt));
                _angle += _speed * dt;
            }
            target.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }
    }
}
