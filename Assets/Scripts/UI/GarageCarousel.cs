using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SomeGame.UI
{
    /// <summary>
    /// The garage's row of big cards: the one in the middle big and upright, the ones beside it smaller, tilted
    /// and dimmed, further ones tucked behind. Swipe to glide through them (it settles on a card with a spring,
    /// ticking as cards pass the middle); tap a card at the side to bring it to the middle.
    /// </summary>
    public class GarageCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] float spacing = 400f;
        [SerializeField] float sideScale = 0.74f, sideTilt = 8f, sideDrop = 40f;

        const float Omega = 13f;
        readonly List<RectTransform> _cards = new();
        readonly List<CanvasGroup> _groups = new();
        readonly List<RectTransform> _order = new();
        float _pos, _vel, _dragFrom, _dx;
        int _target, _tick = -1;
        bool _dragging;
        Canvas _canvas;

        /// <summary>The card in the middle changed (also while gliding past).</summary>
        public event Action<int> FocusChanged;

        public int Focused => _target;
        public bool Dragging => _dragging;

        void Awake() => _canvas = GetComponentInParent<Canvas>()?.rootCanvas;

        public void Set(List<RectTransform> cards, int focus)
        {
            _cards.Clear(); _groups.Clear();
            _cards.AddRange(cards);
            foreach (var card in cards)
            {
                if (!card.TryGetComponent(out CanvasGroup group)) group = card.gameObject.AddComponent<CanvasGroup>();
                _groups.Add(group);
            }
            _pos = _target = _tick = Mathf.Clamp(focus, 0, Mathf.Max(0, cards.Count - 1));
            _vel = 0f;
            Layout();
            FocusChanged?.Invoke(_target);
        }

        public void FlyTo(int index)
        {
            if (_dragging || _cards.Count == 0) return;
            _target = Mathf.Clamp(index, 0, _cards.Count - 1);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _dragging = true;
            _dragFrom = _pos;
            _dx = 0f;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging) return;
            float step = e.delta.x / (_canvas != null ? _canvas.scaleFactor : 1f);
            float dt = Mathf.Max(1e-4f, Time.unscaledDeltaTime);
            _dx += step;
            float raw = _dragFrom - _dx / spacing, last = _cards.Count - 1;
            if (raw < 0f) raw = -Rubber(-raw);
            else if (raw > last) raw = last + Rubber(raw - last);
            _vel = Mathf.Lerp(_vel, (raw - _pos) / dt, 0.5f);
            _pos = raw;
        }

        public void OnEndDrag(PointerEventData e)
        {
            _dragging = false;
            float projected = _pos + Mathf.Clamp(_vel, -14f, 14f) * 0.2f;
            _target = Mathf.Clamp(Mathf.RoundToInt(projected), 0, _cards.Count - 1);
        }

        static float Rubber(float x) => 0.3f * (1f - Mathf.Exp(-x / 0.3f));

        void Update()
        {
            if (_cards.Count == 0) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (!_dragging)
            {
                float a = -Omega * Omega * (_pos - _target) - 2f * Omega * _vel;
                _vel += a * dt;
                _pos += _vel * dt;
                if (Mathf.Abs(_pos - _target) < 0.001f && Mathf.Abs(_vel) < 0.01f) { _pos = _target; _vel = 0f; }
            }
            int near = Mathf.Clamp(Mathf.RoundToInt(_pos), 0, _cards.Count - 1);
            if (near != _tick)
            {
                _tick = near;
                if (_dragging) _target = near;
                SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.click, 0.3f);
                FocusChanged?.Invoke(near);
            }
            Layout();
        }

        void Layout()
        {
            float lean = Mathf.Clamp(_vel * 3f, -7f, 7f);
            _order.Clear();
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                float off = i - _pos, a = Mathf.Abs(off), side = Mathf.Sign(off);
                bool show = a < 3.2f;
                if (card.gameObject.activeSelf != show) card.gameObject.SetActive(show);
                if (!show) continue;
                _order.Add(card);
                float near = Mathf.Min(a, 1f);
                float x = side * (a <= 1f ? a * spacing : spacing + (a - 1f) * spacing * 0.42f);
                card.anchoredPosition = new Vector2(x, -sideDrop * Mathf.Min(a, 1.6f));
                card.localScale = Vector3.one * (Mathf.Lerp(1f, sideScale, near) - Mathf.Max(0f, a - 1f) * 0.07f);
                card.localRotation = Quaternion.Euler(0f, 0f, -off * sideTilt * Mathf.Min(1f, a) / Mathf.Max(a, 0.001f) * Mathf.Min(a, 1.4f) + lean * (1f - near));
                _groups[i].alpha = Mathf.Clamp01(3.2f - a) * Mathf.Lerp(1f, 0.82f, near);
            }
            // The middle card on top, the others behind it by their distance.
            _order.Sort((p, q) => Mathf.Abs(_cards.IndexOf(q) - _pos).CompareTo(Mathf.Abs(_cards.IndexOf(p) - _pos)));
            for (int i = 0; i < _order.Count; i++) _order[i].SetSiblingIndex(i);
        }
    }
}
