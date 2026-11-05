using System.Collections.Generic;
using SomeGame.Car;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.Track
{
    /// <summary>
    /// The weather turns on you (<see cref="TrackLayout.storms"/>): in a storm the snow drives across the screen and a
    /// whiteout closes in from the edges; side-wind gusts (a warning first) shove every car across the road - on a
    /// narrow ledge, over the edge; with lightning, thundersnow strikes the road just ahead of you (a ring marks the
    /// spot a moment before: be gone, or you are zapped into a spin). Snow twisters (<see cref="TrackLayout.twisters"/>)
    /// patrol their stretch, crossing and recrossing the road: touch one and you spin out.
    /// </summary>
    public class TrackStorm : MonoBehaviour
    {
        [SerializeField] Track track;
        [SerializeField] Material particleMaterial;

        const float Ramp = 40f;                 // a storm builds up / dies down over this many metres
        const float GustWarn = 0.9f, GustLength = 1.7f, GustPush = 15f;
        const float StrikeWarn = 1.25f, StrikeRadius = 3.4f;
        const float TwisterRadius = 2.6f, TwisterSpeed = 4f;
        const int TwisterOrder = 45, RingOrder = 44, BoltOrder = 66, ScorchOrder = 3;

        SomeGame.Race.RaceManager _race;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.CameraRig.CameraShake _shake;
        CarMovement[] _cars;
        CarMovement _player;
        Camera _camera;
        readonly List<GameObject> _parts = new();

        // On screen.
        Canvas _canvas;
        Image _whiteout, _vignette, _flash, _gustArrow;
        TextMeshProUGUI _gustLabel;
        ParticleSystem _blizzard, _burst;
        float _intensity; // the storm where the player is, smoothed

        // Gusts.
        float _nextGust, _gustAt = -10f;
        int _gustSide = 1;

        // Lightning.
        class Strike { public Vector2 At; public float HitsAt; public Transform Ring; public bool Done; }
        readonly List<Strike> _strikes = new();
        float _nextStrike;
        Sprite _ringSprite, _discSprite;
        LineRenderer _bolt;
        float _boltAt = -10f;

        // Twisters.
        class Twister { public Vector2 Span; public float Phase, Across, Speed; public Transform Root; public ParticleSystem Swirl; public Vector2 Position; }
        readonly List<Twister> _twisters = new();
        readonly Dictionary<CarMovement, float> _spunAt = new();

        void Start()
        {
            if (track == null) track = FindAnyObjectByType<Track>();
            var layout = track != null ? track.Layout : null;
            if (layout == null || (layout.storms.Count == 0 && layout.twisters.Count == 0 && layout.snowBase <= 0f && (layout.vortices == null || layout.vortices.Count == 0))) { enabled = false; return; }
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            _cars = FindObjectsByType<CarMovement>(FindObjectsSortMode.None);
            foreach (var car in _cars) if (car.GetComponent<SomeGame.Input.PlayerCarInput>() != null) _player = car;
            _camera = Camera.main;
            if (particleMaterial == null)
            {
                var any = FindAnyObjectByType<ParticleSystemRenderer>();
                if (any != null) particleMaterial = any.sharedMaterial;
            }
            _ringSprite = RadialSprite(64, (d) => Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(d - 0.84f) / 0.16f), 0.28f * Mathf.Clamp01((0.8f - d) / 0.1f)));
            _discSprite = RadialSprite(64, (d) => Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));
            if (_camera != null) _blizzard = Blizzard(_camera.transform);
            _burst = Burst();
            BuildBolt();
            foreach (var span in layout.twisters) BuildTwister(span);
            if (layout.vortices != null) foreach (var vortex in layout.vortices) BuildVortex(vortex);
            BuildUi();
            _nextGust = Time.time + 3f;
            _nextStrike = Time.time + 3f;
        }

        void OnDestroy()
        {
            foreach (var go in _parts) if (go != null) Destroy(go);
            if (_canvas != null) Destroy(_canvas.gameObject);
            WinterFx.Storm = 0f;
        }

        // How stormy it is at a lap distance (0 calm .. 1 the worst), and whether that storm brings lightning.
        float StormAt(float d, out bool lightning)
        {
            lightning = false;
            float best = 0f;
            var path = track.Path;
            // Sheltered in a tunnel.
            foreach (var cave in track.Layout.caves)
                if (cave.branch < 0 && path.DeltaDistance(cave.from, d) >= 0f && path.DeltaDistance(d, cave.to) >= 0f) return 0f;
            foreach (var s in track.Layout.storms)
            {
                float into = path.DeltaDistance(s.from, d), span = path.DeltaDistance(s.from, s.to);
                if (into < 0f || into > span) continue;
                float edge = Mathf.Min(into, span - into);
                float v = s.strength * Mathf.SmoothStep(0f, 1f, edge / Ramp);
                if (v > best) { best = v; lightning = s.lightning && edge > Ramp * 0.5f; }
            }
            return best;
        }

        bool Racing => _race != null && _race.State == SomeGame.Race.RaceState.Racing;

        // Cars the weather can get at: on the ground, on the road (not up on a train, riding, being carried, in a cave).
        static bool Exposed(CarMovement car, TrackSensor sensor) =>
            car != null && car.isActiveAndEnabled && sensor != null && !car.IsAirborne && !sensor.Aboard && sensor.Branch < 0;

        void Update()
        {
            if (_player == null) return;
            var sensor = _player.GetComponent<TrackSensor>();
            float d = sensor != null ? sensor.Current.Distance : 0f;
            float here = StormAt(d, out bool lightning);
            if (track.InGap(d) && here <= 0f) here = _intensity; // over a gap: no change
            _intensity = Mathf.MoveTowards(_intensity, here, Time.deltaTime * 0.6f);
            WinterFx.Storm = _intensity;
            float dark = TrackJourney.Darkness;
            DrawScreen(dark);
            if (Racing) Gusts(d);
            if (Racing && lightning) Lightning(sensor);
            UpdateStrikes();
            UpdateBolt();
        }

        void FixedUpdate()
        {
            if (!Racing) return;
            MoveTwisters(Time.fixedDeltaTime);
            PullIntoVortices(Time.fixedDeltaTime);
            // A gust: every car out in the storm is shoved across the road.
            float since = Time.time - _gustAt;
            if (since < 0f || since > GustLength) return;
            float push = GustPush * Mathf.Sin(since / GustLength * Mathf.PI);
            var path = track.Path;
            foreach (var car in _cars)
            {
                var s = car != null ? car.GetComponent<TrackSensor>() : null;
                if (!Exposed(car, s)) continue;
                float storm = StormAt(s.Current.Distance, out _);
                if (storm <= 0f) continue;
                car.Body.linearVelocity += path.NormalAt(s.Current.Distance) * (_gustSide * push * storm * Time.fixedDeltaTime);
            }
        }

        // ------------------------------------------------------------------ gusts

        void Gusts(float d)
        {
            if (_intensity < 0.25f) { _nextGust = Mathf.Max(_nextGust, Time.time + 1.5f); return; }
            if (Time.time < _nextGust) return;
            _gustSide = Random.value < 0.5f ? 1 : -1;
            _gustAt = Time.time + GustWarn;
            _nextGust = _gustAt + GustLength + Random.Range(2.2f, 4.5f) / Mathf.Max(0.4f, _intensity);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.5f * _intensity);
        }

        // ------------------------------------------------------------------ lightning

        void Lightning(TrackSensor sensor)
        {
            if (Time.time < _nextStrike || _player.IsAirborne) return;
            _nextStrike = Time.time + Random.Range(2.6f, 5f);
            var path = track.Path;
            float speed = Mathf.Max(8f, _player.Body.linearVelocity.magnitude);
            // Where the player will be when it hits (give or take): a fair chance to steer round it.
            float d = path.WrapDistance(sensor.Current.Distance + speed * StrikeWarn * Random.Range(0.85f, 1.1f));
            if (track.InGap(d)) return;
            float lat = Mathf.Clamp(sensor.Current.Lateral + Random.Range(-1.5f, 1.5f), -track.HalfWidthAt(d), track.HalfWidthAt(d));
            var at = path.PointAt(d) + path.NormalAt(d) * lat;
            var ring = Part("StrikeRing", _ringSprite, at, StrikeRadius * 2.3f, RingOrder);
            _strikes.Add(new Strike { At = at, HitsAt = Time.time + StrikeWarn, Ring = ring.transform });
        }

        void UpdateStrikes()
        {
            for (int i = _strikes.Count - 1; i >= 0; i--)
            {
                var s = _strikes[i];
                float left = s.HitsAt - Time.time;
                if (s.Ring != null)
                {
                    var sr = s.Ring.GetComponent<SpriteRenderer>();
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (14f + 20f * (1f - left / StrikeWarn)));
                    sr.color = new Color(0.45f, 0.8f, 1f, 0.6f + 0.4f * pulse);
                    s.Ring.localScale = Vector3.one * StrikeRadius * 2.3f * (0.8f + 0.2f * Mathf.Clamp01(left / StrikeWarn));
                }
                if (left > 0f) continue;
                if (!s.Done) Hit(s);
                if (s.Ring != null) Destroy(s.Ring.gameObject);
                _strikes.RemoveAt(i);
            }
        }

        void Hit(Strike s)
        {
            s.Done = true;
            _boltAt = Time.time;
            DrawBolt(s.At);
            if (_burst != null) { _burst.transform.position = s.At; _burst.Emit(40); }
            var scorch = Part("Scorch", _discSprite, s.At, StrikeRadius * 1.6f, ScorchOrder);
            scorch.color = new Color(0.12f, 0.12f, 0.16f, 0.55f);
            Destroy(scorch.gameObject, 8f);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.thunder, 0.9f);
            if (_shake != null) _shake.Shake(0.55f);
            foreach (var car in _cars)
            {
                var sensor = car != null ? car.GetComponent<TrackSensor>() : null;
                if (!Exposed(car, sensor) || Vector2.Distance(car.Body.position, s.At) > StrikeRadius) continue;
                car.Body.linearVelocity *= 0.35f;
                car.Slip(Random.value < 0.5f ? 1 : -1);
                if (car == _player && _combo != null) _combo.Banner("ZAPPED!", new Color(0.7f, 0.85f, 1f));
            }
        }

        void BuildBolt()
        {
            var go = new GameObject("LightningBolt");
            _parts.Add(go);
            _bolt = go.AddComponent<LineRenderer>();
            _bolt.material = new Material(Shader.Find("Sprites/Default"));
            _bolt.useWorldSpace = true;
            _bolt.widthMultiplier = 0.55f;
            _bolt.numCapVertices = 2;
            _bolt.sortingOrder = BoltOrder;
            _bolt.positionCount = 0;
        }

        // A jagged bolt down from the sky (off the top of the screen) onto the spot, with a fork or two.
        void DrawBolt(Vector2 at)
        {
            var points = new List<Vector3>();
            Vector2 from = at + new Vector2(Random.Range(-10f, 10f), 34f);
            const int steps = 14;
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                Vector2 p = Vector2.Lerp(from, at, t);
                if (k > 0 && k < steps) p += new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(-0.6f, 0.6f));
                points.Add(p);
            }
            // A fork from the middle.
            int f = Random.Range(4, 9);
            Vector2 fork = points[f];
            for (int k = 1; k <= 4; k++) points.Add(fork + new Vector2(Random.Range(1f, 2f) * k * (Random.value < 0.5f ? 1f : -1f) * 0.7f, -k * 1.6f));
            _bolt.positionCount = points.Count;
            _bolt.SetPositions(points.ToArray());
        }

        void UpdateBolt()
        {
            if (_bolt == null) return;
            float since = Time.time - _boltAt;
            // Bright, a flicker, gone.
            float a = since < 0.08f ? 1f : since < 0.14f ? 0.2f : since < 0.26f ? 0.9f : Mathf.Clamp01(1f - (since - 0.26f) / 0.15f);
            var c = new Color(0.85f, 0.93f, 1f, a).linear;
            _bolt.startColor = _bolt.endColor = c;
            if (a <= 0f) _bolt.positionCount = 0;
            if (_flash != null) _flash.color = new Color(0.9f, 0.95f, 1f, a * 0.55f);
        }

        // ------------------------------------------------------------------ the tornado wall

        // A huge whirl of snow round the eye, its shadow on the ground.
        void BuildVortex(TrackLayout.Vortex data)
        {
            var root = new GameObject("Vortex").transform;
            _parts.Add(root.gameObject);
            root.position = data.position;
            var shadow = Part("VortexShadow", _discSprite, Vector2.zero, data.radius * 2.2f, TwisterOrder - 30);
            shadow.color = new Color(0.16f, 0.2f, 0.32f, 0.45f);
            shadow.transform.SetParent(root, false);
            var system = Create("VortexSwirl", root, ParticleSystemSimulationSpace.Local, TwisterOrder, 3500);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 3f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.58f, 0.74f, 0.9f).linear, new Color(0.95f, 0.97f, 1f, 0.85f));
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = data.radius;
            shape.radiusThickness = 1f;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            velocity.radial = new ParticleSystem.MinMaxCurve(-3f, -1f);
            velocity.x = velocity.y = velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 2.5f;
            Fade(system, 0.75f);
            var emission = system.emission;
            emission.rateOverTime = 800f;
        }

        // Every car out in a vortex is dragged towards its eye (hardest close in).
        void PullIntoVortices(float dt)
        {
            var list = track.Layout.vortices;
            if (list == null || list.Count == 0) return;
            foreach (var car in _cars)
            {
                var s = car != null ? car.GetComponent<TrackSensor>() : null;
                if (!Exposed(car, s)) continue;
                foreach (var v in list)
                {
                    Vector2 to = v.position - car.Body.position;
                    float d = to.magnitude;
                    if (d > v.radius || d < 0.5f) continue;
                    car.Body.linearVelocity += to / d * (v.pull * (0.35f + 0.65f * (1f - d / v.radius)) * dt);
                }
            }
        }

        // ------------------------------------------------------------------ twisters

        void BuildTwister(Vector2 span)
        {
            var root = new GameObject("SnowTwister").transform;
            _parts.Add(root.gameObject);
            var shadow = Part("TwisterShadow", _discSprite, Vector2.zero, TwisterRadius * 3.4f, TwisterOrder - 30);
            shadow.color = new Color(0.2f, 0.26f, 0.4f, 0.55f);
            shadow.transform.SetParent(root, false);
            var swirl = Swirl(root);
            _twisters.Add(new Twister { Span = span, Phase = Random.Range(0f, 10f), Across = Random.Range(0f, 10f), Speed = Random.Range(0.85f, 1.15f), Root = root, Swirl = swirl });
            Place(_twisters[_twisters.Count - 1], 0f);
        }

        void Place(Twister t, float time)
        {
            var path = track.Path;
            float span = path.DeltaDistance(t.Span.x, t.Span.y);
            // Drifting up and down its stretch, and weaving across the road and back (out over the edge each time).
            float along = span * (0.5f + 0.5f * Mathf.Sin(time * TwisterSpeed * t.Speed / Mathf.Max(10f, span) + t.Phase));
            float d = path.WrapDistance(t.Span.x + along);
            float lat = (track.HalfWidthAt(d) + 1.5f) * Mathf.Sin(time * 1.1f * t.Speed + t.Across);
            t.Position = path.PointAt(d) + path.NormalAt(d) * lat;
            t.Root.position = t.Position;
        }

        void MoveTwisters(float dt)
        {
            float time = Track.Clock;
            foreach (var t in _twisters)
            {
                Place(t, time);
                foreach (var car in _cars)
                {
                    var sensor = car != null ? car.GetComponent<TrackSensor>() : null;
                    if (!Exposed(car, sensor) || car.IsSlipping) continue;
                    Vector2 away = car.Body.position - t.Position;
                    if (away.magnitude > TwisterRadius) continue;
                    if (_spunAt.TryGetValue(car, out float last) && Time.time - last < 2f) continue;
                    _spunAt[car] = Time.time;
                    // Caught in the whirl: flung round it and out, spinning.
                    Vector2 swirl = new Vector2(-away.y, away.x).normalized;
                    car.Body.linearVelocity = car.Body.linearVelocity * 0.5f + swirl * 9f + away.normalized * 4f;
                    car.Slip(Random.value < 0.5f ? 1 : -1);
                    if (car != _player) continue;
                    if (_combo != null) _combo.Banner("TWISTER!", new Color(0.8f, 0.9f, 1f));
                    if (_shake != null) _shake.Shake(0.4f);
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.wallWhoosh, 0.9f);
                }
            }
        }

        // A whirl of snow: flakes circling fast round the eye, drawn in towards it, rising (bigger) as they go.
        ParticleSystem Swirl(Transform parent)
        {
            var system = Create("Swirl", parent, ParticleSystemSimulationSpace.Local, TwisterOrder, 300);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.7f, 0.84f, 0.95f).linear, new Color(0.97f, 0.98f, 1f, 0.9f));
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = TwisterRadius;
            shape.radiusThickness = 0.6f;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(5f, 7f);
            velocity.radial = new ParticleSystem.MinMaxCurve(-1.2f, -0.6f);
            velocity.x = velocity.y = velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            Fade(system, 0.85f);
            var emission = system.emission;
            emission.rateOverTime = 220f;
            return system;
        }

        // ------------------------------------------------------------------ on screen

        void DrawScreen(float dark)
        {
            if (_camera != null && _blizzard != null)
            {
                float h = _camera.orthographicSize * 2f, w = h * _camera.aspect;
                var shape = _blizzard.shape;
                shape.scale = new Vector3(w * 1.4f, h * 1.3f, 0f);
                float snow = Mathf.Max(track.Layout.snowBase * 0.3f, _intensity);
                var emission = _blizzard.emission;
                emission.rateOverTime = w * h * (0.03f + 0.6f * snow * snow);
                // The snow drives the way the gusts blow (sideways to the road at the player).
                var sensor = _player.GetComponent<TrackSensor>();
                Vector2 n = sensor != null ? track.Path.NormalAt(sensor.Current.Distance) : Vector2.right;
                Vector2 t = sensor != null ? track.Path.TangentAt(sensor.Current.Distance) : Vector2.up;
                float since = Time.time - _gustAt;
                float gust = since >= 0f && since <= GustLength ? Mathf.Sin(since / GustLength * Mathf.PI) : 0f;
                Vector2 wind = (n * (_gustSide * (4f + 22f * gust)) - t * 3f) * (0.3f + snow);
                var velocity = _blizzard.velocityOverLifetime;
                velocity.x = new ParticleSystem.MinMaxCurve(wind.x * 0.8f, wind.x * 1.2f);
                velocity.y = new ParticleSystem.MinMaxCurve(wind.y * 0.8f - 2f, wind.y * 1.2f - 1f);
            }
            if (_canvas == null) return;
            // The whiteout: a veil over everything, thickest at the edges (dimmer as night falls).
            Color veil = Color.Lerp(new Color(0.93f, 0.96f, 1f), new Color(0.6f, 0.68f, 0.82f), dark);
            _whiteout.color = new Color(veil.r, veil.g, veil.b, (0.3f - 0.12f * dark) * _intensity * _intensity);
            _vignette.color = new Color(veil.r, veil.g, veil.b, Mathf.Clamp01(_intensity * (1.15f - 0.35f * dark)));
            // The gust warning: arrows the way it will blow, flashing.
            float until = _gustAt - Time.time, since2 = -until;
            bool warn = until > 0f && until <= GustWarn, blowing = since2 >= 0f && since2 <= GustLength;
            _gustLabel.gameObject.SetActive(warn || blowing);
            _gustArrow.gameObject.SetActive(warn || blowing);
            if (warn || blowing)
            {
                var sensor = _player.GetComponent<TrackSensor>();
                Vector2 n = sensor != null ? track.Path.NormalAt(sensor.Current.Distance) * _gustSide : Vector2.right;
                Vector3 screen = _camera != null ? _camera.transform.InverseTransformDirection(n) : (Vector3)n;
                float angle = Mathf.Atan2(screen.y, screen.x) * Mathf.Rad2Deg;
                _gustArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                _gustArrow.rectTransform.anchoredPosition = new Vector2(0f, 560f);
                float blink = warn ? (Mathf.Repeat(Time.time * 6f, 1f) < 0.5f ? 1f : 0.35f) : 1f;
                _gustArrow.color = new Color(1f, 1f, 1f, 0.9f * blink);
                _gustLabel.alpha = blink;
                _gustLabel.text = warn ? "GUST!" : "HOLD ON!";
            }
        }

        void BuildUi()
        {
            var go = new GameObject("StormHud");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -5; // over the world, under the race HUD
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170, 2532);
            scaler.matchWidthOrHeight = 0.5f;
            var square = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _whiteout = Stretch(go.transform, "Whiteout", square);
            _vignette = Stretch(go.transform, "Vignette", VignetteSprite());
            _flash = Stretch(go.transform, "Flash", square);
            _flash.color = Color.clear;
            _gustArrow = Img(go.transform, "GustArrow", ArrowSprite(), new Vector2(260f, 160f));
            var label = new GameObject("GustLabel", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            _gustLabel = label.AddComponent<TextMeshProUGUI>();
            var any = FindAnyObjectByType<TMP_Text>();
            if (any != null) _gustLabel.font = any.font;
            _gustLabel.fontSize = 76f;
            _gustLabel.fontStyle = FontStyles.Italic;
            _gustLabel.alignment = TextAlignmentOptions.Center;
            _gustLabel.raycastTarget = false;
            _gustLabel.fontMaterial = new Material(_gustLabel.fontSharedMaterial);
            _gustLabel.outlineWidth = 0.25f;
            _gustLabel.outlineColor = new Color32(20, 30, 60, 255);
            _gustLabel.rectTransform.sizeDelta = new Vector2(900f, 110f);
            _gustLabel.rectTransform.anchoredPosition = new Vector2(0f, 700f);
            _gustLabel.gameObject.SetActive(false);
            _gustArrow.gameObject.SetActive(false);
        }

        static Image Stretch(Transform parent, string name, Sprite sprite)
        {
            var img = Img(parent, name, sprite, Vector2.zero);
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.sizeDelta = Vector2.zero;
            img.color = Color.clear;
            return img;
        }

        static Image Img(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = size;
            return img;
        }

        // Clear in the middle, thick at the edges and corners.
        static Sprite VignetteSprite()
        {
            const int w = 64, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx * 1.1f + dy * dy * 0.9f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.2f, r));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        // A fat arrow pointing right (three chevrons).
        static Sprite ArrowSprite()
        {
            const int w = 96, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fy = Mathf.Abs((y + 0.5f) / h * 2f - 1f); // 0 middle .. 1 edge
                    bool on = false;
                    for (int k = 0; k < 3; k++)
                    {
                        float tip = 30f + k * 30f, back = tip - 18f;
                        float at = tip - fy * 22f; // the chevron's front edge at this height
                        if (x <= at && x >= at - 11f && x >= back - 22f) on = true;
                    }
                    tex.SetPixel(x, y, on ? Color.white : Color.clear);
                }
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        static Sprite RadialSprite(int size, System.Func<float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, d > 1f ? 0f : alpha(d)));
                }
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        SpriteRenderer Part(string name, Sprite sprite, Vector2 at, float size, int order)
        {
            var go = new GameObject(name);
            _parts.Add(go);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        // ------------------------------------------------------------------ particles

        // Big flakes driving across the whole view.
        ParticleSystem Blizzard(Transform cam)
        {
            var system = Create("Blizzard", cam, ParticleSystemSimulationSpace.World, 64, 4000);
            system.transform.localPosition = new Vector3(0f, 0f, 10f);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(4f, 6f);
            velocity.y = new ParticleSystem.MinMaxCurve(-3f, -1f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 1.5f;
            noise.frequency = 0.5f;
            Fade(system, 0.9f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.05f;
            renderer.lengthScale = 2f;
            return system;
        }

        ParticleSystem Burst()
        {
            var system = Create("StrikeBurst", null, ParticleSystemSimulationSpace.World, 46, 300);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.92f, 1f).linear, Color.white);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            Fade(system, 1f);
            var emission = system.emission;
            emission.rateOverTime = 0f;
            return system;
        }

        ParticleSystem Create(string name, Transform parent, ParticleSystemSimulationSpace space, int order, int max)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            else _parts.Add(go);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = space;
            main.maxParticles = max;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Play();
            return system;
        }

        static void Fade(ParticleSystem system, float alpha)
        {
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, 0.15f), new GradientAlphaKey(alpha, 0.6f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
        }
    }
}
