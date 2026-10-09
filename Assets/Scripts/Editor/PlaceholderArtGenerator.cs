using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Generates flat-colour placeholder sprites into Assets/Art/Placeholder.
    /// Re-run from the menu to regenerate; existing files are overwritten (GUIDs are kept).
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        public const string Folder = "Assets/Art/Placeholder";
        public const int PixelsPerUnit = 64;

        static readonly Color Grass = Hex("5C9E45");
        static readonly Color GrassDark = Hex("528F3D");
        static readonly Color Road = Hex("7A7D82");
        static readonly Color RoadDark = Hex("72757A");
        static readonly Color KerbRed = Hex("D63A2F");
        static readonly Color KerbWhite = Hex("F2F2F2");
        static readonly Color TreeDark = Hex("2F6B2A");
        static readonly Color TreeLight = Hex("3F8A36");
        static readonly Color Clear = new Color(0, 0, 0, 0);

        [MenuItem("SomeGame/Generate Placeholder Art")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Folder);

            Write("Car", 64, 112, Car, tiled: false);
            Write("RoadSurface", 64, 64, (x, y) => Noise(x, y) > 0.8f ? RoadDark : Road, tiled: true);
            // One red and one white block along V; the track mesh tiles it along the road.
            Write("Kerb", 16, 64, (x, y) => y < 32 ? KerbRed : KerbWhite, tiled: true);
            Write("Grass", 64, 64, (x, y) => (y / 32) % 2 == 0 ? Grass : GrassDark, tiled: true);
            Write("Tree", 128, 128, Tree, tiled: false);
            Write("StartLine", 64, 32, (x, y) => ((x / 16) + (y / 16)) % 2 == 0 ? Color.white : Color.black, tiled: true);
            Write("JoystickBase", 256, 256, (x, y) => Ring(x, y, 256, 120, 112, 0.45f), tiled: false);
            Write("JoystickKnob", 128, 128, (x, y) => Disc(x, y, 128, 60, 0.8f), tiled: false);
            Write("Circle", 64, 64, (x, y) => Disc(x, y, 64, 31, 1f), tiled: false);
            Write("Star", 128, 128, (x, y) => InStar(x + 0.5f, y + 0.5f, 64f, 64f, 60f, 25f) ? Color.white : Clear, tiled: false);
            Write("Lock", 64, 64, Lock, tiled: false);
            // Menu icon: three rounded bars.
            Write("Menu", 64, 64, (x, y) => x >= 8 && x < 56 && (Mathf.Abs(y - 14) < 4 || Mathf.Abs(y - 32) < 4 || Mathf.Abs(y - 50) < 4)
                ? Color.white : Clear, tiled: false);

            // Track meshes and tyre marks are not sprites, so they need unlit sprite materials.
            WriteMaterial("Road", "RoadSurface");
            WriteMaterial("Kerb", "Kerb");
            WriteMaterial("StartLine", "StartLine");
            WriteMaterial("Grass", "Grass");
            WriteMaterial("Tree", "Tree");
            WriteMaterial("TyreMark", null);
            WriteMaterial("Spark", "Circle");

            AssetDatabase.Refresh();
            Debug.Log($"Placeholder art generated in {Folder}");
        }

        static Color Car(int x, int y)
        {
            // Pointing up (+Y). White body so SpriteRenderer.color can tint each car.
            bool wheel = (x < 8 || x >= 56) && ((y >= 14 && y < 34) || (y >= 78 && y < 98));
            if (wheel) return new Color(0.12f, 0.12f, 0.12f);
            bool body = x >= 8 && x < 56 && y >= 4 && y < 108 && !Corner(x - 8, y - 4, 48, 104, 10);
            if (!body) return Clear;
            if (y >= 66 && y < 82 && x >= 14 && x < 50) return new Color(0.2f, 0.25f, 0.32f); // windscreen
            if (y >= 30 && y < 40 && x >= 16 && x < 48) return new Color(0.3f, 0.33f, 0.38f);  // rear window
            if (x >= 29 && x < 35) return new Color(0.85f, 0.85f, 0.85f);                     // stripe
            return Color.white;
        }

        static Color Tree(int x, int y)
        {
            float dx = x - 63.5f, dy = y - 63.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 60) return Clear;
            float hx = x - 52f, hy = y - 76f;
            if (hx * hx + hy * hy < 26 * 26) return TreeLight;
            return d > 56 ? TreeDark * 0.85f + new Color(0, 0, 0, 0.15f) : TreeDark;
        }

        // Five-pointed star, point up: even-odd test against its 10-vertex outline.
        static bool InStar(float x, float y, float cx, float cy, float outer, float inner)
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                points[i] = new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
            }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                if ((points[i].y > y) != (points[j].y > y) &&
                    x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                    inside = !inside;
            }
            return inside;
        }

        static Color Lock(int x, int y)
        {
            bool body = x >= 12 && x < 52 && y >= 6 && y < 36;
            float dx = x - 31.5f, dy = y - 36f, d = Mathf.Sqrt(dx * dx + dy * dy);
            bool shackle = y >= 36 && d >= 11f && d <= 18f;
            bool keyhole = Mathf.Abs(x - 31.5f) < 3f && y >= 14 && y < 26;
            return (body && !keyhole) || shackle ? Color.white : Clear;
        }

        static bool Corner(int x, int y, int w, int h, int r)
        {
            int cx = x < r ? r : (x >= w - r ? w - r - 1 : x);
            int cy = y < r ? r : (y >= h - r ? h - r - 1 : y);
            return (x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r;
        }

        static Color Disc(int x, int y, int size, float radius, float alpha)
        {
            float c = (size - 1) * 0.5f, dx = x - c, dy = y - c;
            float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
            return new Color(1, 1, 1, a * alpha);
        }

        static Color Ring(int x, int y, int size, float outer, float inner, float fillAlpha)
        {
            float c = (size - 1) * 0.5f, dx = x - c, dy = y - c, d = Mathf.Sqrt(dx * dx + dy * dy);
            float outside = Mathf.Clamp01(outer - d + 0.5f);
            float edge = Mathf.Clamp01(d - inner + 0.5f);
            return new Color(1, 1, 1, outside * Mathf.Lerp(fillAlpha * 0.4f, 0.9f, edge));
        }

        static float Noise(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0xFFFF) / 65535f;
        }

        static void Write(string name, int w, int h, Func<int, int, Color> pixel, bool tiled)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = pixel(x, y);
            tex.SetPixels(pixels);
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = tiled ? FilterMode.Point : FilterMode.Bilinear;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = tiled ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static void WriteMaterial(string name, string textureName)
        {
            string path = $"{Folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.mainTexture = textureName == null
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{textureName}.png");
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
