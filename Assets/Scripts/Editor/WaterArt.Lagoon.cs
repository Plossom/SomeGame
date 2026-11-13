using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Water Cup's world (<see cref="SomeGame.Track.TrackLayout.Biome.Lagoon"/>): the turquoise shallows all
    /// round, the deep blue racing channel, the caustic light that dances over both (SomeGame/LagoonWater), white
    /// sand and jungle green for the islands, a foam edge and the lines of red and white buoys in the corners.
    /// </summary>
    public static partial class WaterArt
    {
        const string ShaderName = "SomeGame/LagoonWater";

        [MenuItem("SomeGame/Generate Lagoon World")]
        public static void GenerateLagoon()
        {
            string f = ArtGenerator.Folder;
            Write(f, "LagoonShallows", 256, 256, ShallowsPixel, 32, true);
            Write(f, "LagoonDeep", 256, 256, DeepPixel, 32, true);
            Write(f, "LagoonCaustics", 256, 256, CausticsPixel, 32, true);
            Write(f, "LagoonSand", 128, 128, SandPixel, 32, true);
            Write(f, "LagoonJungle", 128, 128, JunglePixel, 32, true);
            Write(f, "LagoonFoam", 32, 128, FoamPixel, 32, true);
            Write(f, "LagoonBuoys", 64, 128, BuoyPixel, 32, true);
            Water("LagoonSea", "LagoonShallows", Color.white, new Color(1f, 1f, 0.92f, 0.1f), 0.4f, 0.06f);
            Water("LagoonChannel", "LagoonDeep", Color.white, new Color(0.8f, 0.97f, 1f, 0.12f), 0.28f, 0.1f);
            Water("LagoonRunoff", "LagoonShallows", new Color(1f, 1f, 0.94f), new Color(1f, 1f, 0.9f, 0.16f), 0.45f, 0.2f);
            // The boats' foam (wakes and spray): a soft white puff, as a particle material.
            Write(f, "FoamPuff", 64, 64, (x, y) =>
            {
                float d = Circle(x, y, 32, 32, 22) + Noise(x / 8f, y / 8f, 8) * 8f;
                return White(Mathf.Clamp01(0.5f - d / 9f));
            }, 64, false);
            const string foamPath = "Assets/Resources/BoatFoam.mat";
            var foamMat = AssetDatabase.LoadAssetAtPath<Material>(foamPath);
            if (foamMat == null)
            {
                foamMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(foamMat, foamPath);
            }
            foamMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{f}/FoamPuff.png");
            EditorUtility.SetDirty(foamMat);
            ArtGenerator.Material("LagoonSandMat", "LagoonSand");
            ArtGenerator.Material("LagoonJungleMat", "LagoonJungle");
            ArtGenerator.Material("LagoonFoamMat", "LagoonFoam");
            ArtGenerator.Material("LagoonBuoysMat", "LagoonBuoys");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void Water(string name, string baseTexture, Color tint, Color caustic, float scale, float glint)
        {
            string path = $"{ArtGenerator.Folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(ShaderName));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find(ShaderName);
            mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtGenerator.Folder}/{baseTexture}.png"));
            mat.SetTexture("_Caustics", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtGenerator.Folder}/LagoonCaustics.png"));
            mat.SetColor("_Tint", tint);
            mat.SetColor("_CausticColor", caustic);
            mat.SetFloat("_CausticScale", scale);
            mat.SetFloat("_Glint", glint);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
        }

        // Turquoise shallows over a sandy bottom: soft lighter sandbanks and darker weed patches, tiling.
        static Color ShallowsPixel(float x, float y)
        {
            float n = Noise(x / 64f, y / 64f, 4) * 0.55f + Noise(x / 21.33f, y / 21.33f, 12) * 0.3f + Noise(x / 8f, y / 8f, 32) * 0.15f;
            Color c = Color.Lerp(Hex("2FC4C6"), Hex("7EE6DA"), Mathf.SmoothStep(0.2f, 0.85f, n));
            return Color.Lerp(c, Hex("B9F2E4"), Mathf.Clamp01((n - 0.72f) * 3f) * 0.6f);
        }

        // The deep channel: rich azure with slow darker swells.
        static Color DeepPixel(float x, float y)
        {
            float n = Noise(x / 64f, y / 64f, 4) * 0.6f + Noise(x / 25.6f, y / 25.6f, 10) * 0.3f + Noise(x / 10.67f, y / 10.67f, 24) * 0.1f;
            return Color.Lerp(Hex("0E5FA6"), Hex("1E8DCF"), Mathf.SmoothStep(0.15f, 0.9f, n));
        }

        // Caustics: the bright net of a cellular pattern (cell borders), tiling, in the red channel (and alpha).
        static Color CausticsPixel(float x, float y)
        {
            const int cells = 6;
            float size = 256f / cells;
            float gx = x / size, gy = y / size;
            int cx = Mathf.FloorToInt(gx), cy = Mathf.FloorToInt(gy);
            float f1 = 9f, f2 = 9f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int ix = cx + i, iy = cy + j;
                    int wx = ((ix % cells) + cells) % cells, wy = ((iy % cells) + cells) % cells;
                    float px = ix + 0.15f + 0.7f * Hash(wx, wy), py = iy + 0.15f + 0.7f * Hash(wx + 31, wy + 17);
                    float d = Mathf.Sqrt((gx - px) * (gx - px) + (gy - py) * (gy - py));
                    if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
                }
            float edge = 1f - Mathf.SmoothStep(0f, 0.22f, f2 - f1);
            edge = Mathf.Pow(edge, 1.6f);
            return new Color(edge, edge, edge, edge);
        }

        static Color SandPixel(float x, float y)
        {
            float n = Noise(x / 32f, y / 32f, 4) * 0.6f + Noise(x / 10.67f, y / 10.67f, 12) * 0.4f;
            Color c = Color.Lerp(Hex("EED7A1"), Hex("FBEBC4"), n);
            // Wind ripples and a few shell specks.
            float ripple = Mathf.Sin((x * 0.35f + y * 0.12f + Noise(x / 16f, y / 16f, 8) * 9f));
            c = Color.Lerp(c, Hex("E2C78C"), Mathf.Clamp01(ripple - 0.6f) * 0.6f);
            if (Hash((int)x, (int)y) > 0.985f) c = Hex("FFF8EA");
            return c;
        }

        static Color JunglePixel(float x, float y)
        {
            float n = Noise(x / 32f, y / 32f, 4) * 0.5f + Noise(x / 12.8f, y / 12.8f, 10) * 0.35f + Noise(x / 5.33f, y / 5.33f, 24) * 0.15f;
            Color c = Color.Lerp(Hex("2F8A45"), Hex("5FBF5A"), Mathf.SmoothStep(0.2f, 0.8f, n));
            return Color.Lerp(c, Hex("7BD06A"), Mathf.Clamp01((n - 0.7f) * 4f) * 0.5f);
        }

        // Foam along the channel's edge: white, frothy, bubbles in it, fading out across (u) from the edge.
        static Color FoamPixel(float x, float y)
        {
            float across = x / 32f;
            float n = Noise(x / 8f, y / 16f, 8) * 0.6f + Noise(x / 4f, y / 8f, 16) * 0.4f;
            float a = Mathf.Clamp01(1f - Mathf.Abs(across - 0.5f) * 2f) * Mathf.Lerp(0.55f, 1f, n);
            float bubble = Mathf.Min(Circle(x, Mathf.Repeat(y, 32f), 10, 12, 3.2f), Circle(x, Mathf.Repeat(y + 11f, 32f), 22, 20, 2.4f));
            a = Mathf.Max(a * 0.85f, Stroke(bubble, 1.4f) * 0.9f);
            return White(a);
        }

        // A rope floating along the corner with a red and white buoy on it every repeat.
        static Color BuoyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float rope = Mathf.Abs(x - 32f) - 1.6f;
            c = Over(c, new Color(0.95f, 0.95f, 0.92f, 0.9f), Fill(rope));
            // Ripples round the buoy, its shadow, then the ball: red with a white band.
            c = Over(c, White(0.45f), Stroke(Circle(x, y, 32, 64, 21), 2f));
            c = Over(c, new Color(0f, 0.1f, 0.18f, 0.35f), Fill(Circle(x, y, 35, 60, 15)));
            float ball = Circle(x, y, 32, 64, 15);
            bool band = Mathf.Abs(y - 64f) < 4.5f;
            c = Over(c, band ? Color.white : Hex("E8423A"), Fill(ball));
            c = Over(c, White(0.75f), Fill(Circle(x, y, 27, 70, 4)));
            return Over(c, Hex("12303F"), Stroke(ball, 2f));
        }
    }
}
