using TMPro;
using UnityEngine;

namespace SomeGame.Online
{
    /// <summary>A player's name floating above their car in an online race, in their car colour.</summary>
    public class NameTag : MonoBehaviour
    {
        static readonly Vector3 Offset = new(0f, 1.7f, 0f);

        Transform _target;
        TextMeshPro _text;

        public void Follow(Transform target, string name, Color color)
        {
            _target = target;
            _text = gameObject.AddComponent<TextMeshPro>();
            _text.text = name;
            _text.fontSize = 5f;
            _text.fontStyle = FontStyles.Bold | FontStyles.Italic;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.color = Color.Lerp(color, Color.white, 0.25f);
            _text.outlineColor = new Color32(23, 61, 51, 255);
            _text.outlineWidth = 0.28f;
            _text.rectTransform.sizeDelta = new Vector2(10f, 2f);
            _text.sortingOrder = 60;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                if (_text != null) _text.enabled = false;
                if (_target == null) Destroy(gameObject);
                return;
            }
            _text.enabled = true;
            transform.SetPositionAndRotation(_target.position + Offset, Quaternion.identity);
        }
    }
}
