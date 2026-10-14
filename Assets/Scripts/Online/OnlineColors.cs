using UnityEngine;

namespace SomeGame.Online
{
    /// <summary>The eight car colours a player can pick in an online room.</summary>
    public static class OnlineColors
    {
        public static readonly Color[] All =
        {
            Hex("FF7A1A"), Hex("3D7DD8"), Hex("F2C230"), Hex("8A5CD6"), Hex("2BB3A3"), Hex("E86FB0"), Hex("F1F1EE"), Hex("9BD448"),
        };

        public static readonly string[] Names = { "ORANGE", "BLUE", "YELLOW", "PURPLE", "TEAL", "PINK", "WHITE", "GREEN" };

        public static Color Get(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
