using UnityEngine;

namespace SomeGame.Audio
{
    /// <summary>All sound clips of the game in one place (loaded from Resources/AudioLibrary).</summary>
    [CreateAssetMenu(menuName = "SomeGame/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Music")]
        public AudioClip mapMusic;
        public AudioClip raceMusic;
        [Range(0f, 1f)] public float mapMusicVolume = 0.55f;
        [Range(0f, 1f)] public float raceMusicVolume = 0.35f;

        [Header("Car")]
        public AudioClip engineLoop;
        public AudioClip screechLoop;
        public AudioClip boost;
        public AudioClip jump;
        public AudioClip land;
        public AudioClip splash;
        public AudioClip oilSpin;
        public AudioClip crash;

        [Header("Race and UI")]
        public AudioClip beepLow;
        public AudioClip beepHigh;
        public AudioClip click;
        public AudioClip lap;
        public AudioClip star;
        public AudioClip win;
        public AudioClip lose;

        static AudioLibrary _instance;
        public static AudioLibrary Instance => _instance != null ? _instance : _instance = Resources.Load<AudioLibrary>("AudioLibrary");
    }
}
