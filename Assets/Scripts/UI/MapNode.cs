using System;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    public enum MapNodeState { Hidden, Locked, Open, Won }

    /// <summary>One race on the map: a round button with its number, earned stars and lock.</summary>
    public class MapNode : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image circle;
        [Tooltip("White ring and drop shadow; faded out for hidden races.")]
        [SerializeField] UnityEngine.UI.Image[] frame;
        [SerializeField] TMP_Text number;
        [SerializeField] UnityEngine.UI.Image[] stars;
        [SerializeField] GameObject lockGroup;
        [SerializeField] TMP_Text lockText;

        [SerializeField] Color hiddenColor = new(1f, 1f, 1f, 0.18f);
        [SerializeField] Color lockedColor = new(0.55f, 0.57f, 0.6f);
        [SerializeField] Color openColor = new(1f, 0.8f, 0.2f);
        [SerializeField] Color wonColor = new(0.3f, 0.75f, 0.35f);
        [SerializeField] Color starOn = new(1f, 0.82f, 0.2f);
        [SerializeField] Color starOff = new(0f, 0f, 0f, 0.25f);

        public void Bind(int index, MapNodeState state, int earnedStars, int starsRequired, Action onClick)
        {
            bool hidden = state == MapNodeState.Hidden;
            circle.color = state switch
            {
                MapNodeState.Hidden => hiddenColor,
                MapNodeState.Locked => lockedColor,
                MapNodeState.Won => wonColor,
                _ => openColor,
            };
            foreach (var image in frame)
            {
                var c = image.color;
                c.a = hidden ? 0.15f : (image.name == "Shadow" ? 0.3f : 1f);
                image.color = c;
            }
            number.text = hidden ? "?" : (index + 1).ToString();
            number.alpha = hidden ? 0.5f : 1f;

            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(!hidden);
                stars[i].color = i < earnedStars ? starOn : starOff;
            }

            lockGroup.SetActive(state == MapNodeState.Locked);
            lockText.text = starsRequired.ToString();

            button.interactable = !hidden;
            button.onClick.RemoveAllListeners();
            if (!hidden) button.onClick.AddListener(() => onClick());
        }
    }
}
