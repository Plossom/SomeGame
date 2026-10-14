using System;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    public enum MapNodeState { Hidden, Locked, Open, Won }

    /// <summary>
    /// One stop on the map road: a round badge with the race number (white with a hard shadow once
    /// won, orange when it is the next race, grey with a lock while short of stars, a dashed "?" while
    /// still hidden), the stars earned below and a pulsing ring when selected.
    /// </summary>
    public class MapNode : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image shadow;
        [SerializeField] UnityEngine.UI.Image face;
        [SerializeField] UnityEngine.UI.Image ring;
        [Tooltip("Dashed outline used while the race is still hidden.")]
        [SerializeField] UnityEngine.UI.Image dashedRing;
        [SerializeField] TMP_Text number;
        [SerializeField] UnityEngine.UI.Image lockIcon;
        [SerializeField] GameObject starsPill;
        [SerializeField] UnityEngine.UI.Image[] stars;
        [Tooltip("Trophy slot after the stars, filled once the race is won.")]
        [SerializeField] UnityEngine.UI.Image trophy;
        [Tooltip("Tag above a locked race showing the total stars it needs.")]
        [SerializeField] GameObject requirement;
        [SerializeField] TMP_Text requirementText;
        [Tooltip("Pulsing ring shown on the selected race.")]
        [SerializeField] GameObject selection;
        [SerializeField] TMP_Text caption;
        [Tooltip("Background of the caption, shown with it.")]
        [SerializeField] GameObject captionRoot;

        /// <summary>
        /// The race opens: the lock shakes, bursts away in a flash of sparkles, and the stop pops up in its
        /// open colours (<paramref name="open"/> rebinds it). Takes about a second.
        /// </summary>
        // Size: springs towards its target (bigger when selected, with a little bounce when it gets
        // selected) and pops in when the map opens.
        float _targetScale = 1f, _scale = 1f, _scaleVelocity, _appearAt = -1f;
        bool _scaled, _wasSelected, _manual;

        /// <summary>Pops the stop in (from nothing, with an overshoot) after <paramref name="delay"/> seconds.</summary>
        public void Appear(float delay)
        {
            _appearAt = Time.unscaledTime + delay;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            if (_manual) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _scaleVelocity += (_targetScale - _scale) * 320f * dt;
            _scaleVelocity *= Mathf.Exp(-16f * dt);
            _scale += _scaleVelocity * dt;
            float appear = 1f;
            if (_appearAt >= 0f)
            {
                float t = (Time.unscaledTime - _appearAt) / 0.38f;
                if (t >= 1f) _appearAt = -1f;
                appear = t <= 0f ? 0f : EaseOutBack(t);
            }
            transform.localScale = Vector3.one * (_scale * appear);
        }

        static float EaseOutBack(float t)
        {
            const float s = 2f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((s + 1f) * t + s);
        }

        public System.Collections.IEnumerator PlayUnlock(Sprite sparkle, Action open)
        {
            _manual = true;
            var lockT = lockIcon.rectTransform;
            Vector3 baseScale = transform.localScale;
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.5f;
                lockT.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 45f) * 16f * k);
                transform.localScale = baseScale * (1f + 0.06f * k * Mathf.Abs(Mathf.Sin(t * 30f)));
                yield return null;
            }
            // The burst: the lock flies up and fades, sparkles spray out.
            var burst = new GameObject("Burst", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var burstRt = (RectTransform)burst.transform;
            burstRt.SetParent(transform, false);
            burstRt.sizeDelta = Vector2.one * 150f;
            var burstImage = burst.GetComponent<UnityEngine.UI.Image>();
            burstImage.sprite = sparkle;
            burstImage.raycastTarget = false;
            var requirementTexts = requirement.GetComponentsInChildren<UnityEngine.UI.Graphic>();
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.unlock, 0.9f);
            bool opened = false;
            for (float t = 0f; t < 0.55f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.55f;
                if (k < 0.45f)
                {
                    float u = k / 0.45f;
                    lockT.localScale = Vector3.one * (1f + 0.8f * u);
                    lockT.anchoredPosition = new Vector2(0f, 22f + 30f * u);
                    lockIcon.color = Theme.WithAlpha(Theme.StoneDark, 1f - u);
                    foreach (var g in requirementTexts) { var c = g.color; c.a = 1f - u; g.color = c; }
                }
                else if (!opened)
                {
                    opened = true;
                    open?.Invoke(); // now an open race
                    baseScale = transform.localScale;
                }
                burstRt.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.6f, k);
                burstRt.localRotation = Quaternion.Euler(0f, 0f, k * 120f);
                burstImage.color = new Color(1f, 0.92f, 0.6f, 1f - k);
                if (opened)
                {
                    float u = Mathf.InverseLerp(0.45f, 1f, k);
                    transform.localScale = baseScale * (1f + 0.35f * Mathf.Sin(u * Mathf.PI) * (1f - u * 0.5f));
                }
                yield return null;
            }
            if (!opened) open?.Invoke();
            Destroy(burst);
            transform.localScale = baseScale;
            _scale = baseScale.x;
            _scaleVelocity = 0f;
            _manual = false;
            // Put the lock back as it was (hidden now; used again if the race locks after a reset).
            lockT.localRotation = Quaternion.identity;
            lockT.localScale = Vector3.one;
            lockT.anchoredPosition = new Vector2(0f, 22f);
            lockIcon.color = Theme.StoneDark;
            foreach (var g in requirementTexts) { var c = g.color; c.a = 1f; g.color = c; }
        }

        public void Bind(string label, MapNodeState state, int earnedStars, int starsRequired, bool selected, string captionText, Action onClick)
        {
            bool hidden = state == MapNodeState.Hidden;
            bool won = state == MapNodeState.Won;
            // A won race looks like any open race; its stars and trophy show the result.
            if (won) state = MapNodeState.Open;
            face.color = state switch
            {
                MapNodeState.Open => Theme.Orange,
                MapNodeState.Won => Theme.White,
                MapNodeState.Locked => Theme.Stone,
                _ => Theme.WithAlpha(Theme.Cream, 0.85f),
            };
            ring.gameObject.SetActive(!hidden);
            ring.color = state == MapNodeState.Open ? Theme.White : state == MapNodeState.Locked ? Theme.StoneDark : Theme.Ink;
            dashedRing.gameObject.SetActive(hidden);
            shadow.gameObject.SetActive(!hidden);
            shadow.color = state == MapNodeState.Open ? Theme.OrangeDeep : state == MapNodeState.Locked ? Theme.StoneDark : Theme.Ink;

            number.gameObject.SetActive(state != MapNodeState.Locked);
            number.text = hidden ? "?" : label;
            number.color = state == MapNodeState.Open ? Theme.White : hidden ? Theme.StoneDark : Theme.Ink;
            lockIcon.gameObject.SetActive(state == MapNodeState.Locked);

            // Always show the slots: empty stars and trophy as faint outlines, filled once earned.
            starsPill.SetActive(!hidden && state != MapNodeState.Locked); // a locked race shows the stars it needs instead
            var empty = Theme.WithAlpha(Theme.Stone, 0.3f);
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < earnedStars ? Theme.Amber : empty;
            if (trophy != null) trophy.color = won ? Theme.Amber : empty;

            requirement.SetActive(state == MapNodeState.Locked);
            requirementText.text = starsRequired.ToString();
            // A slightly deeper yellow than elsewhere, so it reads on the grey badge.
            var reqStar = requirement.transform.Find("Icon");
            if (reqStar != null) reqStar.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Theme.Amber, Theme.Orange, 0.45f);

            (captionRoot != null ? captionRoot : caption.gameObject).SetActive(!string.IsNullOrEmpty(captionText));
            caption.text = captionText;

            selection.SetActive(selected && !hidden);
            _targetScale = selected ? 1.15f : hidden ? 0.8f : 1f;
            if (!_scaled) { _scale = _targetScale; _scaled = true; if (!_manual && _appearAt < 0f) transform.localScale = Vector3.one * _scale; }
            else if (selected && !_wasSelected) _scaleVelocity += 3.2f; // a springy bounce when it gets selected
            _wasSelected = selected;

            button.interactable = !hidden;
            button.onClick.RemoveAllListeners();
            if (!hidden) button.onClick.AddListener(() => onClick());
        }
    }
}
