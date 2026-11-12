using System;
using System.Collections.Generic;
using SomeGame.UI;
using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The Water Cup's landscape (Assets/Art/Rally/MapWater.png): a sunny sky over a tropical sea, islands on the
    /// horizon (a green volcano, a rocky islet with a lighthouse), the sea growing turquoise towards you over reefs
    /// and sandbars, jungle islands with palms and huts on stilts, sailboats, a pirate wreck, a beach with
    /// umbrellas along the bottom, and the sea lane of <see cref="MapLayout.Water"/>: deep blue water between foam
    /// edges, lined with red and white buoys. Same canvas, scale and stretch as the other maps.
    /// </summary>
    public static partial class MapArtGenerator
    {
        static readonly Color SeaLine = Hex("0F3A55"); // outlines
        const float Horizon = 1470f;

        // Islands in the sea (painting space): centre and radii.
        static readonly (Vector2 c, Vector2 r)[] SeaIslands =
        {
            (new Vector2(1050f, 1140f), new Vector2(160f, 46f)),
            (new Vector2(70f, 760f), new Vector2(140f, 52f)),
            (new Vector2(560f, 1290f), new Vector2(120f, 30f)),
            (new Vector2(1080f, 470f), new Vector2(110f, 44f)),
            (new Vector2(330f, 560f), new Vector2(60f, 20f)),
        };

        [MenuItem("SomeGame/Generate Water Map Art")]
        public static void GenerateWater()
        {
            var previous = MapLayout.Current;
            MapLayout.Current = MapLayout.Water;
            try
            {
                _w = Mathf.RoundToInt(MapLayout.Size.x * Scale);
                _h = Mathf.RoundToInt(MapLayout.Size.y * Scale);
                _px = new Color[_w * _h];
                for (int py = 0; py < _h; py++)
                    for (int px = 0; px < _w; px++)
                        _px[py * _w + px] = WaterLandscape((px + 0.5f) / Scale, (py + 0.5f) / Scale / Stretch);
                WaterIslandsBase();
                SeaLane();
                WaterObjects();
                Sdf.WritePixels(ArtGenerator.Folder, "MapWater", _w, _h, _px, 100, false, 0, compress: true);
                _px = null;
                Debug.Log("Water map art generated");
            }
            finally
            {
                MapLayout.Current = previous;
            }
        }

        // ================================================================== sky and sea

        static Color WaterLandscape(float x, float y)
        {
            if (y > Horizon - 2f)
            {
                // Warm at the horizon, clear blue above, a big sun on the right with a soft glow.
                float t = Mathf.InverseLerp(Horizon, 1948f, y);
                Color sky = Color.Lerp(Hex("FFE8C2"), Hex("6FC6EE"), Mathf.SmoothStep(0f, 1f, Mathf.Pow(t, 0.7f)));
                float sun = Circle(x, y, 890f, 1610f, 52f);
                sky = Over(sky, Hex("FFF4C8"), Gauss(Mathf.Max(0f, sun), 120f) * 0.6f);
                sky = Over(sky, Hex("FFF6D2"), Cov(sun));
                sky = WaterClouds(sky, x, y);
                sky = HorizonIslands(sky, x, y);
                if (y > Horizon + 2f) return sky;
            }
            // The sea: deep blue far away, turquoise nearer, pale over the sand in front.
            float near = Mathf.InverseLerp(Horizon, 120f, y);
            Color sea = Color.Lerp(Hex("1C6FA6"), Hex("23A9CF"), Mathf.SmoothStep(0f, 0.55f, near));
            sea = Color.Lerp(sea, Hex("3FCDC8"), Mathf.SmoothStep(0.45f, 0.85f, near));
            sea = Color.Lerp(sea, Hex("8CE6D8"), Mathf.SmoothStep(0.85f, 1f, near));
            // Reefs and sandbars: lighter turquoise patches, coral spots in them.
            float reef = Noise(x / 140f + 3f, y / 70f + 9f, 1000);
            if (y < 1250f) sea = Color.Lerp(sea, Hex("7FE3D3"), Mathf.Clamp01((reef - 0.62f) * 3f) * 0.6f);
            if (y < 1100f && reef > 0.7f)
            {
                int cx = Mathf.FloorToInt(x / 26f), cy = Mathf.FloorToInt(y / 18f);
                float h = Hash(cx * 5 + 1, cy * 7 + 3);
                if (h > 0.7f)
                {
                    float fx = cx * 26f + 6f + Hash(cx, cy) * 14f, fy = cy * 18f + 4f + Hash(cy, cx) * 10f;
                    Color coral = h > 0.9f ? Hex("E86AA6") : h > 0.8f ? Hex("FF8A4C") : Hex("9A6AD8");
                    sea = Over(sea, new Color(coral.r, coral.g, coral.b, 0.55f), Cov(Sdf.Ellipse(x, y, fx, fy, 7f * SizeAt(y), 4f * SizeAt(y))));
                }
            }
            // Swell lines and glitter: short white dashes, smaller and denser in the distance.
            float cell = Mathf.Lerp(18f, 70f, near);
            int gx = Mathf.FloorToInt(x / cell), gy = Mathf.FloorToInt(y / (cell * 0.35f));
            float g = Hash(gx * 13 + 7, gy * 3 + 11);
            if (g > 0.78f)
            {
                float cxp = gx * cell + cell * 0.5f, cyp = gy * cell * 0.35f + cell * 0.17f;
                float dash = Capsule(x, y, cxp - cell * 0.28f, cyp, cxp + cell * 0.28f, cyp, Mathf.Lerp(0.8f, 2.2f, near));
                sea = Over(sea, new Color(1f, 1f, 1f, g > 0.92f ? 0.85f : 0.45f), Cov(dash));
            }
            // A sun path glittering on the water below the sun.
            float path = Mathf.Abs(x - 890f) - Mathf.Lerp(30f, 240f, Mathf.InverseLerp(Horizon, 760f, y));
            if (path < 0f && y > 760f)
            {
                float pc = Mathf.Lerp(10f, 30f, Mathf.InverseLerp(Horizon, 760f, y));
                int px = Mathf.FloorToInt(x / pc), py = Mathf.FloorToInt(y / (pc * 0.3f));
                float ph = Hash(px * 17 + 5, py * 11 + 1);
                if (ph > 0.72f)
                {
                    float cxp = px * pc + pc * 0.5f, cyp = py * pc * 0.3f + pc * 0.15f;
                    float glitter = Capsule(x, y, cxp - pc * 0.3f, cyp, cxp + pc * 0.3f, cyp, 1.2f);
                    float fade = Mathf.Clamp01(-path / 40f);
                    sea = Over(sea, Hex("FFF4D0"), Cov(glitter) * 0.8f * fade);
                }
            }
            // The beach along the bottom: wet sand, a foam line, dry sand.
            float shore = y - (110f + 24f * Mathf.Sin(x / 95f + 1f) + 10f * Mathf.Sin(x / 37f));
            sea = Over(sea, Hex("E2C58E"), Cov(shore + 8f));
            sea = Over(sea, Color.white, Cov(Mathf.Abs(shore + 4f) - 3f) * 0.9f);
            sea = Over(sea, Hex("F6E2B2"), Cov(shore + 16f));
            return sea;
        }

        static Color WaterClouds(Color c, float x, float y)
        {
            float Cloud(float cx, float cy, float s) =>
                Min(Circle(x, y, cx, cy, 40f * s), Circle(x, y, cx + 52f * s, cy + 22f * s, 54f * s),
                    Circle(x, y, cx + 112f * s, cy + 2f * s, 40f * s), RoundRect(x, y, cx + 56f * s, cy - 14f * s, 106f * s, 28f * s, 28f * s));
            float d = Min(Cloud(560f, 1720f, 0.5f), Cloud(130f, 1800f, 0.36f), Cloud(980f, 1820f, 0.3f));
            return Over(c, new Color(1f, 1f, 1f, 0.92f), Cov(d));
        }

        // Far islands sitting on the horizon: a green volcano with a wisp of cloud, a long low island, a rocky islet.
        static Color HorizonIslands(Color c, float x, float y)
        {
            float volcano = y - (Horizon + Mathf.Max(0f, 120f - Mathf.Abs(x - 250f) * 0.75f) + 6f * Mathf.Sin(x / 13f));
            c = Over(c, Color.Lerp(Hex("5E9C8E"), Hex("88B9A6"), Mathf.InverseLerp(Horizon, Horizon + 120f, y)), Cov(volcano) * Cov(Mathf.Abs(x - 250f) - 175f));
            float low = y - (Horizon + 26f + 10f * Mathf.Sin(x / 40f));
            c = Over(c, Hex("6FA693"), Cov(low) * Cov(Mathf.Abs(x - 700f) - 110f));
            float islet = y - (Horizon + 40f - Mathf.Abs(x - 1090f) * 0.5f);
            c = Over(c, Hex("8E9AA0"), Cov(islet) * Cov(Mathf.Abs(x - 1090f) - 80f));
            return c;
        }

        // ================================================================== islands

        static void WaterIslandsBase()
        {
            foreach (var (centre, r) in SeaIslands)
            {
                // Pale shallows, a ring of foam, the beach, the jungle.
                Ellipse(centre + new Vector2(0f, -4f), r.x + 34f, r.y + 14f, new Color(0.6f, 0.95f, 0.88f, 0.75f));
                Ellipse(centre, r.x + 8f, r.y + 4f, new Color(1f, 1f, 1f, 0.9f));
                Ellipse(centre, r.x, r.y, Hex("F3DDA8"));
                Ellipse(centre + new Vector2(0f, r.y * 0.25f), r.x * 0.72f, r.y * 0.62f, Hex("3E9B4F"));
                Ellipse(centre + new Vector2(-r.x * 0.15f, r.y * 0.38f), r.x * 0.4f, r.y * 0.3f, Hex("5BB862"));
            }
        }

        // ================================================================== the sea lane

        static void SeaLane()
        {
            var points = RoadPoints();
            foreach (var (p, _) in points) Disc(p + new Vector2(0f, -3f), RoadWidth(p.y) * 0.5f + 12f, new Color(0.55f, 0.92f, 0.9f, 0.6f));
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f + 5f, new Color(1f, 1f, 1f, 0.95f));
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.5f, Hex("1B78BE"));
            foreach (var (p, _) in points) Disc(p, RoadWidth(p.y) * 0.32f, Hex("2389CC"));
        }

        // ================================================================== objects

        static bool WaterBlocked(Vector2 p, float hw, float h)
        {
            for (int k = 0; k <= 4; k++)
            {
                var q = p + new Vector2(0f, h * k / 4f);
                if (RoadDistance(q) < RoadWidth(q.y) * 0.5f + 18f + (k == 0 ? hw * 0.6f : hw)) return true;
            }
            return false;
        }

        static void WaterObjects()
        {
            var items = new List<(float y, Action draw)>();
            var occupied = new List<(Vector2 p, float r)>();
            bool Free(Vector2 p, float r)
            {
                foreach (var (q, qr) in occupied) if ((q - p).sqrMagnitude < (r + qr) * (r + qr) * 0.55f) return false;
                return true;
            }
            for (int i = 0; i < MapLayout.StopCount; i++) occupied.Add((Unstretch(MapLayout.StopPosition(i)), 120f));

            // Buoys along both sides of the lane.
            var road = RoadPoints();
            float next = 30f;
            bool red = false;
            for (int i = 1; i < road.Count; i++)
            {
                var (p, dist) = road[i];
                if (dist < next || p.y < 120f) continue;
                float w = RoadWidth(p.y), s = SizeAt(p.y);
                next = dist + Mathf.Lerp(90f, 50f, Mathf.InverseLerp(200f, 1400f, p.y));
                Vector2 dir = (p - road[i - 1].p).normalized, normal = new(-dir.y, dir.x);
                red = !red;
                foreach (float side in new[] { -1f, 1f })
                {
                    var q = p + normal * side * (w * 0.5f + 11f);
                    bool nearStop = false;
                    for (int k = 0; k < MapLayout.StopCount; k++) nearStop |= Vector2.Distance(q, Unstretch(MapLayout.StopPosition(k))) < 100f;
                    if (nearStop) continue;
                    bool r = red;
                    items.Add((q.y, () => Buoy(q, s, r)));
                }
            }

            // Palms and huts on the islands.
            var rng = new System.Random(31);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            foreach (var (centre, r) in SeaIslands)
            {
                int palms = Mathf.RoundToInt(r.x / 22f);
                for (int k = 0; k < palms * 6 && palms > 0; k++)
                {
                    var p = centre + new Vector2(Range(-r.x * 0.75f, r.x * 0.75f), Range(-r.y * 0.3f, r.y * 0.55f));
                    if (Sdf.Ellipse(p.x, p.y, centre.x, centre.y, r.x * 0.8f, r.y * 0.8f) > 0f) continue;
                    float s = SizeAt(p.y) * Range(0.85f, 1.15f);
                    if (!Free(p, 22f * s)) continue;
                    occupied.Add((p, 22f * s));
                    palms--;
                    items.Add((p.y, () => MapPalm(p, s, rng.NextDouble() < 0.5)));
                }
            }
            foreach (var (p, s) in new[] { (new Vector2(820f, 1090f), 1.15f), (new Vector2(170f, 690f), 1.2f), (new Vector2(990f, 410f), 1.1f) })
            {
                float sc = SizeAt(p.y) * s;
                if (WaterBlocked(p, 30f * sc, 60f * sc)) continue;
                occupied.Add((p, 34f * sc));
                items.Add((p.y, () => StiltHut(p, sc)));
            }

            // The lighthouse on its islet by the horizon, sailboats, the pirate wreck.
            items.Add((1380f, () => MapLighthouse(new Vector2(1090f, 1382f), SizeAt(1382f) * 1.5f)));
            foreach (var (p, s) in new[] { (new Vector2(370f, 1400f), 0.8f), (new Vector2(860f, 1330f), 0.9f), (new Vector2(130f, 1210f), 1f), (new Vector2(640f, 760f), 1.1f) })
            {
                float sc = SizeAt(p.y) * s;
                if (WaterBlocked(p, 26f * sc, 70f * sc) || !Free(p, 30f * sc)) continue;
                occupied.Add((p, 30f * sc));
                items.Add((p.y, () => Sailboat(p, sc)));
            }
            var wreck = new Vector2(1000f, 860f);
            if (!WaterBlocked(wreck, 50f, 80f)) items.Add((wreck.y, () => Wreck(wreck, SizeAt(wreck.y) * 1.3f)));

            // The beach in front: palms, umbrellas and a towel or two.
            foreach (var (p, s) in new[] { (new Vector2(40f, 40f), 2.3f), (new Vector2(1150f, 60f), 2f), (new Vector2(250f, 20f), 1.6f) })
                items.Add((p.y, () => MapPalm(p, s, true)));
            foreach (var p in new[] { new Vector2(520f, 50f), new Vector2(760f, 40f), new Vector2(940f, 70f) })
                items.Add((p.y, () => BeachUmbrella(p, SizeAt(p.y) * 1.4f)));

            items.Sort((a, b) => b.y.CompareTo(a.y)); // far (high) first
            foreach (var (_, draw) in items) draw();
        }

        static void MapPalm(Vector2 p, float s, bool leanRight)
        {
            float lean = leanRight ? 1f : -1f;
            Ellipse(p + new Vector2(10 * s, 0), 24 * s, 6 * s, new Color(0.05f, 0.25f, 0.2f, 0.25f));
            // A curved trunk with rings.
            Vector2 top = p + new Vector2(18f * s * lean, 92f * s);
            Vector2 mid = p + new Vector2(4f * s * lean, 48f * s);
            Shape(Box((p + top) * 0.5f, 70 * s), (x, y) => Mathf.Min(Capsule(x, y, p.x, p.y, mid.x, mid.y, 5.5f * s), Capsule(x, y, mid.x, mid.y, top.x, top.y, 4.5f * s)), Hex("9C6B3C"));
            for (int i = 1; i < 6; i++)
            {
                Vector2 q = i < 3 ? Vector2.Lerp(p, mid, i / 3f) : Vector2.Lerp(mid, top, (i - 3) / 3f);
                Shape(Box(q, 8 * s), (x, y) => Capsule(x, y, q.x - 5 * s, q.y, q.x + 5 * s, q.y, 1.1f * s), Hex("6E4A28"));
            }
            // Fronds drooping all round, coconuts at the crown.
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f + 0.3f;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a) * 0.5f + 0.15f);
                Vector2 m = top + dir * 26f * s + new Vector2(0, 6f * s), tip = top + dir * 50f * s - new Vector2(0, 14f * s);
                Color leaf = i % 2 == 0 ? Hex("2E9E4F") : Hex("3DB15A");
                Shape(Box(top, 60 * s), (x, y) => Mathf.Min(Capsule(x, y, top.x, top.y, m.x, m.y, 6.5f * s), Capsule(x, y, m.x, m.y, tip.x, tip.y, 4f * s)), leaf);
            }
            foreach (var o in new[] { new Vector2(-5, -4), new Vector2(4, -6), new Vector2(0, -1) })
                Disc(top + o * s, 4.2f * s, Hex("6B4426"));
        }

        static void StiltHut(Vector2 p, float s)
        {
            Ellipse(p, 36 * s, 7 * s, new Color(1f, 1f, 1f, 0.6f));
            for (int i = -1; i <= 1; i += 2) Rect(p + new Vector2(i * 22 * s, 14 * s), 2.2f * s, 14 * s, Hex("6B4A2E"));
            Rect(p + new Vector2(0, 30 * s), 32 * s, 4 * s, Hex("A57A4C"));
            Rect(p + new Vector2(0, 44 * s), 24 * s, 12 * s, Hex("D9B27A"));
            Rect(p + new Vector2(0, 42 * s), 6 * s, 9 * s, Hex("5E3F24"));
            Poly(new[] { new Vector2(p.x - 36 * s, p.y + 54 * s), new Vector2(p.x + 36 * s, p.y + 54 * s), new Vector2(p.x, p.y + 84 * s) }, Hex("D4A65C"));
            Poly(new[] { new Vector2(p.x - 36 * s, p.y + 54 * s), new Vector2(p.x + 36 * s, p.y + 54 * s), new Vector2(p.x, p.y + 84 * s) }, SeaLine, 2.2f);
        }

        static void MapLighthouse(Vector2 p, float s)
        {
            Ellipse(p, 40 * s, 10 * s, Hex("8E9AA0"));
            for (int i = 0; i < 4; i++)
            {
                float y0 = p.y + i * 22 * s, y1 = y0 + 22 * s;
                float hw0 = Mathf.Lerp(13f, 8f, i / 4f) * s, hw1 = Mathf.Lerp(13f, 8f, (i + 1) / 4f) * s;
                Poly(new[] { new Vector2(p.x - hw0, y0), new Vector2(p.x + hw0, y0), new Vector2(p.x + hw1, y1), new Vector2(p.x - hw1, y1) }, i % 2 == 0 ? Hex("E8423A") : Color.white);
            }
            Rect(p + new Vector2(0, 96 * s), 9 * s, 8 * s, Hex("FFF1A8"));
            Poly(new[] { new Vector2(p.x - 11 * s, p.y + 104 * s), new Vector2(p.x + 11 * s, p.y + 104 * s), new Vector2(p.x, p.y + 116 * s) }, Hex("2A2F36"));
            Poly(new[] { new Vector2(p.x - 13 * s, p.y), new Vector2(p.x + 13 * s, p.y), new Vector2(p.x + 8 * s, p.y + 88 * s), new Vector2(p.x - 8 * s, p.y + 88 * s) }, SeaLine, 2f);
        }

        static void Sailboat(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(0, -2 * s), 30 * s, 5 * s, new Color(1f, 1f, 1f, 0.6f));
            Poly(new[] { new Vector2(p.x - 26 * s, p.y + 8 * s), new Vector2(p.x + 30 * s, p.y + 8 * s), new Vector2(p.x + 20 * s, p.y), new Vector2(p.x - 20 * s, p.y) }, Hex("FF6B52"));
            Rect(p + new Vector2(0, 40 * s), 1.4f * s, 32 * s, Hex("6B4A2E"));
            Poly(new[] { new Vector2(p.x + 2 * s, p.y + 12 * s), new Vector2(p.x + 2 * s, p.y + 70 * s), new Vector2(p.x + 30 * s, p.y + 14 * s) }, Color.white);
            Poly(new[] { new Vector2(p.x - 2 * s, p.y + 16 * s), new Vector2(p.x - 2 * s, p.y + 56 * s), new Vector2(p.x - 20 * s, p.y + 16 * s) }, Hex("F6F1E4"));
            Poly(new[] { new Vector2(p.x - 26 * s, p.y + 8 * s), new Vector2(p.x + 30 * s, p.y + 8 * s), new Vector2(p.x + 20 * s, p.y), new Vector2(p.x - 20 * s, p.y) }, SeaLine, 1.8f);
        }

        static void Wreck(Vector2 p, float s)
        {
            // A pirate ship's hull half sunk on a reef, a broken mast and a skull flag.
            Ellipse(p, 60 * s, 10 * s, new Color(1f, 1f, 1f, 0.55f));
            Poly(new[] { new Vector2(p.x - 50 * s, p.y + 4 * s), new Vector2(p.x + 44 * s, p.y + 2 * s), new Vector2(p.x + 56 * s, p.y + 24 * s), new Vector2(p.x - 40 * s, p.y + 18 * s) }, Hex("6B4426"));
            for (int i = 0; i < 4; i++) Rect(p + new Vector2(-30 * s + i * 22 * s, 12 * s), 1.2f * s, 7 * s, Hex("4A2E18"));
            Shape(Box(p + new Vector2(10 * s, 50 * s), 50 * s), (x, y) => Capsule(x, y, p.x + 6 * s, p.y + 20 * s, p.x + 20 * s, p.y + 82 * s, 2.4f * s), Hex("4A2E18"));
            Poly(new[] { new Vector2(p.x + 20 * s, p.y + 82 * s), new Vector2(p.x + 46 * s, p.y + 76 * s), new Vector2(p.x + 18 * s, p.y + 64 * s) }, Hex("1C1F24"));
            Disc(p + new Vector2(28 * s, 74 * s), 3.2f * s, Color.white);
            Poly(new[] { new Vector2(p.x - 50 * s, p.y + 4 * s), new Vector2(p.x + 44 * s, p.y + 2 * s), new Vector2(p.x + 56 * s, p.y + 24 * s), new Vector2(p.x - 40 * s, p.y + 18 * s) }, SeaLine, 2f);
        }

        static void Buoy(Vector2 p, float s, bool red)
        {
            Ellipse(p, 9 * s, 3 * s, new Color(1f, 1f, 1f, 0.7f));
            Shape(Box(p + new Vector2(0, 9 * s), 12 * s), (x, y) => RoundRect(x, y, p.x, p.y + 9 * s, 6 * s, 9 * s, 5 * s), red ? Hex("E8423A") : Color.white);
            Rect(p + new Vector2(0, 9 * s), 6 * s, 2 * s, red ? Color.white : Hex("E8423A"));
            Shape(Box(p + new Vector2(0, 9 * s), 12 * s), (x, y) => Shell(RoundRect(x, y, p.x, p.y + 9 * s, 6 * s, 9 * s, 5 * s), 1f), SeaLine);
        }

        static void BeachUmbrella(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(14 * s, 0), 30 * s, 7 * s, new Color(0.4f, 0.3f, 0.1f, 0.2f));
            Rect(p + new Vector2(0, 24 * s), 1.6f * s, 24 * s, Hex("8A5A32"));
            for (int i = 0; i < 6; i++)
            {
                float a0 = Mathf.PI * i / 6f, a1 = Mathf.PI * (i + 1) / 6f;
                Vector2 c = p + new Vector2(0, 48 * s);
                Poly(new[] { c + new Vector2(0, 14 * s), c + new Vector2(-Mathf.Cos(a0) * 34 * s, 0), c + new Vector2(-Mathf.Cos(a1) * 34 * s, 0) }, i % 2 == 0 ? Hex("FF5A4E") : Color.white);
            }
            Rect(p + new Vector2(-24 * s, 2 * s), 14 * s, 4 * s, Hex("2EC4B6"));
        }
    }
}
