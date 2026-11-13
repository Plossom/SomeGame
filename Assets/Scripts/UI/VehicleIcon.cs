using SomeGame.Car;
using SomeGame.Race;
using UnityEngine;
using Vehicle = SomeGame.Track.TrackLayout.Vehicle;

namespace SomeGame.UI
{
    /// <summary>
    /// A car in the menus (on the map, on the loading bar, on the garage's stage, on a rival's card): the car as
    /// built in the garage (<see cref="Dress"/>: its body, decal, part on top and paint), or on the Winter Cup the
    /// snowmobile (which takes only the paint). Swaps the body (tinted), details and shadow pictures; the decal and
    /// the top are images of their own laid over it.
    /// </summary>
    public class VehicleIcon : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Image body;
        [SerializeField] UnityEngine.UI.Image details;
        [SerializeField] UnityEngine.UI.Image shadow;
        [SerializeField] Sprite snowmobileBody;
        [SerializeField] Sprite snowmobileDetails;
        [SerializeField] Sprite snowmobileShadow;

        Sprite _carBody, _carDetails, _carShadow;
        bool _remembered;
        Vehicle _vehicle;
        public Vehicle Vehicle => _vehicle;
        CarLook? _look;
        UnityEngine.UI.Image _decal, _top;

        void Remember()
        {
            if (_remembered) return;
            _carBody = body != null ? body.sprite : null;
            _carDetails = details != null ? details.sprite : null;
            _carShadow = shadow != null ? shadow.sprite : null;
            _remembered = true;
        }

        public void Show(bool snowmobile) => Show(snowmobile ? Vehicle.Snowmobile : Vehicle.Car);

        /// <summary>Which vehicle it shows: the car, the snowmobile or the boat (each in its own setup).</summary>
        public void Show(Vehicle vehicle)
        {
            Remember();
            _vehicle = vehicle;
            Refresh();
        }

        /// <summary>The body in a paint (the rest as it is).</summary>
        public void Paint(Color color)
        {
            if (body != null) body.color = color;
        }

        /// <summary>The car as built: body, decal, top and paint.</summary>
        public void Dress(CarLook look)
        {
            Remember();
            _look = look;
            Refresh();
        }

        void Refresh()
        {
            var art = GarageArt.Instance;
            // A body of another cup (a snowmobile's, a boat's) makes it that vehicle; on a vehicle a body of another
            // cup doesn't show (its own plain one does).
            var bodyPart = _look.HasValue ? _look.Value.Body : null;
            var shown = bodyPart != null && bodyPart.Cup != "Valley" ? Garage.VehicleOfCup(bodyPart.Cup) : _vehicle;
            string cup = Garage.CupOf(shown);
            bool Fits(Garage.Part part) => part != null && part.Cup == cup;
            var entry = art != null && Fits(bodyPart) ? art.Get(bodyPart) : null;
            // The plain one of each vehicle (when no body of its own is chosen).
            var plain = shown == Vehicle.Boat && art != null ? art.Get("body.speedboat") : null;
            Sprite plainBody = shown switch { Vehicle.Snowmobile when snowmobileBody != null => snowmobileBody, Vehicle.Boat when plain?.full != null => plain.full, _ => _carBody };
            Sprite plainDetails = shown switch { Vehicle.Snowmobile when snowmobileDetails != null => snowmobileDetails, Vehicle.Boat when plain?.details != null => plain.details, _ => _carDetails };
            Sprite plainShadow = shown switch { Vehicle.Snowmobile when snowmobileShadow != null => snowmobileShadow, Vehicle.Boat when art?.Get("shadow.boat")?.full != null => art.Get("shadow.boat").full, _ => _carShadow };
            if (body != null)
            {
                body.sprite = entry?.full != null ? entry.full : plainBody;
                if (_look.HasValue) body.color = _look.Value.Paint;
            }
            if (details != null) details.sprite = entry?.details != null ? entry.details : plainDetails;
            if (shadow != null) shadow.sprite = plainShadow;
            Sprite decal = null, top = null;
            if (_look.HasValue && art != null)
            {
                if (Fits(_look.Value.Decal)) decal = art.Get(_look.Value.Decal)?.full;
                if (Fits(_look.Value.Top)) top = art.Get(_look.Value.Top)?.full;
            }
            Overlay(ref _decal, "Decal", decal, details != null ? details.transform.GetSiblingIndex() : -1);
            Overlay(ref _top, "Top", top, -1);
        }

        // An image over the body (the decal under the details, the top over everything).
        void Overlay(ref UnityEngine.UI.Image image, string name, Sprite sprite, int index)
        {
            if (image == null)
            {
                if (sprite == null || body == null) return;
                var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rt.SetParent(body.transform.parent, false);
                var from = (RectTransform)body.transform;
                rt.anchorMin = from.anchorMin; rt.anchorMax = from.anchorMax; rt.pivot = from.pivot;
                rt.anchoredPosition = from.anchoredPosition; rt.sizeDelta = from.sizeDelta;
                rt.localRotation = from.localRotation; rt.localScale = from.localScale;
                image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.raycastTarget = false;
                image.preserveAspect = body.preserveAspect;
            }
            image.transform.SetSiblingIndex(index >= 0 ? index : image.transform.parent.childCount - 1);
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
