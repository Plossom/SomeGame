using SomeGame.Online;
using SomeGame.Race;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.UI
{
    /// <summary>
    /// The online race's screens: "waiting for racers" until every phone has loaded, then (after your
    /// finish) the results, live: each racer's place, car colour, name and time as they come in. The host
    /// takes everyone back to the room; if the room closes, a card says so and leads to the map.
    /// </summary>
    public class OnlineRaceView : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] GameObject waiting;
        [SerializeField] TMP_Text waitingLabel;
        [SerializeField] GameObject results;
        [SerializeField] CanvasGroup resultsGroup;
        [SerializeField] TMP_Text placeLabel, raceLabel;
        [SerializeField] RectTransform[] rows;
        [SerializeField] Button lobbyButton, leaveButton;
        [SerializeField] TMP_Text lobbyLabel, hintLabel;
        [SerializeField] GameObject closed;
        [SerializeField] TMP_Text closedLabel;
        [SerializeField] Button closedButton;

        float _shownAt = -10f;
        int _shownPlace;

        void Awake()
        {
            waiting.SetActive(false);
            results.SetActive(false);
            closed.SetActive(false);
            lobbyButton.onClick.AddListener(() => OnlineRoom.Current?.BackToLobby());
            leaveButton.onClick.AddListener(() =>
            {
                OnlineSession.Instance.Leave();
                GameSession.ReturnToMap();
            });
            closedButton.onClick.AddListener(GameSession.ReturnToMap);
        }

        void OnEnable()
        {
            race.PlayerFinished += ShowResults;
            if (GameSession.Online) OnlineSession.Instance.Failed += OnClosed;
        }

        void OnDisable()
        {
            race.PlayerFinished -= ShowResults;
            if (GameSession.Online && OnlineSession.Instance != null) OnlineSession.Instance.Failed -= OnClosed;
        }

        void ShowResults()
        {
            if (race.Online == null) return;
            _shownAt = Time.unscaledTime;
            results.SetActive(true);
            var level = race.Level;
            raceLabel.text = level != null ? level.displayName.ToUpperInvariant() : "";
        }

        void OnClosed(string message)
        {
            closedLabel.text = message;
            closed.SetActive(true);
            results.SetActive(false);
        }

        void Update()
        {
            var online = race.Online;
            if (online == null) return;
            waiting.SetActive(online.WaitingForPlayers && race.State == RaceState.Countdown && !closed.activeSelf);
            if (waiting.activeSelf)
            {
                int dots = 1 + Mathf.FloorToInt(Time.unscaledTime * 3f) % 3;
                waitingLabel.text = "WAITING FOR RACERS" + new string('.', dots);
            }
            if (!results.activeSelf) return;

            float t = Time.unscaledTime - _shownAt;
            resultsGroup.alpha = Mathf.Clamp01(t / 0.35f);
            int place = race.PositionOf(race.Player);
            placeLabel.text = Ordinal(place);
            if (place != _shownPlace) _shownPlace = place;
            float pop = Mathf.Clamp01(t / 0.5f);
            placeLabel.transform.localScale = Vector3.one * Back(pop);

            var standings = race.Standings;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                bool used = i < standings.Count;
                row.gameObject.SetActive(used);
                if (!used) continue;
                var car = standings[i];
                var entry = online.EntryOf(car);
                bool you = car == race.Player;
                row.Find("Content/Place").GetComponent<TMP_Text>().text = (i + 1).ToString();
                row.Find("Content/Name").GetComponent<TMP_Text>().text = you ? "YOU" : entry != null ? entry.Name : car.DisplayName;
                row.Find("Content/Car/Body").GetComponent<Image>().color = entry != null ? entry.Color : Theme.Orange;
                var time = row.Find("Content/Time").GetComponent<TMP_Text>();
                if (car.IsFinished) time.text = TimeFormat.Race(car.FinishTime - race.StartTime);
                else time.text = "LAP " + car.CurrentLap + "/" + car.TotalLaps;
                time.color = car.IsFinished ? Theme.Cream : Theme.Stone;
                row.Find("Content/Highlight").gameObject.SetActive(you);
                // Rows slide in one after another.
                float k = Mathf.Clamp01((t - 0.25f - i * 0.06f) / 0.3f);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                var content = (RectTransform)row.Find("Content");
                content.anchoredPosition = new Vector2(120f * (1f - e), 0f);
                row.GetComponent<CanvasGroup>().alpha = e;
            }

            bool host = online.IsHost;
            lobbyButton.gameObject.SetActive(host);
            hintLabel.text = host ? "" : "THE HOST TAKES EVERYONE BACK TO THE ROOM";
        }

        static string Ordinal(int n) => n + (n % 100 is >= 11 and <= 13 ? "TH" : (n % 10) switch { 1 => "ST", 2 => "ND", 3 => "RD", _ => "TH" });

        static float Back(float t)
        {
            const float s = 1.7f;
            float u = Mathf.Clamp01(t) - 1f;
            return 1f + u * u * ((s + 1f) * u + s);
        }
    }
}
