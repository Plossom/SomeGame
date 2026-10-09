using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>Popup with a race's details and the Start button (or how many stars are missing).</summary>
    public class LevelCard : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Full-screen dimmer behind the card; tapping it closes the card.")]
        [SerializeField] UnityEngine.UI.Button closeArea;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text info;
        [Tooltip("Stars earned so far on this race.")]
        [SerializeField] UnityEngine.UI.Image[] earnedStars;
        [Tooltip("Time label next to the 1, 2 and 3 star rows.")]
        [SerializeField] TMP_Text[] starTimeLabels;
        [SerializeField] TMP_Text bestTime;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] Color starOn = new(1f, 0.82f, 0.2f);
        [SerializeField] Color starOff = new(1f, 1f, 1f, 0.15f);
        [SerializeField] Color startColor = new(0.2f, 0.7f, 0.3f);
        [SerializeField] Color lockedStartColor = new(0.35f, 0.37f, 0.42f);

        LevelDefinition _level;

        void Awake()
        {
            closeArea.onClick.AddListener(Hide);
            startButton.onClick.AddListener(() => GameSession.StartRace(_level));
            Hide();
        }

        public void Show(LevelCatalog catalog, int index)
        {
            _level = catalog[index];
            title.text = $"{index + 1}. {_level.displayName}";
            info.text = $"{_level.laps} laps  ·  {_level.rivalCount} rivals\nWin the race to earn stars";

            int earned = ProgressStore.StarsOf(_level);
            for (int i = 0; i < earnedStars.Length; i++)
                earnedStars[i].color = i < earned ? starOn : starOff;
            for (int i = 0; i < starTimeLabels.Length; i++)
                starTimeLabels[i].text = i < _level.starTimes.Length ? TimeFormat.Race(_level.starTimes[i]) : "-";

            var best = ProgressStore.BestTimeOf(_level);
            bestTime.text = best.HasValue ? $"Best  {TimeFormat.Race(best.Value)}" : "Not won yet";

            bool canEnter = ProgressStore.CanEnter(catalog, index);
            startButton.interactable = canEnter;
            startButton.image.color = canEnter ? startColor : lockedStartColor;
            int missing = _level.starsRequired - ProgressStore.TotalStars;
            startLabel.text = canEnter ? "START" : $"NEED {missing} MORE STAR{(missing == 1 ? "" : "S")}";
            startLabel.fontSize = canEnter ? 84 : 52;

            root.SetActive(true);
        }

        public void Hide() => root.SetActive(false);
    }
}
