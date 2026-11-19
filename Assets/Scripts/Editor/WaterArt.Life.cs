using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The little lives along the Water Cup's later races, seen from above (heads to the top): butterflies (white wings,
    /// tinted in the race), dragonflies, frogs, a heron, a bat, falling leaves; floating barrels, crates, bottles and
    /// driftwood, a pirate flag; tropical fish, sea turtles and bubbles; a bell buoy for the storm.
    /// </summary>
    public static partial class WaterArt
    {
        [MenuItem("SomeGame/Generate Water Life Art")]
        public static void GenerateLife()
        {
            string f = ArtGenerator.Folder;
            Write(f, "Butterfly", 128, 128, ButterflyPixel, 96, false);
            Write(f, "Dragonfly", 128, 128, DragonflyPixel, 96, false);
            Write(f, "Frog", 128, 128, FrogPixel, 80, false);
            Write(f, "Heron", 192, 192, HeronPixel, 64, false);
            Write(f, "Bat", 128, 96, BatPixel, 80, false);
            Write(f, "Leaf", 64, 64, LeafPixel, 64, false);
            Write(f, "Barrel", 96, 96, BarrelPixel, 64, false);
            Write(f, "Crate", 96, 96, CratePixel, 64, false);
            Write(f, "Bottle", 64, 128, BottlePixel, 80, false);
            Write(f, "Driftwood", 64, 256, DriftwoodPixel, 64, false);
            Write(f, "PirateFlag", 256, 192, PirateFlagPixel, 48, false);
            Write(f, "TropicalFish", 64, 128, TropicalFishPixel, 96, false);
            Write(f, "SeaTurtle", 192, 192, SeaTurtlePixel, 64, false);
            Write(f, "Bubbles", 96, 96, BubblesPixel, 80, false);
            Write(f, "BellBuoy", 128, 128, BellBuoyPixel, 64, false);
            Write(f, "SurfSpray", 128, 128, SurfSprayPixel, 64, false);
            Write(f, "GullStanding", 96, 96, GullStandingPixel, 64, false);
            Write(f, "Walker", 64, 64, WalkerPixel, 64, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // A butterfly: four white wings (tinted per butterfly), dark body, spots on the wings.
        static Color ButterflyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float upper = Mathf.Min(Ellipse(x, y, 40, 78, 30, 26), Ellipse(x, y, 88, 78, 30, 26));
            float lower = Mathf.Min(Ellipse(x, y, 46, 42, 20, 18), Ellipse(x, y, 82, 42, 20, 18));
            float wings = Mathf.Min(upper, lower);
            c = Over(c, Color.white, Fill(wings));
            c = Over(c, new Color(0f, 0f, 0f, 0.25f), Stroke(wings + 6f, 5f) * Fill(wings));
            c = Over(c, new Color(0.1f, 0.1f, 0.15f, 0.85f), Fill(Circle(x, y, 30, 86, 6)) + Fill(Circle(x, y, 98, 86, 6)));
            c = Over(c, new Color(1f, 1f, 1f, 1f), Fill(Circle(x, y, 30, 86, 2.5f)) + Fill(Circle(x, y, 98, 86, 2.5f)));
            c = Over(c, Line, Stroke(wings, 3f));
            float body = Capsule(x, y, 64, 24, 64, 100, 6);
            c = Over(c, Hex("2A2230"), Fill(body));
            c = Over(c, Hex("2A2230"), Stroke(SegDist(new Vector2(x, y), new Vector2(62, 100), new Vector2(52, 120)), 2.5f));
            c = Over(c, Hex("2A2230"), Stroke(SegDist(new Vector2(x, y), new Vector2(66, 100), new Vector2(76, 120)), 2.5f));
            return c;
        }

        // A dragonfly: a long blue body, four glassy wings.
        static Color DragonflyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color glass = new(0.85f, 0.95f, 1f, 0.55f);
            float wings = Min(RotRect(x, y, 34, 80, 30, 7, 7, 10f), RotRect(x, y, 94, 80, 30, 7, 7, -10f),
                RotRect(x, y, 36, 62, 28, 7, 7, -12f), RotRect(x, y, 92, 62, 28, 7, 7, 12f));
            c = Over(c, glass, Fill(wings));
            c = Over(c, new Color(0.3f, 0.45f, 0.6f, 0.8f), Stroke(wings, 2f));
            float body = Mathf.Min(Capsule(x, y, 64, 8, 64, 78, 4.5f), Circle(x, y, 64, 88, 8));
            c = Over(c, Hex("2C8FD6"), Fill(body));
            c = Over(c, Hex("1B5C8E"), Fill(body) * (Mathf.Repeat(y, 10f) < 3f && y < 78f ? 1f : 0f));
            c = Over(c, Hex("5EE0A0"), Fill(Circle(x, y, 59, 92, 4)) + Fill(Circle(x, y, 69, 92, 4)));
            return Over(c, Line, Stroke(body, 2f));
        }

        // A frog seen from above: green, its legs folded, big eyes.
        static Color FrogPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color skin = Hex("6BBF3A"), dark = Hex("3F8A24");
            float legs = Min(Ellipse(x, y, 30, 40, 16, 26), Ellipse(x, y, 98, 40, 16, 26), Ellipse(x, y, 36, 84, 10, 14), Ellipse(x, y, 92, 84, 10, 14));
            float body = Ellipse(x, y, 64, 62, 30, 40);
            float all = SMin(body, legs, 6f);
            c = Over(c, new Color(0, 0.1f, 0, 0.3f), Fill(all - 5f) * (1f - Fill(all)));
            c = Over(c, dark, Fill(legs));
            c = Over(c, skin, Fill(body));
            c = Over(c, dark, Fill(Circle(x, y, 56, 56, 5)) + Fill(Circle(x, y, 74, 46, 4)) + Fill(Circle(x, y, 62, 36, 4)));
            foreach (float ex in new[] { 48f, 80f })
            {
                c = Over(c, skin, Fill(Circle(x, y, ex, 94, 11)));
                c = Over(c, Color.white, Fill(Circle(x, y, ex, 96, 7)));
                c = Over(c, Hex("1A1A1A"), Fill(Circle(x, y, ex, 97, 4)));
            }
            return Over(c, Line, Stroke(all, 3f));
        }

        // A grey heron standing in the shallows: long body, folded wings, its neck and dagger beak forward.
        static Color HeronPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Ellipse(x, y, 96, 84, 30, 46);
            float neck = Capsule(x, y, 96, 118, 96, 150, 9);
            float head = Circle(x, y, 96, 156, 12);
            float beak = Triangle(x, y, new Vector2(90, 162), new Vector2(102, 162), new Vector2(96, 190));
            float tail = Triangle(x, y, new Vector2(78, 48), new Vector2(114, 48), new Vector2(96, 22));
            float all = Min(body, neck, head, beak, tail);
            c = Over(c, new Color(0, 0.1f, 0.15f, 0.3f), Fill(all - 6f) * (1f - Fill(all)));
            c = Over(c, Hex("8E9AA6"), Fill(Mathf.Min(body, tail)));
            c = Over(c, Hex("6E7A86"), Fill(Mathf.Min(Ellipse(x, y, 76, 80, 12, 38), Ellipse(x, y, 116, 80, 12, 38))));
            c = Over(c, Hex("E8ECF0"), Fill(Mathf.Min(neck, head)));
            c = Over(c, Hex("2A2E36"), Fill(Capsule(x, y, 90, 158, 102, 158, 2.5f)));
            c = Over(c, Hex("F0B030"), Fill(beak));
            return Over(c, Line, Stroke(all, 3f));
        }

        // A gull standing (from above, head to the top): a white body, grey folded wings with black tips crossed
        // over its tail, a yellow beak.
        static Color GullStandingPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Ellipse(x, y, 48, 44, 13, 24);
            float head = Circle(x, y, 48, 70, 9);
            float tail = Triangle(x, y, new Vector2(40, 26), new Vector2(56, 26), new Vector2(48, 10));
            float all = Min(body, head, tail);
            c = Over(c, new Color(0, 0.1f, 0.15f, 0.3f), Fill(all - 4f) * (1f - Fill(all)));
            c = Over(c, Color.white, Fill(all));
            float wings = Mathf.Min(Ellipse(x, y, 40, 40, 7, 22), Ellipse(x, y, 56, 40, 7, 22));
            c = Over(c, Hex("AEB9C4"), Fill(wings));
            c = Over(c, Hex("2A2F36"), Fill(wings) * Mathf.Clamp01((30f - y) / 4f));
            c = Over(c, Hex("FFC93C"), Fill(Triangle(x, y, new Vector2(44, 76), new Vector2(52, 76), new Vector2(48, 88))));
            c = Over(c, Hex("1C1F24"), Fill(Circle(x, y, 43, 72, 1.6f)) + Fill(Circle(x, y, 53, 72, 1.6f)));
            return Over(c, Line, Stroke(all, 2f));
        }

        // Someone strolling along the quay, from above (facing the top): a shirt (white, tinted in the race),
        // arms, a head of hair.
        static Color WalkerPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float shoulders = Ellipse(x, y, 32, 30, 15, 8);
            float arms = Mathf.Min(Capsule(x, y, 17, 33, 16, 24, 3.5f), Capsule(x, y, 47, 33, 48, 24, 3.5f));
            float head = Circle(x, y, 32, 33, 7.5f);
            float all = Min(shoulders, arms, head);
            c = Over(c, new Color(0, 0.1f, 0.15f, 0.3f), Fill(all - 3f) * (1f - Fill(all)));
            c = Over(c, Hex("F2B48C"), Fill(arms));
            c = Over(c, Color.white, Fill(shoulders));
            c = Over(c, Hex("F2B48C"), Fill(head));
            c = Over(c, Hex("4A3226"), Fill(Mathf.Max(head, y - 37f)));
            return Over(c, Line, Stroke(all, 1.8f));
        }

        // A bat, wings spread.
        static Color BatPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float wing(float side)
            {
                float d = Ellipse(x, y, 64 + side * 30, 52, 34, 22);
                for (int k = 0; k < 3; k++) d = Mathf.Max(d, -Circle(x, y, 64 + side * (18 + k * 18), 26, 10));
                return d;
            }
            float all = SMin(Mathf.Min(wing(1f), wing(-1f)), Ellipse(x, y, 64, 52, 10, 18), 4f);
            c = Over(c, Hex("3A2E44"), Fill(all));
            c = Over(c, Hex("5A4866"), Fill(Ellipse(x, y, 64, 54, 8, 14)));
            c = Over(c, Hex("3A2E44"), Fill(Triangle(x, y, new Vector2(56, 64), new Vector2(62, 64), new Vector2(58, 76))) + Fill(Triangle(x, y, new Vector2(66, 64), new Vector2(72, 64), new Vector2(70, 76))));
            return Over(c, Line, Stroke(all, 2.5f));
        }

        // A leaf (white, tinted green, yellow or orange in the race): a pointed oval, its midrib and veins.
        static Color LeafPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float leaf = SMin(Ellipse(x, y, 32, 30, 15, 22), Triangle(x, y, new Vector2(24, 44), new Vector2(40, 44), new Vector2(32, 60)), 6f);
            c = Over(c, Color.white, Fill(leaf));
            c = Over(c, new Color(0, 0, 0, 0.25f), Stroke(SegDist(new Vector2(x, y), new Vector2(32, 6), new Vector2(32, 56)), 2f));
            for (int k = 0; k < 3; k++)
            {
                float vy = 18 + k * 12;
                c = Over(c, new Color(0, 0, 0, 0.18f), Stroke(SegDist(new Vector2(x, y), new Vector2(32, vy), new Vector2(20, vy + 8)), 1.5f) * Fill(leaf));
                c = Over(c, new Color(0, 0, 0, 0.18f), Stroke(SegDist(new Vector2(x, y), new Vector2(32, vy), new Vector2(44, vy + 8)), 1.5f) * Fill(leaf));
            }
            return Over(c, new Color(0, 0, 0, 0.45f), Stroke(leaf, 2f));
        }

        // A barrel floating on its side.
        static Color BarrelPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float barrel = RoundRect(x, y, 48, 48, 26, 36, 14);
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(RoundRect(x, y, 52, 44, 28, 38, 14)) * (1f - Fill(barrel)));
            float bulge = 1f - Mathf.Abs(x - 48f) / 26f;
            c = Over(c, Color.Lerp(Hex("6E4726"), Hex("B07A47"), bulge), Fill(barrel));
            c = Over(c, Hex("5C3A1E"), Fill(barrel) * (Mathf.Repeat(x - 22f, 10f) < 1.6f ? 1f : 0f));
            foreach (float hy in new[] { 24f, 72f }) c = Over(c, Hex("4A4A50"), Fill(RoundRect(x, y, 48, hy, 27, 3, 1)) * Fill(barrel + 1f));
            return Over(c, Line, Stroke(barrel, 2.5f));
        }

        // A wooden crate adrift.
        static Color CratePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float crate = RoundRect(x, y, 48, 48, 34, 34, 4);
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(RoundRect(x, y, 52, 44, 34, 34, 4)) * (1f - Fill(crate)));
            c = Over(c, Hex("B88A52"), Fill(crate));
            c = Over(c, Hex("8A6234"), Fill(crate) * (Mathf.Repeat(y - 14f, 17f) < 2f ? 1f : 0f));
            c = Over(c, Hex("7A5228"), Fill(Mathf.Min(RotRect(x, y, 48, 48, 44, 5, 2, 45f), RotRect(x, y, 48, 48, 44, 5, 2, -45f))) * Fill(crate + 4f));
            c = Over(c, Hex("7A5228"), Stroke(RoundRect(x, y, 48, 48, 29, 29, 2), 5f));
            return Over(c, Line, Stroke(crate, 2.5f));
        }

        // A message in a bottle: green glass, a cork, a rolled note inside.
        static Color BottlePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = SMin(RoundRect(x, y, 32, 50, 16, 34, 14), RoundRect(x, y, 32, 98, 7, 18, 4), 8f);
            c = Over(c, new Color(0.25f, 0.65f, 0.4f, 0.75f), Fill(body));
            c = Over(c, new Color(0.95f, 0.9f, 0.75f, 1f), Fill(RoundRect(x, y, 32, 50, 7, 22, 4)));
            c = Over(c, White(0.6f), Fill(RoundRect(x, y, 22, 54, 3, 22, 3)));
            c = Over(c, Hex("A0703C"), Fill(RoundRect(x, y, 32, 118, 7, 7, 2)));
            return Over(c, Line, Stroke(body, 2.5f));
        }

        // A piece of driftwood: a grey-brown branch, a stub of a side branch.
        static Color DriftwoodPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float log = Mathf.Min(Capsule(x, y, 32, 18, 30, 236, 12), Capsule(x, y, 30, 150, 52, 186, 5));
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(Capsule(x, y, 36, 14, 34, 232, 12)) * (1f - Fill(log)));
            c = Over(c, Color.Lerp(Hex("7E7466"), Hex("A89C8A"), Noise(x * 0.1f, y * 0.03f, 16)), Fill(log));
            c = Over(c, Hex("5E564C"), Fill(log) * (Mathf.Abs(Mathf.Sin(y * 0.09f + x * 0.2f)) > 0.94f ? 1f : 0f));
            return Over(c, Line, Stroke(log, 2.5f));
        }

        // A pirate flag on its pole, seen from above: the pole's top and a black flag streaming to the right with a
        // white skull and crossbones (it flaps in the race).
        static Color PirateFlagPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float wave = Mathf.Sin((x - 40f) * 0.05f) * 8f;
            float flag = Mathf.Max(RoundRect(x, y - wave, 140, 96, 104, 62, 6), 36f - x);
            c = Over(c, new Color(0, 0, 0, 0.3f), Fill(Mathf.Max(RoundRect(x - 6, y + 8 - wave, 140, 96, 104, 62, 6), 36f - x)) * (1f - Fill(flag)));
            c = Over(c, Hex("1C1C22"), Fill(flag));
            float skull = Mathf.Min(Circle(x, y - wave, 140, 106, 22), RoundRect(x, y - wave, 140, 84, 13, 10, 4));
            float bones = Mathf.Min(Capsule(x, y - wave, 106, 64, 174, 128, 5), Capsule(x, y - wave, 106, 128, 174, 64, 5));
            c = Over(c, Hex("F2EEE4"), Fill(Mathf.Min(skull, bones)));
            c = Over(c, Hex("1C1C22"), Fill(Circle(x, y - wave, 131, 108, 6)) + Fill(Circle(x, y - wave, 149, 108, 6)));
            c = Over(c, Line, Stroke(flag, 3f));
            float pole = Circle(x, y, 34, 96, 12);
            c = Over(c, Hex("6E4726"), Fill(pole));
            c = Over(c, Hex("FFC83D"), Fill(Circle(x, y, 34, 96, 6)));
            return Over(c, Line, Stroke(pole, 2.5f));
        }

        // A little tropical fish (white and dark: tinted per fish), head up.
        static Color TropicalFishPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Ellipse(x, y, 32, 72, 18, 36);
            float tail = Triangle(x, y, new Vector2(32, 44), new Vector2(12, 8), new Vector2(52, 8));
            float all = SMin(body, tail, 4f);
            c = Over(c, Color.white, Fill(all));
            c = Over(c, new Color(0.15f, 0.15f, 0.2f, 0.9f), Fill(body) * (Mathf.Abs(y - 66f) < 5f || Mathf.Abs(y - 88f) < 4f ? 1f : 0f));
            c = Over(c, new Color(0.15f, 0.15f, 0.2f, 1f), Fill(Circle(x, y, 24, 94, 3)) + Fill(Circle(x, y, 40, 94, 3)));
            return Over(c, Line, Stroke(all, 2.5f));
        }

        // A sea turtle swimming (head up): a patterned shell, four flippers.
        static Color SeaTurtlePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float shell = Ellipse(x, y, 96, 92, 42, 54);
            float flippers = Min(RotRect(x, y, 48, 122, 30, 11, 10, 30f), RotRect(x, y, 144, 122, 30, 11, 10, -30f),
                RotRect(x, y, 60, 52, 18, 9, 8, -30f), RotRect(x, y, 132, 52, 18, 9, 8, 30f));
            float head = Ellipse(x, y, 96, 160, 16, 20);
            float all = Min(shell, flippers, head);
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(all - 6f) * (1f - Fill(all)));
            c = Over(c, Hex("8CBF6A"), Fill(Mathf.Min(flippers, head)));
            c = Over(c, Hex("6E8F3A"), Fill(shell));
            for (int i = 0; i < 7; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector2 q = i == 6 ? new Vector2(96, 92) : new Vector2(96 + Mathf.Cos(a) * 26, 92 + Mathf.Sin(a) * 32);
                c = Over(c, Hex("A9C76A"), Fill(Circle(x, y, q.x, q.y, 11)) * Fill(shell + 4f));
                c = Over(c, Hex("4E6A28"), Stroke(Circle(x, y, q.x, q.y, 11), 2f) * Fill(shell));
            }
            c = Over(c, Hex("1A1A1A"), Fill(Circle(x, y, 88, 166, 2.5f)) + Fill(Circle(x, y, 104, 166, 2.5f)));
            return Over(c, Line, Stroke(all, 3f));
        }

        // A cluster of bubbles rising.
        static Color BubblesPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var spots = new[] { new Vector3(48, 48, 18), new Vector3(22, 30, 9), new Vector3(74, 70, 11), new Vector3(70, 26, 7), new Vector3(28, 72, 6) };
            foreach (var b in spots)
            {
                float d = Circle(x, y, b.x, b.y, b.z);
                c = Over(c, new Color(0.85f, 0.97f, 1f, 0.35f), Fill(d));
                c = Over(c, new Color(1f, 1f, 1f, 0.9f), Stroke(d, 2.5f));
                c = Over(c, White(0.9f), Fill(Circle(x, y, b.x - b.z * 0.4f, b.y + b.z * 0.4f, b.z * 0.22f)));
            }
            return c;
        }

        // Surf bursting against a cliff's foot (the cliff at the bottom): a fan of foam and flying droplets thrown up
        // and out.
        static Color SurfSprayPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float foam = float.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(0.2f, Mathf.PI - 0.2f, i / 8f);
                float reach = 22f + Hash(i, 3) * 26f;
                foam = SMin(foam, Circle(x, y, 64 + Mathf.Cos(a) * reach, 14 + Mathf.Sin(a) * reach, 12 + Hash(i, 4) * 9), 10f);
            }
            foam = SMin(foam, Ellipse(x, y, 64, 12, 52, 14), 12f);
            c = Over(c, new Color(0.82f, 0.92f, 0.98f, 0.9f), Fill(foam));
            c = Over(c, White(0.95f), Fill(foam + 5f));
            for (int i = 0; i < 16; i++)
            {
                float a = Mathf.Lerp(0.15f, Mathf.PI - 0.15f, Hash(i, 7));
                float reach = 50f + Hash(i, 8) * 40f;
                c = Over(c, White(0.85f), Fill(Circle(x, y, 64 + Mathf.Cos(a) * reach, 14 + Mathf.Sin(a) * reach, 2.5f + Hash(i, 9) * 3.5f)));
            }
            return c;
        }

        // A red bell buoy, seen from above: its float, its cage and its lamp.
        static Color BellBuoyPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float floatRing = Circle(x, y, 64, 64, 44);
            c = Over(c, new Color(0, 0.1f, 0.2f, 0.3f), Fill(Circle(x, y, 70, 58, 44)) * (1f - Fill(floatRing)));
            c = Over(c, Hex("D23A2E"), Fill(floatRing));
            c = Over(c, Hex("F2EEE4"), Fill(Shell(Circle(x, y, 64, 64, 36), 4f)));
            c = Over(c, Hex("3A3A40"), Stroke(Circle(x, y, 64, 64, 24), 4f));
            c = Over(c, Hex("3A3A40"), Stroke(SegDist(new Vector2(x, y), new Vector2(40, 64), new Vector2(88, 64)), 4f));
            c = Over(c, Hex("3A3A40"), Stroke(SegDist(new Vector2(x, y), new Vector2(64, 40), new Vector2(64, 88)), 4f));
            c = Over(c, Hex("FFE680"), Fill(Circle(x, y, 64, 64, 10)));
            return Over(c, Line, Stroke(floatRing, 3f));
        }
    }
}
