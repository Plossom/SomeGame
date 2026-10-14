using System.Collections.Generic;
using SomeGame.Online;
using SomeGame.Race;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.UI
{
    /// <summary>
    /// The online race's screens: "waiting for racers" until every phone has loaded; after your finish
    /// the winner's moment (rays, the trophy dropping in, the winner's car rolling in, confetti, WINNER and
    /// the name; tap to go on), then the ranking, live: medals for the first three, each racer's car colour,
    /// name, time and gap to the winner (or the lap they are on), the crown on the winner's row. The host
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

        [Header("Winner")]
        [SerializeField] GameObject winner;
        [SerializeField] Image winnerDim;
        [SerializeField] RectTransform winnerRays, winnerTrophy, winnerCar;
        [SerializeField] Image winnerCarBody;
        [SerializeField] TMP_Text winnerCaption, winnerName, winnerTap;
        [SerializeField] Button winnerSkip;
        [SerializeField] RectTransform confettiLayer;
        [SerializeField] Sprite confettiSprite;

        static readonly Color Gold = new(1f, 0.78f, 0.2f), Silver = new(0.8f, 0.84f, 0.88f), Bronze = new(0.86f, 0.55f, 0.3f);

        float _shownAt = -10f, _winnerAt = -10f;
        bool _winnerDone;
        readonly List<(RectTransform rt, Vector2 velocity, float spin)> _confetti = new();

        void Awake()
        {
            waiting.SetActive(false);
            results.SetActive(false);
            closed.SetActive(false);
            winner.SetActive(false);
            lobbyButton.onClick.AddListener(() => OnlineRoom.Current?.BackToLobby());
            leaveButton.onClick.AddListener(() =>
            {
                OnlineSession.Instance.Leave();
                GameSession.ReturnToMap();
            });
            closedButton.onClick.AddListener(GameSession.ReturnToMap);
            winnerSkip.onClick.AddListener(EndWinner);
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

        // Your finish: first the winner's moment, then the ranking.
        void ShowResults()
        {
            var online = race.Online;
            if (online == null) return;
            var level = race.Level;
            raceLabel.text = level != null ? level.displayName.ToUpperInvariant() : "";
            var first = online.Winner;
            if (first == null) { EndWinner(); return; }
            bool you = first.Car == race.Player.GetComponent<SomeGame.Car.CarMovement>();
            winnerCaption.text = you ? "YOU WIN!" : "WINNER";
            winnerName.text = you ? OnlineSession.PlayerName : first.Name;
            winnerName.color = Color.Lerp(first.Color, Color.white, 0.15f);
            winnerCarBody.color = first.Color;
            _winnerAt = Time.unscaledTime;
            _winnerDone = false;
            winner.SetActive(true);
            SpawnConfetti();
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.fanfare, 1f);
        }

        void EndWinner()
        {
            if (_winnerDone) return;
            _winnerDone = true;
            _shownAt = Time.unscaledTime;
            results.SetActive(true);
        }

        void OnClosed(string message)
        {
            closedLabel.text = message;
            closed.SetActive(true);
            results.SetActive(false);
            winner.SetActive(false);
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
            if (winner.activeSelf) AnimateWinner();
            if (results.activeSelf) AnimateResults(online);
        }

        void AnimateWinner()
        {
            float t = Time.unscaledTime - _winnerAt;
            // Fading out once the ranking is up.
            float fade = _winnerDone ? 1f - Mathf.Clamp01((Time.unscaledTime - _shownAt) / 0.35f) : 1f;
            if (_winnerDone && fade <= 0f)
            {
                winner.SetActive(false);
                foreach (var c in _confetti) if (c.rt != null) Destroy(c.rt.gameObject);
                _confetti.Clear();
                return;
            }
            winnerDim.color = new Color(0f, 0f, 0f, 0.82f * Mathf.Clamp01(t / 0.3f) * fade);
            // Rays turn slowly behind the trophy.
            winnerRays.localRotation = Quaternion.Euler(0f, 0f, -t * 14f);
            winnerRays.localScale = Vector3.one * Ease(Mathf.Clamp01((t - 0.1f) / 0.6f)) * fade;
            // The trophy drops in with a bounce, then floats.
            float drop = Mathf.Clamp01((t - 0.15f) / 0.55f);
            winnerTrophy.anchoredPosition = new Vector2(0f, Mathf.Lerp(900f, 0f, Ease(drop)) + (drop >= 1f ? Mathf.Sin(t * 2.4f) * 14f : 0f) + 330f);
            winnerTrophy.localScale = Vector3.one * Back(drop) * fade;
            winnerTrophy.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f) * 4f);
            // The winner's car rolls in from the left with a little drift, and bounces to a stop.
            float roll = Mathf.Clamp01((t - 0.6f) / 0.7f);
            float e = Ease(roll);
            winnerCar.anchoredPosition = new Vector2(Mathf.Lerp(-900f, 0f, e), -40f + (roll >= 1f ? Mathf.Abs(Mathf.Sin(t * 5f)) * 10f * Mathf.Exp(-(t - 1.3f) * 2f) : 0f));
            winnerCar.localRotation = Quaternion.Euler(0f, 0f, -90f + 25f * (1f - e) * Mathf.Sin(roll * Mathf.PI));
            winnerCar.localScale = Vector3.one * fade;
            // WINNER, then the name.
            float title = Mathf.Clamp01((t - 1.1f) / 0.4f), name = Mathf.Clamp01((t - 1.35f) / 0.4f);
            winnerCaption.transform.localScale = Vector3.one * Back(title);
            winnerCaption.alpha = title * fade;
            winnerName.transform.localScale = Vector3.one * Back(name);
            winnerName.alpha = name * fade;
            winnerTap.alpha = Mathf.Clamp01((t - 2.4f) / 0.4f) * (0.6f + 0.4f * Mathf.Sin(t * 4f)) * fade;
            if (t > 6f) EndWinner(); // goes on by itself after a while

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _confetti.Count; i++)
            {
                var (rt, v, spin) = _confetti[i];
                if (rt == null) continue;
                v.y -= 900f * dt;
                v *= Mathf.Exp(-dt * 0.8f);
                rt.anchoredPosition += v * dt;
                rt.localRotation = Quaternion.Euler(0f, 0f, rt.localEulerAngles.z + spin * dt);
                var img = rt.GetComponent<Image>();
                var c = img.color; c.a = fade; img.color = c;
                _confetti[i] = (rt, v, spin);
            }
        }

        void SpawnConfetti()
        {
            Color[] colours = { Gold, Theme.Orange, new(0.24f, 0.49f, 0.85f), new(0.6f, 0.83f, 0.28f), new(0.91f, 0.44f, 0.69f), Color.white };
            for (int i = 0; i < 110; i++)
            {
                var go = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(confettiLayer, false);
                rt.sizeDelta = new Vector2(Random.Range(16f, 28f), Random.Range(28f, 46f));
                rt.anchoredPosition = new Vector2(Random.Range(-120f, 120f), Random.Range(200f, 420f));
                var img = go.GetComponent<Image>();
                img.sprite = confettiSprite;
                img.type = Image.Type.Sliced;
                img.color = colours[i % colours.Length];
                img.raycastTarget = false;
                float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                _confetti.Add((rt, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(700f, 1700f), Random.Range(-600f, 600f)));
            }
        }

        void AnimateResults(OnlineRace online)
        {
            float t = Time.unscaledTime - _shownAt;
            resultsGroup.alpha = Mathf.Clamp01(t / 0.35f);
            int place = race.PositionOf(race.Player);
            placeLabel.text = Ordinal(place);
            placeLabel.transform.localScale = Vector3.one * Back(Mathf.Clamp01(t / 0.5f));

            var standings = race.Standings;
            var first = online.Winner;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                bool used = i < standings.Count;
                row.gameObject.SetActive(used);
                if (!used) continue;
                var car = standings[i];
                var entry = online.EntryOf(car);
                bool you = car == race.Player;
                var placeText = row.Find("Content/Place").GetComponent<TMP_Text>();
                placeText.text = (i + 1).ToString();
                var medal = row.Find("Content/Medal").GetComponent<Image>();
                medal.gameObject.SetActive(car.IsFinished && i < 3);
                medal.color = i == 0 ? Gold : i == 1 ? Silver : Bronze;
                placeText.color = car.IsFinished && i < 3 ? Theme.Ink : Theme.Amber;
                row.Find("Content/Name").GetComponent<TMP_Text>().text = you ? "YOU" : entry != null ? entry.Name : car.DisplayName;
                row.Find("Content/Car/Body").GetComponent<Image>().color = entry != null ? entry.Color : Theme.Orange;
                var time = row.Find("Content/Time").GetComponent<TMP_Text>();
                var sub = row.Find("Content/Sub").GetComponent<TMP_Text>();
                bool isWinner = first != null && entry == first;
                row.Find("Content/Crown").gameObject.SetActive(isWinner);
                if (car.IsFinished)
                {
                    float seconds = car.FinishTime - race.StartTime;
                    time.text = TimeFormat.Race(seconds);
                    sub.text = isWinner ? "WINNER" : first != null ? "+" + (car.FinishTime - first.Progress.FinishTime).ToString("0.00") + " S" : "";
                    sub.color = isWinner ? Gold : Theme.Stone;
                }
                else
                {
                    time.text = "LAP " + car.CurrentLap + "/" + car.TotalLaps;
                    sub.text = "STILL RACING" + new string('.', 1 + Mathf.FloorToInt(Time.unscaledTime * 3f) % 3);
                    sub.color = Theme.Stone;
                }
                time.color = car.IsFinished ? Theme.Cream : Theme.Stone;
                row.Find("Content/Highlight").gameObject.SetActive(you);
                // Rows slide in one after another.
                float k = Mathf.Clamp01((t - 0.25f - i * 0.07f) / 0.32f);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                var content = (RectTransform)row.Find("Content");
                content.anchoredPosition = new Vector2(140f * (1f - e), 0f);
                row.GetComponent<CanvasGroup>().alpha = e;
            }

            bool host = online.IsHost;
            lobbyButton.gameObject.SetActive(host);
            hintLabel.text = host ? "" : "THE HOST TAKES EVERYONE BACK TO THE ROOM";
        }

        static string Ordinal(int n) => n + (n % 100 is >= 11 and <= 13 ? "TH" : (n % 10) switch { 1 => "ST", 2 => "ND", 3 => "RD", _ => "TH" });

        static float Ease(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

        static float Back(float t)
        {
            const float s = 1.7f;
            float u = Mathf.Clamp01(t) - 1f;
            return 1f + u * u * ((s + 1f) * u + s);
        }
    }
}
