using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// One race on the map: which track, how many laps, how strong the rivals are, the star times and
    /// how many total stars it takes to enter.
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/Level", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        public string displayName = "Race";
        [Tooltip("Shown above the race name on the map, e.g. ALPINE CUP · RACE 1.")]
        public string chapter = "CHAPTER 01";
        public TrackLayout track;
        [Tooltip("Picture of the whole track from above, shown while the race loads (SomeGame > Generate Track Previews).")]
        public Sprite preview;
        [Min(1)] public int laps = 3;

        [Header("Rivals")]
        [Range(0, 7)] public int rivalCount = 7;
        [Tooltip("Multiplies each rival's top speed (and acceleration).")]
        [Range(0.5f, 1.5f)] public float rivalSpeedScale = 1f;
        [Tooltip("Multiplies how fast rivals dare to take corners.")]
        [Range(0.5f, 1.5f)] public float rivalCornerScale = 1f;

        [Header("Unlock")]
        [Tooltip("Total stars needed to enter this race (it also needs the previous race won).")]
        [Min(0)] public int starsRequired;

        [Header("Stars (by total time; winning is not required)")]
        [Tooltip("Total race time (including penalties) for 1, 2 and 3 stars, in seconds. Must decrease.")]
        public float[] starTimes = { 75f, 68f, 63f };

        /// <summary>Stars for a finished race, by total time only (the place does not matter).</summary>
        public int StarsFor(bool won, float totalTime)
        {
            int stars = 0;
            for (int i = 0; i < starTimes.Length; i++)
                if (totalTime <= starTimes[i]) stars = i + 1;
            return stars;
        }
    }
}
