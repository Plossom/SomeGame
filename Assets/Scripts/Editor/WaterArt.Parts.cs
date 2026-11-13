using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Water Cup's shop on the boat canvas (192 x 336, bow up): tops (a radar arch, a surfboard, a parrot on
    /// the windscreen, a flamingo float on the back bench, the gold treasure chest on the bow), decals on the bow
    /// (a wave, a shark's grin, a palm island, the Jolly Roger, the gold trident crest), trail particles (a bubble)
    /// and the cup's round icon (a speedboat on a turquoise sea under the sun).
    /// </summary>
    public static partial class WaterArt
    {
        static void GenerateBoatParts()
        {
            string f = ArtGenerator.Folder;
            Write(f, "TopRadar", 192, 336, RadarPixel, 192, false);
            Write(f, "TopSurfboard", 192, 336, SurfboardPixel, 192, false);
            Write(f, "TopParrot", 192, 336, ParrotPixel, 192, false);
            Write(f, "TopFlamingoFloat", 192, 336, FloatiePixel, 192, false);
            Write(f, "TopTreasure", 192, 336, TreasurePixel, 192, false);
            Write(f, "DecalWave", 192, 336, WaveDecal, 192, false);
            Write(f, "DecalShark", 192, 336, SharkDecal, 192, false);
            Write(f, "DecalPalm", 192, 336, PalmDecal, 192, false);
            Write(f, "DecalSkull", 192, 336, SkullDecal, 192, false);
            Write(f, "DecalTrident", 192, 336, TridentDecal, 192, false);
            Write(f, "TrailBubble", 64, 64, (x, y) =>
            {
                float ring = Circle(x, y, 32, 32, 24);
                Color c = White(0.35f * Fill(ring));
                c = Over(c, White(1f), Stroke(ring, 4f));
                return Over(c, White(0.9f), Fill(Circle(x, y, 23, 41, 5)));
            }, 64, false);
            Write(f, "CupWater", 128, 128, CupWaterPixel, 100, false);
        }

        // ------------------------------------------------------------------ tops

        static Color RadarPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // An arch across the boat behind the seats, a radar dome on top with its bar.
            float arch = Mathf.Min(RoundRect(x, y, 96, 138, 58, 7, 3), Mathf.Min(RoundRect(x, y, 40, 138, 6, 12, 3), RoundRect(x, y, 152, 138, 6, 12, 3)));
            c = Over(c, new Color(0f, 0.05f, 0.1f, 0.3f), Fill(arch + 2f) * Fill(Mathf.Abs(y - 132f) - 10f));
            c = Over(c, Hex("E9EEF2"), Fill(arch));
            c = Over(c, Line, Stroke(arch, 2.2f));
            float dome = Circle(x, y, 96, 138, 16);
            c = Over(c, Color.white, Fill(dome));
            c = Over(c, Hex("C9D3DC"), Fill(Circle(x, y, 99, 135, 11)));
            c = Over(c, Line, Stroke(dome, 2.2f));
            float bar = RotRect(x, y, 96, 138, 22, 3.5f, 1.5f, 0.6f);
            c = Over(c, Hex("2A2F36"), Fill(bar));
            return Over(c, Hex("E5413A"), Fill(Circle(x, y, 96, 138, 3.5f)));
        }

        static Color SurfboardPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Lying across the back bench, a stripe down its length.
            float board = Mathf.Min(RotRect(x, y, 96, 92, 66, 13, 12, 0.18f), Circle(x, y, 96 + Mathf.Cos(0.18f) * 66, 92 + Mathf.Sin(0.18f) * 66, 1f));
            c = Over(c, new Color(0f, 0.05f, 0.1f, 0.3f), Fill(RotRect(x, y, 100, 86, 66, 13, 12, 0.18f)));
            c = Over(c, Hex("FFD24D"), Fill(board));
            c = Over(c, Hex("FF6B52"), Fill(RotRect(x, y, 96, 92, 66, 3.5f, 2, 0.18f)) * Fill(board));
            c = Over(c, White(0.6f), Fill(RotRect(x, y, 90, 98, 40, 2, 1, 0.18f)) * Fill(board));
            return Over(c, Line, Stroke(board, 2.4f));
        }

        static Color ParrotPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Perched on the windscreen's corner: a green body, red head, long blue and red tail feathers back.
            float tail = Mathf.Min(Triangle(x, y, new Vector2(134, 196), new Vector2(146, 196), new Vector2(150, 160)), Triangle(x, y, new Vector2(140, 196), new Vector2(150, 194), new Vector2(158, 166)));
            c = Over(c, Hex("2E6FD8"), Fill(tail - 1f));
            c = Over(c, Hex("E5413A"), Fill(Triangle(x, y, new Vector2(138, 196), new Vector2(144, 196), new Vector2(146, 168))));
            float body = Ellipse(x, y, 140, 206, 11, 15);
            c = Over(c, Hex("3DBE5A"), Fill(body));
            c = Over(c, Hex("2C9A46"), Fill(Ellipse(x, y, 134, 204, 5, 11)));
            c = Over(c, Hex("2C9A46"), Fill(Ellipse(x, y, 146, 204, 5, 11)));
            float head = Circle(x, y, 140, 222, 9);
            c = Over(c, Hex("E5413A"), Fill(head));
            float beak = Triangle(x, y, new Vector2(135, 228), new Vector2(145, 228), new Vector2(140, 238));
            c = Over(c, Hex("F2C14E"), Fill(beak));
            c = Over(c, Line, Fill(Circle(x, y, 136, 223, 1.8f)));
            c = Over(c, Line, Fill(Circle(x, y, 144, 223, 1.8f)));
            return Over(c, Line, Stroke(Mathf.Min(body, head), 2f));
        }

        static Color FloatiePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // A pink inflatable flamingo ring on the back bench: the ring, the neck curling up, the head and beak.
            float ring = Shell(Circle(x, y, 96, 94, 30), 11f);
            c = Over(c, new Color(0f, 0.05f, 0.1f, 0.3f), Fill(Shell(Circle(x, y, 100, 88, 30), 11f)));
            c = Over(c, Hex("FF8FB8"), Fill(ring));
            c = Over(c, White(0.55f), Fill(Shell(Circle(x, y, 92, 98, 30), 3f)) * Fill(ring));
            c = Over(c, Line, Stroke(ring, 2.2f));
            float neck = Capsule(x, y, 96, 118, 104, 138, 6f);
            c = Over(c, Hex("FF8FB8"), Fill(neck));
            float head = Circle(x, y, 104, 142, 9);
            c = Over(c, Hex("FF8FB8"), Fill(head));
            c = Over(c, Line, Stroke(Mathf.Min(neck, head), 2f));
            float beak = Mathf.Min(Capsule(x, y, 104, 148, 106, 158, 3.5f), Circle(x, y, 106, 158, 3f));
            c = Over(c, Color.white, Fill(beak));
            c = Over(c, Line, Fill(Circle(x, y, 106, 159, 2.6f)));
            return Over(c, Line, Fill(Circle(x, y, 100, 144, 1.8f)));
        }

        static Color TreasurePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // On the bow: a wooden chest with gold bands, its lid open on a heap of coins.
            c = Over(c, new Color(0f, 0.05f, 0.1f, 0.35f), Fill(RoundRect(x, y, 100, 236, 28, 18, 4)));
            float chest = RoundRect(x, y, 96, 240, 26, 16, 4);
            c = Over(c, Mathf.Repeat(y, 7f) < 1.5f ? Hex("6B4426") : Hex("8E5A30"), Fill(chest));
            float bands = Mathf.Min(RoundRect(x, y, 80, 240, 3, 16, 1), RoundRect(x, y, 112, 240, 3, 16, 1));
            c = Over(c, Hex("E8B923"), Fill(bands));
            float coins = Ellipse(x, y, 96, 248, 20, 8);
            c = Over(c, Hex("F5CF4A"), Fill(coins));
            for (int i = 0; i < 6; i++) c = Over(c, Hex("FFF1A8"), Fill(Circle(x, y, 82 + i * 6, 247 + (i % 2) * 3, 1.8f)) * Fill(coins));
            float lid = RoundRect(x, y, 96, 262, 26, 6, 3);
            c = Over(c, Hex("6B4426"), Fill(lid));
            c = Over(c, Hex("E8B923"), Fill(RoundRect(x, y, 96, 262, 26, 1.5f, 1)) * Fill(lid));
            c = Over(c, Line, Stroke(Mathf.Min(chest, lid), 2.2f));
            return Over(c, Color.white, Fill(Star(x, y, 116, 252, 6, 2, 4)));
        }

        // ------------------------------------------------------------------ decals (on the bow, centre 96, 238)

        static Color WaveDecal(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float wave = Mathf.Max(Circle(x, y, 96, 234, 20), -Circle(x, y, 104, 240, 12));
            wave = Mathf.Min(wave, RoundRect(x, y, 90, 218, 24, 5, 3));
            c = Over(c, Hex("1F7BE0"), Fill(wave));
            c = Over(c, Color.white, Fill(Shell(Circle(x, y, 96, 234, 20), 2.2f)) * Fill(wave + 1f) * Mathf.Clamp01((y - 236f) / 6f));
            return Over(c, Line, Stroke(wave, 2f));
        }

        static Color SharkDecal(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // A grin across the bow: a dark red mouth with a row of white teeth top and bottom, an eye each side.
            float mouth = Ellipse(x, y, 96, 230, 34, 13);
            c = Over(c, Hex("8E1D24"), Fill(mouth));
            for (int i = -3; i <= 3; i++)
            {
                c = Over(c, Color.white, Fill(Triangle(x, y, new Vector2(90 + i * 9, 240), new Vector2(102 + i * 9, 240), new Vector2(96 + i * 9, 230))) * Fill(mouth));
                c = Over(c, Color.white, Fill(Triangle(x, y, new Vector2(90 + i * 9, 219), new Vector2(102 + i * 9, 219), new Vector2(96 + i * 9, 228))) * Fill(mouth));
            }
            c = Over(c, Line, Stroke(mouth, 2f));
            foreach (float s in new[] { -1f, 1f })
            {
                c = Over(c, Color.white, Fill(Circle(x, y, 96 + s * 30, 252, 6)));
                c = Over(c, Line, Fill(Circle(x, y, 96 + s * 30, 252, 3)));
            }
            return c;
        }

        static Color PalmDecal(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float island = Ellipse(x, y, 96, 222, 22, 7);
            c = Over(c, Hex("F2D49B"), Fill(island));
            c = Over(c, Line, Stroke(island, 2f));
            float trunk = Capsule(x, y, 96, 224, 102, 252, 3f);
            c = Over(c, Hex("8A5A32"), Fill(trunk));
            float leaves = float.MaxValue;
            foreach (float a in new[] { 0.3f, 1.2f, 2.0f, 2.9f, 3.8f })
                leaves = Mathf.Min(leaves, Capsule(x, y, 102, 254, 102 + Mathf.Cos(a) * 18, 254 + Mathf.Sin(a) * 10 - 2, 4.5f));
            c = Over(c, Hex("2E9E5B"), Fill(leaves));
            return Over(c, Line, Stroke(leaves, 1.8f));
        }

        static Color SkullDecal(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float flag = RoundRect(x, y, 96, 238, 26, 20, 4);
            c = Over(c, Hex("1C1F24"), Fill(flag));
            float bones = Mathf.Min(Capsule(x, y, 80, 224, 112, 248, 2.6f), Capsule(x, y, 80, 248, 112, 224, 2.6f));
            c = Over(c, Color.white, Fill(bones));
            float skull = Mathf.Min(Circle(x, y, 96, 240, 10), RoundRect(x, y, 96, 230, 6, 4, 2));
            c = Over(c, Color.white, Fill(skull));
            c = Over(c, Hex("1C1F24"), Fill(Mathf.Min(Circle(x, y, 92, 241, 2.6f), Circle(x, y, 100, 241, 2.6f))));
            return Over(c, Line, Stroke(flag, 2f));
        }

        static Color TridentDecal(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float shield = Polygon(x, y, new[] { new Vector2(74, 256), new Vector2(118, 256), new Vector2(118, 230), new Vector2(96, 212), new Vector2(74, 230) }) - 2f;
            c = Over(c, Hex("1B4E7A"), Fill(shield));
            c = Over(c, Line, Stroke(shield, 2.2f));
            float trident = Min(Capsule(x, y, 96, 218, 96, 250, 2.2f), Capsule(x, y, 86, 250, 106, 250, 2f),
                                Capsule(x, y, 86, 250, 86, 242, 2f), Capsule(x, y, 106, 250, 106, 242, 2f));
            trident = Mathf.Min(trident, Triangle(x, y, new Vector2(92, 250), new Vector2(100, 250), new Vector2(96, 256)) - 0.5f);
            return Over(c, Hex("FFD24D"), Fill(trident));
        }

        // ------------------------------------------------------------------ the cup's icon

        static Color CupWaterPixel(float x, float y)
        {
            float disc = Circle(x, y, 64, 64, 58);
            // Sky to sea, a sun on the horizon, an island with a palm, a speedboat cutting a white wake.
            Color c = Color.Lerp(Hex("FFE7A8"), Hex("8FDDF2"), Mathf.InverseLerp(64f, 128f, y));
            c = Over(c, Hex("FFD24D"), Fill(Circle(x, y, 86, 70, 18)));
            float sea = y - (64f + 2f * Mathf.Sin(x / 9f));
            c = Over(c, Color.Lerp(Hex("1C9FC4"), Hex("2EC4B6"), y / 64f), Fill(sea));
            c = Over(c, White(0.7f), Fill(Shell(sea, 1.4f)));
            float island = Ellipse(x, y, 34, 64, 20, 6);
            c = Over(c, Hex("F2D49B"), Fill(island));
            c = Over(c, Hex("8A5A32"), Fill(Capsule(x, y, 32, 66, 38, 90, 2f)));
            foreach (float a in new[] { 0.2f, 1.3f, 2.2f, 3.0f })
                c = Over(c, Hex("2E9E5B"), Fill(Capsule(x, y, 38, 90, 38 + Mathf.Cos(a) * 13, 90 + Mathf.Sin(a) * 6 - 3, 3.2f)));
            // A speedboat from the side racing right, spray flying off its stern.
            foreach (var (sx, sy, r) in new[] { (30f, 44f, 5f), (22f, 48f, 4f), (36f, 40f, 4f), (16f, 42f, 3f), (26f, 38f, 3f) })
                c = Over(c, White(0.95f), Fill(Circle(x, y, sx, sy, r)));
            float hull = Polygon(x, y, new[] { new Vector2(38, 36), new Vector2(88, 36), new Vector2(108, 50), new Vector2(36, 50) }) - 1f;
            c = Over(c, Hex("FF4F86"), Fill(hull));
            c = Over(c, White(1f), Fill(Capsule(x, y, 42, 42, 92, 42, 1.8f)) * Fill(hull));
            float screen = Polygon(x, y, new[] { new Vector2(58, 50), new Vector2(80, 50), new Vector2(72, 60), new Vector2(62, 60) });
            c = Over(c, Hex("A8E4FF"), Fill(screen));
            c = Over(c, Hex("12303F"), Stroke(screen, 1.6f));
            c = Over(c, Hex("12303F"), Stroke(hull, 2f));
            c = Over(c, White(0.9f), Fill(Capsule(x, y, 40, 34, 104, 34, 1.6f)));
            c.a = Fill(disc);
            return Over(c, Hex("12303F"), Stroke(disc, 5f));
        }
    }
}
