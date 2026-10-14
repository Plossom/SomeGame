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
        [Tooltip("Drifting on / off.")]
        [SerializeField] UnityEngine.UI.Button driftButton;
        [SerializeField] TMP_Text driftLabel;
        [Tooltip("Smart steering: on (light) / off.")]
        [SerializeField] UnityEngine.UI.Button assistButton;
        [SerializeField] TMP_Text assistLabel;

        [Header("Map only")]
        [Tooltip("Resets all progress (asks for a second tap first).")]
        [SerializeField] UnityEngine.UI.Button tutorialButton;
        [Tooltip("Development: unlocks or locks the Winter Cup and all races.")]
        [SerializeField] UnityEngine.UI.Button cupsButton;
        [SerializeField] TMP_Text cupsLabel;
        [SerializeField] UnityEngine.UI.Button resetButton;
        [SerializeField] TMP_Text resetLabel;
        [SerializeField] LevelCatalog catalog;

        bool _confirmReset;

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
            if (driftButton != null)
                driftButton.onClick.AddListener(() => ControlSettings.Drifting = !ControlSettings.Drifting);
            if (assistButton != null)
                assistButton.onClick.AddListener(() => ControlSettings.SteerAssist =
                    ControlSettings.SteerAssist == ControlSettings.Assist.Off ? ControlSettings.Assist.Light : ControlSettings.Assist.Off);
            if (tutorialButton != null)
            {
                tutorialButton.gameObject.SetActive(!inRace);
                tutorialButton.onClick.AddListener(GameSession.StartTutorial);
            }
            if (cupsButton != null)
            {
                cupsButton.gameObject.SetActive(!inRace);
                cupsButton.onClick.AddListener(ToggleCups);
            }
            if (resetButton != null)
            {
                resetButton.gameObject.SetActive(!inRace);
                resetButton.onClick.AddListener(OnReset);
            }
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

        // First tap asks, second tap resets and reloads the map.
        void OnReset()
        {
            if (!_confirmReset)
            {
                _confirmReset = true;
                resetLabel.text = "TAP AGAIN TO RESET";
                return;
            }
            GameReset.ResetAll(catalog);
            GameSession.ReturnToMap();
        }

        // Development: opens (or locks again) the Winter Cup and every race of the Valley Cup; the map
        // reloads to show it.
        void ToggleCups()
        {
            CupList.DevUnlockAll = !CupList.DevUnlockAll;
            RefreshSettings();
            GameSession.ReturnToMap();
        }

        public void Open()
        {
            if (IsOpen) return;
            _confirmReset = false;
            if (resetLabel != null) resetLabel.text = "RESET GAME";
            RefreshSettings();
            panel.SetActive(true);
            if (race == null) return;

            Time.timeScale = 0f;
            AudioListener.pause = true; // car sounds stop; music and menu clicks go on
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

        static void Unpause()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void RefreshSettings()
        {
            volumeSlider.SetValueWithoutNotify(SoundSettings.Volume);
            joystickLabel.text = ControlSettings.InvisibleJoystick
                ? $"JOYSTICK   <color={Theme.Html(Theme.StoneDark)}>HIDDEN</color>"
                : $"JOYSTICK   <color={Theme.Html(Theme.Orange)}>VISIBLE</color>";
            if (driftLabel != null)
                driftLabel.text = ControlSettings.Drifting
                    ? $"DRIFTING   <color={Theme.Html(Theme.Orange)}>ON</color>"
                    : $"DRIFTING   <color={Theme.Html(Theme.StoneDark)}>OFF</color>";
            if (assistLabel != null)
            {
                var a = ControlSettings.SteerAssist;
                string word = a == ControlSettings.Assist.Off ? "OFF" : "ON";
                assistLabel.text = $"STEERING HELP   <color={Theme.Html(a == ControlSettings.Assist.Off ? Theme.StoneDark : Theme.Orange)}>{word}</color>";
            }
            if (cupsLabel != null)
                cupsLabel.text = CupList.DevUnlockAll
                    ? $"UNLOCK ALL   <color={Theme.Html(Theme.Orange)}>ON</color>"
                    : $"UNLOCK ALL   <color={Theme.Html(Theme.StoneDark)}>OFF</color>";
        }
    }
}
