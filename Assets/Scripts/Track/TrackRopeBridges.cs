using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Rope bridges (<see cref="TrackLayout.ropeBridges"/>): a deck of planks hanging on ropes over the void between two
    /// rock pillars. It swings slowly from side to side and carries the cars on it with it; the road under it is not drawn
    /// - the deck is the road, and off its edge you fall. Ropes run from the pillars' posts to the deck's corners, the
    /// handrails sag, and the deck's shadow sways on the clouds far below.
    /// </summary>
    public class TrackRopeBridges : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;

        const int ShadowOrder = -52, PillarOrder = -51, DeckOrder = -45, RopeOrder = -44;
        static readonly Color Plank = MeshKit.Hex("9C6B3E"), PlankDark = MeshKit.Hex("7E5530"), PlankLight = MeshKit.Hex("B5814E"),
            Seam = MeshKit.Hex("2A1A0E"), Rope = MeshKit.Hex("D9C29A"), RopeDark = MeshKit.Hex("8C7550"), Post = MeshKit.Hex("5A3B20"),
            Rock = MeshKit.Hex("8A93A3"), RockLight = MeshKit.Hex("B4BCC8"), RockDark = MeshKit.Hex("5E6676"), SnowCap = MeshKit.Hex("F4F8FC"),
            Shadow = new Color(0.02f, 0.04f, 0.1f, 0.16f);

        class Bridge
        {
            public TrackLayout.RopeBridge Data;
            public Transform Deck, Shadow;
            public MeshFilter Ropes;
            public Vector2 Normal, Mid;
            public float Last;
            public Vector2[] Anchors; // fixed posts on the pillars: from-left, from-right, to-left, to-right
            public Vector2[] Corners; // the deck's corners (deck space): same order
        }

        readonly List<Bridge> _bridges = new();
        readonly List<GameObject> _parts = new();
        CarMovement[] _cars;
        readonly MeshKit _ropeKit = new();

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

        /// <summary>How far the deck is swung to the left of the centre line now.</summary>
        public static float Offset(TrackLayout.RopeBridge b, float time) => b.sway * Mathf.Sin((time / b.period + b.phase) * Mathf.PI * 2f);

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _bridges.Clear();
            if (layout.ropeBridges == null || layout.ropeBridges.Count == 0) return;
            var root = new GameObject("RopeBridges") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var kit = new MeshKit();
            foreach (var data in layout.ropeBridges)
            {
                float length = path.DeltaDistance(data.from, data.to);
                float midD = data.from + length * 0.5f;
                var b = new Bridge { Data = data, Normal = path.NormalAt(midD), Mid = path.PointAt(midD) };
                float half = data.deckWidth * 0.5f;

                // The pillars: rocky outcrops at both ends, snow on top, wooden posts for the ropes.
                kit.Clear();
                foreach (float end in new[] { data.from, data.to })
                {
                    float dir = end == data.from ? -1f : 1f;
                    Vector2 c = path.PointAt(end + dir * 3.5f), n = path.NormalAt(end);
                    float r = Mathf.Max(half + data.sway, track.HalfWidthAt(end - dir * 2f)) + 2.2f;
                    // a ragged outcrop: lit rock, darker towards its foot, snow lying on top
                    float ang = Mathf.Atan2(path.TangentAt(end).y, path.TangentAt(end).x);
                    Vector2 ax = new(Mathf.Cos(ang), Mathf.Sin(ang)), ay = new(-ax.y, ax.x);
                    const int m = 22;
                    for (int q = 0; q < m; q++)
                    {
                        float u0 = q * Mathf.PI * 2f / m, u1 = (q + 1) * Mathf.PI * 2f / m;
                        float j0 = 1f + 0.16f * Mathf.Sin(q * 2.7f + end), j1 = 1f + 0.16f * Mathf.Sin((q + 1) * 2.7f + end);
                        Vector2 e0 = c + ax * Mathf.Cos(u0) * 6f * j0 + ay * Mathf.Sin(u0) * r * j0, e1 = c + ax * Mathf.Cos(u1) * 6f * j1 + ay * Mathf.Sin(u1) * r * j1;
                        kit.Tri(c, e0, e1, RockLight, Mathf.Sin(u0) < 0f ? RockDark : Rock, Mathf.Sin(u1) < 0f ? RockDark : Rock);
                    }
                    kit.Disc(c + ay * 0.8f + ax * 0.5f, new Vector2(4.2f, r * 0.7f), ang * Mathf.Rad2Deg, SnowCap, new Color(SnowCap.r, SnowCap.g, SnowCap.b, 0.85f), 20);
                }
                MeshKit.Part(root, "BridgePillars", kit.Build("BridgePillars"), plain, PillarOrder, out _);

                // The deck, built round its middle (it is moved sideways as a whole).
                kit.Clear();
                int k = 0;
                for (float s = 0f; s < length; s += 0.85f, k++)
                {
                    float d0 = data.from + s, d1 = data.from + Mathf.Min(length, s + 0.72f);
                    Vector2 a = path.PointAt(d0) - b.Mid, e = path.PointAt(d1) - b.Mid, n = b.Normal;
                    // a slightly uneven plank, every so often a darker (or missing-looking) one
                    float jag = 0.12f * Mathf.Sin(k * 2.3f);
                    Color col = k % 7 == 3 ? PlankDark : k % 3 == 1 ? PlankLight : Plank;
                    kit.Quad(a - n * (half + jag), e - n * (half + jag), e + n * (half - jag), a + n * (half - jag), col);
                    Vector2 g = path.PointAt(Mathf.Min(data.from + length, d1 + 0.13f)) - b.Mid;
                    kit.Quad(e - n * half, g - n * half, g + n * half, e + n * half, Seam);
                }
                // handrails along both edges, posts every 2.5 m
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 a = path.PointAt(data.from) - b.Mid + b.Normal * side * (half + 0.15f), e = path.PointAt(data.to) - b.Mid + b.Normal * side * (half + 0.15f);
                    kit.Line(a, e, 0.22f, Rope);
                    for (float s = 0f; s <= length; s += 2.5f)
                    {
                        Vector2 p = path.PointAt(data.from + s) - b.Mid + b.Normal * side * (half + 0.15f);
                        kit.Disc(p, 0.22f, Post, 8);
                    }
                }
                var deck = MeshKit.Part(root, "BridgeDeck", kit.Build("BridgeDeck"), plain, DeckOrder, out _);
                deck.transform.position = b.Mid;
                b.Deck = deck.transform;

                // Its shadow far below on the clouds: the deck's outline, dark and soft.
                kit.Clear();
                Vector2 sa = path.PointAt(data.from) - b.Mid, se = path.PointAt(data.to) - b.Mid;
                kit.Quad(sa - b.Normal * half, se - b.Normal * half, se + b.Normal * half, sa + b.Normal * half, Shadow);
                var shadow = MeshKit.Part(root, "BridgeShadow", kit.Build("BridgeShadow"), plain, ShadowOrder, out _);
                b.Shadow = shadow.transform;

                // Ropes from the pillar posts to the deck's corners (rebuilt as the deck swings).
                float postOut = half + data.sway + 1.2f;
                b.Anchors = new[]
                {
                    path.PointAt(data.from - 1.5f) + path.NormalAt(data.from) * postOut, path.PointAt(data.from - 1.5f) - path.NormalAt(data.from) * postOut,
                    path.PointAt(data.to + 1.5f) + path.NormalAt(data.to) * postOut, path.PointAt(data.to + 1.5f) - path.NormalAt(data.to) * postOut,
                };
                b.Corners = new[]
                {
                    sa + b.Normal * (half + 0.15f), sa - b.Normal * (half + 0.15f), se + b.Normal * (half + 0.15f), se - b.Normal * (half + 0.15f),
                };
                var ropes = MeshKit.Part(root, "BridgeRopes", new Mesh { name = "BridgeRopes", hideFlags = HideFlags.DontSave }, plain, RopeOrder, out b.Ropes);
                _ = ropes;
                // the anchor posts themselves
                kit.Clear();
                foreach (var post in b.Anchors) { kit.Disc(post, 0.45f, Post, 10); kit.Disc(post, 0.25f, RopeDark, 8); }
                MeshKit.Part(root, "BridgePosts", kit.Build("BridgePosts"), plain, RopeOrder, out _);
                b.Last = Offset(data, Application.isPlaying ? Track.Clock : 0f);
                _bridges.Add(b);
            }
            Place(Application.isPlaying ? Track.Clock : 0f);
        }

        void Place(float time)
        {
            foreach (var b in _bridges)
            {
                if (b.Deck == null) continue;
                float off = Offset(b.Data, time);
                // the deck leans a touch into its swing
                float lean = 1.6f * Mathf.Cos((time / b.Data.period + b.Data.phase) * Mathf.PI * 2f);
                Vector2 at = b.Mid + b.Normal * off;
                b.Deck.position = at;
                b.Deck.rotation = Quaternion.Euler(0f, 0f, lean * 0.25f);
                // far below: it moves less, further off
                b.Shadow.position = b.Mid + b.Normal * (off * 0.6f) + new Vector2(2.2f, -3.2f);
                _ropeKit.Clear();
                for (int i = 0; i < 4; i++)
                {
                    Vector2 corner = at + b.Corners[i];
                    _ropeKit.Line(b.Anchors[i], corner, 0.16f, Rope);
                    _ropeKit.Line(b.Anchors[i] + new Vector2(0.6f, -0.8f), corner + new Vector2(0.6f, -0.8f), 0.12f, new Color(0f, 0f, 0f, 0.18f));
                }
                var mesh = b.Ropes.sharedMesh;
                mesh.Clear();
                mesh.SetVertices(_ropeKit.V);
                mesh.SetColors(_ropeKit.C);
                mesh.SetTriangles(_ropeKit.T, 0);
                mesh.RecalculateBounds();
            }
        }

        void Update()
        {
            if (_bridges.Count == 0) return;
            Place(Application.isPlaying ? Track.Clock : 0f);
        }

        // Every car on a deck (on the ground) swings with it.
        void FixedUpdate()
        {
            if (!Application.isPlaying || _bridges.Count == 0) return;
            if (_cars == null) _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            float now = Track.Clock;
            var path = track.Path;
            foreach (var b in _bridges)
            {
                float off = Offset(b.Data, now), delta = off - b.Last;
                b.Last = off;
                if (Mathf.Abs(delta) < 1e-5f) continue;
                foreach (var car in _cars)
                {
                    if (car == null || !car.isActiveAndEnabled || car.IsAirborne || car.IsPuppet) continue;
                    var sensor = car.GetComponent<TrackSensor>();
                    if (sensor == null) continue;
                    float d = sensor.Current.Distance;
                    if (path.DeltaDistance(b.Data.from, d) < 0f || path.DeltaDistance(d, b.Data.to) < 0f) continue;
                    car.Body.position += b.Normal * delta;
                }
            }
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near)
        {
            if (_bridges.Count == 0) return false;
            var path = track.Path;
            float now = Track.Clock;
            foreach (var b in _bridges)
            {
                if (path.DeltaDistance(b.Data.from, near.Distance) < 0f || path.DeltaDistance(near.Distance, b.Data.to) < 0f) continue;
                return Mathf.Abs(near.Lateral - Offset(b.Data, now)) > b.Data.deckWidth * 0.5f + 0.25f;
            }
            return false;
        }

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            float top = car.Stats != null ? car.Stats.topSpeed : 18f;
            float v = Mathf.Max(6f, car.ForwardSpeed);
            foreach (var b in _bridges)
            {
                float to = path.DeltaDistance(ahead, b.Data.from), past = path.DeltaDistance(b.Data.to, ahead);
                if (to > 14f || past > 0f) continue;
                // Aim where the deck will be when the car gets there: a skilled rival reads the swing ahead, a weak one
                // follows it late (and sometimes drops off).
                float lead = Mathf.Max(0f, path.DeltaDistance(distance, ahead)) / v;
                float when = Track.Clock + Mathf.Lerp(-0.9f, lead, skill);
                float target = Offset(b.Data, when);
                lateral = to > 0f ? Mathf.Lerp(lateral, target, Mathf.InverseLerp(14f, 0f, to)) : target;
                speed = Mathf.Min(speed, top * Mathf.Lerp(0.72f, 0.92f, skill));
            }
        }
    }
}
