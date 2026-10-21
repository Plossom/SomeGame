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
            // One ski centred in its own sprite (pivot at its spindle), turned with the steering like the
            // car's front wheels and put in their place (centre x 28 / 164, y 266 of the body sprite).
            Write(ArtGenerator.Folder, "SnowmobileSki", 32, 160, (x, y) => SkiPixel(x - 16f + 28f, y - 80f + 266f), 192, false);
            Write(ArtGenerator.Folder, "SnowmobileShadow", 192, 336, (x, y) =>
            {
                float d = Mathf.Min(Mathf.Min(HoodSdf(x, y), TunnelSdf(x, y)), Mathf.Min(SkiSdf(x, y), RoundRect(x, y, 96, 36, 36, 26, 10)));
                return new Color(0, 0, 0, Mathf.Clamp01(0.5f - (d - 6f) / 14f));
            }, 192, false);
            Write(ArtGenerator.Folder, "PackedSnowRoad", 128, 128, PackedSnowRoadPixel, 32, true);
            ArtGenerator.Material("PackedSnowRoad", "PackedSnowRoad");
            AssetDatabase.SaveAssets();
            Debug.Log("Snowmobile art generated");
        }

        // ------------------------------------------------------------------ snowmobile (nose at the top)

        static readonly Color Rubber = Hex("23262C"), Steel = Hex("5B616B"), Suit = Hex("2C3442"), SuitLight = Hex("46536A");

        // Skis with their upturned tips (for the shadow; the skis themselves are the "SnowmobileSki" sprite).
        static float SkiSdf(float x, float y) => Mathf.Min(SkiAt(x, y, 28f), SkiAt(x, y, 164f));

        static float SkiAt(float x, float y, float cx) => Mathf.Min(RoundRect(x, y, cx, 260, 8f, 58, 8f), Ellipse(x, y, cx, 316, 10f, 14f));

        // A ski: dark with a light keel and a silver tip.
        static Color SkiPixel(float x, float y)
        {
            float ski = SkiAt(x, y, 28f);
            Color c = Over(new Color(0, 0, 0, 0), Rubber, Fill(ski));
            c = Over(c, Hex("8C939E"), Fill(Ellipse(x, y, 28, 318, 6f, 9f)));
            c = Over(c, Hex("4A505A"), Fill(RoundRect(x, y, 28, 258, 2f, 50, 2f)));
            return Over(c, Outline, Stroke(ski, 4f));
        }

        // The hood: a pointed nose widening to the side panels in front of the rider.
        static float HoodSdf(float x, float y) => Polygon(x, y, new[]
        {
            new Vector2(50, 172), new Vector2(142, 172), new Vector2(146, 208), new Vector2(126, 286),
            new Vector2(96, 318), new Vector2(66, 286), new Vector2(46, 208),
        }) - 6f;

        // The tunnel over the track, with the footboards along both sides of the seat.
        static float TunnelSdf(float x, float y) => RoundRect(x, y, 96, 116, 48, 66, 14);

        static Color SnowmobileBodyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Rear of the track sticking out behind the tunnel, with lugs.
            float track = RoundRect(x, y, 96, 40, 34, 30, 10);
            c = Over(c, Mathf.Repeat(y, 9f) < 4f ? Rubber : Hex("383C44"), Fill(track));
            c = Over(c, Outline, Stroke(track, 4f));
            // Suspension arms from the skis to the hood, each with a tinted shock absorber.
            float arms = Min(Capsule(x, y, 30, 270, 64, 226, 4f), Capsule(x, y, 30, 256, 60, 200, 4f),
                             Capsule(x, y, 162, 270, 128, 226, 4f), Capsule(x, y, 162, 256, 132, 200, 4f));
            c = Over(c, Steel, Fill(arms));
            c = Over(c, Outline, Stroke(arms, 2.5f));
            float shocks = Mathf.Min(Capsule(x, y, 36, 262, 56, 214, 5f), Capsule(x, y, 156, 262, 136, 214, 5f));
            c = Over(c, new Color(0.9f, 0.9f, 0.9f), Fill(shocks));
            c = Over(c, Outline, Stroke(shocks, 2.5f));

            // Tunnel (tinted) with dark grippy footboards.
            float tunnel = TunnelSdf(x, y);
            c = Over(c, new Color(0.82f, 0.82f, 0.82f), Fill(tunnel));
            float boards = Mathf.Min(RoundRect(x, y, 58, 118, 10, 48, 5), RoundRect(x, y, 134, 118, 10, 48, 5));
            c = Over(c, Mathf.Repeat(y, 8f) < 3f ? Hex("2E3238") : Hex("3B4048"), Fill(boards));
            c = Over(c, Outline, Stroke(tunnel, 4.5f));

            // Hood (tinted): lit along the middle, side panels a shade darker, dark air intakes.
            float hood = HoodSdf(x, y);
            float side = Mathf.Abs(x - 96f);
            float v = Mathf.Lerp(1f, 0.8f, Mathf.InverseLerp(14f, 44f, side)) - Mathf.Clamp01((x - 120f) / 60f) * 0.06f;
            c = Over(c, new Color(v, v, v), Fill(hood));
            c = Over(c, Hex("2A2E35"), Fill(Mathf.Min(RotRect(x, y, 70, 250, 4, 16, 3, -16f), RotRect(x, y, 122, 250, 4, 16, 3, 16f))));
            c = Over(c, Outline, Stroke(hood, 4.5f));

            // Seat.
            float seat = RoundRect(x, y, 96, 106, 22, 58, 14);
            c = Over(c, Hex("1E2126"), Fill(seat));
            c = Over(c, Hex("33373F"), Fill(RoundRect(x, y, 92, 96, 10, 40, 8)));
            c = Over(c, Outline, Stroke(seat, 3f));

            // The rider in a dark suit: legs down to the boots on the footboards, arms out to the grips.
            float legs = Mathf.Min(Capsule(x, y, 78, 140, 58, 104, 11f), Capsule(x, y, 114, 140, 134, 104, 11f));
            c = Over(c, Suit, Fill(legs));
            c = Over(c, Hex("15181D"), Fill(Mathf.Min(RoundRect(x, y, 57, 94, 9, 14, 6), RoundRect(x, y, 135, 94, 9, 14, 6))));
            c = Over(c, Outline, Stroke(legs, 3f));
            float armsUp = Mathf.Min(Capsule(x, y, 66, 156, 60, 194, 10f), Capsule(x, y, 126, 156, 132, 194, 10f));
            float torso = Ellipse(x, y, 96, 146, 40, 28);
            float rider = Mathf.Min(armsUp, torso);
            c = Over(c, Suit, Fill(rider));
            c = Over(c, SuitLight, Fill(Ellipse(x, y, 88, 152, 24, 15)) * Fill(torso));
            c = Over(c, Outline, Stroke(rider, 3.5f));

            // Handlebar with gloves on the grips.
            c = Over(c, Steel, Fill(Capsule(x, y, 60, 198, 132, 198, 3.5f)));
            float gloves = Mathf.Min(Circle(x, y, 61, 197, 10f), Circle(x, y, 131, 197, 10f));
            c = Over(c, Hex("15181D"), Fill(gloves));
            c = Over(c, Outline, Stroke(gloves, 2.5f));

            // Helmet (tinted).
            float helmet = Circle(x, y, 96, 154, 27);
            c = Over(c, new Color(0.95f, 0.95f, 0.95f), Fill(helmet));
            c = Over(c, Color.white, Fill(Circle(x, y, 88, 160, 12)) * Fill(helmet));
            c = Over(c, Outline, Stroke(helmet, 3.5f));
            return c;
        }

        static Color SnowmobileDetailsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hood = HoodSdf(x, y);
            // White racing stripes swooping down both sides of the hood.
            float stripes = Mathf.Min(Capsule(x, y, 84, 300, 62, 200, 4.5f), Capsule(x, y, 108, 300, 130, 200, 4.5f));
            c = Over(c, new Color(1f, 1f, 1f, 0.92f), Fill(stripes) * Fill(hood + 6f));
            // Headlights: two slanted slits at the nose.
            float lights = Mathf.Min(RotRect(x, y, 84, 290, 9, 4, 3, 35f), RotRect(x, y, 108, 290, 9, 4, 3, -35f));
            c = Over(c, Hex("FFF6D8"), Fill(lights));
            c = Over(c, Outline, Stroke(lights, 2.5f));
            // Tall curved windscreen in front of the handlebar.
            float screen = Mathf.Max(Ellipse(x, y, 96, 206, 38, 30), 206f - y);
            c = Over(c, new Color(0.62f, 0.82f, 0.96f, 0.8f), Fill(screen));
            c = Over(c, new Color(1f, 1f, 1f, 0.75f), Fill(Capsule(x, y, 74, 214, 86, 226, 2.5f)));
            c = Over(c, Outline, Stroke(screen, 3f));
            // Helmet: dark visor at the front and a white stripe over the top.
            float shell = Circle(x, y, 96, 154, 23);
            c = Over(c, Hex("1D2733"), Fill(Mathf.Max(shell, 165f - y)));
            c = Over(c, new Color(1f, 1f, 1f, 0.6f), Fill(Capsule(x, y, 86, 172, 96, 175, 2.2f)));
            c = Over(c, new Color(1f, 1f, 1f, 0.9f), Fill(Mathf.Max(Mathf.Abs(x - 96f) - 4f, Circle(x, y, 96, 154, 25))) * Mathf.Clamp01(160f - y));
            // Tail light across the back of the tunnel and the snow flap behind the track.
            float tail = RoundRect(x, y, 96, 54, 18, 3.5f, 2);
            c = Over(c, Hex("E0303A"), Fill(tail));
            c = Over(c, Outline, Stroke(tail, 2f));
            float flap = RoundRect(x, y, 96, 10, 34, 6, 3);
            c = Over(c, Hex("16181C"), Fill(flap));
            c = Over(c, new Color(1f, 1f, 1f, 0.8f), Fill(RoundRect(x, y, 96, 10, 14, 2, 1)));
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
