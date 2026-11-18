using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The art of the Water Cup's later races (4-8), seen from above: the jungle river's crocodiles, temple ruins, stone
    /// doors and the waterfall cliff; Pirate Bay's ship, skull rock, cannonballs, target rings, coins, plank ramps and a
    /// treasure chest; the reef's jellyfish, manta rays and coral arch; the storm's fortress and lightning bolts.
    /// </summary>
    public static partial class WaterArt
    {
        [MenuItem("SomeGame/Generate Wild Water Art")]
        public static void GenerateWild()
        {
            string f = ArtGenerator.Folder;
            Write(f, "Crocodile", 128, 256, CrocodilePixel, 48, false);
            Write(f, "TempleRuin", 256, 256, TempleRuinPixel, 28, false);
            Write(f, "TempleStatue", 128, 128, TempleStatuePixel, 40, false);
            Write(f, "TempleArch", 512, 192, TempleArchPixel, 36, false);
            Write(f, "StoneDoor", 256, 64, StoneDoorPixel, 48, false);
            Write(f, "JungleFalls", 512, 512, JungleFallsPixel, 20, false);
            Write(f, "PirateShip", 256, 640, PirateShipPixel, 22, false);
            Write(f, "SkullRock", 512, 320, SkullRockPixel, 30, false);
            Write(f, "Cannonball", 64, 64, CannonballPixel, 64, false);
            Write(f, "TargetRing", 128, 128, TargetRingPixel, 48, false);
            Write(f, "Coin", 64, 64, CoinPixel, 80, false);
            Write(f, "PlankRamp", 192, 256, PlankRampPixel, 48, false);
            Write(f, "TreasureChest", 128, 96, TreasureChestPixel, 40, false);
            Write(f, "Jellyfish", 96, 96, JellyfishPixel, 64, false);
            Write(f, "MantaRay", 256, 192, MantaRayPixel, 40, false);
            Write(f, "CoralArch", 512, 192, CoralArchPixel, 36, false);
            Write(f, "Fortress", 640, 448, FortressPixel, 24, false);
            Write(f, "Bolt", 64, 256, BoltPixel, 32, false);
            Write(f, "LogJam", 320, 640, LogJamPixel, 28, false);
            Write(f, "SceneryForest", 1024, 1024, ForestCell, 128, false);
            ArtGenerator.Material("SceneryForest", "SceneryForest");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static readonly Color Stone = Hex("A39E8F"), StoneDark = Hex("6E6A60"), StoneLight = Hex("C9C3B2"), Moss = Hex("5E9A3A"), MossDark = Hex("3F7428");
        static readonly Color Wood = Hex("8A5A32"), WoodDark = Hex("5C3A1E"), WoodLight = Hex("B07A47"), Gold = Hex("FFC83D"), GoldDark = Hex("C98A12");

        static float Mossy(float x, float y) => Noise(x * 0.06f, y * 0.06f, 64);

        // ------------------------------------------------------------------ jungle

        static Color CrocodilePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color skin = Hex("4F8A3A"), dark = Hex("2F5E24"), belly = Hex("A9C76A");
            float body = Ellipse(x, y, 64, 128, 26, 58);
            float head = Mathf.Min(Ellipse(x, y, 64, 200, 18, 34), RoundRect(x, y, 64, 226, 11, 22, 10));
            float tail = Polygon(x, y, new[] { new Vector2(44, 90), new Vector2(84, 90), new Vector2(68, 6), new Vector2(60, 6) });
            float legs = Min(Ellipse(x, y, 34, 160, 12, 7), Ellipse(x, y, 94, 160, 12, 7), Ellipse(x, y, 34, 96, 12, 7), Ellipse(x, y, 94, 96, 12, 7));
            float all = Min(body, head, tail, legs);
            c = Over(c, new Color(0f, 0.1f, 0.05f, 0.35f), Fill(all - 4f) * (1f - Fill(all)));
            c = Over(c, skin, Fill(all));
            c = Over(c, belly, Fill(Ellipse(x, y, 64, 128, 12, 40)) * 0.25f);
            for (int i = 0; i < 9; i++)
            {
                float sy = 40 + i * 18;
                c = Over(c, dark, Fill(RoundRect(x, y, 56, sy, 4, 5, 2)) + Fill(RoundRect(x, y, 72, sy, 4, 5, 2)));
            }
            c = Over(c, Hex("FFE066"), Fill(Circle(x, y, 54, 206, 5)) + Fill(Circle(x, y, 74, 206, 5)));
            c = Over(c, Line, Fill(Ellipse(x, y, 54, 206, 1.4f, 4)) + Fill(Ellipse(x, y, 74, 206, 1.4f, 4)));
            c = Over(c, Color.white, Stroke(SegDist(new Vector2(x, y), new Vector2(52, 232), new Vector2(56, 246)), 2f) + Stroke(SegDist(new Vector2(x, y), new Vector2(76, 232), new Vector2(72, 246)), 2f));
            return Over(c, Line, Stroke(all, 3f));
        }

        static Color TempleRuinPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // A stepped pyramid seen from above: four tiers, stairs up the middle, moss, a shrine on top.
            for (int tier = 0; tier < 4; tier++)
            {
                float half = 118 - tier * 26;
                float d = RoundRect(x, y, 128, 128, half, half, 6);
                Color s = Color.Lerp(StoneDark, StoneLight, tier / 3f);
                if (Mathf.Repeat(x + y * 0.3f, 22f) < 2f) s = Shade(s, 0.85f);
                c = Over(c, s, Fill(d));
                c = Over(c, Shade(s, 0.7f), Stroke(d, 3f) * Fill(d + 3f));
            }
            float stairs = RoundRect(x, y, 128, 60, 18, 58, 2);
            c = Over(c, Mathf.Repeat(y, 10f) < 3f ? StoneDark : StoneLight, Fill(stairs));
            float shrine = RoundRect(x, y, 128, 128, 22, 22, 4);
            c = Over(c, Hex("8C5B3C"), Fill(shrine));
            c = Over(c, Hex("3A2416"), Fill(RoundRect(x, y, 128, 124, 9, 12, 3)));
            float moss = Mossy(x, y);
            c = Over(c, Color.Lerp(MossDark, Moss, moss), Fill(RoundRect(x, y, 128, 128, 118, 118, 6)) * Mathf.Clamp01((moss - 0.7f) * 6f) * 0.8f);
            return Over(c, Line, Stroke(RoundRect(x, y, 128, 128, 118, 118, 6), 3f));
        }

        static Color TempleStatuePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float head = Circle(x, y, 64, 64, 50);
            c = Over(c, new Color(0, 0, 0, 0.3f), Fill(Circle(x, y, 70, 58, 52)) * (1f - Fill(head)));
            c = Over(c, Stone, Fill(head));
            c = Over(c, StoneLight, Fill(Circle(x, y, 54, 76, 30)) * 0.5f);
            c = Over(c, StoneDark, Fill(RoundRect(x, y, 46, 74, 10, 6, 3)) + Fill(RoundRect(x, y, 82, 74, 10, 6, 3)));
            c = Over(c, StoneDark, Fill(RoundRect(x, y, 64, 44, 20, 6, 3)));
            c = Over(c, Hex("37C6C0"), Fill(Circle(x, y, 46, 74, 3.5f)) + Fill(Circle(x, y, 82, 74, 3.5f)));
            float moss = Mossy(x * 1.7f, y * 1.7f);
            c = Over(c, Moss, Fill(head) * Mathf.Clamp01((moss - 0.72f) * 6f));
            return Over(c, Line, Stroke(head, 3f));
        }

        // A stone gateway over the river: a pillar on each bank, a carved lintel across, vines hanging down.
        static Color TempleArchPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float lintel = RoundRect(x, y, 256, 96, 250, 34, 6);
            float pillars = Mathf.Min(RoundRect(x, y, 36, 96, 34, 90, 6), RoundRect(x, y, 476, 96, 34, 90, 6));
            float all = Mathf.Min(lintel, pillars);
            c = Over(c, Mathf.Repeat(x, 36f) < 3f ? StoneDark : Stone, Fill(all));
            c = Over(c, StoneLight, Fill(RoundRect(x, y, 256, 110, 240, 10, 3)) * Fill(lintel));
            for (int i = 0; i < 7; i++)
            {
                float gx = 110 + i * 48;
                c = Over(c, StoneDark, Stroke(Circle(x, y, gx, 92, 12), 3f) * Fill(lintel));
                c = Over(c, Hex("37C6C0"), Fill(Circle(x, y, gx, 92, 4)) * Fill(lintel));
            }
            float moss = Mossy(x, y);
            c = Over(c, Moss, Fill(all) * Mathf.Clamp01((moss - 0.7f) * 5f));
            for (int i = 0; i < 9; i++)
            {
                float vx = 70 + i * 46 + Hash(i, 3) * 20;
                c = Over(c, MossDark, Stroke(SegDist(new Vector2(x, y), new Vector2(vx, 62), new Vector2(vx + Mathf.Sin(i) * 6, 20 + Hash(i, 5) * 20)), 4f));
                c = Over(c, Moss, Fill(Circle(x, y, vx + Mathf.Sin(i) * 6, 20 + Hash(i, 5) * 20, 5)));
            }
            return Over(c, Line, Stroke(all, 3f));
        }

        // Logs jammed across the river (seen from above, lying along the long axis), branches sticking out.
        static Color LogJamPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float all = float.MaxValue;
            for (int i = 0; i < 7; i++)
            {
                float lx = 50 + i * 37 + (Hash(i, 1) - 0.5f) * 10, tilt = (Hash(i, 2) - 0.5f) * 0.12f;
                float log = RotRect(x, y, lx, 320 + (Hash(i, 3) - 0.5f) * 40, 17, 290 - Hash(i, 4) * 40, 16, tilt);
                all = Mathf.Min(all, log);
                Color bark = Color.Lerp(Hex("6B4426"), Hex("8C5E36"), Hash(i, 5));
                c = Over(c, Mathf.Repeat(y + i * 13, 26f) < 3f ? Shade(bark, 0.75f) : bark, Fill(log));
                c = Over(c, Hex("C9A57A"), Fill(Ellipse(x, y, lx, 320 + (Hash(i, 3) - 0.5f) * 40 + 270 - Hash(i, 4) * 40, 12, 7)) * Fill(log + 2f));
                c = Over(c, Line, Stroke(log, 2.5f));
            }
            for (int i = 0; i < 6; i++)
            {
                Vector2 from = new(60 + Hash(i, 7) * 200, 80 + Hash(i, 8) * 480), to = from + new Vector2(30 + Hash(i, 9) * 30, 20 - Hash(i, 6) * 40);
                c = Over(c, Hex("4E3220"), Stroke(SegDist(new Vector2(x, y), from, to), 5f));
                c = Over(c, Moss, Fill(Circle(x, y, to.x, to.y, 9)));
            }
            return Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(all - 6f) * (1f - Fill(all)) );
        }

        static Color StoneDoorPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float slab = RoundRect(x, y, 128, 32, 124, 28, 4);
            c = Over(c, Mathf.Repeat(x, 40f) < 3f ? StoneDark : Stone, Fill(slab));
            c = Over(c, StoneLight, Fill(RoundRect(x, y, 128, 40, 118, 8, 3)) * Fill(slab));
            for (int i = 0; i < 4; i++) c = Over(c, StoneDark, Stroke(Circle(x, y, 40 + i * 58, 30, 10), 3f));
            return Over(c, Line, Stroke(slab, 3f));
        }

        // The head of the river: a jungle cliff with a waterfall pouring into a pool, mist round its foot (scenery).
        static Color JungleFallsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float cliff = Mathf.Min(RoundRect(x, y, 256, 360, 250, 150, 60), Ellipse(x, y, 256, 300, 240, 110));
            float n = Noise(x * 0.03f, y * 0.03f, 64);
            c = Over(c, Color.Lerp(Hex("5D6B5A"), Hex("7E8C78"), n), Fill(cliff));
            c = Over(c, Color.Lerp(MossDark, Moss, n), Fill(cliff) * Mathf.Clamp01((n - 0.45f) * 4f));
            // The fall: a bright ribbon down the cliff, darker stripes of water in it.
            float fall = RoundRect(x, y, 256, 330, 54, 170, 20);
            float stripe = 0.85f + 0.15f * Mathf.Sin(x * 0.35f + Noise(x * 0.1f, y * 0.02f, 32) * 4f);
            c = Over(c, Color.Lerp(Hex("8FD8F0"), Color.white, stripe), Fill(fall));
            // The pool at its foot and the mist.
            float pool = Ellipse(x, y, 256, 140, 150, 80);
            c = Over(c, Hex("3FAFCB"), Fill(pool));
            c = Over(c, White(0.85f), Gauss(Ellipse(x, y, 256, 168, 90, 34), 18f));
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                c = Over(c, White(0.7f), Fill(Circle(x, y, 256 + Mathf.Cos(a) * 100, 150 + Mathf.Sin(a) * 50, 7 + Hash(i, 2) * 6)));
            }
            return c;
        }

        // ------------------------------------------------------------------ the forest sheet

        static Color ForestCell(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return SomeGame.Track.ForestSheet.Cells[row * 4 + col] switch
            {
                "Oak" => Crown(lx, ly, 96, Hex("3E8A2E"), Hex("5FB13F"), 1),
                "OakBig" => Crown(lx, ly, 118, Hex("2F7A2A"), Hex("4FA03A"), 2),
                "Pine" => Pine(lx, ly, Hex("1F5E3A"), Hex("2F7F4C")),
                "PineDark" => Pine(lx, ly, Hex("174A30"), Hex("23663F")),
                "Birch" => Crown(lx, ly, 80, Hex("6FA83A"), Hex("A8D45A"), 3),
                "Bush" => Crown(lx, ly, 60, Hex("3F8A35"), Hex("6DBA4A"), 4),
                "Fern" => Fern(lx, ly),
                "Mushrooms" => Mushrooms(lx, ly),
                "Stump" => Stump(lx, ly),
                "Log" => ForestLog(lx, ly),
                "Rock" => MossRock(lx, ly),
                "Flowers" => Flowerbed(lx, ly),
                "Reeds" => Reeds(lx, ly),
                "LilyPads" => LilyPads(lx, ly),
                "Willow" => Willow(lx, ly),
                _ => new Color(0, 0, 0, 0),
            };
        }

        // A tree crown from above: a lumpy round mass of leaves, lit from the top left, a shadow to the bottom right.
        static Color Crown(float x, float y, float r, Color dark, Color light, int seed)
        {
            Color c = new(0, 0, 0, 0);
            float crown = float.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f + seed;
                float rr = r * (0.42f + 0.12f * Hash(i, seed));
                crown = SMin(crown, Circle(x, y, 128 + Mathf.Cos(a) * r * 0.5f, 128 + Mathf.Sin(a) * r * 0.5f, rr), 18f);
            }
            crown = SMin(crown, Circle(x, y, 128, 128, r * 0.62f), 18f);
            c = Over(c, new Color(0f, 0.08f, 0.02f, 0.4f), Fill(crown - 10f) * (1f - Fill(crown)));
            float lit = Mathf.Clamp01(0.5f + ((128 - x) + (y - 128)) / (r * 2.2f));
            float n = Noise(x * 0.08f + seed, y * 0.08f, 32);
            c = Over(c, Color.Lerp(dark, light, lit * 0.8f + n * 0.3f), Fill(crown));
            for (int i = 0; i < 14; i++)
            {
                float bx = 128 + (Hash(i, seed + 3) - 0.5f) * r * 1.2f, by = 128 + (Hash(i, seed + 7) - 0.5f) * r * 1.2f;
                c = Over(c, Color.Lerp(light, Color.white, 0.15f), Fill(Circle(x, y, bx, by, r * 0.1f)) * Fill(crown + 6f) * 0.5f);
            }
            return Over(c, Shade(dark, 0.55f), Stroke(crown, 3f));
        }

        static Color Pine(float x, float y, Color dark, Color light)
        {
            Color c = new(0, 0, 0, 0);
            float star = Star(x, y, 128, 128, 104, 70, 9);
            c = Over(c, new Color(0f, 0.08f, 0.02f, 0.4f), Fill(Star(x, y, 138, 118, 104, 70, 9)) * (1f - Fill(star)));
            c = Over(c, dark, Fill(star));
            c = Over(c, light, Fill(Star(x, y, 124, 132, 66, 42, 9, 0.3f)));
            c = Over(c, Color.Lerp(light, Color.white, 0.2f), Fill(Star(x, y, 122, 134, 32, 18, 7, 0.6f)));
            return Over(c, Shade(dark, 0.55f), Stroke(star, 3f));
        }

        static Color Willow(float x, float y)
        {
            Color c = Crown(x, y, 112, Hex("4E8F3A"), Hex("8CC65A"), 5);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                Vector2 from = new(128 + Mathf.Cos(a) * 40, 128 + Mathf.Sin(a) * 40), to = new(128 + Mathf.Cos(a) * 112, 128 + Mathf.Sin(a) * 112);
                c = Over(c, Hex("9ED36A"), Stroke(SegDist(new Vector2(x, y), from, to), 3f) * 0.7f);
            }
            return c;
        }

        static Color Fern(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                Vector2 tip = new(128 + Mathf.Cos(a) * 92, 128 + Mathf.Sin(a) * 92);
                float leaf = Capsule(x, y, 128, 128, tip.x, tip.y, 14);
                c = Over(c, Hex("4E9E3A"), Fill(leaf));
                c = Over(c, Hex("2F6E26"), Stroke(SegDist(new Vector2(x, y), new Vector2(128, 128), tip), 3f) * Fill(leaf));
            }
            return c;
        }

        static Color Mushrooms(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var spots = new[] { new Vector3(110, 120, 34), new Vector3(160, 150, 26), new Vector3(150, 96, 20) };
            foreach (var m in spots)
            {
                float cap = Circle(x, y, m.x, m.y, m.z);
                c = Over(c, new Color(0, 0, 0, 0.3f), Fill(Circle(x, y, m.x + 5, m.y - 5, m.z)) * (1f - Fill(cap)));
                c = Over(c, Hex("E0413A"), Fill(cap));
                for (int i = 0; i < 5; i++) c = Over(c, Color.white, Fill(Circle(x, y, m.x + Mathf.Cos(i * 1.3f) * m.z * 0.55f, m.y + Mathf.Sin(i * 1.3f) * m.z * 0.55f, m.z * 0.16f)));
                c = Over(c, Line, Stroke(cap, 2.5f));
            }
            return c;
        }

        static Color Stump(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float s = Circle(x, y, 128, 128, 52);
            c = Over(c, Hex("6B4426"), Fill(s));
            c = Over(c, Hex("C9A57A"), Fill(s + 8f));
            for (int i = 1; i < 5; i++) c = Over(c, Hex("A07E58"), Stroke(Circle(x, y, 128, 128, i * 9f), 2f) * Fill(s + 8f));
            return Over(c, Line, Stroke(s, 3f));
        }

        static Color ForestLog(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float log = RotRect(x, y, 128, 128, 100, 24, 22, 0.5f);
            c = Over(c, Mathf.Repeat(x + y, 22f) < 3f ? Hex("5A3A20") : Hex("7A5230"), Fill(log));
            c = Over(c, Moss, Fill(log) * Mathf.Clamp01((Mossy(x, y) - 0.6f) * 5f));
            return Over(c, Line, Stroke(log, 3f));
        }

        static Color MossRock(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float rock = SMin(Circle(x, y, 118, 122, 62), Circle(x, y, 160, 140, 40), 16f);
            c = Over(c, new Color(0, 0, 0, 0.35f), Fill(rock - 8f) * (1f - Fill(rock)));
            c = Over(c, Color.Lerp(StoneDark, StoneLight, Mathf.Clamp01(0.5f + (x - y) / 200f)), Fill(rock));
            c = Over(c, Moss, Fill(rock) * Mathf.Clamp01((Mossy(x, y) - 0.5f) * 5f));
            return Over(c, Line, Stroke(rock, 3f));
        }

        static Color Flowerbed(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] cols = { Hex("FF6F91"), Hex("FFD23F"), Hex("B266FF"), Color.white };
            for (int i = 0; i < 22; i++)
            {
                float fx = 50 + Hash(i, 1) * 156, fy = 50 + Hash(i, 2) * 156;
                c = Over(c, Hex("3E8A2E"), Fill(Circle(x, y, fx, fy, 14)));
                c = Over(c, cols[i % 4], Fill(Star(x, y, fx, fy, 10, 5, 5)));
            }
            return c;
        }

        static Color Reeds(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 24; i++)
            {
                float rx = 60 + Hash(i, 3) * 136, ry = 60 + Hash(i, 4) * 136;
                c = Over(c, Hex("6E9A3A"), Fill(Ellipse(x, y, rx, ry, 5, 22)));
                c = Over(c, Hex("6B4426"), Fill(Ellipse(x, y, rx, ry + 16, 4, 8)));
            }
            return c;
        }

        static Color LilyPads(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 6; i++)
            {
                float px = 70 + Hash(i, 5) * 116, py = 70 + Hash(i, 6) * 116, r = 20 + Hash(i, 7) * 12;
                float pad = Mathf.Max(Circle(x, y, px, py, r), -Triangle(x, y, new Vector2(px, py), new Vector2(px + r * 1.2f, py + r * 0.3f), new Vector2(px + r * 1.2f, py - r * 0.3f)));
                c = Over(c, Hex("4FA03A"), Fill(pad));
                c = Over(c, Hex("2F7A2A"), Stroke(pad, 2f));
                if (i % 2 == 0) c = Over(c, Hex("FF9EC4"), Fill(Star(x, y, px - r * 0.3f, py, 9, 4, 6)));
            }
            return c;
        }

        // ------------------------------------------------------------------ pirates

        static Color PirateShipPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = Mathf.Min(Ellipse(x, y, 128, 330, 104, 300), RoundRect(x, y, 128, 250, 104, 230, 40));
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.35f), Fill(hull - 8f) * (1f - Fill(hull)));
            c = Over(c, WoodDark, Fill(hull));
            float deck = Mathf.Min(Ellipse(x, y, 128, 330, 88, 280), RoundRect(x, y, 128, 250, 88, 214, 32));
            c = Over(c, Mathf.Repeat(x, 14f) < 2f ? Wood : WoodLight, Fill(deck));
            // Cannons along both sides.
            for (int i = 0; i < 6; i++)
            {
                float cy = 160 + i * 56;
                c = Over(c, Hex("2A2A2E"), Fill(RoundRect(x, y, 28, cy, 18, 7, 3)) + Fill(RoundRect(x, y, 228, cy, 18, 7, 3)));
            }
            // Masts with the sails set (seen from above: curved bands), the big one with a skull.
            float[] masts = { 470, 330, 180 };
            for (int m = 0; m < 3; m++)
            {
                float my = masts[m], w = m == 1 ? 112 : 92;
                float sail = Mathf.Max(Ellipse(x, y, 128, my, w, 46), -Ellipse(x, y, 128, my - 26, w + 6, 40));
                c = Over(c, Hex("F1E6C8"), Fill(sail));
                c = Over(c, Hex("CDBF98"), Stroke(sail, 3f) * Fill(sail + 2f));
                c = Over(c, WoodDark, Fill(RoundRect(x, y, 128, my - 6, w + 4, 4, 2)));
                c = Over(c, WoodDark, Fill(Circle(x, y, 128, my - 6, 9)));
            }
            float skull = Circle(x, y, 128, 350, 18);
            c = Over(c, Line, Fill(skull) * 0.9f);
            c = Over(c, Hex("F1E6C8"), Fill(Circle(x, y, 120, 352, 4.5f)) + Fill(Circle(x, y, 136, 352, 4.5f)));
            c = Over(c, Line, Fill(RoundRect(x, y, 128, 322, 26, 4, 2)));
            // The flag at the top of the main mast.
            c = Over(c, Line, Fill(Polygon(x, y, new[] { new Vector2(128, 600), new Vector2(170, 590), new Vector2(160, 572), new Vector2(128, 578) })));
            return Over(c, Line, Stroke(hull, 4f));
        }

        // A rock in the shape of a skull, the channel running through its open jaw (drawn over the boats).
        static Color SkullRockPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float rock = Mathf.Min(Ellipse(x, y, 256, 190, 240, 120), RoundRect(x, y, 256, 110, 230, 100, 40));
            float jaw = RoundRect(x, y, 256, 70, 92, 80, 30); // the opening the channel runs through
            float skull = Mathf.Max(rock, -jaw);
            float n = Noise(x * 0.04f, y * 0.04f, 64);
            c = Over(c, Color.Lerp(Hex("8E8A84"), Hex("C8C2B8"), n), Fill(skull));
            c = Over(c, Hex("2A2522"), Fill(Ellipse(x, y, 176, 210, 46, 40)) + Fill(Ellipse(x, y, 336, 210, 46, 40)));
            c = Over(c, Hex("2A2522"), Fill(Triangle(x, y, new Vector2(256, 190), new Vector2(236, 150), new Vector2(276, 150))));
            for (int i = 0; i < 6; i++)
            {
                float tx = 178 + i * 31;
                c = Over(c, Hex("EFE9DC"), Fill(RoundRect(x, y, tx, 150, 12, 14, 4)) * Fill(skull + 2f));
                c = Over(c, Line, Stroke(RoundRect(x, y, tx, 150, 12, 14, 4), 2f) * Fill(skull + 2f));
            }
            c = Over(c, Moss, Fill(skull) * Mathf.Clamp01((Mossy(x, y) - 0.74f) * 6f));
            return Over(c, Line, Stroke(skull, 4f));
        }

        static Color CannonballPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float ball = Circle(x, y, 32, 32, 26);
            c = Over(c, Hex("25262B"), Fill(ball));
            c = Over(c, White(0.45f), Fill(Circle(x, y, 24, 40, 7)));
            return Over(c, Line, Stroke(ball, 2.5f));
        }

        static Color TargetRingPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float d = Dist(x, y, 64, 64);
            c = Over(c, new Color(1f, 0.2f, 0.15f, 0.9f), Stroke(d - 54f, 8f) + Stroke(d - 30f, 5f));
            c = Over(c, new Color(1f, 0.2f, 0.15f, 0.9f), Fill(RoundRect(x, y, 64, 64, 4, 24, 2)) + Fill(RoundRect(x, y, 64, 64, 24, 4, 2)));
            c = Over(c, new Color(1f, 0.2f, 0.15f, 0.18f), Fill(d - 54f));
            return c;
        }

        static Color CoinPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float coin = Circle(x, y, 32, 32, 27);
            c = Over(c, GoldDark, Fill(coin));
            c = Over(c, Gold, Fill(coin + 4f));
            c = Over(c, GoldDark, Fill(Star(x, y, 32, 32, 13, 6)));
            c = Over(c, White(0.6f), Fill(Ellipse(x, y, 24, 42, 6, 3)));
            return Over(c, Line, Stroke(coin, 2.5f));
        }

        static Color PlankRampPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float ramp = RoundRect(x, y, 96, 128, 86, 120, 8);
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.35f), Fill(ramp - 6f) * (1f - Fill(ramp)));
            int plank = Mathf.FloorToInt(y / 24f);
            c = Over(c, plank % 2 == 0 ? WoodLight : Wood, Fill(ramp));
            c = Over(c, WoodDark, Fill(ramp) * (Mathf.Repeat(y, 24f) < 2.5f ? 1f : 0f));
            // Ropes along the edges, chevrons up the middle.
            c = Over(c, Hex("E8D7A8"), Fill(RoundRect(x, y, 18, 128, 5, 116, 2)) + Fill(RoundRect(x, y, 174, 128, 5, 116, 2)));
            for (int i = 0; i < 3; i++)
            {
                float cy = 70 + i * 60;
                float chev = Mathf.Min(SegDist(new Vector2(x, y), new Vector2(56, cy - 14), new Vector2(96, cy + 14)), SegDist(new Vector2(x, y), new Vector2(136, cy - 14), new Vector2(96, cy + 14)));
                c = Over(c, Color.white, Fill(chev - 7f));
            }
            return Over(c, Line, Stroke(ramp, 4f));
        }

        static Color TreasureChestPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float chest = RoundRect(x, y, 64, 48, 56, 38, 8);
            c = Over(c, Wood, Fill(chest));
            c = Over(c, WoodLight, Fill(RoundRect(x, y, 64, 58, 50, 20, 6)));
            c = Over(c, Gold, Fill(RoundRect(x, y, 34, 48, 5, 38, 2)) + Fill(RoundRect(x, y, 94, 48, 5, 38, 2)));
            c = Over(c, Gold, Fill(RoundRect(x, y, 64, 36, 9, 9, 3)));
            for (int i = 0; i < 5; i++) c = Over(c, Gold, Fill(Circle(x, y, 40 + i * 12, 74, 7)));
            return Over(c, Line, Stroke(chest, 3f));
        }

        // ------------------------------------------------------------------ the reef

        static Color JellyfishPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector2 from = new(48 + Mathf.Cos(a) * 18, 48 + Mathf.Sin(a) * 18), to = new(48 + Mathf.Cos(a + 0.4f) * 44, 48 + Mathf.Sin(a + 0.4f) * 44);
                c = Over(c, new Color(1f, 0.55f, 0.85f, 0.7f), Stroke(SegDist(new Vector2(x, y), from, to), 3f));
            }
            float bell = Circle(x, y, 48, 48, 26);
            c = Over(c, new Color(1f, 0.55f, 0.85f, 0.85f), Fill(bell));
            c = Over(c, new Color(1f, 0.85f, 0.95f, 0.9f), Fill(Circle(x, y, 48, 48, 13)));
            c = Over(c, White(0.6f), Fill(Ellipse(x, y, 40, 58, 7, 4)));
            return Over(c, new Color(0.6f, 0.1f, 0.45f, 0.9f), Stroke(bell, 2.5f));
        }

        static Color MantaRayPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Polygon(x, y, new[] { new Vector2(128, 170), new Vector2(246, 92), new Vector2(150, 70), new Vector2(128, 40), new Vector2(106, 70), new Vector2(10, 92) });
            body = SMin(body, Ellipse(x, y, 128, 110, 30, 50), 12f);
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(body - 6f) * (1f - Fill(body)));
            c = Over(c, Hex("2E3F5C"), Fill(body));
            c = Over(c, Hex("6E86A8"), Fill(body + 10f) * 0.35f);
            for (int i = 0; i < 6; i++) c = Over(c, Hex("DCE6F2"), Fill(Circle(x, y, 92 + i * 15, 104 + Mathf.Sin(i * 2f) * 8, 4)));
            c = Over(c, Hex("2E3F5C"), Stroke(SegDist(new Vector2(x, y), new Vector2(128, 40), new Vector2(128, 2)), 4f));
            c = Over(c, Hex("2E3F5C"), Fill(Ellipse(x, y, 116, 172, 6, 10)) + Fill(Ellipse(x, y, 140, 172, 6, 10)));
            return Over(c, Line, Stroke(body, 3f));
        }

        static Color CoralArchPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float arch = SMin(RoundRect(x, y, 256, 96, 200, 46, 30), Mathf.Min(Ellipse(x, y, 62, 96, 60, 84), Ellipse(x, y, 450, 96, 60, 84)), 30f);
            float n = Noise(x * 0.05f, y * 0.05f, 64);
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(arch - 6f) * (1f - Fill(arch)));
            c = Over(c, Color.Lerp(Hex("C9A27A"), Hex("E8CFA8"), n), Fill(arch));
            Color[] corals = { Hex("FF6F91"), Hex("FFB347"), Hex("B266FF"), Hex("3FD0C9"), Hex("FF5E5E") };
            for (int i = 0; i < 46; i++)
            {
                float cx = Hash(i, 1) * 512, cy = 20 + Hash(i, 2) * 152;
                float blob = Circle(x, y, cx, cy, 7 + Hash(i, 3) * 11);
                c = Over(c, corals[i % corals.Length], Fill(blob) * Fill(arch + 3f));
                c = Over(c, Shade(corals[i % corals.Length], 0.7f), Stroke(blob, 2f) * Fill(arch + 3f));
            }
            return Over(c, Line, Stroke(arch, 3f));
        }

        // ------------------------------------------------------------------ the storm

        // The storm fortress over the channel: walls and four towers round a courtyard, the gate the boats race through.
        static Color FortressPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float outer = RoundRect(x, y, 320, 224, 300, 200, 20);
            float passage = RoundRect(x, y, 320, 224, 66, 260, 6); // the water gate the channel runs through
            float body = Mathf.Max(outer, -passage);
            float yard = Mathf.Max(RoundRect(x, y, 320, 224, 250, 150, 12), -RoundRect(x, y, 320, 224, 92, 260, 6));
            float towers = Min(Circle(x, y, 40, 44, 46), Circle(x, y, 600, 44, 46), Circle(x, y, 40, 404, 46), Circle(x, y, 600, 404, 46));
            float gateTowers = Mathf.Min(RoundRect(x, y, 230, 224, 26, 120, 8), RoundRect(x, y, 410, 224, 26, 120, 8));
            float all = Min(body, towers, gateTowers);
            float n = Noise(x * 0.04f, y * 0.04f, 64);
            c = Over(c, new Color(0, 0, 0, 0.35f), Fill(all - 8f) * (1f - Fill(all)));
            c = Over(c, Color.Lerp(Hex("4E535C"), Hex("767C86"), n), Fill(all));
            // The courtyards inside the walls (paved), battlements round the top of the walls.
            c = Over(c, Mathf.Repeat(x, 30f) < 2f || Mathf.Repeat(y, 30f) < 2f ? Hex("6A6E74") : Hex("8E9298"), Fill(yard));
            c = Over(c, Hex("3A3E46"), Fill(body) * (Mathf.Repeat(x + y, 28f) < 10f ? 1f : 0f) * Stroke(outer, 14f));
            c = Over(c, Hex("3A3E46"), Fill(gateTowers) * (Mathf.Repeat(y, 24f) < 9f ? 1f : 0f) * Stroke(gateTowers, 10f));
            foreach (var t in new[] { new Vector2(40, 44), new Vector2(600, 44), new Vector2(40, 404), new Vector2(600, 404) })
            {
                c = Over(c, Hex("8A2E2E"), Fill(Circle(x, y, t.x, t.y, 32)));
                c = Over(c, Hex("5E1F1F"), Stroke(Circle(x, y, t.x, t.y, 32), 3f) + Fill(Circle(x, y, t.x, t.y, 5)));
            }
            // Torches either side of the gate, a portcullis bar across its top.
            foreach (var t in new[] { new Vector2(230, 120), new Vector2(410, 120), new Vector2(230, 328), new Vector2(410, 328) })
            {
                c = Over(c, Hex("FFB347"), Fill(Circle(x, y, t.x, t.y, 11)));
                c = Over(c, Hex("FFF1B0"), Fill(Circle(x, y, t.x, t.y, 5)));
            }
            c = Over(c, Hex("2A2D33"), Fill(RoundRect(x, y, 320, 336, 70, 8, 3)) + Fill(RoundRect(x, y, 320, 112, 70, 8, 3)));
            return Over(c, Line, Stroke(all, 4f));
        }

        static Color BoltPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var pts = new[] { new Vector2(40, 256), new Vector2(22, 180), new Vector2(40, 170), new Vector2(18, 90), new Vector2(38, 86), new Vector2(26, 0) };
            float d = float.MaxValue;
            for (int i = 0; i < pts.Length - 1; i++) d = Mathf.Min(d, SegDist(new Vector2(x, y), pts[i], pts[i + 1]));
            c = Over(c, new Color(0.7f, 0.85f, 1f, 0.6f), Gauss(d, 8f));
            c = Over(c, new Color(1f, 1f, 0.85f, 1f), Fill(d - 3.5f));
            return c;
        }
    }
}
