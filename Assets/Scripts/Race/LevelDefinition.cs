using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// One race on the map: which track, how many laps, how strong the rivals are and how many total stars it
    /// takes to enter. Stars come from the finishing place (<see cref="StarsForPlace"/>), the same in every race.
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/Level", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        public string displayName = "Race";
        [Tooltip("Shown above the race name on the map, e.g. VALLEY CUP · RACE 1.")]
        public string chapter = "CHAPTER 01";
        public TrackLayout track;
        [Tooltip("The practice drive: no rivals, no finish, the tutorial hints guide the player.")]
        public bool isTutorial;
        [Tooltip("Picture of the whole track from above, shown while the race loads (SomeGame > Generate Track Previews).")]
        public Sprite preview;
        [Min(1)] public int laps = 3;

        [Header("Race info (map)")]
        [Tooltip("One or two sentences about the race, shown in the race info window.")]
        [TextArea(2, 4)] public string description;
        [Tooltip("1 = easy, 5 = very hard.")]
        [Range(1, 5)] public int difficulty = 1;

        [Header("Rivals")]
        [Range(0, 7)] public int rivalCount = 7;
        [Tooltip("Multiplies each rival's top speed (and acceleration).")]
        [Range(0.5f, 1.5f)] public float rivalSpeedScale = 1f;
        [Tooltip("Multiplies how fast rivals dare to take corners.")]
        [Range(0.5f, 1.5f)] public float rivalCornerScale = 1f;

        [Header("Unlock")]
        [Tooltip("Total stars needed to enter this race (it also needs the previous race won).")]
        [Min(0)] public int starsRequired;

        /// <summary>True for a journey in sections (one long loop) instead of laps.</summary>
        public bool Sectioned => track != null && (track.sections > 1 || track.sectionLines is { Length: > 0 });

        /// <summary>Laps raced: the sections of a journey track, else <see cref="laps"/>.</summary>
        public int RaceLaps => !Sectioned ? laps
            : track.sectionLines is { Length: > 0 } ? track.sectionLines.Length + (track.finishAtLastLine ? 0 : 1) : track.sections;

        /// <summary>"3 laps" or "3 sections".</summary>
        public string LapsText => Sectioned ? $"{RaceLaps} sections" : (laps == 1 ? "1 lap" : $"{laps} laps");

        /// <summary>Stars for a finishing place: a win 3, 2nd or 3rd 2, 4th or 5th 1, further back none.</summary>
        public static int StarsForPlace(int place) => place == 1 ? 3 : place is 2 or 3 ? 2 : place is 4 or 5 ? 1 : 0;

        /// <summary>The places that earn 1, 2 and 3 stars, as shown on the map and the race cards.</summary>
        public static readonly string[] StarPlaces = { "4–5TH", "2–3RD", "1ST" };
    }
}
