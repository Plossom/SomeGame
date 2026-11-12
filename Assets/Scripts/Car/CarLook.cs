using SomeGame.Race;
using UnityEngine;
using Vehicle = SomeGame.Track.TrackLayout.Vehicle;

namespace SomeGame.Car
{
    /// <summary>
    /// How a car looks: its body, the part on top, a decal, the paint and the trail behind it. The player's comes
    /// from the <see cref="Garage"/>; a rival with a face has their own (<see cref="RivalCharacter"/>).
    /// </summary>
    public struct CarLook
    {
        public Garage.Part Body, Top, Decal, Trail;
        public Color Paint;

        /// <summary>Your car as built in the garage.</summary>
        public static CarLook Player => For(Vehicle.Car);

        /// <summary>A vehicle as built in the garage (each vehicle has its own page there).</summary>
        public static CarLook For(Vehicle vehicle) => new()
        {
            Body = Garage.Equipped(Garage.Slot.Body, vehicle), Top = Garage.Equipped(Garage.Slot.Top, vehicle), Decal = Garage.Equipped(Garage.Slot.Decal, vehicle),
            Trail = Garage.Equipped(Garage.Slot.Trail, vehicle), Paint = Garage.ColorOf(vehicle),
        };

        /// <summary>The vehicle a race is driven in (the car, or the snowmobile on a snowmobile track).</summary>
        public static Vehicle VehicleOf(LevelDefinition level) =>
            level != null && level.track != null ? level.track.vehicle : Vehicle.Car;

        public static CarLook Of(string body, string top, string decal, Color paint) => new()
        {
            Body = Garage.Find(body, Garage.Slot.Body), Top = Garage.Find(top, Garage.Slot.Top), Decal = Garage.Find(decal, Garage.Slot.Decal),
            Trail = Garage.Find("", Garage.Slot.Trail), Paint = paint,
        };

        /// <summary>
        /// Dresses a race car: its body and details swapped, the decal and the top laid over it, the paint on the body
        /// (and its tyre marks) and the trail. <paramref name="car"/> false: on the snowmobile (the track's own
        /// vehicle), which takes the snowmobile's parts (the Winter Cup's) and none of the car's.
        /// </summary>
        public void Apply(CarMovement target, bool car)
        {
            // Remembered on the car: the track swaps its vehicle's sprites in (maybe after this), then dresses it again.
            var dress = target.GetComponent<CarDress>() ?? target.gameObject.AddComponent<CarDress>();
            dress.Look = this; dress.Car = car;
            string cup = Garage.CupOf(car ? Vehicle.Car : Vehicle.Snowmobile);
            bool Fits(Garage.Part part) => part != null && part.Cup == cup;
            var art = GarageArt.Instance;
            SpriteRenderer visual = null, details = null;
            foreach (var sr in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name == "Visual") visual = sr;
                else if (sr.name == "Details") details = sr;
            }
            if (visual == null) return;
            visual.color = Paint;
            foreach (var marks in target.GetComponents<TyreMarks>()) marks.Tint = new Color(Paint.r, Paint.g, Paint.b, marks.Tint.a);
            var body = art != null && Fits(Body) ? art.Get(Body) : null;
            if (body != null)
            {
                if (body.race != null) visual.sprite = body.race;
                if (details != null && body.details != null) details.sprite = body.details;
            }
            if (details != null) details.sortingOrder = 12; // the decal goes between the body and its details
            Layer(visual, "Decal", art != null && Fits(Decal) ? art.Get(Decal)?.full : null, 11);
            Layer(visual, "Top", art != null && Fits(Top) ? art.Get(Top)?.full : null, 13);
            if (Trail != null && Trail.Id != "standard")
            {
                if (!target.TryGetComponent(out TrailFx trail)) trail = target.gameObject.AddComponent<TrailFx>();
                trail.Use(Trail, Paint);
            }
        }

        static void Layer(SpriteRenderer visual, string name, Sprite sprite, int order)
        {
            var t = visual.transform.Find(name);
            if (sprite == null)
            {
                if (t != null) t.gameObject.SetActive(false);
                return;
            }
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(visual.transform, false);
                t.gameObject.AddComponent<SpriteRenderer>();
            }
            t.gameObject.SetActive(true);
            var sr = t.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
        }
    }

    /// <summary>The look a race car was last dressed in (<see cref="CarLook.Apply"/>), to dress it again after its sprites are swapped.</summary>
    public class CarDress : MonoBehaviour
    {
        public CarLook Look;
        public bool Car;

        public void Again() => Look.Apply(GetComponent<CarMovement>(), Car);
    }
}
