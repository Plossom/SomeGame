using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Tilting ice slabs (<see cref="TrackLayout.tiltSlabs"/>): seesaws of ice over the moat, each on a pivot along the road.
    /// A car off the middle tips its slab towards its side; tipped, the slab lets the cars on it slide down towards its low
    /// edge - and over it into the moat. Keep to the bright stripe down the middle and it stays level. Rivals tip it too:
    /// the slab you arrive on may already lean. The low side darkens, the high side catches the light, the shadow shifts.
    /// </summary>
    public class TrackTiltSlabs : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;

        const int ShadowOrder = -52, SlabOrder = -45, PinOrder = -44;
        const float Push = 9f, Load = 1.6f, Spring = 2.2f, Damp = 1.8f, MaxTilt = 1f;

        static readonly Color Ice = MeshKit.Hex("BFE6F7"), IceLight = MeshKit.Hex("F2FBFF"), IceDark = MeshKit.Hex("5E9FC6"),
            Stripe = MeshKit.Hex("FFFFFF"), StripeEdge = MeshKit.Hex("3D7DD8"), Edge = MeshKit.Hex("2C6C93"), Pin = MeshKit.Hex("3A3F48"),
            PinLight = MeshKit.Hex("9AA4B2"), Shadow = new Color(0.02f, 0.04f, 0.12f, 0.3f);

        class Slab
        {
            public float From, To, Half;
            public float Tilt, Spin; // -1..1: + tips down to the left
            public MeshFilter Mesh;
            public Color[] Base;
            public float[] Side; // per vertex: -1 right edge .. +1 left edge
            public Transform Shadow;
        }

        readonly List<Slab> _slabs = new();
        readonly List<GameObject> _parts = new();
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

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _slabs.Clear();
            if (layout.tiltSlabs == null || layout.tiltSlabs.Count == 0) return;
            var root = new GameObject("TiltSlabs") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var kit = new MeshKit();
            foreach (var span in layout.tiltSlabs)
            {
                var slab = new Slab { From = span.from, To = span.to, Half = track.HalfWidthAt((span.from + span.to) * 0.5f) };
                float length = path.DeltaDistance(span.from, span.to), h = slab.Half;
                // the slab: ice, lighter towards the edges' rims, a bright stripe with blue borders down the middle
                kit.Clear();
                var side = new List<float>();
                for (float s = 0f; s < length - 0.01f; s += 1f)
                {
                    float d0 = span.from + s, d1 = span.from + Mathf.Min(length, s + 1f);
                    Vector2 a = path.PointAt(d0), b = path.PointAt(d1), na = path.NormalAt(d0), nb = path.NormalAt(d1);
                    float[] lat = { -h, -0.6f, -0.25f, 0.25f, 0.6f, h };
                    for (int k = 0; k < lat.Length - 1; k++)
                    {
                        Color col = k == 2 ? Stripe : k == 1 || k == 3 ? StripeEdge : Ice;
                        if (k == 1 || k == 3) { kit.Quad(a + na * lat[k], b + nb * lat[k], b + nb * (lat[k] + 0.12f), a + na * (lat[k] + 0.12f), col); side.AddRange(new[] { lat[k] / h, lat[k] / h, (lat[k] + 0.12f) / h, (lat[k] + 0.12f) / h }); col = Ice; }
                        kit.Quad(a + na * lat[k], b + nb * lat[k], b + nb * lat[k + 1], a + na * lat[k + 1], col);
                        side.AddRange(new[] { lat[k] / h, lat[k] / h, lat[k + 1] / h, lat[k + 1] / h });
                    }
                }
                // a dark rim all round
                foreach (float s in new[] { -1f, 1f })
                    for (float x = 0f; x < length - 0.01f; x += 1f)
                    {
                        float d0 = span.from + x, d1 = span.from + Mathf.Min(length, x + 1f);
                        Vector2 a = path.PointAt(d0) + path.NormalAt(d0) * s * h, b = path.PointAt(d1) + path.NormalAt(d1) * s * h;
                        kit.Quad(a, b, b + path.NormalAt(d1) * s * 0.18f, a + path.NormalAt(d0) * s * 0.18f, Edge);
                        side.AddRange(new[] { s, s, s, s });
                    }
                MeshKit.Part(root, "TiltSlab", kit.Build("TiltSlab"), plain, SlabOrder, out slab.Mesh);
                Assign(slab.Mesh, slab.Mesh.sharedMesh);
                slab.Base = kit.C.ToArray();
                slab.Side = side.ToArray();
                // the pivots at both ends
                kit.Clear();
                foreach (float end in new[] { span.from, span.to })
                {
                    Vector2 p = path.PointAt(end);
                    kit.Disc(p, 0.75f, Pin, 14);
                    kit.Disc(p + new Vector2(-0.15f, 0.15f), 0.35f, PinLight, 10);
                }
                MeshKit.Part(root, "TiltPivots", kit.Build("TiltPivots"), plain, PinOrder, out _);
                // its shadow on the clouds below
                kit.Clear();
                Vector2 m = path.PointAt(span.from + length * 0.5f);
                for (float s = 0f; s < length - 0.01f; s += 2f)
                {
                    float d0 = span.from + s, d1 = span.from + Mathf.Min(length, s + 2f);
                    kit.Quad(path.PointAt(d0) - path.NormalAt(d0) * h - m, path.PointAt(d1) - path.NormalAt(d1) * h - m, path.PointAt(d1) + path.NormalAt(d1) * h - m, path.PointAt(d0) + path.NormalAt(d0) * h - m, Shadow);
                }
                var sh = MeshKit.Part(root, "TiltShadow", kit.Build("TiltShadow"), plain, ShadowOrder, out _);
                sh.transform.position = m + new Vector2(2.2f, -3f);
                slab.Shadow = sh.transform;
                _slabs.Add(slab);
            }
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || _slabs.Count == 0) return;
            if (_cars == null) _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            float dt = Time.fixedDeltaTime;
            var path = track.Path;
            foreach (var slab in _slabs)
            {
                float torque = 0f;
                foreach (var car in _cars)
                {
                    if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                    if (!_sensors.TryGetValue(car, out var sensor)) _sensors[car] = sensor = car.GetComponent<TrackSensor>();
                    if (sensor == null) continue;
                    var at = sensor.Current;
                    if (path.DeltaDistance(slab.From, at.Distance) < 0f || path.DeltaDistance(at.Distance, slab.To) < 0f) continue;
                    // its weight tips the slab; the tilt slides it down towards the low edge
                    torque += Mathf.Clamp(at.Lateral / slab.Half, -1f, 1f);
                    car.Body.linearVelocity += path.NormalAt(at.Distance) * (slab.Tilt * Push * dt);
                }
                slab.Spin += (torque * Load - slab.Tilt * Spring - slab.Spin * Damp) * dt;
                slab.Tilt = Mathf.Clamp(slab.Tilt + slab.Spin * dt, -MaxTilt, MaxTilt);
                if (Mathf.Abs(slab.Tilt) >= MaxTilt) slab.Spin = 0f;
            }
        }

        void Update()
        {
            foreach (var slab in _slabs)
            {
                if (slab.Mesh == null) continue;
                // the low side darkens, the high side catches the light
                var colours = new Color[slab.Base.Length];
                for (int i = 0; i < colours.Length; i++)
                {
                    float k = slab.Side[i] * slab.Tilt; // > 0: this edge is going down
                    colours[i] = k > 0f ? Color.Lerp(slab.Base[i], IceDark, k * 0.7f) : Color.Lerp(slab.Base[i], IceLight, -k * 0.6f);
                }
                slab.Mesh.sharedMesh.SetColors(colours);
                if (slab.Shadow != null)
                {
                    var path = track.Path;
                    float mid = (slab.From + slab.To) * 0.5f;
                    slab.Shadow.position = path.PointAt(mid) + new Vector2(2.2f, -3f) + path.NormalAt(mid) * (slab.Tilt * 1.2f);
                }
            }
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near)
        {
            var path = track.Path;
            foreach (var slab in _slabs)
            {
                if (path.DeltaDistance(slab.From, near.Distance) < 0f || path.DeltaDistance(near.Distance, slab.To) < 0f) continue;
                return Mathf.Abs(near.Lateral) > slab.Half + 0.2f;
            }
            return false;
        }

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            float top = car.Stats != null ? car.Stats.topSpeed : 18f;
            foreach (var slab in _slabs)
            {
                float to = path.DeltaDistance(ahead, slab.From), past = path.DeltaDistance(slab.To, ahead);
                if (to > 12f || past > 2f) continue;
                // Down the middle (a weak rival wanders off it, and leans against a tilt too late).
                float wander = (1f - skill) * 2.2f * Mathf.Sin(Time.time * 1.3f + (car.name.GetHashCode() & 0xFF));
                lateral = Mathf.Lerp(lateral, wander - slab.Tilt * 1.2f * skill, Mathf.InverseLerp(12f, 0f, to));
                speed = Mathf.Min(speed, top * Mathf.Lerp(0.7f, 0.9f, skill));
            }
        }
    }
}
