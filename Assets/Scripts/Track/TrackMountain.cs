using System;
using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The mountain itself (<see cref="TrackLayout.chasms"/>, <see cref="TrackLayout.caves"/>): chasms drawn as
    /// deep clefts in the snow (a snow cornice at the edge, rock walls dropping away into the dark), and caves
    /// through the mountain - rock walls heaped with boulders on both sides (solid), a rocky floor beside the road, the
    /// mountain's rocky flank round the passage, studded with boulders (the passage stays open to the view), a
    /// little dusk inside with glowing ice crystals and ice stalagmites to steer round.
    /// </summary>
    public class TrackMountain : TrackDerivedBehaviour
    {
        [Tooltip("Plain vertex-colour material (chasms, walls, shadows, the dark).")]
        [SerializeField] Material plain;
        [Tooltip("Rock (tiled): the cave's floor beside the road and the mountain's flank round the passage.")]
        [SerializeField] Material floorMaterial;
        [SerializeField] Sprite crystalSprite, stalagmiteSprite, glowSprite, boulderSprite;
        [SerializeField] Material particleMaterial;

        const int ChasmOrder = -54, ChasmRimOrder = -53, FloorOrder = -53, WallOrder = -40,
                  StalagmiteOrder = 7, DarkOrder = 50, CrystalOrder = 51, GlowOrder = 52, FlankOrder = -42;
        const float FloorMargin = 1.2f, WallWidth = 2.4f, DuskAlpha = 0.2f;

        // Vertex colours are linear (the project renders in linear space): these are the colours as seen on screen.
        static readonly Color Depth = new Color(0.14f, 0.18f, 0.27f).linear, RimRock = new Color(0.56f, 0.61f, 0.69f).linear, Cornice = new(0.97f, 0.98f, 1f),
            Edge = new Color(0.2f, 0.23f, 0.3f).linear, WallTop = new Color(0.6f, 0.65f, 0.73f).linear, WallFrost = new Color(0.86f, 0.9f, 0.96f).linear,
            Dark = new Color(0.05f, 0.08f, 0.17f).linear, FlankTint = new Color(0.95f, 0.97f, 1f);

        readonly List<GameObject> _parts = new();

        class CaveView
        {
            public TrackLayout.Cave Data;
            public readonly List<(SpriteRenderer sr, float phase)> Glows = new();
        }

        readonly List<CaveView> _caves = new();

        // A stretch of road (main road or branch) as a function of distance along it.
        struct Route
        {
            public Func<float, Vector2> Point, Normal;
            public Func<float, float> Half;
            public float From, Span;
        }

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear();
            _caves.Clear();
            if (layout.chasms != null)
                foreach (var chasm in layout.chasms)
                    if (chasm.points != null && chasm.points.Length >= 3) BuildChasm(chasm);
            if (layout.caves != null)
                foreach (var cave in layout.caves)
                {
                    var route = RouteOf(path, layout, cave);
                    if (route.HasValue && route.Value.Span > 2f) BuildCave(cave, route.Value);
                }
            if (Application.isPlaying && layout.chasms is { Count: > 0 } && particleMaterial != null) BuildMist(layout);
        }

        Route? RouteOf(TrackPath path, TrackLayout layout, TrackLayout.Cave cave)
        {
            if (cave.branch < 0)
                return new Route { Point = path.PointAt, Normal = path.NormalAt, Half = track.OffRoadAt, From = cave.from, Span = path.DeltaDistance(cave.from, cave.to) };
            if (cave.branch >= track.Branches.Count) return null;
            var b = track.Branches[cave.branch];
            float kerb = layout.kerbWidth;
            float to = cave.to <= 0f ? b.Length + cave.to : Mathf.Min(cave.to, b.Length);
            Vector2 Normal(float a)
            {
                Vector2 t = (b.TangentAt(a - 1.5f) + b.TangentAt(a + 1.5f)).normalized;
                return new Vector2(-t.y, t.x);
            }
            return new Route { Point = b.PointAt, Normal = Normal, Half = _ => b.HalfWidth + kerb, From = cave.from, Span = to - cave.from };
        }

        // ------------------------------------------------------------------ chasms

        void BuildChasm(TrackLayout.Chasm chasm)
        {
            // The outline every metre or so, a little ragged.
            var outline = Resample(chasm.points, 1.5f);
            int n = outline.Count;
            if (n < 3) return;
            float area = 0f;
            for (int i = 0; i < n; i++) area += Cross(outline[i], outline[(i + 1) % n]);
            float inward = area > 0f ? 1f : -1f; // counter-clockwise: the inside is to the left
            var rim = new Vector2[n];
            var lip = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 dir = (outline[(i + 1) % n] - outline[(i - 1 + n) % n]).normalized;
                Vector2 left = new(-dir.y, dir.x);
                float jag = Noise1(i * 0.7f, chasm.points[0].x);
                rim[i] = outline[i] + left * inward * (2.6f + jag * 1.6f);   // where the cliff wall meets the dark
                lip[i] = outline[i] - left * inward * (0.8f + jag * 0.5f);   // the snow cornice overhanging the edge
            }

            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            // The depths.
            foreach (var tri in Triangulate(outline))
            {
                int k = v.Count;
                foreach (int idx in tri) { v.Add(outline[idx]); c.Add(Depth); }
                t.Add(k); t.Add(k + 1); t.Add(k + 2);
            }
            Part("ChasmDepth", MakeMesh("ChasmDepth", v, c, t, both: true), plain, ChasmOrder);

            v.Clear(); c.Clear(); t.Clear();
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                // The rock wall dropping away (light at the top, into the dark), banded with strata.
                float band = Mathf.Repeat(i * 0.37f, 1f) < 0.5f ? 1f : 0.88f;
                AddQuad(outline[i], outline[j], rim[j], rim[i], RimRock * band, RimRock * band, Depth, Depth, v, c, t);
                // The cornice: white snow up to a dark edge line.
                AddQuad(lip[i], lip[j], outline[j], outline[i], Cornice, Cornice, Cornice, Cornice, v, c, t);
                Vector2 e0 = Vector2.Lerp(outline[i], rim[i], 0.08f), e1 = Vector2.Lerp(outline[j], rim[j], 0.08f);
                AddQuad(outline[i], outline[j], e1, e0, Edge, Edge, Edge, Edge, v, c, t);
                Vector2 s0 = lip[i] + (lip[i] - outline[i]).normalized * 0.6f, s1 = lip[j] + (lip[j] - outline[j]).normalized * 0.6f;
                var clear = new Color(0.55f, 0.65f, 0.8f, 0f);
                AddQuad(s0, s1, lip[j], lip[i], clear, clear, new Color(0.55f, 0.65f, 0.8f, 0.35f), new Color(0.55f, 0.65f, 0.8f, 0.35f), v, c, t);
            }
            Part("ChasmRim", MakeMesh("ChasmRim", v, c, t, both: true), plain, ChasmRimOrder);
        }

        // Faint mist drifting up out of the chasms, round the camera.
        void BuildMist(TrackLayout layout)
        {
            var go = new GameObject("ChasmMist") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _parts.Add(go);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startColor = new Color(0.85f, 0.92f, 1f, 0.22f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.6f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = ChasmRimOrder + 1;
            system.Play();
            _mist = system;
        }

        ParticleSystem _mist;

        void EmitMist()
        {
            if (_mist == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 centre = cam.transform.position;
            float h = cam.orthographicSize, w = h * cam.aspect;
            for (int k = 0; k < 2; k++)
            {
                if (UnityEngine.Random.value > Time.deltaTime * 10f) continue;
                Vector2 p = centre + new Vector2(UnityEngine.Random.Range(-w, w), UnityEngine.Random.Range(-h, h));
                if (!track.ChasmAt(p)) continue;
                var on = track.Path.Project(p);
                if (Mathf.Abs(on.Lateral) < track.OffRoadAt(on.Distance) + 1.5f) continue; // not over the road
                _mist.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector2(0.3f, 0.5f), applyShapeToPosition = false }, 1);
            }
        }

        // ------------------------------------------------------------------ caves

        void BuildCave(TrackLayout.Cave cave, Route r)
        {
            var view = new CaveView { Data = cave };
            int steps = Mathf.Max(2, Mathf.CeilToInt(r.Span));
            var walls = new List<Vector3>(); var wc = new List<Color>(); var wt = new List<int>();
            var floor = new List<Vector3>(); var fuv = new List<Vector2>(); var ft = new List<int>();
            var collider = new GameObject("CaveWalls") { hideFlags = HideFlags.DontSave };
            collider.transform.SetParent(transform, false);
            _parts.Add(collider);
            float seed = cave.from * 0.37f + cave.branch * 11f;

            // Rock walls: the inner line (solid), a band of rock with a frosted top, a ragged outer edge. At each
            // mouth they splay out a little so you drive in cleanly.
            var inner = new List<Vector2>[2] { new(), new() };
            var outer = new List<Vector2>[2] { new(), new() };
            var flankEdge = new List<Vector2>[2] { new(), new() };
            for (int i = 0; i <= steps; i++)
            {
                float s = r.Span * i / steps, d = r.From + s;
                Vector2 p = r.Point(d), n = r.Normal(d);
                float half = r.Half(d) + FloorMargin;
                float mouth = Mathf.Min(s, r.Span - s);
                float flare = mouth < 5f ? 2.2f * (1f - mouth / 5f) * (1f - mouth / 5f) : 0f;
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? 1f : -1f;
                    float jag = Noise1(i * 0.45f, seed + side * 5f);
                    inner[side].Add(p + n * sign * (half + flare));
                    outer[side].Add(p + n * sign * (half + flare + WallWidth * (0.8f + jag * 0.5f)));
                    flankEdge[side].Add(p + n * sign * (half + WallWidth + 4.5f + Noise1(i * 0.2f, seed + 9f + side) * 2.5f));
                }
            }
            for (int side = 0; side < 2; side++)
            {
                var a = inner[side]; var b = outer[side];
                for (int i = 1; i < a.Count; i++)
                {
                    AddQuad(a[i - 1], a[i], b[i], b[i - 1], Edge, Edge, Edge, Edge, walls, wc, wt);
                    Vector2 a0 = Vector2.Lerp(a[i - 1], b[i - 1], 0.18f), a1 = Vector2.Lerp(a[i], b[i], 0.18f);
                    Vector2 b0 = Vector2.Lerp(a[i - 1], b[i - 1], 0.8f), b1 = Vector2.Lerp(a[i], b[i], 0.8f);
                    Color top = Color.Lerp(WallTop, WallFrost, Noise1(i * 0.3f, seed + side * 3f));
                    AddQuad(a0, a1, b1, b0, top, top, WallFrost, WallFrost, walls, wc, wt);
                    // The floor beside the road, from the road's edge to the wall.
                    Vector2 inA = a[i - 1] + (a[i - 1] - b[i - 1]).normalized * FloorMargin * 1.5f;
                    Vector2 inB = a[i] + (a[i] - b[i]).normalized * FloorMargin * 1.5f;
                    int k = floor.Count;
                    floor.Add(inA); floor.Add(inB); floor.Add(b[i]); floor.Add(b[i - 1]);
                    foreach (var q in new[] { inA, inB, b[i], b[i - 1] }) fuv.Add(q / 6f);
                    ft.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
                var edge = collider.AddComponent<EdgeCollider2D>();
                edge.points = a.ToArray();
                edge.edgeRadius = 0.35f;
            }
            Part("CaveWalls", MakeMesh("CaveWalls", walls, wc, wt, both: true), plain, WallOrder);
            // Boulders heaped along both walls: a rough, natural rock face (over the wall's band).
            if (boulderSprite != null)
                for (int side = 0; side < 2; side++)
                {
                    var a = inner[side]; var b = outer[side];
                    for (float u = 0.5f; u < a.Count - 1; u += 1.8f + Hash(u, seed + side) * 1.4f)
                    {
                        int k = Mathf.Min((int)u, a.Count - 2);
                        float f = u - k;
                        Vector2 inPt = Vector2.Lerp(a[k], a[k + 1], f), outPt = Vector2.Lerp(b[k], b[k + 1], f);
                        float h = Hash(u * 3.1f, seed + side * 7f);
                        Vector2 at = Vector2.Lerp(inPt, outPt, 0.3f + h * 0.5f);
                        Sprite("CaveBoulder", boulderSprite, at, 2.4f + h * 1.6f, Hash(u * 1.7f, seed) * 360f, WallOrder + 1 + (h > 0.5f ? 1 : 0));
                    }
                }
            var floorMesh = new Mesh { name = "CaveFloor", hideFlags = HideFlags.DontSave };
            floorMesh.SetVertices(floor); floorMesh.SetUVs(0, fuv);
            var fcol = new Color[floor.Count];
            for (int i = 0; i < fcol.Length; i++) fcol[i] = new Color(0.8f, 0.82f, 0.88f, 1f);
            floorMesh.colors = fcol;
            floorMesh.SetTriangles(BothSides(ft), 0);
            floorMesh.RecalculateBounds();
            Part("CaveFloor", floorMesh, floorMaterial != null ? floorMaterial : plain, FloorOrder);

            // Ice stalagmites standing in the cave (solid): steer round them.
            if (cave.stalagmites != null)
                foreach (var spot in cave.stalagmites)
                {
                    float d = r.From + Mathf.Clamp(spot.x, 0f, r.Span);
                    Vector2 p = r.Point(d) + r.Normal(d) * spot.y;
                    var go = Sprite("Stalagmite", stalagmiteSprite, p, 2.6f, spot.x * 47f, StalagmiteOrder);
                    var circle = go.gameObject.AddComponent<CircleCollider2D>();
                    circle.radius = 0.85f / Mathf.Max(0.01f, go.transform.localScale.x);
                }

            // Glowing ice crystals along the walls.
            for (float s = 3f, k = 0f; s < r.Span - 2f; s += 6.5f + Noise1(s, seed) * 3f, k++)
            {
                float d = r.From + s;
                float sign = (int)k % 2 == 0 ? 1f : -1f;
                Vector2 p = r.Point(d) + r.Normal(d) * sign * (r.Half(d) + FloorMargin + 0.7f);
                Sprite("IceCrystal", crystalSprite, p, 1.8f + Noise1(s * 2f, seed) * 0.8f, s * 61f, CrystalOrder);
                var glow = Sprite("CrystalGlow", glowSprite, p, 5f, 0f, GlowOrder);
                glow.color = new Color(0.45f, 0.85f, 1f, 0.2f);
                view.Glows.Add((glow, s));
            }

            // A little dusk in the passage (soft at the mouths), over the cars too.
            var dv = new List<Vector3>(); var dc = new List<Color>(); var dt = new List<int>();
            for (int i = 1; i <= steps; i++)
            {
                float s0 = r.Span * (i - 1) / steps, s1 = r.Span * i / steps;
                float k0 = Mathf.Clamp01(Mathf.Min(s0, r.Span - s0) / 5f) * DuskAlpha, k1 = Mathf.Clamp01(Mathf.Min(s1, r.Span - s1) / 5f) * DuskAlpha;
                Color c0 = new(Dark.r, Dark.g, Dark.b, k0), c1 = new(Dark.r, Dark.g, Dark.b, k1);
                AddQuad(outer[1][i - 1], outer[1][i], outer[0][i], outer[0][i - 1], c0, c1, c1, c0, dv, dc, dt);
            }
            Part("CaveDusk", MakeMesh("CaveDusk", dv, dc, dt, both: true), plain, DarkOrder);

            // The mountain's flank on both sides of the passage: grey rock at ground level out to a ragged edge,
            // heaped with boulders (bigger along the edge), so the passage runs through solid rock.
            var mv = new List<Vector3>(); var muv = new List<Vector2>(); var mt = new List<int>();
            void Row(Vector2 a, Vector2 b, bool connect)
            {
                int k = mv.Count;
                mv.Add(a); mv.Add(b); muv.Add(a / 6f); muv.Add(b / 6f);
                if (connect) mt.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 });
            }
            int last = flankEdge[0].Count - 1;
            for (int side = 0; side < 2; side++)
                for (int i = 0; i <= last; i++) Row(outer[side][i], flankEdge[side][i], i > 0);
            var rock = new Mesh { name = "CaveFlank", hideFlags = HideFlags.DontSave };
            rock.SetVertices(mv); rock.SetUVs(0, muv);
            var tint = new Color[mv.Count];
            for (int i = 0; i < tint.Length; i++) tint[i] = FlankTint;
            rock.colors = tint;
            rock.SetTriangles(BothSides(mt), 0);
            rock.RecalculateBounds();
            Part("CaveFlank", rock, floorMaterial != null ? floorMaterial : plain, FlankOrder);
            if (boulderSprite != null)
                for (int side = 0; side < 2; side++)
                    for (float u = 0.3f; u < last; u += 1.6f + Hash(u, seed + 30f + side) * 1.6f)
                    {
                        int k = Mathf.Min((int)u, last - 1);
                        float f = u - k;
                        Vector2 inPt = Vector2.Lerp(outer[side][k], outer[side][k + 1], f), edgePt = Vector2.Lerp(flankEdge[side][k], flankEdge[side][k + 1], f);
                        float h = Hash(u * 2.3f, seed + 40f + side);
                        // Most of them along the ragged edge, the rest over the flank.
                        Vector2 at = Vector2.Lerp(inPt, edgePt, h < 0.6f ? 0.85f + h * 0.2f : 0.25f + h * 0.5f);
                        Sprite("FlankBoulder", boulderSprite, at, (h < 0.6f ? 3.2f : 2.4f) + Hash(u * 5.1f, seed) * 1.6f, Hash(u * 1.3f, seed + side) * 360f, FlankOrder + 1);
                    }
            _caves.Add(view);
        }

        static Vector2 Tangent(Route r, float d)
        {
            Vector2 n = r.Normal(d);
            return new Vector2(n.y, -n.x);
        }

        // ------------------------------------------------------------------ Play mode: mist, crystals

        void LateUpdate()
        {
            if (!Application.isPlaying || track == null || track.Layout == null) return;
            EmitMist();
            if (_caves.Count == 0) return;
            foreach (var view in _caves)
                foreach (var (glow, phase) in view.Glows)
                    if (glow != null) glow.color = new Color(0.45f, 0.85f, 1f, 0.2f + Mathf.Sin(Time.time * 2.2f + phase) * 0.06f);
        }

        // ------------------------------------------------------------------ helpers

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 position, float size, float angle, int order)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, sprite.bounds.size.x));
            _parts.Add(go);
            return sr;
        }

        void Part(string name, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            _parts.Add(go);
        }

        static Mesh MakeMesh(string name, List<Vector3> v, List<Color> c, List<int> t, bool both)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(both ? BothSides(t) : t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static List<int> BothSides(List<int> t)
        {
            var both = new List<int>(t);
            for (int i = 0; i < t.Count; i += 3) both.AddRange(new[] { t[i], t[i + 2], t[i + 1] });
            return both;
        }

        static void AddQuad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color ca, Color cb, Color ccc, Color cd, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            c.Add(ca); c.Add(cb); c.Add(ccc); c.Add(cd);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        // Smooth value noise in -1..1 (stable for a seed).
        static float Noise1(float x, float seed)
        {
            float i = Mathf.Floor(x), f = x - i;
            float a = Hash(i, seed), b = Hash(i + 1f, seed);
            return Mathf.Lerp(a, b, f * f * (3f - 2f * f)) * 2f - 1f;
        }

        static float Hash(float i, float seed)
        {
            float h = Mathf.Sin(i * 127.1f + seed * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        static List<Vector2> Resample(Vector2[] points, float spacing)
        {
            var list = new List<Vector2>();
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / spacing));
                for (int s = 0; s < steps; s++) list.Add(Vector2.Lerp(a, b, s / (float)steps));
            }
            return list;
        }

        // Ear clipping of a simple polygon: triangles as index triples.
        static List<int[]> Triangulate(List<Vector2> poly)
        {
            var result = new List<int[]>();
            var idx = new List<int>();
            for (int i = 0; i < poly.Count; i++) idx.Add(i);
            float area = 0f;
            for (int i = 0; i < poly.Count; i++) area += Cross(poly[i], poly[(i + 1) % poly.Count]);
            if (area < 0f) idx.Reverse();
            int guard = 0;
            while (idx.Count > 3 && guard++ < 20000)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int ia = idx[(i - 1 + idx.Count) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                    if (Cross(b - a, c - b) <= 1e-5f) continue; // reflex
                    bool empty = true;
                    foreach (int k in idx)
                    {
                        if (k == ia || k == ib || k == ic) continue;
                        if (InTriangle(poly[k], a, b, c)) { empty = false; break; }
                    }
                    if (!empty) continue;
                    result.Add(new[] { ia, ib, ic });
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (idx.Count == 3) result.Add(new[] { idx[0], idx[1], idx[2] });
            else
                // Not simple after all: fan the rest so nothing is left open.
                for (int i = 1; i + 1 < idx.Count; i++) result.Add(new[] { idx[0], idx[i], idx[i + 1] });
            return result;
        }

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a), d2 = Cross(c - b, p - b), d3 = Cross(a - c, p - c);
            return d1 >= 0f && d2 >= 0f && d3 >= 0f;
        }
    }
}
