using System.Collections.Generic;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The garage: your car big on a turntable under a spotlight, its paint's name, and a swatch for every paint.
    /// Paints open with stars (a locked one shows the stars it needs and shakes when tapped); tap an open one and
    /// the car takes the new colour with a pop and a ring of light.
    /// </summary>
    public class GarageScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] UnityEngine.UI.Button closeArea;
        [SerializeField] RectTransform car;
        [SerializeField] UnityEngine.UI.Image carBody;
        [SerializeField] UnityEngine.UI.Image burst;
        [SerializeField] TMP_Text paintName;
        [SerializeField] TMP_Text starsLabel;
        [SerializeField] RectTransform swatchTemplate;

        readonly List<RectTransform> _swatches = new();
        Color _from, _to;
        float _changedAt = -10f, _messageUntil, _shakeAt = -10f;
        int _shake = -1;

        void Awake()
        {
            closeButton.onClick.AddListener(Close);
            closeArea.onClick.AddListener(Close);
            swatchTemplate.gameObject.SetActive(false);
            for (int i = 0; i < Garage.Paints.Length; i++)
            {
                var swatch = Instantiate(swatchTemplate, swatchTemplate.parent);
                swatch.gameObject.SetActive(true);
                swatch.name = Garage.Paints[i].Name;
                swatch.Find("Face/Color").GetComponent<UnityEngine.UI.Image>().color = Garage.Paints[i].Color;
                int paint = i;
                swatch.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Pick(paint));
                _swatches.Add(swatch);
            }
            panel.SetActive(false);
        }

        public void Open()
        {
            Garage.MarkSeen();
            _from = _to = Garage.Color;
            carBody.color = _to;
            _messageUntil = 0f;
            Refresh();
            PopIn.Show(panel);
        }

        public void Close() => PopIn.Hide(panel);

        void Refresh()
        {
            int selected = Garage.Selected;
            starsLabel.text = $"{ProgressStore.TotalStars} STARS COLLECTED";
            if (Time.unscaledTime >= _messageUntil)
            {
                paintName.text = Garage.Paints[selected].Name;
                paintName.color = Theme.Ink;
            }
            for (int i = 0; i < _swatches.Count; i++)
            {
                bool open = Garage.IsUnlocked(i);
                var s = _swatches[i];
                s.Find("Ring").gameObject.SetActive(i == selected);
                s.Find("Face/Lock").gameObject.SetActive(!open);
                s.Find("Face/Color").GetComponent<UnityEngine.UI.Image>().color = open ? Garage.Paints[i].Color : Color.Lerp(Garage.Paints[i].Color, Theme.Stone, 0.65f);
                var need = s.Find("Need");
                need.gameObject.SetActive(!open);
                need.Find("Count").GetComponent<TMP_Text>().text = Garage.Paints[i].Stars.ToString();
            }
        }

        void Pick(int paint)
        {
            if (!Garage.IsUnlocked(paint))
            {
                // Not yet: it shakes, and says what it takes.
                _shake = paint;
                _shakeAt = Time.unscaledTime;
                paintName.text = $"COLLECT {Garage.Paints[paint].Stars} STARS";
                paintName.color = Theme.Rust;
                _messageUntil = Time.unscaledTime + 1.4f;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.4f);
                return;
            }
            _messageUntil = 0f;
            if (paint == Garage.Selected) return;
            _from = carBody.color;
            Garage.Selected = paint;
            _to = Garage.Color;
            _changedAt = Time.unscaledTime;
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.star, 0.6f);
            Refresh();
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            float t = Time.unscaledTime;
            if (_messageUntil > 0f && t >= _messageUntil) { _messageUntil = 0f; Refresh(); }
            // The car floats gently on its turntable; a new paint flows over it with a pop and a ring of light.
            float k = Mathf.Clamp01((t - _changedAt) / 0.35f);
            carBody.color = Color.Lerp(_from, _to, k);
            float pop = 1f + 0.12f * Mathf.Sin(k * Mathf.PI);
            car.localScale = Vector3.one * pop;
            car.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 1.6f) * 10f);
            car.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 3f);
            float ring = Mathf.Clamp01((t - _changedAt) / 0.5f);
            burst.gameObject.SetActive(ring < 1f);
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.6f, 1f - (1f - ring) * (1f - ring));
            burst.color = Theme.WithAlpha(Theme.Amber, 0.85f * (1f - ring));
            for (int i = 0; i < _swatches.Count; i++)
            {
                float s = i == _shake ? Mathf.Clamp01((t - _shakeAt) / 0.4f) : 1f;
                var face = _swatches[i].Find("Face") as RectTransform;
                if (face != null) face.anchoredPosition = new Vector2(Mathf.Sin(s * Mathf.PI * 6f) * 14f * (1f - s), face.anchoredPosition.y);
                _swatches[i].localScale = Vector3.one * (i == Garage.Selected ? 1f + 0.03f * Mathf.Sin(t * 3f) : 1f);
            }
        }
    }
}
