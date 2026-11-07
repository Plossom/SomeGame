using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The end of a challenge, over everything: ALL CAUGHT! or TIME'S UP!, the count and your best; the first win
    /// shows the prize (your car wearing it); AGAIN plays it once more, MAP goes back.
    /// </summary>
    public class ChallengeResult : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform card, prize;
        [SerializeField] TMP_Text title, score, best, prizeName;
        [SerializeField] VehicleIcon prizeCar;
        [SerializeField] UnityEngine.UI.Button againButton, mapButton;

        float _shownAt;

        void Awake()
        {
            againButton.onClick.AddListener(() => FindAnyObjectByType<RaceManager>()?.Restart());
            mapButton.onClick.AddListener(GameSession.ReturnToMap);
            panel.SetActive(false);
        }

        public void Show(bool won, int caught, int total, int bestScore, bool firstWin)
        {
            title.text = won ? "ALL CAUGHT!" : "TIME'S UP!";
            title.color = won ? Theme.Orange : Theme.Rust;
            score.text = $"{caught} / {total}";
            best.text = $"BEST  {bestScore} / {total}";
            prize.gameObject.SetActive(firstWin);
            card.sizeDelta = new Vector2(card.sizeDelta.x, firstWin ? 1420f : 900f); // no prize: a shorter card
            if (firstWin)
            {
                var part = Garage.Find("chicken", Garage.Slot.Top);
                prizeName.text = part.Name;
                var look = CarLook.Player;
                look.Top = part;
                prizeCar.Dress(look);
            }
            _shownAt = Time.unscaledTime;
            panel.SetActive(true);
            SomeGame.Audio.GameAudio.Play(won ? SomeGame.Audio.AudioLibrary.Instance?.fanfare : SomeGame.Audio.AudioLibrary.Instance?.lose, 0.8f);
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            float t = Time.unscaledTime - _shownAt;
            group.alpha = Mathf.Clamp01(t / 0.25f);
            card.localScale = Vector3.one * Pop(t / 0.4f);
            prize.localScale = Vector3.one * Pop((t - 0.6f) / 0.4f);
            prize.localRotation = Quaternion.Euler(0f, 0f, 3f * Mathf.Sin(Time.unscaledTime * 2f));
        }

        static float Pop(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return x <= 0f ? 0f : 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
