using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The level map (start screen): a vertical path of race nodes, first race at the bottom.
    /// Nodes are created from the <see cref="LevelCatalog"/> using a node template.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        [SerializeField] LevelCatalog catalog;
        [SerializeField] UnityEngine.UI.ScrollRect scroll;
        [Tooltip("Scroll content; nodes and path pieces are created inside it.")]
        [SerializeField] RectTransform content;
        [Tooltip("Inactive node used as a template.")]
        [SerializeField] MapNode nodeTemplate;
        [SerializeField] LevelCard card;
        [SerializeField] TMP_Text totalStars;

        [Header("Layout (canvas units)")]
        [SerializeField, Min(50f)] float nodeSpacing = 360f;
        [SerializeField] float sideSwing = 280f;
        [SerializeField] float bottomPadding = 450f;
        [SerializeField] float topPadding = 600f;
        [SerializeField, Min(1f)] float pathWidth = 64f;
        [SerializeField] Color pathColor = new(0.48f, 0.49f, 0.52f);
        [SerializeField] Color hiddenPathColor = new(0.48f, 0.49f, 0.52f, 0.3f);

        readonly List<MapNode> _nodes = new();

        void Start()
        {
            Build();
            Refresh();
            ScrollToLatest();
        }

        Vector2 NodePosition(int index) =>
            new(Mathf.Sin(index * 1.3f) * sideSwing, bottomPadding + index * nodeSpacing);

        void Build()
        {
            content.sizeDelta = new Vector2(content.sizeDelta.x, bottomPadding + (catalog.Count - 1) * nodeSpacing + topPadding);
            nodeTemplate.gameObject.SetActive(false);

            for (int i = 0; i < catalog.Count; i++)
            {
                if (i > 0) CreatePath(NodePosition(i - 1), NodePosition(i), ProgressStore.IsVisible(catalog, i));
                var node = Instantiate(nodeTemplate, content);
                node.name = $"Node{i + 1}";
                var rect = (RectTransform)node.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.anchoredPosition = NodePosition(i);
                node.gameObject.SetActive(true);
                _nodes.Add(node);
            }
        }

        void CreatePath(Vector2 from, Vector2 to, bool visible)
        {
            var piece = new GameObject("Path", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = (RectTransform)piece.transform;
            rect.SetParent(content, false);
            rect.SetAsFirstSibling(); // behind the nodes
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            Vector2 delta = to - from;
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(pathWidth, delta.magnitude);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
            var image = piece.GetComponent<UnityEngine.UI.Image>();
            image.color = visible ? pathColor : hiddenPathColor;
            image.raycastTarget = false;
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
                _nodes[i].Bind(i, state, ProgressStore.StarsOf(level), level.starsRequired, () => card.Show(catalog, index));
            }
        }

        // Start with the furthest visible race in view.
        void ScrollToLatest()
        {
            int latest = 0;
            for (int i = 0; i < catalog.Count; i++)
                if (ProgressStore.IsVisible(catalog, i)) latest = i;
            Canvas.ForceUpdateCanvases();
            float viewport = scroll.viewport != null ? scroll.viewport.rect.height : ((RectTransform)scroll.transform).rect.height;
            float scrollable = Mathf.Max(1f, content.rect.height - viewport);
            float target = NodePosition(latest).y - viewport * 0.4f;
            scroll.verticalNormalizedPosition = Mathf.Clamp01(target / scrollable);
        }
    }
}
