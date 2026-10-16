using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Art for the journey race (Valley Cup race 6): a scenery atlas with the farm, garden and forest pieces and
    /// the furniture of the house, and the sprites of the things that move or stand on the road (sheep, boulders,
    /// the train and its level crossing, the windmill's sails, the section arches). The house itself (floors,
    /// roofs, the cat, the robot vacuum, the toy train...) is in ArtGenerator.House. Same flat, outlined style.
    /// </summary>
    public static partial class ArtGenerator
    {
        // ---------- scenery atlas: 4 x 8 cells of 256 px (outdoors, then the furniture) ----------
        public static readonly string[] JourneyCells =
        {
            "Pine", "Tree", "Bushes", "Rock",
            "Windmill", "Sunflowers", "HayRolls", "Barn",
            "Field", "Stump", "Ferns", "Mushrooms",
            "Reeds", "RiverStones", "Crowd", "Flowers",
            "Hedge", "GardenBed", "Fountain", "LogPile",
            "Sofa", "Armchair", "CoffeeTable", "Rug",
            "Bookshelf", "Plant", "TvUnit", "Bed",
            "Counter", "DiningTable", "Toys", "Lamp",
        };

        const int JourneyRows = 8;

        /// <summary>UV rect of a journey scenery cell by name (row 0 at the top of the texture).</summary>
        public static Rect JourneyCell(string name)
        {
            int i = System.Array.IndexOf(JourneyCells, name);
            int col = i % 4, row = i / 4;
            return new Rect(col * 0.25f, 1f - (row + 1) / (float)JourneyRows, 0.25f, 1f / JourneyRows);
        }

        [MenuItem("SomeGame/Generate Journey Art")]
        public static void GenerateJourney()
        {
            World("SceneryJourney", 1024, 256 * JourneyRows, JourneyPixel, 128, false);
            Material("SceneryJourney", "SceneryJourney");

            World("WindmillTower", 256, 256, WindmillTower, 128, false);
            World("Sheep", 128, 128, Sheep, 128, false);
            World("Cow", 160, 256, Cow, 128, false);
            World("Fence", 256, 64, Fence, 128, false);
            World("Boulder", 160, 160, Boulder, 128, false);
            World("WindmillSails", 512, 512, WindmillSails, 128, false);
            World("MineLoco", 192, 320, MineLoco, 128, false);
            World("MineWagon", 192, 256, MineWagon, 128, false);
            World("Rails", 128, 64, RailsPixel, 32, true);
            Material("Rails", "Rails");
            World("CrossingLight", 128, 128, CrossingLight, 128, false);
            World("Barrier", 48, 384, Barrier, 128, false);
            World("ArchPillar", 160, 160, ArchPillar, 128, false);
            GenerateHouse();
            World("Banner", 128, 128, (x, y) => White(Fill(RoundRect(x, y, 64, 64, 60, 60, 18))), 128, false);
            AssetDatabase.Refresh();
        }

        static Color JourneyPixel(float x, float y)
        {
            int col = (int)(x / 256), row = JourneyRows - 1 - (int)(y / 256);
            float lx = x - col * 256, ly = y - (JourneyRows - 1 - row) * 256;
            return JourneyCells[row * 4 + col] switch
            {
                "Pine" => Pine(lx, ly),
                "Tree" => RoundTree(lx, ly),
                "Bushes" => Bushes(lx, ly),
                "Rock" => Rock(lx, ly),
                "Windmill" => WindmillTower(lx, ly),
                "Sunflowers" => Sunflowers(lx, ly),
                "HayRolls" => HayRolls(lx, ly),
                "Barn" => Barn(lx, ly),
                "Field" => PatchField(lx, ly),
                "Stump" => Stump(lx, ly),
                "Ferns" => Ferns(lx, ly),
                "Mushrooms" => Mushrooms(lx, ly),
                "Reeds" => Reeds(lx, ly),
                "RiverStones" => RiverStones(lx, ly),
                "Crowd" => Crowd(lx, ly),
                "Flowers" => Flowers(lx, ly),
                "Hedge" => Hedge(lx, ly),
                "GardenBed" => GardenBed(lx, ly),
                "Fountain" => Fountain(lx, ly),
                "LogPile" => LogPile(lx, ly),
                "Sofa" => Sofa(lx, ly),
                "Armchair" => Armchair(lx, ly),
                "CoffeeTable" => CoffeeTable(lx, ly),
                "Rug" => Rug(lx, ly),
                "Bookshelf" => Bookshelf(lx, ly),
                "Plant" => HousePlant(lx, ly),
                "TvUnit" => TvUnit(lx, ly),
                "Bed" => Bed(lx, ly),
                "Counter" => Counter(lx, ly),
                "DiningTable" => DiningTable(lx, ly),
                "Toys" => Toys(lx, ly),
                _ => FloorLamp(lx, ly),
            };
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

        // A black-and-white cow from above, lying down for a rest, head (+Y) a little to one side.
        static Color Cow(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float Body(float ox, float oy) => SMin(Ellipse(x, y, 80 + ox, 104 + oy, 50, 78), Circle(x, y, 80 + ox, 60 + oy, 44), 20f);
            c = Over(c, Shadow, Soft(Body(10, -10), 10f));
            float body = Body(0, 0);
            c = Over(c, Hex("F6F3EC"), Fill(body));
            c = Over(c, Hex("FFFFFF"), Fill(Circle(x, y, 66, 120, 30)) * Fill(body + 8f));
            // Black patches (fixed blobs so every cow looks the same and reads well).
            float patches = Min(Ellipse(x, y, 58, 130, 22, 30), Ellipse(x, y, 104, 88, 20, 26), Circle(x, y, 70, 58, 18), Ellipse(x, y, 100, 150, 14, 12));
            c = Over(c, Hex("2C2A2B"), Fill(patches + (N(x, y, 10) - 0.5f) * 8f) * Fill(body + 2f));
            // The tail curled beside her.
            c = Over(c, Hex("2C2A2B"), Fill(Capsule(x, y, 92, 26, 122, 16, 4f)));
            c = Over(c, Hex("2C2A2B"), Fill(Circle(x, y, 124, 15, 7f)));
            c = Over(c, Outline, Stroke(body, 4f) * 0.7f);
            // The head: white with a black ear, a pink muzzle, little horns.
            float head = RotRect(x, y, 86, 206, 26, 34, 18, 0.18f);
            c = Over(c, Shadow, Soft(head + 4f, 8f));
            c = Over(c, Hex("FFFFFF"), Fill(head));
            c = Over(c, Hex("F2A9A4"), Fill(Ellipse(x, y, 90, 232, 20, 13)) * Fill(head + 1f));
            c = Over(c, Hex("B86A66"), Fill(Circle(x, y, 82, 234, 3.5f)));
            c = Over(c, Hex("B86A66"), Fill(Circle(x, y, 98, 232, 3.5f)));
            float earL = Ellipse(x, y, 52, 192, 16, 8), earR = Ellipse(x, y, 120, 198, 16, 8);
            c = Over(c, Hex("2C2A2B"), Fill(earL));
            c = Over(c, Hex("F6F3EC"), Fill(earR));
            c = Over(c, Outline, Stroke(earR, 3f) * 0.7f);
            c = Over(c, Hex("E8D9B8"), Fill(Capsule(x, y, 68, 196, 62, 184, 5f)));
            c = Over(c, Hex("E8D9B8"), Fill(Capsule(x, y, 104, 200, 112, 188, 5f)));
            c = Over(c, Hex("2C2A2B"), Fill(Circle(x, y, 74, 212, 4f)));
            c = Over(c, Hex("2C2A2B"), Fill(Circle(x, y, 100, 216, 4f)));
            c = Over(c, Outline, Stroke(head, 4f) * 0.7f);
            return c;
        }

        // One length of farm fence from above: two posts and two rails between them (laid along +X).
        static Color Fence(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float rails = Min(RoundRect(x, y, 128, 24, 124, 5, 3), RoundRect(x, y, 128, 42, 124, 5, 3));
            c = Over(c, Shadow, Soft(rails + 2f, 6f) * Fill(Circle(x, y, 128, 26, 400)));
            c = Over(c, Hex("A87444"), Fill(rails));
            c = Over(c, Hex("C99760"), Fill(RoundRect(x, y, 128, 26, 124, 1.5f, 1f)));
            c = Over(c, Hex("C99760"), Fill(RoundRect(x, y, 128, 44, 124, 1.5f, 1f)));
            c = Over(c, Outline, Stroke(rails, 2.5f) * 0.6f);
            foreach (float px in new[] { 12f, 244f })
            {
                float post = RoundRect(x, y, px, 33, 11, 14, 4);
                c = Over(c, Hex("8A5A32"), Fill(post));
                c = Over(c, Hex("B0804E"), Fill(Circle(x, y, px - 2, 35, 6)));
                c = Over(c, Outline, Stroke(post, 3f) * 0.7f);
            }
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
