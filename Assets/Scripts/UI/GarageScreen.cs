using System.Collections.Generic;
using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The garage, a showroom over the map: your car big on a turning stage under a spotlight (drag to spin it),
    /// its name plate, and a shelf below with a tab for each kind of thing (paint, body, top, decal, trail, gadget)
    /// and the rivals' cards. Every slot is on the shelf, also the locked ones, each with what it takes: stars, a
    /// rival to beat (their face) or a cup's gold. Tap an open one and the car takes it with a pop and a ring of
    /// light; a locked one shakes and says what it takes. The rivals' tab is a collection of cards: the ones you
    /// have met with their face, the others still a mystery; tap one for the whole card.
    /// </summary>
    public class GarageScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform sheet;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [Header("Stage")]
        [SerializeField] RectTransform carHolder;
        [SerializeField] VehicleIcon car;
        [SerializeField] RectTransform turntable;
        [SerializeField] UnityEngine.UI.Image burst;
        [SerializeField] GarageSpin spin;
        [SerializeField] TMP_Text starsLabel, buildName, buildDetails;
        [Header("Shelf")]
        [SerializeField] RectTransform tabTemplate;
        [SerializeField] RectTransform tileTemplate;
        [SerializeField] RectTransform cardTemplate;
        [SerializeField] TMP_Text itemName, itemBlurb, shelfTitle, shelfCount;
        [Header("A rival's card")]
        [SerializeField] GameObject detail;
        [SerializeField] UnityEngine.UI.Button detailClose;
        [SerializeField] UnityEngine.UI.Image detailPainting;
        [SerializeField] TMP_Text detailName, detailTitle, detailBio, detailHome, detailGadget, detailStatus;
        [SerializeField] UnityEngine.UI.Image detailGadgetIcon;

        enum Tab { Paint, Body, Top, Decal, Trail, Gadget, Rivals }
        static readonly string[] TabNames = { "PAINT", "BODY", "TOP", "DECAL", "TRAIL", "GADGET", "RIVALS" };
        [SerializeField] Sprite[] tabIcons;

        // The rivals still to come: their home races (names and faces stay a secret).
        static readonly string[] MysteryHomes = { "VALLEY SPRINT", "RIVER JUMP", "LAGOON LEAP", "SNOWBALL VILLAGE", "FROSTBITE PARK", "THE ABYSS" };

        readonly List<RectTransform> _tabs = new(), _tiles = new(), _cards = new();
        readonly List<Garage.Part> _shown = new();
        Tab _tab = Tab.Paint;
        float _openedAt = -10f, _changedAt = -10f, _shakeAt = -10f, _messageUntil, _tabAt = -10f, _closingAt = -1f;
        int _shake = -1;
        Garage.Part _focus;
        RivalCharacter[] _rivals;

        void Awake()
        {
            closeButton.onClick.AddListener(Close);
            if (detailClose != null) detailClose.onClick.AddListener(() => PopIn.Hide(detail));
            if (detail != null && detail.transform.Find("Dim")?.GetComponent<UnityEngine.UI.Button>() is { } dim) dim.onClick.AddListener(() => PopIn.Hide(detail));
            tabTemplate.gameObject.SetActive(false);
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tab = Instantiate(tabTemplate, tabTemplate.parent);
                tab.gameObject.SetActive(true);
                tab.name = TabNames[i];
                tab.Find("Label").GetComponent<TMP_Text>().text = TabNames[i];
                if (tabIcons != null && i < tabIcons.Length) tab.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite = tabIcons[i];
                var t = (Tab)i;
                tab.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Show(t));
                _tabs.Add(tab);
            }
            tileTemplate.gameObject.SetActive(false);
            for (int i = 0; i < 8; i++)
            {
                var tile = Instantiate(tileTemplate, tileTemplate.parent);
                tile.name = "Tile" + i;
                int index = i;
                tile.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Pick(index));
                _tiles.Add(tile);
            }
            cardTemplate.gameObject.SetActive(false);
            for (int i = 0; i < 1 + MysteryHomes.Length; i++)
            {
                var card = Instantiate(cardTemplate, cardTemplate.parent);
                card.name = "Card" + i;
                int index = i;
                card.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OpenCard(index));
                _cards.Add(card);
            }
            if (detail != null) detail.SetActive(false);
            panel.SetActive(false);
        }

        public void Open()
        {
            _rivals = RivalCharacter.All;
            Garage.MarkSeen();
            _openedAt = Time.unscaledTime;
            _closingAt = -1f;
            panel.SetActive(true);
            Show(_tab, quiet: true);
        }

        public void Close()
        {
            if (_closingAt < 0f) _closingAt = Time.unscaledTime;
        }

        // ---------------------------------------------------------------- the shelf

        void Show(Tab tab, bool quiet = false)
        {
            if (!quiet && tab != _tab) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
            _tab = tab;
            _tabAt = Time.unscaledTime;
            _focus = null;
            _messageUntil = 0f;
            Refresh();
        }

        void Refresh()
        {
            starsLabel.text = ProgressStore.TotalStars.ToString();
            var look = CarLook.Player;
            car.Dress(look);
            buildName.text = look.Body.Name;
            var extras = new List<string> { Garage.Equipped(Garage.Slot.Paint).Name };
            if (look.Top.Id != "none") extras.Add(look.Top.Name);
            if (look.Decal.Id != "none") extras.Add(look.Decal.Name);
            buildDetails.text = string.Join("  ·  ", extras);

            for (int i = 0; i < _tabs.Count; i++)
            {
                bool on = i == (int)_tab;
                var tab = _tabs[i];
                tab.Find("On").gameObject.SetActive(on);
                tab.Find("Icon").GetComponent<UnityEngine.UI.Image>().color = on ? Color.white : Theme.WithAlpha(Theme.Cream, 0.75f);
                tab.Find("Label").GetComponent<TMP_Text>().color = on ? Color.white : Theme.WithAlpha(Theme.Cream, 0.75f);
            }

            bool rivals = _tab == Tab.Rivals;
            foreach (var card in _cards) card.gameObject.SetActive(rivals);
            if (rivals)
            {
                foreach (var tile in _tiles) tile.gameObject.SetActive(false);
                FillCards();
                return;
            }
            var slot = (Garage.Slot)(int)_tab;
            _shown.Clear();
            _shown.AddRange(Garage.Of(slot));
            int open = 0;
            foreach (var part in _shown) if (Garage.IsUnlocked(part)) open++;
            int mystery = slot == Garage.Slot.Gadget ? Mathf.Min(Garage.MysteryGadgets, _tiles.Count - _shown.Count) : 0;
            shelfTitle.text = TabNames[(int)_tab];
            shelfCount.text = $"{open} / {_shown.Count + mystery} OPEN";
            var equipped = Garage.Equipped(slot);
            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                bool exists = i < _shown.Count + mystery;
                tile.gameObject.SetActive(exists);
                if (!exists) continue;
                FillTile(tile, i < _shown.Count ? _shown[i] : null, equipped, look);
            }
            if (Time.unscaledTime >= _messageUntil)
            {
                var focus = _focus ?? equipped;
                itemName.text = focus.Name;
                itemName.color = Color.white;
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
            }
        }

        static Color TrailTint(Garage.Part trail, Color paint) => trail.Id switch
        {
            "flame" => new Color(1f, 0.6f, 0.2f),
            "flake" => new Color(0.8f, 0.93f, 1f),
            "confetti" => new Color(1f, 0.45f, 0.6f),
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
            Garage.Unlock.Stars => $"COLLECT {part.Stars} STARS",
            Garage.Unlock.Rival => rival != null ? $"BEAT {rival.displayName.ToUpperInvariant()} IN {rival.homeRace?.displayName.ToUpperInvariant()}" : "BEAT A RIVAL",
            Garage.Unlock.CupGold => "3 STARS IN EVERY RACE OF THE CUP",
            _ => "",
        };

        void Pick(int index)
        {
            if (_tab == Tab.Rivals) return;
            var part = index < _shown.Count ? _shown[index] : null;
            if (part == null || !Garage.IsUnlocked(part))
            {
                // Not yet: it shakes, and says what it takes.
                _shake = index;
                _shakeAt = Time.unscaledTime;
                itemName.text = part != null ? part.Name : "A RIVAL'S GADGET";
                itemName.color = Theme.Amber;
                itemBlurb.text = part != null ? Requirement(part, Rival(part.From)) : "Every rival you beat in their home race gives you their gadget.";
                _messageUntil = Time.unscaledTime + 2.2f;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
                return;
            }
            _focus = part;
            _messageUntil = 0f;
            if (Garage.Equipped(part.Slot) != part)
            {
                Garage.Equip(part);
                _changedAt = Time.unscaledTime;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.6f);
            }
            Refresh();
        }

        // ---------------------------------------------------------------- the rivals' cards

        void FillCards()
        {
            shelfTitle.text = "RIVALS";
            int met = 0;
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                var rival = i < (_rivals?.Length ?? 0) ? _rivals[i] : null;
                bool known = rival != null && (RivalProgress.HasMet(rival.name) || CupList.DevUnlockAll);
                if (known) met++;
                var picture = card.Find("Picture").GetComponent<UnityEngine.UI.Image>();
                picture.sprite = rival != null ? rival.portrait : null;
                picture.enabled = rival != null && rival.portrait != null;
                picture.color = known ? Color.white : new Color(0.05f, 0.08f, 0.1f, 0.9f);
                card.Find("Question").gameObject.SetActive(!known);
                card.Find("Name").GetComponent<TMP_Text>().text = known ? rival.displayName.ToUpperInvariant() : "???";
                string home = rival != null ? rival.homeRace?.displayName.ToUpperInvariant() : MysteryHomes[Mathf.Clamp(i - (_rivals?.Length ?? 0), 0, MysteryHomes.Length - 1)];
                card.Find("Home").GetComponent<TMP_Text>().text = home;
                bool beaten = rival != null && RivalProgress.IsBeaten(rival.name);
                card.Find("Beaten").gameObject.SetActive(beaten);
            }
            shelfCount.text = $"{met} / {_cards.Count} MET";
            if (Time.unscaledTime >= _messageUntil)
            {
                itemName.text = "RIVAL CARDS";
                itemName.color = Color.white;
                itemBlurb.text = "Meet the rivals in their home races. Beat them to win their gadget.";
            }
        }

        void OpenCard(int index)
        {
            var rival = _rivals != null && index < _rivals.Length ? _rivals[index] : null;
            if (rival == null || !(RivalProgress.HasMet(rival.name) || CupList.DevUnlockAll))
            {
                _shake = 100 + index;
                _shakeAt = Time.unscaledTime;
                itemName.text = "???";
                itemName.color = Theme.Amber;
                itemBlurb.text = rival != null ? $"Waiting for you in {rival.homeRace?.displayName.ToUpperInvariant()}." : "A rival still to come.";
                _messageUntil = Time.unscaledTime + 2.2f;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
                return;
            }
            detailPainting.sprite = rival.painting != null ? rival.painting : rival.portrait;
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
            float t = Time.unscaledTime;
            // Slides up when it opens, down when it closes.
            float open = Mathf.Clamp01((t - _openedAt) / 0.35f);
            float shut = _closingAt >= 0f ? Mathf.Clamp01((t - _closingAt) / 0.25f) : 0f;
            float shown = (1f - Mathf.Pow(1f - open, 3f)) * (1f - shut * shut);
            group.alpha = shown;
            sheet.anchoredPosition = new Vector2(0f, -220f * (1f - shown));
            if (shut >= 1f) { panel.SetActive(false); return; }

            if (_messageUntil > 0f && t >= _messageUntil) { _messageUntil = 0f; Refresh(); }
            // The stage turns slowly (or as you spin it); a new part pops the car with a ring of light.
            if (turntable != null) turntable.localRotation = Quaternion.Euler(0f, 0f, carHolder.localEulerAngles.z);
            float k = Mathf.Clamp01((t - _changedAt) / 0.35f);
            float pop = 1f + 0.14f * Mathf.Sin(k * Mathf.PI);
            ((RectTransform)car.transform).localScale = Vector3.one * pop;
            ((RectTransform)car.transform).anchoredPosition = new Vector2(0f, Mathf.Sin(t * 1.6f) * 8f);
            float ring = Mathf.Clamp01((t - _changedAt) / 0.55f);
            burst.gameObject.SetActive(ring < 1f);
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.7f, 1f - (1f - ring) * (1f - ring));
            burst.color = Theme.WithAlpha(Theme.Amber, 0.85f * (1f - ring));

            // The tabs: the chosen one sits up; tiles pop in one after another when the tab changes.
            for (int i = 0; i < _tabs.Count; i++)
                _tabs[i].Find("Icon").localScale = Vector3.one * (i == (int)_tab ? 1.1f + 0.04f * Mathf.Sin(t * 3f) : 1f);
            for (int i = 0; i < _tiles.Count; i++) Wiggle(_tiles[i], i, t, i);
            for (int i = 0; i < _cards.Count; i++) Wiggle(_cards[i], 100 + i, t, i);
        }

        void Wiggle(RectTransform item, int id, float t, int order)
        {
            if (!item.gameObject.activeSelf) return;
            float appear = Mathf.Clamp01((t - _tabAt - order * 0.035f) / 0.25f);
            float s = id == _shake ? Mathf.Clamp01((t - _shakeAt) / 0.4f) : 1f;
            item.localScale = Vector3.one * EaseOutBack(appear);
            item.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(s * Mathf.PI * 6f) * 6f * (1f - s));
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
