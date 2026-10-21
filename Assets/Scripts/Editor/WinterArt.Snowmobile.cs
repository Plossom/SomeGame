using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// A snowmobile to race in instead of the rally car (same size and layers as the car: a body tinted in
    /// the car's colour, untinted details and a soft shadow), and a packed-snow road for snowy tracks.
    /// </summary>
    public static partial class WinterArt
    {
        [MenuItem("SomeGame/Generate Snowmobile Art")]
        public static void GenerateSnowmobile()
        {
            Write(ArtGenerator.Folder, "Snowmobile", 192, 336, SnowmobileBodyPixel, 192, false);
            Write(ArtGenerator.Folder, "SnowmobileDetails", 192, 336, SnowmobileDetailsPixel, 192, false);
            Write(ArtGenerator.Folder, "SnowmobileShadow", 192, 336, (x, y) =>
            {
                float d = Mathf.Min(RoundRect(x, y, 96, 150, 58, 118, 40), Mathf.Min(RoundRect(x, y, 42, 262, 12, 58, 10), RoundRect(x, y, 150, 262, 12, 58, 10)));
                return new Color(0, 0, 0, Mathf.Clamp01(0.5f - d / 14f));
            }, 192, false);
            Write(ArtGenerator.Folder, "PackedSnowRoad", 128, 128, PackedSnowRoadPixel, 32, true);
            ArtGenerator.Material("PackedSnowRoad", "PackedSnowRoad");
            AssetDatabase.SaveAssets();
            Debug.Log("Snowmobile art generated");
        }

        // ------------------------------------------------------------------ snowmobile (nose at the top)

        static readonly Color Rubber = new(0.15f, 0.16f, 0.18f), Steel = new(0.24f, 0.25f, 0.28f);

        // The skis with their curled tips.
        static float SkiSdf(float x, float y) => Mathf.Min(RoundRect(x, y, 34, 258, 6.5f, 62, 6.5f), RoundRect(x, y, 158, 258, 6.5f, 62, 6.5f));

        // The hood (tapered to the nose) and the running boards along both sides of the seat.
        static float SledBodySdf(float x, float y)
        {
            float hood = SMin(Polygon(x, y, new[] { new Vector2(56, 190), new Vector2(136, 190), new Vector2(114, 304), new Vector2(78, 304) }) - 10f,
                              Ellipse(x, y, 96, 212, 52, 34), 10f);
            float boards = RoundRect(x, y, 96, 132, 56, 72, 16);
            return Mathf.Min(hood, boards);
        }

        static Color SnowmobileBodyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Rear track (rubber with tread bars) sticking out behind the body.
            float track = RoundRect(x, y, 96, 70, 40, 56, 14);
            bool bar = Mathf.Repeat(y, 10f) < 4.5f;
            c = Over(c, bar ? Rubber : Shade(Rubber, 1.4f), Fill(track));
            c = Over(c, Outline, Stroke(track, 4f));
            // Ski struts and the skis.
            float struts = Mathf.Min(Capsule(x, y, 34, 250, 64, 238, 4.5f), Capsule(x, y, 158, 250, 128, 238, 4.5f));
            c = Over(c, Steel, Fill(struts));
            float skis = SkiSdf(x, y);
            c = Over(c, Rubber, Fill(skis));
            c = Over(c, new Color(0.42f, 0.43f, 0.47f), Fill(Mathf.Min(RoundRect(x, y, 32, 258, 2, 54, 2), RoundRect(x, y, 156, 258, 2, 54, 2))));
            c = Over(c, new Color(0.55f, 0.56f, 0.6f), Fill(Mathf.Min(Circle(x, y, 34, 314, 6), Circle(x, y, 158, 314, 6)))); // curled tips
            c = Over(c, Outline, Stroke(skis, 4f));

            // Body (tinted): bright on the hood's ridge, darker towards the right.
            float body = SledBodySdf(x, y);
            float inside = Mathf.Clamp01(-body / 14f);
            float v = Mathf.Lerp(0.8f, 1f, inside) - Mathf.Clamp01((x - 120f) / 60f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(body));
            c = Over(c, new Color(0.94f, 0.94f, 0.94f), Fill(Polygon(x, y, new[] { new Vector2(84, 214), new Vector2(108, 214), new Vector2(102, 296), new Vector2(90, 296) })) * Fill(body + 4f));
            // Darker footwells on the running boards.
            float wells = Mathf.Min(RoundRect(x, y, 52, 122, 9, 40, 6), RoundRect(x, y, 140, 122, 9, 40, 6));
            c = Over(c, new Color(0.66f, 0.66f, 0.66f), Fill(wells));
            c = Over(c, Outline, Stroke(body, 4.5f));

            // Long dark seat.
            float seat = RoundRect(x, y, 96, 118, 26, 62, 14);
            c = Over(c, new Color(0.14f, 0.14f, 0.16f), Fill(seat));
            c = Over(c, new Color(0.22f, 0.22f, 0.25f), Fill(RoundRect(x, y, 90, 104, 12, 40, 8)));

            // The rider: arms to the handlebar, jacket (tinted) and helmet.
            float arms = Mathf.Min(Capsule(x, y, 72, 160, 62, 198, 8f), Capsule(x, y, 120, 160, 130, 198, 8f));
            float jacket = Ellipse(x, y, 96, 144, 34, 26);
            c = Over(c, new Color(0.88f, 0.88f, 0.88f), Fill(Mathf.Min(arms, jacket)));
            c = Over(c, Outline, Stroke(Mathf.Min(arms, jacket), 3.5f));
            // Handlebar across the front of the seat with black grips (drawn over the hands).
            float handlebar = Capsule(x, y, 58, 202, 134, 202, 4f);
            c = Over(c, Steel, Fill(handlebar));
            c = Over(c, Rubber, Fill(Mathf.Min(Capsule(x, y, 54, 202, 66, 202, 6f), Capsule(x, y, 126, 202, 138, 202, 6f))));
            float helmet = Circle(x, y, 96, 158, 22);
            c = Over(c, new Color(0.97f, 0.97f, 0.98f), Fill(helmet));
            c = Over(c, Outline, Stroke(helmet, 3.5f));
            return c;
        }

        static Color SnowmobileDetailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Tinted windscreen in front of the handlebar.
            float screen = Polygon(x, y, new[] { new Vector2(64, 214), new Vector2(128, 214), new Vector2(118, 238), new Vector2(74, 238) }) - 3f;
            c = Over(c, new Color(0.55f, 0.78f, 0.95f, 0.75f), Fill(screen));
            c = Over(c, new Color(1f, 1f, 1f, 0.7f), Fill(Capsule(x, y, 76, 226, 92, 232, 2.5f)));
            c = Over(c, Outline, Stroke(screen, 3f));
            // Helmet: dark visor at the front, a stripe over the top.
            float shell = Circle(x, y, 96, 158, 19);
            c = Over(c, Hex("1D2733"), Fill(Mathf.Max(shell, Mathf.Abs(x - 96f) - 14f)) * Mathf.Clamp01(y - 164f));
            c = Over(c, new Color(1f, 1f, 1f, 0.55f), Fill(Capsule(x, y, 88, 171, 96, 174, 2f)));
            c = Over(c, Hex("E0303A"), Fill(Mathf.Max(Mathf.Abs(x - 96f) - 3.5f, shell)) * Mathf.Clamp01(160f - y));
            // Headlight on the nose, tail light on the back of the seat, snow flap behind the track.
            float head = RoundRect(x, y, 96, 296, 14, 6, 4);
            c = Over(c, Hex("FFF6D8"), Fill(head));
            c = Over(c, Outline, Stroke(head, 2.5f));
            float tail = RoundRect(x, y, 96, 52, 14, 4, 2);
            c = Over(c, Hex("E0303A"), Fill(tail));
            c = Over(c, Outline, Stroke(tail, 2f));
            float flap = RoundRect(x, y, 96, 16, 34, 4, 2);
            c = Over(c, new Color(0.12f, 0.12f, 0.13f), Fill(flap));
            // White racing chevron down the hood.
            float chevron = Mathf.Max(Shell(Mathf.Abs(x - 96f) * 0.8f + (y - 270f), 5f), Mathf.Abs(x - 96f) - 30f);
            c = Over(c, new Color(1f, 1f, 1f, 0.85f), Fill(chevron) * Fill(SledBodySdf(x, y) + 8f));
            return c;
        }

        // ------------------------------------------------------------------ packed-snow road

        // Trodden snow, clearly greyer and bluer than the fresh snow beside it so the road stays readable: soft
        // lumps, two wavy pairs of tyre ruts along the road (v), grit and the odd sparkle. Tiles both ways.
        static Color PackedSnowRoadPixel(float x, float y)
        {
            float n = Noise(x / 16f, y / 16f, 8) * 0.55f + Noise(x / 6f, y / 6f, 21) * 0.25f + Hash((int)x, (int)y) * 0.2f;
            Color c = Color.Lerp(Hex("A3B4C7"), Hex("BCCAD9"), n);
            // Ruts: compressed, a little darker, with a bright packed ridge between them.
            float wobble = (Noise(3.5f, y / 32f, 4) - 0.5f) * 10f;
            foreach (float lane in new[] { 30f, 98f })
            {
                float d = Mathf.Abs(x - lane - wobble);
                c = Color.Lerp(c, Hex("8597AD"), Mathf.Clamp01(1f - Mathf.Abs(d - 9f) / 5f) * 0.65f);
                c = Color.Lerp(c, Hex("D3DEEA"), Mathf.Clamp01(1f - d / 3f) * 0.5f);
            }
            // Grit strewn on the snow and a few sparkles.
            float h = Hash((int)(x / 2) + 31, (int)(y / 2) + 17);
            if (h > 0.985f) c = Color.Lerp(c, Hex("8C7B6A"), 0.55f);
            else if (h < 0.008f) c = Color.white;
            return c;
        }
    }
}
