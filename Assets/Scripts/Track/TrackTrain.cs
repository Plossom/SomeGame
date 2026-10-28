using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The Midnight Express (<see cref="TrackLayout.train"/>): steam trains running round their own loop of rails.
    /// Where the rails run beside the road, kicker ramps launch cars up onto the flat wagons: up there a car rides
    /// along with the train (and can drive forward over the wagons, faster than on the road), until it leaps back
    /// down onto the road further on (a boost: TRAIN SURF!). Off the side of the train it drops to the ground. On
    /// the ground the train is solid: it shoves cars aside at the level crossing, where rivals stop for it. Also the
    /// loose gift boxes spilled over the street (<see cref="TrackLayout.gifts"/>), which scatter when driven through.
    /// </summary>
    [DefaultExecutionOrder(-20)] // moves riders with the train before the cars drive
    public class TrackTrain : TrackDerivedBehaviour
    {
        [SerializeField] Material plain, particleMaterial;
        [SerializeField] Sprite locoSprite, wagonSprite, giftSprite;

        /// <summary>The track's train, if it has one (rivals ask it where to ride).</summary>
        public static TrackTrain Instance { get; private set; }

        const float LocoLength = 8f, WagonLength = 6.6f, Gap = 0.7f, Width = 3.4f;
        const int RailOrder = -47, ShadowOrder = 2, TrainOrder = 7, RampOrder = -45, GiftOrder = 8;
        static readonly Color Sleeper = new Color(0.36f, 0.24f, 0.15f).linear, Steel = new Color(0.62f, 0.66f, 0.72f).linear,
            Hazard = new Color(0.96f, 0.76f, 0.19f).linear, HazardDark = new Color(0.16f, 0.16f, 0.18f).linear;

        readonly List<GameObject> _parts = new();
        TrackPath _rails;
        TrackLayout.TrainLine _line;
        // Each train: its vehicles (loco first), each a kinematic body.
        readonly List<List<(Transform t, Rigidbody2D body, Collider2D collider, SpriteRenderer shadow, bool loco)>> _trains = new();
        readonly List<Collider2D> _allColliders = new();
        readonly List<(Vector2 lip, Vector2 dir, float reach)> _ramps = new();
        readonly List<(Rigidbody2D body, Vector2 home)> _gifts = new();
        ParticleSystem _smoke;

        class Rider { public int Train; public float Since; }
        readonly Dictionary<SomeGame.Car.CarMovement, Rider> _riders = new();
        readonly HashSet<SomeGame.Car.CarMovement> _leaping = new(), _flying = new();
        readonly Dictionary<SomeGame.Car.CarMovement, System.Action> _landHandlers = new();
        SomeGame.Car.CarMovement[] _cars;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.Race.RaceManager _race;
        float _giftPopAt = -10f;
        readonly float[] _whistledAt = new float[8];

        // ------------------------------------------------------------------ build

        protected override void Build(TrackPath path, TrackLayout layout)
        {
            foreach (var go in _parts) if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            _parts.Clear(); _trains.Clear(); _allColliders.Clear(); _ramps.Clear(); _gifts.Clear();
            _line = layout.train;
            _rails = null;
            if (_line == null || !_line.enabled || _line.points == null || _line.points.Length < 3) { BuildGifts(layout); return; }
            _rails = new TrackPath(_line.points, 1f);
            BuildRails();
            foreach (var ramp in _line.ramps) BuildRamp(path, ramp);
            BuildGifts(layout);
            if (!Application.isPlaying) return;
            for (int k = 0; k < _line.trains; k++)
            {
                var vehicles = new List<(Transform, Rigidbody2D, Collider2D, SpriteRenderer, bool)>();
                for (int j = 0; j <= _line.wagons; j++)
                {
                    bool loco = j == 0;
                    var sprite = loco ? locoSprite : wagonSprite;
                    float length = loco ? LocoLength : WagonLength;
                    var shadow = Sprite("TrainShadow", sprite, Vector2.zero, Width, length, ShadowOrder);
                    shadow.color = new Color(0f, 0.03f, 0.1f, 0.3f);
                    var sr = Sprite(loco ? "Locomotive" : "FlatWagon", sprite, Vector2.zero, Width, length, TrainOrder);
                    var body = sr.gameObject.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    var box = sr.gameObject.AddComponent<BoxCollider2D>();
                    box.size = sprite != null ? sprite.bounds.size * 0.95f : Vector2.one;
                    _allColliders.Add(box);
                    vehicles.Add((sr.transform, body, box, shadow, loco));
                }
                _trains.Add(vehicles);
            }
            _smoke = Smoke();
            PlaceTrains(Track.Clock);
        }

        void BuildRails()
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (float s = 0f; s < _rails.Length; s += 0.9f)
            {
                Vector2 p = _rails.PointAt(s), d = _rails.TangentAt(s), n = new(-d.y, d.x);
                Quad(p - n * 1.5f - d * 0.2f, p + n * 1.5f - d * 0.2f, p + n * 1.5f + d * 0.2f, p - n * 1.5f + d * 0.2f, Sleeper, v, c, t);
            }
            for (int side = -1; side <= 1; side += 2)
                for (float s = 0f; s < _rails.Length; s += 1f)
                {
                    Vector2 a = _rails.PointAt(s), b = _rails.PointAt(s + 1f);
                    Vector2 na = _rails.NormalAt(s), nb = _rails.NormalAt(s + 1f);
                    Quad(a + na * (side * 0.75f - 0.1f), b + nb * (side * 0.75f - 0.1f), b + nb * (side * 0.75f + 0.1f), a + na * (side * 0.75f + 0.1f), Steel, v, c, t);
                }
            Part("Rails", Mesh("Rails", v, c, t), RailOrder);
        }

        // A kicker on the road's edge, angled toward the rails: hazard stripes and arrows pointing up onto the train.
        void BuildRamp(TrackPath path, Vector2 ramp)
        {
            Vector2 lip = path.PointAt(ramp.x) + path.NormalAt(ramp.x) * ramp.y;
            float side = Mathf.Sign(ramp.y);
            Vector2 tan = path.TangentAt(ramp.x), toward = path.NormalAt(ramp.x) * side;
            Vector2 dir = (tan * Mathf.Cos(28f * Mathf.Deg2Rad) + toward * Mathf.Sin(28f * Mathf.Deg2Rad)).normalized;
            // How far to the rails along that line.
            float reach = 8f, best = float.MaxValue;
            for (float x = 1f; x <= 24f; x += 0.25f)
            {
                var on = _rails.Project(lip + dir * x);
                if (Mathf.Abs(on.Lateral) < best) { best = Mathf.Abs(on.Lateral); reach = x; }
            }
            _ramps.Add((lip, dir, reach));
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            Vector2 n = new(-dir.y, dir.x);
            for (int q = 0; q < 6; q++)
            {
                Vector2 p0 = lip - dir * (4f - q * 0.67f), p1 = lip - dir * (4f - (q + 1) * 0.67f);
                Quad(p0 - n * 1.6f, p1 - n * 1.6f, p1 + n * 1.6f, p0 + n * 1.6f, q % 2 == 0 ? Hazard : HazardDark, v, c, t);
            }
            Part("TrainRamp", Mesh("TrainRamp", v, c, t), RampOrder);
        }

        void BuildGifts(TrackLayout layout)
        {
            if (!Application.isPlaying || layout.gifts == null || giftSprite == null) return;
            Color[] tints = { new(0.9f, 0.2f, 0.22f), new(0.2f, 0.62f, 0.35f), new(0.25f, 0.45f, 0.9f), new(0.95f, 0.75f, 0.2f), new(0.75f, 0.3f, 0.8f) };
            for (int i = 0; i < layout.gifts.Count; i++)
            {
                var sr = Sprite("GiftBox", giftSprite, layout.gifts[i], 1.4f, 1.4f, GiftOrder);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, i * 37f);
                sr.color = tints[i % tints.Length];
                var body = sr.gameObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.mass = 0.25f;
                body.linearDamping = 2.5f;
                body.angularDamping = 2f;
                var box = sr.gameObject.AddComponent<BoxCollider2D>();
                box.size = giftSprite.bounds.size * 0.8f;
                _gifts.Add((body, layout.gifts[i]));
            }
        }

        // ------------------------------------------------------------------ the trains' timetable

        float HeadAt(int k, float time) => Mathf.Repeat(time * _line.speed + k * _rails.Length / _line.trains, _rails.Length);

        // Where vehicle j of a train sits behind the front of its locomotive (rails distance), and its length.
        static float Behind(int j, out float length)
        {
            length = j == 0 ? LocoLength : WagonLength;
            return j == 0 ? LocoLength * 0.5f : LocoLength + Gap + (j - 1) * (WagonLength + Gap) + WagonLength * 0.5f;
        }

        // Is a point on vehicle j of train k at a time (with some slack along it, to bridge the couplings)?
        bool OnVehicle(int k, int j, Vector2 p, float time, float slackAlong = 0f, float slackAcross = 0.2f)
        {
            float centre = HeadAt(k, time) - Behind(j, out float length);
            Vector2 c = _rails.PointAt(centre), d = _rails.TangentAt(centre);
            Vector2 local = p - c;
            float along = Vector2.Dot(local, d), across = Vector2.Dot(local, new Vector2(-d.y, d.x));
            return Mathf.Abs(along) <= length * 0.5f + slackAlong && Mathf.Abs(across) <= Width * 0.5f + slackAcross;
        }

        /// <summary>The train whose wagons (or locomotive) are under a point now, or -1.</summary>
        int TrainUnder(Vector2 p, bool wagonsOnly, float slack = 0f)
        {
            if (_rails == null) return -1;
            float time = Track.Clock;
            for (int k = 0; k < _line.trains; k++)
                for (int j = wagonsOnly ? 1 : 0; j <= _line.wagons; j++)
                    if (OnVehicle(k, j, p, time, j == 0 ? 0f : Gap * 0.5f + slack)) return k;
            return -1;
        }

        /// <summary>For rivals: will a wagon be where kicker <paramref name="ramp"/> drops you, if you hit it in <paramref name="eta"/> seconds?</summary>
        public bool TrainWillBeThere(int ramp, float eta)
        {
            if (_rails == null || ramp < 0 || ramp >= _ramps.Count) return false;
            var (lip, dir, reach) = _ramps[ramp];
            Vector2 landing = lip + dir * reach;
            float time = Track.Clock + eta + (reach + 1f) / 18f;
            for (int k = 0; k < _line.trains; k++)
                for (int j = 1; j <= _line.wagons; j++)
                    if (OnVehicle(k, j, landing, time, -1f, -0.6f)) return true;
            return false;
        }

        /// <summary>For rivals riding a train: a point ahead to drive at (along the train, short of the locomotive's nose) and the room left.</summary>
        public Vector2 AimAboard(Vector2 position, float look, out float room)
        {
            room = 0f;
            if (_rails == null) return position;
            var on = _rails.Project(position);
            int k = TrainUnder(position, false, 1f);
            if (k < 0) return position;
            float nose = HeadAt(k, Track.Clock);
            room = Mathf.Max(0f, _rails.DeltaDistance(on.Distance, nose) - 3f);
            return _rails.PointAt(on.Distance + Mathf.Min(look, room));
        }

        // ------------------------------------------------------------------ Play mode

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isPlaying) Instance = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Instance == this) Instance = null;
            if (Track.Obstacle == (System.Func<Vector2, bool>)Blocks) Track.Obstacle = null;
            foreach (var kv in _landHandlers) if (kv.Key != null) kv.Key.Landed -= kv.Value;
            _landHandlers.Clear();
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            Instance = this;
            _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            if (_rails != null) Track.Obstacle = Blocks;
            foreach (var car in _cars)
            {
                var c = car;
                System.Action handler = () => OnLanded(c);
                c.Landed += handler;
                _landHandlers[c] = handler;
            }
        }

        // A point the train is on or right next to (for cars on the ground).
        bool Blocks(Vector2 p)
        {
            if (_rails == null) return false;
            float time = Track.Clock;
            for (int k = 0; k < _line.trains; k++)
                for (int j = 0; j <= _line.wagons; j++)
                    if (OnVehicle(k, j, p, time, 2.5f, 1.2f)) return true;
            return false;
        }

        void PlaceTrains(float time)
        {
            for (int k = 0; k < _trains.Count; k++)
                for (int j = 0; j < _trains[k].Count; j++)
                {
                    float centre = HeadAt(k, time) - Behind(j, out _);
                    Vector2 p = _rails.PointAt(centre), d = _rails.TangentAt(centre);
                    float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
                    var v = _trains[k][j];
                    if (Application.isPlaying && v.body != null) { v.body.MovePosition(p); v.body.MoveRotation(angle); }
                    else v.t.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, angle));
                    v.shadow.transform.SetPositionAndRotation(p + new Vector2(0.9f, -1.3f), Quaternion.Euler(0f, 0f, angle));
                }
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying || _rails == null || _cars == null) return;
            float dt = Time.fixedDeltaTime, time = Track.Clock;
            PlaceTrains(time + dt);
            var path = track.Path;
            foreach (var car in _cars)
            {
                if (car == null || !car.isActiveAndEnabled) continue;
                var sensor = car.GetComponent<TrackSensor>();
                if (sensor == null) continue;
                if (_riders.TryGetValue(car, out var rider))
                {
                    Ride(car, sensor, rider, dt, time);
                    continue;
                }
                if (car.IsAirborne || car.IsRiding || sensor.Branch >= 0) continue;
                // Off a kicker toward the rails: launched up at the train.
                for (int i = 0; i < _ramps.Count; i++)
                {
                    var ramp = _line.ramps[i];
                    float to = path.DeltaDistance(sensor.Current.Distance, ramp.x);
                    if (to < -0.6f || to > 0.6f || Mathf.Abs(sensor.Current.Lateral - ramp.y) > 1.7f) continue;
                    float speed = Vector2.Dot(car.Body.linearVelocity, path.TangentAt(ramp.x));
                    if (speed < 9f) continue;
                    var (lip, dir, reach) = _ramps[i];
                    // A straight flight that comes down just past the middle of the rails.
                    float flight = Mathf.Clamp((reach + 0.8f) / speed, 0.45f, 1.3f);
                    car.Body.linearVelocity = dir * speed;
                    car.Launch(flight, dir, true);
                    _flying.Add(car);
                    break;
                }
            }
            Gifts();
            Whistle(time);
        }

        // Up on a train: carried along with it (turning with it on the curves), until the car leaves its wagons -
        // off the side it drops to the ground; at the leap it jumps back down onto the road.
        void Ride(SomeGame.Car.CarMovement car, TrackSensor sensor, Rider rider, float dt, float time)
        {
            rider.Since += dt;
            Vector2 pos = car.Body.position;
            var on = _rails.Project(pos);
            Vector2 a = _rails.PointAt(on.Distance), b = _rails.PointAt(on.Distance + _line.speed * dt);
            Vector2 ta = _rails.TangentAt(on.Distance), tb = _rails.TangentAt(on.Distance + _line.speed * dt);
            float turn = Vector2.SignedAngle(ta, tb);
            // Rotate the offset from the rails with the train, and carry the car along.
            Vector2 offset = pos - a;
            Vector2 moved = b + (Vector2)(Quaternion.Euler(0f, 0f, turn) * offset);
            car.Body.position = moved;
            car.Body.rotation += turn;
            car.Body.linearVelocity = Quaternion.Euler(0f, 0f, turn) * car.Body.linearVelocity;
            if (car.IsAirborne) return; // a hop on the train: still over it
            // The leap: back down onto the road ahead.
            var path = track.Path;
            float d = sensor.Current.Distance;
            float past = path.DeltaDistance(_line.leapAt, d);
            bool farFromRoad = Mathf.Abs(path.Project(car.Body.position).Lateral) > 26f;
            if (past >= 0f && past < 40f || farFromRoad)
            {
                Leap(car, sensor, d, rider);
                return;
            }
            if (TrainUnder(car.Body.position, false, 0.2f) == rider.Train) return;
            // Off the side (or the front or back): it drops to the ground, keeping the train's speed.
            GetOff(car, sensor);
            car.Body.linearVelocity += tb * _line.speed;
            car.Launch(0.35f);
        }

        void Leap(SomeGame.Car.CarMovement car, TrackSensor sensor, float d, Rider rider)
        {
            var path = track.Path;
            Vector2 target = path.PointAt(d + 16f);
            Vector2 train = _rails.TangentAt(_rails.Project(car.Body.position).Distance) * _line.speed;
            float speed = Mathf.Max(16f, (car.Body.linearVelocity + train).magnitude);
            Vector2 dir = (target - car.Body.position).normalized;
            GetOff(car, sensor);
            car.Body.linearVelocity = dir * speed;
            Vector2 road = path.TangentAt(d + 16f);
            car.Launch(Mathf.Clamp(Vector2.Distance(target, car.Body.position) / speed, 0.5f, 1.2f), dir, true, Mathf.Atan2(road.y, road.x) * Mathf.Rad2Deg - 90f);
            _leaping.Add(car);
        }

        void GetOff(SomeGame.Car.CarMovement car, TrackSensor sensor)
        {
            _riders.Remove(car);
            sensor.Aboard = false;
            var collider = car.GetComponent<Collider2D>();
            if (collider != null) foreach (var c in _allColliders) Physics2D.IgnoreCollision(collider, c, false);
        }

        void OnLanded(SomeGame.Car.CarMovement car)
        {
            if (_rails == null) return;
            var sensor = car.GetComponent<TrackSensor>();
            bool player = car.GetComponent<SomeGame.Input.PlayerCarInput>() != null;
            if (_leaping.Remove(car))
            {
                car.GiveBoost(0.75f);
                if (player && _combo != null) _combo.Announce("TRAIN SURF!", new Color(1f, 0.6f, 0.25f));
                return;
            }
            bool fromKicker = _flying.Remove(car);
            if (_riders.ContainsKey(car) || sensor == null) return;
            int k = TrainUnder(car.Body.position, true, 0.3f);
            if (k < 0)
            {
                if (fromKicker && player && _combo != null) _combo.Announce("MISSED THE TRAIN!", new Color(0.7f, 0.75f, 0.85f));
                return;
            }
            _riders[car] = new Rider { Train = k };
            sensor.Aboard = true;
            // Up there it moves with the train: what is left of its own speed is its speed over the wagons, and it
            // straightens up along the train.
            Vector2 tan = _rails.TangentAt(_rails.Project(car.Body.position).Distance);
            float along = Vector2.Dot(car.Body.linearVelocity, tan) - _line.speed;
            car.Body.rotation = Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg - 90f;
            car.transform.rotation = Quaternion.Euler(0f, 0f, car.Body.rotation);
            car.Body.linearVelocity = tan * along;
            var collider = car.GetComponent<Collider2D>();
            if (collider != null) foreach (var c in _allColliders) Physics2D.IgnoreCollision(collider, c, true);
            if (player && _combo != null) _combo.Announce("ALL ABOARD!", new Color(1f, 0.85f, 0.35f));
        }

        // The gift boxes: knocked about by the cars; one far from home and out of sight goes back.
        void Gifts()
        {
            if (_gifts.Count == 0) return;
            var player = _race != null && _race.Player != null ? _race.Player.transform : null;
            foreach (var (body, home) in _gifts)
            {
                if (body == null) continue;
                if (player != null && (body.position - home).sqrMagnitude > 4f && Vector2.Distance(player.position, body.position) > 30f && Vector2.Distance(player.position, home) > 30f)
                {
                    body.position = home;
                    body.linearVelocity = Vector2.zero;
                }
                if (player != null && Time.time - _giftPopAt > 1.2f && (body.position - (Vector2)player.position).sqrMagnitude < 2.6f && body.linearVelocity.sqrMagnitude > 9f)
                {
                    _giftPopAt = Time.time;
                    if (_combo != null) _combo.Announce("GIFT SMASH!", new Color(1f, 0.4f, 0.4f));
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.snowPoof, 0.6f);
                }
            }
        }

        // The whistle as a train nears the player (once per pass), smoke from every chimney.
        void Whistle(float time)
        {
            var player = _race != null && _race.Player != null ? _race.Player.transform : null;
            for (int k = 0; k < _trains.Count; k++)
            {
                var loco = _trains[k][0].t;
                if (_smoke != null && Random.value < 0.35f)
                {
                    _smoke.transform.position = (Vector2)loco.position + (Vector2)loco.up * 2.2f;
                    _smoke.Emit(1);
                }
                if (player == null || k >= _whistledAt.Length) continue;
                float dist = Vector2.Distance(player.position, loco.position);
                if (dist < 35f && time - _whistledAt[k] > 12f)
                {
                    _whistledAt[k] = time;
                    SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.trainWhistle, Mathf.Lerp(0.8f, 0.35f, dist / 35f));
                }
            }
        }

        // ------------------------------------------------------------------ helpers

        ParticleSystem Smoke()
        {
            var go = new GameObject("TrainSmoke") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _parts.Add(go);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startColor = new Color(0.85f, 0.88f, 0.95f, 0.7f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = 30;
            system.Play();
            return system;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 position, float width, float length, int order)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = new Vector3(width / sprite.bounds.size.x, length / sprite.bounds.size.y, 1f);
            _parts.Add(go);
            return sr;
        }

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
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
    }
}
