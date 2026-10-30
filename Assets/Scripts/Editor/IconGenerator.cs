using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Paints the app icon (1024 x 1024): the orange snowmobile flying high off a snow-park kicker in the middle of
    /// a spin, seen from above like in the game - a groomed piste running up the picture between blue nets, the
    /// kicker with its blue lip below, the powder it threw up, white trick arcs round the sled, its shadow far
    /// below on the snow, sparkles, snowflakes and snowy pines in the corners; the light falls on the middle and
    /// the edges darken. Saves it to Assets/Art/AppIcon.png and sets it as the app icon for iOS and as the default
    /// icon. iOS rounds the corners itself, so the picture fills the square.
    /// </summary>
    public static class IconGenerator
    {
        const int Size = 1024;
        const string IconPath = "Assets/Art/AppIcon.png";

        // The piste runs diagonally up the picture (along D), PisteHalf wide each side of the middle.
        static readonly Vector2 O = new(512f, 512f), D = new Vector2(1f, 1f).normalized, N = new Vector2(-1f, 1f).normalized;
        const float PisteHalf = 300f, Lip = -300f, KickerHalf = 165f;
        // The sled: up in the air past the lip, turned 30 degrees into its spin, big (close to the camera).
        const float SledAlong = 95f, Spin = 32f, SledScale = 1.6f;

        static readonly Color Outline = Hex("1B2A44");
        static readonly Color TreeShadow = new(0.1f, 0.2f, 0.4f, 0.25f);

        [MenuItem("SomeGame/Generate App Icon")]
        public static void Generate()
        {
            var body = LoadPixels(ArtGenerator.Folder + "/SnowmobileIcon.png", out int cw, out int ch);
            var details = LoadPixels(ArtGenerator.Folder + "/SnowmobileDetails.png", out _, out _);

            Vector2 centre = O + D * SledAlong;
            float heading = Mathf.Atan2(D.y, D.x) * Mathf.Rad2Deg + Spin;
            float angle = (heading - 90f) * Mathf.Deg2Rad; // the sprite's nose points up
            Vector2 tail = centre - new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad)) * 150f * SledScale;
            var puffs = Puffs(tail);

            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    px[y * Size + x] = Spray(Background(fx, fy), fx, fy, puffs);
                }

            Color orange = Hex("FF7A1A");
            // High up: its shadow lies far below on the snow, smaller and soft.
            Stamp(px, body, details, cw, ch, centre + new Vector2(70f, -230f), angle, SledScale * 0.7f, orange, shadow: true);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    px[i] = Arcs(px[i], x + 0.5f, y + 0.5f, centre, heading);
                    px[i] = Snowflakes(px[i], x + 0.5f, y + 0.5f); // (under the sled: it is flying through them)
                }
            Stamp(px, body, details, cw, ch, centre, angle, SledScale, orange, shadow: false);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    px[i] = Sparkles(px[i], x + 0.5f, y + 0.5f);
                    // Light on the middle, darker (night-blue) edges.
                    float v = Dist(x + 0.5f, y + 0.5f, 540f, 540f) / 720f;
                    px[i] = Over(px[i], new Color(0.04f, 0.08f, 0.2f, 1f), Mathf.Clamp01(v * v - 0.1f) * 0.6f);
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

        static Color Background(float x, float y)
        {
            Vector2 p = new Vector2(x, y) - O;
            float u = Vector2.Dot(p, D), v = Vector2.Dot(p, N);

            // Deep powder off the piste: soft blue drifts.
            float drift = Noise(x / 110f, y / 110f, 64) * 0.7f + Noise(x / 35f, y / 35f, 128) * 0.3f;
            Color c = Color.Lerp(Hex("C9D9EE"), Hex("E6EFFA"), drift);

            // The groomed piste: brighter, its corduroy running straight down it, a soft shadow along its edges.
            float edge = Mathf.Abs(v) - PisteHalf;
            c = Over(c, Hex("9DB3D3"), Fill(edge - 14f) * 0.35f);
            Color piste = Color.Lerp(Hex("EEF4FC"), Hex("FBFDFF"), Noise(x / 60f, y / 60f, 64));
            c = Over(c, piste, Fill(edge));
            float groove = Mathf.Abs(Mathf.Repeat(v, 30f) - 15f) - 2.5f;
            c = Over(c, Hex("C3D3EA"), Fill(groove) * Fill(edge + 8f) * 0.3f);

            // Blue safety nets along both sides: dashes on poles.
            foreach (float side in new[] { -1f, 1f })
            {
                float net = Mathf.Abs(v - side * (PisteHalf + 22f)) - 6f;
                bool dash = Mathf.Repeat(u, 64f) < 46f;
                if (dash) c = Over(c, Hex("1E6FD9"), Fill(net));
                float pole = Dist(Mathf.Repeat(u + 9f, 64f), v, 0f, side * (PisteHalf + 22f)) - 8f;
                c = Over(c, Hex("F4F7FB"), Fill(pole));
                c = Over(c, Outline, Stroke(pole, 3f) * 0.6f);
            }

            // The kicker below the sled: a snow table top, its take-off shaded up to the hard blue lip.
            float kickerSide = Mathf.Abs(v) - KickerHalf, kickerAlong = Mathf.Max(Lip - 330f - u, u - Lip);
            float kicker = Mathf.Max(kickerSide, kickerAlong);
            c = Over(c, Hex("8FA8CC"), Fill(Mathf.Max(Mathf.Abs(v - 14f) - KickerHalf - 18f, Mathf.Max(Lip - 330f - u, u - Lip - 26f))) * 0.4f);
            float up = Mathf.Clamp01((u - (Lip - 330f)) / 330f);
            Color ramp = Color.Lerp(Hex("D3E0F2"), Hex("FFFFFF"), up * up);
            c = Over(c, ramp, Fill(kicker));
            c = Over(c, Hex("AFC3DF"), Fill(Mathf.Max(Mathf.Abs(Mathf.Abs(v) - KickerHalf + 14f) - 14f, kickerAlong)) * 0.6f); // its side walls
            c = Over(c, Hex("1E6FD9"), Fill(Mathf.Max(kickerSide, Mathf.Abs(u - Lip + 12f) - 12f)));               // the lip
            c = Over(c, Outline, Stroke(kicker, 5f) * 0.55f);

            // Snowy pines in the corners, off the piste.
            foreach (var (tx, ty, s) in Trees)
            {
                if (Mathf.Abs(x - tx) > 160f * s || Mathf.Abs(y - ty) > 160f * s) continue;
                c = Pine(c, x, y, tx, ty, s);
            }
            return c;
        }

        static readonly (float x, float y, float s)[] Trees =
        {
            (60f, 980f, 1.25f), (250f, 1060f, 0.95f), (-20f, 770f, 1.05f), (170f, 830f, 0.7f),
            (980f, 60f, 1.25f), (1060f, 270f, 0.95f), (780f, -30f, 1f), (870f, 200f, 0.7f),
        };

        // Powder thrown up off the lip, trailing after the sled: soft white clouds, blue in their shade.
        static List<(Vector2 p, float r)> Puffs(Vector2 tail)
        {
            var list = new List<(Vector2, float)>();
            Vector2 lip = O + D * Lip;
            for (int k = 0; k < 9; k++)
            {
                float t = k / 8f;
                Vector2 p = Vector2.Lerp(lip, tail, t) + N * ((Hash(k, 3) - 0.5f) * 170f * (1f - t * 0.5f)) - D * 20f;
                list.Add((p, Mathf.Lerp(78f, 26f, t) + Hash(k, 7) * 14f));
            }
            return list;
        }

        static Color Spray(Color c, float x, float y, List<(Vector2 p, float r)> puffs)
        {
            float d = 1e6f, light = 0f;
            foreach (var (p, r) in puffs)
            {
                d = SMin(d, Circle(x, y, p.x, p.y, r), 20f);
                light = Mathf.Max(light, Fill(Circle(x, y, p.x - r * 0.25f, p.y + r * 0.25f, r * 0.65f) / 3f));
            }
            if (d > 6f) return c;
            c = Over(c, Hex("CFDDF0"), Fill(d) * 0.95f);
            c = Over(c, Color.white, light * Fill(d + 3f));
            return Over(c, Hex("6F86AA"), Stroke(d, 4f) * 0.6f);
        }

        // The spin: two white swooshes curving round the sled (the way it turns), outlined, thinning to their tails.
        static Color Arcs(Color c, float x, float y, Vector2 centre, float heading)
        {
            float r = Dist(x, y, centre.x, centre.y);
            if (r < 280f || r > 400f) return c;
            float a = Mathf.Atan2(y - centre.y, x - centre.x) * Mathf.Rad2Deg;
            foreach (float start in new[] { heading + 25f, heading + 205f })
            {
                float into = Mathf.Repeat(a - start, 360f);
                if (into > 95f) continue;
                float t = into / 95f, half = Mathf.Lerp(16f, 3f, t); // thick at its head, thin at its tail
                float band = Mathf.Abs(r - 340f) - half;
                c = Over(c, Outline, Fill(band - 5f) * 0.7f * (1f - t * 0.6f));
                c = Over(c, Color.white, Fill(band));
            }
            return c;
        }

        static readonly Vector3[] Stars = { new(820f, 830f, 46f), new(300f, 690f, 30f), new(905f, 600f, 26f), new(700f, 960f, 22f), new(420f, 330f, 20f) };

        // Golden sparkles round the jump.
        static Color Sparkles(Color c, float x, float y)
        {
            foreach (var s in Stars)
            {
                if (Mathf.Abs(x - s.x) > s.z * 2.5f || Mathf.Abs(y - s.y) > s.z * 2.5f) continue;
                c = Over(c, Hex("FFE680"), Gauss(Dist(x, y, s.x, s.y), s.z * 0.5f) * 0.5f);
                float star = Star(x, y, s.x, s.y, s.z, s.z * 0.28f, 4);
                c = Over(c, Outline, Stroke(star, 5f) * 0.7f);
                c = Over(c, Hex("FFD23F"), Fill(star));
                c = Over(c, Color.white, Fill(Star(x, y, s.x, s.y, s.z * 0.5f, s.z * 0.14f, 4)));
            }
            return c;
        }

        static Color Snowflakes(Color c, float x, float y)
        {
            const float Cell = 64f;
            int gx = Mathf.FloorToInt(x / Cell), gy = Mathf.FloorToInt(y / Cell);
            if (Hash(gx, gy + 50) > 0.55f) return c;
            float fx = (gx + 0.15f + 0.7f * Hash(gx, gy)) * Cell, fy = (gy + 0.15f + 0.7f * Hash(gx + 9, gy)) * Cell;
            float r = 3.5f + 4f * Hash(gx, gy + 3);
            return Over(c, Color.white, Fill(Circle(x, y, fx, fy, r)) * 0.9f);
        }

        // A dark pine with snow on its boughs.
        static Color Pine(Color c, float x, float y, float cx, float cy, float s)
        {
            float lx = (x - cx) / s, ly = (y - cy) / s;
            c = Over(c, TreeShadow, Mathf.Clamp01(0.5f - Star(lx, ly, 20f, -24f, 104f, 80f, 11, 0.2f) / 10f));
            float outer = Star(lx, ly, 0f, 0f, 104f, 80f, 11, 0.2f);
            c = Over(c, Hex("24584A"), Fill(outer * s));
            c = Over(c, Hex("FFFFFF"), Fill(Star(lx, ly, -10f, 10f, 84f, 58f, 11, 0.45f) * s) * Fill((outer + 14f) * s) * 0.85f);
            c = Over(c, Hex("2F6A57"), Fill(Star(lx, ly, -4f, 5f, 64f, 46f, 10, 0.5f) * s));
            c = Over(c, Hex("F4F8FF"), Fill(Star(lx, ly, -9f, 9f, 40f, 28f, 8, 0.1f) * s));
            return Over(c, Outline, Stroke(outer * s, 5f) * 0.8f);
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
                        px[i] = Over(px[i], new Color(0.08f, 0.16f, 0.36f, 0.4f), b.a);
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
