using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Pirate Bay's pirate-movie art, seen from above (bow at the top of each ship picture): the galleon you sail
    /// through (its insides cut open along a flooded aisle: the hold, the galley, the mess, the bunks with snoring
    /// pirates in hammocks, the captain's treasure cabin) and its top deck over them, the wreck you drive over (up the
    /// gangplank, along the deck, off the bow), the kraken's tentacles and head, shark fins, parrots, snores, an anchor,
    /// an X marking the spot and barrels.
    /// </summary>
    public static partial class WaterArt
    {
        [MenuItem("SomeGame/Generate Pirate Art")]
        public static void GeneratePirates()
        {
            string f = ArtGenerator.Folder;
            Write(f, "GalleonInside", 256, 768, GalleonInsidePixel, 12, false);
            Write(f, "GalleonTop", 256, 768, GalleonTopPixel, 12, false);
            Write(f, "DeckShip", 256, 768, DeckShipPixel, 12, false);
            Write(f, "Tentacle", 128, 384, TentaclePixel, 48, false);
            Write(f, "KrakenHead", 320, 320, KrakenHeadPixel, 24, false);
            Write(f, "SharkFin", 96, 128, SharkFinPixel, 64, false);
            Write(f, "Parrot", 128, 128, PirateParrotPixel, 64, false);
            Write(f, "Snore", 64, 64, SnorePixel, 64, false);
            Write(f, "Anchor", 128, 160, AnchorPixel, 48, false);
            Write(f, "XMark", 128, 128, XMarkPixel, 40, false);
            Write(f, "PirateBarrels", 128, 128, PirateBarrelsPixel, 40, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static readonly Color Plank = Hex("9C6B3E"), PlankDark = Hex("6E4726"), PlankLight = Hex("B98852"), Hull = Hex("4A2E18"),
            HullDark = Hex("2E1C0E"), Rope = Hex("D9C08A"), Sail = Hex("F1E6C8"), Red = Hex("C8302B"), Pirate = Hex("F2C9A0");

        // The ship's outline: a long hull, pointed bow at the top, square stern at the bottom.
        static float ShipHull(float x, float y, float inset = 0f)
        {
            float body = RoundRect(x, y, 128, 330, 118 - inset, 320 - inset, 30);
            float bow = Ellipse(x, y, 128, 600, 118 - inset, 160 - inset);
            float hull = SMin(body, bow, 20f);
            return Mathf.Max(hull, y - (760 - inset));
        }

        static float Boards(float x, float y, float width) => Mathf.Repeat(x, width) < 2.5f ? 0.82f : 1f;

        // ---------------------------------------------------------------- the galleon you sail through

        // Its aisle (x 80..176) runs the whole length: wet planks you skim over. Either side, behind wooden walls,
        // the rooms.
        static Color GalleonInsidePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = ShipHull(x, y), inner = ShipHull(x, y, 12);
            c = Over(c, Hull, Fill(hull));
            // The floors of the rooms: planks across.
            c = Over(c, Shade(Plank, Boards(y, x, 22) * 0.85f), Fill(inner));
            // The aisle: wet planks along, a sheen of water.
            bool aisle = x > 80 && x < 176;
            if (aisle)
            {
                c = Over(c, Shade(PlankLight, Boards(x, y, 24)), Fill(inner));
                c = Over(c, new Color(0.6f, 0.85f, 1f, 0.28f + 0.12f * Mathf.Sin(y * 0.05f)), Fill(inner));
            }
            // Walls along the aisle (with gaps: doorways), cross walls between rooms.
            float[] rooms = { 170, 330, 470, 640 };
            foreach (float side in new[] { 78f, 178f })
            {
                float wall = RoundRect(x, y, side, 380, 4, 380, 2);
                bool door = false;
                foreach (float r in rooms) if (Mathf.Abs(y - (r - 40)) < 18) door = true;
                if (!door) c = Over(c, HullDark, Fill(wall) * Fill(inner));
            }
            foreach (float r in rooms)
                c = Over(c, HullDark, Fill(RoundRect(x, y, 128, r, 128, 4, 2)) * Fill(inner) * (aisle ? 0f : 1f));
            if (!aisle) c = Rooms(c, x, y);
            return Over(c, Line, Stroke(hull, 4f));
        }

        static Color Rooms(Color c, float x, float y)
        {
            float lx = x < 128 ? x : 256 - x; // (mirrored: the same kind of room either side, a little different)
            bool left = x < 128;
            // The captain's cabin at the stern: gold heaps, a chest, the map table, candles.
            if (y < 170)
            {
                if (left)
                {
                    for (int i = 0; i < 9; i++) c = Over(c, i % 3 == 0 ? Hex("FFE066") : Hex("FFC83D"), Fill(Circle(x, y, 26 + Hash(i, 1) * 44, 40 + Hash(i, 2) * 100, 9 + Hash(i, 3) * 6)));
                    c = Over(c, Hex("8A5A32"), Fill(RoundRect(x, y, 46, 130, 22, 14, 3)));
                    c = Over(c, Gold, Fill(RoundRect(x, y, 46, 130, 4, 14, 1)));
                }
                else
                {
                    c = Over(c, Hex("E8D7A8"), Fill(RoundRect(x, y, 210, 90, 30, 40, 4)));
                    c = Over(c, Red, Stroke(SegDist(new Vector2(x, y), new Vector2(198, 80), new Vector2(222, 104)), 4f) + Stroke(SegDist(new Vector2(x, y), new Vector2(198, 104), new Vector2(222, 80)), 4f));
                    c = Over(c, Hex("8C6A45"), Stroke(SegDist(new Vector2(x, y), new Vector2(190, 60), new Vector2(228, 122)), 1.5f) * 0.6f);
                    c = Over(c, Hex("FFF1B0"), Fill(Circle(x, y, 230, 150, 5)));
                    c = Over(c, new Color(1f, 0.85f, 0.4f, 0.35f), Fill(Circle(x, y, 230, 150, 16)));
                }
                return c;
            }
            // The bunk room: hammocks slung across, a pirate snoring in each.
            if (y < 330)
            {
                for (int i = 0; i < 3; i++)
                {
                    float hy = 200 + i * 44;
                    float hammock = Ellipse(x, y, left ? 40 : 216, hy, 32, 13);
                    c = Over(c, Rope, Fill(hammock));
                    c = Over(c, Shade(Rope, 0.75f), Stroke(hammock, 2f));
                    float px = left ? 34 : 222;
                    c = Over(c, i % 2 == 0 ? Hex("3A5FA8") : Hex("6B8F3A"), Fill(Ellipse(x, y, px + (left ? 6 : -6), hy, 16, 8)));
                    float head = Circle(x, y, left ? 58 : 198, hy, 8);
                    c = Over(c, Pirate, Fill(head));
                    c = Over(c, Red, Fill(Mathf.Max(head, -(y - hy)))); // a red bandana
                    c = Over(c, Line, Stroke(head, 1.5f));
                }
                return c;
            }
            // The mess: a long table with plates and mugs, benches either side.
            if (y < 470)
            {
                float table = RoundRect(x, y, left ? 44 : 212, 400, 16, 56, 4);
                c = Over(c, PlankDark, Fill(RoundRect(x, y, left ? 20 : 236, 400, 5, 56, 2)) + Fill(RoundRect(x, y, left ? 68 : 188, 400, 5, 56, 2)));
                c = Over(c, Hex("B07A47"), Fill(table));
                for (int i = 0; i < 4; i++)
                {
                    float py = 360 + i * 26;
                    c = Over(c, Color.white, Fill(Circle(x, y, left ? 40 : 216, py, 6)));
                    c = Over(c, Hex("E8913A"), Fill(Circle(x, y, left ? 40 : 216, py, 3)));
                    c = Over(c, Hex("C9C3B2"), Fill(Circle(x, y, left ? 52 : 204, py + 10, 4)));
                }
                return c;
            }
            // The galley: the stove with its fire and a bubbling pot (left), pans hanging, a table of fish and bread (right).
            if (y < 640)
            {
                if (left)
                {
                    float stove = RoundRect(x, y, 40, 560, 28, 34, 6);
                    c = Over(c, Hex("3A3A40"), Fill(stove));
                    c = Over(c, Hex("FF8A1E"), Fill(Circle(x, y, 40, 536, 10)));
                    c = Over(c, Hex("FFE066"), Fill(Circle(x, y, 40, 536, 5)));
                    float pot = Circle(x, y, 40, 574, 16);
                    c = Over(c, Hex("2A2A2E"), Fill(pot));
                    c = Over(c, Hex("9ACD5A"), Fill(Circle(x, y, 40, 574, 11)));
                    for (int i = 0; i < 4; i++) c = Over(c, Hex("C8E890"), Fill(Circle(x, y, 34 + i * 4, 570 + (i % 2) * 6, 2.5f)));
                    for (int i = 0; i < 3; i++) c = Over(c, Hex("5A5A60"), Fill(Circle(x, y, 18 + i * 22, 620, 7)) + Stroke(SegDist(new Vector2(x, y), new Vector2(18 + i * 22, 620), new Vector2(18 + i * 22, 632)), 2f));
                }
                else
                {
                    c = Over(c, Hex("B07A47"), Fill(RoundRect(x, y, 214, 560, 26, 40, 4)));
                    for (int i = 0; i < 3; i++) c = Over(c, Hex("7FA8C9"), Fill(Ellipse(x, y, 214, 530 + i * 22, 16, 6)));
                    for (int i = 0; i < 3; i++) c = Over(c, Hex("7FA8C9"), Fill(Triangle(x, y, new Vector2(230, 530 + i * 22), new Vector2(238, 524 + i * 22), new Vector2(238, 536 + i * 22))));
                    c = Over(c, Hex("D9A05A"), Fill(Ellipse(x, y, 206, 594, 12, 8)));
                    c = Over(c, Hex("8A5A32"), Fill(Circle(x, y, 226, 620, 10)) + Fill(Circle(x, y, 204, 622, 9)));
                }
                return c;
            }
            // The hold at the bow: barrels, coiled ropes, the anchor chain.
            for (int i = 0; i < 4; i++)
            {
                float bx = left ? 36 + (i % 2) * 24 : 220 - (i % 2) * 24, by = 670 + (i / 2) * 26;
                c = Over(c, Hex("8A5A32"), Fill(Circle(x, y, bx, by, 11)));
                c = Over(c, Hex("4A4A50"), Stroke(Circle(x, y, bx, by, 8), 2f));
            }
            c = Over(c, Rope, Stroke(Circle(x, y, left ? 50 : 206, 720, 12), 4f));
            return c;
        }

        // Its top deck (over the rooms: it fades away while you are inside): planks, three masts with furled sails and
        // the crow's nest, cannons along both rails, hatches, the wheel at the stern, the Jolly Roger.
        static Color GalleonTopPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = ShipHull(x, y), deck = ShipHull(x, y, 10);
            c = Over(c, new Color(0f, 0.08f, 0.15f, 0.35f), Fill(ShipHull(x - 8, y + 8)) * (1f - Fill(hull)));
            c = Over(c, Hull, Fill(hull));
            c = Over(c, Shade(Plank, Boards(x, y, 18)), Fill(deck));
            for (int i = 0; i < 7; i++)
            {
                float cy = 90 + i * 84;
                c = Over(c, Hex("25262B"), Fill(RoundRect(x, y, 14, cy, 14, 6, 3)) + Fill(RoundRect(x, y, 242, cy, 14, 6, 3)));
            }
            c = Over(c, PlankDark, Fill(RoundRect(x, y, 128, 250, 26, 20, 3)) + Fill(RoundRect(x, y, 128, 420, 26, 20, 3)));
            c = Over(c, HullDark, Stroke(RoundRect(x, y, 128, 250, 26, 20, 3), 3f) + Stroke(RoundRect(x, y, 128, 420, 26, 20, 3), 3f));
            float[] masts = { 160, 340, 540 };
            for (int m = 0; m < 3; m++)
            {
                float my = masts[m];
                float sail = RoundRect(x, y, 128, my, 104, 14, 7);
                c = Over(c, Sail, Fill(sail));
                c = Over(c, Shade(Sail, 0.8f), Stroke(sail, 2.5f) + (Mathf.Repeat(x, 20f) < 2f ? Fill(sail) : 0f));
                c = Over(c, PlankDark, Fill(Circle(x, y, 128, my, 14)));
                c = Over(c, Rope, Stroke(Circle(x, y, 128, my, 22), 3f));
            }
            // The Jolly Roger atop the main mast.
            float flag = RoundRect(x, y, 158, 360, 30, 18, 2);
            c = Over(c, Line, Fill(flag));
            c = Over(c, Color.white, Fill(Circle(x, y, 158, 364, 7)));
            c = Over(c, Color.white, Stroke(SegDist(new Vector2(x, y), new Vector2(144, 348), new Vector2(172, 372)), 3f) + Stroke(SegDist(new Vector2(x, y), new Vector2(144, 372), new Vector2(172, 348)), 3f));
            // The wheel at the stern.
            c = Over(c, PlankDark, Stroke(Circle(x, y, 128, 60, 16), 4f));
            for (int i = 0; i < 8; i++) c = Over(c, PlankDark, Stroke(SegDist(new Vector2(x, y), new Vector2(128, 60), new Vector2(128 + Mathf.Cos(i * 0.785f) * 22, 60 + Mathf.Sin(i * 0.785f) * 22)), 3f));
            return Over(c, Line, Stroke(hull, 4f));
        }

        // ---------------------------------------------------------------- the wreck you drive over

        static Color DeckShipPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = ShipHull(x, y), deck = ShipHull(x, y, 12);
            c = Over(c, new Color(0f, 0.08f, 0.15f, 0.35f), Fill(ShipHull(x - 8, y + 8)) * (1f - Fill(hull)));
            c = Over(c, Hull, Fill(hull));
            c = Over(c, Shade(Plank, Boards(x, y, 20)), Fill(deck));
            // The broken masts lie along the rails, a coil of rope, a few barrels - the middle is clear to drive.
            c = Over(c, PlankDark, Fill(RotRect(x, y, 30, 420, 8, 150, 6, 0.05f)) + Fill(RotRect(x, y, 226, 300, 8, 130, 6, -0.04f)));
            c = Over(c, Rope, Stroke(Circle(x, y, 40, 230, 14), 4f) + Stroke(Circle(x, y, 216, 520, 12), 4f));
            c = Over(c, Hex("8A5A32"), Fill(Circle(x, y, 222, 160, 11)) + Fill(Circle(x, y, 34, 600, 11)));
            // The gangplank up the stern (chevrons), the bow's lip.
            float plank = RoundRect(x, y, 128, 22, 48, 26, 4);
            c = Over(c, PlankLight, Fill(plank));
            for (int i = 0; i < 2; i++)
            {
                float cy = 12 + i * 20;
                float chev = Mathf.Min(SegDist(new Vector2(x, y), new Vector2(104, cy - 6), new Vector2(128, cy + 6)), SegDist(new Vector2(x, y), new Vector2(152, cy - 6), new Vector2(128, cy + 6)));
                c = Over(c, Color.white, Fill(chev - 4f));
            }
            for (int i = 0; i < 3; i++)
            {
                float cy = 660 + i * 26;
                float chev = Mathf.Min(SegDist(new Vector2(x, y), new Vector2(98, cy - 8), new Vector2(128, cy + 8)), SegDist(new Vector2(x, y), new Vector2(158, cy - 8), new Vector2(128, cy + 8)));
                c = Over(c, Hex("FFD23F"), Fill(chev - 4f) * Fill(deck));
            }
            return Over(c, Line, Stroke(hull, 4f));
        }

        // ---------------------------------------------------------------- creatures and bits

        static Color TentaclePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float w = Mathf.Lerp(30f, 6f, y / 384f), cx = 64 + Mathf.Sin(y * 0.02f) * 14f;
            float body = Mathf.Abs(x - cx) - w;
            body = Mathf.Max(body, y - 376);
            c = Over(c, Hex("7A3FA0"), Fill(body));
            c = Over(c, Hex("A866D0"), Fill(body + w * 0.4f) * (x < cx ? 1f : 0f));
            for (int i = 0; i < 12; i++)
            {
                float sy = 20 + i * 28, sw = Mathf.Lerp(30f, 6f, sy / 384f);
                c = Over(c, Hex("F2C9E8"), Fill(Circle(x, y, 64 + Mathf.Sin(sy * 0.02f) * 14f + sw * 0.45f, sy, sw * 0.28f)));
            }
            return Over(c, Line, Stroke(body, 3f));
        }

        static Color KrakenHeadPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float head = Ellipse(x, y, 160, 170, 120, 130);
            c = Over(c, new Color(0f, 0.05f, 0.15f, 0.4f), Fill(head - 16f) * (1f - Fill(head)));
            c = Over(c, Hex("7A3FA0"), Fill(head));
            c = Over(c, Hex("A866D0"), Fill(Ellipse(x, y, 130, 210, 60, 50)) * 0.5f);
            foreach (float ex in new[] { 110f, 210f })
            {
                c = Over(c, Hex("FFE066"), Fill(Circle(x, y, ex, 130, 26)));
                c = Over(c, Line, Fill(Ellipse(x, y, ex, 130, 6, 20)));
                c = Over(c, Line, Stroke(Circle(x, y, ex, 130, 26), 3f));
            }
            for (int i = 0; i < 10; i++) c = Over(c, Hex("5E2E80"), Fill(Circle(x, y, 80 + Hash(i, 3) * 160, 180 + Hash(i, 4) * 100, 6 + Hash(i, 5) * 6)));
            return Over(c, Line, Stroke(head, 4f));
        }

        static Color SharkFinPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, White(0.75f), Fill(Ellipse(x, y, 48, 26, 40, 14)));
            float fin = Triangle(x, y, new Vector2(30, 30), new Vector2(66, 30), new Vector2(40, 118));
            fin = SMin(fin, Ellipse(x, y, 48, 34, 20, 8), 6f);
            c = Over(c, Hex("6E7C8A"), Fill(fin));
            c = Over(c, Hex("9AA8B5"), Fill(fin + 5f) * (x < 44 ? 1f : 0f));
            return Over(c, Line, Stroke(fin, 2.5f));
        }

        static Color PirateParrotPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float wings = SMin(Ellipse(x, y, 34, 64, 30, 14), Ellipse(x, y, 94, 64, 30, 14), 8f);
            float body = Ellipse(x, y, 64, 64, 14, 34);
            float tail = Triangle(x, y, new Vector2(56, 34), new Vector2(72, 34), new Vector2(64, 4));
            c = Over(c, Hex("2E86DE"), Fill(wings));
            c = Over(c, Hex("FFD23F"), Fill(Ellipse(x, y, 30, 62, 18, 7)) + Fill(Ellipse(x, y, 98, 62, 18, 7)));
            c = Over(c, Hex("E8402C"), Fill(Mathf.Min(body, tail)));
            c = Over(c, Hex("E8402C"), Fill(Circle(x, y, 64, 96, 12)));
            c = Over(c, Hex("FFE066"), Fill(Triangle(x, y, new Vector2(58, 104), new Vector2(70, 104), new Vector2(64, 118))));
            c = Over(c, Line, Fill(Circle(x, y, 59, 98, 2.5f)) + Fill(Circle(x, y, 69, 98, 2.5f)));
            return Over(c, Line, Stroke(Mathf.Min(Mathf.Min(wings, body), tail), 2f));
        }

        static Color SnorePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float z = Mathf.Min(Mathf.Min(SegDist(new Vector2(x, y), new Vector2(16, 50), new Vector2(48, 50)), SegDist(new Vector2(x, y), new Vector2(48, 50), new Vector2(16, 14))), SegDist(new Vector2(x, y), new Vector2(16, 14), new Vector2(48, 14)));
            c = Over(c, Line, Fill(z - 7f));
            return Over(c, Color.white, Fill(z - 4f));
        }

        static Color AnchorPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float shank = RoundRect(x, y, 64, 90, 6, 56, 3);
            float ring = Mathf.Abs(Circle(x, y, 64, 146, 10)) - 4;
            float stock = RoundRect(x, y, 64, 120, 30, 5, 3);
            float arms = Mathf.Abs(Circle(x, y, 64, 70, 40)) - 6;
            arms = Mathf.Max(arms, y - 60);
            float all = Min(shank, ring, stock, arms);
            c = Over(c, Hex("5A5F68"), Fill(all));
            return Over(c, Line, Stroke(all, 3f));
        }

        static Color XMarkPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float x1 = SegDist(new Vector2(x, y), new Vector2(24, 24), new Vector2(104, 104)), x2 = SegDist(new Vector2(x, y), new Vector2(24, 104), new Vector2(104, 24));
            c = Over(c, Hex("6B4426"), Fill(Mathf.Min(x1, x2) - 14f) * 0.5f);
            return Over(c, Red, Fill(Mathf.Min(x1, x2) - 9f));
        }

        static Color PirateBarrelsPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            var spots = new[] { new Vector3(44, 44, 30), new Vector3(88, 52, 26), new Vector3(62, 90, 28) };
            foreach (var b in spots)
            {
                float barrel = Circle(x, y, b.x, b.y, b.z);
                c = Over(c, new Color(0, 0, 0, 0.3f), Fill(Circle(x, y, b.x + 4, b.y - 4, b.z)) * (1f - Fill(barrel)));
                c = Over(c, Hex("8A5A32"), Fill(barrel));
                c = Over(c, Hex("4A4A50"), Stroke(Circle(x, y, b.x, b.y, b.z * 0.7f), 3f));
                c = Over(c, Line, Stroke(barrel, 2.5f));
            }
            return c;
        }
    }
}
