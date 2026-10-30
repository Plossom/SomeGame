using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Turntables (<see cref="TrackLayout.turntables"/>): giant spinning carousel floors the road crosses - red and
    /// white segments, a gold rim, lights round the edge, a hub with a star. They turn all the time and carry every car
    /// on them round with them (its position and its heading), so you have to steer against them to hold your line;
    /// all of the floor counts as road.
    /// </summary>
    public class TrackTurntables : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;

        const int FloorOrder = -47, RimOrder = -46;
        const int Segments = 16;
        static readonly Color Red = new Color(0.78f, 0.12f, 0.14f).linear, White = new Color(0.97f, 0.95f, 0.9f).linear,
            Gold = new Color(0.96f, 0.76f, 0.25f).linear, GoldDark = new Color(0.72f, 0.5f, 0.12f).linear,
            Bulb = new Color(1f, 0.95f, 0.7f).linear, Hub = new Color(0.15f, 0.42f, 0.24f).linear;

        readonly List<GameObject> _parts = new();
        readonly List<(TrackLayout.Turntable data, Transform spin)> _tables = new();
        SomeGame.Car.CarMovement[] _cars;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _tables.Clear();
            if (layout.turntables == null) return;
            foreach (var t in layout.turntables) BuildTable(t);
        }

        void BuildTable(TrackLayout.Turntable t)
        {
            var root = new GameObject("Turntable") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(transform, false);
            root.transform.position = t.position;
            _parts.Add(root);
            // The floor: red and white segments, a gold rim with bulbs round it, a green hub with a gold star.
            var v = new List<Vector3>(); var c = new List<Color>(); var tri = new List<int>();
            float r = t.radius, step = Mathf.PI * 2f / Segments;
            for (int k = 0; k < Segments; k++)
            {
                float a0 = k * step, a1 = a0 + step;
                Wedge(Vector2.zero, r * 0.18f, r * 0.92f, a0, a1, k % 2 == 0 ? Red : White, v, c, tri);
            }
            var floor = Mesh("TurntableFloor", v, c, tri);
            v = new List<Vector3>(); c = new List<Color>(); tri = new List<int>();
            for (int k = 0; k < Segments * 3; k++)
            {
                float a0 = k * step / 3f, a1 = a0 + step / 3f;
                Wedge(Vector2.zero, r * 0.92f, r, a0, a1, k % 2 == 0 ? Gold : GoldDark, v, c, tri);
            }
            for (int k = 0; k < Segments * 2; k++)
            {
                float a = (k + 0.5f) * step / 2f;
                Vector2 at = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.96f;
                Disc(at, r * 0.025f + 0.08f, Bulb, v, c, tri);
            }
            Disc(Vector2.zero, r * 0.18f, Hub, v, c, tri);
            Star(Vector2.zero, r * 0.15f, Gold, v, c, tri);
            var rim = Mesh("TurntableRim", v, c, tri);
            var spin = new GameObject("Spin") { hideFlags = HideFlags.DontSave }.transform;
            spin.SetParent(root.transform, false);
            Part(spin, "Floor", floor, FloorOrder);
            Part(spin, "Rim", rim, RimOrder);
            _tables.Add((t, spin));
        }

        void Part(Transform parent, string name, Mesh mesh, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = plain;
            renderer.sortingOrder = order;
        }

        void Update()
        {
            float time = Application.isPlaying ? Track.Clock : 0f;
            foreach (var (data, spin) in _tables)
                if (spin != null) spin.localRotation = Quaternion.Euler(0f, 0f, data.speed * time);
        }

        // Every car on a turntable (on the ground) goes round with it.
        void FixedUpdate()
        {
            if (!Application.isPlaying || _tables.Count == 0) return;
            if (_cars == null) _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
            float dt = Time.fixedDeltaTime;
            foreach (var car in _cars)
            {
                if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                Vector2 p = car.Body.position;
                foreach (var (data, _) in _tables)
                {
                    if ((p - data.position).sqrMagnitude > data.radius * data.radius) continue;
                    float turn = data.speed * dt;
                    var q = Quaternion.Euler(0f, 0f, turn);
                    car.Body.position = data.position + (Vector2)(q * (p - data.position));
                    car.Body.rotation += turn;
                    car.Body.linearVelocity = q * car.Body.linearVelocity;
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ mesh helpers

        static void Wedge(Vector2 centre, float inner, float outer, float a0, float a1, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            const int steps = 3;
            for (int s = 0; s < steps; s++)
            {
                float b0 = Mathf.Lerp(a0, a1, s / (float)steps), b1 = Mathf.Lerp(a0, a1, (s + 1f) / steps);
                Vector2 d0 = new Vector2(Mathf.Cos(b0), Mathf.Sin(b0)), d1 = new Vector2(Mathf.Cos(b1), Mathf.Sin(b1));
                Quad(centre + d0 * inner, centre + d0 * outer, centre + d1 * outer, centre + d1 * inner, color, v, c, t);
            }
        }

        static void Disc(Vector2 centre, float radius, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(centre); c.Add(color);
            const int n = 12;
            for (int k = 0; k <= n; k++)
            {
                float a = k * Mathf.PI * 2f / n;
                v.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius); c.Add(color);
                if (k > 0) t.AddRange(new[] { i, i + k, i + k + 1, i, i + k + 1, i + k });
            }
        }

        static void Star(Vector2 centre, float radius, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(centre); c.Add(color);
            for (int k = 0; k <= 10; k++)
            {
                float a = Mathf.PI * 0.5f + k * Mathf.PI / 5f, r = k % 2 == 0 ? radius : radius * 0.42f;
                v.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); c.Add(color);
                if (k > 0) t.AddRange(new[] { i, i + k, i + k + 1, i, i + k + 1, i + k });
            }
        }

        static void Quad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) c.Add(color);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        static Mesh Mesh(string name, List<Vector3> v, List<Color> c, List<int> t)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
