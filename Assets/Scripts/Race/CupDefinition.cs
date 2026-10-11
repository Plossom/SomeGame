using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>The look of a cup's map: its colours, its road and its landscape.</summary>
    public enum CupLook { Valley, Winter }

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
        [Tooltip("Colours, road and landscape of the map.")]
        public CupLook look;
        [Tooltip("The painted landscape behind the map road.")]
        public Sprite mapBackground;
        [Min(0)] public int starsRequired;
        [Min(0)] public int trophiesRequired;

        public bool HasRaces => races != null && races.Count > 0;
    }
}
