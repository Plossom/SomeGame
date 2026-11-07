using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The cup's challenge as a card in the race deck, tucked behind the card of the race of the rival who sets it
    /// (Chicken Chase behind Rosie's Meadow Run): a CHALLENGE tab sticks out of its edge. Tapped, the two cards swap
    /// (<see cref="RaceDeck"/> does the shuffle); in front it shows what to do, your best, the prize on your car
    /// and PLAY (or who to beat first while it is locked).
    /// </summary>
    public class DeckChallengeCard : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [Tooltip("Tapping the card (or its tab) while it lies behind brings it to the front.")]
        [SerializeField] UnityEngine.UI.Button body, tab, playButton;
        [SerializeField] UnityEngine.UI.Image icon, tabIcon, playFace, veil;
        [SerializeField] TMP_Text title, description, best, prizeName, playLabel;
        [SerializeField] VehicleIcon prizeCar;
        [SerializeField] GameObject prizeWon;

        CupDefinition _cup;
        float _front;

        public UnityEngine.UI.Button Body => body;
        public UnityEngine.UI.Button Tab => tab;

        void Awake() => playButton.onClick.AddListener(Play);

        public static bool IsOpen(CupDefinition cup) => cup != null && cup.challenge != null &&
            (CupList.DevUnlockAll || string.IsNullOrEmpty(cup.challengeAfterRival) || RivalProgress.IsBeaten(cup.challengeAfterRival));

        /// <summary>The race (index in the cup) whose card it lies behind, or -1 (no challenge).</summary>
        public static int RaceOf(CupDefinition cup, LevelCatalog catalog)
        {
            if (cup == null || cup.challenge == null || catalog == null) return -1;
            var rival = string.IsNullOrEmpty(cup.challengeAfterRival) ? null : Resources.Load<RivalCharacter>("Rivals/" + cup.challengeAfterRival);
            return rival != null && rival.homeRace != null ? catalog.IndexOf(rival.homeRace) : catalog.Count - 1;
        }

        public void Bind(CupDefinition cup)
        {
            _cup = cup;
            var challenge = cup.challenge;
            bool open = IsOpen(cup);
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
        }

        /// <summary>How far in front it is (0 behind its race's card, 1 in front), and how much it shows at all.</summary>
        public void Show(float front, float alpha)
        {
            _front = front;
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.5f;
            veil.color = Theme.WithAlpha(Theme.Cream, 0.55f * (1f - front));
            body.interactable = front < 0.05f;
            playButton.interactable = front > 0.95f;
            bool tabShown = front < 0.5f && alpha > 0.5f;
            if (tab.gameObject.activeSelf != tabShown) tab.gameObject.SetActive(tabShown);
        }

        void Update()
        {
            // The tab's chicken pecks now and then.
            if (!tab.gameObject.activeSelf) return;
            float t = Mathf.Repeat(Time.unscaledTime, 2.4f);
            tabIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t < 0.5f ? 8f * Mathf.Sin(t / 0.5f * Mathf.PI * 3f) : 0f);
        }

        void Play()
        {
            if (_cup == null || !IsOpen(_cup) || GameSession.Loading || _front < 0.95f) return;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.boost, 0.6f);
            GameSession.StartRace(_cup.challenge);
        }
    }
}
