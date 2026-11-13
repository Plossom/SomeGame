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

        public enum IslandLook { Jungle, Sand, Rock, Cliff }

        // Islands in the sea (painting space): centre, radii and look. Each sits in a bend of the lane (or by the
        // edge) and is shrunk until it keeps clear of the lane.
        static readonly (Vector2 c, Vector2 r, IslandLook look)[] SeaIslandPlan =
        {
            (new Vector2(700f, 495f), new Vector2(150f, 52f), IslandLook.Jungle),
            (new Vector2(420f, 905f), new Vector2(175f, 62f), IslandLook.Jungle),
            (new Vector2(760f, 1250f), new Vector2(115f, 34f), IslandLook.Jungle),
            (new Vector2(30f, 610f), new Vector2(130f, 56f), IslandLook.Cliff),
            (new Vector2(1130f, 880f), new Vector2(110f, 46f), IslandLook.Jungle),
            (new Vector2(150f, 1262f), new Vector2(90f, 26f), IslandLook.Sand),
            (new Vector2(1000f, 1065f), new Vector2(64f, 20f), IslandLook.Rock),
            (new Vector2(440f, 1372f), new Vector2(70f, 16f), IslandLook.Sand),
            (new Vector2(1090f, 380f), new Vector2(80f, 32f), IslandLook.Rock),
        };

        static readonly List<(Vector2 c, Vector2 r, IslandLook look)> SeaIslands = new();

        // Lane clearance of an ellipse: the least distance from its rim to the lane's edge.
        static float LaneClearance(Vector2 c, Vector2 r)
        {
            float best = float.MaxValue;
            for (int k = 0; k < 28; k++)
            {
                float a = k / 28f * Mathf.PI * 2f;
                var q = c + new Vector2(Mathf.Cos(a) * r.x, Mathf.Sin(a) * r.y);
                best = Mathf.Min(best, RoadDistance(q) - RoadWidth(q.y) * 0.5f);
            }
            return best;
        }

        static void PlanIslands()
        {
            SeaIslands.Clear();
            foreach (var (c, r0, look) in SeaIslandPlan)
            {
                var r = r0;
                for (int i = 0; i < 30 && LaneClearance(c, r + new Vector2(30f, 14f)) < 16f; i++) r *= 0.93f;
                if (r.x > 30f) SeaIslands.Add((c, r, look));
            }
        }

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
                PlanIslands();
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
            // The beach along the bottom right (the lane comes in over open water on the left): wet sand, a foam
            // line, dry sand; the shore curves away out of the picture to the left.
            float shore = y - (110f + 24f * Mathf.Sin(x / 95f + 1f) + 10f * Mathf.Sin(x / 37f) - Mathf.Pow(Mathf.Max(0f, 520f - x) / 520f, 1.6f) * 300f);
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
            foreach (var (centre, r, look) in SeaIslands)
            {
                // Pale shallows, a ring of foam, the beach, the green (or the rock) on top.
                Ellipse(centre + new Vector2(0f, -4f), r.x + 34f, r.y + 14f, new Color(0.6f, 0.95f, 0.88f, 0.75f));
                Ellipse(centre, r.x + 8f, r.y + 4f, new Color(1f, 1f, 1f, 0.9f));
                Ellipse(centre, r.x, r.y, look == IslandLook.Rock ? Hex("9AA5AB") : Hex("F3DDA8"));
                if (look == IslandLook.Jungle || look == IslandLook.Cliff)
                {
                    Ellipse(centre + new Vector2(0f, r.y * 0.25f), r.x * 0.74f, r.y * 0.64f, Hex("3E9B4F"));
                    Ellipse(centre + new Vector2(-r.x * 0.15f, r.y * 0.4f), r.x * 0.42f, r.y * 0.32f, Hex("5BB862"));
                }
                if (look == IslandLook.Rock)
                    for (int k = 0; k < 4; k++)
                    {
                        var q = centre + new Vector2((k - 1.5f) * r.x * 0.42f, r.y * (0.1f + 0.25f * (k % 2)));
                        Ellipse(q, r.x * 0.2f, r.y * 0.55f, k % 2 == 0 ? Hex("7E8A90") : Hex("B4BDC2"));
                    }
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
            // Clear of the lane, its stops and what is there already.
            bool Fits(Vector2 p, float hw, float h, float r)
            {
                if (WaterBlocked(p, hw, h)) return false;
                for (int i = 0; i < MapLayout.StopCount; i++) if (Vector2.Distance(p, Unstretch(MapLayout.StopPosition(i))) < 95f + r) return false;
                return Free(p, r);
            }
            void Put(Vector2 p, float r, Action draw) { occupied.Add((p, r)); items.Add((p.y, draw)); }

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

            var rng = new System.Random(31);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // On the islands: palms (the jungle ones crowded), huts, a lighthouse, a waterfall off the cliff.
            foreach (var (centre, r, look) in SeaIslands)
            {
                if (look == IslandLook.Cliff)
                {
                    var top = centre + new Vector2(r.x * 0.2f, r.y * 0.3f);
                    items.Add((centre.y + 1f, () => CliffFall(top, SizeAt(centre.y) * 1.4f)));
                }
                if (look == IslandLook.Rock && centre.y > 1000f)
                {
                    var lh = centre + new Vector2(0f, r.y * 0.3f);
                    Put(lh, 40f, () => MapLighthouse(lh, SizeAt(lh.y) * 1.5f));
                }
                int palms = look switch { IslandLook.Jungle => Mathf.RoundToInt(r.x / 16f), IslandLook.Cliff => 3, IslandLook.Sand => 2, _ => 0 };
                for (int k = 0; k < palms * 8 && palms > 0; k++)
                {
                    var p = centre + new Vector2(Range(-r.x * 0.75f, r.x * 0.75f), Range(-r.y * 0.3f, r.y * 0.55f));
                    if (Sdf.Ellipse(p.x, p.y, centre.x, centre.y, r.x * 0.8f, r.y * 0.8f) > 0f) continue;
                    float s = SizeAt(p.y) * Range(0.8f, 1.15f);
                    if (!Fits(p, 26f * s, 90f * s, 18f * s)) continue;
                    palms--;
                    bool lean = rng.NextDouble() < 0.5;
                    Put(p, 18f * s, () => MapPalm(p, s, lean));
                }
                if (look == IslandLook.Jungle && r.x > 90f)
                {
                    var hut = centre + new Vector2(r.x * 0.62f, -r.y * 0.55f);
                    float s = SizeAt(hut.y);
                    if (Fits(hut, 34f * s, 80f * s, 30f * s)) Put(hut, 30f * s, () => StiltHut(hut, s));
                }
            }

            // Out on the water: sailboats, kayaks, a seaplane, a floating bar, rocks, reefs, the wreck, a sandbar
            // with flamingos, and a jetty off the beach with its boats.
            (Vector2 p, Action<Vector2, float> draw, float size, float r)[] things =
            {
                (new Vector2(560f, 1300f), (q, s) => Sailboat(q, s), 0.85f, 26f),
                (new Vector2(220f, 1330f), (q, s) => Sailboat(q, s), 0.9f, 26f),
                (new Vector2(1080f, 960f), (q, s) => Sailboat(q, s), 1.05f, 30f),
                (new Vector2(60f, 1180f), (q, s) => Sailboat(q, s), 0.95f, 26f),
                (new Vector2(860f, 940f), (q, s) => Seaplane(q, s), 1.1f, 40f),
                (new Vector2(300f, 470f), (q, s) => FloatingBar(q, s), 1.1f, 36f),
                (new Vector2(1000f, 230f), (q, s) => Kayaks(q, s), 1f, 28f),
                (new Vector2(370f, 565f), (q, s) => Sandbar(q, s), 1.15f, 48f),
                (new Vector2(900f, 1010f), (q, s) => Wreck(q, s), 1.2f, 50f),
                (new Vector2(1060f, 1160f), (q, s) => Rocks(q, s), 1f, 22f),
                (new Vector2(320f, 1240f), (q, s) => Rocks(q, s), 0.85f, 20f),
                (new Vector2(1080f, 200f), (q, s) => Reef(q, s), 1.2f, 40f),
                (new Vector2(120f, 720f), (q, s) => Reef(q, s), 1f, 36f),
                (new Vector2(990f, 790f), (q, s) => Reef(q, s), 1f, 36f),
                (new Vector2(700f, 860f), (q, s) => Reef(q, s), 0.9f, 32f),
            };
            foreach (var (p, draw, size, r) in things)
            {
                float s = SizeAt(p.y) * size;
                if (!Fits(p, r * s * 0.8f, r * s * 1.5f, r * s)) continue;
                Put(p, r * s, () => draw(p, s));
            }
            var jetty = new Vector2(980f, 105f);
            items.Add((jetty.y, () => Jetty(jetty, 1.5f)));

            // The beach in front (bottom right): palms, umbrellas and a towel or two.
            foreach (var (p, s) in new[] { (new Vector2(1150f, 50f), 2.3f), (new Vector2(820f, 20f), 1.7f), (new Vector2(620f, 15f), 1.4f) })
                items.Add((p.y, () => MapPalm(p, s, false)));
            foreach (var p in new[] { new Vector2(700f, 55f), new Vector2(900f, 75f), new Vector2(1060f, 30f) })
                items.Add((p.y, () => BeachUmbrella(p, SizeAt(p.y) * 1.4f)));

            // The horizon: a chain of little atolls with palms all along it, like the far forests of the other maps.
            var atolls = new System.Random(5);
            for (float ax = -20f; ax < 1200f; ax += 95f + (float)atolls.NextDouble() * 50f)
            {
                var c = new Vector2(ax, Horizon - 22f - (float)atolls.NextDouble() * 16f);
                float rx = 34f + (float)atolls.NextDouble() * 26f;
                int count = 2 + atolls.Next(3);
                var tops = new List<(Vector2, bool)>();
                for (int k = 0; k < count; k++) tops.Add((c + new Vector2((k - (count - 1) * 0.5f) * rx * 0.5f + (float)atolls.NextDouble() * 6f, 1f), atolls.NextDouble() < 0.5));
                items.Add((c.y + 1f, () =>
                {
                    Ellipse(c + new Vector2(0f, -1f), rx + 12f, 6f, new Color(0.6f, 0.95f, 0.88f, 0.7f));
                    Ellipse(c, rx + 3f, 4.5f, new Color(1f, 1f, 1f, 0.9f));
                    Ellipse(c, rx, 3.6f, Hex("F3DDA8"));
                    Ellipse(c + new Vector2(0f, 2f), rx * 0.7f, 2.6f, Hex("3E9B4F"));
                }));
                foreach (var (p, lean) in tops) items.Add((p.y, () => MapPalm(p, SizeAt(p.y) * 0.62f, lean)));
            }

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

        // A green cliff with a waterfall pouring into the sea, mist at its foot.
        static void CliffFall(Vector2 p, float s)
        {
            Poly(new[] { new Vector2(p.x - 50 * s, p.y), new Vector2(p.x + 40 * s, p.y), new Vector2(p.x + 30 * s, p.y + 70 * s), new Vector2(p.x - 10 * s, p.y + 92 * s), new Vector2(p.x - 46 * s, p.y + 64 * s) }, Hex("6E7E78"));
            Poly(new[] { new Vector2(p.x - 46 * s, p.y + 64 * s), new Vector2(p.x - 10 * s, p.y + 92 * s), new Vector2(p.x + 30 * s, p.y + 70 * s), new Vector2(p.x + 20 * s, p.y + 82 * s), new Vector2(p.x - 14 * s, p.y + 104 * s), new Vector2(p.x - 44 * s, p.y + 78 * s) }, Hex("3E9B4F"));
            // The fall: white streaks down the face, a pool of foam and mist below.
            Shape(Box(p + new Vector2(0, 45 * s), 50 * s), (x, y) => RoundRect(x, y, p.x - 4 * s, p.y + 46 * s, 9 * s, 44 * s, 4 * s), new Color(0.78f, 0.95f, 1f, 0.95f));
            for (int i = 0; i < 4; i++)
            {
                float fx = p.x - 10 * s + i * 4 * s;
                Shape(Box(p + new Vector2(0, 45 * s), 50 * s), (x, y) => Capsule(x, y, fx, p.y + 88 * s, fx, p.y + 4 * s, 0.9f * s), Color.white);
            }
            Ellipse(p + new Vector2(-4 * s, 2 * s), 26 * s, 8 * s, new Color(1f, 1f, 1f, 0.85f));
            Ellipse(p + new Vector2(-4 * s, 10 * s), 32 * s, 12 * s, new Color(1f, 1f, 1f, 0.35f));
            Poly(new[] { new Vector2(p.x - 50 * s, p.y), new Vector2(p.x + 40 * s, p.y), new Vector2(p.x + 30 * s, p.y + 70 * s), new Vector2(p.x - 10 * s, p.y + 92 * s), new Vector2(p.x - 46 * s, p.y + 64 * s) }, SeaLine, 2f);
        }

        static void Seaplane(Vector2 p, float s)
        {
            Ellipse(p, 46 * s, 7 * s, new Color(1f, 1f, 1f, 0.6f));
            foreach (float fx in new[] { -16f, 16f }) Rect(p + new Vector2(fx * s, 4 * s), 22 * s, 3 * s, Color.white);
            Rect(p + new Vector2(0, 16 * s), 30 * s, 7 * s, Hex("FFC93C"));
            Rect(p + new Vector2(0, 26 * s), 52 * s, 3 * s, Hex("FF6B52"));
            Poly(new[] { new Vector2(p.x - 30 * s, p.y + 16 * s), new Vector2(p.x - 44 * s, p.y + 34 * s), new Vector2(p.x - 36 * s, p.y + 16 * s) }, Hex("FF6B52"));
            Rect(p + new Vector2(14 * s, 19 * s), 7 * s, 3 * s, Hex("8FDDFF"));
            Shape(Box(p + new Vector2(32 * s, 16 * s), 10 * s), (x, y) => Capsule(x, y, p.x + 32 * s, p.y + 8 * s, p.x + 32 * s, p.y + 24 * s, 1.4f * s), Hex("2A2F36"));
        }

        static void FloatingBar(Vector2 p, float s)
        {
            Ellipse(p, 40 * s, 9 * s, new Color(1f, 1f, 1f, 0.6f));
            Rect(p + new Vector2(0, 5 * s), 34 * s, 5 * s, Hex("A57A4C"));
            Rect(p + new Vector2(0, 14 * s), 14 * s, 6 * s, Hex("C99A4C"));
            BeachUmbrella(p + new Vector2(14 * s, 8 * s), s * 0.75f);
            foreach (float fx in new[] { -24f, -12f }) Disc(p + new Vector2(fx * s, 12 * s), 3 * s, Hex("FF6B52"));
        }

        static void Kayaks(Vector2 p, float s)
        {
            for (int i = 0; i < 2; i++)
            {
                var q = p + new Vector2(i * 30 * s, i * 10 * s);
                Ellipse(q, 20 * s, 4 * s, new Color(1f, 1f, 1f, 0.55f));
                Shape(Box(q + new Vector2(0, 4 * s), 24 * s), (x, y) => Sdf.Ellipse(x, y, q.x, q.y + 4 * s, 20 * s, 3.5f * s), i == 0 ? Hex("FFC93C") : Hex("2EC4B6"));
                Disc(q + new Vector2(0, 10 * s), 4 * s, Hex("F2C6A0"));
                Shape(Box(q + new Vector2(0, 10 * s), 18 * s), (x, y) => Capsule(x, y, q.x - 14 * s, q.y + 4 * s, q.x + 14 * s, q.y + 14 * s, 1.1f * s), Hex("2A2F36"));
            }
        }

        static void Sandbar(Vector2 p, float s)
        {
            Ellipse(p + new Vector2(0, -3 * s), 74 * s, 16 * s, new Color(0.6f, 0.95f, 0.88f, 0.8f));
            Ellipse(p, 60 * s, 10 * s, new Color(1f, 1f, 1f, 0.9f));
            Ellipse(p, 54 * s, 8 * s, Hex("F6E2B2"));
            // Flamingos standing on it.
            foreach (float fx in new[] { -26f, -6f, 16f })
            {
                var q = p + new Vector2(fx * s, 2 * s);
                Shape(Box(q + new Vector2(0, 10 * s), 10 * s), (x, y) => Capsule(x, y, q.x, q.y, q.x, q.y + 10 * s, 0.7f * s), Hex("E86AA6"));
                Ellipse(q + new Vector2(0, 14 * s), 6 * s, 4 * s, Hex("FF8FB8"));
                Shape(Box(q + new Vector2(4 * s, 20 * s), 10 * s), (x, y) => Capsule(x, y, q.x + 3 * s, q.y + 15 * s, q.x + 5 * s, q.y + 24 * s, 1.1f * s), Hex("FF8FB8"));
                Disc(q + new Vector2(5 * s, 25 * s), 2.2f * s, Hex("FF8FB8"));
            }
        }

        static void Rocks(Vector2 p, float s)
        {
            Ellipse(p, 30 * s, 7 * s, new Color(1f, 1f, 1f, 0.65f));
            Ellipse(p + new Vector2(-8 * s, 8 * s), 16 * s, 12 * s, Hex("8E9AA0"));
            Ellipse(p + new Vector2(10 * s, 5 * s), 12 * s, 8 * s, Hex("6E7A80"));
            Ellipse(p + new Vector2(-12 * s, 12 * s), 7 * s, 5 * s, Hex("B4BDC2"));
        }

        static void Reef(Vector2 p, float s)
        {
            // Under the water: a soft patch of colourful coral.
            Ellipse(p, 50 * s, 16 * s, new Color(0.65f, 0.95f, 0.88f, 0.55f));
            var rng = new System.Random((int)(p.x * 7 + p.y));
            for (int i = 0; i < 14; i++)
            {
                var q = p + new Vector2((float)(rng.NextDouble() - 0.5) * 80 * s, (float)(rng.NextDouble() - 0.5) * 22 * s);
                Color c = (i % 4) switch { 0 => Hex("FF8A4C"), 1 => Hex("E86AA6"), 2 => Hex("9A6AD8"), _ => Hex("FFD24D") };
                Ellipse(q, 6 * s, 3.5f * s, new Color(c.r, c.g, c.b, 0.7f));
            }
        }

        static void Jetty(Vector2 p, float s)
        {
            // Planks out from the beach, posts, a speedboat and a rowing boat tied up beside it.
            Rect(p + new Vector2(0, 60 * s), 9 * s, 62 * s, Hex("A57A4C"));
            for (int i = 0; i < 9; i++) Rect(p + new Vector2(0, 4 * s + i * 14 * s), 9 * s, 0.8f * s, Hex("6B4A2E"));
            foreach (float fx in new[] { -10f, 10f })
                for (int i = 0; i < 4; i++) Rect(p + new Vector2(fx * s, 10 * s + i * 32 * s), 1.6f * s, 4 * s, Hex("4A2E18"));
            var b = p + new Vector2(26 * s, 70 * s);
            Ellipse(b, 20 * s, 4 * s, new Color(1f, 1f, 1f, 0.6f));
            Poly(new[] { new Vector2(b.x - 18 * s, b.y + 6 * s), new Vector2(b.x + 22 * s, b.y + 6 * s), new Vector2(b.x + 14 * s, b.y), new Vector2(b.x - 14 * s, b.y) }, Hex("FF6B52"));
            Rect(b + new Vector2(2 * s, 9 * s), 6 * s, 3 * s, Hex("8FDDFF"));
            var r = p + new Vector2(-24 * s, 40 * s);
            Ellipse(r, 14 * s, 3 * s, new Color(1f, 1f, 1f, 0.6f));
            Poly(new[] { new Vector2(r.x - 13 * s, r.y + 5 * s), new Vector2(r.x + 13 * s, r.y + 5 * s), new Vector2(r.x + 9 * s, r.y), new Vector2(r.x - 9 * s, r.y) }, Hex("8A5A32"));
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
