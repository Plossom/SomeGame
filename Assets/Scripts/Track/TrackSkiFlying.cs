using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The ski-flying hill (a gap marked <see cref="TrackLayout.Gap.skiFlying"/>): flags along the in-run, the landing
    /// strip below with its blue distance lines and the red K-line. In the air you steer (much more than on any other
    /// jump): a ring on the hill shows where you will come down - green over the strip, red beside it - and at the top of
    /// the flight time slows for a moment. Land on the strip: "TELEMARK!" and a big boost; beside it, down the cliff.
    /// Weak rivals sometimes take off crooked and can't save it.
    /// </summary>
    public class TrackSkiFlying : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;
        [SerializeField] Material particleMaterial;

        const int LineOrder = -44, FlagOrder = 6, RingOrder = 40;
        static readonly Color Blue = MeshKit.Hex("2E6FD8", 0.85f), Red = MeshKit.Hex("E0342B"), Groove = MeshKit.Hex("B9CCDD", 0.8f),
            FlagA = MeshKit.Hex("E0342B"), FlagB = MeshKit.Hex("1E5BC6"), Pole = MeshKit.Hex("30343C"), Shadow = new Color(0f, 0.02f, 0.08f, 0.22f),
            Good = MeshKit.Hex("4BE07A", 0.9f), Bad = MeshKit.Hex("FF4A3D", 0.9f), RingShade = new Color(0f, 0f, 0f, 0.25f);

        readonly List<GameObject> _parts = new();
        readonly Dictionary<CarMovement, int> _flying = new(); // car -> the hill's gap it flies off
        readonly Dictionary<CarMovement, System.Action<float>> _launch = new();
        readonly Dictionary<CarMovement, System.Action> _land = new();
        Transform _ringGood, _ringBad;
        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        CameraShakeProxy _shake;
        bool _slowed;
        ParticleSystem _powder;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            if (!HasHill(layout)) return;
            var root = new GameObject("SkiHill") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var lines = new MeshKit();
            var flags = new MeshKit();
            foreach (var gap in layout.gaps)
            {
                if (!gap.skiFlying) continue;
                // The in-run: ski grooves down the middle, flags on poles along both edges.
                const float inrun = 140f;
                for (float s = 0f; s < inrun; s += 2f)
                {
                    float d0 = gap.from - inrun + s, d1 = d0 + 2f;
                    foreach (float lane in new[] { -1.6f, -0.9f, 0.9f, 1.6f })
                        lines.Line(path.PointAt(d0) + path.NormalAt(d0) * lane, path.PointAt(d1) + path.NormalAt(d1) * lane, 0.16f, Groove);
                }
                int k = 0;
                for (float s = 0f; s < inrun; s += 9f, k++)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        float d = gap.from - inrun + s;
                        Vector2 at = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + 0.9f);
                        Flag(flags, at, (k + (side > 0f ? 1 : 0)) % 2 == 0 ? FlagA : FlagB);
                    }
                // The landing strip: blue distance lines every 5 m, the red K-line, flags down both sides.
                float strip = 60f;
                for (float s = 0f; s <= strip; s += 5f)
                {
                    float d = gap.to + s, w = track.OffRoadAt(d);
                    bool kLine = Mathf.Abs(s - Mathf.Round(gap.landingAt / 5f) * 5f) < 0.1f;
                    Vector2 p = path.PointAt(d), n = path.NormalAt(d), t = path.TangentAt(d);
                    float thick = kLine ? 0.7f : 0.22f;
                    lines.Quad(p - n * w - t * thick * 0.5f, p + n * w - t * thick * 0.5f, p + n * w + t * thick * 0.5f, p - n * w + t * thick * 0.5f, kLine ? Red : Blue);
                }
                k = 0;
                for (float s = 2f; s < strip; s += 7f, k++)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        float d = gap.to + s;
                        Flag(flags, path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + 0.7f), k % 2 == 0 ? FlagB : FlagA);
                    }
            }
            MeshKit.Part(root, "SkiLines", lines.Build("SkiLines"), plain, LineOrder, out _);
            MeshKit.Part(root, "SkiFlags", flags.Build("SkiFlags"), plain, FlagOrder, out _);
            if (!Application.isPlaying) return;
            // The landing ring (green / red), hidden until a flight.
            var ring = new MeshKit();
            ring.Ring(Vector2.zero, 1.3f, 1.75f, RingShade, 28);
            ring.Ring(Vector2.zero, 1.15f, 1.5f, Good, 28);
            ring.Disc(Vector2.zero, 0.25f, Good, 10);
            _ringGood = MeshKit.Part(root, "LandingGood", ring.Build("LandingGood"), plain, RingOrder, out _).transform;
            ring.Clear();
            ring.Ring(Vector2.zero, 1.3f, 1.75f, RingShade, 28);
            ring.Ring(Vector2.zero, 1.15f, 1.5f, Bad, 28);
            ring.Line(new Vector2(-0.6f, -0.6f), new Vector2(0.6f, 0.6f), 0.22f, Bad);
            ring.Line(new Vector2(-0.6f, 0.6f), new Vector2(0.6f, -0.6f), 0.22f, Bad);
            _ringBad = MeshKit.Part(root, "LandingBad", ring.Build("LandingBad"), plain, RingOrder, out _).transform;
            _ringGood.gameObject.SetActive(false);
            _ringBad.gameObject.SetActive(false);
            if (particleMaterial != null)
                _powder = MeshKit.Burst(root, "TelemarkPowder", particleMaterial, FlagOrder + 1, new Color(1f, 1f, 1f, 0.9f), new Vector2(3f, 8f), new Vector2(0.5f, 1.3f), 0.9f, 1f);
        }

        static bool HasHill(TrackLayout layout)
        {
            foreach (var gap in layout.gaps) if (gap.skiFlying) return true;
            return false;
        }

        // A little flag on a pole, with its shadow.
        static void Flag(MeshKit kit, Vector2 at, Color colour)
        {
            kit.Disc(at + new Vector2(0.35f, -0.45f), 0.32f, Shadow, 8);
            kit.Disc(at, 0.18f, Pole, 8);
            kit.Tri(at + new Vector2(0f, 0.15f), at + new Vector2(1.1f, 0.45f), at + new Vector2(0f, 0.8f), colour);
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying || track == null || track.Layout == null || !HasHill(track.Layout)) return;
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _shake = new CameraShakeProxy();
            foreach (var car in FindObjectsByType<CarMovement>(FindObjectsSortMode.None))
            {
                var c = car;
                System.Action<float> launched = _ => OnLaunched(c);
                System.Action landed = () => OnLanded(c);
                c.Launched += launched;
                c.Landed += landed;
                _launch[c] = launched;
                _land[c] = landed;
            }
        }

        void OnDestroy()
        {
            foreach (var kv in _launch) if (kv.Key != null) kv.Key.Launched -= kv.Value;
            foreach (var kv in _land) if (kv.Key != null) kv.Key.Landed -= kv.Value;
        }

        void OnLaunched(CarMovement car)
        {
            if (car.IsCarried || car.IsRiding) return;
            var sensor = car.GetComponent<TrackSensor>();
            if (sensor == null) return;
            var gaps = track.Layout.gaps;
            for (int g = 0; g < gaps.Count; g++)
            {
                if (!gaps[g].skiFlying) continue;
                float to = track.Path.DeltaDistance(sensor.Current.Distance, gaps[g].from);
                if (to < -2f || to > 4f) continue;
                _flying[car] = g;
                var ai = car.GetComponent<SomeGame.Input.AIDriverInput>();
                if (ai != null && car.GetComponent<SomeGame.Input.PlayerCarInput>() == null)
                {
                    // A weak rival sometimes takes off crooked - and can't steer it back.
                    float roll = MeshKit.Hash(gaps[g].from, (car.name.GetHashCode() & 0xFFFF) + Mathf.Floor(Track.Clock));
                    if (roll > ai.Skill + 0.12f)
                    {
                        float turn = (roll > 0.5f ? 1f : -1f) * Random.Range(9f, 13f);
                        car.Body.rotation += turn;
                        car.transform.rotation = Quaternion.Euler(0f, 0f, car.Body.rotation);
                        car.Body.linearVelocity = Quaternion.Euler(0f, 0f, turn) * car.Body.linearVelocity;
                        car.LockFlight();
                    }
                }
                return;
            }
        }

        void OnLanded(CarMovement car)
        {
            if (!_flying.Remove(car)) return;
            bool player = _race != null && _race.Player != null && _race.Player.gameObject == car.gameObject;
            if (player) { _slowed = false; HideRing(); }
            var sensor = car.GetComponent<TrackSensor>();
            if (sensor == null || track.IsWater(car.Body.position, sensor.Current)) return;
            if (Mathf.Abs(sensor.Current.Lateral) > track.OffRoadAt(sensor.Current.Distance)) return;
            car.GiveBoost(1f);
            MeshKit.Emit(_powder, car.Body.position, 30);
            if (player)
            {
                if (_combo != null) _combo.Banner("TELEMARK!", new Color(1f, 0.85f, 0.3f));
                _shake.Shake(0.35f);
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.land, 0.9f);
            }
        }

        void Update()
        {
            if (!Application.isPlaying || _ringGood == null || _race == null || _race.Player == null) return;
            var car = _race.Player.GetComponent<CarMovement>();
            if (car == null || !_flying.ContainsKey(car) || !car.IsAirborne || car.IsCarried)
            {
                HideRing();
                return;
            }
            // Where it comes down if it holds this line: on the strip (green) or off it (red).
            Vector2 land = car.Body.position + car.Body.linearVelocity * car.AirTimeLeft;
            var at = track.Path.Project(land);
            bool good = !track.ChasmAt(land) || Mathf.Abs(at.Lateral) <= track.OffRoadAt(at.Distance) - 0.2f;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 10f);
            _ringGood.gameObject.SetActive(good);
            _ringBad.gameObject.SetActive(!good);
            var ring = good ? _ringGood : _ringBad;
            ring.position = land;
            ring.localScale = Vector3.one * pulse;
            // At the top of the flight, time slows for a moment (once per flight).
            if (!_slowed && car.AirHeight > 0.9f)
            {
                _slowed = true;
                StartCoroutine(Slow());
            }
        }

        void HideRing()
        {
            if (_ringGood != null) _ringGood.gameObject.SetActive(false);
            if (_ringBad != null) _ringBad.gameObject.SetActive(false);
        }

        System.Collections.IEnumerator Slow()
        {
            float before = Time.timeScale;
            Time.timeScale = before * 0.45f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(0.9f);
            Time.timeScale = before;
            Time.fixedDeltaTime = 0.02f;
        }

        // The camera shake, found lazily (the camera may come after the track).
        sealed class CameraShakeProxy
        {
            SomeGame.CameraRig.CameraShake _shake;
            public void Shake(float amount)
            {
                if (_shake == null) _shake = Object.FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
                if (_shake != null) _shake.Shake(amount);
            }
        }
    }
}
