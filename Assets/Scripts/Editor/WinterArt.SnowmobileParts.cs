using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The snowmobile's own garage parts (the Winter Cup's), on its 192 x 336 canvas (nose up, the skis and the
    /// track in the same spots, so every sled drives and steers the same):
    /// <list type="bullet">
    /// <item>Bodies (tinted with the paint; details untinted): the BUBBLE TUB (a clawfoot bathtub on skis, the
    /// rider in a shower cap in the foam, a rubber duck along for the ride) and the TOURING SLED (a wide hood, a
    /// long seat with a backrest and a rack behind). Each as Snowmobile{Name} (with its skis, for the menus),
    /// Snowmobile{Name}Body (without, for races) and Snowmobile{Name}Details.</item>
    /// <item>Tops, laid over it: a tall windscreen, a safety flag, a cargo box, reindeer antlers, a snowman riding
    /// pillion and the gold ice crown.</item>
    /// <item>Decals on the hood: a zigzag, a pine tree, a penguin, a number one and the gold Winter crest.</item>
    /// </list>
    /// </summary>
    public static partial class WinterArt
    {
        [MenuItem("SomeGame/Generate Snowmobile Parts")]
        public static void GenerateSnowmobileParts()
        {
            foreach (var (name, kind) in new[] { ("Tub", 1), ("Touring", 2) })
            {
                int k = kind;
                Write(ArtGenerator.Folder, "Snowmobile" + name, 192, 336, (x, y) =>
                {
                    Color skis = Over(SkiPixel(x, y), SkiPixel(x - 136f, y), 1f);
                    return Over(skis, SledPixel(k, x, y), 1f);
                }, 192, false);
                Write(ArtGenerator.Folder, "Snowmobile" + name + "Body", 192, 336, (x, y) => SledPixel(k, x, y), 192, false);
                Write(ArtGenerator.Folder, "Snowmobile" + name + "Details", 192, 336, (x, y) => SledDetailsPixel(k, x, y), 192, false);
            }
            Write(ArtGenerator.Folder, "TopSnowScreen", 192, 336, ScreenPixel, 192, false);
            Write(ArtGenerator.Folder, "TopSnowFlag", 192, 336, FlagPixel, 192, false);
            Write(ArtGenerator.Folder, "TopSnowCargo", 192, 336, CargoPixel, 192, false);
            Write(ArtGenerator.Folder, "TopSnowAntlers", 192, 336, AntlersPixel, 192, false);
            Write(ArtGenerator.Folder, "TopSnowman", 192, 336, SnowmanPixel, 192, false);
            Write(ArtGenerator.Folder, "TopIceCrown", 192, 336, IceCrownPixel, 192, false);
            Write(ArtGenerator.Folder, "DecalZigzag", 192, 336, ZigzagPixel, 192, false);
            Write(ArtGenerator.Folder, "DecalPine", 192, 336, PinePixel, 192, false);
            Write(ArtGenerator.Folder, "DecalPenguin", 192, 336, PenguinPixel, 192, false);
            Write(ArtGenerator.Folder, "DecalNumberOne", 192, 336, NumberOnePixel, 192, false);
            Write(ArtGenerator.Folder, "DecalWinterCrest", 192, 336, WinterCrestPixel, 192, false);
            AssetDatabase.SaveAssets();
            Debug.Log("Snowmobile parts generated");
        }

        // ------------------------------------------------------------------ bodies (1 the bubble tub, 2 touring)

        static Color SledPixel(int kind, float x, float y) => kind == 1 ? TubPixel(x, y) : TouringPixel(x, y);
        static Color SledDetailsPixel(int kind, float x, float y) => kind == 1 ? TubDetailsPixel(x, y) : TouringDetailsPixel(x, y);

        // ------------------------------------------------------------------ the touring sled

        static float TouringBodySdf(float x, float y)
        {
            float hood = RoundRect(x, y, 96, 262, 54, 54, 26);
            float middle = RoundRect(x, y, 96, 140, 54, 80, 16);
            return Mathf.Min(hood, middle);
        }

        static Color TouringPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float arms = Min(Capsule(x, y, 58, 252, 30, 258, 4.5f), Capsule(x, y, 58, 274, 30, 270, 4.5f),
                             Capsule(x, y, 134, 252, 162, 258, 4.5f), Capsule(x, y, 134, 274, 162, 270, 4.5f));
            c = Over(c, new Color(0.16f, 0.16f, 0.16f), Fill(arms));
            float track = SledTrackSdf(x, y);
            c = Over(c, Mathf.Repeat(y, 9f) < 4f ? Tyre : TyreLight, Fill(track));
            c = Over(c, SledOutline, Stroke(track, 4f));

            // The rack over the back of the track (under the body's tail).
            float rack = Mathf.Max(RoundRect(x, y, 96, 52, 40, 22, 8), -RoundRect(x, y, 96, 52, 32, 14, 4));
            rack = Mathf.Min(rack, Capsule(x, y, 96, 34, 96, 70, 2.5f));
            c = Over(c, new Color(0.18f, 0.18f, 0.2f), Fill(rack));
            c = Over(c, SledOutline, Stroke(rack, 2f));

            float body = TouringBodySdf(x, y);
            float inside = Mathf.Clamp01(-body / 14f);
            float v = Mathf.Lerp(0.8f, 1f, inside) - Mathf.Clamp01((x - 120f) / 60f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(body));
            c = Over(c, new Color(0.92f, 0.92f, 0.92f), Fill(RoundRect(x, y, 96, 270, 26, 34, 14)) * Fill(body + 4f));
            c = Over(c, SledOutline, Stroke(body, 4.5f));

            // A long seat with a backrest for a passenger, footboards either side.
            c = Over(c, Cockpit, Fill(RoundRect(x, y, 96, 118, 26, 60, 12)));
            c = Over(c, new Color(0.24f, 0.24f, 0.26f), Fill(RoundRect(x, y, 96, 100, 16, 34, 8)));
            float back = RoundRect(x, y, 96, 64, 28, 8, 5);
            c = Over(c, Cockpit, Fill(back));
            c = Over(c, SledOutline, Stroke(back, 2.5f));
            float boards = Mathf.Min(RoundRect(x, y, 56, 128, 10, 48, 5), RoundRect(x, y, 136, 128, 10, 48, 5));
            c = Over(c, Mathf.Repeat(y, 8f) < 3f ? Cockpit : new Color(0.22f, 0.22f, 0.24f), Fill(boards));

            // The rider, as on the classic sled.
            float armsUp = Mathf.Min(Capsule(x, y, 72, 168, 60, 208, 9.5f), Capsule(x, y, 120, 168, 132, 208, 9.5f));
            float torso = Ellipse(x, y, 96, 160, 34, 24);
            float rider = Mathf.Min(armsUp, torso);
            c = Over(c, Suit, Fill(rider));
            c = Over(c, SledOutline, Stroke(rider, 3.5f));

            // Tubes (tinted): the handlebar, a wide bumper and side rails.
            const float r = 4.2f;
            float tubes = Min(Capsule(x, y, 54, 212, 138, 212, r), Capsule(x, y, 44, 288, 64, 328, r), Capsule(x, y, 64, 328, 128, 328, r), Capsule(x, y, 128, 328, 148, 288, r));
            c = Over(c, new Color(0.95f, 0.95f, 0.95f), Fill(tubes));
            c = Over(c, SledOutline, Stroke(tubes, 2.5f));
            float gloves = Mathf.Min(Circle(x, y, 60, 211, 9f), Circle(x, y, 132, 211, 9f));
            c = Over(c, Cockpit, Fill(gloves));
            c = Over(c, SledOutline, Stroke(gloves, 2.5f));
            float helmet = Circle(x, y, 96, 166, 24);
            c = Over(c, new Color(0.95f, 0.95f, 0.95f), Fill(helmet));
            c = Over(c, SledOutline, Stroke(helmet, 3.5f));
            return c;
        }

        static Color TouringDetailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float screen = RoundRect(x, y, 96, 234, 40, 12, 7);
            c = Over(c, new Color(0.62f, 0.82f, 0.96f, 0.85f), Fill(screen));
            c = Over(c, new Color(1f, 1f, 1f, 0.7f), Fill(Capsule(x, y, 76, 230, 88, 230, 2f)));
            c = Over(c, SledOutline, Stroke(screen, 3f));
            c = Over(c, Hex("1D2733"), Fill(Mathf.Max(Circle(x, y, 96, 166, 20), 172f - y)));
            c = Over(c, new Color(1f, 1f, 1f, 0.45f), Fill(Circle(x, y, 88, 160, 7f)));
            // One big round lamp and mirrors on the bar ends.
            float lamp = Circle(x, y, 96, 300, 11);
            c = Over(c, Hex("FFF6D8"), Fill(lamp));
            c = Over(c, new Color(1f, 1f, 1f, 0.8f), Fill(Circle(x, y, 92, 304, 3.5f)));
            c = Over(c, SledOutline, Stroke(lamp, 2.5f));
            float mirrors = Mathf.Min(Ellipse(x, y, 42, 222, 8, 6), Ellipse(x, y, 150, 222, 8, 6));
            c = Over(c, Hex("9FB4C8"), Fill(mirrors));
            c = Over(c, SledOutline, Stroke(mirrors, 2f));
            float tails = Mathf.Min(RoundRect(x, y, 74, 84, 8, 4, 2), RoundRect(x, y, 118, 84, 8, 4, 2));
            c = Over(c, Hex("E0303A"), Fill(tails));
            return c;
        }

        // ------------------------------------------------------------------ the bubble tub

        // A clawfoot bathtub (tinted), its front on the ski arms, little runners under its claw feet at the back.
        static float TubSdf(float x, float y) => RoundRect(x, y, 96, 178, 62, 112, 56);
        static float TubBasinSdf(float x, float y) => RoundRect(x, y, 96, 176, 48, 96, 44);

        static Color TubPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float arms = Min(Capsule(x, y, 58, 252, 30, 258, 4.5f), Capsule(x, y, 58, 274, 30, 270, 4.5f),
                             Capsule(x, y, 134, 252, 162, 258, 4.5f), Capsule(x, y, 134, 274, 162, 270, 4.5f));
            c = Over(c, new Color(0.16f, 0.16f, 0.16f), Fill(arms));
            // The back feet on short runners (dark, like the track they replace).
            foreach (float fx in new[] { 40f, 152f })
            {
                float runner = RoundRect(x, y, fx, 86, 9, 40, 8);
                c = Over(c, Mathf.Repeat(y, 9f) < 4f ? Tyre : TyreLight, Fill(runner));
                c = Over(c, SledOutline, Stroke(runner, 3f));
                float foot = Ellipse(x, y, fx + (fx < 96 ? 10 : -10), 96, 12, 16);
                c = Over(c, new Color(0.82f, 0.68f, 0.3f), Fill(foot));
                c = Over(c, SledOutline, Stroke(foot, 2.5f));
            }
            // The tub: a thick rolled rim, the basin inside a shade deeper.
            float tub = TubSdf(x, y);
            float v = Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(-tub / 10f)) - Mathf.Clamp01((x - 120f) / 60f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(tub));
            c = Over(c, new Color(0.97f, 0.97f, 0.97f), Fill(Capsule(x, y, 54, 120, 54, 236, 3f)) * Fill(tub + 3f));
            float basin = TubBasinSdf(x, y);
            c = Over(c, new Color(0.74f, 0.74f, 0.74f), Fill(basin));
            c = Over(c, SledOutline, Stroke(basin, 2.5f));
            c = Over(c, SledOutline, Stroke(tub, 4.5f));
            return c;
        }

        static Color TubDetailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Water in the basin with foam heaped along its edge (untinted: whatever the paint, the bath is blue).
            float basin = TubBasinSdf(x, y) + 2f;
            c = Over(c, new Color(0.55f, 0.82f, 0.96f, 0.95f), Fill(basin));
            c = Over(c, new Color(1f, 1f, 1f, 0.5f), Fill(Capsule(x, y, 70, 104, 82, 100, 2f)) * Fill(basin));
            float foam = float.MaxValue;
            for (int i = 0; i < 26; i++)
            {
                float a = i / 26f * Mathf.PI * 2f;
                float fx = 96 + Mathf.Cos(a) * 44f, fy = 176 + Mathf.Sin(a) * 90f;
                foam = Mathf.Min(foam, Circle(x, y, fx, fy, 7f + 4f * Hash(i, 3)));
            }
            foam = Mathf.Min(foam, Min(Circle(x, y, 74, 190, 9), Circle(x, y, 118, 196, 8), Circle(x, y, 84, 128, 7), Circle(x, y, 112, 134, 6)));
            c = Over(c, White(0.95f), Fill(foam) * Fill(basin));
            // A chrome towel rail across the front to hold on to.
            float rail = Capsule(x, y, 52, 228, 140, 228, 4.2f);
            c = Over(c, Hex("C9D3DC"), Fill(rail));
            c = Over(c, SledOutline, Stroke(rail, 2.5f));
            // The rider sat in the foam, arms out to the rail.
            float shoulders = Ellipse(x, y, 96, 156, 30, 16);
            float armsUp = Mathf.Min(Capsule(x, y, 72, 162, 62, 222, 8.5f), Capsule(x, y, 120, 162, 130, 222, 8.5f));
            float rider = Mathf.Min(shoulders, armsUp);
            c = Over(c, Hex("F2C6A0"), Fill(rider));
            c = Over(c, SledOutline, Stroke(rider, 3f));
            float hands = Mathf.Min(Circle(x, y, 62, 226, 7.5f), Circle(x, y, 130, 226, 7.5f));
            c = Over(c, Hex("F2C6A0"), Fill(hands));
            c = Over(c, SledOutline, Stroke(hands, 2.5f));
            float head = Circle(x, y, 96, 166, 22);
            c = Over(c, Hex("F2C6A0"), Fill(head));
            c = Over(c, SledOutline, Stroke(head, 3.5f));
            // The rubber duck bobbing at the back.
            float duck = Mathf.Min(Ellipse(x, y, 112, 100, 14, 11), Circle(x, y, 112, 114, 8));
            c = Over(c, Hex("FFD23F"), Fill(duck));
            c = Over(c, Hex("FF8A1E"), Fill(Triangle(x, y, new Vector2(108, 120), new Vector2(116, 120), new Vector2(112, 128))));
            c = Over(c, SledOutline, Stroke(duck, 2f));
            c = Over(c, SledOutline, Fill(Circle(x, y, 109, 116, 1.6f)));
            // A chrome tap at the front end.
            float tap = Mathf.Min(RoundRect(x, y, 96, 282, 7, 10, 3), Capsule(x, y, 84, 284, 108, 284, 3.2f));
            c = Over(c, Hex("C9D3DC"), Fill(tap));
            c = Over(c, SledOutline, Stroke(tap, 2f));
            // The shower cap, pink with a frill.
            float cap = Circle(x, y, 96, 168, 20);
            c = Over(c, Hex("FF8FB8"), Fill(cap));
            c = Over(c, Hex("FFC2D8"), Fill(Circle(x, y, 90, 174, 7)));
            float frill = Shell(Circle(x, y, 96, 168, 21), 2.2f);
            c = Over(c, White(0.9f), Fill(frill));
            return c;
        }

        // ------------------------------------------------------------------ tops

        static Color ScreenPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float screen = Polygon(x, y, new[] { new Vector2(46, 218), new Vector2(146, 218), new Vector2(132, 254), new Vector2(60, 254) }) - 4f;
            c = Over(c, new Color(0.55f, 0.78f, 0.95f, 0.8f), Fill(screen));
            c = Over(c, new Color(1f, 1f, 1f, 0.75f), Fill(Capsule(x, y, 66, 244, 84, 244, 2.5f)));
            c = Over(c, new Color(1f, 1f, 1f, 0.5f), Fill(Capsule(x, y, 92, 246, 100, 246, 2f)));
            return Over(c, SledOutline, Stroke(screen, 3f));
        }

        static Color FlagPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // A whip pole at the back corner, its pennant streaming out behind.
            float pole = Capsule(x, y, 148, 46, 148, 92, 2.4f);
            float flag = Triangle(x, y, new Vector2(150, 92), new Vector2(150, 62), new Vector2(182, 72));
            c = Over(c, Hex("FF6A1A"), Fill(flag));
            c = Over(c, White(0.9f), Fill(Capsule(x, y, 154, 80, 166, 76, 2f)) * Fill(flag));
            c = Over(c, SledOutline, Stroke(flag, 2.5f));
            c = Over(c, new Color(0.2f, 0.2f, 0.22f), Fill(pole));
            float tip = Circle(x, y, 148, 94, 4.5f);
            c = Over(c, Hex("FFD23F"), Fill(tip));
            return Over(c, SledOutline, Stroke(tip, 2f));
        }

        static Color CargoPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float box = RoundRect(x, y, 96, 64, 38, 30, 9);
            float v = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(-box / 12f));
            c = Over(c, Shade(Hex("3E5A78"), v), Fill(box));
            c = Over(c, Hex("5E7FA3"), Fill(RoundRect(x, y, 96, 70, 28, 16, 6)));
            foreach (float s in new[] { -1f, 1f })
                c = Over(c, Hex("FFB020"), Fill(RoundRect(x, y, 96 + s * 20, 64, 4, 30, 1.5f)) * Fill(box));
            c = Over(c, SledOutline, Stroke(box, 3f));
            return c;
        }

        static Color AntlersPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float d = float.MaxValue;
            foreach (float s in new[] { -1f, 1f })
            {
                float X(float dx) => 96 + s * dx;
                d = Min(d, Capsule(x, y, X(16), 176, X(34), 188, 4f), Capsule(x, y, X(34), 188, X(46), 210, 3.6f),
                        Capsule(x, y, X(34), 188, X(54), 186, 3.2f), Capsule(x, y, X(42), 202, X(58), 206, 3f), Capsule(x, y, X(46), 210, X(44), 224, 3f));
            }
            c = Over(c, Hex("9A6A3E"), Fill(d));
            c = Over(c, Hex("B88655"), Fill(d + 1.5f) * Fill(Circle(x, y, 96, 230, 60)));
            return Over(c, SledOutline, Stroke(d, 2.5f));
        }

        static Color SnowmanPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Riding pillion behind you: a round body, the head on it (seen from above), a scarf and a carrot nose.
            float bodyD = Circle(x, y, 96, 92, 26);
            c = Over(c, Hex("F4F8FB"), Fill(bodyD));
            c = Over(c, new Color(0.8f, 0.86f, 0.92f), Fill(bodyD + 6f) * Mathf.Clamp01((x - 96f) / 24f));
            c = Over(c, SledOutline, Stroke(bodyD, 3f));
            float scarf = Shell(Circle(x, y, 96, 98, 16), 4f);
            c = Over(c, Hex("D8343C"), Fill(scarf));
            float tail = RoundRect(x, y, 116, 84, 5, 10, 3);
            c = Over(c, Hex("D8343C"), Fill(tail));
            float head = Circle(x, y, 96, 98, 15);
            c = Over(c, Hex("FFFFFF"), Fill(head));
            c = Over(c, SledOutline, Stroke(head, 2.5f));
            float nose = Triangle(x, y, new Vector2(91, 108), new Vector2(101, 108), new Vector2(96, 128));
            c = Over(c, Hex("FF8A1E"), Fill(nose));
            c = Over(c, SledOutline, Stroke(nose, 1.5f));
            float eyes = Mathf.Min(Circle(x, y, 89, 104, 2.6f), Circle(x, y, 103, 104, 2.6f));
            return Over(c, SledOutline, Fill(eyes));
        }

        static Color IceCrownPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float crown = Mathf.Max(Star(x, y, 96, 166, 40, 27, 8, 0.2f), -Circle(x, y, 96, 166, 23));
            c = Over(c, Hex("A8E4FF"), Fill(crown));
            c = Over(c, White(0.85f), Fill(crown + 2.5f) * Mathf.Clamp01((y - 160f) / 20f));
            c = Over(c, Hex("2C6FA8"), Stroke(crown, 2.5f));
            float gems = Min(Circle(x, y, 96, 196, 4f), Circle(x, y, 66, 166, 3.5f), Circle(x, y, 126, 166, 3.5f));
            c = Over(c, Hex("E8B923"), Fill(gems));
            return c;
        }

        // ------------------------------------------------------------------ decals (on the hood)

        static Color ZigzagPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Vector2[] p = { new(96, 312), new(84, 296), new(108, 280), new(84, 264), new(108, 248), new(96, 230) };
            float d = float.MaxValue;
            for (int i = 0; i + 1 < p.Length; i++) d = Mathf.Min(d, Capsule(x, y, p[i].x, p[i].y, p[i + 1].x, p[i + 1].y, 0f));
            c = Over(c, SledOutline, Fill(d - 6f));
            return Over(c, Hex("E9F7FF"), Fill(d - 3.6f));
        }

        static Color PinePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float tree = Min(Triangle(x, y, new Vector2(76, 262), new Vector2(116, 262), new Vector2(96, 290)),
                             Triangle(x, y, new Vector2(72, 248), new Vector2(120, 248), new Vector2(96, 278)),
                             Triangle(x, y, new Vector2(68, 234), new Vector2(124, 234), new Vector2(96, 264))) - 1f;
            float trunk = RoundRect(x, y, 96, 228, 5, 7, 1.5f);
            c = Over(c, Hex("6B4A2E"), Fill(trunk));
            c = Over(c, SledOutline, Stroke(trunk, 2f));
            c = Over(c, Hex("2E8A55"), Fill(tree));
            c = Over(c, White(0.9f), Fill(Min(Capsule(x, y, 84, 252, 92, 256, 2.2f), Capsule(x, y, 100, 268, 108, 264, 2.2f), Capsule(x, y, 80, 238, 90, 241, 2.2f))) * Fill(tree));
            return Over(c, SledOutline, Stroke(tree, 2.5f));
        }

        static Color PenguinPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float head = Ellipse(x, y, 96, 266, 21, 24);
            c = Over(c, Hex("1E2530"), Fill(head));
            float face = Mathf.Min(Circle(x, y, 88, 262, 11), Circle(x, y, 104, 262, 11));
            face = Mathf.Min(face, Ellipse(x, y, 96, 256, 15, 10));
            c = Over(c, Hex("F7FAFC"), Fill(face));
            float eyes = Mathf.Min(Circle(x, y, 89, 266, 3f), Circle(x, y, 103, 266, 3f));
            c = Over(c, Hex("1E2530"), Fill(eyes));
            float beak = Triangle(x, y, new Vector2(90, 258), new Vector2(102, 258), new Vector2(96, 248));
            c = Over(c, Hex("FFA21E"), Fill(beak));
            return Over(c, SledOutline, Stroke(head, 2.5f));
        }

        static Color NumberOnePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float disc = Circle(x, y, 96, 272, 19);
            c = Over(c, Hex("FFF6D8"), Fill(disc));
            c = Over(c, SledOutline, Stroke(disc, 2.5f));
            float one = Min(Capsule(x, y, 98, 284, 98, 261, 3.4f), Capsule(x, y, 98, 284, 90, 278, 3.2f), Capsule(x, y, 91, 261, 105, 261, 3f));
            return Over(c, Hex("1E2A26"), Fill(one));
        }

        static Color WinterCrestPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float shield = Polygon(x, y, new[] { new Vector2(72, 294), new Vector2(120, 294), new Vector2(120, 266), new Vector2(96, 242), new Vector2(72, 266) }) - 2f;
            c = Over(c, Hex("E8B923"), Fill(shield));
            c = Over(c, Hex("F6D867"), Fill(shield + 5f) * Mathf.Clamp01((x - 80f) / -10f + 1f));
            c = Over(c, SledOutline, Stroke(shield, 2.5f));
            float flake = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector2 u = new(Mathf.Cos(a), Mathf.Sin(a));
                flake = Mathf.Min(flake, Capsule(x, y, 96 - u.x * 11, 272 - u.y * 11, 96 + u.x * 11, 272 + u.y * 11, 2.2f));
            }
            return Over(c, Hex("2C6FA8"), Fill(flake));
        }
    }
}
