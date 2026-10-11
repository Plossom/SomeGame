using SomeGame.Car;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Shows the player's tricks big on screen and chains them into combos: a drift released into a boost
    /// (DRIFT, SUPER DRIFT, ULTRA DRIFT by its charge), a jump (JUMP, BIG AIR, MEGA AIR by its air time)
    /// and a landed shortcut. Another trick within <see cref="comboWindow"/> seconds raises the combo
    /// (x2, x3...); spinning on oil, crashing or splashing into water breaks it. Each trick pops in
    /// tilted with a starburst, then floats up and fades; the combo counter punches on every step.
    /// </summary>
    public class ComboDisplay : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] RectTransform trickRoot;
        [SerializeField] TMP_Text trickLabel;
        [SerializeField] UnityEngine.UI.Image burst;
        [SerializeField] RectTransform comboRoot;
        [SerializeField] TMP_Text comboLabel;
        [Tooltip("Seconds after a trick in which the next one continues the combo.")]
        [SerializeField, Min(0.5f)] float comboWindow = 2.5f;
        [Tooltip("Seconds a trick stays on screen before it floats away.")]
        [SerializeField, Min(0.2f)] float hold = 1f;

        // Drift tiers (blue, orange, purple sparks), jumps, shortcuts.
        static readonly Color[] DriftColors = { Hex("4FA3FF"), Hex("4FA3FF"), Hex("FF9A3D"), Hex("B66BFF") };
        static readonly Color AirColor = Hex("FFD447");
        static readonly Color ShortcutColor = Hex("7EE07A");

        CarMovement _car;
        SomeGame.Track.TrackSensor _sensor;
        int _tierSeen;
        float _driftSeenAt = -10f;
        float _airTime;
        int _combo;
        float _lastTrickAt = -100f;
        float _trickShownAt = -100f, _comboPunchAt = -100f;
        Vector2 _trickHome;

        void Start()
        {
            _trickHome = trickRoot.anchoredPosition;
            trickRoot.gameObject.SetActive(false);
            comboRoot.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_car == null) return;
            _car.BoostStarted -= OnBoost;
            _car.Launched -= OnLaunch;
            _car.Landed -= OnLand;
            _car.Slipped -= Break;
            _car.Crashed -= OnCrash;
        }

        bool Bind()
        {
            if (_car != null) return true;
            if (race == null || race.Player == null) return false;
            _car = race.Player.GetComponent<CarMovement>();
            _sensor = race.Player.GetComponent<SomeGame.Track.TrackSensor>();
            if (_car == null) return false;
            _car.BoostStarted += OnBoost;
            _car.Launched += OnLaunch;
            _car.Landed += OnLand;
            _car.Slipped += Break;
            _car.Crashed += OnCrash;
            return true;
        }

        void Update()
        {
            if (!Bind()) return;
            // Remember the drift's charge: the boost event comes after the drift has ended.
            if (_car.IsDrifting)
            {
                _tierSeen = Mathf.Max(_tierSeen, _car.DriftTier);
                _driftSeenAt = Time.time;
            }
            else if (Time.time - _driftSeenAt > 0.25f) _tierSeen = 0;

            if (_combo > 0 && Time.time - _lastTrickAt > comboWindow) _combo = 0;
            Animate();
        }

        void OnBoost(float strength)
        {
            if (race.State != RaceState.Racing) return;
            bool fromDrift = Time.time - _driftSeenAt < 0.25f && _tierSeen > 0;
            if (fromDrift)
            {
                int tier = Mathf.Clamp(_tierSeen, 1, 3);
                Trick(tier == 3 ? "ULTRA DRIFT!" : tier == 2 ? "SUPER DRIFT!" : "DRIFT!", DriftColors[tier]);
                _tierSeen = 0;
            }
            else Trick("SHORTCUT!", ShortcutColor); // the boost for landing a corner cut on the road
        }

        void OnLaunch(float duration) => _airTime = duration;

        void OnLand()
        {
            float air = _airTime;
            _airTime = 0f;
            if (race.State != RaceState.Racing || air < 0.35f) return;
            // A splash is not a jump.
            if (_sensor != null && _sensor.Track != null && _sensor.Track.IsWater(_car.transform.position)) { Break(); return; }
            Trick(air >= 1.6f ? "MEGA AIR!" : air >= 0.9f ? "BIG AIR!" : "JUMP!", AirColor);
        }

        void OnCrash(float impact) => Break();

        void Break()
        {
            _combo = 0;
            comboRoot.gameObject.SetActive(false);
        }

        void Trick(string text, Color color)
        {
            _combo = Time.time - _lastTrickAt <= comboWindow ? _combo + 1 : 1;
            _lastTrickAt = Time.time;

            trickLabel.text = text;
            trickLabel.color = Color.white;
            trickLabel.enableVertexGradient = true;
            trickLabel.colorGradient = new VertexGradient(Color.Lerp(color, Color.white, 0.55f), Color.Lerp(color, Color.white, 0.55f), color, color);
            burst.color = Theme.WithAlpha(color, 0.9f);
            trickRoot.gameObject.SetActive(true);
            _trickShownAt = Time.time;

            if (_combo >= 2)
            {
                comboLabel.text = $"COMBO <size=140%>x{_combo}</size>";
                comboRoot.gameObject.SetActive(true);
                _comboPunchAt = Time.time;
            }
        }

        void Animate()
        {
            // The trick: pops in with an overshoot, holds, then floats up and fades.
            if (trickRoot.gameObject.activeSelf)
            {
                float t = Time.time - _trickShownAt;
                float pop = t < 0.28f ? Overshoot(t / 0.28f) : 1f;
                float leave = Mathf.Clamp01((t - hold) / 0.45f);
                trickRoot.localScale = Vector3.one * pop * Mathf.Lerp(1f, 0.85f, leave);
                trickRoot.anchoredPosition = _trickHome + new Vector2(0f, 60f * leave * leave);
                trickRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-10f, -5f, Mathf.Clamp01(t / 0.28f)));
                trickLabel.alpha = 1f - leave;
                burst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.6f, Mathf.Clamp01(t / 0.5f));
                var c = burst.color;
                c.a = 0.9f * (1f - Mathf.Clamp01(t / 0.6f));
                burst.color = c;
                if (leave >= 1f) trickRoot.gameObject.SetActive(false);
            }

            // The combo counter: punches on each step, fades when the window runs out.
            if (comboRoot.gameObject.activeSelf)
            {
                float t = Time.time - _comboPunchAt;
                float punch = t < 0.2f ? 1f + 0.35f * Mathf.Sin(t / 0.2f * Mathf.PI) : 1f;
                comboRoot.localScale = Vector3.one * punch;
                float left = comboWindow - (Time.time - _lastTrickAt);
                comboLabel.alpha = Mathf.Clamp01(left / 0.4f);
                if (left <= 0f || _combo < 2) comboRoot.gameObject.SetActive(false);
            }
        }

        // 0..1 to a scale that overshoots past 1 and settles.
        static float Overshoot(float x)
        {
            const float s = 2.2f;
            x -= 1f;
            return 1f + x * x * ((s + 1f) * x + s);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
