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
        [Tooltip("Round portrait (transparent around the circle).")]
        public Sprite portrait;
        public Color carColor = Color.red;
        public LevelDefinition homeRace;
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
