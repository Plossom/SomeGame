using SomeGame.City;
using SomeGame.Race;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Creates the City scene (start screen): top-down camera with the 3D renderer and bloom, the city
    /// generator with one zone per district, and then the UI via <see cref="NeonUIBuilder"/>.
    /// </summary>
    public static class CitySceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/City.unity";

        // District layout on the ground (x, z, width, depth), landmark and style, in campaign order.
        static readonly (string asset, CityStyle style, Rect area, Vector2 landmark)[] Layout =
        {
            ("Downtown", CityStyle.Downtown, new Rect(-48, -48, 96, 80), new Vector2(8, -8)),
            ("Harbor", CityStyle.Harbor, new Rect(48, -64, 64, 112), new Vector2(98, -36)),
            ("Summit", CityStyle.Summit, new Rect(-112, 32, 96, 80), new Vector2(-64, 64)),
            ("Underground", CityStyle.Underground, new Rect(-112, -64, 64, 96), new Vector2(-72, -24)),
            ("Skyline", CityStyle.Skyline, new Rect(-16, 32, 128, 80), new Vector2(56, 72)),
        };

        [MenuItem("SomeGame/Rebuild City Scene")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera: 3D renderer (index 1), HDR, post-processing.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.027f, 0.043f);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 20f;
            cam.farClipPlane = 1400f;
            cam.allowHDR = true;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.SetRenderer(1);
            var cityCam = camGo.AddComponent<CityCamera>();
            Configure(cityCam, ("bounds", new Rect(-110, -78, 220, 190)), ("pitch", 90f));

            RenderSettings.fog = false;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.02f, 0.027f, 0.043f);
            RenderSettings.fogStartDistance = 330f;
            RenderSettings.fogEndDistance = 760f;

            // Bloom: only HDR (emission > 1) glows.
            var profPath = "Assets/Settings/CityVolume.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profPath);
            if (prof == null)
            {
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(prof, profPath);
            }
            if (!prof.TryGet<Bloom>(out var bloom)) { bloom = prof.Add<Bloom>(true); AssetDatabase.AddObjectToAsset(bloom, prof); }
            bloom.threshold.Override(1f); bloom.intensity.Override(0.9f); bloom.scatter.Override(0.72f);
            if (!prof.TryGet<Vignette>(out var vignette)) { vignette = prof.Add<Vignette>(true); AssetDatabase.AddObjectToAsset(vignette, prof); }
            vignette.intensity.Override(0.32f); vignette.smoothness.Override(0.55f);
            if (!prof.TryGet<Tonemapping>(out var tone)) { tone = prof.Add<Tonemapping>(true); AssetDatabase.AddObjectToAsset(tone, prof); }
            tone.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(prof);
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = prof;

            // City generator + zones.
            var cityGo = new GameObject("City");
            var generator = cityGo.AddComponent<CityGenerator>();
            for (int i = 0; i < Layout.Length; i++)
            {
                var (asset, style, area, landmark) = Layout[i];
                var zoneGo = new GameObject($"Zone_{asset}");
                zoneGo.transform.SetParent(cityGo.transform, false);
                var zone = zoneGo.AddComponent<CityZone>();
                Configure(zone,
                    ("district", AssetDatabase.LoadAssetAtPath<DistrictDefinition>($"Assets/Data/Districts/{asset}.asset")),
                    ("style", (int)style), ("area", area), ("landmark", landmark), ("seed", i + 1));
            }

            Configure(generator,
                ("flatMaterial", Mat("CityFlat")), ("linesMaterial", Mat("CityLines")),
                ("neonMaterial", Mat("CityNeon")), ("glowMaterial", Mat("CityGlow")),
                ("fogMaterial", Mat("CityFog")), ("garagePosition", new Vector2(-8f, -60f)),
                ("carSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Neon/Car.png")),
                ("carDetailsSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Neon/CarDetails.png")),
                ("glowSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Neon/Glow.png")),
                ("signFont", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/ChakraPetch/ChakraPetch-BoldItalic SDF.asset")));

            EditorSceneManager.SaveScene(scene, ScenePath);
            NeonUIBuilder.BuildCityUI();

            var scenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/Race.unity", true),
            };
        }

        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/City/{name}.mat");

        static void Configure(Object target, params (string name, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (name, value) in values)
            {
                var p = so.FindProperty(name);
                switch (value)
                {
                    case int i: if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = i; else p.intValue = i; break;
                    case float f: p.floatValue = f; break;
                    case Vector2 v: p.vector2Value = v; break;
                    case Rect r: p.rectValue = r; break;
                    case Object o: p.objectReferenceValue = o; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
