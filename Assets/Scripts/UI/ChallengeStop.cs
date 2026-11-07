using System.Collections.Generic;
using SomeGame.Race;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The cup's challenge as a little side stop on the map, off the road next to the race of the rival who sets it
    /// (Chicken Chase next to Rosie's Meadow Run), joined to it by a dotted track. Locked until that rival is beaten;
    /// a "!" while it is new, a tick once won. Tap: the challenge's card (<see cref="ChallengePanel"/>).
    /// </summary>
    public class ChallengeStop : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image icon;
        [SerializeField] GameObject lockIcon, doneBadge, newBadge;
        [SerializeField] RectTransform dotTemplate;
        [SerializeField] ChallengePanel panel;
        [SerializeField, Min(80f)] float distance = 230f;

        readonly List<RectTransform> _dots = new();
        CupDefinition _cup;

        void Awake()
        {
            button.onClick.AddListener(() => { if (_cup != null) panel.Show(_cup); });
            dotTemplate.gameObject.SetActive(false);
        }

        public static bool IsOpen(CupDefinition cup) => cup != null && cup.challenge != null &&
            (CupList.DevUnlockAll || string.IsNullOrEmpty(cup.challengeAfterRival) || RivalProgress.IsBeaten(cup.challengeAfterRival));

        /// <summary>Puts the stop by its race (hidden when the cup has no challenge or that race is not on the map yet).</summary>
        public void Bind(CupDefinition cup, LevelCatalog catalog, IReadOnlyList<RectTransform> stops)
        {
            _cup = cup;
            int index = -1;
            if (cup != null && cup.challenge != null && catalog != null)
            {
                var rival = string.IsNullOrEmpty(cup.challengeAfterRival) ? null : Resources.Load<RivalCharacter>("Rivals/" + cup.challengeAfterRival);
                index = rival != null && rival.homeRace != null ? catalog.IndexOf(rival.homeRace) : catalog.Count - 1;
            }
            bool show = index >= 0 && index < stops.Count && ProgressStore.IsVisible(catalog, index);
            gameObject.SetActive(show);
            foreach (var dot in _dots) dot.gameObject.SetActive(show);
            if (!show) return;

            // Off the road to the side with more room (away from the other stops), within the picture.
            Vector2 at = MapLayout.PointAt(MapLayout.StopDistance(index), out var tangent);
            Vector2 normal = new(-tangent.y, tangent.x);
            Vector2 best = at + normal * distance;
            float bestScore = float.MinValue;
            foreach (float side in new[] { 1f, -1f })
                foreach (float lean in new[] { 0f, 0.35f, -0.35f })
                {
                    Vector2 p = at + (normal * side + tangent * lean).normalized * distance;
                    float score = Mathf.Min(p.x - 110f, MapLayout.Size.x - 110f - p.x) * 2f;
                    for (int i = 0; i < stops.Count; i++) if (i != index) score = Mathf.Min(score, Vector2.Distance(p, stops[i].anchoredPosition));
                    if (score > bestScore) { bestScore = score; best = p; }
                }
            if (cup.challengeSpot != Vector2.zero) best = cup.challengeSpot; // placed by hand for this cup's map
            var rt = (RectTransform)transform;
            rt.anchoredPosition = best;

            // The dotted track from the race's stop.
            const int count = 0; // the map has a little side road to it now
            while (_dots.Count < count)
            {
                var dot = Instantiate(dotTemplate, dotTemplate.parent);
                dot.gameObject.SetActive(true);
                _dots.Add(dot);
            }
            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / (count + 1f);
                _dots[i].anchoredPosition = Vector2.Lerp(at, best, Mathf.Lerp(0.3f, 0.78f, t));
            }

            bool open = IsOpen(cup);
            bool won = ChallengeProgress.IsWon(cup.challenge.name);
            icon.color = open ? Color.white : new Color(0.1f, 0.12f, 0.14f, 0.8f);
            lockIcon.SetActive(!open);
            doneBadge.SetActive(won);
            newBadge.SetActive(open && !won && ChallengeProgress.Best(cup.challenge.name) == 0);
        }

        void Update()
        {
            // A new challenge bobs to be noticed.
            if (newBadge.activeSelf)
                newBadge.transform.localScale = Vector3.one * (1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(Time.unscaledTime * 1.7f));
        }

        /// <summary>Keeps the stop and its track above the road's progress line and the car.</summary>
        public void BringForward()
        {
            foreach (var dot in _dots) dot.SetAsLastSibling();
            transform.SetAsLastSibling();
        }
    }
}
