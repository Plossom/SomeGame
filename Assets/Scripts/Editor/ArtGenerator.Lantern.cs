using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Art for the sunset race (Valley Cup race 7): the drawbridge's leaves, a stone bridge, the golden boost
    /// rings, the waterfall, lanterns (on posts, floating on the lake, in the sky), the cars' headlight beams.
    /// </summary>
    public static partial class ArtGenerator
    {
        [MenuItem("SomeGame/Generate Lantern Art")]
        public static void GenerateLantern()
        {
            World("BridgeLeaf", 256, 256, BridgeLeaf, 128, false);
            World("StoneBridge", 256, 256, StoneBridge, 128, false);
            World("BoostRing", 256, 256, BoostRing, 128, false);
            World("Waterfall", 128, 128, WaterfallPixel, 32, true);
            Material("Waterfall", "Waterfall");
            World("Lantern", 128, 128, Lantern, 128, false);
            World("FloatLantern", 96, 96, FloatLantern, 128, false);
            World("Glow", 128, 128, (x, y) => new Color(1f, 0.85f, 0.5f, Mathf.Pow(Mathf.Clamp01(1f - Dist(x, y, 64, 64) / 64f), 1.8f)), 128, false);
            World("Headlight", 256, 256, Headlight, 128, false);
            AssetDatabase.Refresh();
        }

        // One leaf of the drawbridge (the road runs along y; the hinge at y = 0): wooden planks between
        // steel girders with white railings, the free end (top) with a steel lip.
        static Color BridgeLeaf(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float deck = RoundRect(x, y, 128, 128, 120, 126, 4);
            float plank = Mathf.Repeat(y, 16f);
            Color wood = Color.Lerp(Hex("8A5A32"), Hex("A87444"), Hash(0, (int)(y / 16f)));
            c = Over(c, plank < 1.5f ? Hex("5E3C20") : wood, Fill(deck));
            foreach (float gx in new[] { 14f, 242f })
            {
                float girder = RoundRect(x, y, gx, 128, 12, 126, 3);
                c = Over(c, Hex("4A5560"), Fill(girder));
                c = Over(c, Hex("6E7C88"), Fill(RoundRect(x, y, gx - 3, 128, 4, 124, 2)));
                // rivets
                if (Mathf.Abs(Mathf.Repeat(y, 24f) - 12f) < 3f && Mathf.Abs(x - gx) < 3f) c = Hex("C8D0D8");
            }
            c = Over(c, Hex("C9D0D6"), Fill(RoundRect(x, y, 128, 250, 120, 6, 2)));
            c = Over(c, Hex("E2513A"), Fill(RoundRect(x, y, 128, 250, 120, 6, 2)) * (Mathf.Repeat(x / 32f, 2f) < 1f ? 1f : 0f));
            c = Over(c, Outline, Stroke(deck, 4f) * 0.8f);
            return c;
        }

        // A stone bridge deck (road along y): grey flagstones, a low parapet on both sides with copings.
        static Color StoneBridge(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float deck = RoundRect(x, y, 128, 128, 124, 128, 2);
            int row = (int)(y / 20f);
            float u = Mathf.Repeat(x + (row % 2) * 16f, 32f), v = Mathf.Repeat(y, 20f);
            Color stone = Color.Lerp(Hex("A9A49A"), Hex("BDB8AD"), Hash((int)((x + (row % 2) * 16f) / 32f), row));
            if (u < 2f || v < 2f) stone = Hex("8C877D");
            c = Over(c, stone, Fill(deck));
            foreach (float px in new[] { 10f, 246f })
            {
                float wall = RoundRect(x, y, px, 128, 10, 128, 3);
                c = Over(c, Hex("9A9488"), Fill(wall));
                c = Over(c, Hex("D8D2C4"), Fill(RoundRect(x, y, px - 2, 128, 5, 128, 2)));
                c = Over(c, Outline, Stroke(wall, 3f) * 0.7f);
            }
            return c;
        }

        // A golden ring seen from above, standing up across the road: a thick gold hoop (an ellipse) with a
        // shine and a soft glow.
        static Color BoostRing(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Kept well inside the picture, so the glow fades out before its edges (no cut-off box round it).
            float outer = Ellipse(x, y, 128, 128, 104, 40), inner = Ellipse(x, y, 128, 128, 83, 23);
            float hoop = Mathf.Max(outer, -inner);
            float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(x, 256f - x), Mathf.Min(y, 256f - y)) / 8f);
            c = Over(c, new Color(1f, 0.85f, 0.3f, 0.35f), Mathf.Clamp01(1f - Mathf.Abs(outer) / 14f) * 0.8f * edge);
            c = Over(c, Hex("E8A520"), Fill(hoop));
            c = Over(c, Hex("FFD54A"), Fill(Mathf.Max(hoop, -(y - 128f))) * 0.9f);
            c = Over(c, Hex("FFF4C2"), Fill(Ellipse(x, y, 98, 154, 22, 5)));
            c = Over(c, Hex("8A5A10"), Stroke(hoop, 4f) * 0.85f);
            return c;
        }

        // Falling water (tiled; it falls along y, the texture scrolls): streaks of white over blue, foamy.
        static Color WaterfallPixel(float x, float y)
        {
            float streak = Noise(x / 6f, y / 40f, 21) * 0.7f + Noise(x / 3f, y / 18f, 43) * 0.3f;
            Color c = Color.Lerp(Hex("7FC4EC"), Hex("F2FBFF"), Mathf.SmoothStep(0.35f, 0.75f, streak));
            return new Color(c.r, c.g, c.b, Mathf.Lerp(0.55f, 0.9f, streak));
        }

        // A lantern on a post from above: a round paper lantern (warm), a dark cap.
        static Color Lantern(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 74, 56, 20), 8f));
            float paper = Circle(x, y, 64, 64, 26);
            c = Over(c, Hex("F2723A"), Fill(paper));
            c = Over(c, Hex("FFB561"), Fill(Circle(x, y, 58, 70, 15)));
            for (int i = -1; i <= 1; i++) c = Over(c, Hex("C04A26"), Fill(Mathf.Abs(x - 64 - i * 14f) - 1.2f) * Fill(paper + 3f));
            c = Over(c, Hex("2E2A28"), Fill(Circle(x, y, 64, 64, 8)));
            c = Over(c, Outline, Stroke(paper, 3f) * 0.8f);
            return c;
        }

        // A little paper lantern floating on the water (a square boat of paper with a candle).
        static Color FloatLantern(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float boat = RoundRect(x, y, 48, 48, 26, 26, 6);
            c = Over(c, Hex("F6E6C8"), Fill(boat));
            c = Over(c, Hex("E2513A"), Fill(Shell(boat + 5f, 2f)));
            c = Over(c, Hex("FFE58A"), Fill(Circle(x, y, 48, 48, 9)));
            c = Over(c, Color.white, Fill(Circle(x, y, 46, 50, 4)));
            c = Over(c, Outline, Stroke(boat, 3f) * 0.7f);
            return c;
        }

        // A headlight beam (the car at the bottom centre, the light reaching up): a soft cone.
        static Color Headlight(float x, float y)
        {
            float t = y / 256f;
            float half = Mathf.Lerp(34f, 126f, Mathf.Sqrt(t));
            float across = Mathf.Abs(x - 128f) / half;
            float a = Mathf.Clamp01(1f - across * across) * Mathf.Pow(Mathf.Clamp01(1f - t), 0.8f) * Mathf.Clamp01(t * 8f);
            return new Color(1f, 0.95f, 0.75f, a * 0.85f);
        }
    }
}
