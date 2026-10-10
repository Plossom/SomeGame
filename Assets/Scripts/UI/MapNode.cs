using System;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    public enum MapNodeState { Hidden, Locked, Open, Won }

    /// <summary>
    /// One stop on the map road: a round badge with the race number (white with a hard shadow once
    /// won, orange when it is the next race, grey with a lock while short of stars, a dashed "?" while
    /// still hidden), the stars earned below and a pulsing ring when selected.
    /// </summary>
    public class MapNode : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image shadow;
        [SerializeField] UnityEngine.UI.Image face;
        [SerializeField] UnityEngine.UI.Image ring;
        [Tooltip("Dashed outline used while the race is still hidden.")]
        [SerializeField] UnityEngine.UI.Image dashedRing;
        [SerializeField] TMP_Text number;
        [SerializeField] UnityEngine.UI.Image lockIcon;
        [SerializeField] GameObject starsPill;
        [SerializeField] UnityEngine.UI.Image[] stars;
        [Tooltip("Trophy slot after the stars, filled once the race is won.")]
        [SerializeField] UnityEngine.UI.Image trophy;
        [Tooltip("Tag above a locked race showing the total stars it needs.")]
        [SerializeField] GameObject requirement;
        [SerializeField] TMP_Text requirementText;
        [Tooltip("Pulsing ring shown on the selected race.")]
        [SerializeField] GameObject selection;
        [SerializeField] TMP_Text caption;
        [Tooltip("Background of the caption, shown with it.")]
        [SerializeField] GameObject captionRoot;

        public void Bind(string label, MapNodeState state, int earnedStars, int starsRequired, bool selected, string captionText, Action onClick)
        {
            bool hidden = state == MapNodeState.Hidden;
            bool won = state == MapNodeState.Won;
            // A won race looks like any open race; its stars and trophy show the result.
            if (won) state = MapNodeState.Open;
            face.color = state switch
            {
                MapNodeState.Open => Theme.Orange,
                MapNodeState.Won => Theme.White,
                MapNodeState.Locked => Theme.Stone,
                _ => Theme.WithAlpha(Theme.Cream, 0.85f),
            };
            ring.gameObject.SetActive(!hidden);
            ring.color = state == MapNodeState.Open ? Theme.White : state == MapNodeState.Locked ? Theme.StoneDark : Theme.Ink;
            dashedRing.gameObject.SetActive(hidden);
            shadow.gameObject.SetActive(!hidden);
            shadow.color = state == MapNodeState.Open ? Theme.OrangeDeep : state == MapNodeState.Locked ? Theme.StoneDark : Theme.Ink;

            number.gameObject.SetActive(state != MapNodeState.Locked);
            number.text = hidden ? "?" : label;
            number.color = state == MapNodeState.Open ? Theme.White : hidden ? Theme.StoneDark : Theme.Ink;
            lockIcon.gameObject.SetActive(state == MapNodeState.Locked);

            // Always show the slots: empty stars and trophy as faint outlines, filled once earned.
            starsPill.SetActive(!hidden);
            var empty = Theme.WithAlpha(Theme.Stone, 0.3f);
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < earnedStars ? Theme.Amber : empty;
            if (trophy != null) trophy.color = won ? Theme.Amber : empty;

            requirement.SetActive(state == MapNodeState.Locked);
            requirementText.text = starsRequired.ToString();

            (captionRoot != null ? captionRoot : caption.gameObject).SetActive(!string.IsNullOrEmpty(captionText));
            caption.text = captionText;

            selection.SetActive(selected && !hidden);
            transform.localScale = Vector3.one * (selected ? 1.15f : hidden ? 0.8f : 1f);

            button.interactable = !hidden;
            button.onClick.RemoveAllListeners();
            if (!hidden) button.onClick.AddListener(() => onClick());
        }
    }
}
