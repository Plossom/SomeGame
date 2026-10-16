using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Brings the layout's hazards to life in Play mode: gummiboats (squash when a car bounces off),
    /// jumping fish (the water bubbles as a warning, then a big fish leaps across the road with its
    /// shadow; where there is no water beside the road it jumps between two little ponds) and obstacles
    /// that roll back and forth across the road (solid: they block and push cars). Winter: spring pads on dry
    /// ground, penguins waddling across, rolling snowballs and glowing dash panels on the road.
    /// </summary>
    public class TrackHazards : MonoBehaviour
    {
        [SerializeField] Track track;
        [SerializeField] Sprite gummiboatSprite;
        [Tooltip("Arrow on a jump pad, pointing where it throws you.")]
        [SerializeField] Sprite arrowSprite;
        [Tooltip("Little pond beside the road for fish on dry ground.")]
        [SerializeField] Sprite geyserSprite;
        [SerializeField] Sprite fishSprite;
        [SerializeField, Min(0.5f)] float fishLength = 3.2f;
        [SerializeField] Sprite logSprite;
        [SerializeField] Sprite hayBaleSprite;
        [SerializeField] Sprite snowballSprite;
        [Tooltip("Jump pad on dry ground (a gummiboat is used on water).")]
        [SerializeField] Sprite jumpPadSprite;
        [SerializeField] Sprite penguinSprite;
        [SerializeField] Sprite sheepSprite;
        [SerializeField] Sprite boulderSprite;
        [SerializeField] Sprite catSprite, vacuumSprite, ballSprite;
        [Tooltip("Jump pad on dry ground in the meadows and woods (a bouncy mushroom).")]
        [SerializeField] Sprite mushroomPadSprite;
        [Tooltip("Jump pad on the fairground (a trampoline).")]
        [SerializeField] Sprite trampolineSprite;
        [SerializeField, Min(0.3f)] float penguinSize = 2f;
        [SerializeField] Sprite dashPanelSprite;
        [SerializeField] Material particleMaterial;
        [Tooltip("Seconds of bubbling before a fish leaps.")]
        [SerializeField, Min(0f)] float geyserWarning = 0.7f;
        [SerializeField] int sortingOrder = 6;

        readonly List<GameObject> _spawned = new();
        readonly List<(Transform t, Vector2 position, float baseScale)> _boats = new();
        readonly List<(TrackLayout.Geyser data, Transform fish, Transform shadow, ParticleSystem splash, ParticleSystem bubbles, bool wasUp)> _fish = new();
        readonly List<(TrackLayout.Sweeper data, Rigidbody2D body, Transform visual, float span)> _sweepers = new();
        readonly Dictionary<Transform, float> _squash = new();
        readonly List<(TrackLayout.Penguins data, Transform[] birds)> _penguins = new();
        readonly List<(Vector2 position, Vector2 heading)> _penguinSpots = new();

        void Start()
        {
            Build();
            track.Rebuilt += Build;
            SomeGame.Car.CarTerrain.AnyBounce += OnBounce;
        }

        void OnDestroy()
        {
            if (track != null) track.Rebuilt -= Build;
            SomeGame.Car.CarTerrain.AnyBounce -= OnBounce;
        }

        void Build()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear(); _boats.Clear(); _fish.Clear(); _sweepers.Clear(); _squash.Clear(); _penguins.Clear();
            var layout = track.Layout;
            var path = track.Path;

            foreach (var b in layout.bouncers)
            {
                bool orange = layout.padLook == TrackLayout.PadLook.Orange;
                bool dry = jumpPadSprite != null && (orange || !track.IsWater(b.position));
                bool trampoline = dry && trampolineSprite != null && layout.padLook == TrackLayout.PadLook.Trampoline;
                bool mushroom = dry && !trampoline && !orange && mushroomPadSprite != null && layout.biome == TrackLayout.Biome.Meadow;
                var sr = Sprite(trampoline ? "Trampoline" : mushroom ? "MushroomPad" : dry ? "JumpPad" : "Gummiboat",
                    trampoline ? trampolineSprite : mushroom ? mushroomPadSprite : dry ? jumpPadSprite : gummiboatSprite,
                    b.position, b.radius * (mushroom || trampoline ? 2.4f : 2.15f), 0f, sortingOrder - 2);
                _boats.Add((sr.transform, b.position, sr.transform.localScale.x));
                if (b.HasTarget && arrowSprite != null)
                {
                    Vector2 dir = (b.target - b.position).normalized;
                    var arrow = Sprite("PadArrow", arrowSprite, b.position, b.radius * 1.1f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f, sortingOrder - 1);
                    arrow.transform.SetParent(sr.transform, true);
                }
            }

            foreach (var g in layout.geysers)
            {
                Vector2 normal = path.NormalAt(g.distance) * Mathf.Sign(g.side);
                float reach = track.OffRoadAt(g.distance) + 2.2f;
                Vector2 from = path.PointAt(g.distance) + normal * reach, to = path.PointAt(g.distance) - normal * reach;
                // On dry ground the fish needs a pond on each side.
                foreach (var end in new[] { from, to })
                    if (!track.IsWater(end)) Sprite("Pond", geyserSprite, end, 2.6f, 0f, sortingOrder - 3);
                var shadow = Sprite("FishShadow", fishSprite, from, fishLength, 0f, sortingOrder - 1);
                shadow.color = new Color(0f, 0f, 0f, 0.28f);
                var fish = Sprite("Fish", fishSprite, from, fishLength, 0f, sortingOrder + 8);
                var splash = Particles("Splash", from, Vector2.up, new Color(0.85f, 0.95f, 1f, 0.9f), 3f, 0.45f, new Vector2(0.3f, 0.6f), 180f);
                var bubbles = Particles("Bubbles", from, Vector2.up, new Color(0.75f, 0.92f, 1f, 0.85f), 1.2f, 0.4f, new Vector2(0.2f, 0.35f), 180f);
                fish.gameObject.SetActive(false);
                shadow.gameObject.SetActive(false);
                _fish.Add((g, fish.transform, shadow.transform, splash, bubbles, false));
            }

            foreach (var s in layout.sweepers)
            {
                var go = new GameObject($"Sweeper{_sweepers.Count}");
                go.transform.SetParent(transform, false);
                _spawned.Add(go);
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                Vector2 tangent = path.TangentAt(s.distance);
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                // Start where it rolls (not at the origin: its first move would sweep through the grid).
                Vector2 start = path.PointAt(s.distance) + path.NormalAt(s.distance) * (Mathf.Sin(Mathf.Repeat(s.offset / s.period, 1f) * Mathf.PI * 2f) * (track.OffRoadAt(s.distance) + 2.6f));
                go.transform.SetPositionAndRotation(start, Quaternion.Euler(0f, 0f, angle));
                body.position = start;
                body.rotation = angle;
                Transform visual;
                if (s.kind == TrackLayout.SweeperKind.Log)
                {
                    go.AddComponent<BoxCollider2D>().size = new Vector2(4f, 0.9f);
                    visual = Sprite("Log", logSprite, Vector2.zero, 4.2f, 0f, sortingOrder, go.transform).transform;
                }
                else if (s.kind == TrackLayout.SweeperKind.Vacuum && vacuumSprite != null)
                {
                    go.AddComponent<CircleCollider2D>().radius = 1.6f;
                    visual = Sprite("Vacuum", vacuumSprite, Vector2.zero, 3.4f, 0f, sortingOrder, go.transform).transform;
                }
                else if (s.kind == TrackLayout.SweeperKind.Ball && ballSprite != null)
                {
                    go.AddComponent<CircleCollider2D>().radius = 1.25f;
                    visual = Sprite("BeachBall", ballSprite, Vector2.zero, 2.6f, 0f, sortingOrder, go.transform).transform;
                }
                else if (s.kind == TrackLayout.SweeperKind.Boulder && boulderSprite != null)
                {
                    go.AddComponent<CircleCollider2D>().radius = 1.45f;
                    visual = Sprite("Boulder", boulderSprite, Vector2.zero, 3.1f, 0f, sortingOrder, go.transform).transform;
                    var dust = Particles("BoulderDust", start, Vector2.up, new Color(0.85f, 0.55f, 0.35f, 0.55f), 1.2f, 0.7f, new Vector2(0.6f, 1.2f), 60f);
                    dust.transform.SetParent(go.transform, true);
                    var e = dust.emission; e.rateOverTime = 14f;
                }
                else if (s.kind == TrackLayout.SweeperKind.Snowball && snowballSprite != null)
                {
                    go.AddComponent<CircleCollider2D>().radius = 1.35f;
                    visual = Sprite("Snowball", snowballSprite, Vector2.zero, 2.9f, 0f, sortingOrder, go.transform).transform;
                }
                else
                {
                    go.AddComponent<CircleCollider2D>().radius = 1.05f;
                    visual = Sprite("HayBale", hayBaleSprite, Vector2.zero, 2.2f, 0f, sortingOrder, go.transform).transform;
                }
                _sweepers.Add((s, body, visual, track.OffRoadAt(s.distance) + 2.6f));
            }

            if (penguinSprite != null)
                foreach (var line in layout.penguins)
                {
                    var birds = new Transform[line.count];
                    var (name, sprite, scale) = line.kind switch
                    {
                        TrackLayout.CrossingKind.Sheep when sheepSprite != null => ("Sheep", sheepSprite, 1.15f),
                        TrackLayout.CrossingKind.Cat when catSprite != null => ("Cat", catSprite, 2.2f),
                        _ => ("Penguin", penguinSprite, 1f),
                    };
                    for (int k = 0; k < line.count; k++)
                        birds[k] = Sprite(name, sprite, path.PointAt(line.distance), penguinSize * scale, 0f, sortingOrder + 1).transform;
                    _penguins.Add((line, birds));
                }

            // Dash panels lie flat on the road, under the cars, pointing along it.
            if (dashPanelSprite != null)
                foreach (var panel in layout.dashPanels)
                {
                    Vector2 tangent = path.TangentAt(panel.distance);
                    var sr = Sprite("DashPanel", dashPanelSprite, path.PointAt(panel.distance) + path.NormalAt(panel.distance) * panel.lateral, 1f,
                        Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f, -45);
                    Vector2 size = dashPanelSprite.bounds.size;
                    sr.transform.localScale = new Vector3(panel.width / size.x, panel.length / size.y, 1f);
                }
        }

        void FixedUpdate()
        {
            var path = track.Path;
            foreach (var (s, body, visual, span) in _sweepers)
            {
                float phase = Mathf.Repeat((Time.time + s.offset) / s.period, 1f);
                float lateral = Mathf.Sin(phase * Mathf.PI * 2f) * span;
                Vector2 target = path.PointAt(s.distance) + path.NormalAt(s.distance) * lateral;
                Vector2 step = target - body.position;
                body.MovePosition(target);
                // Hay bales, snowballs and balls roll as they move; the robot vacuum turns slowly as it goes.
                if (s.kind == TrackLayout.SweeperKind.Vacuum) visual.Rotate(0f, 0f, 40f * Time.fixedDeltaTime);
                else if (s.kind != TrackLayout.SweeperKind.Log) visual.Rotate(0f, 0f, -step.magnitude * 50f * Mathf.Sign(Mathf.Cos(phase * Mathf.PI * 2f)));
            }
        }

        void Update()
        {
            for (int i = 0; i < _fish.Count; i++)
            {
                var (g, fish, shadow, splash, bubbles, wasUp) = _fish[i];
                float t = Mathf.Repeat(Time.time + g.offset, g.period);
                var e = bubbles.emission;
                e.rateOverTime = t > g.period - geyserWarning ? 40f : 0f; // the water bubbles: it is about to jump
                var at = track.FishAt(g, Time.time);
                bool up = at.HasValue;
                fish.gameObject.SetActive(up);
                shadow.gameObject.SetActive(up);
                if (up)
                {
                    var (ground, height, dir) = at.Value;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    // Nose up on the way up, nose down on the way down; bigger the higher it is.
                    float u = t / g.active;
                    fish.SetPositionAndRotation(ground + new Vector2(1.2f, 1.6f) * height, Quaternion.Euler(0f, 0f, angle + Mathf.Lerp(-25f, 25f, u)));
                    fish.localScale = Vector3.one * (fishLength / FishSize) * (1f + 0.35f * height);
                    shadow.SetPositionAndRotation(ground, Quaternion.Euler(0f, 0f, angle));
                    shadow.localScale = Vector3.one * (fishLength / FishSize) * (1f - 0.25f * height);
                }
                if (up != wasUp)
                {
                    // Splash where it leaves the water and where it dives back in.
                    Vector2 normal = track.Path.NormalAt(g.distance) * Mathf.Sign(g.side);
                    float reach = track.OffRoadAt(g.distance) + 2.2f;
                    splash.transform.position = track.Path.PointAt(g.distance) + normal * (up ? reach : -reach);
                    splash.Emit(24);
                }
                _fish[i] = (g, fish, shadow, splash, bubbles, up);
            }
            // Penguins waddle: tilting from side to side as they walk.
            foreach (var (line, birds) in _penguins)
            {
                track.PenguinPositions(line, Time.time, _penguinSpots);
                for (int k = 0; k < birds.Length && k < _penguinSpots.Count; k++)
                {
                    var (position, heading) = _penguinSpots[k];
                    float angle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg - 90f + Mathf.Sin(Time.time * 11f + k * 1.7f) * 12f;
                    birds[k].SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
                }
            }
            // Gummiboat squash after a bounce.
            var keys = new List<Transform>(_squash.Keys);
            foreach (var key in keys)
            {
                float t = _squash[key] + Time.deltaTime;
                _squash[key] = t;
                var boat = _boats.Find(b => b.t == key);
                float wobble = 1f - Mathf.Sin(Mathf.Min(1f, t / 0.35f) * Mathf.PI) * 0.18f;
                key.localScale = Vector3.one * boat.baseScale * wobble;
                if (t > 0.35f) _squash.Remove(key);
            }
        }

        float FishSize => fishSprite != null ? Mathf.Max(fishSprite.bounds.size.x, fishSprite.bounds.size.y) : 1f;

        void OnBounce(Vector2 position)
        {
            foreach (var boat in _boats)
                if ((boat.position - position).sqrMagnitude < 0.01f) _squash[boat.t] = 0f;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 position, float size, float angle, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            if (parent == null) _spawned.Add(go);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null)
            {
                float s = size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                go.transform.localScale = Vector3.one * s;
            }
            return sr;
        }

        ParticleSystem Particles(string name, Vector2 position, Vector2 direction, Color color, float speed, float lifetime, Vector2 size, float spread)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            _spawned.Add(go);
            go.transform.position = position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.8f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.85f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = spread;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = sortingOrder + 6;
            system.Play();
            return system;
        }
    }
}
