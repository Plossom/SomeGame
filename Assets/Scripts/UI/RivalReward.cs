using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The first win against a rival in their home race, over the results: their face with a sore-loser line,
    /// YOU BEAT ROSIE RUST!, and what is new in your garage (their gadget and their signature part), popping in one
    /// after the other with a fanfare. AWESOME! closes.
    /// </summary>
    public class RivalReward : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform card, portrait, bubble;
        [SerializeField] UnityEngine.UI.Image portraitImage, gadgetIcon;
        [SerializeField] TMP_Text title, line, gadgetName, partName;
        [SerializeField] RectTransform[] prizes;
        [SerializeField] VehicleIcon partCar;
        [SerializeField] UnityEngine.UI.Button okButton;

        float _shownAt;

        void Awake()
        {
            okButton.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        public void Show(RivalCharacter rival, string sayLine)
        {
            portraitImage.sprite = rival.portrait;
            title.text = $"YOU BEAT {rival.displayName.ToUpperInvariant()}!";
            line.text = sayLine;
            var gadget = Garage.Find(rival.gadget, Garage.Slot.Gadget);
            gadgetName.text = gadget?.Name ?? "";
            gadgetIcon.sprite = GarageArt.Instance?.Get(gadget)?.full;
            var part = Garage.Find(rival.top, Garage.Slot.Top);
            partName.text = part?.Name ?? "";
            var look = CarLook.Player;
            look.Top = part;
            partCar.Dress(look);
            _shownAt = Time.unscaledTime;
            panel.SetActive(true);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.fanfare, 0.8f);
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            float t = Time.unscaledTime - _shownAt;
            group.alpha = Mathf.Clamp01(t / 0.2f);
            card.localScale = Vector3.one * Pop(t / 0.4f);
            portrait.localScale = Vector3.one * Pop((t - 0.25f) / 0.35f);
            portrait.localRotation = Quaternion.Euler(0f, 0f, 4f * Mathf.Sin(Time.unscaledTime * 3f));
            bubble.localScale = Vector3.one * Pop((t - 0.45f) / 0.35f);
            for (int i = 0; i < prizes.Length; i++)
            {
                float p = (t - 0.8f - i * 0.25f) / 0.4f;
                prizes[i].localScale = Vector3.one * Pop(p);
                prizes[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-20f, 0f, Mathf.Clamp01(p)) + 2f * Mathf.Sin(Time.unscaledTime * 2f + i));
            }
            okButton.transform.localScale = Vector3.one * Pop((t - 1.4f) / 0.35f);
        }

        static float Pop(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return x <= 0f ? 0f : 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
