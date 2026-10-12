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
            AssetDatabase.SaveAssets();
            Debug.Log("Map ambience art generated");
        }

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
