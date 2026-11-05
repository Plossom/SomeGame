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
        // Disco floors: their floor mesh and the colour of each square (four vertices each), relit to the beat.
        readonly List<(Mesh mesh, Color[] colours)> _discos = new();
        int _beat = -1;
        static readonly Color[] DiscoColours =
        {
            new Color(1f, 0.25f, 0.75f).linear, new Color(0.2f, 0.85f, 1f).linear, new Color(1f, 0.86f, 0.2f).linear,
            new Color(0.45f, 1f, 0.35f).linear, new Color(1f, 0.5f, 0.15f).linear, new Color(0.6f, 0.35f, 1f).linear,
        };
        static readonly Color Chrome = new Color(0.82f, 0.86f, 0.92f).linear, ChromeDark = new Color(0.5f, 0.55f, 0.63f).linear,
            DanceDark = new Color(0.1f, 0.08f, 0.16f).linear;
        SomeGame.Car.CarMovement[] _cars;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _tables.Clear(); _discos.Clear(); _beat = -1;
            if (layout.turntables == null) return;
            foreach (var t in layout.turntables) BuildTable(t);
        }

        void BuildTable(TrackLayout.Turntable t)
        {
            var root = new GameObject("Turntable") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(transform, false);
            root.transform.position = t.position;
            _parts.Add(root);
            if (t.disco) { BuildDisco(root, t); return; }
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

        // A disco dance floor: lit squares in a dark grid, a chrome rim with bulbs, a mirror ball in the middle.
        void BuildDisco(GameObject root, TrackLayout.Turntable t)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var tri = new List<int>();
            float r = t.radius, cell = 1.6f;
            Disc(Vector2.zero, r * 0.93f, DanceDark, v, c, tri);
            int n = Mathf.CeilToInt(r / cell);
            var squares = new List<Vector3>(); var colours = new List<Color>(); var squareTris = new List<int>();
            for (int y = -n; y < n; y++)
                for (int x = -n; x < n; x++)
                {
                    Vector2 min = new(x * cell + 0.08f, y * cell + 0.08f), max = new((x + 1) * cell - 0.08f, (y + 1) * cell - 0.08f);
                    if (((min + max) * 0.5f).magnitude > r * 0.86f) continue;
                    Quad(min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y), DanceDark, squares, colours, squareTris);
                }
            var floor = Mesh("DiscoFloor", squares, colours, squareTris);
            var under = Mesh("DiscoUnder", v, c, tri);
            v = new List<Vector3>(); c = new List<Color>(); tri = new List<int>();
            const int segments = 48;
            for (int k = 0; k < segments; k++)
            {
                float a0 = k * Mathf.PI * 2f / segments, a1 = a0 + Mathf.PI * 2f / segments;
                Wedge(Vector2.zero, r * 0.93f, r, a0, a1, k % 2 == 0 ? Chrome : ChromeDark, v, c, tri);
            }
            for (int k = 0; k < Segments * 2; k++)
            {
                float a = (k + 0.5f) * Mathf.PI / Segments;
                Disc(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.965f, r * 0.022f + 0.08f, Bulb, v, c, tri);
            }
            // The mirror ball: a silver disc chequered with facets.
            Disc(Vector2.zero, 1.25f, ChromeDark, v, c, tri);
            for (int y = -3; y < 3; y++)
                for (int x = -3; x < 3; x++)
                {
                    Vector2 min = new(x * 0.4f + 0.03f, y * 0.4f + 0.03f), max = new((x + 1) * 0.4f - 0.03f, (y + 1) * 0.4f - 0.03f);
                    if (((min + max) * 0.5f).magnitude > 1.1f) continue;
                    Quad(min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y), (x + y) % 2 == 0 ? Chrome : White, v, c, tri);
                }
            var rim = Mesh("DiscoRim", v, c, tri);
            var spin = new GameObject("Spin") { hideFlags = HideFlags.DontSave }.transform;
            spin.SetParent(root.transform, false);
            Part(spin, "Under", under, FloorOrder);
            Part(spin, "Floor", floor, FloorOrder);
            Part(spin, "Rim", rim, RimOrder);
            _tables.Add((t, spin));
            _discos.Add((floor, colours.ToArray()));
        }

        // The squares light up to the beat (twice a second), each its own colour.
        void Relight(float time)
        {
            int beat = Mathf.FloorToInt(time * 2f);
            if (beat == _beat || _discos.Count == 0) return;
            _beat = beat;
            foreach (var (mesh, colours) in _discos)
            {
                if (mesh == null) continue;
                for (int q = 0; q < colours.Length / 4; q++)
                {
                    uint h = (uint)(q * 73856093) ^ (uint)(beat * 19349663);
                    h = (h ^ (h >> 13)) * 1274126177u;
                    float roll = (h & 0xFFFF) / 65535f;
                    Color lit = DiscoColours[(int)((h >> 16) % (uint)DiscoColours.Length)];
                    Color col = roll < 0.55f ? Color.Lerp(DanceDark, lit, 0.95f) : Color.Lerp(DanceDark, lit, 0.18f);
                    for (int k = 0; k < 4; k++) colours[q * 4 + k] = col;
                }
                mesh.SetColors(colours);
            }
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
            Relight(time);
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
