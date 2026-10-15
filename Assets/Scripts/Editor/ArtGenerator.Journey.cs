using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Art for the journey race (Valley Cup race 6): ground textures for its stretches (mud, sand, red rock,
    /// pebbles, wheat), a scenery atlas with the farm, canyon and river pieces, and the sprites of the things
    /// that move or stand on the road (sheep, boulders, the mine train and its level crossing, the rockslide,
    /// the windmill's sails, the section arches). Same flat, outlined Alpine Rally style as the rest.
    /// </summary>
    public static partial class ArtGenerator
    {
        // ---------- scenery atlas: 4 x 4 cells of 256 px ----------
        public static readonly string[] JourneyCells =
        {
            "Pine", "Tree", "Bushes", "Rock",
            "Windmill", "Sunflowers", "HayRolls", "Barn",
            "Mesa", "RedRocks", "DryBush", "Hoodoos",
            "Reeds", "RiverStones", "Crowd", "Flowers",
        };

        /// <summary>UV rect of a journey scenery cell by name (row 0 at the top of the texture).</summary>
        public static Rect JourneyCell(string name)
        {
            int i = System.Array.IndexOf(JourneyCells, name);
            int col = i % 4, row = i / 4;
            return new Rect(col * 0.25f, 1f - (row + 1) * 0.25f, 0.25f, 0.25f);
        }

        [MenuItem("SomeGame/Generate Journey Art")]
        public static void GenerateJourney()
        {
            World("GroundMud", 128, 128, MudPixel, 32, true);
            World("GroundSand", 128, 128, DesertSandPixel, 32, true);
            World("GroundRedRock", 128, 128, RedRockPixel, 32, true);
            World("GroundPebbles", 128, 128, PebblesPixel, 32, true);
            World("GroundWheat", 128, 128, WheatPixel, 32, true);
            foreach (var g in new[] { "GroundMud", "GroundSand", "GroundRedRock", "GroundPebbles", "GroundWheat" }) Material(g, g);
            World("SceneryJourney", 1024, 1024, JourneyPixel, 128, false);
            Material("SceneryJourney", "SceneryJourney");

            World("WindmillTower", 256, 256, WindmillTower, 128, false);
            World("Sheep", 128, 128, Sheep, 128, false);
            World("Boulder", 160, 160, Boulder, 128, false);
            World("WindmillSails", 512, 512, WindmillSails, 128, false);
            World("MineLoco", 192, 320, MineLoco, 128, false);
            World("MineWagon", 192, 256, MineWagon, 128, false);
            World("Rails", 128, 64, RailsPixel, 32, true);
            Material("Rails", "Rails");
            World("CrossingLight", 128, 128, CrossingLight, 128, false);
            World("Barrier", 48, 384, Barrier, 128, false);
            World("RockPile", 384, 384, RockPile, 128, false);
            World("ArchPillar", 160, 160, ArchPillar, 128, false);
            World("CanyonWall", 64, 64, CanyonWallPixel, 32, true);
            Material("CanyonWall", "CanyonWall");
            World("Banner", 128, 128, (x, y) => White(Fill(RoundRect(x, y, 64, 64, 60, 60, 18))), 128, false);
            AssetDatabase.Refresh();
        }

        static Color JourneyPixel(float x, float y)
        {
            int col = (int)(x / 256), row = 3 - (int)(y / 256);
            float lx = x - col * 256, ly = y - (3 - row) * 256;
            return (row * 4 + col) switch
            {
                0 => Pine(lx, ly),
                1 => RoundTree(lx, ly),
                2 => Bushes(lx, ly),
                3 => Rock(lx, ly),
                4 => WindmillTower(lx, ly),
                5 => Sunflowers(lx, ly),
                6 => HayRolls(lx, ly),
                7 => Barn(lx, ly),
                8 => Mesa(lx, ly),
                9 => RedRocks(lx, ly),
                10 => DryBush(lx, ly),
                11 => Hoodoos(lx, ly),
                12 => Reeds(lx, ly),
                13 => RiverStones(lx, ly),
                14 => Crowd(lx, ly),
                _ => Flowers(lx, ly),
            };
        }

        // ================================================================== ground textures (tile 128 px)

        static float N(float x, float y, float cell) => Noise(x / cell, y / cell, Mathf.RoundToInt(128f / cell));

        static Color MudPixel(float x, float y)
        {
            float n = N(x, y, 16) * 0.6f + N(x, y, 8) * 0.4f;
            Color c = Color.Lerp(Hex("6E5235"), Hex("8A6A45"), n);
            // Puddles and tyre ruts.
            float puddle = N(x + 40, y + 9, 32);
            if (puddle > 0.68f) c = Color.Lerp(c, Hex("4D3A27"), Mathf.Clamp01((puddle - 0.68f) * 8f));
            if (puddle > 0.8f) c = Color.Lerp(c, Hex("8C9C8C"), Mathf.Clamp01((puddle - 0.8f) * 10f) * 0.35f); // a wet sheen
            c = Color.Lerp(c, Hex("5C432C"), Mathf.Clamp01(1f - Mathf.Abs(Mathf.Repeat(x + N(x, y, 32) * 10f, 64f) - 20f) / 2.5f) * 0.4f);
            return c;
        }

        static Color DesertSandPixel(float x, float y)
        {
            float n = N(x, y, 16) * 0.5f + N(x, y, 4) * 0.5f;
            Color c = Color.Lerp(Hex("D9B37A"), Hex("E8C890"), n);
            // Wind ripples.
            float ripple = Mathf.Sin((y + N(x, y, 32) * 18f) * 0.4f);
            c = Color.Lerp(c, Hex("C79E64"), Mathf.Clamp01(ripple - 0.6f) * 0.8f);
            return c;
        }

        static Color RedRockPixel(float x, float y)
        {
            float n = N(x, y, 32) * 0.55f + N(x, y, 8) * 0.45f;
            Color c = Color.Lerp(Hex("B5582F"), Hex("D27845"), n);
            // Strata bands and cracks.
            float band = Mathf.Repeat(y + N(x, y, 32) * 24f, 32f);
            if (band < 3f) c = Color.Lerp(c, Hex("9A4626"), 0.55f);
            float crack = Mathf.Abs(N(x, y, 16) - 0.5f);
            if (crack < 0.018f) c = Color.Lerp(c, Hex("7C3820"), 0.7f);
            if (Hash((int)x / 3, (int)y / 3) > 0.985f) c = Color.Lerp(c, Hex("F0B27A"), 0.6f); // grit
            return c;
        }

        static Color PebblesPixel(float x, float y)
        {
            Color c = Color.Lerp(Hex("B8B1A0"), Hex("CFC8B6"), N(x, y, 16));
            // Pebbles on a jittered grid.
            const float cell = 16f;
            int cx = Mathf.FloorToInt(x / cell), cy = Mathf.FloorToInt(y / cell);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    int gx = ((cx + i) % 8 + 8) % 8, gy = ((cy + j) % 8 + 8) % 8;
                    float px = (cx + i + 0.2f + Hash(gx, gy) * 0.6f) * cell, py = (cy + j + 0.2f + Hash(gy + 7, gx) * 0.6f) * cell;
                    float r = 3f + Hash(gx * 3, gy * 5) * 3f;
                    float d = Ellipse(x, y, px, py, r * 1.3f, r);
                    if (d < 0f)
                    {
                        Color stone = Color.Lerp(Hex("8E8A80"), Hex("E2DDD0"), Hash(gx + 11, gy + 3));
                        c = Over(c, Color.Lerp(stone, Color.white, Mathf.Clamp01((py - y) / r) * 0.25f), Fill(d));
                    }
                }
            return c;
        }

        static Color WheatPixel(float x, float y)
        {
            float n = N(x, y, 16);
            Color c = Color.Lerp(Hex("D9B54A"), Hex("EBCB62"), n);
            // Rows of wheat with darker furrows.
            float row = Mathf.Repeat(x + N(x, y, 32) * 6f, 12f);
            c = Color.Lerp(c, Hex("B8923A"), Mathf.Clamp01(1f - Mathf.Abs(row - 1.5f) / 1.5f) * 0.6f);
            if (Hash((int)x / 2, (int)y / 4) > 0.93f) c = Color.Lerp(c, Hex("F7E39A"), 0.6f);
            return c;
        }

        public static void GenerateCanyonWall()
        {
            World("CanyonWall", 64, 64, CanyonWallPixel, 32, true);
            Material("CanyonWall", "CanyonWall");
            AssetDatabase.Refresh();
        }

        // A banked sandstone wall (u = across, from the kerb up to the rim), with a chevron along it.
        static Color CanyonWallPixel(float x, float y)
        {
            float u = x / 64f;
            if (u > 0.93f) return Hex("6E2E17");
            if (u > 0.82f) return Hex("E9A26A");
            Color c = Color.Lerp(Hex("A44E2A"), Hex("DB8550"), Mathf.SmoothStep(0f, 1f, u / 0.82f));
            c = Color.Lerp(c, Shade(c, 0.9f), Noise(x / 8f, y / 8f, 8) * 0.6f);
            if (Mathf.Repeat(x + Noise(x / 8f, y / 8f, 8) * 6f, 16f) < 1.5f) c = Color.Lerp(c, Hex("8A3E20"), 0.5f); // strata
            float cx = 32f, chevron = Mathf.Abs(Mathf.Abs(x - cx) * 0.9f + (y - 40f)) - 4f;
            if (Mathf.Abs(x - cx) < 18f && chevron < 0f) c = Color.Lerp(c, Hex("FFE2C0"), 0.7f);
            return c;
        }

        // ================================================================== scenery cells

        static Color WindmillTower(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 146, 110, 84), 12f));
            float tower = Circle(x, y, 128, 128, 84);
            c = Over(c, Hex("E8DCC4"), Fill(tower));
            // Conical roof seen from above: wedges round the centre.
            float a = Mathf.Atan2(y - 128, x - 128);
            float wedge = Mathf.Repeat(a / (Mathf.PI * 2f) * 16f, 1f);
            float roof = Circle(x, y, 128, 128, 66);
            c = Over(c, wedge < 0.5f ? Hex("A9472F") : Hex("C2583A"), Fill(roof));
            c = Over(c, Hex("7A3020"), Stroke(roof, 4f));
            c = Over(c, Hex("5A4636"), Fill(Circle(x, y, 128, 128, 10)));
            c = Over(c, Outline, Stroke(tower, 5f) * 0.85f);
            return c;
        }

        static Color Sunflowers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float patch = RoundRect(x, y, 128, 128, 110, 104, 30);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 110, 104, 30), 12f));
            c = Over(c, Hex("4E8A35"), Fill(patch));
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                {
                    float fx = 40 + i * 44 + (j % 2) * 10 + Hash(i, j) * 8, fy = 42 + j * 43 + Hash(j, i) * 8;
                    if (patch > -10f) continue;
                    float petals = Star(x, y, fx, fy, 19, 12, 10, Hash(i + 3, j));
                    c = Over(c, Hex("3A6E28"), Soft(Circle(x, y, fx + 5, fy - 5, 18), 6f) * 0.5f);
                    c = Over(c, Hex("F6C531"), Fill(petals));
                    c = Over(c, Hex("6B3E1F"), Fill(Circle(x, y, fx, fy, 7)));
                }
            c = Over(c, Outline, Stroke(patch, 4f) * 0.6f);
            return c;
        }

        static Color HayRolls(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            void Roll(float cx, float cy, float angle)
            {
                float body = RotRect(x, y, cx, cy, 54, 36, 14, angle);
                c = Over(c, Shadow, Soft(RotRect(x, y, cx + 12, cy - 12, 54, 36, 14, angle), 10f));
                c = Over(c, Hex("E4B64E"), Fill(body));
                // Straw lines along the roll.
                float s = Mathf.Sin(((x - cx) * Mathf.Sin(-angle) + (y - cy) * Mathf.Cos(-angle)) * 0.6f);
                c = Over(c, Hex("C99633"), Fill(body + 4f) * Mathf.Clamp01(s - 0.5f) * 0.8f);
                c = Over(c, Outline, Stroke(body, 4f) * 0.8f);
            }
            Roll(92, 160, 0.3f);
            Roll(170, 96, -0.4f);
            return c;
        }

        static Color Mesa(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float wob = (N(x, y, 32) - 0.5f) * 26f; // soft, slow wobble (no jagged edge)
            float outer = Circle(x, y, 128, 128, 112) + wob;
            c = Over(c, Shadow, Soft(Circle(x, y, 144, 110, 112) + wob, 14f));
            c = Over(c, Hex("9A4626"), Fill(outer));
            c = Over(c, Hex("B9572F"), Fill(outer + 14f));
            c = Over(c, Hex("CC6B3C"), Fill(outer + 30f));
            float top = outer + 46f;
            c = Over(c, Hex("DE8A52"), Fill(top));
            c = Over(c, Hex("8DA357"), Fill(top + 6f) * Mathf.Clamp01((N(x, y, 32) - 0.55f) * 6f)); // a little scrub on top
            c = Over(c, Outline, Stroke(outer, 5f) * 0.8f);
            return c;
        }

        static Color RedRocks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            void Stone(float cx, float cy, float r, float seed)
            {
                float d = Circle(x, y, cx, cy, r) + (N(x + seed * 10f, y, 32) - 0.5f) * r * 0.35f;
                c = Over(c, Shadow, Soft(Circle(x, y, cx + r * 0.25f, cy - r * 0.25f, r), 8f));
                c = Over(c, Hex("A44E2A"), Fill(d));
                c = Over(c, Hex("D27A48"), Fill(Circle(x, y, cx - r * 0.25f, cy + r * 0.25f, r * 0.55f)) * Fill(d + 4f));
                c = Over(c, Outline, Stroke(d, 4f) * 0.8f);
            }
            Stone(96, 150, 58, 1);
            Stone(170, 100, 44, 7);
            Stone(170, 180, 26, 13);
            return c;
        }

        static Color DryBush(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 116, 70), 14f));
            float a = Mathf.Atan2(y - 128, x - 128), r = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
            float spikes = r - (56f + 18f * Mathf.Abs(Mathf.Sin(a * 7f)));
            c = Over(c, Hex("8C8A4A"), Fill(spikes));
            c = Over(c, Hex("A9A65C"), Fill(Circle(x, y, 118, 138, 36)) * Fill(spikes + 6f));
            // Twigs.
            for (int i = 0; i < 7; i++)
            {
                float ang = i * 0.9f + 0.3f;
                c = Over(c, Hex("6B4E33"), Fill(Capsule(x, y, 128, 128, 128 + Mathf.Cos(ang) * 70, 128 + Mathf.Sin(ang) * 70, 2.5f)) * 0.8f);
            }
            c = Over(c, Outline, Stroke(spikes, 3f) * 0.6f);
            return c;
        }

        static Color Hoodoos(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            void Pillar(float cx, float cy, float r)
            {
                c = Over(c, Shadow, Soft(Circle(x, y, cx + r * 0.6f, cy - r * 0.6f, r), 8f));
                float d = Circle(x, y, cx, cy, r);
                c = Over(c, Hex("B9572F"), Fill(d));
                c = Over(c, Hex("D9804A"), Fill(d + r * 0.3f));
                c = Over(c, Hex("E9A26A"), Fill(d + r * 0.6f)); // cap stone
                c = Over(c, Outline, Stroke(d, 4f) * 0.8f);
            }
            Pillar(84, 170, 40);
            Pillar(170, 150, 32);
            Pillar(118, 84, 46);
            return c;
        }

        static Color Reeds(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(Shadow.r, Shadow.g, Shadow.b, Shadow.a * 0.5f), Soft(Ellipse(x, y, 136, 120, 80, 60), 16f));
            for (int i = 0; i < 26; i++)
            {
                float a = Hash(i, 3) * Mathf.PI * 2f, r = Hash(i, 9) * 60f;
                float bx = 128 + Mathf.Cos(a) * r * 1.3f, by = 128 + Mathf.Sin(a) * r;
                float lean = (Hash(i, 5) - 0.5f) * 30f;
                c = Over(c, Hex("4F7F3A"), Fill(Capsule(x, y, bx, by - 30, bx + lean, by + 30, 4f)));
                c = Over(c, Hex("6E4A2B"), Fill(Capsule(x, y, bx + lean * 0.9f, by + 18, bx + lean, by + 36, 6f))); // cattail
            }
            return c;
        }

        static Color RiverStones(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 7; i++)
            {
                float cx = 50 + Hash(i, 1) * 156, cy = 50 + Hash(i, 2) * 156, rx = 18 + Hash(i, 4) * 22, ry = rx * (0.6f + Hash(i, 6) * 0.3f);
                float d = Ellipse(x, y, cx, cy, rx, ry);
                c = Over(c, Shadow, Soft(Ellipse(x, y, cx + 6, cy - 6, rx, ry), 6f));
                c = Over(c, Color.Lerp(Hex("8F938F"), Hex("C5C8C2"), Hash(i, 8)), Fill(d));
                c = Over(c, Color.white, Fill(Ellipse(x, y, cx - rx * 0.3f, cy + ry * 0.3f, rx * 0.35f, ry * 0.3f)) * 0.35f);
                c = Over(c, Outline, Stroke(d, 3f) * 0.6f);
            }
            return c;
        }

        // Spectators from above: rows of heads in coloured shirts, some waving.
        static Color Crowd(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] shirts = { Hex("E74C3C"), Hex("3D7DD8"), Hex("F2C230"), Hex("2BB3A3"), Hex("E86FB0"), Hex("FF7A1A"), Hex("FFFFFF"), Hex("8A5CD6") };
            c = Over(c, Hex("6B4A2E"), Fill(RoundRect(x, y, 128, 74, 120, 16, 6))); // a bench rail
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 6; i++)
                {
                    float px = 30 + i * 39 + (row % 2) * 18, py = 100 + row * 52;
                    int k = (int)(Hash(i, row) * shirts.Length);
                    c = Over(c, Shadow, Soft(Circle(x, y, px + 5, py - 5, 18), 6f));
                    c = Over(c, shirts[k], Fill(Ellipse(x, y, px, py, 18, 14)));
                    if (Hash(i + 5, row) > 0.55f) // a waving arm
                        c = Over(c, Hex("F2C9A0"), Fill(Capsule(x, y, px + 12, py + 6, px + 24, py + 26, 4.5f)));
                    c = Over(c, Hex("F2C9A0"), Fill(Circle(x, y, px, py + 2, 9)));
                    c = Over(c, Hex("5A3A22"), Fill(Circle(x, y, px, py + 6, 8)) * 0.85f); // hair
                    c = Over(c, Outline, Stroke(Ellipse(x, y, px, py, 18, 14), 2.5f) * 0.6f);
                }
            return c;
        }

        static Color Flowers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] petals = { Hex("FFFFFF"), Hex("F6C531"), Hex("E86FB0"), Hex("8A5CD6") };
            for (int i = 0; i < 22; i++)
            {
                float fx = 30 + Hash(i, 11) * 196, fy = 30 + Hash(i, 13) * 196;
                c = Over(c, Hex("4E8A35"), Fill(Circle(x, y, fx, fy - 6, 10)) * 0.7f);
                c = Over(c, petals[i % petals.Length], Fill(Star(x, y, fx, fy, 11, 6, 5, Hash(i, 2))));
                c = Over(c, Hex("F6C531"), Fill(Circle(x, y, fx, fy, 3.5f)));
            }
            return c;
        }

        // ================================================================== moving things and road furniture

        // A sheep from above: a woolly cloud, a dark face pointing +Y.
        static Color Sheep(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float Wool(float ox, float oy)
            {
                float d = Ellipse(x, y, 64 + ox, 58 + oy, 30, 36);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    d = SMin(d, Circle(x, y, 64 + ox + Mathf.Cos(a) * 26f, 58 + oy + Mathf.Sin(a) * 30f, 14), 8f);
                }
                return d;
            }
            c = Over(c, Shadow, Soft(Wool(8, -8), 8f));
            float wool = Wool(0, 0);
            c = Over(c, Hex("F4F1EA"), Fill(wool));
            c = Over(c, Hex("FFFFFF"), Fill(Circle(x, y, 56, 70, 18)) * Fill(wool + 6f));
            float head = Ellipse(x, y, 64, 102, 13, 16);
            c = Over(c, Hex("2E2A28"), Fill(head));
            c = Over(c, Hex("2E2A28"), Fill(Ellipse(x, y, 48, 98, 8, 4)));
            c = Over(c, Hex("2E2A28"), Fill(Ellipse(x, y, 80, 98, 8, 4)));
            c = Over(c, Outline, Stroke(wool, 3f) * 0.7f);
            return c;
        }

        static Color Boulder(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float d = Circle(x, y, 80, 80, 66) + (N(x, y, 32) - 0.5f) * 12f;
            c = Over(c, Hex("9A4626"), Fill(d));
            c = Over(c, Hex("C4683A"), Fill(Circle(x, y, 66, 94, 44)) * Fill(d + 6f));
            c = Over(c, Hex("E09A62"), Fill(Circle(x, y, 58, 104, 18)) * Fill(d + 10f));
            // Cracks so the roll can be seen.
            c = Over(c, Hex("7C3820"), Fill(Capsule(x, y, 40, 70, 96, 52, 3f)) * Fill(d + 4f));
            c = Over(c, Hex("7C3820"), Fill(Capsule(x, y, 96, 52, 118, 92, 3f)) * Fill(d + 4f));
            c = Over(c, Outline, Stroke(d, 5f));
            return c;
        }

        // Four sails on a cross, turning (the tower is in the atlas).
        static Color WindmillSails(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.4f;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a)), side = new(-dir.y, dir.x);
                Vector2 tip = new Vector2(256, 256) + dir * 236f;
                c = Over(c, Hex("6B4A2E"), Fill(Capsule(x, y, 256, 256, tip.x, tip.y, 7f)));
                // The sail: a lattice panel beside the spar.
                Vector2 p0 = new Vector2(256, 256) + dir * 60f, p1 = new Vector2(256, 256) + dir * 226f;
                var panel = new[] { p0, p1, p1 + side * 46f, p0 + side * 40f };
                float d = Polygon(x, y, panel);
                c = Over(c, Hex("F2E9D8"), Fill(d));
                float u = Vector2.Dot(new Vector2(x, y) - new Vector2(256, 256), dir);
                c = Over(c, Hex("B9A88A"), Fill(d + 2f) * Mathf.Clamp01(1f - Mathf.Abs(Mathf.Repeat(u, 28f) - 14f) / 2f) * 0.7f);
                c = Over(c, Outline, Stroke(d, 3f) * 0.8f);
            }
            c = Over(c, Hex("5A4636"), Fill(Circle(x, y, 256, 256, 22)));
            c = Over(c, Outline, Stroke(Circle(x, y, 256, 256, 22), 4f));
            return c;
        }

        // The little mine engine from above, nose +Y: a red cab, a black boiler with a funnel and a cow catcher.
        static Color MineLoco(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 108, 152, 70, 140, 20), 10f));
            c = Over(c, Hex("3A3634"), Fill(RoundRect(x, y, 96, 160, 74, 140, 16))); // chassis and wheels
            float cab = RoundRect(x, y, 96, 84, 62, 52, 10);
            c = Over(c, Hex("C9412F"), Fill(cab));
            c = Over(c, Hex("E8634C"), Fill(RoundRect(x, y, 96, 84, 48, 38, 8)));
            float boiler = RoundRect(x, y, 96, 196, 44, 70, 40);
            c = Over(c, Hex("2C3A44"), Fill(boiler));
            c = Over(c, Hex("4E6575"), Fill(RoundRect(x, y, 86, 196, 14, 60, 10)));
            c = Over(c, Hex("D9A441"), Fill(Circle(x, y, 96, 176, 10)) + Fill(Circle(x, y, 96, 226, 8)) * 0f);
            c = Over(c, Hex("1E2224"), Fill(Circle(x, y, 96, 238, 16))); // funnel
            c = Over(c, Hex("D9A441"), Fill(Triangle(x, y, new Vector2(46, 286), new Vector2(146, 286), new Vector2(96, 312)))); // cow catcher
            c = Over(c, Outline, Stroke(RoundRect(x, y, 96, 160, 74, 140, 16), 5f));
            return c;
        }

        static Color MineWagon(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 108, 116, 70, 100, 16), 10f));
            float tub = RoundRect(x, y, 96, 128, 70, 100, 14);
            c = Over(c, Hex("6B4A2E"), Fill(tub));
            c = Over(c, Hex("8C6440"), Fill(RoundRect(x, y, 96, 128, 58, 88, 10)));
            // Ore heaped inside: red rocks and gold glints.
            for (int i = 0; i < 15; i++)
            {
                float ox = 66 + (i % 3) * 30 + (Hash(i * 7 + 3, 11) - 0.5f) * 10f, oy = 60 + (i / 3) * 34 + (Hash(i * 5 + 1, 17) - 0.5f) * 10f;
                bool gold = Hash(i * 3 + 2, 23) > 0.72f;
                float lump = Circle(x, y, ox, oy, 12 + Hash(i * 11, 29) * 8);
                c = Over(c, gold ? Hex("F2C230") : (i % 2 == 0 ? Hex("B5582F") : Hex("9A4626")), Fill(lump));
                c = Over(c, Outline, Stroke(lump, 2f) * 0.5f);
            }
            c = Over(c, Hex("3A3634"), Stroke(RoundRect(x, y, 96, 128, 64, 94, 12), 6f));
            c = Over(c, Outline, Stroke(tub, 5f));
            return c;
        }

        // Rails across the road: two steel rails on wooden sleepers (tiled along the rails' length = U).
        static Color RailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float sleeper = Mathf.Repeat(x, 32f);
            if (sleeper > 6f && sleeper < 22f && y > 4f && y < 60f) c = Over(c, Hex("6B4A2E"), 1f);
            if (sleeper > 6f && sleeper < 22f && (y < 6f || y > 58f)) c = Over(c, Hex("4E3520"), 0.6f);
            foreach (float ry in new[] { 18f, 46f })
            {
                if (Mathf.Abs(y - ry) < 4f) c = Over(c, Hex("8E979C"), 1f);
                if (Mathf.Abs(y - ry - 2f) < 1.2f) c = Over(c, Hex("C9D1D5"), 1f);
            }
            return c;
        }

        // A crossing signal from above: a post with two lamps on a cross-buck.
        static Color CrossingLight(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 72, 56, 40), 8f));
            c = Over(c, Hex("FFFFFF"), Fill(RotRect(x, y, 64, 64, 54, 9, 4, 0.785f)));
            c = Over(c, Hex("FFFFFF"), Fill(RotRect(x, y, 64, 64, 54, 9, 4, -0.785f)));
            c = Over(c, Hex("D63A2C"), Fill(RotRect(x, y, 64, 64, 46, 4, 2, 0.785f)));
            c = Over(c, Hex("D63A2C"), Fill(RotRect(x, y, 64, 64, 46, 4, 2, -0.785f)));
            c = Over(c, Hex("2A2A2A"), Fill(Circle(x, y, 64, 64, 14)));
            c = Over(c, Outline, Stroke(Circle(x, y, 64, 64, 14), 3f));
            return c;
        }

        // The barrier arm (lies along +Y), red and white.
        static Color Barrier(float x, float y)
        {
            float arm = RoundRect(x, y, 24, 192, 14, 186, 10);
            bool red = Mathf.Repeat(y, 64f) < 32f;
            Color c = Over(new Color(0, 0, 0, 0), red ? Hex("D63A2C") : Hex("FFFFFF"), Fill(arm));
            c = Over(c, Outline, Stroke(arm, 3f));
            return c;
        }

        // A rockslide blocking a passage: a heap of boulders, dust round its foot.
        static Color RockPile(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(0.85f, 0.6f, 0.4f, 0.45f), Soft(Circle(x, y, 192, 192, 170), 40f));
            for (int i = 0; i < 16; i++)
            {
                float a = Hash(i, 1) * Mathf.PI * 2f, r = Mathf.Sqrt(Hash(i, 2)) * 120f;
                float cx = 192 + Mathf.Cos(a) * r, cy = 192 + Mathf.Sin(a) * r, s = 30 + Hash(i, 3) * 36;
                float d = Circle(x, y, cx, cy, s) + (N(x + i * 9f, y, 32) - 0.5f) * s * 0.35f;
                c = Over(c, Shadow, Soft(Circle(x, y, cx + 10, cy - 10, s), 8f));
                c = Over(c, Hash(i, 4) > 0.5f ? Hex("A44E2A") : Hex("8F918A"), Fill(d));
                c = Over(c, Color.white, Fill(Circle(x, y, cx - s * 0.3f, cy + s * 0.3f, s * 0.35f)) * Fill(d + 4f) * 0.25f);
                c = Over(c, Outline, Stroke(d, 4f) * 0.8f);
            }
            return c;
        }

        // An arch pillar from above: a square stone block with a flag pole.
        static Color ArchPillar(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 92, 68, 56, 56, 12), 10f));
            float block = RoundRect(x, y, 80, 80, 56, 56, 12);
            c = Over(c, Hex("E8DCC4"), Fill(block));
            c = Over(c, Hex("C9B998"), Fill(RoundRect(x, y, 80, 80, 40, 40, 8)));
            c = Over(c, Hex("173D33"), Fill(RoundRect(x, y, 80, 80, 22, 22, 6)));
            c = Over(c, Outline, Stroke(block, 5f));
            return c;
        }
    }
}
