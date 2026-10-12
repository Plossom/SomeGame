using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// Remembers which unlocks the player has already been shown, so the map can celebrate the new ones:
    /// per cup, how many races were open when last seen, and which cups have been seen unlocked.
    /// Counts real progress only (never the UNLOCK ALL development switch). The first time it is used
    /// (an existing save, or after a reset) it simply takes the current state, so nothing old is replayed.
    /// </summary>
    public static class UnlockLog
    {
        const string InitKey = "Seen.Init";

        static string RaceKey(LevelCatalog catalog) => "Seen.Open." + catalog.name;
        static string CupKey(CupDefinition cup) => "Seen.Cup." + cup.name;

        /// <summary>How many races of a cup are open by your stars (they open in order).</summary>
        public static int OpenRaces(LevelCatalog catalog)
        {
            int open = 0;
            for (int i = 0; i < catalog.Count; i++)
                if (i == 0 || ProgressStore.TotalStars >= catalog[i].starsRequired) open = i + 1;
                else break;
            return open;
        }

        /// <summary>A cup unlocked by real progress (the first cup always is; a "coming soon" one never).</summary>
        public static bool CupUnlocked(CupDefinition cup)
        {
            var list = CupList.Instance;
            if (list == null || cup == null || CupList.IsComingSoon(cup)) return false;
            if (list.cups.IndexOf(cup) <= 0) return true;
            return ProgressStore.TotalStars >= cup.starsRequired && ProgressStore.TotalWins >= cup.trophiesRequired;
        }

        /// <summary>First use: record everything as already seen.</summary>
        public static void EnsureInitialised()
        {
            if (PlayerPrefs.GetInt(InitKey, 0) == 1) return;
            var list = CupList.Instance;
            if (list != null)
                foreach (var cup in list.cups)
                {
                    if (cup.HasRaces) PlayerPrefs.SetInt(RaceKey(cup.races), OpenRaces(cup.races));
                    if (CupUnlocked(cup)) PlayerPrefs.SetInt(CupKey(cup), 1);
                }
            PlayerPrefs.SetInt(InitKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Races of a cup opened since it was last shown (indices, in order).</summary>
        public static List<int> NewRaces(LevelCatalog catalog)
        {
            var list = new List<int>();
            if (catalog == null || catalog.Count == 0) return list;
            int seen = PlayerPrefs.GetInt(RaceKey(catalog), 1), open = OpenRaces(catalog);
            for (int i = seen; i < open; i++) list.Add(i);
            return list;
        }

        /// <summary>Records how many races of a cup are open now (also when that is fewer, e.g. after a reset).</summary>
        public static void SeeRaces(LevelCatalog catalog)
        {
            if (catalog == null || catalog.Count == 0) return;
            PlayerPrefs.SetInt(RaceKey(catalog), OpenRaces(catalog));
            PlayerPrefs.Save();
        }

        /// <summary>Cups unlocked since last shown, in cup order.</summary>
        public static List<CupDefinition> NewCups()
        {
            var cups = new List<CupDefinition>();
            var list = CupList.Instance;
            if (list == null) return cups;
            foreach (var cup in list.cups)
                if (CupUnlocked(cup) && PlayerPrefs.GetInt(CupKey(cup), 0) == 0) cups.Add(cup);
            return cups;
        }

        public static void SeeCup(CupDefinition cup)
        {
            PlayerPrefs.SetInt(CupKey(cup), 1);
            PlayerPrefs.Save();
        }

        /// <summary>Forgets everything (game reset): the next map visit records the fresh state.</summary>
        public static void Clear()
        {
            var list = CupList.Instance;
            if (list != null)
                foreach (var cup in list.cups)
                {
                    PlayerPrefs.DeleteKey(CupKey(cup));
                    if (cup.races != null) PlayerPrefs.DeleteKey(RaceKey(cup.races));
                }
            PlayerPrefs.DeleteKey(InitKey);
        }
    }
}
