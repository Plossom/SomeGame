using System;
using System.Collections.Generic;
using SomeGame.UI;
using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Paints the map screen's landscape into Assets/Art/Rally/MapBackground.png: warm sky and sun,
    /// hazy forest bands on the horizon, rolling meadows, a lake with a village, pine forests and the
    /// winding forest road of <see cref="MapLayout"/>, which disappears behind the trees on the horizon. Works in map canvas units (1170 x 2532, origin bottom
    /// left) and renders at <see cref="Scale"/> of that size.
    /// </summary>
    public static class MapArtGenerator
    {
        const float Scale = 2f / 3f;

        /// <summary>
        /// The landscape is painted in an unstretched space and stretched upward by this much when written
        /// out, so the land reaches higher up the screen (the horizon sits just below the header).
        /// <see cref="MapLayout"/>'s road is in the stretched (canvas) space.
        /// </summary>
        public const float Stretch = 1.15f;

        // The road in painting space.
        static Vector2 Unstretch(Vector2 canvas) => new(canvas.x, canvas.y / Stretch);
        static float RoadWidth(float paintY) => MapLayout.WidthAt(paintY * Stretch);

        static int _w, _h;
        static Color[] _px;

        static readonly Color Outline = Hex("1E3A30");

        [MenuItem("SomeGame/Generate Map Art")]
        public static void Generate()
        {
            _w = Mathf.RoundToInt(MapLayout.Size.x * Scale);
            _h = Mathf.RoundToInt(MapLayout.Size.y * Scale);
            _px = new Color[_w * _h];

            for (int py = 0; py < _h; py++)
                for (int px = 0; px < _w; px++)
                    _px[py * _w + px] = Landscape((px + 0.5f) / Scale, (py + 0.5f) / Scale / Stretch);

            Road();
            Objects();
            HorizonTrees();
            Sdf.WritePixels(ArtGenerator.Folder, "MapBackground", _w, _h, _px, 100, false, 0, compress: true);
            _px = null;
            Debug.Log("Map art generated");
        }

        // ================================================================== landscape layers

        // Rolling meadows from back to front: (top edge, colour).
        static float Hill(int i, float x) => i switch
        {
            0 => 1470f + 26f * Mathf.Sin(x / 60f) * Mathf.Sin(x / 23f) + 18f * Mathf.Sin(x / 140f + 1f),
            1 => 1405f + 45f * Mathf.Sin(x / 170f + 1f) + 18f * Mathf.Sin(x / 77f),
            2 => 1250f + 55f * Mathf.Sin(x / 210f + 2.2f) + 16f * Mathf.Sin(x / 90f),
            3 => 960f + 65f * Mathf.Sin(x / 260f + 0.5f) + 12f * Mathf.Sin(x / 70f + 2f),
            4 => 620f + 60f * Mathf.Sin(x / 240f + 3f),
            _ => 300f + 50f * Mathf.Sin(x / 200f + 1.4f),
        };

        static readonly Color[] HillColors =
        {
            Hex("4F7F57"), Hex("B9D39A"), Hex("A5C987"), Hex("8FBD76"), Hex("7BB066"), Hex("67A057"),
        };

        static readonly Vector2 LakeCenter = new(240f, 1110f);
        const float LakeRx = 210f, LakeRy = 62f;

        static Color Landscape(float x, float y)
        {
            // Sky: warm gradient and the sun.
            float t = Mathf.InverseLerp(1500f, 2532f, y);
            Color c = Color.Lerp(Hex("F8EBCF"), Hex("F3CF9C"), t * t);
            // A small sun low on the right, half behind the distant forest (clear of the header text).
            float sun = Circle(x, y, 1045f, 1585f, 46f);
            c = Over(c, Hex("FFE2AE"), Gauss(Mathf.Max(0f, sun), 90f) * 0.6f);
            c = Over(c, Hex("FFB347"), Cov(sun));
            c = Clouds(c, x, y);

            // Distant forest: two hazy bands of tree tops above the near forest.
            c = Over(c, Hex("B5CCA8"), Cov(y - (1610f + 22f * Mathf.Sin(x / 210f + 0.4f) + 9f * Mathf.Abs(Mathf.Sin(x / 13f)))));
            c = Over(c, Hex("8DB38D"), Cov(y - (1545f + 26f * Mathf.Sin(x / 160f + 2f) + 11f * Mathf.Abs(Mathf.Sin(x / 12f + 1f)))));

            for (int i = 0; i < HillColors.Length; i++)
            {
                float top = Hill(i, x);
                if (i == 0) top += 10f * Mathf.Abs(Mathf.Sin(x / 11f)); // tree tops on the forested foothills
                float d = y - top;
                if (d > 4f / Scale) continue;
                // Slightly lighter near the crest, darker toward the bottom.
                Color fill = Color.Lerp(HillColors[i], Shade(HillColors[i], 0.93f), Mathf.Clamp01(-d / 260f));
                c = Over(c, fill, Cov(d));
                if (i > 0) c = Over(c, Color.Lerp(HillColors[i], Color.white, 0.25f), Cov(Mathf.Abs(d + 5f) - 4f) * 0.8f);
            }

            // Lake with a sandy shore and glints.
            float lake = Sdf.Ellipse(x, y, LakeCenter.x, LakeCenter.y, LakeRx, LakeRy);
            c = Over(c, Hex("E8D9AE"), Cov(lake - 9f));
            Color water = Color.Lerp(Hex("5FA9B8"), Hex("8CCAD0"), Mathf.InverseLerp(LakeCenter.y - LakeRy, LakeCenter.y + LakeRy, y));
            c = Over(c, water, Cov(lake));
            float glint = Mathf.Min(Capsule(x, y, 150, 1120, 230, 1120, 3), Capsule(x, y, 260, 1090, 320, 1090, 3), Capsule(x, y, 190, 1070, 220, 1070, 3));
            c = Over(c, new Color(1f, 1f, 1f, 0.7f), Cov(glint) * Cov(lake + 6f));

            // Flowers sprinkled over the front meadow.
            if (y < 560f)
            {
                int cx = Mathf.FloorToInt(x / 26f), cy = Mathf.FloorToInt(y / 26f);
                float h = Hash(cx * 3 + 11, cy * 5 + 7);
                if (h > 0.72f)
                {
                    float fx = cx * 26f + 6f + Hash(cx, cy) * 14f, fy = cy * 26f + 6f + Hash(cy, cx) * 14f;
                    Color flower = h > 0.9f ? Hex("FFF6E4") : h > 0.81f ? Hex("F7D257") : Hex("F29BB0");
                    c = Over(c, flower, Cov(Circle(x, y, fx, fy, 3.2f)));
                }
            }
            return c;
        }

        static Color Clouds(Color c, float x, float y)
        {
            float Cloud(float cx, float cy, float s) =>
                Min(Circle(x, y, cx, cy, 40f * s), Circle(x, y, cx + 52f * s, cy + 22f * s, 54f * s),
                    Circle(x, y, cx + 112f * s, cy + 2f * s, 40f * s), RoundRect(x, y, cx + 56f * s, cy - 14f * s, 106f * s, 28f * s, 28f * s));
            float d = Min(Cloud(905f, 1945f, 0.42f), Cloud(60f, 2110f, 0.36f));
            return Over(c, Hex("FFF7E8"), Cov(d) * 0.95f);
        }

        static float Cov(float d) => Mathf.Clamp01(0.5f - d * Scale);

        // ================================================================== road

        static void Road()
        {
            var samples = new List<Vector2>();
            foreach (var p in MapLayout.Samples) samples.Add(Unstretch(p));
            // Densify, then stamp: dark edge, asphalt, dashed centre line.
            var points = new List<(Vector2 p, float dist)>();
            float acc = 0f;
            for (int i = 1; i < samples.Count; i++)
            {
                Vector2 a = samples[i - 1], b = samples[i];
                float len = Vector2.Distance(a, b);
                int n = Mathf.Max(1, Mathf.CeilToInt(len / 3f));
                for (int k = 0; k < n; k++) points.Add((Vector2.Lerp(a, b, k / (float)n), acc + len * k / n));
                acc += len;
            }
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f + 5f, Hex("4A4D4B"));
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f, Hex("606462"));
            foreach (var (p, dist) in points)
            {
                float w = RoadWidth(p.y);
                float period = Mathf.Max(18f, w * 0.75f);
                if (Mathf.Repeat(dist, period) < period * 0.5f) Disc(p, Mathf.Max(1.6f, w * 0.045f), new Color(1f, 0.98f, 0.92f, 0.85f));
            }
        }

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

        // True when an object standing at p (half width hw, height h, in canvas units) would touch the
        // road or the lake: checks its whole silhouette, not just where it stands.
        static bool Blocked(Vector2 p, float hw, float h)
        {
            for (int k = 0; k <= 4; k++)
            {
                var q = p + new Vector2(0f, h * k / 4f);
                float r = k == 0 ? hw * 0.6f : hw;
                if (RoadDistance(q) < RoadWidth(q.y) * 0.5f + 8f + r) return true;
                if (Sdf.Ellipse(q.x, q.y, LakeCenter.x, LakeCenter.y, LakeRx + 14f, LakeRy + 14f) < r) return true;
            }
            return false;
        }

        // Things get smaller further up the screen (further away).
        static float SizeAt(float y) => Mathf.Lerp(1.15f, 0.42f, Mathf.InverseLerp(200f, 1480f, y));

        static void Objects()
        {
            var items = new List<(float y, Action draw)>();
            var occupied = new List<(Vector2 p, float r)>();
            bool Free(Vector2 p, float r)
            {
                foreach (var (q, qr) in occupied) if ((q - p).sqrMagnitude < (r + qr) * (r + qr) * 0.55f) return false;
                return true;
            }

            // Stops on the road stay clear.
            for (int i = 0; i < MapLayout.StopCount; i++) occupied.Add((Unstretch(MapLayout.StopPosition(i)), 120f));

            // Village by the lake and a farm by the first race.
            var houses = new (Vector2 p, bool church)[]
            {
                (new(90, 1215), false), (new(200, 1236), false), (new(330, 1222), true), (new(90, 990), false),
                (new(190, 985), false), (new(560, 690), false), (new(1000, 560), false), (new(1080, 600), false),
                (new(980, 1290), false), (new(860, 1355), false), (new(470, 960), false),
            };
            foreach (var (p, church) in houses)
            {
                float s = SizeAt(p.y) * 1.6f;
                if (Blocked(p, 34f * s, 70f * s)) continue;
                occupied.Add((p + new Vector2(0, 30 * s), 70f * s));
                items.Add((p.y, () => { if (church) Church(p, s); else Chalet(p, s); }));
            }

            var rng = new System.Random(11);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // Pine forests in clusters on the meadows; a few round trees in between.
            for (int attempt = 0; attempt < 9000 && items.Count < 250; attempt++)
            {
                var p = new Vector2(Range(-20f, 1190f), Range(250f, 1470f));
                float cluster = Noise(p.x / 150f + 3f, p.y / 150f + 7f, 1000);
                if (cluster < 0.56f && rng.NextDouble() > 0.04) continue;
                float s = SizeAt(p.y) * Range(0.8f, 1.2f);
                float r = 34f * s;
                if (Blocked(p, 36f * s, 112f * s)) continue;
                // Keep trees on their hill (not floating above the crest of the hill behind).
                if (p.y > Hill(1, p.x) - 8f) continue;
                if (!Free(p, r)) continue;
                occupied.Add((p, r));
                bool pine = rng.NextDouble() < 0.78;
                items.Add((p.y, () => { if (pine) Pine(p, s); else RoundTree(p, s); }));
            }

            // Big framing trees in the foreground corners.
            foreach (var (p, s) in new[] { (new Vector2(-10, 430), 2.1f), (new Vector2(330, 40), 1.6f), (new Vector2(1150, 430), 2f), (new Vector2(1060, 330), 1.6f) })
                if (!Blocked(p, 36f * s, 112f * s)) items.Add((p.y, () => Pine(p, s)));

            items.Sort((a, b) => b.y.CompareTo(a.y)); // far (high) first
            foreach (var (_, draw) in items) draw();
        }

        // A row of small pines along the near forest edge.
        static void HorizonTrees()
        {
            var rng = new System.Random(5);
            var trees = new List<Vector2>();
            for (float x = -20f; x < 1200f; x += 24f + (float)rng.NextDouble() * 10f)
                trees.Add(new Vector2(x, Hill(0, x) - 26f - (float)rng.NextDouble() * 14f));
            trees.Sort((a, b) => b.y.CompareTo(a.y));
            foreach (var t in trees) Pine(t, 0.42f + (float)rng.NextDouble() * 0.08f);
        }

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

        static void RoundTree(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(10 * s, 2 * s), 30 * s, 8 * s, new Color(0.1f, 0.2f, 0.12f, 0.25f));
            Rect(p + new Vector2(0, 14 * s), 5f * s, 16 * s, Hex("6B4A33"));
            Vector2 c = p + new Vector2(0, 52 * s);
            Shape(Box(c, 40 * s), (x, y) => Circle(x, y, c.x, c.y, 32 * s), Hex("4E8E47"));
            Shape(Box(c, 40 * s), (x, y) => Mathf.Max(Circle(x, y, c.x - 9 * s, c.y + 9 * s, 24 * s), Circle(x, y, c.x, c.y, 32 * s)), Hex("6BAA55"));
            Shape(Box(c, 40 * s), (x, y) => Shell(Circle(x, y, c.x, c.y, 32 * s), 1.1f), Outline);
        }

        static void Chalet(Vector2 p, float s)
        {
            float w = 52 * s, wall = 34 * s, roofH = 30 * s;
            Ellipse(p + new Vector2(8 * s, 0), w * 1.1f, 7 * s, new Color(0.1f, 0.2f, 0.12f, 0.25f));
            // Stone base, wooden upper floor, white window frames.
            Rect(p + new Vector2(0, wall * 0.25f), w * 0.5f, wall * 0.25f, Hex("E9DEC6"));
            Rect(p + new Vector2(0, wall * 0.75f), w * 0.5f, wall * 0.25f, Hex("9A6440"));
            for (int k = -1; k <= 1; k += 2)
            {
                Rect(p + new Vector2(k * w * 0.22f, wall * 0.28f), 6 * s, 6 * s, Hex("FFFFFF"));
                Rect(p + new Vector2(k * w * 0.22f, wall * 0.28f), 4 * s, 4 * s, Hex("3A4A55"));
                Rect(p + new Vector2(k * w * 0.22f, wall * 0.74f), 5 * s, 5 * s, Hex("FFF1C8"));
            }
            Rect(p + new Vector2(0, wall * 0.52f), w * 0.5f, 1.6f * s, Hex("6E4429")); // balcony
            Vector2 l = new(p.x - w * 0.62f, p.y + wall), r = new(p.x + w * 0.62f, p.y + wall), top = new(p.x, p.y + wall + roofH);
            Poly(new[] { l, top, new Vector2(p.x, p.y + wall) }, Hex("C25E34"));
            Poly(new[] { new Vector2(p.x, p.y + wall), top, r }, Hex("9E4627"));
            Rect(p + new Vector2(w * 0.2f, wall + roofH * 0.7f), 4 * s, 8 * s, Hex("CFC6B4")); // chimney
            Poly(new[] { new Vector2(p.x - w * 0.5f, p.y), new Vector2(p.x + w * 0.5f, p.y), new Vector2(p.x + w * 0.5f, p.y + wall), r, top, l, new Vector2(p.x - w * 0.5f, p.y + wall) }, Outline, 2.4f);
        }

        static void Church(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(8 * s, 0), 50 * s, 7 * s, new Color(0.1f, 0.2f, 0.12f, 0.25f));
            // Nave.
            Rect(p + new Vector2(10 * s, 16 * s), 28 * s, 16 * s, Hex("FBF6EA"));
            Poly(new[] { new Vector2(p.x - 22 * s, p.y + 32 * s), new Vector2(p.x + 42 * s, p.y + 32 * s), new Vector2(p.x + 34 * s, p.y + 46 * s), new Vector2(p.x - 14 * s, p.y + 46 * s) }, Hex("9E4627"));
            // Tower with clock and spire.
            Vector2 t = p + new Vector2(-22 * s, 0);
            Rect(t + new Vector2(0, 30 * s), 12 * s, 30 * s, Hex("FFFFFF"));
            Shape(Box(t + new Vector2(0, 46 * s), 8 * s), (x, y) => Circle(x, y, t.x, t.y + 46 * s, 5 * s), Hex("C9A24A"));
            Poly(new[] { new Vector2(t.x - 14 * s, t.y + 60 * s), new Vector2(t.x + 14 * s, t.y + 60 * s), new Vector2(t.x, t.y + 102 * s) }, Hex("2F6A4A"));
            Poly(new[] { new Vector2(t.x - 12 * s, t.y), new Vector2(t.x + 12 * s, t.y), new Vector2(t.x + 12 * s, t.y + 60 * s), new Vector2(t.x, t.y + 102 * s), new Vector2(t.x - 12 * s, t.y + 60 * s) }, Outline, 2.4f);
            Poly(new[] { new Vector2(t.x + 12 * s, p.y), new Vector2(p.x + 38 * s, p.y), new Vector2(p.x + 38 * s, p.y + 32 * s), new Vector2(p.x + 34 * s, p.y + 46 * s), new Vector2(t.x + 12 * s, p.y + 46 * s) }, Outline, 2.4f);
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
