using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The garage's parts, seen from above like the buggy and on its 192 x 336 canvas (nose up, the wheels in the
    /// same four spots, so every body drives and steers the same):
    /// <list type="bullet">
    /// <item>Bodies (tinted with the paint; details untinted): RALLY (a hatchback with fender flares), KART (an open
    /// go-kart, the driver's helmet in your paint) and the FARM PICKUP (Rosie Rust's: a cab and a wooden bed).
    /// Each as Car{Name} (all four wheels, for the menus), Car{Name}Body (no front wheels, for races) and
    /// Car{Name}Details.</item>
    /// <item>Tops, laid over the car: a spoiler, a roof rack with luggage, a light bar, Rosie's hay-bale spoiler
    /// (two bales and a pitchfork) and the gold wing.</item>
    /// <item>Decals on the nose and tail: racing stripes, a number, flames, checkers and the gold Valley crest.</item>
    /// <item>Trail particles (flame, snowflake, confetti) and the garage's tab icons.</item>
    /// </list>
    /// </summary>
    public static partial class ArtGenerator
    {
        [MenuItem("SomeGame/Generate Garage Parts")]
        public static void GenerateGarageParts()
        {
            foreach (var (name, body, details) in new (string, System.Func<float, float, float>, System.Func<float, float, Color>)[]
                     { ("Rally", RallySdf, RallyDetails), ("Kart", KartSdf, KartDetails), ("Pickup", PickupSdf, PickupDetails) })
            {
                string n = name;
                World("Car" + n, 192, 336, (x, y) => PartBody(n, x, y, false), 192, false);
                World("Car" + n + "Body", 192, 336, (x, y) => PartBody(n, x, y, true), 192, false);
                World("Car" + n + "Details", 192, 336, details, 192, false);
            }
            World("TopSpoiler", 192, 336, (x, y) => Wing(x, y, false), 192, false);
            World("TopGoldWing", 192, 336, (x, y) => Wing(x, y, true), 192, false);
            World("TopRoofRack", 192, 336, RoofRackPixel, 192, false);
            World("TopLightBar", 192, 336, LightBarPixel, 192, false);
            World("TopHayBales", 192, 336, HayBalesPixel, 192, false);
            World("DecalStripes", 192, 336, StripesPixel, 192, false);
            World("DecalNumber", 192, 336, NumberPixel, 192, false);
            World("DecalFlames", 192, 336, FlamesPixel, 192, false);
            World("DecalCheckers", 192, 336, CheckersPixel, 192, false);
            World("DecalCrest", 192, 336, CrestPixel, 192, false);
            World("TrailFlame", 64, 64, (x, y) => White(Fill(Min(Circle(x, y, 32, 24, 16), Triangle(x, y, new Vector2(18, 30), new Vector2(46, 30), new Vector2(32, 60)))) * 0.95f), 64, false);
            World("TrailFlake", 64, 64, (x, y) =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    Vector2 u = new(Mathf.Cos(a), Mathf.Sin(a));
                    d = Mathf.Min(d, Capsule(x, y, 32 - u.x * 26, 32 - u.y * 26, 32 + u.x * 26, 32 + u.y * 26, 3.2f));
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Vector2 p = new Vector2(32, 32) + u * 15f * s, q = new(-u.y, u.x);
                        d = Mathf.Min(d, Capsule(x, y, p.x, p.y, p.x + (u.x * s + q.x) * 7f, p.y + (u.y * s + q.y) * 7f, 2.4f));
                        d = Mathf.Min(d, Capsule(x, y, p.x, p.y, p.x + (u.x * s - q.x) * 7f, p.y + (u.y * s - q.y) * 7f, 2.4f));
                    }
                }
                return White(Fill(d));
            }, 64, false);
            World("TrailConfetti", 32, 32, (x, y) => White(Fill(RotRect(x, y, 16, 16, 11, 6, 1.5f, 0.35f))), 64, false);

            Ui("IconPaint", 128, 128, (x, y) => Fill(Min(Circle(x, y, 64, 46, 34), Triangle(x, y, new Vector2(32, 54), new Vector2(96, 54), new Vector2(64, 118)))));
            Ui("IconBody", 128, 128, (x, y) =>
            {
                float shell = Min(RoundRect(x, y, 64, 52, 54, 18, 10), RoundRect(x, y, 60, 76, 30, 16, 12));
                float wheels = Min(Circle(x, y, 32, 34, 15), Circle(x, y, 96, 34, 15));
                bool window = RoundRect(x, y, 60, 76, 20, 8, 4) < 0f;
                return window ? 0f : Mathf.Max(Fill(shell) * (1f - Fill(Circle(x, y, 32, 34, 21)) - Fill(Circle(x, y, 96, 34, 21))), Fill(wheels));
            });
            Ui("IconWing", 128, 128, (x, y) => Fill(Min(RoundRect(x, y, 64, 90, 56, 12, 6), Capsule(x, y, 40, 84, 40, 36, 7), Capsule(x, y, 88, 84, 88, 36, 7), RoundRect(x, y, 64, 30, 40, 9, 4))));
            Ui("IconDecal", 128, 128, (x, y) =>
            {
                float sticker = RoundRect(x, y, 64, 64, 50, 50, 16);
                float peel = Triangle(x, y, new Vector2(80, 114), new Vector2(114, 114), new Vector2(114, 80));
                float star = Star(x, y, 58, 58, 30, 13);
                return Fill(sticker) * (1f - Fill(star)) * (1f - Fill(peel - 1f)) + Fill(Triangle(x, y, new Vector2(84, 110), new Vector2(110, 84), new Vector2(84, 84)));
            });
            Ui("IconTrail", 128, 128, (x, y) => Fill(Min(Star(x, y, 86, 80, 32, 10, 4), Star(x, y, 40, 40, 20, 7, 4), Circle(x, y, 34, 96, 8), Circle(x, y, 96, 30, 7))));
            Ui("IconGadget", 128, 128, (x, y) =>
            {
                // A spanner and a bolt.
                float jaw = Circle(x, y, 92, 92, 24);
                float notch = RotRect(x, y, 102, 102, 9, 20, 2, -0.785f);
                float handle = Capsule(x, y, 30, 30, 84, 84, 11);
                float spanner = Mathf.Max(Mathf.Min(jaw, handle), -notch);
                return Fill(Mathf.Min(spanner, Circle(x, y, 30, 30, 15))) * (1f - Fill(Circle(x, y, 30, 30, 6)));
            });
            Ui("IconCards", 128, 128, (x, y) =>
            {
                float back = RotRect(x, y, 52, 66, 30, 42, 8, 0.24f);
                float front = RotRect(x, y, 76, 60, 30, 42, 8, -0.17f);
                float gap = RotRect(x, y, 76, 60, 36, 48, 11, -0.17f);
                return Mathf.Max(Fill(back) * (1f - Fill(gap)), Fill(front) * (1f - Fill(Star(x, y, 76, 60, 15, 6))));
            });
            AssetDatabase.Refresh();
        }

        /// <summary>Fills Resources/GarageArt with the parts' pictures (run after Generate Garage Parts).</summary>
        [MenuItem("SomeGame/Build Garage Art")]
        public static void BuildGarageArt()
        {
            const string path = "Assets/Resources/GarageArt.asset";
            var art = AssetDatabase.LoadAssetAtPath<SomeGame.Race.GarageArt>(path);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<SomeGame.Race.GarageArt>();
                AssetDatabase.CreateAsset(art, path);
            }
            Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
            art.entries.Clear();
            void Add(string id, string full, string race = null, string details = null) =>
                art.entries.Add(new SomeGame.Race.GarageArt.Entry { id = id, full = S(full), race = race != null ? S(race) : null, details = details != null ? S(details) : null });
            Add("body.buggy", "Car", "CarBody", "CarDetails");
            Add("body.rally", "CarRally", "CarRallyBody", "CarRallyDetails");
            Add("body.kart", "CarKart", "CarKartBody", "CarKartDetails");
            Add("body.pickup", "CarPickup", "CarPickupBody", "CarPickupDetails");
            Add("top.spoiler", "TopSpoiler"); Add("top.roofrack", "TopRoofRack"); Add("top.lightbar", "TopLightBar");
            Add("top.haybales", "TopHayBales"); Add("top.goldwing", "TopGoldWing");
            Add("decal.stripes", "DecalStripes"); Add("decal.number", "DecalNumber"); Add("decal.flames", "DecalFlames");
            Add("decal.checkers", "DecalCheckers"); Add("decal.crest", "DecalCrest");
            Add("trail.sparkle", "Sparkle"); Add("trail.flame", "TrailFlame"); Add("trail.flake", "TrailFlake"); Add("trail.confetti", "TrailConfetti");
            Add("gadget.haybale", "HayBale");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        // ---------- bodies ----------

        // Wheels on their arms (as the buggy has them), the body over them, then the body's own shading.
        static Color PartBody(string name, float x, float y, bool raceBody)
        {
            Color c = new(0, 0, 0, 0);
            float arms = Min(Capsule(x, y, 48, 254, 28, 262, 4), Capsule(x, y, 48, 278, 28, 272, 4), Capsule(x, y, 144, 254, 164, 262, 4), Capsule(x, y, 144, 278, 164, 272, 4),
                             Capsule(x, y, 54, 62, 24, 66, 4.5f), Capsule(x, y, 54, 84, 24, 80, 4.5f), Capsule(x, y, 138, 62, 168, 66, 4.5f), Capsule(x, y, 138, 84, 168, 80, 4.5f));
            c = Over(c, new Color(0.16f, 0.16f, 0.16f), Fill(arms));
            bool keep = _bodyOnly;
            _bodyOnly = raceBody;
            float wheels = WheelSdf(x, y);
            _bodyOnly = keep;
            bool tread = Mathf.Repeat(y, 9f) < 4f;
            c = Over(c, tread ? new Color(0.11f, 0.11f, 0.11f) : new Color(0.17f, 0.17f, 0.17f), Fill(wheels));
            c = Over(c, Outline, Stroke(wheels, 4f));
            return name switch
            {
                "Rally" => RallyPaint(c, x, y),
                "Kart" => KartPaint(c, x, y),
                _ => PickupPaint(c, x, y),
            };
        }

        static Color Panel(Color c, float d, float x, float lightFrom = 0.8f)
        {
            float inside = Mathf.Clamp01(-d / 14f);
            float v = Mathf.Lerp(lightFrom, 1f, inside) - Mathf.Clamp01((x - 120f) / 60f) * 0.08f;
            return Over(c, new Color(v, v, v), Fill(d));
        }

        static readonly Color Glass = new(0.14f, 0.16f, 0.2f);

        // Rally: a closed hatchback, wide arches over the wheels, a windscreen, a roof and a rear window.
        static float RallySdf(float x, float y) => Min(
            SMin(RoundRect(x, y, 96, 172, 48, 140, 30), RoundRect(x, y, 96, 300, 40, 18, 16), 10f),
            RoundRect(x, y, 50, 266, 10, 36, 9), RoundRect(x, y, 142, 266, 10, 36, 9),
            RoundRect(x, y, 48, 72, 12, 40, 9), RoundRect(x, y, 144, 72, 12, 40, 9));

        static Color RallyPaint(Color c, float x, float y)
        {
            float body = RallySdf(x, y);
            c = Panel(c, body, x);
            c = Over(c, new Color(0.93f, 0.93f, 0.93f), Fill(RoundRect(x, y, 96, 270, 20, 34, 10)) * Fill(body + 6f)); // bonnet bulge
            float shield = Polygon(x, y, new[] { new Vector2(58, 236), new Vector2(134, 236), new Vector2(126, 204), new Vector2(66, 204) });
            c = Over(c, Glass, Fill(shield));
            float roof = RoundRect(x, y, 96, 158, 40, 44, 12);
            c = Over(c, new Color(1f, 1f, 1f), Fill(roof));
            c = Over(c, Outline, Stroke(roof, 2.5f));
            float back = Polygon(x, y, new[] { new Vector2(64, 112), new Vector2(128, 112), new Vector2(120, 94), new Vector2(72, 94) });
            c = Over(c, Glass, Fill(back));
            float mirrors = Min(RoundRect(x, y, 40, 216, 8, 5, 2), RoundRect(x, y, 152, 216, 8, 5, 2));
            c = Panel(c, mirrors, x);
            c = Over(c, Outline, Stroke(Min(body, mirrors), 4.5f));
            c = Over(c, Outline, Stroke(Min(shield, back), 3f));
            return c;
        }

        static Color RallyDetails(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, White(0.35f), Fill(Capsule(x, y, 70, 210, 82, 232, 3f)) * Fill(Polygon(x, y, new[] { new Vector2(58, 236), new Vector2(134, 236), new Vector2(126, 204), new Vector2(66, 204) })));
            float heads = Min(RoundRect(x, y, 66, 312, 11, 5, 3), RoundRect(x, y, 126, 312, 11, 5, 3));
            c = Over(c, Hex("FFF6D8"), Fill(heads));
            c = Over(c, Outline, Stroke(heads, 2.5f));
            float fogs = Min(Circle(x, y, 80, 322, 4), Circle(x, y, 112, 322, 4));
            c = Over(c, Hex("FFE58A"), Fill(fogs));
            float tails = Min(RoundRect(x, y, 62, 34, 10, 4, 2), RoundRect(x, y, 130, 34, 10, 4, 2));
            c = Over(c, Hex("E0303A"), Fill(tails));
            float vents = Min(Capsule(x, y, 84, 262, 84, 284, 2.4f), Capsule(x, y, 108, 262, 108, 284, 2.4f));
            c = Over(c, new Color(0.1f, 0.1f, 0.1f, 0.7f), Fill(vents));
            c = Over(c, new Color(0.1f, 0.1f, 0.1f, 0.85f), Fill(Capsule(x, y, 96, 30, 96, 26, 6f))); // exhaust
            return c;
        }

        // Kart: an open frame low between the wheels, a nose cone, side pods and the driver in a helmet.
        static float KartSdf(float x, float y) => Min(
            RoundRect(x, y, 96, 266, 30, 26, 12), Polygon(x, y, new[] { new Vector2(68, 292), new Vector2(124, 292), new Vector2(134, 322), new Vector2(58, 322) }) - 4f,
            RoundRect(x, y, 54, 166, 12, 58, 8), RoundRect(x, y, 138, 166, 12, 58, 8));

        static Color KartPaint(Color c, float x, float y)
        {
            float frame = Min(Capsule(x, y, 66, 238, 66, 52, 5), Capsule(x, y, 126, 238, 126, 52, 5), Capsule(x, y, 40, 40, 152, 40, 6), Capsule(x, y, 66, 110, 126, 110, 4));
            c = Over(c, new Color(0.22f, 0.22f, 0.24f), Fill(frame));
            c = Over(c, Outline, Stroke(frame, 2.5f));
            float floor = RoundRect(x, y, 96, 170, 28, 66, 10);
            c = Over(c, new Color(0.18f, 0.18f, 0.2f), Fill(floor));
            float engine = RoundRect(x, y, 124, 74, 18, 18, 5);
            c = Over(c, new Color(0.35f, 0.35f, 0.38f), Fill(engine));
            c = Over(c, Outline, Stroke(engine, 3f));
            float body = KartSdf(x, y);
            c = Panel(c, body, x);
            c = Over(c, Outline, Stroke(body, 4.5f));
            float seat = RoundRect(x, y, 96, 150, 24, 28, 10);
            c = Over(c, new Color(0.12f, 0.12f, 0.13f), Fill(seat));
            // The driver: shoulders and a helmet in your paint.
            float shoulders = RoundRect(x, y, 96, 166, 26, 12, 10);
            c = Over(c, new Color(0.3f, 0.3f, 0.33f), Fill(shoulders));
            float helmet = Circle(x, y, 96, 176, 20);
            c = Panel(c, helmet, x, 0.85f);
            c = Over(c, Outline, Stroke(helmet, 3.5f));
            return c;
        }

        static Color KartDetails(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float visor = RoundRect(x, y, 96, 190, 13, 4, 3);
            c = Over(c, Glass, Fill(visor));
            c = Over(c, White(0.5f), Fill(Capsule(x, y, 88, 191, 94, 191, 1.4f)));
            c = Over(c, White(0.55f), Fill(Capsule(x, y, 96, 160, 96, 192, 2.6f)) * Fill(Circle(x, y, 96, 176, 19))); // helmet stripe
            c = Over(c, new Color(0.08f, 0.08f, 0.08f), Stroke(Circle(x, y, 96, 214, 12), 4f));
            float number = RoundRect(x, y, 96, 300, 14, 10, 4);
            c = Over(c, Hex("FFF6D8"), Fill(number));
            c = Over(c, Outline, Stroke(number, 2f));
            c = Over(c, Outline, Fill(Capsule(x, y, 96, 294, 96, 306, 2.4f)));
            float pipe = Capsule(x, y, 140, 72, 158, 50, 4.5f);
            c = Over(c, new Color(0.62f, 0.62f, 0.66f), Fill(pipe));
            c = Over(c, Outline, Stroke(pipe, 2f));
            return c;
        }

        // Farm pickup: a long bonnet and cab at the front, a wooden bed with side rails behind.
        static float PickupSdf(float x, float y) => Min(
            RoundRect(x, y, 96, 238, 50, 84, 22), RoundRect(x, y, 96, 98, 54, 70, 8),
            RoundRect(x, y, 48, 266, 12, 36, 9), RoundRect(x, y, 144, 266, 12, 36, 9));

        static Color PickupPaint(Color c, float x, float y)
        {
            float body = PickupSdf(x, y);
            c = Panel(c, body, x);
            c = Over(c, new Color(0.93f, 0.93f, 0.93f), Fill(RoundRect(x, y, 96, 282, 30, 30, 12)) * Fill(body + 6f)); // the bonnet
            float shield = Polygon(x, y, new[] { new Vector2(56, 240), new Vector2(136, 240), new Vector2(128, 214), new Vector2(64, 214) });
            c = Over(c, Glass, Fill(shield));
            float roof = RoundRect(x, y, 96, 192, 42, 24, 10);
            c = Over(c, new Color(1f, 1f, 1f), Fill(roof));
            c = Over(c, Outline, Stroke(roof, 2.5f));
            c = Over(c, Glass, Fill(RoundRect(x, y, 96, 164, 38, 4, 2)));
            float bed = RoundRect(x, y, 96, 96, 44, 58, 4);
            c = Over(c, new Color(0.72f, 0.72f, 0.72f), Fill(bed));
            c = Over(c, Outline, Stroke(bed, 3f));
            float mirrors = Min(RoundRect(x, y, 40, 224, 8, 6, 2), RoundRect(x, y, 152, 224, 8, 6, 2));
            c = Panel(c, mirrors, x);
            c = Over(c, Outline, Stroke(Min(body, mirrors), 4.5f));
            c = Over(c, Outline, Stroke(shield, 3f));
            return c;
        }

        static Color PickupDetails(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // The bed's floor: wooden planks.
            float bed = RoundRect(x, y, 96, 96, 42, 56, 3);
            if (bed < 0f)
            {
                float plank = Mathf.Repeat(x - 54f, 17f);
                float grain = Noise(x / 3f, y / 22f, 9) * 0.12f;
                var wood = Color.Lerp(Hex("B07A44"), Hex("8F5D30"), grain * 4f);
                c = Over(c, plank < 1.6f ? Hex("6B4322") : wood, Fill(bed));
            }
            c = Over(c, White(0.3f), Fill(Capsule(x, y, 70, 220, 80, 236, 3f)) * Fill(Polygon(x, y, new[] { new Vector2(56, 240), new Vector2(136, 240), new Vector2(128, 214), new Vector2(64, 214) })));
            float grille = RoundRect(x, y, 96, 318, 30, 5, 2);
            c = Over(c, new Color(0.2f, 0.2f, 0.22f), Fill(grille));
            float heads = Min(Circle(x, y, 62, 314, 8), Circle(x, y, 130, 314, 8));
            c = Over(c, Hex("FFF6D8"), Fill(heads));
            c = Over(c, Outline, Stroke(heads, 2.5f));
            float bumper = RoundRect(x, y, 96, 327, 44, 4, 3);
            c = Over(c, Hex("C9CDD2"), Fill(bumper));
            c = Over(c, Outline, Stroke(bumper, 2f));
            float tails = Min(RoundRect(x, y, 50, 30, 6, 4, 2), RoundRect(x, y, 142, 30, 6, 4, 2));
            c = Over(c, Hex("E0303A"), Fill(tails));
            return c;
        }

        // ---------- tops ----------

        static Color Wing(float x, float y, bool gold)
        {
            Color c = new(0, 0, 0, 0);
            Color main = gold ? Hex("F2C230") : new Color(0.16f, 0.16f, 0.18f);
            Color edge = gold ? Hex("FFE98A") : new Color(0.32f, 0.32f, 0.36f);
            Color plate = gold ? Hex("D9A21B") : Hex("D8432C");
            float struts = Min(Capsule(x, y, 70, 30, 70, 54, 3.5f), Capsule(x, y, 122, 30, 122, 54, 3.5f));
            c = Over(c, new Color(0.2f, 0.2f, 0.2f), Fill(struts));
            float wing = RoundRect(x, y, 96, 24, 70, 10, 4);
            c = Over(c, main, Fill(wing));
            c = Over(c, edge, Fill(RoundRect(x, y, 96, 30, 68, 3, 2)) * Fill(wing));
            float plates = Min(RoundRect(x, y, 24, 26, 5, 15, 2), RoundRect(x, y, 168, 26, 5, 15, 2));
            c = Over(c, plate, Fill(plates));
            c = Over(c, Outline, Stroke(Min(wing, plates), 3.5f));
            if (gold) c = Over(c, Hex("FFF6D8"), Fill(Star(x, y, 96, 24, 8, 3.5f)));
            return c;
        }

        static Color RoofRackPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float rails = Min(Shell(RoundRect(x, y, 96, 166, 38, 42, 8), 2.6f), Capsule(x, y, 58, 152, 134, 152, 2.6f), Capsule(x, y, 58, 180, 134, 180, 2.6f));
            c = Over(c, new Color(0.2f, 0.2f, 0.22f), Fill(rails));
            float box = RoundRect(x, y, 80, 172, 18, 16, 4);
            c = Over(c, Hex("3E7CC4"), Fill(box));
            c = Over(c, Hex("5D98DA"), Fill(RoundRect(x, y, 80, 176, 14, 6, 3)));
            c = Over(c, Outline, Stroke(box, 2.5f));
            float bag = RoundRect(x, y, 118, 176, 13, 12, 6);
            c = Over(c, Hex("C9A36A"), Fill(bag));
            c = Over(c, Outline, Stroke(bag, 2.5f));
            float tyre = Circle(x, y, 98, 142, 14);
            c = Over(c, new Color(0.13f, 0.13f, 0.13f), Fill(tyre));
            c = Over(c, new Color(0.45f, 0.45f, 0.47f), Fill(Circle(x, y, 98, 142, 6)));
            c = Over(c, Outline, Stroke(tyre, 2.5f));
            float strap = Capsule(x, y, 62, 160, 132, 186, 1.8f);
            c = Over(c, Hex("E0303A"), Fill(strap));
            return c;
        }

        static Color LightBarPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float bar = RoundRect(x, y, 96, 214, 56, 7, 3);
            c = Over(c, new Color(0.14f, 0.14f, 0.15f), Fill(bar));
            c = Over(c, Outline, Stroke(bar, 2.5f));
            foreach (float lx in new[] { 54f, 82f, 110f, 138f })
            {
                float lamp = Circle(x, y, lx, 216, 10);
                c = Over(c, new Color(0.18f, 0.18f, 0.2f), Fill(lamp));
                c = Over(c, Hex("FFF3B8"), Fill(Circle(x, y, lx, 217, 6.5f)));
                c = Over(c, White(0.9f), Fill(Circle(x, y, lx - 2f, 219, 2.2f)));
                c = Over(c, Outline, Stroke(lamp, 2.5f));
            }
            return c;
        }

        // Rosie's hay-bale spoiler: two bales strapped across the back and a pitchfork laid over them.
        static Color HayBalesPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (float bx in new[] { 70f, 122f })
            {
                float bale = RoundRect(x, y, bx, 64, 24, 18, 6);
                float straw = Noise(x / 2.2f, y / 9f, 11);
                var hay = Color.Lerp(Hex("E8C25A"), Hex("C99A34"), straw);
                if (Mathf.Repeat(y + Hash((int)(x / 3), 7) * 6f, 6f) < 1.2f) hay = Shade(hay, 0.85f);
                c = Over(c, hay, Fill(bale));
                c = Over(c, Hex("FFE48F"), Fill(RoundRect(x, y, bx - 4, 72, 16, 4, 2)) * Fill(bale) * 0.6f);
                float twine = Min(Capsule(x, y, bx - 10, 46, bx - 10, 82, 1.6f), Capsule(x, y, bx + 10, 46, bx + 10, 82, 1.6f));
                c = Over(c, Hex("7A3B1E"), Fill(twine) * Fill(bale));
                c = Over(c, Hex("6B4A1C"), Stroke(bale, 3f));
            }
            float handle = Capsule(x, y, 34, 112, 150, 38, 3.4f);
            c = Over(c, Hex("9A6A3A"), Fill(handle));
            c = Over(c, Outline, Stroke(handle, 2f));
            // The fork's head and three tines pointing forward-left.
            Vector2 head = new(42, 107), dir = new Vector2(-116, 74).normalized, side = new(-dir.y, dir.x);
            float tines = float.MaxValue;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 a = head + side * (i * 7f), b = a + dir * 20f;
                tines = Mathf.Min(tines, Capsule(x, y, a.x, a.y, b.x, b.y, 1.8f));
            }
            tines = Mathf.Min(tines, Capsule(x, y, head.x - side.x * 8f, head.y - side.y * 8f, head.x + side.x * 8f, head.y + side.y * 8f, 2.2f));
            c = Over(c, new Color(0.62f, 0.64f, 0.68f), Fill(tines));
            c = Over(c, Outline, Stroke(tines, 1.5f));
            return c;
        }

        // ---------- decals (on the nose, x 62-130 / y 236-318, and the tail) ----------

        static float NoseZone(float x, float y) => Polygon(x, y, new[] { new Vector2(62, 236), new Vector2(130, 236), new Vector2(118, 316), new Vector2(74, 316) });

        static Color StripesPixel(float x, float y)
        {
            float stripes = Min(RoundRect(x, y, 86, 276, 5, 42, 2), RoundRect(x, y, 106, 276, 5, 42, 2), RoundRect(x, y, 86, 54, 5, 22, 2), RoundRect(x, y, 106, 54, 5, 22, 2));
            return Over(new Color(0, 0, 0, 0), new Color(1f, 1f, 1f, 0.95f), Fill(stripes) * Fill(Mathf.Min(NoseZone(x, y), RoundRect(x, y, 96, 54, 40, 22, 4))));
        }

        static Color NumberPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float disc = Circle(x, y, 96, 276, 19);
            c = Over(c, Hex("FFF6D8"), Fill(disc));
            c = Over(c, Outline, Stroke(disc, 2.5f));
            float seven = Min(Capsule(x, y, 87, 286, 105, 286, 3.4f), Capsule(x, y, 105, 286, 92, 264, 3.4f));
            c = Over(c, Hex("1E2A26"), Fill(seven));
            return c;
        }

        static Color FlamesPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            foreach (float s in new[] { -1f, 1f })
            {
                // Three tongues licking back from the nose on each side.
                float d = float.MaxValue, inner = float.MaxValue;
                for (int i = 0; i < 3; i++)
                {
                    float ox = 96f + s * (12f + i * 9f), top = 314f - i * 6f, tip = 246f - i * 10f;
                    d = Mathf.Min(d, Polygon(x, y, new[] { new Vector2(ox - 10f, top), new Vector2(ox + 10f, top), new Vector2(ox + s * 12f, tip) }) - 2f);
                    inner = Mathf.Min(inner, Polygon(x, y, new[] { new Vector2(ox - 5f, top - 4f), new Vector2(ox + 5f, top - 4f), new Vector2(ox + s * 7f, tip + 18f) }));
                }
                c = Over(c, Hex("E8432A"), Fill(d));
                c = Over(c, Hex("FFB02E"), Fill(d + 3f));
                c = Over(c, Hex("FFE680"), Fill(inner));
            }
            return c * new Color(1, 1, 1, Fill(NoseZone(x, y) - 2f) > 0f ? 1f : 0f);
        }

        static Color CheckersPixel(float x, float y)
        {
            float band = RoundRect(x, y, 96, 262, 34, 10, 2);
            bool dark = ((int)((x - 62f) / 10f) + (int)((y - 252f) / 10f)) % 2 == 0;
            Color c = Over(new Color(0, 0, 0, 0), dark ? Hex("1E2A26") : Color.white, Fill(band));
            return Over(c, Outline, Stroke(band, 2f) * Fill(band - 2f));
        }

        static Color CrestPixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float shield = Min(RoundRect(x, y, 96, 282, 18, 14, 4), Triangle(x, y, new Vector2(78, 272), new Vector2(114, 272), new Vector2(96, 252)));
            c = Over(c, Hex("F2C230"), Fill(shield));
            c = Over(c, Hex("FFE98A"), Fill(RoundRect(x, y, 90, 288, 9, 4, 2)) * Fill(shield));
            c = Over(c, Hex("8A5B0A"), Stroke(shield, 2.5f));
            c = Over(c, Hex("FFF6D8"), Fill(Star(x, y, 96, 276, 8, 3.5f)));
            // Laurels either side.
            foreach (float s in new[] { -1f, 1f })
                for (int i = 0; i < 4; i++)
                {
                    float lx = 96f + s * (26f - i * 1.5f), ly = 258f + i * 10f;
                    c = Over(c, Hex("E4B026"), Fill(Ellipse(x, y, lx, ly, 3.4f, 6.5f)));
                }
            return c;
        }
    }
}
