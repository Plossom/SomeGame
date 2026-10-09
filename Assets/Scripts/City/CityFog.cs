using UnityEngine;

namespace SomeGame.City
{
    /// <summary>
    /// Fog over a locked district: a dark veil plus soft cloud layers that drift slowly and wrap
    /// around inside the district's area.
    /// </summary>
    public class CityFog : MonoBehaviour
    {
        [SerializeField, Min(1)] int clouds = 22;
        [SerializeField] Color cloudColor = new(0.36f, 0.33f, 0.52f, 0.75f);
        [SerializeField] Color veilColor = new(0.04f, 0.035f, 0.08f, 0.6f);
        [SerializeField] Vector2 drift = new(1.2f, 0.5f);

        Rect _area;
        Transform[] _clouds;
        Vector2[] _speeds;

        public void Setup(Rect area, Material material, int seed)
        {
            _area = area;
            var rng = new System.Random(seed * 17 + 3);
            var quad = CreateQuad();

            Spawn("Veil", quad, material, new Vector3(area.center.x, 2.5f, area.center.y),
                new Vector3(area.width * 1.05f, 1f, area.height * 1.05f), veilColor, null);

            _clouds = new Transform[clouds];
            _speeds = new Vector2[clouds];
            for (int i = 0; i < clouds; i++)
            {
                float size = 26f + (float)rng.NextDouble() * 30f;
                var pos = new Vector3(Mathf.Lerp(area.xMin, area.xMax, (float)rng.NextDouble()), 6f + (float)rng.NextDouble() * 22f,
                    Mathf.Lerp(area.yMin, area.yMax, (float)rng.NextDouble()));
                var c = cloudColor;
                c.a *= 0.6f + (float)rng.NextDouble() * 0.4f;
                _clouds[i] = Spawn($"Cloud{i}", quad, material, pos, new Vector3(size, 1f, size * 0.7f), c,
                    Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
                _speeds[i] = drift * (0.6f + (float)rng.NextDouble() * 0.8f);
            }
        }

        Transform Spawn(string name, Mesh quad, Material material, Vector3 position, Vector3 scale, Color color, Quaternion? rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (rotation.HasValue) go.transform.rotation = rotation.Value;
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            if (name == "Veil") block.SetTexture("_BaseMap", Texture2D.whiteTexture);
            r.SetPropertyBlock(block);
            return go.transform;
        }

        void Update()
        {
            if (_clouds == null) return;
            for (int i = 0; i < _clouds.Length; i++)
            {
                var p = _clouds[i].position;
                p.x += _speeds[i].x * Time.deltaTime;
                p.z += _speeds[i].y * Time.deltaTime;
                if (p.x > _area.xMax + 10f) p.x = _area.xMin - 10f;
                if (p.z > _area.yMax + 10f) p.z = _area.yMin - 10f;
                _clouds[i].position = p;
            }
        }

        static Mesh CreateQuad()
        {
            var mesh = new Mesh { name = "FogQuad" };
            mesh.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.colors32 = new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
