using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Signed distance helpers (distance in pixels, negative inside), colour blending and PNG writing
    /// shared by the procedural art generators.
    /// </summary>
    public static class Sdf
    {
        public static float Dist(float x, float y, float cx, float cy) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        public static float Circle(float x, float y, float cx, float cy, float r) => Dist(x, y, cx, cy) - r;

        public static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            // Approximate distance: scaled circle distance, good enough for anti-aliasing.
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            float k = Mathf.Sqrt(dx * dx + dy * dy);
            return (k - 1f) * Mathf.Min(rx, ry);
        }

        public static float RoundRect(float x, float y, float cx, float cy, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x - cx) - (hw - r), qy = Mathf.Abs(y - cy) - (hh - r);
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        /// <summary>Rounded rectangle rotated by <paramref name="angle"/> radians around its centre.</summary>
        public static float RotRect(float x, float y, float cx, float cy, float hw, float hh, float r, float angle)
        {
            float c = Mathf.Cos(-angle), s = Mathf.Sin(-angle);
            float dx = x - cx, dy = y - cy;
            return RoundRect(dx * c - dy * s, dx * s + dy * c, 0, 0, hw, hh, r);
        }

        public static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            Vector2 p = new(x - ax, y - ay), ba = new(bx - ax, by - ay);
            float h = Mathf.Clamp01(Vector2.Dot(p, ba) / ba.sqrMagnitude);
            return (p - ba * h).magnitude - r;
        }

        public static float ArcBand(float x, float y, float cx, float cy, float radius, float fromDeg, float toDeg)
        {
            float a = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
            if (Mathf.Repeat(a - fromDeg, 360f) <= Mathf.Repeat(toDeg - fromDeg, 360f)) return Mathf.Abs(Dist(x, y, cx, cy) - radius);
            Vector2 e1 = new(cx + Mathf.Cos(fromDeg * Mathf.Deg2Rad) * radius, cy + Mathf.Sin(fromDeg * Mathf.Deg2Rad) * radius);
            Vector2 e2 = new(cx + Mathf.Cos(toDeg * Mathf.Deg2Rad) * radius, cy + Mathf.Sin(toDeg * Mathf.Deg2Rad) * radius);
            return Mathf.Min(Dist(x, y, e1.x, e1.y), Dist(x, y, e2.x, e2.y));
        }

        public static float Shell(float d, float halfWidth) => Mathf.Abs(d) - halfWidth;

        public static float Triangle(float x, float y, Vector2 a, Vector2 b, Vector2 c) => Polygon(x, y, new[] { a, b, c });

        /// <summary>Signed distance to a simple polygon (even-odd inside test).</summary>
        public static float Polygon(float x, float y, Vector2[] pts)
        {
            Vector2 p = new(x, y);
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                d = Mathf.Min(d, SegDist(p, pts[j], pts[i]));
                if ((pts[i].y > y) != (pts[j].y > y) &&
                    x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            }
            return inside ? -d : d;
        }

        public static float Star(float x, float y, float cx, float cy, float outer, float inner, int points = 5, float rotation = 0f)
        {
            var pts = new Vector2[points * 2];
            for (int i = 0; i < pts.Length; i++)
            {
                float ang = Mathf.PI / 2f + rotation + i * Mathf.PI / points;
                float r = i % 2 == 0 ? outer : inner;
                pts[i] = new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r);
            }
            return Polygon(x, y, pts);
        }

        public static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, ba.sqrMagnitude));
            return (pa - ba * h).magnitude;
        }

        /// <summary>Polynomial smooth minimum: blends two distance fields with a rounded seam of size k.</summary>
        public static float SMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        public static float Min(params float[] values) { float m = float.MaxValue; foreach (var v in values) m = Mathf.Min(m, v); return m; }

        public static float Fill(float d) => Mathf.Clamp01(0.5f - d);
        public static float Stroke(float d, float width) => Mathf.Clamp01(width * 0.5f - Mathf.Abs(d) + 0.5f);
        public static float Gauss(float d, float sigma) => Mathf.Exp(-(d * d) / (2f * sigma * sigma));
        public static Color White(float a) => new(1f, 1f, 1f, a);

        public static Color Over(Color under, Color over, float coverage)
        {
            float a = coverage * over.a;
            if (a <= 0f) return under;
            float outA = a + under.a * (1f - a);
            if (outA <= 0f) return new Color(0, 0, 0, 0);
            Color rgb = (over * a + under * under.a * (1f - a)) / outA;
            rgb.a = outA;
            return rgb;
        }

        public static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0xFFFF) / 65535f;
        }

        /// <summary>Smooth value noise in [0,1], tiling every <paramref name="period"/> cells.</summary>
        public static float Noise(float x, float y, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float H(int i, int j) => Hash(((i % period) + period) % period, ((j % period) + period) % period);
            return Mathf.Lerp(Mathf.Lerp(H(x0, y0), H(x0 + 1, y0), fx), Mathf.Lerp(H(x0, y0 + 1), H(x0 + 1, y0 + 1), fx), fy);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color Shade(Color c, float k) => new(c.r * k, c.g * k, c.b * k, c.a);

        // ---------- writing ----------

        /// <summary>Paints a texture pixel by pixel and imports it as a sprite.</summary>
        public static void Write(string folder, string name, int w, int h, Func<float, float, Color> painter, int ppu, bool tiled, int border = 0)
        {
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = painter(x + 0.5f, y + 0.5f);
            WritePixels(folder, name, w, h, pixels, ppu, tiled, border);
        }

        public static void WritePixels(string folder, string name, int w, int h, Color[] pixels, int ppu, bool tiled, int border = 0, bool compress = false)
        {
            Directory.CreateDirectory(folder);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            string path = $"{folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = tiled;
            importer.alphaIsTransparency = true;
            importer.textureCompression = compress ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(w, h)));
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.spriteBorder = new Vector4(border, border, border, border);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
