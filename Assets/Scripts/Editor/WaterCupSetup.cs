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
            WaterArt.GenerateHarbour();
            WaterArt.GenerateCoast();
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
            var harbour = BuildHarborHavoc();
            var level2 = HarbourLevel(harbour);
            var coast = BuildLighthouseRun();
            var level3 = CoastLevel(coast);
            var catalog = Asset<LevelCatalog>("Assets/Data/Levels/WaterCatalog.asset");
            catalog.levels = new List<LevelDefinition> { level, level2, level3 };
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
            cup.comingSoonRaces = new List<string> { "Jungle Rapids", "Pirate Bay", "Coral Coast", "Storm Chasers", "Storm Rally" };
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
            lso.FindProperty("quay").objectReferenceValue = Mat("QuayStoneMat");
            lso.FindProperty("harbourScenery").objectReferenceValue = Mat("SceneryHarbourWater");
            lso.FindProperty("barge").objectReferenceValue = Sprite("Barge");
            lso.ApplyModifiedPropertiesWithoutUndo();

            // The ferry route and the other branches on the water: channel water with foam edges.
            var branches = Object.FindAnyObjectByType<TrackBranches>(FindObjectsInactive.Include);
            if (branches != null)
            {
                var bso = new SerializedObject(branches);
                bso.FindProperty("lagoonRoad").objectReferenceValue = Mat("LagoonChannel");
                bso.FindProperty("lagoonEdges").objectReferenceValue = Mat("LagoonFoamMat");
                bso.ApplyModifiedPropertiesWithoutUndo();
            }

            // The harbour's moving set pieces.
            var harbour = Object.FindAnyObjectByType<TrackHarbour>(FindObjectsInactive.Include);
            if (harbour == null) harbour = trackObject.gameObject.AddComponent<TrackHarbour>();
            var hso = new SerializedObject(harbour);
            hso.FindProperty("track").objectReferenceValue = trackObject;
            hso.FindProperty("plain").objectReferenceValue = Mat("Plain");
            foreach (var (field, sprite) in new[] { ("ferry", "Ferry"), ("ferryDeck", "FerryDeck"), ("boom", "Boom"), ("container", "Container"), ("duck", "RubberDuck"),
                         ("cruiseShip", "CruiseShip"), ("fireboat", "Fireboat"), ("waterArc", "WaterArc"), ("seaplane", "SeaplaneTop"), ("screenDrop", "ScreenDrop"), ("slideTower", "SlideTower") })
                hso.FindProperty(field).objectReferenceValue = Sprite(sprite);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // The night coast.
            var coastPart = Object.FindAnyObjectByType<TrackCoast>(FindObjectsInactive.Include);
            if (coastPart == null) coastPart = trackObject.gameObject.AddComponent<TrackCoast>();
            var cso = new SerializedObject(coastPart);
            cso.FindProperty("track").objectReferenceValue = trackObject;
            cso.FindProperty("plain").objectReferenceValue = Mat("Plain");
            cso.FindProperty("sand").objectReferenceValue = Mat("LagoonSandMat");
            cso.FindProperty("foamParticles").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BoatFoam.mat");
            cso.FindProperty("wreck").objectReferenceValue = Sprite("Wreck");
            cso.FindProperty("rock").objectReferenceValue = Sprite("CoastRock");
            cso.ApplyModifiedPropertiesWithoutUndo();
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

        // ------------------------------------------------------------------ race 2: Harbor Havoc

        static TrackLayout BuildHarborHavoc()
        {
            var t = Asset<TrackLayout>("Assets/Data/Tracks/HarborHavoc.asset");
            t.waypoints = HARBOUR;
            t.roadWidth = 7.5f; // (narrower than the lagoon: this is a harder race)
            t.rampWidth = 3.4f;
            // The duck basin: the channel opens out where the ducks bob.
            t.widths = new List<TrackLayout.WidthKey>
            {
                new() { distance = 425f, width = 7.5f }, new() { distance = 440f, width = 15f },
                new() { distance = 468f, width = 15f }, new() { distance = 482f, width = 7.5f },
            };
            t.kerbWidth = 0.7f;
            t.sampleSpacing = 1f;
            t.checkpointCount = 8;
            t.boundaryMargin = 28f;
            t.biome = TrackLayout.Biome.Lagoon;
            t.vehicle = TrackLayout.Vehicle.Boat;
            t.sceneryMaterial = Mat("SceneryHarbourWater");
            t.skyLife = 0f;
            var path = new TrackPath(t.waypoints, t.sampleSpacing);
            Vector2 At(float d, float lateral = 0f) => path.PointAt(d) + path.NormalAt(d) * lateral;

            // The waterfront: the fish market quay, the water park's beach, the container port, the old town inside
            // the loop and the ferry pier.
            t.lagoonIslands = new List<TrackLayout.Island>
            {
                new() { points = SOUTH, kind = TrackLayout.IslandKind.Quay, beach = 2f, palms = 40, huts = 9, seed = 11 },
                new() { points = WEST, kind = TrackLayout.IslandKind.Jungle, beach = 7f, palms = 30, huts = 2, seed = 12 },
                new() { points = NORTH, kind = TrackLayout.IslandKind.Quay, beach = 2f, palms = 40, huts = 8, seed = 13 },
                new() { points = TOWN, kind = TrackLayout.IslandKind.Quay, beach = 2f, palms = 90, huts = 26, seed = 14 },
                new() { points = PIER, kind = TrackLayout.IslandKind.Quay, beach = 2f, palms = 6, huts = 1, seed = 15 },
            };
            Rect H(string n) => HarbourSheet.Cell(n);
            t.decor = new List<TrackLayout.Decor>
            {
                new() { cell = "FishStall", uv = H("FishStall"), position = new Vector2(12f, -12f), size = 6f, angle = 0f },
                new() { cell = "FishStall", uv = H("FishStall"), position = new Vector2(38f, -13f), size = 6f, angle = 10f },
                new() { cell = "Nets", uv = H("Nets"), position = new Vector2(2f, -12f), size = 5f, angle = 30f },
                new() { cell = "People", uv = H("People"), position = new Vector2(24f, -19f), size = 4.5f, angle = 0f },
                new() { cell = "Seal", uv = H("Seal"), position = new Vector2(150f, 30f), size = 3.5f, angle = 40f },
                new() { cell = "Seal", uv = H("Seal"), position = new Vector2(-30f, 40f), size = 3.5f, angle = -20f },
                new() { cell = "Cafe", uv = H("Cafe"), position = new Vector2(-36f, 60f), size = 7f, angle = 0f },
                new() { cell = "Lifebuoys", uv = H("Lifebuoys"), position = new Vector2(-24f, 30f), size = 4f, angle = 0f },
            };
            // A branch through the ferry: while it is docked (doors open) you race in over its bow and out of its stern.
            t.branches = new List<TrackLayout.Branch>
            {
                new() { kind = TrackLayout.BranchKind.Road, from = 50f, to = 165f, width = 7f,
                        points = new[] { new Vector2(62f, -1f), new Vector2(76f, 0f), new Vector2(90f, 0f), new Vector2(104f, 0f), new Vector2(116f, 2f), new Vector2(126f, 7f) } },
            };
            t.ferry = new TrackLayout.Ferry { branch = 0, at = 0.5f, docked = 7f, away = 5f };
            // Out at sea beside the long straight the cruise ship steams by; its wake rolls in as swells (WAVE JUMPs).
            t.cruiseShip = new TrackLayout.CruiseShip { enabled = true, from = 150f, to = 255f, lateral = -22f, speed = 6f };
            t.swells = new List<TrackLayout.Swell> { new() { from = 170f, to = 248f, speed = 8f, period = 3.2f } };
            t.seaplane = new TrackLayout.Seaplane { enabled = true, from = 180f, to = 245f, dropAt = 340f }; // (drive onto its floats: it flies you over the old town)
            // Fireboats spraying across the channel: at the fish market and out on the straight.
            t.fireJets = new List<TrackLayout.FireJet>
            {
                new() { distance = 24f, side = -1f, period = 4f },
                new() { distance = 215f, side = -1f, period = 4.6f, offset = 1.5f },
            };
            // The container port: two gantry cranes swinging containers across, then the barge to jump.
            t.cranes = new List<TrackLayout.Crane> { new() { distance = 296f, period = 5f }, new() { distance = 316f, period = 5.6f, offset = 2.2f } };
            t.gaps = new List<TrackLayout.Gap> { new() { from = 356f, to = 368f, trickJump = true, tricks = 3, barge = true, rampLateral = 0f } };
            // The water park: the giant slide (fast) or the long way through the rubber duck regatta.
            t.duckBasins = new List<Vector2> { new(432f, 476f) };
            t.ducks = 160;
            t.rides = new List<TrackLayout.Ride>
            {
                new() { type = TrackLayout.RideType.Flume, from = 405f, lateral = -3f, to = 482f, toLateral = -2f, speed = 30f,
                        points = new[] { At(414f, -10f), At(426f, -15f), At(441f, -19f), At(456f, -19.5f), At(470f, -15f) } },
            };
            t.currents = new List<TrackLayout.Current>();
            t.dolphins = new List<float>();
            t.footbridges = new List<float> { 270f };
            t.geysers = new List<TrackLayout.Geyser>();
            t.bouncers = new List<TrackLayout.Bouncer>
            {
                new() { position = At(140f, 1.5f), radius = 2.2f, airTime = 0.85f, speedBoost = 1.1f },
            };
            t.rings = new List<TrackLayout.Ring> { new() { distance = 149f, lateral = 1.5f } };
            t.dashPanels = new List<TrackLayout.DashPanel>
            {
                new() { distance = 8f, lateral = 1.5f },
                new() { distance = 336f, lateral = -1.5f },
                new() { distance = 500f, lateral = 0f },
            };
            t.boats = new List<TrackLayout.Boat>
            {
                new() { kind = TrackLayout.BoatKind.Sailboat, position = new Vector2(176f, 0f), angle = 20f, drift = 6f },
                new() { kind = TrackLayout.BoatKind.Sailboat, position = new Vector2(190f, 90f), angle = -30f, drift = 8f },
                new() { kind = TrackLayout.BoatKind.Trawler, position = new Vector2(168f, 150f), angle = 100f },
                new() { kind = TrackLayout.BoatKind.Buoy, position = new Vector2(150f, -20f) },
                new() { kind = TrackLayout.BoatKind.Buoy, position = new Vector2(152f, 60f) },
                new() { kind = TrackLayout.BoatKind.Rowboat, position = new Vector2(12f, 0f) + new Vector2(0f, -1f) * 9.5f + new Vector2(12f, -1.5f), angle = 80f },
            };
            // Out on the open water only a few seals (the quays bring their own bits and pieces).
            t.lastLapShow = true; // (the sun sets on the last lap, fireworks over the harbour)
            t.scenery = new List<TrackScenery.Kind>
            {
                new() { name = "Seal", uv = H("Seal"), count = 8, sizeRange = new Vector2(2.6f, 3.4f), clearance = new Vector2(3f, 30f) },
            };
            t.bakedSignature = null;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return t;
        }

        static LevelDefinition HarbourLevel(TrackLayout track)
        {
            var level = Asset<LevelDefinition>("Assets/Data/Levels/Water02.asset");
            level.displayName = "Harbor Havoc";
            level.chapter = "WATER CUP · RACE 2";
            level.track = track;
            level.laps = 3;
            level.description = "Rush hour in the busiest harbour in the tropics. Hop the ferry while its doors are open, dodge the cranes' containers and the fireboats, jump the barge, take the giant slide or plough through the rubber ducks, and beat the seaplane!";
            level.difficulty = 3;
            level.rivalCount = 7;
            level.rivalSpeedScale = 0.97f;
            level.rivalCornerScale = 1.05f;
            level.starsRequired = 42;
            EditorUtility.SetDirty(level);
            return level;
        }

        // ------------------------------------------------------------------ race 3: Lighthouse Run

        static TrackLayout BuildLighthouseRun()
        {
            var t = Asset<TrackLayout>("Assets/Data/Tracks/LighthouseRun.asset");
            t.waypoints = COAST;
            t.roadWidth = 8f;
            t.rampWidth = 3.4f;
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

            // The cliff (the road runs through it: the sea caves, the locks, the pond on top), its lip over the bay, and
            // the harbour wall and rocky west shore.
            t.lagoonIslands = new List<TrackLayout.Island>
            {
                new() { points = CLIFFN, kind = TrackLayout.IslandKind.Cliff, beach = 4f, palms = 40, seed = 21 },
                new() { points = LIP, kind = TrackLayout.IslandKind.Cliff, beach = 3f, palms = 8, seed = 22 },
                new() { points = SHORE, kind = TrackLayout.IslandKind.Cliff, beach = 4f, palms = 30, seed = 23 },
            };
            t.decor = new List<TrackLayout.Decor>
            {
                new() { cell = "Lighthouse", uv = LagoonSheet.Cell("Lighthouse"), position = new Vector2(-2f, 154f), size = 14f },
            };
            foreach (var light in LIGHTS) t.decor.Add(new() { cell = "Lighthouse", uv = LagoonSheet.Cell("Lighthouse"), position = light, size = 7f });
            // Across the tide flats: only once the tide is in (the last lap).
            t.branches = new List<TrackLayout.Branch>
            {
                new() { kind = TrackLayout.BranchKind.Road, from = 57f, to = 188f, width = 6.5f, openFromLap = 3,
                        points = new[] { new Vector2(70f, 16f), new Vector2(82f, 36f), new Vector2(94f, 56f) } },
            };
            t.coast = new TrackLayout.Coast
            {
                enabled = true, lighthouse = new Vector2(-2f, 154f), beamPeriod = 7f, darkness = 0.95f,
                flats = FLATS, tideBranch = 0,
                // Small lighthouses all round the course (the only light besides the big one): on the cliffs and shores,
                // or out on rocks in the bay.
                lights = LIGHTS,
                rocks = new List<TrackLayout.TideRock>
                {
                    new() { position = At(20f, 8.5f), radius = 1.8f },
                    new() { position = At(72f, -8.5f), radius = 1.6f, untilLap = 3 },
                    new() { position = At(140f, 3.2f), radius = 1.4f, untilLap = 3 },
                    new() { position = At(160f, -3.4f), radius = 1.5f },
                    new() { position = At(176f, 2.8f), radius = 1.3f, untilLap = 2 },
                    new() { position = At(400f, 3f), radius = 1.4f, untilLap = 3 },
                    new() { position = At(462f, -3.2f), radius = 1.5f },
                    new() { position = At(480f, 8f), radius = 1.8f },
                    // Either side of the way across the flats.
                    new() { position = new Vector2(71.8f, 28.6f), radius = 1.5f },
                    new() { position = new Vector2(80.3f, 23.4f), radius = 1.3f },
                    new() { position = new Vector2(83.8f, 48.6f), radius = 1.4f },
                    new() { position = new Vector2(92.3f, 43.4f), radius = 1.5f },
                },
                gates = new[] { 258f, 278f, 298f }, top = 368f, gatePeriod = 3.6f, gateOpen = 0.55f,
                wreck = 430f,
            };
            // The sea caves into the cliff, then the Leap of Faith off the cliff edge where the pond spills over.
            t.caves = new List<TrackLayout.Cave> { new() { branch = -1, from = 208f, to = 242f } };
            t.gaps = new List<TrackLayout.Gap> { new() { from = 368f, to = 390f, drop = true, slowMotion = true, trickJump = true, tricks = 3 } };
            // The whirlpool by the reef, the gale's swells on the open sea.
            t.vortices = new List<TrackLayout.Vortex> { new() { position = At(110f, -17f), radius = 16f, pull = 4f } };
            t.swells = new List<TrackLayout.Swell> { new() { from = 92f, to = 170f, speed = 8f, period = 3f } };
            t.currents = new List<TrackLayout.Current>();
            t.dolphins = new List<float>();
            t.footbridges = new List<float>();
            t.geysers = new List<TrackLayout.Geyser>();
            t.bouncers = new List<TrackLayout.Bouncer>();
            t.rings = new List<TrackLayout.Ring> { new() { distance = 330f, lateral = 0f } };
            t.dashPanels = new List<TrackLayout.DashPanel>
            {
                new() { distance = 10f, lateral = -1.5f },
                new() { distance = 320f, lateral = 1.5f },
                new() { distance = 452f, lateral = 0f },
            };
            t.boats = new List<TrackLayout.Boat>
            {
                new() { kind = TrackLayout.BoatKind.Trawler, position = new Vector2(150f, 60f), angle = 100f },
                new() { kind = TrackLayout.BoatKind.Buoy, position = At(124f, 9f) },
                new() { kind = TrackLayout.BoatKind.Buoy, position = At(445f, 9f) },
            };
            t.scenery = new List<TrackScenery.Kind>
            {
                new() { name = "Rock", uv = LagoonSheet.Cell("Rock"), count = 12, sizeRange = new Vector2(3f, 5f), clearance = new Vector2(4f, 30f) },
            };
            t.lastLapShow = false;
            t.bakedSignature = null;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return t;
        }

        static LevelDefinition CoastLevel(TrackLayout track)
        {
            var level = Asset<LevelDefinition>("Assets/Data/Levels/Water03.asset");
            level.displayName = "Lighthouse Run";
            level.chapter = "WATER CUP · RACE 3";
            level.track = track;
            level.laps = 3;
            level.description = "A wild coast at night in a gale. Only the lighthouse beam and your headlights show the rocks. Brave the whirlpool, race through the sea caves, ride the locks up the cliff and take the Leap of Faith - and the tide rises every lap.";
            level.difficulty = 4;
            level.rivalCount = 7;
            level.rivalSpeedScale = 0.98f;
            level.rivalCornerScale = 1.05f;
            level.starsRequired = 44;
            EditorUtility.SetDirty(level);
            return level;
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

        static readonly Vector2[] HARBOUR = new Vector2[] { new(0.0f, 0.0f), new(28.0f, 0.0f), new(52.0f, -4.0f), new(66.0f, -18.0f), new(86.0f, -28.0f), new(110.0f, -24.0f), new(126.0f, -8.0f), new(136.0f, 16.0f), new(140.0f, 46.0f), new(138.0f, 78.0f), new(132.0f, 106.0f), new(118.0f, 126.0f), new(96.0f, 136.0f), new(72.0f, 130.0f), new(50.0f, 138.0f), new(28.0f, 138.0f), new(8.0f, 126.0f), new(-4.0f, 104.0f), new(-10.0f, 80.0f), new(-12.0f, 54.0f), new(-8.0f, 34.0f), new(-14.0f, 16.0f) };
        static readonly Vector2[] SOUTH = new Vector2[] { new(-70.0f, -60.9f), new(-16.7f, 3.1f), new(-14.6f, 2.8f), new(-8.0f, -3.3f), new(-1.0f, -6.8f), new(17.3f, -7.5f), new(20.0f, -12.9f), new(23.2f, -14.8f), new(28.1f, -13.4f), new(31.7f, -7.2f), new(47.7f, -9.5f), new(51.4f, -11.8f), new(60.0f, -21.9f), new(69.8f, -28.7f), new(79.7f, -33.4f), new(89.1f, -35.0f), new(103.5f, -33.4f), new(115.3f, -28.8f), new(126.4f, -18.9f), new(136.7f, -2.8f), new(138.9f, -2.0f), new(140.9f, -3.7f), new(141.0f, -30.1f), new(128.5f, -73.2f), new(126.9f, -74.9f), new(-67.7f, -75.0f), new(-70.0f, -73.0f) },
            WEST = new Vector2[] { new(-70.0f, 214.9f), new(3.5f, 141.4f), new(7.6f, 134.8f), new(-1.3f, 125.0f), new(-9.2f, 109.2f), new(-15.8f, 100.3f), new(-21.0f, 77.6f), new(-22.2f, 61.3f), new(-14.7f, 34.9f), new(-20.0f, 22.6f), new(-20.8f, 16.0f), new(-17.6f, 7.3f), new(-11.2f, -0.7f), new(-70.0f, -70.9f) },
            NORTH = new Vector2[] { new(-70.0f, 212.5f), new(-67.5f, 215.0f), new(148.5f, 214.8f), new(150.0f, 212.5f), new(150.0f, 170.0f), new(141.0f, 122.1f), new(140.8f, 113.1f), new(139.0f, 111.6f), new(136.9f, 112.1f), new(128.3f, 125.3f), new(118.0f, 134.4f), new(108.0f, 139.8f), new(98.2f, 142.5f), new(90.0f, 142.2f), new(72.2f, 136.8f), new(52.5f, 144.4f), new(32.5f, 145.7f), new(18.9f, 141.8f), new(4.2f, 131.8f), new(1.8f, 131.7f), new(-2.0f, 137.0f), new(-69.4f, 204.4f) },
            TOWN = new Vector2[] { new(40.7f, 132.4f), new(50.2f, 131.0f), new(64.0f, 124.9f), new(71.4f, 123.3f), new(78.5f, 124.1f), new(93.6f, 129.2f), new(99.0f, 128.6f), new(111.2f, 122.7f), new(120.4f, 113.3f), new(125.8f, 103.4f), new(128.6f, 94.1f), new(133.3f, 53.8f), new(132.6f, 35.8f), new(129.5f, 18.0f), new(122.6f, 12.1f), new(114.1f, 7.9f), new(108.0f, 6.9f), new(72.0f, 7.2f), new(72.0f, 5.8f), new(60.9f, 5.0f), new(53.3f, 2.8f), new(44.4f, 5.2f), new(28.5f, 6.7f), new(3.6f, 6.0f), new(-0.8f, 8.2f), new(-5.0f, 12.6f), new(-7.4f, 17.6f), new(-2.5f, 28.0f), new(-1.2f, 34.8f), new(-5.0f, 51.7f), new(-1.6f, 61.1f), new(3.2f, 93.0f), new(3.5f, 104.6f), new(9.9f, 117.5f), new(17.4f, 125.3f), new(29.0f, 131.2f) },
            PIER = new Vector2[] { new(65.2f, -7.3f), new(72.0f, -6.4f), new(108.0f, -7.2f), new(108.0f, -5.5f), new(117.6f, -3.9f), new(121.8f, -1.9f), new(116.0f, -10.4f), new(109.9f, -16.2f), new(103.5f, -19.4f), new(93.8f, -21.4f), new(86.2f, -21.2f), new(80.4f, -19.1f), new(69.4f, -12.1f) };

        static readonly Vector2[] LIGHTS = { new(-1.5f, -11.9f), new(44.7f, -14.6f), new(84.9f, -17f), new(130.6f, 15.6f), new(129.7f, 62.1f), new(112.9f, 102.5f),
            new(72.8f, 121.2f), new(35.6f, 124f), new(-4.2f, 122.4f), new(-24.5f, 104f), new(-27.7f, 66.7f), new(-19.1f, 30.5f) };
        static readonly Vector2[] COAST = new Vector2[] { new(0.0f, 0.0f), new(28.0f, -2.0f), new(56.0f, 2.0f), new(84.0f, -2.0f), new(108.0f, 10.0f), new(120.0f, 32.0f), new(118.0f, 58.0f), new(104.0f, 78.0f), new(98.0f, 100.0f), new(92.0f, 120.0f), new(78.0f, 132.0f), new(56.0f, 136.0f), new(32.0f, 136.0f), new(8.0f, 136.0f), new(-14.0f, 132.0f), new(-30.0f, 120.0f), new(-38.0f, 100.0f), new(-40.0f, 76.0f), new(-38.0f, 52.0f), new(-32.0f, 30.0f), new(-22.0f, 12.0f), new(-12.0f, 3.0f) };
        static readonly Vector2[] CLIFFN = new Vector2[] { new(200.0f, 230.0f), new(200.0f, 96.0f), new(140.0f, 96.0f), new(120.0f, 98.0f), new(104.5f, 102.6f), new(99.3f, 121.3f), new(95.2f, 127.5f), new(89.6f, 133.1f), new(76.0f, 140.1f), new(52.7f, 143.2f), new(11.3f, 143.2f), new(-8.3f, 141.0f), new(-18.9f, 137.5f), new(-29.0f, 131.1f), new(-36.3f, 123.3f), new(-41.4f, 113.2f), new(-60.0f, 110.0f), new(-110.0f, 108.0f), new(-110.0f, 230.0f) },
            LIP = new Vector2[] { new(89.1f, 107.1f), new(70.0f, 114.0f), new(50.0f, 117.0f), new(10.0f, 118.0f), new(-25.3f, 114.5f), new(-16.6f, 122.8f), new(-4.4f, 127.5f), new(12.9f, 129.3f), new(53.3f, 129.2f), new(70.5f, 127.1f), new(79.2f, 123.4f), new(86.9f, 114.6f) },
            SHORE = new Vector2[] { new(-60.0f, 60.0f), new(-50.0f, 20.0f), new(-34.0f, 0.0f), new(-14.0f, -8.0f), new(10.0f, -11.0f), new(120.0f, -14.0f), new(150.0f, -24.0f), new(200.0f, -22.0f), new(200.0f, -70.0f), new(-110.0f, -70.0f), new(-110.0f, 108.0f), new(-58.0f, 100.0f) },
            FLATS = new Vector2[] { new(96.0f, 74.0f), new(98.7f, 73.1f), new(106.5f, 63.1f), new(110.0f, 56.0f), new(110.0f, 32.0f), new(98.0f, 12.0f), new(74.0f, 8.0f), new(70.8f, 6.6f), new(63.2f, 8.3f) };
    }
}
