using UnityEngine.SceneManagement;

namespace SomeGame.Race
{
    /// <summary>Carries the chosen race from the map to the race scene and back.</summary>
    public static class GameSession
    {
        public const string MapScene = "City";
        public const string RaceScene = "Race";

        /// <summary>The race being played, or null when the race scene was opened directly (Editor testing).</summary>
        public static LevelDefinition CurrentLevel { get; private set; }

        /// <summary>The district the player last raced in; the city opens focused on it.</summary>
        public static DistrictDefinition CurrentDistrict { get; private set; }

        public static void StartRace(LevelDefinition level, DistrictDefinition district = null)
        {
            CurrentLevel = level;
            if (district != null) CurrentDistrict = district;
            SceneManager.LoadScene(RaceScene);
        }

        public static void ReturnToMap() => SceneManager.LoadScene(MapScene);
    }
}
