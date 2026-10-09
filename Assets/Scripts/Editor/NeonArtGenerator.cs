using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Generates the Night Neon art set into Assets/Art/Neon: UI shapes (9-sliced panels, rings,
    /// glows, diamonds), icons, the car sprite and the track/world textures and materials.
    /// Everything is drawn with signed distance functions so edges are anti-aliased at any size.
    /// Most art is white so it can be tinted by Image/SpriteRenderer/vertex colours.
    /// </summary>
    public static class NeonArtGenerator
    {
        public const string Folder = "Assets/Art/Neon";

        public static readonly Color Asphalt = Hex("101318");
        public static readonly Color Lime = Hex("C8FF3D");

        delegate Color Painter(float x, float y);

        [MenuItem("SomeGame/Generate Neon Art")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Folder);

            // UI shapes (white, tinted in UI). Borders = 9-slice margins in pixels.
            Ui("Panel", 128, 128, (x, y) => Fill(RoundRect(x, y, 64, 64, 64, 64, 28)), 32);
            Ui("PanelOutline", 128, 128, (x, y) => Stroke(RoundRect(x, y, 64, 64, 62, 62, 26), 3f), 32);
            Ui("Chip", 64, 64, (x, y) => Fill(RoundRect(x, y, 32, 32, 32, 32, 10)), 16);
            Ui("ChipOutline", 64, 64, (x, y) => Stroke(RoundRect(x, y, 32, 32, 31, 31, 9), 2f), 16);
            Ui("Circle", 256, 256, (x, y) => Fill(Circle(x, y, 128, 128, 126)), 0);
            Ui("Ring", 256, 256, (x, y) => Stroke(Circle(x, y, 128, 128, 122), 6f), 0);
            Ui("RingThin", 256, 256, (x, y) => Stroke(Circle(x, y, 128, 128, 124), 3f), 0);
            Ui("Glow", 256, 256, (x, y) =>
            {
                float d = Dist(x, y, 128, 128);
                return White(Gauss(d, 44f) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(96f, 127f, d))));
            }, 0);
            Ui("FadeV", 8, 128, (x, y) => y / 128f, 0); // transparent at the bottom, opaque at the top
            Ui("Diamond", 192, 192, (x, y) => Fill(Diamond(x, y, 96, 96, 84, 16)), 0);
            Ui("DiamondOutline", 192, 192, (x, y) => Stroke(Diamond(x, y, 96, 96, 82, 14), 5f), 0);

            // Icons (128 px, white).
            Ui("IconMenu", 128, 128, (x, y) => Fill(Min(Capsule(x, y, 26, 40, 102, 40, 6), Capsule(x, y, 26, 64, 82, 64, 6), Capsule(x, y, 26, 88, 102, 88, 6))), 0);
            Ui("IconStar", 128, 128, (x, y) => Fill(Star(x, y, 64, 66, 60, 25)), 0);
            Ui("IconLock", 128, 128, (x, y) => Fill(Min(Shell(RoundRect(x, y, 64, 44, 34, 28, 8), 5f),
                Shell(ArcBand(x, y, 64, 80, 20, 0f, 180f), 5f), Capsule(x, y, 44, 80, 44, 70, 5), Capsule(x, y, 84, 80, 84, 70, 5))), 0);
            Ui("IconRetry", 128, 128, (x, y) => Fill(Min(Shell(ArcBand(x, y, 64, 62, 36, 100f, 40f), 6f),
                Triangle(x, y, new Vector2(50, 116), new Vector2(50, 82), new Vector2(78, 99)))), 0);
            Ui("IconPlay", 128, 128, (x, y) => Fill(Triangle(x, y, new Vector2(40, 24), new Vector2(40, 104), new Vector2(104, 64))), 0);
            Ui("IconClose", 128, 128, (x, y) => Fill(Min(Capsule(x, y, 34, 34, 94, 94, 7), Capsule(x, y, 34, 94, 94, 34, 7))), 0);
            Ui("IconBack", 128, 128, (x, y) => Fill(Min(Capsule(x, y, 80, 26, 42, 64, 8), Capsule(x, y, 42, 64, 80, 102, 8))), 0);
            Ui("IconPause", 128, 128, (x, y) => Fill(Min(Capsule(x, y, 46, 30, 46, 98, 9), Capsule(x, y, 82, 30, 82, 98, 9))), 0);
            Ui("IconSound", 128, 128, (x, y) => Fill(Min(
                Triangle(x, y, new Vector2(24, 50), new Vector2(24, 78), new Vector2(46, 78)),
                Triangle(x, y, new Vector2(24, 50), new Vector2(46, 78), new Vector2(46, 50)),
                Triangle(x, y, new Vector2(46, 50), new Vector2(46, 78), new Vector2(70, 100)),
                Triangle(x, y, new Vector2(46, 50), new Vector2(70, 100), new Vector2(70, 28)),
                Shell(ArcBand(x, y, 70, 64, 18, -50f, 50f), 4.5f), Shell(ArcBand(x, y, 70, 64, 34, -50f, 50f), 4.5f))), 0);
            Ui("IconCar", 128, 128, (x, y) =>
            {
                // Side view: body + roof, wheel arches cut out, wheels added back.
                float body = Min(RoundRect(x, y, 64, 56, 52, 14, 9), RoundRect(x, y, 60, 76, 28, 12, 10));
                float arches = Min(Circle(x, y, 36, 42, 14), Circle(x, y, 92, 42, 14));
                float wheels = Min(Circle(x, y, 36, 42, 9), Circle(x, y, 92, 42, 9));
                float window = RoundRect(x, y, 60, 77, 20, 6, 4);
                return Fill(Min(Mathf.Max(Mathf.Max(body, -arches), -window), wheels));
            }, 0);

            // World.
            World("Car", 192, 336, CarBodyPixel, 192, tiled: false);
            World("CarDetails", 192, 336, CarDetailsPixel, 192, tiled: false);
            World("Ground", 128, 128, GroundPixel, 32, tiled: true);
            World("AsphaltTile", 128, 128, AsphaltPixel, 32, tiled: true);
            World("Edge", 64, 8, (x, y) => EdgePixel(x / 64f), 32, tiled: true);
            World("StartLine", 64, 32, (x, y) => ((int)(x / 16) + (int)(y / 16)) % 2 == 0 ? Color.white : Hex("1B2129"), 64, tiled: true);
            World("Pylon", 128, 128, (x, y) =>
            {
                float d = Dist(x, y, 64, 64);
                float core = Mathf.Clamp01(16f - d + 0.5f);
                float halo = Gauss(d, 22f) * 0.75f;
                return White(Mathf.Clamp01(core + halo));
            }, 64, tiled: false);

            Material("Road", "AsphaltTile");
            Material("Edge", "Edge");
            Material("StartLine", "StartLine");
            Material("Ground", "Ground");
            Material("Light", "Pylon");
            Material("Trail", null);

            AssetDatabase.Refresh();
            Debug.Log($"Neon art generated in {Folder}");
        }

        // ---------- world pixels ----------

        // 192 x 336 top-down sports car, nose up, in two layers: the body (white/grey, tinted with the
        // team colour by the SpriteRenderer) and the details on top in their own colours.

        static float CarBodySdf(float x, float y)
        {
            // Tapered core: narrow nose, wide rear.
            float taper = Mathf.Lerp(58f, 44f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 320f, y)));
            float core = RoundRect(96f + (x - 96f) * 50f / taper, y, 96, 168, 50, 158, 30);
            float frontFlare = RoundRect(x, y, 96, 262, 64, 32, 22);
            float rearFlare = RoundRect(x, y, 96, 86, 72, 40, 26);
            return SMin(core, Mathf.Min(frontFlare, rearFlare), 10f);
        }

        static float CarRoofSdf(float x, float y) => RoundRect(x, y, 96, 160, 30, 26, 12);
        static float CarGlassSdf(float x, float y) =>
            Mathf.Max(RoundRect(x, y, 96, 172, 36, 52, 18), -Triangle(x, y, new Vector2(54, 232), new Vector2(64, 232), new Vector2(54, 200)));

        static Color CarBodyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float tyres = Min(RoundRect(x, y, 34, 262, 11, 25, 7), RoundRect(x, y, 158, 262, 11, 25, 7),
                              RoundRect(x, y, 27, 86, 13, 28, 7), RoundRect(x, y, 165, 86, 13, 28, 7));
            c = Over(c, Hex("0E1013"), Fill(tyres));

            float body = CarBodySdf(x, y);
            float inside = Mathf.Clamp01(-body / 20f);
            float shade = Mathf.Lerp(0.4f, 1f, Mathf.Pow(inside, 0.6f));
            float highlight = Mathf.Exp(-Mathf.Pow((x - 76f) / 14f, 2f)) * 0.16f * inside;
            // Darker sills along the sides give the body a waist.
            float sill = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x - 96f) - 40f) / 6f, 2f)) * 0.18f * Mathf.Clamp01((y - 110f) / 40f) * Mathf.Clamp01((230f - y) / 40f);
            float v = Mathf.Clamp01(shade + highlight - sill);
            c = Over(c, new Color(v, v, v), Fill(body));

            float roof = CarRoofSdf(x, y);
            float roofShade = Mathf.Lerp(0.8f, 0.97f, Mathf.Clamp01(1f - Mathf.Abs(x - 96f) / 30f));
            c = Over(c, new Color(roofShade, roofShade, roofShade), Fill(roof));
            float mirrors = Min(RoundRect(x, y, 48, 210, 9, 5, 4), RoundRect(x, y, 144, 210, 9, 5, 4));
            c = Over(c, new Color(0.75f, 0.75f, 0.75f), Fill(mirrors));
            c = Over(c, Hex("08090C"), Stroke(body, 3f) * 0.9f);
            return c;
        }

        static Color CarDetailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = CarBodySdf(x, y);

            // Glass around the roof, with a diagonal reflection.
            float glass = Mathf.Max(CarGlassSdf(x, y), -CarRoofSdf(x, y) + 1.5f);
            float reflection = Mathf.Clamp01(1f - Mathf.Abs((x - 74f) - (y - 205f) * 0.55f) / 9f) * 0.5f;
            Color glassColor = Color.Lerp(Hex("080C12"), Hex("6F8FAA"), Mathf.Clamp01((y - 140f) / 100f) * 0.3f + reflection);
            c = Over(c, glassColor, Fill(glass));

            // Thin twin stripes over hood, roof and tail.
            float stripes = Min(RoundRect(x, y, 89, 172, 2.6f, 146, 1.3f), RoundRect(x, y, 103, 172, 2.6f, 146, 1.3f));
            stripes = Mathf.Max(stripes, body + 8f);
            stripes = Mathf.Max(stripes, -Mathf.Max(CarGlassSdf(x, y), -CarRoofSdf(x, y)));
            c = Over(c, new Color(1f, 1f, 1f, 0.75f), Fill(stripes));

            float vents = Min(Capsule(x, y, 76, 280, 70, 300, 2.2f), Capsule(x, y, 116, 280, 122, 300, 2.2f));
            c = Over(c, Hex("12151A"), Fill(vents));
            float intakes = Min(Capsule(x, y, 44, 112, 50, 146, 3.5f), Capsule(x, y, 148, 112, 142, 146, 3.5f));
            c = Over(c, Hex("0B0D10"), Fill(intakes));

            float heads = Min(Capsule(x, y, 60, 306, 78, 318, 3.6f), Capsule(x, y, 132, 306, 114, 318, 3.6f));
            c = Over(c, Hex("F2FDFF"), Fill(heads));
            float tail = Capsule(x, y, 50, 18, 142, 18, 4f);
            c = Over(c, Hex("FF2449"), Fill(tail));
            c = Over(c, Hex("0B0D10"), Fill(RoundRect(x, y, 96, 8, 30, 3.5f, 2)));
            return c;
        }

        // Polynomial smooth minimum: blends two distance fields with a rounded seam of size k.
        static float SMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        static Color GroundPixel(float x, float y)
        {
            Color c = Hex("0E1116");
            float gx = Mathf.Min(x, 128f - x), gy = Mathf.Min(y, 128f - y);
            float line = Mathf.Clamp01(1.4f - Mathf.Min(gx, gy));
            float noise = (Hash((int)x, (int)y) - 0.5f) * 0.012f;
            c = new Color(c.r + noise, c.g + noise, c.b + noise);
            return Color.Lerp(c, new Color(0.24f, 0.32f, 0.18f), line * 0.55f);
        }

        static Color AsphaltPixel(float x, float y)
        {
            float n = Hash((int)x, (int)y);
            float v = 0.105f + (n - 0.5f) * 0.035f + (n > 0.985f ? 0.05f : 0f);
            return new Color(v * 0.95f, v, v * 1.12f);
        }

        // Across the edge strip: road side (u=0) fades in, a bright core line, glow outwards.
        static Color EdgePixel(float u)
        {
            float core = Mathf.Exp(-Mathf.Pow((u - 0.28f) / 0.07f, 2f));
            float glow = Mathf.Exp(-Mathf.Pow((u - 0.28f) / 0.3f, 2f)) * 0.45f;
            return White(Mathf.Clamp01(core + glow));
        }

        // ---------- SDF helpers (distance in pixels, negative inside) ----------

        static float Dist(float x, float y, float cx, float cy) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        static float Circle(float x, float y, float cx, float cy, float r) => Dist(x, y, cx, cy) - r;

        static float RoundRect(float x, float y, float cx, float cy, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x - cx) - (hw - r), qy = Mathf.Abs(y - cy) - (hh - r);
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        static float Diamond(float x, float y, float cx, float cy, float half, float r)
        {
            // Rounded square rotated 45 degrees.
            float dx = x - cx, dy = y - cy;
            float rx = (dx + dy) * 0.70710678f, ry = (dy - dx) * 0.70710678f;
            float side = half * 0.70710678f;
            return RoundRect(rx, ry, 0, 0, side, side, r);
        }

        static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            Vector2 p = new(x - ax, y - ay), ba = new(bx - ax, by - ay);
            float h = Mathf.Clamp01(Vector2.Dot(p, ba) / ba.sqrMagnitude);
            return (p - ba * h).magnitude - r;
        }

        static float ArcBand(float x, float y, float cx, float cy, float radius, float fromDeg, float toDeg)
        {
            // Distance to an arc of a circle (as a line), limited to an angle range.
            float a = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
            float lo = fromDeg, hi = toDeg;
            bool inRange = InAngle(a, lo, hi);
            if (inRange) return Mathf.Abs(Dist(x, y, cx, cy) - radius);
            Vector2 e1 = new(cx + Mathf.Cos(lo * Mathf.Deg2Rad) * radius, cy + Mathf.Sin(lo * Mathf.Deg2Rad) * radius);
            Vector2 e2 = new(cx + Mathf.Cos(hi * Mathf.Deg2Rad) * radius, cy + Mathf.Sin(hi * Mathf.Deg2Rad) * radius);
            return Mathf.Min(Dist(x, y, e1.x, e1.y), Dist(x, y, e2.x, e2.y));
        }

        static bool InAngle(float a, float lo, float hi)
        {
            float span = Mathf.Repeat(hi - lo, 360f);
            return Mathf.Repeat(a - lo, 360f) <= span;
        }

        static float Shell(float d, float halfWidth) => Mathf.Abs(d) - halfWidth;

        static float Triangle(float x, float y, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 p = new(x, y);
            float d = Mathf.Min(SegDist(p, a, b), SegDist(p, b, c), SegDist(p, c, a));
            bool inside = Cross(b - a, p - a) * Cross(b - a, c - a) >= 0 &&
                          Cross(c - b, p - b) * Cross(c - b, a - b) >= 0 &&
                          Cross(a - c, p - c) * Cross(a - c, b - c) >= 0;
            return inside ? -d : d;
        }

        static float Trapezoid(float x, float y) =>
            // car roof outline for the car icon
            Mathf.Max(Triangle(x, y, new Vector2(34, 70), new Vector2(94, 70), new Vector2(64, 98)) - 0f,
                      RoundRect(x, y, 64, 80, 26, 14, 6));

        static float Star(float x, float y, float cx, float cy, float outer, float inner)
        {
            // Distance to a 5-point star polygon (point up in texture space = +Y).
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                pts[i] = new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r);
            }
            Vector2 p = new(x, y);
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                d = Mathf.Min(d, SegDist(p, pts[j], pts[i]));
                if ((pts[i].y > y) != (pts[j].y > y) &&
                    x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            }
            return inside ? -d : d;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / ba.sqrMagnitude);
            return (pa - ba * h).magnitude;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        static float Min(params float[] values) { float m = float.MaxValue; foreach (var v in values) m = Mathf.Min(m, v); return m; }

        static float Fill(float d) => Mathf.Clamp01(0.5f - d);
        static float Stroke(float d, float width) => Mathf.Clamp01(width * 0.5f - Mathf.Abs(d) + 0.5f);
        static float Gauss(float d, float sigma) => Mathf.Exp(-(d * d) / (2f * sigma * sigma));
        static Color White(float a) => new(1f, 1f, 1f, a);
        static Color Fill(float d, Color c) { c.a *= Fill(d); return c; }

        static Color Over(Color under, Color over, float coverage)
        {
            float a = coverage * over.a;
            float outA = a + under.a * (1f - a);
            if (outA <= 0f) return new Color(0, 0, 0, 0);
            Color rgb = (over * a + under * under.a * (1f - a)) / outA;
            rgb.a = outA;
            return rgb;
        }

        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0xFFFF) / 65535f;
        }

        // ---------- writing ----------

        static void Ui(string name, int w, int h, Func<float, float, float> coverage, int border) =>
            Write(name, w, h, (x, y) => White(coverage(x, y)), 100, tiled: false, border);

        static void Ui(string name, int w, int h, Func<float, float, Color> painter, int border) =>
            Write(name, w, h, (x, y) => painter(x, y), 100, tiled: false, border);

        static void World(string name, int w, int h, Painter painter, int ppu, bool tiled) =>
            Write(name, w, h, painter, ppu, tiled, 0);

        static void Write(string name, int w, int h, Painter painter, int ppu, bool tiled, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = painter(x + 0.5f, y + 0.5f);
            tex.SetPixels(pixels);
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = tiled;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.spriteBorder = new Vector4(border, border, border, border);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static void Material(string name, string textureName)
        {
            string path = $"{Folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.mainTexture = textureName == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{textureName}.png");
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
