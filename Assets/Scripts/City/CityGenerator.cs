using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.City
{
    /// <summary>
    /// Builds the 3D neon city at startup from the <see cref="CityZone"/>s (one per district):
    /// street grid with neon edges, buildings with window facades and roof neon, district
    /// landmarks, harbour water, the garage, fog over locked districts, beacons and traffic lanes.
    /// Deterministic (seeded) so the city always looks the same.
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] Material wallsMaterial;
        [SerializeField] Material flatMaterial;
        [SerializeField] Material linesMaterial;
        [SerializeField] Material neonMaterial;
        [SerializeField] Material waterMaterial;
        [SerializeField] Material glowMaterial;
        [SerializeField] Material fogMaterial;

        [Header("Garage")]
        [Tooltip("Centre of the garage lot (world X, Z). The garage faces south (toward the camera).")]
        [SerializeField] Vector2 garagePosition = new(-14f, -54f);
        [SerializeField] Sprite carSprite;
        [SerializeField] Sprite glowSprite;
        [SerializeField] TMP_FontAsset signFont;

        [Header("Layout")]
        [SerializeField] int seed = 7;
        [SerializeField, Min(8f)] float blockPitch = 16f;
        [SerializeField, Min(1f)] float streetWidth = 4f;
        [Tooltip("Dark outskirts around the districts, so the city does not end in a void.")]
        [SerializeField] Rect outskirts = new(-150, -120, 300, 270);
        [SerializeField] CityTraffic traffic;

        CityMeshBatch _walls, _flat, _lines, _neon, _water, _glow;
        readonly List<Vector3> _beacons = new();
        readonly List<CityZone> _zones = new();

        static readonly Color Roof = new(0.05f, 0.063f, 0.085f);
        static readonly Color PavementColor = new(0.045f, 0.055f, 0.075f);
        static readonly Color Ground = new(0.018f, 0.024f, 0.035f);

        public IReadOnlyList<CityZone> Zones => _zones;
        public Vector3 GaragePoint => new(garagePosition.x, 0f, garagePosition.y);
        public Bounds CityBounds { get; private set; }

        void Awake() => Build();

        public void Build()
        {
            _walls = new(); _flat = new(); _lines = new(); _neon = new(); _water = new(); _glow = new();
            _beacons.Clear();
            _zones.Clear();
            _zones.AddRange(GetComponentsInChildren<CityZone>());

            _flat.Flat(-600f, -600f, 600f, 600f, -0.02f, Ground);
            var lanes = new List<(Vector3, Vector3)>();
            Bounds bounds = new(GaragePoint, Vector3.one);

            BuildOutskirts();
            foreach (var zone in _zones)
            {
                var district = zone.District;
                bool locked = district == null || !ProgressStore.IsUnlocked(district);
                var rng = new System.Random(seed * 101 + zone.Seed);
                var ctx = new ZoneContext(zone, locked, rng, district != null ? district.accent : Color.white);
                BuildZone(ctx, locked ? null : lanes);
                bounds.Encapsulate(new Vector3(zone.Area.xMin, 0f, zone.Area.yMin));
                bounds.Encapsulate(new Vector3(zone.Area.xMax, 0f, zone.Area.yMax));
                if (locked) CreateFog(zone);
            }
            BuildGarage();
            CityBounds = bounds;

            Emit("Walls", _walls, wallsMaterial);
            Emit("Flat", _flat, flatMaterial);
            Emit("Lines", _lines, linesMaterial);
            Emit("Neon", _neon, neonMaterial);
            Emit("Water", _water, waterMaterial).AddComponent<CityScroll>();
            Emit("Glow", _glow, glowMaterial);
            BuildBeacons();
            if (traffic != null) traffic.Setup(lanes);
        }

        // ------------------------------------------------------------------ zones

        sealed class ZoneContext
        {
            public readonly CityZone Zone;
            public readonly bool Locked;
            public readonly System.Random Rng;
            public readonly Color Accent;
            public readonly float Dim;

            public ZoneContext(CityZone zone, bool locked, System.Random rng, Color accent)
            {
                Zone = zone; Locked = locked; Rng = rng;
                Dim = locked ? 0.32f : 1f;
                Accent = locked ? Color.Lerp(accent, new Color(0.5f, 0.5f, 0.6f), 0.6f) * 0.55f : accent;
            }

            public float Range(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
            public bool Chance(float p) => Rng.NextDouble() < p;
        }

        void BuildZone(ZoneContext ctx, List<(Vector3, Vector3)> lanes)
        {
            var area = ctx.Zone.Area;
            _flat.Flat(area.xMin, area.yMin, area.xMax, area.yMax, 0f, Color.Lerp(Ground, ctx.Accent * 0.06f, 0.5f));
            DistrictBorder(ctx);

            if (ctx.Zone.Style != CityStyle.Summit) Streets(ctx, lanes);

            switch (ctx.Zone.Style)
            {
                case CityStyle.Downtown: Downtown(ctx); break;
                case CityStyle.Harbor: Harbor(ctx); break;
                case CityStyle.Summit: Summit(ctx); break;
                case CityStyle.Underground: Underground(ctx); break;
                case CityStyle.Skyline: Skyline(ctx); break;
            }
        }

        void DistrictBorder(ZoneContext ctx)
        {
            var a = ctx.Zone.Area;
            Color c = ctx.Accent * (ctx.Locked ? 0.5f : 0.75f);
            var corners = new[] { new Vector3(a.xMin, 0.06f, a.yMin), new Vector3(a.xMax, 0.06f, a.yMin), new Vector3(a.xMax, 0.06f, a.yMax), new Vector3(a.xMin, 0.06f, a.yMax) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 from = corners[i], to = corners[(i + 1) % 4];
                float len = Vector3.Distance(from, to);
                for (float d = 0f; d < len; d += 4f)
                    _lines.Strip(Vector3.Lerp(from, to, d / len), Vector3.Lerp(from, to, Mathf.Min(len, d + 2.2f) / len), 0.35f, c);
            }
        }

        IEnumerable<Rect> Blocks(Rect area)
        {
            int kx0 = Mathf.CeilToInt(area.xMin / blockPitch), kx1 = Mathf.FloorToInt(area.xMax / blockPitch);
            int kz0 = Mathf.CeilToInt(area.yMin / blockPitch), kz1 = Mathf.FloorToInt(area.yMax / blockPitch);
            float half = streetWidth * 0.5f;
            for (int kx = kx0; kx < kx1; kx++)
                for (int kz = kz0; kz < kz1; kz++)
                    yield return Rect.MinMaxRect(kx * blockPitch + half, kz * blockPitch + half,
                        (kx + 1) * blockPitch - half, (kz + 1) * blockPitch - half);
        }

        void Streets(ZoneContext ctx, List<(Vector3, Vector3)> lanes)
        {
            var area = ctx.Zone.Area;
            int kx0 = Mathf.CeilToInt(area.xMin / blockPitch), kx1 = Mathf.FloorToInt(area.xMax / blockPitch);
            int kz0 = Mathf.CeilToInt(area.yMin / blockPitch), kz1 = Mathf.FloorToInt(area.yMax / blockPitch);
            float z0 = kz0 * blockPitch, z1 = kz1 * blockPitch, x0 = kx0 * blockPitch, x1 = kx1 * blockPitch;
            for (int kx = kx0; kx <= kx1; kx++)
                Street(ctx, new Vector3(kx * blockPitch, 0f, z0), new Vector3(kx * blockPitch, 0f, z1), kx % 3 == 0, lanes);
            for (int kz = kz0; kz <= kz1; kz++)
                Street(ctx, new Vector3(x0, 0f, kz * blockPitch), new Vector3(x1, 0f, kz * blockPitch), kz % 3 == 0, lanes);
        }

        void Street(ZoneContext ctx, Vector3 from, Vector3 to, bool avenue, List<(Vector3, Vector3)> lanes)
        {
            Vector3 dir = (to - from).normalized, side = new Vector3(-dir.z, 0f, dir.x);
            float half = streetWidth * 0.5f, len = Vector3.Distance(from, to);
            Color edge = avenue ? ctx.Accent * 0.9f : ctx.Accent * 0.35f;
            var batch = avenue ? _neon : _lines;
            Vector3 lift = Vector3.up * 0.04f;
            batch.Strip(from + side * half + lift, to + side * half + lift, avenue ? 0.16f : 0.1f, avenue ? edge * 0.6f : edge);
            batch.Strip(from - side * half + lift, to - side * half + lift, avenue ? 0.16f : 0.1f, avenue ? edge * 0.6f : edge);
            Color dash = new Color(0.45f, 0.5f, 0.55f) * ctx.Dim * 0.5f;
            for (float d = 1f; d < len - 1f; d += 3f)
                _lines.Strip(from + dir * d + lift, from + dir * (d + 1.4f) + lift, 0.12f, dash);
            if (lanes == null) return;
            lanes.Add((from + side * 0.9f, to + side * 0.9f));
            lanes.Add((to - side * 0.9f, from - side * 0.9f));
        }

        // A building: window walls, dark roof, optional neon roof outline, rooftop box, antenna beacon.
        float Building(ZoneContext ctx, Rect lot, float height, float neonChance = 0.35f, bool windows = true)
        {
            float v = ctx.Range(0.78f, 1f) * ctx.Dim;
            Color wall = new(v, v, v * 1.05f);
            Vector3 min = new(lot.xMin, 0f, lot.yMin), max = new(lot.xMax, height, lot.yMax);
            if (windows) _walls.Box(min, max, wall, Roof * ctx.Dim, 12f, ctx.Range(0f, 8f), top: false);
            else _flat.Box(min, max, new Color(0.07f, 0.085f, 0.11f) * v, Roof, 12f, 0f, top: false);
            _flat.Flat(lot.xMin, lot.yMin, lot.xMax, lot.yMax, height, Roof * ctx.Dim);

            if (ctx.Chance(neonChance))
            {
                Color neon = ctx.Chance(0.7f) ? ctx.Accent : NeonColor(ctx);
                RoofOutline(lot, height + 0.03f, 0.22f, neon * ctx.Dim);
            }
            if (ctx.Chance(0.3f) && lot.width > 4f && lot.height > 4f)
            {
                float s = ctx.Range(1.2f, 2.4f);
                Vector2 c = lot.center + new Vector2(ctx.Range(-1f, 1f), ctx.Range(-1f, 1f));
                _flat.Box(new Vector3(c.x - s, height, c.y - s * 0.7f), new Vector3(c.x + s, height + 1.2f, c.y + s * 0.7f),
                    new Color(0.08f, 0.09f, 0.12f) * ctx.Dim, Roof * ctx.Dim);
            }
            if (height > 26f && ctx.Chance(0.75f))
            {
                Vector2 c = lot.center;
                float mast = ctx.Range(3f, 7f);
                _flat.Box(new Vector3(c.x - 0.15f, height, c.y - 0.15f), new Vector3(c.x + 0.15f, height + mast, c.y + 0.15f),
                    new Color(0.15f, 0.17f, 0.2f), Roof);
                if (!ctx.Locked) _beacons.Add(new Vector3(c.x, height + mast + 0.2f, c.y));
            }
            if (!ctx.Locked && ctx.Chance(0.16f) && height > 10f)
                FacadeSign(ctx, lot, height);
            return height;
        }

        Color NeonColor(ZoneContext ctx)
        {
            double r = ctx.Rng.NextDouble();
            return r < 0.35 ? new Color(1f, 0.24f, 0.5f) : r < 0.7 ? new Color(0.24f, 0.88f, 1f) : r < 0.85 ? new Color(0.65f, 0.42f, 1f) : new Color(0.78f, 1f, 0.24f);
        }

        void RoofOutline(Rect r, float y, float w, Color c)
        {
            _neon.Flat(r.xMin, r.yMin, r.xMax, r.yMin + w, y, c);
            _neon.Flat(r.xMin, r.yMax - w, r.xMax, r.yMax, y, c);
            _neon.Flat(r.xMin, r.yMin, r.xMin + w, r.yMax, y, c);
            _neon.Flat(r.xMax - w, r.yMin, r.xMax, r.yMax, y, c);
        }

        // Vertical neon sign on the south face (toward the camera).
        void FacadeSign(ZoneContext ctx, Rect lot, float height)
        {
            float x = ctx.Range(lot.xMin + 1f, lot.xMax - 1.6f), z = lot.yMin - 0.06f;
            float h0 = ctx.Range(height * 0.25f, height * 0.5f), h1 = Mathf.Min(height - 1f, h0 + ctx.Range(4f, 9f));
            Color c = NeonColor(ctx);
            _neon.Quad(new Vector3(x, h0, z), new Vector3(x + 0.7f, h0, z), new Vector3(x + 0.7f, h1, z), new Vector3(x, h1, z), c);
            Halo(new Vector3(x + 0.35f, (h0 + h1) * 0.5f, z - 0.3f), (h1 - h0) * 0.9f, c * 0.35f, vertical: true);
        }

        void Halo(Vector3 center, float size, Color color, bool vertical = false)
        {
            float s = size * 0.5f;
            if (vertical)
                _glow.Quad(center + new Vector3(-s, -s, 0), center + new Vector3(s, -s, 0), center + new Vector3(s, s, 0), center + new Vector3(-s, s, 0), color);
            else
                _glow.Quad(center + new Vector3(-s, 0, -s), center + new Vector3(s, 0, -s), center + new Vector3(s, 0, s), center + new Vector3(-s, 0, s), color);
        }

        IEnumerable<Rect> Lots(ZoneContext ctx, Rect block)
        {
            double r = ctx.Rng.NextDouble();
            const float gap = 0.8f;
            if (r < 0.35) { yield return Shrink(block, gap); yield break; }
            if (r < 0.75)
            {
                bool vertical = ctx.Chance(0.5f);
                float t = ctx.Range(0.4f, 0.6f);
                if (vertical)
                {
                    float x = Mathf.Lerp(block.xMin, block.xMax, t);
                    yield return Shrink(Rect.MinMaxRect(block.xMin, block.yMin, x, block.yMax), gap);
                    yield return Shrink(Rect.MinMaxRect(x, block.yMin, block.xMax, block.yMax), gap);
                }
                else
                {
                    float z = Mathf.Lerp(block.yMin, block.yMax, t);
                    yield return Shrink(Rect.MinMaxRect(block.xMin, block.yMin, block.xMax, z), gap);
                    yield return Shrink(Rect.MinMaxRect(block.xMin, z, block.xMax, block.yMax), gap);
                }
                yield break;
            }
            float mx = block.center.x, mz = block.center.y;
            yield return Shrink(Rect.MinMaxRect(block.xMin, block.yMin, mx, mz), gap);
            yield return Shrink(Rect.MinMaxRect(mx, block.yMin, block.xMax, mz), gap);
            yield return Shrink(Rect.MinMaxRect(block.xMin, mz, mx, block.yMax), gap);
            yield return Shrink(Rect.MinMaxRect(mx, mz, block.xMax, block.yMax), gap);
        }

        static Rect Shrink(Rect r, float m) => Rect.MinMaxRect(r.xMin + m * 0.5f, r.yMin + m * 0.5f, r.xMax - m * 0.5f, r.yMax - m * 0.5f);

        void Pavement(ZoneContext ctx, Rect block) =>
            _flat.Flat(block.xMin, block.yMin, block.xMax, block.yMax, 0.05f, PavementColor * Mathf.Max(ctx.Dim, 0.6f));

        bool IsLandmarkBlock(ZoneContext ctx, Rect block) => block.Contains(ctx.Zone.Landmark);

        bool IsGarageBlock(Rect block) =>
            block.Overlaps(Rect.MinMaxRect(garagePosition.x - 13f, garagePosition.y - 12f, garagePosition.x + 13f, garagePosition.y + 9f));

        // ------------------------------------------------------------------ district styles

        void Downtown(ZoneContext ctx)
        {
            Vector2 lm = ctx.Zone.Landmark;
            foreach (var block in Blocks(ctx.Zone.Area))
            {
                Pavement(ctx, block);
                if (IsGarageBlock(block)) continue;
                if (IsLandmarkBlock(ctx, block)) { Plaza(ctx, block); continue; }
                float near = 1f - Mathf.Clamp01(Vector2.Distance(block.center, lm) / 60f);
                foreach (var lot in Lots(ctx, block))
                    Building(ctx, lot, Mathf.Lerp(6f, 24f, Mathf.Pow((float)ctx.Rng.NextDouble(), 1.4f)) + near * 18f);
            }
            // Landmark tower with a neon crown.
            float h = 64f;
            var baseRect = new Rect(lm.x - 3.5f, lm.y - 3.5f, 7f, 7f);
            Building(ctx, baseRect, h, 0f);
            Crown(new Vector3(lm.x, h, lm.y), 4.6f, ctx.Accent);
            _flat.Box(new Vector3(lm.x - 0.25f, h, lm.y - 0.25f), new Vector3(lm.x + 0.25f, h + 14f, lm.y + 0.25f), new Color(0.2f, 0.22f, 0.26f), Roof);
            _beacons.Add(new Vector3(lm.x, h + 14.3f, lm.y));
            ctx.Zone.LabelHeight = h + 18f;
        }

        void Plaza(ZoneContext ctx, Rect block)
        {
            _flat.Flat(block.xMin + 0.5f, block.yMin + 0.5f, block.xMax - 0.5f, block.yMax - 0.5f, 0.07f, new Color(0.06f, 0.07f, 0.1f) * ctx.Dim);
            RoofOutline(Shrink(block, 1.2f), 0.09f, 0.15f, ctx.Accent * 0.6f);
        }

        void Crown(Vector3 top, float radius, Color color)
        {
            for (int ring = 0; ring < 3; ring++)
            {
                float y = top.y - ring * 2.2f + 0.2f;
                float r = radius * (1f - ring * 0.08f);
                for (int i = 0; i < 8; i++)
                {
                    float a0 = i * Mathf.PI / 4f, a1 = (i + 1) * Mathf.PI / 4f;
                    Vector3 p0 = new(top.x + Mathf.Cos(a0) * r, y, top.z + Mathf.Sin(a0) * r);
                    Vector3 p1 = new(top.x + Mathf.Cos(a1) * r, y, top.z + Mathf.Sin(a1) * r);
                    _neon.Quad(p0, p1, p1 + Vector3.up * 0.4f, p0 + Vector3.up * 0.4f, color);
                }
            }
            Halo(top + Vector3.down * 2f, radius * 5f, color * 0.4f);
        }

        void Harbor(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            float waterX = area.xMax - 22f;
            _water.Flat(waterX, -400f, 600f, 400f, 0.02f, Color.white * Mathf.Max(ctx.Dim, 0.6f), new Vector2(40f, 50f));
            foreach (var block in Blocks(area))
            {
                if (block.xMax > waterX) continue;
                Pavement(ctx, block);
                if (IsLandmarkBlock(ctx, block)) continue;
                if (ctx.Chance(0.45f))
                {
                    // Warehouse.
                    Building(ctx, Shrink(block, 1.2f), ctx.Range(4f, 7f), 0.5f, windows: false);
                }
                else
                {
                    // Container stacks.
                    for (float x = block.xMin + 1f; x < block.xMax - 2.5f; x += 2.8f)
                        for (float z = block.yMin + 1f; z < block.yMax - 6f; z += 6.6f)
                        {
                            int stack = ctx.Rng.Next(1, 4);
                            for (int s = 0; s < stack; s++)
                                _flat.Box(new Vector3(x, s * 2.4f, z), new Vector3(x + 2.4f, (s + 1) * 2.4f - 0.1f, z + 6f),
                                    ContainerColor(ctx) * ctx.Dim, ContainerColor(ctx) * 0.8f * ctx.Dim);
                        }
                }
            }
            // Quay edge and cranes along the water.
            _neon.Strip(new Vector3(waterX, 0.08f, area.yMin), new Vector3(waterX, 0.08f, area.yMax), 0.3f, ctx.Accent * 0.8f);
            for (float z = area.yMin + 10f; z < area.yMax - 8f; z += 24f) Crane(ctx, new Vector3(waterX - 4f, 0f, z));
            // Lighthouse landmark.
            Vector2 lm = ctx.Zone.Landmark;
            Lighthouse(ctx, new Vector3(lm.x, 0f, lm.y));
            ctx.Zone.LabelHeight = 32f;
        }

        Color ContainerColor(ZoneContext ctx)
        {
            switch (ctx.Rng.Next(5))
            {
                case 0: return new Color(0.42f, 0.16f, 0.12f);
                case 1: return new Color(0.1f, 0.33f, 0.36f);
                case 2: return new Color(0.13f, 0.17f, 0.32f);
                case 3: return new Color(0.42f, 0.36f, 0.12f);
                default: return new Color(0.2f, 0.22f, 0.26f);
            }
        }

        void Crane(ZoneContext ctx, Vector3 at)
        {
            Color steel = new Color(0.16f, 0.2f, 0.26f) * Mathf.Max(ctx.Dim, 0.5f);
            float h = 22f;
            _flat.Box(at + new Vector3(-2f, 0, -2f), at + new Vector3(-1.4f, h, -1.4f), steel, steel);
            _flat.Box(at + new Vector3(1.4f, 0, -2f), at + new Vector3(2f, h, -1.4f), steel, steel);
            _flat.Box(at + new Vector3(-2f, 0, 1.4f), at + new Vector3(-1.4f, h, 2f), steel, steel);
            _flat.Box(at + new Vector3(1.4f, 0, 1.4f), at + new Vector3(2f, h, 2f), steel, steel);
            _flat.Box(at + new Vector3(-2.5f, h, -2.5f), at + new Vector3(2.5f, h + 3f, 2.5f), steel, steel);
            _flat.Box(at + new Vector3(-10f, h + 3f, -0.6f), at + new Vector3(26f, h + 4.2f, 0.6f), steel, steel);
            _neon.Strip(at + new Vector3(-10f, h + 4.25f, 0f), at + new Vector3(26f, h + 4.25f, 0f), 0.25f, ctx.Accent);
            if (!ctx.Locked) _beacons.Add(at + new Vector3(26f, h + 4.6f, 0f));
        }

        void Lighthouse(ZoneContext ctx, Vector3 at)
        {
            int bands = 6;
            for (int i = 0; i < bands; i++)
            {
                float y0 = i * 4f, y1 = y0 + 4f, r0 = 3.2f - i * 0.25f, r1 = 3.2f - (i + 1) * 0.25f;
                Color band = (i % 2 == 0 ? new Color(0.85f, 0.88f, 0.92f) : new Color(0.75f, 0.12f, 0.2f)) * Mathf.Max(ctx.Dim, 0.5f);
                _flat.Prism(at + Vector3.up * y0, r0, r1, 4f, 10, (f, facing) => band * Mathf.Lerp(0.55f, 1f, (facing + 1f) * 0.5f), band);
            }
            _neon.Prism(at + Vector3.up * 24f, 1.6f, 1.6f, 2f, 10, (f, facing) => new Color(1f, 0.95f, 0.75f), new Color(1f, 0.95f, 0.75f));
            Halo(at + Vector3.up * 25f, 22f, new Color(1f, 0.9f, 0.6f) * 0.5f, vertical: true);
        }

        void Summit(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            // Low-poly snowy mountains.
            for (int i = 0; i < 9; i++)
            {
                Vector3 c = new(ctx.Range(area.xMin + 8f, area.xMax - 8f), 0f, ctx.Range(area.yMin + 12f, area.yMax));
                float r = ctx.Range(12f, 24f), h = ctx.Range(16f, 38f);
                Mountain(ctx, c, r, h, ctx.Rng.Next(5, 8), ctx.Range(0f, 6f));
            }
            // Pine trees.
            for (int i = 0; i < 70; i++)
            {
                Vector3 p = new(ctx.Range(area.xMin + 2f, area.xMax - 2f), 0f, ctx.Range(area.yMin + 2f, area.yMax - 2f));
                float s = ctx.Range(1.2f, 2.4f);
                Color pine = new Color(0.07f, 0.14f, 0.16f) * Mathf.Max(ctx.Dim, 0.6f);
                _flat.Prism(p, s, 0f, s * 2.8f, 5, (f, facing) => pine * Mathf.Lerp(0.6f, 1.2f, (facing + 1f) * 0.5f), pine);
            }
            // Ski jump landmark: a long glowing ramp.
            Vector2 lm = ctx.Zone.Landmark;
            Vector3 top = new(lm.x, 22f, lm.y + 10f), bottom = new(lm.x, 2f, lm.y - 16f);
            Ramp(top, bottom, 3.2f, new Color(0.85f, 0.9f, 1f) * Mathf.Max(ctx.Dim, 0.5f), ctx.Accent);
            ctx.Zone.LabelHeight = 34f;
        }

        void Mountain(ZoneContext ctx, Vector3 c, float r, float h, int sides, float rot)
        {
            Color rock = new(0.1f, 0.1f, 0.2f), snow = new(0.82f, 0.86f, 0.95f);
            float lit = Mathf.Max(ctx.Dim, 0.55f);
            // Lower rock band, upper snow cap.
            _flat.Prism(c, r, r * 0.42f, h * 0.58f, sides, (f, facing) => rock * Mathf.Lerp(0.5f, 1.3f, (facing + 1f) * 0.5f) * lit, rock, rot);
            _flat.Prism(c + Vector3.up * h * 0.58f, r * 0.42f, 0f, h * 0.42f, sides, (f, facing) => snow * Mathf.Lerp(0.55f, 1f, (facing + 1f) * 0.5f) * lit, snow, rot);
        }

        void Ramp(Vector3 top, Vector3 bottom, float width, Color deck, Color neon)
        {
            Vector3 side = Vector3.right * (width * 0.5f);
            _flat.Quad(bottom - side, bottom + side, top + side, top - side, deck);
            _neon.Quad(bottom - side, bottom - side + Vector3.up * 0.4f, top - side + Vector3.up * 0.4f, top - side, neon);
            _neon.Quad(bottom + side, bottom + side + Vector3.up * 0.4f, top + side + Vector3.up * 0.4f, top + side, neon);
            for (float t = 0f; t <= 1f; t += 0.25f)
            {
                Vector3 p = Vector3.Lerp(top, bottom, t);
                _flat.Box(new Vector3(p.x - width * 0.5f, 0f, p.z - 0.3f), new Vector3(p.x - width * 0.5f + 0.4f, p.y, p.z + 0.3f), new Color(0.12f, 0.13f, 0.2f), Roof);
                _flat.Box(new Vector3(p.x + width * 0.5f - 0.4f, 0f, p.z - 0.3f), new Vector3(p.x + width * 0.5f, p.y, p.z + 0.3f), new Color(0.12f, 0.13f, 0.2f), Roof);
            }
        }

        void Underground(ZoneContext ctx)
        {
            foreach (var block in Blocks(ctx.Zone.Area))
            {
                Pavement(ctx, block);
                if (IsLandmarkBlock(ctx, block)) continue;
                foreach (var lot in Lots(ctx, block))
                {
                    float h = Building(ctx, lot, ctx.Range(4f, 12f), 0.25f, windows: ctx.Chance(0.4f));
                    if (ctx.Chance(0.35f))
                    {
                        Vector3 p = new(lot.center.x + ctx.Range(-1f, 1f), h, lot.center.y + ctx.Range(-1f, 1f));
                        float sh = ctx.Range(6f, 14f);
                        Color brick = new Color(0.16f, 0.1f, 0.09f) * Mathf.Max(ctx.Dim, 0.5f);
                        _flat.Prism(p, 0.9f, 0.7f, sh, 8, (f, facing) => brick * Mathf.Lerp(0.6f, 1.2f, (facing + 1f) * 0.5f), brick);
                        Halo(p + Vector3.up * (sh + 0.5f), 4f, ctx.Accent * 0.5f);
                    }
                }
            }
            // Tunnel portal landmark: a glowing arch over a dark mouth.
            Vector2 lm = ctx.Zone.Landmark;
            Vector3 c = new(lm.x, 0f, lm.y);
            _flat.Quad(c + new Vector3(-6f, 0.06f, -1f), c + new Vector3(6f, 0.06f, -1f), c + new Vector3(6f, 0.06f, 8f), c + new Vector3(-6f, 0.06f, 8f), new Color(0.01f, 0.01f, 0.015f));
            for (int i = 0; i < 12; i++)
            {
                float a0 = Mathf.PI * i / 12f, a1 = Mathf.PI * (i + 1) / 12f;
                Vector3 p0 = c + new Vector3(Mathf.Cos(a0) * 6.5f, Mathf.Sin(a0) * 6.5f, -1f);
                Vector3 p1 = c + new Vector3(Mathf.Cos(a1) * 6.5f, Mathf.Sin(a1) * 6.5f, -1f);
                _neon.Quad(p0, p1, p1 + (p1 - c).normalized * 0.5f, p0 + (p0 - c).normalized * 0.5f, ctx.Accent);
            }
            ctx.Zone.LabelHeight = 22f;
        }

        void Skyline(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            foreach (var block in Blocks(area))
            {
                Pavement(ctx, block);
                if (IsLandmarkBlock(ctx, block)) continue;
                foreach (var lot in Lots(ctx, block))
                    Building(ctx, lot, Mathf.Lerp(24f, 70f, Mathf.Pow((float)ctx.Rng.NextDouble(), 1.2f)), 0.55f);
            }
            // Elevated highway loop inside the district.
            Rect loop = Rect.MinMaxRect(area.xMin + 10f, area.yMin + 10f, area.xMax - 10f, area.yMax - 10f);
            float y = 18f;
            var corners = new[] { new Vector3(loop.xMin, y, loop.yMin), new Vector3(loop.xMax, y, loop.yMin), new Vector3(loop.xMax, y, loop.yMax), new Vector3(loop.xMin, y, loop.yMax) };
            for (int i = 0; i < 4; i++) Highway(ctx, corners[i], corners[(i + 1) % 4]);
            // Supertall landmark.
            Vector2 lm = ctx.Zone.Landmark;
            float h = 105f;
            _walls.Box(new Vector3(lm.x - 4f, 0f, lm.y - 4f), new Vector3(lm.x + 4f, h * 0.7f, lm.y + 4f), Color.white * ctx.Dim, Roof, 12f, 0f, false);
            _walls.Box(new Vector3(lm.x - 2.6f, h * 0.7f, lm.y - 2.6f), new Vector3(lm.x + 2.6f, h, lm.y + 2.6f), Color.white * ctx.Dim, Roof, 12f, 3f, false);
            Crown(new Vector3(lm.x, h, lm.y), 3.4f, ctx.Accent);
            ctx.Zone.LabelHeight = h + 12f;
        }

        void Highway(ZoneContext ctx, Vector3 from, Vector3 to)
        {
            Vector3 dir = (to - from).normalized, side = new Vector3(-dir.z, 0f, dir.x) * 2.4f;
            Color deck = new Color(0.07f, 0.08f, 0.11f) * Mathf.Max(ctx.Dim, 0.5f);
            _flat.Quad(from - side, from + side, to + side, to - side, deck);
            _neon.Strip(from + side + Vector3.up * 0.05f, to + side + Vector3.up * 0.05f, 0.2f, ctx.Accent);
            _neon.Strip(from - side + Vector3.up * 0.05f, to - side + Vector3.up * 0.05f, 0.2f, ctx.Accent);
            float len = Vector3.Distance(from, to);
            for (float d = 0f; d < len; d += 12f)
            {
                Vector3 p = from + dir * d;
                _flat.Box(new Vector3(p.x - 0.6f, 0f, p.z - 0.6f), new Vector3(p.x + 0.6f, p.y, p.z + 0.6f), deck * 1.3f, deck);
            }
        }

        void BuildOutskirts()
        {
            var rng = new System.Random(seed * 7);
            float half = streetWidth * 0.5f;
            for (float x = outskirts.xMin; x < outskirts.xMax; x += blockPitch)
                for (float z = outskirts.yMin; z < outskirts.yMax; z += blockPitch)
                {
                    var block = Rect.MinMaxRect(x + half, z + half, x + blockPitch - half, z + blockPitch - half);
                    if (InAnyZone(block) || block.Overlaps(Rect.MinMaxRect(garagePosition.x - 16f, garagePosition.y - 16f, garagePosition.x + 16f, garagePosition.y + 12f))) continue;
                    if (rng.NextDouble() < 0.25) continue;
                    float h = 2f + (float)rng.NextDouble() * 8f;
                    float v = 0.18f + (float)rng.NextDouble() * 0.12f;
                    _walls.Box(new Vector3(block.xMin + 1f, 0f, block.yMin + 1f), new Vector3(block.xMax - 1f, h, block.yMax - 1f),
                        new Color(v, v, v), Roof * 0.7f, 12f, (float)rng.NextDouble() * 8f);
                }
        }

        bool InAnyZone(Rect r)
        {
            foreach (var z in _zones)
                if (z.Area.Overlaps(r)) return true;
            return false;
        }

        // ------------------------------------------------------------------ garage

        void BuildGarage()
        {
            Vector2 g = garagePosition;
            Color magenta = new(1f, 0.24f, 0.5f), cyan = new(0.24f, 0.88f, 1f);
            // Forecourt.
            _flat.Flat(g.x - 13f, g.y - 12f, g.x + 13f, g.y + 9f, 0.06f, new Color(0.06f, 0.07f, 0.1f));
            RoofOutline(Rect.MinMaxRect(g.x - 13f, g.y - 12f, g.x + 13f, g.y + 9f), 0.08f, 0.2f, magenta * 0.8f);
            for (float x = g.x - 10f; x <= g.x + 10f; x += 4f)
                _lines.Strip(new Vector3(x, 0.08f, g.y - 10f), new Vector3(x + 2f, 0.08f, g.y - 6f), 0.4f, cyan * 0.5f);
            // Building.
            var body = Rect.MinMaxRect(g.x - 11f, g.y - 2f, g.x + 11f, g.y + 8f);
            _flat.Box(new Vector3(body.xMin, 0f, body.yMin), new Vector3(body.xMax, 7.5f, body.yMax), new Color(0.07f, 0.085f, 0.12f), Roof);
            RoofOutline(body, 7.55f, 0.3f, magenta);
            // Three glowing doors.
            for (int i = -1; i <= 1; i++)
            {
                float x = g.x + i * 6.5f;
                Vector3 a = new(x - 2.4f, 0.1f, body.yMin - 0.05f), b = new(x + 2.4f, 0.1f, body.yMin - 0.05f);
                _lines.Quad(a, b, b + Vector3.up * 4.6f, a + Vector3.up * 4.6f, cyan * 0.55f);
                for (float y = 0.8f; y < 4.6f; y += 0.8f)
                    _neon.Quad(a + Vector3.up * y + Vector3.back * 0.02f, b + Vector3.up * y + Vector3.back * 0.02f,
                        b + Vector3.up * (y + 0.08f) + Vector3.back * 0.02f, a + Vector3.up * (y + 0.08f) + Vector3.back * 0.02f, cyan);
            }
            Halo(new Vector3(g.x, 2.5f, body.yMin - 1f), 26f, cyan * 0.25f, vertical: true);

            // Neon sign above the doors.
            if (signFont != null)
            {
                var sign = new GameObject("GarageSign");
                sign.transform.SetParent(transform, false);
                sign.transform.position = new Vector3(g.x, 10.5f, body.yMin + 1f);
                sign.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
                var text = sign.AddComponent<TextMeshPro>();
                text.font = signFont;
                text.text = "GARAGE";
                text.fontSize = 34;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.rectTransform.sizeDelta = new Vector2(30f, 6f);
                var mat = new Material(text.fontSharedMaterial);
                mat.SetColor("_FaceColor", magenta * 2.6f);
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor("_UnderlayColor", new Color(1f, 0.2f, 0.5f, 0.6f));
                mat.SetFloat("_UnderlaySoftness", 0.8f);
                mat.SetFloat("_UnderlayDilate", 0.6f);
                text.fontSharedMaterial = mat;
                Halo(new Vector3(g.x, 10.5f, body.yMin + 0.6f), 22f, magenta * 0.35f, vertical: true);
            }

            // A parked car on the forecourt.
            if (carSprite != null)
            {
                var car = new GameObject("ParkedCar");
                car.transform.SetParent(transform, false);
                car.transform.position = new Vector3(g.x + 6f, 0.12f, g.y - 7f);
                car.transform.rotation = Quaternion.Euler(90f, 0f, 35f);
                car.transform.localScale = Vector3.one * 2.6f;
                var sr = car.AddComponent<SpriteRenderer>();
                sr.sprite = carSprite;
                sr.color = cyan;
                if (glowSprite != null)
                {
                    var glow = new GameObject("Underglow");
                    glow.transform.SetParent(car.transform, false);
                    glow.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                    glow.transform.localScale = new Vector3(0.95f, 1.25f, 1f);
                    var gsr = glow.AddComponent<SpriteRenderer>();
                    gsr.sprite = glowSprite;
                    gsr.color = new Color(cyan.r, cyan.g, cyan.b, 0.5f);
                    gsr.sortingOrder = -1;
                }
            }
        }

        // ------------------------------------------------------------------ fog, beacons, output

        void CreateFog(CityZone zone)
        {
            var go = new GameObject($"Fog_{zone.name}");
            go.transform.SetParent(transform, false);
            var fog = go.AddComponent<CityFog>();
            fog.Setup(zone.Area, fogMaterial, zone.Seed);
        }

        void BuildBeacons()
        {
            var batch = new CityMeshBatch();
            foreach (var b in _beacons)
            {
                float s = 1.6f;
                batch.Quad(b + new Vector3(-s, -s, 0), b + new Vector3(s, -s, 0), b + new Vector3(s, s, 0), b + new Vector3(-s, s, 0), new Color(1f, 0.25f, 0.3f));
            }
            var go = Emit("Beacons", batch, glowMaterial);
            var blink = go.AddComponent<CityBlink>();
            blink.Setup(go.GetComponent<MeshRenderer>());
        }

        GameObject Emit(string name, CityMeshBatch batch, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = batch.ToMesh(name);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }
    }
}
