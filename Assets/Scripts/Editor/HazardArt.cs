using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Sprites for the moving surprises on the tracks, seen from above: a spring jump pad for dry ground,
    /// a penguin (it waddles by tilting), a big rolling snowball and a glowing dash panel with chevrons.
    /// </summary>
    public static class HazardArt
    {
        static readonly Color Outline = Hex("16283A");
        static readonly Color Shadow = new(0.12f, 0.2f, 0.36f, 0.3f);

        [MenuItem("SomeGame/Generate Hazard Art")]
        public static void Generate()
        {
            Write(ArtGenerator.Folder, "JumpPad", 128, 128, JumpPadPixel, 64, false);
            Write(ArtGenerator.Folder, "Penguin", 96, 96, PenguinPixel, 64, false);
            Write(ArtGenerator.Folder, "Snowball", 128, 128, SnowballPixel, 64, false);
            Write(ArtGenerator.Folder, "DashPanel", 128, 128, DashPanelPixel, 64, false);
            AssetDatabase.SaveAssets();
            Debug.Log("Hazard art generated");
        }

        static float Soft(float d, float width) => Mathf.Clamp01(0.5f - d / width);

        // A round spring pad: orange rim, striped springy top, white bullseye.
        static Color JumpPadPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 70, 58, 56), 6f));
            float rim = Circle(x, y, 64, 64, 56);
            c = Over(c, Hex("FF7A1A"), Fill(rim));
            float top = Circle(x, y, 64, 64, 44);
            bool stripe = Mathf.Repeat(Dist(x, y, 64, 64), 12f) < 6f;
            c = Over(c, stripe ? Hex("F4F2EC") : Hex("D9E3EE"), Fill(top));
            c = Over(c, Hex("FF7A1A"), Fill(Circle(x, y, 64, 64, 12)));
            c = Over(c, Outline, Stroke(rim, 4f));
            c = Over(c, Hex("B4510C"), Stroke(top, 3f));
            return c;
        }

        // From above, heading up: black back and head, white belly showing at the sides, orange beak and feet.
        static Color PenguinPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Sdf.Ellipse(x, y, 52, 42, 26, 32), 6f));
            c = Over(c, Hex("FF9A2E"), Fill(Mathf.Min(Sdf.Ellipse(x, y, 36, 18, 8, 6), Sdf.Ellipse(x, y, 60, 18, 8, 6)))); // feet
            float body = Sdf.Ellipse(x, y, 48, 44, 24, 30);
            c = Over(c, Hex("F4F7FA"), Fill(body));
            c = Over(c, Hex("222A35"), Fill(Sdf.Ellipse(x, y, 48, 48, 19, 27)));
            float flippers = Mathf.Min(Sdf.Ellipse(x, y, 25, 46, 6, 16), Sdf.Ellipse(x, y, 71, 46, 6, 16));
            c = Over(c, Hex("2E3846"), Fill(flippers));
            float head = Circle(x, y, 48, 70, 15);
            c = Over(c, Hex("222A35"), Fill(head));
            c = Over(c, Color.white, Fill(Mathf.Min(Circle(x, y, 42, 74, 3.2f), Circle(x, y, 54, 74, 3.2f))));
            c = Over(c, Hex("FF9A2E"), Fill(Polygon(x, y, new[] { new Vector2(43, 82), new Vector2(53, 82), new Vector2(48, 92) })));
            c = Over(c, Outline, Stroke(Mathf.Min(body, Mathf.Min(head, flippers)), 3f));
            return c;
        }

        static Color SnowballPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 74, 56, 54), 8f));
            float ball = Circle(x, y, 64, 64, 54);
            c = Over(c, Hex("C9D9EA"), Fill(ball));
            c = Over(c, Hex("EEF4FA"), Fill(Circle(x, y, 56, 72, 44)) * Fill(ball + 2f));
            c = Over(c, Color.white, Fill(Circle(x, y, 46, 84, 16)) * Fill(ball + 4f));
            // Lumps of packed snow (they show it rolling).
            foreach (var (lx, ly, r) in new[] { (84f, 46f, 8f), (40f, 52f, 6f), (70f, 88f, 5f), (90f, 74f, 6f) })
                c = Over(c, Hex("B4C8DD"), Fill(Circle(x, y, lx, ly, r)) * Fill(ball + 3f) * 0.8f);
            c = Over(c, Outline, Stroke(ball, 4f));
            return c;
        }

        // A dash panel: a glowing ice-blue plate with bright chevrons pointing up (the way it boosts).
        static Color DashPanelPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float plate = RoundRect(x, y, 64, 64, 54, 58, 10);
            c = Over(c, new Color(0.35f, 0.8f, 1f, 1f), Gauss(Mathf.Max(0f, plate), 6f) * 0.6f);
            c = Over(c, Hex("1F6FB2"), Fill(plate));
            c = Over(c, Hex("2A93E8"), Fill(RoundRect(x, y, 64, 64, 46, 50, 8)));
            for (int k = 0; k < 3; k++)
            {
                float cy = 34f + k * 26f;
                float chevron = Mathf.Abs(Mathf.Abs(x - 64f) * 0.75f + (y - cy - 14f)) - 6f;
                if (Mathf.Abs(x - 64f) < 34f) c = Over(c, Color.Lerp(Hex("BFF0FF"), Color.white, k / 2f), Fill(chevron));
            }
            c = Over(c, Hex("0F3D66"), Stroke(plate, 4f));
            return c;
        }
    }
}
