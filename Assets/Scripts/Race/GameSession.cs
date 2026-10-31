using UnityEngine.SceneManagement;

namespace SomeGame.Race
{
    /// <summary>Carries the chosen race from the map to the race scene and back.</summary>
    public static class GameSession
    {
        public const string MapScene = "Map";
        public const string RaceScene = "Race";

        /// <summary>The race being played, or null when the race scene was opened directly (Editor testing).</summary>
        public static LevelDefinition CurrentLevel { get; private set; }

        /// <summary>True while a scene is loading (screens can show "loading" meanwhile).</summary>
        public static bool Loading { get; private set; }

        /// <summary>True while the race being played is an online one (a room's race, see SomeGame.Online).</summary>
        public static bool Online { get; private set; }

        /// <summary>True while the race scene is the active scene.</summary>
        public static bool InRaceScene => SceneManager.GetActiveScene().name == RaceScene;

        public static void StartRace(LevelDefinition level) => StartRace(level, false);

        /// <summary>Enters an online room's race (the room tells every phone when).</summary>
        public static void StartOnlineRace(LevelDefinition level) => StartRace(level, true);

        static void StartRace(LevelDefinition level, bool online)
        {
            if (Loading) return;
            CurrentLevel = level;
            Online = online;
            SomeGame.UI.Theme.Use(SomeGame.UI.Theme.Valley); // the race screens use the Valley colours
            // The loading screen shows the track and the progress, then hands over to the race.
            Loading = true;
            SomeGame.UI.LoadingScreen.Show(level, RaceScene, () => Loading = false);
        }

        public static void ReturnToMap()
        {
            Online = false;
            Load(MapScene);
        }

        /// <summary>Starts the practice drive (it counts as done once it has been finished or skipped: until then the app opens on it).</summary>
        public static void StartTutorial()
        {
            var tutorial = UnityEngine.Resources.Load<LevelDefinition>("Tutorial");
            if (tutorial == null) return;
            UnityEngine.PlayerPrefs.Save();
            StartRace(tutorial);
        }

        /// <summary>True until the tutorial has been finished or skipped (first launch, after a reset, or quit half way).</summary>
        public static bool TutorialPending => UnityEngine.PlayerPrefs.GetInt(GameReset.TutorialDoneKey, 0) == 0;

        // Loads in the background, so the current screen stays responsive (and can say "loading").
        static void Load(string scene)
        {
            if (Loading) return;
            Loading = true;
            var op = SceneManager.LoadSceneAsync(scene);
            op.completed += _ => Loading = false;
        }
    }
}
