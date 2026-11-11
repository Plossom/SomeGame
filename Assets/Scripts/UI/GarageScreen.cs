using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The garage, a showroom over the map: your car big in the middle on a lit podium, with a glowing hotspot for
    /// each kind of thing joined to its part of the car by a line (PAINT on its flank, BODY, TOP on the roof, DECAL
    /// on the bonnet, TRAIL behind it, GADGET), each saying what you have on. The car drops onto the podium, the
    /// lines draw out and the hotspots pop in one after another.
    /// Tap one: the others fly away, the car rises, its part pulses, and a row of big cards slides up from the
    /// bottom with everything for that slot, cup by cup (also the locked ones, with what they take: stars of the
    /// cup, a rival to beat, the cup's gold or its challenge). Tap a card: the car takes it there and then with a
    /// pop, a ring of light and sparks; a locked one shakes and says what it takes. DONE (or a tap on the car) goes
    /// back to all the hotspots. The rivals' cards are behind their own button.
    /// </summary>
    public class GarageScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform sheet;
        [SerializeField] UnityEngine.UI.Button closeButton, rivalsButton;
        [SerializeField] TMP_Text starsLabel;
        [Header("The car")]
        [SerializeField] RectTransform carRoot;
        [SerializeField] VehicleIcon car;
        [SerializeField] RectTransform podium, podiumRing;
        [SerializeField] UnityEngine.UI.Image burst, highlight;
        [SerializeField] UnityEngine.UI.Button carButton;
        [SerializeField] Sprite sparkle;
        [Header("Hotspots")]
        [SerializeField] RectTransform hotspotTemplate;
        [SerializeField] Sprite[] slotIcons;
        [SerializeField] TMP_Text hint, buildName;
        [Header("The row of cards")]
        [SerializeField] RectTransform tray;
        [SerializeField] UnityEngine.UI.ScrollRect scroll;
        [SerializeField] RectTransform content, tileTemplate, dividerTemplate;
        [SerializeField] TMP_Text trayTitle, trayCount, itemName, itemBlurb;
        [SerializeField] UnityEngine.UI.Button doneButton;
        [Header("The rivals' cards")]
        [SerializeField] GameObject rivalsPanel;
        [SerializeField] RectTransform rivalsGrid, cardTemplate;
        [SerializeField] UnityEngine.UI.Button rivalsClose;
        [SerializeField] TMP_Text rivalsCount;
        [Header("A rival's card")]
        [SerializeField] GameObject detail;
        [SerializeField] UnityEngine.UI.Button detailClose;
        [SerializeField] UnityEngine.UI.Image detailPainting;
        [SerializeField] TMP_Text detailName, detailTitle, detailBio, detailHome, detailGadget, detailStatus;
        [SerializeField] UnityEngine.UI.Image detailGadgetIcon;

        const float Rise = 240f, FocusScale = 0.92f; // the car with one slot open: a little higher and smaller (the tray below)
        static readonly string[] SlotNames = { "PAINT", "BODY", "TOP", "DECAL", "TRAIL", "GADGET" };
        // Where each hotspot sits round the car, and the point of the car its line goes to (car units, car facing up).
        static readonly Vector2[] Spots = { new(-400f, 40f), new(400f, 40f), new(380f, 400f), new(-380f, 400f), new(-330f, -440f), new(330f, -440f) };
        static readonly Vector2[] Anchors = { new(-140f, -40f), new(140f, -40f), new(40f, 110f), new(0f, 270f), new(0f, -330f), new(105f, -265f) };

        // The rivals still to come: their home races (names and faces stay a secret).
        static readonly string[] MysteryHomes = { "VALLEY SPRINT", "RIVER JUMP", "LAGOON LEAP", "SNOWBALL VILLAGE", "FROSTBITE PARK", "THE ABYSS" };

        class Hotspot
        {
            public RectTransform Root, Button, Line, Dot;
            public TMP_Text Name, Value;
        }

        readonly List<Hotspot> _hotspots = new();
        readonly List<RectTransform> _tiles = new(), _cards = new();
        readonly List<Garage.Part> _shown = new(); // the part on each tile (null: a mystery slot)
        readonly List<(RectTransform rt, UnityEngine.UI.Image image, Vector2 velocity, float spin, float born)> _sparks = new();
        int _focus = -1;               // the slot being changed (-1: all the hotspots)
        float _openedAt = -10f, _closingAt = -1f, _focusAt = -10f, _unfocusAt = -10f, _changedAt = -10f, _shakeAt = -10f, _messageUntil;
        int _shake = -1;
        Garage.Part _picked;
        RivalCharacter[] _rivals;
        Vector2 _carHome;

        void Awake()
        {
            closeButton.onClick.AddListener(Close);
            rivalsButton.onClick.AddListener(OpenRivals);
            rivalsClose.onClick.AddListener(() => PopIn.Hide(rivalsPanel));
            doneButton.onClick.AddListener(Unfocus);
            carButton.onClick.AddListener(() => { if (_focus >= 0) Unfocus(); });
            if (detailClose != null) detailClose.onClick.AddListener(() => PopIn.Hide(detail));
            if (detail != null && detail.transform.Find("Dim")?.GetComponent<UnityEngine.UI.Button>() is { } dim) dim.onClick.AddListener(() => PopIn.Hide(detail));
            if (rivalsPanel.transform.Find("Dim")?.GetComponent<UnityEngine.UI.Button>() is { } rdim) rdim.onClick.AddListener(() => PopIn.Hide(rivalsPanel));
            _carHome = carRoot.anchoredPosition;
            hotspotTemplate.gameObject.SetActive(false);
            for (int i = 0; i < SlotNames.Length; i++)
            {
                var root = Instantiate(hotspotTemplate, hotspotTemplate.parent);
                root.gameObject.SetActive(true);
                root.name = SlotNames[i];
                var spot = new Hotspot
                {
                    Root = root, Button = (RectTransform)root.Find("Button"), Line = (RectTransform)root.Find("Line"), Dot = (RectTransform)root.Find("Dot"),
                    Name = root.Find("Button/Label/Name").GetComponent<TMP_Text>(), Value = root.Find("Button/Label/Value").GetComponent<TMP_Text>(),
                };
                spot.Name.text = SlotNames[i];
                if (slotIcons != null && i < slotIcons.Length) root.Find("Button/Icon").GetComponent<UnityEngine.UI.Image>().sprite = slotIcons[i];
                int slot = i;
                spot.Button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Focus(slot));
                _hotspots.Add(spot);
            }
            tileTemplate.gameObject.SetActive(false);
            dividerTemplate.gameObject.SetActive(false);
            cardTemplate.gameObject.SetActive(false);
            if (detail != null) detail.SetActive(false);
            rivalsPanel.SetActive(false);
            panel.SetActive(false);
        }

        public void Open()
        {
            _rivals = RivalCharacter.All;
            Garage.MarkSeen();
            _openedAt = Time.unscaledTime;
            _closingAt = -1f;
            _focus = -1;
            _unfocusAt = -10f;
            panel.SetActive(true);
            tray.gameObject.SetActive(false);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.5f);
            Refresh();
        }

        public void Close()
        {
            if (_closingAt < 0f) _closingAt = Time.unscaledTime;
        }

        // ---------------------------------------------------------------- one slot

        void Focus(int slot)
        {
            if (_focus == slot) return;
            _focus = slot;
            _focusAt = Time.unscaledTime;
            _picked = null;
            _messageUntil = 0f;
            tray.gameObject.SetActive(true);
            BuildTray();
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.45f);
            Refresh();
        }

        void Unfocus()
        {
            if (_focus < 0) return;
            _focus = -1;
            _unfocusAt = Time.unscaledTime;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
            Refresh();
        }

        // The row of cards for the slot: a divider with the cup's name and stars, then its things; scrolled so the one
        // you have on is in view.
        void BuildTray()
        {
            foreach (Transform child in content) if (child != tileTemplate && child != dividerTemplate) Destroy(child.gameObject);
            _tiles.Clear(); _shown.Clear();
            var slot = (Garage.Slot)_focus;
            var equipped = Garage.Equipped(slot);
            int equippedAt = 0;
            foreach (var cup in CupList.Instance.cups)
            {
                if (cup == null || !cup.HasRaces) continue;
                var parts = new List<Garage.Part>();
                foreach (var part in Garage.Of(slot)) if (part.Cup == cup.name) parts.Add(part);
                if (slot == Garage.Slot.Gadget && !parts.Exists(p => p.How != Garage.Unlock.Free)) parts.Add(null); // a rival's gadget to come
                if (parts.Count == 0) continue;
                var divider = Instantiate(dividerTemplate, content);
                divider.gameObject.SetActive(true);
                divider.Find("Name").GetComponent<TMP_Text>().text = cup.displayName;
                divider.Find("Stars/Count").GetComponent<TMP_Text>().text = $"{Garage.CupStars(cup.name)}/{Garage.CupMaxStars(cup.name)}";
                foreach (var part in parts)
                {
                    var tile = Instantiate(tileTemplate, content);
                    tile.gameObject.SetActive(true);
                    int index = _tiles.Count;
                    tile.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Pick(index));
                    if (part == equipped) equippedAt = index;
                    _tiles.Add(tile);
                    _shown.Add(part);
                }
            }
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            // The one you have on, near the middle.
            float x = _tiles.Count > 0 ? -_tiles[equippedAt].anchoredPosition.x + scroll.viewport.rect.width * 0.5f : 0f;
            float max = Mathf.Max(0f, content.rect.width - scroll.viewport.rect.width);
            content.anchoredPosition = new Vector2(Mathf.Clamp(x, -max, 0f), 0f);
            scroll.velocity = Vector2.zero;
        }

        void Refresh()
        {
            starsLabel.text = ProgressStore.TotalStars.ToString();
            var look = CarLook.Player;
            car.Dress(look);
            buildName.text = $"{look.Body.Name}  ·  {Garage.Equipped(Garage.Slot.Paint).Name}";
            for (int i = 0; i < _hotspots.Count; i++) _hotspots[i].Value.text = Garage.Equipped((Garage.Slot)i).Name;
            if (_focus < 0) return;
            var slot = (Garage.Slot)_focus;
            int open = 0, all = 0;
            foreach (var part in _shown) { all++; if (part != null && Garage.IsUnlocked(part)) open++; }
            trayTitle.text = SlotNames[_focus];
            trayCount.text = $"{open} / {all} OPEN";
            var equipped = Garage.Equipped(slot);
            for (int i = 0; i < _tiles.Count; i++) FillTile(_tiles[i], _shown[i], equipped, look);
            if (Time.unscaledTime >= _messageUntil)
            {
                var focus = _picked ?? equipped;
                itemName.text = focus.Name;
                itemName.color = Theme.Ink;
                itemBlurb.text = focus.Blurb;
            }
        }

        void FillTile(RectTransform tile, Garage.Part part, Garage.Part equipped, CarLook look)
        {
            bool mystery = part == null;
            bool open = !mystery && Garage.IsUnlocked(part);
            var preview = tile.Find("Preview");
            var swatch = preview.Find("Swatch").gameObject;
            var mini = preview.Find("Car").GetComponent<VehicleIcon>();
            var iconRoot = preview.Find("Icon").gameObject;
            var icon = preview.Find("Icon/Pic").GetComponent<UnityEngine.UI.Image>();
            var question = preview.Find("Question").gameObject;
            swatch.SetActive(false);
            mini.gameObject.SetActive(false);
            iconRoot.SetActive(false);
            question.SetActive(mystery);
            tile.Find("Name").GetComponent<TMP_Text>().text = mystery ? "???" : part.Name;
            if (!mystery)
                switch (part.Slot)
                {
                    case Garage.Slot.Paint:
                        swatch.SetActive(true);
                        swatch.transform.Find("Color").GetComponent<UnityEngine.UI.Image>().color = part.Color;
                        break;
                    case Garage.Slot.Body:
                    case Garage.Slot.Top:
                    case Garage.Slot.Decal:
                        mini.gameObject.SetActive(true);
                        var dressed = look;
                        if (part.Slot == Garage.Slot.Body) dressed.Body = part;
                        if (part.Slot == Garage.Slot.Top) dressed.Top = part;
                        if (part.Slot == Garage.Slot.Decal) dressed.Decal = part;
                        mini.Dress(dressed);
                        break;
                    default:
                        iconRoot.SetActive(true);
                        var art = GarageArt.Instance?.Get(part);
                        icon.sprite = art?.full;
                        icon.enabled = icon.sprite != null;
                        icon.color = part.Slot == Garage.Slot.Trail ? TrailTint(part, look.Paint) : Color.white;
                        break;
                }
            if (!mystery && (part.Id == "none" || part.Id == "standard") && (part.Slot == Garage.Slot.Trail || part.Slot == Garage.Slot.Gadget))
            {
                iconRoot.SetActive(false);
                question.SetActive(false);
            }
            tile.Find("Veil").gameObject.SetActive(!open);
            tile.Find("Lock").gameObject.SetActive(!open && !mystery);
            tile.Find("Equipped").gameObject.SetActive(!mystery && part == equipped);
            tile.Find("Check").gameObject.SetActive(!mystery && part == equipped);

            // What it takes.
            var need = tile.Find("Need");
            need.gameObject.SetActive(!open);
            if (open) return;
            var needStar = need.Find("Star").gameObject;
            var needFace = need.Find("Face").GetComponent<UnityEngine.UI.Image>();
            var needText = need.Find("Text").GetComponent<TMP_Text>();
            needStar.SetActive(false);
            needFace.gameObject.SetActive(false);
            if (mystery) { needText.text = "BEAT A RIVAL"; return; }
            switch (part.How)
            {
                case Garage.Unlock.Stars:
                    needStar.SetActive(true);
                    needText.text = part.Stars.ToString();
                    break;
                case Garage.Unlock.Rival:
                    var rival = Rival(part.From);
                    needFace.gameObject.SetActive(rival != null && rival.portrait != null);
                    if (rival != null) needFace.sprite = rival.portrait;
                    needText.text = "BEAT " + (rival != null ? FirstName(rival) : "A RIVAL");
                    break;
                case Garage.Unlock.CupGold:
                    needStar.SetActive(true);
                    needText.text = "CUP GOLD";
                    break;
                case Garage.Unlock.Challenge:
                    needText.text = "CHALLENGE";
                    break;
            }
        }

        static Color TrailTint(Garage.Part trail, Color paint) => trail.Id switch
        {
            "flame" => new Color(1f, 0.6f, 0.2f),
            "flake" => new Color(0.8f, 0.93f, 1f),
            "confetti" => new Color(1f, 0.45f, 0.6f),
            "aurora" => new Color(0.55f, 1f, 0.8f),
            _ => Color.Lerp(paint, Color.white, 0.4f),
        };

        RivalCharacter Rival(string id)
        {
            if (_rivals == null) return null;
            foreach (var r in _rivals) if (r.name == id) return r;
            return null;
        }

        static string FirstName(RivalCharacter rival) => rival.displayName.Split(' ')[0].ToUpperInvariant();

        static string Requirement(Garage.Part part, RivalCharacter rival) => part.How switch
        {
            Garage.Unlock.Stars => $"WIN {part.Stars} STARS IN THE {Garage.CupNamed(part.Cup)?.displayName}",
            Garage.Unlock.Rival => rival != null ? $"BEAT {rival.displayName.ToUpperInvariant()} IN {rival.homeRace?.displayName.ToUpperInvariant()}" : "BEAT A RIVAL",
            Garage.Unlock.CupGold => $"3 STARS IN EVERY RACE OF THE {Garage.CupNamed(part.From)?.displayName}",
            Garage.Unlock.Challenge => "WIN THE CUP'S CHALLENGE",
            _ => "",
        };

        void Pick(int index)
        {
            var part = index >= 0 && index < _shown.Count ? _shown[index] : null;
            if (part == null || !Garage.IsUnlocked(part))
            {
                // Not yet: it shakes, and says what it takes.
                _shake = index;
                _shakeAt = Time.unscaledTime;
                itemName.text = part != null ? part.Name : "A RIVAL'S GADGET";
                itemName.color = Theme.Rust;
                itemBlurb.text = part != null ? Requirement(part, Rival(part.From)) : "Beat this cup's rival in their home race to win their gadget.";
                _messageUntil = Time.unscaledTime + 2.2f;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
                return;
            }
            _picked = part;
            _messageUntil = 0f;
            if (Garage.Equipped(part.Slot) != part)
            {
                Garage.Equip(part);
                _changedAt = Time.unscaledTime;
                Sparks();
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.6f);
            }
            Refresh();
        }

        // Sparks fly off the part that changed.
        void Sparks()
        {
            if (sparkle == null || _focus < 0) return;
            Vector2 from = carRoot.anchoredPosition + Anchors[_focus] * carRoot.localScale.x;
            for (int i = 0; i < 14; i++)
            {
                var go = new GameObject("Spark", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(carRoot.parent, false);
                rt.sizeDelta = Vector2.one * Random.Range(34f, 70f);
                rt.anchoredPosition = from;
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = sparkle;
                image.raycastTarget = false;
                image.color = i % 3 == 0 ? Color.white : Theme.Amber;
                float a = (i / 14f + Random.Range(-0.03f, 0.03f)) * Mathf.PI * 2f;
                _sparks.Add((rt, image, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(320f, 560f), Random.Range(-400f, 400f), Time.unscaledTime));
            }
        }

        // ---------------------------------------------------------------- the rivals' cards

        void OpenRivals()
        {
            foreach (var card in _cards) Destroy(card.gameObject);
            _cards.Clear();
            int met = 0, count = (_rivals?.Length ?? 0) + MysteryHomes.Length;
            for (int i = 0; i < count; i++)
            {
                var card = Instantiate(cardTemplate, rivalsGrid);
                card.gameObject.SetActive(true);
                int index = i;
                card.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OpenCard(index));
                var rival = i < (_rivals?.Length ?? 0) ? _rivals[i] : null;
                bool known = rival != null && (RivalProgress.HasMet(rival.name) || CupList.DevUnlockAll);
                if (known) met++;
                var picture = card.Find("Picture").GetComponent<UnityEngine.UI.Image>();
                picture.sprite = rival != null ? rival.portrait : null;
                picture.enabled = rival != null && rival.portrait != null;
                picture.color = known ? Color.white : new Color(0.05f, 0.08f, 0.1f, 0.9f);
                card.Find("Question").gameObject.SetActive(!known);
                card.Find("Name").GetComponent<TMP_Text>().text = known ? rival.displayName.ToUpperInvariant() : "???";
                card.Find("Home").GetComponent<TMP_Text>().text = rival != null ? rival.homeRace?.displayName.ToUpperInvariant() : MysteryHomes[Mathf.Clamp(i - (_rivals?.Length ?? 0), 0, MysteryHomes.Length - 1)];
                card.Find("Beaten").gameObject.SetActive(rival != null && RivalProgress.IsBeaten(rival.name));
                _cards.Add(card);
            }
            rivalsCount.text = $"{met} / {count} MET";
            PopIn.Show(rivalsPanel);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.5f);
        }

        void OpenCard(int index)
        {
            var rival = _rivals != null && index < _rivals.Length ? _rivals[index] : null;
            if (rival == null || !(RivalProgress.HasMet(rival.name) || CupList.DevUnlockAll))
            {
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
                return;
            }
            detailPainting.sprite = rival.fullBody != null ? rival.fullBody : rival.painting != null ? rival.painting : rival.portrait;
            detailName.text = rival.displayName.ToUpperInvariant();
            detailTitle.text = rival.title;
            detailBio.text = rival.bio;
            detailHome.text = rival.homeRace != null ? rival.homeRace.displayName.ToUpperInvariant() : "";
            var gadget = Garage.Find(rival.gadget, Garage.Slot.Gadget);
            bool beaten = RivalProgress.IsBeaten(rival.name);
            detailGadget.text = gadget != null ? gadget.Name : "";
            detailGadgetIcon.sprite = GarageArt.Instance?.Get(gadget)?.full;
            detailStatus.text = beaten ? "BEATEN! THE GADGET IS YOURS" : "BEAT " + FirstName(rival) + " TO WIN IT";
            detailStatus.color = beaten ? Theme.Leaf : Theme.Rust;
            PopIn.Show(detail);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.5f);
        }

        // ---------------------------------------------------------------- motion

        void Update()
        {
            if (!panel.activeSelf) return;
            float t = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float open = Mathf.Clamp01((t - _openedAt) / 0.3f);
            float shut = _closingAt >= 0f ? Mathf.Clamp01((t - _closingAt) / 0.25f) : 0f;
            group.alpha = open * (1f - shut);
            sheet.anchoredPosition = new Vector2(0f, -120f * shut * shut);
            if (shut >= 1f) { panel.SetActive(false); return; }
            if (_messageUntil > 0f && t >= _messageUntil) { _messageUntil = 0f; Refresh(); }

            // In focus (0..1): the car rises and shrinks a little, the tray slides up; back out the other way.
            float focus = _focus >= 0 ? EaseOut((t - _focusAt) / 0.45f) : _unfocusAt > _openedAt ? 1f - EaseOut((t - _unfocusAt) / 0.4f) : 0f;

            // The car drops onto the podium with a bounce, then floats; a change pops it.
            float drop = Mathf.Clamp01((t - _openedAt - 0.05f) / 0.6f);
            float fall = 1f - Bounce(drop);
            float pop = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01((t - _changedAt) / 0.35f) * Mathf.PI);
            carRoot.anchoredPosition = _carHome + new Vector2(0f, 700f * fall + Rise * focus + Mathf.Sin(t * 1.4f) * 8f);
            carRoot.localScale = Vector3.one * Mathf.Lerp(1f, FocusScale, focus) * pop;
            podium.anchoredPosition = _carHome + new Vector2(0f, Rise * focus);
            podium.localScale = Vector3.one * Mathf.Lerp(1f, FocusScale, focus) * Mathf.Lerp(0.6f, 1f, EaseOut(drop));
            podiumRing.localRotation = Quaternion.Euler(0f, 0f, t * 12f);
            float ring = Mathf.Clamp01((t - _changedAt) / 0.55f);
            burst.gameObject.SetActive(ring < 1f);
            burst.rectTransform.anchoredPosition = carRoot.anchoredPosition;
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.9f, 1f - (1f - ring) * (1f - ring));
            burst.color = Theme.WithAlpha(Theme.Amber, 0.85f * (1f - ring));

            // The hotspots: drawn out one after another after the drop; flying off when one is chosen.
            for (int i = 0; i < _hotspots.Count; i++)
            {
                var spot = _hotspots[i];
                float appear = Mathf.Clamp01((t - _openedAt - 0.6f - i * 0.08f) / 0.35f);
                float stay = 1f - focus;
                Vector2 anchor = carRoot.anchoredPosition + Anchors[i] * carRoot.localScale.x;
                Vector2 home = _carHome + Spots[i];
                Vector2 at = home + (home - _carHome).normalized * 520f * focus;
                spot.Button.anchoredPosition = at;
                spot.Button.localScale = Vector3.one * EaseOutBack(appear) * stay * (1f + 0.04f * Mathf.Sin(t * 3f + i));
                spot.Dot.anchoredPosition = anchor;
                spot.Dot.localScale = Vector3.one * appear * stay * (1f + 0.25f * Mathf.Abs(Mathf.Sin(t * 2.5f + i)));
                Vector2 d = anchor - at;
                float length = Mathf.Max(0f, d.magnitude - 70f) * Mathf.Clamp01(appear * 1.4f) * stay;
                spot.Line.anchoredPosition = at + d.normalized * 70f;
                spot.Line.sizeDelta = new Vector2(length, spot.Line.sizeDelta.y);
                spot.Line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                bool active = stay > 0.01f;
                if (spot.Root.gameObject.activeSelf != active) spot.Root.gameObject.SetActive(active);
            }
            hint.alpha = Mathf.Clamp01((t - _openedAt - 1.1f) / 0.4f) * (1f - focus);
            buildName.alpha = hint.alpha;

            // The part being changed pulses.
            highlight.gameObject.SetActive(_focus >= 0);
            if (_focus >= 0)
            {
                highlight.rectTransform.anchoredPosition = carRoot.anchoredPosition + Anchors[_focus] * carRoot.localScale.x;
                highlight.rectTransform.localScale = Vector3.one * (0.9f + 0.15f * Mathf.Sin(t * 4f)) * focus;
            }

            // The tray: slides up with the cards popping in one after another.
            tray.anchoredPosition = new Vector2(0f, Mathf.Lerp(-900f, 0f, focus));
            if (focus <= 0.001f && _focus < 0 && tray.gameObject.activeSelf) tray.gameObject.SetActive(false);
            var equipped = _focus >= 0 ? Garage.Equipped((Garage.Slot)_focus) : null;
            for (int i = 0; i < _tiles.Count; i++)
            {
                float appear = Mathf.Clamp01((t - _focusAt - 0.15f - i * 0.04f) / 0.3f);
                float s = i == _shake ? Mathf.Clamp01((t - _shakeAt) / 0.4f) : 1f;
                bool on = _shown[i] != null && _shown[i] == equipped;
                _tiles[i].localScale = Vector3.one * EaseOutBack(appear) * (on ? 1.06f : 1f);
                _tiles[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(s * Mathf.PI * 6f) * 6f * (1f - s));
            }

            // The sparks fly out and fade.
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var (rt, image, velocity, spin, born) = _sparks[i];
                float age = (t - born) / 0.7f;
                if (age >= 1f || rt == null) { if (rt != null) Destroy(rt.gameObject); _sparks.RemoveAt(i); continue; }
                rt.anchoredPosition += velocity * (1f - age) * dt;
                rt.localRotation = Quaternion.Euler(0f, 0f, spin * age);
                var c = image.color; c.a = 1f - age; image.color = c;
            }
        }

        static float EaseOut(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        static float Bounce(float x)
        {
            x = Mathf.Clamp01(x);
            const float n = 7.5625f, d = 2.75f;
            if (x < 1f / d) return n * x * x;
            if (x < 2f / d) { x -= 1.5f / d; return n * x * x + 0.75f; }
            if (x < 2.5f / d) { x -= 2.25f / d; return n * x * x + 0.9375f; }
            x -= 2.625f / d;
            return n * x * x + 0.984375f;
        }
    }
}
