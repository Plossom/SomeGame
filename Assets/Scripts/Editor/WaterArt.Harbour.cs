using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Harbor Havoc's art (Water Cup race 2), seen from above: the harbour scenery sheet
    /// (<see cref="SomeGame.Track.HarbourSheet"/>), the quays' paving stones, and the big moving pieces - the car ferry,
    /// the cruise ship, the fireboat, the seaplane, the barge loaded with containers, the crane's container, a rubber
    /// duck, the water park's slide tower, the booms that close the ferry route, the water jets and the drops on the
    /// screen.
    /// </summary>
    public static partial class WaterArt
    {
        [MenuItem("SomeGame/Generate Harbour Art")]
        public static void GenerateHarbour()
        {
            string f = ArtGenerator.Folder;
            Write(f, "SceneryHarbourWater", 1024, 1024, HarbourCell, 128, false);
            ArtGenerator.Material("SceneryHarbourWater", "SceneryHarbourWater");
            Write(f, "QuayStone", 128, 128, QuayStonePixel, 32, true);
            ArtGenerator.Material("QuayStoneMat", "QuayStone");
            Write(f, "Ferry", 320, 768, FerryPixel, 22, false);
            Write(f, "FerryDeck", 320, 768, FerryDeckPixel, 22, false);
            Write(f, "CruiseShip", 384, 1536, CruisePixel, 21, false);
            Write(f, "Fireboat", 160, 320, FireboatPixel, 40, false);
            Write(f, "SeaplaneTop", 320, 256, SeaplaneTopPixel, 40, false);
            Write(f, "Container", 128, 256, ContainerPixel, 48, false);
            Write(f, "Barge", 320, 640, BargePixel, 28, false);
            Write(f, "RubberDuck", 64, 64, DuckPixel, 64, false);
            Write(f, "SlideTower", 256, 256, SlideTowerPixel, 40, false);
            Write(f, "Boom", 512, 48, BoomPixel, 48, false);
            Write(f, "WaterArc", 64, 256, WaterArcPixel, 32, false);
            Write(f, "ScreenDrop", 64, 64, ScreenDropPixel, 100, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static readonly Color Roof1 = Hex("C8553D"), Roof2 = Hex("2E86AB"), Roof3 = Hex("F2A541"), Roof4 = Hex("3BA99C");

        static Color HarbourCell(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return SomeGame.Track.HarbourSheet.Cells[row * 4 + col] switch
            {
                "HouseRed" => House(lx, ly, Roof1, Hex("F6E7C8")),
                "HouseBlue" => House(lx, ly, Roof2, Hex("FDF6E3")),
                "HouseYellow" => House(lx, ly, Roof3, Hex("FFFFFF")),
                "HouseTeal" => House(lx, ly, Roof4, Hex("F9E4D4")),
                "Containers" => Containers(lx, ly),
                "Crates" => CratesStack(lx, ly),
                "Bollards" => BollardPair(lx, ly),
                "Barrels" => Barrels(lx, ly),
                "Car" => TopCar(lx, ly),
                "People" => People(lx, ly),
                "Nets" => FishNets(lx, ly),
                "FishStall" => FishStall(lx, ly),
                "Palm" => Palm(lx, ly, 128, 128, 1f, 0.7f),
                "Cafe" => Cafe(lx, ly),
                "Seal" => SealPixel(lx, ly),
                _ => Lifebuoys(lx, ly),
            };
        }

        // A harbour house from above: a pitched tiled roof in one colour, a chimney, a little balcony at the front.
        static Color House(float x, float y, Color roof, Color wall)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(RoundRect(x, y, 140, 116, 96, 86, 6) + 4f));
            float walls = RoundRect(x, y, 128, 128, 96, 86, 4);
            c = Over(c, wall, Fill(walls));
            float r = RoundRect(x, y, 128, 136, 88, 74, 4);
            bool left = x < 128f;
            Color tile = Mathf.Repeat(y, 14f) < 2f ? Shade(roof, 0.8f) : left ? roof : Shade(roof, 0.88f);
            c = Over(c, tile, Fill(r));
            c = Over(c, Shade(roof, 0.7f), Fill(Mathf.Abs(x - 128f) - 2.5f) * Fill(r));
            c = Over(c, Hex("8E8A84"), Fill(RoundRect(x, y, 170, 170, 9, 12, 2)));
            // The balcony and its door over the water side (bottom).
            float balcony = RoundRect(x, y, 128, 48, 40, 12, 3);
            c = Over(c, Hex("9C6B3C"), Fill(balcony));
            c = Over(c, Hex("3A2A1C"), Fill(RoundRect(x, y, 128, 60, 12, 4, 1)));
            return Over(c, Line, Stroke(Mathf.Min(walls, balcony), 3f));
        }

        static Color Containers(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            Color[] cols = { Hex("D9483B"), Hex("2E86AB"), Hex("F2A541"), Hex("3BA99C"), Hex("8C5A9E"), Hex("D9483B") };
            for (int i = 0; i < 6; i++)
            {
                float cx = 70 + (i % 2) * 116, cy = 52 + (i / 2) * 76;
                float box = RoundRect(x, y, cx, cy, 54, 32, 2);
                c = Over(c, Shadow, Fill(box - 6f));
                c = Over(c, Mathf.Repeat(x, 8f) < 2f ? Shade(cols[i], 0.82f) : cols[i], Fill(box));
                c = Over(c, Line, Stroke(box, 2.5f));
            }
            return c;
        }

        static Color CratesStack(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (cx, cy, a) in new[] { (90f, 100f, 0.1f), (160f, 92f, -0.2f), (124f, 160f, 0.05f) })
            {
                float box = RotRect(x, y, cx, cy, 34, 30, 3, a);
                c = Over(c, Shadow, Fill(RotRect(x, y, cx + 8, cy - 8, 34, 30, 3, a)));
                c = Over(c, Mathf.Repeat(x + y, 16f) < 2f ? Hex("8A5A32") : Hex("C08A52"), Fill(box));
                c = Over(c, Hex("8A5A32"), Fill(Mathf.Min(Mathf.Abs(x - cx) - 2f, Mathf.Abs(y - cy) - 2f)) * Fill(box));
                c = Over(c, Line, Stroke(box, 2.5f));
            }
            return c;
        }

        static Color BollardPair(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (float bx in new[] { 80f, 176f })
            {
                c = Over(c, Shadow, Fill(Circle(x, y, bx + 6, 122, 26)));
                float b = Circle(x, y, bx, 128, 24);
                c = Over(c, Hex("2A2F36"), Fill(b));
                c = Over(c, Hex("4A535E"), Fill(Circle(x, y, bx - 5, 133, 12)));
                c = Over(c, Line, Stroke(b, 2.5f));
            }
            // A rope from one to the other, slack.
            float rope = Mathf.Abs(y - (100f + 14f * Mathf.Sin((x - 80f) / 96f * Mathf.PI))) - 3f;
            return Over(c, Hex("E8D7AE"), Fill(rope) * Fill(Mathf.Abs(x - 128f) - 48f));
        }

        static Color Barrels(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (bx, by, col) in new[] { (90f, 100f, "2E86AB"), (150f, 92f, "D9483B"), (120f, 156f, "F2A541"), (176f, 150f, "2E86AB") })
            {
                c = Over(c, Shadow, Fill(Circle(x, y, bx + 6, by - 6, 30)));
                float b = Circle(x, y, bx, by, 28);
                c = Over(c, Hex(col), Fill(b));
                c = Over(c, Shade(Hex(col), 0.7f), Fill(Shell(Circle(x, y, bx, by, 18), 2.5f)));
                c = Over(c, Line, Stroke(b, 2.5f));
            }
            return c;
        }

        static Color TopCar(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(RoundRect(x, y, 136, 120, 42, 86, 22)));
            float body = RoundRect(x, y, 128, 128, 42, 86, 22);
            c = Over(c, Hex("4FB3E8"), Fill(body));
            c = Over(c, Hex("1D2733"), Fill(RoundRect(x, y, 128, 150, 32, 18, 8)));
            c = Over(c, Hex("1D2733"), Fill(RoundRect(x, y, 128, 92, 32, 12, 6)));
            c = Over(c, Hex("FFF6D8"), Fill(Mathf.Min(Circle(x, y, 104, 210, 7), Circle(x, y, 152, 210, 7))));
            return Over(c, Line, Stroke(body, 2.5f));
        }

        static Color People(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            string[] shirts = { "FF4F86", "2EC4B6", "FFC93C", "2E86AB", "FF8A1E", "FFFFFF" };
            for (int i = 0; i < 7; i++)
            {
                float px = 50 + Hash(i, 1) * 156, py = 50 + Hash(i, 2) * 156;
                c = Over(c, Shadow, Fill(Circle(x, y, px + 5, py - 5, 18)));
                float body = Ellipse(x, y, px, py, 18, 12);
                c = Over(c, Hex(shirts[i % shirts.Length]), Fill(body));
                float head = Circle(x, y, px, py + 3, 9);
                c = Over(c, i % 3 == 0 ? Hex("F2D49B") : Hex("6B4426"), Fill(head)); // a sunhat or hair
                c = Over(c, Line, Stroke(Mathf.Min(body, head), 2f));
            }
            return c;
        }

        static Color FishNets(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float heap = Circle(x, y, 128, 128, 90) + Noise(x / 16f, y / 16f, 32) * 22f;
            c = Over(c, Shadow, Fill(heap - 6f));
            bool mesh = Mathf.Repeat(x + y, 14f) < 2f || Mathf.Repeat(x - y, 14f) < 2f;
            c = Over(c, mesh ? Hex("2C6E5A") : Hex("4FA08A"), Fill(heap));
            foreach (var (fx, fy) in new[] { (100f, 140f), (150f, 110f), (130f, 170f) })
                c = Over(c, Hex("FF8A1E"), Fill(Circle(x, y, fx, fy, 9)));
            return c;
        }

        static Color FishStall(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(RoundRect(x, y, 140, 116, 100, 70, 6)));
            // A striped awning over a counter of ice and fish.
            float awning = RoundRect(x, y, 128, 150, 100, 46, 4);
            c = Over(c, Mathf.Repeat(x, 40f) < 20f ? Hex("2E86AB") : Color.white, Fill(awning));
            c = Over(c, Line, Stroke(awning, 2.5f));
            float counter = RoundRect(x, y, 128, 82, 92, 26, 4);
            c = Over(c, Hex("DDEFF6"), Fill(counter));
            for (int i = 0; i < 6; i++)
            {
                float fish = Ellipse(x, y, 56 + i * 28, 82, 11, 6);
                c = Over(c, i % 2 == 0 ? Hex("8FA6B5") : Hex("E8835A"), Fill(fish));
            }
            return Over(c, Line, Stroke(counter, 2.5f));
        }

        static Color Cafe(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // Little round tables with umbrellas.
            foreach (var (tx, ty, col) in new[] { (76f, 80f, "FF4F86"), (180f, 92f, "2EC4B6"), (126f, 176f, "FFC93C") })
            {
                c = Over(c, Shadow, Fill(Circle(x, y, tx + 10, ty - 10, 44)));
                float a = Mathf.Atan2(y - ty, x - tx);
                bool stripe = Mathf.Repeat(a / (Mathf.PI * 2f) * 8f, 2f) < 1f;
                float u = Circle(x, y, tx, ty, 44);
                c = Over(c, stripe ? Hex(col) : Color.white, Fill(u));
                c = Over(c, Line, Stroke(u, 2.2f));
            }
            return c;
        }

        static Color SealPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // A seal lying on a round buoy, flippers out.
            float buoy = Circle(x, y, 128, 110, 56);
            c = Over(c, Shadow, Fill(Circle(x, y, 140, 98, 56)));
            c = Over(c, Mathf.Abs(y - 110f) < 14f ? Color.white : Hex("E8423A"), Fill(buoy));
            c = Over(c, Line, Stroke(buoy, 3f));
            float body = Ellipse(x, y, 128, 128, 26, 54);
            float flippers = Mathf.Min(Ellipse(x, y, 96, 130, 16, 8), Ellipse(x, y, 160, 130, 16, 8));
            float tail = Triangle(x, y, new Vector2(112, 76), new Vector2(144, 76), new Vector2(128, 92));
            float all = Min(body, flippers, tail);
            c = Over(c, Hex("7A8794"), Fill(all));
            c = Over(c, Hex("9AA7B4"), Fill(Ellipse(x, y, 122, 140, 12, 30)));
            float head = Circle(x, y, 128, 180, 20);
            c = Over(c, Hex("7A8794"), Fill(head));
            c = Over(c, Line, Fill(Mathf.Min(Circle(x, y, 120, 186, 3), Circle(x, y, 136, 186, 3))));
            c = Over(c, Hex("2A2F36"), Fill(Circle(x, y, 128, 196, 4)));
            return Over(c, Line, Stroke(Mathf.Min(all, head), 2.5f));
        }

        static Color Lifebuoys(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (var (bx, by) in new[] { (90f, 120f), (166f, 140f) })
            {
                float ring = Shell(Circle(x, y, bx, by, 40), 13f);
                c = Over(c, Shadow, Fill(Shell(Circle(x, y, bx + 6, by - 6, 40), 13f)));
                float a = Mathf.Atan2(y - by, x - bx);
                c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 8f, 2f) < 1f ? Hex("E8423A") : Color.white, Fill(ring));
                c = Over(c, Line, Stroke(ring, 2.2f));
            }
            return c;
        }

        static Color QuayStonePixel(float x, float y)
        {
            // Big paving slabs in rows, each a slightly different stone, fine joints.
            int row = Mathf.FloorToInt(y / 32f);
            float sx = x + (row % 2) * 24f;
            int colI = Mathf.FloorToInt(sx / 48f);
            float h = Hash(colI * 7 + 3, row * 5 + 1);
            Color stone = Color.Lerp(Hex("C9C2B4"), Hex("DCD5C6"), h);
            stone = Color.Lerp(stone, Hex("B8B0A0"), Noise(x / 8f, y / 8f, 16) * 0.25f);
            bool joint = Mathf.Repeat(y, 32f) < 1.6f || Mathf.Repeat(sx, 48f) < 1.6f;
            return joint ? Hex("9A9284") : stone;
        }

        // ------------------------------------------------------------------ the big moving pieces

        // A car ferry from above, bow up: a white hull, a blue band, the bridge across the front, lifeboats; the car deck
        // down the middle is open (drawn by FerryDeck, under the cars).
        static float FerryHull(float x, float y) => Mathf.Min(RoundRect(x, y, 160, 384, 140, 330, 60), Ellipse(x, y, 160, 690, 120, 70));

        static Color FerryPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = FerryHull(x, y);
            float deck = RoundRect(x, y, 160, 384, 70, 420, 4);
            // Hull sides (the deck through the middle is left open: you drive through it).
            float sides = Mathf.Max(hull, -deck);
            c = Over(c, Color.white, Fill(sides));
            c = Over(c, Hex("1F6FB2"), Fill(Mathf.Max(Shell(hull + 14f, 6f), -deck)));
            for (int i = 0; i < 6; i++)
                foreach (float s in new[] { -1f, 1f })
                {
                    float lifeboat = RoundRect(x, y, 160 + s * 112, 180 + i * 80, 12, 28, 10);
                    c = Over(c, Hex("FF8A1E"), Fill(lifeboat));
                    c = Over(c, Line, Stroke(lifeboat, 2f));
                }
            c = Over(c, Line, Stroke(sides, 4f));
            // The bridge across the front, high over the deck (you pass underneath): windows and the funnel.
            float bridge = RoundRect(x, y, 160, 600, 140, 40, 10);
            c = Over(c, new Color(0f, 0f, 0f, 0.2f), Fill(RoundRect(x, y, 168, 590, 140, 40, 10)));
            c = Over(c, Color.white, Fill(bridge));
            c = Over(c, Hex("1D2733"), Fill(RoundRect(x, y, 160, 622, 120, 6, 3)));
            c = Over(c, Line, Stroke(bridge, 3f));
            float funnel = Ellipse(x, y, 160, 700, 26, 18);
            c = Over(c, Hex("E8423A"), Fill(funnel));
            c = Over(c, Hex("2A2F36"), Fill(Ellipse(x, y, 160, 700, 14, 9)));
            return Over(c, Line, Stroke(funnel, 2.5f));
        }

        static Color FerryDeckPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float deck = RoundRect(x, y, 160, 384, 70, 420, 4);
            c = Over(c, Hex("5A6470"), Fill(deck));
            // Painted lanes and arrows, parked cars along both sides of the deck.
            c = Over(c, Hex("FFD24D"), Fill(Mathf.Abs(x - 160f) - 3f) * Fill(deck) * (Mathf.Repeat(y, 60f) < 32f ? 1f : 0f));
            string[] cols = { "E8423A", "4FB3E8", "FFC93C", "FFFFFF", "3BA99C", "8C5A9E" };
            for (int i = 0; i < 7; i++)
                foreach (float s in new[] { -1f, 1f })
                {
                    float car = RoundRect(x, y, 160 + s * 50, 100 + i * 90, 14, 30, 8);
                    c = Over(c, Hex(cols[(i + (s > 0 ? 2 : 0)) % cols.Length]), Fill(car));
                    c = Over(c, Hex("1D2733"), Fill(RoundRect(x, y, 160 + s * 50, 108 + i * 90, 10, 8, 3)));
                    c = Over(c, Line, Stroke(car, 2f));
                }
            return c;
        }

        // A gigantic cruise ship from above, bow up: tiers of decks, a pool with loungers, the funnels, rows of
        // balconies along the sides.
        static Color CruisePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = Mathf.Min(RoundRect(x, y, 192, 700, 176, 640, 80), Ellipse(x, y, 192, 1320, 150, 200));
            c = Over(c, Color.white, Fill(hull));
            c = Over(c, Hex("1D3A5C"), Fill(Shell(hull + 10f, 5f)));
            // Balcony rows along both sides.
            foreach (float s in new[] { -1f, 1f })
                c = Over(c, Mathf.Repeat(y, 22f) < 12f ? Hex("4FB3E8") : Hex("E9EEF2"), Fill(RoundRect(x, y, 192 + s * 150, 700, 10, 560, 4)));
            // The decks: a long sun deck, the pool, two funnels, the bow deck.
            float sun = RoundRect(x, y, 192, 720, 120, 420, 30);
            c = Over(c, Hex("E5D9C3"), Fill(sun));
            float pool = RoundRect(x, y, 192, 820, 60, 120, 30);
            c = Over(c, Hex("3FCDE0"), Fill(pool));
            c = Over(c, Color.white, Fill(Shell(pool, 4f)));
            for (int i = 0; i < 8; i++)
                foreach (float s in new[] { -1f, 1f })
                {
                    float lounger = RoundRect(x, y, 192 + s * 92, 700 + i * 34, 16, 6, 3);
                    c = Over(c, i % 2 == 0 ? Hex("FF4F86") : Hex("FFC93C"), Fill(lounger));
                }
            foreach (float fy in new[] { 520f, 400f })
            {
                float funnel = Ellipse(x, y, 192, fy, 50, 34);
                c = Over(c, Hex("E8423A"), Fill(funnel));
                c = Over(c, Hex("1D2733"), Fill(Ellipse(x, y, 192, fy, 30, 18)));
                c = Over(c, Line, Stroke(funnel, 3f));
            }
            float bow = Ellipse(x, y, 192, 1300, 90, 120);
            c = Over(c, Hex("C9D3DC"), Fill(bow));
            c = Over(c, Hex("FFD24D"), Fill(Circle(x, y, 192, 1300, 26)));
            c = Over(c, Hex("1D2733"), Fill(RoundRect(x, y, 192, 1150, 140, 10, 4)));
            return Over(c, Line, Stroke(hull, 5f));
        }

        static Color FireboatPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = Mathf.Min(RoundRect(x, y, 80, 140, 56, 110, 26), Ellipse(x, y, 80, 260, 50, 46));
            c = Over(c, Hex("D9312B"), Fill(hull));
            c = Over(c, Color.white, Fill(Shell(hull + 8f, 3f)));
            float cabin = RoundRect(x, y, 80, 150, 36, 40, 8);
            c = Over(c, Color.white, Fill(cabin));
            c = Over(c, Hex("1D2733"), Fill(RoundRect(x, y, 80, 178, 28, 6, 3)));
            foreach (float cy in new[] { 80f, 240f })
            {
                float cannon = Circle(x, y, 80, cy, 16);
                c = Over(c, Hex("C9D3DC"), Fill(cannon));
                c = Over(c, Hex("2A2F36"), Fill(Circle(x, y, 80, cy, 7)));
                c = Over(c, Line, Stroke(cannon, 2f));
            }
            return Over(c, Line, Stroke(hull, 4f));
        }

        static Color SeaplaneTopPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // From above, nose right: the wing across, the fuselage, two floats under it, the tail.
            float floats = Mathf.Min(RoundRect(x, y, 170, 92, 90, 10, 9), RoundRect(x, y, 170, 164, 90, 10, 9));
            c = Over(c, Hex("2A2F36"), Fill(floats));
            float body = Mathf.Min(RoundRect(x, y, 160, 128, 120, 20, 18), Triangle(x, y, new Vector2(40, 108), new Vector2(40, 148), new Vector2(14, 128)));
            float wing = RoundRect(x, y, 190, 128, 26, 120, 10);
            float tail = RoundRect(x, y, 34, 128, 12, 44, 8);
            c = Over(c, Hex("FFC93C"), Fill(body));
            c = Over(c, Hex("FF4F86"), Fill(Mathf.Min(wing, tail)));
            c = Over(c, Hex("8FDDFF"), Fill(RoundRect(x, y, 236, 128, 14, 12, 6)));
            c = Over(c, Line, Stroke(Min(body, wing, tail), 3f));
            return Over(c, Hex("2A2F36"), Fill(RoundRect(x, y, 284, 128, 4, 40, 2)));
        }

        static Color ContainerPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float box = RoundRect(x, y, 64, 128, 52, 116, 3);
            c = Over(c, Mathf.Repeat(y, 12f) < 3f ? Hex("A8352A") : Hex("D9483B"), Fill(box));
            c = Over(c, Hex("E8D7AE"), Fill(RoundRect(x, y, 64, 128, 20, 10, 3)));
            return Over(c, Line, Stroke(box, 3f));
        }

        // A barge from above, across the channel (its length runs up the picture): a black hull, containers stacked on
        // its deck in rows, a little wheelhouse at one end.
        static Color BargePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hull = RoundRect(x, y, 160, 320, 140, 300, 40);
            c = Over(c, Hex("2A2F36"), Fill(hull));
            c = Over(c, Hex("E8423A"), Fill(Shell(hull + 8f, 4f)));
            Color[] cols = { Hex("2E86AB"), Hex("F2A541"), Hex("3BA99C"), Hex("D9483B"), Hex("8C5A9E") };
            for (int row = 0; row < 6; row++)
                for (int k = 0; k < 2; k++)
                {
                    float box = RoundRect(x, y, 104 + k * 112, 110 + row * 76, 50, 32, 2);
                    var col = cols[(row * 2 + k) % cols.Length];
                    c = Over(c, Mathf.Repeat(x, 8f) < 2f ? Shade(col, 0.82f) : col, Fill(box));
                    c = Over(c, Line, Stroke(box, 2.2f));
                }
            float house = RoundRect(x, y, 160, 56, 50, 24, 6);
            c = Over(c, Color.white, Fill(house));
            c = Over(c, Hex("1D2733"), Fill(RoundRect(x, y, 160, 66, 40, 5, 2)));
            return Over(c, Line, Stroke(hull, 4f));
        }

        static Color DuckPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float body = Ellipse(x, y, 30, 28, 20, 16);
            float head = Circle(x, y, 32, 44, 11);
            c = Over(c, Hex("FFD23F"), Fill(Mathf.Min(body, head)));
            c = Over(c, Hex("FF8A1E"), Fill(Ellipse(x, y, 32, 56, 6, 4)));
            c = Over(c, Line, Fill(Circle(x, y, 28, 47, 1.8f)));
            return Over(c, Line, Stroke(Mathf.Min(body, head), 2f));
        }

        static Color SlideTowerPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Fill(RoundRect(x, y, 140, 116, 92, 92, 14)));
            float deck = RoundRect(x, y, 128, 128, 92, 92, 14);
            c = Over(c, Hex("2EC4B6"), Fill(deck));
            c = Over(c, Hex("FFFFFF"), Fill(Shell(deck + 10f, 3f)));
            // A canopy roof in stripes, a flag on top.
            float roof = Circle(x, y, 128, 128, 62);
            float a = Mathf.Atan2(y - 128f, x - 128f);
            c = Over(c, Mathf.Repeat(a / (Mathf.PI * 2f) * 10f, 2f) < 1f ? Hex("FF4F86") : Hex("FFD24D"), Fill(roof));
            c = Over(c, Line, Stroke(roof, 3f));
            return Over(c, Line, Stroke(deck, 3f));
        }

        static Color BoomPixel(float x, float y)
        {
            float bar = RoundRect(x, y, 256, 24, 250, 14, 10);
            Color c = Over(new Color(0, 0, 0, 0), Mathf.Repeat(x, 64f) < 32f ? Hex("E8423A") : Color.white, Fill(bar));
            return Over(c, Line, Stroke(bar, 3f));
        }

        // A jet of water: a white core in a pale blue stream, droplets breaking off its edges (it runs up the picture).
        static Color WaterArcPixel(float x, float y)
        {
            float width = Mathf.Lerp(8f, 20f, y / 256f);
            float d = Mathf.Abs(x - 32f) - width;
            Color c = new Color(0.75f, 0.92f, 1f, 0.75f * Fill(d));
            c = Over(c, White(0.9f), Fill(Mathf.Abs(x - 32f) - width * 0.4f));
            for (int i = 0; i < 12; i++)
            {
                float dx = 32 + (Hash(i, 3) - 0.5f) * width * 2.6f, dy = Hash(i, 5) * 256f;
                c = Over(c, White(0.85f), Fill(Circle(x, y, dx, dy, 2.5f + Hash(i, 7) * 2f)));
            }
            return c;
        }

        static Color ScreenDropPixel(float x, float y)
        {
            float d = Circle(x, y, 32, 32, 26);
            Color c = new Color(0.85f, 0.95f, 1f, 0.35f * Fill(d));
            c = Over(c, White(0.8f), Stroke(d, 3f) * Mathf.Clamp01((y - 30f) / 20f));
            return Over(c, White(0.9f), Fill(Circle(x, y, 24, 42, 6)));
        }
    }
}
