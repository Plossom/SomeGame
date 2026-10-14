using SomeGame.Input;
using SomeGame.Race;
using SomeGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Builds the Alpine Rally UI: the Map scene (start screen) and the Race scene's HUD, countdown,
    /// results and menu, wired to the runtime components. Re-running replaces each scene's "UI" canvas.
    /// Units are canvas units of the 1170x2532 reference resolution. Look: flat cream, pine-green and
    /// orange shapes with hard drop shadows and condensed italic type.
    /// </summary>
    public static class UIBuilder
    {
        const string Art = ArtGenerator.Folder + "/";
        const string FontFolder = "Assets/Art/Fonts/Barlow/";
        public const string MapScenePath = "Assets/Scenes/Map.unity";
        public const string RaceScenePath = "Assets/Scenes/Race.unity";

        static TMP_FontAsset _semi, _bold, _cond, _condItalic, _black;
        static Material _blackShadow, _blackOutline, _condOutline;

        [MenuItem("SomeGame/Rebuild UI (Map + Race)")]
        public static void RebuildAll()
        {
            EditorSceneManager.SaveOpenScenes();
            BuildRace();
            BuildMap();
            BuildLoadingScreen();
        }

        // ================================================================== LOADING SCREEN

        public const string LoadingScreenPath = "Assets/Resources/LoadingScreen.prefab";

        [MenuItem("SomeGame/Rebuild Loading Screen")]
        public static void BuildLoadingScreen()
        {
            LoadFonts();
            var canvas = CreateCanvas();
            canvas.name = "LoadingScreen";
            canvas.GetComponent<Canvas>().sortingOrder = 100;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            Img(Stretch("Background", canvas), null, Theme.Cream, raycast: true);
            Img(At("HillBack", canvas, new Vector2(0.5f, 0f), new Vector2(-300f, -760f), new Vector2(2000f, 1500f)), "Circle", Hex("9CC383")).preserveAspect = false;
            Img(At("HillFront", canvas, new Vector2(0.5f, 0f), new Vector2(260f, -960f), new Vector2(2200f, 1600f)), "Circle", Hex("7FB06C")).preserveAspect = false;
            var safe = SafeArea(canvas);
            var band = At("Checker", safe, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1400f, 80f));
            var checker = Img(band, "Checker", Color.white);
            checker.type = Image.Type.Tiled; checker.preserveAspect = false; checker.pixelsPerUnitMultiplier = 1.6f;

            var chapter = Txt(At("Chapter", safe, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1000f, 60f)), "VALLEY CUP · RACE 1", 44, Theme.Rust, _condItalic);
            chapter.characterSpacing = 8f;
            var title = Txt(At("Title", safe, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1050f, 190f)), "MEADOW RUN", 170, Theme.Ink, _black);
            title.enableAutoSizing = true; title.fontSizeMin = 90; title.fontSizeMax = 170;
            var details = Txt(At("Details", safe, new Vector2(0.5f, 1f), new Vector2(0f, -450f), new Vector2(1000f, 60f)), "3 laps  ·  7 rivals", 44, Theme.InkSoft, _bold);

            // The track picture in a framed card with a hard shadow.
            var card = At("Preview", safe, new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(860f, 1500f));
            var cardShadow = Stretch("Shadow", card);
            cardShadow.offsetMin = cardShadow.offsetMax = new Vector2(0f, -22f);
            Img(cardShadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Frame", card), "Round", Theme.Ink, sliced: true);
            var picture = Stretch("Picture", card);
            picture.offsetMin = new Vector2(22f, 22f); picture.offsetMax = new Vector2(-22f, -22f);
            var previewImage = Img(picture, null, Color.white);
            previewImage.preserveAspect = true;

            // Progress bar with the buggy riding along it.
            var bar = At("Bar", safe, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(900f, 56f));
            Img(bar, "Pill", Theme.Ink, sliced: true);
            var fillRt = Stretch("Fill", bar);
            fillRt.offsetMin = new Vector2(8f, 8f); fillRt.offsetMax = new Vector2(-8f, -8f);
            var fill = Img(fillRt, "Pill", Theme.Orange);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0.3f; fill.preserveAspect = false;
            var track = Rt("CarTrack", bar);
            track.anchorMin = new Vector2(0f, 0.5f); track.anchorMax = new Vector2(1f, 0.5f); track.sizeDelta = new Vector2(0f, 0f);
            var car = At("Car", track, new Vector2(0f, 0.5f), new Vector2(0f, 74f), new Vector2(70f, 122f));
            car.pivot = new Vector2(0.5f, 0.5f);
            car.localRotation = Quaternion.Euler(0f, 0f, -90f);
            Img(Stretch("Body", car), "Car", Theme.Orange).preserveAspect = false;
            Img(Stretch("Details", car), "CarDetails", Color.white).preserveAspect = false;
            var percent = Txt(At("Percent", safe, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 70f)), "LOADING 30%", 48, Theme.Ink, _condItalic);
            percent.characterSpacing = 6f;

            var screen = canvas.gameObject.AddComponent<LoadingScreen>();
            var so = new SerializedObject(screen);
            Set(so, "group", group); Set(so, "preview", previewImage); Set(so, "chapter", chapter); Set(so, "title", title);
            Set(so, "details", details); Set(so, "barFill", fill); Set(so, "barCar", car); Set(so, "percent", percent);
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, LoadingScreenPath);
            Object.DestroyImmediate(canvas.gameObject);
        }

        // ================================================================== MAP

        public static void BuildMap()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadFonts();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Cream;
            camGo.AddComponent<AudioListener>();
            new GameObject("Music").AddComponent<SomeGame.Audio.MusicPlayer>(); // plays the map theme
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var canvas = CreateCanvas();

            // The painted valley; stops, road progress and the car live in the same fixed-size space.
            var map = Stretch("Map", canvas);
            Img(map, null, new Color(0f, 0f, 0f, 0f), raycast: true); // catches sideways swipes between cups
            var world = At("World", map, new Vector2(0.5f, 0.5f), Vector2.zero, MapLayout.Size);
            var background = Img(Stretch("Background", world), "MapBackground", Color.white);
            background.preserveAspect = false;
            // Animated life on the landscape (clouds, smoke, glints...), under the road progress, stops and car.
            var ambience = Stretch("Ambience", world);
            var ambienceComp = ambience.gameObject.AddComponent<MapAmbience>();
            Configure(ambienceComp, ("cloud", Sprite("MapCloud")), ("bird", Sprite("MapBird")), ("balloon", Sprite("MapBalloon")),
                ("sparkle", Sprite("Sparkle")), ("puff", Sprite("Smoke")), ("chair", Sprite("LiftChair")),
                ("fisher", Sprite("MapFisher")), ("rowboat", Sprite("MapRowboat")), ("ring", Sprite("Ring")), ("fish", Sprite("Fish")));
            var node = BuildNode(world);
            var lockedPage = BuildLockedPage(map);
            var car = At("Car", world, Vector2.zero, Vector2.zero, new Vector2(64f, 112f));
            car.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", car, new Vector2(0.5f, 0.5f), new Vector2(6f, -8f), new Vector2(70f, 120f)), "CarShadow", new Color(0f, 0f, 0f, 0.45f)).preserveAspect = false;
            Img(Stretch("Body", car), "Car", Theme.Orange).preserveAspect = false;
            Img(Stretch("Details", car), "CarDetails", Color.white).preserveAspect = false;

            // Snowfall over the landscape (under the header), switched on by the winter map.
            var snow = Stretch("Snow", canvas);
            Configure(snow.gameObject.AddComponent<SnowFall>(), ("flake", Sprite("Circle")));
            snow.gameObject.SetActive(false);

            var safe = SafeArea(canvas);
            var menuButton = SquareButton(safe, "MenuButton", new Vector2(0f, 1f), new Vector2(60f, -50f), "IconMenu", 140f);

            // Stars and trophies (races won) on one small plate, its top level with the menu button.
            // Same size as the menu button.
            var counters = At("Counters", safe, new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(140f, 140f));
            // A hard shadow under it like the menu button's (which makes that one 12 units taller).
            var countersShadow = At("Shadow", counters, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(140f, 140f));
            Img(countersShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Face", counters), "Round", Theme.Ink, sliced: true);
            TMP_Text CounterRow(string name, float y, string icon)
            {
                var row = At(name, counters, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(140f, 64f));
                Img(At("Icon", row, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(38f, 38f)), icon, Theme.Amber);
                return Txt(At("Count", row, new Vector2(1f, 0.5f), new Vector2(-20f, -2f), new Vector2(76f, 64f)), "0", 46, Theme.Amber, _condItalic, TextAlignmentOptions.Right);
            }
            var total = CounterRow("Stars", -6f, "IconStar");
            var trophies = CounterRow("Trophies", -70f, "IconTrophy");

            // The cup name, big, centred in the gap between the menu button (ends 200 from the left) and the
            // stars and trophies counter (starts 256 from the right), level with the menu button.
            var cupRt = Rt("Cup", safe);
            cupRt.anchorMin = new Vector2(0f, 1f); cupRt.anchorMax = new Vector2(1f, 1f); cupRt.pivot = new Vector2(0.5f, 1f);
            cupRt.offsetMin = new Vector2(216f, -206f); cupRt.offsetMax = new Vector2(-216f, -40f);
            var chapter = Txt(cupRt, "VALLEY CUP", 128, Theme.Rust, _condItalic);
            chapter.characterSpacing = 6f;
            chapter.enableAutoSizing = true; chapter.fontSizeMin = 70; chapter.fontSizeMax = 128;
            // Same left and right edges as the menu button (x 60) and the stars counter (60 from the right).
            // Right of the cup column (which starts under the menu button), out to the right edge (60).
            // In line with the menu button on the left; the cup dropdown sits on the right under the counters.
            var header = At("Header", safe, new Vector2(0f, 1f), new Vector2(60f, -222f), new Vector2(906f, 360f));
            var title = Txt(At("Title", header, new Vector2(0f, 1f), new Vector2(-2f, -14f), new Vector2(800f, 130f)), "MEADOW RUN", 100, Theme.Ink, _black, TextAlignmentOptions.TopLeft);
            title.enableAutoSizing = true; title.fontSizeMin = 64; title.fontSizeMax = 100; // keeps clear of the trophy counter
            // Your records for the race, beside the star times: best time over best lap.
            // Star times | records | info button, sharing the header width (1050).
            var records = At("Records", header, new Vector2(0f, 1f), new Vector2(512f, -160f), new Vector2(382f, 130f));
            Img(records, "Round", Theme.Ink, sliced: true);
            TMP_Text Record(string name, float y, string caption)
            {
                var row = At(name, records, new Vector2(0f, 1f), new Vector2(22f, y), new Vector2(338f, 56f));
                Txt(At("Caption", row, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(130f, 56f)), caption, 32, Theme.Stone, _semi, TextAlignmentOptions.Left).rectTransform.pivot = new Vector2(0f, 0.5f);
                var value = Txt(At("Value", row, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(200f, 56f)), "—", 44, Theme.Cream, _cond, TextAlignmentOptions.Right);
                value.rectTransform.pivot = new Vector2(1f, 0.5f);
                return value;
            }
            var best = Record("BestTime", -10f, "Best");
            var bestLap = Record("BestLap", -64f, "Best lap");
            // Opens the race info window: same height as the panels beside it (button + shadow = 130).
            // On the race name's line, at its right end.
            // Race info: the race name is the button, with a small "i" chip right after it (placed at runtime).
            var titleHit = At("InfoButton", header, new Vector2(0f, 1f), new Vector2(-2f, -14f), new Vector2(906f, 130f));
            var titleHitImage = Img(titleHit, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var infoButton = titleHit.gameObject.AddComponent<Button>();
            infoButton.targetGraphic = titleHitImage; infoButton.transition = Selectable.Transition.None;
            titleHit.gameObject.AddComponent<ButtonFeedback>();
            var chip = At("InfoChip", header, new Vector2(0f, 1f), new Vector2(700f, -80f), new Vector2(66f, 66f));
            chip.pivot = new Vector2(0.5f, 0.5f);
            Img(chip, "Circle", Theme.Ink);
            Img(At("Icon", chip, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f)), "IconInfo", Theme.Cream);

            // Bottom: star times, START.
            var bottom = At("Bottom", safe, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1050f, 480f));
            // Star times sit in the header, right under the race info.
            var timesRt = At("StarTimes", header, new Vector2(0f, 1f), new Vector2(0f, -160f), new Vector2(498f, 130f));
            Img(timesRt, "Round", Theme.Ink, sliced: true);
            var times = new TMP_Text[3];
            var groups = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var group = At($"Star{i + 1}", timesRt, new Vector2(0f, 0.5f), new Vector2(16f + i * 159f, 0f), new Vector2(156f, 120f));
                groups[i] = group;
                group.pivot = new Vector2(0f, 0.5f);
                for (int k = 0; k <= i; k++)
                    Img(At($"S{k}", group, new Vector2(0f, 1f), new Vector2(4f + k * 44f, -8f), new Vector2(44f, 44f)), "IconStar", Theme.Amber).rectTransform.pivot = new Vector2(0f, 1f);
                times[i] = Txt(At("Time", group, new Vector2(0f, 0f), new Vector2(4f, 4f), new Vector2(152f, 58f)), "1:00.00", 46, Theme.Cream, _cond, TextAlignmentOptions.BottomLeft);
                times[i].rectTransform.pivot = new Vector2(0f, 0f);
            }
            var start = BigRoundButton(bottom, "StartButton", new Vector2(1f, 0f), new Vector2(0f, 10f), 300f, "START",
                out var startFace, out var startShadow, out var startLabel, out var startRing);

            // Switching cups: a blurred picture of the incoming cup over its page, sharpening as it comes in...
            var incoming = Stretch("IncomingBlur", canvas).gameObject.AddComponent<RawImage>();
            incoming.raycastTarget = false;
            incoming.gameObject.SetActive(false);
            // ...and the old page frozen as a picture that the new cup slides in over (MapScreen).
            var wipe = Stretch("CupWipe", canvas);
            wipe.gameObject.AddComponent<RectMask2D>();
            var wipeImage = Rt("Old", wipe).gameObject.AddComponent<RawImage>();
            wipeImage.raycastTarget = true; // blocks taps while the cups change
            var wipeBlur = Rt("Blur", wipe).gameObject.AddComponent<RawImage>();
            wipeBlur.raycastTarget = false;
            var wipeDim = Img(Rt("Dim", wipe), null, new Color(0f, 0f, 0f, 0f));
            var wipeEdge = Img(Rt("Edge", wipe), "EdgeShade", new Color(0f, 0f, 0f, 0.35f));
            wipeEdge.preserveAspect = false;
            wipe.gameObject.SetActive(false);

            BuildMenu(canvas, menuButton, null, null); // (the cup bar under the cup name replaces the old globe menu)

            var infoWindow = BuildRaceInfo(canvas);

            var screen = canvas.gameObject.AddComponent<MapScreen>();
            var so = new SerializedObject(screen);
            Set(so, "catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Data/Levels/LevelCatalog.asset"));
            Set(so, "world", world); Set(so, "nodeTemplate", node); Set(so, "car", car); Set(so, "background", background); Set(so, "ambience", ambienceComp); Set(so, "snow", snow.gameObject); Set(so, "totalStars", total); Set(so, "totalTrophies", trophies);
            Set(so, "chapterLabel", chapter); Set(so, "titleLabel", title);
            Set(so, "bestChip", best); Set(so, "bestLapLabel", bestLap);
            SetArray(so, "starTimes", times); SetArray(so, "starGroups", groups);
            Set(so, "startButton", start); Set(so, "startFace", startFace); Set(so, "startShadow", startShadow);
            Set(so, "startLabel", startLabel); Set(so, "startRing", startRing.gameObject);
            Set(so, "infoButton", infoButton); Set(so, "infoWindow", infoWindow); Set(so, "infoChip", chip);
            // Swipe between cups: the page parts slide (the header and cup name a little faster, for depth).
            SetArray(so, "page", new[] { map, header, cupRt, bottom });
            var parallax = so.FindProperty("pageParallax");
            float[] depth = { 1f, 1.15f, 1.3f, 1f };
            parallax.arraySize = depth.Length;
            for (int i = 0; i < depth.Length; i++) parallax.GetArrayElementAtIndex(i).floatValue = depth[i];
            // The cup column under the menu button: a dark plate (clipping) with a badge per cup in a column
            // and a glowing ring on the current one; folded it shows just the current cup and a small arrow.
            // Above the frozen picture of a cup switch (so it can be seen unfolding and selecting), below the menus.
            var columnLayer = SafeArea(canvas);
            columnLayer.name = "CupColumnLayer";
            columnLayer.SetSiblingIndex(wipe.GetSiblingIndex() + 1);
            var dots = At("CupColumn", columnLayer, new Vector2(1f, 1f), new Vector2(-66f, -206f), new Vector2(128f, 64f));
            Img(dots, "Round", Theme.Ink, sliced: true, raycast: true);
            dots.gameObject.AddComponent<RectMask2D>();
            var content = At("Content", dots, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(128f, 600f));
            var ring = At("Ring", content, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(110f, 110f));
            ring.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Glow", ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(126f, 126f)), "Circle", Theme.WithAlpha(Theme.Amber, 0.22f));
            Img(At("Line", ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f)), "Ring", Theme.Amber);
            var badgeT = At("Badge", content, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(92f, 92f));
            badgeT.pivot = new Vector2(0.5f, 0.5f);
            var badgeFace = Img(At("Face", badgeT, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 92f)), "Circle", Theme.Cream, raycast: true);
            Img(At("Icon", badgeT, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f)), null, Color.white).preserveAspect = true;
            badgeT.gameObject.AddComponent<CanvasGroup>();
            var badgeButton = badgeT.gameObject.AddComponent<Button>();
            badgeButton.targetGraphic = badgeFace; badgeButton.transition = Selectable.Transition.None;
            badgeT.gameObject.AddComponent<ButtonFeedback>();
            // The fold toggle along the bottom: a small arrow pointing down (folded) or up (unfolded).
            var toggleRt = At("Toggle", dots, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(128f, 64f));
            var toggleHit = Img(toggleRt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var toggle = toggleRt.gameObject.AddComponent<Button>();
            toggle.targetGraphic = toggleHit; toggle.transition = Selectable.Transition.None;
            toggleRt.gameObject.AddComponent<ButtonFeedback>();
            var chevron = Img(At("Chevron", toggleRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f)), "IconPlay", Theme.Cream);
            chevron.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            Set(so, "barToggle", toggle);
            Set(so, "barContent", content); Set(so, "barChevron", chevron);
            SetArray(so, "fadeRoots", new Transform[] { columnLayer }); // the cup column sits above the frozen picture: its colours blend over
            Set(so, "dots", dots); Set(so, "badgeTemplate", badgeT); Set(so, "barRing", ring);
            Set(so, "wipe", wipe); Set(so, "wipeImage", wipeImage); Set(so, "wipeEdge", wipeEdge);
            Set(so, "wipeBlur", wipeBlur); Set(so, "wipeDim", wipeDim); Set(so, "incomingBlur", incoming);
            Set(so, "lockedPage", lockedPage.gameObject);
            Set(so, "lockedIcon", lockedPage.Find("Plate/Icon").GetComponent<Image>());
            Set(so, "lockSprite", Sprite("IconLock")); Set(so, "soonSprite", Sprite("IconQuestion"));
            Set(so, "lockedTitle", lockedPage.Find("Title").GetComponent<TMP_Text>());
            Set(so, "lockedCaption", lockedPage.Find("Caption").GetComponent<TMP_Text>());
            Set(so, "lockedNeeds", lockedPage.Find("Needs").gameObject);
            Set(so, "lockedStars", lockedPage.Find("Needs/Stars/Count").GetComponent<TMP_Text>());
            Set(so, "lockedTrophies", lockedPage.Find("Needs/Trophies/Count").GetComponent<TMP_Text>());
            SetArray(so, "racePageParts", new[] { header.gameObject, bottom.gameObject });
            Set(so, "celebration", BuildCupCelebration(canvas)); Set(so, "sparkle", Sprite("Sparkle"));
            
            var swipe = map.gameObject.AddComponent<MapSwipe>();
            Configure(swipe, ("screen", screen), ("canvas", canvas.GetComponent<Canvas>()));
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MapScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MapScenePath, true),
                new EditorBuildSettingsScene(RaceScenePath, true),
            };
        }

        // The page shown instead of the map for a locked or coming-soon cup: grey, a big lock (or "?") on a
        // round plate, LOCKED, and what it takes to unlock (stars and trophies, with your progress).
        static RectTransform BuildLockedPage(RectTransform map)
        {
            var page = Stretch("LockedPage", map);
            Img(page, null, Hex("8D918F"));
            Img(At("Glow", page, new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(1100f, 1100f)), "Circle", new Color(1f, 1f, 1f, 0.12f));
            var plate = At("Plate", page, new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(420f, 420f));
            Img(At("Shadow", plate, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(420f, 420f)), "Circle", Hex("6F7371"));
            Img(At("Face", plate, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f)), "Circle", Hex("C9CCCA"));
            Img(At("Ring", plate, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f)), "Ring", Hex("6F7371"));
            Img(At("Icon", plate, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 210f)), "IconLock", Hex("6F7371"));
            var title = Txt(At("Title", page, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1000f, 190f)), "LOCKED", 170, Hex("F4F5F3"), _black);
            title.enableAutoSizing = true; title.fontSizeMin = 90; title.fontSizeMax = 170;
            var caption = Txt(At("Caption", page, new Vector2(0.5f, 0.5f), new Vector2(0f, -212f), new Vector2(1050f, 80f)), "TO UNLOCK", 64, Hex("E3E5E2"), _condItalic);
            caption.enableAutoSizing = true; caption.fontSizeMin = 40; caption.fontSizeMax = 64;
            caption.characterSpacing = 8f;
            var needs = At("Needs", page, new Vector2(0.5f, 0.5f), new Vector2(0f, -350f), new Vector2(640f, 150f));
            Img(needs, "Round", Theme.Ink, sliced: true);
            void Need(string name, float x, string icon)
            {
                var item = At(name, needs, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(290f, 130f));
                Img(At("Icon", item, new Vector2(0.5f, 0.5f), new Vector2(-82f, 0f), new Vector2(80f, 80f)), icon, Theme.Amber);
                Txt(At("Count", item, new Vector2(0.5f, 0.5f), new Vector2(42f, -2f), new Vector2(190f, 120f)), "0/20", 76, Theme.Amber, _condItalic, TextAlignmentOptions.Left);
            }
            Need("Stars", -155f, "IconStar");
            Need("Trophies", 155f, "IconTrophy");
            page.gameObject.SetActive(false);
            return page;
        }

        // The full-screen celebration when a new cup unlocks (CupUnlockCelebration).
        static CupUnlockCelebration BuildCupCelebration(RectTransform canvas)
        {
            var host = Stretch("CupUnlock", canvas);
            var panel = Stretch("Panel", host);
            var dimRt = Stretch("Dim", panel);
            var dim = Img(dimRt, null, new Color(0f, 0f, 0f, 0.75f), raycast: true);
            var tap = dimRt.gameObject.AddComponent<Button>();
            tap.targetGraphic = dim; tap.transition = Selectable.Transition.None;
            var rays = At("Rays", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1500f, 1500f));
            var raysImage = Img(rays, "Rays", new Color(1f, 0.9f, 0.55f, 0.55f));
            var confetti = At("Confetti", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), Vector2.zero);
            var badge = At("Badge", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(520f, 520f));
            var badgeImage = Img(badge, "CupWinter", Color.white);
            var lockRt = At("Lock", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(300f, 300f));
            var lockImage = Img(lockRt, "IconLock", Color.white);
            var titleRt = At("Title", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1100f, 200f));
            var title = Txt(titleRt, "NEW CUP!", 190, Theme.Amber, _black);
            title.fontSharedMaterial = _blackOutline;
            var cupName = Txt(At("CupName", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(1100f, 120f)), "WINTER CUP", 96, Color.white, _condItalic);
            cupName.characterSpacing = 8f;
            var goRt = At("Go", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -560f), new Vector2(640f, 170f));
            var goShadowRt = Stretch("Shadow", goRt); goShadowRt.offsetMax = new Vector2(0f, -14f);
            Img(goShadowRt, "Round", Theme.OrangeDeep, sliced: true);
            var goFaceRt = Stretch("Face", goRt); goFaceRt.offsetMin = new Vector2(0f, 14f);
            var goFace = Img(goFaceRt, "Round", Theme.Orange, sliced: true, raycast: true);
            Txt(Stretch("Label", goFaceRt), "LET'S GO!", 76, Color.white, _black).characterSpacing = 6f;
            var go = goRt.gameObject.AddComponent<Button>();
            go.targetGraphic = goFace; go.transition = Selectable.Transition.None;
            goRt.gameObject.AddComponent<ButtonFeedback>();
            var flash = Img(Stretch("Flash", panel), null, new Color(1f, 1f, 1f, 0f));
            var celebration = host.gameObject.AddComponent<CupUnlockCelebration>();
            Configure(celebration, ("panel", panel.gameObject), ("dim", dim), ("rays", rays), ("raysImage", raysImage), ("badge", badge),
                ("badgeImage", badgeImage), ("lockRt", lockRt), ("lockImage", lockImage), ("flash", flash), ("titleRt", titleRt), ("title", title),
                ("cupName", cupName), ("goButton", go), ("tapAnywhere", tap), ("confettiLayer", confetti), ("confettiSprite", Sprite("Pill")));
            panel.gameObject.SetActive(false);
            return celebration;
        }

        // The race info window: a big cream card over the map with the track from above (as on the loading
        // screen) beside the difficulty, laps, rivals and what is on the track; the description below; START.
        static RaceInfoWindow BuildRaceInfo(RectTransform canvas)
        {
            var host = Stretch("RaceInfo", canvas);
            var panel = Stretch("Panel", host);
            var dimRt = Stretch("Dim", panel);
            var dimImage = Img(dimRt, null, Theme.WithAlpha(Theme.Ink, 0.7f), raycast: true);
            var dim = dimRt.gameObject.AddComponent<Button>();
            dim.targetGraphic = dimImage;
            dim.transition = Selectable.Transition.None;
            var safe = SafeArea(panel);

            var card = At("Card", safe, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1050f, 1780f));
            var cardShadow = Stretch("Shadow", card);
            cardShadow.offsetMin = cardShadow.offsetMax = new Vector2(0f, -20f);
            Img(cardShadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Face", card), "Round", Theme.Cream, sliced: true, raycast: true);

            var closeRt = At("Close", card, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(110f, 110f));
            var closeImage = Img(closeRt, null, new Color(0, 0, 0, 0), raycast: true);
            Img(At("Icon", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f)), "IconClose", Theme.Ink);
            var close = closeRt.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImage;
            closeRt.gameObject.AddComponent<ButtonFeedback>();

            var chapter = Txt(At("Chapter", card, new Vector2(0f, 1f), new Vector2(60f, -60f), new Vector2(820f, 50f)), "VALLEY CUP · RACE 1", 40, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            chapter.characterSpacing = 6f;
            var title = Txt(At("Title", card, new Vector2(0f, 1f), new Vector2(56f, -110f), new Vector2(840f, 140f)), "SUNDAY LOOP", 120, Theme.Ink, _black, TextAlignmentOptions.Left);
            title.enableAutoSizing = true; title.fontSizeMin = 70; title.fontSizeMax = 120;

            // The track from above in a dark frame (the picture is portrait, 560 x 1000), big and centred.
            var frame = At("Track", card, new Vector2(0.5f, 1f), new Vector2(0f, -272f), new Vector2(570f, 1006f));
            Img(frame, "Round", Theme.Ink, sliced: true);
            var picture = Img(Stretch("Picture", frame), null, Color.white);
            picture.rectTransform.offsetMin = new Vector2(12f, 12f); picture.rectTransform.offsetMax = new Vector2(-12f, -12f);
            picture.preserveAspect = true;

            // Difficulty: a small caption and five pips under the picture.
            var difficulty = Txt(At("DifficultyCaption", card, new Vector2(0.5f, 1f), new Vector2(0f, -1298f), new Vector2(600f, 44f)), "DIFFICULTY", 36, Theme.Rust, _condItalic);
            difficulty.characterSpacing = 8f;
            var pips = new Image[5];
            for (int i = 0; i < pips.Length; i++)
                pips[i] = Img(At($"Pip{i + 1}", card, new Vector2(0.5f, 1f), new Vector2((i - 2) * 112f, -1368f), new Vector2(96f, 38f)), "Pill", Theme.Orange, sliced: true);

            // What is on the track: a row of round badges with the game's own pictures.
            var row = At("Features", card, new Vector2(0.5f, 1f), new Vector2(0f, -1432f), new Vector2(930f, 108f));
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 18f; rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = false; rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false; rowLayout.childForceExpandHeight = false;
            var badge = Rt("Badge", row);
            badge.sizeDelta = new Vector2(108f, 108f);
            Img(badge, "Circle", Theme.Ink);
            var mask = At("Mask", badge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 92f));
            Img(mask, "Circle", Color.white);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Img(At("Icon", mask, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f)), null, Color.white).preserveAspect = true;

            // START along the bottom of the card.
            var startRt = At("StartButton", card, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(600f, 164f));
            // Previous / next race either side of START (same height, square, dark with a light arrow).
            var prevRace = SquareButton(card, "PrevRace", new Vector2(0f, 0f), new Vector2(60f, 60f), "IconPlay", 152f);
            prevRace.transform.Find("Face/Icon").localScale = new Vector3(-1f, 1f, 1f);
            var nextRace = SquareButton(card, "NextRace", new Vector2(1f, 0f), new Vector2(-60f, 60f), "IconPlay", 152f);
            foreach (var arrow in new[] { prevRace, nextRace }) // orange like START
            {
                arrow.transform.Find("Shadow").GetComponent<Image>().color = Theme.OrangeDeep;
                arrow.transform.Find("Face").GetComponent<Image>().color = Theme.Orange;
                arrow.transform.Find("Face/Icon").GetComponent<Image>().color = Color.white;
            }
            var startShadowRt = Stretch("Shadow", startRt);
            startShadowRt.offsetMax = new Vector2(0f, -14f);
            var startShadow = Img(startShadowRt, "Round", Theme.OrangeDeep, sliced: true);
            var startFaceRt = Stretch("Face", startRt);
            startFaceRt.offsetMin = new Vector2(0f, 14f);
            var startFace = Img(startFaceRt, "Round", Theme.Orange, sliced: true, raycast: true);
            var startLabel = Txt(Stretch("Label", startFaceRt), "START", 72, Color.white, _black);
            startLabel.characterSpacing = 6f;
            var start = startRt.gameObject.AddComponent<Button>();
            start.targetGraphic = startFace;
            start.transition = Selectable.Transition.None;
            startRt.gameObject.AddComponent<ButtonFeedback>();

            Configure(panel.gameObject.AddComponent<PopIn>(), ("card", card), ("backdrop", dimImage));
            var window = host.gameObject.AddComponent<RaceInfoWindow>();
            Configure(window, ("panel", panel.gameObject), ("closeArea", dim), ("closeButton", close), ("chapterLabel", chapter), ("titleLabel", title),
                ("trackPicture", picture), ("featureRow", row), ("featureTemplate", badge), ("previousButton", prevRace), ("nextButton", nextRace),
                ("startButton", start), ("startFace", startFace), ("startShadow", startShadow), ("startLabel", startLabel));
            var wso = new SerializedObject(window);
            SetArray(wso, "difficultyPips", pips);
            var iconList = new (string key, string sprite)[]
            {
                ("roof", "RoofRow"), ("wall", "IceWall"), ("ice", "Ice"), ("ramp", "Ramp"), ("gummiboat", "Gummiboat"), ("pad", "JumpPad"),
                ("dash", "DashPanel"), ("penguin", "Penguin"), ("fish", "Fish"), ("snowball", "Snowball"), ("log", "Log"), ("hay", "HayBale"),
                ("oil", "Oil"), ("shortcut", "Arrow"),
            };
            var icons = wso.FindProperty("icons");
            icons.arraySize = iconList.Length;
            for (int i = 0; i < iconList.Length; i++)
            {
                var entry = icons.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("key").stringValue = iconList[i].key;
                entry.FindPropertyRelative("sprite").objectReferenceValue = Sprite(iconList[i].sprite);
                entry.FindPropertyRelative("tint").colorValue = iconList[i].key == "shortcut" ? new Color(0.17f, 0.23f, 0.23f) : Color.clear;
            }
            wso.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            return window;
        }

        // The small world tab on the left edge and the row of cup tiles it opens.
        static void BuildCupMenu(RectTransform canvas, RectTransform safe)
        {
            var root = Stretch("CupMenu", canvas);
            var rootSafe = SafeArea(root);
            // Left edge in line with the menu button and the race info, just under the star times.
            var tab = SquareButton(rootSafe, "WorldButton", new Vector2(0f, 1f), new Vector2(60f, -566f), "IconGlobe", 112f);

            var panel = Stretch("Panel", rootSafe);
            var closeRt = Stretch("Close", panel);
            var closeImg = Img(closeRt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var close = closeRt.gameObject.AddComponent<Button>(); close.targetGraphic = closeImg; close.transition = Selectable.Transition.None;

            var box = At("Box", panel, new Vector2(0f, 1f), new Vector2(196f, -546f), new Vector2(712f, 364f));
            var boxShadow = Stretch("Shadow", box); boxShadow.offsetMin = boxShadow.offsetMax = new Vector2(0f, -14f);
            Img(boxShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Face", box), "Round", Theme.Ink, sliced: true, raycast: true);
            var row = Stretch("Tiles", box);
            row.offsetMin = new Vector2(24f, 24f); row.offsetMax = new Vector2(-24f, -24f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 22f; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false; layout.childControlHeight = false; layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

            var tile = Rt("TileTemplate", row); tile.sizeDelta = new Vector2(200f, 316f);
            var face = Img(Stretch("Face", tile), "Round", Color.white, sliced: true, raycast: true);
            var ring = Img(Stretch("Ring", tile), "Round", Theme.Orange, sliced: true);
            ring.rectTransform.offsetMin = new Vector2(-8f, -8f); ring.rectTransform.offsetMax = new Vector2(8f, 8f);
            ring.transform.SetAsFirstSibling();
            var icon = Img(At("Icon", tile, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(140f, 140f)), null, Color.white);
            var lockIcon = Img(At("Lock", tile, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(60f, 60f)), "IconLock", Theme.Ink);
            var title = Txt(At("Title", tile, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(190f, 56f)), "VALLEY", 44, Theme.Ink, _condItalic);
            title.enableAutoSizing = true; title.fontSizeMin = 28; title.fontSizeMax = 44;
            // Two short rows: stars (count + star) over trophies (count + trophy), centred.
            var req = At("Needs", tile, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(190f, 92f));
            TMP_Text Counter(string name, float y, string iconName)
            {
                var text = Txt(At(name, req, new Vector2(0.5f, 0f), new Vector2(-10f, y), new Vector2(120f, 44f)), "0", 38, Theme.Ink, _condItalic, TextAlignmentOptions.Right);
                text.rectTransform.pivot = new Vector2(1f, 0f);
                text.rectTransform.anchoredPosition = new Vector2(12f, y);
                var icon = Img(At(name + "Icon", req, new Vector2(0.5f, 0f), new Vector2(18f, y + 6f), new Vector2(32f, 32f)), iconName, Theme.Amber);
                icon.rectTransform.pivot = new Vector2(0f, 0f);
                return text;
            }
            var stars = Counter("Stars", 46f, "IconStar");
            var trophies = Counter("Trophies", 2f, "IconTrophy");
            var soon = Txt(At("Soon", tile, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(190f, 52f)), "SOON", 40, Theme.StoneDark, _condItalic);
            soon.characterSpacing = 8f;
            var button = tile.gameObject.AddComponent<Button>(); button.targetGraphic = face; button.transition = Selectable.Transition.None;
            tile.gameObject.AddComponent<ButtonFeedback>();
            var cupTile = tile.gameObject.AddComponent<CupTile>();
            var tso = new SerializedObject(cupTile);
            Set(tso, "button", button); Set(tso, "face", face); Set(tso, "ring", ring); Set(tso, "icon", icon); Set(tso, "lockIcon", lockIcon);
            Set(tso, "title", title); Set(tso, "requirementRow", req.gameObject); Set(tso, "starsNeeded", stars); Set(tso, "trophiesNeeded", trophies); Set(tso, "soonLabel", soon);
            tso.ApplyModifiedPropertiesWithoutUndo();

            var menu = root.gameObject.AddComponent<CupMenu>();
            var so = new SerializedObject(menu);
            Set(so, "worldButton", tab); Set(so, "panel", panel.gameObject); Set(so, "closeArea", close);
            Set(so, "tileTemplate", cupTile); Set(so, "tilesParent", row);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static MapNode BuildNode(RectTransform world)
        {
            var root = At("NodeTemplate", world, Vector2.zero, Vector2.zero, new Vector2(170f, 170f));
            root.pivot = new Vector2(0.5f, 0.5f);
            var selection = At("Selection", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(214f, 214f));
            var selImage = Img(selection, "RingDashed", Color.white);
            Configure(selection.gameObject.AddComponent<UIPulse>(), ("speed", 0.6f), ("fade", selImage), ("scale", new Vector2(0.94f, 1.08f)), ("alpha", new Vector2(0.55f, 1f)));
            var shadow = Img(At("Shadow", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(150f, 150f)), "Circle", Theme.Ink);
            var face = Img(At("Face", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "Circle", Theme.White, raycast: true);
            var ring = Img(At("Ring", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "Ring", Theme.Ink);
            var dashed = Img(At("Dashed", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "RingDashed", Theme.StoneDark);
            var number = Txt(At("Number", root, new Vector2(0.5f, 0.5f), new Vector2(-2f, 0f), new Vector2(150f, 150f)), "1", 92, Theme.Ink, _black);
            var lockIcon = Img(At("Lock", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(50f, 50f)), "IconLock", Theme.StoneDark);

            var starsPill = At("Stars", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(222f, 60f));
            Img(starsPill, "Pill", Theme.Ink, sliced: true);
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
                stars[i] = Img(At($"Star{i + 1}", starsPill, new Vector2(0.5f, 0.5f), new Vector2(-72f + i * 44f, 0f), new Vector2(40f, 40f)), "IconStar", Theme.Amber);
            Img(At("Divider", starsPill, new Vector2(0.5f, 0.5f), new Vector2(42f, 0f), new Vector2(3f, 36f)), null, Theme.WithAlpha(Theme.Cream, 0.2f));
            var trophy = Img(At("Trophy", starsPill, new Vector2(0.5f, 0.5f), new Vector2(76f, 1f), new Vector2(40f, 40f)), "IconTrophy", Theme.Amber);

            // A locked race shows the stars it needs inside its badge, under the lock.
            // Centred in the space between the lock and the bottom of the badge.
            var req = At("Requirement", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(130f, 44f));
            var reqText = Txt(At("Count", req, new Vector2(0.5f, 0.5f), new Vector2(-37f, -1f), new Vector2(70f, 44f)), "2", 44, Theme.Ink, _condItalic, TextAlignmentOptions.Right);
            reqText.verticalAlignment = VerticalAlignmentOptions.Geometry; // centre the digits themselves
            Img(At("Icon", req, new Vector2(0.5f, 0.5f), new Vector2(16f, 0f), new Vector2(34f, 34f)), "IconStar", Theme.Amber);

            var capRt = At("Caption", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(230f, 56f));
            Img(capRt, "Pill", Theme.WithAlpha(Theme.Cream, 0.92f), sliced: true);
            var caption = Txt(Stretch("Label", capRt), "MORE SOON", 32, Theme.Ink, _condItalic);
            caption.characterSpacing = 4f;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<ButtonFeedback>();

            var mapNode = root.gameObject.AddComponent<MapNode>();
            var so = new SerializedObject(mapNode);
            Set(so, "button", button); Set(so, "shadow", shadow); Set(so, "face", face); Set(so, "ring", ring);
            Set(so, "dashedRing", dashed); Set(so, "number", number); Set(so, "lockIcon", lockIcon);
            Set(so, "starsPill", starsPill.gameObject); SetArray(so, "stars", stars); Set(so, "trophy", trophy);
            Set(so, "requirement", req.gameObject); Set(so, "requirementText", reqText);
            Set(so, "selection", selection.gameObject); Set(so, "caption", caption); Set(so, "captionRoot", capRt.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            return mapNode;
        }

        // ================================================================== RACE

        public static void BuildRace()
        {
            var scene = EditorSceneManager.OpenScene(RaceScenePath);
            LoadFonts();
            var old = GameObject.Find("UI");
            if (old != null) Object.DestroyImmediate(old);
            var race = Object.FindAnyObjectByType<RaceManager>();

            var canvas = CreateCanvas();

            // Snowfall over the race on snow tracks (switched on by the track renderer), under the HUD.
            var raceSnow = Stretch("Snow", canvas);
            Configure(raceSnow.gameObject.AddComponent<SnowFall>(), ("flake", Sprite("Circle")), ("count", 90));
            raceSnow.gameObject.SetActive(false);
            WireSnowBiome(raceSnow.gameObject);
            EnsureBranchRenderers();

            // Floating joystick (whole screen is the touch area).
            var area = Stretch("JoystickArea", canvas);
            var baseRt = At("JoystickBase", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 360f));
            Img(At("Fill", baseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(350f, 350f)), "Circle", Theme.WithAlpha(Theme.Ink, 0.18f));
            Img(baseRt, "Ring", Theme.WithAlpha(Color.white, 0.8f));
            var knob = At("Knob", baseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            Img(At("Shadow", knob, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(150f, 150f)), "Circle", Theme.WithAlpha(Theme.Ink, 0.45f));
            Img(At("Face", knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "Circle", Color.white);
            Img(At("Ring", knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "Ring", Theme.Ink);
            baseRt.gameObject.SetActive(false);
            var joystick = area.gameObject.AddComponent<FloatingJoystick>();
            var jso = new SerializedObject(joystick);
            Set(jso, "canvas", canvas.GetComponent<Canvas>()); Set(jso, "background", baseRt); Set(jso, "knob", knob);
            jso.ApplyModifiedPropertiesWithoutUndo();
            var rso = new SerializedObject(race);
            Set(rso, "joystick", joystick);
            rso.ApplyModifiedPropertiesWithoutUndo();

            var safe = SafeArea(canvas);
            var menuButton = SquareButton(safe, "MenuButton", new Vector2(0f, 1f), new Vector2(48f, -48f), "IconMenu", 138f);

            // HUD bar: LAP | TIME | POS on a pine-green card with a hard shadow.
            var hud = Rt("Hud", safe);
            hud.anchorMin = new Vector2(0f, 1f); hud.anchorMax = new Vector2(1f, 1f); hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = new Vector2(87f, -48f); hud.sizeDelta = new Vector2(-270f, 138f);
            var hudShadow = Stretch("Shadow", hud);
            hudShadow.offsetMin = new Vector2(0f, -12f); hudShadow.offsetMax = new Vector2(0f, -12f);
            Img(hudShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Fill", hud), "Round", Theme.Ink, sliced: true);
            var lap = HudColumn(hud, "Lap", 0f, 0.27f, "LAP", "1/3", 76, _condItalic);
            var time = HudColumn(hud, "Time", 0.27f, 0.68f, "TIME", "0:00.00", 76, _condItalic);
            var pos = HudColumn(hud, "Position", 0.68f, 1f, "POS", "1ST", 84, _black);
            var flashRt = At("LapFlash", safe, new Vector2(0.5f, 1f), new Vector2(0f, -224f), new Vector2(560f, 84f));
            Img(flashRt, "Pill", Theme.Cream, sliced: true);
            var flash = Txt(Stretch("Label", flashRt), "LAP 0:21.56", 46, Theme.Ink, _condItalic);
            flash.characterSpacing = 4f;
            var warning = Txt(At("Warning", safe, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(1050f, 240f)), "WRONG WAY", 116, Theme.Orange, _black);
            warning.fontSharedMaterial = _blackOutline;
            warning.enableAutoSizing = true; warning.fontSizeMin = 50; warning.fontSizeMax = 116;
            var hudComp = hud.gameObject.AddComponent<RaceHud>();
            var hso = new SerializedObject(hudComp);
            Set(hso, "race", race); Set(hso, "lapLabel", lap); Set(hso, "timeLabel", time); Set(hso, "positionLabel", pos);
            Set(hso, "lapFlashLabel", flash); Set(hso, "lapFlashRoot", flashRt.gameObject); Set(hso, "warningLabel", warning);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // The road around the car, top right under the HUD bar: fixed like the screen, the car in the middle.
            var aheadRt = At("TrackAhead", safe, new Vector2(1f, 1f), new Vector2(-48f, -222f), new Vector2(300f, 300f));
            Img(aheadRt, "Round", Theme.WithAlpha(Theme.Ink, 0.9f), sliced: true);
            aheadRt.gameObject.AddComponent<RectMask2D>();
            var aheadOther = Line(Stretch("OtherWay", aheadRt));
            aheadOther.color = Theme.WithAlpha(Theme.Cream, 0.35f);
            aheadOther.raycastTarget = false;
            var aheadRoad = Line(Stretch("Road", aheadRt));
            aheadRoad.color = Theme.WithAlpha(Theme.Cream, 0.9f);
            aheadRoad.raycastTarget = false;
            var aheadArrow = Img(At("Car", aheadRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 66f)), "NavArrow", Theme.Orange).rectTransform;
            Configure(aheadRt.gameObject.AddComponent<TrackAheadView>(), ("race", race), ("road", aheadRoad), ("otherWay", aheadOther), ("arrow", aheadArrow));

            // Tricks and combos, big above the car (the car sits in the middle of the screen).
            var combo = Stretch("Combo", canvas);
            var trickRoot = At("Trick", combo, new Vector2(0.5f, 0.5f), new Vector2(0f, 620f), new Vector2(1000f, 240f));
            var burst = Img(At("Burst", trickRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 560f)), "Sparkle", Theme.Amber);
            var trickLabel = Txt(Stretch("Label", trickRoot), "ULTRA DRIFT!", 156, Color.white, _black);
            trickLabel.fontSharedMaterial = _blackOutline;
            var comboRoot = At("Chain", combo, new Vector2(0.5f, 0.5f), new Vector2(0f, 450f), new Vector2(800f, 130f));
            var comboLabel = Txt(Stretch("Label", comboRoot), "COMBO x2", 80, Color.white, _black);
            comboLabel.fontSharedMaterial = _blackOutline;
            comboLabel.characterSpacing = 4f;
            Configure(combo.gameObject.AddComponent<ComboDisplay>(), ("race", race), ("trickRoot", trickRoot), ("trickLabel", trickLabel),
                ("burst", burst), ("comboRoot", comboRoot), ("comboLabel", comboLabel));

            // Countdown.
            var cd = Stretch("Countdown", canvas);
            var cdLabel = Txt(At("Number", cd, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1000f, 520f)), "3", 440, Color.white, _black);
            cdLabel.fontSharedMaterial = _blackShadow;
            cdLabel.gameObject.SetActive(false);
            var cdComp = cd.gameObject.AddComponent<CountdownView>();
            var cso = new SerializedObject(cdComp);
            Set(cso, "race", race); Set(cso, "label", cdLabel);
            cso.ApplyModifiedPropertiesWithoutUndo();

            BuildResults(canvas, race);
            BuildTutorial(safe, race, hud.gameObject, aheadRt.gameObject);
            BuildMenu(canvas, menuButton, race, joystick);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // The practice-drive hint card (top) and the "you're ready" card (centre).
        static void BuildTutorial(RectTransform safe, RaceManager race, params GameObject[] hideInTutorial)
        {
            var root = Stretch("Tutorial", safe);
            var card = Rt("Card", root);
            card.anchorMin = new Vector2(0f, 1f); card.anchorMax = new Vector2(1f, 1f); card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(87f, -48f); card.sizeDelta = new Vector2(-270f, 400f);
            var shadow = Stretch("Shadow", card); shadow.offsetMin = shadow.offsetMax = new Vector2(0f, -14f);
            Img(shadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Face", card), "Round", Theme.Cream, sliced: true);
            var step = Txt(At("Step", card, new Vector2(0f, 1f), new Vector2(40f, -24f), new Vector2(600f, 50f)), "PRACTICE · 1/6", 40, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            step.rectTransform.pivot = new Vector2(0f, 1f); step.characterSpacing = 6f;
            var title = Txt(At("Title", card, new Vector2(0f, 1f), new Vector2(38f, -72f), new Vector2(820f, 116f)), "STEER", 100, Theme.Ink, _black, TextAlignmentOptions.Left);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            var body = Txt(At("Body", card, new Vector2(0f, 1f), new Vector2(40f, -190f), new Vector2(820f, 200f)), "Hint", 48, Theme.InkSoft, _semi, TextAlignmentOptions.TopLeft);
            body.rectTransform.pivot = new Vector2(0f, 1f); body.textWrappingMode = TextWrappingModes.Normal;
            var skipRt = At("Skip", card, new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(170f, 72f));
            skipRt.pivot = new Vector2(1f, 1f);
            var skipBg = Img(skipRt, "Pill", Theme.Ink, sliced: true, raycast: true);
            Txt(Stretch("Label", skipRt), "SKIP", 40, Theme.Cream, _condItalic);
            var skip = skipRt.gameObject.AddComponent<Button>(); skip.targetGraphic = skipBg; skip.transition = Selectable.Transition.None;
            skipRt.gameObject.AddComponent<ButtonFeedback>();

            var done = Stretch("Done", root);
            Img(done, null, Theme.WithAlpha(Theme.Ink, 0.55f), raycast: true);
            var panel = At("Panel", done, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 760f));
            var pShadow = Stretch("Shadow", panel); pShadow.offsetMin = pShadow.offsetMax = new Vector2(0f, -20f);
            Img(pShadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Face", panel), "Round", Theme.Cream, sliced: true);
            Txt(At("Title", panel, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(820f, 170f)), "YOU'RE READY!", 120, Theme.Orange, _black).fontSharedMaterial = _blackShadow;
            var text = Txt(At("Body", panel, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(760f, 200f)),
                "Win races and beat the star times to collect stars and trophies.", 44, Theme.Ink, _semi);
            text.textWrappingMode = TextWrappingModes.Normal;
            var go = BigRoundButton(panel, "MapButton", new Vector2(0.5f, 0f), new Vector2(0f, 50f), 190f, "MAP", out _, out _, out _, out _);

            // Covers the whole screen for a moment while the car is moved to the next step's spot.
            var fadeRt = Stretch("TutorialFade", (RectTransform)safe.parent);
            Img(fadeRt, null, Theme.Ink);
            var fade = fadeRt.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f; fade.blocksRaycasts = false; fade.interactable = false;

            // The demo under the hint card: white fingers act out the step, a mini car shows what happens.
            // At the top, right under the hint card.
            var demo = At("Demo", root, new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(820f, 460f));
            demo.localScale = Vector3.one * 1.6f; // big enough to read at a glance
            var demoGroup = demo.gameObject.AddComponent<CanvasGroup>();
            demoGroup.blocksRaycasts = false; demoGroup.interactable = false;
            Img(demo, "Round", Theme.WithAlpha(Theme.Ink, 0.35f), sliced: true);
            RectTransform Part(string name, string sprite, Vector2 size, Color color, Vector2 pivot)
            {
                var rt = At(name, demo, new Vector2(0.5f, 0.5f), Vector2.zero, size);
                rt.pivot = pivot;
                var image = Img(rt, sprite, color);
                image.preserveAspect = sprite != null;
                return rt;
            }
            var center = new Vector2(0.5f, 0.5f);
            var ramp = Part("Ramp", "Ramp", new Vector2(124f, 155f), Color.white, center);
            var carShadow = Part("CarShadow", "CarShadow", new Vector2(72f, 126f), new Color(0f, 0f, 0f, 0.4f), center);
            var demoCar = Part("Car", "Car", new Vector2(72f, 126f), Theme.Orange, center);
            Img(Stretch("Details", demoCar), "CarDetails", Color.white).preserveAspect = true;
            var sparks = new Image[3];
            for (int i = 0; i < sparks.Length; i++) sparks[i] = Part($"Spark{i}", "Circle", new Vector2(24f, 24f), Color.white, center).GetComponent<Image>();
            var lines = new RectTransform[3];
            for (int i = 0; i < lines.Length; i++) lines[i] = Part($"SpeedLine{i}", null, new Vector2(8f, 72f), new Color(1f, 1f, 1f, 0.7f), center);
            var ring = Part("Ring", "Ring", new Vector2(190f, 190f), new Color(1f, 1f, 1f, 0.8f), center);
            ring.anchoredPosition = new Vector2(-210f, -30f);
            var knob = Part("Knob", "Circle", new Vector2(72f, 72f), new Color(1f, 1f, 1f, 0.9f), center);
            var press = Part("Press", "Circle", new Vector2(86f, 86f), new Color(1f, 1f, 1f, 0.5f), center);
            var ripple = Part("Ripple", "Ring", new Vector2(110f, 110f), Color.white, center);
            var finger1 = Part("Finger1", "Finger", new Vector2(80f, 100f), Color.white, new Vector2(0.5f, 0.96f));
            var finger2 = Part("Finger2", "Finger", new Vector2(80f, 100f), Color.white, new Vector2(0.5f, 0.96f));
            var demoComp = demo.gameObject.AddComponent<TutorialDemo>();
            Configure(demoComp, ("group", demoGroup), ("finger1", finger1), ("finger2", finger2), ("ring", ring), ("knob", knob), ("press", press),
                ("ripple", ripple), ("car", demoCar), ("carShadow", carShadow), ("ramp", ramp));
            var dso = new SerializedObject(demoComp);
            SetArray(dso, "sparks", sparks);
            SetArray(dso, "speedLines", lines);
            dso.ApplyModifiedPropertiesWithoutUndo();
            demo.gameObject.SetActive(false);

            var guide = root.gameObject.AddComponent<TutorialGuide>();
            var so = new SerializedObject(guide);
            Set(so, "fade", fade); Set(so, "demo", demoComp);
            Set(so, "race", race); Set(so, "card", card.gameObject); Set(so, "stepLabel", step); Set(so, "titleLabel", title); Set(so, "bodyLabel", body);
            Set(so, "skipButton", skip); Set(so, "doneCard", done.gameObject); Set(so, "doneButton", go);
            SetArray(so, "hideInTutorial", hideInTutorial);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_Text HudColumn(RectTransform hud, string name, float from, float to, string caption, string value, float size, TMP_FontAsset font)
        {
            var col = Rt(name, hud);
            col.anchorMin = new Vector2(from, 0f); col.anchorMax = new Vector2(to, 1f);
            col.offsetMin = col.offsetMax = Vector2.zero;
            var cap = Txt(At("Caption", col, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(260f, 36f)), caption, 28, Theme.Stone, _cond);
            cap.characterSpacing = 10f;
            var val = Txt(At("Value", col, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(380f, 100f)), value, size, Color.white, font);
            val.enableAutoSizing = true; val.fontSizeMin = 36; val.fontSizeMax = size;
            return val;
        }

        static void BuildResults(RectTransform canvas, RaceManager race)
        {
            var root = Stretch("FinishScreen", canvas);
            var panel = Stretch("Panel", root);
            Img(panel, null, Theme.Cream, raycast: true);
            // Meadow at the bottom, checkered band at the top.
            Img(At("HillBack", panel, new Vector2(0.5f, 0f), new Vector2(-300f, -700f), new Vector2(2000f, 1500f)), "Circle", Hex("9CC383")).preserveAspect = false;
            Img(At("HillFront", panel, new Vector2(0.5f, 0f), new Vector2(260f, -900f), new Vector2(2200f, 1600f)), "Circle", Hex("7FB06C")).preserveAspect = false;
            var safe = SafeArea(panel);
            var band = At("Checker", safe, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1400f, 90f));
            var checker = Img(band, "Checker", Color.white);
            checker.type = Image.Type.Tiled; checker.preserveAspect = false; checker.pixelsPerUnitMultiplier = 1.2f;

            var raceLabel = Txt(At("Race", safe, new Vector2(1f, 1f), new Vector2(-60f, -90f), new Vector2(800f, 60f)), "VALLEY CUP · RACE 1", 42, Theme.Rust, _condItalic, TextAlignmentOptions.Right);
            raceLabel.characterSpacing = 8f;
            var position = Txt(At("Position", safe, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(1100f, 360f)), "1ST", 340, Theme.Orange, _black);
            position.fontSharedMaterial = _blackShadow;
            Pop(position.rectTransform, 0.05f);
            var subtitle = Txt(At("Subtitle", safe, new Vector2(0.5f, 1f), new Vector2(0f, -630f), new Vector2(1000f, 100f)), "PLACE", 88, Theme.Ink, _black);
            subtitle.characterSpacing = 10f;
            var tag = At("NewBest", safe, new Vector2(1f, 1f), new Vector2(-80f, -420f), new Vector2(330f, 90f));
            tag.localRotation = Quaternion.Euler(0f, 0f, 8f);
            Img(tag, "Pill", Theme.Ink, sliced: true);
            Txt(Stretch("Label", tag), "NEW BEST LAP", 40, Theme.Amber, _condItalic);
            Pop(tag, 0.5f);

            var starsRow = At("Stars", safe, new Vector2(0.5f, 1f), new Vector2(0f, -800f), new Vector2(900f, 280f));
            var stars = new Image[3];
            float[] sizes = { 190f, 250f, 190f };
            float[] xs = { -240f, 0f, 240f };
            for (int i = 0; i < 3; i++)
            {
                var s = At($"Star{i + 1}", starsRow, new Vector2(0.5f, 0.5f), new Vector2(xs[i], i == 1 ? 34f : 0f), new Vector2(sizes[i], sizes[i]));
                Img(At("Outline", s, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(sizes[i] * 1.14f, sizes[i] * 1.14f)), "IconStar", Theme.Ink);
                stars[i] = Img(At("Fill", s, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sizes[i], sizes[i])), "IconStar", Theme.Amber);
                Pop(s, 0.25f + i * 0.18f);
            }

            var card = At("Card", safe, new Vector2(0.5f, 1f), new Vector2(0f, -1110f), new Vector2(1030f, 580f));
            Img(card, "Round", Theme.Ink, sliced: true);
            var totalValue = ResultRow(card, "Total", -22f, "Total time", 96, Color.white, out _);
            Img(At("Divider", card, new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(950f, 4f)), null, Theme.WithAlpha(Theme.Cream, 0.15f));
            var bestTime = ResultRow(card, "BestTime", -190f, "Best time", 66, Color.white, out _);
            var bestValue = ResultRow(card, "BestLap", -310f, "Best lap", 66, Theme.Amber, out _);
            var nextValue = ResultRow(card, "NextStar", -430f, "2nd star at", 66, Color.white, out var nextLabel);
            Pop(card, 0.15f);

            var bottom = At("Bottom", safe, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1050f, 460f));
            var retry = SquareButton(bottom, "RetryButton", new Vector2(0f, 0f), new Vector2(0f, 80f), "IconRetry", 200f, white: true);
            var next = BigRoundButton(bottom, "NextButton", new Vector2(1f, 0f), new Vector2(0f, 0f), 310f, "MAP", out _, out _, out _, out _);

            panel.gameObject.SetActive(false);
            var finish = root.gameObject.AddComponent<FinishScreen>();
            var so = new SerializedObject(finish);
            Set(so, "race", race); Set(so, "panel", panel.gameObject); Set(so, "raceLabel", raceLabel);
            Set(so, "positionLabel", position); Set(so, "subtitle", subtitle); Set(so, "newBestTag", tag.gameObject);
            SetArray(so, "stars", stars); Set(so, "totalValue", totalValue); Set(so, "bestLapValue", bestValue); Set(so, "bestTimeValue", bestTime);
            Set(so, "nextStarLabel", nextLabel); Set(so, "nextStarValue", nextValue);
            Set(so, "continueButton", next); Set(so, "retryButton", retry);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_Text ResultRow(RectTransform card, string name, float y, string label, float valueSize, Color valueColor, out TMP_Text labelText)
        {
            float height = valueSize * 1.5f;
            var row = At(name, card, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(950f, height));
            labelText = Txt(At("Label", row, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(520f, 70f)), label, 44, Theme.Stone, _semi, TextAlignmentOptions.Left);
            labelText.rectTransform.pivot = new Vector2(0f, 0.5f);
            var value = Txt(At("Value", row, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(520f, height)), "0:00.00", valueSize, valueColor, _condItalic, TextAlignmentOptions.Right);
            value.rectTransform.pivot = new Vector2(1f, 0.5f);
            return value;
        }

        // ================================================================== MENU

        static void BuildMenu(RectTransform canvas, Button openButton, RaceManager race, FloatingJoystick joystick)
        {
            var host = Stretch("GameMenu", canvas);
            var panel = Stretch("Panel", host);
            var dimRt = Stretch("Dim", panel);
            var dimImage = Img(dimRt, null, Theme.WithAlpha(Theme.Ink, 0.7f), raycast: true);
            var dim = dimRt.gameObject.AddComponent<Button>();
            dim.targetGraphic = dimImage;
            dim.transition = Selectable.Transition.None;

            // The card sizes itself to its content; its shadow and face are stretched children outside the layout.
            var card = At("Card", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(930f, 1000f));
            var cardShadow = Stretch("Shadow", card);
            cardShadow.offsetMin = cardShadow.offsetMax = new Vector2(0f, -20f);
            Img(cardShadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            cardShadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var cardFace = Stretch("Face", card);
            Img(cardFace, "Round", Theme.Cream, sliced: true, raycast: true);
            cardFace.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(80, 80, 80, 90);
            layout.spacing = 34f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var closeRt = At("Close", card, new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(110f, 110f));
            closeRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var closeImage = Img(closeRt, null, new Color(0, 0, 0, 0), raycast: true);
            Img(At("Icon", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f)), "IconClose", Theme.Ink);
            var close = closeRt.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImage;
            closeRt.gameObject.AddComponent<ButtonFeedback>();

            var title = Txt(Layout(Rt("Title", card), 770f, 150f), "PAUSED", 136, Theme.Ink, _black);
            var resume = MenuButton(card, "ResumeButton", "RESUME", Theme.Orange, Theme.OrangeDeep, Color.white, 170f, out _);
            var restart = MenuButton(card, "RestartButton", "RESTART", Color.white, Theme.Ink, Theme.Ink, 150f, out _);
            var mainMenu = MenuButton(card, "MainMenuButton", "MAP", Color.white, Theme.Ink, Theme.Ink, 150f, out _);

            var settings = Txt(Layout(Rt("SettingsTitle", card), 770f, 80f), "SETTINGS", 40, Theme.Rust, _condItalic);
            settings.characterSpacing = 12f;

            var sound = Layout(Rt("Sound", card), 770f, 110f);
            Img(At("Icon", sound, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(84f, 84f)), "IconSound", Theme.Ink).rectTransform.pivot = new Vector2(0f, 0.5f);
            var slider = BuildSlider(sound);

            var joystickButton = MenuButton(card, "JoystickButton", "JOYSTICK   VISIBLE", Color.white, Theme.Ink, Theme.Ink, 130f, out var joystickLabel, 46);
            var driftButton = MenuButton(card, "DriftButton", "DRIFT   MANUAL", Color.white, Theme.Ink, Theme.Ink, 130f, out var driftLabel, 46);
            var assistButton = MenuButton(card, "AssistButton", "STEERING HELP   ON", Color.white, Theme.Ink, Theme.Ink, 130f, out var assistLabel, 46);
            var tutorialButton = MenuButton(card, "TutorialButton", "TUTORIAL", Color.white, Theme.Ink, Theme.Ink, 130f, out _, 46);
            var cupsButton = MenuButton(card, "CupsButton", "UNLOCK ALL   OFF", Color.white, Theme.Ink, Theme.Ink, 130f, out var cupsLabel, 46);
            var resetButton = MenuButton(card, "ResetButton", "RESET GAME", Color.white, Theme.Rust, Theme.Rust, 130f, out var resetLabel, 46);

            Configure(panel.gameObject.AddComponent<PopIn>(), ("card", card), ("backdrop", dimImage));
            var menu = host.gameObject.AddComponent<GameMenu>();
            var so = new SerializedObject(menu);
            Set(so, "race", race); Set(so, "joystick", joystick);
            Set(so, "openButton", openButton); Set(so, "panel", panel.gameObject); Set(so, "closeArea", dim);
            Set(so, "closeButton", close); Set(so, "title", title);
            Set(so, "resumeButton", resume); Set(so, "restartButton", restart); Set(so, "mainMenuButton", mainMenu);
            Set(so, "volumeSlider", slider); Set(so, "joystickButton", joystickButton); Set(so, "joystickLabel", joystickLabel);
            Set(so, "resetButton", resetButton); Set(so, "resetLabel", resetLabel); Set(so, "tutorialButton", tutorialButton);
            Set(so, "cupsButton", cupsButton); Set(so, "cupsLabel", cupsLabel);
            Set(so, "assistButton", assistButton); Set(so, "assistLabel", assistLabel);
            Set(so, "driftButton", driftButton); Set(so, "driftLabel", driftLabel);
            Set(so, "catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Data/Levels/LevelCatalog.asset"));
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
        }

        static Slider BuildSlider(RectTransform parent)
        {
            var root = At("Slider", parent, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(620f, 110f));
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            var track = Stretch("Track", root);
            track.anchorMin = new Vector2(0f, 0.38f); track.anchorMax = new Vector2(1f, 0.62f);
            Img(track, "Pill", Theme.CreamDark, sliced: true);
            var fillArea = Stretch("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.38f); fillArea.anchorMax = new Vector2(1f, 0.62f);
            fillArea.offsetMax = new Vector2(-40f, 0f);
            var fill = Stretch("Fill", fillArea);
            fill.offsetMax = new Vector2(40f, 0f);
            Img(fill, "Pill", Theme.Orange, sliced: true);
            var handleArea = Stretch("Handle Slide Area", root);
            handleArea.offsetMin = new Vector2(40f, 0f); handleArea.offsetMax = new Vector2(-40f, 0f);
            var handle = Rt("Handle", handleArea);
            handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(90f, 0f);
            Img(At("Shadow", handle, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(90f, 90f)), "Circle", Theme.Ink);
            var knob = Img(At("Knob", handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f)), "Circle", Color.white, raycast: true);
            Img(At("Ring", handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f)), "Ring", Theme.Ink);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = knob;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
            return slider;
        }

        static Button MenuButton(RectTransform card, string name, string label, Color face, Color shadow, Color text, float height,
            out TMP_Text labelText, float fontSize = 60)
        {
            var rt = Layout(Rt(name, card), 770f, height + 14f);
            var shadowRt = Stretch("Shadow", rt);
            shadowRt.offsetMax = new Vector2(0f, -14f);
            Img(shadowRt, "Round", shadow, sliced: true);
            var faceRt = Stretch("Face", rt);
            faceRt.offsetMin = new Vector2(0f, 14f);
            var bg = Img(faceRt, "Round", face, sliced: true, raycast: true);
            labelText = Txt(Stretch("Label", faceRt), label, fontSize, text, _condItalic);
            labelText.characterSpacing = 6f;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        // ================================================================== building blocks

        static void LoadFonts()
        {
            Theme.Use(Theme.Valley); // screens are built in the Valley colours; other cups recolour them at runtime
            MapLayout.Current = MapLayout.Valley;
            _semi = Font("Barlow-SemiBold");
            _bold = Font("Barlow-Bold");
            _cond = Font("BarlowCondensed-ExtraBold");
            _condItalic = Font("BarlowCondensed-ExtraBoldItalic");
            _black = Font("BarlowCondensed-BlackItalic");
            _blackShadow = AssetDatabase.LoadAssetAtPath<Material>(FontFolder + "BarlowCondensed-BlackItalic Shadow.mat");
            _blackOutline = AssetDatabase.LoadAssetAtPath<Material>(FontFolder + "BarlowCondensed-BlackItalic Outline.mat");
            _condOutline = AssetDatabase.LoadAssetAtPath<Material>(FontFolder + "BarlowCondensed-ExtraBoldItalic Outline.mat");
        }

        static TMP_FontAsset Font(string name) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + name + " SDF.asset");

        /// <summary>Creates the Barlow font assets (dynamic SDF) and the shadow/outline material presets.</summary>
        [MenuItem("SomeGame/Create Fonts")]
        public static void CreateFonts()
        {
            const string chars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·–—’“”…×°";
            foreach (var name in new[] { "Barlow-SemiBold", "Barlow-Bold", "BarlowCondensed-ExtraBold", "BarlowCondensed-ExtraBoldItalic", "BarlowCondensed-BlackItalic" })
            {
                string path = FontFolder + name + " SDF.asset";
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) continue;
                var font = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(FontFolder + name + ".ttf");
                var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                asset.name = name + " SDF";
                AssetDatabase.CreateAsset(asset, path);
                asset.atlasTextures[0].name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                asset.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                asset.TryAddCharacters(chars, out _);
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            Preset("BarlowCondensed-BlackItalic", "Shadow", m =>
            {
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", Theme.Ink);
                m.SetFloat("_UnderlayOffsetX", 0f);
                m.SetFloat("_UnderlayOffsetY", -0.5f);
                m.SetFloat("_UnderlayDilate", 0.1f);
                m.SetFloat("_UnderlaySoftness", 0f);
            });
            Preset("BarlowCondensed-BlackItalic", "Outline", Outline);
            Preset("BarlowCondensed-ExtraBoldItalic", "Outline", Outline);
            var defaultFont = Font("Barlow-Bold");
            var so = new SerializedObject(TMP_Settings.instance);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = defaultFont;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            static void Outline(Material m)
            {
                m.EnableKeyword("OUTLINE_ON");
                m.SetColor("_OutlineColor", Theme.Ink);
                m.SetFloat("_OutlineWidth", 0.24f);
                m.SetFloat("_FaceDilate", 0.24f);
            }
        }

        static void Preset(string font, string suffix, System.Action<Material> setup)
        {
            string path = FontFolder + $"{font} {suffix}.mat";
            var asset = Font(font);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(asset.material);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.CopyPropertiesFromMaterial(asset.material);
            mat.shaderKeywords = asset.material.shaderKeywords;
            setup(mat);
            EditorUtility.SetDirty(mat);
        }

        static RectTransform CreateCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        static RectTransform SafeArea(RectTransform parent)
        {
            var safe = Stretch("SafeArea", parent);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return safe;
        }

        // A rounded square button with a hard shadow: pine green with a cream icon, or white with a green one.
        static Button SquareButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, string icon, float size, bool white = false)
        {
            var rt = At(name, parent, anchor, position, new Vector2(size, size + 12f));
            Img(At("Shadow", rt, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(size, size)), "Round", white ? Theme.Ink : Shade(Theme.Ink, 0.6f), sliced: true);
            var face = At("Face", rt, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(size, size));
            var bg = Img(face, "Round", white ? Color.white : Theme.Ink, sliced: true, raycast: true);
            Img(At("Icon", face, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.48f, size * 0.48f)), icon, white ? Theme.Ink : Theme.Cream);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        // The big round orange action button with a hard shadow and a slowly pulsing dashed ring.
        static Button BigRoundButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, float size, string label,
            out Image face, out Image shadow, out TMP_Text text, out RectTransform ring)
        {
            var rt = At(name, parent, anchor, position, new Vector2(size * 1.18f, size * 1.18f));
            ring = At("Ring", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 1.18f, size * 1.18f));
            var ringImage = Img(ring, "RingDashed", Color.white);
            Configure(ring.gameObject.AddComponent<UIPulse>(), ("speed", 0.5f), ("fade", ringImage), ("scale", new Vector2(0.97f, 1.04f)), ("alpha", new Vector2(0.7f, 1f)));
            shadow = Img(At("Shadow", rt, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(size, size)), "Circle", Theme.OrangeDeep);
            face = Img(At("Fill", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size)), "Circle", Theme.Orange, raycast: true);
            text = Txt(At("Label", rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(size, size * 0.5f)), label, size * 0.27f, Color.white, _black);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        static TMP_Text Chip(RectTransform parent, string text)
        {
            var rt = Rt("Chip", parent);
            Img(rt, "Round", Theme.Ink, sliced: true); // same pine green as the star-times panel
            var fit = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(30, 30, 8, 8);
            fit.childControlWidth = true; fit.childControlHeight = true;
            var label = Txt(Rt("Label", rt), text, 40, Theme.Cream, _bold);
            return label;
        }

        static void Pop(RectTransform rt, float delay) => Configure(rt.gameObject.AddComponent<UIPopIn>(), ("delay", delay));

        static RectTransform Layout(RectTransform rt, float width, float height)
        {
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            return rt;
        }

        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var rt = Rt(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static RectTransform At(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = Rt(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        // The track renderer's snow materials (Winter Cup), the scenery mesh it re-skins and the snowfall.
        static void WireSnowBiome(GameObject snowfall)
        {
            var renderer = Object.FindAnyObjectByType<SomeGame.Track.TrackRenderer>();
            if (renderer == null) return;
            Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(ArtGenerator.Folder + "/" + name + ".mat");
            var scenery = Object.FindAnyObjectByType<SomeGame.Track.TrackScenery>();
            var sceneryMesh = scenery != null ? new SerializedObject(scenery).FindProperty("trees").objectReferenceValue as MeshFilter : null;
            var sceneryRenderer = sceneryMesh != null ? sceneryMesh.GetComponent<MeshRenderer>() : null;
            Configure(renderer, ("snowGround", Mat("SnowGround")), ("snowRoad", Mat("WinterRoad")), ("snowKerbs", Mat("SnowKerb")),
                ("snowRunoff", Mat("SnowRunoff")), ("snowScenery", Mat("SceneryWinter")), ("scenery", sceneryRenderer), ("snowfall", snowfall));
        }

        // Renderers for the other ways round (TrackBranches) under the track: road branches just under the
        // main road (junctions blend into it), their edge lines, and ice under the road but over the scenery.
        static void EnsureBranchRenderers()
        {
            var track = Object.FindAnyObjectByType<SomeGame.Track.Track>();
            if (track == null) return;
            Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(ArtGenerator.Folder + "/" + name + ".mat");
            MeshFilter Layer(string name, string material, int order)
            {
                var t = track.transform.Find(name);
                var go = t != null ? t.gameObject : new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(track.transform, false);
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Mat(material);
                renderer.sortingOrder = order;
                return go.GetComponent<MeshFilter>();
            }
            var roads = Layer("BranchRoads", "Road", -52);
            var edges = Layer("BranchEdges", "Line", -51);
            var ice = Layer("BranchIce", "Ice", -56);
            var roofShadows = Layer("RoofShadows", "Trail", -45);
            var roofs = Layer("Roofs", "RoofRow", -41);
            // The winter surprises' sprites on the hazards.
            var hazards = Object.FindAnyObjectByType<SomeGame.Track.TrackHazards>();
            if (hazards != null)
                Configure(hazards, ("snowballSprite", Sprite("Snowball")), ("jumpPadSprite", Sprite("JumpPad")), ("penguinSprite", Sprite("Penguin")),
                    ("dashPanelSprite", Sprite("DashPanel")));
            var bankLayer = Layer("Banks", "IceWall", -47);
            var banks = track.GetComponent<SomeGame.Track.TrackBanks>();
            if (banks == null) banks = track.gameObject.AddComponent<SomeGame.Track.TrackBanks>();
            Configure(banks, ("track", track), ("walls", bankLayer));
            var branches = track.GetComponent<SomeGame.Track.TrackBranches>();
            if (branches == null) branches = track.gameObject.AddComponent<SomeGame.Track.TrackBranches>(); // Unity's null: no ?? here
            Configure(branches, ("track", track), ("roads", roads), ("edges", edges), ("ice", ice), ("roofs", roofs), ("roofShadows", roofShadows), ("meadowRoad", Mat("Road")), ("snowRoad", Mat("WinterRoad")));
        }

        static TrackLine Line(RectTransform rt)
        {
            rt.gameObject.AddComponent<CanvasRenderer>();
            return rt.gameObject.AddComponent<TrackLine>();
        }

        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

        static Image Img(RectTransform rt, string sprite, Color color, bool sliced = false, bool raycast = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? Sprite(sprite) : null;
            image.color = color;
            image.raycastTarget = raycast;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (sliced) image.pixelsPerUnitMultiplier = 1f;
            image.preserveAspect = !sliced && sprite != null;
            return image;
        }

        static TextMeshProUGUI Txt(RectTransform rt, string text, float size, Color color, TMP_FontAsset font,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        static Color Shade(Color c, float k) => new(c.r * k, c.g * k, c.b * k, c.a);
        static Color Hex(string hex) => Sdf.Hex(hex);

        static void Set(SerializedObject so, string property, Object value) => so.FindProperty(property).objectReferenceValue = value;

        static void SetArray<T>(SerializedObject so, string property, T[] values) where T : Object
        {
            var p = so.FindProperty(property);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void Configure(Object target, params (string name, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (name, value) in values)
            {
                var p = so.FindProperty(name);
                switch (value)
                {
                    case float f: p.floatValue = f; break;
                    case int i: p.intValue = i; break;
                    case bool b: p.boolValue = b; break;
                    case Vector2 v: p.vector2Value = v; break;
                    case Object o: p.objectReferenceValue = o; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
