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
        [Tooltip("Share of the screen width a drag must pass to switch cup.")]
        [SerializeField, Range(0.05f, 0.6f)] float switchDistance = 0.2f;
        [SerializeField, Min(0.05f)] float slideOut = 0.22f, slideIn = 0.5f;
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

        public bool CanSwipe => !_switching && _pageHome != null && !GameSession.Loading && Switchable().Count > 1;

        float PageWidth => ((RectTransform)transform).rect.width;

        // Every part of the page moves by the offset (times its depth factor) and shrinks a little.
        void SetPage(float offset, float scale = 1f)
        {
            if (page == null || _pageHome == null) return;
            for (int i = 0; i < page.Length; i++)
            {
                float k = pageParallax != null && i < pageParallax.Length ? pageParallax[i] : 1f;
                page[i].anchoredPosition = _pageHome[i] + new Vector2(offset * k, 0f);
                page[i].localScale = Vector3.one * (i == 0 ? 1f : scale); // the map itself stays full size (no empty edges)
            }
        }

        /// <summary>The page follows the finger; past the first or last cup it only stretches a little.</summary>
        public void DragPage(float dx)
        {
            if (_switching) return;
            bool open = Neighbour(dx < 0f ? 1 : -1) != null;
            float w = PageWidth;
            float offset = open ? dx : Mathf.Sign(dx) * w * 0.12f * (1f - Mathf.Exp(-Mathf.Abs(dx) / (w * 0.25f)));
            SetPage(offset, 1f - 0.05f * Mathf.Clamp01(Mathf.Abs(offset) / w));
        }

        /// <summary>Released: far or fast enough towards another cup switches to it, otherwise the page springs back.</summary>
        public void ReleasePage(float dx, float velocity)
        {
            if (_switching) return;
            int direction = dx < 0f ? 1 : -1;
            var next = Neighbour(direction);
            bool go = next != null && (Mathf.Abs(dx) > PageWidth * switchDistance || (Mathf.Abs(velocity) > 1600f && Mathf.Sign(velocity) == Mathf.Sign(dx)));
            if (go) StartCoroutine(Switch(next, direction, dx));
            else StartCoroutine(SpringBack(dx));
        }

        /// <summary>Slides over to <paramref name="cup"/> (from the cup menu).</summary>
        public void SwitchTo(CupDefinition cup)
        {
            if (_switching || cup == null || cup == _cup || !CupList.IsPlayable(cup)) return;
            var cups = Switchable();
            int direction = cups.IndexOf(cup) >= cups.IndexOf(_cup) ? 1 : -1;
            StartCoroutine(Switch(cup, direction, 0f));
        }

        System.Collections.IEnumerator SpringBack(float from)
        {
            _switching = true;
            float start = from;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float e = 1f - Mathf.Pow(1f - t, 3f);
                float offset = Mathf.Lerp(start, 0f, e);
                SetPage(offset, 1f - 0.05f * Mathf.Clamp01(Mathf.Abs(offset) / PageWidth));
                yield return null;
            }
            SetPage(0f);
            _switching = false;
        }

        System.Collections.IEnumerator Switch(CupDefinition cup, int direction, float from)
        {
            _switching = true;
            float w = PageWidth;
            var oldPalette = Theme.Current;
            var newPalette = Theme.Of(cup.look);
            Color oldSky = Camera.main != null ? Camera.main.backgroundColor : Color.clear;

            // Out: the old page speeds off to the side, shrinking a little.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / slideOut)
            {
                float e = t * t;
                SetPage(Mathf.Lerp(from, -direction * w * 1.15f, e), Mathf.Lerp(1f, 0.9f, e));
                yield return null;
            }

            // Swap the content while it is off screen; the colours of what stays (buttons, counters) morph.
            var plan = Theme.RecolorPlan(transform, oldPalette, newPalette);
            ShowCup(cup);
            Color newSky = oldSky;
            if (Camera.main != null) Theme.TryMap(oldSky, oldPalette, newPalette, out newSky);

            // In: the new page comes from the other side and settles with a little overshoot.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / slideIn)
            {
                float e = EaseOutBack(t);
                SetPage(Mathf.LerpUnclamped(direction * w * 1.15f, 0f, e), Mathf.LerpUnclamped(0.9f, 1f, e));
                float c = Mathf.SmoothStep(0f, 1f, t);
                foreach (var (graphic, a, b) in plan) if (graphic != null) graphic.color = Color.Lerp(a, b, c);
                if (Camera.main != null) Camera.main.backgroundColor = Color.Lerp(oldSky, newSky, c);
                yield return null;
            }
            foreach (var (graphic, a, b) in plan) if (graphic != null) graphic.color = b;
            if (Camera.main != null) Camera.main.backgroundColor = newSky;
            SetPage(0f);
            Refresh(); // exact colours for everything that depends on progress
            _switching = false;
        }

        static float EaseOutBack(float t)
        {
            const float s = 1.4f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((s + 1f) * t + s);
        }

        // Puts another cup on the page: its races, road, landscape, life and colours.
        void ShowCup(CupDefinition cup)
        {
            _cup = cup;
            CupList.Selected = cup;
            catalog = CatalogOf(cup);
            MapLayout.Current = MapLayout.Of(cup.look);
            Theme.Use(Theme.Of(cup.look));
            if (cup.mapBackground != null && background != null) background.sprite = cup.mapBackground;
            if (snow != null) snow.SetActive(cup.look == CupLook.Winter);
            if (ambience != null) ambience.Play(cup.look);
            foreach (var node in _nodes) if (node != null) Destroy(node.gameObject);
            _nodes.Clear();
            _selected = InitialSelection();
            Build();
            Refresh();
            UpdateDots(true);
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

        // The current cup's dot is a long pill, the others small dots; they ease between the two.
        void UpdateDots(bool target)
        {
            if (_dots.Count == 0) return;
            _dotWidth ??= new float[_dots.Count];
            int current = Switchable().IndexOf(_cup);
            float size = dotTemplate.rectTransform.sizeDelta.y, gap = size * 0.7f;
            float total = 0f;
            for (int i = 0; i < _dots.Count; i++)
            {
                float want = i == current ? size * 3f : size;
                _dotWidth[i] = target && _dotWidth[i] > 0f ? Mathf.Lerp(_dotWidth[i], want, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime)) : want;
                total += _dotWidth[i] + (i > 0 ? gap : 0f);
            }
            float x = -total * 0.5f;
            for (int i = 0; i < _dots.Count; i++)
            {
                var rt = _dots[i].rectTransform;
                rt.sizeDelta = new Vector2(_dotWidth[i], size);
                rt.anchoredPosition = new Vector2(x + _dotWidth[i] * 0.5f, 0f);
                x += _dotWidth[i] + gap;
                _dots[i].color = i == current ? Theme.Rust : Theme.WithAlpha(Theme.Ink, 0.3f);
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
            infoWindow.Show(catalog[_selected], ProgressStore.CanEnter(catalog, _selected), StartSelected);
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
