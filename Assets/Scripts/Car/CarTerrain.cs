using System;
using System.Collections;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// What the ground under the car does: a kicker ramp before a river launches the car into the
    /// air, a big shortcut ramp beside the road flies it across a corner (the checkpoints it flies over
    /// count), water (missing the ramp, landing short, driving into a lake) means a splash and a restart a
    /// little way back, and an oil puddle spins the car round once. Rooftops: a car that lands on a roof
    /// row drives on up there; off an edge it drops to the street, at the end it jumps back down onto the
    /// road, and from the street a house is a wall it bumps off. Also plays the splash and landing dust.
    /// </summary>
    [DefaultExecutionOrder(-10)] // after the TrackSensor, before CarMovement reads OnRamp
    [RequireComponent(typeof(CarMovement), typeof(TrackSensor))]
    public class CarTerrain : MonoBehaviour
    {
        [SerializeField] Material particleMaterial;
        [Tooltip("Cars slower than this along the road just roll off the ramp into the water.")]
        [SerializeField, Min(0f)] float minLaunchSpeed = 4f;
        [Tooltip("Boost strength (0-1) for landing a corner-cut jump on the road.")]
        [SerializeField, Range(0f, 1f)] float shortcutBoost = 0.6f;
        [Tooltip("A corner-cut ramp launches a car moving within this angle of its arrows.")]
        [SerializeField, Range(10f, 90f)] float shortcutMaxAngle = 50f;
        [Tooltip("From this speed a kicker always carries the car across its gap; slower cars fall short.")]
        [SerializeField, Min(0f)] float minClearSpeed = 9f;
        [Tooltip("How far back along the road a car restarts after a splash.")]
        [SerializeField, Min(0f)] float respawnBack = 10f;
        [Tooltip("Seconds the car blinks after a restart (rivals; the player only blinks briefly).")]
        [SerializeField, Min(0f)] float blinkSeconds = 1.2f;
        [SerializeField, Min(0f)] float playerBlinkSeconds = 0.8f;
        [Tooltip("Seconds the player has to wait before driving on after a splash (the penalty).")]
        [SerializeField, Min(0f)] float playerWait = 0.5f;
        [SerializeField] int sortingOrder = 13;
        [Tooltip("Boost strength (0-1) from a dash panel.")]
        [SerializeField, Range(0f, 1f)] float dashBoost = 0.85f;
        [Tooltip("Metres of riding a banked wall that earn a boost when the car comes off it.")]
        [SerializeField, Min(1f)] float wallRideReward = 14f;
        [SerializeField, Range(0f, 1f)] float wallRideBoost = 0.55f;
        [Tooltip("Seconds in the air when a car drives off the edge of a roof.")]
        [SerializeField, Min(0.1f)] float roofDrop = 0.3f;

        CarMovement _car;
        TrackSensor _sensor;
        SomeGame.Race.RaceProgress _progress;
        float? _shortcutFrom;
        float _lastSplashAt = -1000f;
        int _repeatSplashes;
        bool _isRival;
        bool _fromCornerCut;
        bool _roofExit;
        float _wallRide;
        float _dashAt = -10f;
        int _lastBranch = -1;
        float _lastAlong;
        ParticleSystem _splash, _dust;
        SpriteRenderer[] _sprites;

        /// <summary>The car fell into water and is being put back on the road.</summary>
        public event Action Splashed;
        /// <summary>The car rode the whole row of roofs and landed back on the road.</summary>
        public event Action RoofRun;
        /// <summary>The car rode a banked wall far enough and came off it with a boost.</summary>
        public event Action WallRide;
        /// <summary>The car crossed a whole ice branch (frozen lake) and came off its far end.</summary>
        public event Action IceRun;
        /// <summary>The car bumped into a penguin (and spins).</summary>
        public event Action PenguinHit;
        /// <summary>Bumped into a cow on the road (its index in the layout).</summary>
        public event Action<int> CowHit;
        /// <summary>The car drove over a dash panel (boost).</summary>
        public event Action DashPanel;
        float _logAt = -10f;
        /// <summary>The car landed up on a row of roofs.</summary>
        public event Action RoofLanded;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _sensor = GetComponent<TrackSensor>();
            _progress = GetComponent<SomeGame.Race.RaceProgress>();
            _isRival = GetComponent<SomeGame.Input.PlayerCarInput>() == null;
            _sprites = GetComponentsInChildren<SpriteRenderer>();
            _splash = CreateBurst("Splash", new Color(0.85f, 0.95f, 1f, 0.9f), new Vector2(3f, 7f), new Vector2(0.25f, 0.55f), 0.7f);
            _dust = CreateBurst("LandingDust", new Color(0.93f, 0.86f, 0.7f, 0.7f), new Vector2(1.5f, 4f), new Vector2(0.4f, 0.8f), 0.6f);
        }

        void OnEnable() => _car.Landed += OnLanded;
        void OnDisable() => _car.Landed -= OnLanded;

        void FixedUpdate()
        {
            var track = _sensor.Track;
            if (track == null || _car.IsAirborne) return;
            Vector2 position = _car.Body.position;
            var point = _sensor.Current;

            if (_sensor.Elevated)
            {
                OnTheRoofs(track);
                return;
            }
            // Off the far end of an ice branch: the lake was crossed.
            if (_sensor.Branch < 0 && _lastBranch >= 0 && _lastBranch < track.Branches.Count)
            {
                var crossed = track.Branches[_lastBranch];
                if (crossed.Kind == SomeGame.Track.TrackLayout.BranchKind.Ice && _lastAlong > crossed.Length - 8f) IceRun?.Invoke();
            }
            _lastBranch = _sensor.Branch;
            _lastAlong = _sensor.BranchAlong;
            // Banked walls: a good ride ends with a boost.
            if (_car.IsWallRiding) _wallRide += _car.Body.linearVelocity.magnitude * Time.fixedDeltaTime;
            else if (!_sensor.OnBank && _wallRide > 0f)
            {
                if (_wallRide >= wallRideReward)
                {
                    _car.GiveBoost(wallRideBoost);
                    WallRide?.Invoke();
                }
                _wallRide = 0f;
            }
            // From the street, a house is a wall: bounce off it.
            int house = track.BranchAt(position, out float houseAlong, out float houseLateral, roofs: true, ground: false);
            if (house >= 0)
            {
                var route = track.Branches[house];
                Vector2 tangent = route.TangentAt(houseAlong);
                Vector2 normal = new Vector2(-tangent.y, tangent.x) * (houseLateral < 0f ? -1f : 1f);
                _car.Body.position = route.PointAt(houseAlong) + normal * (route.HalfWidth + track.Layout.kerbWidth + 0.4f);
                _car.Bump(normal);
                return;
            }

            // Shortcut ramps: the ramp (and a little approach) counts as road; take off at the lip.
            _car.OnRamp = false;
            foreach (var shortcut in track.Shortcuts)
            {
                Vector2 local = shortcut.Local(position);
                bool onApproach = local.y < -shortcut.Length * 0.5f;
                float halfWidth = shortcut.Width * 0.5f + (onApproach ? 0.2f : 0f);
                if (Mathf.Abs(local.x) > halfWidth || local.y < -shortcut.Length * 0.5f - 3.4f || local.y > shortcut.Length * 0.5f) continue;
                _car.OnRamp = true;
                // Take off anywhere on the front half, when moving roughly along the arrows.
                Vector2 velocity = _car.Body.linearVelocity;
                float speedAlong = Vector2.Dot(velocity, shortcut.Direction);
                bool aligned = Vector2.Angle(velocity, shortcut.Direction) < shortcutMaxAngle;
                if (local.y > 0f && local.y <= shortcut.Length * 0.5f && aligned && speedAlong > minLaunchSpeed)
                {
                    _shortcutFrom = shortcut.From;
                    _fromCornerCut = true;
                    _car.Launch(shortcut.AirTime); // the car keeps its own heading: aim at the arrows
                    return;
                }
            }

            // Gummiboat: driving onto one catapults the car on.
            var bouncer = track.BouncerAt(position);
            if (bouncer != null)
            {
                Bounce(bouncer);
                return;
            }

            // Take-off: at the end of a ramp, moving forward along the road.
            var ramp = track.RampAt(point.Distance, point.Lateral);
            if (ramp.HasValue)
            {
                float toLip = track.Path.DeltaDistance(point.Distance, ramp.Value.Lip);
                float along = Vector2.Dot(_car.Body.linearVelocity, track.Path.TangentAt(point.Distance));
                if (toLip < 0.8f && along > minLaunchSpeed)
                {
                    var stats = _car.Stats;
                    _shortcutFrom = point.Distance; // checkpoints over the gap count on landing
                    float air = stats.airTimeBase + stats.airTimePerSpeed * along;
                    // Hit the kicker at a decent speed and you clear the water (or reach the gummiboat in it).
                    if (along >= minClearSpeed)
                    {
                        float? pad = BouncerInGap(track, ramp.Value);
                        if (pad.HasValue)
                            air = (track.Path.DeltaDistance(ramp.Value.Lip, pad.Value) + 0.4f) / along; // land right on the first pad
                        else
                            air = Mathf.Max(air, (track.Path.DeltaDistance(ramp.Value.Lip, ramp.Value.FarBank) + 1.6f) / along);
                    }
                    _car.Launch(air);
                    return;
                }
            }

            // A log across the forest road, met on the ground (too slow off its ramp): bump back off it.
            if (track.Layout.logJumps != null && Time.time - _logAt > 0.8f)
                foreach (float log in track.Layout.logJumps)
                {
                    float to = track.Path.DeltaDistance(point.Distance, log);
                    if (Mathf.Abs(to) > 0.9f || Mathf.Abs(point.Lateral) > track.OffRoadAt(log)) continue;
                    _logAt = Time.time;
                    _car.Bump(-track.Path.TangentAt(log), 7f);
                    break;
                }

            if (!_car.OnRamp && track.IsWater(position))
            {
                Splash(track);
                return;
            }
            if (!_car.IsSlipping)
            {
                bool penguin = track.PenguinHit(position, Time.time);
                int cow = track.CowAt(position);
                if (penguin || cow >= 0 || track.IsOil(position) || track.FishHit(position, Time.time))
                {
                    _car.Slip(UnityEngine.Random.value < 0.5f ? -1 : 1);
                    if (penguin) PenguinHit?.Invoke();
                    if (cow >= 0) CowHit?.Invoke(cow);
                }
            }
            // Dash panel: a boost (once per panel).
            // (Not before GO: a boost drives the car, it would roll off the grid.)
            if (_car.ControlsEnabled && Time.time - _dashAt > 0.6f && _sensor.Branch < 0 && track.DashPanelAt(point.Distance, point.Lateral) != null)
            {
                _dashAt = Time.time;
                _car.GiveBoost(dashBoost);
                DashPanel?.Invoke();
            }
        }

        // Up on the roofs: off an edge the car drops to the street; at the end of the row it jumps back
        // down onto the road (the checkpoints it flies over count on landing).
        void OnTheRoofs(SomeGame.Track.Track track)
        {
            if (!_sensor.OnRoof)
            {
                _sensor.Elevated = false;
                _car.Launch(roofDrop);
                return;
            }
            var route = track.Branches[_sensor.Branch];
            if (_sensor.BranchAlong < route.Length - 1.2f) return;
            _sensor.Elevated = false;
            Vector2 position = _car.Body.position, land = track.Path.PointAt(route.To);
            float speed = Mathf.Max(_car.Body.linearVelocity.magnitude, 12f);
            _car.Body.linearVelocity = (land - position).normalized * speed;
            _shortcutFrom = route.MainDistance(_sensor.BranchAlong);
            _roofExit = true;
            _car.Launch(Mathf.Clamp(Vector2.Distance(position, land) / speed, 0.5f, 1.8f), land - position, locked: true);
        }

        void OnLanded()
        {
            var track = _sensor.Track;
            // Landed on a roof (from a jump pad): the car is up there now, turned to run along the row.
            float roofAlong = 0f;
            int roof = track != null ? track.BranchAt(_car.Body.position, out roofAlong, out _, roofs: true, ground: false) : -1;
            if (roof >= 0)
            {
                Vector2 along = track.Branches[roof].TangentAt(roofAlong);
                _car.Body.rotation = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, _car.Body.rotation);
                _car.Body.linearVelocity = along * _car.Body.linearVelocity.magnitude;
                _sensor.Elevated = true;
                _sensor.Sample();
                _dust.Emit(12);
                RoofLanded?.Invoke();
                return;
            }
            _sensor.Sample();
            var bouncer = track != null ? track.BouncerAt(_car.Body.position) : null;
            if (bouncer != null)
            {
                Bounce(bouncer); // chains: ramp -> gummiboat -> next road
                return;
            }
            bool dry = track != null && !track.IsWater(_car.Body.position);
            if (_shortcutFrom.HasValue && dry)
            {
                if (_progress != null) _progress.CreditFlight(_shortcutFrom.Value, _sensor.Current.Distance);
                if (_fromCornerCut && !_car.IsOffRoad) _car.GiveBoost(shortcutBoost); // landed the cut on the road: reward
            }
            _fromCornerCut = false;
            _shortcutFrom = null;
            if (_roofExit)
            {
                _roofExit = false;
                if (dry) RoofRun?.Invoke();
            }
            if (track != null && track.IsWater(_car.Body.position)) Splash(track);
            else _dust.Emit(18);
        }

        /// <summary>Any car bounced off a gummiboat (position), for the boat's squash animation.</summary>
        public static event Action<Vector2> AnyBounce;

        // The lap distance of the first gummiboat lying in a jump's gap, if any.
        static float? BouncerInGap(SomeGame.Track.Track track, SomeGame.Track.Track.Jump jump)
        {
            float? best = null;
            foreach (var b in track.Layout.bouncers)
            {
                var p = track.Path.Project(b.position);
                if (Mathf.Abs(p.Lateral) < 4f && track.Path.DeltaDistance(jump.Lip, p.Distance) > 0f && track.Path.DeltaDistance(p.Distance, jump.FarBank) > 0f)
                    if (!best.HasValue || track.Path.DeltaDistance(p.Distance, best.Value) > 0f) best = p.Distance;
            }
            return best;
        }

        void Bounce(SomeGame.Track.TrackLayout.Bouncer bouncer)
        {
            // Checkpoints the chain flies over count once the car lands on the road.
            _shortcutFrom ??= _sensor.Current.Distance;
            _car.Body.linearVelocity *= bouncer.speedBoost;
            float air = bouncer.airTime;
            if (bouncer.HasTarget)
            {
                // Jump pad: throw the car towards the next pad, keeping how far off-centre it landed,
                // so a sloppy first jump drifts further off with every hop.
                Vector2 chord = bouncer.target - bouncer.position;
                float padSpeed = Mathf.Max(_car.Body.linearVelocity.magnitude, 13f);
                // Onto a roof: an aimed flight that lands exactly on it.
                bool ontoRoof = _sensor.Track.BranchAt(bouncer.target, out _, out _, roofs: true, ground: false) >= 0;
                float heading = Mathf.Atan2(chord.y, chord.x) * Mathf.Rad2Deg - 90f, length = chord.magnitude;
                float? landing = null;
                if (!ontoRoof && _sensor.Track.BouncerAt(bouncer.target) == null)
                {
                    // Onto the road (not the next pad of a chain): an arc that lands on the target already pointing along the road (so a
                    // hidden shortcut across a bend drops you straight back into the race).
                    var path = _sensor.Track.Path;
                    Vector2 along = path.TangentAt(path.Project(bouncer.target).Distance);
                    float roadHeading = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg - 90f;
                    float turn = Mathf.DeltaAngle(heading, roadHeading);
                    if (Mathf.Abs(turn) < 75f && Mathf.Abs(turn) > 3f)
                    {
                        // On a circular arc the chord points halfway between the take-off and landing headings.
                        float half = Mathf.Abs(turn) * Mathf.Deg2Rad;
                        length *= half / Mathf.Sin(half); // and the arc is a little longer than the chord
                        landing = roadHeading;
                        heading -= turn;
                    }
                }
                Vector2 dir = new(Mathf.Cos((heading + 90f) * Mathf.Deg2Rad), Mathf.Sin((heading + 90f) * Mathf.Deg2Rad));
                _car.Body.linearVelocity = dir * padSpeed;
                _car.Body.rotation = heading;
                transform.rotation = Quaternion.Euler(0f, 0f, heading);
                _car.Launch(length / padSpeed, null, ontoRoof, landing);
                AnyBounce?.Invoke(bouncer.position);
                return;
            }
            // A boat in a gap carries the car on to the far side.
            var track = _sensor.Track;
            var at = track.Path.Project(bouncer.position);
            float speed = _car.Body.linearVelocity.magnitude;
            foreach (var jump in track.Jumps)
                if (track.Path.DeltaDistance(jump.Lip, at.Distance) > 0f && track.Path.DeltaDistance(at.Distance, jump.FarBank) > 0f && speed > 1f)
                    air = Mathf.Max(air, (track.Path.DeltaDistance(at.Distance, jump.FarBank) + 1.6f) / speed);
            _car.Launch(air);
            AnyBounce?.Invoke(bouncer.position);
        }

        void Splash(SomeGame.Track.Track track)
        {
            // Short of a window jump: a fall into the garden (dust), not a splash.
            bool dry = track.WaterDistance(_car.Body.position) > 2f && !track.Layout.waterWorld;
            var burst = dry ? _dust : _splash;
            burst.transform.position = _car.Body.position;
            burst.Emit(40);
            float d = _sensor.Current.Distance;
            _repeatSplashes = Mathf.Abs(track.Path.DeltaDistance(_lastSplashAt, d)) < 40f ? _repeatSplashes + 1 : 1;
            _lastSplashAt = d;
            if (_isRival && _repeatSplashes >= 3)
            {
                // Rivals never get stuck for good: after a few failed tries they are put down past the water.
                _repeatSplashes = 0;
                _car.Respawn(track.RespawnAfterWater(d));
            }
            else _car.Respawn(track.RespawnBefore(d, respawnBack)); // always back before the water, never ahead
            if (!dry) Splashed?.Invoke();
            StopAllCoroutines();
            StartCoroutine(Blink());
        }

        IEnumerator Blink()
        {
            float seconds = _isRival ? blinkSeconds : playerBlinkSeconds;
            // The player waits a moment before driving on (the penalty), then blinks while getting going.
            if (!_isRival && playerWait > 0f)
            {
                _car.ControlsEnabled = false;
                for (float w = 0f; w < playerWait; w += Time.deltaTime)
                {
                    bool show = Mathf.Repeat(w * 8f, 1f) < 0.6f;
                    foreach (var s in _sprites) if (s != null) s.enabled = show;
                    yield return null;
                }
                if (_progress == null || !_progress.IsFinished) _car.ControlsEnabled = true;
            }
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                bool visible = Mathf.Repeat(t * 8f, 1f) < 0.6f;
                foreach (var s in _sprites) if (s != null) s.enabled = visible;
                yield return null;
            }
            foreach (var s in _sprites) if (s != null) s.enabled = true;
        }

        ParticleSystem CreateBurst(string name, Color color, Vector2 speed, Vector2 size, float lifetime)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.3f;
            limit.dampen = 0.12f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;
            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.8f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = sortingOrder;
            system.Play();
            return system;
        }
    }
}
