using UnityEngine;
using UnityEngine.EventSystems;

namespace SomeGame.UI
{
    /// <summary>
    /// Sideways drags on the map (anywhere that is not a button or panel) go to the <see cref="MapScreen"/>,
    /// which drags the page along and switches to the next or previous cup on release. Mostly vertical
    /// drags are ignored. Lives on the map's full-screen layer, so taps on the race stops still work.
    /// </summary>
    public class MapSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] MapScreen screen;
        [SerializeField] Canvas canvas;

        bool _active;
        float _dx, _velocity;

        public void OnBeginDrag(PointerEventData e)
        {
            _active = Mathf.Abs(e.delta.x) >= Mathf.Abs(e.delta.y) && screen.CanSwipe;
            _dx = 0f;
            _velocity = 0f;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_active) return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            float step = e.delta.x / scale;
            _dx += step;
            if (Time.unscaledDeltaTime > 0f) _velocity = Mathf.Lerp(_velocity, step / Time.unscaledDeltaTime, 0.5f);
            screen.DragPage(_dx);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_active) return;
            _active = false;
            screen.ReleasePage(_dx, _velocity);
        }
    }
}
