using System;
using UnityEngine;

namespace SomeGame.Audio
{
    /// <summary>Master sound volume, persisted and applied to the AudioListener (all game sound).</summary>
    public static class SoundSettings
    {
        const string VolumeKey = "Sound.Volume";

        public static event Action Changed;

        /// <summary>0 = mute, 1 = full volume.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, Volume)) return;
                PlayerPrefs.SetFloat(VolumeKey, value);
                PlayerPrefs.Save();
                Apply();
                Changed?.Invoke();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply() => AudioListener.volume = Volume;
    }
}
