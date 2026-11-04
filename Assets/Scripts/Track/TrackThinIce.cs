using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Thin ice (<see cref="TrackLayout.thinIce"/>): glassy patches on the road with the dark water showing through. They
    /// carry a car that keeps its speed and drives straight; a slow car, or one sliding sideways (drifting), loads the ice
    /// - cracks spread out from under it, the ice groans and flashes white - and at the limit it breaks: open water, the
    /// car sinks and is carried back. A few seconds later it freezes over again. Holes a rival broke stay open for a
    /// moment: steer round them.
    /// </summary>
    public class TrackThinIce : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;
        [SerializeField] Material particleMaterial;

        const int IceOrder = -46, CrackOrder = -45;
        /// <summary>Below this share of its top speed a car loads the ice.</summary>
        const float SafeShare = 0.62f, LoadRate = 3.6f, SlideLoad = 1.5f, Recover = 0.22f;
        const float Broken = 4.5f, Refreeze = 1.2f;

        static readonly Color Glass = MeshKit.Hex("2F6E8C"), GlassLight = MeshKit.Hex("7FC4DE"), Rim = MeshKit.Hex("EAF7FF"),
            Frost = MeshKit.Hex("CFEAF7"), Crack = MeshKit.Hex("FFFFFF"), Water = MeshKit.Hex("0B2B3D"), WaterLight = MeshKit.Hex("1E5872"),
            Shard = MeshKit.Hex("DDF3FF");

        class Patch
        {
            public TrackLayout.IcePatch Data;
            public float Load, BrokeAt = -1000f, Distance, Lateral, HalfAlong, HalfAcross;
            public bool Loaded; // a car on it this step
            public MeshFilter Ice, Cracks;
            public Color[] IceColours, CrackColours;
            public float[] CrackReach; // how far out each crack vertex is (0 centre .. 1 rim): cracks grow outwards with the load
            public float Groaned;
        }

        readonly List<Patch> _patches = new();
        readonly List<GameObject> _parts = new();
        CarMovement[] _cars;
        ParticleSystem _shards, _spray;

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
            _patches.Clear();
            if (layout.thinIce == null || layout.thinIce.Count == 0) return;
            var root = new GameObject("ThinIce") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            foreach (var data in layout.thinIce)
            {
                var at = path.Project(data.position);
                var p = new Patch { Data = data, Distance = at.Distance, Lateral = at.Lateral, HalfAlong = data.size.x * 0.5f, HalfAcross = data.size.y * 0.5f };
                BuildPatch(root, p);
                _patches.Add(p);
            }
            if (Application.isPlaying && particleMaterial != null)
            {
                _shards = MeshKit.Burst(root, "IceShards", particleMaterial, CrackOrder + 30, Shard, new Vector2(2f, 6f), new Vector2(0.25f, 0.6f), 0.9f, 1.2f);
                _spray = MeshKit.Burst(root, "IceSpray", particleMaterial, CrackOrder + 31, new Color(0.9f, 0.97f, 1f, 0.8f), new Vector2(0.5f, 2.5f), new Vector2(0.15f, 0.35f), 0.5f, 0.6f);
            }
        }

        // The glass (dark water under clear ice, light streaks, a frosted rim) and its web of cracks (hidden until loaded).
        void BuildPatch(Transform root, Patch p)
        {
            var d = p.Data;
            float a = d.angle * Mathf.Deg2Rad;
            Vector2 ax = new(Mathf.Cos(a), Mathf.Sin(a)), ay = new(-ax.y, ax.x);
            Vector2 Local(float u, float v) => d.position + ax * u + ay * v;
            float seed = d.position.x * 0.13f + d.position.y * 0.07f;
            var kit = new MeshKit();
            const int n = 28;
            // ragged oval outline
            var edge = new Vector2[n + 1];
            for (int k = 0; k <= n; k++)
            {
                float u = k * Mathf.PI * 2f / n;
                float rag = 1f + 0.05f * Mathf.Sin(u * 5f + seed) + 0.03f * Mathf.Sin(u * 11f + seed * 3f);
                edge[k] = Local(Mathf.Cos(u) * p.HalfAlong * rag, Mathf.Sin(u) * p.HalfAcross * rag);
            }
            for (int k = 0; k < n; k++)
            {
                // the frosted rim, then clear glass over dark water (lighter towards the middle)
                Vector2 e0 = edge[k], e1 = edge[k + 1];
                Vector2 i0 = Vector2.Lerp(d.position, e0, 0.86f), i1 = Vector2.Lerp(d.position, e1, 0.86f);
                kit.Quad(i0, e0, e1, i1, Frost, Rim, Rim, Frost);
                kit.Tri(d.position, i0, i1, GlassLight, Glass, Glass);
            }
            // streaks of reflected sky across the glass
            for (int s = 0; s < 4; s++)
            {
                float v0 = (-0.55f + s * 0.33f + 0.08f * Mathf.Sin(seed + s)) * p.HalfAcross;
                float u0 = -p.HalfAlong * 0.55f, u1 = p.HalfAlong * (0.25f + 0.2f * Mathf.Sin(seed * 2f + s));
                Color streak = new Color(GlassLight.r, GlassLight.g, GlassLight.b, 0.55f), clear = new Color(GlassLight.r, GlassLight.g, GlassLight.b, 0f);
                kit.Quad(Local(u0, v0), Local(u1, v0 + 0.6f), Local(u1, v0 + 0.95f), Local(u0, v0 + 0.3f), clear, streak, streak, clear);
            }
            var iceRenderer = MeshKit.Part(root, "Glass", kit.Build("ThinIce"), plain, IceOrder, out p.Ice);
            p.IceColours = kit.C.ToArray();
            _ = iceRenderer;

            // Cracks: branching lines from near the middle out to the rim.
            kit.Clear();
            var reach = new List<float>();
            var rng = new System.Random(Mathf.RoundToInt(seed * 1000f));
            float R() => (float)rng.NextDouble();
            for (int c = 0; c < 9; c++)
            {
                float dir = c * Mathf.PI * 2f / 9f + (R() - 0.5f) * 0.5f;
                Vector2 from = new(R() * 0.15f - 0.075f, R() * 0.15f - 0.075f);
                float len = 0.75f + R() * 0.3f;
                int steps = 5;
                Vector2 prev = from;
                for (int s = 1; s <= steps; s++)
                {
                    float r = len * s / steps;
                    dir += (R() - 0.5f) * 0.5f;
                    Vector2 next = new(Mathf.Cos(dir) * r, Mathf.Sin(dir) * r);
                    Vector2 pa = Local(prev.x * p.HalfAlong, prev.y * p.HalfAcross), pb = Local(next.x * p.HalfAlong, next.y * p.HalfAcross);
                    kit.Line(pa, pb, 0.11f * (1.2f - r * 0.6f), Crack);
                    for (int k = 0; k < 12; k++) reach.Add(r);
                    // a little side branch
                    if (s == 3 && R() < 0.7f)
                    {
                        float bd = dir + (R() < 0.5f ? 0.9f : -0.9f);
                        Vector2 tip = next + new Vector2(Mathf.Cos(bd), Mathf.Sin(bd)) * 0.25f;
                        kit.Line(pb, Local(tip.x * p.HalfAlong, tip.y * p.HalfAcross), 0.07f, Crack);
                        for (int k = 0; k < 12; k++) reach.Add(r + 0.2f);
                    }
                    prev = next;
                }
            }
            MeshKit.Part(root, "Cracks", kit.Build("ThinIceCracks"), plain, CrackOrder, out p.Cracks);
            p.CrackColours = kit.C.ToArray();
            p.CrackReach = reach.ToArray();
        }

        static bool Contains(Patch p, Vector2 pos, float shrink = 1f)
        {
            var d = p.Data;
            Vector2 rel = pos - d.position;
            float a = d.angle * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            float u = (rel.x * c + rel.y * s) / (p.HalfAlong * shrink), v = (-rel.x * s + rel.y * c) / (p.HalfAcross * shrink);
            return u * u + v * v <= 1f;
        }

        // 0 whole, 1 broken (open water), 2 freezing over again
        int State(Patch p)
        {
            float since = Track.Clock - p.BrokeAt;
            return since < 0f ? 0 : since < Broken ? 1 : since < Broken + Refreeze ? 2 : 0;
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || _patches.Count == 0) return;
            if (_cars == null) _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            float dt = Time.fixedDeltaTime;
            foreach (var p in _patches) p.Loaded = false;
            foreach (var car in _cars)
            {
                if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                Vector2 pos = car.Body.position;
                foreach (var p in _patches)
                {
                    if (State(p) != 0 || !Contains(p, pos)) continue;
                    float top = car.Stats != null ? car.Stats.topSpeed : 18f;
                    float share = car.Body.linearVelocity.magnitude / Mathf.Max(1f, top);
                    float load = Mathf.Max(0f, SafeShare - share) * LoadRate + (car.IsDrifting || car.IsSliding ? SlideLoad : 0f);
                    p.Load += load * dt;
                    p.Loaded = true;
                    if (load > 0.2f && Random.value < dt * 14f) MeshKit.Emit(_spray, pos, 2);
                    if (p.Load >= 0.55f && Time.time - p.Groaned > 1.2f && MeshKit.Heard(pos))
                    {
                        p.Groaned = Time.time;
                        SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 0.35f);
                    }
                    if (p.Load >= 1f) Break(p, pos);
                }
            }
            foreach (var p in _patches)
                if (!p.Loaded && State(p) == 0) p.Load = Mathf.Max(0f, p.Load - Recover * dt);
        }

        void Break(Patch p, Vector2 at)
        {
            p.BrokeAt = Track.Clock;
            p.Load = 0f;
            MeshKit.Emit(_shards, at, 40);
            MeshKit.Emit(_shards, p.Data.position, 20);
            if (MeshKit.Heard(at)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 0.9f);
        }

        void Update()
        {
            if (_patches.Count == 0) return;
            float time = Application.isPlaying ? Track.Clock : 0f;
            foreach (var p in _patches)
            {
                if (p.Ice == null) continue;
                int state = Application.isPlaying ? State(p) : 0;
                float since = time - p.BrokeAt;
                // Broken: the glass turns to open water (its colours sink to the water's); freezing over, back again.
                float open = state == 1 ? Mathf.Clamp01(since / 0.25f) : state == 2 ? 1f - Mathf.Clamp01((since - Broken) / Refreeze) : 0f;
                // About to break: a white pulse.
                float flash = state == 0 && p.Load > 0.75f ? (0.5f + 0.5f * Mathf.Sin(time * 30f)) * Mathf.InverseLerp(0.75f, 1f, p.Load) : 0f;
                var mesh = p.Ice.sharedMesh;
                var colours = new Color[p.IceColours.Length];
                for (int i = 0; i < colours.Length; i++)
                {
                    Color c = p.IceColours[i];
                    c = Color.Lerp(c, Color.Lerp(Water, WaterLight, (i % 3 == 0) ? 0.6f : 0f), open);
                    c = Color.Lerp(c, Rim, flash * 0.6f);
                    colours[i] = c;
                }
                mesh.SetColors(colours);
                // Cracks grow outwards with the load (and stay in the floating shards while it is open).
                var cracks = p.Cracks.sharedMesh;
                var cc = new Color[p.CrackColours.Length];
                float grow = state == 0 ? p.Load * 1.15f : 0f;
                for (int i = 0; i < cc.Length; i++)
                {
                    Color c = p.CrackColours[i];
                    c.a = Mathf.Clamp01((grow - p.CrackReach[i]) * 4f) * (0.85f + flash * 0.15f);
                    cc[i] = c;
                }
                cracks.SetColors(cc);
            }
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near)
        {
            foreach (var patch in _patches)
                if (State(patch) == 1 && Contains(patch, p, 0.88f)) return true;
            return false;
        }

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            float top = car.Stats != null ? car.Stats.topSpeed : 18f;
            foreach (var p in _patches)
            {
                float to = path.DeltaDistance(distance, p.Distance);
                if (to < -p.HalfAlong || to > 34f) continue;
                if (State(p) != 0)
                {
                    // Open water ahead: round it if there is room beside it, else wait for it to freeze.
                    float half = track.HalfWidthAt(p.Distance) - 1f;
                    float left = p.Lateral + p.HalfAcross + 1.6f, right = p.Lateral - p.HalfAcross - 1.6f;
                    bool leftOk = left <= half, rightOk = right >= -half;
                    if (leftOk || rightOk)
                    {
                        float dodge = leftOk && (!rightOk || Mathf.Abs(left - lateral) < Mathf.Abs(right - lateral)) ? left : right;
                        lateral = Mathf.Lerp(lateral, dodge, Mathf.InverseLerp(34f, 12f, to));
                    }
                    else if (to > 0f) speed = Mathf.Min(speed, to < 10f ? 0f : (to - 10f) * 1.2f);
                    continue;
                }
                // Whole ice ahead or under it: keep the speed up (a weak rival sometimes lifts off - and goes in).
                bool nervous = MeshKit.Hash(p.Distance, (car.name.GetHashCode() & 0xFFFF) + Mathf.Floor(Track.Clock / 30f)) > skill + 0.2f;
                if (to >= 14f) continue;
                speed = nervous ? Mathf.Min(speed, top * 0.42f) : Mathf.Max(speed, top * Mathf.Lerp(0.7f, 0.9f, skill));
            }
        }
    }
}
