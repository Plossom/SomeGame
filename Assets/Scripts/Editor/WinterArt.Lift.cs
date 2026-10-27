using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The cable car that carries the snowmobiles across a ravine (TrackLift), seen from above: the boarding
    /// platform lying across the road (planks, yellow-and-black edges, the bull wheel), a lattice tower,
    /// the cable, and the red hanger that grips a snowmobile (a frame round it with the grip on the cable).
    /// </summary>
    public static partial class WinterArt
    {
        [MenuItem("SomeGame/Generate Lift Art")]
        public static void GenerateLift()
        {
            Write(ArtGenerator.Folder, "LiftStation", 256, 128, LiftStationPixel, 25, false);
            Write(ArtGenerator.Folder, "LiftTower", 128, 128, LiftTowerPixel, 40, false);
            Write(ArtGenerator.Folder, "LiftCable", 16, 16, (x, y) => Hex("2A2F38"), 16, false);
            Write(ArtGenerator.Folder, "LiftHanger", 192, 336, LiftHangerPixel, 192, false);
            AssetDatabase.SaveAssets();
        }

        // The boarding platform (u across the road, v along it): wooden planks, hazard stripes at both ends,
        // the bull wheel in the middle and arrows pointing on.
        static Color LiftStationPixel(float x, float y)
        {
            float deck = RoundRect(x, y, 128, 64, 124, 58, 10);
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(0.1f, 0.15f, 0.25f, 0.3f), Fill(RoundRect(x - 4, y + 5, 128, 64, 124, 58, 10)));
            Color plank = Mathf.Repeat(x, 20f) < 1.5f ? Hex("7A4E2E") : Color.Lerp(Hex("B07A4A"), Hex("C48C58"), Noise(x / 20f, y / 9f, 13));
            c = Over(c, plank, Fill(deck));
            bool edge = y < 22f || y > 106f;
            if (edge) c = Over(c, ((int)((x + y) / 14f) % 2 == 0) ? Hex("F2C230") : Hex("2A2A2A"), Fill(deck));
            // arrows pointing on (+v)
            for (int k = -1; k <= 1; k++)
            {
                float cx = 128 + k * 70;
                float chevron = Mathf.Abs(Mathf.Abs(x - cx) * 0.9f + (y - 76f)) - 5f;
                if (Mathf.Abs(x - cx) < 20f) c = Over(c, new Color(1f, 1f, 1f, 0.85f), Fill(chevron) * Fill(deck));
            }
            float wheel = Circle(x, y, 128, 64, 22);
            c = Over(c, Hex("C0392B"), Fill(wheel));
            c = Over(c, Hex("2A2F38"), Fill(Circle(x, y, 128, 64, 8)));
            c = Over(c, Hex("2A2F38"), Stroke(wheel, 4f));
            return Over(c, Hex("3B2A1E"), Stroke(deck, 4f));
        }

        // A lattice tower from above: a square frame with cross braces and the cable saddle.
        static Color LiftTowerPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(0.1f, 0.15f, 0.25f, 0.3f), Fill(RoundRect(x - 6, y + 7, 64, 64, 34, 34, 6)));
            float frame = Shell(RoundRect(x, y, 64, 64, 32, 32, 6), 4f);
            float braces = Mathf.Min(Capsule(x, y, 36, 36, 92, 92, 3f), Capsule(x, y, 36, 92, 92, 36, 3f));
            c = Over(c, Hex("7C8796"), Fill(Mathf.Min(frame, braces)));
            float saddle = RoundRect(x, y, 64, 64, 44, 8, 4);
            c = Over(c, Hex("C0392B"), Fill(saddle));
            return Over(c, Hex("2A2F38"), Stroke(Mathf.Min(frame, saddle), 2.5f));
        }

        // The hanger round a snowmobile (same size as the car sprite): a red frame of bars with the grip
        // on top in the middle, where it hangs from the cable.
        static Color LiftHangerPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float frame = Shell(RoundRect(x, y, 96, 168, 78, 150, 26), 5f);
            float arms = Mathf.Min(Capsule(x, y, 96, 168, 34, 70, 4f), Capsule(x, y, 96, 168, 158, 70, 4f));
            arms = Mathf.Min(arms, Mathf.Min(Capsule(x, y, 96, 168, 34, 266, 4f), Capsule(x, y, 96, 168, 158, 266, 4f)));
            c = Over(c, Hex("D8433A"), Fill(frame));
            c = Over(c, new Color(0.85f, 0.25f, 0.2f, 0.8f), Fill(arms));
            c = Over(c, Hex("2A2F38"), Stroke(frame, 2.5f));
            float grip = RoundRect(x, y, 96, 168, 22, 30, 8);
            c = Over(c, Hex("2A2F38"), Fill(grip));
            c = Over(c, Hex("8C939E"), Fill(Mathf.Min(Circle(x, y, 96, 150, 7), Circle(x, y, 96, 186, 7))));
            return c;
        }
    }
}
