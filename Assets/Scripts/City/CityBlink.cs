using UnityEngine;

namespace SomeGame.City
{
    /// <summary>Pulses a renderer's tint like aircraft warning lights.</summary>
    public class CityBlink : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float period = 1.6f;
        [SerializeField, Range(0f, 1f)] float onFraction = 0.35f;

        MeshRenderer _renderer;
        MaterialPropertyBlock _block;

        public void Setup(MeshRenderer target)
        {
            _renderer = target;
            _block = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (_renderer == null) return;
            float t = Mathf.Repeat(Time.time / period, 1f);
            float on = t < onFraction ? Mathf.Sin(t / onFraction * Mathf.PI) : 0.08f;
            _block.SetColor("_BaseColor", Color.white * on);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
