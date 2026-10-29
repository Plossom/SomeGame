using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.Track
{
    /// <summary>
    /// The mountain chases you (<see cref="TrackLayout.avalanche"/>): a wall of snow roars down the track behind the
    /// pack - slower than a good racer, faster than a bad one, never far behind the last car. Caught by it, a car
    /// tumbles and is spat out just ahead of it, slowed. On screen: the snow billowing over everything behind its
    /// front, its distance behind you (frost creeping in at the edges, the ground shaking, the rumble swelling as it
    /// closes in). Also the race's other set pieces: ice bridges (<see cref="TrackLayout.Gap.collapse"/>) that fall
    /// just after the first car is over, pines crashing across half the road ahead of the leader
    /// (<see cref="TrackLayout.fallingTrees"/>), and a moment of slow motion at the top of the big jump.
    /// </summary>
    public class TrackAvalanche : TrackDerivedBehaviour
    {
        [SerializeField] Material plain, particleMaterial;
        [SerializeField] Sprite pineSprite, fallenPineSprite;

        const int BridgeOrder = -49, LogOrder = 9, SnowOrder = 58, CloudOrder = 60;
        static readonly Color IceBlock = new Color(0.7f, 0.86f, 0.96f).linear, IceBlockDark = new Color(0.5f, 0.72f, 0.88f).linear,
            IceEdge = new Color(0.94f, 0.98f, 1f).linear, Crack = new Color(0.2f, 0.4f, 0.6f).linear;

        readonly List<GameObject> _parts = new();
        class BridgeView { public int Gap; public Mesh Mesh; public Color[] Colors; public Vector2 Centre; public bool Burst; }
        readonly List<BridgeView> _bridges = new();
        class TreeView { public Vector2 Data; public Transform Standing, Fallen; public float FellAt = -1f; public bool Down; }
        readonly List<TreeView> _trees = new();

        // The avalanche.
        float _front;           // its lap distance, measured from the start line (negative behind it)
        bool _started, _stopped, _crashed;
        Mesh _snowMesh;
        ParticleSystem _cloud, _burst;
        AudioSource _rumble;
        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.CameraRig.CameraShake _shake;
        readonly Dictionary<SomeGame.Car.CarMovement, float> _caughtAt = new();
        Canvas _canvas;
        TextMeshProUGUI _label;
        Image _frost, _flash;
        bool _closeCall;
        float _goAt = -1f;
        bool _slowDone;

        // ------------------------------------------------------------------ build

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _bridges.Clear(); _trees.Clear();
            for (int g = 0; g < layout.gaps.Count; g++) if (layout.gaps[g].collapse) BuildBridge(path, layout, g);
            if (!Application.isPlaying) return;
            track.ResetAvalanche();
            if (layout.fallingTrees != null) foreach (var t in layout.fallingTrees) BuildTree(path, t);
            if (layout.avalanche != null && layout.avalanche.enabled) BuildAvalanche(layout);
        }

        // An ice bridge over the gap: blocks of ice with white edges (it cracks, then falls into the depths).
        void BuildBridge(TrackPath path, TrackLayout layout, int g)
        {
            var gap = layout.gaps[g];
            float length = path.DeltaDistance(gap.from, gap.to) + 1f;
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (float s = -0.5f; s < length; s += 1.1f)
            {
                float d0 = gap.from + s, d1 = Mathf.Min(gap.from + length - 0.5f, d0 + 1f);
                float w = track.OffRoadAt(d0) + 0.6f;
                Vector2 a = path.PointAt(d0), b = path.PointAt(d1), n0 = path.NormalAt(d0), n1 = path.NormalAt(d1);
                // Two or three blocks across, staggered.
                int across = (int)(s / 1.1f) % 2 == 0 ? 3 : 2;
                for (int k = 0; k < across; k++)
                {
                    float l0 = -w + 2f * w * k / across + 0.08f, l1 = -w + 2f * w * (k + 1) / across - 0.08f;
                    var col = (k + (int)(s / 1.1f)) % 3 == 0 ? IceBlockDark : IceBlock;
                    Quad(a + n0 * l0, b + n1 * l0, b + n1 * l1, a + n0 * l1, col, v, c, t);
                }
            }
            foreach (float side in new[] { -1f, 1f })
                for (float s = -0.5f; s < length - 0.5f; s += 1f)
                {
                    float d0 = gap.from + s, d1 = d0 + 1f;
                    float w = track.OffRoadAt(d0) + 0.6f;
                    Vector2 a = path.PointAt(d0) + path.NormalAt(d0) * side * w, b = path.PointAt(d1) + path.NormalAt(d1) * side * w;
                    Vector2 n = path.NormalAt(d0) * 0.35f;
                    Quad(a - n, b - n, b + n, a + n, IceEdge, v, c, t);
                }
            var mesh = Mesh("IceBridge", v, c, t);
            Part("IceBridge", mesh, BridgeOrder);
            _bridges.Add(new BridgeView { Gap = g, Mesh = mesh, Colors = c.ToArray(), Centre = path.PointAt(gap.from + length * 0.5f) });
        }

        void BuildTree(TrackPath path, Vector2 data)
        {
            float d = data.x, side = Mathf.Sign(data.y);
            Vector2 n = path.NormalAt(d), basePoint = path.PointAt(d) + n * side * (track.OffRoadAt(d) + 2.5f);
            var standing = Sprite("StandingPine", pineSprite, basePoint, 6f, d * 37f, 12);
            var fallen = Sprite("FallenPine", fallenPineSprite, basePoint, 1f, 0f, LogOrder);
            fallen.gameObject.SetActive(false);
            _trees.Add(new TreeView { Data = data, Standing = standing.transform, Fallen = fallen.transform });
        }

        void BuildAvalanche(TrackLayout layout)
        {
            var a = layout.avalanche;
            _front = a.from;
            _snowMesh = new Mesh { name = "AvalancheSnow", hideFlags = HideFlags.DontSave };
            Part("AvalancheSnow", _snowMesh, SnowOrder);
            _cloud = Particles("AvalancheCloud", new Color(0.97f, 0.98f, 1f, 0.95f), CloudOrder, 600, new Vector2(1.6f, 2.8f), new Vector2(4f, 9f));
            _burst = Particles("AvalancheBurst", new Color(0.95f, 0.97f, 1f, 0.9f), CloudOrder + 1, 400, new Vector2(1f, 2f), new Vector2(2f, 5f));
            _rumble = gameObject.AddComponent<AudioSource>();
            _rumble.clip = SomeGame.Audio.AudioLibrary.Instance != null ? SomeGame.Audio.AudioLibrary.Instance.avalancheRumble : null;
            _rumble.loop = true;
            _rumble.volume = 0f;
            _rumble.playOnAwake = false;
            BuildHud();
        }

        // ------------------------------------------------------------------ Play mode

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            if (_race != null) _race.PlayerLapCompleted += OnPlayerLine;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_race != null) _race.PlayerLapCompleted -= OnPlayerLine;
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        void OnPlayerLine(float time, bool best)
        {
            var player = _race != null ? _race.Player : null;
            if (player == null || !player.IsFinished || _combo == null || !_started) return;
            _combo.Banner("OUTRUN!", new Color(0.55f, 0.9f, 1f));
        }

        // A car's progress from the start line (behind it, on the grid: negative).
        float Progress(TrackSensor sensor)
        {
            float d = sensor.Current.Distance, len = track.Path.Length;
            return d > len - 80f ? d - len : d;
        }

        bool Racing => _race != null && _race.State == SomeGame.Race.RaceState.Racing;

        void FixedUpdate()
        {
            if (!Application.isPlaying || track == null || track.Layout == null || _race == null) return;
            var layout = track.Layout;
            float clock = Track.Clock;
            var standings = _race.Standings;
            // The leader and the last car still racing.
            float lead = float.MinValue, last = float.MaxValue;
            foreach (var p in standings)
            {
                if (p == null) continue;
                var s = p.GetComponent<TrackSensor>();
                if (s == null) continue;
                float u = Progress(s);
                lead = Mathf.Max(lead, u);
                if (!p.IsFinished) last = Mathf.Min(last, u);
            }
            if (!Racing && !_started) return;
            if (_goAt < 0f) _goAt = Time.time;
            Bridges(lead, clock);
            Trees(lead);
            if (layout.avalanche == null || !layout.avalanche.enabled) return;
            Chase(layout.avalanche, last);
        }

        // Each ice bridge falls a moment after the first car is over it.
        void Bridges(float lead, float clock)
        {
            var path = track.Path;
            foreach (var b in _bridges)
            {
                var gap = track.Layout.gaps[b.Gap];
                if (float.IsPositiveInfinity(track.CollapseAt(b.Gap)) && lead > gap.to + 1f && lead < gap.to + 200f)
                {
                    track.Collapse(b.Gap, clock + 1.2f);
                    var player = _race.Player;
                    if (player != null && _combo != null)
                    {
                        float behind = gap.from - Progress(player.GetComponent<TrackSensor>());
                        if (behind > 0f && behind < 90f) _combo.Banner("BRIDGE OUT!", new Color(0.6f, 0.85f, 1f));
                    }
                }
            }
        }

        // A pine crashes across half the road just before the leader gets there.
        void Trees(float lead)
        {
            foreach (var tree in _trees)
            {
                if (tree.FellAt >= 0f || lead < tree.Data.x - 32f || lead > tree.Data.x) continue;
                tree.FellAt = Time.time;
            }
        }

        void Chase(TrackLayout.AvalancheChase a, float last)
        {
            if (!_started)
            {
                if (Time.time - _goAt < a.delay) return;
                _started = true;
                if (_rumble != null && _rumble.clip != null) _rumble.Play();
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.thunder, 1f);
                if (_shake != null) _shake.Shake(0.8f);
                if (_combo != null) _combo.Banner("AVALANCHE!", new Color(1f, 0.45f, 0.4f));
            }
            float dt = Time.fixedDeltaTime;
            if (!_stopped)
            {
                float d = Mathf.Repeat(_front, track.Path.Length);
                _front += a.speed * track.SlopeAt(d) * dt;
                if (last < float.MaxValue && last - _front > a.maxBehind) _front = last - a.maxBehind;
                if (a.stopAt > 0f && _front >= a.stopAt)
                {
                    _front = a.stopAt;
                    _stopped = true;
                }
            }
            // Caught: tumbling, spat out just ahead of it. (Once it has come to rest at the foot of the jump it
            // catches nobody any more.)
            if (_stopped) return;
            var path = track.Path;
            foreach (var p in _race.Standings)
            {
                if (p == null || p.IsFinished) continue;
                var car = p.GetComponent<SomeGame.Car.CarMovement>();
                var sensor = p.GetComponent<TrackSensor>();
                if (car == null || sensor == null || car.IsRiding || car.IsCarried) continue;
                if (Progress(sensor) > _front + 1f) continue;
                if (_caughtAt.TryGetValue(car, out float at) && Time.time - at < 2.5f) continue;
                _caughtAt[car] = Time.time;
                // Never put down in front of a jump (too slow to clear it): thrown over to its far side instead.
                float ahead = _front + 8f;
                foreach (var jump in track.Jumps)
                    if (path.DeltaDistance(jump.RampStart - 25f, ahead) >= 0f && path.DeltaDistance(ahead, jump.FarBank) >= 0f)
                        ahead = jump.FarBank + 4f;
                Vector2 pos = path.PointAt(ahead) + path.NormalAt(ahead) * Mathf.Clamp(sensor.Current.Lateral, -2.5f, 2.5f);
                Vector2 dir = path.TangentAt(ahead);
                float was = sensor.Current.Distance;
                car.Respawn(new Pose(pos, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f)));
                p.CreditFlight(was, path.WrapDistance(ahead)); // carried along by the snow: that road counts
                float top = car.Stats != null ? car.Stats.topSpeed : 18f;
                car.Body.linearVelocity = dir * top * 0.45f;
                car.Slip(Random.value < 0.5f ? -1 : 1);
                if (_burst != null) { _burst.transform.position = pos; _burst.Emit(40); }
                if (p == _race.Player)
                {
                    if (_combo != null) _combo.Banner("CAUGHT!", new Color(0.85f, 0.92f, 1f));
                    if (_shake != null) _shake.Shake(0.7f);
                    if (_flash != null) _flash.color = new Color(1f, 1f, 1f, 0.95f);
                    _closeCall = false;
                }
            }
        }

        void Update()
        {
            if (!Application.isPlaying || track == null || track.Layout == null) return;
            UpdateBridges();
            UpdateTrees();
            SlowMotion();
            var a = track.Layout.avalanche;
            if (a == null || !a.enabled || _snowMesh == null) return;
            DrawSnow();
            UpdateHud();
        }

        void UpdateBridges()
        {
            float clock = Track.Clock;
            foreach (var b in _bridges)
            {
                float at = track.CollapseAt(b.Gap);
                if (float.IsPositiveInfinity(at)) continue;
                float u = clock - at;
                var colors = new Color[b.Colors.Length];
                // Cracking for a second (it flickers darker), then gone.
                float alpha = u < 0f ? 1f : Mathf.Clamp01(1f - u / 0.5f);
                float crack = u < 0f ? (0.5f + 0.5f * Mathf.Sin(clock * 30f)) * Mathf.Clamp01(1f + u) : 0f;
                for (int i = 0; i < colors.Length; i++)
                {
                    var c = Color.Lerp(b.Colors[i], Crack, crack * 0.4f);
                    colors[i] = new Color(c.r, c.g, c.b, b.Colors[i].a * alpha);
                }
                b.Mesh.colors = colors;
                if (u >= 0f && !b.Burst && _burst != null)
                {
                    b.Burst = true;
                    _burst.transform.position = b.Centre;
                    _burst.Emit(60);
                    var player = _race != null ? _race.Player : null;
                    if (player != null && Vector2.Distance(player.transform.position, b.Centre) < 50f)
                    {
                        SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.iceCrack, 0.9f);
                        if (_shake != null) _shake.Shake(0.4f);
                    }
                }
            }
        }

        // A falling pine: it tips over from the roadside, swinging down across half the road, and lies there.
        void UpdateTrees()
        {
            var path = track.Path;
            foreach (var tree in _trees)
            {
                if (tree.FellAt < 0f || tree.Down) continue;
                float d = tree.Data.x, side = Mathf.Sign(tree.Data.y);
                float w = track.OffRoadAt(d);
                Vector2 n = path.NormalAt(d), t = path.TangentAt(d);
                Vector2 basePoint = path.PointAt(d) + n * side * (w + 2.5f);
                float u = Mathf.Clamp01((Time.time - tree.FellAt) / 0.9f);
                float eased = u * u; // falls faster and faster
                // From pointing up the road to lying across it, towards the middle (a little past it).
                Vector2 across = -n * side;
                Vector2 dir = Vector2.Lerp(t, across, eased).normalized;
                float length = w + 3f;
                tree.Standing.gameObject.SetActive(false);
                tree.Fallen.gameObject.SetActive(true);
                tree.Fallen.position = basePoint + dir * length * 0.5f;
                tree.Fallen.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                if (fallenPineSprite != null)
                    tree.Fallen.localScale = new Vector3(2.4f / fallenPineSprite.bounds.size.x, length / fallenPineSprite.bounds.size.y, 1f);
                if (u < 1f) continue;
                tree.Down = true;
                var box = tree.Fallen.gameObject.AddComponent<BoxCollider2D>();
                box.size = fallenPineSprite != null ? new Vector2(fallenPineSprite.bounds.size.x * 0.5f, fallenPineSprite.bounds.size.y * 0.95f) : Vector2.one;
                // Rivals steer round its trunk: it covers this side of the road up to the middle.
                Track.Logs.Add(new Vector3(d, side * (w - (length - 2.5f) * 0.5f) , (length - 2.5f) * 0.5f + 0.4f));
                if (_burst != null) { _burst.transform.position = basePoint + dir * length * 0.6f; _burst.Emit(30); }
                var player = _race != null ? _race.Player : null;
                if (player != null && Vector2.Distance(player.transform.position, tree.Fallen.position) < 45f)
                {
                    if (_shake != null) _shake.Shake(0.45f);
                    if (_combo != null) _combo.Banner("TIMBER!", new Color(0.75f, 0.55f, 0.35f));
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.crash, 0.7f);
                }
            }
        }

        // At the top of the player's flight over the big jump, time slows down for a moment.
        void SlowMotion()
        {
            if (_slowDone || _race == null || _race.Player == null) return;
            var car = _race.Player.GetComponent<SomeGame.Car.CarMovement>();
            var sensor = _race.Player.GetComponent<TrackSensor>();
            if (car == null || sensor == null || !car.IsAirborne || car.IsCarried || car.IsRiding || car.AirHeight < 0.93f) return;
            foreach (var gap in track.Layout.gaps)
            {
                if (!gap.slowMotion) continue;
                float into = track.Path.DeltaDistance(gap.from, sensor.Current.Distance);
                if (into < 0f || into > track.Path.DeltaDistance(gap.from, gap.to)) continue;
                _slowDone = true;
                StartCoroutine(Slow());
                if (_combo != null) _combo.Banner("SKY HIGH!", new Color(1f, 0.85f, 0.3f));
                break;
            }
        }

        System.Collections.IEnumerator Slow()
        {
            float before = Time.timeScale;
            Time.timeScale = before * 0.35f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(0.7f);
            Time.timeScale = before;
            Time.fixedDeltaTime = 0.02f;
        }

        // The snow behind the front: a white mass over everything, soft at its leading edge.
        void DrawSnow()
        {
            var path = track.Path;
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            if (!_started) { _snowMesh.Clear(); return; }
            const float back = 70f, half = 34f;
            int steps = 36;
            var col = new Color(0.96f, 0.98f, 1f);
            for (int i = 0; i <= steps; i++)
            {
                float s = -back + (back + 3f) * i / steps;
                float d = Mathf.Repeat(_front + s, path.Length);
                Vector2 p = path.PointAt(d), n = path.NormalAt(d);
                float a = s > 0f ? 0f : s > -10f ? Mathf.InverseLerp(0f, -10f, s) * 0.9f : 0.93f;
                // A ragged, billowing front edge.
                float wobble = Mathf.Sin(Time.time * 3f + i) * 0.8f;
                int k = v.Count;
                for (int q = 0; q <= 6; q++)
                {
                    float lat = -half + 2f * half * q / 6f;
                    float edge = 1f - Mathf.Pow(Mathf.Abs(lat) / half, 4f);
                    v.Add(p + n * lat + path.TangentAt(d) * wobble * edge);
                    c.Add(new Color(col.r, col.g, col.b, a * edge));
                }
                if (i > 0)
                    for (int q = 0; q < 6; q++)
                    {
                        int a0 = k - 7 + q;
                        t.AddRange(new[] { a0, k + q, a0 + 1, a0 + 1, k + q, k + q + 1 });
                    }
            }
            _snowMesh.Clear();
            _snowMesh.SetVertices(v);
            _snowMesh.SetColors(c);
            _snowMesh.SetTriangles(t, 0);
            _snowMesh.RecalculateBounds();
            // Clouds billowing up along the front.
            if (_cloud != null && !_crashed)
            {
                float d = Mathf.Repeat(_front, path.Length);
                Vector2 p = path.PointAt(d), n = path.NormalAt(d), tan = path.TangentAt(d);
                for (int k = 0; k < 6; k++)
                {
                    var ep = new ParticleSystem.EmitParams
                    {
                        position = p + n * Random.Range(-half, half) + tan * Random.Range(-4f, 1f),
                        velocity = tan * Random.Range(4f, 12f) + Random.insideUnitCircle * 2f,
                        applyShapeToPosition = false,
                    };
                    _cloud.Emit(ep, 1);
                }
            }
            if (_stopped && !_crashed && _burst != null)
            {
                // It crashes into the foot of the jump: one great burst of snow.
                _crashed = true;
                float d = Mathf.Repeat(_front, path.Length);
                for (int k = 0; k < 6; k++) { _burst.transform.position = path.PointAt(d) + path.NormalAt(d) * Random.Range(-20f, 20f); _burst.Emit(50); }
            }
        }

        // ------------------------------------------------------------------ HUD: how close it is

        void BuildHud()
        {
            var go = new GameObject("AvalancheHud");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170, 2532);
            scaler.matchWidthOrHeight = 0.5f;
            // Frost creeping in from the edges.
            _frost = Img(go.transform, "Frost", FrostSprite(), new Color(1f, 1f, 1f, 0f));
            _flash = Img(go.transform, "Flash", null, new Color(1f, 1f, 1f, 0f));
            // The distance, under the race panel.
            var label = new GameObject("AvalancheDistance", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            _label = label.AddComponent<TextMeshProUGUI>();
            var any = FindAnyObjectByType<TMP_Text>();
            if (any != null) _label.font = any.font;
            _label.fontSize = 54f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontStyle = FontStyles.Italic;
            _label.raycastTarget = false;
            // Dark letters with a white outline: readable over the snow.
            _label.fontMaterial = new Material(_label.fontSharedMaterial);
            _label.outlineWidth = 0.25f;
            _label.outlineColor = new Color32(255, 255, 255, 255);
            var rt = _label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -360f);
            rt.sizeDelta = new Vector2(900f, 90f);
            _label.text = "";
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        static Sprite FrostSprite()
        {
            const int w = 64, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float ex = Mathf.Min(x, w - 1 - x) / (w * 0.5f), ey = Mathf.Min(y, h - 1 - y) / (h * 0.5f);
                    float e = Mathf.Min(ex, ey * 0.7f);
                    float a = Mathf.Clamp01(1f - e / 0.35f);
                    tex.SetPixel(x, y, new Color(0.92f, 0.97f, 1f, a * a));
                }
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        void UpdateHud()
        {
            if (_label == null) return;
            if (_flash != null && _flash.color.a > 0f) _flash.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(_flash.color.a, 0f, Time.unscaledDeltaTime * 1.4f));
            var player = _race != null ? _race.Player : null;
            if (!_started || player == null || player.IsFinished || _stopped)
            {
                _label.text = "";
                if (_frost != null) _frost.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(_frost.color.a, 0f, Time.deltaTime));
                if (_rumble != null) _rumble.volume = Mathf.MoveTowards(_rumble.volume, 0f, Time.deltaTime * 0.5f);
                return;
            }
            float gap = Progress(player.GetComponent<TrackSensor>()) - _front;
            float danger = Mathf.Clamp01(1f - gap / 45f);
            _label.text = $"AVALANCHE  {Mathf.Max(0, Mathf.RoundToInt(gap))} m";
            float pulse = danger > 0.5f ? 0.5f + 0.5f * Mathf.Sin(Time.time * 12f) : 0f;
            _label.color = Color.Lerp(new Color(0.1f, 0.16f, 0.3f), new Color(0.9f, 0.15f, 0.12f), Mathf.Max(danger * 0.8f, pulse * danger));
            _label.transform.localScale = Vector3.one * (1f + 0.08f * pulse * danger);
            if (_frost != null) _frost.color = new Color(1f, 1f, 1f, danger * 0.85f);
            if (_rumble != null) _rumble.volume = Mathf.Lerp(0.15f, 0.9f, danger);
            if (_shake != null && danger > 0.55f) _shake.Shake(0.12f * danger);
            // A close call: it was right behind you and you got away.
            if (gap < 8f) _closeCall = true;
            else if (_closeCall && gap > 22f)
            {
                _closeCall = false;
                if (_combo != null) _combo.Announce("CLOSE CALL!", new Color(0.6f, 0.9f, 1f));
            }
        }

        // ------------------------------------------------------------------ helpers

        static void Quad(Vector2 a, Vector2 b, Vector2 cc, Vector2 d, Color color, List<Vector3> v, List<Color> c, List<int> t)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            for (int k = 0; k < 4; k++) c.Add(color);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        static Mesh Mesh(string name, List<Vector3> v, List<Color> c, List<int> t)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void Part(string name, Mesh mesh, int order)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            Assign(go.GetComponent<MeshFilter>(), mesh);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = plain;
            r.sortingOrder = order;
            _parts.Add(go);
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

        ParticleSystem Particles(string name, Color color, int order, int max, Vector2 lifetime, Vector2 size)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _parts.Add(go);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.5f;
            var sizeOver = system.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.8f));
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true; col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = order;
            system.Play();
            return system;
        }
    }
}
