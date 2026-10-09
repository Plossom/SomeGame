using SomeGame.Track;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Places the scenery of every track once and stores it in the track asset, so races load instantly.
    /// (The Race scene also re-bakes the track it shows whenever its scenery settings change.)
    /// </summary>
    public static class SceneryBaker
    {
        [MenuItem("SomeGame/Bake Scenery (all tracks)")]
        public static string BakeAll()
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(UIBuilder.RaceScenePath);
            var track = Object.FindAnyObjectByType<SomeGame.Track.Track>();
            var scenery = Object.FindAnyObjectByType<TrackScenery>();
            var layoutField = typeof(SomeGame.Track.Track).GetField("layout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var invalidate = typeof(SomeGame.Track.Track).GetMethod("Invalidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var build = typeof(TrackDerivedBehaviour).GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var original = layoutField.GetValue(track);
            var log = new System.Text.StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:TrackLayout"))
            {
                var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(guid));
                layoutField.SetValue(track, layout);
                invalidate.Invoke(track, null);
                layout.bakedSignature = null; // force a fresh placement
                var watch = System.Diagnostics.Stopwatch.StartNew();
                build.Invoke(scenery, new object[] { track.Path, layout });
                EditorUtility.SetDirty(layout);
                log.Append($"{layout.name}: {layout.bakedScenery.Count} items in {watch.ElapsedMilliseconds} ms; ");
            }
            layoutField.SetValue(track, original);
            invalidate.Invoke(track, null);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(UIBuilder.RaceScenePath);
            Debug.Log(log.ToString());
            return log.ToString();
        }
    }
}
