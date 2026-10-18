using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Lapland scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 6, Northern Lights: the snowy
    /// basics plus a log cabin, a lavvu tent, grazing reindeer, a husky sled, a woodpile, a sauna by the lake,
    /// an ice fisher at his hole, a reindeer pen, the observatory dome, stargazers at a telescope, ice
    /// crystals and stalagmites, cave rocks, the yeti, its footprints and a frozen waterfall. Also the reindeer
    /// that cross the road (their own sprite, heading +Y), a curtain of the northern lights, its badge, and
    /// the rock roof and floor of the yeti's cave. Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] AuroraCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "WinterCrowd", "SnowDrift", "Igloo",
            "Cabin", "Lavvu", "ReindeerHerd", "HuskySled",
            "Woodpile", "Sauna", "IceFishing", "ReindeerPen",
            "Observatory", "Stargazers", "IceCrystals", "Stalagmites",
            "CaveRock", "Yeti", "YetiTracks", "FrozenFall",
        };

        const int AuroraRows = 6;

        static readonly Color Fur = Hex("8A6446"), FurDark = Hex("5A3E2B"), FurLight = Hex("D9C6A8"), Antler = Hex("C9B08A");
        static readonly Color Rock = Hex("7A8597"), RockDark = Hex("4E596B"), RockLight = Hex("A3AEBF");

        /// <summary>UV rect of a Lapland scenery cell by name (row 0 at the bottom).</summary>
        public static Rect AuroraCell(string name)
        {
            int i = System.Array.IndexOf(AuroraCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)AuroraRows, 0.25f, 1f / AuroraRows);
        }

        [MenuItem("SomeGame/Generate Aurora Art")]
        public static void GenerateAurora()
        {
            Write(ArtGenerator.Folder, "SceneryAurora", 1024, 256 * AuroraRows, AuroraPixel, 128, false);
            ArtGenerator.Material("SceneryAurora", "SceneryAurora");
            Write(ArtGenerator.Folder, "Reindeer", 128, 128, ReindeerPixel, 128, false);
            Write(ArtGenerator.Folder, "Aurora", 512, 192, AuroraCurtainPixel, 64, false);
            Write(ArtGenerator.Folder, "AuroraBadge", 128, 128, AuroraBadgePixel, 128, false);
            Write(ArtGenerator.Folder, "RoofCave", 128, 128, CaveRoofPixel, 32, true);
            Write(ArtGenerator.Folder, "FloorRock", 128, 128, RockFloorPixel, 32, true);
            ArtGenerator.Material("RoofCave", "RoofCave");
            ArtGenerator.Material("FloorRock", "FloorRock");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color AuroraPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return AuroraCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Igloo" => Igloo(lx, ly),
                "Cabin" => Cabin(lx, ly),
                "Lavvu" => Lavvu(lx, ly),
                "ReindeerHerd" => ReindeerHerd(lx, ly),
                "HuskySled" => HuskySled(lx, ly),
                "Woodpile" => Woodpile(lx, ly),
                "Sauna" => Sauna(lx, ly),
                "IceFishing" => IceFishing(lx, ly),
                "ReindeerPen" => ReindeerPen(lx, ly),
                "Observatory" => Observatory(lx, ly),
                "Stargazers" => Stargazers(lx, ly),
                "IceCrystals" => IceCrystals(lx, ly),
                "Stalagmites" => Stalagmites(lx, ly),
                "CaveRock" => CaveRock(lx, ly),
                "Yeti" => Yeti(lx, ly),
                "YetiTracks" => YetiTracks(lx, ly),
                _ => FrozenFall(lx, ly),
            };
        }

        // (x, y) in the frame of something at (cx, cy) turned by deg (its +Y pointing that way), divided by scale.
        static Vector2 Local(float x, float y, float cx, float cy, float deg, float scale)
        {
            float a = -deg * Mathf.Deg2Rad, dx = x - cx, dy = y - cy;
            return new Vector2((dx * Mathf.Cos(a) - dy * Mathf.Sin(a)) / scale, (dx * Mathf.Sin(a) + dy * Mathf.Cos(a)) / scale);
        }

        // A reindeer from above in a 128 px frame centred on (64, 64), head at +Y: brown back, pale belly showing
        // at the flanks, the white rump, a dark head with a light muzzle and big branching antlers.
        static Color Reindeer(float x, float y, Color under)
        {
            Color c = under;
            float body = SMin(Ellipse(x, y, 64, 56, 19, 34), Ellipse(x, y, 64, 82, 14, 14), 8f);
            float neck = Capsule(x, y, 64, 78, 64, 94, 10);
            float head = SMin(Ellipse(x, y, 64, 100, 10, 13), Ellipse(x, y, 64, 112, 7, 8), 5f);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 70, 50, 22, 38), 8f));
            // Antlers: two curving beams with tines, drawn first so the head sits on them.
            float antlers = float.MaxValue;
            foreach (float s in new[] { -1f, 1f })
            {
                antlers = Mathf.Min(antlers, Capsule(x, y, 64 + s * 5, 100, 64 + s * 22, 104, 2.6f));
                antlers = Mathf.Min(antlers, Capsule(x, y, 64 + s * 22, 104, 64 + s * 34, 92, 2.4f));
                antlers = Mathf.Min(antlers, Capsule(x, y, 64 + s * 34, 92, 64 + s * 40, 76, 2.2f));
                antlers = Mathf.Min(antlers, Capsule(x, y, 64 + s * 22, 104, 64 + s * 28, 116, 2f));
                antlers = Mathf.Min(antlers, Capsule(x, y, 64 + s * 32, 96, 64 + s * 44, 98, 1.8f));
                antlers = Mathf.Min(antlers, Capsule(x, y, 64 + s * 38, 82, 64 + s * 48, 80, 1.8f));
            }
            c = Over(c, Outline, Fill(antlers - 1.5f) * 0.9f);
            c = Over(c, Antler, Fill(antlers));
            c = Over(c, FurLight, Fill(body));
            c = Over(c, Fur, Fill(Ellipse(x, y, 64, 58, 13, 30)) * Fill(body));
            c = Over(c, Color.white, Fill(Ellipse(x, y, 64, 26, 11, 7)) * Fill(body));
            c = Over(c, Hex("E9DCC6"), Fill(neck));
            c = Over(c, Fur, Fill(Capsule(x, y, 64, 80, 64, 92, 5)) * Fill(neck));
            c = Over(c, FurDark, Fill(head));
            c = Over(c, FurLight, Fill(Ellipse(x, y, 64, 114, 5, 5)));
            c = Over(c, Outline, Fill(Circle(x, y, 64, 118, 2.2f)));
            c = Over(c, Outline, Stroke(Min(body, head), 2.5f) * 0.8f);
            return c;
        }

        static Color ReindeerPixel(float x, float y) => Reindeer(x, y, new Color(0, 0, 0, 0));

        // Three reindeer grazing in the snow, turned every which way, a patch of moss scraped free.
        static Color ReindeerHerd(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Hex("8E9A6A"), Fill(Ellipse(x, y, 132, 120, 54, 30) + (Noise(x * 0.3f, y * 0.3f, 16) - 0.5f) * 14f) * 0.55f);
            foreach (var (cx, cy, a, s) in new[] { (84f, 150f, 30f, 1.15f), (168f, 162f, -60f, 1f), (128f, 80f, 150f, 1.1f) })
            {
                var p = Local(x, y, cx, cy, a, s);
                c = Reindeer(p.x + 64f, p.y + 64f, c);
            }
            return c;
        }

        // A Sami lavvu from above: a cone of reindeer hide with poles crossing at the smoke hole, snow on its folds.
        static Color Lavvu(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float tent = Circle(x, y, 128, 128, 86);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 88), 12f));
            float a = Mathf.Atan2(y - 128, x - 128), r = Dist(x, y, 128, 128);
            Color hide = Color.Lerp(Hex("D8C3A0"), Hex("A88B64"), r / 86f);
            if (Mathf.Repeat(a * 8f / Mathf.PI, 2f) < 1f) hide = Shade(hide, 0.92f);
            c = Over(c, hide, Fill(tent));
            // A blue-red-yellow band round the bottom, the Sami colours.
            float band = Shell(r - 72f, 5f);
            Color stripe = Mathf.Repeat(a * 12f / Mathf.PI, 3f) < 1f ? Hex("2E5BA8") : Mathf.Repeat(a * 12f / Mathf.PI, 3f) < 2f ? Hex("C8352E") : Hex("E8B830");
            c = Over(c, stripe, Fill(band) * Fill(tent));
            if (Noise(x * 0.5f, y * 0.5f, 16) > 0.62f && r < 80f && r > 30f) c = Over(c, Snow, 0.8f * Fill(tent));
            // The poles sticking out of the smoke hole, the door flap.
            for (int i = 0; i < 7; i++)
            {
                float pa = i * Mathf.PI * 2f / 7f + 0.3f;
                c = Over(c, Hex("5A3E2B"), Fill(Capsule(x, y, 128 + Mathf.Cos(pa) * 8, 128 + Mathf.Sin(pa) * 8, 128 + Mathf.Cos(pa) * 32, 128 + Mathf.Sin(pa) * 32, 3f)));
            }
            c = Over(c, Hex("2A2622"), Fill(Circle(x, y, 128, 128, 12)));
            c = Over(c, Hex("6B4A2E"), Fill(Triangle(x, y, new Vector2(110, 44), new Vector2(146, 44), new Vector2(128, 84))) * Fill(tent));
            c = Over(c, Outline, Stroke(tent, 4f) * 0.85f);
            return c;
        }

        // A husky team pulling a wooden sled: six dogs in pairs on a line, the musher at the back.
        static Color HuskySled(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Hex("33475E"), Fill(Capsule(x, y, 128, 70, 128, 222, 1.4f)));
            float sled = RoundRect(x, y, 128, 50, 22, 34, 6);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 138, 42, 24, 36, 6), 8f));
            c = Over(c, Hex("6B4A2E"), Fill(Mathf.Min(Capsule(x, y, 104, 18, 104, 88, 3), Capsule(x, y, 152, 18, 152, 88, 3))));
            c = Over(c, Hex("B5834F"), Fill(sled));
            c = Over(c, Hex("9C6E40"), Fill(sled) * (Mathf.Repeat(y, 10f) < 2f ? 1f : 0f));
            c = Over(c, Hex("C8352E"), Fill(RoundRect(x, y, 128, 58, 16, 16, 5)));
            float musher = Circle(x, y, 128, 26, 13);
            c = Over(c, Hex("2E5BA8"), Fill(musher));
            c = Over(c, Hex("F2D9B8"), Fill(Circle(x, y, 128, 30, 6)));
            c = Over(c, Outline, Stroke(Mathf.Min(sled, musher), 3f) * 0.85f);
            foreach (float row in new[] { 112f, 156f, 200f })
                foreach (float s in new[] { -1f, 1f })
                {
                    float dx = 128 + s * 15, dy = row;
                    float dog = SMin(Ellipse(x, y, dx, dy, 9, 17), Circle(x, y, dx, dy + 18, 7.5f), 4f);
                    float ears = Mathf.Min(Circle(x, y, dx - 4, dy + 23, 3f), Circle(x, y, dx + 4, dy + 23, 3f));
                    c = Over(c, Shadow, Soft(Ellipse(x, y, dx + 4, dy - 4, 10, 18), 6f));
                    c = Over(c, Hex("5E6672"), Fill(Mathf.Min(dog, ears)));
                    c = Over(c, Hex("EEF2F6"), Fill(Ellipse(x, y, dx, dy + 2, 5, 12)) * Fill(dog));
                    c = Over(c, Hex("EEF2F6"), Fill(Circle(x, y, dx, dy + 20, 4f)));
                    c = Over(c, Hex("5E6672"), Fill(Capsule(x, y, dx, dy - 16, dx + s * 6, dy - 24, 3f)));
                    c = Over(c, Outline, Stroke(Mathf.Min(dog, ears), 2.4f) * 0.75f);
                }
            return c;
        }

        // A woodpile under a snowy lean-to: the log ends showing their rings, an axe in the chopping block.
        static Color Woodpile(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float roof = RoundRect(x, y, 128, 158, 96, 44, 4);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 140, 98, 62, 6), 10f));
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 2; j++)
                {
                    float lx = 50 + i * 19.5f + j * 9f, ly = 108 - j * 18f;
                    float log = Circle(x, y, lx, ly, 10);
                    c = Over(c, Hex("C9A46A"), Fill(log));
                    c = Over(c, Hex("9C7148"), Fill(Shell(Dist(x, y, lx, ly) - 5f, 1f)) * Fill(log));
                    c = Over(c, Hex("6B4A2E"), Stroke(log, 2.5f));
                }
            c = Over(c, Hex("6B4A2E"), Fill(roof + 3f));
            c = Over(c, y > 158 ? Snow : SnowShade, Fill(roof));
            c = Over(c, Outline, Stroke(roof, 3.5f) * 0.85f);
            float block = Circle(x, y, 196, 50, 20);
            c = Over(c, Hex("B5834F"), Fill(block));
            c = Over(c, Hex("8E5C3B"), Fill(Shell(Dist(x, y, 196, 50) - 11f, 1.2f)) * Fill(block));
            c = Over(c, Hex("5A3E2B"), Fill(Capsule(x, y, 196, 50, 226, 26, 3.5f)));
            c = Over(c, Hex("A9B4C2"), Fill(RotRect(x, y, 196, 52, 10, 5, 1, 0.65f)));
            c = Over(c, Outline, Stroke(block, 3f));
            return c;
        }

        // A little wooden sauna on the shore: a snowy roof, its stovepipe, a jetty to the ice hole and a bucket.
        static Color Sauna(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hole = Ellipse(x, y, 128, 36, 28, 16);
            c = Over(c, Hex("CDEBF8"), Fill(hole + 6f));
            c = Over(c, Hex("2D6F98"), Fill(hole));
            c = Over(c, Hex("5FA5CF"), Fill(Ellipse(x, y, 120, 40, 10, 4)) * Fill(hole));
            float jetty = RoundRect(x, y, 128, 74, 14, 26, 2);
            c = Over(c, Mathf.Repeat(y, 10f) < 1.5f ? Hex("6B4A2E") : Hex("9C7148"), Fill(jetty));
            c = Over(c, Outline, Stroke(jetty, 2.5f) * 0.8f);
            float hut = RoundRect(x, y, 128, 158, 70, 56, 5);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 146, 72, 58, 6), 10f));
            c = Over(c, Hex("8E5C3B"), Fill(hut + 3f));
            c = Over(c, x > 128 ? Snow : SnowShade, Fill(hut));
            c = Over(c, Hex("B9CBDD"), Fill(RoundRect(x, y, 128, 158, 2.5f, 54, 1)) * Fill(hut));
            float pipe = Circle(x, y, 168, 186, 9);
            c = Over(c, Hex("3A3532"), Fill(pipe));
            c = Over(c, Hex("6B6F78"), Fill(Shell(Dist(x, y, 168, 186) - 6f, 1.2f)));
            // A wisp of steam from the stovepipe.
            float steam = Mathf.Min(Circle(x, y, 182, 204, 11), Mathf.Min(Circle(x, y, 196, 220, 14), Circle(x, y, 214, 236, 12)));
            c = Over(c, White(0.55f), Soft(steam, 8f));
            float bucket = Circle(x, y, 168, 92, 9);
            c = Over(c, Hex("B5834F"), Fill(bucket));
            c = Over(c, Hex("6B4A2E"), Stroke(bucket, 2.5f));
            c = Over(c, Outline, Stroke(hut, 4f) * 0.85f);
            return c;
        }

        // An ice fisher on a stool at a hole in the ice, rod out, a bucket with a fish and a little tent.
        static Color IceFishing(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Hex("CDEBF8"), Fill(Circle(x, y, 128, 120, 96)) * 0.5f);
            float hole = Circle(x, y, 152, 116, 16);
            c = Over(c, Color.white, Fill(hole + 5f));
            c = Over(c, Hex("1F5A80"), Fill(hole));
            c = Over(c, Outline, Fill(Capsule(x, y, 110, 128, 152, 116, 1.6f)));
            c = Over(c, Color.white, Fill(Circle(x, y, 152, 116, 2f)));
            float coat = Circle(x, y, 98, 132, 20);
            c = Over(c, Shadow, Soft(Circle(x, y, 108, 122, 22), 8f));
            c = Over(c, Hex("E07A2E"), Fill(coat));
            c = Over(c, Hex("C8352E"), Fill(Circle(x, y, 98, 136, 11)));
            c = Over(c, Color.white, Fill(Circle(x, y, 98, 142, 4)));
            c = Over(c, Outline, Stroke(coat, 3f) * 0.85f);
            float bucket = Circle(x, y, 76, 92, 13);
            c = Over(c, Hex("6B7788"), Fill(bucket));
            c = Over(c, Hex("9FC6DE"), Fill(Ellipse(x, y, 76, 92, 8, 3)));
            c = Over(c, Outline, Stroke(bucket, 2.5f));
            float tent = Triangle(x, y, new Vector2(150, 176), new Vector2(214, 176), new Vector2(182, 226));
            c = Over(c, Shadow, Soft(Triangle(x, y, new Vector2(160, 166), new Vector2(224, 166), new Vector2(192, 216)), 8f));
            c = Over(c, Hex("2E5BA8"), Fill(tent));
            c = Over(c, Hex("4A78C4"), Fill(Triangle(x, y, new Vector2(182, 176), new Vector2(214, 176), new Vector2(182, 226))));
            c = Over(c, Outline, Stroke(tent, 3f) * 0.85f);
            return c;
        }

        // A round reindeer pen of birch poles, two reindeer inside on the trodden snow.
        static Color ReindeerPen(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float r = Dist(x, y, 128, 128);
            c = Over(c, SnowShade, Fill(r - 104f) * 0.6f);
            c = Over(c, Shadow, Fill(Shell(r - 106f, 3f)) * 0.6f);
            c = Over(c, Hex("E8E2D6"), Fill(Shell(r - 104f, 2.5f)));
            float a = Mathf.Atan2(y - 128, x - 128);
            if (r > 98f && r < 110f && Mathf.Repeat(a * 18f / Mathf.PI, 1f) < 0.22f) c = Over(c, Hex("3A3532"), 1f);
            foreach (var (cx, cy, ang, s) in new[] { (100f, 140f, -20f, 1.05f), (160f, 112f, 120f, 1f) })
            {
                var p = Local(x, y, cx, cy, ang, s);
                c = Reindeer(p.x + 64f, p.y + 64f, c);
            }
            return c;
        }

        // The observatory: a white dome with its slit open to the sky, on a round stone base.
        static Color Observatory(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float stone = Circle(x, y, 128, 128, 104);
            c = Over(c, Shadow, Soft(Circle(x, y, 144, 112, 104), 12f));
            c = Over(c, Hex("8E96A3"), Fill(stone));
            float a = Mathf.Atan2(y - 128, x - 128), r = Dist(x, y, 128, 128);
            if (r > 86f && Mathf.Repeat(a * 14f / Mathf.PI, 1f) < 0.08f) c = Over(c, Hex("5E6672"), Fill(stone));
            float dome = Circle(x, y, 128, 128, 84);
            Color white = Color.Lerp(Hex("FFFFFF"), Hex("C6D2E0"), Mathf.Clamp01((Dist(x, y, 100, 156) - 10f) / 120f));
            c = Over(c, white, Fill(dome));
            c = Over(c, Hex("AFC0D2"), Fill(Shell(r - 54f, 1.2f)) * Fill(dome));
            float slit = Mathf.Max(RoundRect(x, y, 128, 150, 14, 60, 3), dome);
            c = Over(c, Hex("1E2A44"), Fill(slit));
            c = Over(c, Hex("6B7788"), Fill(RotRect(x, y, 128, 176, 7, 26, 3, 0f)) * Fill(dome));
            c = Over(c, Outline, Stroke(dome, 4f) * 0.85f);
            c = Over(c, Outline, Stroke(stone, 4f) * 0.7f);
            return c;
        }

        // Stargazers wrapped up warm round a telescope on its tripod, a flask and a thermos lid.
        static Color Stargazers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (lx, ly) in new[] { (110f, 116f), (146f, 116f), (128f, 84f) })
                c = Over(c, Hex("3A3532"), Fill(Capsule(x, y, 128, 128, lx, ly, 2.4f)));
            float tube = RotRect(x, y, 140, 150, 12, 44, 5, -0.6f);
            c = Over(c, Shadow, Soft(RotRect(x, y, 150, 140, 12, 44, 5, -0.6f), 8f));
            c = Over(c, Hex("E8ECF2"), Fill(tube));
            c = Over(c, Hex("2E5BA8"), Fill(RotRect(x, y, 164, 184, 14, 8, 3, -0.6f)));
            c = Over(c, Outline, Stroke(tube, 3f) * 0.85f);
            var people = new[] { (72f, 150f, Hex("C8352E")), (88f, 82f, Hex("2E8A5A")), (190f, 96f, Hex("E8B830")), (196f, 160f, Hex("7A4FB0")) };
            foreach (var (px, py, coat) in people)
            {
                float body = Circle(x, y, px, py, 18);
                c = Over(c, Shadow, Soft(Circle(x, y, px + 8, py - 8, 19), 7f));
                c = Over(c, coat, Fill(body));
                c = Over(c, Shade(coat, 0.7f), Fill(Circle(x, y, px, py + 3, 10)));
                c = Over(c, Color.white, Fill(Circle(x, y, px, py + 8, 4)));
                c = Over(c, Outline, Stroke(body, 3f) * 0.85f);
            }
            float flask = Circle(x, y, 140, 52, 8);
            c = Over(c, Hex("B0342E"), Fill(flask));
            c = Over(c, Outline, Stroke(flask, 2.4f));
            return c;
        }

        // Icicle stalagmites rising from the cave floor in a cluster, glassy blue with bright tips.
        static Color Stalagmites(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (cx, cy, r) in new[] { (92f, 150f, 34f), (156f, 168f, 26f), (150f, 96f, 40f), (84f, 82f, 22f), (202f, 120f, 20f) })
            {
                float cone = Circle(x, y, cx, cy, r);
                c = Over(c, Shadow, Soft(Circle(x, y, cx + 8, cy - 8, r), 7f));
                float t = Mathf.Clamp01(Dist(x, y, cx - r * 0.25f, cy + r * 0.25f) / r);
                c = Over(c, Color.Lerp(Color.white, Hex("6FB7DD"), t), Fill(cone));
                float a = Mathf.Atan2(y - cy, x - cx);
                if (Mathf.Repeat(a * 3f / Mathf.PI, 1f) < 0.06f) c = Over(c, Hex("BFE6F7"), Fill(cone + 2f) * 0.8f);
                c = Over(c, Hex("2C6C93"), Stroke(cone, 3f) * 0.8f);
            }
            return c;
        }

        // A big rock of the mountain round the cave, grey with soft light from the top left, a thick snow cap.
        static Color CaveRock(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float n = (Noise(x * 0.05f, y * 0.05f, 64) - 0.5f) * 26f;
            float rock = SMin(Ellipse(x, y, 116, 124, 92, 72), Circle(x, y, 176, 146, 54), 20f) + n;
            c = Over(c, Shadow, Soft(SMin(Ellipse(x, y, 132, 110, 92, 72), Circle(x, y, 192, 132, 54), 20f) + n, 14f));
            float light = Mathf.InverseLerp(230f, 40f, x * 0.4f + y * 0.6f + 40f);
            c = Over(c, Color.Lerp(RockLight, RockDark, light * 0.9f), Fill(rock));
            // Facets: a couple of darker planes across the rock.
            float facet = Mathf.Sin(x * 0.045f - y * 0.03f) + Mathf.Sin(x * 0.02f + y * 0.05f + 1f);
            if (facet > 1.1f) c = Over(c, Shade(Rock, 0.85f), Fill(rock + 4f) * 0.6f);
            float cap = rock + 22f - (Noise(x * 0.08f, y * 0.08f, 32) - 0.5f) * 30f + Mathf.Max(0f, 130f - y) * 0.6f;
            c = Over(c, Color.Lerp(Snow, SnowShade, Mathf.InverseLerp(220f, 120f, y)), Fill(cap));
            c = Over(c, SnowDeep, Stroke(cap, 2f) * Fill(rock) * 0.6f);
            c = Over(c, Outline, Stroke(rock, 4f) * 0.8f);
            return c;
        }

        // Big yeti footprints across the snow, flat (they lie under the cars).
        static Color YetiTracks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                float fx = 70 + i * 40f, fy = 60 + i * 44f + (i % 2 == 0 ? -14f : 14f);
                var p = Local(x, y, fx, fy, -40f, 1f);
                float foot = Ellipse(p.x, p.y, 0, 0, 13, 22);
                float toes = Min(Circle(p.x, p.y, -10, 26, 5), Circle(p.x, p.y, -3, 30, 5), Circle(p.x, p.y, 5, 30, 5), Circle(p.x, p.y, 12, 26, 5));
                c = Over(c, SnowDeep, Fill(Mathf.Min(foot, toes)) * 0.85f);
                c = Over(c, SnowShade, Fill(Mathf.Min(foot, toes) + 3f) * 0.6f);
            }
            return c;
        }

        // A frozen waterfall over a rocky ledge: columns of blue ice hanging down, a frozen pool at the bottom.
        static Color FrozenFall(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float ledge = RoundRect(x, y, 128, 214, 118, 30, 10) + (Noise(x * 0.3f, y * 0.3f, 16) - 0.5f) * 12f;
            float pool = Ellipse(x, y, 128, 52, 96, 34);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 140, 44, 96, 34), 10f));
            c = Over(c, Hex("BFE6F7"), Fill(pool));
            c = Over(c, Color.white, Fill(Ellipse(x, y, 100, 60, 30, 8)) * Fill(pool) * 0.7f);
            c = Over(c, Hex("2C6C93"), Stroke(pool, 3f) * 0.8f);
            float falls = Mathf.Max(RoundRect(x, y, 128, 130, 70, 70, 20), -ledge);
            float cols = Mathf.Abs(Mathf.Repeat(x - 58f, 20f) - 10f);
            c = Over(c, Color.Lerp(Hex("E8F8FF"), Hex("6FB7DD"), cols / 10f), Fill(falls));
            c = Over(c, Hex("2C6C93"), Stroke(falls, 3f) * 0.7f);
            c = Over(c, Color.Lerp(RockLight, RockDark, Mathf.InverseLerp(250, 180, y)), Fill(ledge));
            if (y > 224f && Noise(x * 0.3f, y * 0.3f, 16) > 0.45f) c = Over(c, Snow, Fill(ledge));
            c = Over(c, Outline, Stroke(ledge, 4f) * 0.85f);
            return c;
        }

        // A curtain of the northern lights: a wavy band glowing green at its foot, rays rising and fading to
        // violet, soft at the ends (drawn over the view at night).
        static Color AuroraCurtainPixel(float x, float y)
        {
            float foot = 54f + Mathf.Sin(x * 0.018f) * 22f + Mathf.Sin(x * 0.047f + 1.3f) * 9f;
            float up = y - foot;
            float rays = 0.55f + 0.45f * Noise(x * 0.22f, 0f, 64) * (0.7f + 0.3f * Mathf.Sin(x * 0.4f));
            float glow = up < 0f ? Gauss(up, 16f) * 0.7f : (0.3f + 0.7f * Mathf.Exp(-up / 26f)) * Mathf.Exp(-up / 90f) * rays * 0.8f;
            float ends = Mathf.SmoothStep(0f, 1f, Mathf.Min(x, 512f - x) / 110f);
            float top = Mathf.InverseLerp(0f, 120f, up);
            Color tint = Color.Lerp(Hex("5CFFA8"), Hex("8E6BFF"), top * top);
            return new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(glow * ends) * 0.85f);
        }

        // The badge: a night sky with a green ribbon of light over a snowy pine.
        static Color AuroraBadgePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float disc = Circle(x, y, 64, 64, 58);
            c = Over(c, Color.Lerp(Hex("0E1838"), Hex("24305E"), y / 128f), Fill(disc));
            float wave = 74f + Mathf.Sin(x * 0.07f) * 10f;
            float band = Mathf.Abs(y - wave);
            c = Over(c, Hex("5CFFA8"), Fill(disc) * Mathf.Clamp01(Gauss(band, 5f) + (y > wave ? Mathf.Exp(-(y - wave) / 14f) * 0.5f : 0f)));
            foreach (var (sx, sy) in new[] { (34f, 100f), (90f, 104f), (60f, 112f), (100f, 88f) })
                c = Over(c, Color.white, Fill(Circle(x, y, sx, sy, 1.8f)) * Fill(disc));
            float hill = Mathf.Max(Circle(x, y, 64, -40, 76), disc);
            c = Over(c, Snow, Fill(hill));
            float pine = Triangle(x, y, new Vector2(44, 30), new Vector2(84, 30), new Vector2(64, 74));
            c = Over(c, Hex("1F4A3A"), Fill(pine));
            c = Over(c, Snow, Fill(Triangle(x, y, new Vector2(56, 58), new Vector2(72, 58), new Vector2(64, 74))));
            c = Over(c, Outline, Stroke(disc, 4f));
            return c;
        }

        // The cave roof: the snowy mountain over the cave, grey rock showing through in patches and ridges
        // (seamless tile).
        static Color CaveRoofPixel(float x, float y)
        {
            float n = Noise(x * 6f / 128f, y * 6f / 128f, 6) * 0.7f + Noise(x * 16f / 128f, y * 16f / 128f, 16) * 0.3f;
            Color c = Color.Lerp(Snow, SnowShade, Noise(x * 4f / 128f, y * 4f / 128f, 4));
            float rock = Mathf.InverseLerp(0.6f, 0.68f, n);
            c = Color.Lerp(c, Color.Lerp(RockLight, Rock, Mathf.InverseLerp(0.6f, 0.85f, n)), rock);
            if (Mathf.Abs(n - 0.6f) < 0.01f) c = Color.Lerp(c, SnowDeep, 0.8f);
            return c;
        }

        // The cave floor: dark flat rock with frost in the cracks (seamless tile).
        static Color RockFloorPixel(float x, float y)
        {
            float n = Noise(x / 16f, y / 16f, 8);
            Color c = Color.Lerp(Hex("596476"), Hex("727E91"), n);
            float h = Hash((int)(x / 4), (int)(y / 4));
            if (h > 0.993f) c = Color.Lerp(c, Hex("BFE6F7"), 0.35f);
            float crack = Mathf.Abs(Noise(x / 8f, y / 8f, 16) - 0.5f);
            if (crack < 0.015f) c = Color.Lerp(c, Hex("CFE3F2"), 0.7f);
            return c;
        }
    }
}
