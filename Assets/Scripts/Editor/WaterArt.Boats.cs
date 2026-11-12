using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Water Cup's boats, seen from above like the buggy and on its 192 x 336 canvas (bow up), each as a hull
    /// tinted with the paint plus untinted details: the SPEEDBOAT (a pointed hull, a windscreen, the driver in
    /// shades and a cap, a big outboard), the JET SKI (narrow, the rider astride, the jet nozzle behind) and the
    /// RUBBER DUCK (a giant duck with an outboard and its captain on its back). Also the boats' soft shadow, and
    /// the tops and decals of the Water Cup's shop (laid over the hull).
    /// </summary>
    public static partial class WaterArt
    {
        static readonly Color Line = Hex("12303F");
        static readonly Color Skin = Hex("F2C6A0");
        static readonly Color Cushion = Hex("F6F1E4"), CushionShade = Hex("D9D1BE");
        static readonly Color Motor = Hex("2A2F36"), MotorLight = Hex("4A535E");
        static readonly Color Glass = new(0.6f, 0.86f, 1f, 0.85f);

        [MenuItem("SomeGame/Generate Water Cup Boats")]
        public static void GenerateBoats()
        {
            string f = ArtGenerator.Folder;
            Write(f, "BoatSpeed", 192, 336, SpeedHull, 192, false);
            Write(f, "BoatSpeedDetails", 192, 336, SpeedDetails, 192, false);
            Write(f, "BoatJetski", 192, 336, JetHull, 192, false);
            Write(f, "BoatJetskiDetails", 192, 336, JetDetails, 192, false);
            Write(f, "BoatDuck", 192, 336, DuckHull, 192, false);
            Write(f, "BoatDuckDetails", 192, 336, DuckDetails, 192, false);
            Write(f, "BoatShadow", 192, 336, (x, y) =>
            {
                float d = Polygon(x, y, new[] { new Vector2(34, 40), new Vector2(158, 40), new Vector2(160, 200), new Vector2(96, 330), new Vector2(32, 200) }) - 14f;
                return new Color(0f, 0.08f, 0.14f, 0.45f * Mathf.Clamp01(0.5f - d / 16f));
            }, 192, false);
            GenerateBoatParts();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ speedboat

        // A pointed hull: square stern, widest a third of the way up, a sharp bow.
        static float SpeedHullSdf(float x, float y) =>
            Polygon(x, y, new[] { new Vector2(36, 46), new Vector2(156, 46), new Vector2(162, 150), new Vector2(150, 232), new Vector2(96, 326), new Vector2(42, 232), new Vector2(30, 150) }) - 6f;

        static Color SpeedHull(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = SpeedHullSdf(x, y);
            // Rounded sides: lighter down the middle, darker at the chines; a crease down the bow.
            float v = Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(-hull / 16f)) - Mathf.Clamp01((x - 120f) / 70f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(hull));
            c = Over(c, new Color(0.97f, 0.97f, 0.97f), Fill(Capsule(x, y, 96, 240, 96, 312, 2.2f)) * Fill(hull + 3f));
            // The cockpit well, a shade deeper.
            float well = RoundRect(x, y, 96, 142, 50, 70, 26);
            c = Over(c, new Color(0.72f, 0.72f, 0.72f), Fill(well));
            c = Over(c, Line, Stroke(well, 2.5f));
            return Over(c, Line, Stroke(hull, 4.5f));
        }

        static Color SpeedDetails(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // A white stripe along the gunwale.
            float hull = SpeedHullSdf(x, y);
            c = Over(c, White(0.9f), Fill(Shell(hull + 7f, 2.2f)) * Mathf.Clamp01((y - 160f) / 30f));
            // Teak deck at the bow with a cleat.
            float deck = Polygon(x, y, new[] { new Vector2(78, 270), new Vector2(114, 270), new Vector2(96, 306) }) - 5f;
            c = Over(c, Mathf.Repeat(x, 9f) < 1.6f ? Hex("8A5A32") : Hex("B9814D"), Fill(deck));
            c = Over(c, Line, Stroke(deck, 2f));
            c = Over(c, Hex("C9D3DC"), Fill(RoundRect(x, y, 96, 290, 6, 2.6f, 1.5f)));
            // Seats: the driver's and a bench at the back.
            foreach (var (sx, sy, hw, hh) in new[] { (74f, 168f, 18f, 16f), (118f, 168f, 18f, 16f), (96f, 98f, 42f, 16f) })
            {
                float seat = RoundRect(x, y, sx, sy, hw, hh, 7);
                c = Over(c, Cushion, Fill(seat));
                c = Over(c, CushionShade, Fill(RoundRect(x, y, sx, sy - hh * 0.4f, hw - 3, hh * 0.35f, 4)));
                c = Over(c, Line, Stroke(seat, 2.2f));
            }
            // Windscreen: a curved band of glass in a chrome frame.
            float screen = Mathf.Max(Shell(Circle(x, y, 96, 150, 64), 6f), 196f - y);
            screen = Mathf.Max(screen, Mathf.Abs(x - 96f) - 56f);
            c = Over(c, Glass, Fill(screen));
            c = Over(c, White(0.8f), Fill(Capsule(x, y, 70, 207, 84, 211, 1.8f)));
            c = Over(c, Hex("C9D3DC"), Stroke(screen, 2.5f));
            // The driver: shoulders, arms to the wheel, a red cap and shades.
            float arms = Mathf.Min(Capsule(x, y, 66, 168, 66, 194, 6.5f), Capsule(x, y, 84, 172, 76, 192, 6f));
            c = Over(c, Skin, Fill(arms));
            c = Over(c, Line, Stroke(arms, 2f));
            float wheel = Shell(Circle(x, y, 72, 196, 8), 2f);
            c = Over(c, Motor, Fill(wheel));
            float shoulders = Ellipse(x, y, 74, 166, 18, 11);
            c = Over(c, Hex("3FB8C8"), Fill(shoulders));
            c = Over(c, Line, Stroke(shoulders, 2f));
            float head = Circle(x, y, 74, 170, 11);
            c = Over(c, Skin, Fill(head));
            float cap = Mathf.Min(Circle(x, y, 74, 168, 10.5f), Ellipse(x, y, 74, 180, 8, 5));
            c = Over(c, Hex("E5413A"), Fill(cap));
            c = Over(c, Line, Stroke(head, 2f));
            // The outboard behind the stern: a dark cowl with a light top.
            float motor = RoundRect(x, y, 96, 30, 22, 20, 8);
            c = Over(c, Motor, Fill(motor));
            c = Over(c, MotorLight, Fill(RoundRect(x, y, 96, 34, 14, 12, 5)));
            c = Over(c, White(0.85f), Fill(RoundRect(x, y, 96, 22, 10, 2.2f, 1)));
            c = Over(c, Line, Stroke(motor, 2.5f));
            return c;
        }

        // ------------------------------------------------------------------ jet ski

        static float JetHullSdf(float x, float y) =>
            Polygon(x, y, new[] { new Vector2(58, 60), new Vector2(134, 60), new Vector2(142, 170), new Vector2(128, 250), new Vector2(96, 310), new Vector2(64, 250), new Vector2(50, 170) }) - 8f;

        static Color JetHull(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = JetHullSdf(x, y);
            float v = Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(-hull / 14f)) - Mathf.Clamp01((x - 110f) / 50f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(hull));
            // The nose cowl, raised: a lighter shape.
            c = Over(c, new Color(0.97f, 0.97f, 0.97f), Fill(Polygon(x, y, new[] { new Vector2(76, 232), new Vector2(116, 232), new Vector2(96, 296) }) - 4f));
            return Over(c, Line, Stroke(hull, 4.5f));
        }

        static Color JetDetails(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Footwells either side of the seat, the seat down the middle.
            float wells = Mathf.Min(RoundRect(x, y, 66, 140, 7, 56, 5), RoundRect(x, y, 126, 140, 7, 56, 5));
            c = Over(c, Mathf.Repeat(y, 7f) < 2.5f ? Hex("2A2F36") : Hex("3A424C"), Fill(wells));
            float seat = RoundRect(x, y, 96, 132, 20, 62, 14);
            c = Over(c, Hex("2A2F36"), Fill(seat));
            c = Over(c, Hex("4A535E"), Fill(RoundRect(x, y, 92, 132, 8, 54, 6)));
            c = Over(c, Line, Stroke(seat, 2.2f));
            // Handlebar and the rider astride: shoulders in a life vest, arms forward, a helmet.
            float bar = Capsule(x, y, 66, 212, 126, 212, 3.4f);
            c = Over(c, Motor, Fill(bar));
            float arms = Mathf.Min(Capsule(x, y, 78, 170, 68, 208, 6.5f), Capsule(x, y, 114, 170, 124, 208, 6.5f));
            c = Over(c, Skin, Fill(arms));
            c = Over(c, Line, Stroke(arms, 2f));
            float vest = Ellipse(x, y, 96, 166, 24, 15);
            c = Over(c, Hex("FF8A1E"), Fill(vest));
            c = Over(c, White(0.85f), Fill(RoundRect(x, y, 96, 166, 22, 2.4f, 1)) * Fill(vest));
            c = Over(c, Line, Stroke(vest, 2f));
            float helmet = Circle(x, y, 96, 174, 13);
            c = Over(c, White(1f), Fill(helmet));
            c = Over(c, Hex("1D2733"), Fill(Mathf.Max(Circle(x, y, 96, 174, 11), 179f - y)));
            c = Over(c, Line, Stroke(helmet, 2f));
            // The jet nozzle at the stern.
            float nozzle = RoundRect(x, y, 96, 52, 12, 12, 5);
            c = Over(c, Motor, Fill(nozzle));
            c = Over(c, Hex("12171C"), Fill(Circle(x, y, 96, 46, 5)));
            return Over(c, Line, Stroke(nozzle, 2f));
        }

        // ------------------------------------------------------------------ rubber duck

        static float DuckBodySdf(float x, float y) => Mathf.Min(Ellipse(x, y, 96, 140, 64, 92), Triangle(x, y, new Vector2(70, 70), new Vector2(122, 70), new Vector2(96, 36)) - 6f);
        static float DuckHeadSdf(float x, float y) => Circle(x, y, 96, 248, 42);

        static Color DuckHull(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = DuckBodySdf(x, y);
            float v = Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(-body / 18f)) - Mathf.Clamp01((x - 120f) / 60f) * 0.08f;
            c = Over(c, new Color(v, v, v), Fill(body));
            // Wings folded at the sides.
            foreach (float s in new[] { -1f, 1f })
            {
                float wing = Ellipse(x, y, 96 + s * 44, 132, 16, 46);
                c = Over(c, new Color(0.86f, 0.86f, 0.86f), Fill(wing) * Fill(body + 2f));
                c = Over(c, Line, Stroke(wing, 2f) * Fill(body + 2f));
            }
            c = Over(c, Line, Stroke(body, 4.5f));
            float head = DuckHeadSdf(x, y);
            float hv = Mathf.Lerp(0.84f, 1f, Mathf.Clamp01(-head / 16f));
            c = Over(c, new Color(hv, hv, hv), Fill(head));
            return Over(c, Line, Stroke(head, 4.5f));
        }

        static Color DuckDetails(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Beak and eyes (the duck looks ahead), a shine on the head.
            // A wide flat bill sticking out over the front.
            float beak = Mathf.Min(Ellipse(x, y, 96, 300, 26, 13), RoundRect(x, y, 96, 290, 20, 8, 6));
            c = Over(c, Hex("FF8A1E"), Fill(beak));
            c = Over(c, Hex("D4600C"), Fill(Capsule(x, y, 76, 300, 116, 300, 1.4f)) * Fill(beak));
            c = Over(c, Hex("D4600C"), Fill(Mathf.Min(Circle(x, y, 90, 306, 2f), Circle(x, y, 102, 306, 2f))));
            c = Over(c, Line, Stroke(beak, 2.2f));
            foreach (float s in new[] { -1f, 1f })
            {
                c = Over(c, Color.white, Fill(Circle(x, y, 96 + s * 20, 262, 8)));
                c = Over(c, Line, Fill(Circle(x, y, 96 + s * 20, 264, 4.5f)));
            }
            c = Over(c, White(0.6f), Fill(Ellipse(x, y, 80, 236, 10, 6)));
            // The captain on its back: a sailor's hat, a striped shirt, hands on the tiller.
            float arms = Mathf.Min(Capsule(x, y, 82, 116, 92, 82, 6f), Capsule(x, y, 110, 116, 100, 82, 6f));
            c = Over(c, Skin, Fill(arms));
            c = Over(c, Line, Stroke(arms, 2f));
            float shirt = Ellipse(x, y, 96, 124, 22, 14);
            c = Over(c, Mathf.Repeat(y, 8f) < 4f ? Hex("1B4E7A") : Color.white, Fill(shirt));
            c = Over(c, Line, Stroke(shirt, 2f));
            float head = Circle(x, y, 96, 130, 11);
            c = Over(c, Skin, Fill(head));
            float hat = Circle(x, y, 96, 131, 10);
            c = Over(c, Color.white, Fill(hat));
            c = Over(c, Hex("1B4E7A"), Fill(Shell(Circle(x, y, 96, 131, 10), 1.6f)));
            c = Over(c, Line, Stroke(head, 2f));
            // The little outboard behind the tail and its tiller.
            c = Over(c, Motor, Fill(Capsule(x, y, 96, 80, 96, 50, 3f)));
            float motor = RoundRect(x, y, 96, 30, 16, 14, 6);
            c = Over(c, Motor, Fill(motor));
            c = Over(c, MotorLight, Fill(RoundRect(x, y, 96, 33, 10, 8, 4)));
            return Over(c, Line, Stroke(motor, 2.2f));
        }
    }
}
