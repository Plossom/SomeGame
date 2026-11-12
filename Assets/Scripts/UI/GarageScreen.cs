using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Vehicle = SomeGame.Track.TrackLayout.Vehicle;

namespace SomeGame.UI
{
    /// <summary>
    /// The garage, a showroom over the map: your car big in the middle on a lit podium, with a glowing hotspot for
    /// each kind of thing joined to its part of the car by a line (PAINT on its flank, BODY, TOP on the roof, DECAL
    /// on the bonnet, TRAIL behind it, GADGET), each saying what you have on. The car drops onto the podium, the
    /// lines draw out and the hotspots pop in one after another.
    /// Tap one: the others fly away, the car rises, its part pulses, and a strip of round chips slides up with
    /// everything for that slot, cup by cup (also the locked ones, with what they take: stars of the cup, a rival to
    /// beat, the cup's gold or its challenge). Tap a chip and the car wears it at once with a pop, a ring of light
    /// and sparks; a locked one is tried on, shakes and says what it takes. The tabs over the strip jump to another
    /// part; DONE (or a tap on the car) goes back to all the hotspots. Each vehicle has its own page (swipe up or
    /// down, the garage's colours morphing to its cup's); the rivals' cards are behind their own button.
    /// </summary>
    public class GarageScreen : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform sheet;
        [SerializeField] UnityEngine.UI.Button closeButton, rivalsButton;
        [SerializeField] TMP_Text starsLabel;
        [Header("The vehicles: a page each")]
        [Tooltip("The page's name under GARAGE (the cup and the vehicle) and a dot per page.")]
        [SerializeField] TMP_Text pageTitle;
        [SerializeField] UnityEngine.UI.Image[] pageDots;
        [Tooltip("The other vehicles peeking in above and below: tap (or swipe up or down) to go to their page.")]
        [SerializeField] RectTransform peekLeft, peekRight;
        [Tooltip("The vehicle leaving on a page change (it slides away while the new one comes in).")]
        [SerializeField] RectTransform outgoing;
        [SerializeField] VehicleIcon outgoingCar;
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
        [Header("The strip of chips")]
        [SerializeField] RectTransform tray;
        [Tooltip("A row of round chips (a divider per cup): tap one and the car wears it at once.")]
        [SerializeField] UnityEngine.UI.ScrollRect strip;
        [SerializeField] RectTransform stripContent, tileTemplate, dividerTemplate;
        [SerializeField] TMP_Text trayCount, itemName, itemBlurb;
        [Tooltip("The row of part tabs at the top of the tray: jump straight to another part.")]
        [SerializeField] RectTransform tabTemplate;
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

        const float Rise = 130f, FocusScale = 0.8f, HotScale = 0.8f; // the car over the chips, its hotspots round it // the car with one slot open: a little higher and smaller (the tray below)
        static readonly string[] SlotNames = { "PAINT", "BODY", "TOP", "DECAL", "TRAIL", "GADGET" };
        // Where each hotspot sits round the car, and the point of the car its line goes to (car units, car facing up).
        static readonly Vector2[] Spots = { new(-385f, 30f), new(385f, 30f), new(370f, 440f), new(-370f, 440f), new(-330f, -480f), new(330f, -480f) };
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
        float _openedAt = -10f, _closingAt = -1f, _focusAt = -10f, _unfocusAt = -10f, _changedAt = -10f, _shakeAt = -10f, _tryAt = -10f, _messageUntil;
        int _browsing;
        float _switchAt = -10f;
        Vector2 _highlightAt;
        readonly List<RectTransform> _tabs = new();
        int _shake = -1;
        Garage.Part _picked;
        RivalCharacter[] _rivals;
        Vector2 _carHome;
        Vehicle _vehicle = Vehicle.Car;
        float _pageAt = -10f, _pageDir, _dragY;
        // The garage takes the colours of the vehicle's cup (Valley green, Winter blue), morphing between them.
        Theme.Palette _palette, _mapPalette;
        List<(UnityEngine.UI.Graphic graphic, Color from, Color to)> _morph;
        float _morphAt = -10f;
        const float MorphSeconds = 0.55f;
        bool _paging;
        static string[] VehicleNames => new[] { "CAR", "SNOWMOBILE" };

