using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// A challenge's card over the map: what it is, your best, the prize (your car wearing it, or a tick once
    /// won) and PLAY, or who to beat first while it is locked.
    /// </summary>
    public class ChallengePanel : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Button closeButton, closeArea, playButton;
        [SerializeField] UnityEngine.UI.Image icon, playFace;
        [SerializeField] TMP_Text title, description, best, prizeName, playLabel;
        [SerializeField] VehicleIcon prizeCar;
        [SerializeField] GameObject prizeWon;

        CupDefinition _cup;

        void Awake()
        {
            closeButton.onClick.AddListener(Close);
            closeArea.onClick.AddListener(Close);
            playButton.onClick.AddListener(Play);
            panel.SetActive(false);
        }

        public bool IsOpen => panel.activeSelf;

        public void Show(CupDefinition cup)
        {
            _cup = cup;
            var challenge = cup.challenge;
            bool open = ChallengeStop.IsOpen(cup);
            bool won = ChallengeProgress.IsWon(challenge.name);
            title.text = challenge.displayName.ToUpperInvariant();
            description.text = challenge.description;
            int score = ChallengeProgress.Best(challenge.name);
            best.text = score > 0 ? $"YOUR BEST  {score} / 15" : "NOT PLAYED YET";
            icon.color = open ? Color.white : new Color(0.1f, 0.12f, 0.14f, 0.8f);
            var part = Garage.Find("chicken", Garage.Slot.Top);
            prizeName.text = part.Name;
            var look = CarLook.Player;
            look.Top = part;
            prizeCar.Dress(look);
            prizeWon.SetActive(won);
            if (open)
            {
                playLabel.text = won ? "PLAY AGAIN" : "PLAY";
                playFace.color = Theme.Orange;
            }
            else
            {
                var rival = Resources.Load<RivalCharacter>("Rivals/" + cup.challengeAfterRival);
                playLabel.text = rival != null ? $"BEAT {rival.displayName.Split(' ')[0].ToUpperInvariant()} FIRST" : "LOCKED";
                playFace.color = Theme.Stone;
            }
            PopIn.Show(panel);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.5f);
        }

        void Close() => PopIn.Hide(panel);

        void Play()
        {
            if (_cup == null || !ChallengeStop.IsOpen(_cup) || GameSession.Loading) return;
            GameSession.StartRace(_cup.challenge);
        }
    }
}
