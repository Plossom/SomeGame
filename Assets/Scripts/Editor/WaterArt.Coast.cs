using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Lighthouse Run's art (Water Cup race 3), seen from above: the wreck you sail through (its broken hull open to the sea
    /// in the middle) and the dark wet rocks that the tide covers and uncovers.
    /// </summary>
    public static partial class WaterArt
    {
        [MenuItem("SomeGame/Generate Coast Art")]
        public static void GenerateCoast()
        {
            string f = ArtGenerator.Folder;
            Write(f, "Wreck", 320, 768, WreckPixel, 28, false);
            Write(f, "CoastRock", 128, 128, CoastRockPixel, 64, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // A wooden hull lying along the channel, bow up: planked sides (broken through at the bow and the stern, where the
        // boats sail in and out), ribs across the flooded middle, a fallen mast along one side, rigging and a barrel.
        static Color WreckPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color wood = Hex("6B4A2E"), woodDark = Hex("4A321F"), woodLight = Hex("8C6A45");
            float outer = Mathf.Min(Ellipse(x, y, 160, 360, 138, 330), RoundRect(x, y, 160, 300, 138, 260, 60));
            float inner = Mathf.Min(Ellipse(x, y, 160, 360, 112, 300), RoundRect(x, y, 160, 300, 112, 236, 44));
            float hull = Mathf.Max(outer, -inner);
            // Broken through at both ends: gaps in the planking.
            float breach = Mathf.Min(RoundRect(x, y, 160, 690, 70, 70, 20), RoundRect(x, y, 160, 30, 70, 60, 20));
            hull = Mathf.Max(hull, -breach);
            float planks = Mathf.Repeat(y, 22f) < 2.5f ? 0.8f : 1f;
            c = Over(c, Shade(wood, planks), Fill(hull));
            c = Over(c, woodLight, Fill(Shell(hull + 9f, 3f)) * Fill(hull));
            // The ribs across the flooded middle, dark and half under water.
            for (int i = 0; i < 9; i++)
            {
                float ry = 140 + i * 56;
                float rib = RoundRect(x, y, 160, ry, 112, 5, 3);
                float under = 0.45f + 0.35f * Hash(i, 11);
                c = Over(c, new Color(woodDark.r, woodDark.g, woodDark.b, under), Fill(rib) * Fill(inner));
            }
            // The fallen mast along the left side, with torn rigging.
            float mast = Capsule(x, y, 70, 160, 92, 600, 9);
            c = Over(c, woodLight, Fill(mast));
            c = Over(c, Line, Stroke(mast, 2.5f));
            for (int i = 0; i < 4; i++)
                c = Over(c, new Color(0.85f, 0.82f, 0.72f, 0.8f), Stroke(SegDist(new Vector2(x, y), new Vector2(76 + i * 5, 220 + i * 100), new Vector2(150 + Hash(i, 3) * 60, 260 + i * 100)), 2f));
            float barrel = Circle(x, y, 236, 470, 14);
            c = Over(c, Hex("9A6B3A"), Fill(barrel));
            c = Over(c, Line, Stroke(barrel, 2.5f));
            c = Over(c, Line, Stroke(Circle(x, y, 236, 470, 8), 2f));
            return Over(c, Line, Stroke(hull, 4f) * (Fill(outer + 3f)));
        }

        // A dark wet rock in the sea: rounded boulders, a wet sheen, foam round its foot.
        static Color CoastRockPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float rock = SMin(SMin(Circle(x, y, 60, 58, 34), Circle(x, y, 82, 76, 24), 10f), Circle(x, y, 42, 80, 20), 10f);
            c = Over(c, White(0.9f), Fill(rock - 7f) * (1f - Fill(rock)));
            float shade = Mathf.Clamp01(0.5f + (x - y) / 160f);
            c = Over(c, Color.Lerp(Hex("2B3036"), Hex("4B535C"), shade), Fill(rock));
            c = Over(c, new Color(0.75f, 0.85f, 0.95f, 0.45f), Fill(Ellipse(x, y, 54, 70, 10, 5)) * Fill(rock));
            c = Over(c, new Color(0.75f, 0.85f, 0.95f, 0.35f), Fill(Ellipse(x, y, 82, 84, 7, 3)) * Fill(rock));
            return Over(c, Hex("15191E"), Stroke(rock, 3f));
        }
    }
}
