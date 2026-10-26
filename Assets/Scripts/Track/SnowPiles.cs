using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Heaps of snow on the road (<see cref="TrackLayout.snowPiles"/>). Blast through one and it bursts into a
    /// big cloud of powder: the car loses a little speed, the player gets SNOW SPLAT! (it counts in combos).
    /// A burst heap builds up again after a few seconds. Made by <see cref="TrackHazards"/>.
    /// </summary>
    public class SnowPiles : MonoBehaviour
    {
        const float RegrowAfter = 5f, RegrowTime = 0.8f, SpeedKept = 0.82f;

        class Pile
        {
            public Vector2 position;
            public float radius;
            public Transform visual;
            public float baseScale;
            public float burstAt = -100f;
            public bool Down => Time.time - burstAt < RegrowAfter;
        }

        readonly List<Pile> _piles = new();
        ParticleSystem _burst;
        SomeGame.Car.CarMovement[] _cars;
        SomeGame.UI.ComboDisplay _combo;

        public void Init(Track track, Sprite sprite, Material particles, int order)
        {
            var path = track.Path;
            foreach (var data in track.Layout.snowPiles)
            {
                Vector2 p = path.PointAt(data.distance) + path.NormalAt(data.distance) * data.lateral;
                var go = new GameObject("SnowPile");
                go.transform.SetParent(transform, false);
                go.transform.position = p;
                go.transform.rotation = Quaternion.Euler(0f, 0f, data.distance * 37f % 360f); // each heap a little different
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                float scale = sprite != null ? data.radius * 2.3f / sprite.bounds.size.x : 1f;
                go.transform.localScale = Vector3.one * scale;
                _piles.Add(new Pile { position = p, radius = data.radius, visual = go.transform, baseScale = scale });
            }
            _burst = CreateBurst(particles, order + 8);
        }

        void Start()
        {
            _cars = FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None);
            _combo = FindAnyObjectByType<SomeGame.UI.ComboDisplay>();
        }

        void FixedUpdate()
        {
            if (_cars == null) return;
            foreach (var pile in _piles)
            {
                if (pile.Down) continue;
                foreach (var car in _cars)
                {
                    if (car == null || car.IsAirborne || !car.isActiveAndEnabled) continue;
                    if (Vector2.Distance(car.Body.position, pile.position) > pile.radius + 0.9f) continue;
                    Burst(pile, car);
                    break;
                }
            }
        }

        void Update()
        {
            foreach (var pile in _piles)
            {
                float since = Time.time - pile.burstAt;
                float k = since < 0.12f ? 1f - since / 0.12f               // squashed flat in a blink
                        : since < RegrowAfter ? 0f                         // gone
                        : Mathf.SmoothStep(0f, 1f, (since - RegrowAfter) / RegrowTime); // builds up again
                pile.visual.localScale = Vector3.one * pile.baseScale * Mathf.Max(0.001f, k);
            }
        }

        void Burst(Pile pile, SomeGame.Car.CarMovement car)
        {
            pile.burstAt = Time.time;
            car.Body.linearVelocity *= SpeedKept;
            _burst.transform.position = pile.position;
            _burst.Emit(55);
            bool player = car.GetComponent<SomeGame.Input.PlayerCarInput>() != null;
            if (!player) return;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.snowPoof, 0.9f);
            if (_combo != null) _combo.Announce("SNOW SPLAT!", new Color(0.7f, 0.9f, 1f));
            var shake = FindAnyObjectByType<SomeGame.CameraRig.CameraShake>();
            if (shake != null) shake.Shake(0.25f);
        }

        ParticleSystem CreateBurst(Material material, int order)
        {
            var go = new GameObject("SnowPileBurst");
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
            main.startColor = new Color(0.96f, 0.98f, 1f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.5f;
            limit.dampen = 0.15f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 2f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            system.Play();
            return system;
        }
    }
}
