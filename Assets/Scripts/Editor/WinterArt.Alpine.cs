using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The bigger winter scenery atlas (24 cells of 256 px, 4 x 6) for the reworked Winter Cup race 1: the
    /// first sheet's twelve cells plus a Christmas tree, Christmas market huts, a chalet, ski lift pylons,
    /// slalom gates, a ski rack, a crowd in winter coats, a reindeer, snowdrifts, a bench, a hot chocolate
    /// stand and a group of penguins. Row 0 at the bottom, like the first sheet.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] AlpineCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "Snowman",
            "Cabin", "SnowRock", "Igloo", "Sled",
            "IceCrystals", "Gifts", "FrozenPond", "Lamp",
            "ChristmasTree", "MarketHut", "MarketHutRed", "Chalet",
            "SkiLift", "Slalom", "SkiRack", "WinterCrowd",
            "Reindeer", "SnowDrift", "Bench", "CocoaStand",
        };

        const int AlpineRows = 6;

        /// <summary>UV rect of an Alpine scenery cell by name (row 0 at the bottom).</summary>
        public static Rect AlpineCell(string name)
        {
            int i = System.Array.IndexOf(AlpineCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)AlpineRows, 0.25f, 1f / AlpineRows);
        }

        [MenuItem("SomeGame/Generate Alpine Art")]
        public static void GenerateAlpine()
        {
            Write(ArtGenerator.Folder, "SceneryAlpine", 1024, 256 * AlpineRows, AlpinePixel, 128, false);
            ArtGenerator.Material("SceneryAlpine", "SceneryAlpine");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color AlpinePixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return AlpineCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "Cabin" => Cabin(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Igloo" => Igloo(lx, ly),
                "Sled" => Sled(lx, ly),
                "IceCrystals" => IceCrystals(lx, ly),
                "Gifts" => Gifts(lx, ly),
                "FrozenPond" => FrozenPond(lx, ly),
                "Lamp" => Lamp(lx, ly),
                "ChristmasTree" => ChristmasTree(lx, ly),
                "MarketHut" => MarketHut(lx, ly, Hex("8E5C3B")),
                "MarketHutRed" => MarketHut(lx, ly, Hex("B8432C")),
                "Chalet" => Chalet(lx, ly),
                "SkiLift" => SkiLift(lx, ly),
                "Slalom" => Slalom(lx, ly),
                "SkiRack" => SkiRack(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "Reindeer" => Reindeer(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Bench" => SnowBench(lx, ly),
                _ => CocoaStand(lx, ly),
            };
        }

        // The village Christmas tree: a big snowy fir in tiers with coloured baubles, lights and a gold star.
        static Color ChristmasTree(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(1f, 0.85f, 0.5f, 1f), Gauss(Dist(x, y, 128, 128), 70f) * 0.35f);
            c = Over(c, Shadow, Soft(Star(x, y, 142, 108, 112, 88, 12, 0.2f), 10f));
            float outer = Star(x, y, 128, 128, 112, 88, 12, 0.2f);
            c = Over(c, Hex("2E6A4A"), Fill(outer));
            c = Over(c, Hex("3E8259"), Fill(Star(x, y, 126, 132, 80, 62, 10, 0.5f)));
            c = Over(c, Hex("4F9A68"), Fill(Star(x, y, 124, 136, 48, 36, 8, 0.1f)));
            c = Over(c, Snow, Fill(Mathf.Max(outer, -Star(x, y, 128, 128, 98, 76, 12, 0.2f))) * 0.85f);
            Color[] balls = { Hex("E2513A"), Hex("F5C842"), Hex("3D7DD8"), Hex("E86FB0") };
            for (int i = 0; i < 18; i++)
            {
                float a = i * 2.4f, r = 30f + (i % 3) * 24f;
                float bx = 128 + Mathf.Cos(a) * r, by = 128 + Mathf.Sin(a) * r;
                c = Over(c, balls[i % 4], Fill(Circle(x, y, bx, by, 7)));
                c = Over(c, Color.white, Fill(Circle(x, y, bx - 2, by + 2, 2)));
            }
            // A string of lights spiralling round.
            float ang = Mathf.Atan2(y - 128, x - 128), rr = Dist(x, y, 128, 128);
            float spiral = Mathf.Abs(Mathf.Repeat(rr - ang / (Mathf.PI * 2f) * 34f, 34f) - 17f);
            if (rr > 20 && rr < 100 && spiral < 2.2f && Mathf.Repeat(rr * 0.6f, 6f) < 2.5f) c = Over(c, Hex("FFF2B0"), Fill(outer));
            c = Over(c, Outline, Stroke(outer, 4f) * 0.8f);
            float star = Star(x, y, 128, 128, 22, 9, 5, 0f);
            c = Over(c, Hex("F5C842"), Fill(star));
            c = Over(c, Hex("B8862A"), Stroke(star, 2.5f));
            return c;
        }

        // A Christmas market hut: a snowy gable roof over a wooden booth, a garland with lights at the front.
        static Color MarketHut(float x, float y, Color wood)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 116, 96, 70, 6), 10f));
            float counter = RoundRect(x, y, 128, 62, 88, 16, 4);
            c = Over(c, wood, Fill(counter));
            for (int i = 0; i < 6; i++) c = Over(c, i % 2 == 0 ? Hex("E2513A") : Hex("F5C842"), Fill(Circle(x, y, 60 + i * 27, 64, 7)));
            c = Over(c, Outline, Stroke(counter, 3f) * 0.8f);
            float roof = RoundRect(x, y, 128, 140, 96, 62, 6);
            c = Over(c, Shade(wood, 0.8f), Fill(roof));
            c = Over(c, y > 140 ? Snow : SnowShade, Fill(RoundRect(x, y, 128, 142, 88, 54, 5)));
            c = Over(c, Hex("B9CBDD"), Fill(Mathf.Abs(y - 140f) - 2f) * Fill(roof));
            c = Over(c, Outline, Stroke(roof, 4.5f) * 0.85f);
            // The garland of fir with lights along the front edge.
            float garland = Mathf.Abs(y - 82f - Mathf.Abs(Mathf.Sin((x - 32) / 192f * Mathf.PI * 3f)) * 8f) - 4f;
            if (x > 34 && x < 222) c = Over(c, Hex("2E6A4A"), Fill(garland));
            if (x > 34 && x < 222 && Mathf.Repeat(x, 16f) < 4f) c = Over(c, Hex("FFF2B0"), Fill(garland + 1f));
            return c;
        }

        // A big chalet: a red-brown roof under a thick layer of snow, wooden balcony, two chimneys smoking.
        static Color Chalet(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 108, 112, 90, 8), 12f));
            float eaves = RoundRect(x, y, 128, 128, 112, 90, 8);
            c = Over(c, Hex("7A3F2A"), Fill(eaves));
            float roof = RoundRect(x, y, 128, 128, 102, 80, 6);
            Color face = x < 128 ? Snow : SnowShade;
            if (Mathf.Repeat(x - 128, 24f) < 1.6f) face = Shade(face, 0.95f);
            c = Over(c, face, Fill(roof));
            c = Over(c, Hex("B9CBDD"), Fill(Mathf.Abs(x - 128f) - 2.5f) * Fill(roof));
            // The balcony along the front.
            float balcony = RoundRect(x, y, 128, 30, 96, 10, 3);
            c = Over(c, Hex("A06A40"), Fill(balcony));
            for (int i = 0; i < 12; i++) c = Over(c, Hex("6E4228"), Fill(RoundRect(x, y, 40 + i * 16, 30, 2, 9, 1)));
            foreach (var (cx, cy) in new[] { (84f, 176f), (178f, 92f) })
            {
                float ch = RoundRect(x, y, cx, cy, 12, 12, 3);
                c = Over(c, Hex("9A9AA6"), Fill(ch));
                c = Over(c, Hex("3A3532"), Fill(RoundRect(x, y, cx, cy, 5, 5, 2)));
                c = Over(c, new Color(0.9f, 0.92f, 0.95f, 0.55f), Fill(Circle(x, y, cx + 16, cy + 18, 14)) + Fill(Circle(x, y, cx + 30, cy + 34, 10)) * 0.7f);
            }
            c = Over(c, Outline, Stroke(eaves, 5f) * 0.9f);
            return c;
        }

        // A ski lift pylon from above: the steel tower head with the cable running across the cell (pieces join
        // up in a line) and a double chair hanging from it.
        static Color SkiLift(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(Mathf.Abs(y - 112f) - 2f) * 0.8f);
            c = Over(c, Hex("3A4B5E"), Fill(Mathf.Abs(y - 128f) - 2.2f));
            float head = RoundRect(x, y, 128, 128, 18, 40, 4);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 18, 40, 4), 6f));
            c = Over(c, Hex("E2513A"), Fill(head));
            c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 128, 128, 8)));
            c = Over(c, Outline, Stroke(head, 3.5f) * 0.85f);
            // A chair on the cable.
            float chair = RoundRect(x, y, 200, 128, 20, 14, 4);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 210, 114, 20, 14, 4), 5f));
            c = Over(c, Hex("3D7DD8"), Fill(chair));
            c = Over(c, Hex("F2C9A0"), Fill(Circle(x, y, 192, 128, 6)));
            c = Over(c, Hex("E86FB0"), Fill(Circle(x, y, 208, 128, 6)));
            c = Over(c, Outline, Stroke(chair, 3f) * 0.8f);
            return c;
        }

        // A slalom gate: two poles with a red flag between them, ski tracks curving past in the snow.
        static Color Slalom(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float track = Mathf.Abs(x - 128f - Mathf.Sin(y / 256f * Mathf.PI * 2f) * 60f);
            if (Mathf.Abs(track - 8f) < 2f) c = Over(c, SnowDeep, 0.55f);
            float flag = RoundRect(x, y, 128, 140, 44, 14, 3);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 138, 128, 44, 14, 3), 5f));
            c = Over(c, Hex("E2513A"), Fill(flag));
            c = Over(c, Color.white, Fill(Mathf.Abs(x - 128f) - 4f) * Fill(flag));
            c = Over(c, Outline, Stroke(flag, 3f) * 0.8f);
            foreach (float px in new[] { 82f, 174f })
            {
                c = Over(c, Hex("2A3A4A"), Fill(Circle(x, y, px, 140, 7)));
                c = Over(c, Hex("F5C842"), Fill(Circle(x, y, px, 140, 3.5f)));
            }
            return c;
        }

        // A rack of skis and poles stuck in the snow.
        static Color SkiRack(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 138, 118, 100, 12, 4), 6f));
            float rail = RoundRect(x, y, 128, 128, 100, 8, 3);
            c = Over(c, Hex("8E5C3B"), Fill(rail));
            Color[] skis = { Hex("E2513A"), Hex("3D7DD8"), Hex("F5C842"), Hex("2BB3A3"), Hex("E86FB0") };
            for (int i = 0; i < 5; i++)
            {
                float sx = 52 + i * 38;
                for (int k = -1; k <= 1; k += 2)
                {
                    float ski = RoundRect(x, y, sx + k * 6, 128, 4, 92, 4);
                    c = Over(c, skis[i], Fill(ski));
                    c = Over(c, Outline, Stroke(ski, 2f) * 0.7f);
                }
            }
            c = Over(c, Outline, Stroke(rail, 2.5f) * 0.7f);
            return c;
        }

        // Spectators in winter coats and bobble hats behind a snowy bank.
        static Color WinterCrowd(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] coats = { Hex("E2513A"), Hex("3D7DD8"), Hex("F5C842"), Hex("2BB3A3"), Hex("E86FB0"), Hex("8A5CD6"), Hex("FF7A1A") };
            c = Over(c, SnowShade, Fill(RoundRect(x, y, 128, 66, 122, 14, 8)));
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 6; i++)
                {
                    float px = 30 + i * 39 + (row % 2) * 18, py = 100 + row * 52;
                    int k = (int)(Hash(i + 3, row + 7) * coats.Length);
                    c = Over(c, Shadow, Soft(Circle(x, y, px + 5, py - 5, 19), 6f));
                    c = Over(c, coats[k], Fill(Ellipse(x, y, px, py, 19, 15)));
                    if (Hash(i + 9, row) > 0.55f) c = Over(c, Shade(coats[k], 0.85f), Fill(Capsule(x, y, px + 12, py + 6, px + 24, py + 26, 5f)));
                    c = Over(c, Hex("F2C9A0"), Fill(Circle(x, y, px, py + 2, 9)));
                    Color hat = coats[(k + 2) % coats.Length];
                    c = Over(c, hat, Fill(Circle(x, y, px, py + 6, 8.5f)));
                    c = Over(c, Color.white, Fill(Circle(x, y, px, py + 9, 3.5f))); // bobble
                    c = Over(c, Outline, Stroke(Ellipse(x, y, px, py, 19, 15), 2.5f) * 0.6f);
                }
            return c;
        }

        // A reindeer from above: brown body, white tail, antlers spreading over its head, a red nose.
        static Color Reindeer(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 140, 110, 44, 78), 8f));
            float body = Ellipse(x, y, 128, 118, 42, 74);
            c = Over(c, Hex("9A6A44"), Fill(body));
            c = Over(c, Hex("B9875C"), Fill(Ellipse(x, y, 122, 122, 22, 54)));
            c = Over(c, Snow, Fill(Circle(x, y, 128, 48, 10)));
            float head = Ellipse(x, y, 128, 196, 22, 26);
            c = Over(c, Hex("8A5A38"), Fill(head));
            c = Over(c, Hex("E2513A"), Fill(Circle(x, y, 128, 220, 7)));
            foreach (int s in new[] { -1, 1 })
            {
                float antler = Min(Capsule(x, y, 128 + s * 10, 196, 128 + s * 54, 186, 4), Capsule(x, y, 128 + s * 34, 191, 128 + s * 44, 214, 3.5f),
                    Capsule(x, y, 128 + s * 50, 187, 128 + s * 66, 204, 3.5f));
                c = Over(c, Hex("D9C29A"), Fill(antler));
                c = Over(c, Outline, Stroke(antler, 2f) * 0.6f);
            }
            c = Over(c, Outline, Stroke(Mathf.Min(body, head), 3.5f) * 0.8f);
            return c;
        }

        // A soft snowdrift (laid flat): a blob of deep, wind-shaped snow with blue shade on one side.
        static Color SnowDrift(float x, float y)
        {
            float a = Mathf.Atan2(y - 128, x - 128);
            float d = Dist(x, y, 128, 128) - 104f - Mathf.Sin(a * 3f + 0.7f) * 12f - Mathf.Sin(a * 5f) * 6f;
            float alpha = Mathf.Clamp01(-d / 18f);
            if (alpha <= 0f) return new Color(0, 0, 0, 0);
            Color c = Color.Lerp(SnowShade, Snow, Mathf.Clamp01(0.55f + ((x - 128) * -0.6f + (y - 128) * 0.8f) / 200f));
            if (Hash((int)(x / 2), (int)(y / 2)) > 0.993f) c = Color.white;
            return new Color(c.r, c.g, c.b, alpha * 0.95f);
        }

        // A park bench with a cushion of snow on its seat.
        static Color SnowBench(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 138, 118, 96, 34, 5), 6f));
            float seat = RoundRect(x, y, 128, 128, 96, 34, 5);
            c = Over(c, Hex("8E5C3B"), Fill(seat));
            for (int i = -1; i <= 1; i++) c = Over(c, Hex("6E4228"), Fill(Mathf.Abs(y - 128 - i * 22f) - 1.2f) * Fill(seat));
            c = Over(c, Snow, Fill(RoundRect(x, y, 120, 132, 70, 20, 12) + Noise(x / 8f, y / 8f, 32) * 6f));
            c = Over(c, Outline, Stroke(seat, 3.5f) * 0.85f);
            return c;
        }

        // A hot chocolate stand: a round red and white striped canopy, steaming mugs on the counter.
        static Color CocoaStand(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float counter = RoundRect(x, y, 128, 50, 80, 16, 5);
            c = Over(c, Hex("8E5C3B"), Fill(counter));
            for (int i = 0; i < 4; i++)
            {
                float mx = 80 + i * 32;
                c = Over(c, Color.white, Fill(Circle(x, y, mx, 52, 9)));
                c = Over(c, Hex("6B3A22"), Fill(Circle(x, y, mx, 52, 6)));
                c = Over(c, new Color(1f, 1f, 1f, 0.5f), Fill(Circle(x, y, mx + 4, 70, 6)));
            }
            c = Over(c, Outline, Stroke(counter, 3f) * 0.8f);
            float a = Mathf.Atan2(y - 150, x - 128);
            float canopy = Circle(x, y, 128, 150, 84) + Mathf.Abs(Mathf.Sin(a * 6f)) * 6f;
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 134, 84), 10f));
            c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 12f, 1f) < 0.5f ? Hex("D9433A") : Snow, Fill(canopy));
            c = Over(c, Snow, Fill(Circle(x, y, 120, 160, 40) + Noise(x / 9f, y / 9f, 28) * 10f) * Fill(canopy) * 0.85f);
            c = Over(c, Outline, Stroke(canopy, 4f) * 0.85f);
            return c;
        }
    }
}
