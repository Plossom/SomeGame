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
            // One ski centred in its own sprite, turned with the steering like the buggy's front wheels and
            // put in their place (centre x 28 / 164, y 266 of the body sprite).
            Write(ArtGenerator.Folder, "SnowmobileSki", 40, 112, (x, y) => SkiPixel(x - 20f + 28f, y - 56f + 266f), 192, false);
            // The whole snowmobile with its skis in one picture, for the map and the loading bar.
            Write(ArtGenerator.Folder, "SnowmobileIcon", 192, 336, (x, y) =>
            {
                Color skis = Over(SkiPixel(x, y), SkiPixel(x - 136f, y), 1f);
                return Over(skis, SnowmobileBodyPixel(x, y), 1f);
            }, 192, false);
            Write(ArtGenerator.Folder, "SnowmobileShadow", 192, 336, (x, y) =>
            {
                float d = Mathf.Min(Mathf.Min(SledBodySdf(x, y), SledTrackSdf(x, y)), Mathf.Min(SkiAt(x, y, 28f), SkiAt(x, y, 164f)));
                return new Color(0, 0, 0, Mathf.Clamp01(0.5f - (d - 4f) / 14f));
            }, 192, false);
            Write(ArtGenerator.Folder, "PackedSnowRoad", 128, 128, PackedSnowRoadPixel, 32, true);
            ArtGenerator.Material("PackedSnowRoad", "PackedSnowRoad");
            AssetDatabase.SaveAssets();
            Debug.Log("Snowmobile art generated");
        }

        // ------------------------------------------------------------------ snowmobile (nose at the top)
        // In the buggy's style: chunky shapes with the same outline and shading, the body tinted in the
        // racer's colour, light tubes like the buggy's roll cage. Short skis in place of the front wheels
        // (their own sprite), a wide rubber track at the back like the buggy's rear tyres.

        static readonly Color SledOutline = Hex("14231E"), Tyre = new(0.11f, 0.11f, 0.11f), TyreLight = new(0.17f, 0.17f, 0.17f);
        static readonly Color Suit = new(0.2f, 0.21f, 0.24f), Cockpit = new(0.13f, 0.13f, 0.14f);

        // A short ski with a rounded, upturned tip.
        static float SkiAt(float x, float y, float cx) => Mathf.Min(RoundRect(x, y, cx, 256, 10f, 36, 8f), Ellipse(x, y, cx, 290, 10f, 13f));

        static Color SkiPixel(float x, float y)
        {
            float ski = SkiAt(x, y, 28f);
            Color c = Over(new Color(0, 0, 0, 0), TyreLight, Fill(ski));
            c = Over(c, new Color(0.55f, 0.57f, 0.6f), Fill(Ellipse(x, y, 28, 293, 6f, 9f))); // the tip catches the light
            c = Over(c, Tyre, Fill(RoundRect(x, y, 28, 250, 3f, 26, 2f)));                    // runner groove
            return Over(c, SledOutline, Stroke(ski, 4f));
        }

        // The wide track at the back, sticking out beside and behind the body like rear tyres.
        static float SledTrackSdf(float x, float y) => RoundRect(x, y, 96, 76, 62, 44, 12);

        // Bodywork: tapered hood, the seat section and side footboards.
        static float SledBodySdf(float x, float y)
        {
            float hood = Polygon(x, y, new[] { new Vector2(46, 214), new Vector2(146, 214), new Vector2(126, 314), new Vector2(66, 314) }) - 8f;
            float middle = RoundRect(x, y, 96, 150, 50, 70, 16);
            return Mathf.Min(hood, middle);
        }

        // Light tubes like the buggy's cage: a bumper loop round the nose and the handlebar.
        static float SledTubesSdf(float x, float y)
        {
            const float r = 4.2f;
            float bumper = Min(Capsule(x, y, 52, 290, 70, 326, r), Capsule(x, y, 70, 326, 122, 326, r), Capsule(x, y, 122, 326, 140, 290, r));
            float bar = Capsule(x, y, 54, 212, 138, 212, r);
            return Mathf.Min(bumper, bar);
        }

        static Color SnowmobileBodyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Ski arms (dark, like the buggy's suspension arms).
            float arms = Min(Capsule(x, y, 58, 252, 30, 258, 4.5f), Capsule(x, y, 58, 274, 30, 270, 4.5f),
                             Capsule(x, y, 134, 252, 162, 258, 4.5f), Capsule(x, y, 134, 274, 162, 270, 4.5f));
            c = Over(c, new Color(0.16f, 0.16f, 0.16f), Fill(arms));
            // Rubber track with tread blocks.
            float track = SledTrackSdf(x, y);
            c = Over(c, Mathf.Repeat(y, 9f) < 4f ? Tyre : TyreLight, Fill(track));
            c = Over(c, SledOutline, Stroke(track, 4f));

            // Bodywork, shaded like the buggy's.
            float body = SledBodySdf(x, y);
            float inside = Mathf.Clamp01(-body / 14f);
            float v = Mathf.Lerp(0.8f, 1f, inside) - Mathf.Clamp01((x - 120f) / 60f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(body));
            // Raised centre of the hood catches the light.
            c = Over(c, new Color(0.92f, 0.92f, 0.92f), Fill(Polygon(x, y, new[] { new Vector2(80, 232), new Vector2(112, 232), new Vector2(104, 304), new Vector2(88, 304) })) * Fill(body + 4f));
            c = Over(c, SledOutline, Stroke(body, 4.5f));

            // Dark seat (the buggy's cockpit colour) and grippy footboards either side.
            float seat = RoundRect(x, y, 96, 136, 24, 42, 12);
            c = Over(c, Cockpit, Fill(seat));
            c = Over(c, new Color(0.24f, 0.24f, 0.26f), Fill(RoundRect(x, y, 96, 124, 14, 24, 8)));
            float boards = Mathf.Min(RoundRect(x, y, 60, 128, 9, 40, 5), RoundRect(x, y, 132, 128, 9, 40, 5));
            c = Over(c, Mathf.Repeat(y, 8f) < 3f ? Cockpit : new Color(0.22f, 0.22f, 0.24f), Fill(boards));

            // The rider: dark suit, arms to the handlebar.
            float armsUp = Mathf.Min(Capsule(x, y, 72, 168, 60, 208, 9.5f), Capsule(x, y, 120, 168, 132, 208, 9.5f));
            float torso = Ellipse(x, y, 96, 160, 34, 24);
            float rider = Mathf.Min(armsUp, torso);
            c = Over(c, Suit, Fill(rider));
            c = Over(c, SledOutline, Stroke(rider, 3.5f));

            // Tubes on top (tinted), each with an outline.
            float tubes = SledTubesSdf(x, y);
            c = Over(c, new Color(0.95f, 0.95f, 0.95f), Fill(tubes));
            c = Over(c, SledOutline, Stroke(tubes, 2.5f));
            // Gloves on the grips.
            float gloves = Mathf.Min(Circle(x, y, 60, 211, 9f), Circle(x, y, 132, 211, 9f));
            c = Over(c, Cockpit, Fill(gloves));
            c = Over(c, SledOutline, Stroke(gloves, 2.5f));

            // Helmet (tinted).
            float helmet = Circle(x, y, 96, 166, 24);
            c = Over(c, new Color(0.95f, 0.95f, 0.95f), Fill(helmet));
            c = Over(c, SledOutline, Stroke(helmet, 3.5f));
            return c;
        }

        static Color SnowmobileDetailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Windscreen on the back of the hood.
            float screen = RoundRect(x, y, 96, 232, 32, 10, 6);
            c = Over(c, new Color(0.62f, 0.82f, 0.96f, 0.85f), Fill(screen));
            c = Over(c, new Color(1f, 1f, 1f, 0.7f), Fill(Capsule(x, y, 72, 234, 84, 234, 2.2f)));
            c = Over(c, SledOutline, Stroke(screen, 3f));
            // Helmet: dark visor at the front and a white highlight.
            c = Over(c, Hex("1D2733"), Fill(Mathf.Max(Circle(x, y, 96, 166, 20), 172f - y)));
            c = Over(c, new Color(1f, 1f, 1f, 0.45f), Fill(Circle(x, y, 88, 160, 7f)));
            // Headlights on the nose and tail lights on the back, as on the buggy.
            float heads = Mathf.Min(Circle(x, y, 80, 304, 7), Circle(x, y, 112, 304, 7));
            c = Over(c, Hex("FFF6D8"), Fill(heads));
            c = Over(c, SledOutline, Stroke(heads, 2.5f));
            float tails = Mathf.Min(RoundRect(x, y, 74, 84, 8, 4, 2), RoundRect(x, y, 118, 84, 8, 4, 2));
            c = Over(c, Hex("E0303A"), Fill(tails));
            return c;
        }

        // ------------------------------------------------------------------ packed-snow road

        // Packed snow, clearly bluer and greyer than the fresh snow beside it so the road stays readable:
        // soft mottling, nothing in lines or dots. Tiles both ways.
        static Color PackedSnowRoadPixel(float x, float y)
        {
            float n = Noise(x / 32f, y / 32f, 4) * 0.45f + Noise(x / 12.8f, y / 12.8f, 10) * 0.35f + Noise(x / 4f, y / 4f, 32) * 0.2f;
            Color c = Color.Lerp(Hex("ADBDCF"), Hex("C4D1DF"), n);
            return c;
        }
    }
}
