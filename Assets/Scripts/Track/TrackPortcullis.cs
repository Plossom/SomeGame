using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Icicle gates (<see cref="TrackLayout.portcullis"/>): in a gateway of the citadel a curtain of giant icicles hangs over
    /// the road. In its rhythm it shivers (it drips, the icicles tremble), crashes down onto the road, stays a moment and
    /// lifts again. Up high its shadow lies off to one side and faint; as it drops the shadow sweeps in under it and the
    /// icicles turn solid. Caught under it you are smashed back out of the gate, spinning. A band of frost marks where it lands.
    /// </summary>
    public class TrackPortcullis : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;
        [SerializeField] Material particleMaterial;

        const int FrostOrder = -47, ShadowOrder = -44, TowerOrder = 12, CurtainOrder = 13;
        public const float Warn = 0.7f, Fall = 0.18f, Hold = 0.9f, Lift = 0.7f;
        const float Reach = 1.4f; // half its depth along the road

        static readonly Color Frost = MeshKit.Hex("D6F0FF", 0.55f), FrostLine = MeshKit.Hex("8CC8EA", 0.8f), Icicle = MeshKit.Hex("E6F7FF"),
            IcicleDeep = MeshKit.Hex("8ACDEE"), IcicleLine = MeshKit.Hex("2C6C93"), Stone = MeshKit.Hex("8E97A6"), StoneDark = MeshKit.Hex("5F6878"),
            StoneLight = MeshKit.Hex("BAC2CE"), Snow = MeshKit.Hex("F6FAFE"), Shade = new Color(0.03f, 0.06f, 0.15f, 0.35f);

        class Gate
        {
            public TrackLayout.Portcullis Data;
            public Transform Curtain, Shadow;
            public MeshRenderer CurtainRenderer;
            public Vector2 Centre, Along, Across;
            public float Half;
            public bool Struck;
        }

        readonly List<Gate> _gates = new();
        readonly List<GameObject> _parts = new();
        CarMovement[] _cars;
        ParticleSystem _shards, _drips;
        MaterialPropertyBlock _block;

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

        /// <summary>How far the curtain is up now (1 up .. 0 on the road), and whether it is shivering before a drop.</summary>
        public static float Height(TrackLayout.Portcullis p, float time, out bool shiver)
        {
            float open = Mathf.Max(0.5f, p.period - Warn - Fall - Hold - Lift);
            float t = Mathf.Repeat(time + p.phase * p.period, p.period);
            shiver = false;
            if (t < open) return 1f;
            t -= open;
            if (t < Warn) { shiver = true; return 1f; }
            t -= Warn;
            if (t < Fall) return 1f - t / Fall;
            t -= Fall;
            if (t < Hold) return 0f;
            t -= Hold;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Lift));
        }

        /// <summary>Seconds from now until the gate next starts shivering (0 if it is not up and open).</summary>
        static float OpenLeft(TrackLayout.Portcullis p, float time)
        {
            float open = Mathf.Max(0.5f, p.period - Warn - Fall - Hold - Lift);
            float t = Mathf.Repeat(time + p.phase * p.period, p.period);
            return t < open ? open - t : 0f;
        }

        static bool Blocking(float height) => height < 0.6f;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _gates.Clear();
            if (layout.portcullis == null || layout.portcullis.Count == 0) return;
            var root = new GameObject("IcicleGates") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var kit = new MeshKit();
            foreach (var data in layout.portcullis)
            {
                var g = new Gate { Data = data, Centre = path.PointAt(data.distance), Along = path.TangentAt(data.distance), Across = path.NormalAt(data.distance) };
                g.Half = track.OffRoadAt(data.distance) + 0.3f;
                Vector2 c = g.Centre, a = g.Along, n = g.Across;
                // the frost band where it lands
                kit.Clear();
                kit.Quad(c - a * (Reach + 0.6f) - n * g.Half, c + a * (Reach + 0.6f) - n * g.Half, c + a * (Reach + 0.6f) + n * g.Half, c - a * (Reach + 0.6f) + n * g.Half, Frost);
                foreach (float s in new[] { -1f, 1f })
                    kit.Line(c + a * s * (Reach + 0.6f) - n * g.Half, c + a * s * (Reach + 0.6f) + n * g.Half, 0.18f, FrostLine);
                MeshKit.Part(root, "GateFrost", kit.Build("GateFrost"), plain, FrostOrder, out _);
                // the gate towers either side
                kit.Clear();
                foreach (float s in new[] { -1f, 1f })
                {
                    Vector2 tc = c + n * s * (g.Half + 2.4f);
                    kit.Disc(tc + new Vector2(1.2f, -1.4f), new Vector2(3.2f, 3.2f), 0f, Shade, new Color(Shade.r, Shade.g, Shade.b, 0f), 20);
                    kit.Quad(tc - a * 2.6f - n * 2.2f, tc + a * 2.6f - n * 2.2f, tc + a * 2.6f + n * 2.2f, tc - a * 2.6f + n * 2.2f, Stone);
                    // battlements round the top
                    for (int k = 0; k < 4; k++)
                    {
                        float u = -2.6f + k * 1.6f + 0.4f;
                        foreach (float e in new[] { -2.2f, 2.2f })
                        {
                            Vector2 m = tc + a * u + n * e;
                            kit.Quad(m - a * 0.45f - n * 0.35f, m + a * 0.45f - n * 0.35f, m + a * 0.45f + n * 0.35f, m - a * 0.45f + n * 0.35f, StoneLight);
                        }
                    }
                    kit.Quad(tc - a * 1.8f - n * 1.4f, tc + a * 1.8f - n * 1.4f, tc + a * 1.8f + n * 1.4f, tc - a * 1.8f + n * 1.4f, StoneDark);
                    kit.Disc(tc + a * 0.4f + n * 0.3f, new Vector2(1.3f, 1f), 0f, Snow, Snow, 12);
                }
                // the beam over the road the curtain hangs from
                foreach (float s in new[] { -1f, 1f })
                    kit.Quad(c + a * s * 1.75f - n * (g.Half + 0.3f), c + a * s * 1.15f - n * (g.Half + 0.3f), c + a * s * 1.15f + n * (g.Half + 0.3f), c + a * s * 1.75f + n * (g.Half + 0.3f), StoneDark);
                MeshKit.Part(root, "GateTowers", kit.Build("GateTowers"), plain, TowerOrder, out _);
                // the curtain of icicles (built round the gate's centre) and its shadow
                kit.Clear();
                int count = Mathf.Max(6, Mathf.RoundToInt(g.Half * 2f / 0.75f));
                for (int k = 0; k < count; k++)
                {
                    float u = -g.Half + (k + 0.5f) * g.Half * 2f / count;
                    float len = Reach * (0.75f + 0.25f * Mathf.Sin(k * 2.3f));
                    Vector2 root0 = n * (u - 0.32f), root1 = n * (u + 0.32f), tip = n * u + a * (k % 2 == 0 ? len : -len) * 0.9f;
                    kit.Tri(root0, root1, tip, Icicle, IcicleDeep, IcicleDeep);
                    kit.Line(root0, tip, 0.06f, IcicleLine);
                }
                kit.Quad(-a * 0.35f - n * g.Half, a * 0.35f - n * g.Half, a * 0.35f + n * g.Half, -a * 0.35f + n * g.Half, IcicleDeep);
                var curtain = MeshKit.Part(root, "Curtain", kit.Build("Curtain"), plain, CurtainOrder, out _);
                curtain.transform.position = c;
                g.Curtain = curtain.transform;
                g.CurtainRenderer = curtain;
                kit.Clear();
                kit.Quad(-a * Reach - n * g.Half, a * Reach - n * g.Half, a * Reach + n * g.Half, -a * Reach + n * g.Half, Shade);
                var shadow = MeshKit.Part(root, "CurtainShadow", kit.Build("CurtainShadow"), plain, ShadowOrder, out _);
                g.Shadow = shadow.transform;
                _gates.Add(g);
            }
            if (Application.isPlaying && particleMaterial != null)
            {
                _shards = MeshKit.Burst(root, "IcicleShards", particleMaterial, CurtainOrder + 1, new Color(0.9f, 0.97f, 1f, 1f), new Vector2(3f, 8f), new Vector2(0.2f, 0.55f), 0.8f, 2f);
                _drips = MeshKit.Burst(root, "IcicleDrips", particleMaterial, CurtainOrder + 1, new Color(0.8f, 0.92f, 1f, 0.8f), new Vector2(0.2f, 0.8f), new Vector2(0.1f, 0.2f), 0.5f, 2f);
            }
            Place(Application.isPlaying ? Track.Clock : 0f);
        }

        void Place(float time)
        {
            _block ??= new MaterialPropertyBlock();
            foreach (var g in _gates)
            {
                if (g.Curtain == null) continue;
                float h = Height(g.Data, time, out bool shiver);
                // up high: it looks smaller and fainter, its shadow lies off to the side; down: solid, on its shadow
                Vector2 jitter = shiver ? Random.insideUnitCircle * 0.08f : Vector2.zero;
                g.Curtain.position = g.Centre + g.Along * (h * 0.6f) + jitter;
                g.Curtain.localScale = Vector3.one * Mathf.Lerp(1f, 0.82f, h);
                _block.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.45f, h)));
                g.CurtainRenderer.SetPropertyBlock(_block);
                g.Shadow.position = g.Centre + new Vector2(2.4f, -3.2f) * h;
                g.Shadow.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, h);
                if (Application.isPlaying && shiver && Random.value < Time.deltaTime * 20f)
                    MeshKit.Emit(_drips, g.Centre + g.Across * Random.Range(-g.Half, g.Half), 1);
            }
        }

        void Update()
        {
            if (_gates.Count == 0) return;
            Place(Application.isPlaying ? Track.Clock : 0f);
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || _gates.Count == 0) return;
            if (_cars == null) _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            float now = Track.Clock;
            foreach (var g in _gates)
            {
                float h = Height(g.Data, now, out _);
                bool down = Blocking(h);
                if (down && !g.Struck)
                {
                    // the moment it lands: a crash of ice
                    g.Struck = true;
                    MeshKit.Emit(_shards, g.Centre, 30);
                    if (MeshKit.Heard(g.Centre)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 0.8f);
                }
                if (!down) { g.Struck = false; continue; }
                foreach (var car in _cars)
                {
                    if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                    Vector2 rel = car.Body.position - g.Centre;
                    float along = Vector2.Dot(rel, g.Along), across = Vector2.Dot(rel, g.Across);
                    if (Mathf.Abs(along) > Reach + 0.4f || Mathf.Abs(across) > g.Half) continue;
                    Smash(car, g, along);
                }
            }
        }

        // Caught under the icicles: thrown back out of the gate (the way it came), spinning, slowed right down.
        void Smash(CarMovement car, Gate g, float along)
        {
            float back = along >= 0f && Vector2.Dot(car.Body.linearVelocity, g.Along) < 0f ? 1f : -1f;
            car.Body.position = g.Centre + g.Along * back * (Reach + 1.6f) + g.Across * Vector2.Dot(car.Body.position - g.Centre, g.Across);
            car.Body.linearVelocity = g.Along * back * 4f;
            car.Slip(Random.value < 0.5f ? -1 : 1);
            MeshKit.Emit(_shards, car.Body.position, 25);
            var race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            if (race != null && race.Player != null && race.Player.gameObject == car.gameObject)
            {
                var combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
                if (combo != null) combo.Banner("SMASHED!", new Color(0.7f, 0.88f, 1f));
                var shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
                if (shake != null) shake.Shake(0.5f);
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.crash, 0.7f);
            }
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near) => false; // it smashes you back, it does not open a hole

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            float v = Mathf.Max(4f, car.ForwardSpeed);
            float now = Track.Clock;
            foreach (var g in _gates)
            {
                float to = path.DeltaDistance(distance, g.Data.distance) - Reach - 1.5f;
                if (to < 0f || to > 45f) continue;
                // Will it be open when this rival gets there (and still open as it passes under)? A weak rival misjudges it.
                float misjudge = (1f - skill) * (MeshKit.Hash(g.Data.distance, (car.name.GetHashCode() & 0xFFFF) + Mathf.Floor(now / g.Data.period)) - 0.3f) * 1.2f;
                float arrive = to / v + misjudge, pass = arrive + (Reach * 2f + 4f) / v;
                bool openThen = Height(g.Data, now + arrive, out bool s0) >= 0.99f && !s0 && Height(g.Data, now + pass, out bool s1) >= 0.99f && !s1;
                if (openThen) continue;
                // Not open in time: hang back so it arrives as the gate next opens.
                float wait = 0f;
                for (float dt = 0.1f; dt < g.Data.period * 1.5f; dt += 0.1f)
                    if (Height(g.Data, now + dt, out bool sh) >= 0.99f && !sh && OpenLeft(g.Data, now + dt) > (Reach * 2f + 4f) / Mathf.Max(6f, v)) { wait = dt; break; }
                speed = Mathf.Min(speed, Mathf.Max(0f, to / Mathf.Max(0.3f, wait)));
            }
        }
    }
}
