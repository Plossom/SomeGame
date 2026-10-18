using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The winter games scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 4, Winter Games: snowy
    /// basics plus the games sign, flags, the medal podium, grandstands, biathlon targets, the judges' tower
    /// at the ski jump, hockey goals, the curling house, a scoreboard, a bobsleigh, skiers, snowboarders,
    /// a TV camera crane, skaters, advertising boards and the distance lines of the ski jump's landing hill.
    /// Also the curling stone that slides across the arena. Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] GamesCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "WinterCrowd", "SnowDrift", "Chalet",
            "GamesSign", "Flags", "Podium", "Grandstand",
            "BiathlonTarget", "JudgeTower", "HockeyGoal", "CurlingTarget",
            "Scoreboard", "Bobsled", "Skiers", "Snowboarder",
            "CameraCrane", "Skaters", "AdBoard", "JumpLines",
        };

        const int GamesRows = 6;

        static readonly Color[] RingColors = { Hex("3D7DD8"), Hex("22262E"), Hex("E2393F"), Hex("F5C842"), Hex("2E9A57") };

        /// <summary>UV rect of a winter games scenery cell by name (row 0 at the bottom).</summary>
        public static Rect GamesCell(string name)
        {
            int i = System.Array.IndexOf(GamesCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)GamesRows, 0.25f, 1f / GamesRows);
        }

        [MenuItem("SomeGame/Generate Winter Games Art")]
        public static void GenerateGames()
        {
            Write(ArtGenerator.Folder, "SceneryGames", 1024, 256 * GamesRows, GamesPixel, 128, false);
            ArtGenerator.Material("SceneryGames", "SceneryGames");
            Write(ArtGenerator.Folder, "CurlingStone", 128, 128, CurlingStonePixel, 64, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color GamesPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return GamesCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Chalet" => Chalet(lx, ly),
                "GamesSign" => GamesSign(lx, ly),
                "Flags" => Flags(lx, ly),
                "Podium" => Podium(lx, ly),
                "Grandstand" => Grandstand(lx, ly),
                "BiathlonTarget" => BiathlonTarget(lx, ly),
                "JudgeTower" => JudgeTower(lx, ly),
                "HockeyGoal" => HockeyGoal(lx, ly),
                "CurlingTarget" => CurlingTarget(lx, ly),
                "Scoreboard" => Scoreboard(lx, ly),
                "Bobsled" => Bobsled(lx, ly),
                "Skiers" => Skiers(lx, ly),
                "Snowboarder" => Snowboarder(lx, ly),
                "CameraCrane" => CameraCrane(lx, ly),
                "Skaters" => Skaters(lx, ly),
                "AdBoard" => AdBoard(lx, ly),
                _ => JumpLines(lx, ly),
            };
        }

        // The games sign: a white board with a big blue snowflake and coloured stripes beneath.
        static Color GamesSign(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 114, 116, 58, 10), 10f));
            float board = RoundRect(x, y, 128, 128, 116, 58, 10);
            c = Over(c, Icing, Fill(board));
            c = Over(c, Outline, Stroke(board, 4f));
            float flake = 99f;
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector2 d = new(Mathf.Cos(a) * 36f, Mathf.Sin(a) * 36f);
                flake = Mathf.Min(flake, Capsule(x, y, 128 - d.x, 140 - d.y, 128 + d.x, 140 + d.y, 5));
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 tip = new Vector2(128, 140) + d * s * 0.62f;
                    Vector2 n = new Vector2(-d.y, d.x) * 0.3f;
                    flake = Mathf.Min(flake, Capsule(x, y, tip.x, tip.y, tip.x + d.x * s * 0.25f + n.x, tip.y + d.y * s * 0.25f + n.y, 3.5f));
                    flake = Mathf.Min(flake, Capsule(x, y, tip.x, tip.y, tip.x + d.x * s * 0.25f - n.x, tip.y + d.y * s * 0.25f - n.y, 3.5f));
                }
            }
            c = Over(c, Hex("3D7DD8"), Fill(flake));
            for (int i = 0; i < 5; i++) c = Over(c, RingColors[i], Fill(RoundRect(x, y, 48 + i * 40, 88, 16, 5, 2.5f)));
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 182, 110, 4, 3)));
            return c;
        }

        // A row of flags on poles, seen from above: each pole a dot, the flag blowing to the right.
        static Color Flags(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[][] flags =
            {
                new[] { Hex("E2393F"), Color.white, Hex("E2393F") }, new[] { Hex("3D7DD8"), Hex("F5C842"), Hex("3D7DD8") },
                new[] { Color.white, Hex("E2393F"), Color.white }, new[] { Hex("2E9A57"), Color.white, Hex("E2393F") },
                new[] { Hex("22262E"), Hex("E2393F"), Hex("F5C842") },
            };
            for (int i = 0; i < 5; i++)
            {
                float px = 28 + i * 50, py = 70 + (i % 2) * 90;
                float wave = Mathf.Sin((x - px) * 0.12f) * 4f;
                float flag = RoundRect(x, y - wave, px + 26, py, 24, 16, 2);
                c = Over(c, Shadow, Soft(RoundRect(x, y - wave, px + 34, py - 8, 24, 16, 2), 6f));
                int band = Mathf.Clamp((int)((x - px - 2) / 16f), 0, 2);
                c = Over(c, flags[i][band], Fill(flag));
                c = Over(c, Outline, Stroke(flag, 2.5f) * 0.8f);
                float pole = Circle(x, y, px, py, 6);
                c = Over(c, Hex("B9C3CF"), Fill(pole));
                c = Over(c, Outline, Stroke(pole, 2f));
            }
            return c;
        }

        // The medal podium: three steps (gold in the middle), numbers on the fronts.
        static Color Podium(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 104, 110, 52, 6), 10f));
            var steps = new[] { (128f, 132f, 40f, 46f, Hex("F5C842"), 1), (60f, 116f, 32f, 30f, Hex("C9D1DA"), 2), (196f, 108f, 32f, 22f, Hex("D08B4E"), 3) };
            foreach (var (sx, sy, hw, hh, col, n) in steps)
            {
                float step = RoundRect(x, y, sx, sy, hw, hh, 5);
                c = Over(c, Icing, Fill(step));
                c = Over(c, col, Fill(RoundRect(x, y, sx, sy + hh - 10, hw - 4, 6, 3)));
                c = Over(c, Outline, Stroke(step, 3.5f));
                // The number as simple bars.
                for (int k = 0; k < n; k++) c = Over(c, Hex("33475E"), Fill(RoundRect(x, y, sx - (n - 1) * 6 + k * 12, sy - 6, 3, 12, 1.5f)));
            }
            return c;
        }

        // A grandstand: rows of seats full of spectators in bright coats, under a little roof at the back.
        static Color Grandstand(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 120, 76, 6), 12f));
            float stand = RoundRect(x, y, 128, 128, 120, 76, 6);
            c = Over(c, Hex("8A97A8"), Fill(stand));
            Color[] coats = { Hex("E2393F"), Hex("3D7DD8"), Hex("F5C842"), Hex("2BB3A3"), Hex("E86FB0"), Hex("FF7A1A"), Color.white };
            for (int row = 0; row < 5; row++)
            {
                float ry = 66 + row * 22;
                c = Over(c, Hex("6B7788"), Fill(Mathf.Abs(y - ry + 9f) - 1.5f) * Fill(stand));
                for (int i = 0; i < 14; i++)
                {
                    float px = 22 + i * 16 + (row % 2) * 8;
                    float h = Hash(i, row);
                    if (h < 0.15f) continue;
                    c = Over(c, coats[(int)(h * 70) % coats.Length], Fill(Circle(x, y, px, ry, 6.5f)));
                    c = Over(c, Hex("F2C6A0"), Fill(Circle(x, y, px, ry + 4, 3.5f)));
                }
            }
            float roof = RoundRect(x, y, 128, 192, 120, 12, 4);
            c = Over(c, Hex("3D7DD8"), Fill(roof));
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 196, 116, 6, 3)));
            c = Over(c, Outline, Stroke(stand, 4f) * 0.85f);
            return c;
        }

        // A biathlon target board: five black targets on a white board, some already hit (white).
        static Color BiathlonTarget(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 114, 34, 6), 8f));
            float board = RoundRect(x, y, 128, 128, 114, 34, 6);
            c = Over(c, Icing, Fill(board));
            c = Over(c, Outline, Stroke(board, 3.5f));
            for (int i = 0; i < 5; i++)
            {
                float tx = 40 + i * 44;
                float t = Circle(x, y, tx, 128, 17);
                c = Over(c, i % 2 == 0 ? Hex("22262E") : Color.white, Fill(t));
                c = Over(c, Outline, Stroke(t, 2.5f));
            }
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 158, 108, 4, 2)));
            return c;
        }

        // The judges' tower beside the ski jump: a square glass cabin on stilts, a flat snowy roof.
        static Color JudgeTower(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 154, 102, 70, 70, 6), 14f));
            float tower = RoundRect(x, y, 128, 128, 70, 70, 6);
            c = Over(c, Hex("A8D8F0"), Fill(tower));
            c = Over(c, Hex("5B6878"), Fill(RoundRect(x, y, 128, 128, 58, 58, 4)));
            float roof = RoundRect(x, y, 128, 128, 54, 54, 4);
            c = Over(c, x < 128 ? Snow : SnowShade, Fill(roof));
            c = Over(c, Hex("E2393F"), Fill(RoundRect(x, y, 128, 128, 18, 18, 3)));
            c = Over(c, Outline, Stroke(tower, 4f));
            return c;
        }

        // An ice hockey goal: the red frame, a white net.
        static Color HockeyGoal(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 138, 118, 70, 42, 18), 8f));
            float net = RoundRect(x, y, 128, 128, 66, 40, 18);
            c = Over(c, new Color(1f, 1f, 1f, 0.8f), Fill(net));
            if (net < 0f && (Mathf.Repeat(x, 10f) < 1.4f || Mathf.Repeat(y, 10f) < 1.4f)) c = Over(c, Hex("9AA6B4"), 0.9f);
            float frame = Mathf.Max(Stroke(net, 7f), 0f);
            c = Over(c, CandyRed, frame * (y < 160f ? 1f : 0.85f));
            c = Over(c, Outline, Stroke(net, 2f) * 0.7f);
            return c;
        }

        // The curling house (drawn flat): blue and red rings on the ice round a white button.
        static Color CurlingTarget(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float r = Dist(x, y, 128, 128);
            if (r > 120f) return c;
            Color col = r > 92f ? Hex("3D7DD8") : r > 62f ? Color.white : r > 32f ? Hex("E2393F") : Color.white;
            c = Over(c, col, Mathf.Clamp01(120.5f - r));
            c = Over(c, Hex("22262E"), Fill(Mathf.Min(Mathf.Abs(x - 128f), Mathf.Abs(y - 128f)) - 1.2f) * 0.5f * Mathf.Clamp01(120.5f - r));
            c = Over(c, Hex("33475E"), Stroke(r - 120f, 2f) * 0.6f);
            return c;
        }

        // A big scoreboard on legs: dark screen with orange numbers and a little podium icon.
        static Color Scoreboard(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 112, 52, 6), 10f));
            float frame = RoundRect(x, y, 128, 128, 112, 52, 6);
            c = Over(c, Hex("5B6878"), Fill(frame));
            float screen = RoundRect(x, y, 128, 128, 100, 40, 4);
            c = Over(c, Hex("16202C"), Fill(screen));
            // Rows of "digits": little orange and white bars.
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 10; i++)
                {
                    float bx = 44 + i * 18, by = 104 + row * 24;
                    if (Hash(i, row + 9) < 0.25f) continue;
                    c = Over(c, i < 3 ? Hex("FF9F1A") : Color.white, Fill(RoundRect(x, y, bx, by, 6, 7, 1.5f)) * 0.9f);
                }
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 178, 108, 4, 3)));
            c = Over(c, Outline, Stroke(frame, 4f));
            return c;
        }

        // A bobsleigh parked at the start, red with a white number on the nose.
        static Color Bobsled(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 140, 116, 40, 104), 10f));
            foreach (float rx in new[] { 96f, 160f }) c = Over(c, Hex("B9C3CF"), Fill(Capsule(x, y, rx, 32, rx, 222, 4)));
            float body = Mathf.Min(Ellipse(x, y, 128, 120, 36, 96), Circle(x, y, 128, 200, 30));
            c = Over(c, Color.Lerp(Hex("F05A4F"), Hex("B8262E"), Mathf.InverseLerp(100, 160, x)), Fill(body));
            c = Over(c, Hex("22262E"), Fill(RoundRect(x, y, 128, 110, 22, 56, 10)));
            for (int i = 0; i < 3; i++) c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 128, 80 + i * 26, 9)));
            c = Over(c, Color.white, Fill(Circle(x, y, 128, 202, 12)));
            c = Over(c, Outline, Stroke(body, 3.5f));
            return c;
        }

        // Skiers in a line with poles, bright suits.
        static Color Skiers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] suits = { Hex("E2393F"), Hex("3D7DD8"), Hex("F5C842"), Hex("2BB3A3") };
            for (int i = 0; i < 4; i++)
            {
                float sx = 54 + i * 50, sy = 90 + (i % 2) * 70;
                c = Over(c, Shadow, Soft(RoundRect(x, y, sx + 6, sy - 6, 22, 30, 8), 6f));
                foreach (float k in new[] { -8f, 8f }) c = Over(c, Hex("33475E"), Fill(Capsule(x, y, sx + k, sy - 46, sx + k, sy + 40, 2.5f)));
                foreach (float k in new[] { -20f, 20f }) c = Over(c, Hex("9AA6B4"), Fill(Capsule(x, y, sx + k, sy - 20, sx + k * 1.2f, sy + 26, 1.5f)));
                float body = Ellipse(x, y, sx, sy, 15, 20);
                c = Over(c, suits[i], Fill(body));
                c = Over(c, Hex("F2C6A0"), Fill(Circle(x, y, sx, sy + 14, 8)));
                c = Over(c, suits[(i + 2) % 4], Fill(Circle(x, y, sx, sy + 17, 7)));
                c = Over(c, Outline, Stroke(body, 2.5f) * 0.8f);
            }
            return c;
        }

        // A snowboarder mid-trick, board at an angle, arms out.
        static Color Snowboarder(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RotRect(x, y, 142, 112, 90, 22, 20, 0.5f), 10f));
            float board = RotRect(x, y, 128, 128, 90, 22, 20, 0.5f);
            c = Over(c, Hex("8A5CD6"), Fill(board));
            c = Over(c, Hex("F5C842"), Fill(RotRect(x, y, 128, 128, 70, 4, 2, 0.5f)));
            c = Over(c, Outline, Stroke(board, 3.5f));
            float arms = Capsule(x, y, 80, 160, 176, 96, 9);
            c = Over(c, Hex("FF7A1A"), Fill(arms));
            float body = Circle(x, y, 128, 128, 24);
            c = Over(c, Hex("FF7A1A"), Fill(body));
            c = Over(c, Hex("22262E"), Fill(Circle(x, y, 128, 134, 15)));
            c = Over(c, Hex("7FD3F5"), Fill(Ellipse(x, y, 128, 142, 12, 5)));
            c = Over(c, Outline, Stroke(Mathf.Min(arms, body), 3f));
            return c;
        }

        // A TV camera on a crane arm reaching out over the course.
        static Color CameraCrane(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Capsule(x, y, 62, 72, 206, 186, 10), 10f));
            float arm = Capsule(x, y, 56, 84, 196, 196, 8);
            c = Over(c, Hex("F5C842"), Fill(arm));
            if (arm < 0f && Mathf.Repeat((x + y) * 0.7f, 22f) < 3f) c = Over(c, Hex("22262E"), 0.8f);
            c = Over(c, Outline, Stroke(arm, 3f));
            float foot = RoundRect(x, y, 56, 76, 30, 30, 6);
            c = Over(c, Hex("5B6878"), Fill(foot));
            c = Over(c, Outline, Stroke(foot, 3f));
            float cam = RotRect(x, y, 202, 200, 22, 14, 4, 0.67f);
            c = Over(c, Hex("22262E"), Fill(cam));
            c = Over(c, Hex("7FD3F5"), Fill(Circle(x, y, 218, 214, 6)));
            c = Over(c, CandyRed, Fill(Circle(x, y, 194, 192, 3)));
            return c;
        }

        // Ice skaters gliding in a curve, scarves flying.
        static Color Skaters(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] suits = { Hex("E86FB0"), Hex("3D7DD8"), Hex("2BB3A3") };
            for (int i = 0; i < 3; i++)
            {
                float a = 0.9f + i * 0.7f;
                float sx = 128 + Mathf.Cos(a) * 80f, sy = 100 + Mathf.Sin(a) * 70f;
                c = Over(c, Hex("CFE9F7"), Fill(Mathf.Abs(Dist(x, y, 128, 100) - 80f) - 1.5f) * 0.6f * (y > 60 ? 1f : 0f));
                c = Over(c, Shadow, Soft(Circle(x, y, sx + 6, sy - 6, 18), 6f));
                float body = Circle(x, y, sx, sy, 16);
                c = Over(c, suits[i], Fill(body));
                c = Over(c, CandyRed, Fill(Capsule(x, y, sx, sy, sx + 22, sy - 10, 3.5f)));
                c = Over(c, Hex("F2C6A0"), Fill(Circle(x, y, sx, sy + 6, 7)));
                c = Over(c, Hex("6B4A2E"), Fill(Circle(x, y, sx, sy + 9, 6)));
                c = Over(c, Outline, Stroke(body, 2.5f) * 0.8f);
            }
            return c;
        }

        // An advertising board along the course: a long white A-frame with coloured logos.
        static Color AdBoard(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 134, 120, 122, 16, 4), 8f));
            float board = RoundRect(x, y, 128, 128, 122, 16, 4);
            c = Over(c, Icing, Fill(board));
            Color[] logos = { Hex("E2393F"), Hex("3D7DD8"), Hex("F5C842"), Hex("2E9A57") };
            for (int i = 0; i < 4; i++)
            {
                float lx = 30 + i * 66;
                c = Over(c, logos[i], Fill(RoundRect(x, y, lx, 128, 24, 9, 4)));
                c = Over(c, Color.white, Fill(RoundRect(x, y, lx, 128, 14, 2.5f, 1)));
            }
            c = Over(c, Outline, Stroke(board, 3f));
            return c;
        }

        // The landing hill of the ski jump (drawn flat): snow with coloured distance lines and fir branches.
        static Color JumpLines(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hill = RoundRect(x, y, 128, 128, 124, 124, 20);
            c = Over(c, new Color(0.93f, 0.96f, 1f, 0.9f), Fill(hill));
            for (int i = 0; i < 6; i++)
            {
                float lx = 26 + i * 40;
                Color col = i == 4 ? Hex("E2393F") : i == 5 ? Hex("2E9A57") : Hex("3D7DD8");
                c = Over(c, col, Fill(Mathf.Abs(x - lx) - 2.5f) * Fill(hill + 6f) * 0.85f);
            }
            // Fir branches along both edges.
            foreach (float ey in new[] { 16f, 240f })
                if (Mathf.Repeat(x, 16f) < 10f) c = Over(c, Hex("2E6A4A"), Fill(Mathf.Abs(y - ey) - 4f - Mathf.Abs(Mathf.Repeat(x, 16f) - 5f) * 0.4f) * Fill(hill));
            return c;
        }

        // The curling stone (its own sprite): grey granite, a red handle on top.
        static Color CurlingStonePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 70, 58, 50), 8f));
            float stone = Circle(x, y, 64, 64, 52);
            Color granite = Color.Lerp(Hex("8A939E"), Hex("B7BEC6"), Noise(x * 0.3f, y * 0.3f, 16));
            if (Hash((int)x / 2, (int)y / 2) > 0.9f) granite = Shade(granite, 0.8f);
            c = Over(c, granite, Fill(stone));
            c = Over(c, Hex("6B7480"), Stroke(Circle(x, y, 64, 64, 40), 3f));
            float handle = Mathf.Min(RoundRect(x, y, 64, 64, 10, 10, 4), Capsule(x, y, 64, 64, 64, 96, 6));
            c = Over(c, CandyRed, Fill(handle));
            c = Over(c, Outline, Stroke(handle, 2.5f));
            c = Over(c, Outline, Stroke(stone, 3.5f));
            return c;
        }
    }
}
