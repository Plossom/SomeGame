using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The level map (start screen): a vertical path of race nodes, first race at the bottom.
    /// Tapping a race selects it; its details show at the top and START at the bottom enters it.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        [SerializeField] LevelCatalog catalog;
        [SerializeField] UnityEngine.UI.ScrollRect scroll;
        [Tooltip("Scroll content; nodes and path pieces are created inside it.")]
        [SerializeField] RectTransform content;
        [Tooltip("Inactive node used as a template.")]
        [SerializeField] MapNode nodeTemplate;
        [SerializeField] TMP_Text totalStars;

        [Header("Selected race")]
        [SerializeField] TMP_Text chapterLabel;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text lapsChip;
        [SerializeField] TMP_Text rivalsChip;
        [SerializeField] TMP_Text bestChip;
        [Tooltip("Times for 1, 2 and 3 stars.")]
        [SerializeField] TMP_Text[] starTimes;
        [SerializeField] TMP_Text hint;
        [SerializeField] UnityEngine.UI.Button startButton;
        [SerializeField] UnityEngine.UI.Image startFill;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] GameObject startRing;

        [Header("Layout (canvas units)")]
        [SerializeField, Min(50f)] float nodeSpacing = 330f;
        [SerializeField] float sideSwing = 260f;
        [SerializeField] float bottomPadding = 520f;
        [SerializeField] float topPadding = 820f;
        [SerializeField, Min(1f)] float pathWidth = 34f;
        [SerializeField] Sprite pathSprite;
        [SerializeField] Sprite glowSprite;

        readonly List<MapNode> _nodes = new();
        int _selected;

        void Start()
        {
            startButton.onClick.AddListener(StartSelected);
            _selected = LatestVisible();
            Build();
            Refresh();
            ScrollTo(_selected);
        }

        Vector2 NodePosition(int index) =>
            new(Mathf.Sin(index * 1.25f + 0.6f) * sideSwing, bottomPadding + index * nodeSpacing);

        int LatestVisible()
        {
            int latest = 0;
            for (int i = 0; i < catalog.Count; i++)
                if (ProgressStore.IsVisible(catalog, i)) latest = i;
            return latest;
        }

        void Build()
        {
            content.sizeDelta = new Vector2(content.sizeDelta.x, bottomPadding + (catalog.Count - 1) * nodeSpacing + topPadding);
            nodeTemplate.gameObject.SetActive(false);

            for (int i = 1; i < catalog.Count; i++)
                CreatePath(NodePosition(i - 1), NodePosition(i), ProgressStore.HasWon(catalog[i - 1]));

            for (int i = 0; i < catalog.Count; i++)
            {
                var node = Instantiate(nodeTemplate, content);
                node.name = $"Node{i + 1}";
                var rect = (RectTransform)node.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.anchoredPosition = NodePosition(i);
                node.gameObject.SetActive(true);
                _nodes.Add(node);
            }
        }

        // Completed stretches are a glowing magenta line; the road ahead is a faint dashed line.
        void CreatePath(Vector2 from, Vector2 to, bool completed)
        {
            Vector2 delta = to - from;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
            Vector2 middle = (from + to) * 0.5f;
            CreateSegment("PathBase", middle, new Vector2(pathWidth, delta.magnitude + pathWidth), angle,
                NeonTheme.WithAlpha(NeonTheme.Panel, 0.9f), pathSprite);
            if (completed)
            {
                CreateSegment("PathGlow", middle, new Vector2(pathWidth * 3f, delta.magnitude + pathWidth * 2f), angle,
                    NeonTheme.WithAlpha(NeonTheme.Magenta, 0.4f), glowSprite);
                CreateSegment("PathLine", middle, new Vector2(pathWidth * 0.42f, delta.magnitude + pathWidth * 0.4f), angle,
                    NeonTheme.Magenta, pathSprite);
                return;
            }
            int dashes = Mathf.Max(1, Mathf.FloorToInt(delta.magnitude / 46f));
            for (int k = 1; k < dashes; k++)
                CreateSegment("PathDash", Vector2.Lerp(from, to, k / (float)dashes), new Vector2(pathWidth * 0.18f, 18f), angle,
                    NeonTheme.WithAlpha(NeonTheme.Cyan, 0.45f), pathSprite);
        }

        void CreateSegment(string name, Vector2 position, Vector2 size, float angle, Color color, Sprite sprite)
        {
            var piece = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = (RectTransform)piece.transform;
            rect.SetParent(content, false);
            rect.SetAsFirstSibling(); // behind the nodes
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            var image = piece.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
        }

        void Select(int index)
        {
            _selected = index;
            Refresh();
        }

        void Refresh()
        {
            totalStars.text = ProgressStore.TotalStars.ToString();
            for (int i = 0; i < _nodes.Count; i++)
            {
                var level = catalog[i];
                var state = !ProgressStore.IsVisible(catalog, i) ? MapNodeState.Hidden
                    : ProgressStore.HasWon(level) ? MapNodeState.Won
                    : ProgressStore.CanEnter(catalog, i) ? MapNodeState.Open
                    : MapNodeState.Locked;
                int index = i;
                _nodes[i].Bind(i, state, ProgressStore.StarsOf(level), level.starsRequired, i == _selected, () => Select(index));
            }
            ShowSelected();
        }

        void ShowSelected()
        {
            var level = catalog[_selected];
            chapterLabel.text = level.chapter;
            titleLabel.text = AccentLastWord(level.displayName.ToUpperInvariant());
            lapsChip.text = $"{level.laps} LAPS";
            rivalsChip.text = $"{level.rivalCount} RIVALS";
            var best = ProgressStore.BestTimeOf(level);
            bestChip.text = best.HasValue ? $"BEST {TimeFormat.Race(best.Value)}" : "BEST —";
            for (int i = 0; i < starTimes.Length; i++)
                starTimes[i].text = i < level.starTimes.Length ? TimeFormat.Race(level.starTimes[i]) : "-";

            bool canEnter = ProgressStore.CanEnter(catalog, _selected);
            int missing = Mathf.Max(0, level.starsRequired - ProgressStore.TotalStars);
            startButton.interactable = canEnter;
            startFill.color = canEnter ? NeonTheme.Magenta : NeonTheme.PanelRaised;
            startLabel.text = canEnter ? "START" : "LOCKED";
            startLabel.color = canEnter ? NeonTheme.Background : NeonTheme.Dim;
            startRing.SetActive(canEnter);
            hint.text = canEnter
                ? "WIN TO EARN STARS"
                : $"NEED {missing} MORE STAR{(missing == 1 ? "" : "S")}";
            hint.color = canEnter ? NeonTheme.Dim : NeonTheme.Magenta;
        }

        static string AccentLastWord(string title)
        {
            int split = title.LastIndexOf(' ');
            return split <= 0
                ? $"<color={NeonTheme.Html(NeonTheme.Magenta)}>{title}</color>"
                : $"{title.Substring(0, split)}<color={NeonTheme.Html(NeonTheme.Magenta)}>{title.Substring(split)}</color>";
        }

        void StartSelected()
        {
            if (ProgressStore.CanEnter(catalog, _selected)) GameSession.StartRace(catalog[_selected]);
        }

        void ScrollTo(int index)
        {
            Canvas.ForceUpdateCanvases();
            float viewport = scroll.viewport != null ? scroll.viewport.rect.height : ((RectTransform)scroll.transform).rect.height;
            float scrollable = Mathf.Max(1f, content.rect.height - viewport);
            float target = NodePosition(index).y - viewport * 0.45f;
            scroll.verticalNormalizedPosition = Mathf.Clamp01(target / scrollable);
        }
    }
}
