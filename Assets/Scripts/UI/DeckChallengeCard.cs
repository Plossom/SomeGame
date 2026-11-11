using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The cup's challenge as a card in the race deck, fanned out behind the card of the race of the rival who sets it
    /// (Chicken Chase behind Rosie's Meadow Run); tapped, the two swap (<see cref="RaceDeck"/> does the shuffle). It
    /// is laid out like a race card: the chicken on the corner medallion, the cup and CHALLENGE, its name, your best,
    /// the track in a frame with the runaway chickens pecking about on it (a lock over it while locked), what to do
    /// (15 chickens, 75 seconds), the prize on your car, and PLAY (or who to beat first).
    /// </summary>
    public class DeckChallengeCard : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [Tooltip("Tapping the card while it lies behind brings it to the front.")]
        [SerializeField] UnityEngine.UI.Button body, playButton;
        [SerializeField] UnityEngine.UI.Image picture, tabIcon, playFace, veil;
        [SerializeField] TMP_Text chapter, title, best, prizeName, playLabel, lockLabel;
        [SerializeField] RectTransform chickens;
        [SerializeField] GameObject lockCover, prizeWon;
        [SerializeField] VehicleIcon prizeCar;
        [Tooltip("The frost over the medallion sticking out past the card (like a race card's).")]
        [SerializeField] UnityEngine.UI.Image[] edgeVeils;

        CupDefinition _cup;
        float _front;
        readonly List<(RectTransform rt, Vector2 home, float phase)> _hens = new();

        public UnityEngine.UI.Button Body => body;

        void Awake()
        {
            playButton.onClick.AddListener(Play);
            if (chickens != null)
                for (int i = 0; i < chickens.childCount; i++)
                {
                    var rt = (RectTransform)chickens.GetChild(i);
                    _hens.Add((rt, rt.anchoredPosition, i * 1.7f));
                }
        }

        /// <summary>Open once its rival is beaten (for real: not opened by the developer switch that unlocks the races).</summary>
        public static bool IsOpen(CupDefinition cup) => cup != null && cup.challenge != null &&
            (string.IsNullOrEmpty(cup.challengeAfterRival) || RivalProgress.IsBeaten(cup.challengeAfterRival));

        public void Bind(CupDefinition cup)
        {
            _cup = cup;
            var challenge = cup.challenge;
            bool open = IsOpen(cup);
            bool won = ChallengeProgress.IsWon(challenge.name);
            chapter.text = $"{cup.displayName} · CHALLENGE";
            title.text = challenge.displayName.ToUpperInvariant();
            int score = ChallengeProgress.Best(challenge.name);
            string ink = Theme.Html(Theme.Ink);
            best.text = score > 0 ? $"YOUR BEST <color={ink}>{score} / 15</color>" : "NOT PLAYED YET";
            picture.sprite = challenge.preview;
            picture.enabled = picture.sprite != null;
            lockCover.SetActive(!open);
            var part = Garage.Find("chicken", Garage.Slot.Top);
            prizeName.text = part.Name;
            var look = CarLook.Player;
            look.Top = part;
            prizeCar.Dress(look);
            prizeWon.SetActive(won);
            var rival = Resources.Load<RivalCharacter>("Rivals/" + cup.challengeAfterRival);
            string first = rival != null ? rival.displayName.Split(' ')[0].ToUpperInvariant() : "THE RIVAL";
            lockLabel.text = $"BEAT {first} IN\n{(rival != null && rival.homeRace != null ? rival.homeRace.displayName.ToUpperInvariant() : "THEIR RACE")}";
            if (open)
            {
                playLabel.text = won ? "PLAY AGAIN" : "PLAY";
                playFace.color = Theme.Orange;
            }
            else
            {
                playLabel.text = $"BEAT {first} FIRST";
                playFace.color = Theme.Stone;
            }
        }

        /// <summary>How far in front it is (0 behind its race's card, 1 in front), and how much it shows at all.</summary>
        public void Show(float front, float alpha)
        {
            _front = front;
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.5f;
            // Frosted behind like a race card at the side (the same strength as the race card behind it).
            var frost = Theme.WithAlpha(Theme.Cream, 0.62f * Mathf.SmoothStep(0f, 1f, 0.75f * (1f - front)));
            veil.color = frost;
            if (edgeVeils != null) foreach (var edge in edgeVeils) if (edge != null) edge.color = frost;
            body.interactable = front < 0.05f;
            playButton.interactable = front > 0.95f;
        }

        void Update()
        {
            float t = Time.unscaledTime;
            // The medallion's chicken pecks now and then; the chickens on the track bob and peck about.
            float p = Mathf.Repeat(t, 2.4f);
            tabIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, p < 0.5f ? 8f * Mathf.Sin(p / 0.5f * Mathf.PI * 3f) : 0f);
            foreach (var (rt, home, phase) in _hens)
            {
                float peck = Mathf.Max(0f, Mathf.Sin(t * 2.6f + phase));
                rt.anchoredPosition = home + new Vector2(Mathf.Sin(t * 0.7f + phase) * 6f, peck * 5f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f + phase) * 14f);
            }
        }

        void Play()
        {
            if (_cup == null || !IsOpen(_cup) || GameSession.Loading || _front < 0.95f) return;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.boost, 0.6f);
            GameSession.StartRace(_cup.challenge);
        }
    }
}
