using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>All districts of the city, in unlock order.</summary>
    [CreateAssetMenu(menuName = "SomeGame/Campaign", fileName = "Campaign")]
    public class Campaign : ScriptableObject
    {
        public List<DistrictDefinition> districts = new();

        public DistrictDefinition DistrictOf(LevelDefinition level) =>
            districts.Find(d => d.races.Contains(level));
    }
}
