using SomeGame.Online;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.UI
{
    /// <summary>
    /// One seat in the online room: the car in its colour, the name and tags (YOU, HOST, BOT) and a
    /// ready check; an empty seat is a dashed outline (the host can tap it to add a bot). Changes are
    /// animated: a new player pops in, a leaving one shrinks away, a new colour squashes the car, the
    /// check pops when ready.
    /// </summary>
    public class OnlineSlotCard : MonoBehaviour
    {
        [SerializeField] RectTransform body;
        [SerializeField] GameObject filled;
        [SerializeField] GameObject empty;
        [SerializeField] Image face;
        [SerializeField] Image highlight;
        [SerializeField] RectTransform car;
        [SerializeField] Image carBody;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] GameObject youTag, hostTag, botTag;
        [SerializeField] RectTransform readyBadge;
        [SerializeField] Image readyFace;
        [SerializeField] GameObject readyCheck;
        [SerializeField] Button removeButton;
        [SerializeField] Button cardButton;
        [SerializeField] TMP_Text emptyLabel;
        [SerializeField] GameObject emptyPlus;
        [Tooltip("The winner's crown, placed right after the name.")]
        [SerializeField] RectTransform crown;

        public Button CardButton => cardButton;
        public Button RemoveButton => removeButton;

        SlotKind _kind = (SlotKind)255;
        int _color = -1;
        bool _ready;
        float _popAt = -10f, _squashAt = -10f, _readyAt = -10f, _leaveAt = -10f, _crownAt = -10f;
        bool _crowned;
        const float NameWidth = 262f;
        float _delay;
        Color _carFrom, _carTo;

        /// <summary>Shows the seat. <paramref name="intro"/> staggers the first appearance (room opening).</summary>
        public void Show(LobbySlot slot, bool isLocal, bool hostSeat, bool hostView, bool inLobby, bool crowned, float intro = -1f)
        {
            if (intro >= 0f)
            {
                _delay = intro;
                _popAt = Time.unscaledTime;
            }
            if (slot.Kind != _kind)
            {
                bool wasFilled = _kind == SlotKind.Human || _kind == SlotKind.Bot;
                bool nowFilled = slot.Kind != SlotKind.Empty;
                if (nowFilled && intro < 0f) { _popAt = Time.unscaledTime; _delay = 0f; }
                else if (wasFilled && !nowFilled && intro < 0f) _leaveAt = Time.unscaledTime;
                _kind = slot.Kind;
                _color = -1;
            }
            bool isFilled = slot.Kind != SlotKind.Empty;
            if (_leaveAt < 0f || Time.unscaledTime - _leaveAt > 0.25f)
            {
                filled.SetActive(isFilled);
                empty.SetActive(!isFilled);
            }
            emptyLabel.text = hostView && inLobby ? "ADD BOT" : "OPEN SEAT";
            emptyPlus.SetActive(hostView && inLobby);
            cardButton.interactable = hostView && inLobby && slot.Kind != SlotKind.Human;
            if (!isFilled) { crown.gameObject.SetActive(false); _crowned = false; return; }

            nameLabel.text = slot.Name.ToString();
            // The crown sits right after the name (the name gives up room for it).
            if (crowned && !_crowned) _crownAt = Time.unscaledTime;
            _crowned = crowned;
            crown.gameObject.SetActive(crowned);
            var nameRt = nameLabel.rectTransform;
            nameRt.sizeDelta = new Vector2(crowned ? NameWidth - 52f : NameWidth, nameRt.sizeDelta.y);
            if (crowned)
            {
                nameLabel.ForceMeshUpdate();
                float width = Mathf.Min(nameLabel.textBounds.size.x, NameWidth - 52f);
                crown.anchoredPosition = new Vector2(nameRt.anchoredPosition.x + width + 28f, crown.anchoredPosition.y);
            }
            youTag.SetActive(isLocal);
            hostTag.SetActive(hostSeat);
            botTag.SetActive(slot.Kind == SlotKind.Bot);
            highlight.enabled = isLocal;
            removeButton.gameObject.SetActive(hostView && inLobby && slot.Kind == SlotKind.Bot);
            if (slot.Color != _color)
            {
                _carFrom = _color < 0 ? OnlineColors.Get(slot.Color) : carBody.color;
                _carTo = OnlineColors.Get(slot.Color);
                if (_color >= 0) _squashAt = Time.unscaledTime;
                else carBody.color = _carTo;
                _color = slot.Color;
            }
            // Bots and the host are always ready; players tap READY.
            bool ready = slot.Kind == SlotKind.Bot || hostSeat || slot.Ready;
            readyBadge.gameObject.SetActive(slot.Kind == SlotKind.Human);
            if (ready && !_ready) _readyAt = Time.unscaledTime;
            _ready = ready;
            readyCheck.SetActive(ready);
            readyFace.color = ready ? Theme.Leaf : Theme.WithAlpha(Theme.StoneDark, 0.35f);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            // Pop in (overshoot), or shrink away when the seat empties.
            float scale = 1f;
            float pop = (now - _popAt - _delay) / 0.42f;
            if (pop < 0f) scale = 0f;
            else if (pop < 1f) scale = Back(pop);
            if (_leaveAt > 0f)
            {
                float leave = (now - _leaveAt) / 0.25f;
                if (leave < 1f) scale = Mathf.Lerp(1f, 0.6f, leave);
                else
                {
                    _leaveAt = -10f;
                    filled.SetActive(_kind != SlotKind.Empty);
                    empty.SetActive(_kind == SlotKind.Empty);
                    _popAt = now; _delay = 0f;
                }
            }
            body.localScale = Vector3.one * scale;

            // The car: a slow idle wiggle; a squash and a colour blend when the colour changes.
            float squash = Mathf.Clamp01((now - _squashAt) / 0.35f);
            float bounce = squash < 1f ? Mathf.Sin(squash * Mathf.PI) * (1f - squash) : 0f;
            car.localScale = new Vector3(1f + 0.25f * bounce, 1f - 0.18f * bounce, 1f);
            car.localRotation = Quaternion.Euler(0f, 0f, -90f + Mathf.Sin(now * 2.2f + transform.GetSiblingIndex()) * 4f);
            if (squash < 1f) carBody.color = Color.Lerp(_carFrom, _carTo, squash * 2f);

            if (_crowned)
            {
                float c = (now - _crownAt) / 0.5f;
                crown.localScale = Vector3.one * (c >= 0f && c < 1f ? Back(c) : 1f);
                crown.localRotation = Quaternion.Euler(0f, 0f, 12f + Mathf.Sin(now * 2.6f) * 7f);
            }

            float r = (now - _readyAt) / 0.35f;
            readyBadge.localScale = Vector3.one * (r >= 0f && r < 1f ? Back(r) : 1f);
        }

        static float Back(float t)
        {
            const float s = 1.9f;
            float u = Mathf.Clamp01(t) - 1f;
            return 1f + u * u * ((s + 1f) * u + s);
        }
    }
}
