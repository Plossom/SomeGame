using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The freestyle park's pieces (Winter Cup race 5, Frostbite Park): a pinball bumper seen from above (a
    /// candy-striped ring round a bright cap with a star) and the slalom poles (red and blue, each a pole with
    /// its flag).
    /// </summary>
    public static partial class WinterArt
    {
        [MenuItem("SomeGame/Generate Park Art")]
        public static void GeneratePark()
        {
            Write(ArtGenerator.Folder, "PinballBumper", 256, 256, PinballBumperPixel, 256, false);
            Write(ArtGenerator.Folder, "GateRed", 128, 128, (x, y) => GatePolePixel(x, y, Hex("E2393F")), 128, false);
            Write(ArtGenerator.Folder, "GateBlue", 128, 128, (x, y) => GatePolePixel(x, y, Hex("3D7DD8")), 128, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color PinballBumperPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 114, 112), 14f));
            float body = Circle(x, y, 128, 128, 112);
            // The skirt: red and white stripes round the outside.
            float a = Mathf.Atan2(y - 128, x - 128);
            bool stripe = Mathf.Repeat(a / (Mathf.PI * 2f) * 16f, 1f) < 0.5f;
            c = Over(c, stripe ? Hex("E2393F") : Color.white, Fill(body));
            c = Over(c, Outline, Stroke(body, 5f));
            // The ring and the cap, lit from the top left.
            float ring = Circle(x, y, 128, 128, 78);
            c = Over(c, Hex("2A2F38"), Fill(ring));
            float cap = Circle(x, y, 128, 128, 66);
            float light = Mathf.Clamp01(1f - Dist(x, y, 104, 152) / 110f);
            c = Over(c, Color.Lerp(Hex("F5A623"), Hex("FFE680"), light), Fill(cap));
            c = Over(c, Color.white, Fill(Star(x, y, 128, 128, 40, 17, 5, Mathf.PI / 2f)) * 0.95f);
            c = Over(c, Hex("C77A12"), Stroke(Star(x, y, 128, 128, 40, 17, 5, Mathf.PI / 2f), 3f) * 0.7f);
            c = Over(c, Color.white, Fill(Ellipse(x, y, 98, 160, 18, 9)) * 0.7f);
            return Over(c, Outline, Stroke(cap, 3.5f));
        }

        // A slalom pole from above: a round base in the snow, the pole's top, its flag blowing to one side.
        static Color GatePolePixel(float x, float y, Color flag)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 70, 58, 26), 6f));
            c = Over(c, SnowShade, Fill(Circle(x, y, 64, 64, 24)));
            float cloth = Polygon(x, y, new[] { new Vector2(64, 70), new Vector2(116, 84), new Vector2(110, 60), new Vector2(64, 50) });
            c = Over(c, flag, Fill(cloth));
            c = Over(c, Color.white, Fill(Polygon(x, y, new[] { new Vector2(70, 64), new Vector2(104, 72), new Vector2(102, 64), new Vector2(70, 58) })) * 0.5f);
            c = Over(c, Outline, Stroke(cloth, 2.5f));
            float pole = Circle(x, y, 64, 64, 9);
            c = Over(c, flag, Fill(pole));
            c = Over(c, Color.white, Fill(Circle(x, y, 61, 67, 3.5f)));
            return Over(c, Outline, Stroke(pole, 2.5f));
        }
    }
}
