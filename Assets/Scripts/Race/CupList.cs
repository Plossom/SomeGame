using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>All cups, in unlock order (loaded from Resources/Cups).</summary>
    [CreateAssetMenu(menuName = "SomeGame/Cup List", fileName = "Cups")]
    public class CupList : ScriptableObject
    {
        public List<CupDefinition> cups = new();

        static CupList _instance;
        public static CupList Instance => _instance != null ? _instance : _instance = Resources.Load<CupList>("Cups");

        const string SelectedKey = "Cup.Selected";

        /// <summary>Development: every cup with requirements is open (and selectable even without races). Set to false for release.</summary>
        public const bool DevUnlockAll = true;

        /// <summary>A "coming soon" placeholder: no races and nothing to unlock it.</summary>
        public static bool IsComingSoon(CupDefinition cup) => !cup.HasRaces && cup.starsRequired == 0 && cup.trophiesRequired == 0;

        /// <summary>Whether the cup can be shown on the map.</summary>
        public static bool IsPlayable(CupDefinition cup) => IsUnlocked(cup) && !IsComingSoon(cup) && (cup.HasRaces || DevUnlockAll);

        /// <summary>The cup shown on the map (always an unlocked one with races).</summary>
        public static CupDefinition Selected
        {
            get
            {
                var list = Instance;
                if (list == null || list.cups.Count == 0) return null;
                int i = Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, list.cups.Count - 1);
                var cup = list.cups[i];
                return IsPlayable(cup) ? cup : list.cups[0];
            }
            set
            {
                var list = Instance;
                if (list == null || value == null) return;
                PlayerPrefs.SetInt(SelectedKey, Mathf.Max(0, list.cups.IndexOf(value)));
                PlayerPrefs.Save();
            }
        }

        public static bool IsUnlocked(CupDefinition cup) =>
            DevUnlockAll || (ProgressStore.TotalStars >= cup.starsRequired && ProgressStore.TotalWins >= cup.trophiesRequired);
    }
}
