using System;
using System.Collections.Generic;
using UnityEngine;
using Vehicle = SomeGame.Track.TrackLayout.Vehicle;

namespace SomeGame.Race
{
    /// <summary>
    /// Everything for your car, and what you chose. Six kinds (<see cref="Slot"/>): the paint, the body (each with
    /// its own outline from above, all driving the same), a part on top, a decal, the trail behind you and a
    /// gadget for the race. One garage, a shelf per cup: each cup's things open with that cup's own stars, by
    /// beating its rival in their home race (their signature part and gadget), by its challenge, or with the cup's
    /// gold (all three stars in every race of the cup). Once open, a thing goes on your car everywhere. The rivals use the same
    /// parts: Rosie Rust's car is the farm pickup with the hay-bale spoiler. A part you no longer have (after a
    /// reset) falls back to the first of its kind.
    /// </summary>
    public static class Garage
    {
        public enum Slot { Paint, Body, Top, Decal, Trail, Gadget }
        public enum Unlock { Free, Stars, Rival, CupGold, Challenge }

        public sealed class Part
        {
            public readonly string Id, Name, Blurb;
            public readonly Slot Slot;
            /// <summary>The cup it belongs to (its asset's name): its shelf in the garage, and whose stars open it.</summary>
            public readonly string Cup;
            public readonly Unlock How;
            /// <summary>Stars of its own cup.</summary>
            public readonly int Stars;
            /// <summary>The rival (their <see cref="RivalCharacter"/> asset's name) or the challenge (its level's name) that opens it.</summary>
            public readonly string From;
            public readonly Color Color;

            public Part(string cup, Slot slot, string id, string name, string blurb, Unlock how = Unlock.Free, int stars = 0, string from = null, string hex = null)
            {
                Cup = cup; Slot = slot; Id = id; Name = name; Blurb = blurb; How = how; Stars = stars; From = from;
                Color = Color.white;
                if (hex != null) ColorUtility.TryParseHtmlString(hex, out Color);
            }
        }

        const string V = "Valley", W = "Winter";

