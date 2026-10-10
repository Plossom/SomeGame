using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// Resets the game to a fresh start: stars, wins, best times and best laps of every race, and the
    /// "tutorial seen" flag. Settings (sound volume, joystick) are kept.
    /// </summary>
    public static class GameReset
    {
        /// <summary>Set once the tutorial has been played; cleared by a reset so it plays again.</summary>
        public const string TutorialDoneKey = "Tutorial.Done";

        public static void ResetAll(LevelCatalog catalog)
        {
            ProgressStore.ResetAll();
            if (catalog != null)
                foreach (var level in catalog.levels)
                    if (level != null && level.track != null) BestLapStore.Clear(level.track.name);
            PlayerPrefs.DeleteKey(TutorialDoneKey);
            PlayerPrefs.Save();
        }
    }
}
