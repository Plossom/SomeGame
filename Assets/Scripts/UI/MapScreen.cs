using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The map (start screen): a painted alpine valley with the mountain road of <see cref="MapLayout"/>.
    /// The races sit on the road, followed by a "more soon" stop. The road you have opened up is drawn
    /// in orange up to your car. Tapping a race drives the car there and opens the race cards
    /// (<see cref="RaceDeck"/>) on it, which show its name, stars and records; START on a card enters the race.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        [SerializeField] LevelCatalog catalog;
        [Tooltip("Holds the map picture; scaled to cover the screen. Stops, road and car are placed in it.")]
        [SerializeField] RectTransform world;
        [Tooltip("Inactive node used as a template.")]
        [SerializeField] MapNode nodeTemplate;
        [SerializeField] RectTransform car;
        [Tooltip("The painted landscape; replaced by the selected cup's picture.")]
        [SerializeField] UnityEngine.UI.Image background;
        [Tooltip("Animated life on the landscape: clouds, chimney smoke, glints, birds, the lift.")]
        [SerializeField] MapAmbience ambience;
        [SerializeField] RaceDeck deck;
        [Header("Switching cups")]
        [Tooltip("What slides when switching cups (the map, the race header, the cup name), with how much faster each moves (depth).")]
        [SerializeField] RectTransform[] page;
        [SerializeField] float[] pageParallax;
        [Tooltip("The cup bar under the cup name: a dark plate with a round badge per cup (tap one to go there) and a glowing ring on the current cup.")]
        [SerializeField] RectTransform dots;
        [SerializeField] RectTransform badgeTemplate;
        [SerializeField] RectTransform barRing;
        [SerializeField] RectTransform barContent;
        [SerializeField] UnityEngine.UI.Image barChevron;
        [SerializeField] UnityEngine.UI.Button barToggle;
        [Tooltip("Frozen picture of the old page while the next cup slides in over it (a clipping rect, its picture and the shadow at its edge).")]
        [SerializeField] RectTransform wipe;
        [SerializeField] UnityEngine.UI.RawImage wipeImage;
        [SerializeField] UnityEngine.UI.Image wipeEdge;
        [Tooltip("Panels above the frozen picture (the globe button): their colours blend over as the new cup comes in.")]
        [SerializeField] Transform[] fadeRoots;
        [Tooltip("Blurred copy of the frozen picture and a dark veil: the old cup blurs and dims away as the new one comes in.")]
        [SerializeField] UnityEngine.UI.RawImage wipeBlur;
        [SerializeField] UnityEngine.UI.Image wipeDim;
        [Tooltip("Blurred picture of the new cup over its page (under the old one): it sharpens as the new cup slides in.")]
        [SerializeField] UnityEngine.UI.RawImage incomingBlur;

        [Header("Locked and coming-soon cups")]
        [Tooltip("Shown instead of the map for a cup that is locked or still coming.")]
        [SerializeField] GameObject lockedPage;
        [SerializeField] UnityEngine.UI.Image lockedIcon;
        [SerializeField] Sprite lockSprite;
        [SerializeField] Sprite soonSprite;
        [SerializeField] TMP_Text lockedTitle;
        [SerializeField] TMP_Text lockedCaption;
        [SerializeField] GameObject lockedNeeds;
        [SerializeField] TMP_Text lockedStars;
        [Tooltip("Parts of the page hidden on a locked cup (the race header).")]
        [SerializeField] GameObject[] racePageParts;

        [Header("Unlocks")]
        [SerializeField] CupUnlockCelebration celebration;
        [SerializeField] Sprite sparkle;

        [Tooltip("Snowfall over the map, shown on the winter map.")]
        [SerializeField] GameObject snow;
        [SerializeField] TMP_Text totalStars;

        [Header("Garage")]
        [SerializeField] UnityEngine.UI.Button garageButton;
        [Tooltip("The dot on the garage button: a paint has opened since you last looked.")]
        [SerializeField] GameObject garageDot;
        [SerializeField] GarageScreen garage;
        [Tooltip("The garage card's car: yours as built, turning.")]
        [SerializeField] VehicleIcon garageCar;
        [SerializeField] TMP_Text garageBuild;
        [Tooltip("Meeting a rival the first time their home race is open.")]
        [SerializeField] RivalMeeting meeting;

        [Header("Selected race")]
        [SerializeField] TMP_Text chapterLabel;

        [Header("Road progress")]
        [SerializeField, Min(1f)] float progressWidth = 22f;
        [Tooltip("How far before its race stop the car waits (canvas units along the road; less where the stops are close).")]
        [SerializeField] float carOffset = 115f;

        readonly List<MapNode> _nodes = new();
        readonly List<RectTransform> _badges = new();
        float _ringAt;      // where the ring is (in badge slots)
        float _ringVelocity;
        int _wipeTargetSlot = -1;
        float _wipeCovering;
        bool _barOpen;
        float _barOpenAt, _barOpenVelocity, _collapseAt = -1f;
        Vector2[] _pageHome;
        bool _switching;
        CupDefinition _cup;
        bool _celebrating;
        int _hideFrom = int.MaxValue; // races from here on still look locked (they unlock with an animation)
        readonly List<GameObject> _progress = new();
        int _selected;
        Vector2 _lastParentSize;

        void Awake()
        {
            // The map shows the selected cup's races.
            var cup = CupList.Selected;
            _cup = cup;
            if (cup != null) catalog = CatalogOf(cup);
            ApplyLook(cup != null ? cup.look : CupLook.Valley, cup != null ? cup.mapBackground : null);
            PaintCar();
            Garage.Changed += PaintCar;
            if (garageButton != null) garageButton.onClick.AddListener(() => { garage.Open(MapVehicle); garageDot.SetActive(false); });
        }

        // The car on the map road: a snowmobile on the Winter Cup.
        void ShowVehicle(CupLook look)
        {
            if (car != null && car.TryGetComponent(out VehicleIcon icon)) icon.Show(look == CupLook.Winter);
            PaintCar();
        }

        /// <summary>The vehicle of the cup on show: the snowmobile on the Winter Cup, the car elsewhere (its garage page).</summary>
        SomeGame.Track.TrackLayout.Vehicle MapVehicle => CupList.Selected != null && CupList.Selected.look == CupLook.Winter
            ? SomeGame.Track.TrackLayout.Vehicle.Snowmobile : SomeGame.Track.TrackLayout.Vehicle.Car;

        static LevelCatalog CatalogOf(CupDefinition cup) =>
            cup.HasRaces ? cup.races : ScriptableObject.CreateInstance<LevelCatalog>(); // empty: a cup still in the making

        // The cup's map: its road and landscape, and its colours on every image and text of the screen
        // (built in the Valley colours). Layout, fonts and shapes stay the same.
        void ApplyLook(CupLook look, Sprite landscape)
        {
            MapLayout.Current = MapLayout.Of(look);
            ShowVehicle(look);
            Theme.Use(Theme.Of(look));
            Theme.Recolor(transform, Theme.Valley);
            var cam = Camera.main;
            if (cam != null && Theme.TryMap(cam.backgroundColor, Theme.Valley, out var sky)) cam.backgroundColor = sky;
            if (landscape != null && background != null) background.sprite = landscape;
            if (snow != null) snow.SetActive(look == CupLook.Winter);
            if (ambience != null) ambience.Play(look);
        }

        void Start()
        {
            // First launch (or after a reset): straight into the practice drive.
            if (GameSession.TutorialPending)
            {
                GameSession.StartTutorial();
                return;
            }
            if (!CupList.DevUnlockAll) UnlockLog.EnsureInitialised();
            var newRaces = PrepareRaceUnlocks();
            _selected = InitialSelection();
            Build();
            Refresh();
            FitWorld();
            if (page != null)
            {
                _pageHome = new Vector2[page.Length];
                for (int i = 0; i < page.Length; i++) _pageHome[i] = page[i].anchoredPosition;
            }
            SetLockedView(false, _cup);
            BuildDots();
            StartCoroutine(OpenSequence(newRaces));
        }

        // Opening the map: everything pops in, the counters count up to what the last race earned, then
        // any unlocks play.
        System.Collections.IEnumerator OpenSequence(List<int> newRaces)
        {
            _celebrating = true;
            yield return Entrance();
            yield return CountUp();
            yield return Celebrate(newRaces, true);
        }

        // The race stops pop up one after another along the road; the header pops in with them.
        System.Collections.IEnumerator Entrance()
        {
            for (int i = 0; i < _nodes.Count; i++) _nodes[i].Appear(0.08f + i * 0.055f);
            var popped = new List<RectTransform>();
            foreach (var part in racePageParts) if (part != null) popped.Add((RectTransform)part.transform);
            var groups = new List<CanvasGroup>();
            foreach (var rt in popped)
            {
                var g = rt.GetComponent<CanvasGroup>();
                if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
                groups.Add(g);
            }
            float total = 0.45f + _nodes.Count * 0.055f;
            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / 0.35f);
                foreach (var g in groups) g.alpha = k;
                yield return null;
            }
            foreach (var g in groups) g.alpha = 1f;
        }

        // ------------------------------------------------------------------ counters

        const string ShownStarsKey = "Shown.Stars";
        int? _starsShown;

        // Stars earned since the map was last shown count up, the counter popping with each step.
        System.Collections.IEnumerator CountUp()
        {
            int stars = ProgressStore.TotalStars;
            int fromStars = Mathf.Min(PlayerPrefs.GetInt(ShownStarsKey, stars), stars);
            PlayerPrefs.SetInt(ShownStarsKey, stars);
            PlayerPrefs.Save();
            if (fromStars >= stars) yield break;
            _starsShown = fromStars;
            WriteCounters();
            var plate = totalStars.transform.parent.parent;
            yield return new WaitForSecondsRealtime(0.2f);
            int steps = stars - fromStars;
            float step = Mathf.Clamp(0.9f / steps, 0.06f, 0.2f);
            for (int i = 1; i <= steps; i++)
            {
                _starsShown = Mathf.Min(stars, fromStars + i);
                WriteCounters();
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.5f);
                for (float t = 0f; t < step; t += Time.unscaledDeltaTime)
                {
                    plate.localScale = Vector3.one * (1f + 0.14f * Mathf.Sin(Mathf.Clamp01(t / step) * Mathf.PI));
                    yield return null;
                }
            }
            plate.localScale = Vector3.one;
            _starsShown = null;
            WriteCounters();
        }

        // The cup on show's own stars (those still counting up held back).
        void WriteCounters()
        {
            int cupStars = _cup != null ? Garage.CupStars(_cup.name) : ProgressStore.TotalStars;
            int pending = ProgressStore.TotalStars - (_starsShown ?? ProgressStore.TotalStars);
            totalStars.text = Mathf.Max(0, cupStars - pending).ToString();
            if (garageDot != null) garageDot.SetActive(Garage.HasNew);
        }

        // ------------------------------------------------------------------ garage

        // The car on the map wears your paint (also after a cup switch has recoloured the screen).
        void PaintCar()
        {
            var look = SomeGame.Car.CarLook.For(MapVehicle);
            if (car != null && car.TryGetComponent(out VehicleIcon icon)) icon.Dress(look);
            if (garageCar != null)
            {
                garageCar.Show(MapVehicle == SomeGame.Track.TrackLayout.Vehicle.Snowmobile);
                garageCar.Dress(look);
            }
            if (garageBuild != null) garageBuild.text = Garage.Equipped(Garage.Slot.Body).Name;
        }

        // ------------------------------------------------------------------ unlocks

        // Races of the shown cup opened since you last saw it: they stay drawn locked until they unlock
        // with an animation. Nothing with the UNLOCK ALL switch on (that is not progress).
        List<int> PrepareRaceUnlocks()
        {
            var races = CupList.DevUnlockAll ? new List<int>() : UnlockLog.NewRaces(catalog);
            _hideFrom = races.Count > 0 ? races[0] : int.MaxValue;
            return races;
        }

        // One after another: the new races unlock (the last one gets selected), then each newly unlocked
        // cup gets its big moment; after the last one the map switches to it. Input waits meanwhile.
        System.Collections.IEnumerator Celebrate(List<int> races, bool cups)
        {
            _celebrating = true;
            yield return null; // let the layout settle
            if (races.Count > 0)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                foreach (int index in races)
                {
                    if (index >= _nodes.Count) continue;
                    int i = index;
                    yield return _nodes[i].PlayUnlock(sparkle, () => { _hideFrom = i + 1; Refresh(); });
                    yield return new WaitForSecondsRealtime(0.12f);
                }
                _hideFrom = int.MaxValue;
                UnlockLog.SeeRaces(catalog);
                Select(races[^1]); // drive to the newest race
                // A rival waits there: they come to say hello.
                yield return new WaitForSecondsRealtime(0.9f);
                bool met = false;
                if (MeetRival(races[^1], () => met = true)) yield return new WaitUntil(() => met);
            }
            CupDefinition newest = null;
            if (cups && !CupList.DevUnlockAll && celebration != null)
                foreach (var cup in UnlockLog.NewCups())
                {
                    yield return celebration.Play(cup);
                    UnlockLog.SeeCup(cup);
                    newest = cup;
                }
            _celebrating = false;
            RefreshBadges();
            if (newest != null && newest != _cup)
            {
                SwitchTo(newest);
                StartCoroutine(Arrive());
            }
        }

        /// <summary>True while the map is animating (switching cups, unlocking): the tour waits.</summary>
        public bool Busy => _switching || _celebrating || _wipeFrom != null;

        /// <summary>The selected race's stop on the map.</summary>
        public RectTransform SelectedStop => _selected >= 0 && _selected < _nodes.Count ? (RectTransform)_nodes[_selected].transform : null;

        // A new cup's welcome once the map has switched to it: its name jumps up and sparkles fly off it.
        System.Collections.IEnumerator Arrive()
        {
            yield return null;
            while (_switching || _wipeFrom != null) yield return null;
            var title = chapterLabel.rectTransform;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.unlock, 0.9f);
            var bits = new List<(RectTransform rt, Vector2 v)>();
            if (sparkle != null)
                for (int i = 0; i < 18; i++)
                {
                    var go = new GameObject("Sparkle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    var rt = (RectTransform)go.transform;
                    rt.SetParent(title, false);
                    rt.sizeDelta = Vector2.one * Random.Range(40f, 80f);
                    var img = go.GetComponent<UnityEngine.UI.Image>();
                    img.sprite = sparkle; img.raycastTarget = false; img.color = Theme.Amber;
                    float a = i / 18f * Mathf.PI * 2f;
                    bits.Add((rt, new Vector2(Mathf.Cos(a) * 1.6f, Mathf.Sin(a)) * Random.Range(420f, 700f)));
                }
            for (float t = 0f; t < 1.1f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / 0.6f);
                title.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.3f));
                title.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 3f) * 4f * (1f - k));
                foreach (var (rt, v) in bits)
                {
                    rt.anchoredPosition = v * (1f - Mathf.Pow(1f - Mathf.Clamp01(t / 1.1f), 3f));
                    rt.localRotation = Quaternion.Euler(0f, 0f, t * 240f);
                    var img = rt.GetComponent<UnityEngine.UI.Image>();
                    var c = img.color; c.a = 1f - Mathf.Clamp01(t / 1.1f); img.color = c;
                }
                yield return null;
            }
            title.localScale = Vector3.one;
            title.localRotation = Quaternion.identity;
            foreach (var (rt, _) in bits) Destroy(rt.gameObject);
        }

        // ------------------------------------------------------------------ switching cups

        /// <summary>The cups you can page through on the map: all of them (locked and coming-soon ones show their own page).</summary>
        static List<CupDefinition> Switchable() => CupList.Instance != null ? new List<CupDefinition>(CupList.Instance.cups) : new List<CupDefinition>();

        // The next / previous cup, wrapping round (past the last comes the first and the other way round).
        CupDefinition Neighbour(int direction)
        {
            var cups = Switchable();
            int at = cups.IndexOf(_cup);
            if (at < 0 || cups.Count < 2) return null;
            return cups[(at + direction + cups.Count) % cups.Count];
        }

        /// <summary>The cup on the page (may be a locked or coming-soon one).</summary>
        public CupDefinition ShownCup => _cup;

        public bool CanSwipe => !_switching && !_celebrating && _wipeFrom == null && !_wipeStarting && _pageHome != null && !GameSession.Loading && Switchable().Count > 1;

        float PageWidth => ((RectTransform)transform).rect.width;

        // Every part of the page moves by the offset (times its depth factor) and shrinks a little.
        void SetPage(float offset, float scale = 1f, bool parallax = true)
        {
            if (page == null || _pageHome == null) return;
            for (int i = 0; i < page.Length; i++)
            {
                float k = parallax && pageParallax != null && i < pageParallax.Length ? pageParallax[i] : 1f;
                page[i].anchoredPosition = _pageHome[i] + new Vector2(offset * k, 0f);
                page[i].localScale = Vector3.one * scale;
            }
        }

        // Switching works like the pages of a phone's home screen: the old page is frozen as a picture (the
        // screen copied on the graphics card at the end of the frame, so a swipe starts at once), the
        // new cup is put on the page, and both move together under the finger: the old picture out to one
        // side (blurring and dimming away), the new page in from the other (coming in blurred and
        // sharpening). Let go, they glide on with the finger's speed and settle.

        CupDefinition _wipeFrom;
        int _wipeDirection;
        float _reveal, _revealVelocity;
        RenderTexture _snapshot, _newBlur;
        List<(UnityEngine.UI.Graphic graphic, Color from, Color to)> _fadePlan;

        void OnDestroy()
        {
            Garage.Changed -= PaintCar;
            foreach (var rt in new[] { _snapshot, _newBlur })
                if (rt != null) { rt.Release(); Destroy(rt); }
            foreach (var rt in _pictures.Values)
                if (rt != null) { rt.Release(); Destroy(rt); }
        }

        // A blurred picture of each cup's page as it last stood on screen (taken when you swipe away from it):
        // a cup slides in as its own page, blurred, sharpening as it arrives (until then its landscape stands in).
        readonly Dictionary<CupDefinition, RenderTexture> _pictures = new();

        // Pictures the page (the cup column left out for that frame) once the screen shows just it.
        // A captured screen comes upside down where the graphics card counts rows from the top.
        static Rect CaptureUv => SystemInfo.graphicsUVStartsAtTop ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);

        /// <summary>
        /// While dragging: the next cup's page comes in beside the old one, both following the finger 1:1;
        /// past the first or last cup the page only stretches a little.
        /// </summary>
        public void DragPage(float dx)
        {
            if (_switching) return;
            if (_wipeStarting) { _pendingDx = dx; return; } // the old page is being pictured this frame
            int direction = dx < 0f ? 1 : -1;
            if (_wipeFrom != null)
            {
                SetWipe(Mathf.Max(0f, -_wipeDirection * dx));
                return;
            }
            var next = Neighbour(direction);
            if (next != null && Mathf.Abs(dx) > 6f)
            {
                _pendingDx = dx;
                BeginWipe(next, direction);
                return;
            }
            if (next != null) return;
            float w = PageWidth;
            float offset = Mathf.Sign(dx) * w * 0.12f * (1f - Mathf.Exp(-Mathf.Abs(dx) / (w * 0.25f)));
            SetPage(offset, 1f);
        }

        /// <summary>
        /// Let go: the pages glide on with the finger's speed. Flicked towards the new cup, or past half way
        /// once the glide is counted in, it settles on the new cup; otherwise back on the old one.
        /// </summary>
        public void ReleasePage(float dx, float velocity)
        {
            if (_switching) return;
            if (_wipeStarting) { _pendingDx = dx; _pendingRelease = velocity; return; }
            if (_wipeFrom == null)
            {
                _spring = StartCoroutine(SpringBack(dx));
                return;
            }
            float speed = -_wipeDirection * velocity; // how fast the new cup was coming in
            float projected = _reveal + speed * 0.15f;
            bool go = speed > 500f || speed > -500f && projected > PageWidth * 0.5f;
            _revealVelocity = speed;
            _finishing = StartCoroutine(FinishWipe(go));
        }

        /// <summary>Slides <paramref name="cup"/> in (from the cup column).</summary>
        public void SwitchTo(CupDefinition cup)
        {
            if (_celebrating || _wipeStarting || cup == null || cup == _cup) return;
            if (_switching || _wipeFrom != null) HurrySwitch();
            if (_switching || _wipeFrom != null) return;
            var cups = Switchable();
            int direction = cups.IndexOf(cup) >= cups.IndexOf(_cup) ? 1 : -1;
            _pendingDx = 0f;
            _pendingGo = true;
            BeginWipe(cup, direction);
        }

        System.Collections.IEnumerator SpringBack(float from)
        {
            _springing = true;
            _switching = true;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
            {
                SetPage(Mathf.Lerp(from, 0f, 1f - Mathf.Pow(1f - t, 3f)));
                yield return null;
            }
            SetPage(0f);
            _switching = false;
            _springing = false;
        }

        Coroutine _finishing;
        bool _finishGo, _springing;

        /// <summary>
        /// A new swipe while the last switch is still gliding: that switch jumps to its end at once, so the
        /// next one starts straight away (swiping quickly through the cups, like a phone's home screen).
        /// </summary>
        public bool HurrySwitch()
        {
            if (_celebrating || _wipeStarting) return false;
            if (_springing)
            {
                StopAllSpring();
                return true;
            }
            if (_finishing == null || _wipeFrom == null) return false;
            StopCoroutine(_finishing);
            _finishing = null;
            SetWipe(_finishGo ? PageWidth : 0f);
            CompleteWipe(_finishGo);
            return true;
        }

        Coroutine _spring;

        void StopAllSpring()
        {
            if (_spring != null) StopCoroutine(_spring);
            _spring = null;
            SetPage(0f);
            _switching = _springing = false;
        }

        bool _wipeStarting, _pendingGo;
        Color _incomingTint = Color.white;
        int _wipeFromSelected;
        float _pendingDx;
        float? _pendingRelease;

        void BeginWipe(CupDefinition next, int direction)
        {
            if (wipe == null || wipeImage == null || _wipeStarting) return;
            StartCoroutine(BeginWipeRoutine(next, direction));
        }

        // Pictures the current page as it is on screen (taken on the graphics card at the end of this frame;
        // the cup column in it slides away with the old page while the real one stays), then puts the next
        // cup on the page.
        System.Collections.IEnumerator BeginWipeRoutine(CupDefinition next, int direction)
        {
            _wipeStarting = true;
            _pendingRelease = null;
            OpenCups(); // the cup column unfolds and shows the switch
            _wipeTargetSlot = Switchable().IndexOf(next);
            _wipeCovering = 0f;
            _wipeDirection = direction;
            _reveal = _revealVelocity = 0f;
            SetPage(0f);
            yield return new WaitForEndOfFrame();

            int w = Screen.width, h = Screen.height;
            if (_snapshot == null || _snapshot.width != w || _snapshot.height != h)
            {
                if (_snapshot != null) { _snapshot.Release(); Destroy(_snapshot); }
                _snapshot = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "CupPage" };
                _snapshot.Create();
            }
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_snapshot);

            _wipeFrom = _cup;
            _wipeFromSelected = _selected;
            wipe.anchorMin = Vector2.zero; wipe.anchorMax = Vector2.one;
            wipe.offsetMin = wipe.offsetMax = Vector2.zero;
            var uv = CaptureUv;
            wipeImage.texture = _snapshot;
            wipeImage.uvRect = uv;
            // The old page blurs and dims away as the new cup comes in (and this is its picture from now on).
            _pictures.TryGetValue(_cup, out var oldPicture);
            UiBlur.BlurInto(_snapshot, ref oldPicture);
            _pictures[_cup] = oldPicture;
            if (wipeBlur != null) { wipeBlur.texture = oldPicture; wipeBlur.uvRect = uv; wipeBlur.gameObject.SetActive(true); }
            if (wipeDim != null) wipeDim.gameObject.SetActive(true);
            if (wipeEdge != null) wipeEdge.gameObject.SetActive(false);
            wipe.gameObject.SetActive(true);
            ShowCup(next, fadeAbove: true);
            // The new cup comes in blurred (its page as last seen, or else its landscape) and sharpens as it settles.
            if (incomingBlur != null)
            {
                bool shown = false;
                if (_pictures.TryGetValue(next, out var picture) && picture != null)
                {
                    incomingBlur.texture = picture;
                    incomingBlur.uvRect = uv;
                    shown = true;
                }
                else if (CupList.IsPlayable(next) && next.mapBackground != null)
                {
                    UiBlur.BlurInto(next.mapBackground.texture, ref _newBlur);
                    incomingBlur.texture = _newBlur;
                    incomingBlur.uvRect = new Rect(0f, 0f, 1f, 1f);
                    shown = true;
                }
                // Not seen yet and no landscape (coming soon): a veil in its own colour fades away instead.
                _incomingTint = shown ? Color.white : Camera.main != null ? Camera.main.backgroundColor : Color.grey;
                if (!shown) incomingBlur.texture = null;
                incomingBlur.gameObject.SetActive(true);
            }
            _wipeStarting = false;
            SetWipe(Mathf.Max(0f, -_wipeDirection * _pendingDx));
            if (_pendingGo)
            {
                _pendingGo = false;
                _revealVelocity = 0f;
                _finishing = StartCoroutine(FinishWipe(true));
            }
            else if (_pendingRelease.HasValue)
            {
                float velocity = _pendingRelease.Value;
                _pendingRelease = null;
                ReleasePage(_pendingDx, velocity);
            }
        }

        // <paramref name="reveal"/> canvas units of the new cup are in: the old picture has moved that far out
        // to one side, the new page sits right beside it.
        void SetWipe(float reveal)
        {
            float w = PageWidth, h = ((RectTransform)transform).rect.height;
            _reveal = Mathf.Clamp(reveal, 0f, w);
            _wipeCovering = _reveal / w;
            // The old picture (sharp, its blurred copy and a dark veil over it) moves out together.
            foreach (var g in new UnityEngine.UI.Graphic[] { wipeImage, wipeBlur, wipeDim })
            {
                if (g == null) continue;
                var picture = g.rectTransform;
                picture.anchorMin = picture.anchorMax = picture.pivot = new Vector2(0.5f, 0.5f);
                picture.sizeDelta = new Vector2(w, h);
                picture.localScale = Vector3.one;
                picture.anchoredPosition = new Vector2(-_wipeDirection * _reveal, 0f);
            }
            if (wipeBlur != null) wipeBlur.color = new Color(1f, 1f, 1f, Mathf.Clamp01(_wipeCovering * 2.2f));
            if (wipeDim != null) wipeDim.color = new Color(0f, 0f, 0f, 0.35f * _wipeCovering);
            float offset = _wipeDirection * (w - _reveal);
            SetPage(offset, 1f, parallax: false);
            if (incomingBlur != null && incomingBlur.gameObject.activeSelf)
            {
                // The blurred new page moves with it and fades out as it comes in: it sharpens.
                var blurRt = incomingBlur.rectTransform;
                blurRt.anchorMin = Vector2.zero; blurRt.anchorMax = Vector2.one;
                blurRt.offsetMin = blurRt.offsetMax = Vector2.zero;
                blurRt.anchoredPosition = new Vector2(offset, 0f);
                blurRt.localScale = Vector3.one;
                var tint = _incomingTint;
                tint.a = Mathf.Clamp01(1f - _wipeCovering * 1.15f);
                incomingBlur.color = tint;
            }
            if (_fadePlan != null)
                foreach (var (graphic, a, b) in _fadePlan) if (graphic != null) graphic.color = Color.Lerp(a, b, _wipeCovering);
        }

        // Glides to the new cup (or back to the old one) from the release speed, slowing smoothly to a stop
        // there without overshooting (a critically damped spring).
        System.Collections.IEnumerator FinishWipe(bool go)
        {
            _switching = true;
            _finishGo = go;
            const float omega = 15f;
            float target = go ? PageWidth : 0f;
            float x = _reveal, v = _revealVelocity;
            // Faster than the spring can take in without shooting past: capped, so it still eases in.
            float toward = Mathf.Sign(target - x);
            if (v * toward > omega * Mathf.Abs(target - x)) v = toward * omega * Mathf.Abs(target - x);
            while (Mathf.Abs(x - target) > 0.5f || Mathf.Abs(v) > 30f)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                float a = -omega * omega * (x - target) - 2f * omega * v;
                v += a * dt;
                x += v * dt;
                if ((x - target) * toward > 0f) break; // arrived
                SetWipe(x);
                yield return null;
            }
            SetWipe(target);
            _finishing = null;
            CompleteWipe(go);
        }

        // The end of a switch: the new cup (or the old one, if it went back) is the page, the picture is gone.
        void CompleteWipe(bool go)
        {
            if (_fadePlan != null) foreach (var (graphic, _, b) in _fadePlan) if (graphic != null) graphic.color = b;
            _fadePlan = null;
            if (!go)
            {
                // Swiped back: the old cup as it was, with the race that was selected.
                ShowCup(_wipeFrom, fadeAbove: false);
                if (CupList.IsPlayable(_wipeFrom) && _wipeFromSelected >= 0 && _wipeFromSelected < catalog.Count)
                {
                    _selected = _wipeFromSelected;
                    PlaceCar();
                }
            }
            wipe.gameObject.SetActive(false);
            wipeImage.texture = null;
            if (wipeBlur != null) wipeBlur.texture = null;
            if (incomingBlur != null) { incomingBlur.texture = null; incomingBlur.gameObject.SetActive(false); }
            SetPage(0f);
            bool open = CupList.IsPlayable(_cup);
            if (open) Refresh();
            _wipeFrom = null;
            _wipeTargetSlot = -1;
            _collapseAt = Time.unscaledTime + 0.9f; // folds back up after a moment
            _switching = false;
            // Races of this cup that opened since you last looked unlock now.
            if (go && open && _pendingRaces != null && _pendingRaces.Count > 0) StartCoroutine(Celebrate(_pendingRaces, false));
            _pendingRaces = null;
        }

        List<int> _pendingRaces;

        // A locked or coming-soon cup shows a big grey page instead of its map (its name stays a secret).
        void SetLockedView(bool locked, CupDefinition cup)
        {
            if (lockedPage == null) return;
            lockedPage.SetActive(locked);
            world.gameObject.SetActive(!locked);
            if (racePageParts != null) foreach (var part in racePageParts) if (part != null) part.SetActive(!locked);
            if (!locked || cup == null) return;
            bool soon = CupList.IsComingSoon(cup);
            lockedIcon.sprite = soon ? soonSprite : lockSprite;
            lockedTitle.text = soon ? "COMING SOON" : "LOCKED";
            chapterLabel.text = soon ? "COMING SOON" : "LOCKED CUP";
            if (lockedCaption != null) lockedCaption.text = soon ? "MORE CUPS ARE ON THE WAY" : "TO UNLOCK";
            lockedNeeds.SetActive(!soon);
            lockedStars.text = $"{Mathf.Min(ProgressStore.TotalStars, cup.starsRequired)}/{cup.starsRequired}";
            if (snow != null) snow.SetActive(false);
            if (ambience != null) ambience.Clear();
        }

        // Puts another cup on the page: its races, road, landscape, life and colours. Everything under the
        // frozen picture changes at once; with <paramref name="fadeAbove"/> what sits above it (the globe
        // button) blends over as the new cup slides in.
        void ShowCup(CupDefinition cup, bool fadeAbove)
        {
            bool open = CupList.IsPlayable(cup);
            var oldPalette = Theme.Current;
            var newPalette = open ? Theme.Of(cup.look) : Theme.Locked; // a locked page is all greys
            var plan = Theme.RecolorPlan(transform, oldPalette, newPalette);
            _fadePlan = fadeAbove ? new List<(UnityEngine.UI.Graphic, Color, Color)>() : null;
            foreach (var (graphic, a, b) in plan)
            {
                bool above = fadeAbove && fadeRoots != null && System.Array.Exists(fadeRoots, r => r != null && graphic.transform.IsChildOf(r));
                if (above) _fadePlan.Add((graphic, a, b));
                else graphic.color = b;
            }
            if (Camera.main != null && Theme.TryMap(Camera.main.backgroundColor, oldPalette, newPalette, out var sky)) Camera.main.backgroundColor = sky;
            Theme.Use(newPalette); // the screen is in these colours now (also the greys of a locked page)

            _cup = cup;
            SetLockedView(!open, cup);
            if (!open) return;
            CupList.Selected = cup;
            catalog = CatalogOf(cup);
            MapLayout.Current = MapLayout.Of(cup.look);
            ShowVehicle(cup.look);
            PaintCar();
            Theme.Use(newPalette);
            if (cup.mapBackground != null && background != null) background.sprite = cup.mapBackground;
            if (snow != null) snow.SetActive(cup.look == CupLook.Winter);
            if (ambience != null) ambience.Play(cup.look);
            foreach (var node in _nodes) if (node != null) Destroy(node.gameObject);
            _nodes.Clear();
            _pendingRaces = PrepareRaceUnlocks();
            _selected = 0; // a fresh cup always starts at its first race
            Build();
            Refresh();
        }

        // The cup column under the ONLINE button: a badge per cup. Folded it shows the current cup's badge; tapping
        // it unfolds the column downward with all cups and the ring on the current one.
        const float BadgeSpacing = 132f, BadgeTop = 66f, FoldedHeight = 132f;

        void BuildDots()
        {
            if (dots == null || badgeTemplate == null) return;
            badgeTemplate.gameObject.SetActive(false);
            var cups = Switchable();
            dots.gameObject.SetActive(cups.Count >= 2);
            if (cups.Count < 2) return;
            for (int i = 0; i < cups.Count; i++)
            {
                var badge = Instantiate(badgeTemplate, badgeTemplate.parent);
                badge.gameObject.SetActive(true);
                badge.anchoredPosition = new Vector2(0f, -BadgeTop - i * BadgeSpacing);
                int slot = i;
                badge.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OnBadge(slot));
                _badges.Add(badge);
            }
            if (barToggle != null) barToggle.onClick.AddListener(() => { if (_barOpen) CollapseCups(); else OpenCups(true); });
            _ringAt = cups.IndexOf(_cup);
            RefreshBadges();
            UpdateDots(false);
        }

        void OnBadge(int slot)
        {
            if (_switching || _celebrating || _wipeFrom != null) return;
            var cups = Switchable();
            if (!_barOpen) { OpenCups(true); return; }
            if (cups[slot] == _cup) { CollapseCups(); return; }
            SwitchTo(cups[slot]);
        }

        /// <summary>Unfolds the cup list; opened by hand it folds up again by itself after a while.</summary>
        public void OpenCups(bool byHand = false)
        {
            _barOpen = true;
            _collapseAt = byHand ? Time.unscaledTime + 10f : -1f;
        }

        public void CollapseCups()
        {
            if (_wipeFrom != null) return;
            _barOpen = false;
            _collapseAt = -1f;
        }

        // Pictures and colours of the badges (after an unlock they change).
        void RefreshBadges()
        {
            var cups = Switchable();
            for (int i = 0; i < _badges.Count && i < cups.Count; i++)
            {
                var cup = cups[i];
                bool soon = CupList.IsComingSoon(cup), locked = !soon && !CupList.IsUnlocked(cup);
                var icon = _badges[i].Find("Icon").GetComponent<UnityEngine.UI.Image>();
                icon.sprite = soon ? soonSprite : locked ? lockSprite : cup.icon;
                icon.rectTransform.sizeDelta = Vector2.one * (soon || locked ? 56f : 96f);
                icon.color = soon || locked ? Theme.StoneDark : Color.white;
                _badges[i].Find("Face").GetComponent<UnityEngine.UI.Image>().color = soon || locked ? Theme.Stone : Theme.Cream;
            }
        }

        // Springs: the column folding and unfolding, and the ring gliding to the current cup (or following a
        // swipe between two cups). Folded, the column scrolls so the current cup is the one showing.
        void UpdateDots(bool animate)
        {
            if (_badges.Count == 0) return;
            var cups = Switchable();
            float target = cups.IndexOf(_cup);
            if (_wipeFrom != null && _wipeTargetSlot >= 0) target = Mathf.Lerp(cups.IndexOf(_wipeFrom), _wipeTargetSlot, _wipeCovering);
            if (_collapseAt > 0f && Time.unscaledTime >= _collapseAt && !_switching) CollapseCups();
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float openTarget = _barOpen ? 1f : 0f;
            if (animate)
            {
                _ringVelocity += (target - _ringAt) * 260f * dt;
                _ringVelocity *= Mathf.Exp(-18f * dt);
                _ringAt += _ringVelocity * dt;
                _barOpenVelocity += (openTarget - _barOpenAt) * 220f * dt;
                _barOpenVelocity *= Mathf.Exp(-17f * dt);
                _barOpenAt += _barOpenVelocity * dt;
            }
            else { _ringAt = target; _ringVelocity = 0f; _barOpenAt = openTarget; _barOpenVelocity = 0f; }

            float open = _barOpenAt, open01 = Mathf.Clamp01(open);
            // Folded it shows the current cup's badge (the column scrolled to it); unfolded all of them.
            float full = BadgeTop * 2f + (_badges.Count - 1) * BadgeSpacing;
            dots.sizeDelta = new Vector2(dots.sizeDelta.x, Mathf.LerpUnclamped(FoldedHeight, full, open));
            if (barContent != null) barContent.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(_ringAt * BadgeSpacing, 0f, open));
            if (barRing != null)
            {
                barRing.anchoredPosition = new Vector2(0f, -BadgeTop - _ringAt * BadgeSpacing);
                barRing.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3f));
            }
            if (barChevron != null) barChevron.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-90f, 90f, open));
            if (barRing != null)
                foreach (var g in barRing.GetComponentsInChildren<UnityEngine.UI.Graphic>()) { var c = g.color; c.a = g.name == "Glow" ? 0.22f : 1f; g.color = c; }
            for (int i = 0; i < _badges.Count; i++)
            {
                float near = Mathf.Clamp01(1f - Mathf.Abs(_ringAt - i));
                _badges[i].localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, near);
                var group = _badges[i].GetComponent<CanvasGroup>();
                if (group != null) group.alpha = Mathf.Lerp(near, Mathf.Lerp(0.55f, 1f, near), open01);
            }
        }

        void Update() => UpdateDots(true);

        void LateUpdate() => FitWorld();

        // Scale the fixed-size map so it covers the screen, keeping its centre.
        // Returns true when the screen size changed.
        bool FitWorld()
        {
            var parent = (RectTransform)world.parent;
            Vector2 size = parent.rect.size;
            if (size == _lastParentSize) return false;
            _lastParentSize = size;
            float scale = Mathf.Max(size.x / MapLayout.Size.x, size.y / MapLayout.Size.y);
            world.localScale = Vector3.one * scale;
            return true;
        }

        // Back from a race: the race just played (if still open). Otherwise the furthest race you can enter.
        int InitialSelection()
        {
            var last = GameSession.CurrentLevel;
            int index = last != null ? catalog.IndexOf(last) : -1;
            if (index >= 0 && index < _hideFrom && ProgressStore.CanEnter(catalog, index)) return index;
            int open = 0;
            for (int i = 0; i < catalog.Count && i < _hideFrom; i++)
                if (ProgressStore.CanEnter(catalog, i)) open = i;
            return open;
        }

        int LatestVisible()
        {
            int latest = 0;
            for (int i = 0; i < catalog.Count; i++)
                if (ProgressStore.IsVisible(catalog, i)) latest = i;
            return latest;
        }

        void Build()
        {
            nodeTemplate.gameObject.SetActive(false);

            int stops = Mathf.Min(MapLayout.StopCount, catalog.Count + 1);
            for (int i = 0; i < stops; i++)
            {
                var node = Instantiate(nodeTemplate, world);
                node.name = i < catalog.Count ? $"Node{i + 1}" : "NodeSoon";
                var rect = (RectTransform)node.transform;
                rect.anchoredPosition = MapLayout.StopPosition(i);
                node.gameObject.SetActive(true);
                _nodes.Add(node);
            }
            PlaceCar();
        }

        // The car waits on the road just before the selected race; the road behind it is orange. Selecting
        // another race drives it there along the road (the orange line follows).
        float _carDistance = -1f;
        Coroutine _drive;

        // carOffset before the stop, or half way back to the stop before where they are closer.
        float CarTarget(int index)
        {
            float stop = MapLayout.StopDistance(index);
            float gap = index > 0 ? stop - MapLayout.StopDistance(index - 1) : stop;
            return Mathf.Max(0f, stop - Mathf.Min(carOffset, gap * 0.5f));
        }

        void PlaceCar()
        {
            if (_drive != null) { StopCoroutine(_drive); _drive = null; }
            BuildProgress();
            SetCar(CarTarget(_selected), 1f);
            car.SetAsLastSibling();
            foreach (var node in _nodes) node.transform.SetAsLastSibling(); // stops stay on top of the line and the car
        }

        void DriveTo(int index)
        {
            float to = CarTarget(index);
            if (_carDistance < 0f || _progress.Count == 0) { PlaceCar(); Arrived(); return; }
            if (_drive != null) StopCoroutine(_drive);
            _drive = StartCoroutine(Drive(_carDistance, to));
        }

        // Tapped a race on the map: its cards open once the car is there.
        bool _openOnArrival;

        void Arrived()
        {
            if (!_openOnArrival) return;
            _openOnArrival = false;
            ShowInfo();
        }

        // Drives along the road to the stop; back to an earlier race it reverses, still facing up the road.
        System.Collections.IEnumerator Drive(float from, float to)
        {
            float duration = Mathf.Clamp(Mathf.Abs(to - from) / 1500f, 0.3f, 0.95f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                SetCar(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)), 1f);
                yield return null;
            }
            SetCar(to, 1f);
            _drive = null;
            Arrived();
        }

        // Puts the car at a road distance, facing the way it is driving, and fills the orange line up to it.
        void SetCar(float distance, float direction)
        {
            _carDistance = distance;
            Vector2 at = MapLayout.PointAt(distance, out var tangent);
            if (direction < 0f) tangent = -tangent;
            car.anchoredPosition = at;
            car.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f);
            const float step = 10f;
            for (int i = 0; i < _progress.Count; i++)
            {
                float d0 = i * step;
                var piece = _progress[i];
                bool on = d0 < distance;
                if (piece.activeSelf != on) piece.SetActive(on);
                if (!on) continue;
                float d1 = Mathf.Min(distance, d0 + step);
                Vector2 a = MapLayout.PointAt(d0, out _), b = MapLayout.PointAt(d1, out _);
                Vector2 delta = b - a;
                var rect = (RectTransform)piece.transform;
                rect.anchoredPosition = (a + b) * 0.5f;
                rect.sizeDelta = new Vector2(delta.magnitude + 2f, progressWidth);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        // The orange line as short pieces along the whole road (shown up to the car).
        void BuildProgress()
        {
            if (_progress.Count > 0 && _progressRoad == MapLayout.Current) return;
            foreach (var piece in _progress) if (piece != null) Destroy(piece);
            _progress.Clear();
            _progressRoad = MapLayout.Current;
            const float step = 10f;
            for (float d = 0f; d < MapLayout.Length; d += step)
            {
                var piece = new GameObject("Progress", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rect = (RectTransform)piece.transform;
                rect.SetParent(world, false);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                var image = piece.GetComponent<UnityEngine.UI.Image>();
                image.color = Theme.Orange;
                image.raycastTarget = false;
                rect.SetSiblingIndex(1); // just above the map picture
                piece.SetActive(false);
                _progress.Add(piece);
            }
        }

        MapRoad _progressRoad;

        void Select(int index, bool openOnArrival = false)
        {
            _openOnArrival = openOnArrival;
            CollapseCups();
            if (_celebrating && _hideFrom != int.MaxValue) return; // not while races unlock
            bool changed = index != _selected;
            _selected = index;
            DriveTo(index);
            Refresh();
        }

        void Refresh()
        {
            WriteCounters();
            for (int i = 0; i < _nodes.Count; i++)
            {
                int index = i;
                if (i >= catalog.Count)
                {
                    _nodes[i].Bind("?", MapNodeState.Hidden, 0, 0, false, "MORE SOON", null);
                    continue;
                }
                var level = catalog[i];
                var state = !ProgressStore.IsVisible(catalog, i) ? MapNodeState.Hidden
                    : i >= _hideFrom || !ProgressStore.CanEnter(catalog, i) ? MapNodeState.Locked // still to unlock (with an animation)
                    : ProgressStore.HasWon(level) ? MapNodeState.Won
                    : MapNodeState.Open;
                _nodes[i].Bind((i + 1).ToString(), state, ProgressStore.StarsOf(level), level.starsRequired, i == _selected, null, () => OnStopTapped(index));
                var rival = RivalCharacter.For(level);
                _nodes[i].SetRival(rival, rival != null && (RivalProgress.HasMet(rival.name) || (CupList.DevUnlockAll && state != MapNodeState.Locked)), rival != null && RivalProgress.IsBeaten(rival.name));
            }
            ShowSelected();
        }

        // The cup's name over the map (the race cards show the races).
        void ShowSelected()
        {
            var cup = CupList.Selected;
            if (catalog.Count == 0)
            {
                chapterLabel.text = cup != null ? cup.displayName : "";
                return;
            }
            var level = catalog[_selected];
            int dot = level.chapter.IndexOf('·');
            chapterLabel.text = cup != null ? cup.displayName : (dot > 0 ? level.chapter.Substring(0, dot) : level.chapter).Trim();
        }

        // Tapping a race stop drives the car there and opens its card; the selected one opens at once.
        void OnStopTapped(int index)
        {
            if (_celebrating || (deck != null && deck.IsOpen)) return;
            if (index == _selected) ShowInfo();
            else Select(index, openOnArrival: true);
        }

        // The cup's races as cards, on the selected one (a race still to unlock with an animation is left out).
        void ShowInfo()
        {
            if (deck == null || catalog.Count == 0 || _celebrating || deck.IsOpen) return;
            if (MeetRival(_selected, ShowInfo)) return; // first their rival says hello
            var races = new List<RaceDeck.Entry>();
            for (int i = 0; i < catalog.Count && i < _hideFrom; i++)
                if (ProgressStore.IsVisible(catalog, i)) races.Add(new RaceDeck.Entry { level = catalog[i], canEnter = ProgressStore.CanEnter(catalog, i) });
            if (_selected >= races.Count) return;
            deck.Open(races, _selected, _cup, i => i < _nodes.Count ? (RectTransform)_nodes[i].transform : null,
                i => { if (i != _selected) Select(i); }, StartRace);
        }

        // The race's rival, met for the first time now that it is open: their meeting, then (done). False if there is none.
        bool MeetRival(int index, System.Action done)
        {
            if (meeting == null || meeting.IsOpen || index < 0 || index >= catalog.Count || !ProgressStore.CanEnter(catalog, index)) return false;
            var rival = RivalCharacter.For(catalog[index]);
            if (rival == null || RivalProgress.HasMet(rival.name)) return false;
            meeting.Show(rival, () =>
            {
                RivalProgress.MarkMet(rival.name);
                Refresh();
                done?.Invoke();
            });
            return true;
        }

        void StartRace(int index)
        {
            if (index < 0 || index >= catalog.Count || !ProgressStore.CanEnter(catalog, index) || index >= _hideFrom || _celebrating || GameSession.Loading) return;
            GameSession.StartRace(catalog[index]);
        }
    }
}
