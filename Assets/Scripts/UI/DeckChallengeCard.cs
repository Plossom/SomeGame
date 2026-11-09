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
        [Tooltip("Tapping the card while it lies behind brings it to the front.")]
        [SerializeField] UnityEngine.UI.Button body, playButton;
        [SerializeField] UnityEngine.UI.Image icon, tabIcon, playFace, veil;
        [Tooltip("The ring of the chicken badge in the corner (grey while the card lies behind).")]
        [SerializeField] UnityEngine.UI.Image cornerFace;
        [SerializeField] TMP_Text title, description, best, prizeName, playLabel;
        [SerializeField] VehicleIcon prizeCar;
        [SerializeField] GameObject prizeWon;
        [Tooltip("A lock over the picture while it is locked.")]
        [SerializeField] GameObject lockBadge;

        CupDefinition _cup;
        float _front;

        public UnityEngine.UI.Button Body => body;

        void Awake() => playButton.onClick.AddListener(Play);

        /// <summary>Open once its rival is beaten (for real: not opened by the developer switch that unlocks the races).</summary>
        public static bool IsOpen(CupDefinition cup) => cup != null && cup.challenge != null &&
            (string.IsNullOrEmpty(cup.challengeAfterRival) || RivalProgress.IsBeaten(cup.challengeAfterRival));

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
            if (lockBadge != null) lockBadge.SetActive(!open);
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
            // The corner chicken greys out while the card lies behind.
            if (cornerFace != null) cornerFace.color = Color.Lerp(Theme.Stone, Theme.Orange, front);
            tabIcon.color = Color.Lerp(new Color(0.72f, 0.72f, 0.72f), Color.white, front);
            body.interactable = front < 0.05f;
            playButton.interactable = front > 0.95f;
        }

        void Update()
        {
            // The corner chicken pecks now and then.
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
