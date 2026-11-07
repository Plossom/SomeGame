using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// CHICKEN CHASE, the Valley Cup's challenge: Rosie Rust's chickens got out all over Meadow Run. No rivals; at GO
    /// the clock runs down from <see cref="Seconds"/> and every chicken driven into is caught. All caught: won (the
    /// first time, the CHICKEN RIDER for your roof). The clock runs out: over. The HUD shows the count and the time
    /// left; the result shows over everything (<see cref="SomeGame.UI.ChallengeResult"/>).
    /// </summary>
    public class ChickenChase : MonoBehaviour
    {
        public const string Id = "ChickenChase";
        const float Seconds = 75f;

        RaceManager _race;
        readonly List<Chicken> _chickens = new();
        bool _over, _greeted;

        public int Total { get; private set; } = 15;
        public int Caught { get; private set; }
        public float TimeLeft => _race == null || _race.State == RaceState.Countdown ? Seconds : Mathf.Max(0f, Seconds - _race.RaceTime);

        void Start()
        {
            _race = GetComponent<RaceManager>();
            var track = FindAnyObjectByType<SomeGame.Track.Track>();
            var sprite = GarageArt.Instance?.Get("challenge.chicken")?.full;
            if (track == null || sprite == null || _race.Player == null) { Total = 0; return; }
            // Spread round one lap, ahead of the grid, at random spots across the road.
            var path = track.Path;
            float start = path.Project(_race.Player.transform.position).Distance;
            for (int i = 0; i < Total; i++)
            {
                float d = path.WrapDistance(start + 40f + (path.Length - 60f) * i / Total + Random.Range(-4f, 4f));
                float lateral = Random.Range(-1f, 1f) * track.HalfWidthAt(d) * 0.7f;
                _chickens.Add(Chicken.Spawn(track, d, lateral, sprite, _race.Player.transform, OnCaught));
            }
        }

        void OnCaught(Chicken chicken)
        {
            if (_over) return;
            Caught++;
            var combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            if (combo != null) combo.Banner(Caught == Total ? "ALL CAUGHT!" : $"CHICKEN {Caught}/{Total}!", new Color(1f, 0.85f, 0.3f));
            if (Caught >= Total) End(true);
        }

        void Update()
        {
            if (_race == null || _over) return;
            // Rosie asks for help during the countdown.
            if (!_greeted && _race.State == RaceState.Countdown)
            {
                _greeted = true;
                var rosie = Resources.Load<RivalCharacter>("Rivals/RosieRust");
                var watch = FindAnyObjectByType<SomeGame.UI.RivalWatch>();
                if (rosie != null && watch != null) watch.Say(rosie, "My chickens got out! Catch all of them before dinner, rookie!", 2.8f);
            }
            if (_race.State == RaceState.Racing && TimeLeft <= 0f) End(false);
        }

        void End(bool won)
        {
            _over = true;
            if (_race.Player.TryGetComponent(out SomeGame.Car.CarMovement car)) car.ControlsEnabled = false;
            foreach (var chicken in _chickens) if (chicken != null) chicken.enabled = false;
            bool first = ChallengeProgress.Record(Id, Caught, won);
            var result = FindAnyObjectByType<SomeGame.UI.ChallengeResult>(FindObjectsInactive.Include);
            if (result != null) result.Show(won, Caught, Total, ChallengeProgress.Best(Id), first);
        }
    }
}
