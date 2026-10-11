using System;
using System.Collections.Generic;
using SomeGame.UI;
using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Winter Cup's landscape (Assets/Art/Rally/MapWinter.png): a cold blue sky with a pale sun,
    /// two ranges of snowy peaks, rolling snowfields, a frozen lake, a village of log cabins with lit
    /// windows, a ski lift, snowy pines, snowmen, and the mountain road of <see cref="MapLayout.Winter"/>
    /// climbing in switchbacks between snow banks and red marker poles.
    /// Same canvas, scale and stretch as the valley picture.
    /// </summary>
    public static partial class MapArtGenerator
    {
        static readonly Color Frost = Hex("1B2F45"); // outlines
        static readonly Color SnowWhite = Hex("F8FBFE");
        static readonly Color SnowShade = Hex("C9D9EA");

        static readonly Vector2 IceCenter = new(290f, 478f);
        const float IceRx = 205f, IceRy = 60f;

        [MenuItem("SomeGame/Generate Winter Map Art")]
        public static void GenerateWinter()
        {
            var previous = MapLayout.Current;
            MapLayout.Current = MapLayout.Winter;
            try
            {
                _w = Mathf.RoundToInt(MapLayout.Size.x * Scale);
                _h = Mathf.RoundToInt(MapLayout.Size.y * Scale);
                _px = new Color[_w * _h];
                for (int py = 0; py < _h; py++)
                    for (int px = 0; px < _w; px++)
                        _px[py * _w + px] = WinterLandscape((px + 0.5f) / Scale, (py + 0.5f) / Scale / Stretch);

                WinterRoad();
                WinterObjects();
                WinterHorizonTrees();
                Sdf.WritePixels(ArtGenerator.Folder, "MapWinter", _w, _h, _px, 100, false, 0, compress: true);
                _px = null;
                Debug.Log("Winter map art generated");
            }
            finally
            {
                MapLayout.Current = previous;
            }
        }

        // ================================================================== landscape layers

        // Peaks (x, top) of the far and the near range.
        static readonly Vector2[] FarPeaks = { new(40f, 1690f), new(250f, 1625f), new(470f, 1760f), new(700f, 1650f), new(890f, 1735f), new(1110f, 1660f) };
        static readonly Vector2[] NearPeaks = { new(140f, 1540f), new(400f, 1505f), new(640f, 1590f), new(910f, 1515f), new(1130f, 1565f) };

        // Height of a mountain range at x, and which side of its peak x is on (for the shading).
        static float Ridge(Vector2[] peaks, float x, float slope, out float side, out float top)
        {
            float best = float.MinValue;
            side = 0f; top = 0f;
            foreach (var p in peaks)
            {
                float h = p.y - Mathf.Abs(x - p.x) * slope;
                if (h <= best) continue;
                best = h; side = Mathf.Sign(x - p.x); top = p.y;
            }
            // Rocky jaggedness.
            return best + 7f * Mathf.Sin(x / 17f) * Mathf.Sin(x / 41f) + 4f * Mathf.Sin(x / 7f);
        }

        static Color Mountain(Color c, float x, float y, Vector2[] peaks, float slope, Color lit, Color shade, float snowDepth)
        {
            float ridge = Ridge(peaks, x, slope, out float side, out float top);
            float d = y - ridge;
            if (d > 3f) return c;
            Color rock = side > 0f ? shade : lit;
            // Snow caps with a ragged lower edge; the shady side in blue.
            float snowLine = top - snowDepth + 18f * Mathf.Sin(x / 23f) + 10f * Mathf.Sin(x / 9f);
            if (y > snowLine) rock = side > 0f ? Hex("D3E1EF") : SnowWhite;
            c = Over(c, rock, Cov(d));
            return Over(c, Color.Lerp(rock, Frost, 0.35f), Cov(Mathf.Abs(d) - 1.5f) * 0.5f);
        }

        static float SnowHill(int i, float x) => i switch
        {
            0 => 1405f + 18f * Mathf.Sin(x / 130f + 0.6f) + 8f * Mathf.Sin(x / 47f),
            1 => 1180f + 50f * Mathf.Sin(x / 230f + 1f) + 14f * Mathf.Sin(x / 83f),
            2 => 905f + 60f * Mathf.Sin(x / 260f + 2.5f) + 12f * Mathf.Sin(x / 95f),
            3 => 620f + 55f * Mathf.Sin(x / 210f + 0.7f) + 10f * Mathf.Sin(x / 66f + 1f),
            _ => 330f + 45f * Mathf.Sin(x / 190f + 2f),
        };

        static readonly Color[] SnowHillColors = { Hex("DCE7F2"), Hex("EAF1F8"), Hex("E1EBF5"), Hex("EDF3FA"), Hex("E5EEF7") };

        static Color WinterLandscape(float x, float y)
        {
            // Sky: a clear cold blue, paler at the horizon, and a low pale sun on the left.
            float t = Mathf.InverseLerp(1450f, 2202f, y);
            Color c = Color.Lerp(Hex("E3EEF7"), Hex("8DBBE3"), Mathf.SmoothStep(0f, 1f, t));
            float sun = Circle(x, y, 130f, 1640f, 40f);
            c = Over(c, Hex("FFF7E6"), Gauss(Mathf.Max(0f, sun), 110f) * 0.55f);
            c = Over(c, Hex("FFF4DA"), Cov(sun));
            c = WinterClouds(c, x, y);

            // Two mountain ranges, far (hazy) and near.
            c = Mountain(c, x, y, FarPeaks, 1.05f, Hex("B8CCE1"), Hex("A0B7D0"), 95f);
            c = Mountain(c, x, y, NearPeaks, 0.95f, Hex("8EA8C5"), Hex("7590B0"), 70f);

            // Dark forest band at the foot of the mountains, dusted with snow.
            float forest = 1440f + 16f * Mathf.Sin(x / 150f + 2f) + 12f * Mathf.Abs(Mathf.Sin(x / 12f + 1f));
            c = Over(c, Hex("5B7A8C"), Cov(y - forest));
            if (y < forest && y > forest - 26f && Hash((int)(x / 5f), (int)(y / 5f)) > 0.55f) c = Over(c, Hex("E8F0F8"), 0.75f);

            // Snowfields, back to front: blue shade low in each field, a bright crest line on top.
            for (int i = 0; i < SnowHillColors.Length; i++)
            {
                float top = SnowHill(i, x);
                float d = y - top;
                if (d > 4f / Scale) continue;
                Color fill = Color.Lerp(SnowHillColors[i], Hex("C6D7E9"), Mathf.Clamp01(-d / 320f) * 0.55f);
                c = Over(c, fill, Cov(d));
                c = Over(c, Color.white, Cov(Mathf.Abs(d + 5f) - 4f) * 0.9f);
                c = Over(c, Hex("B4C9DF"), Cov(Mathf.Abs(d + 1f) - 1.2f) * 0.5f);
            }

            // The frozen lake: snowy rim, pale ice with cracks and glints.
            float ice = Sdf.Ellipse(x, y, IceCenter.x, IceCenter.y, IceRx, IceRy);
            c = Over(c, Hex("A9C4DD"), Cov(ice - 10f));
            c = Over(c, SnowWhite, Cov(ice - 7f));
            Color iceColor = Color.Lerp(Hex("A9D6EE"), Hex("DDF1FA"), Mathf.InverseLerp(IceCenter.y - IceRy, IceCenter.y + IceRy, y));
            c = Over(c, iceColor, Cov(ice));
            float cracks = Min(
                Capsule(x, y, 410f, 470f, 330f, 500f, 1.4f), Capsule(x, y, 330f, 500f, 270f, 478f, 1.4f), Capsule(x, y, 330f, 500f, 310f, 528f, 1.4f),
                Capsule(x, y, 210f, 450f, 160f, 470f, 1.4f), Capsule(x, y, 160f, 470f, 130f, 455f, 1.4f));
            c = Over(c, new Color(1f, 1f, 1f, 0.85f), Cov(cracks) * Cov(ice + 4f));
            float glint = Min(Capsule(x, y, 390f, 515f, 450f, 500f, 4f), Capsule(x, y, 170f, 520f, 240f, 512f, 4f), Capsule(x, y, 300f, 452f, 350f, 445f, 3f));
            c = Over(c, new Color(1f, 1f, 1f, 0.75f), Cov(glint) * Cov(ice + 6f));

            // Sparkle and soft blue dimples in the near snow.
            if (y < 1150f)
            {
                int cx = Mathf.FloorToInt(x / 22f), cy = Mathf.FloorToInt(y / 22f);
                float h = Hash(cx * 7 + 3, cy * 3 + 19);
                if (h > 0.8f)
                {
                    float fx = cx * 22f + 5f + Hash(cx, cy) * 12f, fy = cy * 22f + 5f + Hash(cy, cx) * 12f;
                    float size = Mathf.Lerp(3.2f, 1.6f, y / 1150f);
                    c = Over(c, h > 0.93f ? Color.white : Hex("C3D6EA"), Cov(Circle(x, y, fx, fy, size)) * (h > 0.93f ? 1f : 0.6f));
                }
            }
            return c;
        }

        static Color WinterClouds(Color c, float x, float y)
        {
            float Cloud(float cx, float cy, float s) =>
                Min(Circle(x, y, cx, cy, 40f * s), Circle(x, y, cx + 52f * s, cy + 22f * s, 54f * s),
                    Circle(x, y, cx + 112f * s, cy + 2f * s, 40f * s), RoundRect(x, y, cx + 56f * s, cy - 14f * s, 106f * s, 28f * s, 28f * s));
            float d = Min(Cloud(820f, 1880f, 0.45f), Cloud(330f, 2050f, 0.32f));
            return Over(c, new Color(1f, 1f, 1f, 0.9f), Cov(d));
        }

        // ================================================================== road

        static void WinterRoad()
        {
            var points = RoadPoints();
            // Snow banks with a blue shadow, a dark edge, asphalt dusted at the sides, a faint centre line.
            foreach (var (p, _) in points) Disc(p + new Vector2(0f, -4f), RoadWidth(p.y) * 0.5f + 17f, SnowShade);
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f + 14f, SnowWhite);
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f + 4f, Hex("3B4759"));
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f, Hex("5A6679"));
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.36f, Hex("626F83"));
            foreach (var (p, dist) in points)
            {
                float w = RoadWidth(p.y);
                float period = Mathf.Max(18f, w * 0.75f);
                if (Mathf.Repeat(dist, period) < period * 0.5f) Disc(p, Mathf.Max(1.6f, w * 0.045f), new Color(0.93f, 0.96f, 1f, 0.8f));
            }
        }

        // The road densified, in painting space, with the distance along it.
        static List<(Vector2 p, float dist)> RoadPoints()
        {
            var samples = new List<Vector2>();
            foreach (var p in MapLayout.Samples) samples.Add(Unstretch(p));
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
            return points;
        }

        // ================================================================== objects

        static bool WinterBlocked(Vector2 p, float hw, float h)
        {
            for (int k = 0; k <= 4; k++)
            {
                var q = p + new Vector2(0f, h * k / 4f);
                float r = k == 0 ? hw * 0.6f : hw;
                if (RoadDistance(q) < RoadWidth(q.y) * 0.5f + 20f + r) return true;
                if (Sdf.Ellipse(q.x, q.y, IceCenter.x, IceCenter.y, IceRx + 16f, IceRy + 16f) < r) return true;
            }
            return false;
        }

        static void WinterObjects()
        {
            var items = new List<(float y, Action draw)>();
            var occupied = new List<(Vector2 p, float r)>();
            bool Free(Vector2 p, float r)
            {
                foreach (var (q, qr) in occupied) if ((q - p).sqrMagnitude < (r + qr) * (r + qr) * 0.55f) return false;
                return true;
            }
            for (int i = 0; i < MapLayout.StopCount; i++) occupied.Add((Unstretch(MapLayout.StopPosition(i)), 120f));

            // Red and white marker poles along both sides of the road.
            var road = RoadPoints();
            float nextPole = 40f;
            for (int i = 1; i < road.Count; i++)
            {
                var (p, dist) = road[i];
                if (dist < nextPole || p.y < 0f) continue;
                float w = RoadWidth(p.y), s = SizeAt(p.y);
                nextPole = dist + Mathf.Lerp(110f, 60f, Mathf.InverseLerp(200f, 1400f, p.y));
                Vector2 dir = (p - road[i - 1].p).normalized, normal = new(-dir.y, dir.x);
                foreach (float sideSign in new[] { -1f, 1f })
                {
                    var q = p + normal * sideSign * (w * 0.5f + 12f);
                    bool nearStop = false;
                    for (int k = 0; k < MapLayout.StopCount; k++) nearStop |= Vector2.Distance(q, Unstretch(MapLayout.StopPosition(k))) < 105f;
                    if (nearStop) continue;
                    items.Add((q.y, () => Pole(q, s)));
                }
            }

            // The village of log cabins on the right; a couple more up the hill.
            var cabins = new[] { new Vector2(1100, 700), new Vector2(990, 730), new Vector2(1075, 830), new Vector2(955, 860), new Vector2(230, 1095), new Vector2(100, 1110), new Vector2(550, 600) };
            foreach (var p in cabins)
            {
                float s = SizeAt(p.y) * 1.6f;
                if (WinterBlocked(p, 34f * s, 80f * s)) continue;
                occupied.Add((p + new Vector2(0, 30 * s), 70f * s));
                items.Add((p.y, () => Cabin(p, s)));
            }

            // Snowmen by the village and by the lake.
            foreach (var p in new[] { new Vector2(870, 760), new Vector2(510, 430), new Vector2(610, 1000) })
            {
                float s = SizeAt(p.y) * 1.3f;
                if (WinterBlocked(p, 22f * s, 70f * s) || !Free(p, 26f * s)) continue;
                occupied.Add((p, 26f * s));
                items.Add((p.y, () => Snowman(p, s)));
            }

            // The ski lift up the hill on the left.
            Vector2 liftLow = new(20f, 1150f), liftHigh = new(160f, 1370f);
            items.Add((liftHigh.y, () => SkiLift(liftLow, liftHigh)));
            occupied.Add(((liftLow + liftHigh) * 0.5f, 90f));

            var rng = new System.Random(23);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // Snowy pine forests in clusters; a few bare trees and snow-covered bushes in between.
            for (int attempt = 0; attempt < 9000 && items.Count < 330; attempt++)
            {
                var p = new Vector2(Range(-20f, 1190f), Range(220f, 1400f));
                float cluster = Noise(p.x / 140f + 11f, p.y / 140f + 2f, 1000);
                if (cluster < 0.55f && rng.NextDouble() > 0.05) continue;
                float s = SizeAt(p.y) * Range(0.8f, 1.2f);
                float r = 34f * s;
                if (WinterBlocked(p, 36f * s, 112f * s)) continue;
                if (p.y > SnowHill(0, p.x) - 8f) continue;
                if (!Free(p, r)) continue;
                occupied.Add((p, r));
                double kind = rng.NextDouble();
                items.Add((p.y, () => { if (kind < 0.8) SnowPine(p, s); else if (kind < 0.9) BareTree(p, s); else SnowBush(p, s); }));
            }

            // Big framing pines in the foreground.
            foreach (var (p, s) in new[] { (new Vector2(1180, 420), 2.1f), (new Vector2(980, 60), 1.6f), (new Vector2(-10, 760), 1.8f), (new Vector2(520, 40), 1.5f) })
                if (!WinterBlocked(p, 36f * s, 112f * s)) items.Add((p.y, () => SnowPine(p, s)));

            items.Sort((a, b) => b.y.CompareTo(a.y)); // far (high) first
            foreach (var (_, draw) in items) draw();
        }

        // A row of small snowy pines along the back snowfield.
        static void WinterHorizonTrees()
        {
            var rng = new System.Random(9);
            var trees = new List<Vector2>();
            for (float x = -20f; x < 1200f; x += 26f + (float)rng.NextDouble() * 12f)
                trees.Add(new Vector2(x, SnowHill(0, x) - 24f - (float)rng.NextDouble() * 12f));
            trees.Sort((a, b) => b.y.CompareTo(a.y));
            foreach (var t in trees) SnowPine(t, 0.4f + (float)rng.NextDouble() * 0.08f);
        }

        static void SnowPine(Vector2 p, float s)
        {
            Color lit = Hex("3C6E6C"), shade = Hex("2A5254");
            Ellipse(p + new Vector2(10 * s, 2 * s), 30 * s, 8 * s, new Color(0.25f, 0.4f, 0.6f, 0.28f));
            Rect(p + new Vector2(0, 6 * s), 4.5f * s, 9 * s, Hex("5E4636"));
            for (int k = 0; k < 3; k++)
            {
                float baseY = p.y + (14 + k * 26) * s, hw = (34 - k * 8) * s, h = (46 - k * 6) * s;
                Vector2 a = new(p.x - hw, baseY), b = new(p.x + hw, baseY), top = new(p.x, baseY + h), mid = new(p.x, baseY - 4 * s);
                Poly(new[] { a, mid, top }, lit);
                Poly(new[] { mid, b, top }, shade);
                // Snow on the upper part of each tier, with a scalloped lower edge.
                float f = 0.5f;
                Vector2 sa = Vector2.Lerp(top, a, f), sb = Vector2.Lerp(top, b, f), sm = Vector2.Lerp(top, mid, f * 0.8f);
                Poly(new[] { sa, sm, top }, SnowWhite);
                Poly(new[] { sm, sb, top }, Hex("D2E0EE"));
                Disc(Vector2.Lerp(sa, sm, 0.5f) + new Vector2(0, -2.5f * s), 4f * s, SnowWhite);
                Disc(Vector2.Lerp(sm, sb, 0.5f) + new Vector2(0, -2.5f * s), 4f * s, Hex("D2E0EE"));
                Poly(new[] { a, mid, b, top }, Frost, 2.2f);
            }
        }

        static void BareTree(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(8 * s, 2 * s), 22 * s, 6 * s, new Color(0.25f, 0.4f, 0.6f, 0.25f));
            Color bark = Hex("6A5040");
            void Branch(Vector2 a, Vector2 b, float r)
            {
                Shape(UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x, b.x) - r - 2, Mathf.Min(a.y, b.y) - r - 2, Mathf.Max(a.x, b.x) + r + 2, Mathf.Max(a.y, b.y) + r + 2),
                    (x, y) => Capsule(x, y, a.x, a.y, b.x, b.y, r), bark);
                // Snow along the top of the branch.
                Shape(UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x, b.x) - r - 2, Mathf.Min(a.y, b.y) - 2, Mathf.Max(a.x, b.x) + r + 2, Mathf.Max(a.y, b.y) + r + 4),
                    (x, y) => Capsule(x, y, a.x, a.y + r * 0.9f, b.x, b.y + r * 0.9f, r * 0.55f), SnowWhite);
            }
            Vector2 baseP = p, fork = p + new Vector2(0, 50 * s), top = p + new Vector2(-4 * s, 100 * s);
            Branch(baseP, fork, 4.5f * s);
            Branch(fork, top, 3f * s);
            Branch(fork, fork + new Vector2(-30 * s, 32 * s), 2.4f * s);
            Branch(fork + new Vector2(0, 18 * s), fork + new Vector2(28 * s, 44 * s), 2.2f * s);
            Branch(fork + new Vector2(-2 * s, 34 * s), fork + new Vector2(-24 * s, 62 * s), 1.8f * s);
        }

        static void SnowBush(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(8 * s, 1 * s), 30 * s, 7 * s, new Color(0.25f, 0.4f, 0.6f, 0.3f));
            float Body(float x, float y) => Min(Circle(x, y, p.x - 14 * s, p.y + 12 * s, 14 * s), Circle(x, y, p.x + 2 * s, p.y + 20 * s, 18 * s), Circle(x, y, p.x + 18 * s, p.y + 11 * s, 13 * s));
            var box = new Rect(p.x - 32 * s, p.y - 4 * s, 64 * s, 48 * s);
            Shape(box, Body, Hex("D3E1EF"));
            Shape(box, (x, y) => Mathf.Max(Body(x, y), -(y - p.y - 16 * s)), SnowWhite);
            Shape(box, (x, y) => Mathf.Min(Circle(x, y, p.x - 8 * s, p.y + 8 * s, 2.4f * s), Circle(x, y, p.x + 12 * s, p.y + 6 * s, 2.2f * s)), Hex("3C6E6C"));
            Shape(box, (x, y) => Shell(Body(x, y), 1.1f), Frost);
        }

        static void Cabin(Vector2 p, float s)
        {
            float w = 52 * s, wall = 30 * s, roofH = 28 * s;
            Ellipse(p + new Vector2(8 * s, 0), w * 1.1f, 7 * s, new Color(0.25f, 0.4f, 0.6f, 0.3f));
            // Log walls with darker log lines, a door and two warm lit windows.
            Rect(p + new Vector2(0, wall * 0.5f), w * 0.5f, wall * 0.5f, Hex("8E5C3B"));
            for (int k = 1; k < 4; k++) Rect(p + new Vector2(0, wall * k / 4f), w * 0.5f, 0.8f * s, Hex("6A4029"));
            Rect(p + new Vector2(0, wall * 0.3f), 6 * s, 9 * s, Hex("5A3522"));
            for (int k = -1; k <= 1; k += 2)
            {
                Vector2 win = p + new Vector2(k * w * 0.3f, wall * 0.55f);
                Ellipse(win, 13 * s, 11 * s, new Color(1f, 0.85f, 0.45f, 0.22f));
                Rect(win, 6 * s, 5.5f * s, Hex("FFFFFF"));
                Rect(win, 4.5f * s, 4f * s, Hex("FFD27A"));
            }
            // Chimney with a curl of smoke.
            Vector2 chim = p + new Vector2(w * 0.25f, wall + roofH * 0.65f);
            Rect(chim, 4.5f * s, 9 * s, Hex("8C8C96"));
            for (int k = 0; k < 3; k++)
            {
                Vector2 puff = chim + new Vector2((6 + k * 7) * s, (14 + k * 11) * s);
                Shape(Box(puff, 9 * s), (x, y) => Circle(x, y, puff.x, puff.y, (4.5f + k * 1.6f) * s), new Color(0.93f, 0.95f, 0.98f, 0.85f - k * 0.2f));
            }
            // A thick snowy roof with blue shade on the right half.
            Vector2 l = new(p.x - w * 0.66f, p.y + wall - 2 * s), r = new(p.x + w * 0.66f, p.y + wall - 2 * s), top = new(p.x, p.y + wall + roofH);
            Poly(new[] { l, top, new Vector2(p.x, p.y + wall - 2 * s) }, SnowWhite);
            Poly(new[] { new Vector2(p.x, p.y + wall - 2 * s), top, r }, Hex("CFDDEC"));
            Rect(p + new Vector2(0, wall - 1 * s), w * 0.66f, 2.4f * s, Hex("A9BCD2")); // eave shadow
            Rect(chim + new Vector2(0, 9 * s), 5.5f * s, 2.2f * s, SnowWhite);
            Poly(new[] { new Vector2(p.x - w * 0.5f, p.y), new Vector2(p.x + w * 0.5f, p.y), new Vector2(p.x + w * 0.5f, p.y + wall - 2 * s), r, top, l, new Vector2(p.x - w * 0.5f, p.y + wall - 2 * s) }, Frost, 2.4f);
        }

        static void Snowman(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(6 * s, 1 * s), 18 * s, 5 * s, new Color(0.25f, 0.4f, 0.6f, 0.3f));
            var balls = new[] { (p + new Vector2(0, 14 * s), 14f * s), (p + new Vector2(0, 36 * s), 10.5f * s), (p + new Vector2(0, 53 * s), 7.5f * s) };
            foreach (var (c, r) in balls)
            {
                Shape(Box(c, r + 3), (x, y) => Circle(x, y, c.x, c.y, r), Hex("D6E3F0"));
                Shape(Box(c, r + 3), (x, y) => Circle(x, y, c.x - r * 0.18f, c.y + r * 0.18f, r * 0.82f), SnowWhite);
                Shape(Box(c, r + 3), (x, y) => Shell(Circle(x, y, c.x, c.y, r), 0.9f), Frost);
            }
            Vector2 head = balls[2].Item1;
            Poly(new[] { head + new Vector2(1 * s, 1.5f * s), head + new Vector2(1 * s, -1.5f * s), head + new Vector2(9 * s, 0) }, Hex("FF8A2A")); // carrot
            Rect(head + new Vector2(0, 8.5f * s), 6 * s, 1.2f * s, Hex("23313F")); // hat brim
            Rect(head + new Vector2(0, 12.5f * s), 4 * s, 4 * s, Hex("23313F"));
            Rect(balls[1].Item1 + new Vector2(0, 7.5f * s), 7 * s, 1.6f * s, Hex("D9433A")); // scarf
        }

        static void Pole(Vector2 p, float s)
        {
            float h = 24f * s, hw = 1.8f * s;
            for (int k = 0; k < 4; k++)
                Rect(p + new Vector2(0, h * (k + 0.5f) / 4f), hw, h / 8f, k % 2 == 0 ? Hex("E2483A") : Color.white);
            Poly(new[] { p + new Vector2(-hw, 0), p + new Vector2(hw, 0), p + new Vector2(hw, h), p + new Vector2(-hw, h) }, Frost, 1.2f);
        }

        static void SkiLift(Vector2 low, Vector2 high)
        {
            float s = SizeAt(low.y) * 1.2f;
            Vector2 up = new(0f, 46f * s);
            // Two pylons, the cable between them and three chairs hanging from it.
            foreach (var basePos in new[] { low, Vector2.Lerp(low, high, 0.55f) })
            {
                Rect(basePos + up * 0.5f, 2.6f * s, up.y * 0.5f, Hex("5D6B7E"));
                Rect(basePos + up, 10f * s, 2f * s, Hex("5D6B7E"));
            }
            Vector2 a = low + up, b = high + up;
            Shape(UnityEngine.Rect.MinMaxRect(a.x - 4, a.y - 4, b.x + 4, b.y + 4), (x, y) => Capsule(x, y, a.x, a.y, b.x, b.y, 1f), Frost);
            for (int k = 1; k <= 3; k++)
            {
                Vector2 hang = Vector2.Lerp(a, b, k / 4f);
                Rect(hang - new Vector2(0, 6 * s), 0.8f * s, 6 * s, Frost);
                Rect(hang - new Vector2(0, 13 * s), 6 * s, 2.4f * s, k == 2 ? Hex("E2483A") : Hex("2A93E8"));
            }
        }
    }
}
