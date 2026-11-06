#if UNITY_EDITOR
using System;
using System.IO;
using SomeGame.Race;
using UnityEngine;

namespace SomeGame.Recording
{
    public enum ShortsCamera { Gameplay, Recording }

    /// <summary>What to record: which races of a cup, how many runs of each, and how they are filmed.</summary>
    [Serializable]
    public class ShortsOptions
    {
        public const string Folder = "Recordings/ShortsRaw";
        public const int Width = 1080, Height = 1920, FrameRate = 60;

        public int cup = 1;
        [Tooltip("Race numbers in the cup, from 1.")]
        public int[] races = { 7, 8 };
        public int runs = 3;
        public ShortsCamera camera = ShortsCamera.Recording;
        [Tooltip("0.3x for 1.5 seconds of video around the landing of a big jump.")]
        public bool slowMotion = true;
        [Tooltip("A new seed for every run: other forks, other lines down the road.")]
        public bool randomLines = true;
        public bool showHud = true;
        [Tooltip("The autopilot drifts through sharp corners, with bigger sparks and darker tyre trails.")]
        public bool drift = true;
        [Tooltip("Flights at least this long (seconds) count as big jumps.")]
        public float bigJumpAir = 0.85f;

        /// <summary>The base name of the next free run of a race ("race8_run03"), with its run number.</summary>
        public static (string name, int run) NextRun(CupDefinition cup, int race)
        {
            Directory.CreateDirectory(Folder);
            string prefix = (cup.look == CupLook.Winter ? "" : cup.look.ToString().ToLowerInvariant() + "_") + $"race{race}_run";
            int last = 0;
            foreach (var file in Directory.GetFiles(Folder, prefix + "*"))
                if (int.TryParse(Path.GetFileNameWithoutExtension(file).Substring(prefix.Length), out int n)) last = Mathf.Max(last, n);
            return ($"{prefix}{last + 1:00}", last + 1);
        }
    }
}
#endif
