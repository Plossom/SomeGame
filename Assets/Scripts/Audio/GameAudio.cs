using System.Collections;
using UnityEngine;

namespace SomeGame.Audio
{
    /// <summary>
    /// Plays the music and the non-positional sounds (UI clicks, countdown, jingles) on a sound object
    /// that survives scene changes. Music fades over to the next track when a scene asks for another one.
    /// These sources ignore the listener pause, so the menu still clicks and the music plays on while
    /// a race is paused.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        static GameAudio _instance;
        AudioSource _ui, _music;
        Coroutine _fade;

        static GameAudio Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("GameAudio");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<GameAudio>();
                _instance._ui = go.AddComponent<AudioSource>();
                _instance._ui.playOnAwake = false;
                _instance._ui.ignoreListenerPause = true;
                _instance._music = go.AddComponent<AudioSource>();
                _instance._music.playOnAwake = false;
                _instance._music.loop = true;
                _instance._music.ignoreListenerPause = true;
                return _instance;
            }
        }

        static AudioLibrary Library => AudioLibrary.Instance;

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null) Instance._ui.PlayOneShot(clip, volume);
        }

        public static void Click() => Play(Library?.click, 0.6f);
        public static void Countdown(bool go) => Play(go ? Library?.beepHigh : Library?.beepLow, 0.8f);

        /// <summary>Starts a music track (fading from the current one), or keeps it if it is already playing.</summary>
        public static void PlayMusic(AudioClip clip, float volume)
        {
            var self = Instance;
            if (clip == null) return;
            if (self._music.clip == clip && self._music.isPlaying)
            {
                self._music.volume = volume;
                return;
            }
            if (self._fade != null) self.StopCoroutine(self._fade);
            self._fade = self.StartCoroutine(self.FadeTo(clip, volume));
        }

        IEnumerator FadeTo(AudioClip clip, float volume)
        {
            const float fade = 0.6f;
            float start = _music.volume;
            for (float t = 0f; t < fade && _music.isPlaying; t += Time.unscaledDeltaTime)
            {
                _music.volume = Mathf.Lerp(start, 0f, t / fade);
                yield return null;
            }
            _music.clip = clip;
            _music.volume = 0f;
            _music.Play();
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                _music.volume = Mathf.Lerp(0f, volume, t / fade);
                yield return null;
            }
            _music.volume = volume;
            _fade = null;
        }
    }
}
