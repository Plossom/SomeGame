using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// One race on the map: which track, how many laps, how strong the rivals are, the star times and
    /// how many total stars are needed to enter it.
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/Level", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        public string displayName = "Race";
        [Tooltip("Shown above the race name on the map, e.g. CHAPTER 01 // NEON GRID.")]
        public string chapter = "CHAPTER 01";
        public TrackLayout track;
        [Min(1)] public int laps = 3;

        [Header("Rivals")]
        [Range(0, 3)] public int rivalCount = 3;
        [Tooltip("Multiplies each rival's top speed (and acceleration).")]
        [Range(0.5f, 1.5f)] public float rivalSpeedScale = 1f;
        [Tooltip("Multiplies how fast rivals dare to take corners.")]
        [Range(0.5f, 1.5f)] public float rivalCornerScale = 1f;

        [Header("Stars (only when you win)")]
        [Tooltip("Total race time (including penalties) for 1, 2 and 3 stars, in seconds. Must decrease.")]
        public float[] starTimes = { 75f, 68f, 63f };

        [Header("Map")]
        [Tooltip("Total stars needed to enter this race (it also only appears once the previous race is won).")]
        [Min(0)] public int starsRequired;

        /// <summary>Stars for a finished race: 0 unless won, then by total time.</summary>
        public int StarsFor(bool won, float totalTime)
        {
            if (!won) return 0;
            int stars = 0;
            for (int i = 0; i < starTimes.Length; i++)
                if (totalTime <= starTimes[i]) stars = i + 1;
            return stars;
        }
    }
}
