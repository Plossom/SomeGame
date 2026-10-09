using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Life in the sky over the race: flocks of birds flapping across the screen and hot-air balloons
    /// drifting over with their shadows on the ground. How busy it is comes from the track
    /// (<see cref="TrackLayout.skyLife"/>). Purely visual; follows the camera.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class SkyLife : MonoBehaviour
    {
        [SerializeField] Track track;
        [SerializeField] Sprite birdSprite;
        [SerializeField] Sprite balloonSprite;
        [SerializeField] int sortingOrder = 40;

        sealed class Flyer
        {
            public Transform transform;
            public Transform shadow;
            public Vector2 velocity;
            public float flap, life;
            public bool bird;
            public float baseScale;
        }

        readonly List<Flyer> _flyers = new();
        UnityEngine.Camera _camera;
        float _nextFlock, _nextBalloon;

        float Busy => track != null && track.Layout != null ? track.Layout.skyLife : 0.3f;

        void Awake() => _camera = GetComponent<UnityEngine.Camera>();

        void Start()
        {
            _nextFlock = Time.time + Random.Range(1f, 4f);
            _nextBalloon = Time.time + Random.Range(2f, 6f);
        }

        void Update()
        {
            float busy = Busy;
            if (busy > 0.01f && Time.time >= _nextFlock)
            {
                SpawnFlock();
                _nextFlock = Time.time + Random.Range(1f, 2f) * Mathf.Lerp(9f, 2f, busy);
            }
            int balloons = _flyers.FindAll(f => !f.bird).Count;
            if (busy > 0.01f && Time.time >= _nextBalloon && balloons < Mathf.RoundToInt(1f + 3f * busy))
            {
                SpawnBalloon();
                _nextBalloon = Time.time + Random.Range(4f, 8f) * Mathf.Lerp(2f, 0.8f, busy);
            }

            for (int i = _flyers.Count - 1; i >= 0; i--)
            {
                var f = _flyers[i];
                f.life -= Time.deltaTime;
                f.transform.position += (Vector3)(f.velocity * Time.deltaTime);
                if (f.shadow != null) f.shadow.position = f.transform.position + new Vector3(3f, -4f, 0f);
                if (f.bird)
                {
                    f.flap += Time.deltaTime * 11f;
                    float s = f.baseScale;
                    f.transform.localScale = new Vector3(s * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(f.flap))), s, 1f);
                }
                if (f.life <= 0f)
                {
                    Destroy(f.transform.gameObject);
                    if (f.shadow != null) Destroy(f.shadow.gameObject);
                    _flyers.RemoveAt(i);
                }
            }
        }

        // Enter just outside the view and fly across it.
        (Vector2 start, Vector2 dir, float distance) Route(float margin)
        {
            Vector2 centre = _camera.transform.position;
            float h = _camera.orthographicSize, w = h * _camera.aspect;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 start = centre - dir * (Mathf.Sqrt(w * w + h * h) + margin) + new Vector2(-dir.y, dir.x) * Random.Range(-h * 0.6f, h * 0.6f);
            return (start, dir, (Mathf.Sqrt(w * w + h * h) + margin) * 2f);
        }

        void SpawnFlock()
        {
            var (start, dir, distance) = Route(4f);
            float speed = Random.Range(6f, 9f);
            int count = Random.Range(3, 8);
            Vector2 side = new(-dir.y, dir.x);
            float heading = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            for (int k = 0; k < count; k++)
            {
                // V formation.
                int row = (k + 1) / 2;
                Vector2 offset = -dir * row * 1.2f + side * row * 1.1f * (k % 2 == 0 ? 1f : -1f);
                var t = Create("Bird", birdSprite, start + offset, 0.9f, heading, sortingOrder + 1);
                _flyers.Add(new Flyer { transform = t, velocity = dir * speed, flap = Random.value * 6f, life = distance / speed + 2f, bird = true, baseScale = t.localScale.x });
            }
        }

        void SpawnBalloon()
        {
            var (start, dir, distance) = Route(8f);
            float speed = Random.Range(1.2f, 2.2f);
            float size = Random.Range(5f, 7.5f);
            var t = Create("Balloon", balloonSprite, start, size, Random.Range(0f, 360f), sortingOrder);
            var shadow = Create("BalloonShadow", balloonSprite, start + new Vector2(3f, -4f), size * 0.9f, 0f, sortingOrder - 30);
            shadow.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.18f);
            _flyers.Add(new Flyer { transform = t, shadow = shadow, velocity = dir * speed, life = distance / speed + 2f, bird = false, baseScale = t.localScale.x });
        }

        Transform Create(string name, Sprite sprite, Vector2 position, float size, float angle, int order)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = Vector3.one * (size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            return go.transform;
        }
    }
}
