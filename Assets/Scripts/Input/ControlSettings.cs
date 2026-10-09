using System;
using UnityEngine;

namespace SomeGame.Input
{
    /// <summary>Player-facing control options, persisted between sessions.</summary>
    public static class ControlSettings
    {
        const string InvisibleJoystickKey = "Controls.InvisibleJoystick";

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
