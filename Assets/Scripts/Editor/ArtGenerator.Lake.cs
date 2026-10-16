using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Art for the lake race (Valley Cup race 7): a scenery atlas for a summer day at the lake (parasols, towels,
    /// beach huts, a lifeguard chair, sandcastles, kayaks, picnics, camping tents, a campfire, the lighthouse, sandy
    /// beaches, plus the usual trees, reeds and spectators) and the things on the water (sailboats, rowboats, swan
    /// pedal boats, buoys, a duck family). Same flat, outlined style as the rest.
    /// </summary>
    public static partial class ArtGenerator
    {
        public static readonly string[] LakeCells =
        {
            "Parasol", "ParasolBlue", "Towels", "BeachHut",
            "Lifeguard", "Sandcastle", "Kayaks", "Picnic",
            "CampTent", "CampTentGreen", "Campfire", "Lighthouse",
            "Beach", "Pine", "Tree", "Bushes",
            "Reeds", "Crowd", "Flowers", "Rock",
        };

        const int LakeRows = 5;

        /// <summary>UV rect of a lake scenery cell by name (row 0 at the top of the texture).</summary>
        public static Rect LakeCell(string name)
        {
            int i = System.Array.IndexOf(LakeCells, name);
            int col = i % 4, row = i / 4;
            return new Rect(col * 0.25f, 1f - (row + 1) / (float)LakeRows, 0.25f, 1f / LakeRows);
        }

        [MenuItem("SomeGame/Generate Lake Art")]
        public static void GenerateLake()
        {
            World("SceneryLake", 1024, 256 * LakeRows, LakePixel, 128, false);
            Material("SceneryLake", "SceneryLake");
            World("Sailboat", 192, 288, Sailboat, 128, false);
            World("Rowboat", 128, 192, Rowboat, 128, false);
            World("SwanBoat", 192, 192, SwanBoat, 128, false);
            World("MarkerBuoy", 96, 96, MarkerBuoy, 128, false);
            World("Ducks", 256, 128, Ducks, 128, false);
            AssetDatabase.Refresh();
        }

        // A rounded rectangle turned by an angle in degrees.
        static float RotDeg(float x, float y, float cx, float cy, float hw, float hh, float r, float degrees) =>
            RotRect(x, y, cx, cy, hw, hh, r, degrees * Mathf.Deg2Rad);

        static Color LakePixel(float x, float y)
        {
            int col = (int)(x / 256), row = LakeRows - 1 - (int)(y / 256);
            float lx = x - col * 256, ly = y - (LakeRows - 1 - row) * 256;
            return LakeCells[row * 4 + col] switch
            {
                "Parasol" => Parasol(lx, ly, Hex("E2513A")),
                "ParasolBlue" => Parasol(lx, ly, Hex("2F8FD8")),
                "Towels" => Towels(lx, ly),
                "BeachHut" => BeachHut(lx, ly),
                "Lifeguard" => Lifeguard(lx, ly),
                "Sandcastle" => Sandcastle(lx, ly),
                "Kayaks" => Kayaks(lx, ly),
                "Picnic" => Picnic(lx, ly),
                "CampTent" => CampTent(lx, ly, Hex("F08A24")),
                "CampTentGreen" => CampTent(lx, ly, Hex("3E9A57")),
                "Campfire" => Campfire(lx, ly),
                "Lighthouse" => Lighthouse(lx, ly),
                "Beach" => BeachPatch(lx, ly),
                "Pine" => Pine(lx, ly),
                "Tree" => RoundTree(lx, ly),
                "Bushes" => Bushes(lx, ly),
                "Reeds" => Reeds(lx, ly),
                "Crowd" => Crowd(lx, ly),
                "Flowers" => Flowers(lx, ly),
                _ => Rock(lx, ly),
            };
        }

        // A beach umbrella from above: eight wedges of white and a colour, a scalloped rim, a towel peeking out
        // from under it and the shadow on the sand.
        static Color Parasol(float x, float y, Color colour)
        {
            Color c = new(0, 0, 0, 0);
            float towel = RotDeg(x, y, 150, 92, 36, 70, 6, -25f);
            c = Over(c, Shadow, Soft(RotDeg(x, y, 156, 86, 36, 70, 6, -25f), 6f));
            bool stripe = Mathf.Repeat((x * 0.42f + y * 0.9f) / 22f, 2f) < 1f;
            c = Over(c, stripe ? Hex("F5C842") : Hex("FFFFFF"), Fill(towel));
            c = Over(c, Outline, Stroke(towel, 3f) * 0.6f);
            float a = Mathf.Atan2(y - 140, x - 116);
            float rim = Circle(x, y, 116, 140, 92) + Mathf.Abs(Mathf.Sin(a * 4f)) * 7f;
            c = Over(c, Shadow, Soft(Circle(x, y, 132, 120, 92), 12f));
            float wedge = Mathf.Repeat(a / (Mathf.PI * 2f) * 8f, 1f);
            Color face = wedge < 0.5f ? colour : Hex("FFFFFF");
            float shade = Mathf.Clamp01(0.5f + ((x - 116) - (y - 140)) / 260f);
            c = Over(c, Color.Lerp(face, Shade(face, 0.82f), shade), Fill(rim));
            // Ribs from the centre to each point of the rim.
            float rib = Mathf.Abs(Mathf.Sin(a * 4f)) * Dist(x, y, 116, 140);
            c = Over(c, Shade(colour, 0.7f), Fill(rib - 1.2f) * Fill(rim) * 0.5f);
            c = Over(c, Hex("8A5A32"), Fill(Circle(x, y, 116, 140, 9)));
            c = Over(c, Outline, Stroke(rim, 5f) * 0.85f);
            return c;
        }

        // Two beach towels side by side, a beach ball and a pair of flip-flops.
        static Color Towels(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            (float cx, float cy, float ang, Color a, Color b)[] towels =
            {
                (88, 128, 8f, Hex("2F8FD8"), Hex("FFFFFF")),
                (168, 124, -6f, Hex("E86FB0"), Hex("F5C842")),
            };
            foreach (var t in towels)
            {
                float d = RotDeg(x, y, t.cx, t.cy, 32, 92, 5, t.ang);
                c = Over(c, Shadow, Soft(RotDeg(x, y, t.cx + 6, t.cy - 6, 32, 92, 5, t.ang), 6f));
                float along = (y - t.cy) * Mathf.Cos(t.ang * Mathf.Deg2Rad) - (x - t.cx) * Mathf.Sin(t.ang * Mathf.Deg2Rad);
                c = Over(c, Mathf.Repeat(along / 18f, 2f) < 1f ? t.a : t.b, Fill(d));
                c = Over(c, Outline, Stroke(d, 3f) * 0.7f);
            }
            // The ball.
            float ball = Circle(x, y, 210, 214, 22);
            c = Over(c, Shadow, Soft(Circle(x, y, 216, 208, 22), 5f));
            float ba = Mathf.Repeat(Mathf.Atan2(y - 214, x - 210) / (Mathf.PI * 2f) * 6f, 3f);
            c = Over(c, ba < 1f ? Hex("E2513A") : ba < 2f ? Hex("FFFFFF") : Hex("2F8FD8"), Fill(ball));
            c = Over(c, Color.white, Fill(Circle(x, y, 210, 214, 6)));
            c = Over(c, Outline, Stroke(ball, 3f) * 0.8f);
            // Flip-flops.
            foreach (float fx in new[] { 40f, 58f })
            {
                float f = Ellipse(x, y, fx, 30, 8, 18);
                c = Over(c, Hex("F08A24"), Fill(f));
                c = Over(c, Outline, Stroke(f, 2.5f) * 0.7f);
            }
            return c;
        }

        // A little beach hut from above: a pitched roof in two shades with a ridge, wooden steps in front.
        static Color BeachHut(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 132, 86, 80, 6), 12f));
            // The deck in front.
            float deck = RoundRect(x, y, 128, 40, 70, 22, 4);
            Color wood = Color.Lerp(Hex("B9874E"), Hex("CFA068"), Hash((int)(x / 18f), 3));
            c = Over(c, Mathf.Repeat(x, 18f) < 2f ? Hex("8A5A32") : wood, Fill(deck));
            c = Over(c, Outline, Stroke(deck, 3f) * 0.7f);
            float roof = RoundRect(x, y, 128, 142, 86, 82, 6);
            bool left = x < 128f;
            Color tone = left ? Hex("5BB5E8") : Hex("3E93C8");
            // Painted planks running down the roof.
            if (Mathf.Repeat(y, 22f) < 2.5f) tone = Shade(tone, 0.85f);
            c = Over(c, tone, Fill(roof));
            c = Over(c, Hex("FFFFFF"), Fill(Mathf.Abs(x - 128f) - 5f) * Fill(roof)); // the ridge
            c = Over(c, Hex("FFFFFF"), Fill(Shell(roof + 6f, 3f)) * 0.9f); // white trim
            c = Over(c, Outline, Stroke(roof, 5f) * 0.85f);
            return c;
        }

        // A lifeguard chair from above: a wooden platform with a red and white umbrella and a red rescue buoy.
        static Color Lifeguard(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 112, 70, 70, 6), 14f));
            float deck = RoundRect(x, y, 128, 128, 66, 66, 6);
            Color wood = Color.Lerp(Hex("D8B07A"), Hex("E6C48E"), Hash(1, (int)(y / 16f)));
            c = Over(c, Mathf.Repeat(y, 16f) < 2f ? Hex("A07848") : wood, Fill(deck));
            c = Over(c, Outline, Stroke(deck, 4f) * 0.8f);
            // The ladder down the front.
            for (int i = 0; i < 4; i++) c = Over(c, Hex("8A5A32"), Fill(RoundRect(x, y, 128, 50 - i * 12, 28, 3, 1.5f)));
            c = Over(c, Hex("8A5A32"), Fill(RoundRect(x, y, 98, 34, 3, 26, 1.5f)));
            c = Over(c, Hex("8A5A32"), Fill(RoundRect(x, y, 158, 34, 3, 26, 1.5f)));
            // The umbrella.
            float a = Mathf.Atan2(y - 150, x - 140);
            float rim = Circle(x, y, 140, 150, 58);
            c = Over(c, Shadow, Soft(Circle(x, y, 150, 138, 58), 8f));
            c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 8f, 1f) < 0.5f ? Hex("E2513A") : Hex("FFFFFF"), Fill(rim));
            c = Over(c, Outline, Stroke(rim, 4f) * 0.85f);
            // The rescue ring hanging on the side.
            float ring = Shell(Circle(x, y, 60, 128, 18), 6f);
            c = Over(c, Mathf.Repeat(Mathf.Atan2(y - 128, x - 60) / (Mathf.PI * 2f) * 8f, 1f) < 0.5f ? Hex("E2513A") : Hex("FFFFFF"), Fill(ring));
            c = Over(c, Outline, Stroke(ring, 2.5f) * 0.7f);
            return c;
        }

        // A sandcastle from above: a square keep with four round towers, a moat ring and little flags.
        static Color Sandcastle(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float moat = Shell(RoundRect(x, y, 128, 128, 92, 92, 30), 8f);
            c = Over(c, Hex("7FC4EC"), Fill(moat));
            c = Over(c, Hex("C9A86C"), Stroke(moat, 3f) * 0.6f);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 140, 116, 60, 60, 6), 10f));
            float keep = RoundRect(x, y, 128, 128, 52, 52, 4);
            c = Over(c, Hex("E6C98E"), Fill(keep));
            // Battlements.
            if (Mathf.Repeat(x + y, 20f) < 10f) c = Over(c, Hex("D4B271"), Fill(Shell(keep + 5f, 4f)));
            c = Over(c, Hex("F0DCA8"), Fill(RoundRect(x, y, 128, 128, 26, 26, 3)));
            c = Over(c, Outline, Stroke(keep, 3.5f) * 0.75f);
            foreach (var (tx, ty) in new[] { (76f, 76f), (180f, 76f), (76f, 180f), (180f, 180f) })
            {
                float tower = Circle(x, y, tx, ty, 20);
                c = Over(c, Hex("E6C98E"), Fill(tower));
                c = Over(c, Hex("F0DCA8"), Fill(Circle(x, y, tx - 4, ty + 4, 9)));
                c = Over(c, Outline, Stroke(tower, 3f) * 0.75f);
            }
            c = Over(c, Hex("E2513A"), Fill(Polygon(x, y, new[] { new Vector2(128, 132), new Vector2(156, 142), new Vector2(128, 152) })));
            c = Over(c, Hex("5A3A22"), Fill(Circle(x, y, 128, 132, 3)));
            return c;
        }

        // Three kayaks side by side on the sand, each with its cockpit and a paddle.
        static Color Kayaks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] colours = { Hex("F2C230"), Hex("E2513A"), Hex("2BB3A3") };
            for (int i = 0; i < 3; i++)
            {
                float kx = 64 + i * 64;
                float hull = Ellipse(x, y, kx, 128, 22, 112);
                c = Over(c, Shadow, Soft(Ellipse(x, y, kx + 6, 120, 22, 112), 6f));
                c = Over(c, colours[i], Fill(hull));
                c = Over(c, Shade(colours[i], 0.82f), Fill(Mathf.Max(hull, x - kx)) * 0.6f);
                c = Over(c, Hex("2E2A28"), Fill(Ellipse(x, y, kx, 132, 11, 22)));
                c = Over(c, Outline, Stroke(hull, 3.5f) * 0.8f);
            }
            float paddle = RotDeg(x, y, 128, 128, 4, 116, 2, 60f);
            c = Over(c, Hex("8A5A32"), Fill(paddle));
            c = Over(c, Hex("2E2A28"), Fill(RotDeg(x, y, 128 + 100 * 0.87f, 128 - 100 * 0.5f, 10, 20, 4, 60f)));
            c = Over(c, Hex("2E2A28"), Fill(RotDeg(x, y, 128 - 100 * 0.87f, 128 + 100 * 0.5f, 10, 20, 4, 60f)));
            return c;
        }

        // A picnic: a red and white checked blanket, a basket, plates and a watermelon.
        static Color Picnic(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float blanket = RotDeg(x, y, 128, 128, 96, 80, 6, 10f);
            c = Over(c, Shadow, Soft(RotDeg(x, y, 136, 120, 96, 80, 6, 10f), 6f));
            bool cx = Mathf.Repeat(x / 24f, 2f) < 1f, cy = Mathf.Repeat(y / 24f, 2f) < 1f;
            c = Over(c, cx ^ cy ? Hex("E2513A") : cx && cy ? Hex("FFFFFF") : Hex("F2A49A"), Fill(blanket));
            c = Over(c, Outline, Stroke(blanket, 3f) * 0.7f);
            float basket = RoundRect(x, y, 94, 140, 34, 26, 6);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 100, 134, 34, 26, 6), 5f));
            c = Over(c, Mathf.Repeat(x + y, 10f) < 5f ? Hex("C99760") : Hex("A87444"), Fill(basket));
            c = Over(c, Hex("8A5A32"), Fill(Shell(Ellipse(x, y, 94, 140, 24, 6), 2.5f)));
            c = Over(c, Outline, Stroke(basket, 3f) * 0.8f);
            foreach (var (px, py) in new[] { (160f, 156f), (170f, 100f) })
            {
                c = Over(c, Color.white, Fill(Circle(x, y, px, py, 18)));
                c = Over(c, Hex("DDE6EA"), Fill(Shell(Circle(x, y, px, py, 12), 1.5f)));
                c = Over(c, Outline, Stroke(Circle(x, y, px, py, 18), 2.5f) * 0.6f);
            }
            float melon = Circle(x, y, 118, 86, 20);
            c = Over(c, Hex("3E9A57"), Fill(melon));
            c = Over(c, Hex("E8505B"), Fill(Circle(x, y, 118, 86, 14)));
            for (int i = 0; i < 5; i++) c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 112 + (i % 3) * 6, 82 + (i / 3) * 8, 1.8f)));
            c = Over(c, Outline, Stroke(melon, 2.5f) * 0.8f);
            return c;
        }

        // A camping tent from above: a ridge down the middle, two shaded halves, the door flap open, guy lines.
        static Color CampTent(float x, float y, Color colour)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (gx, gy, hx, hy) in new[] { (40f, 40f, 84f, 74f), (216f, 40f, 172f, 74f), (40f, 216f, 84f, 182f), (216f, 216f, 172f, 182f) })
            {
                c = Over(c, Hex("E6E0D0"), Fill(Capsule(x, y, gx, gy, hx, hy, 1.5f)) * 0.9f);
                c = Over(c, Hex("6B4A2E"), Fill(Circle(x, y, gx, gy, 4)));
            }
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 116, 74, 92, 8), 12f));
            float tent = RoundRect(x, y, 128, 128, 74, 92, 8);
            c = Over(c, x < 128f ? colour : Shade(colour, 0.78f), Fill(tent));
            c = Over(c, Shade(colour, 0.6f), Fill(Mathf.Abs(x - 128f) - 3f) * Fill(tent));
            // The door at the front, a dark triangle with the flap folded back.
            float door = Polygon(x, y, new[] { new Vector2(98, 36), new Vector2(158, 36), new Vector2(128, 96) });
            c = Over(c, Hex("3A2E2A"), Fill(door));
            c = Over(c, Shade(colour, 1.15f), Fill(Polygon(x, y, new[] { new Vector2(128, 96), new Vector2(158, 36), new Vector2(176, 52) })));
            c = Over(c, Outline, Stroke(tent, 4.5f) * 0.85f);
            return c;
        }

        // A campfire: logs crossed in a ring of stones, flames and a warm glow, two log seats.
        static Color Campfire(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(1f, 0.7f, 0.3f, 0.35f), Mathf.Clamp01(1f - Dist(x, y, 128, 128) / 110f));
            foreach (var (sx, sy) in new[] { (128f, 220f), (128f, 36f) })
            {
                float seat = RoundRect(x, y, sx, sy, 60, 14, 12);
                c = Over(c, Shadow, Soft(RoundRect(x, y, sx + 4, sy - 4, 60, 14, 12), 4f));
                c = Over(c, Hex("8A5A32"), Fill(seat));
                c = Over(c, Hex("C99760"), Fill(Ellipse(x, y, sx - 56, sy, 6, 12)));
                c = Over(c, Outline, Stroke(seat, 3f) * 0.7f);
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                float s = Circle(x, y, 128 + Mathf.Cos(a) * 52, 128 + Mathf.Sin(a) * 52, 13);
                c = Over(c, Color.Lerp(Hex("8C877D"), Hex("B5AFA4"), Hash(i, 2)), Fill(s));
                c = Over(c, Outline, Stroke(s, 2.5f) * 0.6f);
            }
            c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 128, 128, 40)));
            c = Over(c, Hex("6B4A2E"), Fill(RotDeg(x, y, 128, 128, 8, 36, 4, 35f)));
            c = Over(c, Hex("6B4A2E"), Fill(RotDeg(x, y, 128, 128, 8, 36, 4, -35f)));
            float flame = Circle(x, y, 128, 128, 26) + Mathf.Abs(Mathf.Sin(Mathf.Atan2(y - 128, x - 128) * 5f)) * 9f;
            c = Over(c, Hex("FF7A1A"), Fill(flame));
            c = Over(c, Hex("FFC23A"), Fill(flame + 9f));
            c = Over(c, Hex("FFF2B0"), Fill(Circle(x, y, 128, 128, 7)));
            return c;
        }

        // The lighthouse from above: a round tower in red and white rings, the gallery rail and the bright lamp.
        static Color Lighthouse(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(1f, 0.95f, 0.6f, 0.45f), Mathf.Clamp01(1f - Dist(x, y, 128, 128) / 124f) * 0.8f);
            c = Over(c, Shadow, Soft(Circle(x, y, 150, 106, 96), 16f));
            float body = Circle(x, y, 128, 128, 96);
            float r = Dist(x, y, 128, 128);
            c = Over(c, Mathf.Repeat(r / 24f, 2f) < 1f ? Hex("E2513A") : Hex("FFFFFF"), Fill(body));
            c = Over(c, new Color(0f, 0f, 0f, 0.12f), Fill(body) * Mathf.Clamp01(((x - 128) - (y - 128)) / 200f + 0.3f));
            float gallery = Shell(Circle(x, y, 128, 128, 52), 4f);
            c = Over(c, Hex("2E2A28"), Fill(gallery));
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 128 + Mathf.Cos(a) * 52, 128 + Mathf.Sin(a) * 52, 3.5f)));
            }
            float lamp = Circle(x, y, 128, 128, 34);
            c = Over(c, Hex("3A4650"), Fill(lamp));
            c = Over(c, Hex("FFE58A"), Fill(Circle(x, y, 128, 128, 24)));
            c = Over(c, Hex("FFFFFF"), Fill(Circle(x, y, 122, 134, 9)));
            c = Over(c, Outline, Stroke(body, 5f) * 0.85f);
            return c;
        }

        // A patch of sandy beach (laid flat under everything): a soft, wobbly edge, fine grain and shells.
        static Color BeachPatch(float x, float y)
        {
            float a = Mathf.Atan2(y - 128, x - 128);
            float wobble = Mathf.Sin(a * 3f + 1.3f) * 9f + Mathf.Sin(a * 7f) * 5f;
            float d = Dist(x, y, 128, 128) - 112f - wobble;
            float alpha = Mathf.Clamp01(-d / 14f);
            if (alpha <= 0f) return new Color(0, 0, 0, 0);
            float grain = Noise(x / 5f, y / 5f, 31) * 0.5f + Noise(x / 17f, y / 17f, 13) * 0.5f;
            Color sand = Color.Lerp(Hex("EAD39A"), Hex("F5E3B0"), grain);
            if (Hash((int)(x / 9f), (int)(y / 9f)) > 0.985f && Dist(x, y, Mathf.Floor(x / 9f) * 9f + 4.5f, Mathf.Floor(y / 9f) * 9f + 4.5f) < 2.6f)
                sand = Hex("FFF6E8"); // a little shell
            return new Color(sand.r, sand.g, sand.b, alpha);
        }

        // ------------------------------------------------------------------ on the water

        // A boat's hull outline (bow at the top): pointed at the front, rounded at the back.
        static Vector2[] Hull(float cx, float cy, float halfW, float halfL)
        {
            const int n = 14;
            var pts = new Vector2[n * 2];
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1); // stern 0 .. bow 1
                float w = u < 0.55f ? halfW * (0.72f + 0.28f * Mathf.Sin(u / 0.55f * Mathf.PI * 0.5f))
                                    : halfW * Mathf.Pow(Mathf.Cos((u - 0.55f) / 0.45f * Mathf.PI * 0.5f), 0.75f);
                float py = cy - halfL + u * halfL * 2f;
                pts[i] = new Vector2(cx + w, py);
                pts[n * 2 - 1 - i] = new Vector2(cx - w, py);
            }
            return pts;
        }

        static Color Wake(Color c, float x, float y, float cx, float tail, float width)
        {
            // Two white lines of foam spreading out behind the stern.
            for (int s = -1; s <= 1; s += 2)
            {
                float t = Mathf.Clamp01((tail - y) / tail);
                float lx = cx + s * (width + t * width * 1.4f);
                c = Over(c, White(0.55f * (1f - t)), Fill(Mathf.Abs(x - lx) - 3f + t * 2f) * (y < tail ? 1f : 0f));
            }
            return c;
        }

        // A little sailboat: white hull with a blue stripe, a wooden deck, the mast and a big white sail.
        static Color Sailboat(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Wake(c, x, y, 96, 70, 20);
            float hull = Polygon(x, y, Hull(96, 160, 38, 92));
            c = Over(c, Shadow, Soft(Polygon(x, y, Hull(106, 150, 38, 92)), 8f));
            c = Over(c, Color.white, Fill(hull));
            c = Over(c, Hex("2F6DB5"), Fill(Shell(hull + 5f, 3f)));
            float deck = Polygon(x, y, Hull(96, 158, 28, 80));
            c = Over(c, Mathf.Repeat(x, 10f) < 1.5f ? Hex("A07848") : Hex("CFA068"), Fill(deck));
            c = Over(c, Outline, Stroke(hull, 4f) * 0.85f);
            // The sail, swung out to one side from the mast (a curved belly).
            float sail = Polygon(x, y, new[] {
                new Vector2(96, 190), new Vector2(122, 182), new Vector2(150, 160), new Vector2(170, 128), new Vector2(178, 96), new Vector2(96, 96) });
            c = Over(c, Shadow, Soft(Polygon(x, y, new[] {
                new Vector2(106, 180), new Vector2(132, 172), new Vector2(160, 150), new Vector2(180, 118), new Vector2(188, 86), new Vector2(106, 86) }), 8f));
            c = Over(c, Hex("FDFBF4"), Fill(sail));
            c = Over(c, Hex("E3E0D6"), Fill(Mathf.Max(sail, -(x - 140f))) * 0.7f);
            for (int i = 1; i < 4; i++) c = Over(c, Hex("D2CEC2"), Fill(Mathf.Abs(y - 96 - i * 24f) - 1f) * Fill(sail));
            c = Over(c, Hex("E2513A"), Fill(Mathf.Max(sail, -(y - 176f)))); // a red band at the top
            c = Over(c, Outline, Stroke(sail, 3.5f) * 0.8f);
            c = Over(c, Hex("8A5A32"), Fill(RoundRect(x, y, 136, 96, 42, 3, 1.5f))); // the boom
            c = Over(c, Hex("5A5A5A"), Fill(Circle(x, y, 96, 190, 6)));
            return c;
        }

        // A wooden rowboat with two seats and the oars out.
        static Color Rowboat(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Wake(c, x, y, 64, 36, 14);
            float hull = Polygon(x, y, Hull(64, 104, 30, 66));
            c = Over(c, Shadow, Soft(Polygon(x, y, Hull(70, 98, 30, 66)), 6f));
            c = Over(c, Hex("8A5A32"), Fill(hull));
            float inside = Polygon(x, y, Hull(64, 104, 23, 58));
            c = Over(c, Mathf.Repeat(x, 9f) < 1.5f ? Hex("8A5A32") : Hex("B9874E"), Fill(inside));
            c = Over(c, Hex("6B4A2E"), Fill(RoundRect(x, y, 64, 88, 24, 5, 2)));
            c = Over(c, Hex("6B4A2E"), Fill(RoundRect(x, y, 64, 130, 20, 5, 2)));
            c = Over(c, Outline, Stroke(hull, 3.5f) * 0.85f);
            foreach (int s in new[] { -1, 1 })
            {
                c = Over(c, Hex("A87444"), Fill(RotDeg(x, y, 64 + s * 40, 100, 3, 34, 1.5f, s * 60f)));
                c = Over(c, Hex("A87444"), Fill(RotDeg(x, y, 64 + s * 64, 86, 7, 12, 3, s * 60f)));
            }
            return c;
        }

        // A swan pedal boat: a white body with folded wings, the long neck curving up at the front, an orange beak.
        static Color SwanBoat(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Ellipse(x, y, 96, 84, 52, 62);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 104, 76, 52, 62), 8f));
            c = Over(c, Color.white, Fill(body));
            // Wings: feathered scallops along both sides.
            foreach (int s in new[] { -1, 1 })
            {
                float wing = Ellipse(x, y, 96 + s * 28, 78, 24, 50) + Mathf.Abs(Mathf.Sin(y / 9f)) * 3f;
                c = Over(c, Hex("EEF2F4"), Fill(wing));
                c = Over(c, Hex("C9D3D8"), Stroke(wing, 2.5f));
            }
            c = Over(c, Hex("2F8FD8"), Fill(RoundRect(x, y, 96, 70, 22, 14, 5))); // the seat
            c = Over(c, Outline, Stroke(body, 4f) * 0.85f);
            // The neck and head.
            float neck = Capsule(x, y, 96, 132, 96, 168, 10);
            c = Over(c, Color.white, Fill(neck));
            float head = Circle(x, y, 96, 170, 15);
            c = Over(c, Color.white, Fill(head));
            c = Over(c, Hex("F08A24"), Fill(Polygon(x, y, new[] { new Vector2(88, 180), new Vector2(104, 180), new Vector2(96, 192) })));
            c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 89, 170, 2.5f)));
            c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 103, 170, 2.5f)));
            c = Over(c, Outline, Stroke(Mathf.Min(neck, head), 3f) * 0.8f);
            return c;
        }

        // A red and white marker buoy.
        static Color MarkerBuoy(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, White(0.5f), Fill(Shell(Circle(x, y, 48, 48, 38), 2.5f)));
            float b = Circle(x, y, 48, 48, 26);
            c = Over(c, Shadow, Soft(Circle(x, y, 53, 43, 26), 5f));
            c = Over(c, Mathf.Repeat(Mathf.Atan2(y - 48, x - 48) / (Mathf.PI * 2f) * 4f, 1f) < 0.5f ? Hex("E2513A") : Color.white, Fill(b));
            c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 48, 48, 7)));
            c = Over(c, Outline, Stroke(b, 3.5f) * 0.85f);
            return c;
        }

        // A mother duck swimming ahead of three ducklings (the family heads along +x).
        static Color Ducks(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            void Duck(float cx, float cy, float s, Color body, Color head)
            {
                c = Over(c, White(0.4f), Fill(Shell(Ellipse(x, y, cx - 4 * s, cy, 24 * s, 18 * s), 1.6f)));
                c = Over(c, Shadow, Soft(Ellipse(x, y, cx + 3 * s, cy - 3 * s, 20 * s, 14 * s), 3f));
                float b = Ellipse(x, y, cx, cy, 20 * s, 14 * s);
                c = Over(c, body, Fill(b));
                float h = Circle(x, y, cx + 16 * s, cy, 8 * s);
                c = Over(c, head, Fill(h));
                c = Over(c, Hex("F08A24"), Fill(Ellipse(x, y, cx + 26 * s, cy, 5 * s, 4 * s)));
                c = Over(c, Outline, Stroke(Mathf.Min(b, h), 2.5f) * 0.75f);
            }
            Duck(190, 64, 1.6f, Hex("A0805A"), Hex("3E7A4E"));
            Duck(130, 70, 0.9f, Hex("F5D84A"), Hex("F5D84A"));
            Duck(88, 58, 0.9f, Hex("F5D84A"), Hex("F5D84A"));
            Duck(46, 66, 0.9f, Hex("F5D84A"), Hex("F5D84A"));
            return c;
        }
    }
}
