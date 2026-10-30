using UnityEditor;
using UnityEditor.SceneManagement;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Pressing Play in the Editor always starts the game on the map (the start screen), whichever scene
    /// is open, just like the built game. Turn it off from the menu to test the open scene directly.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMap
    {
        const string Menu = "SomeGame/Play Starts On Map";
        const string Key = "SomeGame.PlayFromMap";

        static PlayFromMap() => EditorApplication.delayCall += Apply;

        static bool Enabled
        {
            get => EditorPrefs.GetBool(Key, true);
            set => EditorPrefs.SetBool(Key, value);
        }

        [MenuItem(Menu)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(Menu, true)]
        static bool ToggleValidate()
        {
            UnityEditor.Menu.SetChecked(Menu, Enabled);
            return true;
        }

        static void Apply() => EditorSceneManager.playModeStartScene = Enabled ? MapScene() : null;

        // The map scene where it belongs - or, if it has been moved, wherever it is now (by its name).
        static SceneAsset MapScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(UIBuilder.MapScenePath);
            if (scene != null) return scene;
            foreach (var guid in AssetDatabase.FindAssets("Map t:SceneAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == "Map") return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            return null;
        }
    }
}
