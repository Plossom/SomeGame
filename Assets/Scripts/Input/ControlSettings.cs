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

        /// <summary>How drifts are started: by hand with a second finger (Manual, the default) or by themselves (Auto).</summary>
        public enum DriftMode { Manual, Auto }

        const string DriftModeKey = "Controls.Drift";

        public static DriftMode Drift
        {
            get => PlayerPrefs.GetInt(DriftModeKey, (int)DriftMode.Manual) == (int)DriftMode.Auto ? DriftMode.Auto : DriftMode.Manual;
            set
            {
                if (value == Drift) return;
                PlayerPrefs.SetInt(DriftModeKey, (int)value);
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
