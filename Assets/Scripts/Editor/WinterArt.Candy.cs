using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The candy scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 3, Santa's Workshop: snowy
    /// basics, the Christmas tree, gifts and reindeer, plus gingerbread houses, candy canes, lollipops,
    /// gumdrops, marshmallows, a giant cookie, a hot chocolate pool, elves, Santa's sleigh and the toys of
    /// the workshop (blocks, teddy, nutcracker, rocking horse, robot, workbench). Also the gingerbread roof
    /// with icing. Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] CandyCells =
        {
            "SnowPine", "FrozenPond", "FrostBush", "Snowman",
            "ChristmasTree", "Gifts", "Reindeer", "SnowDrift",
            "GingerHouse", "GingerHousePink", "CandyCane", "Lollipop",
            "Gumdrops", "Marshmallows", "GiantCookie", "ChocolatePool",
            "Elves", "Sleigh", "ToyBlocks", "Teddy",
            "Nutcracker", "RockingHorse", "ToyRobot", "Workbench",
        };

        const int CandyRows = 6;

        static readonly Color Ginger = Hex("B86A33"), GingerDark = Hex("8A4A22"), Icing = Hex("FFFDF8"), CandyRed = Hex("E2393F"), Mint = Hex("5CCB9B");

        /// <summary>UV rect of a candy scenery cell by name (row 0 at the bottom).</summary>
        public static Rect CandyCell(string name)
        {
            int i = System.Array.IndexOf(CandyCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)CandyRows, 0.25f, 1f / CandyRows);
        }

        [MenuItem("SomeGame/Generate Candy Art")]
        public static void GenerateCandy()
        {
            Write(ArtGenerator.Folder, "SceneryCandy", 1024, 256 * CandyRows, CandyPixel, 128, false);
            ArtGenerator.Material("SceneryCandy", "SceneryCandy");
            Write(ArtGenerator.Folder, "RoofGinger", 128, 128, GingerRoofPixel, 32, true);
            ArtGenerator.Material("RoofGinger", "RoofGinger");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color CandyPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return CandyCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrozenPond" => FrozenPond(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "ChristmasTree" => ChristmasTree(lx, ly),
                "Gifts" => Gifts(lx, ly),
                "Reindeer" => Reindeer(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "GingerHouse" => GingerHouse(lx, ly, Hex("E86FB0")),
                "GingerHousePink" => GingerHouse(lx, ly, Mint),
                "CandyCane" => CandyCane(lx, ly),
                "Lollipop" => Lollipop(lx, ly),
                "Gumdrops" => Gumdrops(lx, ly),
                "Marshmallows" => Marshmallows(lx, ly),
                "GiantCookie" => GiantCookie(lx, ly),
                "ChocolatePool" => ChocolatePool(lx, ly),
                "Elves" => Elves(lx, ly),
                "Sleigh" => Sleigh(lx, ly),
                "ToyBlocks" => ToyBlocks(lx, ly),
                "Teddy" => Teddy(lx, ly),
                "Nutcracker" => Nutcracker(lx, ly),
                "RockingHorse" => RockingHorse(lx, ly),
                "ToyRobot" => ToyRobot(lx, ly),
                _ => Workbench(lx, ly),
            };
        }

        // A gingerbread house from above: a brown roof with dripping white icing along the ridge and eaves,
        // sweets stuck on, a candy-cane chimney.
        static Color GingerHouse(float x, float y, Color sweets)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 144, 110, 104, 82, 10), 12f));
            float roof = RoundRect(x, y, 128, 128, 104, 82, 10);
            c = Over(c, x < 128 ? Ginger : GingerDark, Fill(roof));
            // Shingles of icing scallops in rows.
            float sx = Mathf.Repeat(x - 24f, 26f) - 13f, sy = Mathf.Repeat(y - 46f, 28f);
            if (roof < -8f && sy < 5f + Mathf.Sqrt(Mathf.Max(0f, 169f - sx * sx)) * 0.35f && sy > 1f) c = Over(c, Icing, 0.85f);
            // Icing along the ridge and the eaves, drips hanging down.
            c = Over(c, Icing, Fill(Mathf.Abs(x - 128f) - 5f) * Fill(roof));
            float eave = Mathf.Abs(roof + 5f) - 5f;
            c = Over(c, Icing, Fill(eave));
            if (Mathf.Repeat(x, 18f) < 7f && y < 60f) c = Over(c, Icing, Fill(RoundRect(x, y, x, 46f - 8f * Mathf.Abs(Mathf.Sin(x * 0.3f)), 3.5f, 9f, 3f)) * Fill(roof - 2f));
            // Sweets: gumdrops in a row on each side.
            for (int i = 0; i < 5; i++)
            {
                c = Over(c, i % 2 == 0 ? sweets : CandyRed, Fill(Circle(x, y, 74, 64 + i * 32, 7)));
                c = Over(c, i % 2 == 0 ? CandyRed : sweets, Fill(Circle(x, y, 182, 64 + i * 32, 7)));
            }
            c = Over(c, Outline, Stroke(roof, 4f) * 0.85f);
            // The chimney: a red and white stub with snow on top.
            float chimney = Circle(x, y, 176, 176, 14);
            float stripe = Mathf.Repeat(x + y, 12f) < 6f ? 1f : 0f;
            c = Over(c, Color.Lerp(Icing, CandyRed, stripe), Fill(chimney));
            c = Over(c, Outline, Stroke(chimney, 3f));
            return c;
        }

        // A big candy cane stuck in the snow, red and white stripes, the hook curling round.
        static Color CandyCane(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hook = Mathf.Abs(Dist(x, y, 150, 176) - 34f) - 14f;
            hook = Mathf.Max(hook, -(y - 176f));
            float shaft = Capsule(x, y, 116, 40, 116, 176, 14);
            float cane = Mathf.Min(shaft, hook);
            c = Over(c, Shadow, Soft(cane - 2f + 0f * x, 10f) * 0f);
            c = Over(c, Shadow, Soft(Mathf.Min(Capsule(x, y, 130, 26, 130, 162, 14), Mathf.Max(Mathf.Abs(Dist(x, y, 164, 162) - 34f) - 14f, -(y - 162f))), 10f));
            float stripe = Mathf.Repeat(x * 0.7f + y, 26f) < 12f ? 1f : 0f;
            c = Over(c, Color.Lerp(Icing, CandyRed, stripe), Fill(cane));
            c = Over(c, Color.white, Fill(Capsule(x, y, 110, 50, 110, 170, 4)) * 0.6f);
            c = Over(c, Outline, Stroke(cane, 3.5f));
            c = Over(c, SnowShade, Fill(Ellipse(x, y, 116, 34, 30, 12)) * Fill(-(shaft - 2f)) * 0f);
            return c;
        }

        // A giant lollipop: a swirl of colours on a stick.
        static Color Lollipop(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float stick = Capsule(x, y, 128, 128, 128, 18, 7);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 132, 80), 10f));
            c = Over(c, Icing, Fill(stick));
            c = Over(c, Outline, Stroke(stick, 3f));
            float disc = Circle(x, y, 128, 146, 80);
            float ang = Mathf.Atan2(y - 146, x - 128), r = Dist(x, y, 128, 146);
            float swirl = Mathf.Repeat(r * 0.08f + ang / (Mathf.PI * 2f) * 3f, 1f);
            Color[] cols = { Hex("E86FB0"), Icing, Hex("F5C842"), Icing, Hex("3D7DD8"), Icing };
            c = Over(c, cols[(int)(swirl * cols.Length) % cols.Length], Fill(disc));
            c = Over(c, Color.white, Fill(Ellipse(x, y, 100, 176, 22, 12)) * 0.55f);
            c = Over(c, Outline, Stroke(disc, 4f));
            return c;
        }

        // A heap of sugared gumdrops in bright colours.
        static Color Gumdrops(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var drops = new[] { (80f, 100f, Hex("E2393F")), (140f, 84f, Hex("F5C842")), (186f, 120f, Mint), (110f, 152f, Hex("8A5CD6")), (166f, 172f, Hex("FF7A1A")), (70f, 176f, Hex("3D7DD8")) };
            foreach (var (dx, dy, _) in drops) c = Over(c, Shadow, Soft(Circle(x, y, dx + 10, dy - 10, 30), 8f));
            foreach (var (dx, dy, col) in drops)
            {
                float d = Circle(x, y, dx, dy, 30);
                c = Over(c, Color.Lerp(Shade(col, 1.15f), Shade(col, 0.8f), Mathf.InverseLerp(dy + 30, dy - 30, y)), Fill(d));
                if (d < 0f && Hash((int)x / 3, (int)y / 3) > 0.86f) c = Over(c, Color.white, 0.6f);
                c = Over(c, Color.white, Fill(Ellipse(x, y, dx - 9, dy + 11, 9, 5)) * 0.7f);
                c = Over(c, Outline, Stroke(d, 3f));
            }
            return c;
        }

        // A pile of giant marshmallows, soft white with pink ones among them.
        static Color Marshmallows(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pieces = new[] { (88f, 94f, false), (160f, 90f, true), (122f, 150f, false), (186f, 160f, false), (66f, 168f, true) };
            foreach (var (px, py, _) in pieces) c = Over(c, Shadow, Soft(RoundRect(x, y, px + 10, py - 10, 34, 28, 14), 8f));
            foreach (var (px, py, pink) in pieces)
            {
                float d = RoundRect(x, y, px, py, 34, 28, 14);
                Color col = pink ? Hex("F7B8D2") : Icing;
                c = Over(c, Color.Lerp(col, Shade(col, 0.86f), Mathf.InverseLerp(py + 28, py - 28, y)), Fill(d));
                c = Over(c, Shade(col, 0.82f), Stroke(RoundRect(x, y, px, py, 24, 18, 10), 2f) * 0.6f);
                c = Over(c, Outline, Stroke(d, 3f) * 0.8f);
            }
            return c;
        }

        // A giant chocolate-chip cookie lying in the snow.
        static Color GiantCookie(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float edge = Circle(x, y, 128, 128, 104) + (Noise(x * 0.4f, y * 0.4f, 16) - 0.5f) * 10f;
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 104), 12f));
            c = Over(c, Color.Lerp(Hex("D9A05B"), Hex("B97A3A"), Noise(x * 0.12f, y * 0.12f, 8) * 0.8f + Dist(x, y, 128, 128) / 400f), Fill(edge));
            for (int i = 0; i < 14; i++)
            {
                float a = i * 2.4f, r = 20f + (i * 37 % 70);
                c = Over(c, Hex("4A2A18"), Fill(Circle(x, y, 128 + Mathf.Cos(a) * r, 128 + Mathf.Sin(a) * r, 8 + (i % 3) * 2)));
            }
            c = Over(c, Icing, Fill(Ellipse(x, y, 96, 164, 26, 10)) * 0.5f);
            c = Over(c, Hex("6B3E1F"), Stroke(edge, 4f));
            return c;
        }

        // A pool of hot chocolate (drawn flat): rich brown, glossy swirls, marshmallows bobbing at the edge.
        static Color ChocolatePool(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float pool = Ellipse(x, y, 128, 128, 120, 112) + Mathf.Sin(Mathf.Atan2(y - 128, x - 128) * 7f) * 3f;
            c = Over(c, Hex("4A2A18"), Fill(pool + 6f));
            float r = Dist(x, y, 128, 128), ang = Mathf.Atan2(y - 128, x - 128);
            Color choc = Color.Lerp(Hex("8A4E2B"), Hex("5E3220"), Mathf.Clamp01(r / 120f));
            if (Mathf.Abs(Mathf.Repeat(r * 0.12f + ang * 0.6f, 1f) - 0.5f) < 0.06f) choc = Color.Lerp(choc, Hex("B07446"), 0.6f);
            c = Over(c, choc, Fill(pool));
            c = Over(c, Color.white, Fill(Ellipse(x, y, 92, 166, 30, 8)) * 0.35f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f + 0.4f;
                float mx = 128 + Mathf.Cos(a) * 92f, my = 128 + Mathf.Sin(a) * 86f;
                float m = RoundRect(x, y, mx, my, 9, 8, 4);
                c = Over(c, i % 3 == 0 ? Hex("F7B8D2") : Icing, Fill(m));
                c = Over(c, Hex("6B3E1F"), Stroke(m, 2f) * 0.7f);
            }
            return c;
        }

        // Santa's elves: a little group in green with pointy red hats, waving.
        static Color Elves(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var elves = new[] { (64f, 120f), (108f, 150f), (150f, 112f), (194f, 144f), (128f, 70f), (196f, 76f) };
            foreach (var (ex, ey) in elves) c = Over(c, Shadow, Soft(Circle(x, y, ex + 8, ey - 8, 22), 6f));
            for (int i = 0; i < elves.Length; i++)
            {
                var (ex, ey) = elves[i];
                float body = Circle(x, y, ex, ey, 20);
                c = Over(c, i % 2 == 0 ? Hex("2E8A4E") : Hex("3FA35E"), Fill(body));
                float arm = Capsule(x, y, ex + 14, ey + 4, ex + 26, ey + 20, 5);
                c = Over(c, Hex("2E8A4E"), Fill(arm));
                c = Over(c, Hex("F2C6A0"), Fill(Circle(x, y, ex + 27, ey + 22, 5)));
                float hat = Triangle(x, y, new Vector2(ex - 13, ey + 2), new Vector2(ex + 13, ey + 2), new Vector2(ex + 4, ey + 30));
                c = Over(c, CandyRed, Fill(Mathf.Min(hat, Circle(x, y, ex, ey + 2, 12))));
                c = Over(c, Icing, Fill(Circle(x, y, ex + 4, ey + 31, 5)));
                c = Over(c, Icing, Fill(Capsule(x, y, ex - 13, ey - 2, ex + 13, ey - 2, 4)));
                c = Over(c, Outline, Stroke(Mathf.Min(body, hat), 2.5f) * 0.85f);
            }
            return c;
        }

        // Santa's sleigh from above: red body with gold trim, a sack of presents in the back.
        static Color Sleigh(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 114, 60, 104, 26), 10f));
            foreach (float rx in new[] { 74f, 182f })
            {
                float runner = Mathf.Min(Capsule(x, y, rx, 30, rx, 210, 5), Mathf.Abs(Dist(x, y, rx - (rx < 128 ? -14 : 14), 210) - 14f) - 4f);
                c = Over(c, Hex("F5C842"), Fill(Mathf.Min(Capsule(x, y, rx, 30, rx, 200, 5), 99f)));
                c = Over(c, Outline, Stroke(Capsule(x, y, rx, 30, rx, 200, 5), 2f) * 0.8f);
            }
            float body = RoundRect(x, y, 128, 120, 58, 96, 26);
            c = Over(c, CandyRed, Fill(body));
            c = Over(c, Hex("B8262E"), Fill(RoundRect(x, y, 128, 110, 44, 70, 18)));
            c = Over(c, Hex("F5C842"), Stroke(RoundRect(x, y, 128, 120, 50, 88, 22), 4f));
            float sack = Circle(x, y, 128, 74, 36);
            c = Over(c, Hex("8E5C3B"), Fill(sack));
            c = Over(c, Hex("3D7DD8"), Fill(RoundRect(x, y, 112, 86, 12, 12, 2)));
            c = Over(c, Hex("F5C842"), Fill(RoundRect(x, y, 142, 80, 12, 12, 2)));
            c = Over(c, Mint, Fill(RoundRect(x, y, 126, 58, 11, 11, 2)));
            c = Over(c, Outline, Stroke(body, 4f));
            return c;
        }

        // A tumble of wooden toy blocks with letters.
        static Color ToyBlocks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var blocks = new[] { (80f, 90f, 0.2f, CandyRed), (150f, 84f, -0.3f, Hex("3D7DD8")), (190f, 150f, 0.5f, Hex("F5C842")), (112f, 156f, -0.1f, Mint), (64f, 190f, 0.4f, Hex("8A5CD6")) };
            foreach (var (bx, by, a, _) in blocks) c = Over(c, Shadow, Soft(RotRect(x, y, bx + 10, by - 10, 30, 30, 4, a), 8f));
            foreach (var (bx, by, a, col) in blocks)
            {
                float d = RotRect(x, y, bx, by, 30, 30, 4, a);
                c = Over(c, col, Fill(d));
                c = Over(c, Icing, Fill(RotRect(x, y, bx, by, 20, 20, 3, a)));
                c = Over(c, col, Fill(RotRect(x, y, bx, by, 6, 14, 2, a)));
                c = Over(c, col, Fill(RotRect(x, y, bx, by + 0f, 14, 5, 2, a)) * 0.9f);
                c = Over(c, Outline, Stroke(d, 3f));
            }
            return c;
        }

        // A big teddy bear sitting in the snow with a red bow.
        static Color Teddy(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Circle(x, y, 128, 104, 56), head = Circle(x, y, 128, 170, 40);
            float ears = Mathf.Min(Circle(x, y, 94, 200, 15), Circle(x, y, 162, 200, 15));
            float arms = Mathf.Min(Circle(x, y, 74, 118, 20), Circle(x, y, 182, 118, 20));
            float feet = Mathf.Min(Circle(x, y, 92, 54, 22), Circle(x, y, 164, 54, 22));
            float all = Min(body, head, ears, arms, feet);
            c = Over(c, Shadow, Soft(all + 0f - 0f, 10f) * 0f);
            c = Over(c, Shadow, Soft(Mathf.Min(Circle(x, y, 140, 92, 56), Circle(x, y, 140, 158, 40)), 10f));
            c = Over(c, Color.Lerp(Hex("C98A4E"), Hex("9C6232"), Mathf.InverseLerp(220, 40, y)), Fill(all));
            c = Over(c, Hex("E8C08E"), Fill(Mathf.Min(Circle(x, y, 128, 98, 32), Ellipse(x, y, 128, 160, 18, 13))));
            c = Over(c, Hex("E8C08E"), Fill(Mathf.Min(Circle(x, y, 92, 54, 12), Circle(x, y, 164, 54, 12))));
            c = Over(c, Outline, Fill(Circle(x, y, 114, 178, 4.5f)));
            c = Over(c, Outline, Fill(Circle(x, y, 142, 178, 4.5f)));
            c = Over(c, Outline, Fill(Ellipse(x, y, 128, 164, 7, 5)));
            float bow = Mathf.Min(Triangle(x, y, new Vector2(128, 134), new Vector2(104, 146), new Vector2(104, 122)), Triangle(x, y, new Vector2(128, 134), new Vector2(152, 146), new Vector2(152, 122)));
            c = Over(c, CandyRed, Fill(bow));
            c = Over(c, Outline, Stroke(all, 3.5f) * 0.85f);
            return c;
        }

        // A nutcracker soldier from above: tall black hat with a gold badge, red coat, white beard.
        static Color Nutcracker(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 140, 114, 54, 78), 10f));
            float coat = Ellipse(x, y, 128, 118, 48, 74);
            c = Over(c, CandyRed, Fill(coat));
            c = Over(c, Hex("F5C842"), Fill(Mathf.Abs(x - 128f) - 3f) * Fill(coat));
            for (int i = 0; i < 4; i++) c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 128, 74 + i * 20, 4)));
            float arms = Mathf.Min(Capsule(x, y, 80, 130, 70, 70, 12), Capsule(x, y, 176, 130, 186, 70, 12));
            c = Over(c, Hex("2F6DB5"), Fill(arms));
            c = Over(c, Outline, Stroke(Mathf.Min(coat, arms), 3f));
            float hat = Circle(x, y, 128, 150, 34);
            c = Over(c, Hex("22262E"), Fill(hat));
            c = Over(c, Hex("3A3F4A"), Fill(Circle(x, y, 120, 158, 20)));
            float badge = Star(x, y, 128, 176, 12, 6, 5, 0f);
            c = Over(c, Hex("F5C842"), Fill(badge));
            c = Over(c, Icing, Fill(Ellipse(x, y, 128, 112, 22, 10)) * Fill(-(hat + 0f) - 0f + 1f) * 0f);
            c = Over(c, Outline, Stroke(hat, 3f));
            return c;
        }

        // A rocking horse: white with grey dapples, red saddle, golden runners.
        static Color RockingHorse(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 140, 114, 50, 100), 10f));
            foreach (float rx in new[] { 84f, 172f })
            {
                float runner = Mathf.Abs(Dist(x, y, rx + (rx < 128 ? 120f : -120f), 128) - 120f) - 5f;
                runner = Mathf.Max(runner, Mathf.Abs(y - 128f) - 96f);
                c = Over(c, Hex("C9963A"), Fill(runner));
                c = Over(c, Outline, Stroke(runner, 2.5f) * 0.8f);
            }
            float body = Ellipse(x, y, 128, 118, 30, 62), neck = Capsule(x, y, 128, 170, 128, 196, 16), tail = Capsule(x, y, 128, 60, 128, 34, 8);
            float all = Min(body, neck, tail);
            c = Over(c, Icing, Fill(all));
            if (all < 0f && Noise(x * 0.15f, y * 0.15f, 8) > 0.62f) c = Over(c, Hex("B9C3CF"), 0.8f);
            c = Over(c, Hex("6B4A2E"), Fill(Capsule(x, y, 128, 150, 128, 200, 5)));
            c = Over(c, Hex("6B4A2E"), Fill(tail));
            c = Over(c, CandyRed, Fill(RoundRect(x, y, 128, 120, 26, 18, 6)));
            c = Over(c, Outline, Stroke(all, 3.5f));
            return c;
        }

        // A tin toy robot: blue body, a dial on the chest, antenna with a red ball, arms out.
        static Color ToyRobot(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 110, 60, 76, 12), 10f));
            float arms = Mathf.Min(RoundRect(x, y, 64, 120, 16, 34, 6), RoundRect(x, y, 192, 120, 16, 34, 6));
            float body = RoundRect(x, y, 128, 112, 48, 56, 10), head = RoundRect(x, y, 128, 186, 34, 24, 8);
            float all = Min(arms, body, head);
            c = Over(c, Color.Lerp(Hex("7FB6E8"), Hex("3D7DD8"), Mathf.InverseLerp(60, 200, x)), Fill(all));
            c = Over(c, Hex("B9C3CF"), Fill(Circle(x, y, 128, 118, 20)));
            float needle = Capsule(x, y, 128, 118, 140, 130, 2.5f);
            c = Over(c, CandyRed, Fill(needle));
            for (int i = 0; i < 3; i++) c = Over(c, i == 1 ? Hex("F5C842") : Mint, Fill(Circle(x, y, 108 + i * 20, 78, 5)));
            c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 116, 190, 7)));
            c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 140, 190, 7)));
            c = Over(c, Outline, Fill(Circle(x, y, 116, 190, 3)));
            c = Over(c, Outline, Fill(Circle(x, y, 140, 190, 3)));
            c = Over(c, Outline, Fill(Capsule(x, y, 128, 210, 128, 228, 2.5f)));
            c = Over(c, CandyRed, Fill(Circle(x, y, 128, 232, 8)));
            c = Over(c, Outline, Stroke(all, 3.5f));
            return c;
        }

        // An elf's workbench: wooden top with tools, a half-wrapped present, ribbon rolls.
        static Color Workbench(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 114, 112, 54, 6), 10f));
            float top = RoundRect(x, y, 128, 128, 112, 54, 6);
            Color wood = Color.Lerp(Hex("A9713F"), Hex("8E5C3B"), Mathf.Repeat(y * 0.18f + Mathf.Sin(x * 0.05f) * 0.6f, 1f) * 0.6f);
            c = Over(c, wood, Fill(top));
            c = Over(c, Outline, Stroke(top, 3.5f));
            // A present being wrapped, a hammer, ribbon rolls, a saw.
            float box = RoundRect(x, y, 84, 130, 26, 26, 3);
            c = Over(c, Mint, Fill(box));
            c = Over(c, CandyRed, Fill(Mathf.Min(Mathf.Abs(x - 84f) - 4f, Mathf.Abs(y - 130f) - 4f)) * Fill(box));
            c = Over(c, Outline, Stroke(box, 2.5f));
            float hammer = Mathf.Min(Capsule(x, y, 140, 108, 176, 140, 4), RotRect(x, y, 176, 140, 14, 6, 2, -0.7f));
            c = Over(c, Hex("5B6878"), Fill(hammer));
            c = Over(c, Outline, Stroke(hammer, 2f));
            foreach (var (rx, ry, col) in new[] { (150f, 158f, CandyRed), (178f, 158f, Hex("F5C842")), (206f, 158f, Hex("3D7DD8")) })
            {
                float roll = Circle(x, y, rx, ry, 11);
                c = Over(c, col, Fill(roll));
                c = Over(c, Icing, Fill(Circle(x, y, rx, ry, 4)));
                c = Over(c, Outline, Stroke(roll, 2f));
            }
            return c;
        }

        // The gingerbread roof: rows of brown tiles, each edged with white icing scallops.
        static Color GingerRoofPixel(float x, float y)
        {
            float row = Mathf.Floor(y / 32f);
            float bx = Mathf.Repeat(x + (row % 2) * 16f, 32f) - 16f, by = Mathf.Repeat(y, 32f);
            Color c = Color.Lerp(Hex("A05A28"), Hex("C27A3E"), by / 32f);
            float scallop = 6f + Mathf.Sqrt(Mathf.Max(0f, 256f - bx * bx)) * 0.35f;
            if (by < scallop && by > scallop - 4f) c = Icing;
            if (Hash((int)((x + (row % 2) * 16f) / 32f), (int)row) > 0.78f && Dist(bx, by, 0f, 18f) < 4f) c = (Hash((int)row, (int)(x / 32f)) > 0.5f ? CandyRed : Mint);
            return c;
        }
    }
}
