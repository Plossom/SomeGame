using System.Collections.Generic;
using UnityEngine;
using SomeGame.Car;

namespace SomeGame.Track
{
    /// <summary>The forest scenery sheet (16 cells of 256 px, 4 x 4, row 0 at the bottom), shared by the art and the race.</summary>
    public static class ForestSheet
    {
        public static readonly string[] Cells =
        {
            "Oak", "OakBig", "Pine", "PineDark",
            "Birch", "Bush", "Fern", "Mushrooms",
            "Stump", "Log", "Rock", "Flowers",
            "Reeds", "LilyPads", "Willow", "Clearing",
        };

        public static Rect Cell(string name)
        {
            int i = System.Array.IndexOf(Cells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) * 0.25f, 0.25f, 0.25f);
        }
    }

    /// <summary>
    /// The Water Cup's later races (<see cref="TrackLayout.wild"/>):
    /// - ledges: stretches where the channel runs along the top of the rocks (a rim, then a cliff face dropping away
    ///   to the sea far below) - leave the channel there and you fall; on a reef the coral flats either side, and you
    ///   run aground instead;
    /// - props: temples, ships, skull rock and the fortress, placed by hand (overhead ones fade while you are under);
    /// - crocodiles surfacing in the river (bubbles first), stone doors sliding shut across it;
    /// - a ship's cannon fire (a red ring marks where each ball will land), gold coins, plank ramps;
    /// - jellyfish drifting across the channel, manta rays leaping over it;
    /// - lightning striking the channel (a ring warns first; a small burst of light, no flash of the screen), waves
    ///   washing across the ledges, rain.
    /// </summary>
    public partial class TrackWild : TrackDerivedBehaviour
    {
        [SerializeField] Material plain, scenery, ground, water, sand, foam;
        [Tooltip("The forest sheet: pines over the mountains' tops (the cave roofs).")]
        [SerializeField] Material forest;
        [SerializeField] Sprite[] sprites = { };

        const int FaceOrder = -45, RimOrder = -44, RingOrder = 3, PropOrder = 4, CoinOrder = 7, ShadowOrder = 6, CrocOrder = 7,
                  DoorOrder = 20, BallOrder = 24, OverheadOrder = 30, BoltOrder = 32, RainOrder = 59, StormOrder = 57;

        readonly List<GameObject> _parts = new();
        CarMovement[] _cars = { };
        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.CameraRig.CameraShake _shake;
        TrackLayout.Wild _wild;
        Sprite _glow;

        Sprite Find(string name)
        {
            foreach (var s in sprites) if (s != null && s.name == name) return s;
            return null;
        }

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _overheads.Clear(); _crocs.Clear(); _doors.Clear(); _coins.Clear(); _planks.Clear(); _jellies.Clear();
            _mantas.Clear(); _strikes.Clear(); _waves.Clear(); _balls.Clear(); _fallen.Clear(); _caveViews.Clear(); _ships.Clear(); _snores.Clear(); _tentacles.Clear(); _fins.Clear(); _parrots.Clear(); _life.Clear(); _krakenHead = null;
            _rain = null; _storm = null;
            Release();
            _wild = layout.wild;
            if (layout.biome != TrackLayout.Biome.Lagoon || _wild == null) return;
            // (a lagoon race without the wild water still has its little lives)
            if (!_wild.enabled)
            {
                if (!Application.isPlaying || _wild.life == null || _wild.life.Count == 0) return;
                _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
                BuildLife(path);
                return;
            }
            if (_wild.river) BuildRiver(path, layout);
            foreach (var ledge in _wild.ledges) BuildLedge(path, ledge);
            foreach (var gap in layout.gaps) if (gap.drop && !gap.hidden) { BuildStackEnd(path, gap.from, 1f); BuildStackEnd(path, gap.to, -1f); }
            foreach (var cave in _wild.caves) BuildCave(path, cave);
            foreach (var g in _wild.galleons) BuildShip(path, g, true);
            foreach (var g in _wild.deckShips) BuildShip(path, g, false);
            foreach (var prop in _wild.props) BuildProp(prop);
            foreach (var c in _wild.crocs) BuildCroc(path, c);
            for (int i = 0; i < _wild.doors.Count; i++) BuildDoor(path, _wild.doors[i], i);
            foreach (var c in _wild.coins) BuildCoin(path, c);
            foreach (var p in _wild.planks) BuildPlank(path, p);
            foreach (var j in _wild.jellyfish) BuildJellies(path, j);
            foreach (float m in _wild.mantas) BuildManta(path, m);
            if (!Application.isPlaying) return;
            _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            _glow = Glow();
            if (_doors.Count > 0) Track.Obstacle = DoorShut;
            BuildKraken();
            BuildSharks();
            BuildParrots(path);
            BuildLife(path);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Release();
        }

        void Release()
        {
            if (Track.Obstacle == (System.Func<Vector2, bool>)DoorShut) Track.Obstacle = null;
        }

        // ------------------------------------------------------------------ helpers

        GameObject Mesh(string name, Material material, int order, Mesh mesh, Transform parent = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent != null ? parent : transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            _parts.Add(go);
            return go;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 at, float size, float angle, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, angle));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (plain != null) sr.sharedMaterial = plain;
            if (sprite != null) go.transform.localScale = Vector3.one * (size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            _parts.Add(go);
            return sr;
        }

        static float Heading(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        static Sprite Glow()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        bool IsPlayer(CarMovement car) => _race != null && _race.Player != null && car.gameObject == _race.Player.gameObject;

        void Say(CarMovement car, string text, Color color)
        {
            if (_combo != null && IsPlayer(car)) _combo.Announce(text, color);
        }

        static bool Free(CarMovement car) => car != null && !car.IsAirborne && !car.IsCarried && !car.IsPuppet && !car.IsRiding;

        // ------------------------------------------------------------------ the river world

        const int GroundOrder = -98, RiverOrder = -97, BankOrder = -96, ShoreFoamOrder = -95;

        // The land everywhere, the river a ribbon of water along the course, sandy banks fading into the jungle, foam
        // at the water's edge, and the shores solid.
        void BuildRiver(TrackPath path, TrackLayout layout)
        {
            var b = path.Bounds();
            float pad = layout.boundaryMargin + 60f;
            var gv = new List<Vector3>(); var gu = new List<Vector2>(); var gt = new List<int>(); var gc = new List<Color32>();
            Vector2 centre = b.center, half = b.size * 0.5f + Vector2.one * pad;
            TrackMeshes.AddQuad(gv, gu, gt, centre, new Vector2(half.x, 0f), new Vector2(0f, half.y), Vector2.one);
            for (int k = 0; k < 4; k++) { gu[k] = (Vector2)gv[k] / 4f; gc.Add(new Color32(255, 255, 255, 255)); }
            Mesh("JungleGround", ground != null ? ground : plain, GroundOrder, TrackMeshes.Quads("JungleGround", gv, gu, gt, gc));
            float margin = _wild.riverMargin;
            Ribbon("RiverWater", water, RiverOrder, path, d => -(track.OffRoadAt(d) + margin), d => track.OffRoadAt(d) + margin, Color.white, Color.white, 1f / 8f);
            foreach (float side in new[] { 1f, -1f })
            {
                Ribbon("RiverBank", sand, BankOrder, path, d => side * (track.OffRoadAt(d) + margin), d => side * (track.OffRoadAt(d) + margin + 3f),
                    Color.white, new Color(1f, 1f, 1f, 0f), 1f / 4f);
                Ribbon("RiverFoam", foam, ShoreFoamOrder, path, d => side * (track.OffRoadAt(d) + margin - 0.6f), d => side * (track.OffRoadAt(d) + margin + 0.3f),
                    new Color(1f, 1f, 1f, 0f), Color.white, 1f / 4f);
            }
            if (!Application.isPlaying) return;
            // Solid banks, broken where the river crosses itself (an aqueduct over it, or the river below one).
            foreach (float side in new[] { 1f, -1f })
            {
                var pts = new List<Vector2>();
                for (float d = 0f; d <= path.Length + 0.5f; d += 1f)
                {
                    Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + margin + 0.4f);
                    if (track.OnViaduct(d) || InOtherWater(path, p, d, margin))
                    {
                        Shore(pts);
                        pts = new List<Vector2>();
                        continue;
                    }
                    pts.Add(p);
                }
                Shore(pts);
            }
        }

        void Shore(List<Vector2> pts)
        {
            if (pts.Count < 2) return;
            var go = new GameObject("RiverShore", typeof(EdgeCollider2D)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.GetComponent<EdgeCollider2D>().points = pts.ToArray();
            _parts.Add(go);
        }

        // Inside the river somewhere else along the lap (not the stretch it belongs to)?
        bool InOtherWater(TrackPath path, Vector2 p, float d, float margin)
        {
            for (float o = 0f; o < path.Length; o += 2f)
            {
                if (Mathf.Abs(path.DeltaDistance(d, o)) < 20f) continue;
                if ((path.PointAt(o) - p).sqrMagnitude < (track.OffRoadAt(o) + margin + 1.5f) * (track.OffRoadAt(o) + margin + 1.5f)) return true;
            }
            return false;
        }

        // A strip along the whole lap from one sideways offset to another (functions of the lap distance), world UVs.
        void Ribbon(string name, Material material, int order, TrackPath path, System.Func<float, float> from, System.Func<float, float> to, Color cFrom, Color cTo, float uvScale)
        {
            if (material == null) return;
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
            int steps = Mathf.CeilToInt(path.Length);
            for (int i = 0; i <= steps; i++)
            {
                float d = path.Length * i / steps;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                Vector2 a = p + n * from(d), bb = p + n * to(d);
                v.Add(a); v.Add(bb);
                u.Add(a * uvScale); u.Add(bb * uvScale);
                c.Add(cFrom); c.Add(cTo);
                if (i == steps) break;
                int s = i * 2;
                t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s, s + 1, s + 2, s + 1, s + 3, s + 2 });
            }
            Mesh(name, material, order, TrackMeshes.Quads(name, v, u, t, c));
        }

        // ------------------------------------------------------------------ water caves

        class CaveView { public Vector2 Span; public MeshRenderer Roof, Gloom, RoofRocks, Walls, WallRocks, Mouths, MouthRocks; public Color32[] RoofColors, RoofRockColors, WallColors, WallRockColors, MouthColors, MouthRockColors; public float Shown = -1f; public readonly List<(SpriteRenderer sr, float phase)> Crystals = new(); public float Inside; }
        readonly List<CaveView> _caveViews = new();
        const int ShipInsideOrder = 3, DeckShipOrder = -40, SnoreOrder = 36, ParrotOrder = 41, CaveWallOrder = 6, CaveGloomOrder = 5, CrystalOrder = 7, RoofOrder = 33, RoofRockOrder = 34, MouthOrder = 35;

        // The channel runs into the mountain: the mountain's top carries on over it as a roof (it fades away while you
        // are inside, so you see your boat), dark rock arches at the mouths, dark water inside, crystals glowing on
        // the walls.
        void BuildCave(TrackPath path, Vector2 span)
        {
            var view = new CaveView { Span = span };
            float len = Mathf.Repeat(span.y - span.x, path.Length), roof = _wild.caveRoof;
            Color top = new(0.62f, 0.72f, 0.6f), rim = new(0.32f, 0.33f, 0.36f);
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
            var gv = new List<Vector3>(); var gu = new List<Vector2>(); var gt = new List<int>(); var gc = new List<Color32>();
            int steps = Mathf.CeilToInt(len);
            for (int i = 0; i <= steps; i++)
            {
                float d = span.x + len * i / steps;
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                // The roof narrows to an arch over the water at each mouth (it merges into the mountain either side),
                // its ends darker rock.
                float end = Mathf.Clamp01(Mathf.Min(i, steps - i) / 3f);
                float taper = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(i, steps - i) / 10f));
                float half = track.OffRoadAt(d) + 1.6f + (roof - 1.6f) * taper;
                var col = (Color32)Color.Lerp(rim, top, end);
                // (a soft fringe either side, so the roof melts into the mountain's woods instead of a hard edge)
                var clear = col; clear.a = 0;
                float fringe = 1.2f + 1.8f * taper;
                foreach (float lat in new[] { half + fringe, half, -half, -half - fringe }) { v.Add(p + n * lat); u.Add((p + n * lat) / 4f); }
                c.Add(clear); c.Add(col); c.Add(col); c.Add(clear);
                float g = track.OffRoadAt(d) + 0.8f;
                gv.Add(p + n * g); gv.Add(p - n * g);
                gu.Add(Vector2.zero); gu.Add(Vector2.zero);
                var dark = new Color32(5, 18, 34, (byte)(230f * Mathf.SmoothStep(0f, 1f, Mathf.Min(i, steps - i) / 6f))); // (darkening gradually into the cave)
                gc.Add(dark); gc.Add(dark);
                if (i == steps) break;
                for (int b = 0; b < 3; b++)
                {
                    int r0 = i * 4 + b, r1 = r0 + 4;
                    t.AddRange(new[] { r0, r1, r0 + 1, r0 + 1, r1, r1 + 1, r0, r0 + 1, r1, r0 + 1, r1 + 1, r1 });
                }
                int s0 = i * 2;
                gt.AddRange(new[] { s0, s0 + 2, s0 + 1, s0 + 1, s0 + 2, s0 + 3, s0, s0 + 1, s0 + 2, s0 + 1, s0 + 3, s0 + 2 });
            }
            view.Roof = Mesh("CaveRoof", ground != null ? ground : plain, RoofOrder, TrackMeshes.Quads("CaveRoof", v, u, t, c)).GetComponent<MeshRenderer>();
            view.RoofColors = c.ToArray();
            view.Gloom = Mesh("CaveGloom", plain, CaveGloomOrder, TrackMeshes.Quads("CaveGloom", gv, gu, gt, gc)).GetComponent<MeshRenderer>();
            // The inside (shown once you are in): solid rock walls either side of the water, out to where the roof
            // reaches - they hide the mountain's top and edges below - a lighter ledge of wet rock along the water.
            var wv = new List<Vector3>(); var wu = new List<Vector2>(); var wt = new List<int>(); var wc = new List<Color32>();
            var noise = new System.Random((int)(span.x * 5f));
            foreach (float side in new[] { 1f, -1f })
                for (int i = 0; i < steps; i++)
                {
                    float d0 = span.x + len * i / steps, d1 = span.x + len * (i + 1) / steps;
                    Vector2 p0 = path.PointAt(d0), p1 = path.PointAt(d1), n0 = path.NormalAt(d0) * side, n1 = path.NormalAt(d1) * side;
                    float e0 = track.OffRoadAt(d0) + 0.3f, e1 = track.OffRoadAt(d1) + 0.3f, o0 = track.OffRoadAt(d0) + roof + 2f, o1 = track.OffRoadAt(d1) + roof + 2f;
                    // (the ledge's width wanders smoothly along the wall)
                    float j0 = 0.25f + 0.25f * Mathf.Sin(d0 * 0.7f + side), j1 = 0.25f + 0.25f * Mathf.Sin(d1 * 0.7f + side);
                    // (fading out over the last few metres at each mouth: no hard edge where the cave ends)
                    byte f0 = (byte)(255f * Mathf.SmoothStep(0f, 1f, Mathf.Min(i, steps - i) / 5f)), f1 = (byte)(255f * Mathf.SmoothStep(0f, 1f, Mathf.Min(i + 1, steps - i - 1) / 5f));
                    // wet ledge
                    Quad4(wv, wu, wt, wc, p0 + n0 * e0, p1 + n1 * e1, p1 + n1 * (e1 + 0.8f + j1), p0 + n0 * (e0 + 0.8f + j0), new Color32(70, 84, 98, f0), new Color32(70, 84, 98, f1));
                    // rock
                    Quad4(wv, wu, wt, wc, p0 + n0 * (e0 + 0.8f + j0), p1 + n1 * (e1 + 0.8f + j1), p1 + n1 * o1, p0 + n0 * o0, new Color32(40, 44, 52, f0), new Color32(40, 44, 52, f1));
                }
            view.Walls = Mesh("CaveWalls", plain, CaveWallOrder, TrackMeshes.Quads("CaveWalls", wv, wu, wt, wc)).GetComponent<MeshRenderer>();
            view.WallColors = wc.ToArray();
            SetAlpha(view.Walls, view.WallColors, 0);
            // Boulders along the walls and stalagmites on the wet ledge (shown with the walls).
            if (scenery != null)
            {
                var bv = new List<Vector3>(); var bu = new List<Vector2>(); var bt = new List<int>(); var bc = new List<Color32>();
                var rnd = new System.Random((int)(span.x * 11f) + 5);
                foreach (float side in new[] { 1f, -1f })
                    for (float a = 1.5f; a < len - 1.5f; a += 2.6f)
                    {
                        float d = span.x + a + (float)rnd.NextDouble() * 1.2f;
                        bool near = rnd.Next(3) == 0;
                        float lat = track.OffRoadAt(d) + (near ? 1.2f : 2.2f + (float)rnd.NextDouble() * (roof - 1f));
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * lat;
                        float size = near ? 1f + (float)rnd.NextDouble() * 0.6f : 2f + (float)rnd.NextDouble() * 2.4f, ang = (float)rnd.NextDouble() * 6.28f;
                        Vector2 r = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * size * 0.5f;
                        int first = bv.Count;
                        TrackMeshes.AddQuad(bv, bu, bt, p, r, new Vector2(-r.y, r.x), Vector2.one);
                        var uv = LagoonSheet.Cell("Rock");
                        for (int k = first; k < bu.Count; k++) bu[k] = new Vector2(uv.x + bu[k].x * uv.width, uv.y + bu[k].y * uv.height);
                        var tint = near ? new Color32(110, 126, 142, 255) : new Color32(78, 84, 96, 255);
                        for (int k = 0; k < 4; k++) bc.Add(tint);
                    }
                view.WallRocks = Mesh("CaveWallRocks", scenery, CrystalOrder, TrackMeshes.Quads("CaveWallRocks", bv, bu, bt, bc)).GetComponent<MeshRenderer>();
                view.WallRockColors = bc.ToArray();
                SetAlpha(view.WallRocks, view.WallRockColors, 0);
            }
            // The mouths: where the mountain's edge overhangs the water - a ragged lip of rock across the roof's front
            // edge (dark under the overhang, lighter rock on top, blending into the roof), crags along it. It belongs to
            // the roof: it fades away with it once you are inside.
            var mv = new List<Vector3>(); var mu = new List<Vector2>(); var mt = new List<int>(); var mc = new List<Color32>();
            var crags = new List<(Vector2 p, float size)>();
            (float depth, Color32 col)[] lip =
            {
                (-1.4f, new Color32(4, 12, 22, 0)), (-0.15f, new Color32(4, 12, 22, 120)), (0f, new Color32(34, 36, 42, 255)),
                (1.1f, new Color32(76, 78, 86, 255)), (2.6f, (Color32)new Color(rim.r, rim.g, rim.b, 0f)),
            };
            foreach (float at in new[] { span.x, span.x + len })
            {
                Vector2 p = path.PointAt(at), n = path.NormalAt(at), f = path.TangentAt(at) * (at == span.x ? 1f : -1f);
                float half = track.OffRoadAt(at) + 1.6f + 0.6f;
                const int across = 16;
                int first = mv.Count;
                for (int r = 0; r < lip.Length; r++)
                    for (int k = 0; k <= across; k++)
                    {
                        float lat = Mathf.Lerp(-half, half, k / (float)across);
                        float rag = r is 1 or 2 ? 0f : 0.35f * Mathf.Sin(lat * 1.9f + at) + 0.2f * Mathf.Sin(lat * 4.3f + at * 2f);
                        mv.Add(p + n * lat + f * (lip[r].depth + rag + (r == 0 ? 0f : 0.25f * Mathf.Sin(lat * 0.8f + at))));
                        mu.Add(Vector2.zero);
                        var col = lip[r].col;
                        col.a = (byte)(col.a * Mathf.Clamp01((half - Mathf.Abs(lat)) / 1.2f));
                        mc.Add(col);
                    }
                for (int r = 0; r < lip.Length - 1; r++)
                    for (int k = 0; k < across; k++)
                    {
                        int q = first + r * (across + 1) + k, q2 = q + across + 1;
                        mt.AddRange(new[] { q, q2, q + 1, q + 1, q2, q2 + 1, q, q + 1, q2, q + 1, q2 + 1, q2 });
                    }
                for (int k = 0; k < 6; k++) crags.Add((p + n * Mathf.Lerp(-half + 1f, half - 1f, (k + 0.5f) / 6f) + f * (1.2f + (k % 2) * 0.5f), 1.5f + (k % 3) * 0.4f));
            }
            view.Mouths = Mesh("CaveMouths", plain, MouthOrder, TrackMeshes.Quads("CaveMouths", mv, mu, mt, mc)).GetComponent<MeshRenderer>();
            view.MouthColors = mc.ToArray();
            if (scenery != null)
            {
                var cv = new List<Vector3>(); var cu = new List<Vector2>(); var ct = new List<int>(); var cc = new List<Color32>();
                int z = 0;
                foreach (var (cp, size) in crags)
                {
                    float ang = (z++) * 2.4f;
                    Vector2 r = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * size * 0.5f;
                    int first = cv.Count;
                    TrackMeshes.AddQuad(cv, cu, ct, cp, r, new Vector2(-r.y, r.x), Vector2.one);
                    var uv = LagoonSheet.Cell("Rock");
                    for (int q = first; q < cu.Count; q++) cu[q] = new Vector2(uv.x + cu[q].x * uv.width, uv.y + cu[q].y * uv.height);
                    var tint = _wild.rain ? new Color32(160, 168, 182, 255) : new Color32(200, 200, 205, 255);
                    for (int q = 0; q < 4; q++) cc.Add(tint);
                }
                view.MouthRocks = Mesh("CaveMouthCrags", scenery, MouthOrder + 1, TrackMeshes.Quads("CaveMouthCrags", cv, cu, ct, cc)).GetComponent<MeshRenderer>();
                view.MouthRockColors = cc.ToArray();
            }
            // Pines, boulders and crags over the roof: a wooded mountain, not a meadow (the storm darkens it).
            var roofSheet = forest != null ? forest : scenery;
            if (roofSheet != null)
            {
                var rv = new List<Vector3>(); var ru = new List<Vector2>(); var rt = new List<int>(); var rc = new List<Color32>();
                var random = new System.Random((int)(span.x * 7f) + 1);
                var tint = _wild.rain ? new Color32(178, 199, 219, 255) : new Color32(255, 255, 255, 255);
                var items = new List<(Vector2 p, float size, float ang, Rect uv)>();
                for (float a = 2f; a < len - 2f; a += 1.8f)
                    for (int k = 0; k < 3; k++)
                    {
                        float d = span.x + a, lat = ((float)random.NextDouble() * 2f - 1f) * (track.OffRoadAt(d) + roof * 0.75f);
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * lat;
                        int pick = random.Next(10);
                        Rect uv;
                        float size;
                        if (forest != null)
                        {
                            string cell = pick < 4 ? "PineDark" : pick < 7 ? "Pine" : pick < 9 ? "Bush" : "Fern";
                            uv = ForestSheet.Cell(cell);
                            size = cell.StartsWith("Pine") ? 5f + (float)random.NextDouble() * 2.5f : 2.5f + (float)random.NextDouble() * 2f;
                        }
                        else
                        {
                            uv = LagoonSheet.Cell(pick < 3 ? "Bush" : "Rock");
                            size = 3f + (float)random.NextDouble() * 3f;
                        }
                        items.Add((p, size, (float)random.NextDouble() * 6.28f, uv));
                    }
                items.Sort((x, y) => y.p.y.CompareTo(x.p.y));
                foreach (var (p, size, ang, uv) in items)
                {
                    Vector2 r = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * size * 0.5f;
                    int first = rv.Count;
                    TrackMeshes.AddQuad(rv, ru, rt, p, r, new Vector2(-r.y, r.x), Vector2.one);
                    for (int z = first; z < ru.Count; z++) ru[z] = new Vector2(uv.x + ru[z].x * uv.width, uv.y + ru[z].y * uv.height);
                    for (int z = 0; z < 4; z++) rc.Add(tint);
                }
                view.RoofRocks = Mesh("CaveRoofRocks", roofSheet, RoofRockOrder, TrackMeshes.Quads("CaveRoofRocks", rv, ru, rt, rc)).GetComponent<MeshRenderer>();
                view.RoofRockColors = rc.ToArray();
            }
            // Crystals glowing on the walls inside (seen once the roof fades).
            if (Application.isPlaying)
            {
                if (_glow == null) _glow = Glow();
                var random = new System.Random((int)(span.x * 3f));
                for (float a = 3f; a < len - 3f; a += 4.5f)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        float d = span.x + a + (float)random.NextDouble() * 2f;
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + 0.9f);
                        var sr = Sprite("CaveCrystal", _glow, p, 2.4f + (float)random.NextDouble() * 1.4f, 0f, CrystalOrder);
                        sr.color = random.NextDouble() < 0.5 ? new Color(0.4f, 0.9f, 1f, 0f) : new Color(0.75f, 0.55f, 1f, 0f);
                        view.Crystals.Add((sr, (float)random.NextDouble() * 6f));
                    }
            }
            _caveViews.Add(view);
        }

        static void Quad4(List<Vector3> v, List<Vector2> u, List<int> t, List<Color32> c, Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color32 col) => Quad4(v, u, t, c, a, b, cc, d, col, col);

        // (col0 along the a-d edge, col1 along the b-c edge)
        static void Quad4(List<Vector3> v, List<Vector2> u, List<int> t, List<Color32> c, Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color32 col0, Color32 col1)
        {
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(d); v.Add(cc);
            for (int k = 0; k < 4; k++) u.Add(Vector2.zero);
            c.Add(col0); c.Add(col1); c.Add(col0); c.Add(col1);
            t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s, s + 1, s + 2, s + 1, s + 3, s + 2 });
        }

        // A quad whose inner edge (a, b) has one colour and outer edge (c, d) another.
        static void Band(List<Vector3> v, List<Vector2> u, List<int> t, List<Color32> c, Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color32 inner, Color32 outer)
        {
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(d); v.Add(cc);
            for (int k = 0; k < 4; k++) u.Add(Vector2.zero);
            c.Add(inner); c.Add(inner); c.Add(outer); c.Add(outer);
            t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s, s + 1, s + 2, s + 1, s + 3, s + 2 });
        }

        static void SetAlpha(MeshRenderer r, Color32[] baseColors, byte alpha)
        {
            if (r == null || baseColors == null) return;
            var cols = new Color32[baseColors.Length];
            for (int k = 0; k < cols.Length; k++) { cols[k] = baseColors[k]; cols[k].a = (byte)(baseColors[k].a * alpha / 255); }
            r.GetComponent<MeshFilter>().sharedMesh.colors32 = cols;
        }

        void UpdateCaves(float dt)
        {
            if (_caveViews.Count == 0 || _race == null || _race.Player == null) return;
            float pd = _race.Player.GetComponent<TrackSensor>().Current.Distance;
            foreach (var view in _caveViews)
            {
                float into = track.Path.DeltaDistance(view.Span.x - 2f, pd);
                bool inside = into >= 0f && into <= Mathf.Repeat(view.Span.y - view.Span.x, track.Path.Length) + 4f;
                view.Inside = Mathf.MoveTowards(view.Inside, inside ? 1f : 0f, dt * 3f);
                if (Mathf.Abs(view.Inside - view.Shown) > 0.01f)
                {
                    // Outside: the mountain over the channel. Inside: the roof gone, the cave's walls round you.
                    view.Shown = view.Inside;
                    byte roofAlpha = (byte)(255f * Mathf.Clamp01(1f - view.Inside * 1.4f));
                    SetAlpha(view.Roof, view.RoofColors, roofAlpha);
                    SetAlpha(view.RoofRocks, view.RoofRockColors, roofAlpha);
                    SetAlpha(view.Mouths, view.MouthColors, roofAlpha);
                    SetAlpha(view.MouthRocks, view.MouthRockColors, roofAlpha);
                    SetAlpha(view.Walls, view.WallColors, (byte)(255f * Mathf.Clamp01(view.Inside * 1.4f)));
                    SetAlpha(view.WallRocks, view.WallRockColors, (byte)(255f * Mathf.Clamp01(view.Inside * 1.4f)));
                }
                foreach (var (sr, phase) in view.Crystals)
                {
                    var col = sr.color;
                    col.a = view.Inside * (0.55f + 0.3f * Mathf.Sin(Time.time * 2f + phase));
                    sr.color = col;
                }
            }
        }

        // ------------------------------------------------------------------ pirate ships

        class ShipView
        {
            public float From, To, Length;
            public bool Galleon;
            public Vector2 Centre, Forward, Right;
            public float Scale;
            public SpriteRenderer Top;
            public float Inside;
            public bool Aboard;
        }
        readonly List<ShipView> _ships = new();
        readonly List<(SpriteRenderer sr, float born, Vector2 from)> _snores = new();
        float _nextSnore, _nextSteam;
        readonly Dictionary<CarMovement, float> _bowLaunched = new();

        // A ship lying along a straight stretch of the channel. A galleon: its insides (drawn under the boats) cut open
        // along a flooded aisle as wide as the channel, its top deck over them fading away while you are aboard. A
        // wreck: its deck is the way on - up the gangplank, along the deck, off the bow.
        void BuildShip(TrackPath path, Vector2 span, bool galleon)
        {
            float len = Mathf.Repeat(span.y - span.x, path.Length), mid = span.x + len * 0.5f;
            var view = new ShipView { From = span.x, To = span.x + len, Length = len, Galleon = galleon };
            view.Centre = path.PointAt(mid);
            view.Forward = (path.PointAt(span.x + len) - path.PointAt(span.x)).normalized;
            view.Right = new Vector2(view.Forward.y, -view.Forward.x);
            float angle = Heading(view.Forward);
            var inside = Find(galleon ? "GalleonInside" : "DeckShip");
            if (inside == null) return;
            view.Scale = len / inside.bounds.size.y;
            var sr = Sprite(galleon ? "GalleonInside" : "DeckShip", inside, view.Centre, len, angle, galleon ? ShipInsideOrder : DeckShipOrder);
            sr.transform.localScale = Vector3.one * view.Scale;
            if (galleon)
            {
                var top = Sprite("GalleonTop", Find("GalleonTop"), view.Centre, len, angle, RoofOrder);
                if (top.sprite != null) top.transform.localScale = Vector3.one * view.Scale;
                view.Top = top;
            }
            _ships.Add(view);
            if (!Application.isPlaying) return;
            // Its sides: walls along the aisle (or the deck's edges).
            foreach (float side in new[] { 1f, -1f })
            {
                var pts = new List<Vector2>();
                for (float a = 1.5f; a <= len - 1.5f; a += 1f)
                {
                    float d = span.x + a;
                    pts.Add(path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + 0.2f));
                }
                var go = new GameObject("ShipSide", typeof(EdgeCollider2D)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.GetComponent<EdgeCollider2D>().points = pts.ToArray();
                _parts.Add(go);
            }
        }

        // A point on a ship's picture (pixels, 256 x 768, stern at the bottom) in the world.
        Vector2 OnShip(ShipView v, float px, float py) => v.Centre + v.Right * ((px - 128f) / 12f * v.Scale) + v.Forward * ((py - 384f) / 12f * v.Scale);

        void UpdateShips(float dt)
        {
            if (_ships.Count == 0 || _race == null || _race.Player == null) return;
            var path = track.Path;
            var player = _race.Player.GetComponent<CarMovement>();
            float pd = _race.Player.GetComponent<TrackSensor>().Current.Distance;
            foreach (var view in _ships)
            {
                float into = path.DeltaDistance(view.From, pd);
                bool aboard = into >= -1f && into <= view.Length + 1f;
                if (aboard && !view.Aboard) Say(player, view.Galleon ? "ALL ABOARD!" : "ON DECK!", new Color(1f, 0.82f, 0.3f));
                if (!aboard && view.Aboard && into > view.Length) Say(player, "ARRR!", new Color(1f, 0.5f, 0.3f));
                view.Aboard = aboard;
                bool near = into >= -12f && into <= view.Length + 2f;
                view.Inside = Mathf.MoveTowards(view.Inside, near ? 1f : 0f, dt * 2.5f);
                if (view.Top != null) view.Top.color = new Color(1f, 1f, 1f, 1f - view.Inside);
                if (view.Galleon && view.Inside > 0.5f)
                {
                    // Snores rising from the hammocks, steam from the galley's pot.
                    if (Time.time >= _nextSnore)
                    {
                        _nextSnore = Time.time + 0.45f;
                        float px = Random.value < 0.5f ? 58f : 198f, py = 200f + Random.Range(0, 3) * 44f;
                        var z = Sprite("Snore", Find("Snore"), OnShip(view, px, py), 1.1f, Random.Range(-15f, 15f), SnoreOrder);
                        _snores.Add((z, Time.time, z.transform.position));
                    }
                    if (Time.time >= _nextSteam && _glow != null)
                    {
                        _nextSteam = Time.time + 0.35f;
                        Burst(OnShip(view, 40f, 574f) + Random.insideUnitCircle * 0.4f, new Color(1f, 1f, 1f, 0.7f), 2.4f);
                        Burst(OnShip(view, 40f, 536f), new Color(1f, 0.6f, 0.2f, 0.8f), 1.6f); // (the stove's fire)
                    }
                }
                if (view.Galleon) continue;
                // Off the bow of a wreck: launched into the air.
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    var sensor = car.GetComponent<TrackSensor>();
                    if (sensor == null) continue;
                    float at = path.DeltaDistance(view.From, sensor.Current.Distance);
                    if (at < view.Length - 4f || at > view.Length) continue;
                    if (_bowLaunched.TryGetValue(car, out float t) && Time.time - t < 2f) continue;
                    _bowLaunched[car] = Time.time;
                    car.Launch(1.05f);
                    car.GiveBoost(0.6f);
                    Say(car, "WALK THE PLANK!", new Color(1f, 0.82f, 0.3f));
                    if (IsPlayer(car)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.jump, 0.7f);
                }
            }
            for (int i = _snores.Count - 1; i >= 0; i--)
            {
                var (z, born, from) = _snores[i];
                float k = (Time.time - born) / 1.6f;
                if (z == null || k >= 1f) { if (z != null) Destroy(z.gameObject); _snores.RemoveAt(i); continue; }
                z.transform.position = from + new Vector2(Mathf.Sin(k * 6f) * 0.4f, k * 2.4f);
                z.color = new Color(1f, 1f, 1f, 1f - k * k);
            }
        }

        // ------------------------------------------------------------------ the kraken, sharks and parrots

        SpriteRenderer _krakenHead;
        readonly List<(SpriteRenderer ring, SpriteRenderer arm, Vector2 at, float slams, float angle)> _tentacles = new();
        float _nextTentacle;

        void BuildKraken()
        {
            if (_wild.krakenReach == Vector2.zero) return;
            _krakenHead = Sprite("KrakenHead", Find("KrakenHead"), _wild.kraken, 13f, 0f, PropOrder);
        }

        void UpdateKraken()
        {
            if (_krakenHead == null) return;
            _krakenHead.transform.localScale = Vector3.one * (13f / Mathf.Max(0.01f, _krakenHead.sprite != null ? _krakenHead.sprite.bounds.size.x : 1f)) * (1f + 0.04f * Mathf.Sin(Time.time * 1.7f));
            var path = track.Path;
            if (PlayerIn(new List<Vector2> { _wild.krakenReach }, out float pd) && Time.time >= _nextTentacle)
            {
                _nextTentacle = Time.time + Random.Range(1.6f, 2.6f);
                float d = pd + Random.Range(10f, 18f);
                float half = track.HalfWidthAt(d);
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * Random.Range(-half, half);
                var ring = Sprite("KrakenRing", Find("TargetRing"), at, 4f, 0f, RingOrder);
                ring.color = new Color(0.8f, 0.5f, 1f, 0.8f);
                Vector2 from = (at - _wild.kraken).normalized;
                var arm = Sprite("Tentacle", Find("Tentacle"), at, 7f, Heading(from), BallOrder);
                arm.color = new Color(1f, 1f, 1f, 0f);
                _tentacles.Add((ring, arm, at, Time.time + 1.1f, Heading(from)));
            }
            for (int i = _tentacles.Count - 1; i >= 0; i--)
            {
                var (ring, arm, at, slams, angle) = _tentacles[i];
                float t = Time.time - slams;
                if (ring != null) ring.color = new Color(0.8f, 0.5f, 1f, t < 0f ? 0.5f + 0.5f * Mathf.Sin(Time.time * 16f) : 0f);
                // Rising out of the water before the slam, down across the channel, lying there a moment, sinking.
                float rise = t < 0f ? Mathf.Clamp01((t + 0.6f) / 0.6f) : 1f;
                float sink = t > 0.9f ? 1f - Mathf.Clamp01((t - 0.9f) / 0.4f) : 1f;
                if (arm != null)
                {
                    arm.color = new Color(1f, 1f, 1f, rise * sink);
                    float tilt = t < 0f ? 40f * (1f - rise) : 0f;
                    arm.transform.rotation = Quaternion.Euler(0f, 0f, angle + tilt);
                    arm.transform.localScale = Vector3.one * (7f / Mathf.Max(0.01f, arm.sprite != null ? arm.sprite.bounds.size.y : 1f)) * (t < 0f ? 0.6f + 0.6f * rise : 1.2f);
                }
                if (t >= 0f && t < Time.deltaTime + 0.001f)
                {
                    Burst(at, new Color(0.9f, 0.97f, 1f), 5f);
                    if (_shake != null) _shake.Shake(0.25f);
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.splash, 0.6f);
                    foreach (var car in _cars)
                    {
                        if (!Free(car)) continue;
                        Vector2 o = car.Body.position - at;
                        if (o.sqrMagnitude > 2.8f * 2.8f) continue;
                        car.Bump(o.sqrMagnitude > 0.01f ? o.normalized : Vector2.up, 8f);
                        car.Slip(Random.value < 0.5f ? -1 : 1);
                        Say(car, "KRAKEN!", new Color(0.8f, 0.5f, 1f));
                    }
                }
                if (t < 1.3f) continue;
                if (ring != null) Destroy(ring.gameObject);
                if (arm != null) Destroy(arm.gameObject);
                _tentacles.RemoveAt(i);
            }
        }

        readonly List<(SpriteRenderer fin, Vector3 circle, float phase)> _fins = new();
        readonly Dictionary<CarMovement, float> _bitten = new();

        void BuildSharks()
        {
            foreach (var c in _wild.sharks)
                for (int k = 0; k < 2; k++)
                    _fins.Add((Sprite("SharkFin", Find("SharkFin"), (Vector2)c, 1.8f, 0f, CrocOrder), c, k * Mathf.PI));
        }

        void UpdateSharks()
        {
            foreach (var (fin, c, phase) in _fins)
            {
                if (fin == null) continue;
                float a = Time.time * 0.55f + phase;
                Vector2 at = new Vector2(c.x, c.y) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * c.z;
                fin.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 180f));
                foreach (var car in _cars)
                {
                    if (!Free(car) || car.IsSlipping) continue;
                    if ((car.Body.position - at).sqrMagnitude > 1.4f * 1.4f) continue;
                    if (_bitten.TryGetValue(car, out float t) && Time.time - t < 2f) continue;
                    _bitten[car] = Time.time;
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    Say(car, "CHOMP!", new Color(0.7f, 0.8f, 0.9f));
                }
            }
        }

        readonly List<(SpriteRenderer sr, float offset, float lateral, float speed)> _parrots = new();

        void BuildParrots(TrackPath path)
        {
            for (int i = 0; i < _wild.parrots; i++)
                _parrots.Add((Sprite("Parrot", Find("Parrot"), Vector2.zero, 1.8f, 0f, ParrotOrder), path.Length * i / Mathf.Max(1, _wild.parrots), (i % 2 == 0 ? 1f : -1f) * (6f + i % 3 * 3f), 7f + i % 4));
        }

        void UpdateParrots()
        {
            var path = track.Path;
            foreach (var (sr, offset, lateral, speed) in _parrots)
            {
                if (sr == null) continue;
                float d = offset + Time.time * speed;
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * (lateral + Mathf.Sin(Time.time * 0.8f + offset) * 3f);
                sr.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, Heading(path.TangentAt(d))));
                // Flapping: the wings narrow and widen.
                float flap = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 9f + offset));
                float u = 1.8f / Mathf.Max(0.01f, sr.sprite != null ? sr.sprite.bounds.size.x : 1f);
                sr.transform.localScale = new Vector3(u * flap, u, 1f);
            }
        }

        // ------------------------------------------------------------------ ledges

        void BuildLedge(TrackPath path, TrackLayout.Ledge ledge)
        {
            var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var rimC = new List<Color32>();
            var fv = new List<Vector3>(); var fu = new List<Vector2>(); var ft = new List<int>(); var faceC = new List<Color32>();
            float len = Mathf.Repeat(ledge.to - ledge.from, path.Length);
            bool hardFrom = false, hardTo = false;
            foreach (var gap in track.Layout.gaps)
                if (gap.drop)
                {
                    hardFrom |= Mathf.Abs(path.DeltaDistance(gap.to, ledge.from)) < 1f;
                    hardTo |= Mathf.Abs(path.DeltaDistance(ledge.to, gap.from)) < 1f;
                }
            Color rimTop = ledge.reef ? new Color(1f, 0.55f, 0.55f) : new Color(0.56f, 0.53f, 0.49f);
            // (on a reef: the coral crest along the channel, then pale shallows over the reef fading into what is there)
            Color faceTop = ledge.reef ? new Color(0.8f, 0.97f, 0.93f, 0.9f) : new Color(0.24f, 0.24f, 0.26f);
            Color faceBottom = ledge.reef ? new Color(0.8f, 0.97f, 0.93f, 0f) : new Color(0.08f, 0.12f, 0.18f);
            foreach (float side in new[] { 1f, -1f })
            {
                if (ledge.side != 0f && Mathf.Sign(ledge.side) != side) continue;
                for (float a = 0f; a < len; a += 1f)
                {
                    float d0 = ledge.from + a, d1 = ledge.from + Mathf.Min(len, a + 1f);
                    // (fading in and out at the ends, so the ledge never starts with a hard edge - except where it
                    // meets a leap: there the stack's end is a cliff too)
                    float Fk(float x) => Mathf.Clamp01(Mathf.Min(hardFrom ? 3f : x, hardTo ? 3f : len - x) / 3f);
                    float k0 = Fk(a), k1 = Fk(Mathf.Min(len, a + 1f));
                    Vector2 p0 = path.PointAt(d0), p1 = path.PointAt(d1), n0 = path.NormalAt(d0) * side, n1 = path.NormalAt(d1) * side;
                    float r0 = track.OffRoadAt(d0), r1 = track.OffRoadAt(d1);
                    Strip(v, u, t, rimC, p0 + n0 * r0, p1 + n1 * r1, p1 + n1 * (r1 + 0.9f), p0 + n0 * (r0 + 0.9f), Fade(rimTop, k0), Fade(rimTop, k1), Fade(rimTop, k1), Fade(rimTop, k0));
                    if (ledge.reef)
                        Strip(fv, fu, ft, faceC, p0 + n0 * (r0 + 0.9f), p1 + n1 * (r1 + 0.9f), p1 + n1 * (r1 + 7f), p0 + n0 * (r0 + 7f),
                            Fade(faceTop, k0), Fade(faceTop, k1), Fade(faceBottom, 0f), Fade(faceBottom, 0f));
                    else
                    {
                        // The rock face (solid, darkening as it drops), then its deep shadow on the sea far below.
                        Strip(fv, fu, ft, faceC, p0 + n0 * (r0 + 0.9f), p1 + n1 * (r1 + 0.9f), p1 + n1 * (r1 + 4f), p0 + n0 * (r0 + 4f),
                            Fade(faceTop, k0), Fade(faceTop, k1), Fade(new Color(0.1f, 0.11f, 0.13f), k1), Fade(new Color(0.1f, 0.11f, 0.13f), k0));
                        var deep = new Color(0.02f, 0.1f, 0.18f, 0.75f);
                        Strip(fv, fu, ft, faceC, p0 + n0 * (r0 + 4f), p1 + n1 * (r1 + 4f), p1 + n1 * (r1 + 12f), p0 + n0 * (r0 + 12f),
                            Fade(deep, k0), Fade(deep, k1), Fade(deep, 0f), Fade(deep, 0f));
                    }
                }
            }
            Mesh("LedgeFace", plain, FaceOrder, TrackMeshes.Quads("LedgeFace", fv, fu, ft, faceC));
            Mesh("LedgeRim", plain, RimOrder, TrackMeshes.Quads("LedgeRim", v, u, t, rimC));
            // Crags and boulders down the cliff faces (the storm darkens them).
            if (!ledge.reef && scenery != null)
            {
                var cv = new List<Vector3>(); var cu = new List<Vector2>(); var ct = new List<int>(); var cc = new List<Color32>();
                var random = new System.Random((int)(ledge.from * 17f) + 3);
                var tint = _wild.rain ? new Color32(120, 128, 140, 255) : new Color32(170, 170, 170, 255);
                for (float a = 2.5f; a < len - 2.5f; a += 3.4f)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        if (ledge.side != 0f && Mathf.Sign(ledge.side) != side) continue;
                        float d = ledge.from + a + (float)random.NextDouble() * 2f;
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + 2f + (float)random.NextDouble() * 1.8f);
                        float size = 2f + (float)random.NextDouble() * 1.6f, ang = (float)random.NextDouble() * 6.28f;
                        Vector2 r = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * size * 0.5f;
                        int first = cv.Count;
                        TrackMeshes.AddQuad(cv, cu, ct, p, r, new Vector2(-r.y, r.x), Vector2.one);
                        var uv = LagoonSheet.Cell("Rock");
                        for (int k = first; k < cu.Count; k++) cu[k] = new Vector2(uv.x + cu[k].x * uv.width, uv.y + cu[k].y * uv.height);
                        for (int k = 0; k < 4; k++) cc.Add(tint);
                    }
                Mesh("LedgeCrags", scenery, RimOrder + 1, TrackMeshes.Quads("LedgeCrags", cv, cu, ct, cc));
            }
            // Coral over the reef flats.
            if (ledge.reef && scenery != null)
            {
                var cv = new List<Vector3>(); var cu = new List<Vector2>(); var ct = new List<int>(); var cc = new List<Color32>();
                var random = new System.Random((int)(ledge.from * 13f));
                string[] cells = { "Coral", "CoralPink", "Seaweed" };
                for (float a = 2f; a < len - 2f; a += 1.3f)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        if (ledge.side != 0f && Mathf.Sign(ledge.side) != side) continue;
                        float d = ledge.from + a;
                        Vector2 p = path.PointAt(d) + path.NormalAt(d) * side * (track.OffRoadAt(d) + 1.6f + (float)random.NextDouble() * 3.6f);
                        float size = 2.4f + (float)random.NextDouble() * 1.6f, ang = (float)random.NextDouble() * 6.28f;
                        Vector2 r = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * size * 0.5f;
                        int first = cv.Count;
                        TrackMeshes.AddQuad(cv, cu, ct, p, r, new Vector2(-r.y, r.x), Vector2.one);
                        var uv = LagoonSheet.Cell(cells[random.Next(cells.Length)]);
                        for (int k = first; k < cu.Count; k++) cu[k] = new Vector2(uv.x + cu[k].x * uv.width, uv.y + cu[k].y * uv.height);
                        for (int k = 0; k < 4; k++) cc.Add(new Color32(255, 255, 255, 255));
                    }
                Mesh("ReefCoral", scenery, RimOrder + 1, TrackMeshes.Quads("ReefCoral", cv, cu, ct, cc));
            }
        }

        // Where a leap leaves one rock stack and lands on the next: the end of the stack is a cliff too (its face
        // dropping away beyond the channel's end, its shadow on the sea), the water spilling over the lip in foam.
        void BuildStackEnd(TrackPath path, float d, float forward)
        {
            var fv = new List<Vector3>(); var fu = new List<Vector2>(); var ft = new List<int>(); var fc = new List<Color32>();
            Vector2 p = path.PointAt(d), n = path.NormalAt(d), f = path.TangentAt(d) * forward;
            float r = track.OffRoadAt(d), wide = r + 4f;
            var faceTop = new Color(0.24f, 0.24f, 0.26f); var faceFoot = new Color(0.1f, 0.11f, 0.13f); var deep = new Color(0.02f, 0.1f, 0.18f, 0.75f);
            Vector2 L(float lat, float ahead) => p + n * lat + f * ahead;
            // A rounded end: from the lip (straight across) out to the foot of the face and the edge of its shadow,
            // meeting the ledges' faces either side exactly (no seams where they join).
            Vector2 E(float a, float lat, float ahead) => p + n * lat * Mathf.Sin(a) + f * ahead * Mathf.Cos(a);
            const int Steps = 18;
            for (int k = 0; k < Steps; k++)
            {
                float a0 = Mathf.PI * (k / (float)Steps - 0.5f), a1 = Mathf.PI * ((k + 1) / (float)Steps - 0.5f);
                Band(fv, fu, ft, fc, E(a0, r + 0.9f, 0f), E(a1, r + 0.9f, 0f), E(a1, wide, 3.2f), E(a0, wide, 3.2f), Fade(faceTop, 1f), Fade(faceFoot, 1f));
                Band(fv, fu, ft, fc, E(a0, wide, 3.2f), E(a1, wide, 3.2f), E(a1, r + 12f, 10f), E(a0, r + 12f, 10f), Fade(deep, 1f), Fade(deep, 0f));
            }
            Mesh("StackEndFace", plain, FaceOrder, TrackMeshes.Quads("StackEndFace", fv, fu, ft, fc));
            var rv = new List<Vector3>(); var ru = new List<Vector2>(); var rt = new List<int>(); var rc = new List<Color32>();
            var rim = new Color(0.56f, 0.53f, 0.49f);
            // the rock rim round the corners, and a lip of foam where the water spills over
            Strip(rv, ru, rt, rc, L(r, -0.2f), L(r + 0.9f, -0.2f), L(r + 0.9f, 0.6f), L(r, 0.6f), Fade(rim, 1f), Fade(rim, 1f), Fade(rim, 1f), Fade(rim, 1f));
            Strip(rv, ru, rt, rc, L(-r - 0.9f, -0.2f), L(-r, -0.2f), L(-r, 0.6f), L(-r - 0.9f, 0.6f), Fade(rim, 1f), Fade(rim, 1f), Fade(rim, 1f), Fade(rim, 1f));
            var lip = new Color(0.92f, 0.97f, 1f, 0.85f);
            if (forward > 0f) Strip(rv, ru, rt, rc, L(-r, -0.15f), L(r, -0.15f), L(r, 0.45f), L(-r, 0.45f), Fade(lip, 0.4f), Fade(lip, 0.4f), Fade(lip, 1f), Fade(lip, 1f));
            Mesh("StackEndRim", plain, RimOrder, TrackMeshes.Quads("StackEndRim", rv, ru, rt, rc));
        }

        static Color32 Fade(Color c, float k) => new Color(c.r, c.g, c.b, c.a * k);

        static void Strip(List<Vector3> v, List<Vector2> u, List<int> t, List<Color32> c, Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color32 ca, Color32 cb, Color32 ccol, Color32 cd)
        {
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(d); v.Add(cc);
            u.Add(Vector2.zero); u.Add(Vector2.right); u.Add(Vector2.up); u.Add(Vector2.one);
            c.Add(ca); c.Add(cb); c.Add(cd); c.Add(ccol);
            t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s, s + 1, s + 2, s + 1, s + 3, s + 2 });
        }

        readonly Dictionary<CarMovement, float> _fallen = new();

        void UpdateLedges()
        {
            if (_wild.ledges.Count == 0) return;
            var path = track.Path;
            foreach (var car in _cars)
            {
                if (!Free(car)) continue;
                var sensor = car.GetComponent<TrackSensor>();
                if (sensor == null || sensor.Branch >= 0) continue;
                if (_fallen.TryGetValue(car, out float at) && Time.time - at < 0.6f) continue;
                float d = sensor.Current.Distance, lat = sensor.Current.Lateral;
                foreach (var ledge in _wild.ledges)
                {
                    float into = path.DeltaDistance(ledge.from, d);
                    if (into < 0f || into > Mathf.Repeat(ledge.to - ledge.from, path.Length)) continue;
                    if (ledge.side != 0f && Mathf.Sign(lat) != Mathf.Sign(ledge.side)) continue;
                    if (Mathf.Abs(lat) < track.OffRoadAt(d) + 0.35f) continue;
                    _fallen[car] = Time.time;
                    var pose = track.RespawnBefore(d, 3f);
                    float top = car.Stats != null ? car.Stats.topSpeed : 18f;
                    if (!car.CarryBack(pose, top * 0.4f, fromWater: true, fall: !ledge.reef)) car.Respawn(pose);
                    if (ledge.reef) Say(car, "AGROUND!", new Color(1f, 0.6f, 0.35f));
                    else
                    {
                        Say(car, "OVER THE EDGE!", new Color(1f, 0.45f, 0.4f));
                        if (IsPlayer(car)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.splash, 0.7f);
                    }
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ props

        readonly List<(SpriteRenderer sr, float radius)> _overheads = new();

        void BuildProp(TrackLayout.Piece prop)
        {
            var sprite = Find(prop.sprite);
            if (sprite == null) return;
            var sr = Sprite(prop.sprite, sprite, prop.position, prop.size, prop.angle, prop.overhead ? OverheadOrder : PropOrder);
            if (!prop.overhead)
            {
                var shadow = Sprite(prop.sprite + "Shadow", sprite, prop.position + new Vector2(0.8f, -1f), prop.size, prop.angle, PropOrder - 1);
                shadow.color = new Color(0f, 0.08f, 0.12f, 0.3f);
            }
            else _overheads.Add((sr, prop.size * 0.45f));
        }

        void UpdateOverheads(float dt)
        {
            if (_overheads.Count == 0 || _race == null || _race.Player == null) return;
            Vector2 p = _race.Player.transform.position;
            var path = track.Path;
            foreach (var (sr, radius) in _overheads)
            {
                bool under = ((Vector2)sr.transform.position - p).sqrMagnitude < radius * radius;
                // (one over a cave's mouth - skull rock - is part of the mountain: gone while you are inside the cave,
                // or it would be drawn over the cave's walls behind you)
                float inCave = 0f;
                if (_caveViews.Count > 0)
                {
                    float d = path.Project(sr.transform.position).Distance;
                    foreach (var view in _caveViews)
                    {
                        float into = path.DeltaDistance(view.Span.x - radius, d);
                        if (into >= 0f && into <= Mathf.Repeat(view.Span.y - view.Span.x, path.Length) + radius * 2f) inCave = Mathf.Max(inCave, view.Inside);
                    }
                }
                var c = sr.color;
                c.a = Mathf.MoveTowards(c.a, (under ? 0.35f : 1f) * (1f - inCave), dt * 3f);
                sr.color = c;
            }
        }

        // ------------------------------------------------------------------ crocodiles

        readonly List<(SpriteRenderer croc, SpriteRenderer ripple, float d, float lat, float phase)> _crocs = new();
        const float CrocCycle = 6.5f;

        void BuildCroc(TrackPath path, Vector2 c)
        {
            var sprite = Find("Crocodile");
            var ring = Find("TargetRing");
            if (sprite == null) return;
            var croc = Sprite("Crocodile", sprite, path.PointAt(c.x), 3.2f, 0f, CrocOrder);
            var ripple = Sprite("CrocRipple", ring, path.PointAt(c.x), 2.6f, 0f, RingOrder);
            ripple.color = new Color(1f, 1f, 1f, 0f);
            _crocs.Add((croc, ripple, c.x, c.y, Mathf.Repeat(c.x * 0.37f, CrocCycle)));
        }

        void UpdateCrocs()
        {
            var path = track.Path;
            float now = Track.Clock;
            foreach (var (croc, ripple, d, lat, phase) in _crocs)
            {
                float u = Mathf.Repeat(now + phase, CrocCycle);
                // Drifting across the channel and back; bubbles for a second, then up for a while, then under again.
                float half = track.OffRoadAt(d) - 1f;
                float l = Mathf.Clamp(lat + Mathf.Sin((now + phase) * 0.6f) * half * 0.6f, -half, half);
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * l;
                Vector2 dir = path.NormalAt(d) * Mathf.Sign(Mathf.Cos((now + phase) * 0.6f));
                croc.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, Heading(dir)));
                ripple.transform.position = at;
                bool warn = u > 1.5f && u < 2.6f, up = u >= 2.6f && u < 5.4f;
                ripple.color = new Color(1f, 1f, 1f, warn ? 0.5f + 0.4f * Mathf.Sin(now * 14f) : 0f);
                ripple.transform.localScale = Vector3.one * (2.6f / Mathf.Max(0.01f, ripple.sprite != null ? ripple.sprite.bounds.size.x : 1f)) * (warn ? 0.8f + 0.2f * Mathf.Repeat(now * 2f, 1f) : 1f);
                float rise = up ? Mathf.Clamp01(Mathf.Min(u - 2.6f, 5.4f - u) / 0.35f) : 0f;
                croc.color = new Color(1f, 1f, 1f, rise);
                if (rise < 0.6f) continue;
                foreach (var car in _cars)
                {
                    if (!Free(car) || car.IsSlipping) continue;
                    if ((car.Body.position - at).sqrMagnitude > 1.9f * 1.9f) continue;
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    car.Bump((car.Body.position - at).normalized, 5f);
                    Say(car, "SNAP!", new Color(0.55f, 0.9f, 0.4f));
                    if (IsPlayer(car)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.crash, 0.6f);
                }
            }
        }

        // ------------------------------------------------------------------ stone doors

        class Door { public float D, Half; public Vector2 Centre, Across; public Rigidbody2D Left, Right; public Transform LeftT, RightT; public float Open; public int Index; }
        readonly List<Door> _doors = new();

        void BuildDoor(TrackPath path, float d, int index)
        {
            var sprite = Find("StoneDoor");
            var door = new Door { D = d, Centre = path.PointAt(d), Across = path.NormalAt(d), Half = track.OffRoadAt(d) + 0.4f, Index = index };
            float angle = Mathf.Atan2(door.Across.y, door.Across.x) * Mathf.Rad2Deg;
            foreach (float side in new[] { 1f, -1f })
            {
                var sr = Sprite("StoneDoor", sprite, door.Centre + door.Across * side * door.Half * 0.5f, door.Half, angle, DoorOrder);
                if (sprite != null) sr.transform.localScale = new Vector3(door.Half / sprite.bounds.size.x, 1.1f / sprite.bounds.size.y, 1f);
                Rigidbody2D body = null;
                if (Application.isPlaying)
                {
                    body = sr.gameObject.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    var box = sr.gameObject.AddComponent<BoxCollider2D>();
                    box.size = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
                }
                if (side > 0f) { door.Left = body; door.LeftT = sr.transform; } else { door.Right = body; door.RightT = sr.transform; }
            }
            _doors.Add(door);
        }

        float DoorOpenness(int index)
        {
            float period = _wild.doorPeriod, open = 0.55f * period, slide = 0.6f;
            float u = Mathf.Repeat(Track.Clock - index * period * 0.37f, period);
            if (u < slide) return u / slide;
            if (u < open) return 1f;
            if (u < open + slide) return 1f - (u - open) / slide;
            return 0f;
        }

        void UpdateDoors()
        {
            foreach (var door in _doors)
            {
                door.Open = DoorOpenness(door.Index);
                float e = Mathf.SmoothStep(0f, 1f, door.Open);
                // Shut, each leaf covers its half; open, it has slid back into the bank.
                foreach (float side in new[] { 1f, -1f })
                {
                    Vector2 at = door.Centre + door.Across * side * (door.Half * 0.5f + e * door.Half * 1.05f);
                    var body = side > 0f ? door.Left : door.Right;
                    if (body != null) body.MovePosition(at); else (side > 0f ? door.LeftT : door.RightT).position = at;
                }
            }
        }

        bool DoorShut(Vector2 p)
        {
            if (_doors.Count == 0) return false;
            var on = track.Path.Project(p);
            foreach (var door in _doors)
                if (door.Open < 0.6f && Mathf.Abs(track.Path.DeltaDistance(door.D, on.Distance)) < 1.5f) return true;
            return false;
        }

        // ------------------------------------------------------------------ cannon fire

        readonly List<(Transform ball, SpriteRenderer ring, Vector2 from, Vector2 to, float fired, float lands)> _balls = new();
        float _nextShot;

        readonly Dictionary<int, float> _nextShots = new();

        void UpdateCannon()
        {
            if (_race == null || _race.Player == null) return;
            var path = track.Path;
            float pd = _race.Player.GetComponent<TrackSensor>().Current.Distance;
            // Every ship in range fires at the water a little ahead of the player (the old single-ship fields too).
            var guns = new List<TrackLayout.Cannon>(_wild.cannons);
            if (_wild.cannonAims != Vector2.zero) guns.Add(new TrackLayout.Cannon { position = _wild.cannon, aims = _wild.cannonAims, every = _wild.cannonEvery });
            for (int g = 0; g < guns.Count; g++)
            {
                var gun = guns[g];
                bool inRange = path.DeltaDistance(gun.aims.x, pd) >= -15f && path.DeltaDistance(pd, gun.aims.y) >= 0f;
                _nextShots.TryGetValue(g, out float next);
                if (!inRange || Time.time < next) continue;
                _nextShots[g] = Time.time + gun.every * Random.Range(0.8f, 1.2f);
                float d = pd + Random.Range(8f, 20f);
                float half = track.HalfWidthAt(d);
                Vector2 to = path.PointAt(d) + path.NormalAt(d) * Random.Range(-half, half);
                var ring = Sprite("CannonTarget", Find("TargetRing"), to, 3.6f, 0f, RingOrder);
                var ball = Sprite("Cannonball", Find("Cannonball"), gun.position, 0.9f, 0f, BallOrder);
                _balls.Add((ball.transform, ring, gun.position, to, Time.time, Time.time + 1.15f));
                Burst(gun.position, new Color(1f, 0.85f, 0.6f), 4f); // (the muzzle flash)
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.thunder, 0.25f);
            }
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var (ball, ring, from, to, fired, lands) = _balls[i];
                float k = Mathf.InverseLerp(fired, lands, Time.time);
                if (ball != null)
                {
                    ball.position = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 3f;
                    ball.localScale = Vector3.one * (0.9f / Mathf.Max(0.01f, Find("Cannonball") != null ? Find("Cannonball").bounds.size.x : 1f)) * (1f + Mathf.Sin(k * Mathf.PI) * 1.2f);
                }
                if (ring != null) ring.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * Mathf.Sin(Time.time * 16f));
                if (k < 1f) continue;
                // Splash: anyone near is knocked aside and spun.
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    Vector2 o = car.Body.position - to;
                    if (o.sqrMagnitude > 2.6f * 2.6f) continue;
                    car.Bump(o.sqrMagnitude > 0.01f ? o.normalized : Vector2.up, 8f);
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    Say(car, "KABOOM!", new Color(1f, 0.5f, 0.3f));
                }
                Burst(to, new Color(0.9f, 0.97f, 1f), 4.5f);
                if (_race.Player != null && ((Vector2)_race.Player.transform.position - to).sqrMagnitude < 15f * 15f)
                {
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.splash, 0.6f);
                    if (_shake != null) _shake.Shake(0.15f);
                }
                if (ball != null) Destroy(ball.gameObject);
                if (ring != null) Destroy(ring.gameObject);
                _balls.RemoveAt(i);
            }
        }

        // A short burst of light or spray: a soft disc that grows and fades.
        readonly List<(SpriteRenderer sr, float born, float size)> _bursts = new();

        void Burst(Vector2 at, Color color, float size)
        {
            if (_glow == null) return;
            var sr = Sprite("Burst", _glow, at, size, 0f, BoltOrder - 1);
            sr.color = color;
            _bursts.Add((sr, Time.time, size));
        }

        void UpdateBursts()
        {
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                var (sr, born, size) = _bursts[i];
                float k = (Time.time - born) / 0.45f;
                if (sr == null || k >= 1f) { if (sr != null) Destroy(sr.gameObject); _bursts.RemoveAt(i); continue; }
                sr.transform.localScale = Vector3.one * size * (0.6f + 0.6f * k) / Mathf.Max(0.01f, _glow.bounds.size.x);
                var c = sr.color; c.a = 1f - k; sr.color = c;
            }
        }

        // ------------------------------------------------------------------ coins and plank ramps

        readonly List<(SpriteRenderer sr, Vector2 at, float hiddenUntil)> _coins = new();
        int _coinRun;
        float _coinAt;

        void BuildCoin(TrackPath path, Vector2 c)
        {
            Vector2 at = path.PointAt(c.x) + path.NormalAt(c.x) * c.y;
            _coins.Add((Sprite("Coin", Find("Coin"), at, 1.3f, 0f, CoinOrder), at, 0f));
        }

        void UpdateCoins()
        {
            for (int i = 0; i < _coins.Count; i++)
            {
                var (sr, at, hidden) = _coins[i];
                if (sr == null) continue;
                bool shown = Time.time >= hidden;
                sr.enabled = shown;
                if (!shown) continue;
                float spin = Mathf.Abs(Mathf.Cos(Time.time * 3f + at.x));
                var s = sr.transform.localScale; sr.transform.localScale = new Vector3(Mathf.Max(0.15f, spin) * Mathf.Abs(s.y), s.y, 1f);
                foreach (var car in _cars)
                {
                    if (car == null || car.IsPuppet || car.IsCarried) continue;
                    if ((car.Body.position - at).sqrMagnitude > 1.5f * 1.5f) continue;
                    _coins[i] = (sr, at, Time.time + 6f);
                    car.GiveBoost(0.2f);
                    if (IsPlayer(car))
                    {
                        _coinRun = Time.time - _coinAt < 2.5f ? _coinRun + 1 : 1;
                        _coinAt = Time.time;
                        SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.35f);
                        if (_coinRun == 5) { car.GiveBoost(0.7f); Say(car, "TREASURE!", new Color(1f, 0.85f, 0.25f)); }
                    }
                    break;
                }
            }
        }

        readonly List<(Vector2 at, Vector2 along, Vector2 across)> _planks = new();
        readonly Dictionary<CarMovement, float> _planked = new();

        void BuildPlank(TrackPath path, Vector2 p)
        {
            Vector2 at = path.PointAt(p.x) + path.NormalAt(p.x) * p.y, along = path.TangentAt(p.x);
            Sprite("PlankRamp", Find("PlankRamp"), at, 4.6f, Heading(along), RingOrder + 1);
            _planks.Add((at, along, new Vector2(-along.y, along.x)));
        }

        void UpdatePlanks()
        {
            foreach (var (at, along, across) in _planks)
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    if (_planked.TryGetValue(car, out float t) && Time.time - t < 1f) continue;
                    Vector2 o = car.Body.position - at;
                    if (Mathf.Abs(Vector2.Dot(o, along)) > 1.8f || Mathf.Abs(Vector2.Dot(o, across)) > 1.7f) continue;
                    if (Vector2.Dot(car.Body.linearVelocity, along) < 6f) continue;
                    _planked[car] = Time.time;
                    car.Launch(0.95f);
                    car.GiveBoost(0.45f);
                    Say(car, "PLANK JUMP!", new Color(1f, 0.75f, 0.35f));
                    if (IsPlayer(car)) SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.jump, 0.7f);
                }
        }

        // ------------------------------------------------------------------ jellyfish and manta rays

        readonly List<(SpriteRenderer sr, float d, float phase, float speed)> _jellies = new();
        readonly Dictionary<CarMovement, float> _stung = new();

        void BuildJellies(TrackPath path, Vector2 span)
        {
            float len = Mathf.Repeat(span.y - span.x, path.Length);
            int count = Mathf.Max(3, Mathf.RoundToInt(len / 6f));
            var random = new System.Random((int)(span.x * 7f));
            for (int i = 0; i < count; i++)
            {
                float d = span.x + len * (i + 0.5f) / count;
                _jellies.Add((Sprite("Jellyfish", Find("Jellyfish"), path.PointAt(d), 1.6f, 0f, CrocOrder), d, (float)random.NextDouble() * 6.28f, 0.35f + (float)random.NextDouble() * 0.4f));
            }
        }

        void UpdateJellies()
        {
            var path = track.Path;
            float now = Track.Clock;
            foreach (var (sr, d, phase, speed) in _jellies)
            {
                if (sr == null) continue;
                float half = track.OffRoadAt(d) - 0.5f;
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * (Mathf.Sin(now * speed + phase) * half);
                sr.transform.position = at;
                float pulse = 1f + 0.12f * Mathf.Sin(now * 4f + phase);
                sr.transform.localScale = Vector3.one * (1.6f / Mathf.Max(0.01f, sr.sprite != null ? sr.sprite.bounds.size.x : 1f)) * pulse;
                foreach (var car in _cars)
                {
                    if (!Free(car) || car.IsSlipping) continue;
                    if ((car.Body.position - at).sqrMagnitude > 1.2f * 1.2f) continue;
                    if (_stung.TryGetValue(car, out float t) && Time.time - t < 1.5f) continue;
                    _stung[car] = Time.time;
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    Say(car, "ZAP!", new Color(1f, 0.55f, 0.85f));
                }
            }
        }

        readonly List<(SpriteRenderer sr, SpriteRenderer shadow, float d, float offset)> _mantas = new();
        const float MantaCycle = 5.5f, MantaFlight = 1.6f;

        void BuildManta(TrackPath path, float d)
        {
            var sprite = Find("MantaRay");
            var sr = Sprite("MantaRay", sprite, path.PointAt(d), 4.2f, 0f, BallOrder);
            var shadow = Sprite("MantaShadow", sprite, path.PointAt(d), 4.2f, 0f, ShadowOrder);
            shadow.color = new Color(0f, 0.1f, 0.2f, 0.35f);
            _mantas.Add((sr, shadow, d, Mathf.Repeat(d * 0.21f, MantaCycle)));
        }

        void UpdateMantas()
        {
            var path = track.Path;
            float now = Track.Clock;
            foreach (var (sr, shadow, d, offset) in _mantas)
            {
                float u = Mathf.Repeat(now + offset, MantaCycle);
                bool flying = u < MantaFlight;
                sr.enabled = shadow.enabled = flying;
                if (!flying) continue;
                float k = u / MantaFlight, h = Mathf.Sin(k * Mathf.PI);
                float side = Mathf.Repeat(Mathf.Floor((now + offset) / MantaCycle), 2f) < 1f ? 1f : -1f;
                float reach = track.OffRoadAt(d) + 3f;
                Vector2 n = path.NormalAt(d) * side, at = path.PointAt(d) + n * Mathf.Lerp(reach, -reach, k);
                sr.transform.SetPositionAndRotation(at + new Vector2(-0.5f, 1.6f) * h, Quaternion.Euler(0f, 0f, Heading(-n)));
                sr.transform.localScale = Vector3.one * (4.2f / Mathf.Max(0.01f, sr.sprite != null ? sr.sprite.bounds.size.x : 1f)) * (1f + 0.4f * h);
                shadow.transform.SetPositionAndRotation(at, sr.transform.rotation);
                if (h > 0.45f) continue;
                // Low over the water (taking off, diving in): it knocks you aside.
                foreach (var car in _cars)
                {
                    if (!Free(car) || car.IsSlipping) continue;
                    if ((car.Body.position - at).sqrMagnitude > 2f * 2f) continue;
                    car.Slip(side > 0f ? 1 : -1);
                    Say(car, "SPLAT!", new Color(0.6f, 0.75f, 1f));
                }
            }
        }

        // ------------------------------------------------------------------ lightning and waves

        readonly List<(SpriteRenderer ring, Vector2 at, float strikes)> _strikes = new();
        float _nextStrike;

        bool PlayerIn(List<Vector2> spans, out float pd)
        {
            pd = 0f;
            if (_race == null || _race.Player == null || spans.Count == 0) return false;
            pd = _race.Player.GetComponent<TrackSensor>().Current.Distance;
            foreach (var s in spans)
            {
                float into = track.Path.DeltaDistance(s.x, pd);
                if (into >= -10f && into <= Mathf.Repeat(s.y - s.x, track.Path.Length)) return true;
            }
            return false;
        }

        void UpdateLightning()
        {
            var path = track.Path;
            if (PlayerIn(_wild.lightning, out float pd) && Time.time >= _nextStrike)
            {
                _nextStrike = Time.time + Random.Range(2.2f, 3.6f);
                float d = pd + Random.Range(10f, 20f);
                float half = track.HalfWidthAt(d);
                Vector2 at = path.PointAt(d) + path.NormalAt(d) * Random.Range(-half, half);
                var ring = Sprite("StrikeRing", Find("TargetRing"), at, 4.4f, 0f, RingOrder);
                ring.color = new Color(1f, 0.95f, 0.5f, 0.8f);
                _strikes.Add((ring, at, Time.time + 1.1f));
            }
            for (int i = _strikes.Count - 1; i >= 0; i--)
            {
                var (ring, at, strikes) = _strikes[i];
                if (ring != null) ring.color = new Color(1f, 0.95f, 0.5f, 0.5f + 0.5f * Mathf.Sin(Time.time * 18f));
                if (Time.time < strikes) continue;
                var bolt = Sprite("Bolt", Find("Bolt"), at + Vector2.up * 4f, 8f, 0f, BoltOrder);
                Destroy(bolt.gameObject, 0.22f);
                Burst(at, new Color(1f, 0.98f, 0.8f), 7f);
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    if ((car.Body.position - at).sqrMagnitude > 2.6f * 2.6f) continue;
                    car.Slip(Random.value < 0.5f ? -1 : 1);
                    Say(car, "ZAPPED!", new Color(1f, 0.95f, 0.45f));
                }
                if (_race.Player != null && ((Vector2)_race.Player.transform.position - at).sqrMagnitude < 25f * 25f)
                {
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.thunder, 0.55f);
                    if (_shake != null) _shake.Shake(0.2f);
                }
                if (ring != null) Destroy(ring.gameObject);
                _strikes.RemoveAt(i);
            }
        }

        // A wave: a band of foam rolling across the channel from one side, shoving everything in it the other way.
        readonly List<(MeshFilter mesh, float d, float side, float born)> _waves = new();
        float _nextWave;
        const float WaveWarn = 0.9f, WaveCross = 1.4f, WaveLength = 9f;

        const int WaveSteps = 14;

        void UpdateWaves(float dt)
        {
            var path = track.Path;
            if (PlayerIn(_wild.waves, out float pd) && Time.time >= _nextWave)
            {
                _nextWave = Time.time + Random.Range(3.2f, 4.8f);
                // A ribbon along the channel: (WaveSteps + 1) rows of 4 (the lead edge, the crest's front and back, the
                // tail of the swell behind it), three bands between them.
                var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
                for (int k = 0; k < (WaveSteps + 1) * 4; k++) { v.Add(Vector3.zero); u.Add(Vector2.zero); c.Add(new Color32(255, 255, 255, 0)); }
                for (int j = 0; j < WaveSteps; j++)
                    for (int b = 0; b < 3; b++)
                    {
                        int a0 = j * 4 + b, a1 = a0 + 1, b0 = a0 + 4, b1 = a1 + 4;
                        t.AddRange(new[] { a0, b0, a1, a1, b0, b1, a0, a1, b0, a1, b1, b0 });
                    }
                var go = Mesh("Wave", plain, BoltOrder - 2, TrackMeshes.Quads("Wave", v, u, t, c));
                _waves.Add((go.GetComponent<MeshFilter>(), pd + Random.Range(8f, 14f), Random.value < 0.5f ? 1f : -1f, Time.time));
            }
            for (int i = _waves.Count - 1; i >= 0; i--)
            {
                var (mf, d, side, born) = _waves[i];
                float age = Time.time - born;
                if (mf == null || age > WaveWarn + WaveCross) { if (mf != null) Destroy(mf.gameObject); _waves.RemoveAt(i); continue; }
                Vector2 centre = path.PointAt(d), along = path.TangentAt(d), n = path.NormalAt(d) * side;
                float reach = track.OffRoadAt(d) + 2.5f;
                // Warning: the crest gathers at the edge it comes from; then it rolls across.
                float k = age < WaveWarn ? 0f : (age - WaveWarn) / WaveCross;
                float pos = Mathf.Lerp(reach, -reach, k);
                byte alpha = (byte)(age < WaveWarn ? 120 + 100 * Mathf.Sin(age * 20f) : 230 * (1f - k * k));
                var mesh = mf.sharedMesh;
                int count = (WaveSteps + 1) * 4;
                var verts = new Vector3[count]; var cols = new Color32[count];
                // The crest follows the channel's curve, a little ragged, fading out at its two ends; behind it the
                // swell's paler water. It rolls towards -n.
                float[] lats = { -0.35f, 0f, 0.9f, 3f };
                var tints = new[] { new Color32(240, 250, 255, 0), new Color32(245, 252, 255, alpha), new Color32(225, 242, 250, (byte)(alpha * 0.8f)), new Color32(170, 215, 235, 0) };
                for (int j = 0; j <= WaveSteps; j++)
                {
                    float x = j / (float)WaveSteps, dd = d + (x - 0.5f) * WaveLength;
                    Vector2 p = path.PointAt(dd), nn = path.NormalAt(dd) * side;
                    float wobble = Mathf.Sin(dd * 1.7f + age * 5f) * 0.18f, ends = Mathf.Sqrt(Mathf.Sin(Mathf.PI * x));
                    for (int b = 0; b < 4; b++)
                    {
                        verts[j * 4 + b] = p + nn * (pos + lats[b] + wobble);
                        var col = tints[b]; col.a = (byte)(col.a * ends); cols[j * 4 + b] = col;
                    }
                }
                mesh.vertices = verts;
                mesh.colors32 = cols;
                mesh.RecalculateBounds();
                if (age < WaveWarn) continue;
                foreach (var car in _cars)
                {
                    if (!Free(car)) continue;
                    Vector2 o = car.Body.position - centre;
                    if (Mathf.Abs(Vector2.Dot(o, along)) > WaveLength * 0.5f) continue;
                    if (Mathf.Abs(Vector2.Dot(o, n) - pos) > 1.6f) continue;
                    car.Body.linearVelocity += -n * 22f * dt;
                    if (IsPlayer(car) && age - WaveWarn < 0.1f) Say(car, "WAVE!", new Color(0.6f, 0.85f, 1f));
                }
            }
        }

        // ------------------------------------------------------------------ rain and the storm's gloom

        MeshFilter _rain;
        MeshRenderer _storm;
        Vector3[] _rainVerts;
        Vector2[] _drops;
        Color32[] _rainColors;
        const int Drops = 140;
        static readonly Vector2 Wind = new(6f, -15f);

        void UpdateRain(float dt)
        {
            var cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize + 3f, w = cam.orthographicSize * cam.aspect + 3f;
            if (_rain == null)
            {
                var v = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var c = new List<Color32>();
                _drops = new Vector2[Drops];
                for (int i = 0; i < Drops; i++)
                {
                    _drops[i] = new Vector2(Random.Range(-w, w), Random.Range(-h, h));
                    for (int k = 0; k < 4; k++) { v.Add(Vector3.zero); u.Add(Vector2.zero); c.Add(new Color32(0, 0, 0, 0)); }
                    int s = i * 4; t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
                }
                var go = Mesh("Rain", plain, RainOrder, TrackMeshes.Quads("Rain", v, u, t, c), cam.transform);
                go.transform.localPosition = new Vector3(0f, 0f, 4f);
                _rain = go.GetComponent<MeshFilter>();
                _rainVerts = v.ToArray();
                _rainColors = c.ToArray();
                // A grey-blue gloom over the storm (not darkness: everything stays easy to see).
                var gv = new List<Vector3>(); var gu = new List<Vector2>(); var gt = new List<int>(); var gc = new List<Color32>();
                TrackMeshes.AddQuad(gv, gu, gt, Vector2.zero, Vector2.right * 400f, Vector2.up * 400f, Vector2.one);
                for (int k = 0; k < 4; k++) gc.Add(new Color32(25, 38, 62, 70));
                var gloom = Mesh("StormGloom", plain, StormOrder, TrackMeshes.Quads("StormGloom", gv, gu, gt, gc), cam.transform);
                gloom.transform.localPosition = new Vector3(0f, 0f, 5f);
                _storm = gloom.GetComponent<MeshRenderer>();
            }
            Vector2 dir = Wind.normalized, side = new Vector2(-dir.y, dir.x) * 0.035f, len = dir * 1.1f;
            // (no rain inside a cave)
            float sheltered = 0f;
            foreach (var view in _caveViews) sheltered = Mathf.Max(sheltered, view.Inside);
            byte drop = (byte)(120f * (1f - sheltered));
            for (int i = 0; i < Drops; i++)
            {
                var p = _drops[i] + Wind * dt * (0.8f + (i % 5) * 0.1f);
                if (p.y < -h) p = new Vector2(Random.Range(-w - 6f, w), h);
                if (p.x > w) p.x -= 2f * w;
                _drops[i] = p;
                int k = i * 4;
                _rainVerts[k] = p - side; _rainVerts[k + 1] = p + side; _rainVerts[k + 2] = p - side - len; _rainVerts[k + 3] = p + side - len;
                _rainColors[k] = _rainColors[k + 1] = new Color32(210, 228, 255, drop);
                _rainColors[k + 2] = _rainColors[k + 3] = new Color32(210, 228, 255, 0);
            }
            var mesh = _rain.sharedMesh;
            mesh.vertices = _rainVerts;
            mesh.colors32 = _rainColors;
            mesh.RecalculateBounds();
        }

        // ------------------------------------------------------------------

        void Update()
        {
            if (!Application.isPlaying || _wild == null || track == null || track.Layout == null || track.Layout.biome != TrackLayout.Biome.Lagoon) return;
            float dt = Time.deltaTime;
            if (!_wild.enabled) { UpdateLife(dt); return; }
            UpdateLedges();
            UpdateOverheads(dt);
            UpdateCaves(dt);
            UpdateCrocs();
            UpdateDoors();
            UpdateCannon();
            UpdateShips(dt);
            UpdateKraken();
            UpdateSharks();
            UpdateParrots();
            UpdateCoins();
            UpdatePlanks();
            UpdateJellies();
            UpdateMantas();
            UpdateLightning();
            UpdateWaves(dt);
            UpdateBursts();
            UpdateLife(dt);
            if (_wild.rain) UpdateRain(dt);
        }
    }
}
