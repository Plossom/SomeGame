using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Paints the app icon (1024 x 1024) in the game's style: a warm sky, rolling green hills and a
    /// winding road, with the orange buggy flying over it (big, tilted, with its shadow on the road).
    /// Saves it to Assets/Art/AppIcon.png and sets it as the app icon for iOS and as the default icon.
    /// iOS rounds the corners itself, so the picture fills the square.
    /// </summary>
    public static class IconGenerator
    {
        const int Size = 1024;
        const string IconPath = "Assets/Art/AppIcon.png";

        [MenuItem("SomeGame/Generate App Icon")]
        public static void Generate()
        {
            var car = LoadPixels(ArtGenerator.Folder + "/Car.png", out int cw, out int ch);
            var details = LoadPixels(ArtGenerator.Folder + "/CarDetails.png", out _, out _);
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    px[y * Size + x] = Background(x + 0.5f, y + 0.5f);

            // The buggy: big, tilted, high above its shadow on the road.
            Color orange = Hex("FF7A1A");
            const float angle = -28f * Mathf.Deg2Rad, scale = 1.75f;
            Vector2 centre = new(540f, 590f), shadowCentre = new(590f, 500f);
            Stamp(px, car, details, cw, ch, shadowCentre, angle, scale * 0.92f, orange, shadow: true);
            Stamp(px, car, details, cw, ch, centre, angle, scale, orange, shadow: false);

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            for (int i = 0; i < px.Length; i++) px[i].a = 1f; // app icons must be opaque
            tex.SetPixels(px);
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            // Every iOS icon slot (app, spotlight, settings, notifications, marketing) uses the same picture;
            // Unity scales it to each size.
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                var slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (var slot in slots) slot.SetTexture(icon);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, slots);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("App icon generated and set");
        }

        static Color Background(float x, float y)
        {
            // Sky with the sun.
            Color c = Color.Lerp(Hex("F8EBCF"), Hex("F3C98E"), Mathf.InverseLerp(560f, 1024f, y));
            float sun = Circle(x, y, 820f, 860f, 95f);
            c = Over(c, Hex("FFE2AE"), Gauss(Mathf.Max(0f, sun), 70f) * 0.6f);
            c = Over(c, Hex("FFB347"), Fill(sun));
            // Hills, back to front.
            float h1 = 600f + 40f * Mathf.Sin(x / 150f + 1f), h2 = 470f + 55f * Mathf.Sin(x / 190f + 2.4f), h3 = 300f + 50f * Mathf.Sin(x / 170f + 0.3f);
            c = Over(c, Hex("4F7F57"), Fill(y - h1 - 12f * Mathf.Abs(Mathf.Sin(x / 14f))));
            c = Over(c, Hex("A5C987"), Fill(y - h2));
            c = Over(c, Hex("7BB066"), Fill(y - h3));
            // The road: a wide curve sweeping from the bottom left up to the right.
            float RoadX(float yy) => 300f + 420f * Mathf.SmoothStep(0f, 1f, yy / 760f) + 60f * Mathf.Sin(yy / 160f);
            float width = Mathf.Lerp(230f, 60f, Mathf.InverseLerp(0f, 640f, y));
            if (y < 640f)
            {
                float dx = Mathf.Abs(x - RoadX(y));
                c = Over(c, Hex("4A4D4B"), Fill(dx - width * 0.5f - 10f));
                c = Over(c, Hex("606462"), Fill(dx - width * 0.5f));
                bool dash = Mathf.Repeat(y, Mathf.Lerp(90f, 40f, y / 640f)) < Mathf.Lerp(48f, 20f, y / 640f);
                if (dash) c = Over(c, new Color(1f, 0.98f, 0.92f, 0.9f), Fill(dx - width * 0.04f));
            }
            // A few pines on the hills.
            foreach (var (px, py, s) in new[] { (90f, 330f, 1.4f), (180f, 300f, 1.1f), (940f, 330f, 1.5f), (870f, 290f, 1.1f), (60f, 520f, 0.8f), (980f, 520f, 0.8f) })
            {
                float tri1 = Triangle(x, y, new Vector2(px - 50 * s, py), new Vector2(px + 50 * s, py), new Vector2(px, py + 120 * s));
                float tri2 = Triangle(x, y, new Vector2(px - 38 * s, py + 50 * s), new Vector2(px + 38 * s, py + 50 * s), new Vector2(px, py + 150 * s));
                float tree = Mathf.Min(tri1, tri2);
                c = Over(c, x < px ? Hex("3F7E59") : Hex("2C6247"), Fill(tree));
                c = Over(c, Hex("1E3A30"), Stroke(tree, 4f));
            }
            return c;
        }

        static Color[] LoadPixels(string path, out int w, out int h)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            w = tex.width; h = tex.height;
            var pixels = tex.GetPixels();
            Object.DestroyImmediate(tex);
            return pixels;
        }

        // Draws the two-layer car sprite (tinted body + details), rotated and scaled, or as a soft shadow.
        static void Stamp(Color[] px, Color[] body, Color[] details, int w, int h, Vector2 centre, float angle, float scale, Color tint, bool shadow)
        {
            float cos = Mathf.Cos(-angle), sin = Mathf.Sin(-angle);
            float half = Mathf.Max(w, h) * scale * 0.6f;
            for (int y = (int)(centre.y - half); y < centre.y + half; y++)
                for (int x = (int)(centre.x - half); x < centre.x + half; x++)
                {
                    if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
                    float dx = (x - centre.x) / scale, dy = (y - centre.y) / scale;
                    float u = dx * cos - dy * sin + w * 0.5f, v = dx * sin + dy * cos + h * 0.5f;
                    if (u < 0 || v < 0 || u >= w - 1 || v >= h - 1) continue;
                    Color b = Sample(body, w, u, v);
                    int i = y * Size + x;
                    if (shadow)
                    {
                        px[i] = Over(px[i], new Color(0.05f, 0.12f, 0.08f, 0.35f), b.a);
                        continue;
                    }
                    px[i] = Over(px[i], new Color(b.r * tint.r, b.g * tint.g, b.b * tint.b, 1f), b.a);
                    Color d = Sample(details, w, u, v);
                    px[i] = Over(px[i], new Color(d.r, d.g, d.b, 1f), d.a);
                }
        }

        static Color Sample(Color[] p, int w, float u, float v)
        {
            int x0 = (int)u, y0 = (int)v;
            float fx = u - x0, fy = v - y0;
            Color a = Color.Lerp(p[y0 * w + x0], p[y0 * w + x0 + 1], fx);
            Color b = Color.Lerp(p[(y0 + 1) * w + x0], p[(y0 + 1) * w + x0 + 1], fx);
            return Color.Lerp(a, b, fy);
        }
    }
}
