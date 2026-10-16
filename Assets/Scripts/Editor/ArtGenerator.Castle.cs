using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Art for the castle race (Valley Cup race 8): cobblestones, slate roofs and a scenery atlas for an old town
    /// on a castle hill (town houses, towers, market stalls, a well, barrels, banners, vineyard rows, cypresses,
    /// knights' tents, a knight statue, castle wall pieces, a hay cart, plus the usual trees, crowds and flowers).
    /// Same flat, outlined style as the rest.
    /// </summary>
    public static partial class ArtGenerator
    {
        public static readonly string[] CastleCells =
        {
            "TownHouse", "TownHouseBlue", "TownHouseGreen", "Tower",
            "MarketStall", "Well", "Barrels", "Banner",
            "Vines", "Cypress", "KnightTent", "Statue",
            "CastleWall", "Haycart", "Tree", "Bushes",
            "Crowd", "Flowers", "Rock", "Fountain",
        };

        const int CastleRows = 5;

        /// <summary>UV rect of a castle scenery cell by name (row 0 at the top of the texture).</summary>
        public static Rect CastleCell(string name)
        {
            int i = System.Array.IndexOf(CastleCells, name);
            int col = i % 4, row = i / 4;
            return new Rect(col * 0.25f, 1f - (row + 1) / (float)CastleRows, 0.25f, 1f / CastleRows);
        }

        [MenuItem("SomeGame/Generate Castle Art")]
        public static void GenerateCastle()
        {
            World("SceneryCastle", 1024, 256 * CastleRows, CastlePixel, 128, false);
            Material("SceneryCastle", "SceneryCastle");
            World("FloorCobbles", 128, 128, CobblesPixel, 32, true);
            World("RoofSlate", 128, 128, (x, y) => RoofPixel(x, y, Hex("56606E"), Hex("7A8594")), 32, true);
            foreach (var m in new[] { "FloorCobbles", "RoofSlate" }) Material(m, m);
            AssetDatabase.Refresh();
        }

        static Color CastlePixel(float x, float y)
        {
            int col = (int)(x / 256), row = CastleRows - 1 - (int)(y / 256);
            float lx = x - col * 256, ly = y - (CastleRows - 1 - row) * 256;
            return CastleCells[row * 4 + col] switch
            {
                "TownHouse" => TownHouse(lx, ly, Hex("C2502F")),
                "TownHouseBlue" => TownHouse(lx, ly, Hex("4F6E9C")),
                "TownHouseGreen" => TownHouse(lx, ly, Hex("4F8A5A")),
                "Tower" => Tower(lx, ly),
                "MarketStall" => Stall(lx, ly, Hex("3E9A57")),
                "Well" => Well(lx, ly),
                "Barrels" => Barrels(lx, ly),
                "Banner" => BannerPole(lx, ly),
                "Vines" => Vines(lx, ly),
                "Cypress" => Cypress(lx, ly),
                "KnightTent" => Tent(lx, ly, Hex("2F6DB5"), Hex("F5C842")),
                "Statue" => KnightStatue(lx, ly),
                "CastleWall" => CastleWall(lx, ly),
                "Haycart" => Haycart(lx, ly),
                "Tree" => RoundTree(lx, ly),
                "Bushes" => Bushes(lx, ly),
                "Crowd" => Crowd(lx, ly),
                "Flowers" => Flowers(lx, ly),
                "Rock" => Rock(lx, ly),
                _ => Fountain(lx, ly),
            };
        }

        // Round cobblestones in grey and sand tones, dark joints between them.
        static Color CobblesPixel(float x, float y)
        {
            const float cell = 16f;
            int row = Mathf.FloorToInt(y / cell);
            float ox = (row % 2) * cell * 0.5f;
            int col = Mathf.FloorToInt((x + ox) / cell);
            float cx = col * cell - ox + cell * 0.5f, cy = row * cell + cell * 0.5f;
            float k = Hash(col * 3 + 11, row * 7 + 5);
            float d = Dist(x, y, cx + (k - 0.5f) * 2f, cy + (Hash(col, row) - 0.5f) * 2f) - cell * 0.44f;
            Color stone = Color.Lerp(Hex("9C958A"), Hex("BDB3A2"), k);
            stone = Color.Lerp(stone, Shade(stone, 1.12f), Mathf.Clamp01(((cy - y) + (x - cx)) / cell + 0.3f) * 0.4f);
            return d < 0f ? stone : Hex("6E675E");
        }

        // A town house from above: a pitched roof in two shades (ridge along x), a chimney, the eaves.
        static Color TownHouse(float x, float y, Color roof)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 112, 104, 76, 6), 12f));
            float body = RoundRect(x, y, 128, 128, 104, 76, 6);
            bool far = y > 128f;
            Color tone = far ? Shade(roof, 1.1f) : Shade(roof, 0.82f);
            if (Mathf.Repeat(Mathf.Abs(y - 128f), 14f) < 2f) tone = Shade(tone, 0.85f); // rows of tiles
            if (Mathf.Repeat(x + (Mathf.Floor(Mathf.Abs(y - 128f) / 14f) % 2) * 9f, 18f) < 1.5f) tone = Shade(tone, 0.9f);
            c = Over(c, tone, Fill(body));
            c = Over(c, Shade(roof, 0.65f), Fill(Mathf.Abs(y - 128f) - 3f) * Fill(body)); // ridge
            c = Over(c, Outline, Stroke(body, 5f) * 0.85f);
            float chimney = RoundRect(x, y, 182, 162, 12, 12, 2);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 190, 154, 12, 12, 2), 4f));
            c = Over(c, Hex("9A5A3C"), Fill(chimney));
            c = Over(c, Hex("3A2E2A"), Fill(RoundRect(x, y, 182, 162, 6, 6, 1)));
            c = Over(c, Outline, Stroke(chimney, 3f) * 0.8f);
            return c;
        }

        // A round tower from above: a stone ring with battlements, a pointed slate roof and a flag.
        static Color Tower(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 150, 106, 104), 16f));
            float wall = Circle(x, y, 128, 128, 104);
            float a = Mathf.Atan2(y - 128, x - 128);
            float merlon = Mathf.Repeat(a / (Mathf.PI * 2f) * 16f, 1f) < 0.5f ? 1f : 0f;
            c = Over(c, Color.Lerp(Hex("A8A196"), Hex("C4BDB0"), merlon), Fill(wall));
            c = Over(c, Outline, Stroke(wall, 5f) * 0.85f);
            float roof = Circle(x, y, 128, 128, 80);
            float wedge = Mathf.Repeat(a / (Mathf.PI * 2f) * 12f, 1f);
            Color slate = wedge < 0.5f ? Hex("56606E") : Hex("68737F");
            float shade = Mathf.Clamp01(0.5f + ((x - 128) - (y - 128)) / 220f);
            c = Over(c, Color.Lerp(slate, Shade(slate, 0.75f), shade), Fill(roof));
            c = Over(c, Outline, Stroke(roof, 4f) * 0.8f);
            c = Over(c, Hex("5A3A22"), Fill(Circle(x, y, 128, 128, 6)));
            c = Over(c, Hex("E2513A"), Fill(Polygon(x, y, new[] { new Vector2(128, 132), new Vector2(176, 146), new Vector2(128, 160) })));
            c = Over(c, Outline, Stroke(Polygon(x, y, new[] { new Vector2(128, 132), new Vector2(176, 146), new Vector2(128, 160) }), 2.5f) * 0.6f);
            return c;
        }

        // A stone well with a little wooden roof and a bucket.
        static Color Well(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 70), 12f));
            float ring = Circle(x, y, 128, 128, 70);
            c = Over(c, Mathf.Repeat(Mathf.Atan2(y - 128, x - 128) * 70f / 14f, 2f) < 1f ? Hex("A8A196") : Hex("BDB6A9"), Fill(ring));
            c = Over(c, Hex("2C4A5E"), Fill(Circle(x, y, 128, 128, 46)));
            c = Over(c, Hex("4A7A96"), Fill(Circle(x, y, 120, 136, 18)) * 0.5f);
            c = Over(c, Outline, Stroke(ring, 4f) * 0.85f);
            float roof = RoundRect(x, y, 128, 128, 84, 24, 4);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 136, 118, 84, 24, 4), 6f));
            c = Over(c, y > 128 ? Hex("A87444") : Hex("8A5A32"), Fill(roof));
            c = Over(c, Outline, Stroke(roof, 3.5f) * 0.85f);
            c = Over(c, Hex("6B4A2E"), Fill(Circle(x, y, 176, 76, 12)));
            c = Over(c, Outline, Stroke(Circle(x, y, 176, 76, 12), 2.5f) * 0.7f);
            return c;
        }

        // A group of wooden barrels and a crate.
        static Color Barrels(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (bx, by, r) in new[] { (84f, 150f, 40f), (164f, 160f, 36f), (110f, 76f, 34f) })
            {
                c = Over(c, Shadow, Soft(Circle(x, y, bx + 8, by - 8, r), 6f));
                float b = Circle(x, y, bx, by, r);
                c = Over(c, Mathf.Repeat(Mathf.Atan2(y - by, x - bx) * r / 9f, 2f) < 1f ? Hex("A87444") : Hex("B9874E"), Fill(b));
                c = Over(c, Hex("5E5A56"), Fill(Shell(Circle(x, y, bx, by, r - 7), 2.5f)));
                c = Over(c, Hex("8A5A32"), Fill(Circle(x, y, bx, by, r * 0.4f)));
                c = Over(c, Outline, Stroke(b, 3.5f) * 0.85f);
            }
            float crate = RoundRect(x, y, 186, 78, 32, 32, 3);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 194, 70, 32, 32, 3), 6f));
            c = Over(c, Hex("C99760"), Fill(crate));
            c = Over(c, Hex("8A5A32"), Fill(Mathf.Min(Mathf.Abs((x - 186) - (y - 78)), Mathf.Abs((x - 186) + (y - 78))) - 3f) * Fill(crate));
            c = Over(c, Outline, Stroke(crate, 3.5f) * 0.85f);
            return c;
        }

        // A banner pole from above: the post and a long pennant streaming out in red and gold.
        static Color BannerPole(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var flag = new[] { new Vector2(70, 150), new Vector2(200, 168), new Vector2(232, 140), new Vector2(196, 122), new Vector2(70, 112) };
            c = Over(c, Shadow, Soft(Polygon(x, y, System.Array.ConvertAll(flag, p => p + new Vector2(10, -12))), 8f));
            float f = Polygon(x, y, flag);
            c = Over(c, Mathf.Repeat(x / 32f, 2f) < 1f ? Hex("D9412F") : Hex("F5C842"), Fill(f));
            c = Over(c, Outline, Stroke(f, 3.5f) * 0.8f);
            c = Over(c, Hex("5A3A22"), Fill(Circle(x, y, 66, 131, 12)));
            c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 66, 131, 6)));
            return c;
        }

        // Rows of vines (a square field): leafy green rows with dark gaps and bunches of grapes.
        static Color Vines(float x, float y)
        {
            float field = RoundRect(x, y, 128, 128, 122, 122, 6);
            if (field > 1f) return new Color(0, 0, 0, 0);
            float u = Mathf.Repeat(x, 32f);
            Color c = Hex("8A6A40");
            float leaf = Mathf.Abs(u - 16f) - 9f + Mathf.Sin(y * 0.4f + Mathf.Floor(x / 32f)) * 2f;
            if (leaf < 0f)
            {
                c = Color.Lerp(Hex("4E8A3A"), Hex("6FAE48"), Noise(x / 7f, y / 7f, 37));
                if (Hash((int)(x / 7f), (int)(y / 9f)) > 0.86f && Mathf.Abs(u - 16f) < 5f) c = Hex("6C3F8A"); // grapes
            }
            c.a = Fill(field);
            return c;
        }

        // A cypress from above: a slim dark-green crown with a lighter top.
        static Color Cypress(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 150, 108, 46, 60), 10f));
            float crown = Ellipse(x, y, 128, 128, 46, 60) + Noise(x / 10f, y / 10f, 26) * 6f;
            c = Over(c, Hex("2E5A3A"), Fill(crown));
            c = Over(c, Hex("3E7A4A"), Fill(Ellipse(x, y, 120, 138, 26, 36)));
            c = Over(c, Hex("5A9A5E"), Fill(Ellipse(x, y, 116, 144, 10, 14)));
            c = Over(c, Outline, Stroke(crown, 4f) * 0.8f);
            return c;
        }

        // A knight statue on a pedestal: helmet, shield and a sword, all in stone.
        static Color KnightStatue(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 144, 112, 80, 80, 8), 14f));
            float plinth = RoundRect(x, y, 128, 128, 80, 80, 8);
            c = Over(c, Hex("B5AEA2"), Fill(plinth));
            c = Over(c, Hex("CFC8BB"), Fill(RoundRect(x, y, 128, 128, 64, 64, 6)));
            c = Over(c, Outline, Stroke(plinth, 4f) * 0.8f);
            c = Over(c, Hex("8E8A84"), Fill(Ellipse(x, y, 128, 124, 40, 30))); // shoulders
            c = Over(c, Hex("A3A099"), Fill(Circle(x, y, 128, 130, 20))); // helmet
            c = Over(c, Hex("6E6A64"), Fill(RoundRect(x, y, 128, 134, 14, 3, 1)));
            float shield = Polygon(x, y, new[] { new Vector2(74, 140), new Vector2(102, 140), new Vector2(102, 110), new Vector2(88, 96), new Vector2(74, 110) });
            c = Over(c, Hex("C2502F"), Fill(shield));
            c = Over(c, Outline, Stroke(shield, 2.5f) * 0.7f);
            c = Over(c, Hex("D8D4CC"), Fill(RoundRect(x, y, 170, 120, 4, 44, 2)));
            c = Over(c, Hex("8E8A84"), Fill(RoundRect(x, y, 170, 150, 14, 3, 1)));
            return c;
        }

        // A piece of castle wall running along x across the whole cell (pieces join up): a walkway between
        // battlements on both sides.
        static Color CastleWall(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            if (y < 72f || y > 184f) return c;
            c = Over(c, Shadow, Fill(Mathf.Abs(y - 116f) - 54f) * 0.9f);
            float wall = Mathf.Abs(y - 128f) - 48f;
            Color stone = Color.Lerp(Hex("A8A196"), Hex("BDB6A9"), Hash((int)((x + (Mathf.Floor(y / 16f) % 2) * 12f) / 24f), (int)(y / 16f)));
            if (Mathf.Repeat(y, 16f) < 1.5f || Mathf.Repeat(x + (Mathf.Floor(y / 16f) % 2) * 12f, 24f) < 1.5f) stone = Hex("8C867B");
            c = Over(c, stone, Fill(wall));
            c = Over(c, Hex("CFC8BB"), Fill(Mathf.Abs(y - 128f) - 28f)); // the walkway
            foreach (float side in new[] { 1f, -1f })
            {
                float my = 128f + side * 40f;
                bool merlon = Mathf.Repeat(x, 32f) < 18f;
                if (merlon) c = Over(c, Hex("9A9488"), Fill(Mathf.Abs(y - my) - 8f));
            }
            c = Over(c, Outline, Stroke(wall, 4f) * 0.85f);
            return c;
        }

        // A hay cart: wooden wheels, the cart bed and a heap of hay.
        static Color Haycart(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 90, 60, 8), 12f));
            foreach (var (wx, wy) in new[] { (68f, 60f), (188f, 60f), (68f, 196f), (188f, 196f) })
            {
                c = Over(c, Hex("6B4A2E"), Fill(RoundRect(x, y, wx, wy, 10, 24, 4)));
                c = Over(c, Outline, Stroke(RoundRect(x, y, wx, wy, 10, 24, 4), 2.5f) * 0.7f);
            }
            float bed = RoundRect(x, y, 128, 128, 84, 64, 6);
            c = Over(c, Hex("A87444"), Fill(bed));
            float hay = Ellipse(x, y, 128, 130, 70, 52) + Noise(x / 6f, y / 6f, 43) * 6f;
            c = Over(c, Color.Lerp(Hex("E3C25A"), Hex("F2D97A"), Noise(x / 4f, y / 9f, 64)), Fill(hay));
            c = Over(c, Hex("C9A23E"), Stroke(hay, 3f) * 0.8f);
            c = Over(c, Outline, Stroke(bed, 4f) * 0.85f);
            c = Over(c, Hex("6B4A2E"), Fill(RoundRect(x, y, 128, 20, 4, 30, 2))); // the shaft
            return c;
        }
    }
}
