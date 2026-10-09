using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Generates the Alpine Rally art set into Assets/Art/Rally: UI shapes and icons (white, tinted in
    /// the UI), the rally car, the race world textures (grass, asphalt, gravel shoulder, centre line,
    /// start line, checkpoint post), the scenery atlas (pine, tree, chalet, rock) and the materials.
    /// Flat illustration style: dark outlines and baked drop shadows (light from the top left).
    /// </summary>
    public static class ArtGenerator
    {
        public const string Folder = "Assets/Art/Rally";

        static readonly Color Ink = Hex("173D33");
        static readonly Color Outline = Hex("14231E");
        static readonly Color Shadow = new(0.05f, 0.12f, 0.08f, 0.32f);

        [MenuItem("SomeGame/Generate Art")]
        public static void GenerateAll()
        {
            GenerateUi();
            GenerateWorld();
            AssetDatabase.Refresh();
            Debug.Log($"Art generated in {Folder}");
        }

        // ================================================================== UI (white, tinted)

        static void Ui(string name, int w, int h, System.Func<float, float, float> coverage, int border = 0) =>
            Write(Folder, name, w, h, (x, y) => White(coverage(x, y)), 100, false, border);

        static void GenerateUi()
        {
            Ui("Round", 128, 128, (x, y) => Fill(RoundRect(x, y, 64, 64, 64, 64, 40)), 44);
            Ui("Pill", 128, 128, (x, y) => Fill(Circle(x, y, 64, 64, 63)), 63);
            Ui("Circle", 256, 256, (x, y) => Fill(Circle(x, y, 128, 128, 126)));
            Ui("Ring", 256, 256, (x, y) => Stroke(Circle(x, y, 128, 128, 120), 12f));
            Ui("RingDashed", 256, 256, (x, y) =>
            {
                float a = Mathf.Atan2(y - 128, x - 128) / (Mathf.PI * 2f) * 28f;
                float dash = Mathf.Clamp01((Mathf.Abs(Mathf.Repeat(a, 1f) - 0.5f) - 0.18f) * 40f);
                return Stroke(Circle(x, y, 128, 128, 121), 9f) * dash;
            });
            Write(Folder, "Checker", 64, 64, (x, y) => ((int)(x / 32) + (int)(y / 32)) % 2 == 0 ? Ink : Color.white, 100, true);

            Ui("IconMenu", 128, 128, (x, y) => Fill(Min(Capsule(x, y, 28, 38, 100, 38, 8), Capsule(x, y, 28, 64, 100, 64, 8), Capsule(x, y, 28, 90, 100, 90, 8))));
            Ui("IconStar", 128, 128, (x, y) => Fill(Star(x, y, 64, 62, 60, 26)));
            Ui("IconLock", 128, 128, (x, y) => Fill(Min(RoundRect(x, y, 64, 46, 38, 32, 9),
                Shell(ArcBand(x, y, 64, 80, 22, 0f, 180f), 7f), Capsule(x, y, 42, 80, 42, 66, 7), Capsule(x, y, 86, 80, 86, 66, 7))));
            Ui("IconRetry", 128, 128, (x, y) => Fill(Min(Shell(ArcBand(x, y, 64, 62, 36, 100f, 40f), 8f),
                Triangle(x, y, new Vector2(46, 118), new Vector2(46, 80), new Vector2(80, 99)))));
            Ui("IconPlay", 128, 128, (x, y) => Fill(Triangle(x, y, new Vector2(40, 22), new Vector2(40, 106), new Vector2(108, 64)) + 2f));
            Ui("IconClose", 128, 128, (x, y) => Fill(Min(Capsule(x, y, 34, 34, 94, 94, 9), Capsule(x, y, 34, 94, 94, 34, 9))));
            Ui("IconSound", 128, 128, (x, y) => Fill(Min(
                RoundRect(x, y, 34, 64, 12, 16, 3),
                Triangle(x, y, new Vector2(40, 50), new Vector2(40, 78), new Vector2(72, 104)),
                Triangle(x, y, new Vector2(40, 50), new Vector2(72, 104), new Vector2(72, 24)),
                Shell(ArcBand(x, y, 72, 64, 18, -50f, 50f), 6f), Shell(ArcBand(x, y, 72, 64, 34, -50f, 50f), 6f))));
            Ui("IconFlag", 128, 128, (x, y) =>
            {
                float pole = Capsule(x, y, 30, 14, 30, 114, 6);
                float flag = RoundRect(x, y, 70, 88, 40, 26, 4);
                // Checkered cut-outs on the flag.
                bool hole = flag < 0 && ((int)((x - 30) / 20) + (int)((y - 62) / 13)) % 2 == 1;
                return Mathf.Max(Fill(pole), hole ? 0f : Fill(flag));
            });
        }

        // ================================================================== world

        static void World(string name, int w, int h, System.Func<float, float, Color> painter, int ppu, bool tiled) =>
            Write(Folder, name, w, h, painter, ppu, tiled);

        static void GenerateWorld()
        {
            World("Car", 192, 336, CarBodyPixel, 192, false);
            World("CarDetails", 192, 336, CarDetailsPixel, 192, false);
            World("CarShadow", 192, 336, (x, y) =>
            {
                float d = RoundRect(x, y, 96, 170, 84, 146, 40);
                return new Color(0, 0, 0, Mathf.Clamp01(0.5f - d / 14f));
            }, 192, false);

            World("Grass", 128, 128, GrassPixel, 32, true);
            World("Asphalt", 128, 128, AsphaltPixel, 32, true);
            World("Kerb", 64, 64, KerbPixel, 32, true);
            World("Sand", 64, 64, SandPixel, 32, true);
            World("Line", 8, 8, (x, y) => Hex("F7F5EE"), 32, true);
            World("CenterLine", 16, 64, (x, y) =>
                new Color(0.97f, 0.96f, 0.9f, (y < 32f ? 0.92f : 0f) * Mathf.Clamp01(1f - Mathf.Abs(x - 8f) / 8f * 0.3f)), 32, true);
            World("StartLine", 64, 32, (x, y) => ((int)(x / 16) + (int)(y / 16)) % 2 == 0 ? Color.white : Hex("23272A"), 64, true);
            World("Post", 128, 128, PostPixel, 64, false);
            World("Smoke", 64, 64, (x, y) =>
            {
                // A soft, slightly lumpy puff: white, tinted by the particle colour.
                float a = Mathf.Atan2(y - 32f, x - 32f);
                float r = 26f * (1f + 0.025f * Mathf.Sin(a * 3f + 1f));
                float d = Dist(x, y, 32f, 32f);
                float soft = Mathf.Clamp01(1f - d / r);
                return White(Mathf.SmoothStep(0f, 1f, soft) * 0.9f);
            }, 64, false);
            World("Scenery", 1024, 512, SceneryPixel, 128, false);

            Material("Road", "Asphalt");
            Material("Kerb", "Kerb");
            Material("Sand", "Sand");
            Material("CenterLine", "CenterLine");
            Material("Line", "Line");
            Material("StartLine", "StartLine");
            Material("Ground", "Grass");
            Material("Scenery", "Scenery");
            Material("Trail", null);
            Material("Smoke", "Smoke");
        }

        // ---------- car: off-road buggy, nose up ----------
        // Body layer (tinted with the team colour): bodywork and the tubular roll cage over a dark cockpit,
        // big black wheels on suspension arms. Details layer (untinted): seat highlights, steering wheel,
        // lights and the spare wheel.

        static readonly Vector4[] Wheels =
        {
            new(28, 266, 19, 30), new(164, 266, 19, 30), // front: centre x, y, half width, half length
            new(24, 72, 22, 34), new(168, 72, 22, 34),   // rear: wider
        };

        static float WheelSdf(float x, float y)
        {
            float d = float.MaxValue;
            foreach (var w in Wheels) d = Mathf.Min(d, RoundRect(x, y, w.x, w.y, w.z, w.w, 9));
            return d;
        }

        // Bodywork: tapered nose, side pods and the engine cover at the back.
        static float BodySdf(float x, float y)
        {
            float nose = Polygon(x, y, new[] { new Vector2(44, 226), new Vector2(148, 226), new Vector2(126, 318), new Vector2(66, 318) }) - 8f;
            float pods = Mathf.Min(RoundRect(x, y, 46, 166, 18, 62, 9), RoundRect(x, y, 146, 166, 18, 62, 9));
            float tail = RoundRect(x, y, 96, 70, 54, 42, 14);
            return Mathf.Min(nose, Mathf.Min(pods, tail));
        }

        static float Cockpit(float x, float y) => RoundRect(x, y, 96, 166, 44, 64, 10);

        // Roll cage: outer frame, cross bars, a diagonal and the bars running to the nose and tail.
        static float CageSdf(float x, float y)
        {
            const float r = 4.2f;
            float frame = Shell(RoundRect(x, y, 96, 166, 46, 66, 12), r);
            float bars = Min(Capsule(x, y, 52, 196, 140, 196, r), Capsule(x, y, 52, 138, 140, 138, r), Capsule(x, y, 96, 100, 96, 232, r * 0.9f),
                             Capsule(x, y, 60, 232, 78, 300, r), Capsule(x, y, 132, 232, 114, 300, r),
                             Capsule(x, y, 78, 300, 114, 300, r), Capsule(x, y, 58, 100, 72, 44, r), Capsule(x, y, 134, 100, 120, 44, r));
            return Mathf.Min(frame, bars);
        }

        static Color CarBodyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Suspension arms (dark, nearly untouched by the tint).
            float arms = Min(Capsule(x, y, 48, 254, 28, 262, 4), Capsule(x, y, 48, 278, 28, 272, 4), Capsule(x, y, 144, 254, 164, 262, 4), Capsule(x, y, 144, 278, 164, 272, 4),
                             Capsule(x, y, 54, 62, 24, 66, 4.5f), Capsule(x, y, 54, 84, 24, 80, 4.5f), Capsule(x, y, 138, 62, 168, 66, 4.5f), Capsule(x, y, 138, 84, 168, 80, 4.5f));
            c = Over(c, new Color(0.16f, 0.16f, 0.16f), Fill(arms));
            // Wheels with tread blocks.
            float wheels = WheelSdf(x, y);
            bool tread = Mathf.Repeat(y, 9f) < 4f;
            c = Over(c, tread ? new Color(0.11f, 0.11f, 0.11f) : new Color(0.17f, 0.17f, 0.17f), Fill(wheels));
            c = Over(c, Outline, Stroke(wheels, 4f));

            float body = BodySdf(x, y);
            float inside = Mathf.Clamp01(-body / 14f);
            float v = Mathf.Lerp(0.8f, 1f, inside) - Mathf.Clamp01((x - 120f) / 60f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(body));
            // Raised centre of the nose and the engine cover catch the light.
            c = Over(c, new Color(0.92f, 0.92f, 0.92f), Fill(Polygon(x, y, new[] { new Vector2(80, 240), new Vector2(112, 240), new Vector2(104, 304), new Vector2(88, 304) })) * Fill(body + 4f));
            c = Over(c, new Color(0.9f, 0.9f, 0.9f), Fill(RoundRect(x, y, 96, 62, 26, 24, 8)) * Fill(body + 4f));
            c = Over(c, Outline, Stroke(body, 4.5f));

            // Dark cockpit with two seats.
            float cockpit = Cockpit(x, y);
            c = Over(c, new Color(0.13f, 0.13f, 0.14f), Fill(cockpit));
            float seats = Mathf.Min(RoundRect(x, y, 74, 160, 17, 26, 7), RoundRect(x, y, 118, 160, 17, 26, 7));
            c = Over(c, new Color(0.24f, 0.24f, 0.26f), Fill(seats));
            float heads = Mathf.Min(RoundRect(x, y, 74, 128, 12, 7, 4), RoundRect(x, y, 118, 128, 12, 7, 4));
            c = Over(c, new Color(0.24f, 0.24f, 0.26f), Fill(heads));

            // Roll cage tubes on top (tinted), each with a light top edge.
            float cage = CageSdf(x, y);
            c = Over(c, new Color(0.95f, 0.95f, 0.95f), Fill(cage));
            c = Over(c, Outline, Stroke(cage, 2.5f));
            return c;
        }

        static Color CarDetailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Seat cushions and belts.
            foreach (float sx in new[] { 74f, 118f })
            {
                c = Over(c, new Color(1f, 1f, 1f, 0.08f), Fill(RoundRect(x, y, sx - 4, 166, 9, 16, 5)));
                c = Over(c, new Color(0.85f, 0.3f, 0.2f, 0.9f), Fill(Mathf.Min(Capsule(x, y, sx - 9, 180, sx - 9, 146, 2f), Capsule(x, y, sx + 9, 180, sx + 9, 146, 2f))) * Fill(RoundRect(x, y, sx, 160, 17, 26, 7)));
            }
            // Steering wheel in front of the left seat.
            c = Over(c, new Color(0.08f, 0.08f, 0.08f), Stroke(Circle(x, y, 74, 204, 11), 4f));
            // Headlights on the nose, tail lights and the spare wheel on the engine cover.
            float heads = Mathf.Min(Circle(x, y, 78, 306, 7), Circle(x, y, 114, 306, 7));
            c = Over(c, Hex("FFF6D8"), Fill(heads));
            c = Over(c, Outline, Stroke(heads, 2.5f));
            float tails = Mathf.Min(RoundRect(x, y, 62, 34, 8, 4, 2), RoundRect(x, y, 130, 34, 8, 4, 2));
            c = Over(c, Hex("E0303A"), Fill(tails));
            float spare = Circle(x, y, 96, 62, 19);
            c = Over(c, new Color(0.12f, 0.12f, 0.12f), Fill(spare));
            c = Over(c, new Color(0.45f, 0.45f, 0.47f), Fill(Circle(x, y, 96, 62, 8)));
            c = Over(c, Outline, Stroke(spare, 3f));
            return c;
        }

        // ---------- ground ----------

        static Color GrassPixel(float x, float y)
        {
            float n = Noise(x / 16f, y / 16f, 8) * 0.6f + Noise(x / 6f, y / 6f, 21) * 0.4f;
            Color c = Color.Lerp(Hex("6FB443"), Hex("86C653"), n);
            // Short darker blades.
            float blade = Hash((int)(x / 2), (int)(y / 3));
            if (blade > 0.93f) c = Shade(c, 0.9f);
            // A few tiny flowers.
            int cx = (int)(x / 16), cy = (int)(y / 16);
            float h = Hash(cx * 7 + 3, cy * 13 + 1);
            if (h > 0.82f)
            {
                float fx = cx * 16 + 4 + Hash(cx, cy) * 8, fy = cy * 16 + 4 + Hash(cy, cx) * 8;
                float d = Dist(x, y, fx, fy);
                Color flower = h > 0.93f ? Hex("FFF6DE") : Hex("F7D257");
                c = Over(c, flower, Fill(d - 1.6f));
            }
            return c;
        }

        static Color AsphaltPixel(float x, float y)
        {
            float n = Noise(x / 10f, y / 10f, 13) * 0.5f + Hash((int)x, (int)y) * 0.5f;
            float v = 0.33f + (n - 0.5f) * 0.05f;
            if (Hash((int)x + 7, (int)y * 3) > 0.985f) v += 0.06f;
            return new Color(v * 0.98f, v, v * 1.02f);
        }

        // Across the kerb strip: road side (u = 0) → white edge line → red and white blocks along the road
        // (one colour per half texture repeat) → thin dark outer edge.
        static Color KerbPixel(float x, float y)
        {
            float u = x / 64f;
            if (u < 0.05f) return AsphaltPixel(x, y);
            if (u < 0.3f) return Hex("F7F5EE");
            if (u > 0.93f) return Hex("3C2A22");
            bool red = y < 32f;
            Color c = red ? Hex("D8382E") : Hex("F7F5EE");
            // Slight bevel: lighter toward the road.
            return Color.Lerp(c, Shade(c, 0.86f), Mathf.InverseLerp(0.3f, 0.93f, u));
        }

        // Sandy run-off: grainy sand that fades into the grass with a ragged outer edge (u = 1).
        static Color SandPixel(float x, float y)
        {
            float u = x / 64f;
            float n = Hash((int)x, (int)y) * 0.5f + Noise(x / 5f, y / 5f, 13) * 0.5f;
            Color c = Color.Lerp(Hex("E3C98F"), Hex("F0DCAA"), n);
            float edge = 0.78f + (Noise(y / 7f, 3.5f, 9) - 0.5f) * 0.3f;
            c.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - 0.08f, edge + 0.08f, u));
            return c;
        }

        static Color PostPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Mathf.Clamp01(0.5f - Circle(x, y, 74, 54, 40) / 8f));
            float post = Circle(x, y, 62, 66, 38);
            c = Over(c, Color.white, Fill(post));
            c = Over(c, new Color(0.85f, 0.85f, 0.85f), Fill(Circle(x, y, 68, 60, 24)) * 0.5f);
            c = Over(c, Outline, Stroke(post, 8f));
            return c;
        }

        // ---------- scenery atlas: 4 x 2 cells of 256 px ----------
        // row 0: pine, round tree, bush clump, tyre stack     row 1: chalet, rock, crop field, barn

        public static readonly string[] SceneryCells = { "Pine", "Tree", "Bushes", "Tyres", "Chalet", "Rock", "Field", "Barn" };

        static Color SceneryPixel(float x, float y)
        {
            int col = (int)(x / 256), row = y < 256 ? 0 : 1;
            float lx = x - col * 256, ly = y - row * 256;
            return (row * 4 + col) switch
            {
                0 => Pine(lx, ly),
                1 => RoundTree(lx, ly),
                2 => Bushes(lx, ly),
                3 => Tyres(lx, ly),
                4 => Chalet(lx, ly),
                5 => Rock(lx, ly),
                6 => Field(lx, ly),
                _ => Barn(lx, ly),
            };
        }

        static float Soft(float d, float width) => Mathf.Clamp01(0.5f - d / width);

        static Color Pine(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Star(x, y, 142, 106, 104, 80, 11, 0.2f), 10f));
            float outer = Star(x, y, 124, 128, 104, 80, 11, 0.2f);
            c = Over(c, Hex("2F6A4A"), Fill(outer));
            c = Over(c, Hex("3E7A57"), Fill(Star(x, y, 120, 133, 74, 56, 10, 0.5f)));
            c = Over(c, Hex("4F8F62"), Fill(Star(x, y, 117, 137, 44, 33, 8, 0.1f)));
            c = Over(c, Hex("6FAE74"), Fill(Circle(x, y, 115, 140, 11)));
            c = Over(c, Outline, Stroke(outer, 4f) * 0.8f);
            return c;
        }

        static Color RoundTree(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float Blob(float ox, float oy)
            {
                float d = Circle(x, y, 128 + ox, 128 + oy, 62);
                for (int i = 0; i < 7; i++)
                {
                    float a = i * Mathf.PI * 2f / 7f + 0.4f;
                    d = SMin(d, Circle(x, y, 128 + ox + Mathf.Cos(a) * 60f, 128 + oy + Mathf.Sin(a) * 60f, 40), 14f);
                }
                return d;
            }
            c = Over(c, Shadow, Soft(Blob(18, -18), 10f));
            float crown = Blob(0, 0);
            c = Over(c, Hex("3E8A35"), Fill(crown));
            c = Over(c, Hex("5BAA3F"), Fill(Circle(x, y, 112, 144, 70)) * Fill(crown + 10f));
            c = Over(c, Hex("8ACB52"), Fill(Circle(x, y, 100, 158, 30)) * Fill(crown + 20f));
            c = Over(c, Outline, Stroke(crown, 4f) * 0.8f);
            return c;
        }

        // A clump of several round crowns, each with its own highlight (the lush look of the reference).
        static Color Bushes(float x, float y)
        {
            var crowns = new[] { new Vector3(88, 150, 52), new Vector3(160, 160, 48), new Vector3(120, 98, 56), new Vector3(186, 100, 40), new Vector3(70, 86, 38) };
            Color c = new(0, 0, 0, 0);
            float all = float.MaxValue;
            foreach (var k in crowns) all = SMin(all, Lumpy(x, y, k.x, k.y, k.z), 8f);
            c = Over(c, Shadow, Soft(Shifted(), 10f));
            c = Over(c, Hex("2F7A2E"), Fill(all));
            // Back to front: each crown lit on its top-left.
            System.Array.Sort(crowns, (a, b) => b.y.CompareTo(a.y));
            foreach (var k in crowns)
            {
                float d = Lumpy(x, y, k.x, k.y, k.z);
                c = Over(c, Hex("4C9E37"), Fill(d));
                c = Over(c, Hex("74C04A"), Fill(Circle(x, y, k.x - k.z * 0.22f, k.y + k.z * 0.22f, k.z * 0.6f)) * Fill(d + 4f));
                c = Over(c, Hex("A6DE6A"), Fill(Circle(x, y, k.x - k.z * 0.38f, k.y + k.z * 0.38f, k.z * 0.22f)) * Fill(d + 6f));
                c = Over(c, Outline, Stroke(d, 3f) * 0.7f);
            }
            return c;

            float Shifted()
            {
                float m = float.MaxValue;
                foreach (var k in crowns) m = Mathf.Min(m, Circle(x, y, k.x + 16, k.y - 16, k.z));
                return m;
            }
        }

        static float Lumpy(float x, float y, float cx, float cy, float r)
        {
            float a = Mathf.Atan2(y - cy, x - cx);
            return Dist(x, y, cx, cy) - r * (1f + 0.07f * Mathf.Sin(a * 7f + cx));
        }

        // A pile of loose tyres seen from above: rings in red, white and grey with dark holes.
        static Color Tyres(float x, float y)
        {
            var tyres = new[] { new Vector3(74, 150, 0), new Vector3(128, 172, 1), new Vector3(184, 146, 2), new Vector3(100, 98, 2), new Vector3(158, 92, 0), new Vector3(210, 100, 1), new Vector3(52, 92, 1) };
            Color[] colors = { Hex("B8322A"), Hex("F2F0EA"), Hex("A9AEB0") };
            Color c = new(0, 0, 0, 0);
            foreach (var t in tyres) c = Over(c, Shadow, Soft(Circle(x, y, t.x + 7, t.y - 7, 27), 5f));
            foreach (var t in tyres)
            {
                float ring = Circle(x, y, t.x, t.y, 27);
                c = Over(c, colors[(int)t.z], Fill(ring));
                c = Over(c, Shade(colors[(int)t.z], 0.8f), Stroke(Circle(x, y, t.x, t.y, 19), 4f));
                float hole = Circle(x, y, t.x, t.y, 12);
                c = Over(c, Hex("3A332E"), Fill(hole));
                c = Over(c, Outline, Stroke(ring, 3.5f));
            }
            return c;
        }

        // A crop field: rows of a crop in a soft-edged rectangle.
        static Color Field(float x, float y)
        {
            float field = RoundRect(x, y, 128, 128, 118, 90, 10);
            if (field > 1f) return new Color(0, 0, 0, 0);
            bool rows = Mathf.Repeat(x, 16f) < 9f;
            Color soil = Hex("B98A4E"), crop = Hex("D9B54A");
            Color c = rows ? crop : soil;
            c = Shade(c, 0.92f + Noise(x / 9f, y / 9f, 29) * 0.12f);
            c.a = Fill(field);
            return Over(c, Hex("8A6A3A"), Stroke(field, 4f) * 0.8f);
        }

        // A barn with a grey metal roof (ridge across), seen from above.
        static Color Barn(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 110, 96, 70, 4), 8f));
            float roof = RoundRect(x, y, 128, 128, 96, 70, 4);
            Color face = y > 128 ? Hex("A7AEB1") : Hex("7E878B");
            if (Mathf.Repeat(x, 14f) < 2f) face = Shade(face, 0.88f);
            c = Over(c, face, Fill(roof));
            c = Over(c, Hex("CBD1D3"), Fill(RoundRect(x, y, 128, 128, 92, 3f, 1)) * Fill(roof));
            c = Over(c, Outline, Stroke(roof, 5f));
            return c;
        }

        static Color Chalet(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            const float hw = 104, hh = 74;
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 106, hw, hh, 6), 8f));
            float roof = RoundRect(x, y, 128, 128, hw, hh, 6);
            // Two roof halves meeting at the ridge (top half lit, bottom half in shade), shingle lines.
            Color lit = Hex("C25E34"), shade = Hex("8E3F22");
            Color face = y > 128 ? lit : shade;
            if (Mathf.Repeat(y - 128, 18f) < 2.2f) face = Shade(face, 0.85f);
            c = Over(c, face, Fill(roof));
            c = Over(c, Hex("E9D8B4"), Fill(RoundRect(x, y, 128, 128, hw - 4, 3.5f, 2)) * Fill(roof));
            // Chimney.
            float chimney = RoundRect(x, y, 170, 156, 13, 13, 3);
            c = Over(c, Hex("CFC6B4"), Fill(chimney));
            c = Over(c, Hex("3A3532"), Fill(RoundRect(x, y, 170, 156, 6, 6, 2)));
            c = Over(c, Outline, Stroke(Mathf.Min(roof, chimney), 5f));
            return c;
        }

        static Color Rock(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(52, 120), new Vector2(84, 196), new Vector2(150, 214), new Vector2(206, 160), new Vector2(196, 82), new Vector2(124, 46), new Vector2(66, 66) };
            var shadowPts = System.Array.ConvertAll(pts, p => p + new Vector2(16, -16));
            c = Over(c, Shadow, Soft(Polygon(x, y, shadowPts), 8f));
            float rock = Polygon(x, y, pts);
            c = Over(c, Hex("8F918A"), Fill(rock));
            // Lit facet (top left) and dark facet (bottom right).
            c = Over(c, Hex("B6B7AE"), Fill(Polygon(x, y, new[] { new Vector2(52, 120), new Vector2(84, 196), new Vector2(150, 214), new Vector2(132, 132) })));
            c = Over(c, Hex("6E706A"), Fill(Polygon(x, y, new[] { new Vector2(196, 82), new Vector2(124, 46), new Vector2(132, 132), new Vector2(206, 160) })));
            c = Over(c, Outline, Stroke(rock, 5f));
            return c;
        }

        // ---------- materials ----------

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
    }
}
