using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The chalet scenery atlas (40 cells of 256 px, 4 x 10) for the Winter Cup finale races: the snowy basics of the other
    /// winter sheets outside, and inside the luxury chalets the cars race through - leather sofas, wingback chairs, a stone
    /// fireplace with a fire, a grand piano, a bearskin and a Persian rug, a banquet table, a bar, bookcases, palms, champagne
    /// on ice, a marble bust, a four-poster bed, spa loungers, towels, a telescope at the window, a billiard table, a wine
    /// rack, ottomans, candelabras, an indoor fountain, an aquarium, a jukebox and a DJ booth. Also the chalet floors
    /// (polished marble, oak, glass over the abyss) and the chalet roof. Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] ChaletCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "WinterCrowd", "SnowDrift", "Boulders",
            "RockSpire", "Signpost", "PeakFlag", "SkiRack",
            "Igloo", "Bench", "ChristmasTree", "Gifts",
            "LeatherSofa", "Wingback", "Fireplace", "GrandPiano",
            "BearRug", "PersianRug", "BanquetTable", "Bar",
            "Bookcase", "Palm", "ChampagneTable", "MarbleBust",
            "FourPoster", "Lounger", "Towels", "Telescope",
            "BilliardTable", "WineRack", "Ottoman", "Candelabra",
            "Fountain", "Aquarium", "Jukebox", "DjBooth",
        };

        const int ChaletRows = 10;

        static readonly Color Leather = Hex("9A5A2E"), LeatherLight = Hex("B8743F"), LeatherDark = Hex("6E3C1C"), Cream = Hex("F2E6CF"),
            LuxGold = Hex("E2B547"), LuxGoldDark = Hex("A87A22"), Burgundy = Hex("7A1E2E"), Forest = Hex("2F5D46"), Navy = Hex("23365C"),
            MarbleWhite = Hex("F1EEE8"), Walnut = Hex("5A3418"), WalnutLight = Hex("7D4E2A"), Ebony = Hex("1E1C22");

        /// <summary>UV rect of a chalet scenery cell by name (row 0 at the bottom).</summary>
        public static Rect ChaletCell(string name)
        {
            int i = System.Array.IndexOf(ChaletCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)ChaletRows, 0.25f, 1f / ChaletRows);
        }

        [MenuItem("SomeGame/Generate Chalet Art")]
        public static void GenerateChalet()
        {
            Write(ArtGenerator.Folder, "SceneryChalet", 1024, 256 * ChaletRows, ChaletPixel, 128, false);
            ArtGenerator.Material("SceneryChalet", "SceneryChalet");
            Write(ArtGenerator.Folder, "FloorMarble", 128, 128, MarbleFloorPixel, 32, true);
            Write(ArtGenerator.Folder, "FloorOak", 128, 128, OakFloorPixel, 32, true);
            Write(ArtGenerator.Folder, "FloorGlass", 128, 128, GlassFloorPixel, 32, true);
            Write(ArtGenerator.Folder, "RoofChalet", 128, 128, ChaletRoofPixel, 32, true);
            foreach (var m in new[] { "FloorMarble", "FloorOak", "FloorGlass", "RoofChalet" }) ArtGenerator.Material(m, m);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color ChaletPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return ChaletCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Boulders" => Boulders(lx, ly),
                "RockSpire" => RockSpire(lx, ly),
                "Signpost" => Signpost(lx, ly),
                "PeakFlag" => PeakFlag(lx, ly),
                "SkiRack" => SkiRack(lx, ly),
                "Igloo" => Igloo(lx, ly),
                "Bench" => SnowBench(lx, ly),
                "ChristmasTree" => ChristmasTree(lx, ly),
                "Gifts" => Gifts(lx, ly),
                "LeatherSofa" => LuxSofa(lx, ly),
                "Wingback" => LuxWingback(lx, ly),
                "Fireplace" => LuxFireplace(lx, ly),
                "GrandPiano" => LuxPiano(lx, ly),
                "BearRug" => LuxBearRug(lx, ly),
                "PersianRug" => LuxPersianRug(lx, ly),
                "BanquetTable" => LuxBanquet(lx, ly),
                "Bar" => LuxBar(lx, ly),
                "Bookcase" => LuxBookcase(lx, ly),
                "Palm" => LuxPalm(lx, ly),
                "ChampagneTable" => LuxChampagne(lx, ly),
                "MarbleBust" => LuxBust(lx, ly),
                "FourPoster" => LuxBed(lx, ly),
                "Lounger" => LuxLounger(lx, ly),
                "Towels" => LuxTowels(lx, ly),
                "Telescope" => LuxTelescope(lx, ly),
                "BilliardTable" => LuxBilliardTable(lx, ly),
                "WineRack" => LuxWineRack(lx, ly),
                "Ottoman" => LuxOttoman(lx, ly),
                "Candelabra" => LuxCandelabra(lx, ly),
                "Fountain" => LuxFountain(lx, ly),
                "Aquarium" => LuxAquarium(lx, ly),
                "Jukebox" => LuxJukebox(lx, ly),
                _ => LuxDjBooth(lx, ly),
            };
        }

        // ------------------------------------------------------------------ helpers

        static Color Drop(Color c, float d, float blur = 12f) => Over(c, Shadow, Soft(d, blur));

        // A soft, padded cushion: a lighter highlight up-left, a darker seam round it.
        static Color Pad(Color c, float d, Color fabric)
        {
            c = Over(c, fabric, Fill(d));
            c = Over(c, Color.Lerp(fabric, Color.white, 0.22f), Fill(d + 7f) * 0.55f);
            c = Over(c, Shade(fabric, 0.62f), Stroke(d, 2.5f));
            return c;
        }

        // ------------------------------------------------------------------ furniture

        // A deep cognac leather sofa: a rolled back and arms, three seat cushions with buttons, a tartan throw.
        static Color LuxSofa(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 122, 62, 22));
            float body = RoundRect(x, y, 128, 128, 122, 62, 22);
            c = Over(c, LeatherDark, Fill(body));
            c = Pad(c, RoundRect(x, y, 128, 174, 114, 16, 14), LeatherLight);
            c = Pad(c, RoundRect(x, y, 14, 124, 12, 54, 12), LeatherLight);
            c = Pad(c, RoundRect(x, y, 242, 124, 12, 54, 12), LeatherLight);
            for (int i = 0; i < 3; i++)
            {
                float cx = 56 + i * 72;
                c = Pad(c, RoundRect(x, y, cx, 112, 34, 40, 10), Leather);
                foreach (var (bx, by) in new[] { (cx - 14f, 124f), (cx + 14f, 124f), (cx, 104f), (cx - 14f, 86f), (cx + 14f, 86f) })
                    c = Over(c, LeatherDark, Fill(Circle(x, y, bx, by, 2.4f)));
            }
            // A tartan throw over one end.
            float throwD = RotRect(x, y, 196, 120, 40, 50, 6, 0.18f);
            Color tartan = Mathf.Repeat(x + y * 0.3f, 18f) < 6f || Mathf.Repeat(y - x * 0.3f, 18f) < 6f ? Hex("B32D2D") : Hex("2B4D3A");
            c = Over(c, tartan, Fill(throwD));
            c = Over(c, Shade(Hex("2B4D3A"), 0.7f), Stroke(throwD, 2f));
            c = Over(c, Outline, Stroke(body, 4.5f));
            return c;
        }

        // A green velvet wingback armchair with a footstool.
        static Color LuxWingback(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 136, 70, 70, 24));
            float body = RoundRect(x, y, 128, 150, 70, 70, 24);
            c = Over(c, Shade(Forest, 0.75f), Fill(body));
            c = Pad(c, RoundRect(x, y, 128, 204, 62, 16, 14), Forest);              // the back
            c = Pad(c, RoundRect(x, y, 70, 168, 14, 44, 12), Forest);               // the wings
            c = Pad(c, RoundRect(x, y, 186, 168, 14, 44, 12), Forest);
            c = Pad(c, RoundRect(x, y, 128, 138, 42, 40, 14), Color.Lerp(Forest, Hex("4F8A6A"), 0.5f));
            c = Over(c, Outline, Stroke(body, 4f));
            // The footstool in front.
            float stool = RoundRect(x, y, 128, 46, 40, 24, 12);
            c = Drop(c, RoundRect(x, y, 138, 36, 40, 24, 12), 8f);
            c = Pad(c, stool, Forest);
            c = Over(c, LuxGold, Fill(Circle(x, y, 128, 46, 4)));
            c = Over(c, Outline, Stroke(stool, 3.5f));
            return c;
        }

        // A big stone fireplace from above: a hearth of flagstones, the chimney breast, logs burning in the grate.
        static Color LuxFireplace(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 160, 116, 70, 8));
            float hearth = RoundRect(x, y, 128, 120, 108, 74, 6);
            c = Over(c, Hex("8F8A84"), Fill(hearth));
            // Flagstones.
            float sx = Mathf.Floor(x / 34f), sy = Mathf.Floor((y + Mathf.Floor(x / 34f) * 11f) / 26f);
            c = Over(c, Color.Lerp(Hex("A39D95"), Hex("7F7A73"), Hash((int)sx, (int)sy)), Fill(hearth) * 0.8f);
            if (Mathf.Repeat(x, 34f) < 2f || Mathf.Repeat(y + sx * 11f, 26f) < 2f) c = Over(c, Hex("5E5A55"), Fill(hearth));
            // The chimney breast: big rounded stones.
            float breast = RoundRect(x, y, 128, 196, 112, 44, 10);
            c = Over(c, Hex("6F6A64"), Fill(breast));
            for (int i = 0; i < 14; i++)
            {
                float bx = 26 + (i % 7) * 34 + (i / 7) * 16, by = 178 + (i / 7) * 34;
                float stone = RoundRect(x, y, bx, by, 15, 13, 8);
                c = Over(c, Color.Lerp(Hex("9C958C"), Hex("C3BCB1"), Hash(i, 7)), Fill(stone) * Fill(breast));
                c = Over(c, Hex("4E4A45"), Stroke(stone, 2f) * Fill(breast));
            }
            // The fire: logs in the grate, flames and embers.
            float grate = RoundRect(x, y, 128, 140, 60, 30, 8);
            c = Over(c, Hex("201A18"), Fill(grate));
            for (int i = 0; i < 3; i++) c = Over(c, Hex("6B4226"), Fill(RotRect(x, y, 104 + i * 24, 134 + (i % 2) * 8, 8, 26, 6, 0.5f - i * 0.4f)));
            float flame = Mathf.Min(Ellipse(x, y, 112, 146, 22, 16), Ellipse(x, y, 140, 144, 20, 18));
            c = Over(c, Hex("F07A22"), Fill(flame));
            c = Over(c, Hex("FFC23A"), Fill(flame + 7f));
            c = Over(c, Hex("FFF1B0"), Fill(flame + 12f));
            c = Over(c, Hex("2A2422"), Stroke(grate, 4f));
            // A brass fender in front and the fire irons.
            c = Over(c, LuxGold, Stroke(RoundRect(x, y, 128, 98, 72, 6, 3), 4f));
            c = Over(c, Ebony, Fill(Capsule(x, y, 222, 70, 222, 120, 3f)));
            c = Over(c, LuxGold, Fill(Circle(x, y, 222, 124, 5f)));
            c = Over(c, Outline, Stroke(Min(hearth, breast), 4.5f));
            return c;
        }

        // A black grand piano from above, its lid open, the keys and a bench.
        static Color LuxPiano(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // The case: straight along the keyboard and the left, the bentside curving in towards the tail.
            float body = Mathf.Max(RoundRect(x, y, 122, 140, 78, 96, 16), -Circle(x, y, 252, 262, 108f));
            c = Drop(c, body);
            c = Over(c, Ebony, Fill(body));
            // The open lid shows the gold frame and the strings.
            float inside = body + 10f;
            c = Over(c, Hex("C9A04A"), Fill(inside));
            for (int i = 0; i < 14; i++) c = Over(c, Hex("EDE3C5"), Fill(Mathf.Abs(x - (66 + i * 9f)) - 0.7f) * Fill(inside + 6f));
            c = Over(c, Hex("3A3238"), Stroke(inside, 3f));
            // The keyboard along the bottom.
            float keys = RoundRect(x, y, 122, 52, 70, 10, 2);
            c = Over(c, Color.white, Fill(keys));
            for (int i = 0; i < 24; i++)
            {
                float kx = 56 + i * 5.7f;
                c = Over(c, Hex("7A7A80"), Fill(Mathf.Abs(x - kx) - 0.4f) * Fill(keys));
                if (i % 7 != 2 && i % 7 != 6) c = Over(c, Ebony, Fill(RoundRect(x, y, kx + 2.8f, 57, 1.8f, 5, 0.5f)));
            }
            c = Over(c, Outline, Stroke(body, 4f));
            // The bench.
            float bench = RoundRect(x, y, 122, 22, 40, 10, 4);
            c = Pad(c, bench, Hex("2A2830"));
            c = Over(c, Outline, Stroke(bench, 3f));
            return c;
        }

        // A white bearskin rug, flat on the floor: the pelt with four paws and the head.
        static Color LuxBearRug(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float pelt = SMin(Ellipse(x, y, 128, 118, 70, 92), Min(
                Capsule(x, y, 128, 150, 52, 206, 18), Capsule(x, y, 128, 150, 204, 206, 18),
                Capsule(x, y, 128, 90, 50, 40, 18), Capsule(x, y, 128, 90, 206, 40, 18)), 18f);
            pelt = SMin(pelt, Circle(x, y, 128, 214, 30), 12f);
            Color fur = Color.Lerp(Hex("F4F1EA"), Hex("DCD4C6"), Noise(x / 9f, y / 9f, 28) * 0.8f);
            c = Over(c, fur, Fill(pelt));
            c = Over(c, Hex("C9BFAE"), Stroke(pelt, 3f));
            // The head: ears, eyes and a dark nose.
            c = Over(c, Hex("E6DECF"), Fill(Circle(x, y, 108, 236, 9)) + Fill(Circle(x, y, 148, 236, 9)));
            c = Over(c, Ebony, Fill(Circle(x, y, 116, 214, 3.5f)) + Fill(Circle(x, y, 140, 214, 3.5f)));
            c = Over(c, Ebony, Fill(Ellipse(x, y, 128, 196, 8, 6)));
            foreach (var (px, py) in new[] { (50f, 208f), (206f, 208f), (48f, 38f), (208f, 38f) })
                for (int k = -1; k <= 1; k++) c = Over(c, Hex("8C8478"), Fill(Circle(x, y, px + k * 7f, py + (py > 128 ? 12f : -12f), 2.2f)));
            c.a *= 0.97f;
            return c;
        }

        // A deep red Persian rug, flat: a medallion, a patterned border, fringes at the ends.
        static Color LuxPersianRug(float x, float y)
        {
            float rug = RoundRect(x, y, 128, 128, 104, 122, 2);
            if (rug > 1f && !(Mathf.Abs(x - 128) < 104 && Mathf.Abs(y - 128) < 128 && Mathf.Repeat(x, 6f) < 2.5f)) return new Color(0, 0, 0, 0);
            if (rug > 0f) return Cream; // the fringe
            Color c = Burgundy;
            float bx = Mathf.Abs(x - 128), by = Mathf.Abs(y - 128);
            bool border = bx > 82 || by > 100;
            if (border)
            {
                c = Navy;
                float t = Mathf.Repeat((bx > 82 ? y : x) / 14f, 1f);
                if (Mathf.Abs(t - 0.5f) < 0.18f) c = LuxGold;
                if (bx > 98 || by > 116) c = Burgundy;
                if (Mathf.Abs(bx - 82) < 2f && by <= 100 || Mathf.Abs(by - 100) < 2f && bx <= 82) c = Cream;
            }
            else
            {
                float m = bx / 70f + by / 86f;
                if (m < 1f) c = Navy;
                if (m < 0.75f) c = Hex("C9573D");
                if (m < 0.45f) c = LuxGold;
                if (m < 0.22f) c = Navy;
                if (Mathf.Abs(m - 1f) < 0.03f || Mathf.Abs(m - 0.75f) < 0.03f) c = Cream;
                // Little flowers scattered in the field.
                if (m > 1.05f && Hash((int)(x / 12), (int)(y / 12)) > 0.7f && Circle(Mathf.Repeat(x, 12f), Mathf.Repeat(y, 12f), 6, 6, 2.5f) < 0f) c = Cream;
            }
            if (Hash((int)x, (int)y) > 0.93f) c = Shade(c, 0.9f);
            return c;
        }

        // A long banquet table: white cloth, ten chairs, plates, glasses and two candelabras.
        static Color LuxBanquet(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 118, 70, 10));
            for (int i = 0; i < 5; i++)
                foreach (float cy in new[] { 40f, 216f })
                {
                    float chair = RoundRect(x, y, 32 + i * 48, cy, 16, 14, 5);
                    c = Pad(c, chair, Burgundy);
                    c = Over(c, Walnut, Stroke(chair, 3f));
                }
            float table = RoundRect(x, y, 128, 128, 116, 64, 8);
            c = Over(c, Color.white, Fill(table));
            c = Over(c, Hex("E9E4DA"), Fill(Mathf.Abs(y - 128f) - 30f) * Fill(table) * 0.5f);
            c = Over(c, LuxGold, Fill(Mathf.Abs(y - 128f) - 3f) * Fill(table)); // the runner
            for (int i = 0; i < 5; i++)
                foreach (float py in new[] { 84f, 172f })
                {
                    float px = 32 + i * 48;
                    c = Over(c, Hex("D6D9DE"), Fill(Circle(x, y, px, py, 13)));
                    c = Over(c, Color.white, Fill(Circle(x, y, px, py, 9)));
                    c = Over(c, Hex("A9C9E0"), Fill(Circle(x, y, px + 18, py + (py > 128 ? -10 : 10), 4.5f)));
                }
            foreach (float cx in new[] { 80f, 176f })
            {
                c = Over(c, LuxGoldDark, Fill(Circle(x, y, cx, 128, 11)));
                for (int k = 0; k < 5; k++)
                {
                    float a = k * Mathf.PI * 2f / 5f;
                    c = Over(c, Hex("FFF1B0"), Fill(Circle(x, y, cx + Mathf.Cos(a) * 9f, 128 + Mathf.Sin(a) * 9f, 2.6f)));
                }
            }
            c = Over(c, Outline, Stroke(table, 4f));
            return c;
        }

        // A curved bar counter of dark wood with a brass rail, bottles behind it, stools in front.
        static Color LuxBar(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float counter = Mathf.Max(Ellipse(x, y, 128, 210, 118, 120), -Ellipse(x, y, 128, 210, 86, 88));
            counter = Mathf.Max(counter, 90f - y);
            c = Drop(c, counter);
            c = Over(c, Walnut, Fill(counter));
            c = Over(c, WalnutLight, Fill(counter + 6f) * 0.6f);
            c = Over(c, LuxGold, Stroke(Ellipse(x, y, 128, 210, 112, 114), 3f) * Fill(counter + 1f));
            c = Over(c, Outline, Stroke(counter, 4f));
            // Bottles on the shelf behind.
            Color[] glass = { Hex("2F9A4F"), Hex("8A1E2E"), Hex("F2C230"), Hex("A9C9E0"), Hex("6A3FB5"), Hex("C26A2A") };
            for (int i = 0; i < 9; i++)
            {
                float bx = 64 + i * 16, by = 222 + (i % 2) * 6;
                c = Over(c, glass[i % glass.Length], Fill(Circle(x, y, bx, by, 6)));
                c = Over(c, Color.white, Fill(Circle(x, y, bx - 2, by + 2, 1.6f)) * 0.8f);
            }
            // Stools.
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI * (0.15f + i * 0.175f);
                float sx = 128 - Mathf.Cos(a) * 140f, sy = 210 - Mathf.Sin(a) * 140f;
                if (sy < 8) continue;
                c = Drop(c, Circle(x, y, sx + 6, sy - 6, 15), 6f);
                c = Pad(c, Circle(x, y, sx, sy, 15), Burgundy);
                c = Over(c, Outline, Stroke(Circle(x, y, sx, sy, 15), 3f));
            }
            return c;
        }

        // A tall walnut bookcase seen from above: leather-bound books in rows, gold-tooled spines.
        static Color LuxBookcase(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 124, 42, 6));
            float shelf = RoundRect(x, y, 128, 128, 124, 42, 6);
            c = Over(c, Walnut, Fill(shelf));
            Color[] spines = { Burgundy, Navy, Forest, Hex("6E3C1C"), Hex("3A2A4A"), Hex("8A6A2A") };
            for (int i = 0; i < 26; i++)
            {
                float bx = 12 + i * 9.3f, h = 28 + Hash(i, 3) * 8;
                Color sp = spines[(int)(Hash(i, 5) * spines.Length) % spines.Length];
                c = Over(c, sp, Fill(RoundRect(x, y, bx, 128, 4f, h, 1.2f)) * Fill(shelf + 4f));
                c = Over(c, LuxGold, Fill(RoundRect(x, y, bx, 128 + h * 0.5f, 3f, 1.2f, 0.4f)) * Fill(shelf + 4f));
            }
            c = Over(c, Outline, Stroke(shelf, 4f));
            return c;
        }

        // A potted palm in a brass planter.
        static Color LuxPalm(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, Circle(x, y, 138, 116, 104), 16f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f + 0.2f;
                Vector2 tip = new Vector2(128, 128) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 112f;
                Vector2 mid = new Vector2(128, 128) + new Vector2(Mathf.Cos(a + 0.12f), Mathf.Sin(a + 0.12f)) * 60f;
                float frond = Mathf.Min(Capsule(x, y, 128, 128, mid.x, mid.y, 20f), Capsule(x, y, mid.x, mid.y, tip.x, tip.y, 12f));
                c = Over(c, i % 2 == 0 ? Hex("3E8A35") : Hex("2F7A3A"), Fill(frond));
                // the leaflets: a comb along the frond
                float along = Vector2.Dot(new Vector2(x, y) - new Vector2(128, 128), (tip - new Vector2(128, 128)).normalized);
                if (Mathf.Repeat(along, 9f) < 2f) c = Over(c, Hex("1F5A28"), Fill(frond) * 0.8f);
                c = Over(c, Hex("8ACB52"), Fill(Capsule(x, y, 128, 128, tip.x, tip.y, 1.6f)) * Fill(frond));
            }
            c = Over(c, LuxGold, Fill(Circle(x, y, 128, 128, 30)));
            c = Over(c, Hex("4A3020"), Fill(Circle(x, y, 128, 128, 22)));
            c = Over(c, Outline, Stroke(Circle(x, y, 128, 128, 30), 3f));
            return c;
        }

        // A round marble side table: champagne on ice in a silver bucket, two flutes.
        static Color LuxChampagne(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, Circle(x, y, 140, 116, 80));
            float top = Circle(x, y, 128, 128, 80);
            c = Over(c, MarbleWhite, Fill(top));
            c = Over(c, Hex("CFC9C0"), Fill(Mathf.Abs(Mathf.Sin(x * 0.05f + y * 0.03f) * 30f + x - y) - 1.5f) * Fill(top) * 0.7f);
            c = Over(c, LuxGold, Stroke(top, 5f));
            float bucket = Circle(x, y, 112, 136, 34);
            c = Over(c, Hex("C9CED6"), Fill(bucket));
            c = Over(c, Hex("EEF3F8"), Fill(bucket + 6f));
            for (int i = 0; i < 6; i++) c = Over(c, Hex("DDF2FF"), Fill(RotRect(x, y, 98 + (i % 3) * 13, 124 + (i / 3) * 20, 6, 5, 1, i)));
            c = Over(c, Hex("1F4A2A"), Fill(Circle(x, y, 116, 140, 11)));   // the bottle
            c = Over(c, LuxGold, Fill(Circle(x, y, 116, 140, 6)));
            c = Over(c, Outline, Stroke(bucket, 3f));
            foreach (var (fx, fy) in new[] { (166f, 156f), (170f, 100f) })
            {
                c = Over(c, Hex("F6E7A8"), Fill(Circle(x, y, fx, fy, 9)));
                c = Over(c, Color.white, Stroke(Circle(x, y, fx, fy, 9), 2f));
            }
            c = Over(c, Outline, Stroke(top, 4f) * 0.9f);
            return c;
        }

        // A marble bust on a column, seen from above: the round plinth, the head with its curls and a laurel.
        static Color LuxBust(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 114, 70, 70, 6));
            float plinth = RoundRect(x, y, 128, 128, 70, 70, 6);
            c = Over(c, Hex("DAD5CC"), Fill(plinth));
            c = Over(c, MarbleWhite, Fill(plinth + 8f));
            c = Over(c, Hex("B8B1A6"), Stroke(plinth + 8f, 2f));
            float shoulders = Ellipse(x, y, 128, 118, 54, 30);
            c = Over(c, Hex("E8E3DA"), Fill(shoulders));
            float head = Circle(x, y, 128, 136, 30);
            c = Over(c, MarbleWhite, Fill(head));
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                c = Over(c, Hex("DCD6CC"), Stroke(Circle(x, y, 128 + Mathf.Cos(a) * 18f, 136 + Mathf.Sin(a) * 18f, 7f), 2f) * Fill(head));
            }
            c = Over(c, Hex("6FA35A"), Stroke(Circle(x, y, 128, 136, 26), 4f) * Fill(Mathf.Abs(Mathf.Repeat(Mathf.Atan2(y - 136, x - 128) * 8f, 1f) - 0.5f) - 0.3f));
            c = Over(c, Hex("A9A296"), Stroke(head, 2.5f));
            c = Over(c, Outline, Stroke(plinth, 4f));
            return c;
        }

        // A four-poster bed with a quilted cover, pillows and the canopy's posts.
        static Color LuxBed(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 100, 120, 10));
            float frame = RoundRect(x, y, 128, 128, 100, 120, 10);
            c = Over(c, Walnut, Fill(frame));
            float cover = RoundRect(x, y, 128, 110, 90, 92, 10);
            c = Over(c, Cream, Fill(cover));
            if (Mathf.Repeat(x + y, 22f) < 1.5f || Mathf.Repeat(x - y, 22f) < 1.5f) c = Over(c, Hex("D9C9A6"), Fill(cover));
            c = Over(c, Burgundy, Fill(RoundRect(x, y, 128, 40, 90, 20, 8)));          // a throw at the foot
            c = Over(c, LuxGold, Stroke(RoundRect(x, y, 128, 40, 90, 20, 8), 2f));
            c = Pad(c, RoundRect(x, y, 90, 214, 34, 16, 10), Color.white);
            c = Pad(c, RoundRect(x, y, 166, 214, 34, 16, 10), Color.white);
            foreach (var (px, py) in new[] { (34f, 18f), (222f, 18f), (34f, 238f), (222f, 238f) })
            {
                c = Over(c, WalnutLight, Fill(Circle(x, y, px, py, 11)));
                c = Over(c, LuxGold, Fill(Circle(x, y, px, py, 5)));
                c = Over(c, Outline, Stroke(Circle(x, y, px, py, 11), 3f));
            }
            c = Over(c, Outline, Stroke(frame, 4f));
            return c;
        }

        // A teak spa lounger with a white cushion and a rolled towel.
        static Color LuxLounger(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 114, 46, 112, 10));
            float frame = RoundRect(x, y, 128, 128, 46, 112, 10);
            c = Over(c, Hex("A9733F"), Fill(frame));
            for (int i = 0; i < 18; i++) c = Over(c, Hex("7D4E2A"), Fill(Mathf.Abs(y - (24 + i * 12f)) - 0.8f) * Fill(frame));
            c = Pad(c, RoundRect(x, y, 128, 112, 36, 88, 10), Color.white);
            c = Pad(c, RoundRect(x, y, 128, 210, 36, 22, 10), Color.white);
            c = Over(c, Hex("3FB1C9"), Fill(RoundRect(x, y, 128, 150, 36, 8, 4)));
            c = Over(c, Outline, Stroke(frame, 4f));
            return c;
        }

        // A stack of fluffy towels and a basket of slippers.
        static Color LuxTowels(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 90, 70, 12));
            float basket = RoundRect(x, y, 128, 128, 90, 70, 12);
            c = Over(c, Hex("C9A06A"), Fill(basket));
            if (Mathf.Repeat(x + y * 0.5f, 10f) < 4f) c = Over(c, Hex("A9804E"), Fill(basket) * 0.6f);
            Color[] colours = { Color.white, Hex("BFE3EC"), Color.white, Hex("F2E6CF") };
            for (int i = 0; i < 4; i++)
            {
                float t = RoundRect(x, y, 128 - 4 + i * 3, 128 + 12 - i * 8, 74 - i * 4, 40 - i * 4, 14);
                c = Pad(c, t, colours[i]);
            }
            c = Over(c, Outline, Stroke(basket, 4f));
            return c;
        }

        // A brass telescope on a tripod, pointing out of the window.
        static Color LuxTelescope(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (float a in new[] { 0.6f, 2.7f, 4.6f })
                c = Over(c, Walnut, Fill(Capsule(x, y, 128, 110, 128 + Mathf.Cos(a) * 70f, 110 + Mathf.Sin(a) * 70f, 5f)));
            c = Drop(c, Capsule(x, y, 96, 80, 196, 200, 18), 8f);
            float tube = Capsule(x, y, 92, 74, 192, 196, 16);
            c = Over(c, LuxGold, Fill(tube));
            c = Over(c, Hex("F6DE8C"), Fill(Capsule(x, y, 88, 80, 188, 202, 5)));
            c = Over(c, LuxGoldDark, Fill(Circle(x, y, 192, 196, 18)));
            c = Over(c, Hex("6FB7DD"), Fill(Circle(x, y, 192, 196, 11)));
            c = Over(c, Outline, Stroke(tube, 3.5f));
            c = Over(c, Ebony, Fill(Circle(x, y, 128, 110, 9)));
            return c;
        }

        // A billiard table: green baize, walnut cushions, six pockets, balls racked.
        static Color LuxBilliardTable(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 82, 124, 12));
            float frame = RoundRect(x, y, 128, 128, 82, 124, 12);
            c = Over(c, Walnut, Fill(frame));
            float baize = RoundRect(x, y, 128, 128, 66, 108, 4);
            c = Over(c, Hex("2F7D4F"), Fill(baize));
            foreach (var (px, py) in new[] { (64f, 22f), (192f, 22f), (62f, 128f), (194f, 128f), (64f, 234f), (192f, 234f) })
                c = Over(c, Ebony, Fill(Circle(x, y, px, py, 9)));
            Color[] balls = { Hex("F2C230"), Hex("2D5BD3"), Hex("D8323A"), Hex("6A3FB5"), Hex("F07A22"), Hex("2F9A4F"), Hex("1E1E22"), Hex("8A1E2E"), Hex("F2C230"), Hex("2D5BD3") };
            int k = 0;
            for (int row = 0; row < 4; row++)
                for (int i = 0; i <= row; i++, k++)
                    c = Over(c, balls[k], Fill(Circle(x, y, 128 + (i - row * 0.5f) * 13f, 180 + row * 11f, 6)));
            c = Over(c, Color.white, Fill(Circle(x, y, 128, 70, 6)));
            c = Over(c, Hex("C9A06A"), Fill(Capsule(x, y, 128, 60, 150, 10, 2.5f))); // the cue
            c = Over(c, Outline, Stroke(frame, 4f));
            return c;
        }

        // A wine rack of dark wood, bottle ends in a diamond lattice.
        static Color LuxWineRack(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 122, 40, 5));
            float rack = RoundRect(x, y, 128, 128, 122, 40, 5);
            c = Over(c, Walnut, Fill(rack));
            for (int i = 0; i < 15; i++)
                for (int j = 0; j < 3; j++)
                {
                    float bx = 16 + i * 16f, by = 104 + j * 24f;
                    c = Over(c, (i + j) % 3 == 0 ? Hex("2A4A2A") : Hex("4A1A22"), Fill(Circle(x, y, bx, by, 7)) * Fill(rack + 2f));
                    c = Over(c, LuxGold, Fill(Circle(x, y, bx, by, 2.5f)) * Fill(rack + 2f));
                }
            c = Over(c, Outline, Stroke(rack, 4f));
            return c;
        }

        // A round button-tufted ottoman in burgundy velvet.
        static Color LuxOttoman(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, Circle(x, y, 140, 116, 70));
            float top = Circle(x, y, 128, 128, 70);
            c = Pad(c, top, Burgundy);
            for (int ring = 1; ring <= 2; ring++)
                for (int i = 0; i < ring * 6; i++)
                {
                    float a = i * Mathf.PI * 2f / (ring * 6) + ring * 0.3f;
                    c = Over(c, LuxGold, Fill(Circle(x, y, 128 + Mathf.Cos(a) * ring * 24f, 128 + Mathf.Sin(a) * ring * 24f, 3f)));
                }
            c = Over(c, LuxGold, Fill(Circle(x, y, 128, 128, 4f)));
            c = Over(c, Outline, Stroke(top, 4f));
            return c;
        }

        // A tall golden candelabra: five candles on a ring of arms.
        static Color LuxCandelabra(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, Circle(x, y, 140, 116, 54), 10f);
            c = Over(c, LuxGoldDark, Fill(Circle(x, y, 128, 128, 22)));
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f + 0.3f;
                Vector2 p = new Vector2(128, 128) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 46f;
                c = Over(c, LuxGold, Fill(Capsule(x, y, 128, 128, p.x, p.y, 4f)));
                c = Over(c, Cream, Fill(Circle(x, y, p.x, p.y, 9)));
                c = Over(c, Hex("FFC23A"), Fill(Circle(x, y, p.x, p.y, 4)));
                c = Over(c, Outline, Stroke(Circle(x, y, p.x, p.y, 9), 2.5f));
            }
            c = Over(c, LuxGold, Fill(Circle(x, y, 128, 128, 14)));
            c = Over(c, Outline, Stroke(Circle(x, y, 128, 128, 22), 3f));
            return c;
        }

        // A round marble fountain with a basin, a tiered middle and the spray.
        static Color LuxFountain(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, Circle(x, y, 140, 116, 112), 14f);
            float rim = Circle(x, y, 128, 128, 112);
            c = Over(c, MarbleWhite, Fill(rim));
            c = Over(c, Hex("D6D0C6"), Stroke(Circle(x, y, 128, 128, 104), 2f));
            float water = Circle(x, y, 128, 128, 96);
            c = Over(c, Color.Lerp(Hex("3FB1D9"), Hex("1E78B8"), Mathf.Clamp01((Dist(x, y, 128, 128)) / 96f)), Fill(water));
            for (int r = 30; r < 96; r += 18) c = Over(c, new Color(1f, 1f, 1f, 0.35f), Stroke(Circle(x, y, 128, 128, r + Mathf.Sin(Mathf.Atan2(y - 128, x - 128) * 5f) * 2f), 2f));
            c = Over(c, MarbleWhite, Fill(Circle(x, y, 128, 128, 30)));
            c = Over(c, Hex("CFC9C0"), Stroke(Circle(x, y, 128, 128, 30), 2f));
            c = Over(c, MarbleWhite, Fill(Circle(x, y, 128, 128, 14)));
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                c = Over(c, new Color(0.85f, 0.95f, 1f, 0.9f), Fill(Capsule(x, y, 128, 128, 128 + Mathf.Cos(a) * 52f, 128 + Mathf.Sin(a) * 52f, 2.2f)) * Fill(-(Circle(x, y, 128, 128, 30))));
            }
            c = Over(c, Outline, Stroke(rim, 4f));
            return c;
        }

        // A long aquarium: blue water, sand, plants and bright fish.
        static Color LuxAquarium(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 122, 44, 6));
            float tank = RoundRect(x, y, 128, 128, 122, 44, 6);
            c = Over(c, Ebony, Fill(tank));
            float water = RoundRect(x, y, 128, 128, 114, 36, 3);
            c = Over(c, Color.Lerp(Hex("2B97D4"), Hex("5FC8EE"), (y - 92f) / 72f), Fill(water));
            c = Over(c, Hex("E8D3A0"), Fill(water) * Fill(102f - y));
            for (int i = 0; i < 6; i++) c = Over(c, Hex("2F9A4F"), Fill(Capsule(x, y, 30 + i * 38f, 98, 34 + i * 38f + Mathf.Sin(i) * 6f, 130 + (i % 3) * 10f, 3f)) * Fill(water));
            Color[] fish = { Hex("F07A22"), Hex("F2C230"), Hex("E74C8B"), Hex("3FD0C9"), Hex("F07A22") };
            for (int i = 0; i < 5; i++)
            {
                float fx = 50 + i * 40f, fy = 118 + (i % 3) * 12f, dir = i % 2 == 0 ? 1f : -1f;
                c = Over(c, fish[i], Fill(Ellipse(x, y, fx, fy, 9, 5)) * Fill(water));
                c = Over(c, fish[i], Fill(Triangle(x, y, new Vector2(fx - dir * 8, fy), new Vector2(fx - dir * 15, fy + 5), new Vector2(fx - dir * 15, fy - 5))) * Fill(water));
            }
            c = Over(c, new Color(1f, 1f, 1f, 0.35f), Fill(RoundRect(x, y, 60, 156, 40, 2, 1)));
            c = Over(c, Outline, Stroke(tank, 4f));
            return c;
        }

        // A chrome-and-neon jukebox from above.
        static Color LuxJukebox(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 114, 70, 60, 26));
            float box = RoundRect(x, y, 128, 128, 70, 60, 26);
            c = Over(c, Hex("C8CED8"), Fill(box));
            c = Over(c, Hex("E74C8B"), Stroke(RoundRect(x, y, 128, 128, 58, 48, 20), 6f));
            c = Over(c, Hex("FFC23A"), Stroke(RoundRect(x, y, 128, 128, 48, 38, 16), 4f));
            c = Over(c, Hex("3FD0C9"), Fill(RoundRect(x, y, 128, 128, 34, 24, 10)));
            for (int i = 0; i < 6; i++) c = Over(c, Ebony, Fill(Mathf.Abs(y - (112 + i * 6.5f)) - 1f) * Fill(RoundRect(x, y, 128, 128, 30, 20, 8)));
            c = Over(c, Outline, Stroke(box, 4f));
            return c;
        }

        // A DJ booth: the desk with two turntables, a mixer and a laptop, speakers either side.
        static Color LuxDjBooth(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Drop(c, RoundRect(x, y, 140, 112, 120, 50, 8));
            float desk = RoundRect(x, y, 128, 128, 104, 44, 8);
            c = Over(c, Hex("2A2830"), Fill(desk));
            foreach (float cx in new[] { 72f, 184f })
            {
                c = Over(c, Ebony, Fill(Circle(x, y, cx, 128, 30)));
                c = Over(c, Hex("3A3840"), Stroke(Circle(x, y, cx, 128, 20), 2f));
                c = Over(c, Hex("E74C8B"), Fill(Circle(x, y, cx, 128, 8)));
            }
            c = Over(c, Hex("4A4850"), Fill(RoundRect(x, y, 128, 128, 16, 32, 3)));
            for (int i = 0; i < 4; i++) c = Over(c, Hex("3FD0C9"), Fill(RoundRect(x, y, 122 + (i % 2) * 12, 112 + (i / 2) * 26, 3, 6, 1)));
            foreach (float sx in new[] { 10f, 246f })
            {
                float spk = RoundRect(x, y, sx, 128, 10, 34, 3);
                c = Over(c, Ebony, Fill(spk));
                c = Over(c, Hex("5A5860"), Stroke(Circle(x, y, sx, 140, 6), 2f));
                c = Over(c, Hex("5A5860"), Stroke(Circle(x, y, sx, 110, 8), 2f));
            }
            c = Over(c, Hex("E74C8B"), Stroke(desk, 3f));
            c = Over(c, Outline, Stroke(desk + 2.5f, 2.5f));
            return c;
        }

        // ------------------------------------------------------------------ floors and roof (tile 128 px)

        // Polished white marble with soft grey veins, laid in big squares with a black-and-gold diamond at each corner.
        static Color MarbleFloorPixel(float x, float y)
        {
            float vein = Noise(x / 22f, y / 22f, 6) * 6f + Noise(x / 7f, y / 7f, 18) * 1.5f;
            Color c = Color.Lerp(Hex("F4F1EC"), Hex("E6E1D9"), Noise(x / 40f, y / 40f, 3));
            float v = Mathf.Abs(Mathf.Sin(vein * 2.2f + x * 0.02f));
            if (v < 0.06f) c = Color.Lerp(c, Hex("A9A49C"), (0.06f - v) / 0.06f * 0.7f);
            if (Mathf.Repeat(x, 64f) < 1.2f || Mathf.Repeat(y, 64f) < 1.2f) c = Hex("CFC9BF");
            // the corners of the squares are at multiples of 64: a diamond there
            float cx = Mathf.Abs(Mathf.Repeat(x, 64f) - (Mathf.Repeat(x, 64f) > 32f ? 64f : 0f)), cy = Mathf.Abs(Mathf.Repeat(y, 64f) - (Mathf.Repeat(y, 64f) > 32f ? 64f : 0f));
            float diamond = cx + cy;
            if (diamond < 11f) c = Hex("1F2C2B");
            if (Mathf.Abs(diamond - 11f) < 1.3f) c = Hex("E2B547");
            if (Hash((int)x, (int)y) > 0.996f) c = Color.white;
            return c;
        }

        // Wide oak planks, honey coloured with dark joints.
        static Color OakFloorPixel(float x, float y)
        {
            int plank = Mathf.FloorToInt(y / 21.33f);
            float k = Hash(plank, 11);
            Color c = Color.Lerp(Hex("B07A44"), Hex("C9935A"), k);
            c = Color.Lerp(c, Shade(c, 0.86f), Mathf.Abs(Mathf.Sin(x * 0.09f + k * 9f + Noise(x / 16f, y / 6f, 8) * 3f)) * 0.3f);
            if (Mathf.Repeat(y, 21.33f) < 1.1f) c = Hex("6B4226");
            if (Mathf.Repeat(x + plank * 45f, 128f) < 1.2f) c = Shade(c, 0.7f);
            return c;
        }

        // Thick glass panes over the abyss: mostly see-through (the drop shows beneath), steel frames, a glint.
        static Color GlassFloorPixel(float x, float y)
        {
            float fx = Mathf.Repeat(x, 64f), fy = Mathf.Repeat(y, 64f);
            if (fx < 2.5f || fy < 2.5f) return Hex("8C99A8");
            if (fx < 3.5f || fy < 3.5f) return Hex("4E5966");
            Color c = new(0.82f, 0.93f, 0.98f, 0.16f);
            float glint = Mathf.Abs((fx - fy) - 18f);
            if (glint < 5f) c = new Color(1f, 1f, 1f, 0.3f * (1f - glint / 5f) + 0.16f);
            return c;
        }

        // A chalet roof under deep snow: soft drifts over the rows of shingles (faint steps), sparkles.
        static Color ChaletRoofPixel(float x, float y)
        {
            float drift = Noise(x / 24f, y / 24f, 5) * 0.7f + Noise(x / 8f, y / 8f, 16) * 0.3f;
            Color c = Color.Lerp(Snow, SnowShade, drift * 0.7f);
            float row = Mathf.Repeat(y + Noise(x / 32f, 0.5f, 4) * 3f, 21.33f);
            if (row < 2.2f) c = Color.Lerp(c, Hex("B9CBDD"), 0.55f * (1f - row / 2.2f));
            if (Hash((int)x / 2, (int)y / 2) > 0.994f) c = Color.white;
            return c;
        }
    }
}
