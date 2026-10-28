using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The fjord scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 5, Thin Ice: the Midwinter Ice
    /// Festival on the shore (market stalls, the fish-soup stall, a stage with a band, an ice dragon, ice swan and
    /// bear sculptures, crowds), the fishing village (red, blue and yellow fish houses, an ice-fishing hut, the
    /// lighthouse, seals on the rocks) and the snowy basics. Also a drifting floe and a seal sliding on its belly.
    /// Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] FjordCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "WinterCrowd", "SnowDrift", "Igloo",
            "StallRed", "StallGreen", "StallWine", "FishStall",
            "IceDragon", "IceSwan", "IceBear", "FestivalStage",
            "FishHouseRed", "FishHouseBlue", "FishHouseYellow", "IceFishing",
            "Lighthouse", "Seals", "Cliff", "Crates",
        };

        const int FjordRows = 6;

        /// <summary>UV rect of a fjord scenery cell by name (row 0 at the bottom).</summary>
        public static Rect FjordCell(string name)
        {
            int i = System.Array.IndexOf(FjordCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)FjordRows, 0.25f, 1f / FjordRows);
        }

        [MenuItem("SomeGame/Generate Fjord Art")]
        public static void GenerateFjord()
        {
            Write(ArtGenerator.Folder, "SceneryFjord", 1024, 256 * FjordRows, FjordPixel, 128, false);
            ArtGenerator.Material("SceneryFjord", "SceneryFjord");
            Write(ArtGenerator.Folder, "FjordFloe", 256, 256, FjordFloePixel, 256, false);
            Write(ArtGenerator.Folder, "Seal", 128, 128, SealPixel, 128, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color FjordPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return FjordCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Igloo" => Igloo(lx, ly),
                "StallRed" => Stall(lx, ly, Hex("C8352E"), 0),
                "StallGreen" => Stall(lx, ly, Hex("2E8A5A"), 1),
                "StallWine" => Stall(lx, ly, Hex("7A2E4A"), 2),
                "FishStall" => FishStall(lx, ly),
                "IceDragon" => IceDragon(lx, ly),
                "IceSwan" => IceSwan(lx, ly),
                "IceBear" => IceBear(lx, ly),
                "FestivalStage" => FestivalStage(lx, ly),
                "FishHouseRed" => FishHouse(lx, ly, Hex("A8322D")),
                "FishHouseBlue" => FishHouse(lx, ly, Hex("3E6E9E")),
                "FishHouseYellow" => FishHouse(lx, ly, Hex("E0A82E")),
                "IceFishing" => IceFishing(lx, ly),
                "Lighthouse" => Lighthouse(lx, ly),
                "Seals" => Seals(lx, ly),
                "Cliff" => Cliff(lx, ly),
                _ => Crates(lx, ly),
            };
        }

        // An ice dragon sculpture coiled on a snow plinth, seen from above: an S of a body in glassy ice, spiky
        // wings spread, a horned head with a cold blue eye, its tail curling round.
        static Color IceDragon(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float plinth = Circle(x, y, 128, 124, 104);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 112, 104), 12f));
            c = Over(c, SnowShade, Fill(plinth));
            c = Over(c, Snow, Fill(Circle(x, y, 120, 132, 90)));
            c = Over(c, SnowDeep, Stroke(plinth, 3f));
            // Wings: two fans of icy spikes.
            foreach (float side in new[] { -1f, 1f })
            {
                var w = new[] { new Vector2(128, 132), new Vector2(128 + side * 96, 196), new Vector2(128 + side * 86, 156), new Vector2(128 + side * 104, 128), new Vector2(128 + side * 80, 108), new Vector2(128 + side * 92, 80), new Vector2(128 + side * 30, 110) };
                float wing = Polygon(x, y, w);
                c = Over(c, Color.Lerp(IceLight, IceDeep, Mathf.InverseLerp(128f, 128f + side * 100f, x) * 0.7f), Fill(wing) * 0.92f);
                c = Over(c, IceLine, Stroke(wing, 2.5f) * 0.8f);
                for (int k = 1; k <= 3; k++) c = Over(c, IceLine, Fill(SegDist(new Vector2(x, y), new Vector2(128, 132), w[k * 2 - 1]) - 1f) * Fill(wing) * 0.4f);
            }
            // The body: an S of overlapping circles from the tail (bottom) to the neck (top).
            float body = float.MaxValue;
            for (int k = 0; k <= 14; k++)
            {
                float t = k / 14f;
                float bx = 128 + Mathf.Sin(t * Mathf.PI * 1.6f + 0.4f) * 34f, by = 46 + t * 150f;
                body = Mathf.Min(body, Circle(x, y, bx, by, Mathf.Lerp(9f, 20f, Mathf.Sin(t * Mathf.PI))));
            }
            c = Over(c, Color.Lerp(IceLight, Ice, Mathf.Clamp01((x - 100f) / 60f)), Fill(body));
            // Spines down its back.
            for (int k = 2; k <= 12; k += 2)
            {
                float t = k / 14f;
                float bx = 128 + Mathf.Sin(t * Mathf.PI * 1.6f + 0.4f) * 34f, by = 46 + t * 150f;
                c = Over(c, Color.white, Fill(Star(x, y, bx, by, 7f, 3f, 4, 0.4f)) * 0.9f);
            }
            c = Over(c, IceLine, Stroke(body, 2.5f));
            // The head: a snout pointing up, two horns, eyes.
            float head = Ellipse(x, y, 132, 210, 20, 26);
            c = Over(c, IceLight, Fill(head));
            c = Over(c, IceLine, Stroke(head, 2.5f));
            foreach (float side in new[] { -1f, 1f })
            {
                float horn = Triangle(x, y, new Vector2(132 + side * 10, 200), new Vector2(132 + side * 26, 196), new Vector2(132 + side * 30, 176));
                c = Over(c, Color.white, Fill(horn));
                c = Over(c, IceLine, Stroke(horn, 2f));
                c = Over(c, Hex("2C9BE0"), Fill(Circle(x, y, 132 + side * 9, 214, 3.5f)));
            }
            return c;
        }

        // The festival stage from above: a plank stage with a striped canopy edge, a band of three, speakers,
        // a strip of bunting in front.
        static Color FestivalStage(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float stage = RoundRect(x, y, 128, 140, 104, 70, 8);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 128, 104, 70, 8), 10f));
            Color plank = Mathf.Repeat(y, 14f) < 1.5f ? Hex("7A4E2E") : Color.Lerp(Hex("B07A4A"), Hex("C48C58"), Noise(x / 30f, y / 7f, 13));
            c = Over(c, plank, Fill(stage));
            // Canopy along the back: red and white stripes.
            float canopy = RoundRect(x, y, 128, 196, 104, 16, 4);
            c = Over(c, ((int)(x / 16f) % 2 == 0) ? Hex("D8433A") : Color.white, Fill(canopy));
            c = Over(c, Outline, Stroke(canopy, 2.5f));
            // Speakers and the band.
            foreach (float sx in new[] { 40f, 216f }) { float box = RoundRect(x, y, sx, 160, 14, 16, 3); c = Over(c, Hex("2A2F38"), Fill(box)); c = Over(c, Hex("555C68"), Fill(Circle(x, y, sx, 160, 7))); }
            Color[] shirts = { Hex("3D7DD8"), Hex("F5C842"), Hex("2E9A57") };
            for (int k = 0; k < 3; k++)
            {
                float px = 84 + k * 44, py = 148 - (k == 1 ? 12 : 0);
                c = Over(c, Shadow, Soft(Circle(x, y, px + 4, py - 4, 13), 5f));
                c = Over(c, shirts[k], Fill(Circle(x, y, px, py, 12)));
                c = Over(c, Hex("F2C9A0"), Fill(Circle(x, y, px, py + 2, 6)));
                c = Over(c, Outline, Stroke(Circle(x, y, px, py, 12), 2f));
            }
            c = Over(c, Outline, Stroke(stage, 3.5f));
            // Bunting in front of the stage.
            Color[] flags = { Hex("E2393F"), Hex("F5C842"), Hex("3D7DD8"), Hex("2E9A57") };
            for (int k = 0; k < 9; k++)
            {
                float fx = 28 + k * 25, fy = 58 + Mathf.Sin(k * 0.7f) * 3f;
                float flag = Triangle(x, y, new Vector2(fx - 9, fy + 6), new Vector2(fx + 9, fy + 6), new Vector2(fx, fy - 10));
                c = Over(c, flags[k % 4], Fill(flag));
                c = Over(c, Outline, Stroke(flag, 1.5f) * 0.6f);
            }
            c = Over(c, Outline, Fill(Mathf.Abs(y - 64f - Mathf.Sin(x * 0.03f) * 2f) - 1f) * Fill(Mathf.Abs(x - 128f) - 104f));
            return c;
        }

        // A drifting floe: a ragged slab of white ice with snow on top, a blue rim showing its thickness, cracks.
        static Color FjordFloePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float n = (Noise(x * 0.045f, y * 0.045f, 32) - 0.5f) * 26f + (Noise(x * 0.12f, y * 0.12f, 64) - 0.5f) * 8f;
            float slab = Circle(x, y, 128, 128, 112) + n;
            c = Over(c, new Color(0.05f, 0.15f, 0.25f, 0.35f), Soft(Circle(x, y, 136, 118, 112) + n, 10f));
            c = Over(c, Hex("6FB7DD"), Fill(slab));
            c = Over(c, Hex("BFE6F7"), Fill(slab + 7f));
            float top = slab + 13f;
            c = Over(c, Color.Lerp(Snow, SnowShade, Mathf.Clamp01(Noise(x * 0.03f, y * 0.03f, 8) * 1.3f - 0.2f)), Fill(top));
            float crack = Mathf.Abs(y - 120f - Mathf.Sin(x * 0.05f) * 22f);
            c = Over(c, Hex("9CC9E2"), Fill(crack - 1.2f) * Fill(top + 6f) * 0.8f);
            return Over(c, Hex("2C6C93"), Stroke(slab, 3f) * 0.85f);
        }

        // A seal from above, sliding on its belly (head up): a plump grey body, flippers out, whiskered face.
        static Color SealPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = SMin(Ellipse(x, y, 64, 60, 24, 40), Circle(x, y, 64, 100, 17), 10f);
            c = Over(c, Shadow, Soft(SMin(Ellipse(x, y, 70, 54, 24, 40), Circle(x, y, 70, 94, 17), 10f), 7f));
            foreach (float side in new[] { -1f, 1f })
            {
                float flipper = Ellipse(x, y, 64 + side * 26, 70, 12, 6);
                c = Over(c, Hex("5D6673"), Fill(flipper));
                c = Over(c, Outline, Stroke(flipper, 2f));
                float tail = Ellipse(x, y, 64 + side * 10, 20, 7, 10);
                c = Over(c, Hex("5D6673"), Fill(tail));
                c = Over(c, Outline, Stroke(tail, 2f));
            }
            c = Over(c, Color.Lerp(Hex("8D97A5"), Hex("6B7482"), Mathf.Clamp01((x - 50f) / 30f)), Fill(body));
            for (int k = 0; k < 7; k++) c = Over(c, Hex("5D6673"), Fill(Circle(x, y, 56 + Hash(k, 3) * 18, 40 + Hash(k, 7) * 40, 2.5f)));
            c = Over(c, Outline, Stroke(body, 2.5f));
            foreach (float side in new[] { -1f, 1f }) c = Over(c, Outline, Fill(Circle(x, y, 64 + side * 7, 106, 3f)));
            c = Over(c, Hex("2A2F38"), Fill(Ellipse(x, y, 64, 112, 4, 3)));
            return c;
        }
    }
}
