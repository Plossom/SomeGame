using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Shuttle floes (<see cref="TrackLayout.shuttles"/>): the road ends at a stone pillar over the abyss and carries on at
    /// the next. Between them slabs of ice glide to and fro along frozen chains, waiting a moment at each end. Drive onto a
    /// slab while it is docked, ride it across (it carries you), and drive off when it docks at the far side - or, with two
    /// slabs, hop from one to the other where they meet. Anywhere else in the gap you fall. While a slab glides, a bar at its
    /// front (red) holds the cars on it; docked where you can drive on, the bar drops (green). The pillar's edge is striped
    /// red and white, green and white while a slab is docked there; the chains show where the slabs run.
    /// </summary>
    public class TrackShuttles : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;

        const int ChainOrder = -52, StripeOrder = -44, SlabOrder = -45;
        static readonly Color SlabIce = MeshKit.Hex("CDEEFB"), SlabLight = MeshKit.Hex("F2FBFF"), SlabDark = MeshKit.Hex("6FB0D6"),
            SlabEdge = MeshKit.Hex("2C6C93"), Chain = MeshKit.Hex("5E6878", 0.9f), ChainLight = MeshKit.Hex("A9B3C2", 0.9f),
            Red = MeshKit.Hex("E0342B"), Green = MeshKit.Hex("2EBD5A"), White = MeshKit.Hex("FFFFFF"), Shadow = new Color(0.02f, 0.04f, 0.12f, 0.3f);

        class Ferry
        {
            public TrackLayout.Shuttle Data;
            public float From, To; // the stretch this slab runs on (lap distances)
            public float Phase;
            public Transform Slab, Shadow;
            public GameObject LipUp, LipDown;
            public Ferry Next; // the slab it hands over to (two meeting in the middle), or null
            public Vector2 Centre0; // its centre when at the near end
            public float Last;      // where it was last step (lap distance of its back edge)
        }

        readonly List<Ferry> _ferries = new();
        readonly List<GameObject> _parts = new();
        readonly List<(MeshRenderer red, MeshRenderer green, TrackLayout.Shuttle shuttle, bool near)> _stripes = new();
        CarMovement[] _cars;
        readonly Dictionary<CarMovement, TrackSensor> _sensors = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            if (track != null && !track.Hazards.Contains(this)) track.Hazards.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (track != null) track.Hazards.Remove(this);
        }

        /// <summary>Where a slab's back edge is now (lap distance) and whether it waits at the near (-1) or far (+1) end.</summary>
        static float BackEdge(TrackLayout.Shuttle s, float from, float to, float phase, float time, out int docked)
        {
            float run = Mathf.Max(0f, to - from - s.slabLength);
            float period = 2f * (s.dwell + s.travel);
            float t = Mathf.Repeat(time + phase * period, period);
            docked = 0;
            float u;
            if (t < s.dwell) { u = 0f; docked = -1; }
            else if (t < s.dwell + s.travel) u = Mathf.SmoothStep(0f, 1f, (t - s.dwell) / s.travel);
            else if (t < 2f * s.dwell + s.travel) { u = 1f; docked = 1; }
            else u = 1f - Mathf.SmoothStep(0f, 1f, (t - 2f * s.dwell - s.travel) / s.travel);
            return from + run * u;
        }

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _ferries.Clear();
            _stripes.Clear();
            if (layout.shuttles == null || layout.shuttles.Count == 0) return;
            var root = new GameObject("ShuttleFloes") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var kit = new MeshKit();
            foreach (var s in layout.shuttles)
            {
                float half = track.HalfWidthAt((s.from + s.to) * 0.5f);
                // the slabs' stretches: one across the gap, or two meeting in the middle (the second half a cycle on)
                float mid = (s.from + s.to) * 0.5f;
                var runs = s.slabs >= 2 ? new[] { (s.from, mid, s.phase), (mid, s.to, s.phase + 0.5f) } : new[] { (s.from, s.to, s.phase) };
                // the frozen chains along both sides of the gap
                kit.Clear();
                foreach (float side in new[] { -1f, 1f })
                    for (float d = s.from; d < s.to - 0.01f; d += 0.9f)
                    {
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (half + 0.35f), q = path.PointAt(Mathf.Min(s.to, d + 0.7f)) + path.NormalAt(d) * side * (half + 0.35f);
                        kit.Line(p, q, 0.28f, ((int)(d / 0.9f)) % 2 == 0 ? Chain : ChainLight);
                    }
                MeshKit.Part(root, "ShuttleChains", kit.Build("ShuttleChains"), plain, ChainOrder, out _);
                // the pillar edges: red/white stripes (and green/white, shown while a slab is docked there)
                foreach (var (d, near) in new[] { (s.from, true), (s.to, false) })
                    for (int c = 0; c < 2; c++)
                    {
                        kit.Clear();
                        Vector2 p = path.PointAt(d), n = path.NormalAt(d), a = path.TangentAt(d) * (near ? -1f : 1f);
                        int bands = 8;
                        for (int k = 0; k < bands; k++)
                        {
                            float u0 = -half + k * half * 2f / bands, u1 = u0 + half * 2f / bands;
                            kit.Quad(p + n * u0, p + n * u1, p + n * u1 + a * 0.9f, p + n * u0 + a * 0.9f, k % 2 == 0 ? (c == 0 ? Red : Green) : White);
                        }
                        var r = MeshKit.Part(root, c == 0 ? "PillarStop" : "PillarGo", kit.Build("PillarEdge"), plain, StripeOrder, out _);
                        if (c == 0) _stripes.Add((r, null, s, near));
                        else _stripes[_stripes.Count - 1] = (_stripes[_stripes.Count - 1].red, r, s, near);
                    }
                foreach (var (from, to, phase) in runs)
                {
                    var f = new Ferry { Data = s, From = from, To = to, Phase = phase };
                    // the slab, built round its own centre at the near end of its run
                    float c0 = from + s.slabLength * 0.5f;
                    f.Centre0 = path.PointAt(c0);
                    kit.Clear();
                    for (float x = -s.slabLength * 0.5f; x < s.slabLength * 0.5f - 0.01f; x += 1f)
                    {
                        float d0 = c0 + x, d1 = c0 + Mathf.Min(s.slabLength * 0.5f, x + 1f);
                        Vector2 a = path.PointAt(d0) - f.Centre0, b = path.PointAt(d1) - f.Centre0, na = path.NormalAt(d0), nb = path.NormalAt(d1);
                        kit.Quad(a - na * half, b - nb * half, b + nb * half, a + na * half, SlabIce, SlabIce, SlabLight, SlabLight);
                        kit.Quad(a - na * half, b - nb * half, b - nb * (half - 0.5f), a - na * (half - 0.5f), SlabDark);
                    }
                    Vector2 e0 = path.PointAt(c0 - s.slabLength * 0.5f) - f.Centre0, e1 = path.PointAt(c0 + s.slabLength * 0.5f) - f.Centre0, n0 = path.NormalAt(c0);
                    kit.Line(e0 - n0 * half, e1 - n0 * half, 0.22f, SlabEdge);
                    kit.Line(e0 + n0 * half, e1 + n0 * half, 0.22f, SlabEdge);
                    kit.Line(e0 - n0 * half, e0 + n0 * half, 0.22f, SlabEdge);
                    kit.Line(e1 - n0 * half, e1 + n0 * half, 0.22f, SlabEdge);
                    var slab = MeshKit.Part(root, "ShuttleSlab", kit.Build("ShuttleSlab"), plain, SlabOrder, out _);
                    f.Slab = slab.transform;
                    // the bar across its front: red while it holds the cars, green when it lets them off
                    for (int c = 0; c < 2; c++)
                    {
                        kit.Clear();
                        Vector2 ta = path.TangentAt(c0);
                        for (int k = 0; k < 6; k++)
                        {
                            float u0 = -half + k * half / 3f, u1 = u0 + half / 3f;
                            kit.Quad(e1 + n0 * u0 - ta * 0.7f, e1 + n0 * u1 - ta * 0.7f, e1 + n0 * u1, e1 + n0 * u0, k % 2 == 0 ? (c == 0 ? Red : Green) : White);
                        }
                        var lip = MeshKit.Part(slab.transform, c == 0 ? "LipUp" : "LipDown", kit.Build("ShuttleLip"), plain, StripeOrder, out _);
                        if (c == 0) f.LipUp = lip.gameObject; else f.LipDown = lip.gameObject;
                    }
                    kit.Clear();
                    kit.Quad(e0 - n0 * half, e1 - n0 * half, e1 + n0 * half, e0 + n0 * half, Shadow);
                    f.Shadow = MeshKit.Part(root, "ShuttleShadow", kit.Build("ShuttleShadow"), plain, ChainOrder - 1, out _).transform;
                    f.Last = BackEdge(s, from, to, phase, Application.isPlaying ? Track.Clock : 0f, out _);
                    _ferries.Add(f);
                }
                if (s.slabs >= 2) _ferries[_ferries.Count - 2].Next = _ferries[_ferries.Count - 1];
            }
            Place(Application.isPlaying ? Track.Clock : 0f);
        }

        /// <summary>True while a slab's front bar is down: docked at its far end, where there is road (or the next slab) to drive on to.</summary>
        static bool LipDown(Ferry f, float time)
        {
            BackEdge(f.Data, f.From, f.To, f.Phase, time, out int docked);
            if (docked <= 0) return false;
            if (f.Next == null) return true;
            BackEdge(f.Next.Data, f.Next.From, f.Next.To, f.Next.Phase, time, out int next);
            return next < 0;
        }

        void Place(float time)
        {
            var path = track.Path;
            foreach (var f in _ferries)
            {
                if (f.Slab == null) continue;
                float back = BackEdge(f.Data, f.From, f.To, f.Phase, time, out _);
                Vector2 c = path.PointAt(back + f.Data.slabLength * 0.5f);
                f.Slab.position = c;
                f.Shadow.position = c + new Vector2(2.2f, -3f);
                bool down = LipDown(f, time);
                if (f.LipUp != null) f.LipUp.SetActive(!down);
                if (f.LipDown != null) f.LipDown.SetActive(down);
            }
            foreach (var (red, green, s, near) in _stripes)
            {
                if (red == null || green == null) continue;
                bool docked = DockedAt(s, near ? s.from : s.to, time);
                red.enabled = !docked;
                green.enabled = docked;
            }
        }

        // A slab waiting against this edge of a pillar now.
        bool DockedAt(TrackLayout.Shuttle s, float edge, float time)
        {
            foreach (var f in _ferries)
            {
                if (f.Data != s) continue;
                BackEdge(s, f.From, f.To, f.Phase, time, out int docked);
                if (docked < 0 && Mathf.Abs(f.From - edge) < 0.5f) return true;
                if (docked > 0 && Mathf.Abs(f.To - edge) < 0.5f) return true;
            }
            return false;
        }

        void Update()
        {
            if (_ferries.Count == 0) return;
            Place(Application.isPlaying ? Track.Clock : 0f);
        }

        // Cars on a slab ride along with it.
        void FixedUpdate()
        {
            if (!Application.isPlaying || _ferries.Count == 0) return;
            if (_cars == null) _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            float now = Track.Clock;
            var path = track.Path;
            foreach (var f in _ferries)
            {
                float back = BackEdge(f.Data, f.From, f.To, f.Phase, now, out _), moved = back - f.Last;
                float before = f.Last;
                f.Last = back;
                if (Mathf.Abs(moved) < 1e-5f) continue;
                foreach (var car in _cars)
                {
                    if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                    if (!_sensors.TryGetValue(car, out var sensor)) _sensors[car] = sensor = car.GetComponent<TrackSensor>();
                    if (sensor == null) continue;
                    float d = sensor.Current.Distance;
                    if (path.DeltaDistance(before, d) < 0f || path.DeltaDistance(d, before + f.Data.slabLength) < 0f) continue;
                    if (Mathf.Abs(sensor.Current.Lateral) > track.HalfWidthAt(d) + 0.3f) continue;
                    car.Body.position += path.TangentAt(d) * moved;
                }
            }
            // The front bar holds every car on a gliding slab (they bump against it).
            foreach (var f in _ferries)
            {
                if (LipDown(f, now)) continue;
                float back = BackEdge(f.Data, f.From, f.To, f.Phase, now, out _);
                foreach (var car in _cars)
                {
                    if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                    if (!_sensors.TryGetValue(car, out var sensor) || sensor == null) continue;
                    float rel = path.DeltaDistance(back, sensor.Current.Distance), limit = f.Data.slabLength - 1.9f;
                    if (rel < 0f || rel > f.Data.slabLength + 1f || rel <= limit) continue;
                    Vector2 t = path.TangentAt(sensor.Current.Distance);
                    car.Body.position -= t * (rel - limit);
                    float along = Vector2.Dot(car.Body.linearVelocity, t);
                    if (along > 0f) car.Body.linearVelocity -= t * (along * 1.3f);
                }
            }
        }

        /// <summary>True if there is something to stand on at this lap distance now (outside the gaps, or on a slab).</summary>
        bool Solid(float d, float time)
        {
            var path = track.Path;
            bool inGap = false;
            foreach (var s in track.Layout.shuttles)
                if (path.DeltaDistance(s.from, d) >= 0f && path.DeltaDistance(d, s.to) >= 0f) { inGap = true; break; }
            if (!inGap) return true;
            foreach (var f in _ferries)
            {
                float back = BackEdge(f.Data, f.From, f.To, f.Phase, time, out _);
                if (path.DeltaDistance(back - 0.2f, d) >= 0f && path.DeltaDistance(d, back + f.Data.slabLength + 0.2f) >= 0f) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near)
        {
            if (_ferries.Count == 0) return false;
            if (Mathf.Abs(near.Lateral) > track.HalfWidthAt(near.Distance) + 0.3f)
            {
                // beside the slabs: the abyss
                foreach (var s in track.Layout.shuttles)
                    if (track.Path.DeltaDistance(s.from, near.Distance) >= 0f && track.Path.DeltaDistance(near.Distance, s.to) >= 0f) return true;
                return false;
            }
            return !Solid(near.Distance, Track.Clock);
        }

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            float now = Track.Clock;
            foreach (var s in track.Layout.shuttles)
            {
                float to = path.DeltaDistance(distance, s.from), past = path.DeltaDistance(s.to, distance);
                if (to > 30f || past > 1f) continue;
                lateral = Mathf.Lerp(lateral, 0f, 0.8f);
                // On a gliding slab: roll up to the bar and wait for it.
                bool riding = false;
                foreach (var f in _ferries)
                {
                    if (f.Data != s) continue;
                    float back = BackEdge(f.Data, f.From, f.To, f.Phase, now, out _), rel = path.DeltaDistance(back, distance);
                    if (rel < 0f || rel > f.Data.slabLength) continue;
                    riding = true;
                    if (!LipDown(f, now)) speed = Mathf.Min(speed, 3f);
                }
                if (riding) continue;
                // Look along the next metres: wherever there is nothing to stand on (and won't be by then), stop short of it.
                // A weak rival sometimes sets off a moment too soon.
                float v = Mathf.Max(3f, car.ForwardSpeed);
                float early = (1f - skill) * 0.6f * MeshKit.Hash(s.from, (car.name.GetHashCode() & 0xFFFF) + Mathf.Floor(now / 4f));
                for (float x = 1.5f; x <= 16f; x += 1f)
                {
                    float when = Mathf.Max(0f, x / v - early);
                    if (Solid(distance + x, now + when)) continue; // something to stand on there by the time it gets there
                    speed = Mathf.Min(speed, Mathf.Max(0f, (x - 2.5f) * 1.4f));
                    break;
                }
            }
        }
    }
}
