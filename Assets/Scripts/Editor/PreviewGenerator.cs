using System.IO;
using SomeGame.Race;
using SomeGame.Track;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Renders a picture of each race's whole track from above (road, scenery, water, ramps; no cars)
    /// into Assets/Art/Previews and assigns it to the race, for the loading screen.
    /// </summary>
    public static class PreviewGenerator
    {
        const string Folder = "Assets/Art/Previews";
        const int Width = 560, Height = 1000;

        [MenuItem("SomeGame/Generate Track Previews")]
        public static string Generate()
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(UIBuilder.RaceScenePath);
            Directory.CreateDirectory(Folder);
            var track = Object.FindAnyObjectByType<SomeGame.Track.Track>();
            var cam = Camera.main;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var layoutField = typeof(SomeGame.Track.Track).GetField("layout", flags);
            var invalidate = typeof(SomeGame.Track.Track).GetMethod("Invalidate", flags);
            var build = typeof(TrackDerivedBehaviour).GetMethod("Build", flags);
            var parts = track.GetComponentsInChildren<TrackDerivedBehaviour>(true);
            // Cars and their effects stay out of the picture.
            foreach (var car in Object.FindObjectsByType<SomeGame.Car.CarMovement>(FindObjectsSortMode.None)) car.gameObject.SetActive(false);

            // Every cup's races.
            var levels = new System.Collections.Generic.List<LevelDefinition>();
            foreach (var guid in AssetDatabase.FindAssets("t:LevelCatalog"))
                foreach (var l in AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guid)).levels)
                    if (l != null && !levels.Contains(l)) levels.Add(l);
            var log = new System.Text.StringBuilder();
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            foreach (var level in levels)
            {
                var layout = level.track;
                layoutField.SetValue(track, layout);
                invalidate.Invoke(track, null);
                foreach (var part in parts) build.Invoke(part, new object[] { track.Path, layout });

                Rect b = track.Path.Bounds();
                foreach (var branch in track.Branches) // the other ways round are in the picture too
                    foreach (var p in branch.Points) b = Rect.MinMaxRect(Mathf.Min(b.xMin, p.x), Mathf.Min(b.yMin, p.y), Mathf.Max(b.xMax, p.x), Mathf.Max(b.yMax, p.y));
                float margin = 10f, aspect = (float)Width / Height;
                cam.transform.position = new Vector3(b.center.x, b.center.y, -10f);
                cam.orthographicSize = Mathf.Max(b.height * 0.5f + margin, (b.width * 0.5f + margin) / aspect);
                cam.backgroundColor = layout.waterWorld ? new Color(0.25f, 0.58f, 0.74f)
                    : layout.biome == SomeGame.Track.TrackLayout.Biome.Snow ? new Color(0.9f, 0.94f, 0.98f) : new Color(0.373f, 0.627f, 0.247f);
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;

                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                string path = $"{Folder}/{level.name}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
                level.preview = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                EditorUtility.SetDirty(level);
                log.Append(level.name).Append(' ');
            }
            Object.DestroyImmediate(rt);
            AssetDatabase.SaveAssets();
            // Reopen the scene without saving the temporary changes.
            EditorSceneManager.OpenScene(UIBuilder.RaceScenePath);
            return "previews: " + log;
        }
    }
}
