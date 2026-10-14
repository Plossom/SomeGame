using System.Collections.Generic;
using SomeGame.Online;
using SomeGame.Race;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.UI
{
    /// <summary>
    /// Playing with friends, on the map. The ONLINE button opens a card: your name, CREATE ROOM, or a
    /// friend's six-letter code and JOIN. In a room: the code to share, the race (the host picks it),
    /// eight seats (players and bots), the car colours (each colour once) and START (host) or READY.
    /// Always in plain black and greys (<see cref="Theme.Neutral"/>), whatever cup the map shows.
    /// </summary>
    public class OnlineLobbyScreen : MonoBehaviour
    {
        [Header("Entry card")]
        [SerializeField] Button openButton;
        [SerializeField] GameObject entryPanel;
        [SerializeField] Button entryClose, entryDim;
        [SerializeField] TMP_InputField nameField;
        [SerializeField] Button diceButton;
        [SerializeField] Button createButton, joinButton;
        [SerializeField] TMP_InputField codeField;
        [SerializeField] TMP_Text[] codeLetters;
        [SerializeField] Image[] codeBoxes;
        [SerializeField] TMP_Text statusLabel;
        [Tooltip("The progress bar along the bottom of the card while a room opens: fill, the little car on it, its label.")]
        [SerializeField] GameObject progressBar;
        [SerializeField] Image progressFill;
        [SerializeField] RectTransform progressCar;
        [SerializeField] TMP_Text progressLabel;
        [SerializeField] RectTransform[] demoCars;

        [Header("Room")]
        [SerializeField] GameObject roomPanel;
        [SerializeField] CanvasGroup roomGroup;
        [SerializeField] RectTransform roomContent;
        [SerializeField] Button leaveButton, copyButton;
        [SerializeField] TMP_Text codeLabel, copiedLabel;
        [SerializeField] Image racePicture;
        [SerializeField] TMP_Text raceTitle, raceDetails, raceCounter;
        [SerializeField] Image[] raceDifficulty;
        [SerializeField] Button racePrev, raceNext;
        [Tooltip("Row of cup tabs over the race (one per cup, then RANDOM); the inactive tab to copy.")]
        [SerializeField] RectTransform cupTabs;
        [SerializeField] Button tabTemplate;
        [SerializeField] Sprite diceSprite;
        [SerializeField] TMP_Text playersCaption;
        [SerializeField] Button fillButton;
        [SerializeField] OnlineSlotCard[] slotCards;
        [SerializeField] Button[] swatches;
        [SerializeField] RectTransform swatchRing;
        [SerializeField] Button mainButton;
        [SerializeField] Image mainFace, mainShadow;
        [SerializeField] TMP_Text mainLabel, hintLabel;
        [SerializeField] RectTransform toast;
        [SerializeField] TMP_Text toastLabel;

        OnlineSession _session;
        OnlineRoom _room;
        bool _roomShown;
        float _roomShownAt, _copiedAt = -10f, _toastAt = -10f, _raceAt = -10f, _codeAt = -10f;
        int _raceSlide, _shownRace = -1;
        readonly LobbySlot[] _lastSlots = new LobbySlot[OnlineRoom.SlotCount];
        readonly Queue<string> _toasts = new();
        string _status = "";
        bool _statusError;

        void Awake()
        {
            Theme.RecolorAll(transform, Theme.Valley, Theme.Neutral); // built in the Valley colours
            Theme.Hills(transform, Theme.Neutral);
            openButton.onClick.AddListener(OpenEntry);
            entryClose.onClick.AddListener(CloseEntry);
            entryDim.onClick.AddListener(CloseEntry);
            diceButton.onClick.AddListener(() =>
            {
                nameField.text = OnlineSession.RandomName();
                OnlineSession.PlayerName = nameField.text;
            });
            nameField.onEndEdit.AddListener(v =>
            {
                OnlineSession.PlayerName = v;
                nameField.SetTextWithoutNotify(OnlineSession.PlayerName);
            });
            nameField.onValidateInput = (text, index, c) => char.IsLetterOrDigit(c) || c == ' ' ? char.ToUpperInvariant(c) : '\0';
            codeField.onValidateInput = (text, index, c) => char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '\0';
            codeField.onValueChanged.AddListener(_ => ShowCode());
            codeField.onSubmit.AddListener(_ => Join());
            createButton.onClick.AddListener(Create);
            joinButton.onClick.AddListener(Join);

            leaveButton.onClick.AddListener(Leave);
            copyButton.onClick.AddListener(CopyCode);
            racePrev.onClick.AddListener(() => PageRace(-1));
            raceNext.onClick.AddListener(() => PageRace(1));
            tabTemplate.gameObject.SetActive(false);
            fillButton.onClick.AddListener(() => _room?.FillWithBots());
            mainButton.onClick.AddListener(MainAction);
            for (int i = 0; i < swatches.Length; i++)
            {
                int color = i;
                swatches[i].onClick.AddListener(() => PickColor(color));
            }
            for (int i = 0; i < slotCards.Length; i++)
            {
                int slot = i;
                slotCards[i].CardButton.onClick.AddListener(() => TapSeat(slot));
                slotCards[i].RemoveButton.onClick.AddListener(() => _room?.RemoveBot(slot));
            }
            entryPanel.SetActive(false);
            roomPanel.SetActive(false);
            toast.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            _session = OnlineSession.Instance;
            _session.Failed += OnFailed;
            _session.StateChanged += OnSessionChanged;
        }

        void OnDisable()
        {
            if (_session == null) return;
            _session.Failed -= OnFailed;
            _session.StateChanged -= OnSessionChanged;
            Watch(null);
        }

        void Start()
        {
            // Back from an online race: straight into the room.
            if (OnlineSession.Active) ShowRoom();
            else if (OnlineDev.AutoJoin != null && !_autoJoined)
            {
                _autoJoined = true;
                OpenEntry();
                codeField.text = OnlineDev.AutoJoin;
                Join();
            }
        }

        static bool _autoJoined;

        // ------------------------------------------------------------------ entry card

        void OpenEntry()
        {
            if (OnlineSession.Active) { ShowRoom(); return; }
            Redraw();
            entryPanel.SetActive(true); // first: the card always opens
            nameField.SetTextWithoutNotify(OnlineSession.PlayerName);
            SetStatus("", false);
            ShowCode();
            OnSessionChanged();
        }

        // The X (or a tap beside the card) also gives up on a room that is still opening.
        // Makes sure this screen's own canvas is drawn (the map's cup switch can leave it undrawn).
        void Redraw()
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            canvas.enabled = false;
            canvas.enabled = true;
        }

        void CloseEntry()
        {
            if (_session.State == OnlineSession.Status.Connecting) _session.Cancel();
            entryPanel.SetActive(false);
        }

        void Create()
        {
            OnlineSession.PlayerName = nameField.text;
            SetStatus("OPENING A ROOM...", false);
            _session.Create();
        }

        void Join()
        {
            OnlineSession.PlayerName = nameField.text;
            if (codeField.text.Length < 5 && codeField.text != "LOCAL")
            {
                SetStatus("THE CODE HAS 6 LETTERS", true);
                return;
            }
            SetStatus("JOINING " + codeField.text + "...", false);
            _session.Join(codeField.text);
        }

        void ShowCode()
        {
            string code = codeField.text;
            for (int i = 0; i < codeLetters.Length; i++)
                codeLetters[i].text = i < code.Length ? code[i].ToString() : "";
        }

        void SetStatus(string text, bool error)
        {
            _status = text;
            _statusError = error;
            statusLabel.text = text;
            statusLabel.color = error ? Theme.Neutral.Rust : Theme.Neutral.InkSoft;
        }

        void OnFailed(string message)
        {
            if (_roomShown)
            {
                HideRoom();
                OpenEntry();
            }
            if (entryPanel.activeSelf) SetStatus(message, true);
        }

        void OnSessionChanged()
        {
            bool free = _session.State == OnlineSession.Status.Offline;
            createButton.interactable = joinButton.interactable = free;
            // Busy buttons look it.
            foreach (var b in new[] { createButton, joinButton })
                b.GetComponent<CanvasGroup>().alpha = free ? 1f : 0.45f;
            if (_session.State == OnlineSession.Status.InRoom)
            {
                entryPanel.SetActive(false);
                ShowRoom();
            }
            else if (_session.State == OnlineSession.Status.Offline && _roomShown && !_statusError) HideRoom();
        }

        // ------------------------------------------------------------------ room

        void ShowRoom()
        {
            if (OnlineRoom.Current == null) return;
            Watch(OnlineRoom.Current);
            Redraw();
            _roomShown = true;
            _roomShownAt = _codeAt = Time.unscaledTime;
            _shownRace = -1;
            roomPanel.SetActive(true);
            for (int i = 0; i < _lastSlots.Length; i++) _lastSlots[i] = _room.Slots.Count > i ? _room.Slots[i] : default;
            Refresh(true);
        }

        void HideRoom()
        {
            _roomShown = false;
            roomPanel.SetActive(false);
            Watch(null);
        }

        void Watch(OnlineRoom room)
        {
            if (_room == room) return;
            if (_room != null) _room.Changed -= OnRoomChanged;
            _room = room;
            if (_room != null) _room.Changed += OnRoomChanged;
        }

        void Leave()
        {
            _session.Leave();
            HideRoom();
        }

        void OnRoomChanged()
        {
            if (!_roomShown || _room == null) return;
            if (OnlineDev.AutoReady && !_room.IsServer && _room.Phase.Value == RoomPhase.Lobby && _room.LocalSlot >= 0 && !_room.Slots[_room.LocalSlot].Ready)
                _room.SetReady(true);
            // Toasts for people coming and going.
            for (int i = 0; i < _room.Slots.Count && i < _lastSlots.Length; i++)
            {
                var now = _room.Slots[i];
                var before = _lastSlots[i];
                if (now.Kind == SlotKind.Human && (before.Kind != SlotKind.Human || before.ClientId != now.ClientId) && i != _room.LocalSlot)
                    _toasts.Enqueue(now.Name + " JOINED");
                else if (before.Kind == SlotKind.Human && now.Kind != SlotKind.Human && before.ClientId != _room.NetworkManager.LocalClientId)
                    _toasts.Enqueue(before.Name + " LEFT");
                _lastSlots[i] = now;
            }
            Refresh(false);
        }

        void Refresh(bool intro)
        {
            if (_room == null || !_room.IsSpawned) return;
            bool host = _room.IsServer;
            bool inLobby = _room.Phase.Value == RoomPhase.Lobby;
            int me = _room.LocalSlot;

            codeLabel.text = _session.Code;

            // The race: cup tabs (and RANDOM), then the race of that cup.
            BuildTabs();
            bool random = _room.RandomRace.Value;
            var (cup, inCup, count) = OnlineRoom.Locate(_room.RaceIndex.Value);
            int shown = random ? -2 : _room.RaceIndex.Value;
            var level = _room.Race;
            if (level != null && shown != _shownRace)
            {
                if (_shownRace != -1) _raceAt = Time.unscaledTime;
                _shownRace = shown;
                if (random)
                {
                    racePicture.sprite = diceSprite;
                    racePicture.color = Theme.Neutral.Amber;
                    raceTitle.text = "RANDOM RACE";
                    raceDetails.text = "ANY CUP · A SURPRISE";
                }
                else
                {
                    racePicture.sprite = level.preview;
                    racePicture.color = Color.white;
                    raceTitle.text = level.displayName.ToUpperInvariant();
                    raceDetails.text = level.laps + (level.laps == 1 ? " LAP" : " LAPS");
                }
                racePicture.enabled = racePicture.sprite != null;
                for (int i = 0; i < raceDifficulty.Length; i++)
                {
                    raceDifficulty[i].gameObject.SetActive(!random);
                    raceDifficulty[i].color = i < level.difficulty ? Theme.Neutral.Amber : Theme.WithAlpha(Theme.Neutral.Stone, 0.25f);
                }
            }
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool on = i == _tabs.Count - 1 ? random : !random && i == cup;
                var (button, face, label, icon) = _tabs[i];
                face.color = on ? Theme.Neutral.Amber : Theme.Neutral.InkSoft; // the chosen tab lights up
                label.color = on ? Theme.Neutral.Ink : Theme.Neutral.Stone;
                if (icon != null) icon.color = on ? Theme.Neutral.Ink : Theme.Neutral.Stone;
                button.interactable = host && inLobby;
            }
            raceCounter.text = $"RACE {inCup + 1}/{count}";
            bool paging = host && inLobby && !random && count > 1;
            racePrev.gameObject.SetActive(paging);
            raceNext.gameObject.SetActive(paging);
            raceCounter.gameObject.SetActive(!random);

            // Seats.
            int people = 0, filled = 0;
            for (int i = 0; i < slotCards.Length && i < _room.Slots.Count; i++)
            {
                var slot = _room.Slots[i];
                if (slot.Kind == SlotKind.Human) people++;
                if (slot.Kind != SlotKind.Empty) filled++;
                bool hostSeat = slot.Kind == SlotKind.Human && slot.ClientId == Unity.Netcode.NetworkManager.ServerClientId;
                slotCards[i].Show(slot, i == me, hostSeat, host, inLobby, _room.CrownSlot.Value == i, intro ? 0.15f + i * 0.05f : -1f);
            }
            playersCaption.text = $"RACERS  <color={Theme.Html(Theme.Neutral.InkSoft)}>{filled}/8</color>";
            fillButton.gameObject.SetActive(host && inLobby && filled < OnlineRoom.SlotCount);

            // Colours: yours ringed, the others' taken ones dimmed.
            int mine = me >= 0 ? _room.Slots[me].Color : -1;
            for (int c = 0; c < swatches.Length; c++)
            {
                var image = swatches[c].targetGraphic as Image;
                bool free = me < 0 || _room.ColorFree(c, me);
                image.color = Theme.WithAlpha(OnlineColors.Get(c), free ? 1f : 0.28f);
                swatches[c].interactable = free && inLobby;
            }
            swatchRing.gameObject.SetActive(mine >= 0);

            // START / READY.
            if (!inLobby)
            {
                bool inRace = me >= 0 && _room.InRace(me);
                Main(inRace ? "STARTING..." : "RACE ON...", false, false);
                hintLabel.text = inRace ? "GET READY!" : "YOU JOIN THE NEXT RACE";
            }
            else if (host)
            {
                bool ready = _room.AllReady;
                Main("START RACE", ready, ready);
                hintLabel.text = filled < 2 ? "ADD BOTS OR SHARE THE CODE WITH FRIENDS"
                    : ready ? "EVERYONE IS READY" : "WAITING FOR EVERYONE TO BE READY";
            }
            else
            {
                bool ready = me >= 0 && _room.Slots[me].Ready;
                Main(ready ? "READY!" : "TAP WHEN READY", true, !ready);
                hintLabel.text = ready ? "WAITING FOR THE HOST TO START" : "THE HOST PICKS THE RACE";
            }
        }

        bool _mainPulse;

        void Main(string label, bool enabled, bool pulse)
        {
            mainLabel.text = label;
            mainButton.interactable = enabled;
            mainFace.color = enabled ? Theme.Neutral.Orange : Theme.Neutral.Stone;
            mainShadow.color = enabled ? Theme.Neutral.OrangeDeep : Theme.Neutral.StoneDark;
            mainLabel.color = enabled ? Color.white : Theme.Neutral.StoneDark;
            _mainPulse = pulse;
        }

        void MainAction()
        {
            if (_room == null || _room.Phase.Value != RoomPhase.Lobby) return;
            if (_room.IsServer) _room.StartRace();
            else
            {
                int me = _room.LocalSlot;
                if (me >= 0) _room.SetReady(!_room.Slots[me].Ready);
            }
        }

        void PageRace(int step)
        {
            if (_room == null) return;
            _raceSlide = step;
            _room.PageRace(step);
        }

        readonly List<(Button button, Image face, TMP_Text label, Image icon)> _tabs = new();

        // One tab per cup with races, then RANDOM (with the die).
        void BuildTabs()
        {
            if (_tabs.Count > 0) return;
            var cups = OnlineRoom.Cups;
            for (int i = 0; i <= cups.Count; i++)
            {
                bool random = i == cups.Count;
                var tab = Instantiate(tabTemplate, cupTabs);
                tab.gameObject.SetActive(true);
                var label = tab.GetComponentInChildren<TMP_Text>();
                label.text = random ? "RANDOM" : cups[i].displayName.ToUpperInvariant();
                var icon = tab.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null) icon.gameObject.SetActive(random);
                int cup = i;
                tab.onClick.AddListener(() =>
                {
                    if (_room == null) return;
                    _raceSlide = 0;
                    if (random) _room.SetRandom(!_room.RandomRace.Value);
                    else _room.SelectCup(cup);
                });
                _tabs.Add((tab, (Image)tab.targetGraphic, label, random ? icon : null));
            }
        }

        void PickColor(int color)
        {
            if (_room == null) return;
            _room.PickColor(color);
        }

        void TapSeat(int slot)
        {
            if (_room == null || !_room.IsServer) return;
            var seat = _room.Slots[slot];
            if (seat.Kind == SlotKind.Empty) _room.AddBot();
            else if (seat.Kind == SlotKind.Bot) _room.CycleBotColor(slot); // tap a bot: try another colour
        }

        void CopyCode()
        {
            GUIUtility.systemCopyBuffer = _session.Code;
            _copiedAt = Time.unscaledTime;
        }

        // ------------------------------------------------------------------ animation

        void Update()
        {
            float now = Time.unscaledTime;
            if (entryPanel.activeSelf) AnimateEntry(now);
            if (!_roomShown) return;
            if (_room == null || !_room.IsSpawned) return;

            // The room slides up and fades in.
            float t = Mathf.Clamp01((now - _roomShownAt) / 0.45f);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            roomGroup.alpha = Mathf.Clamp01(t * 2f);
            roomContent.anchoredPosition = new Vector2(0f, -140f * (1f - e));

            // The code drops in letter by letter.
            string code = _session.Code;
            float dropped = (now - _codeAt - 0.2f) / 0.07f;
            codeLabel.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(dropped) + 1, 0, code.Length);
            copiedLabel.alpha = Mathf.Clamp01(1.6f - (now - _copiedAt));
            copiedLabel.rectTransform.anchoredPosition = new Vector2(copiedLabel.rectTransform.anchoredPosition.x, -6f + 20f * Mathf.Clamp01((now - _copiedAt) * 3f));

            // The race picture slides in from the side of the arrow.
            float r = Mathf.Clamp01((now - _raceAt) / 0.3f);
            float re = 1f - Mathf.Pow(1f - r, 3f);
            racePicture.rectTransform.anchoredPosition = new Vector2(_raceSlide * 80f * (1f - re), 0f);
            var pc = racePicture.color; pc.a = r < 1f ? re : 1f; racePicture.color = pc;
            racePicture.rectTransform.localScale = Vector3.one * (_raceSlide == 0 && r < 1f ? Mathf.Lerp(0.85f, 1f, re) : 1f);
            raceTitle.alpha = r < 1f ? re : 1f;

            // Your colour's ring glides to it.
            int me = _room.LocalSlot;
            if (me >= 0 && me < _room.Slots.Count)
            {
                var target = (RectTransform)swatches[Mathf.Clamp(_room.Slots[me].Color, 0, swatches.Length - 1)].transform;
                swatchRing.anchoredPosition = Vector2.Lerp(swatchRing.anchoredPosition, target.anchoredPosition, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
                swatchRing.localRotation = Quaternion.Euler(0f, 0f, now * -40f);
                for (int c = 0; c < swatches.Length; c++)
                {
                    float goal = c == _room.Slots[me].Color ? 1.12f : swatches[c].interactable ? 1f : 0.78f;
                    var st = swatches[c].transform;
                    st.localScale = Vector3.one * Mathf.Lerp(st.localScale.x, goal, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
                }
            }

            // START / READY breathes when it is waiting for you.
            float pulse = _mainPulse ? 1f + 0.035f * Mathf.Sin(now * 5f) : 1f;
            mainFace.transform.parent.localScale = Vector3.one * pulse;

            // Toasts: one at a time, sliding down from the top.
            if (now - _toastAt > 2.4f && _toasts.Count > 0)
            {
                toastLabel.text = _toasts.Dequeue();
                _toastAt = now;
                toast.gameObject.SetActive(true);
            }
            float k = now - _toastAt;
            if (k < 2.4f)
            {
                float into = Mathf.Clamp01(k / 0.25f), outOf = Mathf.Clamp01((k - 2f) / 0.3f);
                float y = Mathf.Lerp(120f, -20f, 1f - Mathf.Pow(1f - into, 3f)) + 140f * outOf * outOf;
                toast.anchoredPosition = new Vector2(0f, y);
            }
            else if (toast.gameObject.activeSelf) toast.gameObject.SetActive(false);
        }

        float _shownProgress, _stepSince;
        string _lastStep;

        void AnimateEntry(float now)
        {
            bool busy = _session.State == OnlineSession.Status.Connecting;
            if (!busy && !_statusError && _status.EndsWith("...")) SetStatus("", false);
            // Opening / joining: the bar fills step by step (creeping on inside a step, so it never looks stuck).
            progressBar.SetActive(busy);
            statusLabel.gameObject.SetActive(!busy);
            if (busy)
            {
                if (_session.Progress < _shownProgress - 0.01f) _shownProgress = 0f;
                float goal = Mathf.Min(_session.Progress + 0.2f * (1f - Mathf.Exp(-(now - _stepSince) * 0.4f)), 0.97f);
                if (_session.Step != _lastStep) { _lastStep = _session.Step; _stepSince = now; }
                _shownProgress = Mathf.MoveTowards(_shownProgress, Mathf.Max(goal, _session.Progress), Time.unscaledDeltaTime * 0.8f);
                progressFill.fillAmount = _shownProgress;
                var track = (RectTransform)progressCar.parent;
                progressCar.anchoredPosition = new Vector2(track.rect.width * _shownProgress, 64f + Mathf.Abs(Mathf.Sin(now * 9f)) * 6f);
                progressLabel.text = _session.Step + "  " + Mathf.RoundToInt(_shownProgress * 100f) + "%";
            }
            else _shownProgress = 0f;
            // Three little cars racing side by side, swinging into drifts.
            for (int i = 0; i < demoCars.Length; i++)
            {
                float p = now * 1.7f + i * 2.1f;
                var c = demoCars[i];
                float baseX = (i - (demoCars.Length - 1) * 0.5f) * 210f;
                c.anchoredPosition = new Vector2(baseX + Mathf.Sin(p) * 34f, Mathf.Sin(p * 2f) * 10f + Mathf.Cos(now * 0.9f + i) * 14f);
                c.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Cos(p) * 22f);
            }
            // The code boxes: the next one to fill glows.
            int next = codeField.isFocused ? Mathf.Min(codeField.text.Length, codeBoxes.Length - 1) : -1;
            for (int i = 0; i < codeBoxes.Length; i++)
            {
                bool on = i == next;
                codeBoxes[i].color = on ? Color.Lerp(Theme.Neutral.Orange, Theme.Neutral.Amber, 0.5f + 0.5f * Mathf.Sin(now * 6f)) : Theme.Neutral.Ink;
                float s = i < codeField.text.Length ? 1f : 0.96f;
                codeBoxes[i].transform.localScale = Vector3.one * Mathf.Lerp(codeBoxes[i].transform.localScale.x, s, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
            }
        }
    }
}
