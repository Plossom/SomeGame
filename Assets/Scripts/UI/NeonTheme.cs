using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>The Night Neon palette, shared by runtime UI code and the UI builder.</summary>
    public static class NeonTheme
    {
        public static readonly Color Background = Hex("101318");
        public static readonly Color Panel = Hex("171C23");
        public static readonly Color PanelRaised = Hex("1E252E");
        public static readonly Color Border = Hex("2F3945");
        public static readonly Color Text = Hex("EEF2F5");
        public static readonly Color Muted = Hex("B7C2CE");
        public static readonly Color Dim = Hex("7D8A99");
        public static readonly Color Faint = Hex("3A4552");
        public static readonly Color Lime = Hex("C8FF3D");
        public static readonly Color Magenta = Hex("FF3D7F");
        public static readonly Color Cyan = Hex("3DE0FF");
        public static readonly Color Orange = Hex("FF9F43");
        public static readonly Color Violet = Hex("A66BFF");

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static string Html(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
