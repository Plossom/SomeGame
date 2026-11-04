using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The high-mountain scenery atlas (16 cells of 256 px, 4 x 4) for Winter Cup race 7, Glacier Spine: blue seracs of
    /// the icefall, moraine rubble, the summit cross and a cairn with prayer flags, the base camp's tent, a roped team of
    /// climbers, the ski-flying hill's judges' tower and stands, an ice axe on a pack - and the snowy basics. Row 0 at the
    /// bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] RidgeCells =
        {
            "SnowPine", "SnowRock", "FrostBush", "SnowDrift",
            "Serac", "SeracSmall", "Moraine", "Crevasse",
            "SummitCross", "Cairn", "PrayerFlags", "BaseTent",
            "RopeTeam", "JudgesTower", "SkiStands", "IceAxePack",
        };

        const int RidgeRows = 4;

        /// <summary>UV rect of a ridge scenery cell by name (row 0 at the bottom).</summary>
        public static Rect RidgeCell(string name)
        {
            int i = System.Array.IndexOf(RidgeCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)RidgeRows, 0.25f, 1f / RidgeRows);
        }

        [MenuItem("SomeGame/Generate Ridge Art")]
        public static void GenerateRidge()
        {
            Write(ArtGenerator.Folder, "SceneryRidge", 1024, 256 * RidgeRows, RidgePixel, 128, false);
            ArtGenerator.Material("SceneryRidge", "SceneryRidge");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static readonly Color SeracBlue = Hex("7CC4E6"), SeracDeep = Hex("3E8DBE"), SeracLight = Hex("D9F2FF"), SeracLine = Hex("1F5A80"),
            Stone = Hex("8B93A0"), StoneDark = Hex("5D6573"), StoneLight = Hex("B6BDC8"), Wood = Hex("8A5A32"), WoodDark = Hex("5B3A1E"),
            Iron = Hex("3A3F48"), TentOrange = Hex("F07A22"), TentDark = Hex("B9551A"), Rope = Hex("E8C13A");

        static Color RidgePixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return RidgeCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "Serac" => Serac(lx, ly, 0),
                "SeracSmall" => SeracCluster(lx, ly),
                "Moraine" => Moraine(lx, ly),
                "Crevasse" => Crevasse(lx, ly),
                "SummitCross" => RidgeSummitCross(lx, ly),
                "Cairn" => Cairn(lx, ly),
                "PrayerFlags" => PrayerFlags(lx, ly),
                "BaseTent" => BaseTent(lx, ly),
                "RopeTeam" => RopeTeam(lx, ly),
                "JudgesTower" => JudgesTower(lx, ly),
                "SkiStands" => SkiStands(lx, ly),
                "IceAxePack" => IceAxePack(lx, ly),
                _ => new Color(0, 0, 0, 0),
            };
        }

        static Vector2[] Shift(Vector2[] pts, Vector2 by) => System.Array.ConvertAll(pts, p => p + by);

        // A serac: a tower of blue glacier ice seen from above - sharp facets, lit from the top left, a white cap of snow,
        // dark cracks, a long cold shadow.
        static Color Serac(float x, float y, int seed)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(40, 132), new Vector2(70, 206), new Vector2(138, 230), new Vector2(204, 196), new Vector2(222, 120), new Vector2(178, 44), new Vector2(98, 34), new Vector2(46, 76) };
            c = Over(c, Shadow, Soft(Polygon(x, y, Shift(pts, new Vector2(22, -22))), 10f));
            float body = Polygon(x, y, pts);
            c = Over(c, SeracBlue, Fill(body));
            // facets: a lit top-left face, a deep bottom-right face
            c = Over(c, SeracLight, Fill(Mathf.Max(body, Polygon(x, y, new[] { new Vector2(40, 132), new Vector2(70, 206), new Vector2(138, 230), new Vector2(128, 132) }))) * 0.75f);
            c = Over(c, SeracDeep, Fill(Mathf.Max(body, Polygon(x, y, new[] { new Vector2(222, 120), new Vector2(178, 44), new Vector2(98, 34), new Vector2(128, 132) }))) * 0.8f);
            // snow lying on top
            c = Over(c, Snow, Fill(Mathf.Max(body + 10f, Ellipse(x, y, 112, 170, 70, 44))));
            c = Over(c, SnowShade, Stroke(Mathf.Max(body + 10f, Ellipse(x, y, 112, 170, 70, 44)), 3f) * 0.8f);
            // cracks
            foreach (var (a, b) in new[] { (new Vector2(128, 132), new Vector2(196, 74)), (new Vector2(128, 132), new Vector2(206, 170)), (new Vector2(150, 110), new Vector2(120, 52)) })
                c = Over(c, SeracLine, Fill(SegDist(new Vector2(x, y), a, b) - 2.2f) * Fill(body) * 0.8f);
            c = Over(c, SeracLine, Stroke(body, 4.5f));
            return c;
        }

        // Three smaller blocks of ice tumbled together.
        static Color SeracCluster(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var blocks = new[]
            {
                new[] { new Vector2(30, 120), new Vector2(54, 176), new Vector2(110, 182), new Vector2(124, 128), new Vector2(92, 86), new Vector2(42, 90) },
                new[] { new Vector2(120, 170), new Vector2(150, 226), new Vector2(214, 214), new Vector2(226, 160), new Vector2(178, 128) },
                new[] { new Vector2(118, 40), new Vector2(110, 100), new Vector2(168, 124), new Vector2(222, 92), new Vector2(200, 36) },
            };
            foreach (var b in blocks) c = Over(c, Shadow, Soft(Polygon(x, y, Shift(b, new Vector2(16, -16))), 8f));
            foreach (var b in blocks)
            {
                float d = Polygon(x, y, b);
                Vector2 centre = Vector2.zero;
                foreach (var p in b) centre += p / b.Length;
                c = Over(c, Color.Lerp(SeracLight, SeracDeep, Mathf.Clamp01(0.5f + ((x - centre.x) - (y - centre.y)) / 120f)), Fill(d));
                c = Over(c, Snow, Fill(Mathf.Max(d + 7f, Circle(x, y, centre.x - 10, centre.y + 12, 28))));
                c = Over(c, SeracLine, Stroke(d, 4f));
            }
            return c;
        }

        // Moraine: grey rubble the glacier has pushed along, dusted with snow.
        static Color Moraine(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 9; i++)
            {
                float cx = 40 + Hash(i, 3) * 176, cy = 40 + Hash(i, 11) * 176, r = 18 + Hash(i, 5) * 26;
                c = Over(c, Shadow, Soft(Circle(x, y, cx + 9, cy - 9, r), 6f));
            }
            for (int i = 0; i < 9; i++)
            {
                float cx = 40 + Hash(i, 3) * 176, cy = 40 + Hash(i, 11) * 176, r = 18 + Hash(i, 5) * 26;
                float d = Star(x, y, cx, cy, r, r * 0.82f, 6, Hash(i, 7) * 6f);
                c = Over(c, Color.Lerp(StoneLight, StoneDark, Mathf.Clamp01(0.5f + ((x - cx) - (y - cy)) / (r * 2.4f))), Fill(d));
                c = Over(c, Snow, Fill(Mathf.Max(d + 5f, Circle(x, y, cx - r * 0.35f, cy + r * 0.4f, r * 0.55f))) * 0.9f);
                c = Over(c, Outline, Stroke(d, 3.5f) * 0.8f);
            }
            return c;
        }

        // The summit cross: a tall iron cross on a heap of stones, its long shadow down the slope.
        static Color RidgeSummitCross(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Mathf.Min(RotRect(x, y, 158, 100, 12, 82, 4, -45f), RotRect(x, y, 150, 108, 46, 10, 4, -45f)), 6f));
            c = Over(c, Shadow, Soft(Circle(x, y, 136, 116, 44), 8f));
            c = Over(c, StoneDark, Fill(Circle(x, y, 128, 128, 42)));
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f, cx = 128 + Mathf.Cos(a) * 26, cy = 128 + Mathf.Sin(a) * 26;
                c = Over(c, Color.Lerp(StoneLight, Stone, Hash(i, 2)), Fill(Circle(x, y, cx, cy, 14)));
            }
            c = Over(c, Snow, Fill(Ellipse(x, y, 118, 140, 30, 18)) * 0.9f);
            // the cross itself (seen from above, standing up: its arms and the beam drawn as they rise towards us)
            float beam = RoundRect(x, y, 128, 150, 9, 72, 3), arms = RoundRect(x, y, 128, 178, 44, 8, 3);
            c = Over(c, Iron, Fill(Mathf.Min(beam, arms)));
            c = Over(c, Hex("6B7280"), Fill(Mathf.Min(RoundRect(x, y, 125, 150, 3, 68, 2), RoundRect(x, y, 128, 181, 40, 2.5f, 2))) * 0.8f);
            c = Over(c, Hex("E8C13A"), Fill(Circle(x, y, 128, 178, 7)));
            return c;
        }

        // A cairn: flat stones stacked in rings, a little flag on top.
        static Color Cairn(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 146, 110, 70), 10f));
            for (int ring = 0; ring < 4; ring++)
            {
                float r = 70 - ring * 16;
                c = Over(c, ring % 2 == 0 ? Stone : StoneLight, Fill(Star(x, y, 128, 128, r, r - 6, 11, ring * 0.5f)));
                c = Over(c, StoneDark, Stroke(Star(x, y, 128, 128, r, r - 6, 11, ring * 0.5f), 3f) * 0.7f);
            }
            c = Over(c, Snow, Fill(Circle(x, y, 120, 138, 18)) * 0.8f);
            c = Over(c, Iron, Fill(Circle(x, y, 128, 128, 4)));
            c = Over(c, Hex("E2393F"), Fill(Triangle(x, y, new Vector2(128, 132), new Vector2(128, 116), new Vector2(178, 126))));
            return c;
        }

        // A line of prayer flags strung between two poles, blowing in the wind.
        static Color PrayerFlags(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] cols = { Hex("3D7DD8"), Hex("F6FAFE"), Hex("E2393F"), Hex("2E9A57"), Hex("F5C842") };
            Vector2 a = new(30, 70), b = new(226, 186);
            for (int i = 0; i < 2; i++)
            {
                Vector2 p = i == 0 ? a : b;
                c = Over(c, Shadow, Soft(Circle(x, y, p.x + 8, p.y - 8, 10), 4f));
                c = Over(c, WoodDark, Fill(Circle(x, y, p.x, p.y, 8)));
            }
            // the string, sagging a little
            for (int k = 0; k <= 20; k++)
            {
                float t0 = k / 20f, t1 = (k + 1) / 20f;
                Vector2 p0 = Vector2.Lerp(a, b, t0) + new Vector2(10, -10) * Mathf.Sin(t0 * Mathf.PI), p1 = Vector2.Lerp(a, b, t1) + new Vector2(10, -10) * Mathf.Sin(t1 * Mathf.PI);
                c = Over(c, Hex("6B5A44"), Fill(SegDist(new Vector2(x, y), p0, p1) - 1.6f));
            }
            for (int k = 1; k < 10; k++)
            {
                float t = k / 10f;
                Vector2 p = Vector2.Lerp(a, b, t) + new Vector2(10, -10) * Mathf.Sin(t * Mathf.PI);
                var flag = new[] { p + new Vector2(-8, 4), p + new Vector2(8, -4), p + new Vector2(22, -26), p + new Vector2(6, -18) };
                c = Over(c, Shadow, Soft(Polygon(x, y, Shift(flag, new Vector2(6, -6))), 3f) * 0.6f);
                c = Over(c, cols[k % cols.Length], Fill(Polygon(x, y, flag)));
                c = Over(c, Outline, Stroke(Polygon(x, y, flag), 2f) * 0.5f);
            }
            return c;
        }

        // The base camp's tent: an orange dome with its seams, guy lines pegged out, snow banked round it.
        static Color BaseTent(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, SnowShade, Fill(Circle(x, y, 128, 128, 104)) * 0.8f);
            c = Over(c, Snow, Fill(Circle(x, y, 128, 128, 96)));
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) * Mathf.PI / 2f;
                Vector2 o = new(128 + Mathf.Cos(a) * 66, 128 + Mathf.Sin(a) * 66), peg = new(128 + Mathf.Cos(a) * 100, 128 + Mathf.Sin(a) * 100);
                c = Over(c, Hex("6B5A44"), Fill(SegDist(new Vector2(x, y), o, peg) - 1.2f));
                c = Over(c, Iron, Fill(Circle(x, y, peg.x, peg.y, 3.5f)));
            }
            c = Over(c, Shadow, Soft(Circle(x, y, 144, 112, 70), 10f));
            float dome = RoundRect(x, y, 128, 128, 66, 66, 40);
            c = Over(c, Color.Lerp(Hex("FF9A45"), TentDark, Mathf.Clamp01(0.5f + ((x - 128) - (y - 128)) / 170f)), Fill(dome));
            c = Over(c, TentDark, Fill(Mathf.Max(dome, Mathf.Min(Mathf.Abs(x - y) - 3f, Mathf.Abs(x + y - 256) - 3f))) * 0.9f);
            c = Over(c, Hex("FFD3A8"), Fill(Circle(x, y, 128, 128, 7)));
            // the porch
            c = Over(c, TentOrange, Fill(Triangle(x, y, new Vector2(84, 76), new Vector2(172, 76), new Vector2(128, 38))));
            c = Over(c, Outline, Stroke(dome, 4f));
            return c;
        }

        // A roped team of four climbers on their way up: helmets, bright jackets, packs, the rope between them.
        static Color RopeTeam(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] coats = { Hex("E2393F"), Hex("3D7DD8"), Hex("F5C842"), Hex("2E9A57") };
            var at = new[] { new Vector2(52, 60), new Vector2(98, 108), new Vector2(146, 150), new Vector2(196, 196) };
            // the rope
            for (int i = 0; i < at.Length - 1; i++)
                for (int k = 0; k < 10; k++)
                {
                    float t0 = k / 10f, t1 = (k + 1) / 10f;
                    Vector2 bend = new Vector2(14, -14) * Mathf.Sin(t0 * Mathf.PI), bend1 = new Vector2(14, -14) * Mathf.Sin(t1 * Mathf.PI);
                    c = Over(c, Rope, Fill(SegDist(new Vector2(x, y), Vector2.Lerp(at[i], at[i + 1], t0) + bend, Vector2.Lerp(at[i], at[i + 1], t1) + bend1) - 2.2f));
                }
            for (int i = 0; i < at.Length; i++)
            {
                Vector2 p = at[i];
                c = Over(c, Shadow, Soft(Circle(x, y, p.x + 10, p.y - 10, 18), 5f));
                c = Over(c, coats[i], Fill(Ellipse(x, y, p.x, p.y, 17, 14)));
                c = Over(c, Shade(coats[i], 0.7f), Fill(RoundRect(x, y, p.x - 7, p.y - 6, 9, 8, 3))); // pack
                c = Over(c, Color.white, Fill(Circle(x, y, p.x + 4, p.y + 5, 8.5f))); // helmet
                c = Over(c, Iron, Fill(Capsule(x, y, p.x + 14, p.y - 2, p.x + 26, p.y + 12, 2.2f))); // ice axe
                c = Over(c, Outline, Stroke(Ellipse(x, y, p.x, p.y, 17, 14), 2.5f) * 0.7f);
            }
            return c;
        }

        // The judges' tower of the ski-flying hill: a box of glass and wood on legs, a flag on the roof.
        static Color JudgesTower(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 158, 98, 84, 70, 6), 10f));
            float box = RoundRect(x, y, 128, 128, 84, 70, 6);
            c = Over(c, Hex("44505E"), Fill(box));
            // the glass front, judges' windows
            c = Over(c, Hex("7FC7E8"), Fill(RoundRect(x, y, 128, 74, 76, 12, 3)));
            for (int i = 0; i < 5; i++) c = Over(c, Hex("2D3A48"), Fill(RoundRect(x, y, 66 + i * 31, 74, 1.5f, 11, 1)));
            // the roof: wooden boards, a ridge
            c = Over(c, Wood, Fill(RoundRect(x, y, 128, 140, 80, 52, 4)));
            for (int i = 0; i < 9; i++) c = Over(c, WoodDark, Fill(RoundRect(x, y, 128, 98 + i * 10.5f, 80, 1.2f, 1)) * 0.8f);
            c = Over(c, Snow, Fill(Mathf.Max(RoundRect(x, y, 128, 140, 80, 52, 4) + 4f, Ellipse(x, y, 100, 168, 60, 26))) * 0.9f);
            c = Over(c, Iron, Fill(Circle(x, y, 196, 182, 5)));
            c = Over(c, Hex("E2393F"), Fill(Triangle(x, y, new Vector2(196, 188), new Vector2(196, 170), new Vector2(240, 180))));
            c = Over(c, Outline, Stroke(box, 4.5f));
            return c;
        }

        // Stands below the ski hill: grey steps packed with spectators in bright coats and bobble hats.
        static Color SkiStands(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] coats = { Hex("E2513A"), Hex("3D7DD8"), Hex("F5C842"), Hex("2BB3A3"), Hex("E86FB0"), Hex("8A5CD6"), Hex("FF7A1A") };
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 118, 96, 6), 10f));
            for (int row = 0; row < 4; row++)
                c = Over(c, row % 2 == 0 ? Hex("9AA4B2") : Hex("8892A0"), Fill(RoundRect(x, y, 128, 52 + row * 46, 118, 22, 3)));
            for (int row = 0; row < 4; row++)
                for (int i = 0; i < 9; i++)
                {
                    float px = 26 + i * 25.5f + (row % 2) * 10, py = 56 + row * 46;
                    int k = (int)(Hash(i + 11, row + 3) * coats.Length);
                    c = Over(c, coats[k], Fill(Ellipse(x, y, px, py, 11, 9)));
                    c = Over(c, Hex("F2C9A0"), Fill(Circle(x, y, px, py + 2, 5.5f)));
                    c = Over(c, coats[(k + 3) % coats.Length], Fill(Circle(x, y, px, py + 4, 5)));
                    c = Over(c, Color.white, Fill(Circle(x, y, px, py + 6, 2.2f)));
                }
            // a banner along the front
            c = Over(c, Hex("1E5BC6"), Fill(RoundRect(x, y, 128, 22, 112, 8, 2)));
            for (int i = 0; i < 8; i++) c = Over(c, Color.white, Fill(RoundRect(x, y, 34 + i * 27, 22, 6, 3, 1)));
            return c;
        }

        // A climber's pack in the snow, an ice axe across it, a coil of rope.
        static Color IceAxePack(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 112, 46, 58, 18), 8f));
            float pack = RoundRect(x, y, 124, 128, 46, 58, 18);
            c = Over(c, Hex("C9302C"), Fill(pack));
            c = Over(c, Hex("9E2421"), Fill(RoundRect(x, y, 124, 104, 34, 20, 8)));
            c = Over(c, Iron, Fill(RoundRect(x, y, 124, 160, 30, 4, 2)));
            c = Over(c, Outline, Stroke(pack, 4f));
            // the rope coil beside it
            c = Over(c, Shadow, Soft(Circle(x, y, 202, 70, 30), 6f));
            c = Over(c, Rope, Fill(Shell(Circle(x, y, 196, 76, 24), 6f)));
            c = Over(c, Hex("B8922A"), Stroke(Circle(x, y, 196, 76, 24), 2f));
            // the ice axe across the pack
            c = Over(c, Hex("2A2F38"), Fill(Capsule(x, y, 58, 40, 196, 214, 5f)));
            c = Over(c, Hex("9AA4B2"), Fill(RotRect(x, y, 190, 206, 26, 6, 2, -38f)));
            return c;
        }
    }
}
