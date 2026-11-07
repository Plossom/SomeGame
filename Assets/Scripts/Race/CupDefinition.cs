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
        [Header("Challenge")]
        [Tooltip("The cup's funny challenge on the map (not a race).")]
        public LevelDefinition challenge;
        [Tooltip("The rival to beat before the challenge opens (their RivalCharacter asset's name).")]
        public string challengeAfterRival;
        [Tooltip("Where its side stop sits on the map (map canvas units; zero: beside the race, wherever there is room).")]
        public Vector2 challengeSpot;

        public bool HasRaces => races != null && races.Count > 0;
    }
}
