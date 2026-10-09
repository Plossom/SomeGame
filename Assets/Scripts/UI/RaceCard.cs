using System;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>One race in a district's Grand Prix list: number, name, stars, locked/selected state.</summary>
    public class RaceCard : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image fill;
        [SerializeField] UnityEngine.UI.Image outline;
        [SerializeField] UnityEngine.UI.Image glow;
        [SerializeField] TMP_Text number;
        [SerializeField] TMP_Text title;
        [SerializeField] UnityEngine.UI.Image[] stars;
        [SerializeField] UnityEngine.UI.Image lockIcon;

        public void Bind(int index, string name, int earned, bool open, bool won, bool selected, Color accent, Action onClick)
        {
            number.text = (index + 1).ToString("00");
            title.text = name.ToUpperInvariant();
            number.color = !open ? NeonTheme.Faint : selected ? accent : NeonTheme.Text;
            title.color = open ? NeonTheme.Muted : NeonTheme.Faint;
            fill.color = selected ? NeonTheme.PanelRaised : NeonTheme.Panel;
            outline.color = selected ? accent : (won ? NeonTheme.WithAlpha(NeonTheme.Lime, 0.6f) : NeonTheme.Border);
            glow.gameObject.SetActive(selected);
            glow.color = NeonTheme.WithAlpha(accent, 0.35f);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(open);
                stars[i].color = i < earned ? NeonTheme.Lime : NeonTheme.Faint;
            }
            lockIcon.gameObject.SetActive(!open);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
