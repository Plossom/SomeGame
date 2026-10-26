using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    public static partial class WinterArt
    {
        /// <summary>A heap of snow seen from above (SnowPiles): soft lumpy outline, lit on the upper left, blue shade low right, sparkles.</summary>
        [MenuItem("SomeGame/Generate Snow Pile Art")]
        public static void GenerateSnowPile()
        {
            Write(ArtGenerator.Folder, "SnowPile", 192, 192, SnowPilePixel, 96, false);
            AssetDatabase.SaveAssets();
        }

        // Lumps of snow heaped together: a big one in the middle and smaller ones round it.
        static readonly Vector3[] PileLumps =
        {
            new(96, 98, 46), new(58, 118, 30), new(138, 116, 28), new(70, 64, 30), new(126, 66, 32), new(100, 140, 26), new(40, 82, 20), new(154, 86, 20),
        };

        static float SnowPileSdf(float x, float y)
        {
            float d = float.MaxValue;
            foreach (var l in PileLumps) d = SMin(d, Circle(x, y, l.x, l.y, l.z), 6f);
            return d;
        }

        // Each lump lit on its upper left.
        static float PileLight(float x, float y)
        {
            float best = 0f;
            foreach (var l in PileLumps)
            {
                float dx = (x - (l.x - l.z * 0.3f)) / l.z, dy = (y - (l.y + l.z * 0.3f)) / l.z;
                best = Mathf.Max(best, Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)));
            }
            return best;
        }

        static Color SnowPilePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(0.15f, 0.25f, 0.4f, 0.28f), Fill(SnowPileSdf(x - 7f, y + 9f) - 2f)); // shadow
            float heap = SnowPileSdf(x, y);
            Color body = Color.Lerp(Hex("B4C9E0"), Hex("FBFDFF"), Mathf.Clamp01(0.25f + PileLight(x, y) * 1.1f));
            body = Color.Lerp(body, Shade(body, 0.95f), Noise(x / 10f, y / 10f, 19) * 0.4f);
            c = Over(c, body, Fill(heap));
            if (Hash((int)x / 3, (int)y / 3) > 0.994f && heap < -6f) c = Over(c, Color.white, 1f);
            return Over(c, Hex("7E97B4"), Stroke(heap, 3f));
        }
    }
}
