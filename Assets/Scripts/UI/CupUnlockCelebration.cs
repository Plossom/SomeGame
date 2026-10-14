using System.Collections;
using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The big moment when a new cup unlocks: the screen dims, the cup's badge drops in under a big lock,
    /// the lock shakes and bursts in a white flash, light rays spin up behind the badge, confetti rains
    /// down and "NEW CUP!" punches in with the cup's name. A tap on "LET'S GO" (or anywhere, once it is
    /// all in) closes it; <see cref="Play"/> waits until then.
    /// </summary>
    public class CupUnlockCelebration : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Image dim;
        [SerializeField] RectTransform rays;
        [SerializeField] UnityEngine.UI.Image raysImage;
        [SerializeField] RectTransform badge;
        [SerializeField] UnityEngine.UI.Image badgeImage;
        [SerializeField] RectTransform lockRt;
        [SerializeField] UnityEngine.UI.Image lockImage;
        [SerializeField] UnityEngine.UI.Image flash;
        [SerializeField] RectTransform titleRt;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text cupName;
        [SerializeField] UnityEngine.UI.Button goButton;
        [SerializeField] UnityEngine.UI.Button tapAnywhere;
        [SerializeField] RectTransform confettiLayer;
        [SerializeField] Sprite confettiSprite;
        [SerializeField, Min(0)] int confettiCount = 90;
        [Tooltip("Where the badge flies when you go on (the cup dropdown on the map).")]
        [SerializeField] RectTransform flyTarget;

        static readonly Color[] ConfettiColors =
        {
            new(1f, 0.48f, 0.1f), new(1f, 0.83f, 0.28f), new(0.17f, 0.58f, 0.91f), new(0.49f, 0.88f, 0.48f), new(0.9f, 0.3f, 0.6f), Color.white,
        };

        bool _done, _canClose;
        readonly List<(RectTransform rt, Vector2 velocity, float spin)> _confetti = new();

        public bool IsPlaying => panel.activeSelf;

        void Awake()
        {
            panel.SetActive(false);
            goButton.onClick.AddListener(Close);
            tapAnywhere.onClick.AddListener(() => { if (_canClose) Close(); });
        }

        void Close() => _done = true;

        public IEnumerator Play(CupDefinition cup)
        {
            _done = _canClose = false;
            panel.SetActive(true);
            badgeImage.sprite = cup.icon;
            cupName.text = cup.displayName;
            goButton.gameObject.SetActive(false);
            rays.gameObject.SetActive(false);
            title.gameObject.SetActive(false);
            cupName.gameObject.SetActive(false);
            lockRt.gameObject.SetActive(true);
            lockRt.localScale = Vector3.one;
            lockRt.localRotation = Quaternion.identity;
            lockImage.color = Color.white;
            flash.color = new Color(1f, 1f, 1f, 0f);
            badgeImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);

            // Dim in, the badge drops in with a bounce.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
            {
                dim.color = new Color(0f, 0f, 0f, 0.75f * t);
                badge.localScale = Vector3.one * EaseOutBack(t);
                yield return null;
            }
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            badge.localScale = Vector3.one;

            // The lock shakes harder and harder.
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.8f;
                lockRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 50f) * 18f * k);
                lockRt.localScale = Vector3.one * (1f + 0.08f * k);
                yield return null;
            }

            // Burst: white flash, the lock flies apart, the badge lights up, rays and confetti.
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.fanfare, 1f);
            rays.gameObject.SetActive(true);
            SpawnConfetti();
            title.gameObject.SetActive(true);
            cupName.gameObject.SetActive(true);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                flash.color = new Color(1f, 1f, 1f, 0.85f * (1f - t));
                lockRt.localScale = Vector3.one * (1.1f + 1.4f * t);
                lockImage.color = new Color(1f, 1f, 1f, 1f - t);
                badgeImage.color = Color.Lerp(new Color(0.55f, 0.55f, 0.55f, 1f), Color.white, t);
                badge.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
                titleRt.localScale = Vector3.one * EaseOutBack(t);
                cupName.alpha = t;
                raysImage.color = new Color(1f, 0.9f, 0.55f, 0.55f * t);
                yield return null;
            }
            lockRt.gameObject.SetActive(false);
            flash.color = new Color(1f, 1f, 1f, 0f);
            goButton.gameObject.SetActive(true);
            _canClose = true;

            // Celebrate until tapped.
            float shownButton = Time.unscaledTime;
            while (!_done)
            {
                goButton.transform.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01((Time.unscaledTime - shownButton) / 0.35f));
                yield return null;
            }

            // Out: the badge flies into the cup dropdown (it lives there from now on), the rest fades.
            Vector3 home = badge.position;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.55f)
            {
                float e = t * t * (3f - 2f * t);
                dim.color = new Color(0f, 0f, 0f, 0.75f * (1f - t));
                if (flyTarget != null)
                {
                    // An arc: up and over, then into the dropdown, spinning once.
                    Vector3 to = flyTarget.position;
                    Vector3 p = Vector3.Lerp(home, to, e);
                    p.y += Mathf.Sin(e * Mathf.PI) * (Screen.height * 0.08f);
                    badge.position = p;
                    badge.localScale = Vector3.one * Mathf.Lerp(1f, 0.18f, e);
                    badge.localRotation = Quaternion.Euler(0f, 0f, -360f * e);
                }
                else badge.localScale = Vector3.one * (1f - t);
                titleRt.localScale = Vector3.one * (1f - t);
                cupName.alpha = 1f - t;
                raysImage.color = new Color(1f, 0.9f, 0.55f, 0.55f * (1f - t));
                goButton.transform.localScale = Vector3.one * (1f - t);
                yield return null;
            }
            foreach (var c in _confetti) if (c.rt != null) Destroy(c.rt.gameObject);
            _confetti.Clear();
            panel.SetActive(false);
            badge.position = home;
            badge.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            float dt = Time.unscaledDeltaTime;
            if (rays.gameObject.activeSelf) rays.Rotate(0f, 0f, -25f * dt);
            for (int i = 0; i < _confetti.Count; i++)
            {
                var (rt, v, spin) = _confetti[i];
                if (rt == null) continue;
                v.y -= 900f * dt;
                v.x *= 1f - 0.8f * dt;
                rt.anchoredPosition += v * dt;
                rt.Rotate(0f, 0f, spin * dt);
                rt.localScale = new Vector3(1f, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f + i)) * 0.8f + 0.2f, 1f);
                _confetti[i] = (rt, v, spin);
            }
        }

        void SpawnConfetti()
        {
            for (int i = 0; i < confettiCount; i++)
            {
                var go = new GameObject("Confetti", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(confettiLayer, false);
                rt.sizeDelta = new Vector2(Random.Range(16f, 26f), Random.Range(28f, 44f));
                rt.anchoredPosition = new Vector2(Random.Range(-60f, 60f), Random.Range(0f, 80f));
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = confettiSprite;
                image.color = ConfettiColors[i % ConfettiColors.Length];
                image.raycastTarget = false;
                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float speed = Random.Range(900f, 1900f);
                _confetti.Add((rt, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, Random.Range(-500f, 500f)));
            }
        }

        static float EaseOutBack(float t)
        {
            const float s = 1.7f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((s + 1f) * t + s);
        }
    }
}
