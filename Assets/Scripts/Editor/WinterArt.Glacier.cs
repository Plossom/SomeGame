using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The glacier scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 2, Glacier Pass: the snowy
    /// basics of the Alpine sheet plus an ice palace (towers, walls, torches), ice sculptures of a swan and a
    /// bear, gondola cabins, a yeti, a stone mountain hut, a signpost, a cliff and a crevasse. Also the ice
    /// floor and the ice-block roof of the palace hall. Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] GlacierCells =
        {
            "SnowPine", "FrostTree", "FrostBush", "SnowRock",
            "Snowman", "Igloo", "Chalet", "Lamp",
            "IceCrystals", "SnowDrift", "WinterCrowd", "FrozenPond",
            "IceTower", "IceWall", "IceSwan", "IceBear",
            "SkiLift", "Gondola", "Yeti", "MountainHut",
            "Signpost", "IceTorch", "Cliff", "Crevasse",
        };

        const int GlacierRows = 6;

        static readonly Color Ice = Hex("BFE6F7"), IceLight = Hex("E8F8FF"), IceDeep = Hex("6FB7DD"), IceLine = Hex("2C6C93");

        /// <summary>UV rect of a glacier scenery cell by name (row 0 at the bottom).</summary>
        public static Rect GlacierCell(string name)
        {
            int i = System.Array.IndexOf(GlacierCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)GlacierRows, 0.25f, 1f / GlacierRows);
        }

        [MenuItem("SomeGame/Generate Glacier Art")]
        public static void GenerateGlacier()
        {
            Write(ArtGenerator.Folder, "SceneryGlacier", 1024, 256 * GlacierRows, GlacierPixel, 128, false);
            ArtGenerator.Material("SceneryGlacier", "SceneryGlacier");
            Write(ArtGenerator.Folder, "FloorIce", 128, 128, IceFloorPixel, 32, true);
            Write(ArtGenerator.Folder, "RoofIce", 128, 128, IceRoofPixel, 32, true);
            ArtGenerator.Material("FloorIce", "FloorIce");
            ArtGenerator.Material("RoofIce", "RoofIce");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color GlacierPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return GlacierCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "Igloo" => Igloo(lx, ly),
                "Chalet" => Chalet(lx, ly),
                "Lamp" => Lamp(lx, ly),
                "IceCrystals" => IceCrystals(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "FrozenPond" => FrozenPond(lx, ly),
                "IceTower" => IceTower(lx, ly),
                "IceWall" => IceWall(lx, ly),
                "IceSwan" => IceSwan(lx, ly),
                "IceBear" => IceBear(lx, ly),
                "SkiLift" => SkiLift(lx, ly),
                "Gondola" => Gondola(lx, ly),
                "Yeti" => Yeti(lx, ly),
                "MountainHut" => MountainHut(lx, ly),
                "Signpost" => Signpost(lx, ly),
                "IceTorch" => IceTorch(lx, ly),
                "Cliff" => Cliff(lx, ly),
                _ => Crevasse(lx, ly),
            };
        }

        // Ice from above: lighter at the top left, darker at the bottom right.
        static Color IceShade(float x, float y, float cx, float cy, float r) =>
            Color.Lerp(IceLight, IceDeep, Mathf.Clamp01(0.5f + ((x - cx) - (y - cy)) / (r * 2.6f)));

        // A round ice tower seen from above: a ring of battlements round a faceted spire with a blue pennant.
        static Color IceTower(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 144, 112, 100), 12f));
            float wall = Star(x, y, 128, 128, 104, 92, 16, 0f);
            c = Over(c, IceShade(x, y, 128, 128, 104), Fill(wall));
            c = Over(c, Snow, Fill(Star(x, y, 128, 128, 104, 92, 16, 0f)) * Fill(-Circle(x, y, 128, 128, 90)) * 0.7f);
            c = Over(c, IceLine, Stroke(wall, 3.5f) * 0.9f);
            // The spire: eight facets, light on one side, a white tip.
            float spire = Circle(x, y, 128, 128, 78);
            float ang = Mathf.Atan2(y - 128, x - 128);
            int facet = (int)Mathf.Floor((ang + Mathf.PI) / (Mathf.PI / 4f));
            Color face = Color.Lerp(IceLight, IceDeep, (facet % 8) / 7f);
            c = Over(c, face, Fill(spire));
            float edge = Mathf.Abs(Mathf.Repeat(ang + Mathf.PI, Mathf.PI / 4f) - Mathf.PI / 8f);
            if (edge > Mathf.PI / 8f - 0.02f) c = Over(c, Color.white, Fill(spire) * 0.6f);
            c = Over(c, Color.white, Fill(Circle(x, y, 122, 134, 14)));
            c = Over(c, IceLine, Stroke(spire, 3f) * 0.8f);
            // The pennant on its pole, blowing to the right.
            c = Over(c, Hex("3D7DD8"), Fill(Triangle(x, y, new Vector2(128, 136), new Vector2(128, 116), new Vector2(176, 126))));
            c = Over(c, Outline, Fill(Circle(x, y, 128, 128, 5)));
            return c;
        }

        // A stretch of the palace wall from above: blocks of ice with battlements along the top, a snow cap.
        static Color IceWall(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 114, 124, 30, 6), 10f));
            float wall = RoundRect(x, y, 128, 128, 126, 26, 5);
            c = Over(c, IceShade(x, y, 128, 128, 60), Fill(wall));
            // Blocks: rows with staggered joints.
            float row = Mathf.Floor((y - 102f) / 17.3f);
            float joint = Mathf.Repeat(x + row * 18f, 36f);
            if (wall < 0f && (joint < 2f || Mathf.Repeat(y - 102f, 17.3f) < 1.6f)) c = Over(c, IceDeep, 0.6f);
            c = Over(c, Color.white, Fill(RoundRect(x, y, 128, 146, 120, 4, 2)) * 0.7f);
            // Battlements along the top edge, capped with snow.
            for (int i = 0; i < 7; i++)
            {
                float m = RoundRect(x, y, 20 + i * 36, 160, 11, 9, 3);
                c = Over(c, IceShade(x, y, 20 + i * 36, 160, 12), Fill(m));
                c = Over(c, Snow, Fill(RoundRect(x, y, 20 + i * 36, 163, 9, 5, 3)));
                c = Over(c, IceLine, Stroke(m, 2.5f) * 0.8f);
            }
            c = Over(c, IceLine, Stroke(wall, 3.5f) * 0.9f);
            return c;
        }

        // An ice sculpture of a swan on a snowy plinth: round body, raised wings, a curved neck.
        static Color IceSwan(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 96), 10f));
            float plinth = Circle(x, y, 128, 128, 96);
            c = Over(c, SnowShade, Fill(plinth));
            c = Over(c, Snow, Fill(Circle(x, y, 120, 136, 86)));
            c = Over(c, Outline, Stroke(plinth, 3.5f) * 0.6f);
            float body = Ellipse(x, y, 128, 108, 50, 64);
            float wingL = Ellipse(x, y, 92, 116, 26, 56), wingR = Ellipse(x, y, 164, 116, 26, 56);
            float neck = Mathf.Min(Capsule(x, y, 128, 160, 128, 196, 11), Capsule(x, y, 128, 196, 148, 206, 11));
            float head = Circle(x, y, 152, 204, 15);
            float all = Min(body, wingL, wingR, neck, head);
            c = Over(c, Color.Lerp(IceLight, IceDeep, Mathf.InverseLerp(220, 40, y) * 0.6f + Mathf.InverseLerp(60, 200, x) * 0.4f), Fill(all));
            // Feathers carved into the wings, glints of light.
            if (Mathf.Min(wingL, wingR) < 0f && Mathf.Repeat(y + Mathf.Abs(x - 128) * 0.5f, 14f) < 2f) c = Over(c, IceDeep, 0.6f);
            c = Over(c, Color.white, Fill(Ellipse(x, y, 116, 122, 12, 26)) * 0.8f);
            c = Over(c, Color.white, Fill(Circle(x, y, 148, 210, 4)));
            c = Over(c, Hex("F5A142"), Fill(Triangle(x, y, new Vector2(164, 208), new Vector2(164, 198), new Vector2(178, 203))) * 0.8f);
            c = Over(c, IceLine, Stroke(all, 3f));
            return c;
        }

        // An ice sculpture of a polar bear walking, on a snowy plinth.
        static Color IceBear(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 114, 100, 70, 30), 10f));
            float plinth = RoundRect(x, y, 128, 128, 100, 70, 30);
            c = Over(c, SnowShade, Fill(plinth));
            c = Over(c, Snow, Fill(RoundRect(x, y, 122, 134, 90, 60, 26)));
            c = Over(c, Outline, Stroke(plinth, 3.5f) * 0.6f);
            float body = Ellipse(x, y, 116, 128, 62, 38);
            float head = Circle(x, y, 182, 132, 24), snout = Ellipse(x, y, 204, 130, 12, 10);
            float ears = Mathf.Min(Circle(x, y, 176, 152, 8), Circle(x, y, 190, 152, 8));
            float paws = Min(Circle(x, y, 78, 98, 12), Circle(x, y, 150, 98, 12), Circle(x, y, 78, 158, 12), Circle(x, y, 150, 158, 12));
            float all = Min(body, head, snout, ears, paws);
            c = Over(c, IceShade(x, y, 128, 128, 80), Fill(all));
            c = Over(c, Color.white, Fill(Ellipse(x, y, 104, 140, 30, 12)) * 0.75f);
            c = Over(c, IceLine, Fill(Circle(x, y, 212, 131, 3.5f)));
            c = Over(c, IceLine, Fill(Circle(x, y, 188, 140, 2.5f)));
            c = Over(c, IceLine, Stroke(all, 3f));
            return c;
        }

        // A gondola cabin from above: a red roof with a white band, the hanger on its cable.
        static Color Gondola(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Outline, Fill(Mathf.Abs(y - 128f) - 2.2f) * 0.9f); // the cable
            c = Over(c, Shadow, Soft(RoundRect(x, y, 148, 98, 54, 44, 16), 10f));
            float cabin = RoundRect(x, y, 128, 128, 56, 46, 16);
            c = Over(c, Hex("B8262E"), Fill(cabin));
            c = Over(c, Hex("E2513A"), Fill(RoundRect(x, y, 122, 134, 46, 36, 12)));
            c = Over(c, Color.white, Fill(Mathf.Abs(x - 128f) - 6f) * Fill(cabin));
            // Windows along both sides.
            for (int i = 0; i < 3; i++)
                foreach (float sy in new[] { 86f, 170f })
                    c = Over(c, Hex("A8D8F0"), Fill(RoundRect(x, y, 96 + i * 32, sy, 11, 4, 2)));
            c = Over(c, Snow, Fill(RoundRect(x, y, 108, 146, 22, 10, 6)) * 0.9f);
            c = Over(c, Outline, Stroke(cabin, 4f) * 0.9f);
            c = Over(c, Hex("5B6878"), Fill(RoundRect(x, y, 128, 128, 10, 22, 4)));
            c = Over(c, Outline, Stroke(RoundRect(x, y, 128, 128, 10, 22, 4), 2.5f));
            return c;
        }

        // The yeti: a big white furry fellow waving both arms, a friendly blue-grey face looking up.
        static Color Yeti(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float fur = (Noise(x * 0.9f, y * 0.9f, 32) - 0.5f) * 8f;
            float body = Circle(x, y, 128, 112, 62) + fur;
            float armL = Capsule(x, y, 80, 120, 34, 176, 18) + fur * 0.6f, armR = Capsule(x, y, 176, 120, 222, 176, 18) + fur * 0.6f;
            float feet = Mathf.Min(Ellipse(x, y, 100, 52, 20, 14), Ellipse(x, y, 156, 52, 20, 14));
            float head = Circle(x, y, 128, 170, 40) + fur * 0.8f;
            float all = Min(body, armL, armR, feet, head);
            c = Over(c, Shadow, Soft(Mathf.Min(Circle(x, y, 142, 98, 62), Circle(x, y, 142, 156, 40)), 10f));
            c = Over(c, Color.Lerp(Snow, SnowShade, Mathf.InverseLerp(200, 40, y) * 0.7f), Fill(all));
            if (all < 0f && Noise(x * 2.1f, y * 2.1f, 16) > 0.72f) c = Over(c, SnowDeep, 0.5f);
            // Hands and feet in blue-grey, the face.
            c = Over(c, Hex("7C93AE"), Fill(Mathf.Min(Circle(x, y, 32, 180, 11), Circle(x, y, 224, 180, 11))));
            c = Over(c, Hex("7C93AE"), Fill(feet + 6f));
            float face = Ellipse(x, y, 128, 162, 26, 22);
            c = Over(c, Hex("8DA4BF"), Fill(face));
            c = Over(c, Outline, Fill(Circle(x, y, 118, 168, 4.5f)));
            c = Over(c, Outline, Fill(Circle(x, y, 138, 168, 4.5f)));
            c = Over(c, Color.white, Fill(Circle(x, y, 119.5f, 169.5f, 1.5f)));
            c = Over(c, Color.white, Fill(Circle(x, y, 139.5f, 169.5f, 1.5f)));
            float mouth = Mathf.Max(Ellipse(x, y, 128, 152, 12, 8), y - 152f);
            c = Over(c, Hex("33475E"), Fill(mouth));
            c = Over(c, Color.white, Fill(Mathf.Max(RoundRect(x, y, 124, 150, 3, 3, 1), -mouth - 0.5f)) * Fill(mouth - 0.5f));
            c = Over(c, Outline, Stroke(all, 4f) * 0.85f);
            return c;
        }

        // A stone mountain hut: grey stone walls, a heavy roof under deep snow, a red shutter, smoke rising.
        static Color MountainHut(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 106, 98, 76, 10), 12f));
            float walls = RoundRect(x, y, 128, 122, 98, 76, 10);
            Color stone = Color.Lerp(Hex("7D8794"), Hex("A3ACB8"), Noise(x * 0.35f, y * 0.35f, 16));
            c = Over(c, stone, Fill(walls));
            float roof = RoundRect(x, y, 128, 132, 86, 62, 8);
            c = Over(c, Hex("5A4636"), Fill(roof + 3f));
            c = Over(c, x < 128 ? Snow : SnowShade, Fill(roof));
            c = Over(c, SnowDeep, Fill(Mathf.Abs(x - 128f) - 2f) * Fill(roof));
            // Shutters at the front, a little bench.
            c = Over(c, Hex("B8432C"), Fill(RoundRect(x, y, 84, 52, 12, 6, 2)));
            c = Over(c, Hex("B8432C"), Fill(RoundRect(x, y, 172, 52, 12, 6, 2)));
            c = Over(c, Hex("8E5C3B"), Fill(RoundRect(x, y, 128, 50, 20, 5, 2)));
            c = Over(c, Outline, Stroke(walls, 4f) * 0.85f);
            // The chimney and its smoke.
            float chimney = RoundRect(x, y, 168, 160, 12, 12, 3);
            c = Over(c, Hex("6B7480"), Fill(chimney));
            c = Over(c, Outline, Stroke(chimney, 3f));
            for (int i = 0; i < 3; i++) c = Over(c, new Color(0.92f, 0.94f, 0.97f, 0.75f - i * 0.18f), Fill(Circle(x, y, 178 + i * 14, 182 + i * 18, 10 + i * 3)));
            return c;
        }

        // A hiking signpost: a wooden post with yellow arrow signs pointing three ways, snow on top.
        static Color Signpost(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 116, 70), 10f));
            var signs = new[] { (130f, 156f, 1f), (126f, 128f, -1f), (130f, 100f, 1f) };
            foreach (var (sx, sy, dir) in signs)
            {
                float board = RoundRect(x, y, sx + dir * 30, sy, 40, 11, 2);
                float tip = Triangle(x, y, new Vector2(sx + dir * 70, sy + 11), new Vector2(sx + dir * 70, sy - 11), new Vector2(sx + dir * 86, sy));
                float sign = Mathf.Min(board, tip);
                c = Over(c, Hex("F5C842"), Fill(sign));
                c = Over(c, Hex("33475E"), Fill(RoundRect(x, y, sx + dir * 28, sy, 26, 2, 1)));
                c = Over(c, Snow, Fill(RoundRect(x, y, sx + dir * 30, sy + 8, 38, 3, 2)));
                c = Over(c, Outline, Stroke(sign, 3f));
            }
            float post = Circle(x, y, 128, 128, 12);
            c = Over(c, Hex("8E5C3B"), Fill(post));
            c = Over(c, Snow, Fill(Circle(x, y, 126, 130, 8)));
            c = Over(c, Outline, Stroke(post, 3f));
            return c;
        }

        // An ice torch: a post of ice with a cold blue flame glowing round it.
        static Color IceTorch(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Hex("7FD3F5"), Gauss(Dist(x, y, 128, 128), 52f) * 0.45f);
            c = Over(c, Shadow, Soft(Circle(x, y, 138, 118, 30), 8f));
            float post = Star(x, y, 128, 128, 30, 24, 6, 0.3f);
            c = Over(c, IceShade(x, y, 128, 128, 30), Fill(post));
            c = Over(c, IceLine, Stroke(post, 3f));
            float flame = Star(x, y, 128, 130, 22, 10, 5, Mathf.PI * 0.5f);
            c = Over(c, Hex("4FB8F0"), Fill(flame));
            c = Over(c, Hex("D9F4FF"), Fill(Circle(x, y, 128, 130, 8)));
            return c;
        }

        // A cliff edge of grey rock in layers, snow lying on the ledges.
        static Color Cliff(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(14, 120), new Vector2(40, 186), new Vector2(104, 226), new Vector2(176, 218), new Vector2(234, 166), new Vector2(238, 92), new Vector2(190, 34), new Vector2(110, 26), new Vector2(46, 56) };
            float rock = Polygon(x, y, pts);
            var shadowPts = System.Array.ConvertAll(pts, p => p + new Vector2(14, -14));
            c = Over(c, Shadow, Soft(Polygon(x, y, shadowPts), 12f));
            float n = Noise(x * 0.25f, y * 0.25f, 16);
            c = Over(c, Color.Lerp(Hex("5E6A78"), Hex("98A3B0"), n), Fill(rock));
            // Ledges: bands of snow across the rock.
            for (int i = 0; i < 4; i++)
            {
                float band = Mathf.Abs(y - (70 + i * 44) - Mathf.Sin(x * 0.05f + i) * 10f) - 7f;
                c = Over(c, i % 2 == 0 ? Snow : SnowShade, Fill(band) * Fill(rock + 6f));
                c = Over(c, Hex("47515E"), Fill(Mathf.Abs(y - (62 + i * 44) - Mathf.Sin(x * 0.05f + i) * 10f) - 1.5f) * Fill(rock + 4f) * 0.7f);
            }
            c = Over(c, Snow, Fill(Ellipse(x, y, 118, 196, 60, 22)) * Fill(rock + 4f));
            c = Over(c, Outline, Stroke(rock, 4f) * 0.85f);
            return c;
        }

        // A crevasse (drawn flat on the snow): a deep blue crack with bright icy lips and snow overhangs.
        static Color Crevasse(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float wobble = Mathf.Sin(x * 0.07f) * 10f + Mathf.Sin(x * 0.19f + 1f) * 4f;
            float halfWidth = 54f * Mathf.Clamp01(Mathf.Min(x - 6f, 250f - x) / 50f);
            float crack = Mathf.Abs(y - 128f - wobble) - halfWidth;
            if (halfWidth <= 0f) return c;
            float depth = Mathf.Clamp01(-crack / Mathf.Max(1f, halfWidth));
            c = Over(c, Shade(SnowDeep, 0.9f), Fill(crack - 8f) * 0.8f);
            c = Over(c, Color.Lerp(Ice, Hex("10325A"), Mathf.Pow(depth, 0.7f)), Fill(crack));
            // Icy lips with icicles, light on the far side.
            c = Over(c, IceLight, Stroke(crack, 5f) * (y > 128f + wobble ? 1f : 0.6f));
            if (Mathf.Repeat(x, 13f) < 4f) c = Over(c, IceLight, Fill(crack + 8f) * Fill(-(crack + 2f)) * 0.9f);
            c = Over(c, Snow, Fill(Mathf.Abs(y - 128f - wobble) - halfWidth - 12f) * Fill(-(Mathf.Abs(y - 128f - wobble) - halfWidth - 2f)) * 0.9f);
            return c;
        }

        // The palace floor: smooth pale ice with frozen bubbles, faint cracks and glints.
        static Color IceFloorPixel(float x, float y)
        {
            float n = Noise(x * 0.06f, y * 0.06f, 8);
            Color c = Color.Lerp(Hex("D6F0FB"), Hex("A9DAF0"), n);
            float crack = Mathf.Abs(Mathf.Sin(x * 0.049f + Mathf.Sin(y * 0.098f) * 1.5f) * 18f - (y - 64f) * 0.3f);
            if (crack < 0.9f) c = Color.Lerp(c, Color.white, 0.7f);
            float h = Hash((int)(x / 4), (int)(y / 4));
            if (h > 0.985f) c = Color.Lerp(c, Color.white, 0.9f);
            if (Mathf.Repeat(x + y, 64f) < 1.2f) c = Color.Lerp(c, Hex("8CC8E4"), 0.35f);
            return c;
        }

        // The palace roof: rows of ice blocks, each lit at its top edge.
        static Color IceRoofPixel(float x, float y)
        {
            float row = Mathf.Floor(y / 32f);
            float bx = Mathf.Repeat(x + (row % 2) * 16f, 32f), by = Mathf.Repeat(y, 32f);
            Color c = Color.Lerp(Hex("9FD4EE"), Hex("CDEBF8"), by / 32f);
            if (by > 26f) c = Color.Lerp(c, Color.white, 0.6f);
            if (bx < 2f || by < 2f) c = Hex("5E9FC6");
            if (Hash((int)(x / 32 + row * 7), (int)row) > 0.7f && by > 8f && by < 24f && bx > 6f && bx < 14f) c = Color.Lerp(c, Color.white, 0.5f);
            return c;
        }
    }
}
