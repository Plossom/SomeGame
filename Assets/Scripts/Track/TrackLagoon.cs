using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>The harbour scenery sheet (16 cells of 256 px, 4 x 4, row 0 at the bottom), shared by the art and the race.</summary>
    public static class HarbourSheet
    {
        public static readonly string[] Cells =
        {
            "HouseRed", "HouseBlue", "HouseYellow", "HouseTeal",
            "Containers", "Crates", "Bollards", "Barrels",
            "Car", "People", "Nets", "FishStall",
            "Palm", "Cafe", "Seal", "Lifebuoys",
        };

        public static Rect Cell(string name)
        {
            int i = System.Array.IndexOf(Cells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) * 0.25f, 0.25f, 0.25f);
        }
    }

    /// <summary>The lagoon scenery sheet (16 cells of 256 px, 4 x 4, row 0 at the bottom), shared by the art and the race.</summary>
    public static class LagoonSheet
    {
        public static readonly string[] Cells =
        {
            "Palm", "PalmTall", "PalmPair", "Bush",
            "Hut", "Umbrella", "Rock", "Flowers",
            "Coral", "CoralPink", "Seaweed", "Turtle",
            "Lighthouse", "BeachBar", "Canoe", "Flamingos",
        };

        public static Rect Cell(string name)
        {
            int i = System.Array.IndexOf(Cells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) * 0.25f, 0.25f, 0.25f);
        }

        /// <summary>Whether a point is on (or within <paramref name="margin"/> of) one of the layout's islands (their rough shores).</summary>
        public static bool OnIsland(TrackLayout layout, Vector2 p, float margin)
        {
            foreach (var island in layout.lagoonIslands)
            {
                var pts = island.points;
                if (pts == null || pts.Length < 3) continue;
                Vector2 centre = Vector2.zero;
                foreach (var q in pts) centre += q;
                centre /= pts.Length;
                // Grow the rough shore by the margin (and a little for the smoothing), then test.
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    Vector2 a = pts[i] + (pts[i] - centre).normalized * (margin + 1.5f), b = pts[j] + (pts[j] - centre).normalized * (margin + 1.5f);
                    if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                if (inside) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The Water Cup's lagoon (<see cref="TrackLayout.Biome.Lagoon"/>): islands (white sand round jungle, palms and
    /// huts, a halo of pale shallows and foam round the shore; boats bump off them), swells rolling along the
    /// channel (catch one from behind at speed and it throws you into the air: WAVE JUMP!), currents (streaks of
    /// fast water that carry a boat along), dolphins leaping across, and wooden footbridges you race under.
    /// </summary>
    public class TrackLagoon : TrackDerivedBehaviour
    {
        [SerializeField] Material sand, jungle, shallows, foam, plain, current, scenery;
        [SerializeField] Sprite dolphin, bridge, ring;
        [Tooltip("Gulls gliding over the lagoon (their shadows on the water), and shoals of little fish under it.")]
        [SerializeField] Sprite gull, shoal;
        [Tooltip("What lies about on a sandbar: a sunbather, a sandcastle, crabs.")]
        [SerializeField] Sprite sunbather, sandcastle, crab;
        [Tooltip("Quays (harbour races): their paving and the harbour scenery sheet.")]
        [SerializeField] Material quay, harbourScenery;
        [SerializeField] Sprite barge;

        const int HaloOrder = -69, FoamOrder = -68, SandOrder = -63, JungleOrder = -62, DecorOrder = 4,
                  CurrentOrder = -46, SwellOrder = -45, BarUnder = -66, PropOrder = 5, SplashOrder = 3, BridgeShadowOrder = 6, BridgeOrder = 26, DolphinOrder = 15;

        static readonly Color SwellDark = new(0.02f, 0.25f, 0.45f, 1f);

        readonly List<GameObject> _parts = new();
        // Each swell zone rolls a train of crests, one every period: (the zone, its crests' meshes).
        readonly List<(TrackLayout.Swell swell, List<(MeshFilter filter, Mesh mesh)> crests)> _swells = new();
        readonly Dictionary<(int, SomeGame.Car.CarMovement), float> _behind = new(); // (swell, car) → signed distance to the crest last frame
        readonly List<(SpriteRenderer body, SpriteRenderer shadow)> _dolphins = new();
        readonly List<(SpriteRenderer sr, float born)> _splashes = new();
        SomeGame.Car.CarMovement[] _cars = { };
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.Race.RaceManager _race;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _swells.Clear(); _dolphins.Clear(); _splashes.Clear(); _behind.Clear(); _shores.Clear(); _crabs.Clear();
            if (layout.biome != TrackLayout.Biome.Lagoon) return;
            for (int i = 0; i < layout.lagoonIslands.Count; i++) BuildIsland(layout.lagoonIslands[i], i);
            foreach (var c in layout.currents) BuildCurrent(path, c);
            foreach (var s in layout.swells)
            {
                float duration = Mathf.Repeat(s.to - s.from, path.Length) / s.speed;
                int count = Mathf.CeilToInt(duration / s.period) + 1;
                var crests = new List<(MeshFilter, Mesh)>();
                for (int k = 0; k < count; k++)
                {
                    var go = Part("Swell", plain, SwellOrder, out var filter);
                    var mesh = new Mesh { name = "Swell", hideFlags = HideFlags.DontSave };
                    mesh.MarkDynamic();
                    filter.sharedMesh = mesh;
                    Assign(null, mesh);
                    crests.Add((filter, mesh));
                    go.SetActive(false);
                }
                _swells.Add((s, crests));
            }
            foreach (float d in layout.footbridges) BuildBridge(path, d);
            foreach (var gap in layout.gaps) if (!gap.hidden) { if (gap.barge) BuildBarge(path, gap); else if (!gap.drop) BuildSandbar(path, gap); }
            if (Application.isPlaying)
            {
                _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
                _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
                _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            }
        }

        GameObject Part(string name, Material material, int order, out MeshFilter filter)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            filter = go.GetComponent<MeshFilter>();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            _parts.Add(go);
            return go;
        }

        void Mesh(string name, Material material, int order, Mesh mesh)
        {
            Part(name, material, order, out var filter);
            Assign(filter, mesh);
        }

        // ------------------------------------------------------------------ islands

        void BuildIsland(TrackLayout.Island island, int index)
        {
            if (island.points == null || island.points.Length < 3) return;
            var shore = Smooth(island.points, 8);
            if (SignedArea(shore) < 0f) shore.Reverse();
            _shores.Add(shore);
            if (island.kind == TrackLayout.IslandKind.Quay) { BuildQuay(island, shore, index); return; }
            if (island.kind == TrackLayout.IslandKind.Cliff) { BuildCliff(island, shore, index); return; }
            // Pale shallows fading out round the shore, then a ring of foam, the sand, the jungle (or rock) inside.
            Mesh("IslandHalo", shallows, HaloOrder, Ring(shore, 0f, 5f, new Color(1f, 1f, 1f, 0.9f), new Color(1f, 1f, 1f, 0f), 0.25f));
            Mesh("IslandFoam", foam, FoamOrder, Ring(shore, -0.4f, 1.4f, Color.white, Color.white, 1f, true));
            // The inside (jungle, rock or sand) fills the whole shore; a beach strip runs round on top of it, its inner
            // edge fading into the inside (no inner outline to work out, so tight bays never tangle).
            var inner = Offset(shore, -island.beach);
            bool green = island.kind == TrackLayout.IslandKind.Jungle;
            if (green) Mesh("IslandJungle", jungle, SandOrder, Fill(shore, Color.white, 1f / 4f));
            else if (island.kind == TrackLayout.IslandKind.Rock) Mesh("IslandRock", plain, SandOrder, Fill(shore, new Color(0.5f, 0.53f, 0.55f), 1f));
            else Mesh("IslandSand", sand, SandOrder, Fill(shore, Color.white, 1f / 4f));
            if (island.kind != TrackLayout.IslandKind.Sand)
                Mesh("IslandBeach", sand, JungleOrder, Ring(shore, -island.beach, 0f, new Color(1f, 1f, 1f, 0f), Color.white, 1f / 4f));
            BuildDecor(island, shore, green ? inner : Offset(shore, -1.2f), index);
            if (Application.isPlaying)
            {
                var go = new GameObject("IslandShore", typeof(PolygonCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                var points = Offset(shore, -0.5f);
                go.GetComponent<PolygonCollider2D>().points = points.ToArray();
                _parts.Add(go);
            }
        }

        // A cliff on a stormy coast: dark wet rock dropping into the sea (a deep shadow on the water at its foot, surf
        // breaking along it), rough grass on top running out to the rock at the edge.
        void BuildCliff(TrackLayout.Island island, List<Vector2> shore, int index)
        {
            Mesh("CliffShadow", plain, HaloOrder, Ring(shore, 0f, 4f, new Color(0f, 0.04f, 0.08f, 0.55f), new Color(0f, 0.04f, 0.08f, 0f), 1f));
            Mesh("CliffSurf", foam, FoamOrder, Ring(shore, -0.3f, 1.6f, Color.white, Color.white, 1f, true));
            Mesh("CliffTop", jungle, SandOrder, Fill(shore, new Color(0.62f, 0.72f, 0.6f), 1f / 4f));
            Mesh("CliffRock", plain, JungleOrder, Ring(shore, -island.beach, 0f, new Color(0.25f, 0.27f, 0.3f, 0f), new Color(0.25f, 0.27f, 0.3f), 1f));
            Mesh("CliffEdge", plain, JungleOrder + 1, Ring(shore, -1.1f, 0f, new Color(0.17f, 0.18f, 0.21f), new Color(0.12f, 0.13f, 0.15f), 1f));
            BuildDecor(island, shore, Offset(shore, -1.2f), index);
            if (Application.isPlaying)
            {
                var go = new GameObject("CliffShore", typeof(PolygonCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.GetComponent<PolygonCollider2D>().points = Offset(shore, -0.5f).ToArray();
                _parts.Add(go);
            }
        }

        // A quay: stone paving to a hard edge, a dark shadow on the water along the wall, tyre fenders hanging down,
        // bollards along the edge, harbour houses along the waterfront (their balconies to the water) and the
        // harbour's bits and pieces behind.
        void BuildQuay(TrackLayout.Island island, List<Vector2> shore, int index)
        {
            Mesh("QuayShadow", plain, HaloOrder, Ring(shore, 0f, 2.6f, new Color(0.02f, 0.2f, 0.28f, 0.45f), new Color(0.02f, 0.2f, 0.28f, 0f), 1f));
            Mesh("QuayFloor", quay != null ? quay : plain, SandOrder, Fill(shore, Color.white, 1f / 4f));
            Mesh("QuayEdge", plain, JungleOrder, Ring(shore, -0.9f, 0f, new Color(0.55f, 0.52f, 0.47f), new Color(0.42f, 0.4f, 0.36f), 1f));
            // Tyre fenders on the wall (dark rounded blocks just over the water).
            var fv = new List<Vector3>(); var fu = new List<Vector2>(); var ft = new List<int>(); var fc = new List<Color32>();
            float walked = 0f;
            for (int i = 0; i < shore.Count; i++)
            {
                var a = shore[i]; var b = shore[(i + 1) % shore.Count];
                float seg = Vector2.Distance(a, b);
                walked += seg;
                if (walked < 5f) continue;
                walked = 0f;
                Vector2 n = OutwardNormal(shore, i), t = new(-n.y, n.x);
                TrackMeshes.AddQuad(fv, fu, ft, a + n * 0.25f, t * 0.45f, n * 0.3f, Vector2.one);
                for (int k = 0; k < 4; k++) fc.Add(new Color32(34, 36, 40, 255));
            }
            Mesh("QuayFenders", plain, JungleOrder + 1, TrackMeshes.Quads("QuayFenders", fv, fu, ft, fc));
            if (harbourScenery != null)
            {
                var random = new System.Random(island.seed * 7919 + index * 104729 + 31);
                float R(float a, float b) => a + (float)random.NextDouble() * (b - a);
                var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>(); var tints = new List<Color32>();
                var placed = new List<(Vector2 p, float r)>();
                void Put(string cell, Vector2 p, float size, float angle)
                {
                    Vector2 r = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * 0.5f;
                    int first = vertices.Count;
                    TrackMeshes.AddQuad(vertices, uvs, triangles, p, r, new Vector2(-r.y, r.x), Vector2.one);
                    var uv = HarbourSheet.Cell(cell);
                    for (int k = first; k < uvs.Count; k++) uvs[k] = new Vector2(uv.x + uvs[k].x * uv.width, uv.y + uvs[k].y * uv.height);
                    for (int k = 0; k < 4; k++) tints.Add(new Color32(255, 255, 255, 255));
                    placed.Add((p, size * 0.4f));
                }
                bool Free(Vector2 p, float r)
                {
                    foreach (var (q, s) in placed) if ((q - p).sqrMagnitude < (r + s) * (r + s)) return false;
                    return true;
                }
                // Bollards along the edge.
                walked = 0f;
                for (int i = 0; i < shore.Count; i++)
                {
                    walked += Vector2.Distance(shore[i], shore[(i + 1) % shore.Count]);
                    if (walked < 9f) continue;
                    walked = 0f;
                    Vector2 n = OutwardNormal(shore, i);
                    Put("Bollards", shore[i] - n * 1.4f, 2.2f, Mathf.Atan2(n.y, n.x) + Mathf.PI * 0.5f);
                }
                // Houses along the waterfront, balconies to the water.
                string[] houses = { "HouseRed", "HouseBlue", "HouseYellow", "HouseTeal" };
                walked = 0f;
                int h = 0;
                for (int i = 0; i < shore.Count && h < island.huts; i++)
                {
                    walked += Vector2.Distance(shore[i], shore[(i + 1) % shore.Count]);
                    if (walked < 9.5f) continue;
                    Vector2 n = OutwardNormal(shore, i);
                    var p = shore[i] - n * 6f;
                    if (!Inside(shore, p) || !Inside(shore, p - n * 4f) || !Free(p, 4f)) continue;
                    walked = 0f;
                    Put(houses[random.Next(houses.Length)], p, R(8.5f, 9.5f), Mathf.Atan2(n.y, n.x) + Mathf.PI * 0.5f);
                    h++;
                }
                // The harbour's bits and pieces on the paving.
                string[] bits = { "Crates", "Barrels", "Containers", "People", "Nets", "FishStall", "Cafe", "Car", "People", "Lifebuoys", "Palm" };
                var bounds = BoundsOf(shore);
                for (int n = 0, tries = 0; n < island.palms && tries < island.palms * 60; tries++)
                {
                    var p = new Vector2(R(bounds.xMin, bounds.xMax), R(bounds.yMin, bounds.yMax));
                    if (!Inside(shore, p)) continue;
                    string cell = bits[random.Next(bits.Length)];
                    float size = cell is "Containers" ? R(8f, 9f) : cell is "Palm" ? R(6f, 7.5f) : R(4f, 5.5f);
                    bool inner = true;
                    for (int k = 0; k < 8 && inner; k++)
                    {
                        float a = k * Mathf.PI / 4f;
                        inner = Inside(shore, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size * 0.5f);
                    }
                    if (!inner || !Free(p, size * 0.4f)) continue;
                    Put(cell, p, size, cell is "Containers" or "Car" ? random.Next(4) * Mathf.PI * 0.5f : R(0f, Mathf.PI * 2f));
                    n++;
                }
                Mesh("QuayDecor", harbourScenery, DecorOrder, TrackMeshes.Quads("QuayDecor", vertices, uvs, triangles, tints));
            }
            if (Application.isPlaying)
            {
                var go = new GameObject("QuayWall", typeof(PolygonCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.GetComponent<PolygonCollider2D>().points = shore.ToArray();
                _parts.Add(go);
            }
        }

        void BuildDecor(TrackLayout.Island island, List<Vector2> shore, List<Vector2> inside, int index)
        {
            if (scenery == null) return;
            var random = new System.Random(island.seed * 7919 + index * 104729 + 17);
            float R(float a, float b) => a + (float)random.NextDouble() * (b - a);
            var bounds = BoundsOf(inside);
            var placed = new List<(Vector2 p, float r)>();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var tints = new List<Color32>();
            void Put(string cell, Vector2 p, float size, float angle)
            {
                Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * 0.5f;
                int first = vertices.Count;
                TrackMeshes.AddQuad(vertices, uvs, triangles, p, right, new Vector2(-right.y, right.x), Vector2.one);
                var uv = LagoonSheet.Cell(cell);
                for (int k = first; k < uvs.Count; k++) uvs[k] = new Vector2(uv.x + uvs[k].x * uv.width, uv.y + uvs[k].y * uv.height);
                for (int k = 0; k < 4; k++) tints.Add(new Color32(255, 255, 255, 255));
                placed.Add((p, size * 0.35f));
            }
            bool Free(Vector2 p, float r)
            {
                foreach (var (q, s) in placed) if ((q - p).sqrMagnitude < (r + s) * (r + s)) return false;
                return true;
            }
            // Huts near the beach (on the sand, facing out), then palms and bushes inside.
            for (int h = 0, tries = 0; h < island.huts && tries < 200; tries++)
            {
                int i = random.Next(shore.Count);
                Vector2 normal = OutwardNormal(shore, i);
                Vector2 p = shore[i] - normal * (island.beach * 0.55f);
                if (!Free(p, 2.4f)) continue;
                Put(random.Next(3) == 0 ? "BeachBar" : random.Next(2) == 0 ? "Umbrella" : "Hut", p, R(5.5f, 7f), Mathf.Atan2(normal.y, normal.x) - Mathf.PI * 0.5f);
                h++;
            }
            string[] green = island.kind is TrackLayout.IslandKind.Rock or TrackLayout.IslandKind.Cliff ? new[] { "Rock", "Bush", "Rock" } : new[] { "Palm", "PalmTall", "PalmPair", "Palm", "Bush", "Flowers" };
            for (int n = 0, tries = 0; n < island.palms && tries < island.palms * 60; tries++)
            {
                var p = new Vector2(R(bounds.xMin, bounds.xMax), R(bounds.yMin, bounds.yMax));
                if (!Inside(inside, p)) continue;
                string cell = green[random.Next(green.Length)];
                float size = cell is "Bush" or "Flowers" ? R(3f, 4.2f) : R(5.5f, 7.5f);
                if (!Free(p, size * 0.3f)) continue;
                Put(cell, p, size, R(0f, Mathf.PI * 2f));
                n++;
            }
            if (vertices.Count == 0) return;
            Mesh("IslandDecor", scenery, DecorOrder, TrackMeshes.Quads("IslandDecor", vertices, uvs, triangles, tints));
        }

        // ------------------------------------------------------------------ currents and bridges

        void BuildCurrent(TrackPath path, TrackLayout.Current c)
        {
            float length = Mathf.Repeat(c.to - c.from, path.Length);
            if (length < 2f) return;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            int steps = Mathf.CeilToInt(length / 1f);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps, d = c.from + length * t;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                float fade = Mathf.Clamp01(Mathf.Min(t, 1f - t) * 6f);
                var col = new Color(0.75f, 0.95f, 1f, fade); // (a band of brighter, livelier water with the streaks)
                vertices.Add(p + n * (c.lateral - c.width * 0.5f));
                vertices.Add(p + n * (c.lateral + c.width * 0.5f));
                uvs.Add(new Vector2(0f, d / 5f));
                uvs.Add(new Vector2(1f, d / 5f));
                colors.Add(col); colors.Add(col);
                if (i == steps) break;
                int a = i * 2;
                triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
            }
            Mesh("Current", current, CurrentOrder, TrackMeshes.Quads("Current", vertices, uvs, triangles, colors));
        }

        readonly List<List<Vector2>> _shores = new();

        bool OnLand(Vector2 p)
        {
            foreach (var shore in _shores) if (Inside(shore, p)) return true;
            return false;
        }

        // How far out from the centre line the land begins on one side (+1 left, -1 right): the first island shore.
        float ShoreReach(TrackPath path, float d, float side, float max = 40f)
        {
            Vector2 p = path.PointAt(d), n = path.NormalAt(d) * side;
            for (float r = track.OffRoadAt(d); r < max; r += 0.4f)
                if (OnLand(p + n * r)) return r;
            return track.OffRoadAt(d) + 6f;
        }

        // A sandbar across the channel from island to island (a trick jump over it).
        /// <summary>A barge loaded with containers lying across the channel: jump it off the kicker (its ends are solid).</summary>
        void BuildBarge(TrackPath path, TrackLayout.Gap gap)
        {
            float len = Mathf.Repeat(gap.to - gap.from, path.Length);
            float mid = gap.from + len * 0.5f;
            Vector2 c = path.PointAt(mid), across = path.NormalAt(mid);
            float road = track.OffRoadAt(mid);
            float reach = road + 3.4f;
            if (barge != null)
            {
                var sr = NewSprite("Barge", barge, PropOrder);
                sr.transform.position = c;
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg - 90f);
                sr.transform.localScale = Vector3.one * (reach * 2f / Mathf.Max(0.01f, barge.bounds.size.y));
                var shadow = NewSprite("BargeShadow", barge, HaloOrder + 2);
                shadow.transform.position = c + new Vector2(0.6f, -0.8f);
                shadow.transform.rotation = sr.transform.rotation;
                shadow.transform.localScale = sr.transform.localScale;
                shadow.color = new Color(0f, 0.12f, 0.2f, 0.35f);
            }
            if (!Application.isPlaying) return;
            // Beside the channel its ends are solid: no going round it.
            float halfAlong = Mathf.Max(2f, len * 0.5f - 1.5f);
            foreach (float side in new[] { 1f, -1f })
            {
                var go = new GameObject("BargeEnd", typeof(BoxCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.transform.position = c + across * side * (road + 0.3f + (reach + 3f - road) * 0.5f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg);
                go.GetComponent<BoxCollider2D>().size = new Vector2(reach + 3f - road, halfAlong * 2f);
                _parts.Add(go);
            }
        }

        void BuildSandbar(TrackPath path, TrackLayout.Gap gap)
        {
            // Laid out across the channel at the middle of the gap: "a" across (to the left island +), "b" along.
            float len = Mathf.Repeat(gap.to - gap.from, path.Length);
            float mid = gap.from + len * 0.5f;
            Vector2 c = path.PointAt(mid), across = path.NormalAt(mid), along = path.TangentAt(mid);
            float reachL = ShoreReach(path, mid, 1f), reachR = ShoreReach(path, mid, -1f);
            float road = track.OffRoadAt(mid) + 0.4f;
            float half = Mathf.Max(3f, len * 0.5f - 1.2f);
            // Its half width along the channel: the gap's over the water, flaring out into the islands' shores in
            // smooth curves (no corners where it meets them).
            float Width(float a)
            {
                float reach = a >= 0f ? reachL : reachR, x = Mathf.Abs(a);
                return half + 7f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(road + 0.5f, reach + 1.5f, x));
            }
            var outline = new List<Vector2>();
            const int steps = 24;
            float from = -(reachR + 7f), to = reachL + 7f;
            for (int k = 0; k <= steps; k++) { float a = Mathf.Lerp(from, to, k / (float)steps); outline.Add(c + across * a + along * Width(a)); }
            for (int k = steps; k >= 0; k--) { float a = Mathf.Lerp(from, to, k / (float)steps); outline.Add(c + across * a - along * Width(a)); }
            var shore = Smooth(outline.ToArray(), 3);
            if (SignedArea(shore) < 0f) shore.Reverse();
            // Like an island: shallows and foam round it, sand, green in the middle fading out into the sand. Its sand
            // lies under the islands (they keep their own look), its green over their beaches (it runs on into their
            // jungle without a seam).
            Mesh("SandbarHalo", shallows, BarUnder - 4, Ring(shore, 0f, 4.5f, new Color(1f, 1f, 1f, 0.9f), new Color(1f, 1f, 1f, 0f), 0.25f));
            Mesh("SandbarFoam", foam, BarUnder - 1, Ring(shore, -0.4f, 1.3f, Color.white, Color.white, 1f, true));
            Mesh("Sandbar", sand, BarUnder, Fill(shore, Color.white, 1f / 4f));
            // The green: the same shape, narrower all round (built the same way, so it never tangles).
            var greenOutline = new List<Vector2>();
            for (int k = 0; k <= steps; k++) { float a = Mathf.Lerp(from + 2f, to - 2f, k / (float)steps); greenOutline.Add(c + across * a + along * Mathf.Max(1f, Width(a) - 3f)); }
            for (int k = steps; k >= 0; k--) { float a = Mathf.Lerp(from + 2f, to - 2f, k / (float)steps); greenOutline.Add(c + across * a - along * Mathf.Max(1f, Width(a) - 3f)); }
            var green = Smooth(greenOutline.ToArray(), 3);
            if (SignedArea(green) < 0f) green.Reverse();
            Mesh("SandbarGrass", jungle, JungleOrder + 1, Fill(green, Color.white, 1f / 4f));
            Mesh("SandbarGrassEdge", jungle, JungleOrder + 1, Ring(green, 0f, 2.2f, Color.white, new Color(1f, 1f, 1f, 0f), 0.25f));
            // Beside the channel it is solid ground: no going round the ramp. (Over the channel the gap decides:
            // jump it, or run aground and be carried back.)
            if (Application.isPlaying)
                foreach (float side in new[] { 1f, -1f })
                {
                    var solid = new List<Vector2>();
                    float a0 = road + 0.3f, a1 = (side > 0f ? reachL : reachR) + 6f;
                    for (int k = 0; k <= 8; k++) { float a = Mathf.Lerp(a0, a1, k / 8f) * side; solid.Add(c + across * a + along * (Width(a) - 0.6f)); }
                    for (int k = 8; k >= 0; k--) { float a = Mathf.Lerp(a0, a1, k / 8f) * side; solid.Add(c + across * a - along * (Width(a) - 0.6f)); }
                    var go = new GameObject("SandbarShore", typeof(PolygonCollider2D)) { hideFlags = HideFlags.DontSave };
                    go.transform.SetParent(transform, false);
                    go.GetComponent<PolygonCollider2D>().points = solid.ToArray();
                    _parts.Add(go);
                }

            // What is on it: bushes and a palm on the ridge, a sunbather, a sandcastle, flamingos paddling at its edge.
            if (scenery != null)
            {
                var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>(); var tints = new List<Color32>();
                void Put(Vector2 at, string cell, float size, float angle)
                {
                    Vector2 r = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * 0.5f;
                    int first = vertices.Count;
                    TrackMeshes.AddQuad(vertices, uvs, triangles, at, r, new Vector2(-r.y, r.x), Vector2.one);
                    var uv = LagoonSheet.Cell(cell);
                    for (int k = first; k < uvs.Count; k++) uvs[k] = new Vector2(uv.x + uvs[k].x * uv.width, uv.y + uvs[k].y * uv.height);
                    for (int k = 0; k < 4; k++) tints.Add(new Color32(255, 255, 255, 255));
                }
                Put(c + across * (reachL * 0.45f), "Bush", 3f, 0.3f);
                Put(c - across * (reachR * 0.5f), "Bush", 2.6f, 1.2f);
                Put(c + across * (reachL * 0.15f), "Flowers", 2.4f, 0f);
                Put(c - across * (reachR * 0.2f), "PalmTall", 5.5f, 0.8f);
                Put(c + across * (reachL * 0.7f) + along * 3.2f, "Umbrella", 4.5f, -0.4f);
                Put(c - across * (reachR * 0.75f) - along * 3.6f, "Flamingos", 4.5f, 2f);
                Mesh("SandbarDecor", scenery, PropOrder, TrackMeshes.Quads("SandbarDecor", vertices, uvs, triangles, tints));
            }
            // A sunbather and a sandcastle (their own pictures), and crabs that scuttle to and fro.
            if (sunbather != null)
            {
                var sb = NewSprite("Sunbather", sunbather, PropOrder + 1);
                sb.transform.position = c + across * (reachL * 0.45f) - along * 3.4f;
                sb.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg);
                sb.transform.localScale = Vector3.one * 1.1f;
            }
            if (sandcastle != null)
            {
                var sc = NewSprite("Sandcastle", sandcastle, PropOrder + 1);
                sc.transform.position = c - across * (reachR * 0.35f) + along * 3.4f;
                sc.transform.localScale = Vector3.one * 0.9f;
            }
            if (crab != null)
                for (int k = 0; k < 2; k++)
                {
                    var cr = NewSprite("Crab", crab, PropOrder + 1);
                    cr.transform.localScale = Vector3.one * 0.55f;
                    _crabs.Add((cr.transform, c + along * (k == 0 ? 3.8f : -3.8f), across, (k == 0 ? reachL : reachR) * 0.6f, k * 2.3f));
                }
        }

        readonly List<(Transform t, Vector2 centre, Vector2 across, float range, float phase)> _crabs = new();

        void BuildBridge(TrackPath path, float d)
        {
            if (bridge == null) return;
            Vector2 t = path.TangentAt(d), n = path.NormalAt(d);
            // From shore to shore: as far out as the land on each side, a little onto it.
            float left = ShoreReach(path, d, 1f) + 3f, right = ShoreReach(path, d, -1f) + 3f;
            float length = left + right;
            Vector2 p = path.PointAt(d) + n * ((left - right) * 0.5f);
            float angle = Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg; // the planks run across the channel
            var size = bridge.bounds.size;
            for (int k = 0; k < 2; k++)
            {
                var go = new GameObject(k == 0 ? "BridgeShadow" : "Bridge", typeof(SpriteRenderer)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.transform.position = (Vector3)(p + (k == 0 ? new Vector2(0.6f, -1f) : Vector2.zero));
                go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                go.transform.localScale = new Vector3(3.2f / size.x, length / size.y, 1f);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = bridge;
                if (plain != null) sr.sharedMaterial = plain;
                sr.sortingOrder = k == 0 ? BridgeShadowOrder : BridgeOrder;
                if (k == 0) sr.color = new Color(0f, 0.12f, 0.2f, 0.35f);
                _parts.Add(go);
            }
        }

        // ------------------------------------------------------------------ live: swells, currents, dolphins

        void Update()
        {
            if (!Application.isPlaying || track == null || track.Layout == null || track.Layout.biome != TrackLayout.Biome.Lagoon) return;
            var path = track.Path;
            float now = Track.Clock;
            for (int s = 0; s < _swells.Count; s++) UpdateSwell(s, path, now);
            UpdateCurrents(path);
            UpdateDolphins(path, now);
            UpdateSplashes(now);
            UpdateLife(path, now);
            // Crabs scuttle sideways along the sandbar, stop, scuttle back.
            foreach (var (t, centre, across, range, phase) in _crabs)
            {
                if (t == null) continue;
                float u = Mathf.Sin(now * 0.7f + phase);
                float x = Mathf.Sign(u) * Mathf.Pow(Mathf.Abs(u), 0.6f) * range;
                t.position = centre + across * x;
                t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg + 90f + Mathf.Sin(now * 14f) * 6f);
            }
        }

        // ------------------------------------------------------------------ gulls and shoals

        readonly List<(SpriteRenderer body, SpriteRenderer shadow, float phase, float lap)> _gulls = new();
        readonly List<(SpriteRenderer sr, float phase, float lateral, float speed)> _shoals = new();

        void UpdateLife(TrackPath path, float now)
        {
            if (gull != null && _gulls.Count == 0)
                for (int i = 0; i < 5; i++)
                {
                    var body = NewSprite("Gull", gull, 40);
                    var shadow = NewSprite("GullShadow", gull, 5);
                    shadow.color = new Color(0f, 0.12f, 0.2f, 0.25f);
                    _gulls.Add((body, shadow, i * 1.37f, path.Length * i / 5f));
                }
            if (shoal != null && _shoals.Count == 0)
                for (int i = 0; i < 9; i++)
                {
                    var sr = NewSprite("Shoal", shoal, -44);
                    _shoals.Add((sr, path.Length * i / 9f, (i % 2 == 0 ? 1f : -1f) * (2f + i % 3), 2.5f + (i % 4) * 0.7f));
                }
            // Gulls circle lazily along the lap, high over the water, wings beating now and then.
            foreach (var (body, shadow, phase, lap) in _gulls)
            {
                float d = lap + now * 6f;
                Vector2 c = path.PointAt(d) + path.NormalAt(d) * (Mathf.Sin(now * 0.3f + phase) * 14f);
                Vector2 dir = path.TangentAt(d) + path.NormalAt(d) * Mathf.Cos(now * 0.3f + phase) * 0.5f;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                float flap = Mathf.Repeat(now + phase, 3f) < 0.8f ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(now * 9f)) : 1f;
                body.transform.position = c;
                body.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                body.transform.localScale = new Vector3(flap, 1f, 1f) * 1.3f;
                shadow.transform.position = c + new Vector2(2.5f, -4f);
                shadow.transform.rotation = body.transform.rotation;
                shadow.transform.localScale = body.transform.localScale * 0.9f;
            }
            // Shoals of fish darting along under the water beside the channel.
            foreach (var (sr, phase, lateral, speed) in _shoals)
            {
                float d = phase + now * speed;
                float w = track.OffRoadAt(d) + 2.5f + Mathf.Abs(lateral);
                Vector2 p = path.PointAt(d) + path.NormalAt(d) * (Mathf.Sign(lateral) * w + Mathf.Sin(now * 0.8f + phase) * 1.5f);
                Vector2 t = path.TangentAt(d);
                sr.transform.position = p;
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg - 90f + Mathf.Sin(now * 2f + phase) * 12f);
                sr.transform.localScale = Vector3.one * 1.6f;
            }
        }

        void UpdateSwell(int index, TrackPath path, float now)
        {
            var (swell, crests) = _swells[index];
            float length = Mathf.Repeat(swell.to - swell.from, path.Length);
            float duration = length / swell.speed;
            float clock = now + swell.offset;
            int newest = Mathf.FloorToInt(clock / swell.period);
            for (int slot = 0; slot < crests.Count; slot++)
            {
                // Crest number n (one born every period) uses slot n % count.
                int n = newest - ((newest - slot) % crests.Count + crests.Count) % crests.Count;
                float age = clock - n * swell.period;
                var (filter, mesh) = crests[slot];
                bool active = age >= 0f && age < duration;
                if (filter.gameObject.activeSelf != active) filter.gameObject.SetActive(active);
                if (!active) continue;
                float progress = age / duration;
                float crest = path.WrapDistance(swell.from + length * progress);
                float height = Mathf.Clamp01(progress / 0.12f) * Mathf.Clamp01((1f - progress) / 0.1f);
                BuildSwellMesh(mesh, path, crest, height, now);
                Ride(index * 1000 + (n & 0xFFFF) % 1000, swell, path, crest, height, now);
            }
        }

        // Boats crossing a crest from behind at speed are thrown up, with a boost on top.
        void Ride(int key, TrackLayout.Swell swell, TrackPath path, float crest, float height, float now)
        {
            foreach (var car in _cars)
            {
                if (car == null || !car.isActiveAndEnabled || car.IsAirborne) continue;
                var sensor = car.GetComponent<TrackSensor>();
                if (sensor == null) continue;
                var point = sensor.Current;
                float delta = path.DeltaDistance(crest, point.Distance);
                bool had = _behind.TryGetValue((key, car), out float before);
                _behind[(key, car)] = delta;
                if (!had || before >= 0f || delta < 0f || delta > 3f) continue;
                if (Mathf.Abs(point.Lateral) > track.OffRoadAt(point.Distance) + 0.5f || height < 0.4f) continue;
                if (car.ForwardSpeed < swell.speed + 2f) continue;
                car.Launch(0.55f + 0.25f * height);
                car.GiveBoost(0.6f);
                Splash(car.Body.position, 2.2f, now);
                if (_race != null && _race.Player != null && car.gameObject == _race.Player.gameObject && _combo != null)
                    _combo.Announce("WAVE JUMP!", new Color(0.35f, 0.85f, 1f));
            }
            if (_behind.Count > 2000) _behind.Clear();
        }

        // Across the channel at the crest: a dark swell rising behind, a white foaming crest line, then calm.
        void BuildSwellMesh(Mesh mesh, TrackPath path, float crest, float height, float now)
        {
            (float along, Color color)[] rows =
            {
                (-5f, new Color(SwellDark.r, SwellDark.g, SwellDark.b, 0f)),
                (-1.4f, new Color(SwellDark.r, SwellDark.g, SwellDark.b, 0.42f * height)),
                (-0.5f, new Color(0.75f, 0.95f, 1f, 0.75f * height)),
                (0f, new Color(1f, 1f, 1f, 0.95f * height)),
                (0.5f, new Color(1f, 1f, 1f, 0.55f * height)),
                (1.3f, new Color(1f, 1f, 1f, 0f)),
            };
            const int across = 12;
            var vertices = new Vector3[rows.Length * (across + 1)];
            var colors = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (int r = 0; r < rows.Length; r++)
            {
                float d = crest + rows[r].along;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                float w = track.OffRoadAt(d) + 1.5f;
                for (int k = 0; k <= across; k++)
                {
                    float lat = Mathf.Lerp(-w, w, k / (float)across);
                    // The crest isn't a ruler line: it wobbles a little along its length.
                    float wobble = Mathf.Sin(lat * 0.9f + now * 3f) * 0.25f * height;
                    int i = r * (across + 1) + k;
                    vertices[i] = p + n * lat + path.TangentAt(d) * wobble;
                    var col = rows[r].color;
                    col.a *= Mathf.Clamp01((w - Mathf.Abs(lat)) / 1.5f);
                    colors[i] = col;
                    uvs[i] = new Vector2(k / (float)across, r);
                }
            }
            var triangles = new List<int>();
            for (int r = 0; r < rows.Length - 1; r++)
                for (int k = 0; k < across; k++)
                {
                    int a = r * (across + 1) + k, b = a + across + 1;
                    triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        void UpdateCurrents(TrackPath path)
        {
            var currents = track.Layout.currents;
            foreach (var car in _cars)
            {
                if (car == null) continue;
                float push = 0f;
                var sensor = car.GetComponent<TrackSensor>();
                if (sensor != null && !car.IsAirborne)
                {
                    var point = sensor.Current;
                    foreach (var c in currents)
                    {
                        float into = path.DeltaDistance(c.from, point.Distance);
                        float span = Mathf.Repeat(c.to - c.from, path.Length);
                        if (into < 0f || into > span || Mathf.Abs(point.Lateral - c.lateral) > c.width * 0.5f + 0.4f) continue;
                        push = Mathf.Max(push, c.push);
                    }
                }
                car.CurrentPush = push;
            }
        }

        // ------------------------------------------------------------------ dolphins

        void UpdateDolphins(TrackPath path, float now)
        {
            var spots = track.Layout.dolphins;
            int needed = spots.Count * 3;
            while (_dolphins.Count < needed && dolphin != null)
            {
                var body = NewSprite("Dolphin", dolphin, DolphinOrder);
                var shadow = NewSprite("DolphinShadow", dolphin, SplashOrder);
                shadow.color = new Color(0f, 0.12f, 0.2f, 0.35f);
                _dolphins.Add((body, shadow));
            }
            for (int s = 0; s < spots.Count; s++)
            {
                float d = spots[s];
                const float period = 7.5f, leap = 1.5f;
                float phase = Mathf.Repeat(now + s * 2.9f, period);
                Vector2 n = path.NormalAt(d);
                float w = track.OffRoadAt(d) + 5f;
                for (int k = 0; k < 3; k++)
                {
                    var (body, shadow) = _dolphins[s * 3 + k];
                    float t = (phase - k * 0.22f) / leap;
                    bool show = t >= 0f && t <= 1f;
                    body.enabled = shadow.enabled = show;
                    if (!show) continue;
                    // Across the channel (left to right on even cycles, back on odd ones), in an arc.
                    int cycle = Mathf.FloorToInt((now + s * 2.9f) / period);
                    float side = cycle % 2 == 0 ? 1f : -1f;
                    Vector2 a = path.PointAt(d + k * 1.6f) + n * (w * side), b = path.PointAt(d + 3f + k * 1.6f) - n * (w * side);
                    Vector2 p = Vector2.Lerp(a, b, t);
                    float arc = Mathf.Sin(t * Mathf.PI);
                    Vector2 dir = (b - a).normalized;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    body.transform.position = p + new Vector2(-0.4f, 1.2f) * arc;
                    body.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    body.transform.localScale = Vector3.one * (0.9f + 0.45f * arc);
                    shadow.transform.position = p;
                    shadow.transform.rotation = body.transform.rotation;
                    shadow.transform.localScale = Vector3.one * 0.85f;
                    shadow.color = new Color(0f, 0.12f, 0.2f, 0.35f * (1f - arc * 0.5f));
                    if (t < 0.05f && !_splashed.Contains((s, k, cycle, 0))) { _splashed.Add((s, k, cycle, 0)); Splash(a, 1.5f, now); }
                    if (t > 0.95f && !_splashed.Contains((s, k, cycle, 1))) { _splashed.Add((s, k, cycle, 1)); Splash(b, 1.5f, now); }
                }
            }
            if (_splashed.Count > 400) _splashed.Clear();
        }

        readonly HashSet<(int, int, int, int)> _splashed = new();

        SpriteRenderer NewSprite(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (plain != null) sr.sharedMaterial = plain; // unlit, like the rest of the world
            _parts.Add(go);
            return sr;
        }

        // A ring of foam spreading out on the water.
        void Splash(Vector2 at, float size, float now)
        {
            if (ring == null) return;
            SpriteRenderer sr = null;
            for (int i = 0; i < _splashes.Count; i++)
                if (now - _splashes[i].born > 0.9f) { sr = _splashes[i].sr; _splashes.RemoveAt(i); break; }
            if (sr == null) sr = NewSprite("Splash", ring, SplashOrder);
            sr.transform.position = at;
            sr.transform.localScale = Vector3.one * size;
            sr.enabled = true;
            _splashes.Add((sr, now));
            sr.transform.localScale = Vector3.one * size * 0.3f;
            _splashSize[sr] = size;
        }

        readonly Dictionary<SpriteRenderer, float> _splashSize = new();

        void UpdateSplashes(float now)
        {
            foreach (var (sr, born) in _splashes)
            {
                if (sr == null) continue;
                float t = (now - born) / 0.9f;
                if (t >= 1f) { sr.enabled = false; continue; }
                float size = _splashSize.TryGetValue(sr, out var s) ? s : 1.5f;
                sr.transform.localScale = Vector3.one * size * Mathf.Lerp(0.3f, 1.2f, 1f - (1f - t) * (1f - t));
                sr.color = new Color(1f, 1f, 1f, 0.9f * (1f - t));
            }
        }

        // ------------------------------------------------------------------ geometry

        internal static List<Vector2> Smooth(Vector2[] points, int steps)
        {
            var result = new List<Vector2>();
            int n = points.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = points[(i - 1 + n) % n], p1 = points[i], p2 = points[(i + 1) % n], p3 = points[(i + 2) % n];
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)steps, t2 = t * t, t3 = t2 * t;
                    result.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
                }
            }
            return result;
        }

        internal static float SignedArea(List<Vector2> poly)
        {
            float a = 0f;
            for (int i = 0; i < poly.Count; i++) { var p = poly[i]; var q = poly[(i + 1) % poly.Count]; a += p.x * q.y - q.x * p.y; }
            return a * 0.5f;
        }

        static Vector2 OutwardNormal(List<Vector2> poly, int i)
        {
            Vector2 prev = poly[(i - 1 + poly.Count) % poly.Count], next = poly[(i + 1) % poly.Count];
            Vector2 t = (next - prev).normalized;
            return new Vector2(t.y, -t.x); // counter-clockwise loop: outward is to the right
        }

        internal static List<Vector2> Offset(List<Vector2> poly, float by)
        {
            var result = new List<Vector2>(poly.Count);
            for (int i = 0; i < poly.Count; i++) result.Add(poly[i] + OutwardNormal(poly, i) * by);
            return result;
        }

        static Rect BoundsOf(List<Vector2> poly)
        {
            Vector2 min = poly[0], max = poly[0];
            foreach (var p in poly) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static bool Inside(List<Vector2> poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        // A strip along the loop from offset a to offset b (outward), coloured from ca to cb across.
        internal static Mesh Ring(List<Vector2> poly, float a, float b, Color ca, Color cb, float uvAlong, bool uAcross = false)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            float along = 0f;
            for (int i = 0; i <= poly.Count; i++)
            {
                int k = i % poly.Count;
                if (i > 0) along += Vector2.Distance(poly[(i - 1) % poly.Count], poly[k]);
                Vector2 n = OutwardNormal(poly, k);
                vertices.Add(poly[k] + n * a);
                vertices.Add(poly[k] + n * b);
                uvs.Add(uAcross ? new Vector2(0f, along * uvAlong / 4f) : (poly[k] + n * a) * uvAlong);
                uvs.Add(uAcross ? new Vector2(1f, along * uvAlong / 4f) : (poly[k] + n * b) * uvAlong);
                colors.Add(ca); colors.Add(cb);
                if (i == poly.Count) break;
                int v = i * 2;
                triangles.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
            }
            return TrackMeshes.Quads("Ring", vertices, uvs, triangles, colors);
        }

        // The inside of a simple loop (ear clipping), world-mapped UVs.
        internal static Mesh Fill(List<Vector2> poly, Color color, float uvScale)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color32>();
            foreach (var p in poly) { vertices.Add(p); uvs.Add(p * uvScale); colors.Add(color); }
            var triangles = new List<int>();
            var index = new List<int>();
            for (int i = 0; i < poly.Count; i++) index.Add(i);
            int guard = poly.Count * poly.Count;
            while (index.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < index.Count; i++)
                {
                    int ia = index[(i - 1 + index.Count) % index.Count], ib = index[i], ic = index[(i + 1) % index.Count];
                    Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                    if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) <= 0f) continue; // reflex
                    bool ear = true;
                    foreach (int j in index)
                    {
                        if (j == ia || j == ib || j == ic) continue;
                        if (InTriangle(poly[j], a, b, c)) { ear = false; break; }
                    }
                    if (!ear) continue;
                    triangles.AddRange(new[] { ia, ic, ib });
                    index.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (index.Count == 3) triangles.AddRange(new[] { index[0], index[2], index[1] });
            return TrackMeshes.Quads("Fill", vertices, uvs, triangles, colors);
        }

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
    }
}
