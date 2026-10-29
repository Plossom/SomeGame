using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// The town's life on Christmas Eve (Play mode): Santa's sleigh flying over now and then, dropping presents that
    /// fall onto the road (their shadows grow, they land as gift boxes you can knock about), and snowball fights
    /// (<see cref="TrackLayout.snowballFights"/>): kids either side of the road throwing snowballs across it - their
    /// shadows show where they fly; one that hits you slows you down (SPLAT!).
    /// </summary>
    public class TrackTown : MonoBehaviour
    {
        [SerializeField] Track track;
        [SerializeField] Sprite sleighSprite, giftSprite, snowballSprite;
        [SerializeField] Material particleMaterial;

        const float SleighLength = 14f, FallTime = 1.3f;
        static readonly Color[] Tints = { new(0.9f, 0.2f, 0.22f), new(0.2f, 0.62f, 0.35f), new(0.25f, 0.45f, 0.9f), new(0.95f, 0.75f, 0.2f), new(0.75f, 0.3f, 0.8f) };

        Transform _sleigh, _sleighShadow;
        Vector2 _sleighFrom, _sleighTo;
        float _sleighStart = -100f, _nextSleigh;
        readonly List<(float at, Vector2 target, Transform box, Transform shadow, bool landed)> _falling = new();
        readonly List<Rigidbody2D> _presents = new();
        int _nextPresent;
        float _dropFrom, _dropTo;

        class Ball { public Vector2 From, To; public float Start, Duration; public Transform Visual, Shadow; public bool Done; }
        readonly List<Ball> _balls = new();
        readonly List<float> _nextThrow = new();
        int _throwSide = 1;
        ParticleSystem _puff;
        SomeGame.Car.CarMovement[] _cars;
        SomeGame.UI.ComboDisplay _combo;
        SomeGame.Race.RaceManager _race;
        float _splatAt = -10f;

        void Start()
        {
            if (track == null) track = FindAnyObjectByType<Track>();
            var layout = track != null ? track.Layout : null;
            if (layout == null || (!layout.santa && (layout.snowballFights == null || layout.snowballFights.Count == 0))) { enabled = false; return; }
            _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
            _race = FindAnyObjectByType<SomeGame.Race.RaceManager>();
            _puff = Puff();
            if (layout.santa) SetUpSleigh(layout);
            if (layout.snowballFights != null) foreach (var _ in layout.snowballFights) _nextThrow.Add(Time.time + Random.Range(1f, 2f));
        }

        void OnDestroy()
        {
            foreach (Transform child in transform) if (child != null) Destroy(child.gameObject);
        }

        // ------------------------------------------------------------------ Santa

        void SetUpSleigh(TrackLayout layout)
        {
            var path = track.Path;
            // He drops presents where the loose gifts lie (the market street), or on the first stretch.
            _dropFrom = 10f; _dropTo = 80f;
            if (layout.gifts != null && layout.gifts.Count > 0)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var g in layout.gifts) { float d = path.Project(g).Distance; lo = Mathf.Min(lo, d); hi = Mathf.Max(hi, d); }
                _dropFrom = lo; _dropTo = hi;
            }
            var sr = Sprite("SantaSleigh", sleighSprite, 72);
            _sleigh = sr.transform;
            var shadow = Sprite("SantaShadow", sleighSprite, 3);
            shadow.color = new Color(0f, 0.02f, 0.1f, 0.22f);
            _sleighShadow = shadow.transform;
            _sleigh.gameObject.SetActive(false);
            _sleighShadow.gameObject.SetActive(false);
            for (int i = 0; i < 6; i++)
            {
                var box = Sprite("Present", giftSprite, 8);
                box.color = Tints[i % Tints.Length];
                box.transform.localScale = Vector3.one * Scale(giftSprite, 1.4f);
                var body = box.gameObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f; body.mass = 0.25f; body.linearDamping = 2.5f; body.angularDamping = 2f;
                box.gameObject.AddComponent<BoxCollider2D>().size = giftSprite != null ? giftSprite.bounds.size * 0.8f : Vector2.one;
                box.gameObject.SetActive(false);
                _presents.Add(body);
            }
            _nextSleigh = Time.time + 8f;
        }

        void UpdateSleigh()
        {
            if (_sleigh == null) return;
            var path = track.Path;
            if (Time.time >= _nextSleigh)
            {
                // Across the town from one side to the other, over the drop stretch.
                float mid = Mathf.Lerp(_dropFrom, _dropTo, 0.5f);
                Vector2 centre = path.PointAt(mid), across = path.NormalAt(mid);
                Vector2 dir = (path.TangentAt(mid) * 0.6f + across * (Random.value < 0.5f ? 1f : -1f)).normalized;
                _sleighFrom = centre - dir * 90f;
                _sleighTo = centre + dir * 90f;
                _sleighStart = Time.time;
                _nextSleigh = Time.time + Random.Range(18f, 26f);
                // Three presents, each dropped as he passes over its spot.
                for (int k = 0; k < 3; k++)
                {
                    float d = Mathf.Lerp(_dropFrom, _dropTo, Random.value);
                    Vector2 target = path.PointAt(d) + path.NormalAt(d) * Random.Range(-3f, 3f);
                    float along = Vector2.Dot(target - _sleighFrom, dir) / 180f;
                    var box = Sprite("FallingPresent", giftSprite, 71);
                    box.color = Tints[(k + _nextPresent) % Tints.Length];
                    var shadow = Sprite("PresentShadow", giftSprite, 3);
                    shadow.color = new Color(0f, 0.02f, 0.1f, 0.3f);
                    box.gameObject.SetActive(false); shadow.gameObject.SetActive(false);
                    _falling.Add((_sleighStart + Mathf.Clamp01(along) * Flight - FallTime * 0.3f, target, box.transform, shadow.transform, false));
                }
            }
            float t = (Time.time - _sleighStart) / Flight;
            bool flying = t >= 0f && t <= 1f;
            _sleigh.gameObject.SetActive(flying);
            _sleighShadow.gameObject.SetActive(flying);
            if (flying)
            {
                Vector2 p = Vector2.Lerp(_sleighFrom, _sleighTo, t), d = (_sleighTo - _sleighFrom).normalized;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f + Mathf.Sin(Time.time * 2f) * 3f;
                // High up: bigger, its shadow far below and to the side.
                _sleigh.SetPositionAndRotation(p + new Vector2(-3f, 5f), Quaternion.Euler(0f, 0f, angle));
                _sleigh.localScale = Vector3.one * Scale(sleighSprite, 6f) * 1.25f;
                _sleighShadow.SetPositionAndRotation(p + new Vector2(5f, -7f), Quaternion.Euler(0f, 0f, angle));
                _sleighShadow.localScale = Vector3.one * Scale(sleighSprite, 6f) * 0.9f;
            }
            // Presents falling: the box shrinks onto its shadow, which grows; then it lands as a gift box.
            for (int i = _falling.Count - 1; i >= 0; i--)
            {
                var f = _falling[i];
                float u = (Time.time - f.at) / FallTime;
                if (u < 0f) continue;
                f.box.gameObject.SetActive(true); f.shadow.gameObject.SetActive(true);
                float h = 1f - Mathf.Clamp01(u);
                f.box.position = f.target + new Vector2(-1.5f, 2.5f) * h * 4f;
                f.box.rotation = Quaternion.Euler(0f, 0f, u * 400f);
                f.box.localScale = Vector3.one * Scale(giftSprite, 1.4f) * (1f + h * 1.2f);
                f.shadow.position = f.target;
                f.shadow.localScale = Vector3.one * Scale(giftSprite, 1.4f) * Mathf.Lerp(0.4f, 1f, 1f - h);
                if (u < 1f) continue;
                Destroy(f.box.gameObject); Destroy(f.shadow.gameObject);
                _falling.RemoveAt(i);
                var body = _presents[_nextPresent++ % _presents.Count];
                body.gameObject.SetActive(true);
                body.position = f.target;
                body.linearVelocity = Vector2.zero;
                body.rotation = Random.Range(0f, 360f);
                if (_puff != null) { _puff.transform.position = f.target; _puff.Emit(10); }
            }
        }

        float Flight => 7f;

        // ------------------------------------------------------------------ snowball fights

        void UpdateSnowballs()
        {
            var fights = track.Layout.snowballFights;
            if (fights == null) return;
            var path = track.Path;
            for (int i = 0; i < fights.Count; i++)
            {
                if (Time.time < _nextThrow[i]) continue;
                _nextThrow[i] = Time.time + Random.Range(0.45f, 0.9f);
                float d = Mathf.Lerp(fights[i].x, fights[i].y, Random.value);
                float reach = track.OffRoadAt(d) + 4f;
                _throwSide = -_throwSide;
                Vector2 from = path.PointAt(d) + path.NormalAt(d) * reach * _throwSide;
                float d2 = d + Random.Range(-4f, 4f);
                Vector2 to = path.PointAt(d2) - path.NormalAt(d2) * reach * _throwSide;
                var ball = Sprite("Snowball", snowballSprite, 20);
                ball.transform.localScale = Vector3.one * Scale(snowballSprite, 0.8f);
                var shadow = Sprite("SnowballShadow", snowballSprite, 3);
                shadow.color = new Color(0f, 0.02f, 0.1f, 0.3f);
                shadow.transform.localScale = Vector3.one * Scale(snowballSprite, 0.7f);
                _balls.Add(new Ball { From = from, To = to, Start = Time.time, Duration = Vector2.Distance(from, to) / 14f, Visual = ball.transform, Shadow = shadow.transform });
            }
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var b = _balls[i];
                float u = (Time.time - b.Start) / b.Duration;
                Vector2 ground = Vector2.Lerp(b.From, b.To, Mathf.Clamp01(u));
                float h = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI) * 0.6f;
                b.Visual.position = ground + new Vector2(-0.6f, 1f) * h;
                b.Shadow.position = ground;
                if (!b.Done)
                    foreach (var car in _cars)
                    {
                        if (car == null || car.IsAirborne || car.IsRiding) continue;
                        if ((car.Body.position - ground).sqrMagnitude > 1.2f * 1.2f) continue;
                        b.Done = true;
                        Splat(car, ground);
                        break;
                    }
                if (u < 1f && !b.Done) continue;
                Destroy(b.Visual.gameObject); Destroy(b.Shadow.gameObject);
                _balls.RemoveAt(i);
            }
        }

        void Splat(SomeGame.Car.CarMovement car, Vector2 at)
        {
            car.Body.linearVelocity *= 0.75f;
            if (_puff != null) { _puff.transform.position = at; _puff.Emit(14); }
            if (car.GetComponent<SomeGame.Input.PlayerCarInput>() == null || Time.time - _splatAt < 0.6f) return;
            _splatAt = Time.time;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.snowPoof, 0.8f);
            if (_combo != null) _combo.Banner("SPLAT!", new Color(0.85f, 0.95f, 1f));
        }

        void Update()
        {
            if (track == null || track.Layout == null) return;
            UpdateSleigh();
            UpdateSnowballs();
        }

        // ------------------------------------------------------------------ helpers

        static float Scale(Sprite sprite, float size) => sprite != null ? size / Mathf.Max(0.01f, sprite.bounds.size.x) : 1f;

        SpriteRenderer Sprite(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        ParticleSystem Puff()
        {
            var go = new GameObject("SnowPuff");
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = new Color(0.96f, 0.98f, 1f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true; col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = 21;
            system.Play();
            return system;
        }
    }
}
