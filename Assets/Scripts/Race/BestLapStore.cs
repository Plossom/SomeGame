using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>Best lap time per track, saved between sessions in PlayerPrefs.</summary>
    public static class BestLapStore
    {
        static string Key(string trackId) => $"BestLap.{trackId}";

        /// <summary>Best lap in seconds, or null if none has been set.</summary>
        public static float? Get(string trackId)
        {
            float value = PlayerPrefs.GetFloat(Key(trackId), -1f);
            return value > 0f ? value : null;
        }

        /// <summary>Stores the lap if it beats the current best. Returns true for a new best.</summary>
        public static bool Submit(string trackId, float lapTime)
        {
            var best = Get(trackId);
            if (best.HasValue && lapTime >= best.Value) return false;
            PlayerPrefs.SetFloat(Key(trackId), lapTime);
            PlayerPrefs.Save();
            return true;
        }
    }
}
