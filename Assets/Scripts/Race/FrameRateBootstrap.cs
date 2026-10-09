using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>Applies app-wide runtime settings before the first scene loads.</summary>
    public static class FrameRateBootstrap
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
