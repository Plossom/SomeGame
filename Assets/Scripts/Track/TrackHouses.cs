using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The houses the track runs through (<see cref="TrackLayout.rooms"/>, <see cref="TrackLayout.houses"/>):
    /// every room's floor (parquet, tiles, carpet, planks), walls round every room with a doorway wherever
    /// the road crosses (a window frame where the road jumps out), solid so nobody cuts through, staircases
    /// drawn across the road, and each house's roof, which hides the rooms from outside and fades away when
    /// you drive in (Play mode). Upstairs rooms are drawn raised, with a deep shadow round them.
    /// </summary>
    public class TrackHouses : TrackDerivedBehaviour
    {
        [Tooltip("Floor materials in TrackLayout.FloorKind order (tiled).")]
        [SerializeField] Material[] floors;
        [Tooltip("Roof materials in TrackLayout.RoofKind order (tiled).")]
        [SerializeField] Material[] roofs;
        [Tooltip("Plain vertex-colour material for walls, stairs and shadows.")]
        [SerializeField] Material plain;
        [SerializeField, Min(0.5f)] float floorTile = 6f;
        [SerializeField, Min(0.5f)] float roofTile = 5f;
        [SerializeField, Min(0.2f)] float wallThickness = 1.3f;
        [SerializeField] Color wallTop = new(0.96f, 0.92f, 0.84f);
        [SerializeField] Color wallEdge = new(0.23f, 0.17f, 0.13f);
        [SerializeField] Color windowGlass = new(0.62f, 0.85f, 0.95f);

        readonly List<GameObject> _parts = new();
        class Roof { public Mesh Tiles, Ridge; public Color RidgeColor; public Rect Area; public float Alpha = 1f; }
        readonly List<Roof> _roofs = new();
        Transform _player;

        const int FloorOrder = -95, ShadowOrder = -97, WallOrder = -43, StairOrder = -47, RoofOrder = 44;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _roofs.Clear();
            if (layout.rooms == null || layout.rooms.Count == 0) return;

            // Shadows of the houses on the garden, and the upstairs rooms' deeper shadows.
            foreach (var house in layout.houses)
                Part("HouseShadow", Quad(house.min + new Vector2(1.6f, -2.2f), house.max + new Vector2(1.6f, -2.2f), new Color(0f, 0.05f, 0.02f, 0.28f)), plain, ShadowOrder);
            foreach (var room in layout.rooms)
            {
                if (room.upper) Part("UpperShadow", Quad(room.min + new Vector2(0.8f, -1.6f), room.max + new Vector2(0.8f, -1.6f), new Color(0f, 0f, 0f, 0.35f)), plain, FloorOrder - 1);
                int k = Mathf.Clamp((int)room.floor, 0, floors.Length - 1);
                Part("Floor", Floor(room.min, room.max, room.upper ? 1f : 0.93f), floors[k], FloorOrder);
            }

            // Walls round each room, broken where the road goes through.
            var walls = new List<Vector3>();
            var colors = new List<Color32>();
            var tris = new List<int>();
            var colliders = new GameObject("HouseWalls");
            colliders.transform.SetParent(transform, false);
            colliders.hideFlags = HideFlags.DontSave;
            _parts.Add(colliders);
            foreach (var room in layout.rooms)
            {
                var corners = new[] { room.min, new Vector2(room.max.x, room.min.y), room.max, new Vector2(room.min.x, room.max.y) };
                for (int e = 0; e < 4; e++)
                {
                    Vector2 a = corners[e], b = corners[(e + 1) % 4];
                    var openings = Openings(path, layout, a, b);
                    float length = Vector2.Distance(a, b);
                    float at = 0f;
                    openings.Sort((x, y) => x.along.CompareTo(y.along));
                    foreach (var (along, half, window) in openings)
                    {
                        Wall(a, b, at, Mathf.Max(at, along - half), walls, colors, tris, colliders);
                        if (window) Window(a, b, along - half, along + half, walls, colors, tris);
                        at = Mathf.Max(at, along + half);
                    }
                    Wall(a, b, at, length, walls, colors, tris, colliders);
                }
            }
            var wallMesh = new Mesh { name = "Walls" };
            wallMesh.SetVertices(walls);
            wallMesh.SetColors(colors);
            wallMesh.SetTriangles(tris, 0);
            wallMesh.RecalculateBounds();
            Part("Walls", wallMesh, plain, WallOrder);

            // Staircases: steps across the road, lighter towards the top.
            if (layout.stairs != null)
                foreach (var span in layout.stairs) Part("Stairs", Stairs(path, span), plain, StairOrder);

            // Roofs (Play mode only, fading as you come in; the editor and the track picture show the rooms).
            if (Application.isPlaying)
            foreach (var house in layout.houses)
            {
                int k = Mathf.Clamp((int)house.roof, 0, roofs.Length - 1);
                var roof = Part("Roof", Floor(house.min - Vector2.one * 0.8f, house.max + Vector2.one * 0.8f, 1f, roofTile), roofs[k], RoofOrder);
                var ridge = Part("RoofRidge", Quad(new Vector2(house.min.x - 0.8f, (house.min.y + house.max.y) * 0.5f - 0.5f),
                    new Vector2(house.max.x + 0.8f, (house.min.y + house.max.y) * 0.5f + 0.5f), new Color(0.35f, 0.12f, 0.08f, 0.9f)), plain, RoofOrder + 1);
                _roofs.Add(new Roof
                {
                    Tiles = roof.GetComponent<MeshFilter>().sharedMesh, Ridge = ridge.GetComponent<MeshFilter>().sharedMesh,
                    RidgeColor = new Color(0.35f, 0.12f, 0.08f, 0.9f), Area = Rect.MinMaxRect(house.min.x, house.min.y, house.max.x, house.max.y),
                });
            }
        }

        // Where the centre line crosses this wall: a doorway as wide as the road, a window if it is a jump.
        List<(float along, float half, bool window)> Openings(TrackPath path, TrackLayout layout, Vector2 a, Vector2 b)
        {
            var list = new List<(float, float, bool)>();
            Vector2 dir = (b - a).normalized;
            float length = Vector2.Distance(a, b);
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 p = path[i], q = path[i + 1];
                if (!Intersect(a, b, p, q, out float t, out float u)) continue;
                float d = path.DistanceAt(i) + Vector2.Distance(p, q) * u;
                float half = track.OffRoadAt(d) + 1.4f;
                // Crossing at a slant needs a wider opening.
                float slant = Mathf.Abs(Vector2.Dot((q - p).normalized, new Vector2(-dir.y, dir.x)));
                half /= Mathf.Max(0.35f, slant);
                bool window = false;
                foreach (var gap in layout.gaps)
                    if (Mathf.Abs(path.DeltaDistance(gap.from, d)) < 4f || Mathf.Abs(path.DeltaDistance(gap.to, d)) < 4f) window = true;
                list.Add((t * length, half, window));
            }
            return list;
        }

        static bool Intersect(Vector2 a, Vector2 b, Vector2 p, Vector2 q, out float t, out float u)
        {
            Vector2 r = b - a, s = q - p;
            float den = r.x * s.y - r.y * s.x;
            t = u = 0f;
            if (Mathf.Abs(den) < 1e-6f) return false;
            Vector2 ap = p - a;
            t = (ap.x * s.y - ap.y * s.x) / den;
            u = (ap.x * r.y - ap.y * r.x) / den;
            return t >= 0f && t <= 1f && u >= 0f && u < 1f;
        }

        // A wall piece from `from` to `to` along a..b: a dark edge round a light top, and a solid collider.
        void Wall(Vector2 a, Vector2 b, float from, float to, List<Vector3> v, List<Color32> c, List<int> t, GameObject colliders)
        {
            if (to - from < 0.05f) return;
            Vector2 dir = (b - a).normalized, n = new(-dir.y, dir.x);
            Vector2 p0 = a + dir * from, p1 = a + dir * to;
            float h = wallThickness * 0.5f;
            AddQuad(p0 - n * h - dir * h * 0.6f, p1 - n * h + dir * h * 0.6f, p1 + n * h + dir * h * 0.6f, p0 + n * h - dir * h * 0.6f, wallEdge, v, c, t);
            float i = h * 0.55f;
            AddQuad(p0 - n * i, p1 - n * i, p1 + n * i, p0 + n * i, wallTop, v, c, t);
            var edge = colliders.AddComponent<EdgeCollider2D>();
            edge.points = new[] { p0, p1 };
            edge.edgeRadius = h;
        }

        // A window frame where the road jumps out: glass edges at both sides and a sill.
        void Window(Vector2 a, Vector2 b, float from, float to, List<Vector3> v, List<Color32> c, List<int> t)
        {
            Vector2 dir = (b - a).normalized, n = new(-dir.y, dir.x);
            float h = wallThickness * 0.5f;
            foreach (float s in new[] { from, to })
            {
                Vector2 p = a + dir * s;
                AddQuad(p - dir * 0.9f - n * h, p + dir * 0.9f - n * h, p + dir * 0.9f + n * h, p - dir * 0.9f + n * h, wallEdge, v, c, t);
                AddQuad(p - dir * 0.5f - n * h * 0.5f, p + dir * 0.5f - n * h * 0.5f, p + dir * 0.5f + n * h * 0.5f, p - dir * 0.5f + n * h * 0.5f, windowGlass, v, c, t);
            }
            Vector2 s0 = a + dir * from, s1 = a + dir * to;
            AddQuad(s0 - n * 0.25f, s1 - n * 0.25f, s1 + n * 0.25f, s0 + n * 0.25f, new Color(0.55f, 0.36f, 0.22f), v, c, t);
        }

        Mesh Stairs(TrackPath path, TrackLayout.Span span)
        {
            var v = new List<Vector3>(); var c = new List<Color32>(); var t = new List<int>();
            float length = path.DeltaDistance(span.from, span.to);
            int steps = Mathf.Max(2, Mathf.RoundToInt(length / 1.1f));
            for (int s = 0; s < steps; s++)
            {
                float d0 = span.from + length * s / steps, d1 = span.from + length * (s + 0.82f) / steps;
                float w0 = track.OffRoadAt(d0), w1 = track.OffRoadAt(d1);
                Vector2 a0 = path.PointAt(d0), a1 = path.PointAt(d1), n0 = path.NormalAt(d0), n1 = path.NormalAt(d1);
                float k = (s + 0.5f) / steps;
                var step = Color.Lerp(new Color(0.52f, 0.33f, 0.19f), new Color(0.82f, 0.62f, 0.4f), k);
                AddQuad(a0 - n0 * w0, a1 - n1 * w1, a1 + n1 * w1, a0 + n0 * w0, step, v, c, t);
                Vector2 e0 = path.PointAt(d1), e1 = path.PointAt(span.from + length * (s + 1f) / steps);
                AddQuad(e0 - n1 * w1, e1 - n1 * w1, e1 + n1 * w1, e0 + n1 * w1, new Color(0.3f, 0.18f, 0.1f), v, c, t); // the step's edge
            }
            var mesh = new Mesh { name = "Stairs" };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            return mesh;
        }

        static void AddQuad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color, List<Vector3> v, List<Color32> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) c.Add(color);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        Mesh Floor(Vector2 min, Vector2 max, float shade, float tile = 0f)
        {
            if (tile <= 0f) tile = floorTile;
            var mesh = new Mesh { name = "Floor" };
            mesh.SetVertices(new List<Vector3> { min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y) });
            mesh.SetUVs(0, new List<Vector2> { min / tile, new Vector2(max.x, min.y) / tile, max / tile, new Vector2(min.x, max.y) / tile });
            var tint = new Color(shade, shade, shade, 1f);
            mesh.SetColors(new List<Color> { tint, tint, tint, tint });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Quad(Vector2 min, Vector2 max, Color color)
        {
            var mesh = new Mesh { name = "Quad" };
            mesh.SetVertices(new List<Vector3> { min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y) });
            mesh.SetColors(new List<Color> { color, color, color, color });
            mesh.SetUVs(0, new List<Vector2> { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        MeshRenderer Part(string name, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            _parts.Add(go);
            return r;
        }

        // The roof of the house you are in fades away (and comes back when you leave).
        void LateUpdate()
        {
            if (!Application.isPlaying || _roofs.Count == 0) return;
            if (_player == null)
            {
                var race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
                if (race != null && race.Player != null) _player = race.Player.transform;
                if (_player == null) return;
            }
            Vector2 p = _player.position;
            foreach (var roof in _roofs)
            {
                bool inside = roof.Area.Contains(p) || Grow(roof.Area, 2f).Contains(p) && InsideAnyRoom(p);
                float alpha = Mathf.MoveTowards(roof.Alpha, inside ? 0f : 1f, Time.deltaTime * 3f);
                if (Mathf.Approximately(alpha, roof.Alpha)) continue;
                roof.Alpha = alpha;
                var tiles = new Color(1f, 1f, 1f, alpha);
                roof.Tiles.SetColors(new List<Color> { tiles, tiles, tiles, tiles });
                var ridge = roof.RidgeColor; ridge.a *= alpha;
                roof.Ridge.SetColors(new List<Color> { ridge, ridge, ridge, ridge });
            }
        }

        bool InsideAnyRoom(Vector2 p)
        {
            foreach (var room in track.Layout.rooms)
                if (p.x >= room.min.x && p.x <= room.max.x && p.y >= room.min.y && p.y <= room.max.y) return true;
            return false;
        }

        static Rect Grow(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);
    }
}
