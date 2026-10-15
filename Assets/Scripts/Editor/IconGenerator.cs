using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Paints the app icon (1024 x 1024): a close, bold shot of the orange buggy drifting through a bend,
    /// seen from above like in the game. The road fills most of the picture, with kerbs on both sides,
    /// puffy tyre smoke and purple drift sparks behind the rear wheels and a few trees in the corners;
    /// the light falls on the car in the middle and the edges darken.
    /// Saves it to Assets/Art/AppIcon.png and sets it as the app icon for iOS and as the default icon.
    /// iOS rounds the corners itself, so the picture fills the square.
    /// </summary>
    public static class IconGenerator
    {
        const int Size = 1024;
        const string IconPath = "Assets/Art/AppIcon.png";

        // The bend: a circle around C (bottom right) sweeping from the bottom left to the top right.
        static readonly Vector2 C = new(1180f, -110f);
        const float R = 900f, HalfWidth = 300f;
        // The buggy sits on the bend at this angle (degrees around C), its nose turned into the corner.
        const float CarAt = 138f, DriftAngle = 22f, CarScale = 1.95f;

        static readonly Color Outline = Hex("14231E");
        static readonly Color TreeShadow = new(0.05f, 0.12f, 0.08f, 0.32f);

        [MenuItem("SomeGame/Generate App Icon")]
        public static void Generate()
        {
            var car = LoadPixels(ArtGenerator.Folder + "/Car.png", out int cw, out int ch);
            var details = LoadPixels(ArtGenerator.Folder + "/CarDetails.png", out _, out _);

            float t = CarAt * Mathf.Deg2Rad;
            Vector2 centre = C + R * new Vector2(Mathf.Cos(t), Mathf.Sin(t));
            // Driving clockwise round C; the nose points into the bend by the drift angle.
            float heading = Mathf.Atan2(-Mathf.Cos(t), Mathf.Sin(t)) * Mathf.Rad2Deg - DriftAngle;
            float angle = (heading - 90f) * Mathf.Deg2Rad; // the sprite's nose points up
            Vector2 Local(float x, float y) => centre + CarScale * new Vector2(x * Mathf.Cos(angle) - y * Mathf.Sin(angle), x * Mathf.Sin(angle) + y * Mathf.Cos(angle));
            Vector2 rearLeft = Local(-70f, -112f), rearRight = Local(70f, -112f);
            var puffs = Puffs(rearLeft, rearRight);

            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    px[y * Size + x] = Smoke(Background(fx, fy, rearLeft, rearRight), fx, fy, puffs);
                }

            Color orange = Hex("FF7A1A");
            Stamp(px, car, details, cw, ch, centre + new Vector2(22f, -30f), angle, CarScale, orange, shadow: true);
            Stamp(px, car, details, cw, ch, centre, angle, CarScale, orange, shadow: false);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    px[i] = Sparks(px[i], x + 0.5f, y + 0.5f, rearLeft, rearRight, heading + 180f);
                    // Light on the middle, darker edges.
                    float v = Dist(x + 0.5f, y + 0.5f, 540f, 520f) / 720f;
                    px[i] = Over(px[i], new Color(0.02f, 0.06f, 0.05f, 1f), Mathf.Clamp01(v * v - 0.12f) * 0.55f);
                }

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

        static Color Background(float x, float y, Vector2 rearLeft, Vector2 rearRight)
        {
            float r = Dist(x, y, C.x, C.y);
            float theta = Mathf.Atan2(y - C.y, x - C.x) * Mathf.Rad2Deg;
            float off = r - R; // across the road: negative is the inside of the bend

            // Grass with soft patches and speckles, as on the tracks.
            float patch = Noise(x / 90f, y / 90f, 64);
            Color c = Color.Lerp(Hex("58A535"), Hex("6DBA43"), patch);
            float speck = Hash((int)(x / 9f), (int)(y / 9f));
            if (speck > 0.993f) c = Over(c, Hex("F2D46B"), 0.8f);
            else if (speck < 0.01f) c = Over(c, Hex("9AD164"), 0.8f);

            // Asphalt with white edge lines, a red and cream kerb on both sides, a dark outline.
            float road = Mathf.Abs(off) - HalfWidth;
            c = Over(c, Hex("3F4A3E"), Fill(road - 16f) * 0.4f);
            Color asphalt = Color.Lerp(Hex("55595A"), Hex("62666A"), Noise(x / 8f, y / 8f, 256));
            c = Over(c, asphalt, Fill(road));
            c = Over(c, Hex("F4EEDC"), Fill(Shell(Mathf.Abs(off) - (HalfWidth - 30f), 9f)));
            float kerb = Shell(Mathf.Abs(off) - HalfWidth - 24f, 24f);
            float run = theta * Mathf.Deg2Rad * (R + Mathf.Sign(off) * (HalfWidth + 24f));
            bool stripe = Mathf.Repeat(run / 58f, 2f) < 1f;
            c = Over(c, stripe ? Hex("E8402E") : Hex("F4EEDC"), Fill(kerb));
            c = Over(c, Outline, Stroke(kerb, 4f) * 0.5f);
            c = Over(c, Outline, Stroke(Mathf.Abs(off) - HalfWidth - 48f, 6f) * 0.6f);

            // Skid marks from the rear wheels, back along the bend and fading out.
            c = Skid(c, x, y, r, theta, rearLeft);
            c = Skid(c, x, y, r, theta, rearRight);

            // A few trees in the corners, off the road.
            foreach (var (tx, ty, s, pine) in Trees)
            {
                if (Mathf.Abs(x - tx) > 160f * s || Mathf.Abs(y - ty) > 160f * s) continue;
                c = pine ? Pine(c, x, y, tx, ty, s) : RoundTree(c, x, y, tx, ty, s);
            }
            return c;
        }

        static readonly (float x, float y, float s, bool pine)[] Trees =
        {
            (40f, 1000f, 1.3f, true), (220f, 1060f, 1f, false), (-10f, 800f, 0.9f, false),
            (1010f, 150f, 1.1f, false), (1070f, 330f, 0.8f, true), (850f, -20f, 0.85f, true),
        };

        static Color Skid(Color c, float x, float y, float r, float theta, Vector2 wheel)
        {
            float wr = Dist(wheel.x, wheel.y, C.x, C.y);
            float wt = Mathf.Atan2(wheel.y - C.y, wheel.x - C.x) * Mathf.Rad2Deg;
            float behind = theta - wt; // the car drives clockwise, so the marks lie at larger angles
            if (behind < -1f || behind > 40f) return c;
            float fade = Mathf.Clamp01(1.3f - behind / 30f) * Mathf.Clamp01((behind + 1f) / 3f);
            float width = Mathf.Lerp(40f, 30f, behind / 40f);
            return Over(c, Hex("2A2E2E"), Fill(Mathf.Abs(r - wr) - width * 0.5f) * 0.8f * fade);
        }

        // Tyre smoke: puffs trailing back along the bend from both rear wheels, growing as they drift off.
        static List<(Vector2 p, float r)> Puffs(Vector2 rearLeft, Vector2 rearRight)
        {
            var list = new List<(Vector2, float)>();
            for (int k = 0; k < 6; k++)
                foreach (var w in new[] { rearLeft, rearRight })
                {
                    int side = w == rearLeft ? 1 : -1;
                    float wr = Vector2.Distance(w, C), wt = Mathf.Atan2(w.y - C.y, w.x - C.x);
                    float a = wt + 0.045f + k * 0.055f + (Hash(k, side + 5) - 0.5f) * 0.03f;
                    float rr = wr + side * (6f + k * 16f) + (Hash(k, side + 9) - 0.5f) * 40f;
                    list.Add((C + rr * new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 30f + k * 9f + Hash(k, side + 13) * 12f));
                }
            return list;
        }

        // One soft cartoon cloud from all the puffs: light grey body, white tops (light from the top left),
        // a single outline around the whole cloud, thinning out towards its far end.
        static Color Smoke(Color c, float x, float y, List<(Vector2 p, float r)> puffs)
        {
            float d = 1e6f, light = 0f, age = 0f;
            for (int i = 0; i < puffs.Count; i++)
            {
                var (p, r) = puffs[i];
                float di = Circle(x, y, p.x, p.y, r);
                if (di < d) age = i / (float)puffs.Count;
                d = SMin(d, di, 18f);
                light = Mathf.Max(light, Fill(Circle(x, y, p.x - r * 0.25f, p.y + r * 0.25f, r * 0.62f) / 3f));
            }
            if (d > 6f) return c;
            float alpha = Mathf.Lerp(0.97f, 0.7f, age);
            c = Over(c, Hex("CCD3D9"), Fill(d) * alpha);
            c = Over(c, Color.white, light * Fill(d + 3f) * alpha);
            return Over(c, Hex("56616B"), Stroke(d, 4f) * 0.5f * alpha);
        }

        // Purple drift sparks spraying back from the rear wheels, with a glow (the top drift tier).
        static Color Sparks(Color c, float x, float y, Vector2 rearLeft, Vector2 rearRight, float back)
        {
            Color purple = Hex("B66BFF"), light = Hex("E9D4FF");
            foreach (var w in new[] { rearLeft, rearRight })
            {
                float d = Dist(x, y, w.x, w.y);
                if (d > 220f) continue;
                c = Over(c, purple, Gauss(d, 70f) * 0.7f);
                c = Over(c, light, Gauss(d, 22f) * 0.95f);
                // A few sparks thrown backwards and outwards.
                for (int k = 0; k < 5; k++)
                {
                    float a = (back - 50f + k * 25f + (w == rearLeft ? -8f : 8f)) * Mathf.Deg2Rad;
                    float len = 100f + 45f * Hash(k, w == rearLeft ? 1 : 2);
                    Vector2 a0 = w + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 36f, a1 = w + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len;
                    float seg = SegDist(new Vector2(x, y), a0, a1) - 7f;
                    c = Over(c, purple, Fill(seg - 4f));
                    c = Over(c, Color.white, Fill(seg + 1.5f));
                }
            }
            return c;
        }

        static Color Pine(Color c, float x, float y, float cx, float cy, float s)
        {
            float lx = (x - cx) / s, ly = (y - cy) / s;
            c = Over(c, TreeShadow, Mathf.Clamp01(0.5f - Star(lx, ly, 18f, -22f, 104f, 80f, 11, 0.2f) / 10f));
            float outer = Star(lx, ly, 0f, 0f, 104f, 80f, 11, 0.2f);
            c = Over(c, Hex("2F6A4A"), Fill(outer * s));
            c = Over(c, Hex("3E7A57"), Fill(Star(lx, ly, -4f, 5f, 74f, 56f, 10, 0.5f) * s));
            c = Over(c, Hex("4F8F62"), Fill(Star(lx, ly, -7f, 9f, 44f, 33f, 8, 0.1f) * s));
            c = Over(c, Hex("6FAE74"), Fill(Circle(lx, ly, -9f, 12f, 11f) * s));
            return Over(c, Outline, Stroke(outer * s, 5f) * 0.8f);
        }

        static Color RoundTree(Color c, float x, float y, float cx, float cy, float s)
        {
            float lx = (x - cx) / s, ly = (y - cy) / s;
            float Blob(float ox, float oy)
            {
                float d = Circle(lx, ly, ox, oy, 62f);
                for (int i = 0; i < 7; i++)
                {
                    float a = i * Mathf.PI * 2f / 7f + 0.4f;
                    d = SMin(d, Circle(lx, ly, ox + Mathf.Cos(a) * 60f, oy + Mathf.Sin(a) * 60f, 40f), 14f);
                }
                return d;
            }
            c = Over(c, TreeShadow, Mathf.Clamp01(0.5f - Blob(18f, -18f) / 10f));
            float crown = Blob(0f, 0f);
            c = Over(c, Hex("3E8A35"), Fill(crown * s));
            c = Over(c, Hex("5BAA3F"), Fill(Circle(lx, ly, -16f, 16f, 70f) * s) * Fill((crown + 10f) * s));
            c = Over(c, Hex("8ACB52"), Fill(Circle(lx, ly, -28f, 30f, 30f) * s) * Fill((crown + 20f) * s));
            return Over(c, Outline, Stroke(crown * s, 5f) * 0.8f);
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