        void Awake()
        {
            closeButton.onClick.AddListener(Close);
            rivalsButton.onClick.AddListener(OpenRivals);
            rivalsClose.onClick.AddListener(() => PopIn.Hide(rivalsPanel));
            doneButton.onClick.AddListener(Unfocus);

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
            outgoing.gameObject.SetActive(false);
            tabTemplate.gameObject.SetActive(false);
            tabTemplate.parent.gameObject.SetActive(false); // the hotspots round the car choose the part
            for (int i = 0; i < SlotNames.Length; i++)
            {
                var tab = Instantiate(tabTemplate, tabTemplate.parent);
                tab.gameObject.SetActive(true);
                tab.name = SlotNames[i];
                tab.Find("Label").GetComponent<TMP_Text>().text = SlotNames[i];
                if (slotIcons != null && i < slotIcons.Length) tab.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite = slotIcons[i];
                int slot = i;
                tab.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Focus(slot));
                _tabs.Add(tab);
            }
            peekLeft.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Page(-1));
            peekRight.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Page(1));
            cardTemplate.gameObject.SetActive(false);
            if (detail != null) detail.SetActive(false);
            rivalsPanel.SetActive(false);
            panel.SetActive(false);
        }

        /// <summary>Opens the garage on a vehicle's page (the vehicle of the cup on show).</summary>
        public void Open(Vehicle vehicle = Vehicle.Car)
        {
            _rivals = RivalCharacter.All;
            Garage.MarkSeen();
            _vehicle = vehicle;
            _openedAt = Time.unscaledTime;
            _pageAt = -10f;
            _closingAt = -1f;
            _focus = -1;
            _unfocusAt = -10f;
            panel.SetActive(true);
            tray.gameObject.SetActive(false);
            outgoing.gameObject.SetActive(false);
            // In the map's colours now (it recoloured the whole screen); the vehicle's cup's colours from here.
            _mapPalette = _palette = Theme.Current;
            _morph = null;
            Morph(PaletteOf(_vehicle), instant: true);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.5f);
            // Straight into choosing: the part tabs on top, the chips below, on the paint.
            Focus((int)Garage.Slot.Paint);
            _focusAt = _switchAt = _openedAt;
        }

        static Theme.Palette PaletteOf(Vehicle vehicle) => vehicle == Vehicle.Snowmobile ? Theme.Winter : Theme.Valley;

        // Morphs the garage's colours to a palette (or jumps there). A morph still running is finished first.
        void Morph(Theme.Palette to, bool instant = false)
        {
            if (_morph != null) { foreach (var (g, _, end) in _morph) if (g != null) g.color = end; _morph = null; }
            if (to == _palette) return;
            var plan = Theme.RecolorPlan(transform, _palette, to);
            // Not what shows your paint or a part's own colours (the cars, the paint swatches, the trail icons).
            plan.RemoveAll(p => p.graphic.name is "Body" or "Color" or "Pic");
            // Nor the hotspots: which one is chosen sets their colours (straight in the new cup's).
            plan.RemoveAll(p => _hotspots.Exists(h => p.graphic.transform == h.Button.Find("Face") || p.graphic.transform == h.Button.Find("Label") || p.graphic == h.Name));
            _palette = to;
            Theme.Use(to); // what the garage colours now from code (tabs, dots, chips...) is in these colours too
            if (instant) { foreach (var (g, _, end) in plan) g.color = end; return; }
            _morph = plan;
            _morphAt = Time.unscaledTime;
        }

        // ---------------------------------------------------------------- the vehicles' pages

        int PageIndex => System.Array.IndexOf(Garage.Vehicles, _vehicle);

        /// <summary>To the next vehicle's page (+1, the one below) or the one before (-1, above).</summary>
        void Page(int step)
        {
            int to = PageIndex + step;
            if (to < 0 || to >= Garage.Vehicles.Length) return;
            // The vehicle leaving, as it is, slides away; the new one comes in from the other side.
            outgoingCar.Show(_vehicle == Vehicle.Snowmobile);
            outgoingCar.Dress(CarLook.For(_vehicle));
            outgoing.gameObject.SetActive(true);
            _vehicle = Garage.Vehicles[to];
            Morph(PaletteOf(_vehicle));
            _pageAt = Time.unscaledTime;
            _pageDir = step;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.45f);
            // The same part on the new vehicle if it has it (else its paint); its chips pop in afresh.
            if (_focus < 0 || !Fits(_focus)) _focus = (int)Garage.Slot.Paint;
            _switchAt = Time.unscaledTime;
            BuildTray();
            Refresh();
        }

        // Swipe up for the next vehicle (it comes up from below), down for the one before.
        public void OnBeginDrag(PointerEventData e)
        {
            _paging = true; // (the chip strip scrolls on its own: swipes on it don't page)
            _dragY = 0f;
        }

        public void OnDrag(PointerEventData e)
        {
            if (_paging) _dragY += e.delta.y / Mathf.Max(0.01f, GetComponentInParent<Canvas>().rootCanvas.scaleFactor);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_paging) return;
            _paging = false;
            if (Mathf.Abs(_dragY) > 140f) Page(_dragY > 0f ? 1 : -1);
            _dragY = 0f;
        }

        bool Fits(int slot) => Garage.Fits((Garage.Slot)slot, _vehicle);

        public void Close()
        {
            if (_closingAt < 0f) _closingAt = Time.unscaledTime;
        }

        // Closed: back in the map's colours (the screen around it is in them).
        void Closed()
        {
            Morph(_mapPalette, instant: true);
            panel.SetActive(false);
        }

        // ---------------------------------------------------------------- one slot

        void Focus(int slot)
        {
            if (_focus == slot || !Fits(slot)) return;
            // Already in a part: jump across (the tray stays up, the cards swap, the highlight glides over).
            if (_focus >= 0) _switchAt = Time.unscaledTime;
            else _focusAt = _switchAt = Time.unscaledTime;
            _focus = slot;
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

        // The chips for the slot, cup by cup (a divider with the cup's name and stars before its own), scrolled so
        // the one you have on is in view.
        void BuildTray()
        {
            foreach (Transform child in stripContent) if (child != tileTemplate && child != dividerTemplate) Destroy(child.gameObject);
            _tiles.Clear(); _shown.Clear();
            var slot = (Garage.Slot)_focus;
            var equipped = Garage.Equipped(slot, _vehicle);
            int equippedAt = 0;
            foreach (var cup in CupList.Instance.cups)
            {
                if (cup == null || !cup.HasRaces) continue;
                var parts = new List<Garage.Part>();
                foreach (var part in Garage.Of(slot)) if (part.Cup == cup.name) parts.Add(part);
                if (slot == Garage.Slot.Gadget && !parts.Exists(p => p.How != Garage.Unlock.Free)) parts.Add(null); // a rival's gadget to come
                if (parts.Count == 0) continue;
                var divider = Instantiate(dividerTemplate, stripContent);
                divider.gameObject.SetActive(true);
                divider.Find("Name").GetComponent<TMP_Text>().text = cup.displayName;
                divider.Find("Stars/Count").GetComponent<TMP_Text>().text = $"{Garage.CupStars(cup.name)}/{Garage.CupMaxStars(cup.name)}";
                foreach (var part in parts)
                {
                    var tile = Instantiate(tileTemplate, stripContent);
                    tile.gameObject.SetActive(true);
                    int index = _tiles.Count;
                    tile.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Pick(index));
                    if (part == equipped) equippedAt = index;
                    _tiles.Add(tile);
                    _shown.Add(part);
                }
            }
            _browsing = equippedAt;
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(stripContent);
            if (_tiles.Count > 0)
            {
                float x = -_tiles[equippedAt].anchoredPosition.x + strip.viewport.rect.width * 0.5f;
                float max = Mathf.Max(0f, stripContent.rect.width - strip.viewport.rect.width);
                stripContent.anchoredPosition = new Vector2(Mathf.Clamp(x, -max, 0f), 0f);
            }
            strip.velocity = Vector2.zero;
            Browse(equippedAt);
        }

        // The chip tapped last: the car wears it (or tries it on, if it is still locked), its name and what it is
        // (or what it takes) show.
        void Browse(int index)
        {
            if (_focus < 0 || index < 0 || index >= _shown.Count) return;
            _browsing = index;
            _messageUntil = 0f;
            var part = _shown[index];
            var look = CarLook.For(_vehicle);
            if (part != null)
            {
                if (part.Slot == Garage.Slot.Paint) look.Paint = part.Color;
                if (part.Slot == Garage.Slot.Body) look.Body = part;
                if (part.Slot == Garage.Slot.Top) look.Top = part;
                if (part.Slot == Garage.Slot.Decal) look.Decal = part;
                if (part != Garage.Equipped(part.Slot, _vehicle)) _tryAt = Time.unscaledTime;
            }
            car.Dress(look);
            ShowAction();
        }

        void ShowAction()
        {
            var part = _browsing >= 0 && _browsing < _shown.Count ? _shown[_browsing] : null;
            bool open = part != null && Garage.IsUnlocked(part);
            bool on = part != null && part == Garage.Equipped(part.Slot, _vehicle);
            if (Time.unscaledTime >= _messageUntil)
            {
                itemName.text = part != null ? part.Name : "A RIVAL'S GADGET";
                itemName.color = Theme.Ink;
                itemBlurb.text = part == null ? "Beat this cup's rival in their home race to win their gadget."
                    : open ? part.Blurb : Requirement(part, Rival(part.From));
                itemBlurb.color = open ? Theme.InkSoft : Theme.Rust;
            }
            if (Time.unscaledTime >= _messageUntil && part != null && !open) itemName.text = $"{part.Name}  ·  LOCKED";
        }

        static Color Shade(Color c, float k) => new(c.r * k, c.g * k, c.b * k, c.a);

        // What it takes, in a few words (for the button).
        string Short(Garage.Part part) => part.How switch
        {
            Garage.Unlock.Stars => $"{part.Stars} STARS",
            Garage.Unlock.Rival => "BEAT " + (Rival(part.From) is { } r ? FirstName(r) : "THE RIVAL"),
            Garage.Unlock.CupGold => "CUP GOLD",
            Garage.Unlock.Challenge => "WIN THE CHALLENGE",
            _ => "LOCKED",
        };

        void Refresh()
        {
            starsLabel.text = ProgressStore.TotalStars.ToString();
            var look = CarLook.For(_vehicle);
            bool snow = _vehicle == Vehicle.Snowmobile;
            car.Show(snow);
            car.Dress(look);
            buildName.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            for (int i = 0; i < _hotspots.Count; i++)
            {
                _hotspots[i].Value.text = Garage.Equipped((Garage.Slot)i, _vehicle).Name;
                bool on = i == _focus;
                _hotspots[i].Button.Find("Face").GetComponent<UnityEngine.UI.Image>().color = on ? Theme.Orange : Theme.Ink;
                _hotspots[i].Name.color = on ? Color.white : Theme.Amber;
                _hotspots[i].Button.Find("Label").GetComponent<UnityEngine.UI.Image>().color = on ? Theme.OrangeDeep : Theme.WithAlpha(Theme.Ink * 0.6f, 0.95f);
            }
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].gameObject.SetActive(Fits(i));
            // The page: its name, its dot, the other vehicles peeking in at the sides (in their own setup).
            int page = PageIndex;
            pageTitle.text = $"{Garage.CupNamed(Garage.CupOf(_vehicle))?.displayName ?? ""}  ·  {VehicleNames[page]}";
            for (int i = 0; i < pageDots.Length; i++) pageDots[i].color = i == page ? Theme.Amber : Theme.WithAlpha(Theme.Cream, 0.35f);
            Peek(peekLeft, page - 1);  // above
            Peek(peekRight, page + 1); // below
            if (_focus < 0) return;
            var slot = (Garage.Slot)_focus;
            int open = 0, all = 0;
            foreach (var part in _shown) { all++; if (part != null && Garage.IsUnlocked(part)) open++; }
            trayCount.text = $"{open} / {all} OPEN";
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool on = i == _focus;
                _tabs[i].Find("Face").GetComponent<UnityEngine.UI.Image>().color = on ? Theme.Orange : Color.Lerp(Theme.Ink, Theme.Cream, 0.16f);
                _tabs[i].Find("Label").GetComponent<TMP_Text>().color = on ? Theme.Amber : Theme.WithAlpha(Theme.Cream, 0.7f);
            }
            var equipped = Garage.Equipped(slot, _vehicle);
            for (int i = 0; i < _tiles.Count; i++) FillTile(_tiles[i], _shown[i], equipped, look);
            ShowAction();
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
                // Not yet: the car tries it on, the chip shakes, and what it takes shows.
                _shake = index;
                _shakeAt = Time.unscaledTime;
                Browse(index);
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
                return;
            }
            _browsing = index;
            _picked = part;
            _messageUntil = 0f;
            if (Garage.Equipped(part.Slot, _vehicle) != part)
            {
                Garage.Equip(part, _vehicle);
                _changedAt = Time.unscaledTime;
                Sparks();
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.6f);
            }
            Refresh();
            car.Dress(CarLook.For(_vehicle));
            ShowAction();
        }

        void Peek(RectTransform peek, int page)
        {
            bool show = page >= 0 && page < Garage.Vehicles.Length;
            peek.gameObject.SetActive(show);
            if (!show) return;
            var vehicle = Garage.Vehicles[page];
            var icon = peek.Find("Car").GetComponent<VehicleIcon>();
            icon.Show(vehicle == Vehicle.Snowmobile);
            icon.Dress(CarLook.For(vehicle));
            peek.Find("Name").gameObject.SetActive(false); // the page title names the vehicle; the peek just shows it
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
            if (shut >= 1f) { Closed(); return; }
            // The colours morph to the vehicle's cup.
            if (_morph != null)
            {
                float m = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - _morphAt) / MorphSeconds));
                foreach (var (g, from, to) in _morph) if (g != null) g.color = Color.Lerp(from, to, m);
                if (m >= 1f) _morph = null;
            }
            if (_messageUntil > 0f && t >= _messageUntil) { _messageUntil = 0f; Refresh(); }

            // In focus (0..1): the car rises and shrinks a little, the tray slides up; back out the other way.
            // Always choosing now (no overview): the car sits a little higher and smaller over the chips.
            const float focus = 1f;

            // The car drops onto the podium with a bounce, then floats; a change pops it.
            float drop = Mathf.Clamp01((t - _openedAt - 0.05f) / 0.6f);
            float fall = 1f - Bounce(drop);
            float pop = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01((t - _changedAt) / 0.35f) * Mathf.PI)
                           + 0.05f * Mathf.Sin(Mathf.Clamp01((t - _tryAt) / 0.25f) * Mathf.PI);
            // A page change: the old vehicle slides away up (or down) and fades, the new one comes in from the
            // other side; the swipe drags the car along a little meanwhile.
            float page = Mathf.Clamp01((t - _pageAt) / 0.55f);
            float slide = 1f - EaseOut(page);
            float dragOffset = _paging ? Mathf.Clamp(_dragY * 0.5f, -240f, 240f) : 0f;
            carRoot.anchoredPosition = _carHome + new Vector2(0f, -_pageDir * 1150f * slide + dragOffset + 700f * fall + Rise * focus + Mathf.Sin(t * 1.4f) * 8f);
            if (outgoing.gameObject.activeSelf)
            {
                float away = EaseOut(page);
                outgoing.anchoredPosition = _carHome + new Vector2(0f, _pageDir * 1150f * away);
                outgoing.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, away);
                outgoing.GetComponent<CanvasGroup>().alpha = 1f - away;
                if (page >= 1f) outgoing.gameObject.SetActive(false);
            }
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
                float since = Mathf.Min(t - _openedAt - 0.6f, t - _pageAt - 0.3f);
                float appear = Fits(i) ? Mathf.Clamp01((since - i * 0.06f) / 0.35f) : 0f;
                // Round the car (moving with it), the part being chosen orange and a little bigger.
                bool on = i == _focus;
                float pick = on ? 1f + 0.16f * Mathf.Sin(Mathf.Clamp01((t - _switchAt) / 0.3f) * Mathf.PI) : 1f;
                Vector2 anchor = carRoot.anchoredPosition + Anchors[i] * carRoot.localScale.x;
                Vector2 at = carRoot.anchoredPosition + Spots[i] * HotScale;
                spot.Button.anchoredPosition = at;
                spot.Button.localScale = Vector3.one * EaseOutBack(appear) * HotScale * (on ? 1.12f : 1f) * pick;
                spot.Dot.anchoredPosition = anchor;
                spot.Dot.localScale = Vector3.one * appear * (on ? 1.3f : 0.9f) * (1f + 0.25f * Mathf.Abs(Mathf.Sin(t * 2.5f + i)));
                Vector2 d = anchor - at;
                float gap = 70f * HotScale;
                float length = Mathf.Max(0f, d.magnitude - gap) * Mathf.Clamp01(appear * 1.4f);
                spot.Line.anchoredPosition = at + d.normalized * gap;
                spot.Line.sizeDelta = new Vector2(length, on ? 9f : 5f);
                spot.Line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                bool active = Fits(i);
                if (spot.Root.gameObject.activeSelf != active) spot.Root.gameObject.SetActive(active);
            }
            // The other vehicles peek in above and below (not while a part is open); their arrow nods now and then.
            foreach (var (peek, side) in new[] { (peekLeft, 1f), (peekRight, -1f) })
            {
                if (!peek.gameObject.activeSelf) continue;
                float show = Mathf.Clamp01(Mathf.Min(t - _openedAt - 0.9f, t - _pageAt - 0.4f) / 0.4f) * 0.8f;
                peek.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, show);
                peek.GetComponent<CanvasGroup>().alpha = show;
                float nudge = Mathf.Repeat(t, 3.5f) < 0.5f ? Mathf.Sin(Mathf.Repeat(t, 3.5f) / 0.5f * Mathf.PI) * 14f : 0f;
                var arrow = peek.Find("Arrow");
                arrow.localPosition = new Vector3(0f, side * (110f + nudge), 0f);
            }

            // The part being changed pulses.
            highlight.gameObject.SetActive(_focus >= 0);
            if (_focus >= 0)
            {
                Vector2 to = carRoot.anchoredPosition + Anchors[_focus] * carRoot.localScale.x;
                _highlightAt = t - _switchAt < 0.02f && t - _focusAt < 0.02f ? to : Vector2.Lerp(_highlightAt, to, 1f - Mathf.Exp(-14f * dt));
                highlight.rectTransform.anchoredPosition = _highlightAt;
                highlight.rectTransform.localScale = Vector3.one * (0.9f + 0.15f * Mathf.Sin(t * 4f)) * Mathf.Clamp01((t - _openedAt - 0.7f) / 0.3f);
            }

            // The tray: slides up with the cards popping in one after another.
            tray.anchoredPosition = new Vector2(0f, Mathf.Lerp(-700f, 0f, EaseOut((t - _openedAt - 0.25f) / 0.45f)));
            // The chips pop in one after another after the tray (and again on a part switch); the one tapped last
            // stands a little bigger; a locked one shakes.
            for (int i = 0; i < _tabs.Count; i++)
            {
                float tabPop = i == _focus ? 1f + 0.18f * Mathf.Sin(Mathf.Clamp01((t - _switchAt) / 0.3f) * Mathf.PI) : 1f;
                _tabs[i].localScale = Vector3.one * (i == _focus ? 1.08f : 0.94f) * tabPop;
            }
            float shake = Mathf.Clamp01((t - _shakeAt) / 0.4f);
            float wobble = Mathf.Sin(shake * Mathf.PI * 6f) * (1f - shake);
            for (int i = 0; i < _tiles.Count; i++)
            {
                float appear = EaseOutBack(Mathf.Clamp01((t - _switchAt - 0.1f - i * 0.035f) / 0.3f));
                _tiles[i].localScale = Vector3.one * appear * (i == _browsing ? 1.1f + 0.02f * Mathf.Sin(t * 4f) : 1f);
                _tiles[i].localRotation = Quaternion.Euler(0f, 0f, i == _shake ? wobble * 10f : 0f);
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
