using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The small world tab on the left of the map. Tapping it opens a row of cup tiles: the current cup
    /// (highlighted), locked cups with what they need (stars and trophies), and cups still coming soon.
    /// Tapping an unlocked cup switches the map to it.
    /// </summary>
    public class CupMenu : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button worldButton;
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Button closeArea;
        [SerializeField] CupTile tileTemplate;
        [SerializeField] RectTransform tilesParent;

        readonly List<CupTile> _tiles = new();

        void Start()
        {
            panel.SetActive(false);
            tileTemplate.gameObject.SetActive(false);
            worldButton.onClick.AddListener(() => { if (panel.activeSelf) Close(); else Open(); });
            closeArea.onClick.AddListener(Close);
        }

        void Open()
        {
            Build();
            panel.SetActive(true);
        }

        void Close() => panel.SetActive(false);

        void Build()
        {
            foreach (var t in _tiles) Destroy(t.gameObject);
            _tiles.Clear();
            var list = CupList.Instance;
            if (list == null) return;
            var selected = CupList.Selected;
            foreach (var cup in list.cups)
            {
                var tile = Instantiate(tileTemplate, tilesParent);
                tile.gameObject.SetActive(true);
                // A cup marked "coming soon" (no races and no requirement) stays soon; otherwise locked until earned.
                bool comingSoon = !cup.HasRaces && cup.starsRequired == 0 && cup.trophiesRequired == 0;
                var state = comingSoon ? CupTile.State.Soon
                    : !CupList.IsUnlocked(cup) ? CupTile.State.Locked
                    : !cup.HasRaces ? CupTile.State.Soon
                    : cup == selected ? CupTile.State.Current : CupTile.State.Open;
                var c = cup;
                tile.Bind(cup, state, () => Pick(c));
                _tiles.Add(tile);
            }
        }

        void Pick(CupDefinition cup)
        {
            if (!CupList.IsUnlocked(cup) || !cup.HasRaces) return;
            if (cup == CupList.Selected) { Close(); return; }
            CupList.Selected = cup;
            GameSession.ReturnToMap(); // reload the map with the other cup
        }
    }
}
