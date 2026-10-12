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
            _selected = InitialSelection();
            Build();
            Refresh();
            FitWorld();
            if (page != null)
            {
                _pageHome = new Vector2[page.Length];
                for (int i = 0; i < page.Length; i++) _pageHome[i] = page[i].anchoredPosition;
            }
            BuildDots();
        }

        // ------------------------------------------------------------------ switching cups

        /// <summary>The cups you can switch between on the map (unlocked, with a map to show).</summary>
        static List<CupDefinition> Switchable()
        {
            var list = new List<CupDefinition>();
            if (CupList.Instance != null)
                foreach (var cup in CupList.Instance.cups) if (CupList.IsPlayable(cup)) list.Add(cup);
            return list;
        }

        CupDefinition Neighbour(int direction)
        {
            var cups = Switchable();
            int i = cups.IndexOf(_cup) + direction;
            return i >= 0 && i < cups.Count && cups.IndexOf(_cup) >= 0 ? cups[i] : null;
        }

        public bool CanSwipe => !_switching && _wipeFrom == null && _pageHome != null && !GameSession.Loading && Switchable().Count > 1;

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
        Texture2D _snapshot;
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
            if (_switching || _wipeFrom != null || cup == null || cup == _cup || !CupList.IsPlayable(cup)) return;
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
            wipe.gameObject.SetActive(true);
            SetWipe(0f);
            ShowCup(next, fadeAbove: true);
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
            var image = wipeImage.rectTransform;
            image.anchorMin = image.anchorMax = image.pivot = new Vector2(side, 0.5f);
            image.anchoredPosition = Vector2.zero;
            image.sizeDelta = new Vector2(w, h);
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
            // The new page glides in a little as it is uncovered (it seems to slide in on top).
            SetPage(_wipeDirection * covered * 0.25f);
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
            if (_snapshot != null) { Destroy(_snapshot); _snapshot = null; }
            SetPage(0f);
            Refresh();
            _wipeFrom = null;
            _wipeReady = false;
            _switching = false;
        }

        // Puts another cup on the page: its races, road, landscape, life and colours. Everything under the
        // frozen picture changes at once; with <paramref name="fadeAbove"/> what sits above it (the globe
        // button) blends over as the new cup slides in.
        void ShowCup(CupDefinition cup, bool fadeAbove)
        {
            var oldPalette = Theme.Current;
            var newPalette = Theme.Of(cup.look);
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
            CupList.Selected = cup;
            catalog = CatalogOf(cup);
            MapLayout.Current = MapLayout.Of(cup.look);
            Theme.Use(newPalette);
            if (cup.mapBackground != null && background != null) background.sprite = cup.mapBackground;
            if (snow != null) snow.SetActive(cup.look == CupLook.Winter);
            if (ambience != null) ambience.Play(cup.look);
            foreach (var node in _nodes) if (node != null) Destroy(node.gameObject);
            _nodes.Clear();
            _selected = InitialSelection();
            Build();
            Refresh();
            UpdateDots(false);
        }

        void BuildDots()
        {
            if (dots == null || dotTemplate == null) return;
            dotTemplate.gameObject.SetActive(false);
            var cups = Switchable();
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
            if (index >= 0 && ProgressStore.CanEnter(catalog, index)) return index;
            int open = 0;
            for (int i = 0; i < catalog.Count; i++)
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
                    : ProgressStore.HasWon(level) ? MapNodeState.Won
                    : ProgressStore.CanEnter(catalog, i) ? MapNodeState.Open
                    : MapNodeState.Locked;
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
                if (infoButton != null) infoButton.interactable = false;
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

            bool canEnter = ProgressStore.CanEnter(catalog, _selected);
            startButton.interactable = canEnter;
            startFace.color = canEnter ? Theme.Orange : Theme.Stone;
            startShadow.color = canEnter ? Theme.OrangeDeep : Theme.StoneDark;
            startLabel.text = canEnter ? "START" : "LOCKED";
            startLabel.color = canEnter ? Theme.White : Theme.StoneDark;
            startRing.SetActive(canEnter);
        }

        void ShowInfo()
        {
            if (infoWindow == null || catalog.Count == 0) return;
            System.Action previous = _selected > 0 ? () => { Select(_selected - 1); ShowInfo(); } : null;
            System.Action next = _selected < catalog.Count - 1 ? () => { Select(_selected + 1); ShowInfo(); } : null;
            infoWindow.Show(catalog[_selected], ProgressStore.CanEnter(catalog, _selected), StartSelected, previous, next);
        }

        void StartSelected()
        {
            if (catalog.Count == 0 || !ProgressStore.CanEnter(catalog, _selected) || GameSession.Loading) return;
            startLabel.text = "LOADING";
            startLabel.fontSize *= 0.7f;
            startRing.SetActive(false);
            GameSession.StartRace(catalog[_selected]);
        }
    }
}
