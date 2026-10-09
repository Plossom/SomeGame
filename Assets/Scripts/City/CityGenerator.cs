using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.City
{
    /// <summary>
    /// Builds the neon city map at startup from the <see cref="CityZone"/>s (one per district): a flat,
    /// top-down glow town of dark blocks and building footprints, a glowing outline per district,
    /// glowing avenues, one landmark symbol per district, the garage and fog over locked districts.
    /// Deterministic (seeded) so the city always looks the same.
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] Material flatMaterial;
        [SerializeField] Material linesMaterial;
        [SerializeField] Material neonMaterial;
        [SerializeField] Material glowMaterial;
        [SerializeField] Material fogMaterial;

        [Header("Garage")]
        [Tooltip("Centre of the garage lot (world X, Z).")]
        [SerializeField] Vector2 garagePosition = new(-8f, -60f);
        [SerializeField] Sprite carSprite;
        [Tooltip("Untinted details layer drawn over the car body (glass, lights, stripes).")]
        [SerializeField] Sprite carDetailsSprite;
        [SerializeField] Sprite glowSprite;
        [SerializeField] TMP_FontAsset signFont;

        [Header("Layout")]
        [SerializeField] int seed = 7;
        [SerializeField, Min(8f)] float blockPitch = 16f;
        [SerializeField, Min(1f)] float streetWidth = 4f;
        [Tooltip("Dim blocks around the districts, so the city does not end in a void.")]
        [SerializeField] Rect outskirts = new(-176, -144, 352, 320);

        CityMeshBatch _flat, _lines, _neon, _glow;
        readonly List<CityZone> _zones = new();

        // Heights of the flat layers (all seen from straight above; kept apart to avoid z-fighting).
        const float YBase = 0f, YBlock = 0.15f, YBuilding = 0.3f, YDetail = 0.45f, YNeon = 0.6f, YLandmark = 0.75f;

        static readonly Color Ground = new(0.018f, 0.024f, 0.035f);
        static readonly Color BlockColor = new(0.04f, 0.05f, 0.07f);
        static readonly Color WaterColor = new(0.02f, 0.06f, 0.09f);

        public IReadOnlyList<CityZone> Zones => _zones;
        public Vector3 GaragePoint => new(garagePosition.x, 0f, garagePosition.y);
        public Bounds CityBounds { get; private set; }

        void Awake() => Build();

        public void Build()
        {
            _flat = new(); _lines = new(); _neon = new(); _glow = new();
            _zones.Clear();
            _zones.AddRange(GetComponentsInChildren<CityZone>());

            _flat.Flat(-600f, -600f, 600f, 600f, -0.2f, Ground);
            Bounds bounds = new(GaragePoint, Vector3.one);
            BuildOutskirts();
            foreach (var zone in _zones)
            {
                var district = zone.District;
                bool locked = district == null || !ProgressStore.IsUnlocked(district);
                var rng = new System.Random(seed * 101 + zone.Seed);
                BuildZone(new ZoneContext(zone, locked, rng, district != null ? district.accent : Color.white));
                bounds.Encapsulate(new Vector3(zone.Area.xMin, 0f, zone.Area.yMin));
                bounds.Encapsulate(new Vector3(zone.Area.xMax, 0f, zone.Area.yMax));
                if (locked) CreateFog(zone);
            }
            BuildGarage();
            CityBounds = bounds;

            Emit("Flat", _flat, flatMaterial);
            Emit("Lines", _lines, linesMaterial);
            Emit("Neon", _neon, neonMaterial);
            Emit("Glow", _glow, glowMaterial);
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
                Dim = locked ? 0.5f : 1f;
                Accent = locked ? Color.Lerp(accent, new Color(0.5f, 0.5f, 0.6f), 0.6f) * 0.5f : accent;
            }

            public float Range(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
            public bool Chance(float p) => Rng.NextDouble() < p;
        }

        void BuildZone(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            _flat.Flat(area.xMin, area.yMin, area.xMax, area.yMax, YBase, Color.Lerp(Ground, ctx.Accent, 0.04f));
            Outline(_neon, area, YNeon, 0.5f, ctx.Accent * (ctx.Locked ? 0.6f : 0.8f));
            ctx.Zone.LabelHeight = 0.5f;

            switch (ctx.Zone.Style)
            {
                case CityStyle.Downtown: Downtown(ctx); break;
                case CityStyle.Harbor: Harbor(ctx); break;
                case CityStyle.Summit: Summit(ctx); break;
                case CityStyle.Underground: Underground(ctx); break;
                case CityStyle.Skyline: Skyline(ctx); break;
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

        // Only every third street (the avenues) glows; the others are just dark gaps between blocks.
        void Avenues(ZoneContext ctx, float maxX = float.MaxValue)
        {
            var area = ctx.Zone.Area;
            int kx0 = Mathf.CeilToInt(area.xMin / blockPitch), kx1 = Mathf.FloorToInt(Mathf.Min(area.xMax, maxX) / blockPitch);
            int kz0 = Mathf.CeilToInt(area.yMin / blockPitch), kz1 = Mathf.FloorToInt(area.yMax / blockPitch);
            float x0 = kx0 * blockPitch, x1 = kx1 * blockPitch, z0 = kz0 * blockPitch, z1 = kz1 * blockPitch;
            Color c = ctx.Accent * 0.45f;
            for (int kx = kx0; kx <= kx1; kx++)
                if (kx % 3 == 0) _lines.Strip(new Vector3(kx * blockPitch, YNeon, z0), new Vector3(kx * blockPitch, YNeon, z1), 0.3f, c);
            for (int kz = kz0; kz <= kz1; kz++)
                if (kz % 3 == 0) _lines.Strip(new Vector3(x0, YNeon, kz * blockPitch), new Vector3(x1, YNeon, kz * blockPitch), 0.3f, c);
        }

        // A city block with building footprints; a few buildings get a thin accent outline.
        void CityBlock(ZoneContext ctx, Rect block, float brightness = 1f, float outlineChance = 0.15f)
        {
            _flat.Flat(block.xMin, block.yMin, block.xMax, block.yMax, YBlock, BlockColor * ctx.Dim);
            foreach (var lot in Lots(ctx, block))
            {
                float v = ctx.Range(0.075f, 0.12f) * brightness * ctx.Dim;
                _flat.Flat(lot.xMin, lot.yMin, lot.xMax, lot.yMax, YBuilding, new Color(v, v * 1.08f, v * 1.35f));
                if (ctx.Chance(outlineChance)) Outline(_lines, lot, YDetail, 0.18f, ctx.Accent * 0.55f);
            }
        }

        IEnumerable<Rect> Lots(ZoneContext ctx, Rect block)
        {
            double r = ctx.Rng.NextDouble();
            const float gap = 1f;
            block = Shrink(block, 1f);
            if (r < 0.35) { yield return block; yield break; }
            if (r < 0.75)
            {
                bool vertical = ctx.Chance(0.5f);
                float t = ctx.Range(0.4f, 0.6f);
                if (vertical)
                {
                    float x = Mathf.Lerp(block.xMin, block.xMax, t);
                    yield return Shrink(Rect.MinMaxRect(block.xMin, block.yMin, x, block.yMax), gap, 0f);
                    yield return Shrink(Rect.MinMaxRect(x, block.yMin, block.xMax, block.yMax), gap, 0f);
                }
                else
                {
                    float z = Mathf.Lerp(block.yMin, block.yMax, t);
                    yield return Shrink(Rect.MinMaxRect(block.xMin, block.yMin, block.xMax, z), 0f, gap);
                    yield return Shrink(Rect.MinMaxRect(block.xMin, z, block.xMax, block.yMax), 0f, gap);
                }
                yield break;
            }
            float mx = block.center.x, mz = block.center.y;
            yield return Shrink(Rect.MinMaxRect(block.xMin, block.yMin, mx, mz), gap);
            yield return Shrink(Rect.MinMaxRect(mx, block.yMin, block.xMax, mz), gap);
            yield return Shrink(Rect.MinMaxRect(block.xMin, mz, mx, block.yMax), gap);
            yield return Shrink(Rect.MinMaxRect(mx, mz, block.xMax, block.yMax), gap);
        }

        static Rect Shrink(Rect r, float m) => Shrink(r, m, m);
        static Rect Shrink(Rect r, float mx, float mz) =>
            Rect.MinMaxRect(r.xMin + mx * 0.5f, r.yMin + mz * 0.5f, r.xMax - mx * 0.5f, r.yMax - mz * 0.5f);

        bool IsLandmarkBlock(ZoneContext ctx, Rect block) => block.Contains(ctx.Zone.Landmark);

        bool IsGarageBlock(Rect block) =>
            block.Overlaps(Rect.MinMaxRect(garagePosition.x - 13f, garagePosition.y - 12f, garagePosition.x + 13f, garagePosition.y + 9f));

        // ------------------------------------------------------------------ district styles

        // Dense blocks around a round plaza with a glowing ring.
        void Downtown(ZoneContext ctx)
        {
            Avenues(ctx);
            foreach (var block in Blocks(ctx.Zone.Area))
            {
                if (IsGarageBlock(block) || IsLandmarkBlock(ctx, block)) continue;
                CityBlock(ctx, block);
            }
            Vector3 c = Landmark(ctx);
            Disc(_flat, c + Vector3.up * YBlock, 6f, 32, BlockColor * 1.4f * ctx.Dim);
            Ring(_neon, c + Vector3.up * YLandmark, 5.5f, 0.45f, 40, ctx.Accent);
            Ring(_lines, c + Vector3.up * YLandmark, 3f, 0.25f, 32, ctx.Accent * 0.6f);
            Halo(c, 26f, ctx.Accent * 0.25f);
        }

        // Warehouses and container rows along a quay; open water with piers and a lighthouse.
        void Harbor(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            float waterX = area.xMax - 22f;
            _flat.Flat(waterX, area.yMin, area.xMax, area.yMax, YBlock, WaterColor * Mathf.Max(ctx.Dim, 0.6f));
            Avenues(ctx, waterX);
            foreach (var block in Blocks(area))
            {
                if (block.xMax > waterX) continue;
                if (ctx.Chance(0.5f)) { CityBlock(ctx, block, 0.9f, 0.25f); continue; }
                _flat.Flat(block.xMin, block.yMin, block.xMax, block.yMax, YBlock, BlockColor * ctx.Dim);
                for (float x = block.xMin + 1f; x < block.xMax - 2.2f; x += 2.6f)
                    _flat.Flat(x, block.yMin + 1f, x + 1.9f, block.yMax - 1f, YBuilding, ContainerColor(ctx) * ctx.Dim);
            }
            // Quay edge, piers and wave lines.
            _neon.Strip(new Vector3(waterX, YNeon, area.yMin), new Vector3(waterX, YNeon, area.yMax), 0.4f, ctx.Accent * 0.8f);
            for (float z = area.yMin + 14f; z < area.yMax - 8f; z += 26f)
            {
                _flat.Flat(waterX, z - 1.2f, waterX + 12f, z + 1.2f, YBuilding, BlockColor * 1.5f * ctx.Dim);
                _lines.Strip(new Vector3(waterX, YDetail, z), new Vector3(waterX + 12f, YDetail, z), 0.2f, ctx.Accent * 0.5f);
            }
            for (float z = area.yMin + 6f; z < area.yMax; z += 9f)
            {
                float x = waterX + 4f + ctx.Range(0f, 12f);
                _lines.Strip(new Vector3(x, YDetail, z), new Vector3(x + ctx.Range(2f, 4f), YDetail, z), 0.15f, ctx.Accent * 0.25f);
            }
            // Lighthouse on its island.
            Vector3 c = Landmark(ctx);
            Color warm = new Color(1f, 0.9f, 0.65f) * Mathf.Max(ctx.Dim, 0.5f);
            Disc(_flat, c + Vector3.up * YBuilding, 3.5f, 24, BlockColor * 1.6f * ctx.Dim);
            Disc(_neon, c + Vector3.up * YLandmark, 1.2f, 20, warm);
            Ring(_lines, c + Vector3.up * YLandmark, 3.5f, 0.2f, 28, warm * 0.6f);
            Halo(c, 18f, warm * 0.3f);
        }

        Color ContainerColor(ZoneContext ctx)
        {
            switch (ctx.Rng.Next(4))
            {
                case 0: return new Color(0.2f, 0.09f, 0.1f);
                case 1: return new Color(0.07f, 0.17f, 0.19f);
                case 2: return new Color(0.09f, 0.1f, 0.2f);
                default: return new Color(0.12f, 0.13f, 0.16f);
            }
        }

        // Mountains drawn as topographic contour rings, with a glowing ski jump.
        void Summit(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            Vector2[] peaks = { new(area.xMin + 22f, area.yMin + 24f), new(area.xMin + 30f, area.yMax - 18f), new(area.xMax - 18f, area.yMin + 20f), new(area.xMax - 20f, area.yMax - 22f) };
            foreach (var p in peaks)
            {
                float r = ctx.Range(13f, 17f);
                float rot = ctx.Range(0f, 6f);
                for (int i = 0; i < 4; i++)
                {
                    float ri = r * (1f - i * 0.22f);
                    Polygon(_lines, new Vector3(p.x, YDetail, p.y), ri, 7, rot + i * 0.3f, 0.2f, ctx.Accent * (0.25f + i * 0.12f));
                }
            }
            Vector3 c = Landmark(ctx);
            Vector3 top = c + new Vector3(0f, YLandmark, 14f), bottom = c + new Vector3(0f, YLandmark, -14f);
            _flat.Strip(top + Vector3.down * 0.3f, bottom + Vector3.down * 0.3f, 3.2f, BlockColor * 2f * ctx.Dim);
            _neon.Strip(top + Vector3.left * 1.6f, bottom + Vector3.left * 1.6f, 0.3f, ctx.Accent);
            _neon.Strip(top + Vector3.right * 1.6f, bottom + Vector3.right * 1.6f, 0.3f, ctx.Accent);
            Halo(c, 20f, ctx.Accent * 0.2f);
        }

        // Low blocks crossed by glowing metro lines, with a tunnel portal ring.
        void Underground(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            foreach (var block in Blocks(area))
            {
                if (IsLandmarkBlock(ctx, block)) continue;
                CityBlock(ctx, block, 0.8f, 0.1f);
            }
            Vector3 c = Landmark(ctx);
            // Dashed metro lines through the portal.
            DashedLine(new Vector3(area.xMin, YNeon, c.z), new Vector3(area.xMax, YNeon, c.z), 0.35f, ctx.Accent * 0.7f);
            DashedLine(new Vector3(c.x, YNeon, area.yMin), new Vector3(c.x, YNeon, area.yMax), 0.35f, ctx.Accent * 0.7f);
            Disc(_flat, c + Vector3.up * YBlock, 6f, 28, new Color(0.008f, 0.008f, 0.012f));
            Ring(_neon, c + Vector3.up * YLandmark, 5.5f, 0.5f, 32, ctx.Accent);
        }

        // Taller, brighter blocks inside an elevated highway loop.
        void Skyline(ZoneContext ctx)
        {
            var area = ctx.Zone.Area;
            Avenues(ctx);
            foreach (var block in Blocks(area))
            {
                if (IsLandmarkBlock(ctx, block)) continue;
                CityBlock(ctx, block, 1.3f, 0.2f);
            }
            Rect loop = Rect.MinMaxRect(area.xMin + 8f, area.yMin + 8f, area.xMax - 8f, area.yMax - 8f);
            Outline(_flat, Shrink(loop, -4f), YDetail, 4.6f, BlockColor * 1.8f * ctx.Dim);
            Outline(_neon, Shrink(loop, -4f), YLandmark, 0.25f, ctx.Accent * 0.7f);
            Outline(_neon, Shrink(loop, 4.6f), YLandmark, 0.25f, ctx.Accent * 0.7f);
            // Supertall: nested squares.
            Vector3 c = Landmark(ctx);
            Rect sq = new(c.x - 5f, c.z - 5f, 10f, 10f);
            _flat.Flat(sq.xMin, sq.yMin, sq.xMax, sq.yMax, YBuilding, BlockColor * 2f * ctx.Dim);
            Outline(_neon, sq, YLandmark, 0.4f, ctx.Accent);
            Outline(_lines, Shrink(sq, 4f), YLandmark, 0.25f, ctx.Accent * 0.7f);
            Halo(c, 22f, ctx.Accent * 0.2f);
        }

        void BuildOutskirts()
        {
            var rng = new System.Random(seed * 7);
            var garageLot = Rect.MinMaxRect(garagePosition.x - 16f, garagePosition.y - 16f, garagePosition.x + 16f, garagePosition.y + 12f);
            foreach (var block in Blocks(outskirts))
            {
                if (InAnyZone(block) || block.Overlaps(garageLot)) continue;
                // Fade out toward the edge of the map.
                float edge = Mathf.Min(Mathf.Min(block.xMin - outskirts.xMin, outskirts.xMax - block.xMax),
                                       Mathf.Min(block.yMin - outskirts.yMin, outskirts.yMax - block.yMax));
                float fade = Mathf.Clamp01(edge / 48f) * 0.6f;
                _flat.Flat(block.xMin, block.yMin, block.xMax, block.yMax, YBlock, Color.Lerp(Ground, BlockColor, fade));
                if (rng.NextDouble() < 0.3) continue;
                var lot = Shrink(block, 2f + (float)rng.NextDouble() * 3f);
                float v = 0.07f * fade;
                _flat.Flat(lot.xMin, lot.yMin, lot.xMax, lot.yMax, YBuilding, Color.Lerp(Ground, new Color(v, v * 1.08f, v * 1.35f), fade + 0.4f));
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
            var lot = Rect.MinMaxRect(g.x - 13f, g.y - 12f, g.x + 13f, g.y + 9f);
            _flat.Flat(lot.xMin, lot.yMin, lot.xMax, lot.yMax, YBlock, BlockColor * 1.3f);
            Outline(_lines, lot, YDetail, 0.2f, magenta * 0.5f);
            for (float x = g.x - 10f; x <= g.x + 10f; x += 4f)
                _lines.Strip(new Vector3(x, YDetail, g.y - 10f), new Vector3(x + 2f, YDetail, g.y - 6f), 0.3f, cyan * 0.4f);
            var body = Rect.MinMaxRect(g.x - 11f, g.y - 2f, g.x + 11f, g.y + 8f);
            _flat.Flat(body.xMin, body.yMin, body.xMax, body.yMax, YBuilding, new Color(0.07f, 0.085f, 0.12f));
            Outline(_neon, body, YLandmark, 0.4f, magenta);
            Halo(new Vector3(g.x, 0f, g.y + 3f), 34f, magenta * 0.2f);

            // Neon sign on the roof, lying flat so it reads from above.
            if (signFont != null)
            {
                var sign = new GameObject("GarageSign");
                sign.transform.SetParent(transform, false);
                sign.transform.position = new Vector3(g.x, YLandmark + 0.1f, body.center.y);
                sign.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var text = sign.AddComponent<TextMeshPro>();
                text.font = signFont;
                text.text = "GARAGE";
                text.fontSize = 30;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.rectTransform.sizeDelta = new Vector2(22f, 6f);
                var mat = new Material(text.fontSharedMaterial);
                mat.SetColor("_FaceColor", magenta * 2.2f);
                text.fontSharedMaterial = mat;
            }

            // A parked car on the forecourt.
            if (carSprite != null)
            {
                var car = new GameObject("ParkedCar");
                car.transform.SetParent(transform, false);
                car.transform.position = new Vector3(g.x + 6f, YLandmark, g.y - 7f);
                car.transform.rotation = Quaternion.Euler(90f, 0f, 35f);
                car.transform.localScale = Vector3.one * 2.6f;
                var sr = car.AddComponent<SpriteRenderer>();
                sr.sprite = carSprite;
                sr.color = cyan;
                if (carDetailsSprite != null)
                {
                    var details = new GameObject("Details");
                    details.transform.SetParent(car.transform, false);
                    details.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                    var dsr = details.AddComponent<SpriteRenderer>();
                    dsr.sprite = carDetailsSprite;
                    dsr.sortingOrder = 1;
                }
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

        // ------------------------------------------------------------------ shapes

        static Vector3 Landmark(ZoneContext ctx) => new(ctx.Zone.Landmark.x, 0f, ctx.Zone.Landmark.y);

        static void Outline(CityMeshBatch batch, Rect r, float y, float w, Color c)
        {
            batch.Flat(r.xMin, r.yMin, r.xMax, r.yMin + w, y, c);
            batch.Flat(r.xMin, r.yMax - w, r.xMax, r.yMax, y, c);
            batch.Flat(r.xMin, r.yMin + w, r.xMin + w, r.yMax - w, y, c);
            batch.Flat(r.xMax - w, r.yMin + w, r.xMax, r.yMax - w, y, c);
        }

        static void Ring(CityMeshBatch batch, Vector3 c, float r, float w, int segments, Color color) =>
            Polygon(batch, c, r, segments, 0f, w, color);

        static void Polygon(CityMeshBatch batch, Vector3 c, float r, int sides, float rot, float w, Color color)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = rot + i * Mathf.PI * 2f / sides, a1 = rot + (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = new(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                batch.Quad(c + d0 * (r - w * 0.5f), c + d1 * (r - w * 0.5f), c + d1 * (r + w * 0.5f), c + d0 * (r + w * 0.5f), color);
            }
        }

        static void Disc(CityMeshBatch batch, Vector3 c, float r, int segments, Color color)
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                batch.Quad(c, c, c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * r, c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * r, color);
            }
        }

        void DashedLine(Vector3 from, Vector3 to, float width, Color color)
        {
            float len = Vector3.Distance(from, to);
            for (float d = 0f; d < len; d += 5f)
                _neon.Strip(Vector3.Lerp(from, to, d / len), Vector3.Lerp(from, to, Mathf.Min(len, d + 2.5f) / len), width, color);
        }

        void Halo(Vector3 center, float size, Color color)
        {
            float s = size * 0.5f;
            center.y = YBuilding + 0.05f;
            _glow.Quad(center + new Vector3(-s, 0, -s), center + new Vector3(s, 0, -s), center + new Vector3(s, 0, s), center + new Vector3(-s, 0, s), color);
        }

        // ------------------------------------------------------------------ fog, output

        void CreateFog(CityZone zone)
        {
            var go = new GameObject($"Fog_{zone.name}");
            go.transform.SetParent(transform, false);
            var fog = go.AddComponent<CityFog>();
            fog.Setup(zone.Area, fogMaterial, zone.Seed);
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
