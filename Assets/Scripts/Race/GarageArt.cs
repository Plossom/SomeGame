using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// The pictures of the garage's parts (Resources/GarageArt), by "slot.id" (e.g. "body.rally", "top.spoiler"):
    /// for a body the menu picture (all four wheels), the race body (no front wheels) and its details; for a top
    /// or a decal the overlay; for a trail its particle; for a gadget its icon. Filled by the editor
    /// (SomeGame > Build Garage Art).
    /// </summary>
    [CreateAssetMenu(menuName = "SomeGame/Garage Art", fileName = "GarageArt")]
    public class GarageArt : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public Sprite full, race, details;
        }

        public List<Entry> entries = new();

        static GarageArt _instance;
        public static GarageArt Instance => _instance != null ? _instance : _instance = Resources.Load<GarageArt>("GarageArt");

        public Entry Get(Garage.Part part) => part == null ? null : Get(part.Slot.ToString().ToLowerInvariant() + "." + part.Id);

        public Entry Get(string id)
        {
            foreach (var entry in entries) if (entry.id == id) return entry;
            return null;
        }
    }
}