        // Each cup's own things for its own vehicle (the Valley Cup's for the car, the Winter Cup's for the
        // snowmobile), opened with that cup's stars (24 at most: 8 races, 3 stars each), its rival, its challenge or
        // its gold. "Nothing" and the plain tyre smoke are there for every vehicle.
        public static readonly Part[] Parts =
        {
            new(V, Slot.Paint, "orange", "ORANGE", "The classic.", hex: "#FF7A1A"),
            new(V, Slot.Paint, "red", "RED", "Fast by colour alone.", Unlock.Stars, 3, hex: "#E5383B"),
            new(V, Slot.Paint, "green", "GREEN", "Old-school cool.", Unlock.Stars, 7, hex: "#2E9E5B"),
            new(V, Slot.Paint, "grape", "PURPLE", "Juicy.", Unlock.Stars, 12, hex: "#7B4FD1"),
            new(V, Slot.Paint, "gold", "GOLD", "Champions only.", Unlock.Stars, 20, hex: "#E8B923"),
            new(W, Slot.Paint, "frost", "LIGHT BLUE", "Cold and crisp.", hex: "#5BB8F0"),
            new(W, Slot.Paint, "blue", "BLUE", "Deep and calm. Until GO.", Unlock.Stars, 2, hex: "#1F7BE0"),
            new(W, Slot.Paint, "midnight", "BLACK", "For racing after dark.", Unlock.Stars, 8, hex: "#2B2D35"),
            new(W, Slot.Paint, "pearl", "WHITE", "Fresh snow shine.", Unlock.Stars, 14, hex: "#F4F2EC"),
            new(W, Slot.Paint, "berry", "DARK RED", "A splash of red in the snow.", Unlock.Stars, 20, hex: "#C2304A"),

            new(V, Slot.Body, "buggy", "DUNE BUGGY", "Roll cage, big tyres, no roof. Pure fun."),
            new(V, Slot.Body, "rally", "RALLY HATCH", "Wide arches and a roof over your head.", Unlock.Stars, 5),
            new(V, Slot.Body, "pickup", "FARM PICKUP", "A cab up front, a wooden bed behind.", Unlock.Stars, 16),
            new(V, Slot.Body, "kart", "GO-KART", "Low, open, helmet on.", Unlock.Stars, 22),

            new(V, Slot.Top, "none", "NOTHING", "Clean and simple."),
            new(V, Slot.Top, "spoiler", "SPOILER", "Looks fast standing still.", Unlock.Stars, 2),
            new(V, Slot.Top, "roofrack", "ROOF RACK", "Packed for a road trip.", Unlock.Stars, 9),
            new(V, Slot.Top, "lightbar", "LIGHT BAR", "Four lamps for the night stages.", Unlock.Stars, 13),
            new(V, Slot.Top, "skirack", "SKI RACK", "Two pairs of skis on the roof.", Unlock.Stars, 18),
            new(V, Slot.Top, "haybales", "HAY-BALE SPOILER", "Rosie Rust's: two bales and her pitchfork.", Unlock.Rival, from: "RosieRust"),
            new(V, Slot.Top, "chicken", "CHICKEN RIDER", "One of Rosie's chickens liked you. It comes along.", Unlock.Challenge, from: "ChickenChase"),
            new(V, Slot.Top, "goldwing", "GOLDEN WING", "Three stars in every Valley Cup race.", Unlock.CupGold, from: V),

            new(V, Slot.Decal, "none", "NOTHING", "Let the paint speak."),
            new(V, Slot.Decal, "stripes", "RACING STRIPES", "Two white stripes, nose to tail.", Unlock.Stars, 4),
            new(V, Slot.Decal, "checkers", "CHECKERS", "The flag, on your car.", Unlock.Stars, 8),
            new(V, Slot.Decal, "number", "LUCKY SEVEN", "A number on the bonnet.", Unlock.Stars, 10),
            new(V, Slot.Decal, "flames", "HOT FLAMES", "Fire licking off the nose.", Unlock.Stars, 14),
            new(V, Slot.Decal, "frost", "FROST STAR", "A big snowflake on the bonnet.", Unlock.Stars, 19),
            new(V, Slot.Decal, "crest", "VALLEY CREST", "Three stars in every Valley Cup race.", Unlock.CupGold, from: V),

            new(V, Slot.Trail, "standard", "TYRE SMOKE", "The usual."),
            new(V, Slot.Trail, "sparkle", "SPARKLES", "Glitter on every drift and boost.", Unlock.Stars, 6),
            new(V, Slot.Trail, "flame", "FIRE", "Flames out of the back.", Unlock.Stars, 18),
            new(V, Slot.Trail, "confetti", "CONFETTI", "Every lap is a party.", Unlock.Stars, 22),
            new(W, Slot.Trail, "flake", "SNOWFLAKES", "A little blizzard behind you.", Unlock.Stars, 4),
            new(W, Slot.Trail, "winterconfetti", "SNOW CONFETTI", "Every lap is a snow party.", Unlock.Stars, 12),
            new(W, Slot.Trail, "aurora", "NORTHERN LIGHTS", "Three stars in every Winter Cup race.", Unlock.CupGold, from: W),

            new(V, Slot.Gadget, "none", "NO GADGET", "Just you and the road."),
            new(V, Slot.Gadget, "haybale", "HAY BALE DROP", "Drop a hay bale behind you: whoever hits it spins out. Once a lap.", Unlock.Rival, from: "RosieRust"),
        };

        /// <summary>What a vehicle's shop shows for a slot: its cup's things (and "nothing" / the plain tyre smoke,
        /// which every vehicle has). The first is what it falls back to.</summary>
        public static List<Part> For(Slot slot, Vehicle vehicle)
        {
            var free = new List<Part>();
            var rest = new List<Part>();
            if (!Fits(slot, vehicle)) return free;
            string cup = CupOf(vehicle);
            foreach (var part in Parts)
            {
                if (part.Slot != slot) continue;
                bool shared = part.How == Unlock.Free && (part.Id == "none" || part.Id == "standard");
                if (shared || (part.Cup == cup && part.How == Unlock.Free)) free.Add(part);
                else if (part.Cup == cup) rest.Add(part);
            }
            free.AddRange(rest); // the free ones first, then the rest as listed
            return free;
        }

