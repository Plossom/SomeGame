using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// Everything for your car, and what you chose. Six kinds (<see cref="Slot"/>): the paint, the body (each with
    /// its own outline from above, all driving the same), a part on top, a decal, the trail behind you and a
    /// gadget for the race. Things open with stars, by beating a rival in their home race (their signature part
    /// and gadget), or with a cup's gold (all three stars in every race of the cup). The rivals use the same
    /// parts: Rosie Rust's car is the farm pickup with the hay-bale spoiler. A part you no longer have (after a
    /// reset) falls back to the first of its kind.
    /// </summary>
    public static class Garage
    {
        public enum Slot { Paint, Body, Top, Decal, Trail, Gadget }
        public enum Unlock { Free, Stars, Rival, CupGold }

        public sealed class Part
        {
            public readonly string Id, Name, Blurb;
            public readonly Slot Slot;
            public readonly Unlock How;
            public readonly int Stars;
            /// <summary>The rival (their <see cref="RivalCharacter"/> asset's name) or the cup (its asset's name) that opens it.</summary>
            public readonly string From;
            public readonly Color Color;

            public Part(Slot slot, string id, string name, string blurb, Unlock how = Unlock.Free, int stars = 0, string from = null, string hex = null)
            {
                Slot = slot; Id = id; Name = name; Blurb = blurb; How = how; Stars = stars; From = from;
                Color = Color.white;
                if (hex != null) ColorUtility.TryParseHtmlString(hex, out Color);
            }
        }

        public static readonly Part[] Parts =
        {
            new(Slot.Paint, "orange", "SUNSET ORANGE", "The classic.", hex: "#FF7A1A"),
            new(Slot.Paint, "red", "CHERRY RED", "Fast by colour alone.", Unlock.Stars, 4, hex: "#E5383B"),
            new(Slot.Paint, "green", "RACING GREEN", "Old-school cool.", Unlock.Stars, 8, hex: "#2E9E5B"),
            new(Slot.Paint, "blue", "OCEAN BLUE", "Deep and calm. Until GO.", Unlock.Stars, 12, hex: "#1F7BE0"),
            new(Slot.Paint, "grape", "GRAPE", "Juicy.", Unlock.Stars, 16, hex: "#7B4FD1"),
            new(Slot.Paint, "midnight", "MIDNIGHT", "For racing after dark.", Unlock.Stars, 22, hex: "#2B2D35"),
            new(Slot.Paint, "pearl", "PEARL WHITE", "Showroom shine.", Unlock.Stars, 30, hex: "#F4F2EC"),
            new(Slot.Paint, "gold", "SOLID GOLD", "Champions only.", Unlock.Stars, 40, hex: "#E8B923"),

            new(Slot.Body, "buggy", "DUNE BUGGY", "Roll cage, big tyres, no roof. Pure fun."),
            new(Slot.Body, "rally", "RALLY HATCH", "Wide arches and a roof over your head.", Unlock.Stars, 6),
            new(Slot.Body, "kart", "GO-KART", "Low, open, helmet on.", Unlock.Stars, 14),
            new(Slot.Body, "pickup", "FARM PICKUP", "A cab up front, a wooden bed behind.", Unlock.Stars, 24),

            new(Slot.Top, "none", "NOTHING", "Clean and simple."),
            new(Slot.Top, "spoiler", "SPOILER", "Looks fast standing still.", Unlock.Stars, 3),
            new(Slot.Top, "roofrack", "ROOF RACK", "Packed for a road trip.", Unlock.Stars, 10),
            new(Slot.Top, "lightbar", "LIGHT BAR", "Four lamps for the night stages.", Unlock.Stars, 18),
            new(Slot.Top, "haybales", "HAY-BALE SPOILER", "Rosie Rust's: two bales and her pitchfork.", Unlock.Rival, from: "RosieRust"),
            new(Slot.Top, "goldwing", "GOLDEN WING", "Three stars in every Valley Cup race.", Unlock.CupGold, from: "Valley"),

            new(Slot.Decal, "none", "NOTHING", "Let the paint speak."),
            new(Slot.Decal, "stripes", "RACING STRIPES", "Two white stripes, nose to tail.", Unlock.Stars, 2),
            new(Slot.Decal, "number", "LUCKY SEVEN", "A number on the bonnet.", Unlock.Stars, 8),
            new(Slot.Decal, "flames", "HOT FLAMES", "Fire licking off the nose.", Unlock.Stars, 16),
            new(Slot.Decal, "checkers", "CHECKERS", "The flag, on your car.", Unlock.Stars, 28),
            new(Slot.Decal, "crest", "VALLEY CREST", "Three stars in every Valley Cup race.", Unlock.CupGold, from: "Valley"),

            new(Slot.Trail, "standard", "TYRE SMOKE", "The usual."),
            new(Slot.Trail, "sparkle", "SPARKLES", "Glitter on every drift and boost.", Unlock.Stars, 5),
            new(Slot.Trail, "flame", "FIRE", "Flames out of the back.", Unlock.Stars, 12),
            new(Slot.Trail, "flake", "SNOWFLAKES", "A little blizzard behind you.", Unlock.Stars, 20),
            new(Slot.Trail, "confetti", "CONFETTI", "Every lap is a party.", Unlock.Stars, 32),

            new(Slot.Gadget, "none", "NO GADGET", "Just you and the road."),
            new(Slot.Gadget, "haybale", "HAY BALE DROP", "Drop a hay bale behind you: whoever hits it spins out. Once a lap.", Unlock.Rival, from: "RosieRust"),
        };

        /// <summary>Gadgets of rivals still to come: shown as mystery slots.</summary>
        public const int MysteryGadgets = 6;

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
            Unlock.Stars => ProgressStore.TotalStars >= part.Stars,
            Unlock.Rival => RivalProgress.IsBeaten(part.From),
            Unlock.CupGold => CupGold(part.From),
            _ => false,
        };

        /// <summary>All three stars in every race of the cup.</summary>
        public static bool CupGold(string cupName)
        {
            var list = CupList.Instance;
            if (list == null) return false;
            foreach (var cup in list.cups)
            {
                if (cup == null || cup.name != cupName || !cup.HasRaces) continue;
                for (int i = 0; i < cup.races.Count; i++)
                    if (!cup.races[i].isTutorial && ProgressStore.StarsOf(cup.races[i]) < 3) return false;
                return true;
            }
            return false;
        }

        static string Key(Slot slot) => "Garage." + slot;

        /// <summary>The part in a slot (falls back to the first of its kind if it is not, or no longer, open).</summary>
        public static Part Equipped(Slot slot)
        {
            var part = slot == Slot.Paint ? Find(LegacyPaint(), slot) : Find(PlayerPrefs.GetString(Key(slot), ""), slot);
            return IsUnlocked(part) ? part : Find("", slot);
        }

        public static void Equip(Part part)
        {
            if (part == null || !IsUnlocked(part) || Equipped(part.Slot) == part) return;
            if (part.Slot == Slot.Paint) PlayerPrefs.SetInt(PaintKey, Array.IndexOf(Paints, part));
            else PlayerPrefs.SetString(Key(part.Slot), part.Id);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- paints (as before)

        const string PaintKey = "Garage.Paint", SeenKey = "Garage.Seen";

        public static readonly Part[] Paints = new List<Part>(Of(Slot.Paint)).ToArray();

        static string LegacyPaint() => Paints[Mathf.Clamp(PlayerPrefs.GetInt(PaintKey, 0), 0, Paints.Length - 1)].Id;

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
