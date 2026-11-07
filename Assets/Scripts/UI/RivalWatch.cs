using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Who is around you in the race, shown only when it matters:
    /// <list type="bullet">
    /// <item>Place tags: a rival within about <see cref="TagSeconds"/> ahead or behind gets a small tag over its car
    /// with its place; it fades out once the rival is further away.</item>
    /// <item>Edge arrows: a rival just out of view ahead gets an arrow at the screen's edge with its face and the gap
    /// ("+0.8s"); one closing in from behind off screen gets "CLOSING!".</item>
    /// <item>The ladder: on the final lap (or section), in the last 20 seconds and after a place changes, a slim
    /// ladder slides in at the side: the car ahead, you and the car behind, with the gaps. It hides after a few
    /// seconds.</item>
    /// <item>Overtake: the face of the rival you just passed flips down above the OVERTAKE! headline.</item>
    /// </list>
    /// The faces are placeholders (the rival's car in its colours on a disc) until the rivals get their characters.
    /// </summary>
    public class RivalWatch : MonoBehaviour
    {
        const float TagSeconds = 1.5f;
        const float EdgeAheadSeconds = 3f;
        const float LadderSeconds = 4f;
        const float FinalSeconds = 20f;
        const float RowPitch = 100f, RowTop = -60f, RowTopBare = -10f; // the ladder's rows (from its top)

        [SerializeField] RaceManager race;
        [Tooltip("A place tag (copied for each rival): a Pill with a Label.")]
        [SerializeField] RectTransform tagTemplate;
        [Tooltip("An edge arrow (copied for each rival): a Pivot holding the Arrow, a Face and a Label.")]
        [SerializeField] RectTransform edgeTemplate;
        [Tooltip("Where the edge arrows may sit (inset from the screen's edges, clear of the HUD).")]
        [SerializeField] RectTransform edgeArea;
        [SerializeField] RectTransform ladder;
        [SerializeField] TMP_Text ladderHeader;
        [Tooltip("The ladder's rows: the car ahead, you, the car behind (each a Place, a Face and a Gap).")]
        [SerializeField] RectTransform[] ladderRows;
        [Tooltip("The face of the rival just passed, flipped down above OVERTAKE!")]
        [SerializeField] RectTransform passedFace;

        class Rival
        {
            public RaceProgress Car;
            public SpriteRenderer Visual;
            public RectTransform Tag, Edge;
            public CanvasGroup TagGroup, EdgeGroup;
            public float Gap, GapRate, TagAlpha, EdgeAlpha;
            public bool Seen, Ahead;
        }

        readonly List<Rival> _rivals = new();
        Camera _camera;
        RectTransform _layer;
        float _speed = 10f;
        float _ladderShownAt = -100f, _ladderHideAt = -100f, _passedAt = -100f;
        bool _finalLapShown, _finalSecondsShown;
        float _finalLapAt = -100f;
        CanvasGroup _ladderGroup, _passedGroup;
        Vector2 _ladderHome;

        static readonly Color Closing = new(1f, 0.36f, 0.3f);

        void Start()
        {
            _layer = (RectTransform)transform;
            tagTemplate.gameObject.SetActive(false);
            edgeTemplate.gameObject.SetActive(false);
            _ladderHome = ladder.anchoredPosition;
            _ladderGroup = Group(ladder);
            _ladderGroup.alpha = 0f;
            ladder.gameObject.SetActive(false);
            _passedGroup = Group(passedFace);
            passedFace.gameObject.SetActive(false);
            if (race != null) race.PlayerLapCompleted += OnLap;
        }

        void OnDestroy()
        {
            if (race != null) race.PlayerLapCompleted -= OnLap;
        }

        static CanvasGroup Group(RectTransform rt)
        {
            if (!rt.TryGetComponent(out CanvasGroup group)) group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        bool Bind()
        {
            if (race == null || race.Player == null) return false;
            if (_rivals.Count == 0)
                foreach (var car in race.Standings)
                {
                    if (car == null || car == race.Player) continue;
                    // Under the ladder and the face (the templates come before them).
                    var tag = Instantiate(tagTemplate, tagTemplate.parent);
                    tag.SetSiblingIndex(tagTemplate.GetSiblingIndex() + 1);
                    var edge = Instantiate(edgeTemplate, edgeTemplate.parent);
                    edge.SetSiblingIndex(edgeTemplate.GetSiblingIndex() + 1);
                    tag.name = "Tag " + car.DisplayName;
                    edge.name = "Edge " + car.DisplayName;
                    var rival = new Rival
                    {
                        Car = car, Tag = tag, Edge = edge, TagGroup = Group(tag), EdgeGroup = Group(edge),
                        Visual = SomeGame.Online.OnlineRace.Visual(car.GetComponent<SomeGame.Car.CarMovement>()),
                    };
                    rival.TagGroup.alpha = rival.EdgeGroup.alpha = 0f;
                    Paint(edge.Find("Face"), rival);
                    _rivals.Add(rival);
                }
            if (_camera == null) _camera = Camera.main;
            return _camera != null;
        }

        void OnLap(float lapTime, bool newBest)
        {
            var player = race.Player;
            if (_finalLapShown || player.IsFinished || player.CurrentLap < player.TotalLaps || player.TotalLaps < 2) return;
            _finalLapShown = true;
            _finalLapAt = Time.time;
            ShowLadder(race.Sectioned ? "FINAL SECTION" : "FINAL LAP");
        }

        void LateUpdate()
        {
            if (!Bind()) return;
            var player = race.Player;
            bool racing = race.State == RaceState.Racing && !player.IsFinished;
            float dt = Time.deltaTime;

            // The gaps in seconds: metres on the road at your pace (smoothed, never below a crawl).
            if (player.TryGetComponent(out Rigidbody2D body))
                _speed = Mathf.Lerp(_speed, Mathf.Max(8f, body.linearVelocity.magnitude), 1f - Mathf.Exp(-2f * dt));
            float you = player.Metres;
            int place = race.PositionOf(player);
            foreach (var rival in _rivals)
            {
                if (rival.Car == null) continue;
                float gap = (rival.Car.Metres - you) / _speed;
                if (!rival.Seen) { rival.Gap = gap; rival.GapRate = 0f; rival.Seen = true; }
                float smoothed = Mathf.Lerp(rival.Gap, gap, 1f - Mathf.Exp(-6f * dt));
                rival.GapRate = Mathf.Lerp(rival.GapRate, dt > 0f ? (smoothed - rival.Gap) / dt : 0f, 1f - Mathf.Exp(-3f * dt));
                rival.Gap = smoothed;
                // Passed one (ahead of you a moment ago, now behind): its face flips down over OVERTAKE!
                bool ahead = race.PositionOf(rival.Car) < place;
                if (racing && race.RaceTime > 3f && rival.Ahead && !ahead && !rival.Car.IsFinished) Passed(rival);
                // Passed by one: the ladder shows who went by.
                if (racing && race.RaceTime > 8f && !rival.Ahead && ahead) ShowLadder(null);
                rival.Ahead = ahead;
            }

            Tags(racing);
            Edges(racing);
            Ladder(racing, place);
            PassedFace();

            // The last 20 seconds (on the last lap, and not straight after FINAL LAP: on a short track they meet).
            bool lastLap = player.CurrentLap >= player.TotalLaps;
            if (racing && !_finalSecondsShown && lastLap && Time.time - _finalLapAt > 8f && player.MetresToGo / _speed <= FinalSeconds && race.RaceTime > 10f)
            {
                _finalSecondsShown = true;
                ShowLadder("FINAL 20 SECONDS");
            }
        }

        // A tag over every rival close by (at most three, the nearest), on screen.
        void Tags(bool racing)
        {
            Rival[] near = Nearest(3, r => Mathf.Abs(r.Gap) <= TagSeconds);
            foreach (var rival in _rivals)
            {
                Vector3 viewport = rival.Car != null ? _camera.WorldToViewportPoint(rival.Car.transform.position) : Vector3.zero;
                bool onScreen = viewport.x > 0.02f && viewport.x < 0.98f && viewport.y > 0.02f && viewport.y < 0.84f; // not up under the HUD
                bool show = racing && onScreen && System.Array.IndexOf(near, rival) >= 0 && !rival.Car.IsFinished;
                rival.TagAlpha = Mathf.MoveTowards(rival.TagAlpha, show ? 1f : 0f, Time.deltaTime / (show ? 0.15f : 0.35f));
                rival.TagGroup.alpha = rival.TagAlpha;
                bool active = rival.TagAlpha > 0.001f;
                if (rival.Tag.gameObject.activeSelf != active) rival.Tag.gameObject.SetActive(active);
                if (!active) continue;
                Vector2 spot = Local(rival.Car.transform.position) + new Vector2(0f, 120f);
                float room = _layer.rect.width * 0.5f - rival.Tag.rect.width * 0.5f - 12f;
                rival.Tag.anchoredPosition = new Vector2(Mathf.Clamp(spot.x, -room, room), spot.y); // never half off the screen
                rival.Tag.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, Mathf.SmoothStep(0f, 1f, rival.TagAlpha));
                var label = rival.Tag.GetComponentInChildren<TMP_Text>();
                label.text = TimeFormat.Ordinal(race.PositionOf(rival.Car)).ToUpperInvariant();
            }
            // Bunched-up cars: their tags stack up instead of covering each other.
            _shownTags.Clear();
            foreach (var rival in _rivals) if (rival.Tag.gameObject.activeSelf) _shownTags.Add(rival.Tag);
            _shownTags.Sort((x, y) => x.anchoredPosition.y.CompareTo(y.anchoredPosition.y));
            for (int i = 1; i < _shownTags.Count; i++)
                for (int k = 0; k < i; k++)
                {
                    Vector2 p = _shownTags[i].anchoredPosition, q = _shownTags[k].anchoredPosition;
                    var size = _shownTags[i].rect.size + new Vector2(8f, 6f);
                    if (Mathf.Abs(p.x - q.x) < size.x && p.y - q.y < size.y) _shownTags[i].anchoredPosition = new Vector2(p.x, q.y + size.y);
                }
        }
        readonly List<RectTransform> _shownTags = new();

        // An arrow at the edge for the nearest rival just out of view ahead, and for one closing in from behind.
        void Edges(bool racing)
        {
            Rival ahead = null, behind = null;
            foreach (var rival in _rivals)
            {
                if (rival.Car == null || rival.Car.IsFinished || OnScreen(rival)) continue;
                if (rival.Gap > 0f && rival.Gap <= EdgeAheadSeconds && (ahead == null || rival.Gap < ahead.Gap)) ahead = rival;
                if (rival.Gap < 0f && rival.Gap >= -TagSeconds && rival.GapRate > 0.04f && (behind == null || rival.Gap > behind.Gap)) behind = rival;
            }
            // The area in the layer's space (both sit on the layer's centre).
            var area = new Rect((Vector2)edgeArea.localPosition + edgeArea.rect.position, edgeArea.rect.size);
            Vector2 centre = Local(race.Player.transform.position);
            foreach (var rival in _rivals)
            {
                bool show = racing && (rival == ahead || rival == behind);
                bool was = rival.EdgeAlpha > 0.001f;
                rival.EdgeAlpha = Mathf.MoveTowards(rival.EdgeAlpha, show ? 1f : 0f, Time.deltaTime / 0.25f);
                rival.EdgeGroup.alpha = rival.EdgeAlpha;
                bool active = rival.EdgeAlpha > 0.001f;
                if (rival.Edge.gameObject.activeSelf != active) rival.Edge.gameObject.SetActive(active);
                if (!active) continue;
                // Where the line from your car to theirs leaves the area.
                Vector2 dir = Local(rival.Car.transform.position) - centre;
                if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                Vector2 half = area.size * 0.5f, from = centre - area.center;
                float tx = Mathf.Abs(dir.x) > 0.001f ? ((dir.x > 0f ? half.x : -half.x) - from.x) / dir.x : float.MaxValue;
                float ty = Mathf.Abs(dir.y) > 0.001f ? ((dir.y > 0f ? half.y : -half.y) - from.y) / dir.y : float.MaxValue;
                Vector2 spot = from + dir * Mathf.Max(0f, Mathf.Min(tx, ty));
                spot = area.center + new Vector2(Mathf.Clamp(spot.x, -half.x, half.x), Mathf.Clamp(spot.y, -half.y, half.y));
                // Glides along the edge as the rival moves (appears right where it belongs).
                rival.Edge.anchoredPosition = was ? Vector2.Lerp(rival.Edge.anchoredPosition, spot, 1f - Mathf.Exp(-14f * Time.deltaTime)) : spot;
                var pivot = rival.Edge.Find("Pivot");
                if (pivot != null) pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                bool closing = rival.Gap < 0f;
                var label = rival.Edge.Find("Label").GetComponent<TMP_Text>();
                label.text = closing ? "CLOSING!" : $"+{rival.Gap:0.0}s";
                label.color = closing ? Closing : Color.white;
                var arrow = pivot != null ? pivot.Find("Arrow")?.GetComponent<UnityEngine.UI.Image>() : null;
                if (arrow != null) arrow.color = closing ? Closing : Theme.Amber;
                float pulse = closing ? 1f + 0.08f * Mathf.Sin(Time.time * 12f) : 1f;
                rival.Edge.localScale = Vector3.one * pulse * Mathf.Lerp(0.6f, 1f, rival.EdgeAlpha);
            }
        }

        bool OnScreen(Rival rival)
        {
            Vector3 v = _camera.WorldToViewportPoint(rival.Car.transform.position);
            return v.x > -0.02f && v.x < 1.02f && v.y > -0.02f && v.y < 1.02f;
        }

        Rival[] Nearest(int count, System.Func<Rival, bool> filter)
        {
            var list = new List<Rival>();
            foreach (var rival in _rivals) if (rival.Car != null && filter(rival)) list.Add(rival);
            list.Sort((a, b) => Mathf.Abs(a.Gap).CompareTo(Mathf.Abs(b.Gap)));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list.ToArray();
        }

        Vector2 Local(Vector3 world)
        {
            Vector2 screen = _camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out var local);
            return local - _layer.rect.center; // the layer's children sit on its centre
        }

        public void ShowLadder(string header)
        {
            if (race == null || race.Player == null || race.Player.IsFinished) return;
            if (Time.time > _ladderHideAt) _ladderShownAt = Time.time; // already out: it just stays longer
            _ladderHideAt = Time.time + LadderSeconds;
            ladderHeader.text = header ?? "";
            ladderHeader.gameObject.SetActive(!string.IsNullOrEmpty(header));
            ladder.gameObject.SetActive(true);
        }

        // The ladder: the car ahead, you, the car behind; slides in from the side and back out.
        void Ladder(bool racing, int place)
        {
            if (!ladder.gameObject.activeSelf) return;
            float t = Time.time;
            if (!racing) _ladderHideAt = Mathf.Min(_ladderHideAt, t);
            float slideIn = Mathf.Clamp01((t - _ladderShownAt) / 0.35f);
            float slideOut = Mathf.Clamp01((t - _ladderHideAt) / 0.3f);
            float shown = EaseOutBack(slideIn) * (1f - slideOut * slideOut);
            ladder.anchoredPosition = _ladderHome + new Vector2(-(ladder.rect.width + 80f) * (1f - shown), 0f);
            _ladderGroup.alpha = Mathf.Clamp01(shown * 1.5f);
            if (slideOut >= 1f) { ladder.gameObject.SetActive(false); return; }

            var standings = race.Standings;
            // The rows there are (no one ahead of the leader, no one behind the last), stacked under the header.
            float y = ladderHeader.gameObject.activeSelf ? RowTop : RowTopBare;
            for (int row = 0; row < ladderRows.Length && row < 3; row++)
            {
                int at = place - 2 + row; // 0-based index into the standings: ahead, you, behind
                var rowRt = ladderRows[row];
                bool exists = at >= 0 && at < standings.Count && standings[at] != null;
                if (rowRt.gameObject.activeSelf != exists) rowRt.gameObject.SetActive(exists);
                if (!exists) continue;
                ladderRows[row].anchoredPosition = new Vector2(ladderRows[row].anchoredPosition.x, y - RowPitch * 0.5f);
                y -= RowPitch;
                var car = standings[at];
                rowRt.Find("Place").GetComponent<TMP_Text>().text = TimeFormat.Ordinal(at + 1).ToUpperInvariant();
                var gapLabel = rowRt.Find("Gap")?.GetComponent<TMP_Text>();
                if (row == 1) { Paint(rowRt.Find("Face"), null); continue; }
                var rival = _rivals.Find(r => r.Car == car);
                Paint(rowRt.Find("Face"), rival);
                if (gapLabel != null)
                {
                    float gap = rival != null ? Mathf.Max(0.1f, Mathf.Abs(rival.Gap)) : 0.1f;
                    gapLabel.text = car.IsFinished ? "DONE" : $"{gap:0.0}s";
                }
            }
            ladder.sizeDelta = new Vector2(ladder.sizeDelta.x, -y + 10f);
        }

        void Passed(Rival rival)
        {
            if (Time.time - _passedAt < 1.2f) return; // like the OVERTAKE! headline, not again straight away
            _passedAt = Time.time;
            Paint(passedFace.Find("Face") ?? passedFace, rival);
            passedFace.gameObject.SetActive(true);
            if (race.RaceTime > 8f) ShowLadder(null); // not while the grid is still sorting itself out
        }

        // Flips down (turning over from flat), sits a moment over the headline, then floats up with it and fades.
        void PassedFace()
        {
            if (!passedFace.gameObject.activeSelf) return;
            float t = Time.time - _passedAt;
            float flip = Mathf.Clamp01(t / 0.4f);
            float angle = 90f * (1f - flip) - 20f * Mathf.Sin(flip * Mathf.PI) * (1f - flip * 0.5f);
            float leave = Mathf.Clamp01((t - 1.1f) / 0.45f);
            passedFace.localRotation = Quaternion.Euler(angle, 0f, 0f);
            passedFace.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, leave);
            _passedGroup.alpha = 1f - leave;
            if (leave >= 1f) passedFace.gameObject.SetActive(false);
        }

        // A face: the rival's car in its colours (null: your car, in the garage's paint).
        static void Paint(Transform face, Rival rival)
        {
            if (face == null) return;
            var body = face.Find("Car/Body")?.GetComponent<UnityEngine.UI.Image>();
            if (body == null) return;
            body.color = rival == null ? Garage.Color : rival.Visual != null ? rival.Visual.color : Theme.Stone;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
