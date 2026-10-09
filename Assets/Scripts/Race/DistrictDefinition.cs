using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// A district of the neon city = one Grand Prix: an ordered list of races. The district unlocks
    /// with total stars; inside it, each race unlocks by winning the previous one.
    /// A district without races is shown as "coming soon".
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/District", fileName = "District")]
    public class DistrictDefinition : ScriptableObject
    {
        public string displayName = "DISTRICT";
        [TextArea] public string tagline = "";
        [Tooltip("What this district adds to the game, e.g. NEW // SKI JUMPS.")]
        public string feature = "";
        public Color accent = Color.magenta;
        [Min(0)] public int starsRequired;
        public List<LevelDefinition> races = new();

        public bool ComingSoon => races.Count == 0;
        public int MaxStars => races.Count * 3;
    }
}
