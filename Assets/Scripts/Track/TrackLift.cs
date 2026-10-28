using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// A cable car across a ravine (a <see cref="TrackLayout.Gap"/> marked <c>lift</c>): drive onto the valley
    /// station's platform and the hanger grips your vehicle, carries it up the cable over the gap and sets it
    /// down at the top station driving on. Rivals ride it too. Made by <see cref="TrackHazards"/>.
    /// </summary>
    public class TrackLift : MonoBehaviour
    {
        const float RideSpeed = 13f, LeaveShare = 0.7f, StationLength = 7f;

        class Lift
        {
            public float boardFrom, boardTo, arrive;
            public List<Vector2> cable;
            public Pose arrival;
        }

        Track _track;
        Sprite _hanger;
        readonly List<Lift> _lifts = new();
        readonly Dictionary<SomeGame.Car.CarMovement, SpriteRenderer> _hangers = new();
        SomeGame.Car.CarMovement[] _cars;
        SomeGame.UI.ComboDisplay _combo;

        public void Init(Track track, Sprite station, Sprite tower, Sprite cable, Sprite hanger, int order)
        {
            _track = track;
            _hanger = hanger;
            var path = track.Path;
            float half = track.Layout.roadWidth * 0.5f + track.Layout.kerbWidth;
            foreach (var gap in track.Layout.gaps)
            {
                if (!gap.lift) continue;
                var lift = new Lift { boardFrom = gap.from - StationLength - 2f, boardTo = gap.from - 1f, arrive = gap.to + 4f, cable = new List<Vector2>() };
                for (float d = gap.from - 3f; d <= gap.to + 1f; d += 2f) lift.cable.Add(path.PointAt(d));
                Vector2 t = path.TangentAt(lift.arrive);
                lift.arrival = new Pose(path.PointAt(lift.arrive), Quaternion.Euler(0f, 0f, Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg - 90f));
                _lifts.Add(lift);

                // The two stations lie across the road, the towers stand beside them, the cable runs between.
                foreach (float d in new[] { gap.from - StationLength * 0.5f - 1f, gap.to + StationLength * 0.5f })
                {
                    Vector2 tan = path.TangentAt(d);
                    float angle = Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg - 90f;
                    var deck = Place("LiftStation", station, path.PointAt(d), angle, order - 50);
                    if (station != null) deck.transform.localScale = new Vector3(half * 2f / station.bounds.size.x, StationLength / station.bounds.size.y, 1f);
                    Vector2 n = path.NormalAt(d);
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var post = Place("LiftTower", tower, path.PointAt(d) + n * side * (half + 2.2f), angle, order + 12);
                        if (tower != null) post.transform.localScale = Vector3.one * (3.2f / tower.bounds.size.x);
                    }
                }
                // A long cable has pylons along the way (bigger: they stand tall up the mountain).
                float span = path.DeltaDistance(gap.from, gap.to);
                int pylons = Mathf.FloorToInt(span / 34f);
                for (int k = 1; k <= pylons; k++)
                {
                    float d = gap.from + span * k / (pylons + 1f);
                    Vector2 tan = path.TangentAt(d);
                    var pylon = Place("LiftPylon", tower, path.PointAt(d), Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg - 90f, order + 18);
                    if (tower != null) pylon.transform.localScale = Vector3.one * (4.4f / tower.bounds.size.x);
                }
                // Cable: two lines either side of the road's centre (up and down), above everything.
                var line = new List<Vector2> { path.PointAt(gap.from - StationLength - 1f) };
                line.AddRange(lift.cable);
                line.Add(path.PointAt(gap.to + StationLength + 1f));
                for (int s = -1; s <= 1; s += 2)
                    for (int i = 1; i < line.Count; i++)
                    {
                        Vector2 a = line[i - 1], b = line[i], dir = b - a;
                        if (dir.sqrMagnitude < 0.01f) continue;
                        Vector2 off = new Vector2(-dir.y, dir.x).normalized * s * 0.9f;
                        var seg = Place("LiftCable", cable, (a + b) * 0.5f + off, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, order + 20);
                        if (cable != null) seg.transform.localScale = new Vector3((dir.magnitude + 0.05f) / cable.bounds.size.x, 0.14f / cable.bounds.size.y, 1f);
                    }
            }
        }

        SpriteRenderer Place(string name, Sprite sprite, Vector2 position, float angle, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        void Start()
        {
            _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
        }

        void FixedUpdate()
        {
            if (_cars == null) return;
            var path = _track.Path;
            float half = _track.Layout.roadWidth * 0.5f + _track.Layout.kerbWidth;
            foreach (var car in _cars)
            {
                if (car == null || car.IsAirborne || !car.isActiveAndEnabled) continue;
                var sensor = car.GetComponent<TrackSensor>();
                if (sensor == null || sensor.Branch >= 0) continue;
                float d = sensor.Current.Distance;
                foreach (var lift in _lifts)
                {
                    if (path.DeltaDistance(lift.boardFrom, d) < 0f || path.DeltaDistance(d, lift.boardTo) < 0f) continue;
                    if (Mathf.Abs(sensor.Current.Lateral) > half + 1f) continue;
                    Board(car, lift, d);
                    break;
                }
            }
        }

        void Board(SomeGame.Car.CarMovement car, Lift lift, float from)
        {
            float top = car.Stats != null ? car.Stats.topSpeed : 18f;
            var progress = car.GetComponent<SomeGame.Race.RaceProgress>();
            // A long ride goes faster (about five seconds up the mountain).
            float length = 0f;
            for (int i = 1; i < lift.cable.Count; i++) length += Vector2.Distance(lift.cable[i - 1], lift.cable[i]);
            bool ok = car.RideLift(lift.cable, lift.arrival, Mathf.Max(RideSpeed, length / 5.5f), top * LeaveShare, () =>
            {
                if (progress != null) progress.LiftArrived(from, lift.arrive);
                ShowHanger(car, false);
            });
            if (!ok) return;
            ShowHanger(car, true);
            if (car.GetComponent<SomeGame.Input.PlayerCarInput>() == null) return;
            if (_combo != null) _combo.Announce("GONDOLA!", new Color(1f, 0.45f, 0.35f));
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.7f);
        }

        // The red hanger round the vehicle while it rides (a child of the car's picture, so it scales with
        // the ride's rise and fall).
        void ShowHanger(SomeGame.Car.CarMovement car, bool show)
        {
            if (!_hangers.TryGetValue(car, out var hanger) || hanger == null)
            {
                if (!show || _hanger == null) return;
                Transform visual = null;
                foreach (var sr in car.GetComponentsInChildren<SpriteRenderer>(true)) if (sr.name == "Visual") visual = sr.transform;
                if (visual == null) return;
                var go = new GameObject("LiftHanger");
                go.transform.SetParent(visual, false);
                hanger = go.AddComponent<SpriteRenderer>();
                hanger.sprite = _hanger;
                hanger.sortingOrder = 16;
                _hangers[car] = hanger;
            }
            hanger.enabled = show;
        }
    }
}
