using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// A hay bale dropped on the road (<see cref="HayBaleGadget"/>): it thumps down with a bounce, and the next car to
    /// drive into it (not one flying over, nor the car that dropped it for a moment) spins out like on oil while the
    /// bale bursts into straw. Left alone it lies there a while, then fades.
    /// </summary>
    public class HayBale : MonoBehaviour
    {
        const float Life = 30f, Size = 1.7f;

        CarMovement _owner;
        float _droppedAt;
        SpriteRenderer _sprite, _shadow;
        bool _burst;

        public static HayBale Drop(CarMovement owner, Sprite sprite, Vector2 at)
        {
            var go = new GameObject("HayBale");
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            var bale = go.AddComponent<HayBale>();
            bale._owner = owner;
            bale._droppedAt = Time.time;
            bale._shadow = Child(go.transform, "Shadow", sprite, 6, new Color(0f, 0f, 0f, 0.35f));
            bale._shadow.transform.localPosition = new Vector3(0.12f, -0.16f, 0f);
            bale._sprite = Child(go.transform, "Bale", sprite, 7, Color.white);
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = Size * 0.42f;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            return bale;
        }

        static SpriteRenderer Child(Transform parent, string name, Sprite sprite, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            if (sprite != null) go.transform.localScale = Vector3.one * (Size / Mathf.Max(0.01f, sprite.bounds.size.x));
            return sr;
        }

        void Update()
        {
            float age = Time.time - _droppedAt;
            // Thumps down: from a little above (bigger, its shadow apart) with a squash.
            float fall = Mathf.Clamp01(age / 0.28f);
            float drop = 1f - fall * fall;
            float squash = age < 0.45f ? 1f + 0.12f * Mathf.Sin(Mathf.Clamp01((age - 0.28f) / 0.17f) * Mathf.PI) : 1f;
            _sprite.transform.localPosition = new Vector3(0f, drop * 0.6f, 0f);
            transform.localScale = Vector3.one * (1f + drop * 0.25f) * squash;
            float fade = Mathf.Clamp01((Life - age) / 1.5f);
            _sprite.color = new Color(1f, 1f, 1f, fade);
            _shadow.color = new Color(0f, 0f, 0f, 0.35f * fade);
            if (age >= Life) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other) => Hit(other);
        void OnTriggerStay2D(Collider2D other) => Hit(other);

        void Hit(Collider2D other)
        {
            if (_burst) return;
            var car = other.GetComponentInParent<CarMovement>();
            if (car == null || car.IsAirborne || car.IsPuppet) return;
            if (car == _owner && Time.time - _droppedAt < 1.5f) return;
            _burst = true;
            Vector2 side = (Vector2)(transform.position - car.transform.position);
            car.Slip(Vector3.Cross(car.transform.up, side).z > 0f ? -1 : 1);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.snowPoof, 0.9f);
            Straw(transform.position);
            Destroy(gameObject);
        }

        // A burst of straw.
        static void Straw(Vector3 at)
        {
            var go = new GameObject("StrawBurst");
            go.transform.position = at;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.8f, 0.36f), new Color(0.78f, 0.6f, 0.2f));
            main.gravityModifier = 0f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            var drag = system.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 4f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var colour = system.colorOverLifetime;
            colour.enabled = true;
            colour.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3f;
            var drift = Object.FindAnyObjectByType<DriftEffects>();
            if (drift != null) renderer.sharedMaterial = drift.ParticleMaterial;
            renderer.sortingOrder = 14;
            system.Play();
        }
    }
}
