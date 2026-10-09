using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// Saved progress per race (best stars, won, best time) and the unlock rules:
    /// a district (Grand Prix) unlocks once the total stars reach its requirement; inside it,
    /// the first race is open and every further race unlocks by winning the previous one.
    /// </summary>
    public static class ProgressStore
    {
        const string Key = "Progress.v1";

        [Serializable]
        class Entry
        {
            public string level;
            public int stars;
            public bool won;
            public float bestTime;
        }

        [Serializable]
        class Data
        {
            public List<Entry> entries = new();
        }

        static Data _data;
        static Data Saved => _data ??= Load();

        public static event Action Changed;

        public static int StarsOf(LevelDefinition level) => Find(level)?.stars ?? 0;
        public static bool HasWon(LevelDefinition level) => Find(level)?.won ?? false;
        public static float? BestTimeOf(LevelDefinition level) => Find(level) is { bestTime: > 0f } e ? e.bestTime : null;

        public static int TotalStars
        {
            get
            {
                int total = 0;
                foreach (var e in Saved.entries) total += e.stars;
                return total;
            }
        }

        public static bool IsUnlocked(DistrictDefinition district) =>
            !district.ComingSoon && TotalStars >= district.starsRequired;

        /// <summary>Race <paramref name="index"/> of an unlocked district is open once the previous race is won.</summary>
        public static bool CanEnter(DistrictDefinition district, int index) =>
            IsUnlocked(district) && index >= 0 && index < district.races.Count &&
            (index == 0 || HasWon(district.races[index - 1]));

        public static int StarsIn(DistrictDefinition district)
        {
            int total = 0;
            foreach (var race in district.races) total += StarsOf(race);
            return total;
        }

        public static bool IsVisible(LevelCatalog catalog, int index) =>
            index == 0 || (index < catalog.Count && HasWon(catalog[index - 1]));

        public static bool CanEnter(LevelCatalog catalog, int index) =>
            IsVisible(catalog, index) && TotalStars >= catalog[index].starsRequired;

        /// <summary>Stores a finished race; keeps the best stars and time. Returns the stars for this run.</summary>
        public static int Record(LevelDefinition level, bool won, float totalTime)
        {
            int stars = level.StarsFor(won, totalTime);
            var entry = Find(level);
            if (entry == null)
            {
                entry = new Entry { level = level.name };
                Saved.entries.Add(entry);
            }
            entry.stars = Mathf.Max(entry.stars, stars);
            entry.won |= won;
            if (won && (entry.bestTime <= 0f || totalTime < entry.bestTime)) entry.bestTime = totalTime;
            Save();
            return stars;
        }

        public static void ResetAll()
        {
            _data = new Data();
            Save();
        }

        static Entry Find(LevelDefinition level) => level == null ? null : Saved.entries.Find(e => e.level == level.name);

        static Data Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new Data();
            try { return JsonUtility.FromJson<Data>(json) ?? new Data(); }
            catch (ArgumentException) { return new Data(); }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Saved));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
