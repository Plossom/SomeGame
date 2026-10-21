using System;
using UnityEngine;

namespace SomeGame.Input
{
    /// <summary>Player-facing control options, persisted between sessions.</summary>
    public static class ControlSettings
    {
        const string InvisibleJoystickKey = "Controls.InvisibleJoystick";
        const string SteerAssistKey = "Controls.SteerAssist";

        public enum Assist { Off, Light, Strong }

        const string DriftingKey = "Controls.Drifting";

        /// <summary>Drifting (second finger to hop and hold) on, or off for an easier game. On by default.</summary>
        public static bool Drifting
        {
            get => PlayerPrefs.GetInt(DriftingKey, 1) == 1;
            set
            {
                if (value == Drifting) return;
                PlayerPrefs.SetInt(DriftingKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Smart steering: Light (default) or Off. (A saved "Strong" from earlier counts as Light.)</summary>
        public static Assist SteerAssist
        {
            get => PlayerPrefs.GetInt(SteerAssistKey, (int)Assist.Light) == (int)Assist.Off ? Assist.Off : Assist.Light;
            set
            {
                if (value == SteerAssist) return;
                PlayerPrefs.SetInt(SteerAssistKey, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static event Action Changed;

        const string CameraZoomKey = "Controls.CameraZoom";

        /// <summary>Race camera: 0 normal (default), 1 far, 2 farthest (sees more of the track).</summary>
        public static int CameraZoom
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(CameraZoomKey, 0), 0, ZoomFactors.Length - 1);
            set
            {
                value = Mathf.Clamp(value, 0, ZoomFactors.Length - 1);
                if (value == CameraZoom) return;
                PlayerPrefs.SetInt(CameraZoomKey, value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        static readonly float[] ZoomFactors = { 1f, 1.2f, 1.4f };
        public static readonly string[] ZoomNames = { "NORMAL", "FAR", "FARTHEST" };

        /// <summary>How much more of the world the race camera shows than normal.</summary>
        public static float CameraZoomFactor => ZoomFactors[CameraZoom];

        public static int CameraZoomSteps => ZoomFactors.Length;

        /// <summary>When true the floating joystick still works but draws nothing.</summary>
        public static bool InvisibleJoystick
        {
            get => PlayerPrefs.GetInt(InvisibleJoystickKey, 0) == 1;
            set
            {
                if (value == InvisibleJoystick) return;
                PlayerPrefs.SetInt(InvisibleJoystickKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }
    }
}