        /// <summary>Gadgets of rivals still to come: shown as mystery slots.</summary>
        public const int MysteryGadgets = 1; // a mystery slot on each cup's shelf that has no gadget yet

        /// <summary>Something was changed (a part chosen).</summary>
        public static event Action Changed;

        public static IEnumerable<Part> Of(Slot slot)
        {
            foreach (var part in Parts) if (part.Slot == slot) yield return part;
        }

        public static Part Find(string id, Slot slot)
        {
            Part first = null;
            foreach (var part in Parts)
            {
                if (part.Slot != slot) continue;
                first ??= part;
                if (part.Id == id) return part;
            }
            return first;
        }

        public static bool IsUnlocked(Part part) => part.How switch
        {
            Unlock.Free => true,
            _ when CupList.DevUnlockAll => true,
            Unlock.Stars => CupStars(part.Cup) >= part.Stars,
            Unlock.Rival => RivalProgress.IsBeaten(part.From),
            Unlock.CupGold => CupGold(part.From),
            Unlock.Challenge => ChallengeProgress.IsWon(part.From),
            _ => false,
        };

        public static CupDefinition CupNamed(string name)
        {
            var list = CupList.Instance;
            if (list == null) return null;
            foreach (var cup in list.cups) if (cup != null && cup.name == name) return cup;
            return null;
        }

        /// <summary>The stars won in a cup's races (out of 24).</summary>
        public static int CupStars(string cupName)
        {
            var cup = CupNamed(cupName);
            if (cup == null || !cup.HasRaces) return 0;
            int stars = 0;
            for (int i = 0; i < cup.races.Count; i++) if (!cup.races[i].isTutorial) stars += ProgressStore.StarsOf(cup.races[i]);
            return stars;
        }

        public static int CupMaxStars(string cupName)
        {
            var cup = CupNamed(cupName);
            if (cup == null || !cup.HasRaces) return 0;
            int n = 0;
            for (int i = 0; i < cup.races.Count; i++) if (!cup.races[i].isTutorial) n += 3;
            return n;
        }

        /// <summary>All three stars in every race of the cup.</summary>
        public static bool CupGold(string cupName) => CupMaxStars(cupName) > 0 && CupStars(cupName) >= CupMaxStars(cupName);

        // ---------------------------------------------------------------- one setup per vehicle

        public static readonly Vehicle[] Vehicles = { Vehicle.Car, Vehicle.Snowmobile };

        /// <summary>What can be changed on a vehicle: everything on the car; on the snowmobile its paint, trail and gadget.</summary>
        public static Slot[] SlotsOf(Vehicle vehicle) => vehicle == Vehicle.Snowmobile
            ? new[] { Slot.Paint, Slot.Trail, Slot.Gadget }
            : new[] { Slot.Paint, Slot.Body, Slot.Top, Slot.Decal, Slot.Trail, Slot.Gadget };

        public static bool Fits(Slot slot, Vehicle vehicle) => System.Array.IndexOf(SlotsOf(vehicle), slot) >= 0;

        /// <summary>The cup a vehicle belongs to (its page in the garage): the car the Valley Cup's, the snowmobile the Winter Cup's.</summary>
        public static string CupOf(Vehicle vehicle) => vehicle == Vehicle.Snowmobile ? "Winter" : "Valley";

        static string Key(Slot slot, Vehicle vehicle) => vehicle == Vehicle.Snowmobile ? "Garage.Snow." + slot : "Garage." + slot;

        /// <summary>The part in a slot of a vehicle: one of its own shop's, and open (else the first of its shop).</summary>
        public static Part Equipped(Slot slot, Vehicle vehicle = Vehicle.Car)
        {
            var shop = For(slot, vehicle);
            if (shop.Count == 0) return Find("", slot);
            string id = PlayerPrefs.GetString(Key(slot, vehicle), "");
            if (vehicle == Vehicle.Car && slot == Slot.Paint && id == "" && PlayerPrefs.HasKey(PaintKey)) id = LegacyPaint();
            foreach (var part in shop) if (part.Id == id && IsUnlocked(part)) return part;
            return shop[0];
        }

