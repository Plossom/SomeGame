using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// A patch of glare ice lying on the road (TrackLayout.icePatches): glossy blue ice with white cracks
    /// and glints, a frosted rim and a ragged edge fading into the road. One oval filling the texture.
    /// </summary>
    public static partial class WinterArt
    {
        [MenuItem("SomeGame/Generate Ice Patch Art")]
        public static void GenerateIcePatch()
        {
            Write(ArtGenerator.Folder, "IcePatch", 256, 256, IcePatchPixel, 64, false);
            ArtGenerator.Material("IcePatch", "IcePatch");
            AssetDatabase.SaveAssets();
            Debug.Log("Ice patch art generated");
        }

        static Color IcePatchPixel(float x, float y)
        {
            // Distance from the centre in oval units (1 = the edge), the edge wobbling a little.
            float u = (x - 128f) / 115f, v = (y - 128f) / 115f;
            float r = Mathf.Sqrt(u * u + v * v);
            float angle = Mathf.Atan2(v, u), ca = Mathf.Cos(angle), sa = Mathf.Sin(angle); // round the rim without a seam
            float edge = 1f + (Noise(ca * 1.6f + 10f, sa * 1.6f + 10f, 64) - 0.5f) * 0.16f + (Noise(ca * 5f + 30f, sa * 5f + 30f, 64) - 0.5f) * 0.07f;
            float inside = edge - r; // > 0 inside
            if (inside < -0.04f) return new Color(0, 0, 0, 0);

            float n = Noise(x / 32f, y / 32f, 8) * 0.6f + Noise(x / 12f, y / 12f, 22) * 0.4f;
            Color c = Color.Lerp(Hex("8CC8EC"), Hex("C4E6F8"), n);
            // Long glossy sheen across the ice.
            float sheen = Mathf.Abs((x - y) * 0.7071f + 20f + (Noise(x / 40f, y / 40f, 7) - 0.5f) * 30f);
            c = Color.Lerp(c, Color.white, Mathf.Clamp01(1f - sheen / 14f) * 0.55f);
            // Cracks: thin wandering white lines.
            float crack = Mathf.Abs(Mathf.Sin((x + Noise(x / 24f, y / 24f, 11) * 50f) * 0.06f + y * 0.031f));
            if (crack < 0.03f) c = Color.Lerp(c, Color.white, 0.75f);
            float crack2 = Mathf.Abs(Mathf.Sin((y + Noise(y / 24f, x / 24f, 11) * 40f) * 0.05f - x * 0.022f));
            if (crack2 < 0.025f) c = Color.Lerp(c, Color.white, 0.6f);
            // Glints.
            if (Hash((int)(x / 2), (int)(y / 2)) > 0.996f) c = Color.white;
            // Darker blue just inside the rim, then frost, then the ragged edge fades into the road.
            if (inside < 0.12f) c = Color.Lerp(c, Hex("6FAAD6"), Mathf.InverseLerp(0.12f, 0.06f, inside) * 0.7f);
            if (inside < 0.06f) c = Color.Lerp(c, Hex("EEF6FC"), Mathf.InverseLerp(0.06f, 0f, inside));
            c.a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.04f, 0.03f, inside));
            return c;
        }
    }
}
