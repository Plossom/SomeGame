using SomeGame.Race;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// The HAY BALE DROP gadget: drops a hay bale on the road just behind the car. Yours from the garage holds one
    /// bale, filled again every lap (the gadget button in the HUD). Rosie Rust's (<see cref="Ai"/>) drops one when
    /// a car tucks in close behind her, twice a lap at most.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class HayBaleGadget : MonoBehaviour
    {
        /// <summary>Dropped by the rival themselves.</summary>
        public bool Ai { get; set; }
        public int Charges { get; private set; }
        public int MaxCharges => Ai ? 2 : 1;
        /// <summary>A bale was dropped.</summary>
        public event System.Action Dropped;

        CarMovement _car;
        RaceProgress _progress;
        RaceManager _race;
        Sprite _sprite;
        int _lap = -1;
        float _lastDrop = -10f;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _progress = GetComponent<RaceProgress>();
            _race = FindAnyObjectByType<RaceManager>();
            _sprite = GarageArt.Instance?.Get("gadget.haybale")?.full;
        }

        public bool Ready => Charges > 0 && _race != null && _race.State == RaceState.Racing && !_progress.IsFinished && !_car.IsAirborne && Time.time - _lastDrop > 0.5f;

        public bool Drop()
        {
            if (!Ready || _sprite == null) return false;
            Charges--;
            _lastDrop = Time.time;
            HayBale.Drop(_car, _sprite, (Vector2)transform.position - (Vector2)transform.up * 2.3f);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.land, 0.7f);
            Dropped?.Invoke();
            return true;
        }

        void Update()
        {
            if (_race == null || _progress == null || _race.State != RaceState.Racing) return;
            // A fresh lap: the bales are back.
            int lap = _progress.LapsCompleted;
            if (lap != _lap) { _lap = lap; Charges = MaxCharges; }
            if (Ai) Think();
        }

        // Rosie: somebody right behind her, on her line, after the start has sorted itself out: hay!
        void Think()
        {
            if (_race.RaceTime < 6f || Time.time - _lastDrop < 6f || !Ready) return;
            float me = _progress.Metres;
            foreach (var other in _race.Standings)
            {
                if (other == null || other == _progress || other.IsFinished) continue;
                float behind = me - other.Metres;
                if (behind < 4f || behind > 22f) continue;
                Vector2 toThem = other.transform.position - transform.position;
                if (Vector2.Angle(-transform.up, toThem) > 35f) continue;
                if (other == _race.Player || Random.value < 0.35f) { Drop(); return; }
            }
        }
    }
}
