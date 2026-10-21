using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Sizes a track picture's frame to the picture: the track previews have each track's own shape (tall,
    /// square or wide), so the frame shrinks to fit it inside its largest size instead of showing empty
    /// bands. The frame keeps its pivot point, so a centred frame stays centred.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PreviewFrame : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Image picture;
        [Tooltip("The frame's largest size (the picture plus the border on each side).")]
        [SerializeField] Vector2 maxSize = new(570f, 1006f);
        [Tooltip("Border between the frame's edge and the picture.")]
        [SerializeField, Min(0f)] float inset = 12f;

        Sprite _shown;

        void OnEnable() => Fit();

        void LateUpdate()
        {
            if (picture != null && picture.sprite != _shown) Fit();
        }

        void Fit()
        {
            if (picture == null) return;
            _shown = picture.sprite;
            var rt = (RectTransform)transform;
            if (_shown == null)
            {
                rt.sizeDelta = maxSize;
                return;
            }
            Vector2 room = maxSize - Vector2.one * (inset * 2f);
            float aspect = _shown.rect.width / Mathf.Max(1f, _shown.rect.height);
            Vector2 size = room.x / room.y > aspect ? new Vector2(room.y * aspect, room.y) : new Vector2(room.x, room.x / aspect);
            rt.sizeDelta = size + Vector2.one * (inset * 2f);
        }
    }
}
