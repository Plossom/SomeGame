using System;
using System.Collections.Generic;
using SomeGame.UI;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Valley Cup's map, a sunny farming valley in the game's flat cartoon style: a blue sky with puffy clouds,
    /// distant blue hills, rolling green hills with golden wheat fields, a windmill on its hill, a village with a
    /// church, Rosie Rust's farm (red barn, silo, hay bales, chickens) by her race, a creek falling over rocks and
    /// running under a stone bridge into a pond with reeds, lily pads and a jetty, cows and sheep in fenced
    /// pastures, a sunflower field, round trees and pines, rocks and flowers. The sandy country road of
    /// <see cref="MapLayout.Valley"/> winds up through it all. Painting space as in <see cref="MapArtGenerator"/>
    /// (canvas units, height not yet stretched).
    /// </summary>
    public static partial class MapArtGenerator
    {
        static readonly Color Ink = Hex("2F3A24");        // outlines
        static readonly Color Grass = Hex("8DC456"), GrassDark = Hex("74AD45");
        static readonly Color Sand = Hex("E8C686"), SandEdge = Hex("C99A58"), SandDark = Hex("9C6B38");

        // The creek, from the right edge down to the pond (painting space).
        static readonly Vector2[] Creek =
        {
            new(1200f, 770f), new(1110f, 752f), new(1040f, 735f), new(930f, 712f), new(810f, 668f), new(700f, 630f),
            new(590f, 606f), new(480f, 598f), new(390f, 585f),
        };

        static Vector2 Pond => MapScenery.ValleyLake;
        static Vector2 PondRadii => MapScenery.ValleyLakeRadii;

        static float CreekDistance(Vector2 p)
        {
            float d = float.MaxValue;
            for (int i = 1; i < Creek.Length; i++) d = Mathf.Min(d, SegDist(p, Creek[i - 1], Creek[i]));
            return d;
        }

        static float CreekWidth(float x) => Mathf.Lerp(22f, 34f, Mathf.InverseLerp(1200f, 400f, x));

        // ================================================================== the ground

        static float ValleyHill(int i, float x) => i switch
        {
            0 => 1475f + 30f * Mathf.Sin(x / 150f + 0.6f) + 12f * Mathf.Sin(x / 47f),
            1 => 1405f + 50f * Mathf.Sin(x / 190f + 2.1f) + 14f * Mathf.Sin(x / 71f),
            2 => 1270f + 60f * Mathf.Sin(x / 230f + 0.2f) + 12f * Mathf.Sin(x / 83f + 1f),
            3 => 1040f + 55f * Mathf.Sin(x / 260f + 2.7f) + 10f * Mathf.Sin(x / 90f),
            4 => 760f + 50f * Mathf.Sin(x / 280f + 1.1f),
            _ => 420f + 45f * Mathf.Sin(x / 240f + 2.4f),
        };

        static readonly Color[] ValleyHillColors = { Hex("6FA85A"), Hex("B5D86F"), Hex("A3CE62"), Hex("97C85B"), Hex("8DC456"), Hex("86BF51") };

        static Color ValleyLandscape(float x, float y)
        {
            // Sky: deep blue at the top, pale near the hills.
            float t = Mathf.InverseLerp(1480f, 1948f, y);
            Color c = Color.Lerp(Hex("D9F0FA"), Hex("6FB8EA"), t);
            c = ValleyClouds(c, x, y);

            // Distant blue hills.
            c = Over(c, Hex("A9C9DA"), Cov(y - (1560f + 34f * Mathf.Sin(x / 170f + 1.2f) + 16f * Mathf.Sin(x / 61f))));
            c = Over(c, Hex("8FB8B8"), Cov(y - (1525f + 28f * Mathf.Sin(x / 130f + 2.9f) + 10f * Mathf.Sin(x / 43f))));

            for (int i = 0; i < ValleyHillColors.Length; i++)
            {
                float top = ValleyHill(i, x);
                if (i == 0) top += 12f * Mathf.Abs(Mathf.Sin(x / 13f)); // a far line of tree tops
                float d = y - top;
                if (d > 4f / Scale) continue;
                Color fill = Color.Lerp(ValleyHillColors[i], Shade(ValleyHillColors[i], 0.92f), Mathf.Clamp01(-d / 300f));
                c = Over(c, fill, Cov(d));
                if (i > 0) c = Over(c, Color.Lerp(ValleyHillColors[i], Color.white, 0.3f), Cov(Mathf.Abs(d + 5f) - 3.5f) * 0.7f); // a lit crest
            }

            // Mown stripes and tufts on the near meadows.
            if (y < 1300f)
            {
                float stripe = Mathf.Sin((x * 0.7f + y * 1.2f) / 26f);
                if (stripe > 0.85f) c = Shade(c, 0.97f);
                int cx = Mathf.FloorToInt(x / 30f), cy = Mathf.FloorToInt(y / 30f);
                if (Hash(cx * 5 + 3, cy * 9 + 1) > 0.86f)
                {
                    float fx = cx * 30f + 8f + Hash(cx, cy) * 14f, fy = cy * 30f + 8f + Hash(cy, cx) * 14f;
                    float tuft = Min(Capsule(x, y, fx, fy, fx - 3f, fy + 7f, 1.2f), Capsule(x, y, fx, fy, fx + 1f, fy + 8f, 1.2f), Capsule(x, y, fx, fy, fx + 4f, fy + 6f, 1.2f));
                    c = Over(c, Shade(c, 0.8f), Cov(tuft));
                }
            }

            // The pond: a sandy rim, water lighter at the far shore, glints.
            float pond = Sdf.Ellipse(x, y, Pond.x, Pond.y, PondRadii.x, PondRadii.y);
            c = Over(c, Hex("D9C89A"), Cov(pond - 10f));
            c = Over(c, Ink, Cov(Mathf.Abs(pond + 1f) - 1.6f) * Cov(pond - 1f));
            Color water = Color.Lerp(Hex("3E95C9"), Hex("7CC6E8"), Mathf.InverseLerp(Pond.y - PondRadii.y, Pond.y + PondRadii.y, y));
            c = Over(c, water, Cov(pond));

            // The creek: water between grassy-sand banks.
            float creek = CreekDistance(new Vector2(x, y)) - CreekWidth(x) * 0.5f;
            if (creek < 14f)
            {
                c = Over(c, Hex("D9C89A"), Cov(creek - 7f));
                c = Over(c, Ink, Cov(Mathf.Abs(creek + 1f) - 1.6f) * Cov(creek - 1f));
                c = Over(c, Color.Lerp(Hex("4AA3D3"), Hex("8ED3EF"), 0.5f + 0.5f * Mathf.Sin(x / 21f + y / 9f)), Cov(creek));
                if (Mathf.Sin(x / 9f - y / 5f) > 0.93f) c = Over(c, new Color(1f, 1f, 1f, 0.7f), Cov(creek + 3f));
            }
            return c;
        }

        static Color ValleyClouds(Color c, float x, float y)
        {
            float Cloud(float cx, float cy, float s) =>
                Min(Circle(x, y, cx, cy, 34f * s), Circle(x, y, cx + 46f * s, cy + 26f * s, 46f * s), Circle(x, y, cx + 100f * s, cy + 14f * s, 38f * s),
                    Circle(x, y, cx + 140f * s, cy - 4f * s, 26f * s), RoundRect(x, y, cx + 66f * s, cy - 14f * s, 100f * s, 22f * s, 22f * s));
            float d = Min(Cloud(70f, 1585f, 0.62f), Cloud(820f, 1610f, 0.7f), Cloud(470f, 1640f, 0.38f));
            c = Over(c, Hex("FFFFFF"), Cov(d));
            c = Over(c, Hex("DDEFF8"), Cov(d) * Mathf.Clamp01((1600f - y) / 30f) * 0.6f);
            return Over(c, Hex("5E8FB0"), Cov(Mathf.Abs(d) - 1.4f));
        }

        // ================================================================== the road

        static void ValleyRoad()
        {
            var points = RoadPoints();
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f + 6f, Ink);
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f + 3f, SandEdge);
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f - 1f, Sand);
            // Wheel ruts and a few pebbles.
            for (int i = 1; i < points.Count - 1; i++)
            {
                var (p, dist) = points[i];
                if (Mathf.Repeat(dist, 9f) > 3f) continue;
                Vector2 tangent = (points[i + 1].p - points[i - 1].p).normalized;
                Vector2 side = new(-tangent.y, tangent.x);
                float w = RoadWidth(p.y);
                Disc(p + side * w * 0.22f, Mathf.Max(1f, w * 0.03f), new Color(0.72f, 0.55f, 0.3f, 0.45f));
                Disc(p - side * w * 0.22f, Mathf.Max(1f, w * 0.03f), new Color(0.72f, 0.55f, 0.3f, 0.45f));
                if (Hash((int)dist, 3) > 0.9f) Disc(p + side * w * (Hash((int)dist, 7) - 0.5f) * 0.6f, Mathf.Max(1.4f, w * 0.04f), SandDark);
            }
        }

        // The stone bridge where the road crosses the creek: parapets either side of the road, the arch below.
        static void Bridge()
        {
            var road = RoadPoints();
            Vector2 at = Vector2.zero; float best = float.MaxValue; int index = 0;
            for (int i = 0; i < road.Count; i += 2)
            {
                float d = CreekDistance(road[i].p);
                if (d < best) { best = d; at = road[i].p; index = i; }
            }
            Vector2 a = road[Mathf.Max(0, index - 8)].p, b = road[Mathf.Min(road.Count - 1, index + 8)].p;
            Vector2 along = (b - a).normalized, side = new(-along.y, along.x);
            float w = RoadWidth(at.y) * 0.5f + 8f, half = CreekWidth(at.x) * 0.5f + 16f;
            // The arch's shadow on the water, both sides.
            foreach (float s in new[] { -1f, 1f })
            {
                Vector2 m = at + side * (w + 4f) * s;
                Shape(Box(m, half + 10f), (x, y) => Sdf.Ellipse(x, y, m.x, m.y, half * 0.7f, 7f), new Color(0.1f, 0.2f, 0.3f, 0.35f));
                // A parapet of stones along the road edge.
                Vector2 p0 = m - along * half, p1 = m + along * half;
                Shape(Box(m, half + 12f), (x, y) => Capsule(x, y, p0.x, p0.y, p1.x, p1.y, 6.5f), Hex("B9B3A6"));
                Shape(Box(m, half + 12f), (x, y) => Capsule(x, y, p0.x, p0.y + 2f, p1.x, p1.y + 2f, 3f), Hex("D9D4C8"));
                Shape(Box(m, half + 12f), (x, y) => Shell(Capsule(x, y, p0.x, p0.y, p1.x, p1.y, 6.5f), 1.1f), Ink);
                for (int k = 1; k < 4; k++)
                {
                    Vector2 q = Vector2.Lerp(p0, p1, k / 4f);
                    Shape(Box(q, 8f), (x, y) => Capsule(x, y, q.x - side.x * 5f * s, q.y - side.y * 5f * s, q.x + side.x * 5f * s, q.y + side.y * 5f * s, 0.7f), Ink);
                }
            }
        }

        // ================================================================== what stands in the valley

        static bool ValleyBlocked(Vector2 p, float hw, float h)
        {
            for (int k = 0; k <= 4; k++)
            {
                var q = p + new Vector2(0f, h * k / 4f);
                float r = k == 0 ? hw * 0.6f : hw;
                if (RoadDistance(q) < RoadWidth(q.y) * 0.5f + 10f + r) return true;
                if (Sdf.Ellipse(q.x, q.y, Pond.x, Pond.y, PondRadii.x + 18f, PondRadii.y + 18f) < r) return true;
                if (CreekDistance(q) < CreekWidth(q.x) * 0.5f + 10f + r) return true;
            }
            return false;
        }

        // Places with something of their own (no trees there).
        static readonly Rect[] Reserved =
        {
            UnityEngine.Rect.MinMaxRect(880f, 60f, 1180f, 300f),   // Rosie's farm
            UnityEngine.Rect.MinMaxRect(960f, 360f, 1060f, 470f),  // the challenge's stop
            UnityEngine.Rect.MinMaxRect(250f, 20f, 640f, 220f),    // the sunflowers
            UnityEngine.Rect.MinMaxRect(920f, 770f, 1180f, 930f),  // the sheep
            UnityEngine.Rect.MinMaxRect(0f, 960f, 330f, 1120f),    // the cows
            UnityEngine.Rect.MinMaxRect(60f, 1180f, 300f, 1400f),  // the windmill
            UnityEngine.Rect.MinMaxRect(870f, 1220f, 1170f, 1330f),// the village
        };

        static void ValleyObjects()
        {
            var items = new List<(float y, Action draw)>();
            var occupied = new List<(Vector2 p, float r)>();
            bool Free(Vector2 p, float r)
            {
                foreach (var (q, qr) in occupied) if ((q - p).sqrMagnitude < (r + qr) * (r + qr) * 0.55f) return false;
                foreach (var box in Reserved) if (box.Contains(p)) return false;
                return true;
            }
            for (int i = 0; i < MapLayout.StopCount; i++) occupied.Add((Unstretch(MapLayout.StopPosition(i)), 110f));
            void Add(Vector2 at, Action draw) => items.Add((at.y, draw));

            // Rosie's farm: the barn and its silo, hay bales, a chicken yard with a fence, a cart.
            Add(new Vector2(1035f, 150f), () => Barn(new Vector2(1035f, 150f), 1.25f));
            Add(new Vector2(1138f, 190f), () => Silo(new Vector2(1138f, 190f), 1.2f));
            foreach (var (p, s) in new[] { (new Vector2(905f, 112f), 1.15f), (new Vector2(950f, 92f), 1.15f), (new Vector2(926f, 140f), 1.05f) })
                Add(p, () => RoundBale(p, s));
            Fence(new[] { new Vector2(890f, 70f), new Vector2(1170f, 70f) }, 1.1f, items);
            foreach (var p in new[] { new Vector2(990f, 95f), new Vector2(1035f, 82f), new Vector2(1075f, 100f), new Vector2(1110f, 86f) })
                Add(p, () => Hen(p, 1.1f));

            // The sunflower field, fenced.
            var field = new System.Random(41);
            for (int row = 0; row < 6; row++)
                for (int k = 0; k < 11; k++)
                {
                    var p = new Vector2(280f + k * 33f + (row % 2) * 16f + (float)field.NextDouble() * 8f, 196f - row * 30f + (float)field.NextDouble() * 6f);
                    if (RoadDistance(p) < RoadWidth(p.y) * 0.5f + 30f) continue;
                    float size = 1.05f - row * 0.02f + (float)field.NextDouble() * 0.15f;
                    Add(p, () => Sunflower(p, size));
                }
            Fence(new[] { new Vector2(262f, 18f), new Vector2(640f, 18f) }, 1f, items);

            // Sheep in their pen on the right, cows in a meadow on the left.
            Fence(new[] { new Vector2(930f, 790f), new Vector2(1175f, 800f) }, 0.85f, items);
            Fence(new[] { new Vector2(925f, 915f), new Vector2(1175f, 925f) }, 0.85f, items);
            foreach (var (p, flip) in new[] { (new Vector2(990f, 840f), false), (new Vector2(1075f, 860f), true), (new Vector2(1130f, 825f), false), (new Vector2(1030f, 880f), true) })
                Add(p, () => Sheep(p, 0.85f, flip));
            Fence(new[] { new Vector2(-5f, 975f), new Vector2(320f, 985f) }, 0.8f, items);
            foreach (var (p, flip) in new[] { (new Vector2(90f, 1030f), false), (new Vector2(225f, 1055f), true) })
                Add(p, () => Cow(p, 0.8f, flip));

            // The windmill on its hill, a wheat field below it; the village with its church up on the right.
            Add(new Vector2(170f, 1270f), () => Windmill(new Vector2(170f, 1270f), 0.95f));
            WheatField(new Vector2(170f, 1205f), 150f, 32f, -0.12f);
            WheatField(new Vector2(1010f, 1390f), 170f, 26f, 0.1f);
            WheatField(new Vector2(620f, 1430f), 110f, 18f, -0.06f);
            foreach (var (p, church) in MapScenery.ValleyHouses)
            {
                float s = MapScenery.ValleyHouseScale(p);
                Add(p, () => { if (church) VillageChurch(p, s); else VillageHouse(p, s); });
            }

            // By the pond: reeds, lily pads, the jetty; rocks along the creek and its little waterfall.
            Jetty(Pond + new Vector2(PondRadii.x * 0.55f, -PondRadii.y * 0.55f), 1f, items);
            foreach (var p in new[] { Pond + new Vector2(-PondRadii.x * 0.92f, 8f), Pond + new Vector2(-PondRadii.x * 0.72f, PondRadii.y * 0.82f), Pond + new Vector2(PondRadii.x * 0.86f, PondRadii.y * 0.5f) })
                Add(p, () => Reeds(p, 1f));
            foreach (var p in new[] { Pond + new Vector2(-40f, 10f), Pond + new Vector2(-75f, -12f), Pond + new Vector2(20f, 22f) })
                Add(p, () => LilyPad(p, 1f));
            Waterfall(new Vector2(1085f, 748f));
            var rng = new System.Random(23);
            for (int i = 1; i < Creek.Length - 1; i++)
            {
                Vector2 a = Creek[i - 1], b = Creek[i];
                Vector2 side = new Vector2(-(b - a).y, (b - a).x).normalized;
                for (int k = 0; k < 2; k++)
                {
                    var p = Vector2.Lerp(a, b, (float)rng.NextDouble()) + side * (CreekWidth(a.x) * 0.5f + 10f) * (rng.NextDouble() < 0.5 ? -1f : 1f);
                    if (RoadDistance(p) < RoadWidth(p.y) * 0.5f + 20f) continue;
                    float s = 0.7f + (float)rng.NextDouble() * 0.5f;
                    Add(p, () => Rock(p, s));
                }
            }
            Bridge();

            // Trees: round ones and pines in clusters, bushes, rocks and flowers on the open meadows.
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            for (int attempt = 0; attempt < 12000 && items.Count < 330; attempt++)
            {
                var p = new Vector2(Range(-20f, 1190f), Range(230f, 1460f));
                float cluster = Noise(p.x / 140f + 5f, p.y / 140f + 2f, 1000);
                if (cluster < 0.55f && rng.NextDouble() > 0.05) continue;
                float s = SizeAt(p.y) * Range(0.85f, 1.2f);
                if (ValleyBlocked(p, 34f * s, 100f * s) || !Free(p, 30f * s)) continue;
                if (p.y > ValleyHill(1, p.x) - 8f) continue;
                occupied.Add((p, 30f * s));
                double kind = rng.NextDouble();
                items.Add((p.y, () => { if (kind < 0.42) Pine(p, s); else if (kind < 0.88) LeafyTree(p, s, kind < 0.5); else Bush(p, s); }));
            }
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector2(Range(0f, 1170f), Range(20f, 1200f));
                if (ValleyBlocked(p, 12f, 10f) || !Free(p, 10f)) continue;
                double kind = rng.NextDouble();
                float s = SizeAt(p.y);
                if (kind < 0.25) items.Add((p.y, () => Rock(p, s * 0.7f)));
                else items.Add((p.y, () => FlowerPatch(p, s, (int)(kind * 100))));
            }
            // Big framing trees in the bottom corners.
            foreach (var (p, s, pine) in new[] { (new Vector2(-30f, 470f), 2f, true), (new Vector2(1185f, 470f), 1.9f, false), (new Vector2(-40f, 120f), 1.7f, false) })
                if (!ValleyBlocked(p, 30f * s, 100f * s)) items.Add((p.y, () => { if (pine) Pine(p, s); else LeafyTree(p, s, true); }));

            items.Sort((a, b) => b.y.CompareTo(a.y)); // far (high) first
            foreach (var (_, draw) in items) draw();

            // A far row of little trees along the top hill.
            var far = new System.Random(5);
            var trees = new List<Vector2>();
            for (float x = -20f; x < 1200f; x += 26f + (float)far.NextDouble() * 12f)
                trees.Add(new Vector2(x, ValleyHill(0, x) - 18f - (float)far.NextDouble() * 12f));
            trees.Sort((a, b) => b.y.CompareTo(a.y));
            foreach (var p in trees) if (far.NextDouble() < 0.55) Pine(p, 0.4f); else LeafyTree(p, 0.4f, false);
        }

        // ================================================================== painters

        static void Shadow(Vector2 p, float rx, float s) => Ellipse(p + new Vector2(8f * s, 0f), rx, 7f * s, new Color(0.15f, 0.3f, 0.1f, 0.28f));

        static void LeafyTree(Vector2 p, float s, bool apples)
        {
            Shadow(p, 30f * s, s);
            Rect(p + new Vector2(0f, 14f * s), 5f * s, 16f * s, Hex("7A5235"));
            Vector2 c = p + new Vector2(0f, 50f * s);
            float Crown(float x, float y) => Min(Circle(x, y, c.x, c.y, 28f * s), Circle(x, y, c.x - 20f * s, c.y - 6f * s, 20f * s), Circle(x, y, c.x + 20f * s, c.y - 4f * s, 21f * s), Circle(x, y, c.x + 4f * s, c.y + 18f * s, 20f * s));
            Shape(Box(c, 52f * s), Crown, Hex("4C9440"));
            Shape(Box(c, 52f * s), (x, y) => Mathf.Max(Crown(x, y), Circle(x, y, c.x - 10f * s, c.y + 10f * s, 26f * s)), Hex("6DB352"));
            Shape(Box(c, 52f * s), (x, y) => Mathf.Max(Crown(x, y), Circle(x, y, c.x - 16f * s, c.y + 16f * s, 10f * s)), Hex("8CCB66"));
            if (apples)
                foreach (var (ax, ay) in new[] { (-14f, 4f), (10f, 12f), (16f, -8f), (-4f, -12f), (-20f, -10f) })
                {
                    Vector2 a = c + new Vector2(ax, ay) * s;
                    Shape(Box(a, 6f * s), (x, y) => Circle(x, y, a.x, a.y, 4f * s), Hex("E2453A"));
                }
            Shape(Box(c, 54f * s), (x, y) => Shell(Crown(x, y), 1.2f), Ink);
        }

        static void Bush(Vector2 p, float s)
        {
            float B(float x, float y) => Min(Circle(x, y, p.x - 12f * s, p.y + 10f * s, 13f * s), Circle(x, y, p.x + 6f * s, p.y + 14f * s, 15f * s), Circle(x, y, p.x + 18f * s, p.y + 8f * s, 11f * s));
            Shadow(p, 24f * s, s);
            Shape(Box(p + new Vector2(0f, 12f * s), 34f * s), B, Hex("5FA548"));
            Shape(Box(p + new Vector2(0f, 12f * s), 34f * s), (x, y) => Mathf.Max(B(x, y), Circle(x, y, p.x - 6f * s, p.y + 20f * s, 12f * s)), Hex("7DBE5A"));
            Shape(Box(p + new Vector2(0f, 12f * s), 36f * s), (x, y) => Shell(B(x, y), 1.1f), Ink);
        }

        static void Rock(Vector2 p, float s)
        {
            float R(float x, float y) => Min(Sdf.Ellipse(x, y, p.x, p.y + 8f * s, 16f * s, 11f * s), Sdf.Ellipse(x, y, p.x + 10f * s, p.y + 5f * s, 10f * s, 7f * s));
            Shape(Box(p, 30f * s), R, Hex("A7A39A"));
            Shape(Box(p, 30f * s), (x, y) => Mathf.Max(R(x, y), Sdf.Ellipse(x, y, p.x - 4f * s, p.y + 12f * s, 10f * s, 5f * s)), Hex("C8C4BA"));
            Shape(Box(p, 30f * s), (x, y) => Shell(R(x, y), 1.1f), Ink);
        }

        static void FlowerPatch(Vector2 p, float s, int seed)
        {
            Color[] colours = { Hex("FFFFFF"), Hex("F7D257"), Hex("F28AB3"), Hex("C79BF2") };
            for (int k = 0; k < 5; k++)
            {
                Vector2 f = p + new Vector2((Hash(seed, k) - 0.5f) * 30f, (Hash(k, seed) - 0.5f) * 16f) * s;
                Color col = colours[(seed + k) % colours.Length];
                Shape(Box(f, 6f * s), (x, y) => Circle(x, y, f.x, f.y, 3.6f * s), col);
                Shape(Box(f, 3f * s), (x, y) => Circle(x, y, f.x, f.y, 1.4f * s), Hex("E8A23A"));
            }
        }

        static void Barn(Vector2 p, float s)
        {
            float w = 70f * s, wall = 62f * s;
            Shadow(p, w * 1.1f, s);
            // A lean-to on the left.
            Rect(p + new Vector2(-w - 22f * s, wall * 0.32f), 24f * s, wall * 0.32f, Hex("A83A2C"));
            Poly(new[] { new Vector2(p.x - w - 50f * s, p.y + wall * 0.62f), new Vector2(p.x - w, p.y + wall * 0.82f), new Vector2(p.x - w, p.y + wall * 0.62f) }, Hex("6E5A4A"));
            // Red walls, a gambrel roof, white trim, the big X doors and a loft window.
            Rect(p + new Vector2(0f, wall * 0.5f), w, wall * 0.5f, Hex("C8402F"));
            Rect(p + new Vector2(w * 0.5f, wall * 0.5f), w * 0.5f, wall * 0.5f, Hex("B03727"));
            Vector2[] roof = { new(p.x - w - 8f * s, p.y + wall), new(p.x - w * 0.6f, p.y + wall + 34f * s), new(p.x, p.y + wall + 48f * s), new(p.x + w * 0.6f, p.y + wall + 34f * s), new(p.x + w + 8f * s, p.y + wall) };
            Poly(roof, Hex("5E5550"));
            Poly(new[] { roof[2], roof[3], roof[4], new Vector2(p.x, p.y + wall) }, Hex("4A433F"));
            Poly(roof, Ink, 2.6f);
            Vector2 door = p + new Vector2(0f, wall * 0.36f);
            Rect(door, 26f * s, wall * 0.36f, Hex("F4EEDF"));
            Rect(door, 22f * s, wall * 0.36f - 4f * s, Hex("C8402F"));
            Shape(Box(door, 40f * s), (x, y) => Min(Capsule(x, y, door.x - 20f * s, door.y - wall * 0.32f, door.x + 20f * s, door.y + wall * 0.32f, 2.4f * s), Capsule(x, y, door.x - 20f * s, door.y + wall * 0.32f, door.x + 20f * s, door.y - wall * 0.32f, 2.4f * s)), Hex("F4EEDF"));
            Rect(p + new Vector2(0f, wall * 0.84f), 10f * s, 9f * s, Hex("F4EEDF"));
            Rect(p + new Vector2(0f, wall * 0.84f), 7f * s, 6f * s, Hex("3A2E28"));
            Poly(new[] { new Vector2(p.x - w, p.y), new Vector2(p.x + w, p.y), new Vector2(p.x + w, p.y + wall), new Vector2(p.x - w, p.y + wall) }, Ink, 2.6f);
        }

        static void Silo(Vector2 p, float s)
        {
            float r = 18f * s, h = 96f * s;
            Shadow(p, r * 1.3f, s);
            Rect(p + new Vector2(0f, h * 0.5f), r, h * 0.5f, Hex("C9CDD2"));
            Rect(p + new Vector2(r * 0.5f, h * 0.5f), r * 0.5f, h * 0.5f, Hex("A9AEB5"));
            for (int k = 1; k < 5; k++) Rect(p + new Vector2(0f, h * k / 5f), r, 1f * s, Hex("8E949C"));
            Vector2 dome = p + new Vector2(0f, h);
            Shape(Box(dome, r + 4f), (x, y) => Mathf.Max(Circle(x, y, dome.x, dome.y, r), dome.y - y), Hex("B8BEC6"));
            Shape(Box(p + new Vector2(0f, h * 0.5f + r * 0.5f), h), (x, y) => Shell(Min(RoundRect(x, y, p.x, p.y + h * 0.5f, r, h * 0.5f, 0.5f), Mathf.Max(Circle(x, y, dome.x, dome.y, r), dome.y - y)), 1.2f), Ink);
        }

        static void RoundBale(Vector2 p, float s)
        {
            float rx = 20f * s, ry = 16f * s;
            Shadow(p, rx, s);
            Vector2 c = p + new Vector2(0f, ry);
            Shape(Box(c, rx + 4f), (x, y) => Sdf.Ellipse(x, y, c.x, c.y, rx, ry), Hex("E9C25A"));
            Shape(Box(c, rx + 4f), (x, y) => Sdf.Ellipse(x, y, c.x - 6f * s, c.y + 1f * s, rx * 0.55f, ry * 0.75f), Hex("F5D77C"));
            Shape(Box(c, rx + 4f), (x, y) => Shell(Sdf.Ellipse(x, y, c.x - 6f * s, c.y + 1f * s, rx * 0.32f, ry * 0.45f), 0.8f), Hex("C9982F"));
            Shape(Box(c, rx + 4f), (x, y) => Shell(Sdf.Ellipse(x, y, c.x, c.y, rx, ry), 1.1f), Ink);
        }

        static void Hen(Vector2 p, float s)
        {
            Vector2 c = p + new Vector2(0f, 7f * s);
            Shape(Box(c, 12f * s), (x, y) => Sdf.Ellipse(x, y, c.x, c.y, 8f * s, 6f * s), Color.white);
            Shape(Box(c, 12f * s), (x, y) => Circle(x, y, c.x + 7f * s, c.y + 5f * s, 4f * s), Color.white);
            Shape(Box(c, 12f * s), (x, y) => Circle(x, y, c.x + 7f * s, c.y + 9.5f * s, 1.8f * s), Hex("E0303A"));
            Shape(Box(c, 12f * s), (x, y) => Triangle(x, y, new Vector2(c.x + 10.5f * s, c.y + 5.5f * s), new Vector2(c.x + 10.5f * s, c.y + 3.5f * s), new Vector2(c.x + 13.5f * s, c.y + 4.5f * s)), Hex("F6B42C"));
            Shape(Box(c, 13f * s), (x, y) => Shell(Min(Sdf.Ellipse(x, y, c.x, c.y, 8f * s, 6f * s), Circle(x, y, c.x + 7f * s, c.y + 5f * s, 4f * s)), 0.8f), Ink);
        }

        static void Sunflower(Vector2 p, float s)
        {
            Rect(p + new Vector2(0f, 12f * s), 1.6f * s, 12f * s, Hex("4C8A38"));
            Shape(Box(p + new Vector2(4f, 10f) * s, 6f * s), (x, y) => Sdf.Ellipse(x, y, p.x + 5f * s, p.y + 10f * s, 5f * s, 2.4f * s), Hex("5DA048"));
            Vector2 h = p + new Vector2(0f, 28f * s);
            for (int k = 0; k < 10; k++)
            {
                float a = k * Mathf.PI / 5f;
                Vector2 q = h + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 9f * s;
                Shape(Box(q, 7f * s), (x, y) => Circle(x, y, q.x, q.y, 5f * s), Hex("F6C531"));
            }
            Shape(Box(h, 9f * s), (x, y) => Circle(x, y, h.x, h.y, 6.5f * s), Hex("7A4A22"));
            Shape(Box(h, 16f * s), (x, y) => Shell(Circle(x, y, h.x, h.y, 14f * s), 0.7f), new Color(0.18f, 0.22f, 0.12f, 0.6f));
        }

        static void Sheep(Vector2 p, float s, bool flip)
        {
            float f = flip ? -1f : 1f;
            Shadow(p, 20f * s, s);
            foreach (float lx in new[] { -9f, -3f, 5f, 11f }) Rect(p + new Vector2(lx * s * f, 5f * s), 1.8f * s, 5f * s, Hex("3A3330"));
            Vector2 c = p + new Vector2(0f, 16f * s);
            float Wool(float x, float y) => Min(Circle(x, y, c.x - 9f * s, c.y, 9f * s), Circle(x, y, c.x + 2f * s, c.y + 4f * s, 10f * s), Circle(x, y, c.x + 10f * s, c.y - 1f * s, 8f * s), Circle(x, y, c.x, c.y - 4f * s, 9f * s));
            Shape(Box(c, 24f * s), Wool, Hex("F7F4EC"));
            Shape(Box(c, 26f * s), (x, y) => Shell(Wool(x, y), 1f), Ink);
            Vector2 head = c + new Vector2(15f * s * f, 2f * s);
            Shape(Box(head, 9f * s), (x, y) => Sdf.Ellipse(x, y, head.x, head.y, 6f * s, 7f * s), Hex("3A3330"));
        }

        static void Cow(Vector2 p, float s, bool flip)
        {
            float f = flip ? -1f : 1f;
            Shadow(p, 30f * s, s);
            foreach (float lx in new[] { -16f, -9f, 10f, 17f }) Rect(p + new Vector2(lx * s * f, 7f * s), 2.6f * s, 7f * s, Hex("F4F0E6"));
            Vector2 c = p + new Vector2(0f, 22f * s);
            float Body(float x, float y) => RoundRect(x, y, c.x, c.y, 24f * s, 11f * s, 9f * s);
            Shape(Box(c, 30f * s), Body, Hex("F7F3EA"));
            Shape(Box(c, 30f * s), (x, y) => Mathf.Max(Body(x, y), Min(Circle(x, y, c.x - 8f * s * f, c.y + 3f * s, 7f * s), Circle(x, y, c.x + 12f * s * f, c.y - 4f * s, 6f * s))), Hex("8A5233"));
            Vector2 head = c + new Vector2(27f * s * f, 2f * s);
            Shape(Box(head, 12f * s), (x, y) => RoundRect(x, y, head.x, head.y, 8f * s, 9f * s, 5f * s), Hex("F7F3EA"));
            Shape(Box(head, 12f * s), (x, y) => RoundRect(x, y, head.x + 1f * s * f, head.y - 5f * s, 7f * s, 4f * s, 3f * s), Hex("F2B3A6"));
            Shape(Box(c, 42f * s), (x, y) => Shell(Min(Body(x, y), RoundRect(x, y, head.x, head.y, 8f * s, 9f * s, 5f * s)), 1.1f), Ink);
        }

        static void Fence(Vector2[] line, float s, List<(float y, Action draw)> items)
        {
            for (int i = 1; i < line.Length; i++)
            {
                Vector2 a = line[i - 1], b = line[i];
                int posts = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(a, b) / (30f * s)));
                for (int k = 0; k <= posts; k++)
                {
                    Vector2 p = Vector2.Lerp(a, b, k / (float)posts);
                    if (RoadDistance(p) < RoadWidth(p.y) * 0.5f + 8f) continue;
                    items.Add((p.y, () => Rect(p + new Vector2(0f, 9f * s), 2.4f * s, 9f * s, Hex("9A6A3E"))));
                    if (k == posts) continue;
                    Vector2 q = Vector2.Lerp(a, b, (k + 1f) / posts);
                    if (RoadDistance(q) < RoadWidth(q.y) * 0.5f + 8f) continue;
                    items.Add((p.y - 0.1f, () =>
                    {
                        foreach (float h in new[] { 7f, 14f })
                            Shape(UnityEngine.Rect.MinMaxRect(Mathf.Min(p.x, q.x) - 4, Mathf.Min(p.y, q.y) - 2, Mathf.Max(p.x, q.x) + 4, Mathf.Max(p.y, q.y) + 20f * s),
                                (x, y) => Capsule(x, y, p.x, p.y + h * s, q.x, q.y + h * s, 1.4f * s), Hex("B57E4B"));
                    }));
                }
            }
        }

        static void WheatField(Vector2 c, float hw, float hh, float tilt)
        {
            float F(float x, float y) => RotRect(x, y, c.x, c.y, hw, hh, 10f, tilt);
            Shape(Box(c, hw + 20f), F, Hex("E9B949"));
            Shape(Box(c, hw + 20f), (x, y) => Mathf.Max(F(x, y), -Shell(Mathf.Repeat((x - c.x) * Mathf.Cos(tilt) + (y - c.y) * Mathf.Sin(tilt) + 1000f, 16f) - 8f, 3f)), Hex("F4CD62"));
            Shape(Box(c, hw + 20f), (x, y) => Shell(F(x, y), 1.1f), Ink);
        }

        static void Windmill(Vector2 p, float s)
        {
            Shadow(p, 34f * s, s);
            Vector2[] tower = { new(p.x - 30f * s, p.y), new(p.x + 30f * s, p.y), new(p.x + 20f * s, p.y + 96f * s), new(p.x - 20f * s, p.y + 96f * s) };
            Poly(tower, Hex("EFE6D2"));
            Poly(new[] { new Vector2(p.x, p.y), tower[1], tower[2], new Vector2(p.x, p.y + 96f * s) }, Hex("DCCFB4"));
            Poly(tower, Ink, 2.4f);
            Rect(p + new Vector2(0f, 14f * s), 8f * s, 14f * s, Hex("8A5A36"));
            Vector2 cap = p + new Vector2(0f, 96f * s);
            Poly(new[] { new Vector2(cap.x - 26f * s, cap.y), new Vector2(cap.x + 26f * s, cap.y), new Vector2(cap.x, cap.y + 30f * s) }, Hex("B84A33"));
            Poly(new[] { new Vector2(cap.x - 26f * s, cap.y), new Vector2(cap.x + 26f * s, cap.y), new Vector2(cap.x, cap.y + 30f * s) }, Ink, 2.2f);
            Vector2 hub = cap + new Vector2(0f, 6f * s);
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + 30f) * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a)), side = new(-dir.y, dir.x);
                Vector2 tip = hub + dir * 64f * s;
                Vector2[] blade = { hub + dir * 10f * s, tip, tip + side * 14f * s, hub + dir * 10f * s + side * 12f * s };
                Poly(blade, Hex("F7F1E2"));
                Shape(Box(Vector2.Lerp(hub, tip, 0.5f), 40f * s), (x, y) => Mathf.Max(Polygon(x, y, blade), -Shell(Mathf.Repeat(Vector2.Dot(new Vector2(x, y) - hub, dir), 11f * s) - 5.5f * s, 1f * s)), Hex("A47A50"));
                Shape(Box(Vector2.Lerp(hub, tip, 0.5f), 40f * s), (x, y) => Capsule(x, y, hub.x, hub.y, tip.x, tip.y, 1.8f * s), Hex("7A5235"));
                Poly(blade, Ink, 1.8f);
            }
            Shape(Box(hub, 8f * s), (x, y) => Circle(x, y, hub.x, hub.y, 5f * s), Hex("5A3A22"));
        }

        static void VillageHouse(Vector2 p, float s)
        {
            float w = 34f * s, wall = 30f * s, roofH = 24f * s;
            Shadow(p, w * 1.2f, s);
            Rect(p + new Vector2(0f, wall * 0.5f), w, wall * 0.5f, Hex("F1E4C6"));
            Rect(p + new Vector2(w * 0.55f, wall * 0.5f), w * 0.45f, wall * 0.5f, Hex("E0CFAA"));
            for (int k = -1; k <= 1; k += 2) { Rect(p + new Vector2(k * w * 0.5f, wall * 0.6f), 5f * s, 5f * s, Hex("FFF1C8")); Rect(p + new Vector2(k * w * 0.5f, wall * 0.6f), 5f * s, 0.8f * s, Hex("8A6A4A")); }
            Rect(p + new Vector2(0f, wall * 0.28f), 5f * s, wall * 0.28f, Hex("8A5A36"));
            Vector2 l = new(p.x - w * 1.15f, p.y + wall), r = new(p.x + w * 1.15f, p.y + wall), top = new(p.x, p.y + wall + roofH);
            Rect(p + new Vector2(w * 0.5f, wall + roofH * 0.62f), 4f * s, 8f * s, Hex("C9B8A0")); // chimney
            Poly(new[] { l, top, new Vector2(p.x, p.y + wall) }, Hex("E06A3A"));
            Poly(new[] { new Vector2(p.x, p.y + wall), top, r }, Hex("C1552D"));
            Poly(new[] { new Vector2(p.x - w, p.y), new Vector2(p.x + w, p.y), new Vector2(p.x + w, p.y + wall), r, top, l, new Vector2(p.x - w, p.y + wall) }, Ink, 2.2f);
        }

        static void VillageChurch(Vector2 p, float s)
        {
            Shadow(p, 46f * s, s);
            VillageHouse(p + new Vector2(14f * s, 0f), s * 1.05f);
            Vector2 t = p + new Vector2(-30f * s, 0f);
            Rect(t + new Vector2(0f, 34f * s), 11f * s, 34f * s, Hex("F1E4C6"));
            Rect(t + new Vector2(5f * s, 34f * s), 6f * s, 34f * s, Hex("E0CFAA"));
            Rect(t + new Vector2(0f, 54f * s), 4f * s, 6f * s, Hex("4A3A2E"));
            Poly(new[] { new Vector2(t.x - 14f * s, t.y + 68f * s), new Vector2(t.x + 14f * s, t.y + 68f * s), new Vector2(t.x, t.y + 112f * s) }, Hex("C1552D"));
            Poly(new[] { new Vector2(t.x - 11f * s, t.y), new Vector2(t.x + 11f * s, t.y), new Vector2(t.x + 11f * s, t.y + 68f * s), new Vector2(t.x + 14f * s, t.y + 68f * s), new Vector2(t.x, t.y + 112f * s), new Vector2(t.x - 14f * s, t.y + 68f * s), new Vector2(t.x - 11f * s, t.y + 68f * s) }, Ink, 2.2f);
        }

        static void Jetty(Vector2 p, float s, List<(float y, Action draw)> items)
        {
            items.Add((p.y + 30f, () =>
            {
                Vector2 a = p, b = p + new Vector2(-34f, 34f) * s;
                Shape(Box(Vector2.Lerp(a, b, 0.5f), 40f * s), (x, y) => Capsule(x, y, a.x, a.y, b.x, b.y, 8f * s), Hex("B07A44"));
                for (int k = 0; k < 5; k++)
                {
                    Vector2 q = Vector2.Lerp(a, b, k / 4f);
                    Shape(Box(q, 10f * s), (x, y) => Capsule(x, y, q.x - 6f * s, q.y - 6f * s, q.x + 6f * s, q.y + 6f * s, 0.8f), Hex("7A5235"));
                }
                Shape(Box(Vector2.Lerp(a, b, 0.5f), 42f * s), (x, y) => Shell(Capsule(x, y, a.x, a.y, b.x, b.y, 8f * s), 1f), Ink);
            }));
        }

        static void Reeds(Vector2 p, float s)
        {
            for (int k = -2; k <= 2; k++)
            {
                Vector2 b = p + new Vector2(k * 5f * s, 0f), t = b + new Vector2(k * 2f * s, (24f + Mathf.Abs(k) * -3f) * s);
                Shape(Box(Vector2.Lerp(b, t, 0.5f), 20f * s), (x, y) => Capsule(x, y, b.x, b.y, t.x, t.y, 1.3f * s), Hex("4E8F3A"));
                if (k % 2 == 0) Shape(Box(t, 8f * s), (x, y) => Capsule(x, y, t.x, t.y - 4f * s, t.x, t.y + 4f * s, 2.6f * s), Hex("8A5A36"));
            }
        }

        static void LilyPad(Vector2 p, float s)
        {
            Shape(Box(p, 12f * s), (x, y) => Mathf.Max(Sdf.Ellipse(x, y, p.x, p.y, 9f * s, 5f * s), -Triangle(x, y, p, p + new Vector2(10f, 3f) * s, p + new Vector2(10f, -3f) * s)), Hex("5DAA4E"));
            Shape(Box(p, 4f * s), (x, y) => Circle(x, y, p.x - 3f * s, p.y + 2f * s, 2f * s), Hex("F7A8C4"));
        }

        static void Waterfall(Vector2 p)
        {
            // Rocks round a little step in the creek, white water tumbling over it.
            foreach (var (o, s) in new[] { (new Vector2(-26f, 10f), 1.2f), (new Vector2(26f, 6f), 1.1f), (new Vector2(-14f, -14f), 0.9f), (new Vector2(18f, -16f), 0.9f) })
                Rock(p + o, s);
            Shape(Box(p, 24f), (x, y) => RoundRect(x, y, p.x, p.y - 2f, 10f, 13f, 4f), Hex("E6F6FC"));
            Shape(Box(p, 24f), (x, y) => Mathf.Max(RoundRect(x, y, p.x, p.y - 2f, 10f, 13f, 4f), -Shell(Mathf.Repeat(x + 100f, 6f) - 3f, 1f)), Hex("9ED8F0"));
            Shape(Box(p + new Vector2(0f, -16f), 20f), (x, y) => Sdf.Ellipse(x, y, p.x, p.y - 16f, 14f, 4f), new Color(1f, 1f, 1f, 0.85f));
        }
    }
}
