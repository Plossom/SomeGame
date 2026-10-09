using System.Collections.Generic;
using SomeGame.City;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The neon city start screen: labels over each district and the garage, taps on the city,
    /// the district sheet and the garage screen, and camera focus.
    /// </summary>
    public class CityScreen : MonoBehaviour
    {
        [SerializeField] CityGenerator city;
        [SerializeField] CityCamera cityCamera;
        [SerializeField] RectTransform markersParent;
        [SerializeField] DistrictMarker markerTemplate;
        [SerializeField] DistrictMarker garageMarker;
        [SerializeField] DistrictSheet sheet;
        [SerializeField] GarageScreen garage;
        [SerializeField] TMP_Text totalStars;
        [Tooltip("Taps within this distance of the garage open it (world units).")]
        [SerializeField, Min(1f)] float garageTapRadius = 16f;
        [SerializeField] float overviewDistance = 560f;
        [SerializeField] float focusDistance = 240f;

        readonly List<(CityZone zone, DistrictMarker marker)> _markers = new();
        UnityEngine.Camera _camera;

        void Start()
        {
            _camera = cityCamera.GetComponent<UnityEngine.Camera>();
            totalStars.text = ProgressStore.TotalStars.ToString();
            markerTemplate.gameObject.SetActive(false);

            foreach (var zone in city.Zones)
            {
                if (zone.District == null) continue;
                var marker = Instantiate(markerTemplate, markersParent);
                marker.gameObject.SetActive(true);
                var z = zone;
                marker.Setup(_camera, zone.LabelPoint, () => Select(z));
                _markers.Add((zone, marker));
                Bind(zone, marker);
            }
            if (garageMarker != null) garageMarker.gameObject.SetActive(false); // the neon sign on the building says it all

            cityCamera.Tapped += OnTapped;

            var last = GameSession.CurrentDistrict;
            var lastZone = last != null ? _markers.Find(m => m.zone.District == last).zone : null;
            if (lastZone != null)
            {
                cityCamera.Jump(FocusPoint(lastZone), focusDistance);
                sheet.Show(last);
            }
            else
            {
                cityCamera.Jump(new Vector3(0f, 0f, 14f), overviewDistance);
            }
        }

        void Update()
        {
            bool focused = sheet.IsOpen || garage.IsOpen;
            foreach (var (_, marker) in _markers) marker.HideOffScreen = focused;
        }

        static void Bind(CityZone zone, DistrictMarker marker)
        {
            var d = zone.District;
            if (d.ComingSoon)
                marker.Show(d.displayName, "SOON", d.accent, true, true);
            else if (!ProgressStore.IsUnlocked(d))
                marker.Show(d.displayName, d.starsRequired.ToString(), d.accent, true, true);
            else
                marker.Show(d.displayName, $"{ProgressStore.StarsIn(d)}/{d.MaxStars}", d.accent, false, false);
        }

        // Keep the district in the upper part of the screen, above the sheet.
        Vector3 FocusPoint(CityZone zone) => zone.Center + new Vector3(0f, 0f, -55f);

        void OnTapped(Vector3 point)
        {
            if (garage.IsOpen) return;
            if (Vector3.Distance(point, city.GaragePoint) < garageTapRadius)
            {
                OpenGarage();
                return;
            }
            foreach (var (zone, _) in _markers)
            {
                if (!zone.Contains(point)) continue;
                Select(zone);
                return;
            }
            sheet.Hide();
        }

        void Select(CityZone zone)
        {
            if (garage.IsOpen) return;
            cityCamera.FocusOn(FocusPoint(zone), focusDistance);
            sheet.Show(zone.District);
        }

        void OpenGarage()
        {
            sheet.Hide();
            cityCamera.FocusOn(city.GaragePoint + new Vector3(0f, 0f, -20f), 150f);
            garage.Show();
        }
    }
}
