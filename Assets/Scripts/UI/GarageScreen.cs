using SomeGame.Car;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The garage: the player's car with its current stats and the upgrade slots (coming soon).
    /// </summary>
    public class GarageScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] CarStats playerStats;
        [Tooltip("Fill images of the stat bars: top speed, acceleration, grip, boost.")]
        [SerializeField] UnityEngine.UI.Image[] statBars;
        [SerializeField] TMP_Text[] statValues;
        [Tooltip("Reference maxima for the bars (same order).")]
        [SerializeField] float[] statMax = { 25f, 20f, 12f, 0.8f };

        public bool IsOpen => panel.activeSelf;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            panel.SetActive(false);
        }

        public void Show()
        {
            if (playerStats != null)
            {
                float[] values = { playerStats.topSpeed, playerStats.acceleration, playerStats.grip, playerStats.boostMaxSpeedBonus };
                string[] labels =
                {
                    $"{playerStats.topSpeed * 10f:0}", $"{playerStats.acceleration * 10f:0}",
                    $"{playerStats.grip * 10f:0}", $"+{playerStats.boostMaxSpeedBonus * 100f:0}%",
                };
                for (int i = 0; i < statBars.Length && i < values.Length; i++)
                {
                    statBars[i].fillAmount = Mathf.Clamp01(values[i] / statMax[i]);
                    statValues[i].text = labels[i];
                }
            }
            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);
    }
}
