using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Midnight Express (Winter Cup race 6): the steam locomotive and its flat wagons seen from above (front
    /// at the top), and a gift box (white, tinted in the game) for the presents spilled over the street.
    /// </summary>
    public static partial class WinterArt
    {
        [MenuItem("SomeGame/Generate Express Art")]
        public static void GenerateExpress()
        {
            Write(ArtGenerator.Folder, "SteamLoco", 192, 448, SteamLocoPixel, 56, false);
            Write(ArtGenerator.Folder, "FlatWagon", 192, 368, FlatWagonPixel, 56, false);
            Write(ArtGenerator.Folder, "GiftBox", 96, 96, GiftBoxPixel, 96, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // The locomotive from above: a red cowcatcher, the long black boiler with brass bands, the chimney and
        // the steam dome, the cab with its roof at the back, snow on top.
        static Color SteamLocoPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float frame = RoundRect(x, y, 96, 214, 86, 200, 14);
            c = Over(c, Hex("2A2A2E"), Fill(frame));
            // Cowcatcher at the front (top).
            float catcher = Triangle(x, y, new Vector2(20, 404), new Vector2(172, 404), new Vector2(96, 444));
            c = Over(c, Hex("C0392B"), Fill(catcher));
            for (int k = -3; k <= 3; k++) c = Over(c, Hex("7A1E16"), Fill(SegDist(new Vector2(x, y), new Vector2(96 + k * 18, 406), new Vector2(96 + k * 6, 440)) - 1.5f) * Fill(catcher));
            c = Over(c, Outline, Stroke(catcher, 3f));
            // The boiler: a long cylinder, lit along one side.
            float boiler = RoundRect(x, y, 96, 270, 58, 128, 50);
            float shade = Mathf.Clamp01((x - 50f) / 92f);
            c = Over(c, Color.Lerp(Hex("4A4D55"), Hex("15161A"), shade), Fill(boiler));
            c = Over(c, new Color(1f, 1f, 1f, 0.18f), Fill(RoundRect(x, y, 72, 270, 10, 118, 8)));
            for (int k = 0; k < 4; k++) c = Over(c, Hex("D9A441"), Fill(Mathf.Abs(y - (190 + k * 52)) - 3f) * Fill(boiler));
            c = Over(c, Outline, Stroke(boiler, 3f));
            // Chimney and steam dome.
            float chimney = Circle(x, y, 96, 362, 22);
            c = Over(c, Hex("1C1D21"), Fill(chimney));
            c = Over(c, Hex("0B0B0D"), Fill(Circle(x, y, 96, 362, 13)));
            c = Over(c, Outline, Stroke(chimney, 3f));
            float dome = Circle(x, y, 96, 286, 18);
            c = Over(c, Hex("D9A441"), Fill(dome));
            c = Over(c, Hex("FFE08A"), Fill(Circle(x, y, 90, 292, 6)));
            c = Over(c, Outline, Stroke(dome, 2.5f));
            // The cab: a red box with a curved roof, snow on it.
            float cab = RoundRect(x, y, 96, 92, 80, 64, 10);
            c = Over(c, Hex("A8322D"), Fill(cab));
            float roof = RoundRect(x, y, 96, 96, 74, 56, 16);
            c = Over(c, Color.Lerp(Hex("3A3B40"), Hex("222327"), shade), Fill(roof));
            c = Over(c, Snow, Fill(RoundRect(x, y, 90, 104, 56, 38, 14)) * 0.95f);
            c = Over(c, Outline, Stroke(roof, 3f));
            c = Over(c, Outline, Stroke(cab, 3f));
            return Over(c, Outline, Stroke(frame, 3.5f) * 0.8f);
        }

        // A flat wagon: a deck of wooden planks in a dark steel frame, low rails along the sides, couplings at
        // both ends, a dusting of snow at the edges.
        static Color FlatWagonPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float frame = RoundRect(x, y, 96, 184, 90, 170, 8);
            c = Over(c, Hex("2A2D33"), Fill(frame));
            float deck = RoundRect(x, y, 96, 184, 78, 160, 4);
            Color plank = Mathf.Repeat(y, 20f) < 2f ? Hex("6B4426") : Color.Lerp(Hex("A9713F"), Hex("C18950"), Noise(x / 30f, y / 8f, 17));
            c = Over(c, plank, Fill(deck));
            // Snow drifted against the side rails.
            float edge = Mathf.Min(x - 18f, 174f - x);
            if (edge < 14f && Noise(x / 10f, y / 10f, 16) > 0.35f) c = Over(c, Snow, Fill(deck) * 0.85f);
            foreach (float sx in new[] { 12f, 180f }) c = Over(c, Hex("D9A441"), Fill(RoundRect(x, y, sx, 184, 4, 164, 2)));
            foreach (float cy in new[] { 8f, 360f }) c = Over(c, Hex("1C1D21"), Fill(RoundRect(x, y, 96, cy, 10, 8, 3)));
            return Over(c, Outline, Stroke(frame, 3.5f));
        }

        // A gift box from above (white paper, tinted in the game): a ribbon cross and a bow.
        static Color GiftBoxPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(0f, 0f, 0f, 0.3f), Soft(RoundRect(x, y, 52, 44, 36, 36, 5), 6f));
            float box = RoundRect(x, y, 48, 48, 36, 36, 5);
            c = Over(c, Color.white, Fill(box));
            c = Over(c, new Color(0.82f, 0.82f, 0.86f), Fill(box) * Mathf.Clamp01((x - y) / 70f + 0.3f) * 0.5f);
            float ribbon = Mathf.Min(Mathf.Abs(x - 48f) - 6f, Mathf.Abs(y - 48f) - 6f);
            c = Over(c, Hex("F5C842"), Fill(ribbon) * Fill(box));
            foreach (float s in new[] { -1f, 1f }) c = Over(c, Hex("F5C842"), Fill(Ellipse(x, y, 48 + s * 11f, 52, 10, 6)));
            c = Over(c, Hex("C9952A"), Fill(Circle(x, y, 48, 50, 5)));
            return Over(c, Outline, Stroke(box, 3f));
        }
    }
}
