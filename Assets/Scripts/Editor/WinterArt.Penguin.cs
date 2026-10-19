using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The penguin bay scenery atlas (24 cells of 256 px, 4 x 6) for the Winter Cup lap race Penguin Bay: the
    /// snowy basics plus a penguin colony, emperor penguins, a crèche of fluffy chicks, a barrel of fish, the
    /// research hut, a weather mast, an ice arch, a penguin road sign and kayaks on the shore, with the seals,
    /// polar bears, buoys, crates, nets, anchor and iceberg of the harbour sheet. Row 0 at the bottom, like the
    /// other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] PenguinCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "WinterCrowd", "SnowDrift", "Igloo",
            "PenguinColony", "EmperorPenguins", "PenguinChicks", "FishBarrel",
            "ResearchHut", "WeatherMast", "IceArch", "PenguinSign",
            "Kayaks", "Seals", "PolarBears", "Buoys",
            "Crates", "Nets", "Anchor", "IcebergCell",
        };

        const int PenguinRows = 6;

        /// <summary>UV rect of a penguin bay scenery cell by name (row 0 at the bottom).</summary>
        public static Rect PenguinCell(string name)
        {
            int i = System.Array.IndexOf(PenguinCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)PenguinRows, 0.25f, 1f / PenguinRows);
        }

        [MenuItem("SomeGame/Generate Penguin Bay Art")]
        public static void GeneratePenguin()
        {
            Write(ArtGenerator.Folder, "SceneryPenguin", 1024, 256 * PenguinRows, PenguinSheetPixel, 128, false);
            ArtGenerator.Material("SceneryPenguin", "SceneryPenguin");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color PenguinSheetPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return PenguinCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Igloo" => Igloo(lx, ly),
                "PenguinColony" => PenguinColony(lx, ly),
                "EmperorPenguins" => EmperorPenguins(lx, ly),
                "PenguinChicks" => PenguinChicks(lx, ly),
                "FishBarrel" => FishBarrel(lx, ly),
                "ResearchHut" => ResearchHut(lx, ly),
                "WeatherMast" => WeatherMast(lx, ly),
                "IceArch" => IceArch(lx, ly),
                "PenguinSign" => PenguinSign(lx, ly),
                "Kayaks" => Kayaks(lx, ly),
                "Seals" => Seals(lx, ly),
                "PolarBears" => PolarBears(lx, ly),
                "Buoys" => Buoys(lx, ly),
                "Crates" => Crates(lx, ly),
                "Nets" => Nets(lx, ly),
                "Anchor" => Anchor(lx, ly),
                _ => IcebergPixel(lx, ly),
            };
        }

        // One penguin standing in the snow, seen from the front and a little above, at (cx, cy), s = 1 is about
        // 50 px tall. Emperors are bigger with a golden patch at the neck.
        static Color PenguinAt(Color c, float x, float y, float cx, float cy, float s, bool emperor = false, float lean = 0f)
        {
            float px = (x - cx) / s, py = (y - cy) / s;
            px -= py * lean;
            c = Over(c, Shadow, Soft(Ellipse(px, py, 6, -6, 16, 10), 5f / s));
            c = Over(c, Hex("FF9A2E"), Fill(Mathf.Min(Ellipse(px, py, -7, -18, 5, 3.5f), Ellipse(px, py, 7, -18, 5, 3.5f)) * s));
            float body = Ellipse(px, py, 0, 0, 14, 19);
            float flippers = Mathf.Min(Ellipse(px, py, -14, 1, 4, 11), Ellipse(px, py, 14, 1, 4, 11));
            float head = Circle(px, py, 0, 18, 9);
            c = Over(c, Hex("222A35"), Fill(Mathf.Min(body, Mathf.Min(head, flippers)) * s));
            c = Over(c, Hex("F4F7FA"), Fill(Ellipse(px, py, 0, -2, 10, 15) * s));
            if (emperor) c = Over(c, Hex("F2B632"), Fill(Ellipse(px, py, 0, 11, 7, 3.5f) * s) * 0.9f);
            c = Over(c, Color.white, Fill(Mathf.Min(Circle(px, py, -3.5f, 20, 2f), Circle(px, py, 3.5f, 20, 2f)) * s));
            c = Over(c, Hex("FF9A2E"), Fill(Polygon(px, py, new[] { new Vector2(-2.5f, 16), new Vector2(2.5f, 16), new Vector2(0, 11) }) * s));
            c = Over(c, Outline, Stroke(Mathf.Min(body, Mathf.Min(head, flippers)) * s, 2.5f) * 0.85f);
            return c;
        }

        // A crowd of penguins on trodden snow, some leaning as if waddling.
        static Color PenguinColony(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, SnowShade, Soft(Ellipse(x, y, 128, 118, 108, 80), 20f) * 0.6f);
            var spots = new[] { (64f, 190f), (110f, 200f), (160f, 192f), (204f, 178f), (44f, 140f), (90f, 150f), (138f, 156f), (186f, 140f), (226f, 120f),
                (60f, 96f), (108f, 104f), (156f, 108f), (200f, 86f), (84f, 52f), (132f, 60f), (178f, 46f) };
            for (int i = 0; i < spots.Length; i++)
                c = PenguinAt(c, x, y, spots[i].Item1, spots[i].Item2, 0.85f + Hash(i, 3) * 0.3f, false, (Hash(i, 7) - 0.5f) * 0.3f);
            return c;
        }

        // A pair of emperor penguins with their chick between their feet.
        static Color EmperorPenguins(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = PenguinAt(c, x, y, 84, 128, 2.6f, true, 0.06f);
            c = PenguinAt(c, x, y, 172, 128, 2.6f, true, -0.06f);
            c = Chick(c, x, y, 128, 66, 1.3f);
            return c;
        }

        // A grey fluffy penguin chick.
        static Color Chick(Color c, float x, float y, float cx, float cy, float s)
        {
            float px = (x - cx) / s, py = (y - cy) / s;
            float fluff = Circle(px, py, 0, 0, 15) + (Noise(px * 0.8f + cx, py * 0.8f + cy, 64) - 0.5f) * 4f;
            float head = Circle(px, py, 0, 16, 9);
            c = Over(c, Shadow, Soft(Circle(px, py, 5, -5, 15), 5f / s));
            c = Over(c, Hex("9AA3AE"), Fill(Mathf.Min(fluff, head) * s));
            c = Over(c, Hex("222A35"), Fill(Circle(px, py, 0, 19, 6) * s));
            c = Over(c, Color.white, Fill(Ellipse(px, py, 0, 14, 6, 4) * s));
            c = Over(c, Outline, Fill(Mathf.Min(Circle(px, py, -3, 16, 1.6f), Circle(px, py, 3, 16, 1.6f)) * s));
            c = Over(c, Outline, Stroke(Mathf.Min(fluff, head) * s, 2.4f) * 0.8f);
            return c;
        }

        // A crèche of fluffy chicks huddled together, one grown-up keeping watch.
        static Color PenguinChicks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var spots = new[] { (80f, 120f), (116f, 108f), (152f, 124f), (100f, 160f), (140f, 168f), (176f, 92f), (70f, 74f), (124f, 70f) };
            for (int i = 0; i < spots.Length; i++) c = Chick(c, x, y, spots[i].Item1, spots[i].Item2, 1.2f + Hash(i, 5) * 0.3f);
            c = PenguinAt(c, x, y, 204, 168, 1.5f);
            return c;
        }

        // A wooden barrel brimming with silver fish, a couple dropped in the snow.
        static Color FishBarrel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float barrel = Circle(x, y, 128, 128, 70);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 116, 72), 10f));
            c = Over(c, Hex("9C6E40"), Fill(barrel));
            c = Over(c, Hex("5A6472"), Fill(Shell(Dist(x, y, 128, 128) - 64f, 3f)));
            Color Fish(Color under, float fx, float fy, float a)
            {
                var p = Local(x, y, fx, fy, a, 1f);
                float body = Ellipse(p.x, p.y, 0, 0, 7, 20);
                float tail = Triangle(p.x, p.y, new Vector2(-8, -28), new Vector2(8, -28), new Vector2(0, -16));
                under = Over(under, Color.Lerp(Hex("D8E4EE"), Hex("7F95AA"), Mathf.InverseLerp(-6, 6, p.x)), Fill(Mathf.Min(body, tail)));
                under = Over(under, Outline, Fill(Circle(p.x, p.y, 0, 12, 1.8f)));
                return Over(under, Hex("33475E"), Stroke(Mathf.Min(body, tail), 2f) * 0.8f);
            }
            if (barrel < -4f)
                for (int i = 0; i < 9; i++)
                    c = Fish(c, 92 + (i % 3) * 36, 92 + (i / 3) * 36, Hash(i, 11) * 360f);
            c = Over(c, Outline, Stroke(barrel, 4f) * 0.85f);
            c = Fish(c, 216, 50, 70f);
            c = Fish(c, 40, 200, -30f);
            return c;
        }

        // The research hut: an orange box on stilts with a snowy flat roof, solar panels and an antenna.
        static Color ResearchHut(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hut = RoundRect(x, y, 128, 128, 100, 64, 8);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 144, 112, 100, 64, 8), 12f));
            c = Over(c, Hex("E0662E"), Fill(hut));
            float roof = RoundRect(x, y, 128, 128, 90, 54, 6);
            c = Over(c, y > 128 ? Snow : SnowShade, Fill(roof));
            foreach (float px in new[] { 74f, 112f })
            {
                float panel = RoundRect(x, y, px, 116, 16, 26, 2);
                c = Over(c, Hex("2E4A7A"), Fill(panel));
                c = Over(c, Hex("6F93C8"), Fill(panel) * (Mathf.Repeat(x, 8f) < 1.2f || Mathf.Repeat(y, 13f) < 1.2f ? 1f : 0f));
                c = Over(c, Outline, Stroke(panel, 2f) * 0.8f);
            }
            float dish = Circle(x, y, 180, 140, 18);
            c = Over(c, Color.white, Fill(dish));
            c = Over(c, Hex("AFC0D2"), Fill(Circle(x, y, 176, 144, 8)));
            c = Over(c, Outline, Stroke(dish, 2.5f));
            c = Over(c, Hex("33475E"), Fill(Capsule(x, y, 196, 96, 222, 70, 2.5f)));
            c = Over(c, Hex("C8352E"), Fill(Circle(x, y, 222, 70, 5)));
            c = Over(c, Outline, Stroke(hut, 4f) * 0.85f);
            return c;
        }

        // A weather mast: a lattice pole with a spinning anemometer and a wind sock, guy lines pegged in the snow.
        static Color WeatherMast(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (gx, gy) in new[] { (40f, 60f), (216f, 72f), (128f, 226f) })
                c = Over(c, Hex("5E6672"), Fill(Capsule(x, y, 128, 128, gx, gy, 1.2f)));
            float pole = Circle(x, y, 128, 128, 14);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 14), 6f));
            c = Over(c, Hex("D9DEE6"), Fill(pole));
            c = Over(c, Hex("8E96A3"), Stroke(pole, 3f));
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f + 0.4f;
                Vector2 tip = new(128 + Mathf.Cos(a) * 40, 128 + Mathf.Sin(a) * 40);
                c = Over(c, Hex("33475E"), Fill(Capsule(x, y, 128, 128, tip.x, tip.y, 2f)));
                c = Over(c, Hex("E8ECF2"), Fill(Circle(x, y, tip.x, tip.y, 8)));
                c = Over(c, Outline, Stroke(Circle(x, y, tip.x, tip.y, 8), 2f));
            }
            // The wind sock, striped red and white, blowing east.
            float sock = Mathf.Max(Capsule(x, y, 140, 100, 212, 92, 11 - Mathf.Clamp01((x - 140) / 72f) * 5f), 138f - x);
            Color stripe = Mathf.Repeat(x - 140f, 24f) < 12f ? Hex("E0442E") : Color.white;
            c = Over(c, stripe, Fill(sock));
            c = Over(c, Outline, Stroke(sock, 2.5f) * 0.8f);
            return c;
        }

        // An arch of blue ice the road runs under (drawn as two great pillars joined by a bridge of ice).
        static Color IceArch(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float n = (Noise(x * 0.12f, y * 0.12f, 32) - 0.5f) * 10f;
            float arch = RoundRect(x, y, 128, 128, 100, 20, 12) + n * 0.6f; // the bridge of ice between the pillars
            float pillars = Mathf.Min(Circle(x, y, 32, 128, 30), Circle(x, y, 224, 128, 30)) + n;
            float all = Mathf.Min(arch, pillars);
            c = Over(c, Shadow, Soft(Mathf.Min(arch, pillars) - 2f, 10f) * 0.8f);
            c = Over(c, Color.Lerp(Hex("E8F8FF"), Hex("6FB7DD"), Mathf.InverseLerp(170, 90, y)), Fill(all));
            if (y > 136f) c = Over(c, Snow, Fill(all + 4f) * Mathf.InverseLerp(136f, 146f, y));
            c = Over(c, Hex("2C6C93"), Stroke(all, 3.5f) * 0.85f);
            return c;
        }

        // A road sign: a yellow diamond with a penguin on it, on a post with snow on top.
        static Color PenguinSign(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Capsule(x, y, 140, 40, 140, 120, 6), 6f));
            c = Over(c, Hex("6B7788"), Fill(Capsule(x, y, 128, 30, 128, 120, 5)));
            float sign = Mathf.Abs(x - 128) + Mathf.Abs(y - 160) - 70f;
            c = Over(c, Shadow, Soft(Mathf.Abs(x - 140) + Mathf.Abs(y - 148) - 70f, 8f));
            c = Over(c, Hex("F2C230"), Fill(sign));
            c = Over(c, Outline, Stroke(sign + 8f, 5f));
            c = PenguinAt(c, x, y, 128, 152, 1.6f, false, 0.12f);
            c = Over(c, Snow, Fill(Mathf.Max(sign, 208f - y)) * Fill(sign));
            c = Over(c, Outline, Stroke(sign, 4f) * 0.9f);
            return c;
        }

        // Two sea kayaks pulled up on the snowy shore, paddles beside them.
        static Color Kayaks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (kx, col, a) in new[] { (96f, Hex("E0442E"), 0.15f), (160f, Hex("F2C230"), -0.1f) })
            {
                var p = Local(x, y, kx, 128, a * Mathf.Rad2Deg, 1f);
                float hull = Ellipse(p.x, p.y, 0, 0, 20, 92);
                c = Over(c, Shadow, Soft(Ellipse(p.x, p.y, -8, 8, 20, 92), 8f));
                c = Over(c, col, Fill(hull));
                c = Over(c, Shade(col, 0.75f), Fill(Ellipse(p.x, p.y, 0, 0, 6, 80)) * Fill(hull));
                c = Over(c, Hex("222A35"), Fill(Ellipse(p.x, p.y, 0, 0, 10, 18)));
                c = Over(c, Outline, Stroke(hull, 3f) * 0.85f);
            }
            float paddle = Capsule(x, y, 196, 40, 226, 220, 2.5f);
            c = Over(c, Hex("33475E"), Fill(paddle));
            c = Over(c, Hex("2E5BA8"), Fill(Mathf.Min(Ellipse(x, y, 197, 46, 7, 16), Ellipse(x, y, 225, 214, 7, 16))));
            return c;
        }
    }
}
