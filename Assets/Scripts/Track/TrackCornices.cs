using System.Collections.Generic;
using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Cornices (<see cref="TrackLayout.cornices"/>): on a ridge bend the outer strip of the road is an overhang of snow - a
    /// white curled lip with a dark crack along its root. Once the first car has gone past a stretch of it, that stretch
    /// sags (snow dust puffs up along the crack), then breaks off and tumbles away into the depths; from then on the
    /// road there ends at a jagged blue ice edge. The leader gets the whole width, everyone behind a narrower bend -
    /// and whoever is out on the lip when it goes, goes with it.
    /// </summary>
    public class TrackCornices : TrackDerivedBehaviour, Track.IHazard
    {
        [SerializeField] Material plain;
        [SerializeField] Material particleMaterial;

        const int HoleOrder = -44, LipOrder = -43;
        const float ChunkLength = 3.2f, Delay = 0.9f, Sag = 0.6f, Tumble = 0.9f;

        static readonly Color Snow = MeshKit.Hex("F8FBFF"), SnowShade = MeshKit.Hex("C9DCEB"), Curl = MeshKit.Hex("A9C3D8"),
            CrackLine = MeshKit.Hex("3F5F7A"), Depth = MeshKit.Hex("1D2638"), DepthDeep = MeshKit.Hex("10151F"),
            IceEdge = MeshKit.Hex("8FD0EC"), IceEdgeLight = MeshKit.Hex("D8F2FF");

        class Chunk
        {
            public float From, To, Side, Width;
            public float FallAt = float.PositiveInfinity;
            public Transform Lip;
            public GameObject Hole;
            public Vector2 Out; // outward direction (for the tumble)
            public Vector3 Home;
            public float Puffed;
        }

        readonly List<Chunk> _chunks = new();
        readonly List<GameObject> _parts = new();
        TrackSensor[] _sensors;
        ParticleSystem _dust, _fall;

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
            _chunks.Clear();
            if (layout.cornices == null || layout.cornices.Count == 0) return;
            var root = new GameObject("Cornices") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var kit = new MeshKit();
            foreach (var cornice in layout.cornices)
            {
                float length = path.DeltaDistance(cornice.from, cornice.to);
                int count = Mathf.Max(1, Mathf.RoundToInt(length / ChunkLength));
                for (int k = 0; k < count; k++)
                {
                    float from = cornice.from + length * k / count, to = cornice.from + length * (k + 1) / count;
                    var chunk = new Chunk { From = from, To = to, Side = Mathf.Sign(cornice.side), Width = cornice.width };
                    // taper in and out at the ends of the overhang
                    float taper = Mathf.Min(1f, Mathf.Min(k + 1, count - k) / 2.5f);
                    chunk.Width *= Mathf.Lerp(0.45f, 1f, taper);
                    float mid = (from + to) * 0.5f;
                    chunk.Out = path.NormalAt(mid) * chunk.Side;
                    BuildChunk(root, path, layout, chunk, kit, k);
                    _chunks.Add(chunk);
                }
            }
            if (Application.isPlaying && particleMaterial != null)
            {
                _dust = MeshKit.Burst(root, "CorniceDust", particleMaterial, LipOrder + 1, new Color(0.95f, 0.98f, 1f, 0.75f), new Vector2(0.4f, 1.8f), new Vector2(0.4f, 1.1f), 1.1f, 0.6f);
                _fall = MeshKit.Burst(root, "CorniceFall", particleMaterial, LipOrder + 1, new Color(1f, 1f, 1f, 0.9f), new Vector2(2f, 6f), new Vector2(0.5f, 1.4f), 1.2f, 1.2f);
            }
        }

        void BuildChunk(Transform root, TrackPath path, TrackLayout layout, Chunk chunk, MeshKit kit, int index)
        {
            float s = chunk.Side;
            float Root(float d) => track.HalfWidthAt(d) - chunk.Width;
            float Rim(float d) => track.OffRoadAt(d) + 1.1f;
            // Where the overhang was: the jagged ice edge and the drop beyond it (shown once it has fallen).
            kit.Clear();
            const float step = 0.8f;
            for (float d = chunk.From; d < chunk.To - 0.01f; d += step)
            {
                float d1 = Mathf.Min(chunk.To, d + step);
                Vector2 a = path.PointAt(d), b = path.PointAt(d1), na = path.NormalAt(d) * s, nb = path.NormalAt(d1) * s;
                float ja = 0.25f * Mathf.Sin(d * 3.1f) + 0.15f * Mathf.Sin(d * 7.7f), jb = 0.25f * Mathf.Sin(d1 * 3.1f) + 0.15f * Mathf.Sin(d1 * 7.7f);
                Vector2 ra = a + na * (Root(d) + ja), rb = b + nb * (Root(d1) + jb);
                // blue ice face along the break, then the depths
                kit.Quad(ra, rb, rb + nb * 0.55f, ra + na * 0.55f, IceEdgeLight, IceEdgeLight, IceEdge, IceEdge);
                kit.Quad(ra + na * 0.55f, rb + nb * 0.55f, b + nb * (Rim(d1) + 1.5f), a + na * (Rim(d) + 1.5f), Depth, Depth, DepthDeep, DepthDeep);
            }
            var hole = MeshKit.Part(root, "CorniceGone", kit.Build("CorniceGone"), plain, HoleOrder, out _);
            chunk.Hole = hole.gameObject;
            chunk.Hole.SetActive(false);

            // The lip: snow from the crack at its root to the curled edge out over the drop, built round its own centre.
            kit.Clear();
            float mid = (chunk.From + chunk.To) * 0.5f;
            Vector2 centre = path.PointAt(mid) + path.NormalAt(mid) * s * ((Root(mid) + Rim(mid)) * 0.5f);
            for (float d = chunk.From; d < chunk.To - 0.01f; d += step)
            {
                float d1 = Mathf.Min(chunk.To, d + step);
                Vector2 a = path.PointAt(d), b = path.PointAt(d1), na = path.NormalAt(d) * s, nb = path.NormalAt(d1) * s;
                float wave0 = 0.35f * Mathf.Sin(d * 1.3f + index), wave1 = 0.35f * Mathf.Sin(d1 * 1.3f + index);
                Vector2 ra = a + na * Root(d) - centre, rb = b + nb * Root(d1) - centre;
                Vector2 ea = a + na * (Rim(d) + wave0) - centre, eb = b + nb * (Rim(d1) + wave1) - centre;
                Vector2 ma = Vector2.Lerp(ra, ea, 0.7f), mb = Vector2.Lerp(rb, eb, 0.7f);
                kit.Quad(ra, rb, mb, ma, Snow, Snow, SnowShade, SnowShade);
                kit.Quad(ma, mb, eb, ea, SnowShade, SnowShade, Curl, Curl);
                // the crack along its root
                kit.Quad(ra - na * 0.06f, rb - nb * 0.06f, rb + nb * 0.12f, ra + na * 0.12f, CrackLine);
            }
            var lip = MeshKit.Part(root, "Cornice", kit.Build("Cornice"), plain, LipOrder, out _);
            lip.transform.position = centre;
            chunk.Lip = lip.transform;
            chunk.Home = centre;
        }

        void Update()
        {
            if (!Application.isPlaying || _chunks.Count == 0) return;
            if (_sensors == null || _sensors.Length == 0) _sensors = FindObjectsByType<TrackSensor>(FindObjectsSortMode.None);
            float now = Track.Clock;
            var path = track.Path;
            foreach (var chunk in _chunks)
            {
                if (float.IsPositiveInfinity(chunk.FallAt))
                {
                    // The first car past it sets it going.
                    foreach (var sensor in _sensors)
                    {
                        if (sensor == null) continue;
                        float past = path.DeltaDistance(chunk.To, sensor.Current.Distance);
                        if (past < 0.5f || past > 25f) continue;
                        chunk.FallAt = now + Delay + 0.15f * Mathf.Sin(chunk.From);
                        break;
                    }
                    continue;
                }
                float until = chunk.FallAt - now;
                if (chunk.Lip == null) continue;
                if (until > 0f)
                {
                    if (until < Sag)
                    {
                        // sagging: it drops a little and dust puffs up along the crack
                        float sag = 1f - until / Sag;
                        chunk.Lip.position = chunk.Home + (Vector3)(chunk.Out * (0.12f * sag) + Random.insideUnitCircle * 0.04f);
                        if (Time.time - chunk.Puffed > 0.15f)
                        {
                            chunk.Puffed = Time.time;
                            MeshKit.Emit(_dust, (Vector2)chunk.Home - chunk.Out * chunk.Width * 0.4f, 3);
                        }
                    }
                    continue;
                }
                float t = -until;
                if (!chunk.Hole.activeSelf)
                {
                    chunk.Hole.SetActive(true);
                    MeshKit.Emit(_fall, chunk.Home, 14);
                    if (MeshKit.Heard(chunk.Home)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.snowPoof, 0.6f);
                }
                if (t < Tumble)
                {
                    // tumbling away: out over the edge, shrinking into the depths
                    float u = t / Tumble;
                    chunk.Lip.position = chunk.Home + (Vector3)(chunk.Out * (2.2f * u + 1.5f * u * u));
                    chunk.Lip.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, u * u);
                    chunk.Lip.rotation = Quaternion.Euler(0f, 0f, 40f * u * Mathf.Sign(Mathf.Sin(chunk.From)));
                }
                else if (chunk.Lip.gameObject.activeSelf) chunk.Lip.gameObject.SetActive(false);
            }
        }

        /// <summary>A new race: every cornice whole again.</summary>
        public void ResetCornices()
        {
            foreach (var chunk in _chunks)
            {
                chunk.FallAt = float.PositiveInfinity;
                if (chunk.Lip == null) continue;
                chunk.Lip.gameObject.SetActive(true);
                chunk.Lip.position = chunk.Home;
                chunk.Lip.localScale = Vector3.one;
                chunk.Lip.rotation = Quaternion.identity;
                chunk.Hole.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ hazard

        public bool VoidAt(Vector2 p, TrackPoint near)
        {
            if (_chunks.Count == 0) return false;
            float now = Track.Clock;
            var path = track.Path;
            foreach (var chunk in _chunks)
            {
                if (now < chunk.FallAt) continue;
                if (path.DeltaDistance(chunk.From, near.Distance) < 0f || path.DeltaDistance(near.Distance, chunk.To) < 0f) continue;
                if (near.Lateral * chunk.Side > track.HalfWidthAt(near.Distance) - chunk.Width) return true;
            }
            return false;
        }

        public void Guide(CarMovement car, float skill, float distance, float ahead, ref float lateral, ref float speed)
        {
            var path = track.Path;
            foreach (var chunk in _chunks)
            {
                float to = path.DeltaDistance(ahead, chunk.From), past = path.DeltaDistance(chunk.To, ahead);
                if (to > 6f || past > 2f) continue;
                bool fallen = Track.Clock >= chunk.FallAt - Sag;
                // A careful rival keeps off the lip altogether; a careless one only once it has gone.
                if (!fallen && MeshKit.Hash(chunk.From, (car.name.GetHashCode() & 0xFFFF)) > skill + 0.1f) continue;
                float limit = track.HalfWidthAt(ahead) - chunk.Width - 1.4f;
                if (lateral * chunk.Side > limit) lateral = chunk.Side * limit;
            }
        }
    }
}
