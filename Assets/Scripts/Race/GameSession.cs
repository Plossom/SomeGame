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

        public static void StartRace(LevelDefinition level)
        {
            if (Loading) return;
            CurrentLevel = level;
            // The loading screen shows the track and the progress, then hands over to the race.
            Loading = true;
            SomeGame.UI.LoadingScreen.Show(level, RaceScene, () => Loading = false);
        }

        public static void ReturnToMap() => Load(MapScene);

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
