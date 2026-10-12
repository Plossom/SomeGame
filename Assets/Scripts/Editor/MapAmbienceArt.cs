using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Sprites for the animated life on the map screens (<see cref="SomeGame.UI.MapAmbience"/>): a cloud,
    /// a bird seen from the side, a striped hot-air balloon, a water glint and a ski-lift chair.
    /// </summary>
    public static class MapAmbienceArt
    {
        static readonly Color Outline = Hex("1E3A30");

        [MenuItem("SomeGame/Generate Map Ambience Art")]
        public static void Generate()
        {
            Write(ArtGenerator.Folder, "MapCloud", 256, 128, CloudPixel, 100, false);
            Write(ArtGenerator.Folder, "MapBird", 64, 32, BirdPixel, 100, false);
            Write(ArtGenerator.Folder, "MapBalloon", 128, 176, BalloonPixel, 100, false);
            Write(ArtGenerator.Folder, "Sparkle", 64, 64, SparklePixel, 100, false);
            Write(ArtGenerator.Folder, "LiftChair", 48, 64, ChairPixel, 100, false);
            GenerateNavArrow();
            GenerateEdgeShade();
            GenerateRays();
            GenerateFinger();
            Write(ArtGenerator.Folder, "MapFisher", 64, 80, (x, y) => FisherPixel(x, y, Vector2.zero, new Color(0f, 0f, 0f, 0f), true), 100, false);
            Write(ArtGenerator.Folder, "MapRowboat", 128, 80, RowboatPixel, 100, false);
            AssetDatabase.SaveAssets();
            Debug.Log("Map ambience art generated");
        }

        /// <summary>A white pointing hand (index finger up) with a dark outline, for the tutorial's gestures. The fingertip is at (64, 154).</summary>
        [MenuItem("SomeGame/Generate Finger")]
        public static void GenerateFinger() => Write(ArtGenerator.Folder, "Finger", 128, 160, (x, y) =>
        {
            float finger = Capsule(x, y, 64f, 62f, 64f, 140f, 14f);
            float palm = RoundRect(x, y, 70f, 46f, 34f, 30f, 16f);
            float knuckles = Mathf.Min(Circle(x, y, 88f, 70f, 12.5f), Circle(x, y, 102f, 58f, 11.5f));
            float thumb = Capsule(x, y, 38f, 50f, 22f, 82f, 11f);
            float hand = SMin(SMin(SMin(finger, palm, 6f), knuckles, 5f), thumb, 6f);
            Color c = new(0f, 0f, 0f, 0f);
            c = Over(c, new Color(0f, 0f, 0f, 0.25f), Mathf.Clamp01(0.5f - (hand + 3f) / 6f));
            c = Over(c, Color.white, Cover(hand));
            c = Over(c, new Color(0.85f, 0.88f, 0.9f, 1f), Cover(Mathf.Max(hand + 2f, -(x - 78f)))); // a soft shade on the right
            c = Over(c, Hex("173D33"), Stroke(hand, 5f));
            c = Over(c, Hex("C9D2D6"), Cover(RoundRect(x, y, 64f, 136f, 7f, 6f, 4f))); // nail
            return c;
        }, 100, false);

        /// <summary>Light rays for celebrations: sixteen soft wedges from the centre, fading outwards.</summary>
        [MenuItem("SomeGame/Generate Rays")]
        public static void GenerateRays() => Write(ArtGenerator.Folder, "Rays", 512, 512, (x, y) =>
        {
            float dx = x - 256f, dy = y - 256f, r = Mathf.Sqrt(dx * dx + dy * dy) / 256f;
            float a = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) * 16f;
            float wedge = Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Repeat(a, 1f) - 0.5f) * 2f);
            return White(Mathf.Clamp01(1f - r) * wedge * Mathf.SmoothStep(0f, 0.15f, r));
        }, 100, false);

        /// <summary>A soft shadow strip: clear on the left, dark on the right (tinted and flipped in use).</summary>
        [MenuItem("SomeGame/Generate Edge Shade")]
        public static void GenerateEdgeShade() =>
            Write(ArtGenerator.Folder, "EdgeShade", 64, 4, (x, y) => new Color(0f, 0f, 0f, Mathf.Pow(x / 64f, 2.2f)), 100, false);

        /// <summary>The car in the race HUD's track view: a slim navigation arrow pointing up (white, tinted orange in the HUD).</summary>
        [MenuItem("SomeGame/Generate Nav Arrow")]
        public static void GenerateNavArrow() => Write(ArtGenerator.Folder, "NavArrow", 64, 96, NavArrowPixel, 100, false);

        static Color NavArrowPixel(float x, float y)
        {
            float d = Polygon(x, y, new[] { new Vector2(32f, 90f), new Vector2(57f, 8f), new Vector2(32f, 26f), new Vector2(7f, 8f) });
            return White(Cover(d));
        }

        // A little angler seen from the side, facing left, sitting (on a rock when on the shore); the hands
        // (where the rod goes) are at (22, 40) plus the offset.
        internal static Color FisherPixel(float x, float y, Vector2 o, Color c, bool rock)
        {
            x -= o.x; y -= o.y;
            if (rock)
            {
                float stone = Sdf.Ellipse(x, y, 40f, 10f, 17f, 9f);
                c = Over(c, Hex("8F918A"), Cover(stone));
                c = Over(c, Hex("B6B7AE"), Cover(Sdf.Ellipse(x, y, 36f, 13f, 10f, 4f)));
                c = Over(c, Outline, Stroke(stone, 2.4f));
            }
            float legs = Mathf.Min(Capsule(x, y, 40f, 22f, 22f, 22f, 4.5f), Capsule(x, y, 22f, 22f, 18f, 9f, 4f));
            c = Over(c, Hex("2E4A6B"), Cover(legs));
            c = Over(c, Hex("3A2A20"), Cover(Sdf.Ellipse(x, y, 15f, 7f, 6f, 3.5f))); // boot
            float body = RoundRect(x, y, 39f, 36f, 9f, 14f, 5f);
            c = Over(c, Hex("D9433A"), Cover(body));
            float arm = Capsule(x, y, 36f, 45f, 23f, 40f, 3.6f);
            c = Over(c, Hex("B8352D"), Cover(arm));
            c = Over(c, Hex("F2C29B"), Cover(Circle(x, y, 22f, 40f, 3.4f))); // hands
            float head = Circle(x, y, 37f, 58f, 8.5f);
            c = Over(c, Hex("F2C29B"), Cover(head));
            float hat = Mathf.Min(Sdf.Ellipse(x, y, 36f, 64f, 13f, 2.8f), RoundRect(x, y, 38f, 69f, 7.5f, 5f, 3f));
            c = Over(c, Hex("E8B44A"), Cover(hat));
            c = Over(c, Outline, Stroke(Mathf.Min(Mathf.Min(body, head), Mathf.Min(legs, Mathf.Min(arm, hat))), 2.2f));
            return c;
        }

        // A small wooden rowing boat on the water with an angler in it (hands at (56, 54)).
        static Color RowboatPixel(float x, float y)
        {
            Color c = new(0f, 0f, 0f, 0f);
            c = FisherPixel(x, y, new Vector2(34f, 14f), c, false);
            var hull = new[] { new Vector2(6f, 26f), new Vector2(122f, 26f), new Vector2(108f, 6f), new Vector2(20f, 6f) };
            float d = Polygon(x, y, hull);
            c = Over(c, Hex("A0703F"), Cover(d));
            c = Over(c, Hex("7A5230"), Cover(Mathf.Max(d, Mathf.Abs(y - 15f) - 1.4f)));
            c = Over(c, Hex("E9D8B4"), Cover(Mathf.Max(d, -(y - 23f)))); // gunwale
            c = Over(c, Outline, Stroke(d, 2.4f));
            return c;
        }

        static float Cover(float d) => Mathf.Clamp01(0.5f - d);

        static Color CloudPixel(float x, float y)
        {
            float d = Min(Circle(x, y, 70f, 56f, 34f), Circle(x, y, 122f, 76f, 46f), Circle(x, y, 182f, 58f, 34f),
                RoundRect(x, y, 126f, 44f, 104f, 26f, 26f));
            Color c = Color.Lerp(Hex("E4ECF3"), Color.white, Mathf.InverseLerp(30f, 70f, y));
            c.a = Cover(d);
            return c;
        }

        // An "m": two wings meeting at the body. Flapping is done by squashing it.
        static Color BirdPixel(float x, float y)
        {
            var body = new Vector2(32f, 13f);
            float d = Min(Capsule(x, y, 5f, 19f, 18f, 24f, 2.6f), Capsule(x, y, 18f, 24f, body.x, body.y, 2.6f),
                Capsule(x, y, body.x, body.y, 46f, 24f, 2.6f), Capsule(x, y, 46f, 24f, 59f, 19f, 2.6f));
            Color c = Hex("2A3A3A");
            c.a = Cover(d);
            return c;
        }

        static Color BalloonPixel(float x, float y)
        {
            Color c = new(0f, 0f, 0f, 0f);
            // Ropes and the basket.
            float ropes = Min(Capsule(x, y, 47f, 60f, 54f, 34f, 1.3f), Capsule(x, y, 81f, 60f, 74f, 34f, 1.3f));
            c = Over(c, Hex("6B4A33"), Cover(ropes));
            float basket = RoundRect(x, y, 64f, 24f, 13f, 11f, 3f);
            c = Over(c, Hex("A0703F"), Cover(basket));
            c = Over(c, Hex("7A5230"), Cover(RoundRect(x, y, 64f, 31f, 14f, 3f, 1.5f)));
            c = Over(c, Outline, Stroke(basket, 2.4f));
            // The envelope: a round top narrowing to the mouth, in orange and cream gores, shaded on the right.
            float envelope = Mathf.Min(Circle(x, y, 64f, 118f, 50f),
                Polygon(x, y, new[] { new Vector2(17f, 108f), new Vector2(111f, 108f), new Vector2(80f, 60f), new Vector2(48f, 60f) }));
            // Gores follow the round shape: split the envelope's width at this height into six bands.
            float halfWidth = y >= 108f ? Mathf.Sqrt(Mathf.Max(1f, 2500f - (y - 118f) * (y - 118f))) : Mathf.Lerp(16f, 47f, Mathf.InverseLerp(60f, 108f, y));
            float u = Mathf.Clamp((x - 64f) / halfWidth, -1f, 1f);
            int band = Mathf.FloorToInt((Mathf.Asin(u) / Mathf.PI + 0.5f) * 6f);
            Color stripe = band % 2 == 0 ? Hex("FF7A1A") : Hex("F4E6C8");
            stripe = Color.Lerp(stripe, Color.black, Mathf.InverseLerp(40f, 112f, x) * 0.18f);
            c = Over(c, stripe, Cover(envelope));
            c = Over(c, Hex("C2530A"), Cover(Mathf.Max(envelope, Mathf.Abs(y - 64f) - 4f)));
            c = Over(c, new Color(1f, 1f, 1f, 0.35f), Cover(Circle(x, y, 46f, 136f, 12f)) * Cover(envelope + 4f));
            c = Over(c, Outline, Stroke(envelope, 2.6f));
            return c;
        }

        static Color SparklePixel(float x, float y)
        {
            float star = Star(x, y, 32f, 32f, 30f, 5f, 4, 0f);
            float glow = Gauss(Dist(x, y, 32f, 32f), 9f) * 0.6f;
            return White(Mathf.Max(Cover(star), glow));
        }

        // Hangs from its top centre: the hanger, a red seat and its backrest.
        static Color ChairPixel(float x, float y)
        {
            Color c = new(0f, 0f, 0f, 0f);
            Color frame = Hex("1B2F45");
            c = Over(c, frame, Cover(Capsule(x, y, 24f, 62f, 24f, 22f, 2f)));
            c = Over(c, frame, Cover(Capsule(x, y, 24f, 22f, 10f, 22f, 2f)));
            float seat = RoundRect(x, y, 24f, 14f, 18f, 4.5f, 2f), back = RoundRect(x, y, 9f, 24f, 4.5f, 10f, 2f);
            c = Over(c, Hex("E2483A"), Cover(Mathf.Min(seat, back)));
            c = Over(c, frame, Stroke(Mathf.Min(seat, back), 2f));
            return c;
        }
    }
}
