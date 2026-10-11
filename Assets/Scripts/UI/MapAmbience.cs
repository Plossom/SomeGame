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

        readonly List<Action<float>> _animations = new();

        public void Play(CupLook look)
        {
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
                    rt.localScale = new Vector3(1f, Mathf.Sin(t * 9f + flapShift) * 0.9f + 0.1f, 1f);
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

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
