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
            [Tooltip("Where on the full picture the part itself is, in pixels (a top's or a decal's; empty: all of it).")]
            public Rect bounds;

            [NonSerialized] Sprite _alone;

            /// <summary>Just the part (a top or a decal cut out of the car-sized picture it's drawn on).</summary>
            public Sprite Alone
            {
                get
                {
                    if (full == null || bounds.width <= 0f) return full;
                    if (_alone != null) return _alone;
                    var rect = new Rect(full.rect.x + bounds.x, full.rect.y + bounds.y, bounds.width, bounds.height);
                    return _alone = Sprite.Create(full.texture, rect, new Vector2(0.5f, 0.5f), full.pixelsPerUnit);
                }
            }
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
