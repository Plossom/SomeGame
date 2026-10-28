using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The frozen fjord that breaks up lap by lap (<see cref="TrackLayout.fjord"/>), and the other ways round that
    /// open on later laps. Lap 1: thick ice, an ice road straight across. Lap 2: the ice cracks (the cracks grow),
    /// panels of the ice road crumble behind the car that drives onto them and sink, the icebreaker sails to and
    /// fro and cuts a channel that freezes over again. Lap 3: the ice is gone - open water and drifting floes to hop
    /// across - and a route that was buried under snow opens. Driven by the race leader's lap, the same for
    /// everyone. Also the sky darkening into a storm, and the player's moments (THIN ICE!, SHIP DODGE!, ESCAPED!).
    /// </summary>
    public class FjordIce : TrackDerivedBehaviour
    {
        [SerializeField] Material plain, water, ice, shore, road, line, particleMaterial;
        [SerializeField] Sprite shipSprite, floeSprite, snowBankSprite;

        const int ShoreOrder = -58, WaterOrder = -57, IceOrder = -56, CrackOrder = -55, RoadOrder = -52, RoadEdgeOrder = -51,
                  PanelOrder = -50, HoleOrder = -49, CoverOrder = -47, FloeOrder = 2, ShipOrder = 18, SkyOrder = 70;
        const float ShipLength = 15f, ShipBeam = 6f;

        static Color Lin(float r, float g, float b, float a = 1f) { var c = new Color(r, g, b).linear; c.a = a; return c; }
        static readonly Color CrackColor = Lin(0.25f, 0.45f, 0.62f, 0.75f), HoleColor = Lin(0.07f, 0.2f, 0.28f), ShardColor = Lin(0.85f, 0.94f, 1f),
            CoverColor = Lin(0.96f, 0.98f, 1f), ChannelColor = Lin(0.08f, 0.24f, 0.33f), ChannelEdge = Lin(0.22f, 0.45f, 0.56f);

        readonly List<GameObject> _parts = new();
        class Faded { public Mesh Mesh; public Color[] Colors; public float Alpha = -1f; }
        Faded _iceSheet, _iceRoad, _iceRoadEdges, _cracks, _channel;
        float[] _crackBirth; // per vertex of the cracks mesh: when (0..1 of the cracking) it appears
        readonly List<(Faded lines, Faded hole)> _panels = new();
        readonly List<(int branch, Faded cover, List<Collider2D> walls, List<SpriteRenderer> banks)> _covers = new();
        readonly List<SpriteRenderer> _floes = new();
        Transform _ship;
        Rigidbody2D _shipBody;
        SpriteRenderer _sky;
        ParticleSystem _shards, _bow;
        int _shipDirection;
        float _shipHeading;
        int[] _channelRows;
        int _channelSteps;

        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.CameraRig.CameraShake _shake;
        readonly HashSet<int> _playerCracked = new();
        readonly int[] _panelSeen = new int[64];
        float _playerSide;
        float _nextEscapeBurst;
        bool _escaped;

        // ------------------------------------------------------------------ build

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _panels.Clear(); _covers.Clear(); _floes.Clear();
            _iceSheet = _iceRoad = _iceRoadEdges = _cracks = _channel = null;
            _ship = null; _shipBody = null; _sky = null; _shards = _bow = null;
            var f = layout.fjord;
            bool fjord = f != null && f.enabled;
            if (fjord) BuildFjord(path, layout, f);
            // Ways round that open on a later lap: buried under snow, walled off with snow banks until then.
            for (int i = 0; i < track.Branches.Count && i < layout.branches.Count; i++)
                if (layout.branches[i].openFromLap > 1) BuildCover(layout, i);
            if (Application.isPlaying)
            {
                track.ResetFjord();
                BuildSky();
            }
        }

        void BuildFjord(TrackPath path, TrackLayout layout, TrackLayout.Fjord f)
        {
            // Water underneath, a snowy shore round it, the ice sheet on top.
            Part("FjordShore", Ellipse(f, 2.2f, 0f, Color.white, uvScale: 1f / 2f, ring: true), shore, ShoreOrder);
            Part("FjordWater", Ellipse(f, 0f, 0f, Color.white, uvScale: 1f / 8f), water, WaterOrder);
            _iceSheet = Fade(Part("FjordIce", Ellipse(f, 0.4f, 0f, Color.white, uvScale: 1f / 10f), ice, IceOrder));

            // Cracks: jagged lines branching out across the ice, each piece appearing as the cracking spreads.
            var cv = new List<Vector3>(); var cc = new List<Color>(); var ct = new List<int>(); var birth = new List<float>();
            var rnd = new System.Random(7);
            for (int k = 0; k < 9; k++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                Vector2 p = f.center + Rot(f, new Vector2(Mathf.Cos(a) * f.radii.x * 0.3f, Mathf.Sin(a) * f.radii.y * 0.3f) * (float)rnd.NextDouble());
                float heading = (float)rnd.NextDouble() * 360f, start = (float)rnd.NextDouble() * 0.4f;
                Crack(f, p, heading, 0, start, rnd, cv, cc, ct, birth);
            }
            _cracks = Fade(Part("FjordCracks", MakeMesh("FjordCracks", cv, cc, ct), plain, CrackOrder));
            _crackBirth = birth.ToArray();
            UpdateCracks(0f, 1f); // none yet: they appear when the ice cracks

            // The ice road across: packed snow with white edges.
            if (f.iceRoad >= 0 && f.iceRoad < track.Branches.Count)
            {
                var b = track.Branches[f.iceRoad];
                float half = b.HalfWidth + layout.kerbWidth;
                var rv = new List<Vector3>(); var ruv = new List<Vector2>(); var rc = new List<Color>(); var rt = new List<int>();
                var ev = new List<Vector3>(); var ec = new List<Color>(); var et = new List<int>();
                float along = 0f;
                for (int i = 0; i < b.Points.Count; i++)
                {
                    Vector2 p = b.Points[i];
                    Vector2 dir = (b.Points[Mathf.Min(i + 1, b.Points.Count - 1)] - b.Points[Mathf.Max(i - 1, 0)]).normalized, n = new(-dir.y, dir.x);
                    if (i > 0) along += Vector2.Distance(b.Points[i - 1], p);
                    int k = rv.Count;
                    rv.Add(p + n * half); rv.Add(p - n * half);
                    ruv.Add(new Vector2(0f, along / 8f)); ruv.Add(new Vector2(1f, along / 8f));
                    rc.Add(Color.white); rc.Add(Color.white);
                    if (i > 0) rt.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 });
                    // Snow ploughed up along both sides.
                    foreach (float side in new[] { 1f, -1f })
                    {
                        int e = ev.Count;
                        ev.Add(p + n * side * (half - 0.3f)); ev.Add(p + n * side * (half + 1.6f));
                        ec.Add(CoverColor); ec.Add(new Color(CoverColor.r, CoverColor.g, CoverColor.b, 0f));
                        if (i > 0) et.AddRange(new[] { e - 4, e, e - 3, e - 3, e, e + 1 });
                    }
                }
                var roadMesh = MakeMesh("IceRoad", rv, rc, rt, ruv);
                _iceRoad = Fade(Part("IceRoad", roadMesh, layout.roadMaterial != null ? layout.roadMaterial : road, RoadOrder));
                _iceRoadEdges = Fade(Part("IceRoadEdges", MakeMesh("IceRoadEdges", ev, ec, et), plain, RoadEdgeOrder));

                // The crumbling panels: crack lines over them (once the fjord cracks), and the hole they leave.
                if (f.panels != null)
                    foreach (var span in f.panels)
                    {
                        var lv = new List<Vector3>(); var lc = new List<Color>(); var lt = new List<int>();
                        var hv = new List<Vector3>(); var hc = new List<Color>(); var ht = new List<int>();
                        Vector2 P(float a, float lat) { Vector2 t = b.TangentAt(a); return b.PointAt(a) + new Vector2(-t.y, t.x) * lat; }
                        float w = half + 0.3f;
                        var r2 = new System.Random((int)(span.x * 13f));
                        float am = (span.x + span.y) * 0.5f, ah = (span.y - span.x) * 0.5f;
                        Vector2 centre = P(am, (float)(r2.NextDouble() - 0.5) * 2f);
                        // A spider web of cracks: jagged spokes out to the panel's edges, rings joining them.
                        const int spokes = 9;
                        var rings = new Vector2[2, spokes];
                        for (int q = 0; q < spokes; q++)
                        {
                            float ang = (q + (float)r2.NextDouble() * 0.6f) * Mathf.PI * 2f / spokes;
                            Vector2 dir = new(Mathf.Cos(ang), Mathf.Sin(ang));
                            // Out to the panel's rectangle (along, across), in world space.
                            float ext = Mathf.Min(Mathf.Abs(ah / Mathf.Max(0.01f, Mathf.Abs(dir.x))), Mathf.Abs(w / Mathf.Max(0.01f, Mathf.Abs(dir.y))));
                            Vector2 prev = centre;
                            for (int seg = 1; seg <= 4; seg++)
                            {
                                float k = seg / 4f;
                                Vector2 local = dir * ext * k + new Vector2(-dir.y, dir.x) * ((float)r2.NextDouble() - 0.5f) * 0.9f * (seg < 4 ? 1f : 0f);
                                Vector2 pt = P(am + local.x, Mathf.Clamp(local.y, -w, w));
                                LineQuad(prev, pt, 0.11f - seg * 0.012f, CrackColor, lv, lc, lt);
                                if (seg == 1 || seg == 3) rings[seg == 1 ? 0 : 1, q] = pt;
                                prev = pt;
                            }
                        }
                        for (int ring = 0; ring < 2; ring++)
                            for (int q = 0; q < spokes; q++)
                                if (r2.NextDouble() < 0.75) LineQuad(rings[ring, q], rings[ring, (q + 1) % spokes], 0.07f, CrackColor, lv, lc, lt);
                        // The hole: dark water with a ragged edge and a white rim of broken ice, shards floating in it.
                        var rim = new List<Vector2>();
                        int around = 28;
                        for (int q = 0; q < around; q++)
                        {
                            float ang = q * Mathf.PI * 2f / around;
                            Vector2 dir = new(Mathf.Cos(ang), Mathf.Sin(ang));
                            float ext = Mathf.Min(Mathf.Abs(ah / Mathf.Max(0.01f, Mathf.Abs(dir.x))), Mathf.Abs(w / Mathf.Max(0.01f, Mathf.Abs(dir.y))));
                            ext *= 0.92f + (float)r2.NextDouble() * 0.16f;
                            Vector2 local = dir * ext;
                            rim.Add(P(am + local.x, local.y));
                        }
                        Vector2 hc0 = P(am, 0f);
                        for (int q = 0; q < around; q++)
                        {
                            Vector2 a0 = rim[q], a1 = rim[(q + 1) % around];
                            int k = hv.Count;
                            hv.Add(hc0); hv.Add(a0); hv.Add(a1);
                            hc.Add(HoleColor); hc.Add(HoleColor); hc.Add(HoleColor);
                            ht.AddRange(new[] { k, k + 1, k + 2 });
                            LineQuad(a0, a1, 0.28f, ShardColor, hv, hc, ht);
                        }
                        for (int q = 0; q < 12; q++)
                        {
                            Vector2 c = P(am + ((float)r2.NextDouble() * 2f - 1f) * ah * 0.8f, ((float)r2.NextDouble() * 2f - 1f) * w * 0.8f);
                            float sz = 0.3f + (float)r2.NextDouble() * 0.5f, ang = (float)r2.NextDouble() * 6f;
                            Vector2 ax = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sz, ay = new Vector2(-ax.y, ax.x) * 0.6f;
                            AddQuad(c - ax - ay, c + ax - ay, c + ax + ay, c - ax + ay, ShardColor, hv, hc, ht);
                        }
                        var lines = Fade(Part("PanelCracks", MakeMesh("PanelCracks", lv, lc, lt), plain, PanelOrder));
                        var hole = Fade(Part("PanelHole", MakeMesh("PanelHole", hv, hc, ht), plain, HoleOrder));
                        SetAlpha(lines, 0f); SetAlpha(hole, 0f);
                        _panels.Add((lines, hole));
                    }
            }

            if (!Application.isPlaying) return;

            // The icebreaker's channel: open water along its line with ragged edges and broken ice heaped along both
            // sides, freezing over again behind it (each row fades on its own).
            {
                var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>(); var rows = new List<int>();
                Vector2 a = f.shipFrom, ab = f.shipTo - f.shipFrom, n = new Vector2(-ab.y, ab.x).normalized;
                int steps = Mathf.Max(2, Mathf.CeilToInt(ab.magnitude));
                var rr = new System.Random(5);
                float[] edge = new float[2];
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 p = a + ab * (i / (float)steps);
                    // Gently ragged edges (smooth along the channel, not jagged from metre to metre).
                    float l = Track.ChannelHalfWidth * (0.95f + 0.08f * Mathf.Sin(i * 0.45f) + 0.05f * Mathf.Sin(i * 1.3f + 1f));
                    float r = Track.ChannelHalfWidth * (0.95f + 0.08f * Mathf.Sin(i * 0.38f + 2f) + 0.05f * Mathf.Sin(i * 1.1f));
                    int k = v.Count;
                    v.Add(p + n * l); v.Add(p); v.Add(p - n * r);
                    c.Add(ChannelEdge); c.Add(ChannelColor); c.Add(ChannelEdge);
                    rows.Add(i); rows.Add(i); rows.Add(i);
                    if (i > 0) t.AddRange(new[] { k - 3, k, k - 2, k - 2, k, k + 1, k - 2, k + 1, k - 1, k - 1, k + 1, k + 2 });
                    // Chunks of broken ice along both edges.
                    for (int side = -1; side <= 1; side += 2)
                    {
                        if (rr.NextDouble() < 0.35) continue;
                        Vector2 at = p + n * side * ((side > 0 ? l : r) + 0.2f + (float)rr.NextDouble() * 0.6f);
                        float sz = 0.3f + (float)rr.NextDouble() * 0.45f, ang = (float)rr.NextDouble() * 6f;
                        Vector2 ax = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sz, ay = new Vector2(-ax.y, ax.x) * 0.7f;
                        int before = v.Count;
                        AddQuad(at - ax - ay, at + ax - ay, at + ax + ay, at - ax + ay, ShardColor, v, c, t);
                        for (int q = before; q < v.Count; q++) rows.Add(i);
                    }
                }
                _channel = Fade(Part("ShipChannel", MakeMesh("ShipChannel", v, c, t), plain, HoleOrder));
                _channelRows = rows.ToArray();
                _channelSteps = steps;
                ApplyChannel(f);
            }

            // The drifting floes (out on the water once the ice has broken up).
            for (int i = 0; i < f.floes.Count; i++)
            {
                var sr = Sprite("Floe", floeSprite, f.floes[i].position, f.floes[i].radius * 2.2f, i * 47f, FloeOrder);
                sr.color = new Color(1f, 1f, 1f, 0f);
                _floes.Add(sr);
            }

            // The icebreaker: it shoves cars aside.
            if (shipSprite != null)
            {
                var sr = Sprite("Icebreaker", shipSprite, f.shipFrom, ShipBeam, 0f, ShipOrder);
                sr.transform.localScale = new Vector3(ShipBeam / shipSprite.bounds.size.x, ShipLength / shipSprite.bounds.size.y, 1f);
                _ship = sr.transform;
                _shipBody = sr.gameObject.AddComponent<Rigidbody2D>();
                _shipBody.bodyType = RigidbodyType2D.Kinematic;
                _shipBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                var box = sr.gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(shipSprite.bounds.size.x * 0.85f, shipSprite.bounds.size.y * 0.92f);
                Vector2 d = f.shipTo - f.shipFrom;
                _shipHeading = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
                _ship.rotation = Quaternion.Euler(0f, 0f, _shipHeading);
            }
            _shards = Particles("IceShards", ShardColor, 60);
            _bow = Particles("BowSpray", new Color(0.9f, 0.97f, 1f, 0.9f), 120);
        }

        // A crack: a jagged line that forks as it runs, each piece appearing a little later than the one before.
        void Crack(TrackLayout.Fjord f, Vector2 p, float heading, int depth, float birth, System.Random rnd,
            List<Vector3> v, List<Color> c, List<int> t, List<float> births)
        {
            int pieces = depth == 0 ? 7 : 4;
            for (int k = 0; k < pieces; k++)
            {
                heading += (float)(rnd.NextDouble() - 0.5) * 70f;
                float len = 2f + (float)rnd.NextDouble() * 3f;
                Vector2 q = p + new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad)) * len;
                if (!f.Contains(q)) return;
                int before = v.Count;
                LineQuad(p, q, depth == 0 ? 0.18f : 0.11f, CrackColor, v, c, t);
                for (int i = before; i < v.Count; i++) births.Add(Mathf.Min(1f, birth));
                birth += 0.06f;
                if (depth < 2 && rnd.NextDouble() < 0.35) Crack(f, q, heading + (rnd.NextDouble() < 0.5 ? 55f : -55f), depth + 1, birth, rnd, v, c, t, births);
                p = q;
            }
        }

        void BuildCover(TrackLayout layout, int index)
        {
            var b = track.Branches[index];
            var path = track.Path;
            float half = b.HalfWidth + layout.kerbWidth + 1f;
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (int i = 0; i < b.Points.Count; i++)
            {
                Vector2 p = b.Points[i];
                // Not over the main road where the branch leaves and rejoins it.
                var on = path.Project(p);
                float away = Mathf.Clamp01((Mathf.Abs(on.Lateral) - track.OffRoadAt(on.Distance)) / 3f);
                Vector2 dir = (b.Points[Mathf.Min(i + 1, b.Points.Count - 1)] - b.Points[Mathf.Max(i - 1, 0)]).normalized, n = new(-dir.y, dir.x);
                int k = v.Count;
                var col = new Color(CoverColor.r, CoverColor.g, CoverColor.b, away);
                v.Add(p + n * half); v.Add(p - n * half); c.Add(col); c.Add(col);
                if (i > 0) t.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 });
            }
            var cover = Fade(Part("SnowCover", MakeMesh("SnowCover", v, c, t), plain, CoverOrder));
            // Snow banks across both ends (solid until the route opens), where it is clear of the main road.
            var walls = new List<Collider2D>();
            var banks = new List<SpriteRenderer>();
            foreach (bool start in new[] { true, false })
            {
                float at = -1f;
                for (float a = 0f; a <= b.Length; a += 0.5f)
                {
                    float along = start ? a : b.Length - a;
                    var on = path.Project(b.PointAt(along));
                    if (Mathf.Abs(on.Lateral) > track.OffRoadAt(on.Distance) + b.HalfWidth + layout.kerbWidth + 1.5f) { at = along; break; }
                }
                if (at < 0f) continue;
                Vector2 tan = b.TangentAt(at), n = new(-tan.y, tan.x), centre = b.PointAt(at);
                float w = b.HalfWidth + layout.kerbWidth + 0.8f;
                var go = new GameObject("SnowBankWall") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                _parts.Add(go);
                if (Application.isPlaying)
                {
                    var edge = go.AddComponent<EdgeCollider2D>();
                    edge.points = new[] { centre - n * w, centre + n * w };
                    edge.edgeRadius = 0.8f;
                    walls.Add(edge);
                }
                for (float s = -w; s <= w + 0.01f; s += 1.8f)
                    banks.Add(Sprite("SnowBank", snowBankSprite, centre + n * s + tan * Mathf.Sin(s * 2.1f) * 0.4f, 3.2f, s * 61f, 6));
            }
            _covers.Add((index, cover, walls, banks));
        }

        void BuildSky()
        {
            var go = new GameObject("FjordSky") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _parts.Add(go);
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            sr.sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            sr.sortingOrder = SkyOrder;
            sr.color = new Color(0f, 0f, 0f, 0f);
            _sky = sr;
        }

        // ------------------------------------------------------------------ Play mode

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            if (_race != null) _race.PlayerLapCompleted += OnPlayerLap;
            track.ResetFjord();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_race != null) _race.PlayerLapCompleted -= OnPlayerLap;
            if (Application.isPlaying) SomeGame.Track.WinterFx.Storm = 0f;
            Track.PhaseLap = 1;
            Track.CrackStart = Track.BreakStart = float.PositiveInfinity;
        }

        void OnPlayerLap(float time, bool best)
        {
            var player = _race != null ? _race.Player : null;
            if (player == null || _escaped || !player.IsFinished) return;
            _escaped = true;
            if (track.FjordBroken && _combo != null) _combo.Banner("ESCAPED!", new Color(0.55f, 0.9f, 1f));
        }

        void Update()
        {
            if (!Application.isPlaying || track == null || track.Layout == null) return;
            var layout = track.Layout;
            var f = layout.fjord;
            // The phase follows the race leader's lap.
            int lap = 1;
            if (_race != null && _race.Standings.Count > 0 && _race.Standings[0] != null) lap = _race.Standings[0].CurrentLap;
            if (lap > Track.PhaseLap) EnterLap(lap);

            float clock = Track.Clock;
            bool on = f != null && f.enabled;
            float cracking = on && !float.IsInfinity(Track.CrackStart) ? Mathf.Clamp01((clock - Track.CrackStart) / 18f) : 0f;
            float broken = on && !float.IsInfinity(Track.BreakStart) ? Mathf.Clamp01((clock - Track.BreakStart) / 1.5f) : 0f;
            // Just before it breaks the whole sheet shudders: the cracks flash.
            float warn = on && !float.IsInfinity(Track.BreakStart) && clock < Track.BreakStart ? 0.5f + 0.5f * Mathf.Sin(clock * 30f) : 0f;
            if (on)
            {
                SetAlpha(_iceSheet, 1f - broken);
                SetAlpha(_iceRoad, 1f - broken);
                SetAlpha(_iceRoadEdges, 1f - broken);
                UpdateCracks(Mathf.Max(cracking, warn > 0f ? 1f : 0f), (1f - broken) * (warn > 0f ? 0.6f + warn * 0.4f : 1f));
                for (int i = 0; i < _panels.Count; i++)
                {
                    int state = track.PanelState(i);
                    float age = track.PanelAge(i);
                    SetAlpha(_panels[i].lines, cracking > 0f ? (1f - broken) * (state == 1 ? 0.6f + 0.4f * Mathf.Sin(age * 40f) : 0.85f) : 0f);
                    float hole = state == 2 ? Mathf.Clamp01((age - 0.8f) / 0.25f) * Mathf.Clamp01((4.3f - age) / 0.8f) : 0f;
                    SetAlpha(_panels[i].hole, hole * (1f - broken));
                    if (state == 2 && _panelSeen[i] != 2) OnPanelSank(i);
                    _panelSeen[i] = state;
                }
                ApplyChannel(f);
                for (int i = 0; i < _floes.Count; i++)
                {
                    _floes[i].transform.position = track.FloePosition(i);
                    _floes[i].transform.rotation = Quaternion.Euler(0f, 0f, i * 47f + clock * (i % 2 == 0 ? 4f : -4f));
                    _floes[i].color = new Color(1f, 1f, 1f, broken);
                }
                UpdateShip(f, cracking > 0f || !float.IsInfinity(Track.CrackStart) && clock >= Track.CrackStart);
                if (broken > 0f && broken < 1f && _shards != null && Random.value < 0.6f)
                {
                    Vector2 p = f.center + Rot(f, new Vector2(Random.Range(-f.radii.x, f.radii.x), Random.Range(-f.radii.y, f.radii.y)) * 0.8f);
                    if (f.Contains(p)) { _shards.transform.position = p; _shards.Emit(6); }
                }
                Escape(f, broken);
            }
            // Covers over the ways round that open on a later lap.
            foreach (var cover in _covers)
            {
                bool open = track.BranchOpen(cover.branch);
                float a = Mathf.MoveTowards(cover.cover.Alpha < 0f ? 1f : cover.cover.Alpha, open ? 0f : 1f, Time.deltaTime / 1.2f);
                SetAlpha(cover.cover, a);
                foreach (var w in cover.walls) if (w != null) w.enabled = !open;
                foreach (var bank in cover.banks) if (bank != null) bank.enabled = !open || a > 0.6f;
            }
            Sky(lap, broken);
        }

        void EnterLap(int lap)
        {
            Track.PhaseLap = lap;
            var f = track.Layout.fjord;
            bool changed = false;
            if (f != null && f.enabled)
            {
                if (lap >= f.crackLap && float.IsInfinity(Track.CrackStart))
                {
                    Track.CrackStart = Track.Clock + 1.5f;
                    if (_combo != null) _combo.Banner("THE ICE IS CRACKING!", new Color(0.6f, 0.85f, 1f));
                    Rumble(0.35f);
                }
                if (lap >= f.breakLap && float.IsInfinity(Track.BreakStart))
                {
                    Track.BreakStart = Track.Clock + 2.5f;
                    changed = true;
                    Rumble(0.5f);
                }
            }
            for (int i = 0; i < track.Layout.branches.Count; i++)
                if (track.Layout.branches[i].openFromLap == lap)
                {
                    changed = true;
                    foreach (var cover in _covers)
                        if (cover.branch == i && _shards != null)
                            foreach (var bank in cover.banks) if (bank != null) { _shards.transform.position = bank.transform.position; _shards.Emit(10); }
                }
            if (changed && _combo != null) _combo.Banner("ROUTE CHANGED!", new Color(1f, 0.75f, 0.3f));
        }

        void Rumble(float amount)
        {
            if (_shake != null) _shake.Shake(amount);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 0.9f);
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || track == null || track.Layout == null || track.Layout.fjord == null || !track.Layout.fjord.enabled) return;
            if (_shipBody != null) _shipBody.MovePosition(track.ShipPosition);
            if (!track.FjordCracked || track.FjordBroken) return;
            // Panels crack under the first car onto them.
            var player = _race != null ? _race.Player : null;
            foreach (var progress in _race != null ? _race.Standings : (IReadOnlyList<SomeGame.Race.RaceProgress>)System.Array.Empty<SomeGame.Race.RaceProgress>())
            {
                if (progress == null) continue;
                var car = progress.GetComponent<SomeGame.Car.CarMovement>();
                if (car == null || car.IsAirborne) continue;
                int panel = track.PanelAt(car.Body.position);
                if (panel < 0 || !track.CrackPanel(panel)) continue;
                bool mine = progress == player;
                if (mine) _playerCracked.Add(panel);
                if (player != null && Vector2.Distance(player.transform.position, car.Body.position) < 30f)
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, mine ? 0.8f : 0.45f);
            }
            // Over the icebreaker's line just before it comes through: a dodge.
            if (player != null && _combo != null)
            {
                var f = track.Layout.fjord;
                Vector2 p = player.transform.position, ab = f.shipTo - f.shipFrom;
                float side = Mathf.Sign(ab.x * (p.y - f.shipFrom.y) - ab.y * (p.x - f.shipFrom.x));
                if (_playerSide != 0f && side != _playerSide && f.Contains(p))
                {
                    float u = Mathf.Clamp01(Vector2.Dot(p - f.shipFrom, ab) / ab.sqrMagnitude);
                    float shipU = track.ShipAlong(out int dir);
                    float gap = (u - shipU) * ab.magnitude * dir; // metres the ship still has to go to reach you
                    if (dir != 0 && gap > 0f && gap < 16f) _combo.Announce("SHIP DODGE!", new Color(1f, 0.45f, 0.35f));
                }
                _playerSide = side;
            }
        }

        // A panel the player cracked sank behind them: they made it.
        void OnPanelSank(int i)
        {
            if (!_playerCracked.Remove(i) || _combo == null || _race == null || _race.Player == null) return;
            var car = _race.Player.GetComponent<SomeGame.Car.CarMovement>();
            if (car != null && !car.IsCarried && track.PanelAt(car.Body.position) != i) _combo.Announce("THIN ICE!", new Color(0.6f, 0.9f, 1f));
        }

        void UpdateCracks(float growth, float alpha)
        {
            if (_cracks == null || _crackBirth == null) return;
            var colors = new Color[_cracks.Colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var c = _cracks.Colors[i];
                colors[i] = new Color(c.r, c.g, c.b, _crackBirth[i] <= growth ? c.a * alpha : 0f);
            }
            _cracks.Mesh.colors = colors;
        }

        void ApplyChannel(TrackLayout.Fjord f)
        {
            if (_channel == null || _channelRows == null) return;
            bool live = track.FjordCracked && !track.FjordBroken;
            var rowAlpha = new float[_channelSteps + 1];
            for (int r = 0; r <= _channelSteps; r++)
            {
                float since = live ? track.SinceShipPassed(r / (float)_channelSteps) : float.PositiveInfinity;
                rowAlpha[r] = since < f.refreeze ? Mathf.Sqrt(1f - since / f.refreeze) : 0f;
            }
            var colors = new Color[_channel.Colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var c = _channel.Colors[i];
                colors[i] = new Color(c.r, c.g, c.b, c.a * rowAlpha[_channelRows[i]]);
            }
            _channel.Mesh.colors = colors;
        }

        void UpdateShip(TrackLayout.Fjord f, bool sailing)
        {
            if (_ship == null) return;
            float u = track.ShipAlong(out int dir);
            if (sailing && dir != 0 && dir != _shipDirection)
            {
                // Off again: the horn (louder the closer you are).
                var player = _race != null ? _race.Player : null;
                float d = player != null ? Vector2.Distance(player.transform.position, _ship.position) : 100f;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.shipHorn, Mathf.Lerp(0.9f, 0.25f, Mathf.InverseLerp(20f, 120f, d)));
            }
            _shipDirection = dir;
            if (dir != 0)
            {
                Vector2 d = (dir > 0 ? f.shipTo - f.shipFrom : f.shipFrom - f.shipTo);
                float goal = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
                _shipHeading = Mathf.MoveTowardsAngle(_shipHeading, goal, 90f * Time.deltaTime);
                if (_bow != null && !track.FjordBroken && Random.value < 0.7f)
                {
                    _bow.transform.position = (Vector2)_ship.position + d.normalized * ShipLength * 0.45f;
                    _bow.Emit(3);
                }
            }
            if (_shipBody != null) _shipBody.MoveRotation(_shipHeading);
            else _ship.rotation = Quaternion.Euler(0f, 0f, _shipHeading);
        }

        // On the last lap the ice crumbles into the water along the shore behind the player: get out!
        void Escape(TrackLayout.Fjord f, float broken)
        {
            if (broken < 1f || _race == null || _race.Player == null || _shards == null || Time.time < _nextEscapeBurst) return;
            var player = _race.Player;
            if (player.IsFinished || player.CurrentLap < player.TotalLaps) return;
            _nextEscapeBurst = Time.time + 0.18f;
            var car = player.GetComponent<SomeGame.Car.CarMovement>();
            Vector2 p = player.transform.position, back = car != null ? -(Vector2)car.transform.up : Vector2.down;
            // The point of the fjord's edge nearest to a spot just behind the car.
            Vector2 behind = p + back * 10f;
            Vector2 local = Quaternion.Euler(0f, 0f, -f.angle) * (behind - f.center);
            float a = Mathf.Atan2(local.y / f.radii.y, local.x / f.radii.x);
            Vector2 rim = f.center + Rot(f, new Vector2(Mathf.Cos(a) * f.radii.x, Mathf.Sin(a) * f.radii.y));
            if (Vector2.Distance(rim, p) > 26f) return;
            _shards.transform.position = rim + Random.insideUnitCircle * 2f;
            _shards.Emit(8);
        }

        // The sky over the race: clear on lap 1, a cold dusk once it cracks, a dark storm when it breaks up.
        void Sky(int lap, float broken)
        {
            if (_sky == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            var f = track.Layout.fjord;
            bool on = f != null && f.enabled;
            Color goal = new(0f, 0f, 0f, 0f);
            if (on && !float.IsInfinity(Track.CrackStart)) goal = new Color(0.32f, 0.3f, 0.55f, 0.14f);
            if (on && broken > 0f) goal = Color.Lerp(goal, new Color(0.1f, 0.13f, 0.3f, 0.3f), broken);
            _sky.color = Color.Lerp(_sky.color, goal, Time.deltaTime * 0.8f);
            SomeGame.Track.WinterFx.Storm = on ? Mathf.Clamp01(broken + (!float.IsInfinity(Track.CrackStart) ? 0.3f : 0f)) : 0f;
            float h = cam.orthographicSize * 2.4f, w = h * cam.aspect;
            _sky.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            _sky.transform.localScale = new Vector3(w, h, 1f);
        }

        // ------------------------------------------------------------------ helpers

        static Vector2 Rot(TrackLayout.Fjord f, Vector2 v) => Quaternion.Euler(0f, 0f, f.angle) * v;
        static Vector2 Random2(System.Random r) => new((float)r.NextDouble() * 2f - 1f, (float)r.NextDouble() * 2f - 1f);

        Mesh Ellipse(TrackLayout.Fjord f, float grow, float inner, Color color, float uvScale, bool ring = false)
        {
            const int segments = 96;
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>(); var uv = new List<Vector2>();
            Vector2 Edge(float a, float g) => f.center + Rot(f, new Vector2(Mathf.Cos(a) * (f.radii.x + g), Mathf.Sin(a) * (f.radii.y + g)));
            if (!ring) { v.Add(f.center); c.Add(color); uv.Add(f.center * uvScale); }
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                if (ring)
                {
                    // Shore: u runs from the water (0) out into the snow.
                    int k = v.Count;
                    Vector2 p0 = Edge(a0, -0.2f), p1 = Edge(a1, -0.2f), q0 = Edge(a0, grow), q1 = Edge(a1, grow);
                    v.Add(p0); v.Add(p1); v.Add(q1); v.Add(q0);
                    uv.Add(new Vector2(0f, i)); uv.Add(new Vector2(0f, i + 1)); uv.Add(new Vector2(0.96f, i + 1)); uv.Add(new Vector2(0.96f, i));
                    for (int q = 0; q < 4; q++) c.Add(color);
                    t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
                else
                {
                    int k = v.Count;
                    Vector2 p0 = Edge(a0, grow), p1 = Edge(a1, grow);
                    v.Add(p0); v.Add(p1); c.Add(color); c.Add(color); uv.Add(p0 * uvScale); uv.Add(p1 * uvScale);
                    t.AddRange(new[] { 0, k, k + 1 });
                }
            }
            return MakeMesh("Fjord", v, c, t, uv);
        }

        static void LineQuad(Vector2 a, Vector2 b, float width, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x) * width;
            AddQuad(a - n - d * width * 0.5f, b - n + d * width * 0.5f, b + n + d * width * 0.5f, a + n - d * width * 0.5f, color, v, c, t);
        }

        static void AddQuad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) c.Add(color);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        static Mesh MakeMesh(string name, List<Vector3> v, List<Color> c, List<int> t, List<Vector2> uv = null)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetColors(c);
            if (uv != null) mesh.SetUVs(0, uv);
            var both = new List<int>(t);
            for (int i = 0; i < t.Count; i += 3) both.AddRange(new[] { t[i], t[i + 2], t[i + 1] });
            mesh.SetTriangles(both, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        Mesh Part(string name, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material != null ? material : plain;
            r.sortingOrder = order;
            _parts.Add(go);
            return mesh;
        }

        static Faded Fade(Mesh mesh) => new() { Mesh = mesh, Colors = mesh.colors };

        static void SetAlpha(Faded f, float alpha)
        {
            if (f == null || Mathf.Approximately(f.Alpha, alpha)) return;
            f.Alpha = alpha;
            var colors = new Color[f.Colors.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = new Color(f.Colors[i].r, f.Colors[i].g, f.Colors[i].b, f.Colors[i].a * alpha);
            f.Mesh.colors = colors;
        }

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

        ParticleSystem Particles(string name, Color color, int max)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _parts.Add(go);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max * 4;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.5f;
            limit.dampen = 0.12f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = ShipOrder + 1;
            system.Play();
            return system;
        }
    }
}
