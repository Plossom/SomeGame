using System.Collections.Generic;
using SomeGame.Race;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The UI palette, shared by runtime UI code and the UI builder. The Valley palette (warm cream,
    /// deep pine green, rally orange) is the default; a cup can bring its own (the Winter Cup: snow,
    /// deep navy, glacier blue). Every screen keeps the same layout, fonts and shapes: only the colours
    /// change. The UI is built with the Valley colours; <see cref="Recolor"/> swaps them in a scene.
    /// </summary>
    public static class Theme
    {
        public sealed class Palette
        {
            public Color Cream, CreamDark, Ink, InkSoft, Orange, OrangeDeep, Amber, Rust, Stone, StoneDark, Leaf;

            public Color[] All => new[] { Cream, CreamDark, Ink, InkSoft, Orange, OrangeDeep, Amber, Rust, Stone, StoneDark, Leaf };
        }

        public static readonly Palette Valley = new()
        {
            Cream = Hex("F4E6C8"), CreamDark = Hex("E3D3B0"), Ink = Hex("173D33"), InkSoft = Hex("2C5A4B"),
            Orange = Hex("FF7A1A"), OrangeDeep = Hex("B4510C"), Amber = Hex("FFB347"), Rust = Hex("C2530A"),
            Stone = Hex("D9D2BF"), StoneDark = Hex("8C8676"), Leaf = Hex("6A9F5B"),
        };

        /// <summary>Snow white, deep navy plates, glacier blue buttons and icy highlights.</summary>
        public static readonly Palette Winter = new()
        {
            Cream = Hex("EAF2F9"), CreamDark = Hex("C9DCEC"), Ink = Hex("16304F"), InkSoft = Hex("2B4F78"),
            Orange = Hex("2A93E8"), OrangeDeep = Hex("1A5A9A"), Amber = Hex("8FDDFF"), Rust = Hex("1F64AE"),
            Stone = Hex("CCD8E4"), StoneDark = Hex("7B8CA1"), Leaf = Hex("6FA7B8"),
        };

        /// <summary>Greys for a locked or coming-soon cup's page: no colour until it is earned.</summary>
        public static readonly Palette Locked = new()
        {
            Cream = Hex("E9EAE8"), CreamDark = Hex("CDCFCC"), Ink = Hex("3A3D3C"), InkSoft = Hex("55595A"),
            Orange = Hex("75797A"), OrangeDeep = Hex("4C504F"), Amber = Hex("D3D6D3"), Rust = Hex("2F3231"),
            Stone = Hex("C4C7C4"), StoneDark = Hex("686D6B"), Leaf = Hex("9A9F9B"),
        };

        public static Palette Of(CupLook look) => look == CupLook.Winter ? Winter : Valley;

        public static Palette Current { get; private set; } = Valley;

        public static void Use(Palette palette) => Current = palette ?? Valley;

        // Statics survive entering play mode in the Editor (no domain reload): start each run with the Valley colours.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Current = Valley;

        public static Color Cream => Current.Cream;
        public static Color CreamDark => Current.CreamDark;
        public static Color Ink => Current.Ink;
        public static Color InkSoft => Current.InkSoft;
        public static Color Orange => Current.Orange;
        public static Color OrangeDeep => Current.OrangeDeep;
        public static Color Amber => Current.Amber;
        public static Color Rust => Current.Rust;
        public static Color Stone => Current.Stone;
        public static Color StoneDark => Current.StoneDark;
        public static Color Leaf => Current.Leaf;
        public static readonly Color White = Color.white;

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static string Html(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        /// <summary>
        /// Swaps the colours of every image and text under <paramref name="root"/> from one palette to the
        /// current one: each palette colour (and its darker shadow shades) maps to its counterpart, alpha kept.
        /// </summary>
        public static void Recolor(Transform root, Palette from)
        {
            if (from == Current) return;
            Color[] a = from.All, b = Current.All;
            foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (TryMap(graphic.color, a, b, out var mapped)) graphic.color = mapped;
        }

        public static bool TryMap(Color c, Palette from, out Color mapped) => TryMap(c, from.All, Current.All, out mapped);

        public static bool TryMap(Color c, Palette from, Palette to, out Color mapped) => TryMap(c, from.All, to.All, out mapped);

        /// <summary>
        /// Every image and text under <paramref name="root"/> whose colour belongs to palette
        /// <paramref name="from"/>, with its colour now and its counterpart in <paramref name="to"/>,
        /// for a colour morph from one cup to another.
        /// </summary>
        public static List<(UnityEngine.UI.Graphic graphic, Color from, Color to)> RecolorPlan(Transform root, Palette from, Palette to)
        {
            var plan = new List<(UnityEngine.UI.Graphic, Color, Color)>();
            if (from == to) return plan;
            Color[] a = from.All, b = to.All;
            foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (TryMap(graphic.color, a, b, out var mapped)) plan.Add((graphic, graphic.color, mapped));
            return plan;
        }

        static readonly float[] Shades = { 1f, 0.7f, 0.6f };

        static bool TryMap(Color c, Color[] from, Color[] to, out Color mapped)
        {
            for (int i = 0; i < from.Length; i++)
                foreach (float k in Shades)
                {
                    Color f = from[i] * k;
                    if (Mathf.Abs(c.r - f.r) + Mathf.Abs(c.g - f.g) + Mathf.Abs(c.b - f.b) > 0.012f) continue;
                    mapped = to[i] * k;
                    mapped.a = c.a;
                    return true;
                }
            mapped = c;
            return false;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
