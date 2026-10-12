using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The map (start screen): a painted alpine valley with the mountain road of <see cref="MapLayout"/>.
    /// The races sit on the road, followed by a "more soon" stop. The road you have opened up is drawn
    /// in orange up to your car. Tapping a race selects it; its details show at the top and START at
    /// the bottom enters it.
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
        [Tooltip("Opens the race info window for the selected race.")]
        [SerializeField] UnityEngine.UI.Button infoButton;
        [SerializeField] RaceInfoWindow infoWindow;
        [Header("Switching cups")]
        [Tooltip("What slides when switching cups (the map, the race header, the cup name, START), with how much faster each moves (depth).")]
        [SerializeField] RectTransform[] page;
        [SerializeField] float[] pageParallax;
        [Tooltip("Page dots under the cup name, one per cup you can switch to.")]
        [SerializeField] RectTransform dots;
        [SerializeField] UnityEngine.UI.Image dotTemplate;
        [Tooltip("Frozen picture of the old page while the next cup slides in over it (a clipping rect, its picture and the shadow at its edge).")]
        [SerializeField] RectTransform wipe;
        [SerializeField] UnityEngine.UI.RawImage wipeImage;
        [SerializeField] UnityEngine.UI.Image wipeEdge;
        [Tooltip("Panels above the frozen picture (the globe button): their colours blend over as the new cup comes in.")]
        [SerializeField] Transform[] fadeRoots;
        [Tooltip("Share of the screen width a drag must pass to switch cup.")]
        [SerializeField, Range(0.05f, 0.6f)] float switchDistance = 0.2f;
        [SerializeField, Min(0.05f)] float slideIn = 0.45f;
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
        [SerializeField] TMP_Text lockedTrophies;
        [Tooltip("Parts of the page hidden on a locked cup (the race header, START).")]
        [SerializeField] GameObject[] racePageParts;

        [Header("Unlocks")]
        [SerializeField] CupUnlockCelebration celebration;
        [SerializeField] Sprite sparkle;

        [Tooltip("Snowfall over the map, shown on the winter map.")]
        [SerializeField] GameObject snow;
        [SerializeField] TMP_Text totalStars;
        [Tooltip("Races won out of all races.")]
        [SerializeField] TMP_Text totalTrophies;

        [Header("Selected race")]
        [SerializeField] TMP_Text chapterLabel;
        [SerializeField] TMP_Text titleLabel;
        [Tooltip("Your best total time for the selected race.")]
        [SerializeField] TMP_Text bestChip;
        [Tooltip("Your best lap on the selected race's track.")]
        [SerializeField] TMP_Text bestLapLabel;
        [Tooltip("Times for 1, 2 and 3 stars.")]
        [SerializeField] TMP_Text[] starTimes;
        [Tooltip("The three star-time groups (star icons + time): grey until your best time beats them.")]
        [SerializeField] RectTransform[] starGroups;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFace;
        [SerializeField] UnityEngine.UI.Image startShadow;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] GameObject startRing;

        [Header("Road progress")]
        [SerializeField, Min(1f)] float progressWidth = 22f;
        [Tooltip("How far before its race stop the car waits (canvas units along the road).")]
        [SerializeField] float carOffset = 95f;

        readonly List<MapNode> _nodes = new();
        readonly List<UnityEngine.UI.Image> _dots = new();
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
        }

        static LevelCatalog CatalogOf(CupDefinition cup) =>
            cup.HasRaces ? cup.races : ScriptableObject.CreateInstance<LevelCatalog>(); // empty: a cup still in the making

        // The cup's map: its road and landscape, and its colours on every image and text of the screen
        // (built in the Valley colours). Layout, fonts and shapes stay the same.
        void ApplyLook(CupLook look, Sprite landscape)
        {
            MapLayout.Current = MapLayout.Of(look);
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
            startButton.onClick.AddListener(StartSelected);
            if (infoButton != null) infoButton.onClick.AddListener(ShowInfo);
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
            StartCoroutine(Celebrate(newRaces, true));
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
            if (newest != null && newest != _cup) SwitchTo(newest);
        }

        // ------------------------------------------------------------------ switching cups

        /// <summary>The cups you can page through on the map: all of them (locked and coming-soon ones show their own page).</summary>
        static List<CupDefinition> Switchable() => CupList.Instance != null ? new List<CupDefinition>(CupList.Instance.cups) : new List<CupDefinition>();

        CupDefinition Neighbour(int direction)
        {
            var cups = Switchable();
            int i = cups.IndexOf(_cup) + direction;
            return i >= 0 && i < cups.Count && cups.IndexOf(_cup) >= 0 ? cups[i] : null;
        }

        /// <summary>The cup on the page (may be a locked or coming-soon one).</summary>
        public CupDefinition ShownCup => _cup;

        public bool CanSwipe => !_switching && !_celebrating && _wipeFrom == null && _pageHome != null && !GameSession.Loading && Switchable().Count > 1;

        float PageWidth => ((RectTransform)transform).rect.width;

        // Every part of the page moves by the offset (times its depth factor) and shrinks a little.
        void SetPage(float offset, float scale = 1f)
        {
            if (page == null || _pageHome == null) return;
            for (int i = 0; i < page.Length; i++)
            {
                float k = pageParallax != null && i < pageParallax.Length ? pageParallax[i] : 1f;
                page[i].anchoredPosition = _pageHome[i] + new Vector2(offset * k, 0f);
                page[i].localScale = Vector3.one * scale;
            }
        }

        // Switching works like laying a new card over the old one: the old page is frozen as a picture
        // (the "wipe"), the new cup is put on the page underneath, and the picture is cut back from the
        // side the new cup comes from, so the new cup seems to slide in over the old one, which stays put.

        CupDefinition _wipeFrom;
        int _wipeDirection;
        bool _wipeReady, _wipeStarting;
        float _reveal;
        float? _pendingRelease;
        Texture2D _snapshot, _blurred, _incoming;

        // Renders the page as it is now into a texture without showing anything on screen: the canvas is
        // drawn once by a hidden camera (the frozen picture left out), then goes back to normal.
        Texture2D RenderPage()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return null;
            int w = Screen.width, h = Screen.height;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var go = new GameObject("PageCamera");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : Color.black;
            cam.cullingMask = 1 << canvas.gameObject.layer;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f;
            cam.targetTexture = rt;
            bool wipeShown = wipe.gameObject.activeSelf, blurShown = incomingBlur != null && incomingBlur.gameObject.activeSelf;
            wipe.gameObject.SetActive(false);
            if (incomingBlur != null) incomingBlur.gameObject.SetActive(false);
            var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            Canvas.ForceUpdateCanvases();
            var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(cam, request)) UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam, request);
            else cam.Render();
            canvas.renderMode = mode;
            canvas.worldCamera = null;
            wipe.gameObject.SetActive(wipeShown);
            if (incomingBlur != null) incomingBlur.gameObject.SetActive(blurShown);
            Canvas.ForceUpdateCanvases();

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tex.Apply(false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(go);
            return tex;
        }

        // A soft, blurred copy: block-averaged down to 1/factor size, box-blurred twice, shown smoothed.
        static Texture2D Blurred(Texture2D source, int factor)
        {
            int w = Mathf.Max(1, source.width / factor), h = Mathf.Max(1, source.height / factor);
            var src = source.GetPixels32();
            var small = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float r = 0f, g = 0f, b = 0f; int n = 0;
                    for (int dy = 0; dy < factor; dy += 2)
                        for (int dx = 0; dx < factor; dx += 2)
                        {
                            int sx = x * factor + dx, sy = y * factor + dy;
                            if (sx >= source.width || sy >= source.height) continue;
                            var c = src[sy * source.width + sx];
                            r += c.r; g += c.g; b += c.b; n++;
                        }
                    small[y * w + x] = new Color(r / n / 255f, g / n / 255f, b / n / 255f, 1f);
                }
            for (int pass = 0; pass < 2; pass++) small = Box(small, w, h, 2);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, source.isDataSRGB == false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(small);
            tex.Apply(false);
            return tex;
        }

        static Color[] Box(Color[] px, int w, int h, int r)
        {
            var tmp = new Color[px.Length];
            var outp = new Color[px.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color sum = Color.clear; int n = 0;
                    for (int k = -r; k <= r; k++) { int xx = Mathf.Clamp(x + k, 0, w - 1); sum += px[y * w + xx]; n++; }
                    tmp[y * w + x] = sum / n;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color sum = Color.clear; int n = 0;
                    for (int k = -r; k <= r; k++) { int yy = Mathf.Clamp(y + k, 0, h - 1); sum += tmp[yy * w + x]; n++; }
                    outp[y * w + x] = sum / n;
                }
            return outp;
        }
        List<(UnityEngine.UI.Graphic graphic, Color from, Color to)> _fadePlan;

        /// <summary>
        /// While dragging: towards another cup it slides in over the current one under the finger; past
        /// the first or last cup the page only stretches a little.
        /// </summary>
        public void DragPage(float dx)
        {
            if (_switching) return;
            int direction = dx < 0f ? 1 : -1;
            if (_wipeFrom != null)
            {
                if (_wipeReady) SetWipe(Mathf.Max(0f, -_wipeDirection * dx));
                return;
            }
            var next = Neighbour(direction);
            if (next != null && Mathf.Abs(dx) > 8f)
            {
                StartCoroutine(BeginWipe(next, direction));
                return;
            }
            if (next != null) return;
            float w = PageWidth;
            float offset = Mathf.Sign(dx) * w * 0.12f * (1f - Mathf.Exp(-Mathf.Abs(dx) / (w * 0.25f)));
            SetPage(offset, 1f);
        }

        /// <summary>Released: far or fast enough, the new cup slides fully over; otherwise it slides back off.</summary>
        public void ReleasePage(float dx, float velocity)
        {
            if (_switching) return;
            if (_wipeFrom == null)
            {
                StartCoroutine(SpringBack(dx));
                return;
            }
            float reveal = Mathf.Max(0f, -_wipeDirection * dx);
            bool flick = Mathf.Abs(velocity) > 1600f && Mathf.Sign(velocity) == -_wipeDirection;
            float decision = reveal > PageWidth * switchDistance || flick ? 1f : 0f;
            if (!_wipeReady) { _pendingRelease = decision; return; }
            StartCoroutine(FinishWipe(decision > 0.5f, slideIn));
        }

        /// <summary>Slides <paramref name="cup"/> in over the current one (from the cup menu).</summary>
        public void SwitchTo(CupDefinition cup)
        {
            if (_switching || _celebrating || _wipeFrom != null || cup == null || cup == _cup) return;
            var cups = Switchable();
            int direction = cups.IndexOf(cup) >= cups.IndexOf(_cup) ? 1 : -1;
            _pendingRelease = 1f;
            StartCoroutine(BeginWipe(cup, direction));
        }

        System.Collections.IEnumerator SpringBack(float from)
        {
            _switching = true;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                SetPage(Mathf.Lerp(from, 0f, 1f - Mathf.Pow(1f - t, 3f)));
                yield return null;
            }
            SetPage(0f);
            _switching = false;
        }

        // Freezes the current page as a picture over everything, then puts the next cup underneath.
        System.Collections.IEnumerator BeginWipe(CupDefinition next, int direction)
        {
            if (wipe == null || wipeImage == null) yield break;
            _wipeFrom = _cup;
            _wipeDirection = direction;
            _wipeReady = false;
            SetPage(0f);
            yield return new WaitForEndOfFrame();
            if (_snapshot != null) Destroy(_snapshot);
            // The capture holds sRGB colours; copied into an sRGB texture it shows exactly as on screen
            // (as captured it would be gamma-corrected twice and look washed out).
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            _snapshot = new Texture2D(capture.width, capture.height, TextureFormat.RGBA32, false, QualitySettings.activeColorSpace == ColorSpace.Gamma);
            _snapshot.SetPixels32(capture.GetPixels32());
            _snapshot.Apply(false);
            Destroy(capture);
            wipeImage.texture = _snapshot;
            if (wipeBlur != null) wipeBlur.texture = _blurred = Blurred(_snapshot, 10);
            wipe.gameObject.SetActive(true);
            SetWipe(0f);
            ShowCup(next, fadeAbove: true);
            // A blurred picture of the new cup, which sharpens as it slides in.
            if (incomingBlur != null)
            {
                SetPage(0f);
                var page = RenderPage();
                if (page != null)
                {
                    if (_incoming != null) Destroy(_incoming);
                    _incoming = Blurred(page, 10);
                    Destroy(page);
                    incomingBlur.texture = _incoming;
                    incomingBlur.gameObject.SetActive(true);
                }
            }
            _wipeReady = true;
            SetWipe(_reveal);
            if (_pendingRelease.HasValue)
            {
                bool go = _pendingRelease.Value > 0.5f;
                _pendingRelease = null;
                StartCoroutine(FinishWipe(go, slideIn * 1.2f));
            }
        }

        // Shows <paramref name="reveal"/> canvas units of the new cup from the side it comes from.
        void SetWipe(float reveal)
        {
            float w = PageWidth, h = ((RectTransform)transform).rect.height;
            _reveal = Mathf.Clamp(reveal, 0f, w);
            float covered = w - _reveal;
            bool fromRight = _wipeDirection > 0;
            float side = fromRight ? 0f : 1f; // the old picture keeps the side opposite to where the new cup comes from
            wipe.anchorMin = new Vector2(side, 0f);
            wipe.anchorMax = new Vector2(side, 1f);
            wipe.pivot = new Vector2(side, 0.5f);
            wipe.anchoredPosition = Vector2.zero;
            wipe.sizeDelta = new Vector2(covered, 0f);
            // The old page blurs, dims and sinks back a little as the new cup covers it.
            float covering = _reveal / w;
            foreach (var g in new UnityEngine.UI.Graphic[] { wipeImage, wipeBlur, wipeDim })
            {
                if (g == null) continue;
                var image = g.rectTransform;
                image.anchorMin = image.anchorMax = image.pivot = new Vector2(side, 0.5f);
                image.anchoredPosition = Vector2.zero;
                image.sizeDelta = new Vector2(w, h);
                image.localScale = Vector3.one; // the old page stays exactly in place (no gaps at its edges)
            }
            if (wipeBlur != null) wipeBlur.color = new Color(1f, 1f, 1f, Mathf.Clamp01(covering * 2.2f));
            if (wipeDim != null) wipeDim.color = new Color(0f, 0f, 0f, 0.35f * covering);
            if (wipeEdge != null)
            {
                // The new cup's edge casts a soft shadow onto the old page.
                var edge = wipeEdge.rectTransform;
                edge.anchorMin = edge.anchorMax = edge.pivot = new Vector2(1f - side, 0.5f);
                edge.anchoredPosition = Vector2.zero;
                edge.sizeDelta = new Vector2(70f, h);
                edge.localScale = new Vector3(fromRight ? 1f : -1f, 1f, 1f);
                wipeEdge.color = new Color(0f, 0f, 0f, 0.35f * Mathf.Clamp01(_reveal / 40f) * Mathf.Clamp01(covered / 40f));
            }
            // The new page glides in and settles from a slight zoom as it is uncovered (it seems to slide in on top).
            float offset = _wipeDirection * covered * 0.25f, zoom = 1f + 0.05f * covered / w;
            SetPage(offset, zoom);
            if (incomingBlur != null && incomingBlur.gameObject.activeSelf)
            {
                // The blurred new page moves with it and fades out as it comes in: it sharpens.
                var blurRt = incomingBlur.rectTransform;
                blurRt.anchoredPosition = new Vector2(offset, 0f);
                blurRt.localScale = Vector3.one * zoom;
                incomingBlur.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - covering * 1.15f));
            }
            if (_fadePlan != null)
            {
                float c = _reveal / w;
                foreach (var (graphic, a, b) in _fadePlan) if (graphic != null) graphic.color = Color.Lerp(a, b, c);
            }
        }

        // Slides the new cup fully over the old one, or back off it (then the old cup is put back).
        System.Collections.IEnumerator FinishWipe(bool go, float duration)
        {
            _switching = true;
            float from = _reveal, to = go ? PageWidth : 0f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                float e = 1f - Mathf.Pow(1f - t, 3f);
                SetWipe(Mathf.Lerp(from, to, e));
                yield return null;
            }
            SetWipe(to);
            if (_fadePlan != null) foreach (var (graphic, _, b) in _fadePlan) if (graphic != null) graphic.color = b;
            _fadePlan = null;
            if (!go) ShowCup(_wipeFrom, fadeAbove: false);
            wipe.gameObject.SetActive(false);
            wipeImage.texture = null;
            if (wipeBlur != null) wipeBlur.texture = null;
            if (incomingBlur != null) { incomingBlur.texture = null; incomingBlur.gameObject.SetActive(false); }
            if (_incoming != null) { Destroy(_incoming); _incoming = null; }
            if (_snapshot != null) { Destroy(_snapshot); _snapshot = null; }
            if (_blurred != null) { Destroy(_blurred); _blurred = null; }
            SetPage(0f);
            bool open = CupList.IsPlayable(_cup);
            if (open) Refresh();
            _wipeFrom = null;
            _wipeReady = false;
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
            lockedTrophies.text = $"{Mathf.Min(ProgressStore.TotalWins, cup.trophiesRequired)}/{cup.trophiesRequired}";
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
            var newPalette = open ? Theme.Of(cup.look) : oldPalette; // a locked page keeps the colours
            var plan = Theme.RecolorPlan(transform, oldPalette, newPalette);
            _fadePlan = fadeAbove ? new List<(UnityEngine.UI.Graphic, Color, Color)>() : null;
            foreach (var (graphic, a, b) in plan)
            {
                bool above = fadeAbove && fadeRoots != null && System.Array.Exists(fadeRoots, r => r != null && graphic.transform.IsChildOf(r));
                if (above) _fadePlan.Add((graphic, a, b));
                else graphic.color = b;
            }
            if (Camera.main != null && Theme.TryMap(Camera.main.backgroundColor, oldPalette, newPalette, out var sky)) Camera.main.backgroundColor = sky;

            _cup = cup;
            SetLockedView(!open, cup);
            if (!open)
            {
                UpdateDots(false);
                return;
            }
            CupList.Selected = cup;
            catalog = CatalogOf(cup);
            MapLayout.Current = MapLayout.Of(cup.look);
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
            UpdateDots(false);
        }

        void BuildDots()
        {
            if (dots == null || dotTemplate == null) return;
            dotTemplate.gameObject.SetActive(false);
            var cups = Switchable();
            dots.gameObject.SetActive(cups.Count >= 2); // one cup: nothing to page through
            if (cups.Count < 2) return;
            for (int i = 0; i < cups.Count; i++)
            {
                var dot = Instantiate(dotTemplate, dots);
                dot.gameObject.SetActive(true);
                _dots.Add(dot);
            }
            UpdateDots(true);
        }

        float[] _dotWidth;

        // The current cup's mark is a long amber pill, the others round cream dots; they ease between the
        // two. The dark plate behind them fits its marks.
        void UpdateDots(bool target)
        {
            if (_dots.Count == 0) return;
            _dotWidth ??= new float[_dots.Count];
            int current = Switchable().IndexOf(_cup);
            float size = dotTemplate.rectTransform.sizeDelta.y, gap = size * 0.6f;
            float total = 0f;
            for (int i = 0; i < _dots.Count; i++)
            {
                float want = i == current ? size * 2.6f : size;
                _dotWidth[i] = target && _dotWidth[i] > 0f ? Mathf.Lerp(_dotWidth[i], want, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime)) : want;
                total += _dotWidth[i] + (i > 0 ? gap : 0f);
            }
            dots.sizeDelta = new Vector2(total + size * 1.5f, size * 1.8f);
            float x = -total * 0.5f;
            for (int i = 0; i < _dots.Count; i++)
            {
                var rt = _dots[i].rectTransform;
                rt.sizeDelta = new Vector2(_dotWidth[i], size);
                rt.anchoredPosition = new Vector2(x + _dotWidth[i] * 0.5f, 0f);
                x += _dotWidth[i] + gap;
                _dots[i].color = i == current ? Theme.Amber : Theme.WithAlpha(Theme.Cream, 0.55f);
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

        // The car waits on the road just before the selected race; the road behind it is orange.
        void PlaceCar()
        {
            foreach (var piece in _progress) if (piece != null) Destroy(piece);
            _progress.Clear();
            float carDistance = Mathf.Max(0f, MapLayout.StopDistance(_selected) - carOffset);
            DrawProgress(carDistance);
            Vector2 carPos = MapLayout.PointAt(carDistance, out var tangent);
            car.anchoredPosition = carPos;
            car.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f);
            car.SetAsLastSibling();
            foreach (var node in _nodes) node.transform.SetAsLastSibling(); // stops stay on top of the line
        }

        void DrawProgress(float until)
        {
            const float step = 10f;
            for (float d = 0f; d < until; d += step)
            {
                Vector2 a = MapLayout.PointAt(d, out _), b = MapLayout.PointAt(Mathf.Min(until, d + step), out _);
                Vector2 delta = b - a;
                if (delta.sqrMagnitude < 0.01f) continue;
                var piece = new GameObject("Progress", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rect = (RectTransform)piece.transform;
                rect.SetParent(world, false);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.anchoredPosition = (a + b) * 0.5f;
                rect.sizeDelta = new Vector2(delta.magnitude + 2f, progressWidth);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                var image = piece.GetComponent<UnityEngine.UI.Image>();
                image.color = Theme.Orange;
                image.raycastTarget = false;
                rect.SetSiblingIndex(1); // just above the map picture
                _progress.Add(piece);
            }
        }

        void Select(int index)
        {
            if (_celebrating && _hideFrom != int.MaxValue) return; // not while races unlock
            _selected = index;
            PlaceCar();
            Refresh();
        }

        void Refresh()
        {
            totalStars.text = ProgressStore.TotalStars.ToString();
            if (totalTrophies != null)
            {
                int won = 0;
                for (int i = 0; i < catalog.Count; i++) if (ProgressStore.HasWon(catalog[i])) won++;
                totalTrophies.text = won.ToString();
            }
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
                _nodes[i].Bind((i + 1).ToString(), state, ProgressStore.StarsOf(level), level.starsRequired, i == _selected, null, () => Select(index));
            }
            ShowSelected();
        }

        void ShowSelected()
        {
            if (catalog.Count == 0)
            {
                // A cup without races yet (development).
                chapterLabel.text = CupList.Selected != null ? CupList.Selected.displayName : "";
                titleLabel.text = "COMING SOON";
                bestChip.text = bestLapLabel.text = "—";
                foreach (var t in starTimes) t.text = "-";
                startButton.interactable = false;
                startFace.color = Theme.Stone; startShadow.color = Theme.StoneDark;
                startLabel.text = "SOON"; startLabel.color = Theme.StoneDark;
                startRing.SetActive(false);
                SetInfoAvailable(false);
                return;
            }
            var level = catalog[_selected];
            // Just the cup name; the map shows which race of it this is.
            var cup = CupList.Selected;
            int dot = level.chapter.IndexOf('·');
            chapterLabel.text = cup != null ? cup.displayName : (dot > 0 ? level.chapter.Substring(0, dot) : level.chapter).Trim();
            titleLabel.text = level.displayName.ToUpperInvariant();
            var best = ProgressStore.BestTimeOf(level);
            bestChip.text = best.HasValue ? TimeFormat.Race(best.Value) : "—";
            var bestLap = level.track != null ? BestLapStore.Get(level.track.name) : null;
            bestLapLabel.text = bestLap.HasValue ? TimeFormat.Race(bestLap.Value) : "—";
            for (int i = 0; i < starTimes.Length; i++)
            {
                starTimes[i].text = i < level.starTimes.Length ? TimeFormat.Race(level.starTimes[i]) : "-";
                // Earned (your best beats it): yellow stars and a white time; otherwise grey.
                bool earned = best.HasValue && i < level.starTimes.Length && best.Value <= level.starTimes[i];
                starTimes[i].color = earned ? Theme.White : Theme.WithAlpha(Theme.Stone, 0.45f);
                if (starGroups != null && i < starGroups.Length && starGroups[i] != null)
                    foreach (var icon in starGroups[i].GetComponentsInChildren<UnityEngine.UI.Image>())
                        icon.color = earned ? Theme.Amber : Theme.WithAlpha(Theme.Stone, 0.35f);
            }

            bool canEnter = ProgressStore.CanEnter(catalog, _selected) && _selected < _hideFrom;
            SetInfoAvailable(canEnter); // no race info for a locked race
            startButton.interactable = canEnter;
            startFace.color = canEnter ? Theme.Orange : Theme.Stone;
            startShadow.color = canEnter ? Theme.OrangeDeep : Theme.StoneDark;
            startLabel.text = canEnter ? "START" : "LOCKED";
            startLabel.color = canEnter ? Theme.White : Theme.StoneDark;
            startRing.SetActive(canEnter);
        }

        // The info button greys out (like a locked START) when the selected race is locked.
        void SetInfoAvailable(bool available)
        {
            if (infoButton == null) return;
            infoButton.interactable = available;
            var face = infoButton.transform.Find("Face");
            if (face == null) return;
            face.GetComponent<UnityEngine.UI.Image>().color = available ? Theme.Ink : Theme.Stone;
            var icon = face.Find("Icon");
            if (icon != null) icon.GetComponent<UnityEngine.UI.Image>().color = available ? Theme.Cream : Theme.StoneDark;
            var shadow = infoButton.transform.Find("Shadow");
            if (shadow != null) shadow.GetComponent<UnityEngine.UI.Image>().color = available ? Theme.Ink * 0.6f + new Color(0, 0, 0, 0.4f) : Theme.StoneDark;
        }

        // The nearest race you can enter before / after the selected one (-1 if none).
        int OpenRace(int from, int step)
        {
            for (int i = from + step; i >= 0 && i < catalog.Count; i += step)
                if (ProgressStore.CanEnter(catalog, i)) return i;
            return -1;
        }

        void ShowInfo()
        {
            if (infoWindow == null || catalog.Count == 0 || !ProgressStore.CanEnter(catalog, _selected)) return;
            int before = OpenRace(_selected, -1), after = OpenRace(_selected, 1);
            System.Action previous = before >= 0 ? () => { Select(before); ShowInfo(); } : null;
            System.Action next = after >= 0 ? () => { Select(after); ShowInfo(); } : null;
            infoWindow.Show(catalog[_selected], ProgressStore.CanEnter(catalog, _selected), StartSelected, previous, next);
        }

        void StartSelected()
        {
            if (catalog.Count == 0 || !ProgressStore.CanEnter(catalog, _selected) || _selected >= _hideFrom || _celebrating || GameSession.Loading) return;
            startLabel.text = "LOADING";
            startLabel.fontSize *= 0.7f;
            startRing.SetActive(false);
            GameSession.StartRace(catalog[_selected]);
        }
    }
}
