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
        [SerializeField] TMP_Text totalStars;

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
        readonly List<GameObject> _progress = new();
        int _selected;
        Vector2 _lastParentSize;

        void Start()
        {
            startButton.onClick.AddListener(StartSelected);
            _selected = InitialSelection();
            Build();
            Refresh();
            FitWorld();
        }

        void LateUpdate() => FitWorld();

        // Scale the fixed-size map so it covers the screen, keeping its centre.
        void FitWorld()
        {
            var parent = (RectTransform)world.parent;
            Vector2 size = parent.rect.size;
            if (size == _lastParentSize) return;
            _lastParentSize = size;
            float scale = Mathf.Max(size.x / MapLayout.Size.x, size.y / MapLayout.Size.y);
            world.localScale = Vector3.one * scale;
        }

        // On game start race 1 is selected; back from a race, the race just played.
        int InitialSelection()
        {
            var last = GameSession.CurrentLevel;
            int index = last != null ? catalog.IndexOf(last) : -1;
            return index >= 0 && ProgressStore.IsVisible(catalog, index) ? index : 0;
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
            var level = catalog[_selected];
            // Just the cup name; the map shows which race of it this is.
            int dot = level.chapter.IndexOf('·');
            chapterLabel.text = (dot > 0 ? level.chapter.Substring(0, dot) : level.chapter).Trim();
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

        void StartSelected()
        {
            if (!ProgressStore.CanEnter(catalog, _selected) || GameSession.Loading) return;
            startLabel.text = "LOADING";
            startLabel.fontSize *= 0.7f;
            startRing.SetActive(false);
            GameSession.StartRace(catalog[_selected]);
        }
    }
}
