using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The mountain scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 4, Avalanche Peak: snowy pines
    /// and rocks, boulder fields, rock spires and slabs, cliffs and crevasses, the summit cross and the peak flag,
    /// avalanche fences, a mountain hut, the cave's crystals, stalagmites and rocks, a frozen waterfall, crowds,
    /// a yeti and a gondola cabin. Also the cave's glowing ice crystal and the stalagmite that stands in the way.
    /// Row 0 at the bottom, like the other winter sheets.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] MountainCells =
        {
            "SnowPine", "FrostTree", "SnowRock", "Boulders",
            "RockSpire", "Cliff", "Crevasse", "SnowDrift",
            "MountainHut", "Signpost", "SummitCross", "PeakFlag",
            "IceCrystals", "Stalagmites", "CaveRock", "FrozenFall",
            "WinterCrowd", "Snowman", "Yeti", "AvalancheFence",
            "Gondola", "FrostBush", "Igloo", "RockSlab",
        };

        const int MountainRows = 6;

        /// <summary>UV rect of a mountain scenery cell by name (row 0 at the bottom).</summary>
        public static Rect MountainCell(string name)
        {
            int i = System.Array.IndexOf(MountainCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)MountainRows, 0.25f, 1f / MountainRows);
        }

        [MenuItem("SomeGame/Generate Mountain Art")]
        public static void GenerateMountain()
        {
            Write(ArtGenerator.Folder, "SceneryMountain", 1024, 256 * MountainRows, MountainPixel, 128, false);
            ArtGenerator.Material("SceneryMountain", "SceneryMountain");
            Write(ArtGenerator.Folder, "IceCrystal", 128, 128, IceCrystalPixel, 128, false);
            Write(ArtGenerator.Folder, "Stalagmite", 128, 128, StalagmitePixel, 128, false);
            Write(ArtGenerator.Folder, "CaveBoulder", 128, 128, CaveBoulderPixel, 128, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color MountainPixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return MountainCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Boulders" => Boulders(lx, ly),
                "RockSpire" => RockSpire(lx, ly),
                "Cliff" => Cliff(lx, ly),
                "Crevasse" => Crevasse(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "MountainHut" => MountainHut(lx, ly),
                "Signpost" => Signpost(lx, ly),
                "SummitCross" => SummitCross(lx, ly),
                "PeakFlag" => PeakFlag(lx, ly),
                "IceCrystals" => IceCrystals(lx, ly),
                "Stalagmites" => Stalagmites(lx, ly),
                "CaveRock" => CaveRock(lx, ly),
                "FrozenFall" => FrozenFall(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "Snowman" => Snowman(lx, ly),
                "Yeti" => Yeti(lx, ly),
                "AvalancheFence" => AvalancheFence(lx, ly),
                "Gondola" => Gondola(lx, ly),
                "FrostBush" => FrostBush(lx, ly),
                "Igloo" => Igloo(lx, ly),
                _ => RockSlab(lx, ly),
            };
        }

        // A rock seen from above: lit from the top left, a snow cap on the top-left half.
        static Color SnowyRock(Color c, float x, float y, float cx, float cy, float r, int seed)
        {
            float n = (Noise(x * 0.06f + seed, y * 0.06f, 64) - 0.5f) * r * 0.5f;
            float rock = Circle(x, y, cx, cy, r) + n;
            c = Over(c, Shadow, Soft(Circle(x, y, cx + r * 0.25f, cy - r * 0.25f, r) + n, 8f));
            float light = Mathf.InverseLerp(cx + r, cx - r, (x + (cy * 2f - y)) * 0.5f);
            c = Over(c, Color.Lerp(RockDark, RockLight, light), Fill(rock));
            float cap = Mathf.Max(rock + r * 0.12f, Circle(x, y, cx - r * 0.35f, cy + r * 0.35f, r * 0.75f) + n * 0.6f);
            c = Over(c, Snow, Fill(cap));
            c = Over(c, SnowDeep, Stroke(cap, 2f) * Fill(rock) * 0.5f);
            return Over(c, Outline, Stroke(rock, 4f) * 0.85f);
        }

        // A field of boulders half buried in the snow.
        static Color Boulders(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = SnowyRock(c, x, y, 88, 150, 52, 1);
            c = SnowyRock(c, x, y, 176, 104, 46, 7);
            c = SnowyRock(c, x, y, 100, 66, 30, 13);
            c = SnowyRock(c, x, y, 190, 196, 26, 19);
            return c;
        }

        // A jagged spire of rock seen from above: facets falling away from a sharp snowy peak.
        static Color RockSpire(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float n = (Noise(x * 0.08f, y * 0.08f, 32) - 0.5f) * 22f;
            float base0 = Star(x, y, 128, 128, 110, 70, 7, 0.3f) + n;
            c = Over(c, Shadow, Soft(Star(x, y, 146, 110, 110, 70, 7, 0.3f) + n, 14f));
            float ang = Mathf.Atan2(y - 128, x - 128);
            int facet = (int)Mathf.Floor((ang + Mathf.PI) / (Mathf.PI / 3.5f));
            Color face = Color.Lerp(RockLight, RockDark, Mathf.Repeat(facet * 0.37f + (Mathf.Cos(ang + 2.4f) * 0.5f + 0.5f) * 0.6f, 1f));
            c = Over(c, face, Fill(base0));
            // Snow in the gullies and on the peak.
            float gully = Mathf.Abs(Mathf.Repeat(ang + Mathf.PI, Mathf.PI / 3.5f) - Mathf.PI / 7f);
            if (gully < 0.08f) c = Over(c, SnowShade, Fill(base0 + 4f) * 0.9f);
            c = Over(c, Snow, Fill(Star(x, y, 124, 132, 40, 22, 5, 0.1f)));
            c = Over(c, Outline, Stroke(base0, 4f) * 0.85f);
            return c;
        }

        // A flat slab of rock poking out of the snow, cracked, frosted.
        static Color RockSlab(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(30, 110), new Vector2(60, 190), new Vector2(150, 220), new Vector2(226, 170), new Vector2(214, 70), new Vector2(120, 34) };
            float slab = Polygon(x, y, pts) + (Noise(x * 0.1f, y * 0.1f, 16) - 0.5f) * 10f;
            c = Over(c, Shadow, Soft(Polygon(x, y, System.Array.ConvertAll(pts, p => p + new Vector2(10, -10))), 8f));
            c = Over(c, Color.Lerp(Rock, RockLight, Noise(x * 0.04f, y * 0.04f, 8)), Fill(slab));
            float crack = Mathf.Abs(y - 120f - Mathf.Sin(x * 0.06f) * 18f);
            c = Over(c, RockDark, Fill(crack - 1.5f) * Fill(slab + 6f));
            if (Noise(x / 40f, y / 40f, 8) > 0.58f) c = Over(c, Snow, Fill(slab + 3f) * 0.95f);
            c = Over(c, Outline, Stroke(slab, 4f) * 0.85f);
            return c;
        }

        // The summit cross on its cairn: a wooden cross from above, prayer flags strung from it.
        static Color SummitCross(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = SnowyRock(c, x, y, 128, 128, 62, 3);
            // Prayer flags in a fan from the top of the cross.
            Color[] flags = { Hex("3D7DD8"), Color.white, Hex("E2393F"), Hex("2E9A57"), Hex("F5C842") };
            for (int line = 0; line < 3; line++)
            {
                float a = (line * 120f + 30f) * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));
                for (int k = 1; k <= 5; k++)
                {
                    Vector2 p = new Vector2(128, 128) + dir * (40f + k * 15f);
                    var q = Local(x, y, p.x, p.y, a * Mathf.Rad2Deg, 1f);
                    float flag = RoundRect(q.x, q.y, 0, -5, 5, 6, 1);
                    c = Over(c, flags[(k + line) % 5], Fill(flag));
                    c = Over(c, Outline, Stroke(flag, 1.5f) * 0.6f);
                }
                c = Over(c, Outline, Fill(SegDist(new Vector2(x, y), new Vector2(128, 128), new Vector2(128, 128) + dir * 118f) - 1f) * 0.6f);
            }
            float beam = Mathf.Min(RoundRect(x, y, 128, 128, 10, 56, 3), RoundRect(x, y, 128, 146, 38, 9, 3));
            c = Over(c, Shadow, Soft(Mathf.Min(RoundRect(x, y, 140, 116, 10, 56, 3), RoundRect(x, y, 140, 134, 38, 9, 3)), 5f));
            c = Over(c, Hex("8A5A34"), Fill(beam));
            c = Over(c, Hex("B07A4A"), Fill(beam + 3f));
            c = Over(c, Outline, Stroke(beam, 3f));
            c = Over(c, Hex("F5C842"), Fill(Circle(x, y, 128, 146, 6)));
            return c;
        }

        // A big flag on a pole planted in a snow mound: the peak's flag, blowing out to the right.
        static Color PeakFlag(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Ellipse(x, y, 104, 108, 54, 40), 10f));
            float mound = Ellipse(x, y, 96, 118, 54, 40);
            c = Over(c, SnowShade, Fill(mound));
            c = Over(c, Snow, Fill(Ellipse(x, y, 88, 126, 40, 28)));
            c = Over(c, Outline, Stroke(mound, 3f) * 0.7f);
            float wave = Mathf.Sin((x - 96f) * 0.07f) * 8f;
            float flag = Polygon(x, y - wave, new[] { new Vector2(96, 156), new Vector2(226, 168), new Vector2(214, 132), new Vector2(226, 96), new Vector2(96, 104) });
            c = Over(c, Shadow, Soft(Polygon(x - 8, y - wave + 10, new[] { new Vector2(96, 156), new Vector2(226, 168), new Vector2(214, 132), new Vector2(226, 96), new Vector2(96, 104) }), 6f));
            bool stripe = Mathf.Repeat(y - wave - 104f, 52f) < 17f;
            c = Over(c, stripe ? Color.white : Hex("E2393F"), Fill(flag));
            c = Over(c, Outline, Stroke(flag, 3f) * 0.8f);
            c = Over(c, Hex("C9CED6"), Fill(Circle(x, y, 96, 130, 8)));
            c = Over(c, Outline, Stroke(Circle(x, y, 96, 130, 8), 2.5f));
            return c;
        }

        // A row of avalanche fences on the slope: timber frames holding back the snow, snow heaped behind.
        static Color AvalancheFence(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int row = 0; row < 2; row++)
            {
                float cy = 86f + row * 88f, sx = row * 24f;
                float drift = Ellipse(x, y, 128 + sx, cy + 16f, 104, 22);
                c = Over(c, SnowShade, Fill(drift) * 0.9f);
                c = Over(c, Snow, Fill(Ellipse(x, y, 124 + sx, cy + 22f, 92, 14)));
                float beam = RoundRect(x, y, 128 + sx, cy, 100, 5, 2);
                c = Over(c, Shadow, Soft(RoundRect(x, y, 136 + sx, cy - 8, 100, 5, 2), 4f));
                c = Over(c, Hex("7A4E2E"), Fill(beam));
                for (int k = -3; k <= 3; k++)
                {
                    float post = RoundRect(x, y, 128 + sx + k * 30f, cy - 8f, 4, 12, 2);
                    c = Over(c, Hex("5E3A22"), Fill(post));
                }
                c = Over(c, Outline, Stroke(beam, 2.5f) * 0.8f);
            }
            return c;
        }

        // The glowing ice crystal on the cave wall: pointed six-sided prisms growing out of one root, bright cyan.
        static Color IceCrystalPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(0.5f, 0.9f, 1f, 1f), Gauss(Dist(x, y, 64, 64), 24f) * 0.45f);
            var prisms = new[] { (100f, 50f, 11f), (40f, 40f, 9f), (160f, 44f, 10f), (215f, 34f, 8f), (280f, 38f, 9f), (330f, 30f, 7f) };
            foreach (var (deg, length, half) in prisms)
            {
                float a = deg * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a)), side = new(-dir.y, dir.x), root = new(64, 64);
                Vector2 tip = root + dir * length, shoulder = root + dir * (length - half * 1.4f);
                var pts = new[] { root - side * half * 0.7f, shoulder - side * half, tip, shoulder + side * half, root + side * half * 0.7f };
                float d = Polygon(x, y, pts);
                // Two faces: the lit one and the shaded one, split down the prism's middle.
                bool lit = Vector2.Dot(new Vector2(x, y) - root, side) * Mathf.Sign(Vector2.Dot(side, new Vector2(-1f, 1f))) > 0f;
                c = Over(c, lit ? Hex("E2FAFF") : Hex("5CC3EC"), Fill(d));
                c = Over(c, Color.white, Fill(SegDist(new Vector2(x, y), root + dir * 4f, tip - dir * 3f) - 1.2f) * Fill(d) * 0.8f);
                c = Over(c, Hex("1E5F86"), Stroke(d, 2.5f));
            }
            c = Over(c, Hex("A8E6FA"), Fill(Circle(x, y, 64, 64, 8)));
            return Over(c, Hex("1E5F86"), Stroke(Circle(x, y, 64, 64, 8), 2f));
        }

        // An ice stalagmite on the cave floor, seen from above: a jagged faceted cone, lit from the top left,
        // its point a little off-centre.
        static Color StalagmitePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float n = (Noise(x * 0.12f, y * 0.12f, 16) - 0.5f) * 6f;
            c = Over(c, Shadow, Soft(Star(x, y, 76, 52, 50, 38, 8, 0.2f) + n, 10f));
            float cone = Star(x, y, 64, 64, 50, 38, 8, 0.2f) + n;
            float a = Mathf.Atan2(y - 66, x - 60);
            int facet = (int)Mathf.Floor((a + Mathf.PI) / (Mathf.PI / 4f));
            float light = 0.5f + 0.5f * Mathf.Cos(facet * Mathf.PI / 4f + Mathf.PI / 8f - Mathf.PI - 2.36f);
            c = Over(c, Color.Lerp(Hex("4D7A99"), Hex("E8F4FB"), light), Fill(cone));
            if (Mathf.Abs(Mathf.Repeat(a + Mathf.PI, Mathf.PI / 4f)) < 0.05f) c = Over(c, Hex("2C5A78"), Fill(cone + 3f) * 0.7f);
            c = Over(c, Color.white, Fill(Circle(x, y, 60, 66, 5)));
            c = Over(c, Hex("1E3F5A"), Stroke(cone, 3.5f));
            return c;
        }

        // A chunk of rock heaped along the cave walls: angular facets lit from the top left, frost on the
        // top edges, a dark outline.
        static Color CaveBoulderPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(22, 62), new Vector2(36, 98), new Vector2(70, 112), new Vector2(104, 96), new Vector2(110, 58), new Vector2(88, 22), new Vector2(46, 20) };
            float n = (Noise(x * 0.1f, y * 0.1f, 16) - 0.5f) * 6f;
            c = Over(c, Shadow, Soft(Polygon(x, y, System.Array.ConvertAll(pts, p => p + new Vector2(7, -7))) + n, 8f));
            float rock = Polygon(x, y, pts) + n;
            // Facets: planes from a ridge point, each lit by how it faces the top left.
            Vector2 ridge = new(60, 70);
            float a = Mathf.Atan2(y - ridge.y, x - ridge.x);
            int facet = (int)Mathf.Floor((a + Mathf.PI) / (Mathf.PI * 2f / 6f));
            float facing = Mathf.Cos(facet * Mathf.PI / 3f + Mathf.PI / 6f - Mathf.PI - 2.36f) * 0.5f + 0.5f;
            Color face = Color.Lerp(RockDark, RockLight, facing * 0.85f + Noise(x * 0.05f, y * 0.05f, 8) * 0.15f);
            c = Over(c, face, Fill(rock));
            float crease = Mathf.Abs(Mathf.Repeat(a + Mathf.PI, Mathf.PI / 3f) - Mathf.PI / 6f);
            if (crease > Mathf.PI / 6f - 0.04f) c = Over(c, Shade(RockDark, 0.8f), Fill(rock + 3f) * 0.6f);
            // Frost and a little snow on the upper-left faces.
            if (facing > 0.7f && Noise(x * 0.08f, y * 0.08f, 16) > 0.55f) c = Over(c, SnowShade, Fill(rock + 4f) * 0.8f);
            return Over(c, Outline, Stroke(rock, 4f) * 0.9f);
        }
    }
}
