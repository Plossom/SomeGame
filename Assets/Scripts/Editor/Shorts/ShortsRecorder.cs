using System;
using System.Collections.Generic;
using System.IO;
using SomeGame.Race;
using SomeGame.Recording;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Records autopilot runs for Shorts, TikTok and Reels: the Game view at 1080x1920, 60 fps, H.264 MP4, with an
    /// event log (JSON of the same name) beside each video, in Recordings/ShortsRaw. Runs are numbered on from what
    /// is there, so nothing is overwritten. Starts Play mode for the batch and leaves it again when done.
    /// </summary>
    [InitializeOnLoad]
    public static class ShortsRecorder
    {
        const string PendingKey = "SomeGame.Shorts.Pending";

        static RecorderController _controller;

        static ShortsRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            ShortsDirector.StartVideo = StartVideo;
            ShortsDirector.StopVideo = StopVideo;
        }

        static bool StartVideo(string name)
        {
            _controller = Prepare(name);
            return _controller.StartRecording();
        }

        static void StopVideo()
        {
            if (_controller != null && _controller.IsRecording()) _controller.StopRecording();
            _controller = null;
        }

        /// <summary>Starts a batch. Refused during Play mode: that session may be someone's game.</summary>
        public static string Record(ShortsOptions options)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Play mode is running: stop it first.";
            if (EditorApplication.isCompiling) return "Unity is compiling.";
            SessionState.SetString(PendingKey, JsonUtility.ToJson(options));
            EditorApplication.EnterPlaymode();
            return null;
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                string json = SessionState.GetString(PendingKey, "");
                SessionState.EraseString(PendingKey);
                if (json.Length == 0) return;
                var go = new GameObject("ShortsDirector");
                go.AddComponent<ShortsDirector>().Options = JsonUtility.FromJson<ShortsOptions>(json);
            }
            else if (change == PlayModeStateChange.EnteredEditMode) RaceManager.KeepRecords = true;
        }

        /// <summary>A movie recorder for one video: the Game view, vertical, without the Editor around it.</summary>
        static RecorderController Prepare(string baseName)
        {
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = baseName;
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = ShortsOptions.Width, OutputHeight = ShortsOptions.Height };
            movie.CaptureAudio = true;
            movie.OutputFile = Path.Combine(Path.GetFullPath(ShortsOptions.Folder), baseName);

            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = ShortsOptions.FrameRate;
            settings.FrameRatePlayback = FrameRatePlayback.Constant; // every frame 1/60 s of game time, however long it takes to render
            settings.CapFrameRate = true;
            var controller = new RecorderController(settings);
            controller.PrepareRecording();
            return controller;
        }
    }

    /// <summary>SomeGame / Record Shorts: pick races and options, and start a batch.</summary>
    public class ShortsWindow : EditorWindow
    {
        ShortsOptions _options = new();
        readonly HashSet<int> _picked = new() { 7, 8 };

        [MenuItem("SomeGame/Record Shorts...")]
        static void Open() => GetWindow<ShortsWindow>("Record Shorts");

        void OnGUI()
        {
            var cups = CupList.Instance.cups;
            var names = new string[cups.Count];
            for (int i = 0; i < names.Length; i++) names[i] = cups[i].displayName;
            int cup = EditorGUILayout.Popup("Cup", _options.cup, names);
            if (cup != _options.cup) { _options.cup = cup; _picked.Clear(); }
            var races = cups[_options.cup].races;
            for (int i = 0; i < races.Count; i++)
            {
                bool on = EditorGUILayout.ToggleLeft($"{i + 1}  {races[i].displayName}", _picked.Contains(i + 1));
                if (on) _picked.Add(i + 1); else _picked.Remove(i + 1);
            }
            EditorGUILayout.Space();
            _options.runs = EditorGUILayout.IntSlider("Runs of each", _options.runs, 1, 10);
            _options.camera = (ShortsCamera)EditorGUILayout.EnumPopup("Camera", _options.camera);
            _options.slowMotion = EditorGUILayout.Toggle("Slow motion on big jumps", _options.slowMotion);
            _options.randomLines = EditorGUILayout.Toggle("Random lines and forks", _options.randomLines);
            _options.showHud = EditorGUILayout.Toggle("Show the race HUD", _options.showHud);
            _options.drift = EditorGUILayout.Toggle("Autopilot drifts", _options.drift);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"{ShortsOptions.Width}x{ShortsOptions.Height}, {ShortsOptions.FrameRate} fps, H.264 MP4, into {ShortsOptions.Folder}", MessageType.None);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || _picked.Count == 0))
                if (GUILayout.Button("Record"))
                {
                    var list = new List<int>(_picked);
                    list.Sort();
                    _options.races = list.ToArray();
                    string refused = ShortsRecorder.Record(_options);
                    if (refused != null) Debug.LogWarning("Shorts: " + refused);
                }
        }
    }
}
