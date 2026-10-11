using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Light snowfall over the winter map: soft white flakes drifting down and swaying, nearer (bigger)
    /// flakes falling faster. Fills its own rect; flakes that leave the bottom start again at the top.
    /// </summary>
    public class SnowFall : MonoBehaviour
    {
        [SerializeField] Sprite flake;
        [SerializeField, Min(1)] int count = 70;
        [SerializeField] Vector2 size = new(7f, 18f);
        [Tooltip("Fall speed (canvas units per second) of the smallest and the biggest flakes.")]
        [SerializeField] Vector2 speed = new(55f, 150f);
        [SerializeField, Min(0f)] float sway = 26f;

        RectTransform[] _flakes;
        float[] _depth, _phase, _baseX;

        void Start()
        {
            var rect = ((RectTransform)transform).rect;
            _flakes = new RectTransform[count];
            _depth = new float[count];
            _phase = new float[count];
            _baseX = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Flake", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float depth = Random.value;
                rt.sizeDelta = Vector2.one * Mathf.Lerp(size.x, size.y, depth * depth);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = flake;
                image.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 0.95f, depth));
                image.raycastTarget = false;
                _flakes[i] = rt;
                _depth[i] = depth;
                _phase[i] = Random.value * Mathf.PI * 2f;
                _baseX[i] = Random.Range(rect.xMin, rect.xMax);
                rt.anchoredPosition = new Vector2(_baseX[i], Random.Range(rect.yMin, rect.yMax));
            }
        }

        void Update()
        {
            if (_flakes == null) return;
            var rect = ((RectTransform)transform).rect;
            float t = Time.time, dt = Time.deltaTime;
            for (int i = 0; i < _flakes.Length; i++)
            {
                var rt = _flakes[i];
                float y = rt.anchoredPosition.y - Mathf.Lerp(speed.x, speed.y, _depth[i]) * dt;
                if (y < rect.yMin - 20f)
                {
                    y = rect.yMax + 20f;
                    _baseX[i] = Random.Range(rect.xMin, rect.xMax);
                }
                float x = _baseX[i] + Mathf.Sin(t * 0.8f + _phase[i]) * sway * (0.5f + _depth[i]);
                rt.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
