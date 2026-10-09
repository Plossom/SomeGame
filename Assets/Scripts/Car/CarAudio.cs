using SomeGame.Audio;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// The car's sounds: an engine loop whose pitch follows speed and throttle, a tyre screech while
    /// drifting or sliding, and one-shots for boost, jump, landing, splash, oil spin and crashes.
    /// The player's car is heard directly; rivals are positional, so only nearby ones are loud.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class CarAudio : MonoBehaviour
    {
        [SerializeField] Vector2 enginePitch = new(0.7f, 2.1f);
        [SerializeField, Range(0f, 1f)] float playerEngineVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] float rivalEngineVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] float screechVolume = 0.35f;
        [Tooltip("Rivals: full volume within this distance of the camera, silent beyond the max.")]
        [SerializeField] Vector2 rivalHearing = new(12f, 34f);

        CarMovement _car;
        CarTerrain _terrain;
        AudioSource _engine, _screech, _effects;
        bool _isPlayer;
        float _screechLevel;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _terrain = GetComponent<CarTerrain>();
            _isPlayer = GetComponent<SomeGame.Input.PlayerCarInput>() != null;
            var library = AudioLibrary.Instance;
            if (library == null) return;
            _engine = CreateSource(library.engineLoop, true);
            _screech = CreateSource(library.screechLoop, true);
            _effects = CreateSource(null, false);
            _engine.volume = 0f;
            _screech.volume = 0f;
            _engine.Play();
            _screech.Play();
        }

        AudioSource CreateSource(AudioClip clip, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            if (!_isPlayer)
            {
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = rivalHearing.x;
                source.maxDistance = rivalHearing.y;
            }
            return source;
        }

        void OnEnable()
        {
            _car.BoostStarted += OnBoost;
            _car.Launched += OnLaunched;
            _car.Landed += OnLanded;
            _car.Slipped += OnSlipped;
            _car.Crashed += OnCrashed;
            if (_terrain != null) _terrain.Splashed += OnSplashed;
        }

        void OnDisable()
        {
            _car.BoostStarted -= OnBoost;
            _car.Launched -= OnLaunched;
            _car.Landed -= OnLanded;
            _car.Slipped -= OnSlipped;
            _car.Crashed -= OnCrashed;
            if (_terrain != null) _terrain.Splashed -= OnSplashed;
        }

        void Update()
        {
            if (_engine == null) return;
            var stats = _car.Stats;
            float speed01 = stats != null ? Mathf.Clamp01(Mathf.Abs(_car.ForwardSpeed) / stats.topSpeed) : 0f;
            float throttle = Mathf.Clamp01(_car.Throttle);
            float pitch = Mathf.Lerp(enginePitch.x, enginePitch.y, speed01) + throttle * 0.12f
                        + (_car.IsBoosting ? 0.25f : 0f) + (_car.IsAirborne ? 0.3f : 0f);
            _engine.pitch = Mathf.MoveTowards(_engine.pitch, pitch, Time.deltaTime * 3f);
            float volume = (_isPlayer ? playerEngineVolume : rivalEngineVolume) * Mathf.Lerp(0.55f, 1f, Mathf.Max(throttle, speed01));
            _engine.volume = Mathf.MoveTowards(_engine.volume, volume, Time.deltaTime * 2f);

            bool squeal = !_car.IsAirborne && !_car.IsOffRoad &&
                          (_car.IsDrifting || (stats != null && Mathf.Abs(_car.SidewaysSpeed) > stats.slideThreshold + 1.5f));
            _screechLevel = Mathf.MoveTowards(_screechLevel, squeal ? 1f : 0f, Time.deltaTime * 6f);
            _screech.volume = _screechLevel * screechVolume * (_isPlayer ? 1f : 0.6f);
            _screech.pitch = 0.9f + speed01 * 0.25f;
        }

        void PlayOneShot(AudioClip clip, float volume)
        {
            if (_effects != null && clip != null) _effects.PlayOneShot(clip, volume * (_isPlayer ? 1f : 0.7f));
        }

        AudioLibrary Library => AudioLibrary.Instance;

        void OnBoost(float strength) => PlayOneShot(Library.boost, Mathf.Lerp(0.5f, 0.9f, strength));
        void OnLaunched(float airTime) => PlayOneShot(Library.jump, 0.55f);
        void OnLanded() => PlayOneShot(Library.land, 0.8f);
        void OnSlipped() => PlayOneShot(Library.oilSpin, 0.75f);
        void OnSplashed() => PlayOneShot(Library.splash, 0.9f);

        void OnCrashed(float impact)
        {
            if (impact < 3f) return;
            PlayOneShot(Library.crash, Mathf.Clamp01(impact / 14f));
        }
    }
}
