using SomeGame.Race;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// A rival's gadget on a car (yours from the garage, or the rival's own in their home race): a few charges,
    /// filled again every lap, used with the gadget button in the HUD (<see cref="SomeGame.UI.GadgetButton"/>) or,
    /// on a rival's car (<see cref="Ai"/>), when they see their chance. Each kind says what it does in <see cref="Fire"/>.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public abstract class Gadget : MonoBehaviour
    {
        /// <summary>Used by the rival themselves.</summary>
        public bool Ai { get; set; }
        public int Charges { get; private set; }
        public virtual int MaxCharges => Ai ? 2 : 1;
        /// <summary>Its garage part id (its icon is GarageArt's "gadget.{id}").</summary>
        public abstract string Id { get; }
        /// <summary>The word on the button while it is ready.</summary>
        public abstract string Shout { get; }
        /// <summary>It was used.</summary>
        public event System.Action Used;

        protected CarMovement Car { get; private set; }
        protected RaceProgress Progress { get; private set; }
        protected RaceManager Race { get; private set; }
        protected Sprite Sprite { get; private set; }
        protected float LastUse { get; private set; } = -10f;
        int _lap = -1;

        /// <summary>Puts the gadget with this garage id on a car (null for none or an unknown one).</summary>
        public static Gadget Add(GameObject car, string id, bool ai) => id switch
        {
            "haybale" => Init(car.AddComponent<HayBaleGadget>(), ai),
            "snowball" => Init(car.AddComponent<SnowballGadget>(), ai),
            "broadside" => Init(car.AddComponent<BroadsideGadget>(), ai),
            _ => null,
        };

        static Gadget Init(Gadget gadget, bool ai)
        {
            gadget.Ai = ai;
            return gadget;
        }

        protected virtual void Awake()
        {
            Car = GetComponent<CarMovement>();
            Progress = GetComponent<RaceProgress>();
            Race = FindAnyObjectByType<RaceManager>();
            Sprite = GarageArt.Instance?.Get("gadget." + Id)?.full;
        }

        public bool Ready => Charges > 0 && Race != null && Race.State == RaceState.Racing && !Progress.IsFinished && !Car.IsAirborne && Time.time - LastUse > 0.5f;

        public bool Use()
        {
            if (!Ready || Sprite == null) return false;
            Charges--;
            LastUse = Time.time;
            Fire();
            Used?.Invoke();
            return true;
        }

        /// <summary>What it does.</summary>
        protected abstract void Fire();

        /// <summary>A rival deciding whether now is the moment.</summary>
        protected abstract void Think();

        void Update()
        {
            if (Race == null || Progress == null || Race.State != RaceState.Racing) return;
            // A fresh lap: the charges are back.
            int lap = Progress.LapsCompleted;
            if (lap != _lap) { _lap = lap; Charges = MaxCharges; }
            if (Ai) Think();
        }
    }
}
