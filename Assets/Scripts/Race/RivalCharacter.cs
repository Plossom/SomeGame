using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// A rival with a face: a name, a portrait, the colour of their car and the race where they are the one to beat
    /// (their home race). There they drive the car at the front of the grid, say something during the countdown
    /// and have a word for you at the finish. Kept in Resources/Rivals.
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/Rival Character", fileName = "Rival")]
    public class RivalCharacter : ScriptableObject
    {
        public string displayName = "Rival";
        [Tooltip("Who they are in a few words, e.g. THE GRUMPY FARMER.")]
        public string title = "";
        [TextArea(2, 4)] public string bio = "";
        [Tooltip("Round portrait (transparent around the circle).")]
        public Sprite portrait;
        [Tooltip("The rival by their car (the first meeting).")]
        public Sprite painting;
        [Tooltip("The rival standing, head to toe (their card).")]
        public Sprite fullBody;
        [Tooltip("The rival at the wheel (the loading screen of their races).")]
        public Sprite driving;
        [Tooltip("Their face in each mood (Mood order: smug, laugh, surprised, angry, thinking, wink).")]
        public Sprite[] faces;
        public LevelDefinition homeRace;

        [Header("Their car (garage parts, the same as yours)")]
        public Color carColor = Color.red;
        public string body = "buggy", top = "none", decal = "none";
        [Tooltip("The gadget they use in their home race, and you win by beating them (a garage part id).")]
        public string gadget = "";

        [Header("Words")]
        [Tooltip("The first meeting, line by line (when their home race opens).")]
        [TextArea(1, 3)] public string[] meetLines;
        [Tooltip("On their home race's card, leaning in.")]
        public string cardLine = "";
        [Tooltip("Said during the countdown.")]
        [TextArea(1, 3)] public string[] startLines;
        [Tooltip("Said at the finish when they beat you.")]
        [TextArea(1, 3)] public string[] winLines;
        [Tooltip("Said at the finish when you beat them.")]
        [TextArea(1, 3)] public string[] loseLines;

        /// <summary>One of the lines, never the same one twice in a row.</summary>
        public string Pick(string[] lines)
        {
            if (lines == null || lines.Length == 0) return "";
            int i = Random.Range(0, lines.Length);
            if (lines.Length > 1 && lines[i] == _last) i = (i + 1) % lines.Length;
            _last = lines[i];
            return lines[i];
        }
        [System.NonSerialized] string _last;

        public enum Mood { Smug, Laugh, Surprised, Angry, Thinking, Wink }

        /// <summary>Their face in a mood (the portrait if there is none).</summary>
        public Sprite Face(Mood mood) => faces != null && (int)mood < faces.Length && faces[(int)mood] != null ? faces[(int)mood] : portrait;

        /// <summary>A line may start with its mood ("laugh|Ha!"): the words, and the mood (smug if none).</summary>
        public static string Words(string line, out Mood mood)
        {
            mood = Mood.Smug;
            if (string.IsNullOrEmpty(line)) return "";
            int bar = line.IndexOf('|');
            if (bar <= 0) return line;
            if (System.Enum.TryParse(line.Substring(0, bar), true, out Mood parsed)) mood = parsed;
            return line.Substring(bar + 1);
        }

        /// <summary>Everyone with a face (Resources/Rivals).</summary>
        public static RivalCharacter[] All => Resources.LoadAll<RivalCharacter>("Rivals");

        /// <summary>The character whose home race this is, if any.</summary>
        public static RivalCharacter For(LevelDefinition level)
        {
            if (level == null) return null;
            foreach (var character in Resources.LoadAll<RivalCharacter>("Rivals"))
                if (character.homeRace == level) return character;
            return null;
        }
    }
}
