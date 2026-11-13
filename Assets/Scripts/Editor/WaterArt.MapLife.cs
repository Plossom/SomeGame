using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The moving life of the Water Cup's map (UI pictures, side views like the map's painting): a sailboat, a
    /// seaplane, a jet ski with its rider, a whale's tail.
    /// </summary>
    public static partial class WaterArt
    {
        [MenuItem("SomeGame/Generate Water Map Life")]
        public static void GenerateMapLife()
        {
            string f = ArtGenerator.Folder;
            Write(f, "MapSailboat", 128, 160, MapSailboatPixel, 100, false);
            Write(f, "MapSeaplane", 192, 96, MapSeaplanePixel, 100, false);
            Write(f, "MapJetski", 128, 80, MapJetskiPixel, 100, false);
            Write(f, "MapWhaleTail", 128, 96, MapWhaleTailPixel, 100, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color MapSailboatPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = Polygon(x, y, new[] { new Vector2(10, 30), new Vector2(118, 30), new Vector2(98, 8), new Vector2(26, 8) }) - 1f;
            float mast = RoundRect(x, y, 62, 92, 2.4f, 64, 1);
            float main = Triangle(x, y, new Vector2(66, 36), new Vector2(66, 152), new Vector2(116, 40));
            float jib = Triangle(x, y, new Vector2(58, 42), new Vector2(58, 128), new Vector2(18, 42));
            c = Over(c, Hex("FFFFFF"), Fill(main));
            c = Over(c, Hex("F2EEE2"), Fill(jib));
            c = Over(c, Hex("FF4F86"), Fill(Mathf.Max(main, 64f - y)));
            c = Over(c, Hex("6B4A2E"), Fill(mast));
            c = Over(c, Hex("FF6B52"), Fill(hull));
            c = Over(c, White(1f), Fill(RoundRect(x, y, 64, 22, 46, 2.2f, 1)) * Fill(hull));
            c = Over(c, Hex("12303F"), Stroke(Mathf.Min(hull, Mathf.Min(main, jib)), 2.5f));
            return Over(c, Hex("FFD24D"), Fill(Triangle(x, y, new Vector2(64, 156), new Vector2(64, 146), new Vector2(80, 151))));
        }

        static Color MapSeaplanePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Mathf.Min(RoundRect(x, y, 90, 50, 62, 13, 12), Triangle(x, y, new Vector2(24, 50), new Vector2(8, 82), new Vector2(40, 58)));
            float wing = RoundRect(x, y, 100, 66, 54, 5, 3);
            float floats = Mathf.Min(RoundRect(x, y, 80, 14, 36, 5, 4), Capsule(x, y, 70, 18, 74, 38, 2f));
            c = Over(c, Hex("2A2F36"), Fill(floats));
            c = Over(c, Hex("FFC93C"), Fill(body));
            c = Over(c, Hex("FF4F86"), Fill(wing));
            c = Over(c, Hex("8FDDFF"), Fill(RoundRect(x, y, 128, 56, 10, 6, 3)));
            c = Over(c, Hex("12303F"), Stroke(Mathf.Min(body, wing), 2.5f));
            float prop = RoundRect(x, y, 156, 50, 3, 22, 2);
            return Over(c, Hex("2A2F36"), Fill(prop));
        }

        static Color MapJetskiPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Spray behind, the hull, the rider leaning forward.
            foreach (var (sx, sy, r) in new[] { (14f, 22f, 8f), (24f, 30f, 6f), (8f, 34f, 5f), (30f, 18f, 5f) })
                c = Over(c, White(0.9f), Fill(Circle(x, y, sx, sy, r)));
            float hull = Polygon(x, y, new[] { new Vector2(30, 14), new Vector2(110, 14), new Vector2(122, 26), new Vector2(40, 30) }) - 1f;
            c = Over(c, Hex("2EC4B6"), Fill(hull));
            c = Over(c, Hex("12303F"), Stroke(hull, 2.2f));
            float rider = Mathf.Min(Capsule(x, y, 70, 30, 82, 54, 7f), Circle(x, y, 86, 62, 8f));
            c = Over(c, Hex("FF8A1E"), Fill(Capsule(x, y, 70, 30, 82, 54, 7f)));
            c = Over(c, Hex("F2C6A0"), Fill(Circle(x, y, 86, 62, 8f)));
            c = Over(c, Hex("F2C6A0"), Fill(Capsule(x, y, 82, 48, 100, 38, 3f)));
            return Over(c, Hex("12303F"), Stroke(rider, 2f));
        }

        static Color MapWhaleTailPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // The tail stock rising out of the water, the two flukes spread, water streaming off them.
            float stock = Polygon(x, y, new[] { new Vector2(54, 4), new Vector2(74, 4), new Vector2(70, 56), new Vector2(58, 56) });
            float flukes = Mathf.Min(Polygon(x, y, new[] { new Vector2(64, 50), new Vector2(10, 86), new Vector2(30, 92), new Vector2(64, 66) }),
                                     Polygon(x, y, new[] { new Vector2(64, 50), new Vector2(118, 86), new Vector2(98, 92), new Vector2(64, 66) })) - 2f;
            float all = Mathf.Min(stock, flukes);
            c = Over(c, Hex("3E5E7A"), Fill(all));
            c = Over(c, Hex("5C7E9A"), Fill(flukes + 3f) * Mathf.Clamp01((y - 70f) / 10f));
            c = Over(c, Hex("12303F"), Stroke(all, 2.5f));
            for (int i = 0; i < 5; i++)
                c = Over(c, White(0.85f), Fill(Capsule(x, y, 20 + i * 22, 82 - i % 2 * 6, 20 + i * 22, 60 - i % 2 * 6, 1.6f)));
            return c;
        }
    }
}
