using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>A cup (world): its races and what it takes to unlock it.</summary>
    [CreateAssetMenu(menuName = "SomeGame/Cup", fileName = "Cup")]
    public class CupDefinition : ScriptableObject
    {
        public string displayName = "VALLEY CUP";
        [Tooltip("Races of this cup, in map order. Empty = coming soon.")]
        public LevelCatalog races;
        [Tooltip("Small picture for the world menu.")]
        public Sprite icon;
        public Color accent = new(1f, 0.48f, 0.1f);
        [Min(0)] public int starsRequired;
        [Min(0)] public int trophiesRequired;

        public bool HasRaces => races != null && races.Count > 0;
    }
}
