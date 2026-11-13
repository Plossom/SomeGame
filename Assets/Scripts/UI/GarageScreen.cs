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
        [Tooltip("The bar of vehicles under GARAGE: a badge each (the template), the ring on the one on show.")]
        [SerializeField] RectTransform vehicleBar, vehicleBadge, vehicleRing;
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

        const float Rise = 130f, FocusScale = 0.8f, HotScale = 0.8f;
        // The other vehicles' peek spots (as wide as the car shown there) and how long a page change takes.
        static readonly Vector2 PeekAbove = new(0f, 605f), PeekBelow = new(0f, -355f);
        const float PeekGap = 480f;          // from the car to a peek spot: a full swipe
        const float PeekCarWidth = 150f;     // the car in a peek (the main car is 420 wide)
        const float PeekScale = PeekCarWidth / 420f;
        float _scrub, _scrubTarget, _dragVelocity;
        int _scrubDir, _scrubDirWas;
        float _landedAt = -10f, _armsSpringUntil = -10f;
        bool _armsToNew;
        readonly Vector2[] _armEnd = new Vector2[6], _armVelocity = new Vector2[6];
        bool _snapping; // past half way: the change goes through quickly by itself
        List<(UnityEngine.UI.Graphic graphic, Color from, Color to)> _scrubPlan;
        readonly Dictionary<RectTransform, Vector2> _peekHome = new(); // the car over the chips, its hotspots round it // the car with one slot open: a little higher and smaller (the tray below)
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
        readonly List<RectTransform> _badges = new();
        const float BadgeSpacing = 154f;
        readonly List<RectTransform> _tiles = new(), _cards = new();
        readonly List<Garage.Part> _shown = new(); // the part on each tile (null: a mystery slot)
        readonly List<(RectTransform rt, UnityEngine.UI.Image image, Vector2 velocity, float spin, float born)> _sparks = new();
        int _focus = -1;               // the slot being changed (-1: all the hotspots)
        float _openedAt = -10f, _closingAt = -1f, _focusAt = -10f, _unfocusAt = -10f, _changedAt = -10f, _shakeAt = -10f, _tryAt = -10f, _messageUntil;
        int _browsing;
        float _switchAt = -10f;
        readonly List<RectTransform> _tabs = new();
        int _shake = -1;
        Garage.Part _picked;
        RivalCharacter[] _rivals;
        Vector2 _carHome;
        Vehicle _vehicle = Vehicle.Car;
        float _pageAt = -10f, _dragY;
        // The garage takes the colours of the vehicle's cup (Valley green, Winter blue), morphing between them.
        Theme.Palette _palette, _mapPalette;
        List<(UnityEngine.UI.Graphic graphic, Color from, Color to)> _morph;
        float _morphAt = -10f;
        const float MorphSeconds = 0.55f;
        bool _paging;

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
            vehicleBadge.gameObject.SetActive(false);
            for (int i = 0; i < Garage.Vehicles.Length; i++)
            {
                var badge = Instantiate(vehicleBadge, vehicleBadge.parent);
                badge.gameObject.SetActive(true);
                badge.name = Garage.Vehicles[i].ToString();
                badge.anchoredPosition = new Vector2((i - (Garage.Vehicles.Length - 1) * 0.5f) * BadgeSpacing, 0f);
                int index = i;
                badge.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { if (index != PageIndex) Page(Shortest(index - PageIndex)); });
                _badges.Add(badge);
            }
            vehicleBar.sizeDelta = new Vector2(Garage.Vehicles.Length * BadgeSpacing + 32f, vehicleBar.sizeDelta.y);
            vehicleRing.SetAsLastSibling();
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
            _scrubDir = 0; _scrub = _scrubTarget = 0f; _scrubPlan = null; _snapping = false;
            _morph = null;
            Morph(PaletteOf(_vehicle), instant: true);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.5f);
            // Straight into choosing: the part tabs on top, the chips below, on the paint.
            Focus((int)Garage.Slot.Paint);
            if (_focus < 0) Refresh(); // (a locked vehicle: nothing to choose, just its silhouette)
            _focusAt = _switchAt = _openedAt;
        }

        // A vehicle's page in its cup's colours; a locked one all in greys.
        static Theme.Palette PaletteOf(Vehicle vehicle) => !Garage.VehicleOpen(vehicle) ? Theme.Locked
            : vehicle switch { Vehicle.Snowmobile => Theme.Winter, Vehicle.Boat => Theme.Water, _ => Theme.Valley };

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
            if (_scrubDir != 0 || Neighbour(step) < 0) return;
            StartScrub(step);
            Snap();
        }

        // The vehicles go round: after the last comes the first again (and before the first the last).
        int Neighbour(int step)
        {
            int n = Garage.Vehicles.Length;
            return n < 2 ? -1 : ((PageIndex + step) % n + n) % n;
        }

        static int Shortest(int step)
        {
            int n = Garage.Vehicles.Length;
            step = ((step % n) + n) % n;
            return step > n / 2 ? step - n : step;
        }

        // A vehicle change in the making (step +1: the one below comes up, -1: the one above comes down): the colours
        // blend with it, all the way to the other cup's.
        void StartScrub(int step)
        {
            if (_scrubDir == step) return;
            RestoreScrubColours();
            _scrubDir = step;
            _scrubPlan = Theme.RecolorPlan(transform, _palette, PaletteOf(Garage.Vehicles[Neighbour(step)]));
            _scrubPlan.RemoveAll(p => p.graphic.name is "Body" or "Color" or "Pic");
            _scrubPlan.RemoveAll(p => _hotspots.Exists(h => p.graphic.transform == h.Button.Find("Face") || p.graphic.transform == h.Button.Find("Label") || p.graphic == h.Name));
        }

        void RestoreScrubColours()
        {
            if (_scrubPlan != null) foreach (var (g, from, _) in _scrubPlan) if (g != null) g.color = from;
            _scrubPlan = null;
        }

        // The change is through: the new vehicle is in the middle now (it is where the old one was drawn, so nothing
        // jumps), in its cup's colours, with the same part open if it has it.
        void FinishScrub()
        {
            if (_scrubPlan != null) foreach (var (g, _, to) in _scrubPlan) if (g != null) g.color = to;
            _scrubPlan = null;
            var vehicle = Garage.Vehicles[Neighbour(_scrubDir)];
            _palette = PaletteOf(vehicle);
            Theme.Use(_palette);
            _vehicle = vehicle;
            _scrubDirWas = _scrubDir;
            _scrubDir = 0;
            _scrub = _scrubTarget = 0f;
            _snapping = false;
            _pageAt = _switchAt = _landedAt = Time.unscaledTime; // it lands with a little pop
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.land, 0.5f);
            // The old vehicle now sits in its peek spot at peek size: that peek takes over at once.
            var back = _scrubDirWas > 0 ? peekLeft : peekRight;
            var backGroup = back.GetComponent<CanvasGroup>();
            if (backGroup != null) backGroup.alpha = 1f;
            if (_focus < 0 || !Fits(_focus)) _focus = (int)Garage.Slot.Paint;
            BuildTray();
            Refresh();
        }

        // Swipe up for the next vehicle (it comes up from below), down for the one before: the vehicles follow the
        // finger; let go past a third of the way and the change glides through, else it glides back.
        public void OnBeginDrag(PointerEventData e)
        {
            _paging = true;
            _dragY = 0f;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_paging) return;
            _dragY += e.delta.y / Mathf.Max(0.01f, GetComponentInParent<Canvas>().rootCanvas.scaleFactor);
            _dragVelocity = Mathf.Lerp(_dragVelocity, e.delta.y / Mathf.Max(1e-4f, Time.unscaledDeltaTime), 0.4f);
            int step = _dragY > 0f ? 1 : -1;
            if (Neighbour(step) < 0) { if (_scrubDir == 0) _scrub = 0f; return; }
            StartScrub(step);
            _scrub = Mathf.Clamp01(Mathf.Abs(_dragY) / PeekGap);
            if (_scrub >= 0.5f) Snap(); // half way: it pops over to the next vehicle
        }

        void Snap()
        {
            _paging = false;
            _snapping = true;
            _scrubTarget = 1f;
            _dragY = _dragVelocity = 0f;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.45f);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_paging) return;
            _paging = false;
            bool flung = Mathf.Abs(_dragVelocity) > 1400f && Mathf.Sign(_dragVelocity) == _scrubDir;
            if (_scrubDir != 0 && flung) { Snap(); return; }
            _scrubTarget = 0f; // short of half way: it glides back
            _dragY = _dragVelocity = 0f;
        }

        bool Fits(int slot) => Garage.VehicleOpen(_vehicle) && Garage.Fits((Garage.Slot)slot, _vehicle);

        public void Close()
        {
            if (_closingAt < 0f) _closingAt = Time.unscaledTime;
        }

        // Closed: back in the map's colours (the screen around it is in them).
        void Closed()
        {
            RestoreScrubColours();
            _scrubDir = 0; _scrub = _scrubTarget = 0f; _snapping = false;
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

        // The chips for the slot: what this vehicle's shop has (its cup's things), scrolled so the one you have on is
        // in view.
        void BuildTray()
        {
            // (Out of the row at once: destroyed only at the end of the frame, they'd still count in the layout.)
            var old = new List<Transform>();
            foreach (Transform child in stripContent) if (child != tileTemplate && child != dividerTemplate) old.Add(child);
            foreach (var child in old) { child.SetParent(null, false); Destroy(child.gameObject); }
            _tiles.Clear(); _shown.Clear();
            var slot = (Garage.Slot)_focus;
            var equipped = Garage.Equipped(slot, _vehicle);
            var parts = Garage.For(slot, _vehicle);
            // A gadget still to come: a mystery slot for this cup's rival's.
            if (slot == Garage.Slot.Gadget && !parts.Exists(p => p.How != Garage.Unlock.Free)) parts.Add(null);
            int equippedAt = 0;
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
            _browsing = equippedAt;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(stripContent);
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
            itemName.gameObject.SetActive(false); // the chips say it all (their names, what they take)
            itemBlurb.gameObject.SetActive(false);
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
            // The stars of the vehicle's cup: they open its things.
            string cupName = Garage.CupOf(_vehicle);
            bool vehicleOpen = Garage.VehicleOpen(_vehicle);
            starsLabel.text = $"{(vehicleOpen ? Garage.CupStars(cupName) : 0)}<size=60%>/{Garage.CupMaxStars(cupName)}</size>";
            var look = CarLook.For(_vehicle);
            car.Show(_vehicle);
            car.Dress(look);
            // A vehicle whose cup is still locked: a dark silhouette with a lock, nothing to choose.
            Silhouette(car, !vehicleOpen);
            LockMark(carRoot, !vehicleOpen, 220f);
            tray.gameObject.SetActive(vehicleOpen);
            buildName.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            for (int i = 0; i < _hotspots.Count; i++)
            {
                _hotspots[i].Value.gameObject.SetActive(false); // just the part's name on the button
                bool on = i == _focus;
                _hotspots[i].Button.Find("Face").GetComponent<UnityEngine.UI.Image>().color = on ? Theme.Orange : Theme.Ink;
                _hotspots[i].Name.color = on ? Color.white : Theme.Amber;
                _hotspots[i].Button.Find("Label").GetComponent<UnityEngine.UI.Image>().color = on ? Theme.OrangeDeep : Theme.WithAlpha(Theme.Ink * 0.6f, 0.95f);
            }
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].gameObject.SetActive(Fits(i));
            // The page: its name, its dot, the other vehicles peeking in at the sides (in their own setup).
            int page = PageIndex;
            for (int i = 0; i < _badges.Count; i++)
            {
                var badgeCar = _badges[i].Find("Car").GetComponent<VehicleIcon>();
                badgeCar.Show(Garage.Vehicles[i]);
                badgeCar.Dress(CarLook.For(Garage.Vehicles[i]));
                Silhouette(badgeCar, !Garage.VehicleOpen(Garage.Vehicles[i]));
                LockMark(_badges[i], !Garage.VehicleOpen(Garage.Vehicles[i]), 56f, dark: true);
            }
            Peek(peekLeft, Neighbour(-1));  // above
            Peek(peekRight, Neighbour(1)); // below
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
            var alone = preview.Find("Alone").gameObject;
            var none = preview.Find("None").gameObject;
            var stripesRoot = preview.Find("Stripes").gameObject;
            stripesRoot.SetActive(false);
            alone.SetActive(false);
            none.SetActive(!mystery && part.Id == "none");
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
                        // Just the body (in your paint): no top, no decal.
                        mini.gameObject.SetActive(true);
                        var bare = look;
                        bare.Body = part;
                        bare.Top = Garage.Find("none", Garage.Slot.Top);
                        bare.Decal = Garage.Find("none", Garage.Slot.Decal);
                        mini.Dress(bare);
                        break;
                    case Garage.Slot.Top:
                    case Garage.Slot.Decal:
                        // Just the part itself, as big as the card takes it.
                        var sprite = GarageArt.Instance?.Get(part)?.Alone;
                        bool stripes = part.Id == "stripes";
                        stripesRoot.SetActive(stripes);
                        foreach (var bar in stripesRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true)) if (bar.name == "Bar") bar.color = new Color(0.22f, 0.24f, 0.27f); // (white on the car; dark to show on the card, whatever the paint)
                        alone.SetActive(sprite != null && !stripes);
                        foreach (var image in alone.GetComponentsInChildren<UnityEngine.UI.Image>(true)) image.sprite = sprite;
                        // Stickers get a darker shadow to stand off the white card.
                        alone.transform.Find("Shadow").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, part.Slot == Garage.Slot.Decal ? 0.7f : 0.4f);
                        ((RectTransform)alone.transform.Find("Shadow")).anchoredPosition = part.Slot == Garage.Slot.Decal ? new Vector2(4f, -5f) : new Vector2(6f, -8f);
                        break;
                    default:
                        iconRoot.SetActive(true);
                        var art = GarageArt.Instance?.Get(part);
                        icon.sprite = art?.full;
                        icon.enabled = icon.sprite != null;
                        icon.color = part.Slot == Garage.Slot.Trail ? TrailTint(part, look.Paint) : Color.white;
                        break;
                }
            if (!mystery && part.Id == "none")
            {
                iconRoot.SetActive(false);
                question.SetActive(false);
            }
            // Only a colour (or a mystery) sits on a round face; a car or a trail lies straight on the card.
            bool round = mystery || part.Slot == Garage.Slot.Paint;
            tile.Find("Face").gameObject.SetActive(round);
            tile.Find("Veil").gameObject.SetActive(!open && round);
            var tint = open || round ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            foreach (var g in preview.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) if (g.transform != preview) g.canvasRenderer.SetAlpha(tint.a);
            tile.Find("Card").GetComponent<UnityEngine.UI.Image>().color = open ? Color.white : new Color(0.79f, 0.8f, 0.8f);
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
            "flake" => new Color(0.45f, 0.72f, 0.95f), // (deep enough to show on a white card)
            "confetti" => new Color(1f, 0.45f, 0.6f),
            "winterconfetti" => new Color(0.35f, 0.65f, 1f),
            "bubbles" => new Color(0.3f, 0.7f, 0.95f),
            "rainbow" => new Color(1f, 0.45f, 0.6f),
            "goldwake" => new Color(0.95f, 0.72f, 0.1f),
            "aurora" => new Color(0.55f, 1f, 0.8f),
            "standard" => new Color(0.5f, 0.52f, 0.55f),
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
            icon.Show(vehicle);
            icon.Dress(CarLook.For(vehicle));
            Silhouette(icon, !Garage.VehicleOpen(vehicle));
            LockMark(peek, !Garage.VehicleOpen(vehicle), 90f);
            peek.Find("Name").gameObject.SetActive(false); // the page title names the vehicle; the peek just shows it
        }

        // A locked vehicle isn't shown at all (just a lock in its place, see LockMark).
        static void Silhouette(VehicleIcon icon, bool on)
        {
            foreach (var image in icon.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.name != "LockMark" && image.sprite != null) image.enabled = !on;
        }

        // A lock over a locked vehicle.
        void LockMark(Transform over, bool on, float size, bool dark = false)
        {
            var mark = over.Find("LockMark");
            if (mark == null)
            {
                if (!on) return;
                var lockSprite = tileTemplate.Find("Lock")?.GetComponent<UnityEngine.UI.Image>()?.sprite;
                var go = new GameObject("LockMark", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(over, false);
                rt.sizeDelta = Vector2.one * size;
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = lockSprite;
                image.color = Theme.Cream;
                image.raycastTarget = false;
                image.preserveAspect = true;
                mark = rt;
            }
            mark.gameObject.SetActive(on);
            mark.GetComponent<UnityEngine.UI.Image>().color = dark ? Theme.Ink : Theme.Cream;
            mark.SetAsLastSibling();
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
                           + 0.08f * Mathf.Sin(Mathf.Clamp01((t - _landedAt) / 0.3f) * Mathf.PI)
                           + 0.05f * Mathf.Sin(Mathf.Clamp01((t - _tryAt) / 0.25f) * Mathf.PI);
            // A vehicle change (following the finger, or gliding after it): the car shrinks into the peek spot on
            // the far side while the vehicle on the near side grows out of its spot into the middle; the colours blend.
            if (!_paging && _scrubDir != 0)
            {
                _scrub = _snapping ? Mathf.MoveTowards(_scrub, 1f, dt / 0.16f) : Mathf.Lerp(_scrub, _scrubTarget, 1f - Mathf.Exp(-11f * dt));
                if (_scrubTarget > 0f && _scrub > 0.995f) FinishScrub();
                else if (_scrubTarget <= 0f && _scrub < 0.005f) { RestoreScrubColours(); _scrubDir = 0; _scrub = 0f; _snapping = false; }
            }
            // Under the finger 1:1; the snap speeds through the rest (and the new vehicle lands with a pop).
            float k = _scrub;
            if (_scrubPlan != null) foreach (var (g, from, to) in _scrubPlan) if (g != null) g.color = Color.Lerp(from, to, k);
            Vector2 middle = _carHome + new Vector2(0f, Rise * focus);
            float mainScale = Mathf.Lerp(1f, FocusScale, focus);
            Vector2 away = _scrubDir > 0 ? PeekAbove : PeekBelow;
            carRoot.anchoredPosition = Vector2.Lerp(middle, away, k) + new Vector2(0f, 700f * fall + Mathf.Sin(t * 1.4f) * 8f * (1f - k));
            carRoot.localScale = Vector3.one * Mathf.Lerp(mainScale, PeekScale, k) * pop;
            outgoing.gameObject.SetActive(false);
            podium.anchoredPosition = middle;
            podium.localScale = Vector3.one * Mathf.Lerp(1f, FocusScale, focus) * Mathf.Lerp(0.6f, 1f, EaseOut(drop));
            podiumRing.localRotation = Quaternion.Euler(0f, 0f, t * 12f);
            float ring = Mathf.Clamp01((t - _changedAt) / 0.55f);
            burst.gameObject.SetActive(ring < 1f);
            burst.rectTransform.anchoredPosition = carRoot.anchoredPosition;
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.9f, 1f - (1f - ring) * (1f - ring));
            burst.color = Theme.WithAlpha(Theme.Amber, 0.85f * (1f - ring));

            // The hotspots: drawn out one after another after the drop. The buttons stay put round the middle; their
            // arms hold on to the car. On a vehicle change they hold the old one until half way, then let go and
            // snap over (springing a little) to the vehicle coming in, following it into the middle.
            bool toNew = _scrubDir != 0 && (_snapping || k >= 0.5f);
            if (toNew != _armsToNew) { _armsToNew = toNew; _armsSpringUntil = t + 0.5f; }
            Vector2 holdAt = carRoot.anchoredPosition;
            float holdScale = carRoot.localScale.x;
            if (toNew)
            {
                var coming = _scrubDir < 0 ? peekLeft : peekRight;
                holdAt = Vector2.Lerp(_peekHome.TryGetValue(coming, out var home) ? home : coming.anchoredPosition, middle, k);
                holdScale = Mathf.Lerp(PeekScale, mainScale, k);
            }
            bool spring = t < _armsSpringUntil;
            for (int i = 0; i < _hotspots.Count; i++)
            {
                var spot = _hotspots[i];
                float appear = Fits(i) ? Mathf.Clamp01((t - _openedAt - 0.6f - i * 0.06f) / 0.35f) : 0f;
                // The part being chosen orange and a little bigger.
                bool on = i == _focus;
                float pick = on ? 1f + 0.16f * Mathf.Sin(Mathf.Clamp01((t - _switchAt) / 0.3f) * Mathf.PI) : 1f;
                Vector2 target = holdAt + Anchors[i] * holdScale;
                if (spring)
                {
                    _armVelocity[i] += (target - _armEnd[i]) * 520f * dt;
                    _armVelocity[i] *= Mathf.Exp(-20f * dt);
                    _armEnd[i] += _armVelocity[i] * dt;
                }
                else { _armEnd[i] = target; _armVelocity[i] = Vector2.zero; }
                Vector2 anchor = _armEnd[i];
                Vector2 at = middle + Spots[i] * HotScale;
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
            // The other vehicles peek in above and below. The one the change goes to grows out of its spot into the
            // middle (to the main car's size); the one on the far side fades away. They and their arrows fade in and
            // out smoothly.
            foreach (var (peek, step) in new[] { (peekLeft, -1), (peekRight, 1) })
            {
                if (!_peekHome.ContainsKey(peek)) _peekHome[peek] = peek.anchoredPosition;
                bool exists = Neighbour(step) >= 0;
                if (!exists) { peek.gameObject.SetActive(false); continue; }
                peek.gameObject.SetActive(true);
                var group = peek.GetComponent<CanvasGroup>() ?? peek.gameObject.AddComponent<CanvasGroup>();
                bool coming = _scrubDir == step;
                float grow = coming ? k : 0f;
                // Waiting in its spot it floats gently up and down (easing in after a change, so nothing jumps).
                float drift = Mathf.Sin((t - _pageAt) * 1.8f + step) * 12f * Mathf.Clamp01((t - _pageAt) / 0.8f) * (1f - grow);
                peek.anchoredPosition = Vector2.Lerp(_peekHome[peek], middle, grow) + new Vector2(0f, Mathf.Sin(t * 1.4f) * 8f * grow + drift);
                peek.localScale = Vector3.one * Mathf.Lerp(1f, mainScale / PeekScale, grow);
                float want = Mathf.Clamp01((t - _openedAt - 0.8f) / 0.3f) * (coming || _scrubDir == 0 ? 1f : 1f - k);
                group.alpha = coming ? Mathf.Max(group.alpha, want) : Mathf.Lerp(group.alpha, want, 1f - Mathf.Exp(-12f * dt));
                var arrow = peek.Find("Arrow");
                float nudge = Mathf.Repeat(t, 3.5f) < 0.5f ? Mathf.Sin(Mathf.Repeat(t, 3.5f) / 0.5f * Mathf.PI) * 14f : 0f;
                arrow.localPosition = new Vector3(0f, -step * (175f + nudge), 0f);
                var arrowImage = arrow.GetComponent<UnityEngine.UI.Image>();
                arrow.localScale = Vector3.one / peek.localScale.x; // (it doesn't grow with its vehicle)
                float gone = 1f - Mathf.Clamp01(_scrub * 5f);
                var c = arrowImage.color; c.a = Mathf.Min(gone, Mathf.Lerp(c.a, gone, 1f - Mathf.Exp(-8f * dt))); arrowImage.color = c;
            }

            // The vehicle bar: the ring glides along with the change (and pops as it lands); the badge on show a
            // little bigger.
            float shownAt = _scrubDir != 0 ? Mathf.Lerp(PageIndex, Neighbour(_scrubDir), k) : PageIndex; // (round the end it glides back across)
            for (int i = 0; i < _badges.Count; i++)
                _badges[i].localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(1f - Mathf.Abs(shownAt - i)));
            vehicleRing.anchoredPosition = new Vector2((shownAt - (_badges.Count - 1) * 0.5f) * BadgeSpacing, 0f);
            vehicleRing.localScale = Vector3.one * (1f + 0.16f * Mathf.Sin(Mathf.Clamp01((t - _pageAt) / 0.3f) * Mathf.PI));

            highlight.gameObject.SetActive(false); // (no ring pulsing on the part being changed: the orange hotspot says it)

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
