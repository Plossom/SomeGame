using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SomeGame.Track
{
    /// <summary>
    /// The high-mountain look (Winter Cup finales). In every chasm a world far below: a glacier valley, a sea of clouds
    /// drifting over it - each layer moving slower than the ground the deeper it lies, so the road
    /// really runs high above everything (<see cref="TrackLayout.cloudSea"/>). Over the ground drift soft cloud shadows,
    /// the snow and ice glint in the sun, snow streams off the ridges (<see cref="TrackLayout.alpineFx"/>). The camera
    /// pulls back where the track wants to show what lies ahead (<see cref="TrackLayout.cameraZones"/>) and the colour
    /// grading follows the light of each part of the race (<see cref="TrackLayout.grades"/>).
    /// </summary>
    public class TrackAlpineFx : TrackDerivedBehaviour
    {
        [SerializeField] Material plain;
        [SerializeField] Material particleMaterial;
        [Tooltip("The scene's grading volume (global; its weight is 0 unless the track has grades).")]
        [SerializeField] Volume grade;

        const int ValleyOrder = -58, CloudOrder = -57, ShadowOrder = -30, GlintOrder = -29, DriftOrder = 7;
        static readonly Vector2 Wind = new(3.2f, -1.1f);

        // A layer far below: its texture, how deep it lies (0 = at infinity, 1 = on the ground), its scale, its drift.
        struct LayerSpec
        {
            public float Parallax, Scale;
            public Vector2 Drift;
            public int Order;
            public string Name;
        }

        static readonly LayerSpec[] Specs =
        {
            new() { Name = "Valley", Parallax = 0.25f, Scale = 1f / 42f, Drift = Vector2.zero, Order = ValleyOrder },
            new() { Name = "Clouds", Parallax = 0.5f, Scale = 1f / 48f, Drift = new Vector2(0.005f, -0.0018f), Order = CloudOrder },
        };

        class Layer
        {
            public LayerSpec Spec;
            public readonly List<(Mesh mesh, Vector3[] points)> Meshes = new();
            public Vector2[] Uv = System.Array.Empty<Vector2>();
        }

        readonly List<Layer> _layers = new();
        readonly List<GameObject> _parts = new();
        readonly List<Material> _materials = new();
        static Texture2D _valleyTex, _cloudTex;

        readonly List<(Transform t, Vector2 home, float speed)> _shadows = new();
        ParticleSystem _glints, _drift;
        SomeGame.Race.RaceManager _race;
        TrackSensor _playerSensor;
        ColorAdjustments _colour;
        Vignette _vignette;
        Bloom _bloom;
        float _zoom = 1f, _zoomVel;

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _layers.Clear();
            _shadows.Clear();
            foreach (var m in _materials) if (m != null) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
            _materials.Clear();
            if (layout.cloudSea && layout.chasms is { Count: > 0 } && plain != null) BuildCloudSea(layout);
            if (Application.isPlaying && layout.alpineFx) BuildFx();
        }

        // ------------------------------------------------------------------ the sea of clouds

        void BuildCloudSea(TrackLayout layout)
        {
            var root = new GameObject("CloudSea") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            Textures();
            var textures = new[] { _valleyTex, _cloudTex };
            for (int l = 0; l < Specs.Length; l++)
            {
                var layer = new Layer { Spec = Specs[l] };
                var material = new Material(plain) { name = "CloudSea" + Specs[l].Name, hideFlags = HideFlags.DontSave };
                material.SetTexture("_MainTex", textures[l]);
                _materials.Add(material);
                Color tint = l == 0 ? Color.Lerp(Color.white, layout.cloudTint, 0.5f) : layout.cloudTint;
                foreach (var chasm in layout.chasms)
                {
                    if (chasm.points == null || chasm.points.Length < 3) continue;
                    var outline = TrackMountain.Resample(chasm.points, 2f);
                    if (outline.Count < 3) continue;
                    var points = new Vector3[outline.Count];
                    var colours = new Color[outline.Count];
                    for (int i = 0; i < outline.Count; i++) { points[i] = outline[i]; colours[i] = MeshKit.Lin(tint); }
                    var tris = new List<int>();
                    foreach (var tri in TrackMountain.Triangulate(outline)) { tris.Add(tri[0]); tris.Add(tri[1]); tris.Add(tri[2]); tris.Add(tri[0]); tris.Add(tri[2]); tris.Add(tri[1]); }
                    var mesh = new Mesh { name = "CloudSea" + Specs[l].Name, hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
                    mesh.SetVertices(points);
                    mesh.SetColors(colours);
                    mesh.SetUVs(0, new Vector2[points.Length]);
                    mesh.SetTriangles(tris, 0);
                    mesh.RecalculateBounds();
                    MeshKit.Part(root, "CloudSea" + Specs[l].Name, mesh, material, Specs[l].Order, out var filter);
                    Assign(filter, mesh);
                    layer.Meshes.Add((mesh, points));
                }
                _layers.Add(layer);
            }
            // The cliffs falling away: a soft shadow just inside every edge, over the world below.
            var shade = new MeshKit();
            var dark = new Color(0.03f, 0.05f, 0.12f, 0.6f);
            var clear = new Color(dark.r, dark.g, dark.b, 0f);
            foreach (var chasm in layout.chasms)
            {
                if (chasm.points == null || chasm.points.Length < 3) continue;
                var outline = TrackMountain.Resample(chasm.points, 2f);
                int n = outline.Count;
                float area = 0f;
                for (int i = 0; i < n; i++) area += outline[i].x * outline[(i + 1) % n].y - outline[(i + 1) % n].x * outline[i].y;
                float inward = area > 0f ? 1f : -1f;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    Vector2 di = (outline[(i + 1) % n] - outline[(i - 1 + n) % n]).normalized, dj = (outline[(j + 1) % n] - outline[(j - 1 + n) % n]).normalized;
                    Vector2 ni = new Vector2(-di.y, di.x) * inward, nj = new Vector2(-dj.y, dj.x) * inward;
                    shade.Quad(outline[i] + ni * 2.5f, outline[j] + nj * 2.5f, outline[j] + nj * 13f, outline[i] + ni * 13f, dark, dark, clear, clear);
                }
            }
            MeshKit.Part(root, "CliffShade", shade.Build("CliffShade"), plain, CloudOrder + 1, out var shadeFilter);
            Assign(shadeFilter, shadeFilter.sharedMesh);
            UpdateCloudSea(Vector2.zero, 0f);
        }

        void UpdateCloudSea(Vector2 cam, float time)
        {
            foreach (var layer in _layers)
            {
                // Far below, the layer slides with the camera: the deeper, the more it keeps its place on screen.
                Vector2 shift = cam * (1f - layer.Spec.Parallax);
                Vector2 drift = layer.Spec.Drift * time;
                foreach (var (mesh, points) in layer.Meshes)
                {
                    if (mesh == null) continue;
                    if (layer.Uv.Length != points.Length) layer.Uv = new Vector2[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        layer.Uv[i] = ((Vector2)points[i] - shift) * layer.Spec.Scale + drift;
                    mesh.SetUVs(0, layer.Uv, 0, points.Length);
                }
            }
        }

        // ------------------------------------------------------------------ cloud shadows, glints, spindrift

        void BuildFx()
        {
            var root = new GameObject("AlpineFx") { hideFlags = HideFlags.DontSave }.transform;
            root.SetParent(transform, false);
            _parts.Add(root.gameObject);
            var kit = new MeshKit();
            var rng = new System.Random(7);
            for (int i = 0; i < 6; i++)
            {
                kit.Clear();
                float r = 22f + (float)rng.NextDouble() * 22f;
                var dark = new Color(0.05f, 0.09f, 0.2f, 0.16f);
                kit.Disc(Vector2.zero, new Vector2(r, r * 0.62f), (float)rng.NextDouble() * 180f, dark, new Color(dark.r, dark.g, dark.b, 0f), 28);
                kit.Disc(new Vector2(r * 0.4f, r * 0.15f), new Vector2(r * 0.6f, r * 0.4f), 20f, new Color(dark.r, dark.g, dark.b, 0.08f), new Color(dark.r, dark.g, dark.b, 0f), 20);
                var part = MeshKit.Part(root, "CloudShadow", kit.Build("CloudShadow"), plain, ShadowOrder, out _);
                _shadows.Add((part.transform, new Vector2((float)rng.NextDouble() * 240f, (float)rng.NextDouble() * 240f), 0.8f + (float)rng.NextDouble() * 0.5f));
            }
            if (particleMaterial == null) return;
            _glints = MeshKit.Burst(root, "SunGlints", particleMaterial, GlintOrder, new Color(1f, 1f, 1f, 1f), new Vector2(0f, 0f), new Vector2(0.18f, 0.42f), 0.7f, 0f, 120);
            var size = _glints.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f)));
            _drift = MeshKit.Burst(root, "Spindrift", particleMaterial, DriftOrder, new Color(1f, 1f, 1f, 0.42f), new Vector2(0f, 0f), new Vector2(0.25f, 0.7f), 1.4f, 0f, 300);
            var renderer = _drift.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.25f;
            renderer.lengthScale = 1.5f;
            var limit = _drift.limitVelocityOverLifetime;
            limit.enabled = false;
        }

        void UpdateFx(Vector2 cam, float view)
        {
            float time = Time.time;
            // cloud shadows: drifting with the wind, wrapped round the camera
            const float tile = 240f;
            foreach (var (t, home, speed) in _shadows)
            {
                if (t == null) continue;
                Vector2 p = home + Wind * (speed * time) - cam + new Vector2(tile * 0.5f, tile * 0.5f);
                p = new Vector2(Mathf.Repeat(p.x, tile), Mathf.Repeat(p.y, tile)) - new Vector2(tile * 0.5f, tile * 0.5f);
                t.position = cam + p;
            }
            float w = view * (Camera.main != null ? Camera.main.aspect : 0.5f);
            // sun glints on the snow and ice (not over the void)
            if (_glints != null)
                for (int k = 0; k < 3; k++)
                {
                    if (Random.value > Time.deltaTime * 14f) continue;
                    Vector2 p = cam + new Vector2(Random.Range(-w, w), Random.Range(-view, view));
                    if (track.ChasmAt(p)) continue;
                    _glints.Emit(new ParticleSystem.EmitParams { position = p, velocity = Vector2.zero, applyShapeToPosition = false, startColor = new Color(1f, 1f, 1f, Random.Range(0.6f, 1f)) }, 1);
                }
            // spindrift: snow blowing off the road's edges where the drop is right beside them
            if (_drift != null && _playerSensor != null)
            {
                var path = track.Path;
                for (int k = 0; k < 4; k++)
                {
                    if (Random.value > Time.deltaTime * 22f) continue;
                    float d = _playerSensor.Current.Distance + Random.Range(-15f, 30f);
                    float side = Random.value < 0.5f ? -1f : 1f;
                    Vector2 n = path.NormalAt(d) * side;
                    Vector2 edge = path.PointAt(d) + n * (track.OffRoadAt(d) + 0.4f);
                    if (!track.ChasmAt(edge + n * 2f)) continue;
                    Vector2 v = n * Random.Range(2f, 5f) + Wind * 0.8f;
                    _drift.Emit(new ParticleSystem.EmitParams { position = edge - n * Random.Range(0f, 2.5f), velocity = v, applyShapeToPosition = false }, 1);
                }
            }
        }

        // ------------------------------------------------------------------ camera and colour

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying || track == null || track.Layout == null) return;
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            SomeGame.CameraRig.FollowCamera.SceneZoom = 1f;
            var layout = track.Layout;
            bool grading = layout.grades is { Count: > 0 } && grade != null;
            if (grade != null) grade.weight = grading ? 1f : 0f;
            if (!grading) return;
            var profile = grade.profile; // this race's own copy
            profile.TryGet(out _colour);
            profile.TryGet(out _vignette);
            profile.TryGet(out _bloom);
            var cam = Camera.main;
            if (cam != null && cam.TryGetComponent<UniversalAdditionalCameraData>(out var data)) data.renderPostProcessing = true;
        }

        void OnDestroy()
        {
            if (Application.isPlaying) SomeGame.CameraRig.FollowCamera.SceneZoom = 1f;
        }

        void LateUpdate()
        {
            if (track == null || track.Layout == null) return;
            var cam = Camera.main;
            Vector2 c = cam != null && Application.isPlaying ? (Vector2)cam.transform.position : Vector2.zero;
            if (_layers.Count > 0) UpdateCloudSea(c, Application.isPlaying ? Time.time : 0f);
            if (!Application.isPlaying) return;
            if (_playerSensor == null && _race != null && _race.Player != null) _playerSensor = _race.Player.GetComponent<TrackSensor>();
            UpdateFx(c, cam != null ? cam.orthographicSize : 16f);
            if (_playerSensor == null) return;
            float d = _playerSensor.Current.Distance;
            var layout = track.Layout;
            // A view over what lies ahead.
            float zoom = 1f;
            foreach (var zone in layout.cameraZones)
            {
                float into = track.Path.DeltaDistance(zone.from, d), span = track.Path.DeltaDistance(zone.from, zone.to);
                if (into < 0f || into > span) continue;
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.Min(into, span - into) / 25f);
                zoom = Mathf.Max(zoom, Mathf.Lerp(1f, zone.zoom, edge));
            }
            _zoom = Mathf.SmoothDamp(_zoom, zoom, ref _zoomVel, 0.8f);
            SomeGame.CameraRig.FollowCamera.SceneZoom = _zoom;
            if (_colour != null) Grade(layout, d);
        }

        void Grade(TrackLayout layout, float d)
        {
            var grades = layout.grades;
            // the last grade whose start is behind us (wrapping round the lap), and the one before it
            int cur = -1;
            float best = float.MaxValue;
            for (int i = 0; i < grades.Count; i++)
            {
                float behind = Mathf.Repeat(d - grades[i].from, track.Path.Length);
                if (behind < best) { best = behind; cur = i; }
            }
            var g = grades[cur];
            int prevIndex = (cur - 1 + grades.Count) % grades.Count;
            var p = grades[prevIndex];
            // blend in over the metres before it starts (we are 'best' metres past its start)
            float t = 1f;
            int next = (cur + 1) % grades.Count;
            float toNext = Mathf.Repeat(grades[next].from - d, track.Path.Length);
            if (toNext < grades[next].blend) { p = g; g = grades[next]; t = 1f - toNext / grades[next].blend; }
            t = Mathf.SmoothStep(0f, 1f, t);
            _colour.colorFilter.Override(Color.Lerp(p.filter, g.filter, t));
            _colour.saturation.Override(Mathf.Lerp(p.saturation, g.saturation, t));
            _colour.contrast.Override(Mathf.Lerp(p.contrast, g.contrast, t));
            _colour.postExposure.Override(Mathf.Lerp(p.exposure, g.exposure, t));
            if (_vignette != null) _vignette.intensity.Override(Mathf.Lerp(p.vignette, g.vignette, t));
            if (_bloom != null) _bloom.intensity.Override(Mathf.Lerp(p.bloom, g.bloom, t));
        }

        // ------------------------------------------------------------------ textures (made once, tiling)

        static void Textures()
        {
            if (_cloudTex != null) return;
            const int n = 256;
            // Noise fields stretched to their full range (summed octaves bunch up round the middle).
            // the ridges, their coordinates bent by a slow noise (no straight lines)
            var ridge = Field(n, (x, y) => Ridged(x + (Fbm(x, y, 2, 2) - 0.5f) * 0.25f, y + (Fbm(x + 0.5f, y + 0.3f, 2, 2) - 0.5f) * 0.25f, 4, 4));
            var ice = Field(n, (x, y) => Fbm(x + 0.31f, y + 0.77f, 4, 3));
            var cloud = Field(n, (x, y) => Fbm(x, y, 3, 6));
            _valleyTex = Make(n, i =>
            {
                // a glacier valley far below: ridges of rock with snow on their crests, ice in the valleys
                float h = ridge[i];
                // deep slate valleys, lighter ridges, a little snow on the highest crests, pale ice in the hollows
                Color c = Color.Lerp(new Color(0.09f, 0.12f, 0.2f), new Color(0.26f, 0.31f, 0.42f), Edge(0.1f, 0.85f, h));
                c = Color.Lerp(c, new Color(0.62f, 0.69f, 0.8f), Edge(0.88f, 1f, h) * 0.7f);
                return Color.Lerp(c, new Color(0.22f, 0.33f, 0.45f), Edge(0.6f, 0.95f, ice[i]) * (1f - h) * 0.35f);
            });
            _cloudTex = Make(n, i =>
            {
                float v = cloud[i];
                float a = Edge(0.5f, 0.66f, v) * 0.95f;
                float shade = Mathf.Lerp(0.72f, 1f, Edge(0.6f, 0.9f, v));
                return new Color(shade * 0.95f, shade * 0.97f, shade, a);
            });
        }

        // 0 below e0, 1 above e1, smooth between (GLSL's smoothstep; Mathf.SmoothStep blends between its first two arguments).
        static float Edge(float e0, float e1, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(e0, e1, x));

        static float[] Field(int n, System.Func<float, float, float> f)
        {
            var v = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    v[y * n + x] = f(x / (float)n, y / (float)n);
            // stretch between the 4th and 96th percentiles: the full range, not just the rare extremes
            var sorted = (float[])v.Clone();
            System.Array.Sort(sorted);
            float lo = sorted[sorted.Length * 4 / 100], hi = sorted[sorted.Length * 96 / 100];
            for (int i = 0; i < v.Length; i++) v[i] = Mathf.Clamp01(Mathf.InverseLerp(lo, hi, v[i]));
            return v;
        }

        static Texture2D Make(int n, System.Func<int, Color> pixel)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = pixel(i);
            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        // Gradient noise that tiles over the unit square (period cells per side): smooth, no lattice squares.
        static float Noise(float x, float y, int period)
        {
            x *= period; y *= period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float G(int i, int j, float dx, float dy)
            {
                i = ((i % period) + period) % period; j = ((j % period) + period) % period;
                uint h = (uint)(i * 374761393 + j * 668265263 + period * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                float a = (h & 0xFFFF) / 65535f * Mathf.PI * 2f;
                return Mathf.Cos(a) * dx + Mathf.Sin(a) * dy;
            }
            float u = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f), v = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);
            float n = Mathf.Lerp(Mathf.Lerp(G(x0, y0, fx, fy), G(x0 + 1, y0, fx - 1f, fy), u), Mathf.Lerp(G(x0, y0 + 1, fx, fy - 1f), G(x0 + 1, y0 + 1, fx - 1f, fy - 1f), u), v);
            return 0.5f + n * 0.7f;
        }

        static float Fbm(float x, float y, int basePeriod, int octaves)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int period = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                sum += Noise(Mathf.Repeat(x, 1f), Mathf.Repeat(y, 1f), period) * amp;
                norm += amp;
                amp *= 0.5f;
                period *= 2;
            }
            return sum / norm;
        }

        static float Ridged(float x, float y, int basePeriod, int octaves)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int period = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                float v = 1f - Mathf.Abs(Noise(Mathf.Repeat(x, 1f), Mathf.Repeat(y, 1f), period) * 2f - 1f);
                sum += v * v * amp;
                norm += amp;
                amp *= 0.5f;
                period *= 2;
            }
            return sum / norm;
        }
    }
}
