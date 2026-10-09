using UnityEngine;

namespace SomeGame.Audio
{
    /// <summary>Starts this scene's background music (map or race) when the scene loads.</summary>
    public class MusicPlayer : MonoBehaviour
    {
        public enum Track { Map, Race }

        [SerializeField] Track track;

        void Start()
        {
            var library = AudioLibrary.Instance;
            if (library == null) return;
            if (track == Track.Map) GameAudio.PlayMusic(library.mapMusic, library.mapMusicVolume);
            else GameAudio.PlayMusic(library.raceMusic, library.raceMusicVolume);
        }
    }
}
