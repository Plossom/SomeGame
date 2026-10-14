using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// White UI icons for the online room (tinted in the UI, like the other icons): two people, copy,
    /// the host's crown, a check mark, a bot, plus, a die and a back arrow.
    /// </summary>
    public static class OnlineArt
    {
        static void Ui(string name, System.Func<float, float, float> coverage) =>
            Write(ArtGenerator.Folder, name, 128, 128, (x, y) => White(coverage(x, y)), 100, false);

        [MenuItem("SomeGame/Generate Online Icons")]
        public static void Generate()
        {
            // Two people: a head and shoulders in front, a second one behind it to the right.
            Ui("IconPeople", (x, y) =>
            {
                float front = Mathf.Min(Circle(x, y, 52, 80, 20), Mathf.Max(Ellipse(x, y, 52, 22, 38, 34), 22f - y));
                float back = Mathf.Min(Circle(x, y, 86, 88, 17), Mathf.Max(Ellipse(x, y, 88, 36, 32, 30), 36f - y));
                float gap = Mathf.Min(Circle(x, y, 52, 80, 28), Mathf.Max(Ellipse(x, y, 52, 22, 46, 42), 14f - y));
                return Mathf.Max(Fill(front), Fill(back) * Mathf.Clamp01(gap - 0.5f));
            });
            // Copy: two overlapping rounded sheets (the back one an outline).
            Ui("IconCopy", (x, y) => Mathf.Max(
                Fill(RoundRect(x, y, 54, 54, 28, 34, 7)),
                Fill(Shell(RoundRect(x, y, 78, 78, 26, 32, 7), 5f)) * Mathf.Clamp01(RoundRect(x, y, 54, 54, 36, 42, 11) - 0.5f)));
            // Crown: three points on a band.
            Ui("IconCrown", (x, y) => Fill(Min(
                Polygon(x, y, new[] { new Vector2(18, 40), new Vector2(110, 40), new Vector2(118, 98), new Vector2(88, 66), new Vector2(64, 108), new Vector2(40, 66), new Vector2(10, 98) }),
                RoundRect(x, y, 64, 30, 46, 10, 4), Circle(x, y, 10, 100, 9), Circle(x, y, 118, 100, 9), Circle(x, y, 64, 110, 10))));
            Ui("IconCheck", (x, y) => Fill(Min(Capsule(x, y, 26, 66, 52, 38, 11), Capsule(x, y, 52, 38, 104, 94, 11))));
            // A friendly bot: a rounded head with two eyes, an antenna and ears.
            Ui("IconBot", (x, y) =>
            {
                float head = RoundRect(x, y, 64, 56, 42, 34, 14);
                float eyes = Mathf.Min(Circle(x, y, 46, 60, 9), Circle(x, y, 82, 60, 9));
                float mouth = RoundRect(x, y, 64, 38, 16, 4, 3);
                float face = Mathf.Max(head, -Mathf.Min(eyes, mouth));
                float antenna = Mathf.Min(Capsule(x, y, 64, 90, 64, 106, 4), Circle(x, y, 64, 110, 8));
                float ears = Mathf.Min(RoundRect(x, y, 16, 56, 6, 14, 4), RoundRect(x, y, 112, 56, 6, 14, 4));
                return Fill(Min(face, antenna, ears));
            });
            Ui("IconPlus", (x, y) => Fill(Min(Capsule(x, y, 64, 26, 64, 102, 11), Capsule(x, y, 26, 64, 102, 64, 11))));
            // A die showing five.
            Ui("IconDice", (x, y) =>
            {
                float body = RoundRect(x, y, 64, 64, 46, 46, 14);
                float pips = Min(Circle(x, y, 40, 40, 8), Circle(x, y, 88, 40, 8), Circle(x, y, 64, 64, 8), Circle(x, y, 40, 88, 8), Circle(x, y, 88, 88, 8));
                return Fill(Mathf.Max(body, -pips));
            });
            GenerateMute();
            AssetDatabase.Refresh();
        }

        /// <summary>The sound icon's speaker, crossed out (volume all the way down).</summary>
        [MenuItem("SomeGame/Generate Mute Icon")]
        public static void GenerateMute()
        {
            Ui("IconMute", (x, y) => Fill(Min(
                RoundRect(x, y, 30, 64, 12, 16, 3),
                Triangle(x, y, new Vector2(36, 50), new Vector2(36, 78), new Vector2(68, 104)),
                Triangle(x, y, new Vector2(36, 50), new Vector2(68, 104), new Vector2(68, 24)),
                Capsule(x, y, 82, 48, 110, 80, 7), Capsule(x, y, 82, 80, 110, 48, 7))));
            AssetDatabase.Refresh();
        }
    }
}
