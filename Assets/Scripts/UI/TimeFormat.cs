using UnityEngine;

namespace SomeGame.UI
{
    public static class TimeFormat
    {
        /// <summary>Formats seconds as m:ss.ff.</summary>
        public static string Race(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = (int)(seconds / 60f);
            return $"{minutes}:{seconds - minutes * 60f:00.00}";
        }

        public static string Ordinal(int n) => n + (n % 100 is 11 or 12 or 13 ? "th" : (n % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th",
        });
    }
}
