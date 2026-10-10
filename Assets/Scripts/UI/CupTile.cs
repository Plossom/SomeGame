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
            requirementRow.SetActive(locked);
            starsNeeded.text = cup.starsRequired.ToString();
            trophiesNeeded.text = cup.trophiesRequired.ToString();
            soonLabel.gameObject.SetActive(soon);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
            button.interactable = !locked && !soon;
        }
    }
}
