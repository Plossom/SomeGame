using System.Collections.Generic;
using SomeGame.Race;
using SomeGame.Track;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Sets up the Water Cup (the third cup): its art, the lagoon in the Race scene, race 1 "Lagoon Loop" (track,
    /// level, catalog), the cup itself and its place in the cup list. Safe to run again: it updates what is there.
    /// </summary>
    public static class WaterCupSetup
    {
        const string Art = ArtGenerator.Folder;

        [MenuItem("SomeGame/Water Cup/Set Up Everything")]
        public static string SetUpAll()
        {
            WaterArt.GenerateBoats();
            WaterArt.GenerateLagoon();
            WaterArt.GenerateLagoonScenery();
            ArtGenerator.BuildGarageArt();
            return SetUp();
        }

        [MenuItem("SomeGame/Water Cup/Set Up Race And Cup")]
        public static string SetUp()
        {
            string keep = EditorSceneManager.GetActiveScene().path;
            WireRaceScene();
            var layout = BuildLagoonLoop();
            var level = Level("Water01", "Lagoon Loop", layout);
            var catalog = Asset<LevelCatalog>("Assets/Data/Levels/WaterCatalog.asset");
            catalog.levels = new List<LevelDefinition> { level };
            EditorUtility.SetDirty(catalog);
            var cup = Asset<CupDefinition>("Assets/Data/Cups/Water.asset");
            cup.displayName = "WATER CUP";
            cup.races = catalog;
            cup.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/CupWater.png");
            cup.accent = new Color(1f, 0.31f, 0.53f); // hibiscus pink
            cup.look = CupLook.Water;
            var map = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/MapWater.png");
            if (map != null) cup.mapBackground = map;
            cup.starsRequired = 40;
            cup.comingSoonRaces = new List<string> { "Harbor Havoc", "Lighthouse Run", "Jungle Rapids", "Pirate Bay", "Coral Coast", "Storm Chasers", "Storm Rally" };
            EditorUtility.SetDirty(cup);
            var list = AssetDatabase.LoadAssetAtPath<CupList>("Assets/Resources/Cups.asset");
            if (!list.cups.Contains(cup)) list.cups.Insert(Mathf.Min(2, list.cups.Count), cup);
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(keep) && keep != UIBuilder.RaceScenePath) EditorSceneManager.OpenScene(keep);
            return $"water cup: {layout.name} {catalog.Count} race(s), cups {list.cups.Count}";
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Art}/{name}.mat");
        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{name}.png");

        // ------------------------------------------------------------------ the Race scene

        static void WireRaceScene()
        {
            EditorSceneManager.OpenScene(UIBuilder.RaceScenePath);
            var renderer = Object.FindAnyObjectByType<TrackRenderer>();
            var so = new SerializedObject(renderer);
            so.FindProperty("lagoonGround").objectReferenceValue = Mat("LagoonSea");
            so.FindProperty("lagoonRoad").objectReferenceValue = Mat("LagoonChannel");
            so.FindProperty("lagoonEdges").objectReferenceValue = Mat("LagoonFoamMat");
            so.FindProperty("lagoonKerbs").objectReferenceValue = Mat("LagoonBuoysMat");
            so.FindProperty("lagoonRunoff").objectReferenceValue = Mat("LagoonRunoff");
            so.ApplyModifiedPropertiesWithoutUndo();

            var trackObject = Object.FindAnyObjectByType<SomeGame.Track.Track>();
            var lagoon = Object.FindAnyObjectByType<TrackLagoon>(FindObjectsInactive.Include);
            if (lagoon == null)
            {
                var go = new GameObject("Lagoon");
                lagoon = go.AddComponent<TrackLagoon>();
            }
            lagoon.transform.SetParent(trackObject.transform, false); // (with the track's other parts: the previews draw it too)
            var lso = new SerializedObject(lagoon);
            lso.FindProperty("track").objectReferenceValue = Object.FindAnyObjectByType<SomeGame.Track.Track>();
            lso.FindProperty("sand").objectReferenceValue = Mat("LagoonSandMat");
            lso.FindProperty("jungle").objectReferenceValue = Mat("LagoonJungleMat");
            lso.FindProperty("shallows").objectReferenceValue = Mat("LagoonRunoff");
            lso.FindProperty("foam").objectReferenceValue = Mat("LagoonFoamMat");
            lso.FindProperty("plain").objectReferenceValue = Mat("Plain");
            lso.FindProperty("current").objectReferenceValue = Mat("LagoonCurrent");
            lso.FindProperty("scenery").objectReferenceValue = Mat("SceneryLagoon");
            lso.FindProperty("dolphin").objectReferenceValue = Sprite("Dolphin");
            lso.FindProperty("bridge").objectReferenceValue = Sprite("LagoonBridge");
            lso.FindProperty("ring").objectReferenceValue = Sprite("SplashRing");
            lso.FindProperty("gull").objectReferenceValue = Sprite("Gull");
            lso.FindProperty("shoal").objectReferenceValue = Sprite("FishShoal");
            lso.FindProperty("sunbather").objectReferenceValue = Sprite("Sunbather");
            lso.FindProperty("sandcastle").objectReferenceValue = Sprite("SandcastleTop");
            lso.FindProperty("crab").objectReferenceValue = Sprite("Crab");
            lso.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------------ race 1: Lagoon Loop

        static TrackLayout BuildLagoonLoop()
        {
            var t = Asset<TrackLayout>("Assets/Data/Tracks/LagoonLoop.asset");
            t.waypoints = WAYPOINTS;
            t.roadWidth = 9f;
            t.rampWidth = 3.6f; // (the sandbar's kicker a little wider)
            t.widths = new List<TrackLayout.WidthKey>();
            t.kerbWidth = 0.7f;
            t.sampleSpacing = 1f;
            t.checkpointCount = 8;
            t.boundaryMargin = 28f;
            t.biome = TrackLayout.Biome.Lagoon;
            t.vehicle = TrackLayout.Vehicle.Boat;
            t.sceneryMaterial = Mat("SceneryLagoon");
            t.skyLife = 0f;
            var path = new TrackPath(t.waypoints, t.sampleSpacing);
            Vector2 At(float d, float lateral = 0f) => path.PointAt(d) + path.NormalAt(d) * lateral;

            t.lagoonIslands = new List<TrackLayout.Island>
            {
                new() { points = BEACH, kind = TrackLayout.IslandKind.Jungle, beach = 6f, palms = 26, huts = 7, seed = 1 },
                new() { points = PALM, kind = TrackLayout.IslandKind.Jungle, beach = 4.5f, palms = 70, huts = 5, seed = 2 },
                new() { points = LIGHT, kind = TrackLayout.IslandKind.Rock, beach = 2f, palms = 6, seed = 3 },
                new() { points = EASTBANK, kind = TrackLayout.IslandKind.Jungle, beach = 4f, palms = 22, huts = 3, seed = 4 },
                new() { points = NORTHBANK, kind = TrackLayout.IslandKind.Jungle, beach = 4f, palms = 18, huts = 3, seed = 5 },
                new() { points = WESTBANK, kind = TrackLayout.IslandKind.Jungle, beach = 4f, palms = 18, huts = 2, seed = 6 },
                new() { points = NW, kind = TrackLayout.IslandKind.Jungle, beach = 3.5f, palms = 7, huts = 1, seed = 7 },
            };
            // The lighthouse on its rock, a tiki bar and flamingos on the beaches (hand placed).
            t.decor = new List<TrackLayout.Decor>
            {
                new() { cell = "Lighthouse", uv = LagoonSheet.Cell("Lighthouse"), position = new Vector2(42f, 140f), size = 13f },
                new() { cell = "Flamingos", uv = LagoonSheet.Cell("Flamingos"), position = new Vector2(-10f, -14f), size = 7f, angle = 20f },
                new() { cell = "Flamingos", uv = LagoonSheet.Cell("Flamingos"), position = new Vector2(18f, 20f), size = 6.5f, angle = -40f },
                new() { cell = "Umbrella", uv = LagoonSheet.Cell("Umbrella"), position = new Vector2(-30f, -13f), size = 6f, angle = 10f },
                new() { cell = "Umbrella", uv = LagoonSheet.Cell("Umbrella"), position = new Vector2(46f, -13.5f), size = 6f, angle = -15f },
                new() { cell = "Canoe", uv = LagoonSheet.Cell("Canoe"), position = new Vector2(30f, -17f), size = 6f, angle = 70f },
            };
            t.swells = new List<TrackLayout.Swell> { new() { from = 396f, to = 472f, speed = 8f, period = 3.4f } };
            t.currents = new List<TrackLayout.Current>
            {
                new() { from = 158f, to = 206f, lateral = 0f, width = 4.5f, push = 0.3f },
                new() { from = 298f, to = 338f, lateral = 0f, width = 4f, push = 0.25f },
            };
            t.dolphins = new List<float> { 175f, 262f };
            // A sandbar joins the islands across the channel: a trick jump over it (swipe the arrows).
            t.gaps = new List<TrackLayout.Gap> { new() { from = 107f, to = 121f, trickJump = true, tricks = 2, rampLateral = 0f } };
            // Flying fish leaping across the channel (a hit spins you round).
            t.geysers = new List<TrackLayout.Geyser>
            {
                new() { distance = 192f, side = 1f, period = 4.2f, active = 1f },
                new() { distance = 342f, side = -1f, period = 4.8f, active = 1f, offset = 1.7f },
            };
            t.footbridges = new List<float> { 40f };
            t.bouncers = new List<TrackLayout.Bouncer>
            {
                new() { position = At(68f, 1.5f), radius = 2.2f, airTime = 0.85f, speedBoost = 1.1f },
                new() { position = At(358f, -1.5f), radius = 2.2f, airTime = 0.85f, speedBoost = 1.1f },
            };
            t.rings = new List<TrackLayout.Ring> { new() { distance = 77f, lateral = 1.5f }, new() { distance = 367f, lateral = -1.5f } };
            t.dashPanels = new List<TrackLayout.DashPanel>
            {
                new() { distance = 140f, lateral = 0f },
                new() { distance = 232f, lateral = -1.5f },
                new() { distance = 500f, lateral = 1.5f },
            };
            t.boats = new List<TrackLayout.Boat>
            {
                new() { kind = TrackLayout.BoatKind.Sailboat, position = new Vector2(104f, 30f), angle = 20f, drift = 6f },
                new() { kind = TrackLayout.BoatKind.Sailboat, position = new Vector2(-78f, 96f), angle = -30f, drift = 8f },
                new() { kind = TrackLayout.BoatKind.Rowboat, position = new Vector2(96f, -14f), angle = 60f },
                new() { kind = TrackLayout.BoatKind.Buoy, position = new Vector2(100f, 140f) },
                new() { kind = TrackLayout.BoatKind.Buoy, position = new Vector2(-62f, 190f) },
                new() { kind = TrackLayout.BoatKind.Sailboat, position = new Vector2(10f, 196f), angle = 80f, drift = 7f },
            };
            Rect Cell(string n) => LagoonSheet.Cell(n);
            var all = new Vector2(0f, path.Length - 1f); // (beside the channel all round)
            t.scenery = new List<TrackScenery.Kind>
            {
                new() { name = "Coral", uv = Cell("Coral"), count = 46, sizeRange = new Vector2(3.5f, 6.5f), clearance = new Vector2(1.2f, 9f), along = all },
                new() { name = "CoralPink", uv = Cell("CoralPink"), count = 40, sizeRange = new Vector2(3.5f, 6.5f), clearance = new Vector2(1.2f, 9f), along = all },
                new() { name = "Seaweed", uv = Cell("Seaweed"), count = 34, sizeRange = new Vector2(3.5f, 5.5f), clearance = new Vector2(1f, 9f), along = all },
                new() { name = "Turtle", uv = Cell("Turtle"), count = 10, sizeRange = new Vector2(2.6f, 3.4f), clearance = new Vector2(1.5f, 7f), along = all },
                new() { name = "Rock", uv = Cell("Rock"), count = 8, sizeRange = new Vector2(3f, 5f), clearance = new Vector2(5f, 0f) },
                new() { name = "Canoe", uv = Cell("Canoe"), count = 3, sizeRange = new Vector2(5f, 6f), clearance = new Vector2(8f, 0f) },
            };
            t.bakedSignature = null;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return t;
        }

        static LevelDefinition Level(string file, string title, TrackLayout track)
        {
            var level = Asset<LevelDefinition>($"Assets/Data/Levels/{file}.asset");
            level.displayName = title;
            level.chapter = "WATER CUP · RACE 1";
            level.track = track;
            level.laps = 3;
            level.description = "Three laps round the tropical lagoon in speedboats: under the beach bridge, round Palm Island and the lighthouse rock. Catch the swells for big air, let the currents carry you, and mind the dolphins!";
            level.difficulty = 1;
            level.rivalCount = 7;
            level.rivalSpeedScale = 0.87f;
            level.rivalCornerScale = 1f;
            level.starsRequired = 0;
            EditorUtility.SetDirty(level);
            return level;
        }

        static readonly Vector2[] WAYPOINTS = new Vector2[] { new(0.0f, 0.0f), new(28.1f, 0.0f), new(54.6f, 4.7f), new(74.9f, 18.7f), new(82.7f, 40.6f), new(76.4f, 62.4f), new(59.3f, 74.9f), new(45.2f, 90.5f), new(48.4f, 110.8f), new(65.5f, 123.2f), new(73.3f, 143.5f), new(62.4f, 162.2f), new(40.6f, 168.5f), new(20.3f, 159.1f), new(10.9f, 140.4f), new(-4.7f, 124.8f), new(-25.0f, 120.1f), new(-43.7f, 107.6f), new(-49.9f, 84.2f), new(-40.6f, 62.4f), new(-45.2f, 39.0f), new(-39.0f, 17.2f), new(-21.8f, 3.1f) };
        static readonly Vector2[] BEACH = new Vector2[] { new(-58.0f, -16.0f), new(-50.4f, -15.2f), new(-42.8f, -14.4f), new(-35.2f, -13.6f), new(-27.6f, -12.8f), new(-18.6f, -6.2f), new(-11.5f, -7.7f), new(-3.9f, -8.2f), new(4.0f, -8.1f), new(12.1f, -8.6f), new(19.9f, -8.5f), new(27.6f, -7.9f), new(35.6f, -7.4f), new(43.0f, -6.3f), new(52.0f, -14.0f), new(59.0f, -17.0f), new(66.0f, -20.0f), new(73.0f, -23.0f), new(80.0f, -26.0f), new(77.3f, -32.7f), new(74.7f, -39.3f), new(72.0f, -46.0f), new(64.6f, -46.6f), new(57.1f, -47.1f), new(49.7f, -47.7f), new(42.3f, -48.3f), new(34.9f, -48.9f), new(27.4f, -49.4f), new(20.0f, -50.0f), new(12.9f, -49.7f), new(5.7f, -49.4f), new(-1.4f, -49.1f), new(-8.6f, -48.9f), new(-15.7f, -48.6f), new(-22.9f, -48.3f), new(-30.0f, -48.0f), new(-36.8f, -45.2f), new(-43.6f, -42.4f), new(-50.4f, -39.6f), new(-57.2f, -36.8f), new(-64.0f, -34.0f), new(-61.0f, -25.0f) }, PALM = new Vector2[] { new(-25.8f, 18.9f), new(-16.9f, 20.1f), new(-10.2f, 19.0f), new(-3.5f, 18.0f), new(3.2f, 16.9f), new(9.9f, 15.8f), new(15.9f, 16.2f), new(21.9f, 16.7f), new(28.0f, 17.1f), new(34.0f, 17.6f), new(40.0f, 18.0f), new(45.3f, 22.7f), new(50.7f, 27.3f), new(56.0f, 32.0f), new(59.0f, 40.0f), new(62.0f, 48.0f), new(56.7f, 52.0f), new(51.3f, 56.0f), new(46.0f, 60.0f), new(41.3f, 65.3f), new(36.7f, 70.7f), new(32.0f, 76.0f), new(37.1f, 84.0f), new(35.4f, 87.9f), new(35.1f, 92.4f), new(35.2f, 97.4f), new(24.7f, 104.3f), new(19.3f, 108.7f), new(14.0f, 113.0f), new(6.4f, 111.3f), new(-3.2f, 113.6f), new(-9.1f, 111.8f), new(-15.9f, 105.3f), new(-23.0f, 102.7f), new(-32.5f, 103.2f), new(-29.7f, 93.1f), new(-29.4f, 86.2f), new(-29.2f, 79.3f), new(-32.3f, 69.5f), new(-30.8f, 64.7f), new(-30.2f, 59.5f), new(-31.1f, 54.1f), new(-32.7f, 48.9f), new(-34.7f, 42.1f), new(-34.1f, 33.9f), new(-31.5f, 26.2f) }, LIGHT = new Vector2[] { new(60.8f, 137.0f), new(56.9f, 152.4f), new(46.6f, 157.0f), new(36.7f, 156.2f), new(27.1f, 149.3f), new(24.3f, 142.4f), new(32.3f, 132.9f), new(44.5f, 122.3f), new(48.2f, 124.8f), new(54.0f, 128.3f) },
            EASTBANK = new Vector2[] { new(66.6f, -3.2f), new(73.1f, 3.2f), new(79.5f, 9.7f), new(84.7f, 17.4f), new(88.2f, 25.7f), new(90.3f, 34.2f), new(90.9f, 43.4f), new(89.6f, 52.2f), new(87.0f, 60.5f), new(82.3f, 69.2f), new(74.8f, 76.1f), new(68.9f, 80.5f), new(65.8f, 85.4f), new(71.9f, 92.4f), new(74.7f, 90.3f), new(82.4f, 88.3f), new(91.4f, 77.0f), new(98.9f, 65.7f), new(105.2f, 56.0f), new(106.4f, 44.3f), new(104.7f, 31.6f), new(101.8f, 20.7f), new(95.9f, 10.8f), new(89.7f, 0.0f), new(80.8f, -7.4f), new(69.8f, -9.2f) }, NORTHBANK = new Vector2[] { new(85.5f, 141.4f), new(81.2f, 152.5f), new(75.9f, 160.7f), new(69.1f, 167.7f), new(60.4f, 172.8f), new(51.8f, 175.7f), new(42.4f, 177.0f), new(32.3f, 176.0f), new(23.4f, 173.2f), new(13.8f, 169.7f), new(8.6f, 175.9f), new(17.9f, 183.7f), new(28.7f, 187.4f), new(42.3f, 191.3f), new(55.3f, 190.2f), new(66.7f, 186.7f), new(76.9f, 176.6f), new(85.1f, 166.7f), new(93.5f, 157.2f), new(95.0f, 141.1f) }, WESTBANK = new Vector2[] { new(-35.7f, 128.6f), new(-42.4f, 122.4f), new(-49.7f, 115.0f), new(-54.4f, 106.2f), new(-56.9f, 97.5f), new(-58.1f, 88.5f), new(-56.8f, 77.7f), new(-52.9f, 69.5f), new(-50.0f, 63.7f), new(-49.8f, 60.7f), new(-51.7f, 55.5f), new(-56.1f, 48.4f), new(-62.9f, 50.2f), new(-63.4f, 59.2f), new(-60.1f, 61.5f), new(-60.2f, 59.0f), new(-64.9f, 62.2f), new(-67.2f, 73.9f), new(-69.3f, 89.2f), new(-69.8f, 100.7f), new(-65.9f, 111.4f), new(-58.1f, 122.0f), new(-48.2f, 131.1f), new(-38.4f, 134.1f) }, NW = new Vector2[] { new(-62.0f, 150.0f), new(-64.9f, 156.4f), new(-72.0f, 159.0f), new(-79.1f, 156.4f), new(-82.0f, 150.0f), new(-79.1f, 143.6f), new(-72.0f, 141.0f), new(-64.9f, 143.6f) };
    }
}
