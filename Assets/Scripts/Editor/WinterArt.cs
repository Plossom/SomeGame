using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The race art of the snow biome (Winter Cup), in the Alpine Rally style: snow ground with soft blue
    /// drifts and sparkles, cold asphalt with snow flecks, snow-bank kerbs in ice blue and white, packed
    /// snow run-off, and a winter scenery atlas (12 cells of 256 px, 4 x 3), all seen from above.
    /// Writes into <see cref="ArtGenerator.Folder"/> with a material for each.
    /// </summary>
    public static partial class WinterArt
    {
        static readonly Color Outline = Hex("16283A");
        static readonly Color Shadow = new(0.12f, 0.2f, 0.36f, 0.3f);
        static readonly Color Snow = Hex("F6FAFE"), SnowShade = Hex("CFDDEC"), SnowDeep = Hex("AFC3D9");

        public static readonly string[] SceneryCells =
            { "SnowPine", "FrostTree", "FrostBush", "Snowman", "Cabin", "SnowRock", "Igloo", "Sled", "IceCrystals", "Gifts", "FrozenPond", "Lamp" };

        [MenuItem("SomeGame/Generate Winter Art")]
        public static void Generate()
        {
            Write(ArtGenerator.Folder, "SnowGround", 128, 128, SnowGroundPixel, 32, true);
            Write(ArtGenerator.Folder, "WinterRoad", 128, 128, WinterRoadPixel, 32, true);
            Write(ArtGenerator.Folder, "SnowKerb", 64, 64, SnowKerbPixel, 32, true);
            Write(ArtGenerator.Folder, "SnowRunoff", 64, 64, SnowRunoffPixel, 32, true);
            Write(ArtGenerator.Folder, "Ice", 128, 128, IcePixel, 32, true);
            Write(ArtGenerator.Folder, "RoofRow", 128, 256, RoofRowPixel, 32, true);
            Write(ArtGenerator.Folder, "IceWall", 64, 64, IceWallPixel, 32, true);
            Write(ArtGenerator.Folder, "SceneryWinter", 1024, 768, SceneryPixel, 128, false);
            ArtGenerator.Material("SnowGround", "SnowGround");
            ArtGenerator.Material("WinterRoad", "WinterRoad");
            ArtGenerator.Material("SnowKerb", "SnowKerb");
            ArtGenerator.Material("SnowRunoff", "SnowRunoff");
            ArtGenerator.Material("Ice", "Ice");
            ArtGenerator.Material("RoofRow", "RoofRow");
            ArtGenerator.Material("IceWall", "IceWall");
            ArtGenerator.Material("SceneryWinter", "SceneryWinter");
            AssetDatabase.SaveAssets();
            Debug.Log("Winter art generated");
        }

        // ------------------------------------------------------------------ ground, road, kerbs

        static Color SnowGroundPixel(float x, float y)
        {
            float big = Noise(x / 32f, y / 32f, 4), small = Noise(x / 9f, y / 9f, 14);
            Color c = Color.Lerp(Hex("E4EDF6"), Hex("F7FAFD"), big * 0.65f + small * 0.35f);
            // Soft wind ripples.
            float ripple = Mathf.Sin((y + Noise(x / 20f, y / 20f, 6) * 30f) / 128f * Mathf.PI * 8f);
            c = Color.Lerp(c, Hex("D7E4F1"), Mathf.Clamp01(ripple - 0.75f) * 1.6f);
            // Sparkles and blue speckles.
            float h = Hash((int)(x / 2), (int)(y / 2));
            if (h > 0.992f) c = Color.white;
            else if (h < 0.012f) c = Color.Lerp(c, Hex("B9CDE2"), 0.7f);
            return c;
        }

        static Color WinterRoadPixel(float x, float y)
        {
            float n = Noise(x / 10f, y / 10f, 13) * 0.5f + Hash((int)x, (int)y) * 0.5f;
            float v = 0.34f + (n - 0.5f) * 0.05f;
            if (Hash((int)x + 7, (int)y * 3) > 0.985f) v += 0.06f;
            Color c = new(v * 0.94f, v * 0.98f, v * 1.06f);
            // Flecks of snow blown onto the road.
            if (Hash((int)x + 11, (int)y + 5) > 0.993f) c = Color.Lerp(c, Snow, 0.6f);
            return c;
        }

        // Across the kerb strip: road side (u = 0) → white line → ice-blue and white snow-bank blocks → dark edge.
        static Color SnowKerbPixel(float x, float y)
        {
            float u = x / 64f;
            if (u < 0.05f) return WinterRoadPixel(x, y);
            if (u < 0.3f) return Hex("F7FAFD");
            if (u > 0.93f) return Hex("2B4057");
            Color c = y < 32f ? Hex("6FB6E6") : Hex("F2F7FC");
            return Color.Lerp(c, Shade(c, 0.86f), Mathf.InverseLerp(0.3f, 0.93f, u));
        }

        // Packed snow beside the corners with tyre ruts, fading into the snow with a ragged edge (u = 1).
        static Color SnowRunoffPixel(float x, float y)
        {
            float u = x / 64f;
            float n = Hash((int)x, (int)y) * 0.4f + Noise(x / 5f, y / 5f, 13) * 0.6f;
            Color c = Color.Lerp(Hex("D6E2EE"), Hex("EAF1F8"), n);
            if (Mathf.Abs(u - 0.32f) < 0.05f || Mathf.Abs(u - 0.55f) < 0.04f) c = Color.Lerp(c, Hex("BBCCDF"), 0.6f); // ruts
            float edge = 0.8f + (Noise(y / 7f, 3.5f, 9) - 0.5f) * 0.3f;
            c.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - 0.08f, edge + 0.08f, u));
            return c;
        }

        // Frozen lake strip (u across, v along, tiles along): pale blue ice with white cracks and glints,
        // a snowy rim and a ragged edge fading into the snow at both sides.
        static Color IcePixel(float x, float y)
        {
            float u = x / 128f;
            float n = Noise(x / 16f, y / 16f, 8) * 0.6f + Noise(x / 8f, y / 8f, 16) * 0.4f; // periods of 128 px: tiles along
            Color c = Color.Lerp(Hex("7DBDE6"), Hex("B5DDF3"), n);
            // Cracks: thin wandering lines.
            float crack = Mathf.Abs(Mathf.Sin((x + Noise(x / 16f, y / 16f, 8) * 40f) * 0.09f + y * 0.0491f));
            if (crack < 0.035f) c = Color.Lerp(c, Color.white, 0.8f);
            float crack2 = Mathf.Abs(Mathf.Sin((y + Noise(y / 16f, x / 16f, 8) * 30f) * 0.0982f - x * 0.03f));
            if (crack2 < 0.025f) c = Color.Lerp(c, Color.white, 0.6f);
            if (Hash((int)(x / 2), (int)(y / 2)) > 0.993f) c = Color.white;
            // Snowy rim, then fade out with a ragged edge.
            float edgeA = 0.1f + (Noise(y / 8f, 1.5f, 16) - 0.5f) * 0.08f, edgeB = 0.9f + (Noise(y / 8f, 7.5f, 16) - 0.5f) * 0.08f;
            float rim = Mathf.Min(u - edgeA, edgeB - u);
            if (rim < 0.06f) c = Color.Lerp(c, Hex("5D9CC8"), Mathf.InverseLerp(0.06f, 0.035f, rim)); // darker blue edge of the ice
            if (rim < 0.035f) c = Color.Lerp(c, Snow, Mathf.InverseLerp(0.035f, 0f, rim));
            c.a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.03f, 0.02f, rim));
            return c;
        }

        // One house of a roof row, seen from above (u across, v along; tiles along the row): wooden eaves
        // at both sides, the snowy roof lit on the left and in shade on the right of the ridge, faint
        // shingle lines, a dark gap to the next house and a chimney near the edge.
        static Color RoofRowPixel(float x, float y)
        {
            float u = x / 128f;
            if (u < 0.035f || u > 0.965f) return Hex("2B2320");
            if (u < 0.08f || u > 0.92f) return Hex("8E5C3B");
            if (y < 5f || y > 251f) return Hex("4A3A30"); // gap between two houses
            if (y < 9f || y > 247f) return Hex("B9CBDD");
            Color c = u < 0.5f ? Snow : SnowShade;
            if (Mathf.Abs(u - 0.5f) < 0.012f) c = Hex("A9BDD2"); // ridge
            if (Mathf.Repeat(y, 24f) < 1.6f) c = Shade(c, 0.96f);
            if (Hash((int)x / 2, (int)y / 2) > 0.994f) c = Color.white;
            // Chimney with a snow cap.
            float chimney = RoundRect(x, y, 104f, 190f, 9f, 9f, 2f);
            c = Over(c, Hex("9A9AA6"), Fill(chimney));
            c = Over(c, Hex("3A3532"), Fill(RoundRect(x, y, 104f, 190f, 4f, 4f, 1f)));
            c = Over(c, Hex("2B2320"), Stroke(chimney, 2f));
            return c;
        }

        // A banked wall of packed snow and ice (u across: base at the kerb → crest; v along, tiles): darker
        // and bluer at the base, bright towards the white crest, with a dark outline on top and white
        // chevrons pointing along the wall (ride it!).
        static Color IceWallPixel(float x, float y)
        {
            float u = x / 64f;
            if (u > 0.93f) return Hex("2B4057");
            if (u > 0.82f) return Snow;
            Color c = Color.Lerp(Hex("6E9CC4"), Hex("D4E6F4"), Mathf.SmoothStep(0f, 1f, u / 0.82f));
            c = Color.Lerp(c, Shade(c, 0.93f), Noise(x / 8f, y / 8f, 8) * 0.6f);
            // Chevron: a "^" pointing along the wall (towards +v).
            float cx = 32f, chevron = Mathf.Abs(Mathf.Abs(x - cx) * 0.9f + (y - 40f)) - 4f;
            if (Mathf.Abs(x - cx) < 18f && chevron < 0f) c = Color.Lerp(c, Color.white, 0.75f);
            return c;
        }

        // ------------------------------------------------------------------ scenery atlas

        static Color SceneryPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return (row * 4 + col) switch
            {
                0 => SnowPine(lx, ly),
                1 => FrostTree(lx, ly),
                2 => FrostBush(lx, ly),
                3 => Snowman(lx, ly),
                4 => Cabin(lx, ly),
                5 => SnowRock(lx, ly),
                6 => Igloo(lx, ly),
                7 => Sled(lx, ly),
                8 => IceCrystals(lx, ly),
                9 => Gifts(lx, ly),
                10 => FrozenPond(lx, ly),
                _ => Lamp(lx, ly),
            };
        }

        /// <summary>UV rect of a scenery cell by name.</summary>
        public static Rect Cell(string name)
        {
            int i = System.Array.IndexOf(SceneryCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / 3f, 0.25f, 1f / 3f);
        }

        static float Soft(float d, float width) => Mathf.Clamp01(0.5f - d / width);

        static Color SnowPine(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Star(x, y, 142, 106, 104, 80, 11, 0.2f), 10f));
            float outer = Star(x, y, 124, 128, 104, 80, 11, 0.2f);
            c = Over(c, Hex("2E5E5A"), Fill(outer));
            c = Over(c, Hex("3D7470"), Fill(Star(x, y, 120, 133, 74, 56, 10, 0.5f)));
            // Snow on the branch tips and the top tiers.
            float tips = Mathf.Max(outer, -Star(x, y, 124, 128, 86, 66, 11, 0.2f));
            c = Over(c, Snow, Fill(tips) * 0.95f);
            c = Over(c, Hex("4F8A82"), Fill(Star(x, y, 117, 137, 44, 33, 8, 0.1f)));
            c = Over(c, Snow, Fill(Star(x, y, 115, 139, 24, 17, 7, 0.1f)));
            c = Over(c, Outline, Stroke(outer, 4f) * 0.8f);
            return c;
        }

        static Color FrostTree(float x, float y)
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
            c = Over(c, Hex("9DB6C9"), Fill(crown));
            c = Over(c, Hex("D2E1EE"), Fill(Circle(x, y, 112, 144, 70)) * Fill(crown + 10f));
            c = Over(c, Snow, Fill(Circle(x, y, 100, 158, 34)) * Fill(crown + 20f));
            c = Over(c, Outline, Stroke(crown, 4f) * 0.8f);
            return c;
        }

        static Color FrostBush(float x, float y)
        {
            var crowns = new[] { new Vector3(88, 150, 50), new Vector3(160, 160, 46), new Vector3(120, 98, 54), new Vector3(184, 102, 38) };
            Color c = new(0, 0, 0, 0);
            float all = float.MaxValue, shadow = float.MaxValue;
            foreach (var k in crowns)
            {
                all = SMin(all, Circle(x, y, k.x, k.y, k.z), 8f);
                shadow = Mathf.Min(shadow, Circle(x, y, k.x + 14, k.y - 14, k.z));
            }
            c = Over(c, Shadow, Soft(shadow, 10f));
            c = Over(c, Hex("3E6E5C"), Fill(all));
            System.Array.Sort(crowns, (a, b) => b.y.CompareTo(a.y));
            foreach (var k in crowns)
            {
                float d = Circle(x, y, k.x, k.y, k.z);
                c = Over(c, Hex("4F8370"), Fill(d));
                c = Over(c, Snow, Fill(Circle(x, y, k.x - k.z * 0.2f, k.y + k.z * 0.25f, k.z * 0.72f)) * Fill(d + 3f));
                c = Over(c, Outline, Stroke(d, 3f) * 0.6f);
            }
            return c;
        }

        // From above: the round body, the smaller head with a carrot nose, a dark hat and a red scarf.
        static Color Snowman(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 112, 70), 10f));
            float body = Circle(x, y, 128, 118, 66);
            c = Over(c, SnowShade, Fill(body));
            c = Over(c, Snow, Fill(Circle(x, y, 118, 128, 54)) * Fill(body + 2f));
            c = Over(c, Outline, Stroke(body, 4f));
            float scarf = Shell(Circle(x, y, 128, 140, 40), 7f);
            c = Over(c, Hex("D9433A"), Fill(scarf));
            float head = Circle(x, y, 128, 140, 34);
            c = Over(c, Snow, Fill(head));
            c = Over(c, Outline, Stroke(head, 3.5f));
            c = Over(c, Hex("FF8A2A"), Fill(Polygon(x, y, new[] { new Vector2(150, 150), new Vector2(150, 132), new Vector2(190, 141) })));
            c = Over(c, Hex("22303E"), Fill(Circle(x, y, 122, 142, 18)));
            c = Over(c, Hex("3A4B5E"), Fill(Circle(x, y, 118, 146, 8)));
            return c;
        }

        // A log cabin from above: snowy gable roof with a ridge, wooden eaves showing, a chimney.
        static Color Cabin(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            const float hw = 104, hh = 74;
            c = Over(c, Shadow, Soft(RoundRect(x, y, 146, 106, hw, hh, 6), 8f));
            float eaves = RoundRect(x, y, 128, 128, hw, hh, 6);
            c = Over(c, Hex("8E5C3B"), Fill(eaves));
            float roof = RoundRect(x, y, 128, 128, hw - 7, hh - 7, 5);
            Color face = y > 128 ? Snow : SnowShade;
            if (Mathf.Repeat(y - 128, 22f) < 1.6f) face = Shade(face, 0.95f);
            c = Over(c, face, Fill(roof));
            c = Over(c, Hex("B9CBDD"), Fill(RoundRect(x, y, 128, 128, hw - 8, 2.5f, 1)) * Fill(roof));
            float chimney = RoundRect(x, y, 172, 158, 13, 13, 3);
            c = Over(c, Hex("9A9AA6"), Fill(chimney));
            c = Over(c, Hex("3A3532"), Fill(RoundRect(x, y, 172, 158, 6, 6, 2)));
            c = Over(c, Outline, Stroke(Mathf.Min(eaves, chimney), 5f));
            return c;
        }

        static Color SnowRock(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(52, 120), new Vector2(84, 196), new Vector2(150, 214), new Vector2(206, 160), new Vector2(196, 82), new Vector2(124, 46), new Vector2(66, 66) };
            c = Over(c, Shadow, Soft(Polygon(x, y, System.Array.ConvertAll(pts, p => p + new Vector2(16, -16))), 8f));
            float rock = Polygon(x, y, pts);
            c = Over(c, Hex("7F8896"), Fill(rock));
            c = Over(c, Hex("6A7280"), Fill(Polygon(x, y, new[] { new Vector2(196, 82), new Vector2(124, 46), new Vector2(132, 132), new Vector2(206, 160) })));
            // Snow cap over the top-left half.
            c = Over(c, Snow, Fill(Mathf.Max(rock + 6f, Circle(x, y, 110, 168, 82))));
            c = Over(c, Outline, Stroke(rock, 5f));
            return c;
        }

        // A dome of snow blocks with an entrance tunnel at the bottom.
        static Color Igloo(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float dome = Circle(x, y, 128, 140, 84), tunnel = RoundRect(x, y, 128, 52, 30, 34, 14);
            float all = Mathf.Min(dome, tunnel);
            c = Over(c, Shadow, Soft(Mathf.Min(Circle(x, y, 142, 126, 84), RoundRect(x, y, 142, 38, 30, 34, 14)), 10f));
            c = Over(c, SnowShade, Fill(all));
            c = Over(c, Snow, Fill(Circle(x, y, 118, 150, 70)) * Fill(dome + 2f));
            // Block lines: rings and staggered joints.
            float r = Dist(x, y, 128, 140);
            if (dome < 0f && Mathf.Repeat(r, 21f) < 2f) c = Over(c, SnowDeep, 0.8f);
            float ang = Mathf.Atan2(y - 140, x - 128) * Mathf.Rad2Deg;
            int ring = (int)(r / 21f);
            if (dome < 0f && Mathf.Repeat(ang + ring * 15f, 30f) < 1.6f * 60f / Mathf.Max(20f, r)) c = Over(c, SnowDeep, 0.7f);
            c = Over(c, Hex("33475E"), Fill(RoundRect(x, y, 128, 34, 16, 16, 10)));
            c = Over(c, Outline, Stroke(all, 4f));
            return c;
        }

        static Color Sled(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 142, 114, 46, 96, 18), 8f));
            float r1 = Capsule(x, y, 92, 36, 92, 214, 6), r2 = Capsule(x, y, 164, 36, 164, 214, 6);
            c = Over(c, Hex("C8372D"), Fill(Mathf.Min(r1, r2)));
            float deck = RoundRect(x, y, 128, 120, 42, 82, 10);
            c = Over(c, Hex("B57A45"), Fill(deck));
            for (int k = -2; k <= 2; k++) c = Over(c, Hex("94603A"), Fill(RoundRect(x, y, 128, 120 + k * 30, 40, 1.5f, 1)) * Fill(deck));
            c = Over(c, Snow, Fill(Circle(x, y, 118, 160, 18)) * Fill(deck));
            c = Over(c, Outline, Stroke(Mathf.Min(deck, Mathf.Min(r1, r2)), 4f));
            return c;
        }

        static Color IceCrystals(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var shards = new[] { (new Vector2(128, 130), 70f, 0.1f), (new Vector2(80, 100), 46f, 0.5f), (new Vector2(176, 96), 40f, -0.3f), (new Vector2(150, 186), 34f, 0.8f) };
            foreach (var (p, s, a) in shards)
                c = Over(c, Shadow, Soft(Star(x, y, p.x + 14, p.y - 14, s, s * 0.45f, 6, a), 8f));
            foreach (var (p, s, a) in shards)
            {
                float d = Star(x, y, p.x, p.y, s, s * 0.45f, 6, a);
                c = Over(c, Color.Lerp(Hex("7FD3F5"), Hex("D9F4FF"), Mathf.InverseLerp(p.y - s, p.y + s, y)), Fill(d));
                c = Over(c, Color.white, Fill(Star(x, y, p.x - s * 0.15f, p.y + s * 0.15f, s * 0.35f, s * 0.15f, 6, a)) * 0.9f);
                c = Over(c, Hex("2C6C93"), Stroke(d, 3f));
            }
            return c;
        }

        static Color Gifts(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            void Box(Vector2 p, float s, float angle, Color paper, Color ribbon)
            {
                float d = RotRect(x, y, p.x, p.y, s, s, 6, angle);
                c = Over(c, Shadow, Soft(RotRect(x, y, p.x + 12, p.y - 12, s, s, 6, angle), 8f));
                c = Over(c, paper, Fill(d));
                float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);
                float lx = (x - p.x) * ca + (y - p.y) * sa, ly = -(x - p.x) * sa + (y - p.y) * ca;
                c = Over(c, ribbon, Fill(Mathf.Min(Mathf.Abs(lx) - 7f, Mathf.Abs(ly) - 7f)) * Fill(d));
                c = Over(c, ribbon, Fill(Circle(x, y, p.x, p.y, 15)));
                c = Over(c, Outline, Stroke(d, 4f));
            }
            Box(new Vector2(100, 112), 52, 0.2f, Hex("D9433A"), Hex("F5D04A"));
            Box(new Vector2(170, 160), 40, -0.35f, Hex("2E8C5A"), Hex("F2F7FC"));
            return c;
        }

        static Color FrozenPond(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float pond = Sdf.Ellipse(x, y, 128, 128, 112, 80);
            c = Over(c, Snow, Fill(pond - 8f));
            c = Over(c, Color.Lerp(Hex("A8D5EE"), Hex("DDF1FA"), Mathf.InverseLerp(48, 208, y)), Fill(pond));
            float cracks = Min(Capsule(x, y, 70, 120, 120, 140, 1.6f), Capsule(x, y, 120, 140, 160, 118, 1.6f), Capsule(x, y, 120, 140, 132, 176, 1.6f));
            c = Over(c, Color.white, Fill(cracks) * Fill(pond + 4f));
            c = Over(c, new Color(1f, 1f, 1f, 0.7f), Fill(Capsule(x, y, 80, 160, 120, 172, 5f)) * Fill(pond + 6f));
            c = Over(c, Hex("7FA5C6"), Stroke(pond, 3f));
            return c;
        }

        // A lamp post from above: warm light pooled on the snow, the lamp head and its pole cap.
        static Color Lamp(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float d = Dist(x, y, 128, 128);
            c = Over(c, new Color(1f, 0.85f, 0.5f, 1f), Gauss(d, 52f) * 0.5f);
            float head = Circle(x, y, 128, 128, 22);
            c = Over(c, Hex("FFE3A0"), Fill(head));
            c = Over(c, Hex("2A3A4A"), Stroke(head, 6f));
            c = Over(c, Snow, Fill(Circle(x, y, 122, 134, 9)));
            return c;
        }
    }
}
