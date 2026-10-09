using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>All races on the map, in unlock order.</summary>
    [CreateAssetMenu(menuName = "SomeGame/Level Catalog", fileName = "LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        public List<LevelDefinition> levels = new();

        public int Count => levels.Count;
        public LevelDefinition this[int index] => levels[index];
        public int IndexOf(LevelDefinition level) => levels.IndexOf(level);
    }
}
