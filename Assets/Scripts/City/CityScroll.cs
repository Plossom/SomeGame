using UnityEngine;

namespace SomeGame.City
{
    /// <summary>Slowly scrolls the texture of this renderer (harbour water reflections).</summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CityScroll : MonoBehaviour
    {
        [SerializeField] Vector2 speed = new(0.004f, 0.012f);

        MeshRenderer _renderer;
        MaterialPropertyBlock _block;
        Vector4 _st = new(1f, 1f, 0f, 0f);

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
        }

        void Update()
        {
            _st.z = Mathf.Repeat(_st.z + speed.x * Time.deltaTime, 1f);
            _st.w = Mathf.Repeat(_st.w + speed.y * Time.deltaTime, 1f);
            _block.SetVector("_BaseMap_ST", _st);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
