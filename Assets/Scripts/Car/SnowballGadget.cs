using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// The SNOWBALL CANNON gadget (Sven Snowdrift's): fires a big snowball straight ahead; the first car it hits
    /// spins out in a burst of snow. Yours from the garage has one shot a lap (the gadget button in the HUD). Sven
    /// (<see cref="Gadget.Ai"/>) fires when somebody is just ahead of him, in his sights, twice a lap at most.
    /// </summary>
    public class SnowballGadget : Gadget
    {
        public override string Id => "snowball";
        public override string Shout => "FIRE!";

        protected override void Fire()
        {
            Vector2 forward = transform.up;
            Snowball.Fire(Car, Sprite, (Vector2)transform.position + forward * 2.2f, Car.Body.linearVelocity + forward * Snowball.Speed);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.snowPoof, 0.6f);
        }

        // Sven: somebody a little way ahead, right in front of the cannon: fire!
        protected override void Think()
        {
            if (Race.RaceTime < 6f || Time.time - LastUse < 6f || !Ready) return;
            float me = Progress.Metres;
            foreach (var other in Race.Standings)
            {
                if (other == null || other == Progress || other.IsFinished) continue;
                float ahead = other.Metres - me;
                if (ahead < 5f || ahead > 28f) continue;
                Vector2 toThem = other.transform.position - transform.position;
                if (Vector2.Angle(transform.up, toThem) > 10f) continue;
                if (other == Race.Player || Random.value < 0.35f) { Use(); return; }
            }
        }
    }

    /// <summary>
    /// A snowball from the snowball cannon: it flies straight on in a low arc (its shadow apart in the middle),
    /// spinning; the first car it meets (not one in the air, nor the one that fired it) spins out like on oil, and
    /// it bursts into snow. Out of puff it lands and bursts too.
    /// </summary>
    public class Snowball : MonoBehaviour
    {
        public const float Speed = 30f;
        const float Life = 1.1f, Size = 1.25f;

        CarMovement _owner;
        Vector2 _velocity;
        float _firedAt;
        Transform _ball, _shadow;
        float _size;
        bool _burst;

        public static Snowball Fire(CarMovement owner, Sprite sprite, Vector2 at, Vector2 velocity)
        {
            var go = new GameObject("Snowball");
            go.transform.position = at;
            var ball = go.AddComponent<Snowball>();
            ball._owner = owner;
            ball._velocity = velocity;
            ball._firedAt = Time.time;
            ball._shadow = Child(go.transform, "Shadow", sprite, 13, new Color(0f, 0f, 0f, 0.3f));
            ball._ball = Child(go.transform, "Ball", sprite, 14, Color.white);
            ball._size = ball._ball.localScale.x;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = Size * 0.5f;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return ball;
        }

        static Transform Child(Transform parent, string name, Sprite sprite, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            if (sprite != null) go.transform.localScale = Vector3.one * (Size / Mathf.Max(0.01f, sprite.bounds.size.x));
            return go.transform;
        }

        void FixedUpdate()
        {
            if (_burst) return;
            GetComponent<Rigidbody2D>().MovePosition((Vector2)transform.position + _velocity * Time.fixedDeltaTime);
        }

        void Update()
        {
            if (_burst) return;
            float k = (Time.time - _firedAt) / Life;
            if (k >= 1f) { Burst(); return; }
            // A low arc: up and down again, a little bigger at the top, its shadow on the snow below.
            float lift = Mathf.Sin(k * Mathf.PI);
            _ball.localPosition = new Vector3(0f, lift * 0.5f, 0f);
            _ball.localRotation = Quaternion.Euler(0f, 0f, -k * 720f);
            _ball.localScale = Vector3.one * (_size * (1f + 0.2f * lift));
            _shadow.localPosition = new Vector3(0.1f + lift * 0.35f, -0.12f - lift * 0.35f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other) => Hit(other);
        void OnTriggerStay2D(Collider2D other) => Hit(other);

        void Hit(Collider2D other)
        {
            if (_burst) return;
            var car = other.GetComponentInParent<CarMovement>();
            if (car == null || car == _owner || car.IsAirborne || car.IsPuppet) return;
            Vector2 side = transform.position - car.transform.position;
            car.Slip(Vector3.Cross(car.transform.up, side).z > 0f ? -1 : 1);
            Burst();
        }

        void Burst()
        {
            _burst = true;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.snowPoof, 0.9f);
            HayBale.Burst(transform.position, Color.white, new Color(0.78f, 0.88f, 0.97f));
            Destroy(gameObject);
        }
    }
}
