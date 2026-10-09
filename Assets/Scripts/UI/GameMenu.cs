using SomeGame.Audio;
using SomeGame.Input;
using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The ☰ menu at the top of every screen. In a race it pauses the game and offers Resume,
    /// Restart and Main menu. Both the race and the map show the settings (sound volume,
    /// joystick visibility).
    /// </summary>
    public class GameMenu : MonoBehaviour
    {
        [Tooltip("Set in the race scene: the menu then pauses the race. Leave empty on the map.")]
        [SerializeField] RaceManager race;
        [SerializeField] FloatingJoystick joystick;

        [SerializeField] UnityEngine.UI.Button openButton;
        [SerializeField] GameObject panel;
        [Tooltip("Full-screen dimmer behind the panel; tapping it closes the menu (resumes).")]
        [SerializeField] UnityEngine.UI.Button closeArea;
        [Tooltip("Optional explicit close (X) button.")]
        [SerializeField] UnityEngine.UI.Button closeButton;
        [SerializeField] TMP_Text title;

        [Header("Race only")]
        [SerializeField] UnityEngine.UI.Button resumeButton;
        [SerializeField] UnityEngine.UI.Button restartButton;
        [SerializeField] UnityEngine.UI.Button mainMenuButton;

        [Header("Settings")]
        [SerializeField] UnityEngine.UI.Slider volumeSlider;
        [SerializeField] UnityEngine.UI.Button joystickButton;
        [SerializeField] TMP_Text joystickLabel;

        bool _joystickWasInteractable;

        public bool IsOpen => panel.activeSelf;

        void Awake()
        {
            bool inRace = race != null;
            openButton.onClick.AddListener(Open);
            closeArea.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            resumeButton.onClick.AddListener(Close);
            restartButton.onClick.AddListener(() => { Unpause(); race.Restart(); });
            mainMenuButton.onClick.AddListener(() => { Unpause(); GameSession.ReturnToMap(); });
            joystickButton.onClick.AddListener(() => ControlSettings.InvisibleJoystick = !ControlSettings.InvisibleJoystick);
            volumeSlider.onValueChanged.AddListener(v => SoundSettings.Volume = v);

            resumeButton.gameObject.SetActive(inRace);
            restartButton.gameObject.SetActive(inRace);
            mainMenuButton.gameObject.SetActive(inRace);
            title.text = inRace ? "PAUSED" : "MENU";
            panel.SetActive(false);
        }

        void OnEnable()
        {
            ControlSettings.Changed += RefreshSettings;
            SoundSettings.Changed += RefreshSettings;
        }

        void OnDisable()
        {
            ControlSettings.Changed -= RefreshSettings;
            SoundSettings.Changed -= RefreshSettings;
        }

        void OnDestroy() => Unpause();

        public void Open()
        {
            if (IsOpen) return;
            RefreshSettings();
            panel.SetActive(true);
            if (race == null) return;

            Time.timeScale = 0f;
            if (joystick != null)
            {
                _joystickWasInteractable = joystick.Interactable;
                joystick.Interactable = false;
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (race == null) return;
            Unpause();
            if (joystick != null) joystick.Interactable = _joystickWasInteractable && race.State != RaceState.Finished;
        }

        static void Unpause() => Time.timeScale = 1f;

        void RefreshSettings()
        {
            volumeSlider.SetValueWithoutNotify(SoundSettings.Volume);
            joystickLabel.text = ControlSettings.InvisibleJoystick
                ? $"JOYSTICK   <color={NeonTheme.Html(NeonTheme.Dim)}>HIDDEN</color>"
                : $"JOYSTICK   <color={NeonTheme.Html(NeonTheme.Lime)}>VISIBLE</color>";
        }
    }
}
