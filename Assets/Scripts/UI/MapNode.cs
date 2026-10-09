using System;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    public enum MapNodeState { Hidden, Locked, Open, Won }

    /// <summary>
    /// One race on the map: a diamond with its number, earned stars, lock, and a pulsing ring
    /// when it is the selected race.
    /// </summary>
    public class MapNode : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image fill;
        [SerializeField] UnityEngine.UI.Image outline;
        [SerializeField] UnityEngine.UI.Image glow;
        [Tooltip("Pulsing ring shown on the selected race.")]
        [SerializeField] GameObject selectionRing;
        [SerializeField] TMP_Text number;
        [SerializeField] UnityEngine.UI.Image lockIcon;
        [SerializeField] UnityEngine.UI.Image[] stars;
        [Tooltip("Pill above a locked race showing the total stars it needs.")]
        [SerializeField] GameObject requirement;
        [SerializeField] TMP_Text requirementText;

        public void Bind(int index, MapNodeState state, int earnedStars, int starsRequired, bool selected, Action onClick)
        {
            bool hidden = state == MapNodeState.Hidden;
            Color accent = state switch
            {
                MapNodeState.Won => NeonTheme.Lime,
                MapNodeState.Open => NeonTheme.Magenta,
                _ => NeonTheme.Faint,
            };

            fill.color = state switch
            {
                MapNodeState.Open => NeonTheme.Magenta,
                MapNodeState.Won => NeonTheme.Panel,
                MapNodeState.Locked => NeonTheme.PanelRaised,
                _ => NeonTheme.WithAlpha(NeonTheme.Panel, 0.6f),
            };
            outline.color = hidden ? NeonTheme.WithAlpha(NeonTheme.Faint, 0.6f) : accent;
            glow.gameObject.SetActive(state is MapNodeState.Won or MapNodeState.Open);
            glow.color = NeonTheme.WithAlpha(accent, state == MapNodeState.Open ? 0.75f : 0.45f);

            number.gameObject.SetActive(state != MapNodeState.Locked);
            number.text = hidden ? "?" : (index + 1).ToString();
            number.color = state switch
            {
                MapNodeState.Open => NeonTheme.Background,
                MapNodeState.Won => NeonTheme.Lime,
                _ => NeonTheme.Faint,
            };
            lockIcon.gameObject.SetActive(state == MapNodeState.Locked);
            lockIcon.color = NeonTheme.Dim;

            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(state is MapNodeState.Won or MapNodeState.Open);
                stars[i].color = i < earnedStars ? NeonTheme.Lime : NeonTheme.Faint;
            }

            requirement.SetActive(state == MapNodeState.Locked);
            requirementText.text = starsRequired.ToString();

            selectionRing.SetActive(selected && !hidden);
            var ringImage = selectionRing.GetComponent<UnityEngine.UI.Graphic>();
            if (ringImage != null) ringImage.color = state == MapNodeState.Locked ? NeonTheme.Dim : NeonTheme.Magenta;

            button.interactable = !hidden;
            button.onClick.RemoveAllListeners();
            if (!hidden) button.onClick.AddListener(() => onClick());
        }
    }
}
