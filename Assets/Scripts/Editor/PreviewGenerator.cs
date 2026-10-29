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
        // Each picture has its track's own shape (with a small margin), its longer side this many pixels.
        const int LongSide = 1000;

        [MenuItem("SomeGame/Generate Track Previews")]
        public static string Generate() => Generate(null);

        /// <summary>The pictures of every race, or only of the level named <paramref name="only"/>.</summary>
        public static string Generate(string only)
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
            foreach (var level in levels)
            {
                if (only != null && level.name != only) continue;
                var layout = level.track;
                layoutField.SetValue(track, layout);
                invalidate.Invoke(track, null);
                foreach (var part in parts) build.Invoke(part, new object[] { track.Path, layout });

                // The picture frames what is raced: not the hidden way back of a journey.
                Rect b = track.Path.Bounds();
                if (layout.gaps.Exists(g => g.hidden))
                {
                    var line = track.Path;
                    float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
                    for (float d = 0f; d < line.Length; d += 2f)
                    {
                        bool hidden = layout.gaps.Exists(g => g.hidden && line.DeltaDistance(g.from, d) >= 0f && line.DeltaDistance(d, g.to) >= 0f);
                        if (hidden) continue;
                        Vector2 p = line.PointAt(d);
                        xMin = Mathf.Min(xMin, p.x); yMin = Mathf.Min(yMin, p.y); xMax = Mathf.Max(xMax, p.x); yMax = Mathf.Max(yMax, p.y);
                    }
                    b = Rect.MinMaxRect(xMin - 8f, yMin - 8f, xMax + 8f, yMax + 8f);
                }
                foreach (var branch in track.Branches) // the other ways round are in the picture too
                    foreach (var p in branch.Points) b = Rect.MinMaxRect(Mathf.Min(b.xMin, p.x), Mathf.Min(b.yMin, p.y), Mathf.Max(b.xMax, p.x), Mathf.Max(b.yMax, p.y));
                // The ground reaches well beyond this margin, so the picture is filled to its edges.
                const float margin = 10f;
                float worldW = b.width + margin * 2f, worldH = b.height + margin * 2f;
                int Width = worldW >= worldH ? LongSide : Mathf.RoundToInt(LongSide * worldW / worldH);
                int Height = worldW >= worldH ? Mathf.RoundToInt(LongSide * worldH / worldW) : LongSide;
                var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                cam.transform.position = new Vector3(b.center.x, b.center.y, -10f);
                cam.orthographicSize = worldH * 0.5f;
                cam.backgroundColor = layout.waterWorld ? new Color(0.25f, 0.58f, 0.74f)
                    : layout.biome == SomeGame.Track.TrackLayout.Biome.Snow ? new Color(0.9f, 0.94f, 0.98f) : new Color(0.373f, 0.627f, 0.247f);
                cam.targetTexture = rt;
                cam.aspect = (float)Width / Height;
                cam.Render();
                cam.targetTexture = null;
                cam.ResetAspect();

                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                Object.DestroyImmediate(rt);
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
            AssetDatabase.SaveAssets();
            // Reopen the scene without saving the temporary changes.
            EditorSceneManager.OpenScene(UIBuilder.RaceScenePath);
            return "previews: " + log;
        }
    }
}
