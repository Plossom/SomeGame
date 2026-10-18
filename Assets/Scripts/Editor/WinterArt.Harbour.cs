using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The harbour scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 5, Frozen Harbour: snowy
    /// basics plus Nordic wooden fishing houses in three colours, crates, a fish drying rack, nets, the
    /// lighthouse, a harbour crane, containers, seals and polar bears, an anchor, bollards, a fish stall,
    /// lobster pots and a heap of buoys. Also the floating pieces drawn on the water (their own sprites):
    /// an ice floe (the jump pad on winter water), an iceberg, a fishing trawler, an icebreaker and seals on
    /// a floe. Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] HarbourCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "WinterCrowd", "SnowDrift", "Igloo",
            "FishHouseRed", "FishHouseYellow", "FishHouseBlue", "Crates",
            "FishRack", "Nets", "Lighthouse", "Crane",
            "Containers", "Seals", "PolarBears", "Anchor",
            "Bollards", "FishStall", "LobsterPots", "Buoys",
        };

        const int HarbourRows = 6;

        /// <summary>UV rect of a harbour scenery cell by name (row 0 at the bottom).</summary>
        public static Rect HarbourCell(string name)
        {
            int i = System.Array.IndexOf(HarbourCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)HarbourRows, 0.25f, 1f / HarbourRows);
        }

        [MenuItem("SomeGame/Generate Harbour Art")]
        public static void GenerateHarbour()
        {
            Write(ArtGenerator.Folder, "SceneryHarbour", 1024, 256 * HarbourRows, HarbourPixel, 128, false);
            ArtGenerator.Material("SceneryHarbour", "SceneryHarbour");
            Write(ArtGenerator.Folder, "IceFloe", 192, 192, IceFloePixel, 96, false);
            Write(ArtGenerator.Folder, "Iceberg", 256, 256, IcebergPixel, 128, false);
            Write(ArtGenerator.Folder, "Trawler", 160, 320, TrawlerPixel, 80, false);
            Write(ArtGenerator.Folder, "Icebreaker", 192, 384, IcebreakerPixel, 96, false);
            Write(ArtGenerator.Folder, "SealFloe", 192, 192, SealFloePixel, 96, false);
            Write(ArtGenerator.Folder, "SnowShore", 64, 64, SnowShorePixel, 32, true);
            ArtGenerator.Material("SnowShore", "SnowShore");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color HarbourPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return HarbourCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Igloo" => Igloo(lx, ly),
                "FishHouseRed" => FishHouse(lx, ly, Hex("A8322D")),
                "FishHouseYellow" => FishHouse(lx, ly, Hex("E0A82E")),
                "FishHouseBlue" => FishHouse(lx, ly, Hex("3E6E9E")),
                "Crates" => Crates(lx, ly),
                "FishRack" => FishRack(lx, ly),
                "Nets" => Nets(lx, ly),
                "Lighthouse" => Lighthouse(lx, ly),
                "Crane" => HarbourCrane(lx, ly),
                "Containers" => Containers(lx, ly),
                "Seals" => Seals(lx, ly),
                "PolarBears" => PolarBears(lx, ly),
                "Anchor" => Anchor(lx, ly),
                "Bollards" => Bollards(lx, ly),
                "FishStall" => FishStall(lx, ly),
                "LobsterPots" => LobsterPots(lx, ly),
                _ => Buoys(lx, ly),
            };
        }

        // A Nordic fishing house from above: a pitched roof under snow, coloured board walls showing at the
        // gable ends, white window frames, a little jetty of planks at the front.
        static Color FishHouse(float x, float y, Color paint)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 144, 118, 92, 80, 6), 12f));
            float walls = RoundRect(x, y, 128, 134, 92, 76, 5);
            Color boards = Mathf.Repeat(x, 12f) < 1.5f ? Shade(paint, 0.75f) : paint;
            c = Over(c, boards, Fill(walls));
            // Planks of the jetty in front.
            float jetty = RoundRect(x, y, 128, 40, 56, 18, 2);
            c = Over(c, Mathf.Repeat(x, 14f) < 1.5f ? Hex("6B4A2E") : Hex("9C7148"), Fill(jetty));
            c = Over(c, Outline, Stroke(jetty, 2.5f) * 0.8f);
            float roof = RoundRect(x, y, 128, 140, 84, 60, 4);
            c = Over(c, Shade(paint, 0.55f), Fill(roof + 3f));
            c = Over(c, y > 140 ? Snow : SnowShade, Fill(roof));
            c = Over(c, SnowDeep, Fill(Mathf.Abs(y - 140f) - 2f) * Fill(roof));
            // Windows in the gable ends (left and right).
            foreach (float wx in new[] { 40f, 216f })
            {
                float w = RoundRect(x, y, wx, 134, 6, 12, 1.5f);
                c = Over(c, Color.white, Fill(w + 2.5f));
                c = Over(c, Hex("33475E"), Fill(w));
            }
            c = Over(c, Outline, Stroke(walls, 4f) * 0.85f);
            return c;
        }

        // A stack of wooden fish crates, some with fish showing.
        static Color Crates(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var crates = new[] { (80f, 96f, 0.1f), (150f, 90f, -0.15f), (112f, 156f, 0.05f), (182f, 154f, 0.3f) };
            foreach (var (cx, cy, a) in crates) c = Over(c, Shadow, Soft(RotRect(x, y, cx + 10, cy - 10, 34, 26, 3, a), 8f));
            foreach (var (cx, cy, a) in crates)
            {
                float box = RotRect(x, y, cx, cy, 34, 26, 3, a);
                c = Over(c, Hex("B88A56"), Fill(box));
                float inner = RotRect(x, y, cx, cy, 28, 20, 2, a);
                c = Over(c, Hex("C9D6E2"), Fill(inner));
                // Fish lying in the ice.
                for (int i = 0; i < 3; i++)
                {
                    float fx = cx - 14 + i * 14, fy = cy + (i % 2) * 6 - 3;
                    c = Over(c, Hex("7C93AE"), Fill(Ellipse(x, y, fx, fy, 5, 13)) * Fill(inner));
                }
                c = Over(c, Outline, Stroke(box, 3f));
            }
            return c;
        }

        // A wooden drying rack with rows of stockfish hanging from it.
        static Color FishRack(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 116, 50, 4), 10f));
            for (int row = 0; row < 3; row++)
            {
                float ry = 92 + row * 36;
                c = Over(c, Hex("6B4A2E"), Fill(RoundRect(x, y, 128, ry, 116, 4, 2)));
                for (int i = 0; i < 16; i++)
                {
                    float fx = 22 + i * 14;
                    float fish = Ellipse(x, y, fx, ry - 10, 4.5f, 11);
                    c = Over(c, Color.Lerp(Hex("C9B38E"), Hex("9C8562"), Hash(i, row)), Fill(fish));
                    c = Over(c, Hex("5E4A33"), Stroke(fish, 1.5f) * 0.7f);
                }
            }
            foreach (float px in new[] { 14f, 128f, 242f }) c = Over(c, Hex("5E4A33"), Fill(RoundRect(x, y, px, 128, 5, 60, 2)));
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 166, 112, 3, 1.5f)) * 0.9f);
            return c;
        }

        // Fishing nets spread out on the snow, floats along the edge.
        static Color Nets(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float net = Ellipse(x, y, 128, 128, 110, 74) + Mathf.Sin(x * 0.08f) * 6f;
            float mesh = Mathf.Min(Mathf.Abs(Mathf.Repeat(x + y, 14f) - 7f), Mathf.Abs(Mathf.Repeat(x - y, 14f) - 7f));
            if (net < 0f && mesh < 1.3f) c = Over(c, Hex("3D7D86"), 0.9f);
            c = Over(c, Hex("2E5E66"), Stroke(net, 3f));
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                c = Over(c, i % 2 == 0 ? Hex("FF7A1A") : Hex("F5C842"), Fill(Circle(x, y, 128 + Mathf.Cos(a) * 110, 128 + Mathf.Sin(a) * 74, 6)));
            }
            return c;
        }

        // The lighthouse from above: a round red and white tower, the lantern room with its gallery on top.
        static Color Lighthouse(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 150, 106, 70), 14f));
            float tower = Circle(x, y, 128, 128, 70);
            float r = Dist(x, y, 128, 128);
            c = Over(c, Mathf.Repeat(r, 28f) < 14f ? CandyRed : Icing, Fill(tower));
            c = Over(c, Outline, Stroke(tower, 4f));
            float gallery = Circle(x, y, 128, 128, 40);
            c = Over(c, Hex("33475E"), Fill(gallery));
            c = Over(c, Hex("5B6878"), Stroke(gallery, 4f));
            c = Over(c, Hex("FFE9A8"), Fill(Circle(x, y, 128, 128, 26)));
            c = Over(c, Hex("33475E"), Fill(Mathf.Min(Mathf.Abs(x - 128f), Mathf.Abs(y - 128f)) - 1.5f) * Fill(Circle(x, y, 128, 128, 26)));
            c = Over(c, CandyRed, Fill(Circle(x, y, 128, 128, 13)));
            c = Over(c, Outline, Stroke(Circle(x, y, 128, 128, 13), 2f));
            return c;
        }

        // A harbour crane from above: a yellow cabin on a square base, the long jib reaching out.
        static Color HarbourCrane(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Mathf.Min(RoundRect(x, y, 86, 74, 46, 46, 6), Capsule(x, y, 96, 88, 236, 226, 10)), 10f));
            float baseRect = RoundRect(x, y, 72, 88, 46, 46, 6);
            c = Over(c, Hex("5B6878"), Fill(baseRect));
            c = Over(c, Outline, Stroke(baseRect, 3f));
            float jib = Capsule(x, y, 72, 88, 222, 238, 9);
            c = Over(c, Hex("F5C842"), Fill(jib));
            if (jib < 0f && Mathf.Repeat((x + y) * 0.7f, 20f) < 3f) c = Over(c, Hex("B88A1E"), 0.9f);
            c = Over(c, Outline, Stroke(jib, 3f));
            float cab = RoundRect(x, y, 72, 88, 26, 22, 4);
            c = Over(c, Hex("F5C842"), Fill(cab));
            c = Over(c, Hex("A8D8F0"), Fill(RoundRect(x, y, 88, 92, 8, 12, 2)));
            c = Over(c, Outline, Stroke(cab, 3f));
            c = Over(c, Outline, Fill(Capsule(x, y, 222, 238, 222, 216, 2f)));
            c = Over(c, Hex("5B6878"), Fill(RoundRect(x, y, 222, 210, 10, 6, 2)));
            return c;
        }

        // Shipping containers stacked: red, blue, green, with ribs and a little snow on top.
        static Color Containers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var boxes = new[] { (128f, 72f, CandyRed), (128f, 128f, Hex("3D7DD8")), (128f, 184f, Hex("2E9A57")) };
            foreach (var (bx, by, _) in boxes) c = Over(c, Shadow, Soft(RoundRect(x, y, bx + 10, by - 10, 108, 24, 3), 8f));
            foreach (var (bx, by, col) in boxes)
            {
                float box = RoundRect(x, y, bx, by, 108, 24, 3);
                Color face = Mathf.Repeat(x, 9f) < 2f ? Shade(col, 0.78f) : col;
                c = Over(c, face, Fill(box));
                c = Over(c, Snow, Fill(RoundRect(x, y, bx - 20, by + 8, 60, 8, 4)) * 0.85f);
                c = Over(c, Outline, Stroke(box, 3f));
            }
            return c;
        }

        // Seals lying on the snowy shore.
        static Color Seals(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var seals = new[] { (80f, 110f, 0.4f), (160f, 92f, -0.5f), (134f, 172f, 1.4f) };
            foreach (var (sx, sy, a) in seals) c = Over(c, Shadow, Soft(RotRect(x, y, sx + 8, sy - 8, 20, 44, 18, a), 8f));
            foreach (var (sx, sy, a) in seals) c = SealOver(c, x, y, sx, sy, a);
            return c;
        }

        static Color SealOver(Color c, float x, float y, float sx, float sy, float a)
        {
            float body = RotRect(x, y, sx, sy, 20, 44, 18, a);
            c = Over(c, Color.Lerp(Hex("8A939E"), Hex("5E6A78"), Noise(x * 0.2f, y * 0.2f, 8) * 0.6f), Fill(body));
            Vector2 up = new(-Mathf.Sin(a), Mathf.Cos(a));
            Vector2 head = new Vector2(sx, sy) + up * 34f;
            c = Over(c, Hex("6B7480"), Fill(Circle(x, y, head.x, head.y, 14)));
            c = Over(c, Outline, Fill(Circle(x, y, head.x + up.y * 6 + up.x * 6, head.y - up.x * 6 + up.y * 6, 2.5f)));
            c = Over(c, Outline, Fill(Circle(x, y, head.x - up.y * 6 + up.x * 6, head.y + up.x * 6 + up.y * 6, 2.5f)));
            c = Over(c, Outline, Stroke(Mathf.Min(body, Circle(x, y, head.x, head.y, 14)), 2.5f) * 0.85f);
            return c;
        }

        // A polar bear mother and cub walking.
        static Color PolarBears(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (bx, by, s) in new[] { (110f, 128f, 1f), (186f, 104f, 0.6f) })
            {
                float body = Ellipse(x, y, bx, by, 48 * s, 28 * s);
                float head = Circle(x, y, bx + 54 * s, by + 4 * s, 18 * s);
                float paws = Min(Circle(x, y, bx - 30 * s, by - 24 * s, 9 * s), Circle(x, y, bx + 26 * s, by - 24 * s, 9 * s), Circle(x, y, bx - 30 * s, by + 24 * s, 9 * s), Circle(x, y, bx + 26 * s, by + 24 * s, 9 * s));
                float all = Min(body, head, paws);
                c = Over(c, Shadow, Soft(Ellipse(x, y, bx + 8, by - 8, 52 * s, 30 * s), 8f));
                c = Over(c, Color.Lerp(Hex("FFFCF0"), Hex("E8DFC8"), Mathf.InverseLerp(by + 30, by - 30, y)), Fill(all));
                c = Over(c, Outline, Fill(Circle(x, y, bx + 70 * s, by + 4 * s, 3.5f * s)));
                c = Over(c, Outline, Stroke(all, 3f) * 0.8f);
            }
            return c;
        }

        // An old ship's anchor lying in the snow, a coil of rope beside it.
        static Color Anchor(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float shank = Capsule(x, y, 128, 60, 128, 196, 9);
            float stock = Capsule(x, y, 86, 184, 170, 184, 7);
            float arms = Mathf.Max(Mathf.Abs(Dist(x, y, 128, 110) - 52f) - 8f, y - 100f);
            float ring = Mathf.Abs(Circle(x, y, 128, 210, 13)) - 4f;
            float all = Min(shank, stock, arms, ring);
            c = Over(c, Shadow, Soft(Min(Capsule(x, y, 140, 48, 140, 184, 9), Mathf.Max(Mathf.Abs(Dist(x, y, 140, 98) - 52f) - 8f, y - 88f)), 8f));
            c = Over(c, Hex("4A5462"), Fill(all));
            c = Over(c, Hex("6B7788"), Fill(Capsule(x, y, 124, 70, 124, 190, 3)) * Fill(shank));
            c = Over(c, Outline, Stroke(all, 3f));
            float rope = Mathf.Abs(Mathf.Repeat(Dist(x, y, 196, 76), 9f) - 4.5f) - 2.5f;
            if (Dist(x, y, 196, 76) < 36f) c = Over(c, Hex("C9A46A"), Fill(rope));
            return c;
        }

        // A row of iron bollards along the quay.
        static Color Bollards(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                float bx = 40 + i * 58;
                c = Over(c, Shadow, Soft(Circle(x, y, bx + 8, 120, 20), 6f));
                float b = Circle(x, y, bx, 128, 20);
                c = Over(c, Hex("33475E"), Fill(b));
                c = Over(c, Hex("5B6878"), Fill(Circle(x, y, bx - 5, 133, 9)));
                c = Over(c, Snow, Fill(Circle(x, y, bx - 3, 132, 7)) * 0.8f);
                c = Over(c, Outline, Stroke(b, 3f));
            }
            return c;
        }

        // A fish stall in the market hall: a counter full of fish on ice under a striped awning.
        static Color FishStall(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 100, 64, 6), 10f));
            float counter = RoundRect(x, y, 128, 100, 96, 34, 5);
            c = Over(c, Hex("DCEAF4"), Fill(counter));
            Color[] fish = { Hex("7C93AE"), Hex("E2793F"), Hex("8A939E") };
            for (int i = 0; i < 10; i++)
            {
                float fx = 46 + i * 18, fy = 100 + ((i % 2) * 2 - 1) * 10;
                c = Over(c, fish[i % 3], Fill(Ellipse(x, y, fx, fy, 6, 14)));
            }
            c = Over(c, Outline, Stroke(counter, 3f));
            float awning = RoundRect(x, y, 128, 170, 100, 26, 4);
            c = Over(c, Mathf.Repeat(x, 28f) < 14f ? Hex("3E6E9E") : Icing, Fill(awning));
            c = Over(c, Outline, Stroke(awning, 3f));
            return c;
        }

        // Lobster pots stacked on the quay: wooden frames with netting, ropes, orange floats.
        static Color LobsterPots(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pots = new[] { (86f, 96f), (158f, 96f), (122f, 164f) };
            foreach (var (px, py) in pots) c = Over(c, Shadow, Soft(RoundRect(x, y, px + 10, py - 10, 34, 28, 10), 8f));
            foreach (var (px, py) in pots)
            {
                float pot = RoundRect(x, y, px, py, 34, 28, 10);
                c = Over(c, Hex("6B4A2E"), Fill(pot));
                float mesh = Mathf.Min(Mathf.Abs(Mathf.Repeat(x - px, 10f) - 5f), Mathf.Abs(Mathf.Repeat(y - py, 10f) - 5f));
                c = Over(c, Hex("3D7D86"), Fill(pot + 5f) * (mesh < 1.3f ? 1f : 0.15f));
                c = Over(c, Outline, Stroke(pot, 3f));
            }
            c = Over(c, Hex("FF7A1A"), Fill(Circle(x, y, 196, 180, 12)));
            c = Over(c, Outline, Stroke(Circle(x, y, 196, 180, 12), 2.5f));
            return c;
        }

        // A heap of fishing buoys in red, orange and white.
        static Color Buoys(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] cols = { CandyRed, Hex("FF7A1A"), Icing, Hex("F5C842") };
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.3f, r = 18f + (i * 29 % 54);
                float bx = 128 + Mathf.Cos(a) * r, by = 128 + Mathf.Sin(a) * r;
                c = Over(c, Shadow, Soft(Circle(x, y, bx + 6, by - 6, 22), 6f));
                float b = Circle(x, y, bx, by, 22);
                c = Over(c, cols[i % 4], Fill(b));
                c = Over(c, Color.white, Fill(Circle(x, y, bx - 7, by + 7, 6)) * 0.6f);
                c = Over(c, Outline, Stroke(b, 3f));
            }
            return c;
        }

        // The shore of a lake on a winter track (instead of sand): an icy rim at the water, then snow, fading out.
        // U runs from the water (0) outwards, like the sand texture.
        static Color SnowShorePixel(float x, float y)
        {
            float u = x / 64f;
            float n = Noise(x * 0.2f, y * 0.2f, 8);
            Color c = u < 0.22f ? Color.Lerp(Hex("BFE6F7"), Hex("E8F8FF"), u / 0.22f) : Color.Lerp(SnowShade, Snow, Mathf.InverseLerp(0.22f, 0.6f, u) * 0.8f + n * 0.2f);
            if (u > 0.2f && u < 0.26f) c = Color.Lerp(c, Color.white, 0.8f);
            c.a = Mathf.Clamp01((0.92f - u) / 0.25f);
            return c;
        }

        // ---- floating on the water (their own sprites)

        // An ice floe: a flat white slab with an icy blue rim and a few cracks (the jump pad on winter water).
        static Color IceFloePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(30, 92), new Vector2(46, 150), new Vector2(98, 176), new Vector2(150, 160), new Vector2(172, 108), new Vector2(150, 40), new Vector2(92, 18), new Vector2(44, 44) };
            float floe = Polygon(x, y, pts);
            c = Over(c, new Color(0.1f, 0.25f, 0.4f, 0.35f), Soft(floe - 4f, 8f));
            c = Over(c, Hex("8CCDE8"), Fill(floe));
            c = Over(c, Color.Lerp(Snow, SnowShade, Mathf.InverseLerp(160, 30, y) * 0.8f), Fill(floe + 9f));
            if (Mathf.Abs(Mathf.Sin(x * 0.06f + y * 0.03f) * 30f - (y - 96f)) < 1.2f) c = Over(c, Hex("8CCDE8"), Fill(floe + 14f) * 0.8f);
            c = Over(c, Color.white, Fill(Ellipse(x, y, 82, 128, 26, 12)) * 0.7f);
            c = Over(c, Hex("2C6C93"), Stroke(floe, 3f));
            return c;
        }

        // An iceberg: a jagged white mountain of ice with blue faces, the water lighter round its foot.
        static Color IcebergPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(30, 120), new Vector2(54, 196), new Vector2(110, 230), new Vector2(178, 214), new Vector2(228, 150), new Vector2(214, 72), new Vector2(150, 26), new Vector2(76, 36) };
            float berg = Polygon(x, y, pts) + (Noise(x * 0.1f, y * 0.1f, 8) - 0.5f) * 8f;
            c = Over(c, new Color(0.75f, 0.9f, 1f, 0.5f), Soft(berg - 8f, 12f));
            c = Over(c, Hex("7FC3E6"), Fill(berg));
            // Facets: lit on the upper left, blue on the lower right.
            float ang = Mathf.Atan2(y - 128, x - 128);
            int facet = (int)Mathf.Floor((ang + Mathf.PI) / (Mathf.PI / 3f));
            Color face = Color.Lerp(IceLight, Hex("A9DAF0"), (facet % 6) / 5f);
            c = Over(c, face, Fill(berg + 10f));
            c = Over(c, Color.white, Fill(Polygon(x, y, new[] { new Vector2(90, 150), new Vector2(120, 200), new Vector2(150, 160), new Vector2(124, 130) })) * 0.9f);
            c = Over(c, IceLine, Stroke(berg, 3.5f));
            return c;
        }

        // A fishing trawler from above: red hull, white wheelhouse, the gantry and nets at the stern.
        static Color TrawlerPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = Mathf.Min(RoundRect(x, y, 80, 130, 50, 104, 20), Ellipse(x, y, 80, 230, 50, 76));
            hull = Mathf.Max(hull, y - 300f);
            c = Over(c, new Color(1f, 1f, 1f, 0.35f), Soft(hull - 6f, 10f));
            c = Over(c, Hex("B8262E"), Fill(hull));
            c = Over(c, Hex("8C8273"), Fill(hull + 7f));
            float house = RoundRect(x, y, 80, 196, 30, 30, 5);
            c = Over(c, Icing, Fill(house));
            c = Over(c, Hex("33475E"), Fill(RoundRect(x, y, 80, 222, 24, 4, 2)));
            c = Over(c, Snow, Fill(RoundRect(x, y, 76, 198, 20, 20, 4)) * 0.8f);
            c = Over(c, Outline, Stroke(house, 3f));
            c = Over(c, Hex("F5C842"), Fill(Capsule(x, y, 40, 70, 120, 70, 5)));
            c = Over(c, Hex("3D7D86"), Fill(RoundRect(x, y, 80, 100, 30, 22, 8)));
            c = Over(c, Outline, Stroke(hull, 3.5f));
            return c;
        }

        // An icebreaker: a big red hull with a sharp bow, white superstructure, a funnel; ice broken round the bow.
        static Color IcebreakerPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = Mathf.Min(RoundRect(x, y, 96, 150, 64, 130, 24), Triangle(x, y, new Vector2(32, 270), new Vector2(160, 270), new Vector2(96, 370)));
            for (int i = 0; i < 8; i++)
            {
                float a = i * 0.8f;
                float shard = RotRect(x, y, 96 + Mathf.Cos(a) * 70f, 330 + Mathf.Sin(a) * 30f, 10, 7, 2, a);
                c = Over(c, IceLight, Fill(shard));
                c = Over(c, IceLine, Stroke(shard, 1.5f) * 0.6f);
            }
            c = Over(c, new Color(1f, 1f, 1f, 0.35f), Soft(hull - 6f, 12f));
            c = Over(c, CandyRed, Fill(hull));
            c = Over(c, Hex("6B7480"), Fill(hull + 8f));
            float house = RoundRect(x, y, 96, 200, 46, 44, 6);
            c = Over(c, Icing, Fill(house));
            c = Over(c, Hex("33475E"), Fill(RoundRect(x, y, 96, 238, 40, 4, 2)));
            c = Over(c, Outline, Stroke(house, 3f));
            float funnel = RoundRect(x, y, 96, 130, 20, 22, 6);
            c = Over(c, Hex("F5C842"), Fill(funnel));
            c = Over(c, Outline, Fill(RoundRect(x, y, 96, 130, 10, 12, 4)));
            c = Over(c, Outline, Stroke(funnel, 3f));
            c = Over(c, Snow, Fill(RoundRect(x, y, 96, 70, 40, 30, 8)) * 0.6f);
            c = Over(c, Outline, Stroke(hull, 4f));
            return c;
        }

        // Two seals resting on a small ice floe.
        static Color SealFloePixel(float x, float y)
        {
            Color c = IceFloePixel(x, y);
            c = SealOver(c, x, y, 76, 92, 0.6f);
            c = SealOver(c, x, y, 124, 104, -0.4f);
            return c;
        }
    }
}
