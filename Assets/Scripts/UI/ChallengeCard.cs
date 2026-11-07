using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The map's challenge card (top right, under the cup name): the shown cup's funny challenge. Locked until its
    /// rival is beaten (it says who), then PLAY with your best; done shows a tick. A cup without one says MORE FUN
    /// SOON. Tap to play.
    /// </summary>
    public class ChallengeCard : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image icon;
        [SerializeField] TMP_Text title, status;
        [SerializeField] GameObject lockIcon, doneBadge, playBadge;

        CupDefinition _cup;
        bool _bound;

        void Awake() => button.onClick.AddListener(Play);

        bool Open => _cup != null && _cup.challenge != null &&
                     (CupList.DevUnlockAll || string.IsNullOrEmpty(_cup.challengeAfterRival) || RivalProgress.IsBeaten(_cup.challengeAfterRival));

        void Update()
        {
            var cup = CupList.Selected;
            if (_bound && cup == _cup) { Breathe(); return; }
            _cup = cup;
            _bound = true;
            Bind();
        }

        void Bind()
        {
            var challenge = _cup != null ? _cup.challenge : null;
            bool open = Open;
            bool won = challenge != null && ChallengeProgress.IsWon(challenge.name);
            title.text = challenge != null ? challenge.displayName.ToUpperInvariant() : "CHALLENGE";
            icon.enabled = challenge != null;
            icon.color = open ? Color.white : new Color(0.1f, 0.12f, 0.14f, 0.85f);
            lockIcon.SetActive(challenge != null && !open);
            doneBadge.SetActive(won);
            playBadge.SetActive(open && !won);
            if (challenge == null) status.text = "MORE FUN SOON";
            else if (!open)
            {
                var rival = Resources.Load<RivalCharacter>("Rivals/" + _cup.challengeAfterRival);
                status.text = rival != null ? $"BEAT {rival.displayName.Split(' ')[0].ToUpperInvariant()} FIRST" : "LOCKED";
            }
            else
            {
                int best = ChallengeProgress.Best(challenge.name);
                status.text = won ? "DONE! PLAY AGAIN" : best > 0 ? $"BEST {best} / 15" : "NEW! TAP TO PLAY";
            }
        }

        // An open challenge you haven't won wiggles now and then.
        void Breathe()
        {
            bool tease = Open && _cup.challenge != null && !ChallengeProgress.IsWon(_cup.challenge.name);
            float t = Mathf.Repeat(Time.unscaledTime, 3.2f);
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tease && t < 0.6f ? 10f * Mathf.Sin(t / 0.6f * Mathf.PI * 4f) * (1f - t / 0.6f) : 0f);
        }

        void Play()
        {
            if (_cup == null || _cup.challenge == null || GameSession.Loading) return;
            if (!Open)
            {
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
                return;
            }
            GameSession.StartRace(_cup.challenge);
        }
    }
}
