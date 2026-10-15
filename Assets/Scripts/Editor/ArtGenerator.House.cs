using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Art for the house the journey race drives through (toy scale: the furniture is huge round the
    /// track): floors (parquet, kitchen tiles, carpet, planks), roofs, the furniture and garden/farm/forest
    /// cells of the journey atlas, and the house's surprises (a cat strolling across, a robot vacuum, a
    /// beach ball, a wooden toy train on wooden rails), the farm's grass bank and the forest's log.
    /// </summary>
    public static partial class ArtGenerator
    {
        static float N(float x, float y, float cell) => Noise(x / cell, y / cell, Mathf.Max(1, Mathf.RoundToInt(128f / cell)));

        public static void GenerateHouse()
        {
            World("FloorParquet", 128, 128, ParquetPixel, 32, true);
            World("FloorTiles", 128, 128, TilesPixel, 32, true);
            World("FloorCarpet", 128, 128, CarpetPixel, 32, true);
            World("FloorPlanks", 128, 128, PlanksPixel, 32, true);
            World("RoofRed", 128, 128, (x, y) => RoofPixel(x, y, Hex("B8432C"), Hex("D2583A")), 32, true);
            World("RoofBlue", 128, 128, (x, y) => RoofPixel(x, y, Hex("3E6FA8"), Hex("5486C0")), 32, true);
            World("GrassWall", 64, 64, GrassWallPixel, 32, true);
            World("ToyRails", 128, 64, ToyRailsPixel, 32, true);
            foreach (var m in new[] { "FloorParquet", "FloorTiles", "FloorCarpet", "FloorPlanks", "RoofRed", "RoofBlue", "GrassWall", "ToyRails" }) Material(m, m);
            Material("Plain", null);
            World("Cat", 160, 160, Cat, 128, false);
            World("Vacuum", 160, 160, Vacuum, 128, false);
            World("BeachBall", 128, 128, BeachBall, 128, false);
            World("ToyLoco", 192, 320, ToyLoco, 128, false);
            World("ToyWagon", 192, 256, ToyWagon, 128, false);
            World("ForestLog", 512, 128, ForestLog, 128, false);
            World("MushroomPad", 256, 256, MushroomPad, 128, false);
        }

        // ================================================================== floors and roofs (tile 128 px)

        static Color ParquetPixel(float x, float y)
        {
            // Herringbone: blocks of 32 x 8, alternating direction in 32 px squares.
            int bx = Mathf.FloorToInt(x / 32f), by = Mathf.FloorToInt(y / 32f);
            bool turned = (bx + by) % 2 == 0;
            float u = turned ? x : y, v = turned ? y : x;
            int plank = Mathf.FloorToInt(v / 8f);
            float k = Hash(plank * 7 + bx * 3, by * 5 + (turned ? 1 : 0));
            Color c = Color.Lerp(Hex("B9814E"), Hex("D6A06A"), k);
            c = Color.Lerp(c, Shade(c, 0.85f), Mathf.Abs(Mathf.Sin(u * 0.35f + k * 9f)) * 0.25f); // grain
            if (Mathf.Repeat(v, 8f) < 0.9f || Mathf.Repeat(u, 32f) < 0.9f) c = Shade(c, 0.72f); // joints
            return c;
        }

        static Color TilesPixel(float x, float y)
        {
            bool dark = ((int)(x / 32f) + (int)(y / 32f)) % 2 == 0;
            Color c = dark ? Hex("C8603E") : Hex("F3E9D6");
            c = Color.Lerp(c, Shade(c, 0.94f), N(x, y, 16) * 0.5f);
            if (Mathf.Repeat(x, 32f) < 1.2f || Mathf.Repeat(y, 32f) < 1.2f) c = Hex("BFB3A0"); // grout
            return c;
        }

        static Color CarpetPixel(float x, float y)
        {
            Color c = Color.Lerp(Hex("6E8FB8"), Hex("7E9DC4"), N(x, y, 8));
            // A soft diamond pattern.
            float dx = Mathf.Abs(Mathf.Repeat(x, 32f) - 16f), dy = Mathf.Abs(Mathf.Repeat(y, 32f) - 16f);
            if (Mathf.Abs(dx + dy - 12f) < 1.3f) c = Color.Lerp(c, Hex("C9D8EA"), 0.6f);
            if (Hash((int)x, (int)y) > 0.9f) c = Shade(c, 0.92f); // pile
            return c;
        }

        static Color PlanksPixel(float x, float y)
        {
            int plank = Mathf.FloorToInt(y / 16f);
            float k = Hash(plank, 3);
            Color c = Color.Lerp(Hex("C99A66"), Hex("E2B783"), k);
            c = Color.Lerp(c, Shade(c, 0.88f), Mathf.Abs(Mathf.Sin(x * 0.12f + k * 7f)) * 0.3f);
            if (Mathf.Repeat(y, 16f) < 1f) c = Shade(c, 0.7f);
            float join = Mathf.Repeat(x + plank * 37f, 64f);
            if (join < 1f) c = Shade(c, 0.75f);
            if (Mathf.Abs(join - 4f) < 1.2f && Mathf.Abs(Mathf.Repeat(y, 16f) - 8f) < 1.2f) c = Hex("6B4A2E"); // nail
            return c;
        }

        static Color RoofPixel(float x, float y, Color tile, Color light)
        {
            // Rows of round-ended tiles, staggered.
            int row = Mathf.FloorToInt(y / 16f);
            float ox = Mathf.Repeat(x + (row % 2) * 8f, 16f) - 8f, oy = Mathf.Repeat(y, 16f);
            Color c = Color.Lerp(Shade(tile, 0.8f), tile, oy / 16f);
            float lip = Mathf.Sqrt(ox * ox + Mathf.Pow(oy - 4f, 2f));
            if (oy < 6f && lip > 7.5f) c = Shade(tile, 0.62f); // the gap under the tile above
            if (Mathf.Abs(ox) < 2f && oy > 8f) c = Color.Lerp(c, light, 0.5f);
            return c;
        }

        static Color GrassWallPixel(float x, float y)
        {
            float u = x / 64f;
            if (u > 0.93f) return Hex("2F5A2A");
            if (u > 0.82f) return Hex("8ACB52");
            Color c = Color.Lerp(Hex("3E7A31"), Hex("6FB04A"), Mathf.SmoothStep(0f, 1f, u / 0.82f));
            c = Color.Lerp(c, Shade(c, 0.88f), Noise(x / 8f, y / 8f, 8) * 0.6f);
            float cx = 32f, chevron = Mathf.Abs(Mathf.Abs(x - cx) * 0.9f + (y - 40f)) - 4f;
            if (Mathf.Abs(x - cx) < 18f && chevron < 0f) c = Color.Lerp(c, Hex("E4F5C8"), 0.7f);
            return c;
        }

        // A wooden toy railway: pale wood with two grooves (tiled along its length = x).
        static Color ToyRailsPixel(float x, float y)
        {
            if (y < 6f || y > 58f) return new Color(0, 0, 0, 0);
            Color c = Color.Lerp(Hex("D9B07A"), Hex("E8C694"), N(x, y, 16));
            if (y < 9f || y > 55f) c = Shade(c, 0.75f);
            foreach (float g in new[] { 22f, 42f }) if (Mathf.Abs(y - g) < 3f) c = Hex("8A6440");
            if (Mathf.Repeat(x, 64f) < 1.2f) c = Shade(c, 0.7f); // joint between pieces
            return c;
        }

        // ================================================================== garden, farm and forest cells

        static Color PatchField(float x, float y)
        {
            float field = RoundRect(x, y, 128, 128, 120, 120, 8);
            if (field > 1f) return new Color(0, 0, 0, 0);
            // Four plots in different crops, like a patchwork from above.
            int q = (x < 128 ? 0 : 1) + (y < 128 ? 0 : 2);
            Color[] crops = { Hex("D9B54A"), Hex("7FB352"), Hex("B98A4E"), Hex("A6C95A") };
            bool rows = Mathf.Repeat(q % 2 == 0 ? x : y, 14f) < 8f;
            Color c = rows ? crops[q] : Shade(crops[q], 0.82f);
            c = Shade(c, 0.94f + N(x, y, 16) * 0.1f);
            if (Mathf.Abs(x - 128f) < 2.5f || Mathf.Abs(y - 128f) < 2.5f) c = Hex("6E8F3E"); // hedgerow between plots
            c.a = Fill(field);
            return Over(c, Hex("5C7A32"), Stroke(field, 5f));
        }

        static Color Stump(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 64), 12f));
            float bark = Circle(x, y, 128, 128, 64) + (N(x, y, 16) - 0.5f) * 8f;
            c = Over(c, Hex("6B4A2E"), Fill(bark));
            float wood = Circle(x, y, 128, 128, 50);
            c = Over(c, Hex("D9B07A"), Fill(wood));
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
            if (wood < 0f && Mathf.Repeat(r, 9f) < 1.6f) c = Hex("B98A4E"); // rings
            c = Over(c, Outline, Stroke(bark, 4f) * 0.8f);
            return c;
        }

        static Color Ferns(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 138, 118, 84), 16f) * 0.7f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f + 0.2f;
                Vector2 tip = new Vector2(128, 128) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 96f;
                float frond = Capsule(x, y, 128, 128, tip.x, tip.y, 4f);
                // Leaflets either side of each stem.
                Vector2 dir = (tip - new Vector2(128, 128)).normalized, side = new(-dir.y, dir.x);
                float along = Vector2.Dot(new Vector2(x, y) - new Vector2(128, 128), dir), across = Vector2.Dot(new Vector2(x, y) - new Vector2(128, 128), side);
                float leaf = along > 8f && along < 96f && Mathf.Abs(across) < (96f - along) * 0.35f && Mathf.Repeat(along, 10f) < 6f ? -1f : 1f;
                c = Over(c, Color.Lerp(Hex("3E8A35"), Hex("6FB04A"), along / 96f), Fill(Mathf.Min(frond, leaf)));
            }
            return c;
        }

        static Color Mushrooms(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            void Cap(float cx, float cy, float r)
            {
                c = Over(c, Shadow, Soft(Circle(x, y, cx + r * 0.3f, cy - r * 0.3f, r), 6f));
                float d = Circle(x, y, cx, cy, r);
                c = Over(c, Hex("D63A2C"), Fill(d));
                c = Over(c, Hex("E8634C"), Fill(Circle(x, y, cx - r * 0.25f, cy + r * 0.25f, r * 0.55f)) * Fill(d + 2f));
                for (int i = 0; i < 5; i++)
                    c = Over(c, Color.white, Fill(Circle(x, y, cx + Mathf.Cos(i * 1.3f) * r * 0.55f, cy + Mathf.Sin(i * 1.3f) * r * 0.55f, r * 0.14f)));
                c = Over(c, Outline, Stroke(d, 3f) * 0.8f);
            }
            Cap(96, 150, 46);
            Cap(168, 104, 34);
            Cap(150, 178, 22);
            return c;
        }

        static Color Hedge(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = RoundRect(x, y, 128, 128, 120, 34, 30) + (N(x, y, 16) - 0.5f) * 10f;
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 114, 120, 34, 30), 10f));
            c = Over(c, Hex("3E7A31"), Fill(body));
            c = Over(c, Hex("5E9A45"), Fill(body + 8f) * (0.5f + 0.5f * N(x, y, 8)));
            c = Over(c, Outline, Stroke(body, 4f) * 0.7f);
            return c;
        }

        static Color GardenBed(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float bed = RoundRect(x, y, 128, 128, 110, 70, 18);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 110, 70, 18), 10f));
            c = Over(c, Hex("7A5434"), Fill(bed));
            Color[] petals = { Hex("E74C3C"), Hex("F6C531"), Hex("E86FB0"), Color.white, Hex("8A5CD6") };
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 3; j++)
                {
                    float fx = 44 + i * 34, fy = 88 + j * 40;
                    c = Over(c, Hex("4E8A35"), Fill(Circle(x, y, fx, fy - 4, 12)) * Fill(bed + 6f));
                    c = Over(c, petals[(i + j) % petals.Length], Fill(Star(x, y, fx, fy, 11, 5, 5, i * 0.4f)) * Fill(bed + 6f));
                }
            c = Over(c, Hex("B5885A"), Stroke(bed, 7f));
            return c;
        }

        static Color Fountain(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 116, 110), 12f));
            float basin = Circle(x, y, 128, 128, 108);
            c = Over(c, Hex("D9D2BF"), Fill(basin));
            c = Over(c, Hex("6CB6E0"), Fill(basin + 14f));
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
            if (basin + 14f < 0f && Mathf.Repeat(r, 18f) < 2f) c = Color.Lerp(c, Color.white, 0.5f); // ripples
            c = Over(c, Hex("D9D2BF"), Fill(Circle(x, y, 128, 128, 26)));
            c = Over(c, Hex("A9DBF2"), Fill(Circle(x, y, 128, 128, 14)));
            c = Over(c, Outline, Stroke(basin, 4f) * 0.8f);
            return c;
        }

        static Color LogPile(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 104, 70, 20), 10f));
            for (int row = 0; row < 3; row++)
            {
                float ly = 72 + row * 52;
                float log = RoundRect(x, y, 128, ly, 100, 24, 22);
                c = Over(c, Hex("7A5434"), Fill(log));
                c = Over(c, Hex("9A6B42"), Fill(RoundRect(x, y, 120, ly + 6, 90, 10, 8)));
                c = Over(c, Hex("D9B07A"), Fill(Circle(x, y, 34, ly, 20)));
                c = Over(c, Hex("B98A4E"), Stroke(Circle(x, y, 34, ly, 11), 2f));
                c = Over(c, Outline, Stroke(log, 3f) * 0.8f);
            }
            return c;
        }

        // ================================================================== furniture (huge: the cars are toys)

        static Color Cushions(Color c, float x, float y, float cx, float cy, float w, float h, Color fabric)
        {
            float d = RoundRect(x, y, cx, cy, w, h, Mathf.Min(w, h) * 0.45f);
            c = Over(c, fabric, Fill(d));
            c = Over(c, Color.Lerp(fabric, Color.white, 0.25f), Fill(RoundRect(x, y, cx - w * 0.15f, cy + h * 0.15f, w * 0.6f, h * 0.5f, Mathf.Min(w, h) * 0.3f)) * Fill(d + 3f));
            c = Over(c, Shade(fabric, 0.6f), Stroke(d, 3f));
            return c;
        }

        static Color Sofa(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color fabric = Hex("3E8A7A");
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 120, 56, 18), 12f));
            c = Over(c, Shade(fabric, 0.7f), Fill(RoundRect(x, y, 128, 128, 120, 56, 18)));
            c = Cushions(c, x, y, 128, 160, 112, 20, Shade(fabric, 0.9f)); // backrest
            c = Cushions(c, x, y, 16, 118, 12, 46, Shade(fabric, 0.9f));   // arms
            c = Cushions(c, x, y, 240, 118, 12, 46, Shade(fabric, 0.9f));
            c = Cushions(c, x, y, 76, 110, 50, 34, fabric);
            c = Cushions(c, x, y, 180, 110, 50, 34, fabric);
            c = Over(c, Hex("F2C230"), Fill(RotRect(x, y, 70, 138, 18, 14, 6, 0.3f))); // a cushion
            c = Over(c, Outline, Stroke(RoundRect(x, y, 128, 128, 120, 56, 18), 4f));
            return c;
        }

        static Color Armchair(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color fabric = Hex("C9573D");
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 114, 86, 86, 26), 12f));
            c = Over(c, Shade(fabric, 0.7f), Fill(RoundRect(x, y, 128, 128, 86, 86, 26)));
            c = Cushions(c, x, y, 128, 192, 78, 20, Shade(fabric, 0.9f));
            c = Cushions(c, x, y, 58, 116, 14, 66, Shade(fabric, 0.9f));
            c = Cushions(c, x, y, 198, 116, 14, 66, Shade(fabric, 0.9f));
            c = Cushions(c, x, y, 128, 110, 52, 52, fabric);
            c = Over(c, Outline, Stroke(RoundRect(x, y, 128, 128, 86, 86, 26), 4f));
            return c;
        }

        static Color CoffeeTable(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 112, 110, 70, 16), 12f));
            float top = RoundRect(x, y, 128, 128, 110, 70, 16);
            c = Over(c, Hex("8A5A34"), Fill(top));
            c = Over(c, Hex("A9734A"), Fill(top + 8f));
            c = Over(c, Color.white, Fill(Circle(x, y, 80, 140, 16)));   // a cup
            c = Over(c, Hex("6B3E1F"), Fill(Circle(x, y, 80, 140, 10)));
            c = Over(c, Hex("3D7DD8"), Fill(RotRect(x, y, 170, 118, 34, 24, 3, -0.2f))); // a book
            c = Over(c, Hex("F2C230"), Fill(RotRect(x, y, 176, 124, 28, 18, 3, -0.1f)));
            c = Over(c, Outline, Stroke(top, 4f));
            return c;
        }

        static Color Rug(float x, float y)
        {
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
            float rug = r - 122f;
            if (rug > 1f) return new Color(0, 0, 0, 0);
            Color c = Mathf.Repeat(r, 26f) < 13f ? Hex("E8C27A") : Hex("D9573D");
            if (r < 30f) c = Hex("3E8A7A");
            float a = Mathf.Atan2(y - 128, x - 128);
            if (Mathf.Abs(Mathf.Repeat(a * 8f / Mathf.PI, 2f) - 1f) < 0.12f && r > 30f) c = Shade(c, 0.85f);
            c.a = Fill(rug) * 0.95f;
            return c;
        }

        static Color Bookshelf(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 122, 40, 6), 12f));
            float shelf = RoundRect(x, y, 128, 128, 122, 40, 6);
            c = Over(c, Hex("6B4A2E"), Fill(shelf));
            Color[] spines = { Hex("C9412F"), Hex("3D7DD8"), Hex("F2C230"), Hex("2BB3A3"), Hex("8A5CD6"), Hex("E86FB0"), Hex("F4F1EA") };
            for (int i = 0; i < 22; i++)
            {
                float bx = 14 + i * 10.6f, h = 26 + Hash(i, 3) * 10;
                c = Over(c, spines[i % spines.Length], Fill(RoundRect(x, y, bx, 128, 4.2f, h, 1.5f)));
            }
            c = Over(c, Outline, Stroke(shelf, 4f));
            return c;
        }

        static Color HousePlant(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 112, 100), 16f));
            for (int i = 0; i < 11; i++)
            {
                float a = i * Mathf.PI * 2f / 11f;
                Vector2 tip = new Vector2(128, 128) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 104f;
                float leaf = Ellipse(x, y, (128 + tip.x) * 0.5f, (128 + tip.y) * 0.5f, 54, 18);
                // rotate the ellipse along the leaf: approximate by a capsule with a fat middle
                leaf = Capsule(x, y, 128 + Mathf.Cos(a) * 20, 128 + Mathf.Sin(a) * 20, tip.x, tip.y, 16f);
                c = Over(c, i % 2 == 0 ? Hex("3E8A35") : Hex("5BAA3F"), Fill(leaf));
                c = Over(c, Hex("8ACB52"), Fill(Capsule(x, y, 128, 128, tip.x, tip.y, 2f)) * Fill(leaf));
            }
            c = Over(c, Hex("C9573D"), Fill(Circle(x, y, 128, 128, 30))); // the pot
            c = Over(c, Hex("6B4A2E"), Fill(Circle(x, y, 128, 128, 22)));
            c = Over(c, Outline, Stroke(Circle(x, y, 128, 128, 30), 3f));
            return c;
        }

        static Color TvUnit(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 118, 34, 6), 12f));
            float unit = RoundRect(x, y, 128, 128, 118, 34, 6);
            c = Over(c, Hex("F4F1EA"), Fill(unit));
            c = Over(c, Hex("1E2224"), Fill(RoundRect(x, y, 128, 140, 90, 8, 3))); // the TV seen from above (thin)
            c = Over(c, Hex("3A3634"), Fill(RoundRect(x, y, 128, 128, 26, 6, 2)));
            c = Over(c, Hex("3D7DD8"), Fill(RoundRect(x, y, 50, 112, 18, 10, 3))); // a games console
            c = Over(c, Outline, Stroke(unit, 4f));
            return c;
        }

        static Color Bed(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 96, 120, 14), 12f));
            float frame = RoundRect(x, y, 128, 128, 96, 120, 14);
            c = Over(c, Hex("8A5A34"), Fill(frame));
            c = Over(c, Hex("F4F1EA"), Fill(RoundRect(x, y, 128, 124, 86, 104, 12)));
            c = Over(c, Hex("6E8FB8"), Fill(RoundRect(x, y, 128, 98, 88, 78, 12))); // duvet
            c = Over(c, Hex("8AA8CC"), Fill(RoundRect(x, y, 128, 98, 88, 78, 12) + 0f) * (Mathf.Repeat(x + y, 24f) < 12f ? 0.3f : 0f));
            c = Cushions(c, x, y, 92, 210, 30, 16, Color.white);
            c = Cushions(c, x, y, 164, 210, 30, 16, Color.white);
            c = Over(c, Outline, Stroke(frame, 4f));
            return c;
        }

        static Color Counter(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 122, 46, 6), 12f));
            float top = RoundRect(x, y, 128, 128, 122, 46, 6);
            c = Over(c, Hex("E6E1D6"), Fill(top));
            // A hob with four rings and a sink.
            for (int i = 0; i < 4; i++) c = Over(c, Hex("2A2A2A"), Stroke(Circle(x, y, 40 + (i % 2) * 34, 112 + (i / 2) * 32, 12), 5f));
            c = Over(c, Hex("9AA4AA"), Fill(RoundRect(x, y, 176, 128, 34, 26, 8)));
            c = Over(c, Hex("6CB6E0"), Fill(RoundRect(x, y, 176, 128, 26, 18, 6)));
            c = Over(c, Hex("C9412F"), Fill(Circle(x, y, 222, 140, 12))); // a tomato
            c = Over(c, Outline, Stroke(top, 4f));
            return c;
        }

        static Color DiningTable(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 112, 110, 104, 14), 12f));
            foreach (var (cx, cy) in new[] { (60f, 30f), (128f, 30f), (196f, 30f), (60f, 226f), (128f, 226f), (196f, 226f) })
            {
                float chair = RoundRect(x, y, cx, cy, 22, 18, 6);
                c = Over(c, Hex("A9734A"), Fill(chair));
                c = Over(c, Outline, Stroke(chair, 3f));
            }
            float top = RoundRect(x, y, 128, 128, 104, 74, 12);
            c = Over(c, Hex("B98A4E"), Fill(top));
            c = Over(c, Color.white, Fill(Circle(x, y, 70, 128, 18)) + Fill(Circle(x, y, 186, 128, 18)) * 1f);
            c = Over(c, Hex("F2C230"), Fill(Circle(x, y, 128, 128, 22))); // a bowl of lemons
            c = Over(c, Outline, Stroke(top, 4f));
            return c;
        }

        static Color Toys(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Building blocks scattered, and a teddy bear.
            Color[] blocks = { Hex("E74C3C"), Hex("3D7DD8"), Hex("F2C230"), Hex("2BB3A3") };
            for (int i = 0; i < 6; i++)
            {
                float bx = 50 + Hash(i, 1) * 150, by = 50 + Hash(i, 2) * 80, r = Hash(i, 3) * 1.5f;
                float b = RotRect(x, y, bx, by, 16, 16, 3, r);
                c = Over(c, Shadow, Soft(RotRect(x, y, bx + 5, by - 5, 16, 16, 3, r), 4f));
                c = Over(c, blocks[i % 4], Fill(b));
                c = Over(c, Outline, Stroke(b, 2.5f));
            }
            float body = Circle(x, y, 150, 176, 34), head = Circle(x, y, 150, 220, 22);
            c = Over(c, Shadow, Soft(Circle(x, y, 158, 168, 36), 8f));
            foreach (var (ex, ey) in new[] { (134f, 236f), (166f, 236f), (118f, 166f), (182f, 166f) }) c = Over(c, Hex("A9734A"), Fill(Circle(x, y, ex, ey, 11)));
            c = Over(c, Hex("B98A4E"), Fill(Mathf.Min(body, head)));
            c = Over(c, Hex("E8C694"), Fill(Circle(x, y, 150, 214, 9)));
            c = Over(c, Outline, Stroke(Mathf.Min(body, head), 3f));
            return c;
        }

        static Color FloorLamp(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(1f, 0.92f, 0.6f, 0.35f), Soft(Circle(x, y, 128, 128, 116), 40f)); // its light on the floor
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 116, 54), 10f));
            float shade = Circle(x, y, 128, 128, 54);
            c = Over(c, Hex("F2E3C0"), Fill(shade));
            c = Over(c, Hex("E8D3A5"), Stroke(Circle(x, y, 128, 128, 40), 3f));
            c = Over(c, Hex("6B4A2E"), Fill(Circle(x, y, 128, 128, 8)));
            c = Over(c, Outline, Stroke(shade, 4f));
            return c;
        }

        // ================================================================== the house's surprises

        // A ginger cat from above, walking (head +Y), tail curling behind.
        static Color Cat(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 88, 72, 30, 46), 8f));
            float tail = Capsule(x, y, 80, 34, 100, 8, 8f);
            float body = Ellipse(x, y, 80, 78, 28, 44);
            float head = Circle(x, y, 80, 128, 22);
            float ears = Mathf.Min(Triangle(x, y, new Vector2(62, 140), new Vector2(70, 156), new Vector2(76, 142)), Triangle(x, y, new Vector2(98, 140), new Vector2(90, 156), new Vector2(84, 142)));
            float cat = Min(tail, body, head, ears);
            c = Over(c, Hex("E8913A"), Fill(cat));
            // Stripes.
            if (body < 0f && Mathf.Repeat(y, 14f) < 5f) c = Over(c, Hex("C46A22"), 0.8f);
            c = Over(c, Hex("F6E0C4"), Fill(Ellipse(x, y, 80, 122, 10, 7)));
            c = Over(c, Hex("2A2A2A"), Fill(Circle(x, y, 72, 134, 3)) + Fill(Circle(x, y, 88, 134, 3)));
            c = Over(c, Outline, Stroke(cat, 3f));
            return c;
        }

        // A robot vacuum: a round disc with a bumper, a button and a sweeping brush.
        static Color Vacuum(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 88, 72, 66), 8f));
            float disc = Circle(x, y, 80, 80, 66);
            c = Over(c, Hex("2C3A44"), Fill(disc));
            c = Over(c, Hex("4E6575"), Fill(Circle(x, y, 80, 80, 54)));
            c = Over(c, Hex("9AA4AA"), Fill(RoundRect(x, y, 80, 132, 40, 8, 4))); // bumper
            c = Over(c, Hex("2BB3A3"), Fill(Circle(x, y, 80, 92, 12)));          // the glowing button
            c = Over(c, Color.white, Fill(Circle(x, y, 76, 96, 4)));
            // Side brush (spun in the game by rotating the whole sprite).
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.09f;
                c = Over(c, Hex("F2C230"), Fill(Capsule(x, y, 30, 120, 30 + Mathf.Cos(a) * 20, 120 + Mathf.Sin(a) * 20, 2.5f)));
            }
            c = Over(c, Outline, Stroke(disc, 4f));
            return c;
        }

        static Color BeachBall(float x, float y)
        {
            float d = Circle(x, y, 64, 64, 58);
            float a = Mathf.Atan2(y - 64, x - 64);
            Color[] stripes = { Hex("E74C3C"), Color.white, Hex("3D7DD8"), Color.white, Hex("F2C230"), Color.white };
            Color c = stripes[Mathf.FloorToInt(Mathf.Repeat(a / (Mathf.PI * 2f) * 6f, 6f))];
            c = Color.Lerp(c, Color.white, Fill(Circle(x, y, 46, 82, 16)) * 0.5f);
            c.a = Fill(d);
            c = Over(c, Hex("F2C230"), Fill(Circle(x, y, 64, 64, 8)));
            return Over(c, Outline, Stroke(d, 3.5f));
        }

        // A wooden toy engine (nose +Y): a red cab, a blue boiler with a yellow chimney, wooden wheels.
        static Color ToyLoco(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 108, 150, 66, 140, 20), 10f));
            foreach (float wy in new[] { 70f, 150f, 230f }) foreach (float wx in new[] { 30f, 162f })
                    c = Over(c, Hex("D9B07A"), Fill(RoundRect(x, y, wx, wy, 14, 26, 8)));
            float cab = RoundRect(x, y, 96, 86, 62, 54, 14);
            c = Over(c, Hex("D63A2C"), Fill(cab));
            c = Over(c, Hex("F2C230"), Fill(RoundRect(x, y, 96, 86, 46, 38, 10)));
            float boiler = RoundRect(x, y, 96, 196, 50, 74, 40);
            c = Over(c, Hex("3D7DD8"), Fill(boiler));
            c = Over(c, Hex("5E96E2"), Fill(RoundRect(x, y, 84, 196, 14, 64, 10)));
            c = Over(c, Hex("F2C230"), Fill(Circle(x, y, 96, 240, 18)));
            c = Over(c, Hex("D63A2C"), Fill(Circle(x, y, 96, 240, 9)));
            c = Over(c, Outline, Stroke(Mathf.Min(cab, boiler), 5f));
            return c;
        }

        static Color ToyWagon(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 108, 116, 66, 96, 16), 10f));
            foreach (float wy in new[] { 70f, 186f }) foreach (float wx in new[] { 30f, 162f })
                    c = Over(c, Hex("D9B07A"), Fill(RoundRect(x, y, wx, wy, 14, 26, 8)));
            float tub = RoundRect(x, y, 96, 128, 66, 96, 14);
            c = Over(c, Hex("2BB3A3"), Fill(tub));
            c = Over(c, Hex("1E8A7C"), Fill(RoundRect(x, y, 96, 128, 52, 82, 10)));
            // Building blocks as cargo.
            Color[] blocks = { Hex("E74C3C"), Hex("F2C230"), Hex("3D7DD8"), Hex("E86FB0") };
            for (int i = 0; i < 6; i++)
            {
                float bx = 72 + (i % 2) * 48, by = 70 + (i / 2) * 58;
                float b = RotRect(x, y, bx, by, 20, 20, 4, i * 0.4f);
                c = Over(c, blocks[i % 4], Fill(b));
                c = Over(c, Outline, Stroke(b, 2.5f));
            }
            c = Over(c, Outline, Stroke(tub, 5f));
            return c;
        }

        // A bouncy mushroom from above (a jump pad in the meadows and woods): a big red cap with white spots,
        // a lighter ring round its springy middle.
        static Color MushroomPad(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 114, 118), 14f));
            float cap = Circle(x, y, 128, 128, 116);
            c = Over(c, Hex("C9302A"), Fill(cap));
            c = Over(c, Hex("E8463A"), Fill(Circle(x, y, 118, 140, 92)) * Fill(cap + 4f));
            c = Over(c, Hex("F26B5E"), Fill(Circle(x, y, 104, 156, 44)) * Fill(cap + 8f)); // shine
            foreach (var (sx, sy, r) in new[] { (128f, 128f, 24f), (70f, 150f, 16f), (186f, 150f, 15f), (92f, 76f, 14f), (170f, 82f, 17f), (128f, 200f, 12f), (50f, 100f, 10f), (204f, 108f, 10f) })
            {
                float spot = Circle(x, y, sx, sy, r);
                c = Over(c, Color.white, Fill(spot));
                c = Over(c, Hex("E8E2D6"), Fill(Circle(x, y, sx + r * 0.25f, sy - r * 0.25f, r * 0.6f)) * Fill(spot) * 0.6f);
            }
            c = Over(c, Hex("7A1A16"), Stroke(cap, 5f));
            return c;
        }

        // A fallen tree trunk lying across the forest road (along X), bark and cut ends.
        static Color ForestLog(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 262, 50, 236, 40, 38), 10f));
            float log = RoundRect(x, y, 256, 64, 236, 40, 38);
            c = Over(c, Hex("6B4A2E"), Fill(log));
            float bark = Mathf.Sin(x * 0.18f + N(x, y, 16) * 6f);
            if (log < 0f && bark > 0.6f) c = Hex("553A23");
            c = Over(c, Hex("8A6440"), Fill(RoundRect(x, y, 256, 78, 220, 10, 8)) * 0.8f); // lit top
            foreach (float ex in new[] { 26f, 486f })
            {
                c = Over(c, Hex("D9B07A"), Fill(Ellipse(x, y, ex, 64, 14, 36)));
                c = Over(c, Hex("B98A4E"), Stroke(Ellipse(x, y, ex, 64, 7, 18), 2f));
            }
            // Moss.
            if (log < -4f && N(x, y, 16) > 0.7f) c = Color.Lerp(c, Hex("6FB04A"), 0.7f);
            c = Over(c, Outline, Stroke(log, 4f));
            return c;
        }
    }
}
