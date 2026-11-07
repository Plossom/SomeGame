using System;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Meeting a rival, the first time their home race is open: over a sunny meadow the rival slides in, standing
    /// by their car (their painting), their name banner pops up, and their words type out line by line in a speech
    /// box (tap for the next line, or to show the whole line at once). With the last line the prize swings in:
    /// their gadget, yours if you beat them; BRING IT ON! closes. SKIP closes at any time.
    /// </summary>
    public class RivalMeeting : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] UnityEngine.UI.Button tapArea, skipButton, goButton;
        [SerializeField] RectTransform painting;
        [SerializeField] UnityEngine.UI.Image paintingImage;
        [SerializeField] RectTransform banner, box, next, prize;
        [SerializeField] TMP_Text nameLabel, titleLabel, lineLabel, prizeName;
        [SerializeField] UnityEngine.UI.Image prizeIcon;
        [SerializeField, Min(5f)] float charactersPerSecond = 42f;

        RivalCharacter _rival;
        Action _done;
        int _line;
        float _openedAt, _lineAt, _prizeAt = -1f, _closingAt = -1f;
        Vector2 _paintingHome, _bannerHome, _boxHome, _prizeHome;
        bool _homes;

        public bool IsOpen => panel.activeSelf;

        void Awake()
        {
            tapArea.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(Finish);
            goButton.onClick.AddListener(Finish);
            panel.SetActive(false);
        }

        public void Show(RivalCharacter rival, Action done)
        {
            if (!_homes)
            {
                _paintingHome = painting.anchoredPosition; _bannerHome = banner.anchoredPosition;
                _boxHome = box.anchoredPosition; _prizeHome = prize.anchoredPosition;
                _homes = true;
            }
            _rival = rival;
            _done = done;
            _line = 0;
            _openedAt = Time.unscaledTime;
            _lineAt = _openedAt + 0.75f;
            _prizeAt = -1f;
            _closingAt = -1f;
            paintingImage.sprite = rival.painting != null ? rival.painting : rival.portrait;
            nameLabel.text = rival.displayName.ToUpperInvariant();
            titleLabel.text = rival.title;
            var gadget = Garage.Find(rival.gadget, Garage.Slot.Gadget);
            prizeName.text = gadget != null ? gadget.Name : "";
            prizeIcon.sprite = GarageArt.Instance?.Get(gadget)?.full;
            goButton.gameObject.SetActive(false);
            prize.gameObject.SetActive(false);
            panel.SetActive(true);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.unlock, 0.7f);
        }

        string Line => _rival.meetLines != null && _line < _rival.meetLines.Length ? _rival.meetLines[_line] : "";
        bool LastLine => _rival.meetLines == null || _line >= _rival.meetLines.Length - 1;
        int Shown => Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - _lineAt) * charactersPerSecond), 0, Line.Length);

        // A tap: the whole line at once, or the next line; after the last one the prize stays until BRING IT ON!
        void Advance()
        {
            if (_closingAt >= 0f || Time.unscaledTime < _lineAt) return;
            if (Shown < Line.Length) { _lineAt = Time.unscaledTime - Line.Length / charactersPerSecond; return; }
            if (LastLine) return;
            _line++;
            _lineAt = Time.unscaledTime;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
        }

        void Finish()
        {
            if (_closingAt >= 0f) return;
            _closingAt = Time.unscaledTime;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.5f);
        }

        void Update()
        {
            if (!panel.activeSelf || _rival == null) return;
            float t = Time.unscaledTime;
            float shut = _closingAt >= 0f ? Mathf.Clamp01((t - _closingAt) / 0.3f) : 0f;
            group.alpha = Mathf.Clamp01((t - _openedAt) / 0.25f) * (1f - shut);
            if (shut >= 1f)
            {
                panel.SetActive(false);
                var done = _done;
                _done = null;
                done?.Invoke();
                return;
            }
            // The rival slides in from the right with a bounce, then breathes.
            float slide = EaseOutBack(Mathf.Clamp01((t - _openedAt - 0.1f) / 0.55f));
            painting.anchoredPosition = _paintingHome + new Vector2(900f * (1f - slide), 0f);
            painting.localScale = new Vector3(1f, 1f + 0.012f * Mathf.Sin(t * 2.2f), 1f);
            // The banner pops, the box rises.
            float pop = EaseOutBack(Mathf.Clamp01((t - _openedAt - 0.45f) / 0.35f));
            banner.localScale = Vector3.one * pop;
            banner.anchoredPosition = _bannerHome;
            float rise = 1f - Mathf.Pow(1f - Mathf.Clamp01((t - _openedAt - 0.3f) / 0.4f), 3f);
            box.anchoredPosition = _boxHome + new Vector2(0f, -500f * (1f - rise));

            // The words type out; the arrow bobs when the line is all there.
            lineLabel.text = Line;
            lineLabel.maxVisibleCharacters = Shown;
            bool full = Shown >= Line.Length;
            next.gameObject.SetActive(full && !LastLine);
            next.anchoredPosition = new Vector2(next.anchoredPosition.x, -10f * Mathf.Abs(Mathf.Sin(t * 4f)));

            // The prize swings in with the last line, then BRING IT ON!
            if (LastLine && full && _prizeAt < 0f)
            {
                _prizeAt = t;
                prize.gameObject.SetActive(prizeIcon.sprite != null);
                goButton.gameObject.SetActive(true);
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.6f);
            }
            if (_prizeAt >= 0f)
            {
                float p = Mathf.Clamp01((t - _prizeAt) / 0.5f);
                prize.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-30f, 6f, EaseOutBack(p)) + 2f * Mathf.Sin(t * 2f));
                prize.localScale = Vector3.one * EaseOutBack(p);
                prize.anchoredPosition = _prizeHome;
                goButton.transform.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01((t - _prizeAt - 0.25f) / 0.35f)) * (1f + 0.03f * Mathf.Sin(t * 5f));
            }
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
