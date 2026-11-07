using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Who is around you in the race:
    /// <list type="bullet">
    /// <item>The ladder (top left, always there): the car ahead, you (your place, big) and the car behind. Your place
    /// pops green when you gain one and flashes red when you are passed. On the final lap (or section) a header
    /// says so for a few seconds.</item>
    /// <item>Place tags: the car just ahead of you and the one just behind carry a small tag with their place while
    /// they are on screen.</item>
    /// <item>A rival with a face (<see cref="RivalCharacter"/>, in their home race) shows their portrait on the
    /// ladder and their tag, wears a name tag on the grid, and has a word for you in the countdown and at the
    /// finish (<see cref="says"/>).</item>
    /// </list>
    /// Runs after the camera has moved, so the tags sit still on their cars.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class RivalWatch : MonoBehaviour
    {
        const float HeaderSeconds = 4f;
        const float RowPitch = 84f, YouPitch = 108f;

        [SerializeField] RaceManager race;
        [Tooltip("A place tag (copied for the car ahead and the one behind): a Pill with a Label, and a Face for a rival with one.")]
        [SerializeField] RectTransform tagTemplate;
        [SerializeField] RectTransform ladder;
        [Tooltip("FINAL LAP and the like, shown for a few seconds just under the ladder.")]
        [SerializeField] RectTransform ladderHeader;
        [Tooltip("The ladder's rows: the car ahead, you, the car behind (each a Place and a Face).")]
        [SerializeField] RectTransform[] ladderRows;
        [Tooltip("A rival with a face speaking: a Portrait (with a Picture), a Name and a Bubble with a Label.")]
        [SerializeField] RectTransform says;
        [Tooltip("The first win against a rival with a face: their prizes.")]
        [SerializeField] RivalReward reward;

        class Tag
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public TMP_Text Label;
            public RectTransform Face;
            public RaceProgress Car;
            public float Alpha;
        }

        readonly Tag[] _tags = new Tag[3]; // the car ahead, the car behind, a character's name tag on the grid
        Camera _camera;
        RectTransform _layer;
        CanvasGroup _ladderGroup, _saysGroup;
        TMP_Text _headerLabel;
        int _place = -1;
        float _placeChangedAt = -10f;
        bool _gained;
        float _headerAt = -100f;
        bool _finalLapShown;
        float _saysAt = -100f, _saysFor;
        Vector2 _saysHome;

        static readonly Color Gain = new(0.45f, 0.95f, 0.45f), Loss = new(1f, 0.3f, 0.26f);

        void Start()
        {
            _layer = (RectTransform)transform;
            tagTemplate.gameObject.SetActive(false);
            for (int i = 0; i < _tags.Length; i++)
            {
                var root = Instantiate(tagTemplate, tagTemplate.parent);
                root.SetSiblingIndex(tagTemplate.GetSiblingIndex() + 1);
                root.name = i == 0 ? "TagAhead" : i == 1 ? "TagBehind" : "NameTag";
                _tags[i] = new Tag { Root = root, Group = Group(root), Label = root.Find("Label").GetComponent<TMP_Text>(), Face = root.Find("Face") as RectTransform };
                _tags[i].Group.alpha = 0f;
                root.gameObject.SetActive(false);
            }
            _ladderGroup = Group(ladder);
            _ladderGroup.alpha = 0f;
            _headerLabel = ladderHeader.GetComponentInChildren<TMP_Text>();
            ladderHeader.gameObject.SetActive(false);
            if (says != null)
            {
                _saysGroup = Group(says);
                _saysHome = says.anchoredPosition;
                says.gameObject.SetActive(false);
            }
            if (race != null)
            {
                race.PlayerLapCompleted += OnLap;
                race.PlayerFinished += OnFinished;
            }
        }

        void OnDestroy()
        {
            if (race == null) return;
            race.PlayerLapCompleted -= OnLap;
            race.PlayerFinished -= OnFinished;
        }

        static CanvasGroup Group(RectTransform rt)
        {
            if (!rt.TryGetComponent(out CanvasGroup group)) group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        void OnLap(float lapTime, bool newBest)
        {
            var player = race.Player;
            if (_finalLapShown || player.IsFinished || player.CurrentLap < player.TotalLaps || player.TotalLaps < 2) return;
            _finalLapShown = true;
            Header(race.Sectioned ? "FINAL SECTION" : "FINAL LAP");
        }

        void Header(string text)
        {
            _headerLabel.text = text;
            _headerAt = Time.time;
        }

        void LateUpdate()
        {
            if (race == null || race.Player == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            var player = race.Player;
            bool racing = race.State == RaceState.Racing && !player.IsFinished;

            int place = race.PositionOf(player);
            if (_place > 0 && place != _place && racing && race.RaceTime > 3f)
            {
                _gained = place < _place;
                _placeChangedAt = Time.time;
            }
            _place = place;

            var standings = race.Standings;
            RaceProgress ahead = place >= 2 ? standings[place - 2] : null;
            RaceProgress behind = place < standings.Count ? standings[place] : null;
            RaceProgress named = null;
            if (race.State == RaceState.Countdown)
                foreach (var car in standings) if (car != null && car.Character != null) named = car;
            Place(_tags[0], racing ? ahead : null, false);
            Place(_tags[1], racing ? behind : null, false);
            Place(_tags[2], named, true);
            Unstack();

            Ladder((racing || race.State == RaceState.Countdown) && race.Challenge == null, place, ahead, behind);
            Greet();
            Says();

        }

        // A tag over a car while it is on screen. When the car changes (a place changed), the old tag fades out
        // first and the new one fades in on its car.
        void Place(Tag tag, RaceProgress want, bool name)
        {
            if (want != tag.Car && tag.Alpha <= 0.001f) tag.Car = want;
            var car = tag.Car;
            Vector3 viewport = car != null ? _camera.WorldToViewportPoint(car.transform.position) : Vector3.zero;
            bool show = car != null && car == want && !car.IsFinished
                        && viewport.x > 0.03f && viewport.x < 0.97f && viewport.y > 0.03f && viewport.y < 0.82f; // not up under the HUD
            tag.Alpha = Mathf.MoveTowards(tag.Alpha, show ? 1f : 0f, Time.deltaTime / (show ? 0.2f : 0.25f));
            tag.Group.alpha = tag.Alpha;
            bool active = tag.Alpha > 0.001f;
            if (tag.Root.gameObject.activeSelf != active) tag.Root.gameObject.SetActive(active);
            if (!active || car == null) return;

            var character = car.Character;
            tag.Label.text = name && character != null ? character.displayName.ToUpperInvariant() : TimeFormat.Ordinal(race.PositionOf(car)).ToUpperInvariant();
            if (tag.Face != null)
            {
                tag.Face.gameObject.SetActive(character != null);
                if (character != null) Paint(tag.Face, car);
            }
            // The pill fits its words (and the face at its left end).
            float face = character != null ? 58f : 0f;
            float width = Mathf.Ceil(tag.Label.GetPreferredValues(tag.Label.text).x) + 44f + face;
            tag.Root.sizeDelta = new Vector2(Mathf.Max(112f, width), tag.Root.sizeDelta.y);
            tag.Label.rectTransform.offsetMin = new Vector2(face, 0f);
            // Over the car, in whole canvas units (so it doesn't shimmer), never half off the screen.
            Vector2 spot = Local(car.transform.position) + new Vector2(0f, 110f);
            float room = _layer.rect.width * 0.5f - tag.Root.sizeDelta.x * 0.5f - 12f;
            tag.Root.anchoredPosition = new Vector2(Mathf.Round(Mathf.Clamp(spot.x, -room, room)), Mathf.Round(spot.y));
            tag.Root.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, Mathf.SmoothStep(0f, 1f, tag.Alpha));
        }

        // Tags on cars side by side: the upper one moves up out of the way.
        void Unstack()
        {
            for (int i = 0; i < _tags.Length; i++)
                for (int k = 0; k < _tags.Length; k++)
                {
                    if (i == k || !_tags[i].Root.gameObject.activeSelf || !_tags[k].Root.gameObject.activeSelf) continue;
                    RectTransform a = _tags[i].Root, b = _tags[k].Root;
                    float dy = a.anchoredPosition.y - b.anchoredPosition.y;
                    if (dy < 0f || (dy == 0f && i < k)) continue;
                    float needX = (a.sizeDelta.x + b.sizeDelta.x) * 0.5f + 6f, needY = a.sizeDelta.y + 6f;
                    if (Mathf.Abs(a.anchoredPosition.x - b.anchoredPosition.x) < needX && dy < needY)
                        a.anchoredPosition = new Vector2(a.anchoredPosition.x, b.anchoredPosition.y + needY);
                }
        }

        Vector2 Local(Vector3 world)
        {
            Vector2 screen = _camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out var local);
            return local - _layer.rect.center; // the layer's children sit on its centre
        }

        // The ladder: the car ahead (small), you (big), the car behind (small); a header under it now and then.
        void Ladder(bool shown, int place, RaceProgress ahead, RaceProgress behind)
        {
            _ladderGroup.alpha = Mathf.MoveTowards(_ladderGroup.alpha, shown ? 1f : 0f, Time.deltaTime / 0.3f);
            float y = 0f;
            for (int row = 0; row < ladderRows.Length && row < 3; row++)
            {
                var car = row == 0 ? ahead : row == 1 ? race.Player : behind;
                var rowRt = ladderRows[row];
                bool exists = car != null;
                if (rowRt.gameObject.activeSelf != exists) rowRt.gameObject.SetActive(exists);
                if (!exists) continue;
                float pitch = row == 1 ? YouPitch : RowPitch;
                rowRt.anchoredPosition = new Vector2(rowRt.anchoredPosition.x, y - pitch * 0.5f);
                y -= pitch;
                int at = row == 0 ? place - 1 : row == 1 ? place : place + 1;
                var label = rowRt.Find("Place").GetComponent<TMP_Text>();
                label.text = TimeFormat.Ordinal(at).ToUpperInvariant();
                Paint(rowRt.Find("Face"), row == 1 ? null : car);
                if (row != 1) continue;
                // Your place: pops green for a place gained, flashes red when passed.
                float k = Mathf.Clamp01((Time.time - _placeChangedAt) / 0.8f);
                label.color = Color.Lerp(_gained ? Gain : Loss, Color.white, k);
                label.rectTransform.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(Mathf.Clamp01((Time.time - _placeChangedAt) / 0.3f) * Mathf.PI));
            }
            ladder.sizeDelta = new Vector2(ladder.sizeDelta.x, -y);

            float t = Time.time - _headerAt;
            bool header = t < HeaderSeconds && shown;
            if (ladderHeader.gameObject.activeSelf != header) ladderHeader.gameObject.SetActive(header);
            if (header) ladderHeader.localScale = Vector3.one * (t < 0.25f ? Overshoot(t / 0.25f) : 1f) * (1f - Mathf.Clamp01((t - HeaderSeconds + 0.25f) / 0.25f));
        }

        // ---------------------------------------------------------------- a rival with a face speaks

        // Their line during the countdown (the race may have started counting before this was awake).
        void Greet()
        {
            if (_greeted || race.State != RaceState.Countdown || race.Character == null) return;
            _greeted = true;
            Say(race.Character, race.Character.Pick(race.Character.startLines), 2.6f, _saysHome, 0.3f);
        }
        bool _greeted;

        void OnFinished()
        {
            var character = race.Character;
            if (character == null) return;
            RaceProgress them = null;
            foreach (var car in race.Standings) if (car != null && car.Character == character) them = car;
            if (them == null) return;
            bool beatThem = race.PositionOf(race.Player) < race.PositionOf(them);
            // The first win against them: their gadget and part are yours (shown big over the results).
            if (beatThem && race.Online == null && RaceManager.KeepRecords && RivalProgress.MarkBeaten(character.name) && reward != null)
            {
                StartCoroutine(Later(1.6f, () => reward.Show(character, character.Pick(character.loseLines))));
                return;
            }
            // Between the results card and the buttons.
            Say(character, character.Pick(beatThem ? character.loseLines : character.winLines), 4.5f, _saysHome + new Vector2(0f, 20f), 0.9f);
        }

        System.Collections.IEnumerator Later(float seconds, System.Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }

        /// <summary>A rival says something (their face and a speech bubble, low on the screen).</summary>
        public void Say(RivalCharacter character, string line, float seconds) => Say(character, line, seconds, _saysHome);

        void Say(RivalCharacter character, string line, float seconds, Vector2 home, float delay = 0f)
        {
            if (says == null || string.IsNullOrEmpty(line)) return;
            // Their face in the line's mood ("laugh|Ha!"), the words in the bubble.
            string words = RivalCharacter.Words(line, out var mood);
            var portrait = says.Find("Portrait/Picture")?.GetComponent<UnityEngine.UI.Image>();
            if (portrait != null) portrait.sprite = character.Face(mood);
            var nameLabel = says.Find("Portrait/Name")?.GetComponentInChildren<TMP_Text>();
            if (nameLabel != null) nameLabel.text = character.displayName.ToUpperInvariant();
            says.Find("Bubble/Label").GetComponent<TMP_Text>().text = words;
            says.anchoredPosition = home;
            _saysAt = Time.time + delay;
            _saysFor = seconds;
            says.gameObject.SetActive(true);
            _saysGroup.alpha = 0f;
        }

        // Pops up (the portrait first, then the bubble), stays, then fades away.
        void Says()
        {
            if (says == null || !says.gameObject.activeSelf) return;
            float t = Time.time - _saysAt;
            if (t < 0f) { _saysGroup.alpha = 0f; return; }
            float leave = Mathf.Clamp01((t - _saysFor) / 0.3f);
            _saysGroup.alpha = Mathf.Clamp01(t / 0.12f) * (1f - leave);
            var portrait = says.Find("Portrait");
            if (portrait != null) portrait.localScale = Vector3.one * (t < 0.35f ? Overshoot(t / 0.35f) : 1f) * (1f - 0.2f * leave);
            var bubble = says.Find("Bubble");
            if (bubble != null)
            {
                float b = Mathf.Clamp01((t - 0.15f) / 0.3f);
                bubble.localScale = Vector3.one * (b > 0f ? Overshoot(b) : 0f) * (1f - 0.2f * leave);
            }
            if (leave >= 1f) says.gameObject.SetActive(false);
        }

        // A face: a rival with one shows their portrait, the others their car in its colours; null is your car.
        static void Paint(Transform face, RaceProgress car)
        {
            if (face == null) return;
            var character = car != null ? car.Character : null;
            var picture = face.Find("Picture")?.GetComponent<UnityEngine.UI.Image>();
            if (picture != null)
            {
                if (picture.gameObject.activeSelf != (character != null)) picture.gameObject.SetActive(character != null);
                if (character != null) picture.sprite = character.portrait;
            }
            var carIcon = face.Find("Car");
            if (carIcon != null && carIcon.gameObject.activeSelf != (character == null)) carIcon.gameObject.SetActive(character == null);
            var body = face.Find("Car/Body")?.GetComponent<UnityEngine.UI.Image>();
            if (body == null || character != null) return;
            var visual = car != null ? SomeGame.Online.OnlineRace.Visual(car.GetComponent<SomeGame.Car.CarMovement>()) : null;
            body.color = car == null ? Garage.Color : visual != null ? visual.color : Theme.Stone;
            if (car != null) return;
            // Your own: the body you built in the garage.
            var entry = GarageArt.Instance?.Get(Garage.Equipped(Garage.Slot.Body));
            if (entry?.full != null && body.sprite != entry.full)
            {
                body.sprite = entry.full;
                var details = face.Find("Car/Details")?.GetComponent<UnityEngine.UI.Image>();
                if (details != null && entry.details != null) details.sprite = entry.details;
            }
        }

        static float Overshoot(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            x = Mathf.Clamp01(x);
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
