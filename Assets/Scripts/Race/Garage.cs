using System;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// The paints for your car, opened up by your stars, and the one you chose. The car wears it on the map, on the
    /// loading screen and in every race (online races keep the room's colours). A paint you no longer have enough
    /// stars for (after a reset) falls back to the first one.
    /// </summary>
    public static class Garage
    {
        public readonly struct Paint
        {
            public readonly string Name;
            public readonly Color Color;
            public readonly int Stars;
            public Paint(string name, string hex, int stars)
            {
                Name = name;
                Stars = stars;
                ColorUtility.TryParseHtmlString(hex, out Color);
            }
        }

        public static readonly Paint[] Paints =
        {
            new("SUNSET ORANGE", "#FF7A1A", 0),
            new("CHERRY RED", "#E5383B", 4),
            new("RACING GREEN", "#2E9E5B", 8),
            new("OCEAN BLUE", "#1F7BE0", 12),
            new("GRAPE", "#7B4FD1", 16),
            new("MIDNIGHT", "#2B2D35", 22),
            new("PEARL WHITE", "#F4F2EC", 30),
            new("SOLID GOLD", "#E8B923", 40),
        };

        const string Key = "Garage.Paint", SeenKey = "Garage.Seen";

        /// <summary>The paint was changed.</summary>
        public static event Action Changed;

        public static bool IsUnlocked(int paint) => paint == 0 || CupList.DevUnlockAll || ProgressStore.TotalStars >= Paints[paint].Stars;

        public static int Selected
        {
            get
            {
                int paint = Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Paints.Length - 1);
                return IsUnlocked(paint) ? paint : 0;
            }
            set
            {
                if (value < 0 || value >= Paints.Length || !IsUnlocked(value) || value == Selected) return;
                PlayerPrefs.SetInt(Key, value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static Color Color => Paints[Selected].Color;

        static int UnlockedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Paints.Length; i++) if (IsUnlocked(i)) n++;
                return n;
            }
        }

        /// <summary>Paints opened since the garage was last looked into (the dot on its button).</summary>
        public static bool HasNew => UnlockedCount > PlayerPrefs.GetInt(SeenKey, 1);

        public static void MarkSeen()
        {
            PlayerPrefs.SetInt(SeenKey, UnlockedCount);
            PlayerPrefs.Save();
        }
    }
}
