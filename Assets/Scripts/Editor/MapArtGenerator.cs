using System;
using System.Collections.Generic;
using SomeGame.UI;
using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Paints the map screens' landscapes into Assets/Art/Rally: the Valley Cup's farming valley
    /// (MapArtGenerator.Valley) and the Winter Cup's mountains (MapArtGenerator.Winter). Works in map canvas units
    /// (1170 x 2532, origin bottom left) and renders at <see cref="Scale"/> of that size.
    /// </summary>
    public static partial class MapArtGenerator
    {
        const float Scale = 2f / 3f;

        /// <summary>
        /// The landscape is painted in an unstretched space and stretched upward by this much when written
        /// out, so the land reaches higher up the screen (the horizon sits just below the header).
        /// <see cref="MapLayout"/>'s road is in the stretched (canvas) space.
        /// </summary>
        public const float Stretch = MapScenery.Stretch;

        // The road in painting space.
        static Vector2 Unstretch(Vector2 canvas) => new(canvas.x, canvas.y / Stretch);
        static float RoadWidth(float paintY) => MapLayout.WidthAt(paintY * Stretch);

        static int _w, _h;
        static Color[] _px;

        static readonly Color Outline = Hex("1E3A30");

        [MenuItem("SomeGame/Generate Map Art")]
        public static void Generate()
        {
            MapLayout.Current = MapLayout.Valley;
            _w = Mathf.RoundToInt(MapLayout.Size.x * Scale);
            _h = Mathf.RoundToInt(MapLayout.Size.y * Scale);
            _px = new Color[_w * _h];

            for (int py = 0; py < _h; py++)
                for (int px = 0; px < _w; px++)
                    _px[py * _w + px] = ValleyLandscape((px + 0.5f) / Scale, (py + 0.5f) / Scale / Stretch);

            ValleyRoad();
            ValleyObjects();
            Sdf.WritePixels(ArtGenerator.Folder, "MapBackground", _w, _h, _px, 100, false, 0, compress: true);
            _px = null;
            Debug.Log("Map art generated");
        }

        static float Cov(float d) => Mathf.Clamp01(0.5f - d * Scale);

        static void Disc(Vector2 c, float r, Color color) =>
            Shape(new Rect(c.x - r - 2, c.y - r - 2, r * 2 + 4, r * 2 + 4), (x, y) => Circle(x, y, c.x, c.y, r), color);

        // ================================================================== trees, houses

        static float RoadDistance(Vector2 p)
        {
            float d = float.MaxValue;
            var s = MapLayout.Samples;
            for (int i = 1; i < s.Count; i++) d = Mathf.Min(d, SegDist(p, Unstretch(s[i - 1]), Unstretch(s[i])));
            return d;
        }

        // Things get smaller further up the screen (further away).
        static float SizeAt(float y) => MapScenery.SizeAt(y);

        static void Pine(Vector2 p, float s)
        {
            Color lit = Hex("3F7E59"), shade = Hex("2C6247");
            Ellipse(p + new Vector2(10 * s, 2 * s), 30 * s, 8 * s, new Color(0.1f, 0.2f, 0.12f, 0.25f));
            Rect(p + new Vector2(0, 6 * s), 4.5f * s, 9 * s, Hex("6B4A33"));
            for (int k = 0; k < 3; k++)
            {
                float baseY = p.y + (14 + k * 26) * s, hw = (34 - k * 8) * s, h = (46 - k * 6) * s;
                Vector2 a = new(p.x - hw, baseY), b = new(p.x + hw, baseY), top = new(p.x, baseY + h);
                Poly(new[] { a, new Vector2(p.x, baseY - 4 * s), top }, lit);
                Poly(new[] { new Vector2(p.x, baseY - 4 * s), b, top }, shade);
                Poly(new[] { a, new Vector2(p.x, baseY - 4 * s), b, top }, Outline, 2.2f);
            }
        }

        // ================================================================== stamping

        static Rect Box(Vector2 c, float r) => new(c.x - r, c.y - r, r * 2, r * 2);

        static void Rect(Vector2 center, float hw, float hh, Color color) =>
            Shape(new Rect(center.x - hw - 2, center.y - hh - 2, hw * 2 + 4, hh * 2 + 4), (x, y) => RoundRect(x, y, center.x, center.y, hw, hh, 0.5f), color);

        static void Ellipse(Vector2 c, float rx, float ry, Color color) =>
            Shape(new Rect(c.x - rx - 2, c.y - ry - 2, rx * 2 + 4, ry * 2 + 4), (x, y) => Sdf.Ellipse(x, y, c.x, c.y, rx, ry), color);

        static void Poly(Vector2[] pts, Color color, float strokeWidth = 0f)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (var q in pts) { xMin = Mathf.Min(xMin, q.x); yMin = Mathf.Min(yMin, q.y); xMax = Mathf.Max(xMax, q.x); yMax = Mathf.Max(yMax, q.y); }
            var box = UnityEngine.Rect.MinMaxRect(xMin - 4, yMin - 4, xMax + 4, yMax + 4);
            if (strokeWidth > 0f) Shape(box, (x, y) => Shell(Polygon(x, y, pts), strokeWidth * 0.5f), color);
            else Shape(box, (x, y) => Polygon(x, y, pts), color);
        }

        /// <summary>Composites a shape given by a signed distance in canvas units, within a canvas-space box.</summary>
        static void Shape(Rect box, Func<float, float, float> sdf, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(box.xMin * Scale)), x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(box.xMax * Scale));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(box.yMin * Scale * Stretch)), y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(box.yMax * Scale * Stretch));
            for (int py = y0; py <= y1; py++)
                for (int px = x0; px <= x1; px++)
                {
                    float cov = Cov(sdf((px + 0.5f) / Scale, (py + 0.5f) / Scale / Stretch));
                    if (cov <= 0f) continue;
                    int i = py * _w + px;
                    _px[i] = Over(_px[i], color, cov);
                }
        }
    }
}
