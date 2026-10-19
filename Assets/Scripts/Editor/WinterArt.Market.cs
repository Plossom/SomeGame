using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Christmas market scenery atlas (24 cells of 256 px, 4 x 6) for the Winter Cup lap race Christmas
    /// Market: the snowy basics plus the big Christmas tree, market stalls in red and green, a mulled-wine
    /// stand, three town houses, the church, a frozen fountain, carolers, Santa's sleigh with two reindeer, a
    /// nutcracker, benches, a Christmas pyramid, presents, crates and a sled. No glowing lights (they look like
    /// checkpoint posts on snow). Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] MarketCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "WinterCrowd", "SnowDrift", "Gifts",
            "XmasTree", "StallRed", "StallGreen", "StallWine",
            "TownHouseA", "TownHouseB", "TownHouseC", "Church",
            "Fountain", "Carolers", "Sleigh", "Nutcracker",
            "Benches", "Pyramid", "Crates", "Sled",
        };

        const int MarketRows = 6;

        /// <summary>UV rect of a Christmas market scenery cell by name (row 0 at the bottom).</summary>
        public static Rect MarketCell(string name)
        {
            int i = System.Array.IndexOf(MarketCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)MarketRows, 0.25f, 1f / MarketRows);
        }

        [MenuItem("SomeGame/Generate Christmas Market Art")]
        public static void GenerateMarket()
        {
            Write(ArtGenerator.Folder, "SceneryMarket", 1024, 256 * MarketRows, MarketPixel, 128, false);
            ArtGenerator.Material("SceneryMarket", "SceneryMarket");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color MarketPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return MarketCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Gifts" => Gifts(lx, ly),
                "XmasTree" => XmasTree(lx, ly),
                "StallRed" => Stall(lx, ly, Hex("C8352E"), 0),
                "StallGreen" => Stall(lx, ly, Hex("2E8A5A"), 1),
                "StallWine" => Stall(lx, ly, Hex("7A2E4A"), 2),
                "TownHouseA" => TownHouse(lx, ly, Hex("B5452E"), 0),
                "TownHouseB" => TownHouse(lx, ly, Hex("5E6B80"), 1),
                "TownHouseC" => TownHouse(lx, ly, Hex("8E5C3B"), 2),
                "Church" => Church(lx, ly),
                "Fountain" => Fountain(lx, ly),
                "Carolers" => Carolers(lx, ly),
                "Sleigh" => SantaSleigh(lx, ly),
                "Nutcracker" => NutcrackerStatue(lx, ly),
                "Benches" => Benches(lx, ly),
                "Pyramid" => Pyramid(lx, ly),
                "Crates" => Crates(lx, ly),
                _ => Sled(lx, ly),
            };
        }

        // The big Christmas tree from above: layered star-shaped tiers of fir, snow on them, red and gold baubles
        // (no lights) and a golden star on top.
        static Color XmasTree(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 112, 112), 14f));
            float[] radii = { 116f, 90f, 64f, 38f };
            for (int i = 0; i < radii.Length; i++)
            {
                float tier = Star(x, y, 128, 128, radii[i], radii[i] * 0.72f, 9, i * 0.35f);
                Color fir = Color.Lerp(Hex("1F5A3A"), Hex("3E8A52"), i / 3f);
                c = Over(c, Shade(fir, 0.7f), Fill(tier + 3f));
                c = Over(c, fir, Fill(tier));
                if (Noise(x * 0.3f, y * 0.3f, 32) > 0.6f) c = Over(c, Snow, Fill(tier + 3f) * Fill(-tier - 6f) * 0.9f);
            }
            for (int i = 0; i < 22; i++)
            {
                float a = Hash(i, 1) * Mathf.PI * 2f, r = 22f + Hash(i, 2) * 88f;
                float b = Circle(x, y, 128 + Mathf.Cos(a) * r, 128 + Mathf.Sin(a) * r, 6.5f);
                c = Over(c, i % 3 == 0 ? Hex("F2B632") : i % 3 == 1 ? Hex("D8322E") : Hex("3E6EC8"), Fill(b));
                c = Over(c, Color.white, Fill(Circle(x, y, 126 + Mathf.Cos(a) * r, 130 + Mathf.Sin(a) * r, 2f)));
                c = Over(c, Outline, Stroke(b, 1.8f) * 0.7f);
            }
            float star = Star(x, y, 128, 128, 22, 9, 5, Mathf.PI / 2f);
            c = Over(c, Hex("FFD54A"), Fill(star));
            c = Over(c, Hex("B8860B"), Stroke(star, 2.5f));
            return c;
        }

        // A wooden market stall: a striped awning (red, green or wine-red), goods on the counter in front.
        // kind 0 gingerbread hearts, 1 baubles, 2 mugs of mulled wine.
        static Color Stall(float x, float y, Color awning, int kind)
        {
            Color c = new(0, 0, 0, 0);
            float hut = RoundRect(x, y, 128, 146, 96, 64, 4);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 134, 98, 70, 6), 10f));
            c = Over(c, Hex("8E5C3B"), Fill(hut + 3f));
            Color stripe = Mathf.Repeat(x - 32f, 32f) < 16f ? awning : Color.white;
            c = Over(c, stripe, Fill(hut));
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 196, 92, 16, 4)) * Fill(hut));
            // Scalloped edge of the awning over the counter.
            float scallop = Mathf.Max(Circle(x, y, Mathf.Floor((x - 32f) / 16f) * 16f + 40f, 84, 8), Mathf.Abs(x - 128f) - 96f);
            c = Over(c, stripe, Fill(scallop));
            float counter = RoundRect(x, y, 128, 66, 98, 16, 3);
            c = Over(c, Hex("B5834F"), Fill(counter));
            c = Over(c, Hex("6B4A2E"), Stroke(counter, 2.5f));
            for (int i = 0; i < 7; i++)
            {
                float gx = 54 + i * 25f, gy = 66;
                if (kind == 0)
                {
                    float heart = SMin(Circle(x, y, gx - 4, gy + 3, 6), Circle(x, y, gx + 4, gy + 3, 6), 2f);
                    heart = Mathf.Min(heart, Triangle(x, y, new Vector2(gx - 9, gy + 1), new Vector2(gx + 9, gy + 1), new Vector2(gx, gy - 10)));
                    c = Over(c, Hex("9C5A2E"), Fill(heart));
                    c = Over(c, Color.white, Stroke(heart + 3f, 1.4f) * Fill(heart));
                }
                else if (kind == 1)
                {
                    float b = Circle(x, y, gx, gy, 7.5f);
                    c = Over(c, i % 3 == 0 ? Hex("F2B632") : i % 3 == 1 ? Hex("D8322E") : Hex("3E6EC8"), Fill(b));
                    c = Over(c, Color.white, Fill(Circle(x, y, gx - 2, gy + 2, 2f)));
                    c = Over(c, Outline, Stroke(b, 1.6f) * 0.7f);
                }
                else
                {
                    float mug = Circle(x, y, gx, gy, 7f);
                    c = Over(c, Color.white, Fill(mug));
                    c = Over(c, Hex("7A1E30"), Fill(Circle(x, y, gx, gy, 4.5f)));
                    c = Over(c, Outline, Stroke(mug, 1.6f) * 0.7f);
                }
            }
            if (kind == 2)
            {
                // Steam rising off the pot of wine on the roof's edge.
                float steam = Mathf.Min(Circle(x, y, 196, 212, 10), Circle(x, y, 208, 228, 12));
                c = Over(c, White(0.5f), Soft(steam, 8f));
            }
            c = Over(c, Outline, Stroke(hut, 3.5f) * 0.85f);
            return c;
        }

        // A town house from above: a steep roof of tiles (or slate) under snow along the ridge, a chimney with
        // smoke, a dormer window. Style 0..2 changes where the chimney and dormer sit.
        static Color TownHouse(float x, float y, Color tiles, int style)
        {
            Color c = new(0, 0, 0, 0);
            float house = RoundRect(x, y, 128, 128, 104, 84, 4);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 110, 104, 84, 6), 12f));
            Color t = Mathf.Repeat(y, 14f) < 2f ? Shade(tiles, 0.7f) : Mathf.Repeat(x + Mathf.Floor(y / 14f) * 9f, 18f) < 1.5f ? Shade(tiles, 0.8f) : tiles;
            if (y < 128f) t = Shade(t, 0.82f);
            c = Over(c, t, Fill(house));
            // Snow lying thick on the upper half of the roof, thinning to the eaves.
            float snow = house + Mathf.Abs(y - 128f) * 1.6f - 20f - (Noise(x * 0.08f, y * 0.08f, 32) - 0.5f) * 24f;
            c = Over(c, y > 128 ? Snow : SnowShade, Fill(snow) * Fill(house + 3f));
            c = Over(c, SnowDeep, Fill(Mathf.Abs(y - 128f) - 1.5f) * Fill(house));
            float cx = style == 0 ? 180f : style == 1 ? 76f : 150f, cy = style == 2 ? 92f : 160f;
            float chimney = RoundRect(x, y, cx, cy, 13, 13, 2);
            c = Over(c, Hex("9A6A50"), Fill(chimney));
            c = Over(c, Snow, Fill(RoundRect(x, y, cx, cy + 6, 13, 7, 2)));
            c = Over(c, Hex("3A3532"), Fill(RoundRect(x, y, cx, cy, 6, 6, 2)));
            c = Over(c, Outline, Stroke(chimney, 3f));
            float smoke = Mathf.Min(Circle(x, y, cx + 14, cy + 20, 9), Circle(x, y, cx + 28, cy + 34, 12));
            c = Over(c, White(0.5f), Soft(smoke, 8f));
            float dx = style == 1 ? 168f : 92f;
            float dormer = RoundRect(x, y, dx, 70, 18, 16, 3);
            c = Over(c, tiles, Fill(dormer));
            c = Over(c, Snow, Fill(RoundRect(x, y, dx, 76, 18, 9, 3)));
            c = Over(c, Hex("33475E"), Fill(RoundRect(x, y, dx, 58, 8, 5, 1)));
            c = Over(c, Outline, Stroke(dormer, 2.5f) * 0.85f);
            c = Over(c, Outline, Stroke(house, 4f) * 0.85f);
            return c;
        }

        // The church from above: a long nave roof of slate under snow, the square tower with its green spire
        // (an eight-sided cone seen from the top).
        static Color Church(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float nave = RoundRect(x, y, 104, 128, 84, 54, 4);
            float tower = RoundRect(x, y, 206, 128, 36, 36, 3);
            c = Over(c, Shadow, Soft(Mathf.Min(RoundRect(x, y, 120, 112, 84, 54, 4), RoundRect(x, y, 222, 112, 36, 36, 3)), 12f));
            c = Over(c, Mathf.Repeat(y, 12f) < 1.6f ? Hex("3E4656") : Hex("5E6B80"), Fill(nave));
            c = Over(c, y > 128 ? Snow : SnowShade, Fill(nave + Mathf.Abs(y - 128f) * 1.4f - 6f) * Fill(nave + 3f));
            c = Over(c, SnowDeep, Fill(Mathf.Abs(y - 128f) - 1.5f) * Fill(nave));
            c = Over(c, Hex("C9C0B0"), Fill(tower));
            float a = Mathf.Atan2(y - 128, x - 206);
            float spire = Dist(x, y, 206, 128) - 30f;
            c = Over(c, Mathf.Repeat(a * 4f / Mathf.PI, 1f) < 0.5f ? Hex("3E8A72") : Hex("2E6E5A"), Fill(spire));
            c = Over(c, Hex("F2B632"), Fill(Circle(x, y, 206, 128, 5)));
            c = Over(c, Outline, Stroke(Mathf.Min(nave, tower), 4f) * 0.85f);
            c = Over(c, Outline, Stroke(spire, 2.5f) * 0.7f);
            return c;
        }

        // A round stone fountain, frozen: icicles round the basin, a statue of ice in the middle.
        static Color Fountain(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float r = Dist(x, y, 128, 128);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 116, 100), 12f));
            c = Over(c, Hex("A8A29A"), Fill(r - 100f));
            c = Over(c, Hex("CFC8BE"), Fill(Shell(r - 94f, 6f)));
            c = Over(c, Hex("BFE6F7"), Fill(r - 86f));
            c = Over(c, Color.white, Fill(Ellipse(x, y, 96, 156, 28, 10)) * Fill(r - 86f) * 0.6f);
            float crack = Mathf.Abs(Mathf.Sin(Mathf.Atan2(y - 128, x - 128) * 5f) * 20f - (r - 50f));
            if (crack < 1.2f && r < 84f) c = Over(c, Color.white, 0.7f);
            float pillar = Circle(x, y, 128, 128, 22);
            c = Over(c, Hex("CFC8BE"), Fill(pillar));
            c = Over(c, Hex("E8F8FF"), Fill(Star(x, y, 128, 128, 18, 8, 6, 0.3f)));
            c = Over(c, Outline, Stroke(pillar, 3f) * 0.8f);
            c = Over(c, Outline, Stroke(r - 100f, 4f) * 0.85f);
            return c;
        }

        // Carolers in a half circle in scarves and bobble hats, songbooks open, a conductor in front.
        static Color Carolers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] coats = { Hex("C8352E"), Hex("2E5BA8"), Hex("2E8A5A"), Hex("E8B830"), Hex("7A4FB0"), Hex("C8352E") };
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.PI * (0.15f + i * 0.14f);
                float px = 128 + Mathf.Cos(a) * 84f, py = 96 + Mathf.Sin(a) * 84f;
                float body = Circle(x, y, px, py, 18);
                c = Over(c, Shadow, Soft(Circle(x, y, px + 7, py - 7, 19), 7f));
                c = Over(c, coats[i], Fill(body));
                c = Over(c, Color.white, Fill(Circle(x, y, px, py + 4, 9)));
                c = Over(c, coats[(i + 2) % 6], Fill(Circle(x, y, px, py + 4, 6)));
                c = Over(c, Color.white, Fill(Circle(x, y, px, py + 4, 2.5f)));
                float book = RotRect(x, y, px + (128 - px) * 0.25f, py + (96 - py) * 0.25f, 9, 6, 1, a);
                c = Over(c, Color.white, Fill(book));
                c = Over(c, Outline, Stroke(book, 1.5f) * 0.7f);
                c = Over(c, Outline, Stroke(body, 3f) * 0.85f);
            }
            float conductor = Circle(x, y, 128, 70, 18);
            c = Over(c, Hex("222A35"), Fill(conductor));
            c = Over(c, Hex("D8322E"), Fill(Circle(x, y, 128, 74, 7)));
            c = Over(c, Outline, Stroke(conductor, 3f));
            c = Over(c, Hex("5A3E2B"), Fill(Capsule(x, y, 140, 80, 156, 100, 1.6f)));
            return c;
        }

        // Santa's sleigh parked on the square: a red sleigh with a sack of presents, two reindeer in harness.
        static Color SantaSleigh(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Hex("F2B632"), Fill(Mathf.Min(Capsule(x, y, 100, 30, 100, 100, 2.5f), Capsule(x, y, 156, 30, 156, 100, 2.5f))));
            c = Over(c, Hex("5A3E2B"), Fill(Mathf.Min(Capsule(x, y, 110, 98, 104, 150, 1.6f), Capsule(x, y, 146, 98, 152, 150, 1.6f))));
            float sleigh = RoundRect(x, y, 128, 62, 30, 38, 12);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 52, 32, 40, 12), 8f));
            c = Over(c, Hex("C8352E"), Fill(sleigh));
            c = Over(c, Hex("F2B632"), Stroke(sleigh + 4f, 2.5f) * Fill(sleigh));
            float sack = Circle(x, y, 128, 50, 18);
            c = Over(c, Hex("9C7148"), Fill(sack));
            c = Over(c, Hex("2E8A5A"), Fill(RoundRect(x, y, 120, 56, 6, 6, 1)));
            c = Over(c, Hex("3E6EC8"), Fill(RoundRect(x, y, 136, 46, 6, 6, 1)));
            c = Over(c, Outline, Stroke(sack, 2.5f) * 0.8f);
            c = Over(c, Outline, Stroke(sleigh, 3.5f) * 0.85f);
            foreach (var (rx, ry) in new[] { (104f, 172f), (152f, 172f) })
            {
                var p = Local(x, y, rx, ry, 0f, 0.95f);
                c = Reindeer(p.x + 64f, p.y + 64f, c);
            }
            c = Over(c, Hex("D8322E"), Fill(Circle(x, y, 152, 213, 3.5f))); // that one has a red nose
            return c;
        }

        // A tall wooden nutcracker soldier on a plinth, seen from above and a little in front.
        static Color NutcrackerStatue(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float plinth = RoundRect(x, y, 128, 60, 54, 26, 4);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 110, 46, 96, 8), 10f));
            c = Over(c, Hex("6B4A2E"), Fill(plinth));
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 74, 54, 10, 4)));
            float legs = RoundRect(x, y, 128, 96, 24, 18, 3);
            float body = RoundRect(x, y, 128, 136, 34, 30, 6);
            float head = RoundRect(x, y, 128, 180, 20, 18, 6);
            float hat = RoundRect(x, y, 128, 214, 22, 20, 4);
            c = Over(c, Hex("222A35"), Fill(legs));
            c = Over(c, Hex("C8352E"), Fill(body));
            c = Over(c, Hex("F2B632"), Fill(Mathf.Min(RoundRect(x, y, 128, 136, 3, 28, 1), RoundRect(x, y, 128, 118, 34, 3, 1))));
            c = Over(c, Hex("F2D9B8"), Fill(head));
            c = Over(c, Color.white, Fill(RoundRect(x, y, 128, 168, 16, 7, 3)));
            c = Over(c, Outline, Fill(Mathf.Min(Circle(x, y, 121, 184, 2.5f), Circle(x, y, 135, 184, 2.5f))));
            c = Over(c, Hex("222A35"), Fill(hat));
            c = Over(c, Hex("F2B632"), Fill(RoundRect(x, y, 128, 200, 22, 3, 1)));
            c = Over(c, Outline, Stroke(Min(legs, body, head, hat), 3f) * 0.85f);
            return c;
        }

        // Two park benches under snow facing each other, footprints between.
        static Color Benches(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (float by in new[] { 70f, 186f })
            {
                float bench = RoundRect(x, y, 128, by, 84, 18, 3);
                c = Over(c, Shadow, Soft(RoundRect(x, y, 138, by - 8, 84, 18, 3), 6f));
                c = Over(c, Mathf.Repeat(y, 9f) < 2f ? Hex("6B4A2E") : Hex("9C7148"), Fill(bench));
                c = Over(c, Snow, Fill(bench + 4f) * (Noise(x * 0.1f, y * 0.1f, 32) > 0.4f ? 1f : 0f));
                c = Over(c, Outline, Stroke(bench, 3f) * 0.85f);
            }
            for (int i = 0; i < 5; i++)
                c = Over(c, SnowDeep, Fill(Ellipse(x, y, 90 + i * 20f, 128 + (i % 2 == 0 ? 6f : -6f), 4, 7)) * 0.7f);
            return c;
        }

        // A Christmas pyramid from above: the wooden blades of its top wheel, the carved figures on the tiers below.
        static Color Pyramid(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 96), 12f));
            float r = Dist(x, y, 128, 128), a = Mathf.Atan2(y - 128, x - 128);
            float baseDisc = r - 96f;
            c = Over(c, Hex("8E5C3B"), Fill(baseDisc));
            c = Over(c, Hex("B5834F"), Fill(r - 70f));
            for (int i = 0; i < 10; i++)
            {
                float fa = i * Mathf.PI * 2f / 10f;
                float fig = Circle(x, y, 128 + Mathf.Cos(fa) * 82, 128 + Mathf.Sin(fa) * 82, 7);
                c = Over(c, i % 2 == 0 ? Hex("C8352E") : Hex("2E8A5A"), Fill(fig));
                c = Over(c, Outline, Stroke(fig, 1.6f) * 0.7f);
            }
            // The wheel of blades, each a slanted wooden paddle.
            float blade = Mathf.Abs(Mathf.Repeat(a * 8f / (Mathf.PI * 2f), 1f) - 0.5f) * r * 0.8f - 10f;
            float wheel = Mathf.Max(blade, r - 66f);
            c = Over(c, Hex("E0B070"), Fill(wheel));
            c = Over(c, Hex("6B4A2E"), Stroke(wheel, 2.5f) * Fill(r - 66f));
            float hub = Circle(x, y, 128, 128, 12);
            c = Over(c, Hex("C8352E"), Fill(hub));
            c = Over(c, Hex("F2B632"), Fill(Circle(x, y, 128, 128, 5)));
            c = Over(c, Outline, Stroke(baseDisc, 4f) * 0.85f);
            return c;
        }
    }
}
