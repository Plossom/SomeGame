using System;
using System.Collections.Generic;
using SomeGame.Race;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Brings the painted map to life, on top of the landscape and under the road progress, race stops
    /// and car. Both maps: clouds drifting through the sky, smoke curling from the chimneys and glints
    /// twinkling on the lake. The valley: a flock of birds now and then and a hot-air balloon floating by.
    /// The winter map: chairs riding the ski lift (its snowfall is <see cref="SnowFall"/>). Everything is
    /// placed in map canvas units, from the bottom left of the map picture.
    /// </summary>
    public class MapAmbience : MonoBehaviour
    {
        [SerializeField] Sprite cloud;
        [SerializeField] Sprite bird;
        [SerializeField] Sprite balloon;
        [SerializeField] Sprite sparkle;
        [SerializeField] Sprite puff;
        [SerializeField] Sprite chair;
        [SerializeField] Sprite fisher;
        [SerializeField] Sprite rowboat;
        [SerializeField] Sprite ring;
        [SerializeField] Sprite fish;

        readonly List<Action<float>> _animations = new();

        /// <summary>Removes everything that is playing (before switching to another cup's map).</summary>
        public void Clear()
        {
            _animations.Clear();
            foreach (Transform child in transform) Destroy(child.gameObject);
        }

        public void Play(CupLook look)
        {
            Clear();
            Clouds(look);
            Smoke(look);
            if (look == CupLook.Winter)
            {
                Glints(MapScenery.IceLake, MapScenery.IceLakeRadii, 7);
                LiftChairs();
            }
            else
            {
                Glints(MapScenery.ValleyLake, MapScenery.ValleyLakeRadii, 6);
                Birds();
                Balloon();
                Fishers();
            }
        }

        void Update()
        {
            float t = Time.time;
            foreach (var animate in _animations) animate(t);
        }

        RectTransform Make(string name, Sprite sprite, Vector2 size, Color color, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        static UnityEngine.UI.Image ImageOf(RectTransform rt) => rt.GetComponent<UnityEngine.UI.Image>();

        // Slow clouds across the sky, wrapping round.
        void Clouds(CupLook look)
        {
            var rows = new[] { (y: 2000f, width: 260f, speed: 9f, x: 200f), (y: 2150f, width: 330f, speed: 6f, x: 900f), (y: 2300f, width: 230f, speed: 11f, x: 520f) };
            Color tint = look == CupLook.Winter ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 0.98f, 0.94f, 0.95f);
            foreach (var row in rows)
            {
                var rt = Make("Cloud", cloud, new Vector2(row.width, row.width * 0.5f), tint, new Vector2(0.5f, 0.5f));
                var r = row;
                _animations.Add(t => rt.anchoredPosition = new Vector2(Mathf.Repeat(r.x + r.speed * t, MapLayout.Size.x + 2f * r.width) - r.width, r.y));
            }
        }

        // Three puffs per chimney, each rising, drifting, growing and fading, one after the other.
        void Smoke(CupLook look)
        {
            // Light grey-blue in the snow (white would vanish against it), warm white in the valley.
            Color tint = look == CupLook.Winter ? Hex("8FA2B8") : Hex("F4F2EC");
            const float period = 3.6f;
            int index = 0;
            foreach (var (top, scale) in MapScenery.Chimneys(look))
            {
                Vector2 origin = MapScenery.ToCanvas(top);
                float s = scale, offset = index++ * 0.37f;
                for (int k = 0; k < 3; k++)
                {
                    var rt = Make("Smoke", puff, Vector2.one, tint, new Vector2(0.5f, 0.5f));
                    var image = ImageOf(rt);
                    float phaseShift = k / 3f + offset;
                    _animations.Add(t =>
                    {
                        float p = Mathf.Repeat(t / period + phaseShift, 1f);
                        rt.anchoredPosition = origin + new Vector2(18f * s * p + Mathf.Sin(t * 1.3f + phaseShift * 6f) * 3f * s, 70f * s * p);
                        rt.sizeDelta = Vector2.one * Mathf.Lerp(16f, 44f, p) * s;
                        var c = tint;
                        c.a = Mathf.SmoothStep(0f, 1f, p / 0.05f) * Mathf.Pow(1f - p, 0.8f);
                        image.color = c;
                    });
                }
            }
        }

        // Glints popping up and fading at spots on the water.
        void Glints(Vector2 centre, Vector2 radii, int count)
        {
            var rng = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f, dist = Mathf.Sqrt((float)rng.NextDouble()) * 0.75f;
                Vector2 spot = MapScenery.ToCanvas(centre + new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y) * dist);
                float size = Mathf.Lerp(18f, 30f, (float)rng.NextDouble()) * Mathf.Lerp(1f, 0.7f, Mathf.InverseLerp(400f, 1300f, centre.y));
                float period = 2.2f + (float)rng.NextDouble() * 1.6f, shift = (float)rng.NextDouble();
                var rt = Make("Glint", sparkle, Vector2.one * size, Color.white, new Vector2(0.5f, 0.5f));
                rt.anchoredPosition = spot;
                _animations.Add(t =>
                {
                    float p = Mathf.Repeat(t / period + shift, 1f);
                    float k = p < 0.3f ? Mathf.Sin(p / 0.3f * Mathf.PI) : 0f; // a quick twinkle, then a pause
                    rt.localScale = Vector3.one * k;
                    rt.localRotation = Quaternion.Euler(0f, 0f, p * 90f);
                });
            }
        }

        // A small flock crossing the sky every so often, flapping.
        void Birds()
        {
            const float period = 20f, crossing = 13f;
            var flock = new[] { new Vector2(0f, 0f), new Vector2(-46f, 22f), new Vector2(-40f, -26f), new Vector2(-88f, 6f) };
            for (int i = 0; i < flock.Length; i++)
            {
                var rt = Make("Bird", bird, new Vector2(34f, 17f), Color.white, new Vector2(0.5f, 0.5f));
                Vector2 offset = flock[i];
                float flapShift = i * 0.9f;
                _animations.Add(t =>
                {
                    float p = Mathf.Repeat(t + 4f, period) / crossing;
                    if (p > 1f) { rt.anchoredPosition = new Vector2(-400f, 0f); return; }
                    float x = Mathf.Lerp(-120f, MapLayout.Size.x + 120f, p);
                    rt.anchoredPosition = new Vector2(x, 1905f + Mathf.Sin(p * 5f) * 18f) + offset;
                    // Flap by squashing the wings between full height and a third (never through zero:
                    // that made the birds vanish for a moment and blink).
                    rt.localScale = new Vector3(1f, Mathf.Lerp(0.35f, 1f, 0.5f + 0.5f * Mathf.Sin(t * 7f + flapShift)), 1f);
                });
            }
        }

        // A hot-air balloon drifting slowly across, bobbing.
        void Balloon()
        {
            var rt = Make("Balloon", balloon, new Vector2(62f, 85f), Color.white, new Vector2(0.5f, 0.5f));
            _animations.Add(t =>
            {
                float x = MapLayout.Size.x + 80f - Mathf.Repeat(t * 7f + 500f, MapLayout.Size.x + 160f);
                rt.anchoredPosition = new Vector2(x, 1915f + Mathf.Sin(t * 0.6f) * 12f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 2f);
            });
        }

        // Chairs riding up the lift cable, fading in and out at the stations.
        void LiftChairs()
        {
            MapScenery.LiftCable(out var low, out var high);
            Vector2 a = MapScenery.ToCanvas(low), b = MapScenery.ToCanvas(high);
            float s = MapScenery.LiftScale;
            const int count = 4;
            for (int i = 0; i < count; i++)
            {
                var rt = Make("Chair", chair, new Vector2(30f, 40f) * s, Color.white, new Vector2(0.5f, 1f));
                var image = ImageOf(rt);
                float shift = i / (float)count;
                _animations.Add(t =>
                {
                    float p = Mathf.Repeat(t * 0.03f + shift, 1f);
                    rt.anchoredPosition = Vector2.Lerp(a, b, p);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.7f + shift * 9f) * 4f);
                    image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Min(p, 1f - p) / 0.06f));
                });
            }
        }

        // Two anglers at the valley lake: one on a rock on the shore, one in a rowing boat bobbing on the
        // water. Their floats bob and ripple; now and then the rod jerks up and a fish leaps out on the line.
        void Fishers()
        {
            if (fisher == null || rowboat == null) return;
            // Positions in canvas units (the lake is painted around (240, 1110), stretched upward by 1.15).
            Angler(fisher, new Vector2(26f, 32.5f), new Vector2(0.5f, 0f), new Vector2(336f, 1158f), new Vector2(-4.1f, 16.3f), new Vector2(262f, 1214f), false, 0f, 13f);
            Angler(rowboat, new Vector2(60f, 37.5f), new Vector2(0.5f, 0.175f), new Vector2(180f, 1276f), new Vector2(-3.8f, 18.8f), new Vector2(128f, 1270f), true, 6.5f, 11f);
        }

        void Angler(Sprite sprite, Vector2 size, Vector2 pivot, Vector2 at, Vector2 hands, Vector2 bobberAt, bool boat, float phase, float period)
        {
            var ripple = Make("Ripple", ring, Vector2.one * 4f, Color.white, new Vector2(0.5f, 0.5f));
            var line = Make("Line", null, new Vector2(0.5f, 1f), new Color(1f, 1f, 1f, 0.75f), new Vector2(0.5f, 1f));
            var body = Make("Angler", sprite, size, Color.white, pivot);
            var rod = Make("Rod", null, new Vector2(1.3f, 22f), Hex("5A3A22"), new Vector2(0.5f, 0f));
            var bobber = Make("Float", puff, Vector2.one * 4.5f, Hex("E2483A"), new Vector2(0.5f, 0.5f));
            var catchFish = fish != null ? Make("Catch", fish, new Vector2(4.5f, 9f), Color.white, new Vector2(0.5f, 0.5f)) : null;
            var rippleImage = ImageOf(ripple);
            var fishImage = catchFish != null ? ImageOf(catchFish) : null;
            const float rest = 55f, rodLength = 22f;
            _animations.Add(t =>
            {
                float tilt = boat ? Mathf.Sin(t * 1.1f + phase) * 3f : 0f;
                Vector2 bodyAt = at + (boat ? new Vector2(0f, Mathf.Sin(t * 1.3f + phase) * 1.2f) : Vector2.zero);
                body.anchoredPosition = bodyAt;
                body.localRotation = Quaternion.Euler(0f, 0f, tilt);
                Vector2 grip = bodyAt + (Vector2)(Quaternion.Euler(0f, 0f, tilt) * hands);

                // The catch: the rod jerks up, the fish leaps from the float to the angler.
                float c = Mathf.Repeat(t + phase, period);
                float lift = c < 0.5f ? Mathf.Sin(c / 0.5f * Mathf.PI * 0.5f) : c < 1.4f ? 1f - (c - 0.5f) / 0.9f : 0f;
                float angle = rest - 32f * lift + tilt;
                rod.anchoredPosition = grip;
                rod.localRotation = Quaternion.Euler(0f, 0f, angle);
                Vector2 tip = grip + (Vector2)(Quaternion.Euler(0f, 0f, angle) * new Vector2(0f, rodLength));

                bool flying = c > 0.1f && c < 1.1f;
                float dip = c > period - 2.6f && c < period - 2.2f ? -1.3f : 0f; // a nibble before the bite
                Vector2 floatAt = bobberAt + new Vector2(0f, Mathf.Sin(t * 2.2f + phase) * 0.6f + dip);
                Vector2 end = floatAt;
                if (catchFish != null)
                {
                    catchFish.gameObject.SetActive(flying);
                    if (flying)
                    {
                        float u = (c - 0.1f) / 1f;
                        Vector2 from = bobberAt, to = grip + new Vector2(0f, 4f);
                        Vector2 p = Vector2.Lerp(from, to, u) + new Vector2(0f, Mathf.Sin(u * Mathf.PI) * 16f);
                        Vector2 v = (to - from) + new Vector2(0f, Mathf.Cos(u * Mathf.PI) * 16f * Mathf.PI);
                        catchFish.anchoredPosition = p;
                        catchFish.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f + Mathf.Sin(t * 30f) * 15f);
                        fishImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1.1f - c) / 0.15f));
                        end = p;
                    }
                }
                bobber.gameObject.SetActive(!flying);
                bobber.anchoredPosition = floatAt;

                // The line from the rod tip to the float (or the fish).
                Vector2 span = end - tip;
                line.anchoredPosition = tip;
                line.sizeDelta = new Vector2(0.5f, span.magnitude);
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg + 90f);

                // Ripples round the float, and a splash when the fish comes out.
                float r = c < 0.8f ? c / 0.8f : Mathf.Repeat((t + phase) / 2.4f, 1f);
                float big = c < 0.8f ? 2f : 1f;
                ripple.anchoredPosition = bobberAt;
                ripple.sizeDelta = new Vector2(Mathf.Lerp(3f, 14f * big, r), Mathf.Lerp(3f, 14f * big, r) * 0.45f);
                rippleImage.color = new Color(1f, 1f, 1f, 0.6f * (1f - r));
            });
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
