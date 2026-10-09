using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Textures and materials for the 3D neon city (SomeGame > Generate City Art), all using the
    /// SomeGame/City Unlit shader. Emission above 1 marks what should glow under bloom.
    /// </summary>
    public static class CityArtGenerator
    {
        public const string Folder = "Assets/Art/City";
        const string ShaderName = "SomeGame/City Unlit";

        [MenuItem("SomeGame/Generate City Art")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Folder);
            WriteTexture("Windows", 256, 256, WindowsPixel, repeat: true);
            WriteTexture("Cloud", 256, 256, CloudPixel, repeat: false);
            WriteTexture("Water", 128, 128, WaterPixel, repeat: true);
            AssetDatabase.Refresh();

            Material("CityWalls", "Windows", 1.9f, BlendMode.One, BlendMode.Zero, true, 2000);
            Material("CityFlat", null, 1f, BlendMode.One, BlendMode.Zero, true, 2000);
            Material("CityLines", null, 1.7f, BlendMode.One, BlendMode.Zero, true, 2000);
            Material("CityNeon", null, 4f, BlendMode.One, BlendMode.Zero, true, 2000);
            Material("CityWater", "Water", 1.3f, BlendMode.One, BlendMode.Zero, true, 2000);
            Material("CityGlow", "../Neon/Glow", 2.2f, BlendMode.One, BlendMode.One, false, 3000, premultiply: true);
            Material("CityFog", "Cloud", 1f, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, false, 3010);
            AssetDatabase.SaveAssets();
            Debug.Log($"City art generated in {Folder}");
        }

        // 8x8 windows per tile. Wall panels with lit / dark windows in a few colours.
        static Color WindowsPixel(int x, int y)
        {
            int cx = x / 32, cy = y / 32, lx = x % 32, ly = y % 32;
            Color wall = new(0.055f, 0.07f, 0.095f);
            if (lx == 0 || lx == 31) wall *= 0.8f; // panel seams
            bool inWindow = lx >= 7 && lx < 25 && ly >= 8 && ly < 24;
            if (!inWindow) return wall;

            float r = Hash(cx * 7 + 3, cy * 13 + 1);
            if (r > 0.47f) return new Color(0.09f, 0.11f, 0.15f); // dark window
            float pick = Hash(cx * 31 + 5, cy * 17 + 9);
            float brightness = 0.55f + Hash(cx * 3 + 11, cy * 5 + 7) * 0.45f;
            Color lit = pick < 0.6f ? new Color(1f, 0.82f, 0.55f)
                : pick < 0.85f ? new Color(0.82f, 0.92f, 1f)
                : pick < 0.93f ? new Color(0.24f, 0.88f, 1f)
                : new Color(1f, 0.24f, 0.5f);
            // A soft vertical gradient inside the window (lamp near the ceiling).
            float shade = Mathf.Lerp(0.75f, 1f, (ly - 8) / 16f);
            return lit * brightness * shade;
        }

        static Color CloudPixel(int x, int y)
        {
            float nx = x / 256f, ny = y / 256f;
            float d = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.5f)) * 2f;
            float falloff = Mathf.Clamp01(1f - d);
            falloff *= falloff;
            float n = 0f, amp = 0.5f, freq = 3f;
            for (int o = 0; o < 4; o++)
            {
                n += amp * Mathf.PerlinNoise(nx * freq + 13.1f, ny * freq + 7.7f);
                amp *= 0.5f;
                freq *= 2f;
            }
            float a = Mathf.Clamp01((n - 0.25f) * 1.6f) * falloff;
            return new Color(1f, 1f, 1f, a);
        }

        static Color WaterPixel(int x, int y)
        {
            float wave = Mathf.PerlinNoise(x / 9f, y / 42f);
            float streak = Mathf.Pow(Mathf.Clamp01(wave - 0.55f) * 2.2f, 2f);
            return new Color(0.02f, 0.05f, 0.08f) + new Color(0.1f, 0.35f, 0.5f) * streak;
        }

        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0xFFFF) / 65535f;
        }

        static void WriteTexture(string name, int w, int h, System.Func<int, int, Color> painter, bool repeat)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = painter(x, y);
            tex.SetPixels(pixels);
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        static void Material(string name, string texture, float emission, BlendMode src, BlendMode dst, bool zWrite, int queue,
            bool premultiply = false)
        {
            string path = $"{Folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(ShaderName));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find(ShaderName);
            mat.SetTexture("_BaseMap", texture == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(
                texture.StartsWith("../") ? "Assets/Art/" + texture.Substring(3) + ".png" : $"{Folder}/{texture}.png"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Emission", emission);
            mat.SetFloat("_SrcBlend", (float)src);
            mat.SetFloat("_DstBlend", (float)dst);
            mat.SetFloat("_ZWrite", zWrite ? 1f : 0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_Premultiply", premultiply ? 1f : 0f);
            mat.renderQueue = queue;
            EditorUtility.SetDirty(mat);
        }
    }
}
