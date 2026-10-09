using System;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// A label floating over a point of the 3D city (district name and progress, or the garage).
    /// Follows its world point every frame and hides when it is behind the camera.
    /// </summary>
    public class DistrictMarker : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text detail;
        [SerializeField] UnityEngine.UI.Image accentDot;
        [SerializeField] UnityEngine.UI.Image outline;
        [SerializeField] UnityEngine.UI.Image detailIcon;
        [SerializeField] Sprite starIcon;
        [SerializeField] Sprite lockIcon;
        [SerializeField] UnityEngine.UI.Image pin;

        [Tooltip("Keeps off-screen labels at the screen edge (canvas units): left/right, bottom, top.")]
        [SerializeField] Vector3 edgeMargin = new(250f, 180f, 470f);

        Vector3 _world;
        UnityEngine.Camera _camera;
        RectTransform _rect, _parent;
        CanvasGroup _group;

        /// <summary>Hides the label instead of keeping it at the screen edge (e.g. while a sheet covers the city).</summary>
        public bool HideOffScreen { get; set; }

        public void Setup(UnityEngine.Camera cam, Vector3 world, Action onClick)
        {
            _camera = cam;
            _world = world;
            _rect = (RectTransform)transform;
            _parent = (RectTransform)transform.parent;
            _group = GetComponent<CanvasGroup>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        public void Show(string name, string info, Color accent, bool locked, bool lockedIcon)
        {
            title.text = name;
            title.color = locked ? NeonTheme.Muted : NeonTheme.Text;
            detail.text = info;
            detail.color = locked ? NeonTheme.Dim : NeonTheme.Lime;
            accentDot.color = locked ? NeonTheme.Faint : accent;
            outline.color = locked ? NeonTheme.Border : accent;
            pin.color = NeonTheme.WithAlpha(locked ? NeonTheme.Faint : accent, 0.8f);
            detailIcon.sprite = lockedIcon ? lockIcon : starIcon;
            detailIcon.color = locked ? NeonTheme.Dim : NeonTheme.Lime;
            detailIcon.gameObject.SetActive(!string.IsNullOrEmpty(info));
        }

        void LateUpdate()
        {
            if (_camera == null) return;
            Vector3 screen = _camera.WorldToScreenPoint(_world);
            bool visible = screen.z > 0f;
            if (!visible)
            {
                if (_group != null) _group.alpha = 0f;
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, screen, null, out var local);
            // Districts outside the view stay at the edge, slightly faded, as direction hints.
            Rect r = _parent.rect;
            var clamped = new Vector2(
                Mathf.Clamp(local.x, r.xMin + edgeMargin.x, r.xMax - edgeMargin.x),
                Mathf.Clamp(local.y, r.yMin + edgeMargin.y, r.yMax - edgeMargin.z));
            bool offScreen = (clamped - local).sqrMagnitude > 1f;
            _rect.anchoredPosition = clamped;
            pin.gameObject.SetActive(!offScreen);
            if (_group != null) _group.alpha = offScreen ? (HideOffScreen ? 0f : 0.7f) : 1f;
            if (_group != null) _group.blocksRaycasts = !(offScreen && HideOffScreen);
        }
    }
}