        public static void Equip(Part part, Vehicle vehicle = Vehicle.Car)
        {
            if (part == null || !IsUnlocked(part) || !For(part.Slot, vehicle).Contains(part) || Equipped(part.Slot, vehicle) == part) return;
            PlayerPrefs.SetString(Key(part.Slot, vehicle), part.Id);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>A vehicle's paint.</summary>
        public static Color ColorOf(Vehicle vehicle) => Equipped(Slot.Paint, vehicle).Color;

        // ---------------------------------------------------------------- paints (as before)

        const string PaintKey = "Garage.Paint", SeenKey = "Garage.Seen";

        public static readonly Part[] Paints = new List<Part>(Of(Slot.Paint)).ToArray();

        // Paints used to be saved by their place in the old list.
        static readonly string[] OldPaints = { "orange", "red", "green", "blue", "grape", "midnight", "pearl", "gold" };
        static string LegacyPaint() => OldPaints[Mathf.Clamp(PlayerPrefs.GetInt(PaintKey, 0), 0, OldPaints.Length - 1)];

        public static bool IsUnlocked(int paint) => IsUnlocked(Paints[paint]);

        public static int Selected
        {
            get => Array.IndexOf(Paints, Equipped(Slot.Paint));
            set { if (value >= 0 && value < Paints.Length) Equip(Paints[value]); }
        }

        /// <summary>Your paint.</summary>
        public static Color Color => Equipped(Slot.Paint).Color;

        // ---------------------------------------------------------------- what's new

        static int UnlockedCount
        {
            get
            {
                int n = 0;
                foreach (var part in Parts) if (IsUnlocked(part)) n++;
                return n;
            }
        }

        /// <summary>Things opened since the garage was last looked into (the dot on its button).</summary>
        public static bool HasNew => UnlockedCount > PlayerPrefs.GetInt(SeenKey, 1) + FreeParts - 1;

        static int FreeParts
        {
            get
            {
                int n = 0;
                foreach (var part in Parts) if (part.How == Unlock.Free) n++;
                return n;
            }
        }

        public static void MarkSeen()
        {
            PlayerPrefs.SetInt(SeenKey, UnlockedCount - FreeParts + 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>The map's challenges (a funny one per cup): won, and the best score.</summary>
    public static class ChallengeProgress
    {
        public static bool IsWon(string challenge) => !string.IsNullOrEmpty(challenge) && PlayerPrefs.GetInt("Challenge.Won." + challenge, 0) == 1;
        public static int Best(string challenge) => PlayerPrefs.GetInt("Challenge.Best." + challenge, 0);

        /// <summary>Records a try; true if it was the first win.</summary>
        public static bool Record(string challenge, int score, bool won)
        {
            if (score > Best(challenge)) PlayerPrefs.SetInt("Challenge.Best." + challenge, score);
            bool first = won && !IsWon(challenge);
            if (won) PlayerPrefs.SetInt("Challenge.Won." + challenge, 1);
            PlayerPrefs.Save();
            return first;
        }
    }

    /// <summary>The rivals you have met (their first words to you) and beaten in their home race.</summary>
    public static class RivalProgress
    {
        public static bool IsBeaten(string rival) => !string.IsNullOrEmpty(rival) && PlayerPrefs.GetInt("Rival.Beaten." + rival, 0) == 1;
        public static bool HasMet(string rival) => !string.IsNullOrEmpty(rival) && PlayerPrefs.GetInt("Rival.Met." + rival, 0) == 1;

        /// <summary>Records the first win against a rival; true if it was the first.</summary>
        public static bool MarkBeaten(string rival)
        {
            if (IsBeaten(rival)) return false;
            PlayerPrefs.SetInt("Rival.Beaten." + rival, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static void MarkMet(string rival)
        {
            PlayerPrefs.SetInt("Rival.Met." + rival, 1);
            PlayerPrefs.Save();
        }
    }
}
