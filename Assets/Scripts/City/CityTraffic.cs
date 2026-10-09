using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.City
{
    /// <summary>
    /// Night traffic: points of light (headlights ahead, tail lights behind) driving along the street
    /// lanes of unlocked districts. One mesh, rebuilt every frame.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CityTraffic : MonoBehaviour
    {
        [SerializeField, Min(0)] int cars = 160;
        [SerializeField] Vector2 speedRange = new(6f, 13f);
        [SerializeField] Color headlight = new(1f, 0.9f, 0.7f, 1f);
        [SerializeField] Color taillight = new(1f, 0.15f, 0.22f, 1f);
        [SerializeField, Min(0.1f)] float lightSize = 1.1f;

        struct Car
        {
            public int Lane;
            public float Distance;
            public float Speed;
        }

        readonly List<(Vector3 from, Vector3 to)> _lanes = new();
        Car[] _cars;
        Mesh _mesh;
        Vector3[] _vertices;
        System.Random _rng = new(42);

        public void Setup(List<(Vector3, Vector3)> lanes)
        {
            _lanes.Clear();
            _lanes.AddRange(lanes);
            if (_lanes.Count == 0) return;
            _cars = new Car[cars];
            for (int i = 0; i < cars; i++)
            {
                int lane = _rng.Next(_lanes.Count);
                _cars[i] = new Car
                {
                    Lane = lane,
                    Distance = (float)_rng.NextDouble() * Vector3.Distance(_lanes[lane].from, _lanes[lane].to),
                    Speed = Mathf.Lerp(speedRange.x, speedRange.y, (float)_rng.NextDouble()),
                };
            }

            _mesh = new Mesh { name = "Traffic" };
            _mesh.MarkDynamic();
            int quads = cars * 4;
            _vertices = new Vector3[quads * 4];
            var uvs = new Vector2[_vertices.Length];
            var colors = new Color[_vertices.Length];
            var triangles = new int[quads * 6];
            for (int q = 0; q < quads; q++)
            {
                int v = q * 4;
                uvs[v] = new Vector2(0, 0); uvs[v + 1] = new Vector2(1, 0); uvs[v + 2] = new Vector2(1, 1); uvs[v + 3] = new Vector2(0, 1);
                Color c = (q % 4) < 2 ? headlight : taillight;
                colors[v] = colors[v + 1] = colors[v + 2] = colors[v + 3] = c.linear;
                int t = q * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }
            _mesh.vertices = _vertices;
            _mesh.uv = uvs;
            _mesh.colors = colors;
            _mesh.triangles = triangles;
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        void Update()
        {
            if (_cars == null) return;
            float dt = Time.deltaTime, s = lightSize * 0.5f;
            for (int i = 0; i < _cars.Length; i++)
            {
                ref var car = ref _cars[i];
                var (from, to) = _lanes[car.Lane];
                float length = Vector3.Distance(from, to);
                car.Distance += car.Speed * dt;
                if (car.Distance > length)
                {
                    car.Lane = _rng.Next(_lanes.Count);
                    car.Distance = 0f;
                    (from, to) = _lanes[car.Lane];
                }
                Vector3 dir = (to - from).normalized, side = new Vector3(-dir.z, 0f, dir.x) * 0.45f;
                Vector3 pos = from + dir * car.Distance + Vector3.up * 0.35f;
                int v = i * 16;
                Light(v, pos + dir * 1.1f + side, s);
                Light(v + 4, pos + dir * 1.1f - side, s);
                Light(v + 8, pos - dir * 1.1f + side, s * 0.8f);
                Light(v + 12, pos - dir * 1.1f - side, s * 0.8f);
            }
            _mesh.vertices = _vertices;
        }

        void Light(int v, Vector3 c, float s)
        {
            _vertices[v] = c + new Vector3(-s, 0f, -s);
            _vertices[v + 1] = c + new Vector3(s, 0f, -s);
            _vertices[v + 2] = c + new Vector3(s, 0f, s);
            _vertices[v + 3] = c + new Vector3(-s, 0f, s);
        }
    }
}
