using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// One of Rosie's runaway chickens (<see cref="ChickenChase"/>): it pecks about on the road; when a car comes
    /// close it flaps off along the road (not as fast as a car, and never off the road), then settles again. Driven
    /// into, it is caught: a puff of feathers and it is gone.
    /// </summary>
    public class Chicken : MonoBehaviour
    {
        const float Size = 1.35f, CatchRadius = 1.7f, Scare = 9f, RunSpeed = 7.5f, RunSeconds = 1.1f;

        SomeGame.Track.Track _track;
        Transform _car;
        Transform _body;
        SpriteRenderer _sprite;
        float _distance, _lateral, _runUntil, _phase;
        Vector2 _runDirection;
        System.Action<Chicken> _caught;
        bool _done;

        public static Chicken Spawn(SomeGame.Track.Track track, float distance, float lateral, Sprite sprite, Transform car, System.Action<Chicken> caught)
        {
            var go = new GameObject("Chicken");
            var chicken = go.AddComponent<Chicken>();
            chicken._track = track;
            chicken._distance = distance;
            chicken._lateral = lateral;
            chicken._car = car;
            chicken._caught = caught;
            chicken._phase = Random.value * 10f;
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(go.transform, false);
            shadow.sprite = sprite;
            shadow.color = new Color(0f, 0f, 0f, 0.3f);
            shadow.sortingOrder = 6;
            shadow.transform.localPosition = new Vector3(0.1f, -0.12f, 0f);
            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(go.transform, false);
            body.sprite = sprite;
            body.sortingOrder = 7;
            chicken._body = body.transform;
            chicken._sprite = body;
            float scale = Size / Mathf.Max(0.01f, sprite.bounds.size.x);
            go.transform.localScale = Vector3.one * scale;
            chicken.Place();
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            return chicken;
        }

        void Place()
        {
            var path = _track.Path;
            float half = _track.HalfWidthAt(_distance) * 0.8f;
            _lateral = Mathf.Clamp(_lateral, -half, half);
            transform.position = path.PointAt(_distance) + path.NormalAt(_distance) * _lateral;
        }

        void Update()
        {
            if (_done || _car == null) return;
            float t = Time.time;
            Vector2 me = transform.position, car = _car.position;
            float away = Vector2.Distance(me, car);
            if (away < CatchRadius) { Catch(); return; }
            // Scared: off along the road, away from the car, with a little sideways dodge.
            if (away < Scare && t > _runUntil)
            {
                var path = _track.Path;
                Vector2 along = path.TangentAt(_distance);
                float sign = Vector2.Dot(me - car, along) >= 0f ? 1f : -1f;
                _runDirection = (along * sign + path.NormalAt(_distance) * Random.Range(-0.5f, 0.5f)).normalized;
                _runUntil = t + RunSeconds;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.penguin, 0.35f);
            }
            if (t < _runUntil)
            {
                var path = _track.Path;
                Vector2 step = _runDirection * RunSpeed * Time.deltaTime;
                _distance = path.WrapDistance(_distance + Vector2.Dot(step, path.TangentAt(_distance)));
                _lateral += Vector2.Dot(step, path.NormalAt(_distance));
                Place();
                float heading = Mathf.Atan2(_runDirection.y, _runDirection.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, 0f, heading), 1f - Mathf.Exp(-14f * Time.deltaTime));
                // Flapping: hops and wing beats.
                float flap = Mathf.Abs(Mathf.Sin((t + _phase) * 22f));
                _body.localScale = new Vector3(1f + 0.25f * flap, 1f, 1f);
                _body.localPosition = new Vector3(0f, 0.1f * flap, 0f);
            }
            else
            {
                // Pecking.
                float peck = Mathf.Max(0f, Mathf.Sin((t + _phase) * 3f)) ;
                _body.localScale = Vector3.one * (1f - 0.06f * peck);
                _body.localPosition = new Vector3(0f, 0.05f * peck, 0f);
            }
        }

        void Catch()
        {
            _done = true;
            Feathers(transform.position);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.7f);
            _caught?.Invoke(this);
            Destroy(gameObject);
        }

        static void Feathers(Vector3 at)
        {
            var go = new GameObject("Feathers");
            go.transform.position = at;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.duration = 0.3f; main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.93f, 0.9f, 0.82f));
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.3f;
            var drag = system.limitVelocityOverLifetime;
            drag.enabled = true; drag.drag = 5f;
            var spin = system.rotationOverLifetime;
            spin.enabled = true; spin.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            var colour = system.colorOverLifetime;
            colour.enabled = true; colour.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var drift = Object.FindAnyObjectByType<DriftEffects>();
            if (drift != null) renderer.sharedMaterial = drift.ParticleMaterial;
            renderer.sortingOrder = 15;
            system.Play();
        }
    }
}
