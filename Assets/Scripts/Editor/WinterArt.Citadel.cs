using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The citadel scenery atlas (16 cells of 256 px, 4 x 4) for Winter Cup race 8, The Frost Citadel: the ice towers and
    /// walls of the fortress, ice-knight statues with their swords, banners on poles, stone pillars, the frozen throne, the
    /// bobsled start house and its timing board, the arena's line of flags, crowds and stands - and the snowy basics.
    /// Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] CitadelCells =
        {
            "SnowPine", "SnowRock", "FrostBush", "SnowDrift",
            "IceTower", "IceWall", "IceKnight", "Banner",
            "StonePillar", "FrozenThrone", "StartHouse", "TimingBoard",
            "WinterCrowd", "SkiStands", "ArenaFlags", "Serac",
        };

        const int CitadelRows = 4;

        /// <summary>UV rect of a citadel scenery cell by name (row 0 at the bottom).</summary>
        public static Rect CitadelCell(string name)
        {
            int i = System.Array.IndexOf(CitadelCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)CitadelRows, 0.25f, 1f / CitadelRows);
        }

        [MenuItem("SomeGame/Generate Citadel Art")]
        public static void GenerateCitadel()
        {
            Write(ArtGenerator.Folder, "SceneryCitadel", 1024, 256 * CitadelRows, CitadelPixel, 128, false);
            ArtGenerator.Material("SceneryCitadel", "SceneryCitadel");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static readonly Color Royal = Hex("1E4FB0"), RoyalDark = Hex("143A86"), Gold = Hex("F2C23A"), GoldDark = Hex("B98A1C");

        static Color CitadelPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return CitadelCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "IceTower" => IceTower(lx, ly),
                "IceWall" => IceWall(lx, ly),
                "IceKnight" => IceKnight(lx, ly),
                "Banner" => CitadelBanner(lx, ly),
                "StonePillar" => StonePillar(lx, ly),
                "FrozenThrone" => FrozenThrone(lx, ly),
                "StartHouse" => StartHouse(lx, ly),
                "TimingBoard" => TimingBoard(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "SkiStands" => SkiStands(lx, ly),
                "ArenaFlags" => ArenaFlags(lx, ly),
                "Serac" => Serac(lx, ly, 1),
                _ => new Color(0, 0, 0, 0),
            };
        }

        // An ice knight seen from above: helmet with a plume ridge, broad shoulders, a shield and a long sword held point down.
        static Color IceKnight(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 150, 104, 74, 52), 10f));
            c = Over(c, Shadow, Soft(RotRect(x, y, 150, 74, 8, 48, 3, 0f), 5f));
            // plinth
            c = Over(c, StoneDark, Fill(RoundRect(x, y, 128, 128, 84, 84, 10)));
            c = Over(c, Stone, Fill(RoundRect(x, y, 128, 128, 76, 76, 8)));
            // shoulders and cloak
            float body = Ellipse(x, y, 128, 132, 64, 44);
            c = Over(c, IceShade(x, y, 128, 132, 64), Fill(body));
            c = Over(c, IceLine, Stroke(body, 4f));
            // the sword, point down, the hilt by the hands
            c = Over(c, Hex("D9F2FF"), Fill(RoundRect(x, y, 128, 66, 7, 48, 3)));
            c = Over(c, IceLine, Stroke(RoundRect(x, y, 128, 66, 7, 48, 3), 3f));
            c = Over(c, GoldDark, Fill(RoundRect(x, y, 128, 112, 26, 6, 3)));
            // the shield on the left arm
            float shield = Polygon(x, y, new[] { new Vector2(52, 160), new Vector2(84, 166), new Vector2(86, 128), new Vector2(68, 102), new Vector2(50, 128) });
            c = Over(c, Royal, Fill(shield));
            c = Over(c, Gold, Fill(Mathf.Max(shield, Mathf.Min(Mathf.Abs(x - 68) - 3f, Mathf.Abs(y - 136) - 3f))));
            c = Over(c, Outline, Stroke(shield, 3f));
            // the helmet with its crest
            float helm = Circle(x, y, 128, 146, 22);
            c = Over(c, IceShade(x, y, 128, 146, 22), Fill(helm));
            c = Over(c, Royal, Fill(RoundRect(x, y, 128, 146, 5, 24, 3)));
            c = Over(c, IceLine, Stroke(helm, 3.5f));
            return c;
        }

        // A royal banner hanging from a crossbar on a pole, rippling.
        static Color CitadelBanner(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 110, 46, 70, 6), 8f));
            c = Over(c, Hex("3A3F48"), Fill(Circle(x, y, 128, 196, 10)));
            c = Over(c, GoldDark, Fill(RoundRect(x, y, 128, 196, 56, 5, 3)));
            float cloth = Polygon(x, y, new[] { new Vector2(80, 192), new Vector2(176, 192), new Vector2(176 + 6 * Mathf.Sin(y * 0.05f), 70), new Vector2(128, 44), new Vector2(80 + 6 * Mathf.Sin(y * 0.05f), 70) });
            float ripple = 0.85f + 0.15f * Mathf.Sin(x * 0.12f);
            c = Over(c, Shade(Royal, ripple), Fill(cloth));
            c = Over(c, Gold, Fill(Mathf.Max(cloth, Mathf.Abs(Polygon(x, y, new[] { new Vector2(90, 182), new Vector2(166, 182), new Vector2(166, 76), new Vector2(128, 56), new Vector2(90, 76) })) - 2.5f)));
            // a snowflake crest
            for (int k = 0; k < 3; k++)
            {
                float a = k * Mathf.PI / 3f;
                Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a));
                c = Over(c, Color.white, Fill(SegDist(new Vector2(x, y), new Vector2(128, 128) - d * 28, new Vector2(128, 128) + d * 28) - 4f) * Fill(cloth));
            }
            c = Over(c, Outline, Stroke(cloth, 3f));
            return c;
        }

        // A tall stone pillar with an ice cap, frozen chains hanging from it.
        static Color StonePillar(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RotRect(x, y, 170, 86, 60, 30, 10, -45f), 12f));
            float pillar = Star(x, y, 128, 128, 72, 64, 8, 0.2f);
            c = Over(c, Color.Lerp(StoneLight, StoneDark, Mathf.Clamp01(0.5f + ((x - 128) - (y - 128)) / 180f)), Fill(pillar));
            c = Over(c, StoneDark, Stroke(Circle(x, y, 128, 128, 50), 3f) * Fill(pillar));
            c = Over(c, Snow, Fill(Circle(x, y, 118, 138, 40)));
            c = Over(c, SeracLight, Fill(Circle(x, y, 132, 124, 16)) * 0.8f);
            c = Over(c, Outline, Stroke(pillar, 4f));
            // chains down its sides
            for (int k = 0; k < 9; k++)
            {
                c = Over(c, Hex("5E6878"), Stroke(Ellipse(x, y, 214 - k * 3, 128 - k * 12, 5, 7), 2.5f));
                c = Over(c, Hex("5E6878"), Stroke(Ellipse(x, y, 42 + k * 3, 128 + k * 12, 5, 7), 2.5f));
            }
            return c;
        }

        // The frozen throne: a high-backed seat of ice on a dais, a royal cushion, crystals rising round it.
        static Color FrozenThrone(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 148, 106, 96, 86, 12), 12f));
            // the dais, three steps
            for (int k = 0; k < 3; k++)
                c = Over(c, k % 2 == 0 ? Hex("9FB4C8") : Hex("BFD0E0"), Fill(RoundRect(x, y, 128, 128 - k * 6, 96 - k * 14, 86 - k * 14, 8)));
            // the back: a fan of ice crystals
            for (int k = 0; k < 7; k++)
            {
                float a = Mathf.PI * (0.18f + k * 0.107f);
                Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a));
                var spike = new[] { new Vector2(128, 150) + new Vector2(-d.y, d.x) * 10, new Vector2(128, 150) - new Vector2(-d.y, d.x) * 10, new Vector2(128, 150) + d * (72 + (k % 2) * 16) };
                c = Over(c, IceShade(x, y, 128, 170, 60), Fill(Polygon(x, y, spike)));
                c = Over(c, IceLine, Stroke(Polygon(x, y, spike), 2.5f));
            }
            // the seat and its cushion
            float seat = RoundRect(x, y, 128, 128, 42, 34, 8);
            c = Over(c, IceShade(x, y, 128, 128, 42), Fill(seat));
            c = Over(c, Royal, Fill(RoundRect(x, y, 128, 122, 30, 22, 8)));
            c = Over(c, Gold, Stroke(RoundRect(x, y, 128, 122, 30, 22, 8), 3f));
            c = Over(c, IceLine, Stroke(seat, 3.5f));
            return c;
        }

        // The bobsled start house: a timber hut with a pitched snowy roof over the start ramp, a clock on its front.
        static Color StartHouse(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 150, 104, 92, 72, 6), 10f));
            float house = RoundRect(x, y, 128, 128, 92, 72, 6);
            c = Over(c, Wood, Fill(house));
            // the roof: two slopes, snow on top, the ridge
            c = Over(c, Shade(Wood, 1.12f), Fill(Mathf.Max(house, y < 128 ? 0f : 1f) * 0f + RoundRect(x, y, 128, 158, 88, 40, 4)));
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 158, 86, 36, 4)));
            c = Over(c, Snow, Fill(RoundRect(x, y, 128, 98, 86, 34, 4)) * 0.85f);
            c = Over(c, WoodDark, Fill(RoundRect(x, y, 128, 128, 90, 3, 1)));
            // the start opening and a clock face on the front
            c = Over(c, Hex("2D3A48"), Fill(RoundRect(x, y, 128, 60, 34, 8, 2)));
            c = Over(c, Color.white, Fill(Circle(x, y, 200, 70, 14)));
            c = Over(c, Outline, Stroke(Circle(x, y, 200, 70, 14), 3f));
            c = Over(c, Outline, Fill(SegDist(new Vector2(x, y), new Vector2(200, 70), new Vector2(200, 80)) - 1.5f));
            c = Over(c, Outline, Fill(SegDist(new Vector2(x, y), new Vector2(200, 70), new Vector2(207, 70)) - 1.5f));
            c = Over(c, Outline, Stroke(house, 4.5f));
            return c;
        }

        // A timing board on two legs: dark panel, bright digits, the flags of the nations along its top.
        static Color TimingBoard(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 112, 112, 36, 6), 8f));
            c = Over(c, Hex("3A3F48"), Fill(Circle(x, y, 40, 128, 8)));
            c = Over(c, Hex("3A3F48"), Fill(Circle(x, y, 216, 128, 8)));
            float board = RoundRect(x, y, 128, 128, 108, 34, 5);
            c = Over(c, Hex("1A2230"), Fill(board));
            // digits: blocks of white segments
            for (int k = 0; k < 6; k++)
            {
                float cx = 50 + k * 31;
                if (k == 2) { c = Over(c, Gold, Fill(Circle(x, y, cx, 122, 3.5f))); c = Over(c, Gold, Fill(Circle(x, y, cx, 136, 3.5f))); continue; }
                c = Over(c, Color.white, Fill(RoundRect(x, y, cx, 129, 10, 2.5f, 1)));
                c = Over(c, Color.white, Fill(RoundRect(x, y, cx - 9, 129, 2.5f, 15, 1)) * (k % 2 == 0 ? 1f : 0f));
                c = Over(c, Color.white, Fill(RoundRect(x, y, cx + 9, 129, 2.5f, 15, 1)));
                c = Over(c, Color.white, Fill(RoundRect(x, y, cx, 144, 10, 2.5f, 1)));
                c = Over(c, Color.white, Fill(RoundRect(x, y, cx, 114, 10, 2.5f, 1)));
            }
            Color[] flags = { Hex("E2393F"), Color.white, Royal, Hex("2E9A57"), Gold, Hex("E2393F"), Royal };
            for (int k = 0; k < flags.Length; k++) c = Over(c, flags[k], Fill(RoundRect(x, y, 44 + k * 28, 172, 11, 6, 1)));
            c = Over(c, Outline, Stroke(board, 3.5f));
            return c;
        }

        // The arena's line of flags on tall poles, blowing in the wind.
        static Color ArenaFlags(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] cols = { Royal, Hex("E2393F"), Gold, Hex("2E9A57"), Color.white };
            for (int k = 0; k < 5; k++)
            {
                float px = 30 + k * 49, py = 60 + k * 34;
                c = Over(c, Shadow, Soft(Circle(x, y, px + 10, py - 10, 8), 4f));
                c = Over(c, Hex("3A3F48"), Fill(Circle(x, y, px, py, 6)));
                float flag = Polygon(x, y, new[] { new Vector2(px, py + 4), new Vector2(px + 44, py + 10 + 4 * Mathf.Sin(x * 0.2f)), new Vector2(px + 44, py - 14 + 4 * Mathf.Sin(x * 0.2f)), new Vector2(px, py - 16) });
                c = Over(c, cols[k], Fill(flag));
                c = Over(c, Outline, Stroke(flag, 2f) * 0.6f);
            }
            return c;
        }
    }
}
