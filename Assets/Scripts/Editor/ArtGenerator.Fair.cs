using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Art for the fairground race (Valley Cup race 5): a scenery atlas (tents, stalls, booths, balloons, crowds,
    /// benches, lamps, the circus ring...), the rides seen from above (a carousel, a swing ride, the teacups),
    /// the roller coaster's rails and cars, a trampoline, strings of pennants, the circus tent's striped roof and
    /// its sawdust floor. Same flat, outlined style as the rest.
    /// </summary>
    public static partial class ArtGenerator
    {
        public static readonly string[] FairCells =
        {
            "Tent", "TentBlue", "Stall", "StallGreen",
            "GameBooth", "Balloons", "Popcorn", "Crowd",
            "Bench", "LampPost", "FlowerBed", "Tree",
            "Plaza", "Ring", "Pedestal", "BumperCar",
        };

        /// <summary>UV rect of a fair scenery cell by name (row 0 at the top of the texture).</summary>
        public static Rect FairCell(string name)
        {
            int i = System.Array.IndexOf(FairCells, name);
            int col = i % 4, row = i / 4;
            return new Rect(col * 0.25f, 1f - (row + 1) * 0.25f, 0.25f, 0.25f);
        }

        [MenuItem("SomeGame/Generate Fair Art")]
        public static void GenerateFair()
        {
            World("SceneryFair", 1024, 1024, FairPixel, 128, false);
            Material("SceneryFair", "SceneryFair");
            World("FloorSawdust", 128, 128, SawdustPixel, 32, true);
            World("RoofCircus", 128, 128, CircusRoofPixel, 32, true);
            World("CoasterRails", 128, 128, CoasterRailsPixel, 32, true);
            foreach (var m in new[] { "FloorSawdust", "RoofCircus", "CoasterRails" }) Material(m, m);
            World("CoasterCar", 128, 192, CoasterCar, 128, false);
            World("Trampoline", 256, 256, Trampoline, 128, false);
            World("Bunting", 256, 64, Bunting, 128, false);
            World("CarouselBase", 512, 512, CarouselBase, 128, false);
            World("CarouselTop", 512, 512, CarouselTop, 128, false);
            World("SwingHub", 256, 256, SwingHub, 128, false);
            World("SwingSeats", 512, 512, SwingSeats, 128, false);
            World("TeacupFloor", 512, 512, TeacupFloor, 128, false);
            World("Teacup", 192, 192, Teacup, 128, false);
            AssetDatabase.Refresh();
        }

        static Color FairPixel(float x, float y)
        {
            int col = (int)(x / 256), row = 3 - (int)(y / 256);
            float lx = x - col * 256, ly = y - (3 - row) * 256;
            return FairCells[row * 4 + col] switch
            {
                "Tent" => Tent(lx, ly, Hex("D9412F"), Hex("F6EEDC")),
                "TentBlue" => Tent(lx, ly, Hex("2F6DB5"), Hex("F5C842")),
                "Stall" => Stall(lx, ly, Hex("E2513A")),
                "StallGreen" => Stall(lx, ly, Hex("3E9A57")),
                "GameBooth" => GameBooth(lx, ly),
                "Balloons" => Balloons(lx, ly),
                "Popcorn" => Popcorn(lx, ly),
                "Crowd" => Crowd(lx, ly),
                "Bench" => Bench(lx, ly),
                "LampPost" => LampPost(lx, ly),
                "FlowerBed" => GardenBed(lx, ly),
                "Tree" => RoundTree(lx, ly),
                "Plaza" => Plaza(lx, ly),
                "Ring" => CircusRing(lx, ly),
                "Pedestal" => Pedestal(lx, ly),
                _ => BumperCar(lx, ly),
            };
        }

        // A round striped tent from above: wedges of two colours, a scalloped edge, a flag at the top.
        static Color Tent(float x, float y, Color a, Color b)
        {
            Color c = new(0, 0, 0, 0);
            float edge = Circle(x, y, 128, 128, 104) + Mathf.Abs(Mathf.Sin(Mathf.Atan2(y - 128, x - 128) * 8f)) * 6f;
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 112, 106), 12f));
            float wedge = Mathf.Repeat(Mathf.Atan2(y - 128, x - 128) / (Mathf.PI * 2f) * 12f, 1f);
            Color face = wedge < 0.5f ? a : b;
            // Light from the top left: the roof's far side is darker.
            float shade = Mathf.Clamp01(0.5f + ((x - 128) - (y - 128)) / 300f);
            c = Over(c, Color.Lerp(face, Shade(face, 0.8f), shade), Fill(edge));
            c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 128, 128, 14)));
            c = Over(c, Outline, Stroke(Circle(x, y, 128, 128, 14), 3f));
            c = Over(c, Hex("E2513A"), Fill(Polygon(x, y, new[] { new Vector2(128, 140), new Vector2(160, 150), new Vector2(128, 162) })));
            c = Over(c, Outline, Stroke(edge, 5f) * 0.85f);
            return c;
        }

        // A food stall: a striped awning (the front edge scalloped) over a counter.
        static Color Stall(float x, float y, Color stripe)
        {
            Color c = new(0, 0, 0, 0);
            float body = RoundRect(x, y, 128, 132, 100, 70, 10);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 118, 100, 72, 10), 12f));
            float awning = Mathf.Max(body, -(y - 92 + Mathf.Abs(Mathf.Sin(x / 200f * Mathf.PI * 7f)) * 12f));
            c = Over(c, Hex("8A5A32"), Fill(body));
            bool light = Mathf.Repeat(x / 25f, 2f) < 1f;
            c = Over(c, light ? Hex("F6EEDC") : stripe, Fill(awning));
            c = Over(c, Shade(stripe, 0.75f), Fill(Mathf.Max(awning, -(y - 188f))) * 0.4f);
            c = Over(c, Outline, Stroke(awning, 4f) * 0.8f);
            // The counter with things on it.
            c = Over(c, Hex("C99760"), Fill(RoundRect(x, y, 128, 74, 92, 12, 5)));
            for (int i = 0; i < 5; i++) c = Over(c, i % 2 == 0 ? Hex("F5C842") : Hex("E2513A"), Fill(Circle(x, y, 60 + i * 34, 76, 8)));
            c = Over(c, Outline, Stroke(RoundRect(x, y, 128, 74, 92, 12, 5), 3f) * 0.8f);
            return c;
        }

        // A throwing game: a booth with a target wall and rows of prizes (teddies) on the shelves.
        static Color GameBooth(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = RoundRect(x, y, 128, 128, 104, 84, 12);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 114, 104, 84, 12), 12f));
            c = Over(c, Hex("6C3FA0"), Fill(body));
            c = Over(c, Hex("8A5BC4"), Fill(RoundRect(x, y, 128, 150, 92, 52, 8)));
            for (int i = 0; i < 6; i++)
            {
                float tx = 56 + i * 29f;
                c = Over(c, i % 3 == 0 ? Hex("F2C88E") : i % 3 == 1 ? Hex("F5A3C0") : Hex("9FD6F5"), Fill(Circle(x, y, tx, 168, 11)));
                c = Over(c, Outline, Stroke(Circle(x, y, tx, 168, 11), 2.5f) * 0.7f);
            }
            // Targets.
            for (int i = 0; i < 3; i++)
            {
                float tx = 76 + i * 52f;
                c = Over(c, Color.white, Fill(Circle(x, y, tx, 122, 15)));
                c = Over(c, Hex("E2513A"), Fill(Shell(Circle(x, y, tx, 122, 9), 3f)));
                c = Over(c, Hex("E2513A"), Fill(Circle(x, y, tx, 122, 3)));
            }
            c = Over(c, Hex("F5C842"), Fill(RoundRect(x, y, 128, 60, 100, 12, 5)));
            c = Over(c, Outline, Stroke(body, 5f) * 0.85f);
            return c;
        }

        // A bunch of balloons on strings, gathered at a seller's cart.
        static Color Balloons(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] colors = { Hex("E2513A"), Hex("F5C842"), Hex("3E9ADB"), Hex("5CC26A"), Hex("E77BC0"), Hex("9A6BE0"), Hex("F28C38") };
            c = Over(c, Shadow, Soft(Circle(x, y, 150, 100, 70), 20f) * 0.6f);
            c = Over(c, Hex("8A5A32"), Fill(RoundRect(x, y, 128, 52, 34, 22, 6)));
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.39996f, r = 26f + 50f * Mathf.Sqrt(i / 9f);
                Vector2 p = new(128 + Mathf.Cos(a) * r, 150 + Mathf.Sin(a) * r * 0.8f);
                c = Over(c, Hex("5A4636"), Fill(Capsule(x, y, 128, 60, p.x, p.y, 1.2f)) * 0.7f);
            }
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.39996f, r = 26f + 50f * Mathf.Sqrt(i / 9f);
                Vector2 p = new(128 + Mathf.Cos(a) * r, 150 + Mathf.Sin(a) * r * 0.8f);
                float b = Ellipse(x, y, p.x, p.y, 22, 26);
                c = Over(c, colors[i % colors.Length], Fill(b));
                c = Over(c, White(0.6f), Fill(Ellipse(x, y, p.x - 7, p.y + 9, 6, 8)));
                c = Over(c, Outline, Stroke(b, 3f) * 0.8f);
            }
            return c;
        }

        // A popcorn cart: red with white stripes, a glass box of popcorn, a little roof.
        static Color Popcorn(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = RoundRect(x, y, 128, 120, 70, 82, 14);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 106, 70, 82, 14), 12f));
            c = Over(c, Hex("D9412F"), Fill(body));
            c = Over(c, Hex("F6EEDC"), Fill(body) * (Mathf.Repeat(x / 20f, 2f) < 1f ? 1f : 0f) * Fill(-(y - 90f)));
            float glass = RoundRect(x, y, 128, 146, 54, 44, 8);
            c = Over(c, Hex("CFEAF5"), Fill(glass));
            for (int i = 0; i < 26; i++) c = Over(c, Hex("FFF3C4"), Fill(Circle(x, y, 84 + Hash(i, 1) * 88, 110 + Hash(i, 2) * 70, 9)));
            c = Over(c, Outline, Stroke(glass, 3f) * 0.7f);
            c = Over(c, Outline, Stroke(body, 5f) * 0.85f);
            c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 74, 44, 14)));
            c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 182, 44, 14)));
            return c;
        }

        // A park bench with a lamp beside it.
        static Color Bench(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float seat = RoundRect(x, y, 128, 128, 90, 26, 6);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 138, 116, 90, 28, 6), 10f));
            c = Over(c, Hex("3E6B4E"), Fill(RoundRect(x, y, 128, 128, 96, 32, 8)));
            for (int i = 0; i < 4; i++) c = Over(c, Hex("C99760"), Fill(RoundRect(x, y, 128, 108 + i * 13, 88, 4.5f, 2)));
            c = Over(c, Outline, Stroke(seat, 4f) * 0.8f);
            return c;
        }

        // A lamp post with a round glowing lantern (seen from above: a glow round a lamp).
        static Color LampPost(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(1f, 0.92f, 0.55f, 0.55f), Mathf.Clamp01(1f - Dist(x, y, 128, 128) / 110f));
            c = Over(c, Shadow, Soft(Circle(x, y, 146, 110, 30), 10f));
            c = Over(c, Hex("2E3A40"), Fill(Circle(x, y, 128, 128, 32)));
            c = Over(c, Hex("FFE58A"), Fill(Circle(x, y, 128, 128, 22)));
            c = Over(c, Color.white, Fill(Circle(x, y, 122, 134, 8)));
            return c;
        }

        // A round cobbled plaza (flat, under everything).
        static Color Plaza(float x, float y)
        {
            float d = Circle(x, y, 128, 128, 124);
            Color c = new(0, 0, 0, 0);
            float r = Dist(x, y, 128, 128), a = Mathf.Atan2(y - 128, x - 128);
            float ring = Mathf.Repeat(r / 14f, 1f), seg = Mathf.Repeat(a * r / 16f, 1f);
            Color stone = Color.Lerp(Hex("C9BFAE"), Hex("DCD3C3"), Hash((int)(r / 14f), (int)(a * r / 16f)));
            if (ring < 0.12f || seg < 0.1f) stone = Hex("A99E8C");
            c = Over(c, stone, Fill(d));
            c = Over(c, Hex("8E8473"), Stroke(d, 5f));
            return c;
        }

        // The circus ring: a red and white curb round a sawdust floor with a star in the middle.
        static Color CircusRing(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float outer = Circle(x, y, 128, 128, 120);
            float curb = Shell(Circle(x, y, 128, 128, 110), 10f);
            c = Over(c, Hex("E3C891"), Fill(outer));
            float a = Mathf.Atan2(y - 128, x - 128);
            c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 24f, 1f) < 0.5f ? Hex("D9412F") : Hex("F6EEDC"), Fill(curb));
            c = Over(c, Outline, Stroke(curb, 3f) * 0.6f);
            c = Over(c, Hex("F5C842"), Fill(Star(x, y, 128, 128, 40, 17, 5, 0.3f)));
            c = Over(c, Shade(Hex("F5C842"), 0.7f), Stroke(Star(x, y, 128, 128, 40, 17, 5, 0.3f), 3f));
            return c;
        }

        // A circus pedestal: a round drum with stars, for the elephants that are off today.
        static Color Pedestal(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 142, 114, 70), 10f));
            float drum = Circle(x, y, 128, 128, 70);
            c = Over(c, Hex("2F6DB5"), Fill(drum));
            c = Over(c, Hex("F5C842"), Fill(Shell(Circle(x, y, 128, 128, 58), 6f)));
            c = Over(c, Hex("F6EEDC"), Fill(Star(x, y, 128, 128, 34, 15, 5, 0.3f)));
            c = Over(c, Outline, Stroke(drum, 5f) * 0.85f);
            return c;
        }

        // A parked bumper car from above: a round-nosed tub on a fat rubber ring, a pole to the ceiling, a driver.
        static Color BumperCar(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 70, 90, 50), 12f));
            float ring = RoundRect(x, y, 128, 128, 70, 92, 52);
            c = Over(c, Hex("2E2A28"), Fill(ring));
            float tub = RoundRect(x, y, 128, 134, 54, 72, 40);
            c = Over(c, Hex("2FA7D9"), Fill(tub));
            c = Over(c, Hex("7FD3F2"), Fill(RoundRect(x, y, 114, 160, 26, 30, 20)) * Fill(tub + 8f));
            c = Over(c, Hex("F5C842"), Fill(RoundRect(x, y, 128, 110, 34, 26, 12)));
            c = Over(c, Hex("6B3F22"), Fill(Circle(x, y, 128, 104, 14)));
            c = Over(c, Hex("C9302B"), Fill(Circle(x, y, 128, 66, 9)));
            c = Over(c, Outline, Stroke(tub, 4f) * 0.8f);
            c = Over(c, Outline, Stroke(ring, 4f) * 0.85f);
            return c;
        }

        // ================================================================== tiled floors and rails

        static Color SawdustPixel(float x, float y)
        {
            Color c = Color.Lerp(Hex("E0C48E"), Hex("D2B37A"), Noise(x / 16f, y / 16f, 8));
            float speck = Hash((int)(x / 3f), (int)(y / 3f));
            if (speck > 0.9f) c = Hex("F0DCAE");
            else if (speck < 0.06f) c = Hex("B8945C");
            return c;
        }

        // Red and cream canvas in broad stripes, with a seam and a soft fold along each stripe.
        static Color CircusRoofPixel(float x, float y)
        {
            float u = Mathf.Repeat(x, 64f);
            bool red = u < 32f;
            Color c = red ? Hex("D23E2C") : Hex("F4EAD6");
            float fold = Mathf.Abs(Mathf.Repeat(u, 32f) - 16f) / 16f;
            c = Color.Lerp(c, Shade(c, 0.86f), fold * 0.6f);
            if (Mathf.Repeat(u, 32f) < 1.5f) c = Shade(c, 0.7f);
            return c;
        }

        // Coaster rails along x: two red steel tubes with white crossties (transparent between).
        static Color CoasterRailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float tie = Mathf.Abs(Mathf.Repeat(x, 64f) - 32f) - 7f;
            c = Over(c, Hex("E8E4DA"), Fill(Mathf.Max(tie, Mathf.Abs(y - 64f) - 50f)));
            c = Over(c, Outline, Stroke(Mathf.Max(tie, Mathf.Abs(y - 64f) - 50f), 3f) * 0.7f);
            foreach (float ry in new[] { 22f, 106f })
            {
                float rail = Mathf.Abs(y - ry) - 9f;
                c = Over(c, Hex("C9302B"), Fill(rail));
                c = Over(c, Hex("F07A5E"), Fill(Mathf.Abs(y - ry - 3f) - 2.5f));
                c = Over(c, Outline, Stroke(rail, 3f) * 0.8f);
            }
            return c;
        }

        // ================================================================== sprites

        // A coaster car from above (front at +Y): yellow with a red stripe, two riders with their arms up.
        static Color CoasterCar(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = RoundRect(x, y, 64, 96, 50, 84, 26);
            c = Over(c, Hex("F5C842"), Fill(body));
            c = Over(c, Hex("E2513A"), Fill(Mathf.Max(body, Mathf.Abs(x - 64) - 10f)));
            foreach (float sy in new[] { 70f, 128f })
            {
                c = Over(c, Hex("2E2A28"), Fill(RoundRect(x, y, 64, sy, 38, 22, 10)));
                c = Over(c, Hex("F2C49B"), Fill(Circle(x, y, 46, sy, 11)));
                c = Over(c, Hex("6B3F22"), Fill(Circle(x, y, 82, sy, 11)));
                c = Over(c, Hex("F2C49B"), Fill(Capsule(x, y, 34, sy + 6, 22, sy + 22, 4.5f)));
                c = Over(c, Hex("F2C49B"), Fill(Capsule(x, y, 94, sy + 6, 106, sy + 22, 4.5f)));
            }
            c = Over(c, Outline, Stroke(body, 5f) * 0.85f);
            return c;
        }

        // A round trampoline: a dark mat with a star, a padded blue and yellow rim, springs.
        static Color Trampoline(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 116, 118), 12f));
            float rim = Circle(x, y, 128, 128, 118);
            float a = Mathf.Atan2(y - 128, x - 128);
            c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 12f, 1f) < 0.5f ? Hex("2F6DB5") : Hex("F5C842"), Fill(rim));
            float mat = Circle(x, y, 128, 128, 86);
            c = Over(c, Hex("B9BDC6"), Fill(Shell(Circle(x, y, 128, 128, 94), 6f)) * (Mathf.Repeat(a / (Mathf.PI * 2f) * 40f, 1f) < 0.5f ? 1f : 0.3f));
            c = Over(c, Hex("2B2F36"), Fill(mat));
            c = Over(c, Hex("3A404A"), Fill(Circle(x, y, 112, 146, 48)) * Fill(mat + 6f));
            c = Over(c, Hex("F5C842"), Fill(Star(x, y, 128, 128, 40, 17, 5, 0.3f)));
            c = Over(c, Outline, Stroke(mat, 3f));
            c = Over(c, Outline, Stroke(rim, 5f) * 0.85f);
            return c;
        }

        // A string of pennants along x (tiled along the road): a rope, triangles in bright colours, posts at the ends.
        static Color Bunting(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] colors = { Hex("E2513A"), Hex("F5C842"), Hex("3E9ADB"), Hex("5CC26A"), Hex("E77BC0") };
            float sag = 40f - Mathf.Sin(x / 256f * Mathf.PI) * 6f;
            for (int i = 0; i < 8; i++)
            {
                float cx = 16 + i * 32f, top = 40f - Mathf.Sin(cx / 256f * Mathf.PI) * 6f;
                float flag = Polygon(x, y, new[] { new Vector2(cx - 12, top), new Vector2(cx + 12, top), new Vector2(cx, top - 26) });
                c = Over(c, new Color(0f, 0f, 0f, 0.18f), Fill(flag - 0f) * 0f);
                c = Over(c, colors[i % colors.Length], Fill(flag));
                c = Over(c, Outline, Stroke(flag, 2f) * 0.6f);
            }
            c = Over(c, Hex("5A4636"), Fill(Mathf.Abs(y - sag) - 1.6f));
            foreach (float px in new[] { 6f, 250f })
            {
                float post = Circle(x, y, px, 40, 6);
                c = Over(c, Hex("F6EEDC"), Fill(post));
                c = Over(c, Outline, Stroke(post, 2.5f));
            }
            return c;
        }

        // The carousel's platform from above, seen under its turning top: a wooden ring with a scalloped edge.
        static Color CarouselBase(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 276, 232, 236), 18f));
            float deck = Circle(x, y, 256, 256, 240);
            c = Over(c, Hex("B88A58"), Fill(deck));
            c = Over(c, Hex("9A6E42"), Fill(Shell(Circle(x, y, 256, 256, 228), 4f)));
            c = Over(c, Outline, Stroke(deck, 6f) * 0.85f);
            return c;
        }

        // The carousel's top: horses round the rim (pairs of white and brown), a striped canopy with a gold
        // trim and light bulbs, and a flag on top. It turns.
        static Color CarouselTop(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float r = Dist(x, y, 256, 256), a = Mathf.Atan2(y - 256, x - 256);
            // Horses on poles round the rim.
            for (int i = 0; i < 12; i++)
            {
                float ha = i * Mathf.PI * 2f / 12f;
                Vector2 dir = new(Mathf.Cos(ha), Mathf.Sin(ha)), side = new(-dir.y, dir.x);
                Vector2 p = new Vector2(256, 256) + dir * 214f;
                float body = Capsule(x, y, p.x - side.x * 20, p.y - side.y * 20, p.x + side.x * 20, p.y + side.y * 20, 13f);
                float head = Circle(x, y, p.x + side.x * 30 + dir.x * 4, p.y + side.y * 30 + dir.y * 4, 10f);
                float horse = Mathf.Min(body, head);
                Color coat = i % 2 == 0 ? Hex("F6F3EC") : Hex("A8673A");
                c = Over(c, coat, Fill(horse));
                c = Over(c, i % 3 == 0 ? Hex("E2513A") : i % 3 == 1 ? Hex("3E9ADB") : Hex("5CC26A"), Fill(Circle(x, y, p.x, p.y, 8f)));
                c = Over(c, Outline, Stroke(horse, 3f) * 0.8f);
            }
            // The canopy.
            float canopy = Circle(x, y, 256, 256, 176) + Mathf.Abs(Mathf.Sin(a * 8f)) * 7f;
            bool stripe = Mathf.Repeat(a / (Mathf.PI * 2f) * 16f, 1f) < 0.5f;
            Color face = stripe ? Hex("D9412F") : Hex("F6EEDC");
            c = Over(c, Color.Lerp(face, Shade(face, 0.82f), Mathf.Clamp01(r / 176f)), Fill(canopy));
            c = Over(c, Hex("E8B93A"), Fill(Shell(canopy + 9f, 7f)));
            for (int i = 0; i < 24; i++)
            {
                float ba = i * Mathf.PI * 2f / 24f;
                c = Over(c, Hex("FFF6C8"), Fill(Circle(x, y, 256 + Mathf.Cos(ba) * 164f, 256 + Mathf.Sin(ba) * 164f, 4.5f)));
            }
            c = Over(c, Outline, Stroke(canopy, 5f) * 0.85f);
            c = Over(c, Hex("E8B93A"), Fill(Circle(x, y, 256, 256, 26)));
            c = Over(c, Outline, Stroke(Circle(x, y, 256, 256, 26), 4f));
            c = Over(c, Hex("3E9ADB"), Fill(Polygon(x, y, new[] { new Vector2(256, 262), new Vector2(304, 278), new Vector2(256, 294) })));
            return c;
        }

        // The swing ride's top: a round, striped roof on the tower.
        static Color SwingHub(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 144, 112, 110), 12f));
            float roof = Circle(x, y, 128, 128, 110);
            float a = Mathf.Atan2(y - 128, x - 128);
            c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 10f, 1f) < 0.5f ? Hex("3E9ADB") : Hex("F5C842"), Fill(roof));
            c = Over(c, Hex("E2513A"), Fill(Shell(Circle(x, y, 128, 128, 100), 6f)));
            c = Over(c, Hex("F6EEDC"), Fill(Circle(x, y, 128, 128, 20)));
            c = Over(c, Outline, Stroke(roof, 5f) * 0.85f);
            return c;
        }

        // The swing ride's chairs flying round on their chains (the hub sits on top of this).
        static Color SwingSeats(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] colors = { Hex("E2513A"), Hex("F5C842"), Hex("3E9ADB"), Hex("5CC26A"), Hex("E77BC0"), Hex("9A6BE0") };
            for (int i = 0; i < 16; i++)
            {
                float sa = i * Mathf.PI * 2f / 16f;
                Vector2 dir = new(Mathf.Cos(sa), Mathf.Sin(sa)), side = new(-dir.y, dir.x);
                Vector2 seat = new Vector2(256, 256) + dir * 218f - side * 12f; // flung out, trailing a little
                c = Over(c, Hex("5A4636"), Fill(Capsule(x, y, 256 + dir.x * 80, 256 + dir.y * 80, seat.x, seat.y, 1.6f)) * 0.8f);
                c = Over(c, Shadow, Soft(Circle(x, y, seat.x + 10, seat.y - 12, 18), 8f));
                float chair = Circle(x, y, seat.x, seat.y, 17);
                c = Over(c, colors[i % colors.Length], Fill(chair));
                c = Over(c, Hex("F2C49B"), Fill(Circle(x, y, seat.x, seat.y, 8)));
                c = Over(c, Outline, Stroke(chair, 3f) * 0.85f);
            }
            return c;
        }

        // The teacups' turning floor: a pastel platform with a flower pattern.
        static Color TeacupFloor(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 272, 236, 238), 16f));
            float floor = Circle(x, y, 256, 256, 240);
            float a = Mathf.Atan2(y - 256, x - 256);
            c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 12f, 1f) < 0.5f ? Hex("F7D9E6") : Hex("D9EEF7"), Fill(floor));
            c = Over(c, Hex("F5C842"), Fill(Shell(Circle(x, y, 256, 256, 226), 6f)));
            c = Over(c, Hex("E77BC0"), Fill(Circle(x, y, 256, 256, 40)));
            c = Over(c, Hex("F6EEDC"), Fill(Star(x, y, 256, 256, 30, 14, 6, 0f)));
            c = Over(c, Outline, Stroke(floor, 6f) * 0.85f);
            return c;
        }

        // A teacup from above: a round cup with dots, a handle, two riders inside.
        static Color Teacup(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 104, 86, 74), 10f));
            float handle = Shell(Circle(x, y, 168, 96, 14), 6f);
            c = Over(c, Hex("F07AAE"), Fill(handle));
            float cup = Circle(x, y, 96, 96, 74);
            c = Over(c, Hex("F07AAE"), Fill(cup));
            for (int i = 0; i < 10; i++)
            {
                float da = i * Mathf.PI * 2f / 10f;
                c = Over(c, Color.white, Fill(Circle(x, y, 96 + Mathf.Cos(da) * 64, 96 + Mathf.Sin(da) * 64, 5)));
            }
            c = Over(c, Hex("FBE3EE"), Fill(Circle(x, y, 96, 96, 54)));
            c = Over(c, Hex("6B3F22"), Fill(Circle(x, y, 78, 104, 15)));
            c = Over(c, Hex("F2C49B"), Fill(Circle(x, y, 114, 88, 15)));
            c = Over(c, Outline, Stroke(cup, 5f) * 0.85f);
            c = Over(c, Outline, Stroke(handle, 3f) * 0.7f);
            return c;
        }
    }
}
