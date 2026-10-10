using System;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>One cup in the world menu: icon and name, a ring when current, a lock and requirements when locked.</summary>
    public class CupTile : MonoBehaviour
    {
        public enum State { Current, Open, Locked, Soon }

        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image face;
        [SerializeField] UnityEngine.UI.Image ring;
        [SerializeField] UnityEngine.UI.Image icon;
        [SerializeField] UnityEngine.UI.Image lockIcon;
        [SerializeField] TMP_Text title;
        [Tooltip("Shown when locked: stars needed (with a star icon) and trophies needed (with a trophy icon).")]
        [SerializeField] GameObject requirementRow;
        [SerializeField] TMP_Text starsNeeded;
        [SerializeField] TMP_Text trophiesNeeded;
        [SerializeField] TMP_Text soonLabel;

        public void Bind(CupDefinition cup, State state, Action onClick)
        {
            bool locked = state == State.Locked, soon = state == State.Soon;
            title.text = cup.displayName.Replace(" CUP", "");
            icon.sprite = cup.icon;
            icon.color = locked ? new Color(1f, 1f, 1f, 0.55f) : Color.white; // locked: the picture shows faintly under the lock
            face.color = state == State.Current ? Theme.Cream : locked || soon ? Theme.Stone : Color.white;
            ring.gameObject.SetActive(state == State.Current);
            lockIcon.gameObject.SetActive(locked);
            // Locked: what it takes to unlock. Unlocked: what you have collected in this cup.
            requirementRow.SetActive(locked || (!soon && cup.HasRaces));
            if (locked)
            {
                starsNeeded.text = cup.starsRequired.ToString();
                trophiesNeeded.text = cup.trophiesRequired.ToString();
            }
            else if (cup.HasRaces)
            {
                int stars = 0, wins = 0;
                foreach (var level in cup.races.levels)
                {
                    stars += ProgressStore.StarsOf(level);
                    if (ProgressStore.HasWon(level)) wins++;
                }
                starsNeeded.text = $"{stars}/{cup.races.Count * 3}";
                trophiesNeeded.text = $"{wins}/{cup.races.Count}";
            }
            soonLabel.gameObject.SetActive(soon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
            button.interactable = !locked && !soon;
        }
    }
}
