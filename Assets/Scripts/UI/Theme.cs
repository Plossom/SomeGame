using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The Alpine Rally palette (warm cream, deep pine green, rally orange), shared by runtime UI code
    /// and the UI builder.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Cream = Hex("F4E6C8");
        public static readonly Color CreamDark = Hex("E3D3B0");
        public static readonly Color Ink = Hex("173D33");
        public static readonly Color InkSoft = Hex("2C5A4B");
        public static readonly Color Orange = Hex("FF7A1A");
        public static readonly Color OrangeDeep = Hex("B4510C");
        public static readonly Color Amber = Hex("FFB347");
        public static readonly Color Rust = Hex("C2530A");
        public static readonly Color Stone = Hex("D9D2BF");
        public static readonly Color StoneDark = Hex("8C8676");
        public static readonly Color Leaf = Hex("6A9F5B");
        public static readonly Color White = Color.white;

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
