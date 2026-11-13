using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The lagoon's scenery sheet (<see cref="SomeGame.Track.LagoonSheet"/>: 16 cells of 256 px, seen from above):
    /// palms, a pair of palms, a hibiscus bush, a thatched hut, a beach umbrella with a towel, rocks, flowers,
    /// coral and seaweed under the water, a sea turtle, the lighthouse, a tiki bar, a canoe and flamingos. Also the
    /// dolphin, the wooden footbridge, the splash ring and the streaks of a current.
    /// </summary>
    public static partial class WaterArt
    {
        static readonly Color Shadow = new(0.02f, 0.18f, 0.2f, 0.32f);
        static readonly Color Frond = Hex("2E9E4F"), FrondDark = Hex("1F7A3C"), FrondLight = Hex("5CC468");

        [MenuItem("SomeGame/Generate Lagoon Scenery")]
        public static void GenerateLagoonScenery()
        {
            string f = ArtGenerator.Folder;
            Write(f, "SceneryLagoon", 1024, 1024, LagoonCell, 128, false);
            ArtGenerator.Material("SceneryLagoon", "SceneryLagoon");
            Write(f, "Dolphin", 128, 256, DolphinPixel, 64, false);
            Write(f, "LagoonBridge", 128, 512, BridgePixel, 64, false);
            Write(f, "SplashRing", 128, 128, SplashPixel, 64, false);
            Write(f, "Gull", 128, 64, GullPixel, 64, false);
            Write(f, "Sunbather", 128, 192, SunbatherPixel, 64, false);
            Write(f, "SandcastleTop", 128, 128, SandcastlePixel, 64, false);
            Write(f, "Crab", 96, 64, CrabPixel, 32, false);
            Write(f, "FishShoal", 128, 128, ShoalPixel, 64, false);
            Write(f, "LagoonStreaks", 64, 256, StreakPixel, 32, true);
            Water("LagoonCurrent", "LagoonStreaks", new Color(1f, 1f, 1f, 0.8f), new Color(0f, 0f, 0f, 0f), 1f, 0f);
            var current = AssetDatabase.LoadAssetAtPath<Material>($"{f}/LagoonCurrent.mat");
            current.SetVector("_Flow", new Vector4(0f, -0.45f, 0f, 0f));
            current.SetFloat("_Wobble", 0.004f);
            current.SetTexture("_Caustics", Texture2D.blackTexture);
            EditorUtility.SetDirty(current);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color LagoonCell(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return SomeGame.Track.LagoonSheet.Cells[row * 4 + col] switch
            {
                "Palm" => Palm(lx, ly, 128, 128, 1f, 0f),
                "PalmTall" => Palm(lx, ly, 128, 128, 1.2f, 0.4f),
                "PalmPair" => Over(Palm(lx, ly, 96, 104, 0.82f, 1.1f), Palm(lx, ly, 160, 156, 0.86f, 2.3f), Fill(PalmCrown(lx, ly, 160, 156, 0.86f, 2.3f))),
                "Bush" => Bush(lx, ly),
                "Hut" => Hut(lx, ly),
                "Umbrella" => Umbrella(lx, ly),
                "Rock" => Rocks(lx, ly),
                "Flowers" => Flowers(lx, ly),
                "Coral" => Coral(lx, ly, Hex("FF8A4C"), Hex("FFC07A")),
                "CoralPink" => Coral(lx, ly, Hex("E86AA6"), Hex("FFB0D4")),
                "Seaweed" => Seaweed(lx, ly),
                "Turtle" => Turtle(lx, ly),
                "Lighthouse" => Lighthouse(lx, ly),
                "BeachBar" => TikiBar(lx, ly),
                "Canoe" => Canoe(lx, ly),
                _ => Flamingos(lx, ly),
            };
        }

        // ------------------------------------------------------------------ land

        static float PalmCrown(float x, float y, float cx, float cy, float s, float turn)
        {
            float d = float.MaxValue;
            for (int i = 0; i < 7; i++)
            {
                float a = turn + i * Mathf.PI * 2f / 7f;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 tip = new Vector2(cx, cy) + dir * 96f * s;
                Vector2 mid = new Vector2(cx, cy) + dir * 52f * s + new Vector2(-dir.y, dir.x) * 10f * s;
                d = Mathf.Min(d, Mathf.Min(Capsule(x, y, cx, cy, mid.x, mid.y, 15f * s), Capsule(x, y, mid.x, mid.y, tip.x, tip.y, 9f * s)));
            }
            return d;
        }

        static Color Palm(float x, float y, float cx, float cy, float s, float turn)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(PalmCrown(x, y, cx + 16 * s, cy - 18 * s, s, turn) + 4f));
            // Fronds: a lighter side and a darker side of each leaf, a midrib, little notches along the edges.
            for (int i = 0; i < 7; i++)
            {
                float a = turn + i * Mathf.PI * 2f / 7f;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a)), side = new(-dir.y, dir.x);
                Vector2 o = new(cx, cy), mid = o + dir * 52f * s + side * 10f * s, tip = o + dir * 96f * s;
                float leaf = Mathf.Min(Capsule(x, y, o.x, o.y, mid.x, mid.y, 15f * s), Capsule(x, y, mid.x, mid.y, tip.x, tip.y, 9f * s));
                Vector2 p = new(x - o.x, y - o.y);
                bool light = Vector2.Dot(p, side) > 0f;
                bool notch = Mathf.Repeat(Vector2.Dot(p, dir) / (9f * s), 1f) < 0.3f && Mathf.Abs(Vector2.Dot(p, side)) > 8f * s;
                c = Over(c, light ? Frond : FrondDark, Fill(leaf) * (notch ? 0.25f : 1f));
                c = Over(c, FrondLight, Fill(Mathf.Min(Capsule(x, y, o.x, o.y, mid.x, mid.y, 2f * s), Capsule(x, y, mid.x, mid.y, tip.x, tip.y, 1.6f * s))) * Fill(leaf));
            }
            // Coconuts in the middle.
            foreach (var (dx, dy) in new[] { (-9f, 4f), (7f, 8f), (2f, -8f) })
            {
                float nut = Circle(x, y, cx + dx * s, cy + dy * s, 8f * s);
                c = Over(c, Hex("7A5230"), Fill(nut));
                c = Over(c, Hex("A07448"), Fill(Circle(x, y, cx + (dx - 2) * s, cy + (dy + 2) * s, 3f * s)));
            }
            return c;
        }

        static Color Bush(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float bush = Min(Circle(x, y, 110, 120, 52), Circle(x, y, 156, 136, 44), Circle(x, y, 132, 168, 40), Circle(x, y, 92, 160, 36));
            c = Over(c, Shadow, Fill(bush + 6f) * Fill(-Circle(x, y, 150, 110, 70) + 10f));
            float n = Noise(x / 16f, y / 16f, 64);
            c = Over(c, Color.Lerp(FrondDark, Frond, n), Fill(bush));
            c = Over(c, FrondLight, Fill(bush + 12f) * Mathf.Clamp01((n - 0.6f) * 4f));
            foreach (var (fx, fy) in new[] { (96f, 132f), (140f, 150f), (126f, 104f), (162f, 122f), (110f, 170f) })
            {
                c = Over(c, Hex("E8423A"), Fill(Circle(x, y, fx, fy, 8)));
                c = Over(c, Hex("FFD24D"), Fill(Circle(x, y, fx, fy, 2.5f)));
            }
            return c;
        }

        static Color Hut(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(RoundRect(x, y, 144, 112, 84, 78, 10) + 4f));
            // A plank deck in front, then the thatched roof with its ridge.
            float deck = RoundRect(x, y, 128, 52, 70, 26, 3);
            c = Over(c, Mathf.Repeat(x, 13f) < 1.6f ? Hex("6B4A2E") : Hex("A57A4C"), Fill(deck));
            c = Over(c, Line, Stroke(deck, 2.5f));
            float roof = RoundRect(x, y, 128, 138, 86, 78, 14);
            float straw = Noise(x / 6f, y / 24f, 64);
            Color thatch = Color.Lerp(Hex("C99A4C"), Hex("E8C27A"), straw);
            if (y > 138f) thatch = Shade(thatch, 0.88f);
            c = Over(c, thatch, Fill(roof));
            for (int i = 0; i < 6; i++) c = Over(c, Shade(thatch, 0.75f), Fill(Mathf.Abs(Mathf.Abs(y - 138f) - (12 + i * 12)) - 1.2f) * Fill(roof));
            c = Over(c, Hex("8E6A33"), Fill(Mathf.Abs(y - 138f) - 3f) * Fill(roof));
            return Over(c, Line, Stroke(roof, 3f));
        }

        static Color Umbrella(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // A towel on the sand, a lounger, the striped umbrella over them.
            float towel = RotRect(x, y, 86, 92, 26, 50, 4, 0.3f);
            c = Over(c, Mathf.Repeat((x + y) * 0.1f, 2f) < 1f ? Hex("2EC4B6") : Color.white, Fill(towel));
            float lounger = RotRect(x, y, 176, 92, 22, 54, 5, -0.2f);
            c = Over(c, Mathf.Repeat(y, 10f) < 2f ? Hex("A57A4C") : Hex("E8E2D2"), Fill(lounger));
            c = Over(c, Line, Stroke(lounger, 2f));
            c = Over(c, Shadow, Fill(Circle(x, y, 150, 128, 70)));
            float canopy = Circle(x, y, 128, 150, 70);
            float angle = Mathf.Atan2(y - 150f, x - 128f);
            bool red = Mathf.Repeat(angle / (Mathf.PI * 2f) * 8f, 2f) < 1f;
            c = Over(c, red ? Hex("FF5A4E") : Color.white, Fill(canopy));
            c = Over(c, new Color(0f, 0f, 0f, 0.08f), Fill(canopy) * Mathf.Clamp01((x - 128f) / 70f));
            c = Over(c, Line, Stroke(canopy, 2.5f));
            return Over(c, Hex("FFD24D"), Fill(Circle(x, y, 128, 150, 7)));
        }

        static Color Rocks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var rocks = new[] { (110f, 120f, 56f), (172f, 104f, 36f), (150f, 168f, 30f) };
            foreach (var (rx, ry, r) in rocks) c = Over(c, Shadow, Fill(Circle(x, y, rx + 10, ry - 10, r)));
            foreach (var (rx, ry, r) in rocks)
            {
                float rock = Circle(x, y, rx, ry, r) + Noise(x / 10f, y / 10f, 64) * 8f;
                float v = Mathf.Lerp(0.55f, 0.82f, Mathf.Clamp01((y - ry + r) / (2f * r)));
                c = Over(c, new Color(v * 0.95f, v, v * 1.02f), Fill(rock));
                c = Over(c, Line, Stroke(rock, 2.5f) * 0.6f);
            }
            return c;
        }

        static Color Flowers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float patch = Circle(x, y, 128, 128, 90) + Noise(x / 20f, y / 20f, 32) * 30f;
            c = Over(c, new Color(0.35f, 0.72f, 0.36f, 0.9f), Fill(patch));
            for (int i = 0; i < 26; i++)
            {
                float fx = 50 + Hash(i, 1) * 156, fy = 50 + Hash(i, 2) * 156;
                if (Circle(fx, fy, 128, 128, 80) > 0f) continue;
                Color petal = (i % 3) switch { 0 => Hex("FF7FAE"), 1 => Hex("FFD24D"), _ => Color.white };
                c = Over(c, petal, Fill(Circle(x, y, fx, fy, 7)));
                c = Over(c, Hex("FF8A1E"), Fill(Circle(x, y, fx, fy, 2.2f)));
            }
            return c;
        }

        // ------------------------------------------------------------------ under and on the water

        static Color Coral(float x, float y, Color main, Color tips)
        {
            Color c = new(0, 0, 0, 0);
            float d = float.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                float a = i * 0.7f + Hash(i, 5) * 0.4f;
                float len = 50f + Hash(i, 6) * 40f;
                Vector2 o = new(128 + Mathf.Cos(a * 2.1f) * 20f, 128 + Mathf.Sin(a * 2.1f) * 20f);
                Vector2 e = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len;
                d = Mathf.Min(d, Capsule(x, y, o.x, o.y, e.x, e.y, 9f));
                Vector2 b = o + new Vector2(Mathf.Cos(a + 0.6f), Mathf.Sin(a + 0.6f)) * len * 0.6f;
                d = Mathf.Min(d, Capsule(x, y, o.x, o.y, b.x, b.y, 7f));
            }
            // Under the water: soft and a little see-through.
            c = Over(c, new Color(main.r, main.g, main.b, 0.75f), Fill(d));
            c = Over(c, new Color(tips.r, tips.g, tips.b, 0.75f), Fill(d + 4f) * Mathf.Clamp01(Noise(x / 8f, y / 8f, 32) * 2f - 0.6f));
            return c;
        }

        static Color Seaweed(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 7; i++)
            {
                float bx = 60 + i * 22, wave = Mathf.Sin(y / 22f + i) * 12f;
                float strand = Mathf.Abs(x - bx - wave) - (8f - Mathf.Abs(y - 128f) / 22f);
                strand = Mathf.Max(strand, Mathf.Abs(y - 128f) - 100f + i * 6);
                c = Over(c, new Color(0.2f, 0.55f, 0.32f, 0.6f), Fill(strand));
            }
            return c;
        }

        static Color Turtle(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Swimming up the cell: flippers out, a patterned shell, the head in front.
            foreach (var (fx, fy, a) in new[] { (82f, 158f, 0.6f), (174f, 158f, -0.6f), (96f, 86f, -0.4f), (160f, 86f, 0.4f) })
                c = Over(c, Hex("6FA36B"), Fill(RotRect(x, y, fx, fy, 30, 12, 10, a)));
            float head = Circle(x, y, 128, 196, 18);
            c = Over(c, Hex("6FA36B"), Fill(head));
            c = Over(c, Line, Fill(Circle(x, y, 120, 204, 2.5f)));
            c = Over(c, Line, Fill(Circle(x, y, 136, 204, 2.5f)));
            float shell = Ellipse(x, y, 128, 128, 46, 56);
            c = Over(c, Hex("6B5A2E"), Fill(shell));
            float cell = Mathf.Abs(Mathf.Repeat(x / 22f + (Mathf.Floor(y / 26f) % 2) * 0.5f, 1f) - 0.5f) + Mathf.Abs(Mathf.Repeat(y / 26f, 1f) - 0.5f);
            c = Over(c, Hex("9C8240"), Fill(cell - 0.62f) * Fill(shell + 6f));
            return Over(c, Line, Stroke(Mathf.Min(shell, head), 2.5f));
        }

        static Color Lighthouse(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // On rocks: the round tower seen from above, red and white rings, the gallery rail, the lamp room.
            float rocks = Min(Circle(x, y, 128, 128, 96), Circle(x, y, 70, 96, 50), Circle(x, y, 190, 160, 44)) + Noise(x / 14f, y / 14f, 32) * 10f;
            c = Over(c, Hex("8C949A"), Fill(rocks));
            c = Over(c, Hex("6E767C"), Fill(rocks + 12f) * Mathf.Clamp01((x - 128f) / 80f));
            c = Over(c, Shadow, Fill(Circle(x, y, 150, 108, 66)));
            float tower = Circle(x, y, 128, 128, 64);
            float r = Mathf.Sqrt((x - 128) * (x - 128) + (y - 128) * (y - 128));
            c = Over(c, Mathf.Repeat(r, 20f) < 10f ? Hex("E8423A") : Color.white, Fill(tower));
            c = Over(c, Line, Stroke(tower, 3f));
            c = Over(c, Hex("2A2F36"), Stroke(Circle(x, y, 128, 128, 40), 3f));
            float lamp = Circle(x, y, 128, 128, 26);
            c = Over(c, Hex("FFF1A8"), Fill(lamp));
            c = Over(c, Hex("E8423A"), Fill(Circle(x, y, 128, 128, 12)));
            return Over(c, Line, Stroke(lamp, 2.5f));
        }

        static Color TikiBar(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(Circle(x, y, 146, 112, 82)));
            // Stools round the bar, then the round thatched roof (radial straw) with a little pennant.
            for (int i = 0; i < 6; i++)
            {
                float a = 0.4f + i * Mathf.PI / 3f;
                float stool = Circle(x, y, 128 + Mathf.Cos(a) * 100, 128 + Mathf.Sin(a) * 100, 11);
                c = Over(c, Hex("A57A4C"), Fill(stool));
                c = Over(c, Line, Stroke(stool, 2f));
            }
            float roof = Circle(x, y, 128, 128, 82);
            float angle = Mathf.Atan2(y - 128f, x - 128f);
            float straw = Mathf.Repeat(angle * 18f / Mathf.PI, 1f);
            c = Over(c, Color.Lerp(Hex("C99A4C"), Hex("E8C27A"), straw), Fill(roof));
            c = Over(c, Hex("8E6A33"), Fill(Circle(x, y, 128, 128, 10)));
            c = Over(c, Line, Stroke(roof, 3f));
            return Over(c, Hex("2EC4B6"), Fill(Triangle(x, y, new Vector2(128, 128), new Vector2(128, 150), new Vector2(156, 140))));
        }

        static Color Canoe(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = Ellipse(x, y, 128, 128, 30, 104);
            c = Over(c, Shadow, Fill(Ellipse(x, y, 140, 116, 30, 104)));
            c = Over(c, Hex("B9814D"), Fill(hull));
            c = Over(c, Hex("8A5A32"), Fill(Ellipse(x, y, 128, 128, 22, 92)));
            foreach (float sy in new[] { 96f, 160f }) c = Over(c, Hex("B9814D"), Fill(RoundRect(x, y, 128, sy, 22, 4, 2)));
            c = Over(c, Line, Stroke(hull, 2.5f));
            float paddle = Mathf.Min(RotRect(x, y, 150, 128, 4, 70, 2, 0.5f), RotRect(x, y, 176, 176, 9, 18, 6, 0.5f));
            return Over(c, Hex("E8C27A"), Fill(paddle));
        }

        static Color Flamingos(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (fx, fy, a) in new[] { (88f, 110f, 0.4f), (150f, 90f, -0.3f), (130f, 170f, 1.2f) })
            {
                c = Over(c, Shadow, Fill(Ellipse(x, y, fx + 8, fy - 8, 22, 14)));
                float body = RotRect(x, y, fx, fy, 22, 13, 12, a);
                c = Over(c, Hex("FF7FAE"), Fill(body));
                Vector2 neck = new Vector2(fx, fy) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 24f;
                c = Over(c, Hex("FF7FAE"), Fill(Capsule(x, y, fx, fy, neck.x, neck.y + 10, 4f)));
                float head = Circle(x, y, neck.x, neck.y + 12, 7);
                c = Over(c, Hex("FF8FB8"), Fill(head));
                c = Over(c, Line, Fill(Circle(x, y, neck.x + 6, neck.y + 14, 2.5f)));
                c = Over(c, Line, Stroke(body, 2f) * 0.7f);
            }
            return c;
        }

        // ------------------------------------------------------------------ moving pieces

        static Color DolphinPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // From above, nose up: a sleek grey-blue body, flippers, the tail flukes, a pale stripe down the back.
            float body = Mathf.Min(Ellipse(x, y, 64, 136, 26, 96), Ellipse(x, y, 64, 228, 9, 18));
            float flippers = Mathf.Min(RotRect(x, y, 40, 160, 18, 7, 6, 0.6f), RotRect(x, y, 88, 160, 18, 7, 6, -0.6f));
            float flukes = Mathf.Min(RotRect(x, y, 48, 34, 20, 8, 7, -0.4f), RotRect(x, y, 80, 34, 20, 8, 7, 0.4f));
            float all = Min(body, flippers, flukes);
            c = Over(c, Hex("5C86A8"), Fill(all));
            c = Over(c, Hex("7FA6C4"), Fill(Ellipse(x, y, 58, 140, 10, 70)));
            c = Over(c, Hex("3E6688"), Fill(RotRect(x, y, 64, 120, 4, 16, 3, 0f)));
            c = Over(c, Line, Fill(Circle(x, y, 52, 210, 2.6f)));
            c = Over(c, Line, Fill(Circle(x, y, 76, 210, 2.6f)));
            return Over(c, Line, Stroke(all, 2.5f));
        }

        static Color BridgePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Planks across (along x), a rope rail along each edge, posts every so often.
            float deck = RoundRect(x, y, 64, 256, 50, 252, 4);
            bool seam = Mathf.Repeat(y, 18f) < 2.2f;
            float grain = Noise(x / 8f, y / 18f, 16);
            c = Over(c, seam ? Hex("5E3F24") : Color.Lerp(Hex("9C6B3C"), Hex("C08A52"), grain), Fill(deck));
            c = Over(c, Line, Stroke(deck, 3f));
            foreach (float rx in new[] { 12f, 116f })
            {
                c = Over(c, Hex("E8D7AE"), Fill(Mathf.Abs(x - rx) - 3f) * Fill(Mathf.Abs(y - 256f) - 252f));
                for (int i = 0; i <= 8; i++)
                {
                    float post = RoundRect(x, y, rx, 8 + i * 62, 7, 7, 2);
                    c = Over(c, Hex("6B4A2E"), Fill(post));
                    c = Over(c, Line, Stroke(post, 2f));
                }
            }
            return c;
        }

        // A gull from above, gliding: white body, grey wings with black tips, a yellow beak.
        static Color GullPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float wings = Mathf.Min(Polygon(x, y, new[] { new Vector2(64, 36), new Vector2(8, 26), new Vector2(4, 32), new Vector2(60, 44) }),
                                    Polygon(x, y, new[] { new Vector2(64, 36), new Vector2(120, 26), new Vector2(124, 32), new Vector2(68, 44) })) - 2f;
            c = Over(c, Hex("C8D2DA"), Fill(wings));
            c = Over(c, Hex("2A2F36"), Fill(wings) * Mathf.Clamp01((Mathf.Abs(x - 64f) - 46f) / 4f));
            float body = Ellipse(x, y, 64, 34, 7, 20);
            c = Over(c, Color.white, Fill(body));
            c = Over(c, Hex("FFC93C"), Fill(Triangle(x, y, new Vector2(61, 52), new Vector2(67, 52), new Vector2(64, 60))));
            return Over(c, Line, Stroke(Mathf.Min(wings, body), 1.6f) * 0.7f);
        }

        // A tourist sunbathing on a striped towel: sunglasses, a sunhat, a cool drink.
        static Color SunbatherPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float towel = RoundRect(x, y, 64, 96, 34, 80, 6);
            c = Over(c, Shadow, Fill(towel + 2f));
            c = Over(c, Mathf.Repeat(y, 24f) < 12f ? Hex("FF4F86") : Color.white, Fill(towel));
            float legs = Mathf.Min(Capsule(x, y, 54, 30, 58, 80, 8f), Capsule(x, y, 74, 30, 70, 80, 8f));
            float body = RoundRect(x, y, 64, 104, 20, 28, 12);
            float arms = Mathf.Min(Capsule(x, y, 44, 124, 30, 96, 6f), Capsule(x, y, 84, 124, 98, 96, 6f));
            c = Over(c, Hex("F2B48C"), Fill(Mathf.Min(legs, arms)));
            c = Over(c, Hex("F2B48C"), Fill(body));
            c = Over(c, Hex("2EC4B6"), Fill(RoundRect(x, y, 64, 88, 20, 9, 5)));
            c = Over(c, Line, Stroke(Mathf.Min(Mathf.Min(legs, arms), body), 2f));
            float hat = Circle(x, y, 64, 148, 20);
            c = Over(c, Hex("F2D49B"), Fill(hat));
            c = Over(c, Hex("FF4F86"), Fill(Shell(Circle(x, y, 64, 148, 12), 2f)));
            c = Over(c, Line, Stroke(hat, 2f));
            c = Over(c, Hex("1C1F24"), Fill(Mathf.Min(Circle(x, y, 57, 136, 4.5f), Circle(x, y, 71, 136, 4.5f))));
            // A drink with a straw beside the towel.
            float cup = Circle(x, y, 112, 150, 7);
            c = Over(c, Hex("FFC93C"), Fill(cup));
            c = Over(c, Line, Stroke(cup, 1.6f));
            return Over(c, Hex("FF4F86"), Fill(Capsule(x, y, 112, 150, 120, 162, 1.4f)));
        }

        static Color SandcastlePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(Circle(x, y, 70, 58, 44)));
            float moat = Shell(Circle(x, y, 64, 64, 46), 4f);
            c = Over(c, Hex("4FC3D9"), Fill(moat));
            foreach (var (tx, ty, r) in new[] { (44f, 44f, 13f), (84f, 44f, 13f), (44f, 84f, 13f), (84f, 84f, 13f), (64f, 64f, 17f) })
            {
                float tower = Circle(x, y, tx, ty, r);
                c = Over(c, Hex("E2C58E"), Fill(tower));
                c = Over(c, Hex("F2DCAA"), Fill(Circle(x, y, tx - 3, ty + 3, r * 0.6f)));
                c = Over(c, Line, Stroke(tower, 1.8f) * 0.7f);
            }
            c = Over(c, Hex("6B4A2E"), Fill(Capsule(x, y, 64, 64, 64, 96, 1.4f)));
            return Over(c, Hex("FF4F86"), Fill(Triangle(x, y, new Vector2(64, 96), new Vector2(64, 86), new Vector2(80, 91))));
        }

        // A red crab from above, claws up.
        static Color CrabPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float legs = float.MaxValue;
            foreach (float s in new[] { -1f, 1f })
                for (int k = 0; k < 3; k++)
                    legs = Mathf.Min(legs, Capsule(x, y, 48 + s * 14, 26 + k * 7, 48 + s * 32, 18 + k * 9, 2f));
            float claws = Mathf.Min(Circle(x, y, 26, 50, 8), Circle(x, y, 70, 50, 8));
            float arms = Mathf.Min(Capsule(x, y, 38, 36, 28, 46, 2.6f), Capsule(x, y, 58, 36, 68, 46, 2.6f));
            float body = Ellipse(x, y, 48, 30, 18, 13);
            float all = Min(legs, claws, arms, body);
            c = Over(c, Hex("E5413A"), Fill(all));
            c = Over(c, Hex("FF7A5E"), Fill(Ellipse(x, y, 44, 34, 9, 6)));
            c = Over(c, Line, Stroke(all, 1.8f));
            foreach (float s in new[] { -1f, 1f })
            {
                c = Over(c, Color.white, Fill(Circle(x, y, 48 + s * 7, 44, 4)));
                c = Over(c, Line, Fill(Circle(x, y, 48 + s * 7, 45, 2f)));
            }
            return c;
        }

        // A shoal of little fish under the water: dark, soft, all swimming the same way.
        static Color ShoalPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 9; i++)
            {
                float fx = 30 + Hash(i, 2) * 68, fy = 24 + Hash(i, 7) * 80;
                float fish = Mathf.Min(Ellipse(x, y, fx, fy, 4.5f, 9f), Triangle(x, y, new Vector2(fx - 5, fy - 14), new Vector2(fx + 5, fy - 14), new Vector2(fx, fy - 6)));
                c = Over(c, new Color(0.05f, 0.25f, 0.33f, 0.45f), Fill(fish));
            }
            return c;
        }

        static Color SplashPixel(float x, float y)
        {
            float ring = Shell(Circle(x, y, 64, 64, 46), 6f) + Noise(x / 8f, y / 8f, 16) * 5f;
            Color c = White(Fill(ring) * 0.9f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 0.63f;
                c = Over(c, White(0.9f), Fill(Circle(x, y, 64 + Mathf.Cos(a) * 58, 64 + Mathf.Sin(a) * 58, 3.5f)));
            }
            return c;
        }

        // Streaks of fast water, running along (v), transparent between.
        static Color StreakPixel(float x, float y)
        {
            // A faint band of livelier water under the streaks.
            Color c = new(0.75f, 0.97f, 1f, 0.16f + 0.06f * Noise(x / 16f, y / 32f, 8));
            for (int i = 0; i < 5; i++)
            {
                float sx = 6 + i * 13 + Hash(i, 9) * 6;
                float start = Hash(i, 3) * 256f, len = 50f + Hash(i, 4) * 60f;
                float along = Mathf.Repeat(y - start, 256f);
                float streak = Mathf.Abs(x - sx) - 1.6f * Mathf.Sin(Mathf.Clamp01(along / len) * Mathf.PI);
                if (along < len) c = Over(c, White(0.85f), Fill(streak));
                // An arrowhead chevron on the longest ones.
                if (i % 2 == 0 && Mathf.Abs(along - len * 0.5f) < 10f)
                    c = Over(c, White(0.5f), Fill(Mathf.Abs(Mathf.Abs(x - sx) - (along - len * 0.5f + 10f) * 0.5f) - 1.2f));
            }
            return c;
        }
    }
}
