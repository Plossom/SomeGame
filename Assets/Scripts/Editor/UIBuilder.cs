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
    public static partial class UIBuilder
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

            // The track picture in a framed card with a hard shadow, sized to the picture's shape (centred where
            // the largest card would be).
            var card = At("Preview", safe, new Vector2(0.5f, 1f), new Vector2(0f, -560f - 750f), new Vector2(860f, 1500f));
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0f, -560f - 750f);
            var cardShadow = Stretch("Shadow", card);
            cardShadow.offsetMin = cardShadow.offsetMax = new Vector2(0f, -22f);
            Img(cardShadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Frame", card), "Round", Theme.Ink, sliced: true);
            var picture = Stretch("Picture", card);
            picture.offsetMin = new Vector2(22f, 22f); picture.offsetMax = new Vector2(-22f, -22f);
            var previewImage = Img(picture, null, Color.white);
            previewImage.preserveAspect = true;
            Configure(card.gameObject.AddComponent<PreviewFrame>(), ("picture", previewImage), ("maxSize", new Vector2(1000f, 1500f)), ("inset", 22f));

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
            var barBody = Img(Stretch("Body", car), "Car", Theme.Orange); barBody.preserveAspect = false;
            var barDetails = Img(Stretch("Details", car), "CarDetails", Color.white); barDetails.preserveAspect = false;
            Configure(car.gameObject.AddComponent<VehicleIcon>(), ("body", barBody), ("details", barDetails),
                ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")));
            // A race against a rival with a face: the duel band (your car VS them at the wheel) between the picture
            // (made smaller then) and the bar.
            var vsBand = At("Duel", safe, new Vector2(0.5f, 0f), new Vector2(0f, 420f), new Vector2(1060f, 380f));
            vsBand.pivot = new Vector2(0.5f, 0f);
            var vsShadow = Stretch("Shadow", vsBand); vsShadow.offsetMin = new Vector2(0f, -16f); vsShadow.offsetMax = new Vector2(0f, -16f);
            Img(vsShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Face", vsBand), "Round", Theme.Ink, sliced: true);
            var vsYou = Stretch("You", vsBand);
            var youCar = At("Car", vsYou, new Vector2(0.25f, 0.5f), new Vector2(-10f, 36f), new Vector2(180f, 315f));
            youCar.pivot = new Vector2(0.5f, 0.5f); youCar.localRotation = Quaternion.Euler(0f, 0f, -90f);
            var ycShadow = Img(At("Shadow", youCar, new Vector2(0.5f, 0.5f), new Vector2(10f, -12f), new Vector2(188f, 328f)), "CarShadow", new Color(0f, 0f, 0f, 0.5f)); ycShadow.preserveAspect = false;
            var ycBody = Img(Stretch("Body", youCar), "Car", Theme.Orange); ycBody.preserveAspect = false;
            var ycDetails = Img(Stretch("Details", youCar), "CarDetails", Color.white); ycDetails.preserveAspect = false;
            var vsCar = youCar.gameObject.AddComponent<VehicleIcon>();
            Configure(vsCar, ("body", ycBody), ("details", ycDetails), ("shadow", ycShadow),
                ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")));
            var youPlate = At("Name", vsYou, new Vector2(0.25f, 0f), new Vector2(-10f, 30f), new Vector2(220f, 64f));
            youPlate.pivot = new Vector2(0.5f, 0f);
            Img(youPlate, "Pill", Theme.Orange, sliced: true);
            Txt(Stretch("Label", youPlate), "YOU", 44, Color.white, _black).characterSpacing = 6f;
            var vsThem = Stretch("Them", vsBand);
            var rivalDriving = Img(At("Picture", vsThem, new Vector2(0.75f, 0.5f), new Vector2(0f, 40f), new Vector2(360f, 330f)), "Circle", Color.white);
            rivalDriving.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var themPlate = At("Name", vsThem, new Vector2(0.75f, 0f), new Vector2(0f, 30f), new Vector2(320f, 64f));
            themPlate.pivot = new Vector2(0.5f, 0f);
            Img(themPlate, "Pill", Theme.Cream, sliced: true);
            var vsName = Txt(Stretch("Label", themPlate), "ROSIE RUST", 40, Theme.Ink, _black);
            vsName.enableAutoSizing = true; vsName.fontSizeMin = 24; vsName.fontSizeMax = 40;
            var vsBadge = At("VS", vsBand, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(190f, 190f));
            vsBadge.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", vsBadge, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(190f, 190f)), "Circle", Theme.OrangeDeep);
            Img(Stretch("Ring", vsBadge), "Circle", Theme.Cream);
            Img(At("Face", vsBadge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.Orange);
            var vsText = Txt(Stretch("Label", vsBadge), "VS", 96, Color.white, _black);
            vsText.fontSharedMaterial = _blackOutline;
            vsBand.gameObject.SetActive(false);
            var percent = Txt(At("Percent", safe, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 70f)), "LOADING 30%", 48, Theme.Ink, _condItalic);
            percent.characterSpacing = 6f;

            var screen = canvas.gameObject.AddComponent<LoadingScreen>();
            var so = new SerializedObject(screen);
            Set(so, "group", group); Set(so, "preview", previewImage); Set(so, "chapter", chapter); Set(so, "title", title);
            Set(so, "details", details); Set(so, "barFill", fill); Set(so, "barCar", car); Set(so, "percent", percent); Set(so, "rival", rivalDriving);
            Set(so, "vsBand", vsBand); Set(so, "vsYou", vsYou); Set(so, "vsThem", vsThem); Set(so, "vsBadge", vsBadge); Set(so, "vsCar", vsCar); Set(so, "vsName", vsName);
            Set(so, "previewFrame", card.GetComponent<PreviewFrame>());
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
            var mapShadow = Img(At("Shadow", car, new Vector2(0.5f, 0.5f), new Vector2(6f, -8f), new Vector2(70f, 120f)), "CarShadow", new Color(0f, 0f, 0f, 0.45f)); mapShadow.preserveAspect = false;
            var mapBody = Img(Stretch("Body", car), "Car", Theme.Orange); mapBody.preserveAspect = false;
            var mapDetails = Img(Stretch("Details", car), "CarDetails", Color.white); mapDetails.preserveAspect = false;
            // A snowmobile on the Winter Cup's map.
            Configure(car.gameObject.AddComponent<VehicleIcon>(), ("body", mapBody), ("details", mapDetails), ("shadow", mapShadow),
                ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")), ("snowmobileShadow", Sprite("SnowmobileShadow")));

            // Snowfall over the landscape (under the header), switched on by the winter map.
            var snow = Stretch("Snow", canvas);
            Configure(snow.gameObject.AddComponent<SnowFall>(), ("flake", Sprite("Circle")));
            snow.gameObject.SetActive(false);

            var safe = SafeArea(canvas);
            var menuButton = SquareButton(safe, "MenuButton", new Vector2(0f, 1f), new Vector2(60f, -50f), "IconMenu", 140f);

            // Your stars on one small plate, its top level with the menu button: low, so that the right column (stars,
            // online, the cup) ends level with the left one (menu, garage).
            var counters = At("Counters", safe, new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(140f, 96f));
            // A hard shadow under it like the menu button's (which makes that one 12 units taller).
            var countersShadow = At("Shadow", counters, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(140f, 96f));
            Img(countersShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Face", counters), "Round", Theme.Ink, sliced: true);
            TMP_Text CounterRow(string name, float y, string icon)
            {
                var row = At(name, counters, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(140f, 64f));
                Img(At("Icon", row, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(38f, 38f)), icon, Theme.Amber);
                return Txt(At("Count", row, new Vector2(1f, 0.5f), new Vector2(-20f, -2f), new Vector2(76f, 64f)), "0", 46, Theme.Amber, _condItalic, TextAlignmentOptions.Right);
            }
            var total = CounterRow("Stars", -16f, "IconStar");

            // The cup name, big, centred in the gap between the menu button (ends 200 from the left) and the
            // stars and trophies counter (starts 256 from the right), level with the menu button.
            var cupRt = Rt("Cup", safe);
            cupRt.anchorMin = new Vector2(0f, 1f); cupRt.anchorMax = new Vector2(1f, 1f); cupRt.pivot = new Vector2(0.5f, 1f);
            cupRt.offsetMin = new Vector2(216f, -206f); cupRt.offsetMax = new Vector2(-216f, -40f);
            var chapter = Txt(cupRt, "VALLEY CUP", 128, Theme.Rust, _condItalic);
            chapter.characterSpacing = 6f;
            chapter.enableAutoSizing = true; chapter.fontSizeMin = 70; chapter.fontSizeMax = 128;
            // No race header under it: the race cards (RaceDeck) show a race's name, stars and records.


            // The garage: a button under the menu button showing your car as built, slowly turning (a dot when
            // something new has opened).
            var carButtonRt = At("GarageButton", safe, new Vector2(0f, 1f), new Vector2(60f, -222f), new Vector2(140f, 152f));
            Img(At("Shadow", carButtonRt, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(140f, 140f)), "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            var carFace = At("Face", carButtonRt, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(140f, 140f));
            var carFaceImage = Img(carFace, "Round", Theme.Ink, sliced: true, raycast: true);
            Img(At("Glow", carFace, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f)), "Glow", Theme.WithAlpha(Theme.Amber, 0.35f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var carSpin = At("Spin", carFace, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            carSpin.pivot = new Vector2(0.5f, 0.5f);
            var cardCar = At("Car", carSpin, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 112f));
            cardCar.pivot = new Vector2(0.5f, 0.5f);
            var ccShadow = Img(At("Shadow", cardCar, new Vector2(0.5f, 0.5f), new Vector2(5f, -6f), new Vector2(66f, 116f)), "CarShadow", new Color(0f, 0f, 0f, 0.5f));
            ccShadow.preserveAspect = false;
            var ccBody = Img(Stretch("Body", cardCar), "Car", Theme.Orange); ccBody.preserveAspect = false;
            var ccDetails = Img(Stretch("Details", cardCar), "CarDetails", Color.white); ccDetails.preserveAspect = false;
            var garageCar = cardCar.gameObject.AddComponent<VehicleIcon>();
            Configure(garageCar, ("body", ccBody), ("details", ccDetails), ("shadow", ccShadow),
                ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")), ("snowmobileShadow", Sprite("SnowmobileShadow")));
            Configure(carSpin.gameObject.AddComponent<GarageSpin>(), ("target", carSpin), ("idleSpeed", 24f));
            var garageButton = carButtonRt.gameObject.AddComponent<Button>();
            garageButton.targetGraphic = carFaceImage; garageButton.transition = Selectable.Transition.None;
            carButtonRt.gameObject.AddComponent<ButtonFeedback>();
            var garageDot = At("NewDot", carButtonRt, new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(44f, 44f));
            garageDot.pivot = new Vector2(0.5f, 0.5f);
            Img(garageDot, "Circle", Theme.Cream);
            Img(At("Dot", garageDot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f)), "Circle", Theme.Orange);

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

            var deck = BuildRaceDeck(canvas);
            var garage = BuildGarage(canvas);
            var meeting = BuildRivalMeeting(canvas);

            var screen = canvas.gameObject.AddComponent<MapScreen>();
            var so = new SerializedObject(screen);
            Set(so, "catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Data/Levels/LevelCatalog.asset"));
            Set(so, "world", world); Set(so, "nodeTemplate", node); Set(so, "car", car); Set(so, "background", background); Set(so, "ambience", ambienceComp); Set(so, "snow", snow.gameObject); Set(so, "totalStars", total);
            Set(so, "chapterLabel", chapter);
            Set(so, "deck", deck);
            Set(so, "garageButton", garageButton); Set(so, "garageDot", garageDot.gameObject); Set(so, "garage", garage); Set(so, "meeting", meeting); Set(so, "garageCar", garageCar);
            // Swipe between cups: the page parts slide (the header and cup name a little faster, for depth).
            SetArray(so, "page", new[] { map, cupRt });
            var parallax = so.FindProperty("pageParallax");
            float[] depth = { 1f, 1.3f };
            parallax.arraySize = depth.Length;
            for (int i = 0; i < depth.Length; i++) parallax.GetArrayElementAtIndex(i).floatValue = depth[i];
            // The cup column under the menu button: a dark plate (clipping) with a badge per cup in a column
            // and a glowing ring on the current one; folded it shows just the current cup and a small arrow.
            // Above the frozen picture of a cup switch (so it can be seen unfolding and selecting), below the menus.
            var columnLayer = SafeArea(canvas);
            columnLayer.name = "CupColumnLayer";
            columnLayer.SetSiblingIndex(wipe.GetSiblingIndex() + 1);
            // Folded it shows the current cup's badge (tap it to unfold); its bottom level with the garage button's.
            var dots = At("CupColumn", columnLayer, new Vector2(1f, 1f), new Vector2(-60f, -280f), new Vector2(140f, 132f)); // under the ONLINE button
            Img(dots, "Round", Theme.Ink, sliced: true, raycast: true);
            dots.gameObject.AddComponent<RectMask2D>();
            var content = At("Content", dots, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(140f, 600f));
            var ring = At("Ring", content, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(126f, 126f));
            ring.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Glow", ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132f, 132f)), "Circle", Theme.WithAlpha(Theme.Amber, 0.22f));
            Img(At("Line", ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(126f, 126f)), "Ring", Theme.Amber);
            var badgeT = At("Badge", content, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(112f, 112f));
            badgeT.pivot = new Vector2(0.5f, 0.5f);
            var badgeFace = Img(At("Face", badgeT, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f)), "Circle", Theme.Cream, raycast: true);
            Img(At("Icon", badgeT, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f)), null, Color.white).preserveAspect = true;
            badgeT.gameObject.AddComponent<CanvasGroup>();
            var badgeButton = badgeT.gameObject.AddComponent<Button>();
            badgeButton.targetGraphic = badgeFace; badgeButton.transition = Selectable.Transition.None;
            badgeT.gameObject.AddComponent<ButtonFeedback>();
            // (No arrow: the current cup's badge unfolds the column.)
            Set(so, "barContent", content);
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
            SetArray(so, "racePageParts", new GameObject[0]);
            var celebration = BuildCupCelebration(canvas);
            Configure(celebration, ("flyTarget", dots)); // the new cup's badge flies into the dropdown
            Set(so, "celebration", celebration); Set(so, "sparkle", Sprite("Sparkle"));
            
            BuildOnlineLobby(canvas, safe); // over everything else on the map
            // The swipe step of the tour points at the middle of the map.
            var swipeArea = At("SwipeArea", safe, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(900f, 640f));
            swipeArea.pivot = new Vector2(0.5f, 0.5f);
            BuildMapTour(canvas, screen, null, null, null, null, swipeArea, dots, counters,
                (RectTransform)safe.Find("OnlineButton"), (RectTransform)menuButton.transform, null); // the last step shows the selected stop

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
            var needs = At("Needs", page, new Vector2(0.5f, 0.5f), new Vector2(0f, -350f), new Vector2(340f, 150f));
            Img(needs, "Round", Theme.Ink, sliced: true);
            void Need(string name, float x, string icon)
            {
                var item = At(name, needs, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(290f, 130f));
                Img(At("Icon", item, new Vector2(0.5f, 0.5f), new Vector2(-82f, 0f), new Vector2(80f, 80f)), icon, Theme.Amber);
                Txt(At("Count", item, new Vector2(0.5f, 0.5f), new Vector2(42f, -2f), new Vector2(190f, 120f)), "0/20", 76, Theme.Amber, _condItalic, TextAlignmentOptions.Left);
            }
            Need("Stars", 0f, "IconStar");
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
        // The garage (GarageScreen): a card like the race cards with your car big on a turntable under a spotlight,
        // the paint's name, and a swatch per paint (locked ones with the stars they need).
        // The garage: a showroom over the map. The top: back, GARAGE, your stars. The stage: a spotlight, the floor
        // with its rings, your car turning (drag to spin it), a burst of light when a part goes on, its name plate.
        // The shelf: a row of tabs, what the chosen thing is, and a cream shelf of tiles (or the rivals' cards).
        // The garage: a showroom over the map. The top: back, GARAGE, your stars, the rivals' cards. In the middle your
        // car on a lit podium with a hotspot for each kind of thing joined to it by a line; at the bottom the row of
        // cards for the chosen slot (a tray sliding up), and the rivals' cards in a panel of their own.
        static GarageScreen BuildGarage(RectTransform canvas)
        {
            var host = Stretch("Garage", canvas);
            var panel = Stretch("Panel", host);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            Img(Stretch("Backdrop", panel), null, Shade(Theme.Ink, 0.72f), raycast: true);
            var glow = Img(At("Spotlight", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(2000f, 2000f)), "Glow", Theme.WithAlpha(Theme.Amber, 0.32f));
            glow.preserveAspect = false; glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var sheet = Stretch("Sheet", panel);
            var safe = SafeArea(sheet);

            var close = SquareButton(safe, "Close", new Vector2(0f, 1f), new Vector2(48f, -48f), "IconClose", 124f);
            var title = Txt(At("Title", safe, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(700f, 130f)), "GARAGE", 112, Color.white, _black);
            title.fontSharedMaterial = _blackOutline;
            var starsPill = At("Stars", safe, new Vector2(1f, 1f), new Vector2(-48f, -62f), new Vector2(230f, 104f));
            Img(starsPill, "Pill", Shade(Theme.Ink, 0.55f), sliced: true);
            Img(At("Star", starsPill, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(64f, 64f)), "IconStar", Theme.Amber).rectTransform.pivot = new Vector2(0f, 0.5f);
            var stars = Txt(At("Count", starsPill, new Vector2(1f, 0.5f), new Vector2(-30f, 2f), new Vector2(130f, 90f)), "24", 64, Color.white, _black, TextAlignmentOptions.Right);
            stars.rectTransform.pivot = new Vector2(1f, 0.5f);
            var rivals = SquareButton(safe, "Rivals", new Vector2(1f, 1f), new Vector2(-48f, -190f), "IconCards", 124f);
            // The vehicles under GARAGE: a bar of badges (like the race deck's bar of race numbers), each with its
            // vehicle in it; the ring glides along to the one on show.
            var vehicleBar = At("VehicleBar", safe, new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(330f, 164f));
            Img(vehicleBar, "Round", Theme.Ink, sliced: true, raycast: true);
            var vehicleBadge = At("Vehicle", vehicleBar, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(134f, 134f));
            vehicleBadge.pivot = new Vector2(0.5f, 0.5f);
            var badgeFace = Img(At("Face", vehicleBadge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(134f, 134f)), "Circle", Theme.Cream, raycast: true);
            var badgeCar = At("Car", vehicleBadge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 112f));
            badgeCar.pivot = new Vector2(0.5f, 0.5f);
            var bBody = Img(Stretch("Body", badgeCar), "Car", Theme.Orange); bBody.preserveAspect = false;
            var bDetails = Img(Stretch("Details", badgeCar), "CarDetails", Color.white); bDetails.preserveAspect = false;
            Configure(badgeCar.gameObject.AddComponent<VehicleIcon>(), ("body", bBody), ("details", bDetails),
                ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")));
            var badgeButton = vehicleBadge.gameObject.AddComponent<Button>();
            badgeButton.targetGraphic = badgeFace; badgeButton.transition = Selectable.Transition.None;
            vehicleBadge.gameObject.AddComponent<ButtonFeedback>();
            var vehicleRing = At("Ring", vehicleBar, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(164f, 164f));
            vehicleRing.pivot = new Vector2(0.5f, 0.5f);
            Img(vehicleRing, "Ring", Theme.Amber);

            // The stage: everything placed from its middle.
            var stage = Stretch("Stage", safe);
            var podium = At("Podium", stage, new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(800f, 800f));
            podium.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Glow", podium, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 1100f)), "Glow", Theme.WithAlpha(Theme.Amber, 0.22f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // A mid-tone floor: the cars have plenty of black (tyres, trim) that would sink into a dark one, and
            // can be painted white, which would vanish on a light one.
            Img(Stretch("Floor", podium), "Circle", Theme.WithAlpha(Theme.Cream, 0.4f));
            var rim = Stretch("Rim", podium); rim.offsetMin = new Vector2(12f, 12f); rim.offsetMax = new Vector2(-12f, -12f);
            Img(rim, "Ring", Theme.WithAlpha(Theme.Amber, 0.6f));
            var podiumRing = At("Dashes", podium, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 620f));
            podiumRing.pivot = new Vector2(0.5f, 0.5f);
            Img(podiumRing, "RingDashed", Theme.WithAlpha(Theme.Ink, 0.18f));
            var burst = Img(At("Burst", stage, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(640f, 640f)), "Ring", Theme.Amber);
            burst.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var carRoot = At("Car", stage, new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(420f, 735f));
            carRoot.pivot = new Vector2(0.5f, 0.5f);
            var carShadow = Img(At("Shadow", carRoot, new Vector2(0.5f, 0.5f), new Vector2(26f, -34f), new Vector2(436f, 760f)), "CarShadow", new Color(0f, 0f, 0f, 0.55f));
            carShadow.preserveAspect = false;
            var body = Img(Stretch("Body", carRoot), "Car", Theme.Orange); body.preserveAspect = false;
            var details = Img(Stretch("Details", carRoot), "CarDetails", Color.white); details.preserveAspect = false;
            var vehicle = carRoot.gameObject.AddComponent<VehicleIcon>();
            Configure(vehicle, ("body", body), ("details", details), ("shadow", carShadow),
                ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")), ("snowmobileShadow", Sprite("SnowmobileShadow")));
            var carHit = Img(Stretch("Hit", carRoot), null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var carButton = carRoot.gameObject.AddComponent<Button>();
            carButton.targetGraphic = carHit; carButton.transition = Selectable.Transition.None;
            // The other vehicles peeking in above and below (tap, or swipe up or down, to go to their page).
            RectTransform PeekAt(string name, float side)
            {
                var peek = At(name, stage, new Vector2(0.5f, 0.5f), new Vector2(0f, side > 0f ? 605f : -355f), new Vector2(300f, 300f));
                peek.pivot = new Vector2(0.5f, 0.5f);
                peek.gameObject.AddComponent<CanvasGroup>();
                var hit = Img(peek, null, new Color(0f, 0f, 0f, 0f), raycast: true);
                // The main car (420 wide) at the scale of a peek: it grows into it and shrinks out of it on a change.
                var peekCar = At("Car", peek, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 262.5f));
                peekCar.pivot = new Vector2(0.5f, 0.5f);
                var pShadow = Img(At("Shadow", peekCar, new Vector2(0.5f, 0.5f), new Vector2(9.3f, -12.1f), new Vector2(155.7f, 271.4f)), "CarShadow", new Color(0f, 0f, 0f, 0.55f)); pShadow.preserveAspect = false;
                var pBody = Img(Stretch("Body", peekCar), "Car", Theme.Orange); pBody.preserveAspect = false;
                var pDetails = Img(Stretch("Details", peekCar), "CarDetails", Color.white); pDetails.preserveAspect = false;
                Configure(peekCar.gameObject.AddComponent<VehicleIcon>(), ("body", pBody), ("details", pDetails), ("shadow", pShadow),
                    ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")), ("snowmobileShadow", Sprite("SnowmobileShadow")));
                var arrow = Img(At("Arrow", peek, new Vector2(0.5f, 0.5f), new Vector2(0f, side * 175f), new Vector2(54f, 64f)), "NavArrow", Theme.Amber).rectTransform;
                arrow.pivot = new Vector2(0.5f, 0.5f); arrow.localRotation = Quaternion.Euler(0f, 0f, side > 0f ? 0f : 180f);
                var label = Txt(At("Name", peek, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f)), "", 10, Theme.Cream, _black);
                label.gameObject.SetActive(false);
                var button = peek.gameObject.AddComponent<Button>();
                button.targetGraphic = hit; button.transition = Selectable.Transition.None;
                return peek;
            }
            // (No press squash on the peeks and hotspots: the garage scales them itself.)
            var peekLeft = PeekAt("PeekAbove", 1f);
            var peekRight = PeekAt("PeekBelow", -1f);
            // The vehicle leaving on a page change.
            var outgoing = At("Outgoing", stage, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(420f, 735f));
            outgoing.pivot = new Vector2(0.5f, 0.5f);
            outgoing.gameObject.AddComponent<CanvasGroup>();
            var oShadow = Img(At("Shadow", outgoing, new Vector2(0.5f, 0.5f), new Vector2(26f, -34f), new Vector2(436f, 760f)), "CarShadow", new Color(0f, 0f, 0f, 0.55f)); oShadow.preserveAspect = false;
            var oBody = Img(Stretch("Body", outgoing), "Car", Theme.Orange); oBody.preserveAspect = false;
            var oDetails = Img(Stretch("Details", outgoing), "CarDetails", Color.white); oDetails.preserveAspect = false;
            var outgoingCar = outgoing.gameObject.AddComponent<VehicleIcon>();
            Configure(outgoingCar, ("body", oBody), ("details", oDetails), ("shadow", oShadow),
                ("snowmobileBody", Sprite("SnowmobileIcon")), ("snowmobileDetails", Sprite("SnowmobileDetails")), ("snowmobileShadow", Sprite("SnowmobileShadow")));

            var highlight = Img(At("Highlight", stage, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f)), "Ring", Theme.WithAlpha(Theme.Amber, 0.8f));
            highlight.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // A hotspot: the line to the car, a dot where it meets it, the round button with its icon and a label.
            var spot = At("Hotspot", stage, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            spot.pivot = new Vector2(0.5f, 0.5f);
            var line = Img(At("Line", spot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 6f)), null, Theme.WithAlpha(Theme.Amber, 0.8f));
            line.rectTransform.pivot = new Vector2(0f, 0.5f);
            var dot = At("Dot", spot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            dot.pivot = new Vector2(0.5f, 0.5f);
            Img(dot, "Circle", Theme.Amber);
            Img(At("Core", dot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f)), "Circle", Color.white);
            var spotButton = At("Button", spot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(186f, 186f));
            spotButton.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Glow", spotButton, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 320f)), "Glow", Theme.WithAlpha(Theme.Amber, 0.5f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", spotButton, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(186f, 186f)), "Circle", Shade(Theme.Ink, 0.5f));
            var spotFace = Img(Stretch("Face", spotButton), "Circle", Theme.Ink, raycast: true);
            Img(At("Ring", spotButton, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(192f, 192f)), "Ring", Theme.Amber);
            Img(At("Icon", spotButton, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f)), "IconPaint", Color.white).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var spotLabel = At("Label", spotButton, new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(240f, 70f));
            spotLabel.pivot = new Vector2(0.5f, 0.5f);
            Img(spotLabel, "Pill", Theme.WithAlpha(Shade(Theme.Ink, 0.6f), 0.95f), sliced: true);
            Txt(At("Name", spotLabel, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(230f, 60f)), "PAINT", 46, Theme.Amber, _black).characterSpacing = 4f;
            var spotValue = Txt(At("Value", spotLabel, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(270f, 46f)), "SUNSET ORANGE", 34, Color.white, _condItalic);
            spotValue.enableAutoSizing = true; spotValue.fontSizeMin = 18; spotValue.fontSizeMax = 34;
            var spotButtonComp = spotButton.gameObject.AddComponent<Button>();
            spotButtonComp.targetGraphic = spotFace; spotButtonComp.transition = Selectable.Transition.None;

            var buildName = Txt(At("BuildName", stage, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1060f, 90f)), "DUNE BUGGY  ·  SUNSET ORANGE", 60, Color.white, _black);
            buildName.enableAutoSizing = true; buildName.fontSizeMin = 34; buildName.fontSizeMax = 60;
            var hint = Txt(At("Hint", stage, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(1000f, 50f)), "TAP A HOTSPOT TO CHANGE YOUR CAR", 34, Theme.Amber, _condItalic);
            hint.characterSpacing = 6f;

            // The tray for the chosen part: the part tabs and DONE, a strip of round chips (cup by cup), the name and
            // what it is (or what it takes).
            var tray = At("Tray", safe, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1170f, 600f));
            tray.pivot = new Vector2(0.5f, 0f);
            // (No panel behind the cards: they lie straight on the garage.)
            // The part tabs: jump straight to another part (the one open orange, with its name).
            // The part tabs at the top of the screen, under the page's name (always there).
            var tabs = At("Tabs", safe, new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(816f, 170f));
            tabs.pivot = new Vector2(0.5f, 1f);
            var tabRow = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabRow.spacing = 0f; tabRow.childControlWidth = tabRow.childControlHeight = false; tabRow.childForceExpandWidth = false; tabRow.childAlignment = TextAnchor.MiddleCenter;
            var tabT = Rt("Tab", tabs);
            tabT.sizeDelta = new Vector2(136f, 170f);
            Img(tabT, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var tabDisc = At("Disc", tabT, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(112f, 112f));
            tabDisc.pivot = new Vector2(0.5f, 1f);
            Img(At("Shadow", tabDisc, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(112f, 112f)), "Circle", Shade(Theme.CreamDark, 0.8f));
            var tabFace = Stretch("Face", tabT);
            tabFace.anchorMin = tabFace.anchorMax = new Vector2(0.5f, 1f); tabFace.pivot = new Vector2(0.5f, 1f);
            tabFace.sizeDelta = new Vector2(112f, 112f); tabFace.anchoredPosition = new Vector2(0f, -4f);
            Img(tabFace, "Circle", Theme.Ink);
            var tabIconImg = Img(At("Icon", tabT, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(60f, 60f)), "IconPaint", Color.white);
            tabIconImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Txt(At("Label", tabT, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(145f, 44f)), "PAINT", 30, Theme.InkSoft, _black).characterSpacing = 2f;
            var tabButton = tabT.gameObject.AddComponent<Button>();
            tabButton.targetGraphic = tabT.GetComponent<Image>(); tabButton.transition = Selectable.Transition.None;
            tabT.gameObject.AddComponent<ButtonFeedback>();
            var trayCount = Txt(At("Count", tray, new Vector2(1f, 1f), new Vector2(-48f, -146f), new Vector2(260f, 40f)), "4 / 8 OPEN", 30, Theme.Rust, _condItalic, TextAlignmentOptions.Center);
            trayCount.gameObject.SetActive(false);
            trayCount.rectTransform.pivot = new Vector2(1f, 1f); trayCount.characterSpacing = 4f;
            var done = At("Done", tray, new Vector2(1f, 1f), new Vector2(-48f, -36f), new Vector2(240f, 100f));
            done.pivot = new Vector2(1f, 1f);
            Img(At("Shadow", done, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(240f, 100f)), "Pill", Shade(Theme.Ink, 0.6f), sliced: true);
            var doneFace = Img(Stretch("Face", done), "Pill", Theme.Ink, sliced: true, raycast: true);
            Txt(Stretch("Label", done), "DONE", 50, Color.white, _black).characterSpacing = 4f;
            var doneButton = done.gameObject.AddComponent<Button>();
            doneButton.targetGraphic = doneFace; doneButton.transition = Selectable.Transition.None;
            done.gameObject.AddComponent<ButtonFeedback>();
            done.gameObject.SetActive(false); // always choosing: the cross closes the garage

            // The cards: all at once, four to a row (the first row filled first).
            var viewport = At("Strip", tray, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1050f, 600f)); // (a margin at the sides)
            viewport.pivot = new Vector2(0.5f, 1f);
            var content = Stretch("Content", viewport);
            content.pivot = new Vector2(0.5f, 1f);
            content.localScale = Vector3.one * 0.86f; // (the grid works in the unscaled size)
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240f, 300f); grid.spacing = new Vector2(26f, 30f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter; grid.padding = new RectOffset(0, 0, 14, 0);
            var divider = At("Divider", content, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(84f, 250f));
            divider.pivot = new Vector2(0.5f, 0.5f);
            Img(divider, "Round", Theme.Ink, sliced: true);
            var dividerName = Txt(At("Name", divider, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(200f, 60f)), "VALLEY CUP", 32, Theme.Amber, _black);
            dividerName.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f); dividerName.characterSpacing = 3f;
            var dividerStars = At("Stars", divider, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(84f, 56f));
            Img(At("Star", dividerStars, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(28f, 28f)), "IconStar", Theme.Amber).rectTransform.pivot = new Vector2(0.5f, 1f);
            Txt(At("Count", dividerStars, new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(90f, 30f)), "6/24", 24, Color.white, _black);

            // A card: the thing on a round face (a big swatch, the car wearing it, or its icon) on a white card, its
            // name under it; the one you have on in an orange frame with a tick on the corner; locked ones grey, the
            // face dimmed, a lock on it and what they take on a pill along the bottom.
            var tile = At("Chip", content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 300f));
            tile.pivot = new Vector2(0.5f, 0.5f);
            var tileHit = Img(tile, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var tileButton = tile.gameObject.AddComponent<Button>();
            tileButton.targetGraphic = tileHit; tileButton.transition = Selectable.Transition.None;
            var equipped = Stretch("Equipped", tile); equipped.offsetMin = new Vector2(-10f, -18f); equipped.offsetMax = new Vector2(10f, 10f);
            Img(equipped, "Round", Theme.Orange, sliced: true);
            var chipShadow = Stretch("Shadow", tile); chipShadow.offsetMin = new Vector2(0f, -8f); chipShadow.offsetMax = new Vector2(0f, -8f);
            Img(chipShadow, "Round", Shade(Theme.CreamDark, 0.75f), sliced: true);
            Img(Stretch("Card", tile), "Round", Color.white, sliced: true);
            var chipFace = At("Face", tile, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(168f, 168f));
            Img(chipFace, "Circle", Theme.Ink);
            var preview = At("Preview", tile, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(216f, 216f));
            preview.pivot = new Vector2(0.5f, 0.5f); preview.anchoredPosition = new Vector2(0f, -112f);
            preview.localScale = Vector3.one * (168f / 216f);
            var swatch = At("Swatch", preview, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(216f, 216f));
            swatch.pivot = new Vector2(0.5f, 0.5f);
            Img(Stretch("Outline", swatch), "Circle", new Color(0.06f, 0.07f, 0.08f));
            Img(At("Color", swatch, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f)), "Circle", Color.white);
            Img(At("Shine", swatch, new Vector2(0.5f, 0.5f), new Vector2(-42f, 46f), new Vector2(56f, 56f)), "Circle", new Color(1f, 1f, 1f, 0.45f));
            // The car wearing the part, straight on the card (big enough to see the part).
            var mini = At("Car", preview, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 245f));
            mini.pivot = new Vector2(0.5f, 0.5f); mini.localRotation = Quaternion.Euler(0f, 0f, -14f);
            var miniBody = Img(Stretch("Body", mini), "Car", Theme.Orange); miniBody.preserveAspect = false;
            var miniDetails = Img(Stretch("Details", mini), "CarDetails", Color.white); miniDetails.preserveAspect = false;
            Configure(mini.gameObject.AddComponent<VehicleIcon>(), ("body", miniBody), ("details", miniDetails));
            var iconRt = At("Icon", preview, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(184f, 184f));
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Pic", iconRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 230f)), "Sparkle", Color.white).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Txt(At("Question", preview, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 210f)), "?", 150, Theme.Amber, _black);
            // A top or a decal on its own (with a soft shadow, so white stickers show on the white card); a dashed
            // ring for nothing.
            var alone = At("Alone", preview, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 230f));
            alone.pivot = new Vector2(0.5f, 0.5f);
            var aloneShadow = Img(At("Shadow", alone, new Vector2(0.5f, 0.5f), new Vector2(6f, -8f), new Vector2(230f, 230f)), "Circle", new Color(0f, 0f, 0f, 0.4f));
            aloneShadow.rectTransform.pivot = new Vector2(0.5f, 0.5f); aloneShadow.rectTransform.anchoredPosition = new Vector2(6f, -8f); aloneShadow.preserveAspect = true;
            var alonePic = Img(At("Pic", alone, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 230f)), "Circle", Color.white);
            alonePic.rectTransform.pivot = new Vector2(0.5f, 0.5f); alonePic.preserveAspect = true;
            var none = Img(At("None", preview, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 170f)), "RingDashed", Theme.WithAlpha(Theme.Ink, 0.3f));
            none.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // The racing stripes (their picture mostly lies under the cockpit): a pair of bars.
            var stripes = At("Stripes", preview, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 210f));
            stripes.pivot = new Vector2(0.5f, 0.5f);
            foreach (float x in new[] { -22f, 22f })
            {
                Img(At("Shadow", stripes, new Vector2(0.5f, 0.5f), new Vector2(x + 4f, -5f), new Vector2(30f, 210f)), "Pill", new Color(0f, 0f, 0f, 0.35f), sliced: true).rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Img(At("Bar", stripes, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(30f, 210f)), "Pill", Color.white, sliced: true).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            var veil = At("Veil", tile, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(168f, 168f));
            Img(veil, "Circle", Theme.WithAlpha(Theme.Ink, 0.4f));
            var tileLock = At("Lock", tile, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(64f, 64f));
            tileLock.pivot = new Vector2(0.5f, 0.5f); tileLock.anchoredPosition = new Vector2(0f, -112f);
            Img(tileLock, "IconLock", Theme.Cream);
            var need = At("Need", tile, new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(200f, 56f));
            Img(need, "Pill", Theme.Ink, sliced: true);
            Img(At("Star", need, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(34f, 34f)), "IconStar", Theme.Amber).rectTransform.pivot = new Vector2(0f, 0.5f);
            Img(At("Face", need, new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(44f, 44f)), "Circle", Color.white).rectTransform.pivot = new Vector2(0f, 0.5f);
            var needText = Txt(At("Text", need, new Vector2(0.5f, 0.5f), new Vector2(16f, 1f), new Vector2(130f, 46f)), "14", 32, Theme.Cream, _black);
            needText.enableAutoSizing = true; needText.fontSizeMin = 16; needText.fontSizeMax = 32;
            var check = At("Check", tile, new Vector2(1f, 1f), new Vector2(-6f, 6f), new Vector2(66f, 66f));
            check.pivot = new Vector2(0.5f, 0.5f); check.anchoredPosition = new Vector2(-6f, 6f);
            Img(check, "Circle", Theme.Orange);
            Img(At("Icon", check, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f)), "IconCheck", Color.white).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var tileName = Txt(At("Name", tile, new Vector2(0.5f, 1f), new Vector2(0f, -204f), new Vector2(228f, 64f)), "RED", 50, Theme.Ink, _condItalic);
            tileName.enableAutoSizing = true; tileName.fontSizeMin = 26; tileName.fontSizeMax = 50;
            tileName.fontStyle = FontStyles.Bold;

            var itemName = Txt(At("ItemName", tray, new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(1060f, 66f)), "SUNSET ORANGE", 52, Theme.Ink, _black);
            var itemBlurb = Txt(At("ItemBlurb", tray, new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(1000f, 46f)), "The classic.", 34, Theme.InkSoft, _semi);
            itemBlurb.enableAutoSizing = true; itemBlurb.fontSizeMin = 22; itemBlurb.fontSizeMax = 34;

            // The rivals' cards: a panel of their own.
            var rivalsHost = Stretch("RivalsPanel", host);
            var rivalsDim = Img(Stretch("Dim", rivalsHost), null, Theme.WithAlpha(Theme.Ink, 0.8f), raycast: true);
            var rivalsDimButton = rivalsDim.gameObject.AddComponent<Button>();
            rivalsDimButton.targetGraphic = rivalsDim; rivalsDimButton.transition = Selectable.Transition.None;
            var rsafe = SafeArea(rivalsHost);
            var rcard = At("Card", rsafe, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 1460f));
            rcard.pivot = new Vector2(0.5f, 0.5f); rcard.anchoredPosition = Vector2.zero;
            var rOutline = Stretch("Outline", rcard); rOutline.offsetMin = new Vector2(-8f, -8f); rOutline.offsetMax = new Vector2(8f, 8f);
            Img(rOutline, "Round", Theme.Amber, sliced: true);
            Img(Stretch("Face", rcard), "Round", Shade(Theme.Ink, 0.85f), sliced: true, raycast: true);
            Txt(At("Title", rcard, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(900f, 100f)), "RIVAL CARDS", 84, Color.white, _black);
            var rivalsCount = Txt(At("Count", rcard, new Vector2(0.5f, 1f), new Vector2(0f, -144f), new Vector2(900f, 50f)), "1 / 7 MET", 38, Theme.Amber, _condItalic);
            rivalsCount.characterSpacing = 6f;
            var rivalsGrid = At("Grid", rcard, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(900f, 1180f));
            var rgrid = rivalsGrid.gameObject.AddComponent<GridLayoutGroup>();
            rgrid.cellSize = new Vector2(236f, 330f); rgrid.spacing = new Vector2(40f, 36f);
            rgrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; rgrid.constraintCount = 3; rgrid.childAlignment = TextAnchor.UpperCenter;
            var rCloseRt = At("Close", rcard, new Vector2(1f, 1f), Vector2.zero, new Vector2(124f, 124f));
            rCloseRt.pivot = new Vector2(0.5f, 0.5f); rCloseRt.anchoredPosition = new Vector2(-32f, -32f);
            Img(rCloseRt, "Circle", new Color(0.06f, 0.07f, 0.08f));
            var rCloseFace = Img(At("Face", rCloseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(108f, 108f)), "Circle", Theme.Cream, raycast: true);
            Img(At("Icon", rCloseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f)), "IconClose", Theme.Ink);
            var rClose = rCloseRt.gameObject.AddComponent<Button>();
            rClose.targetGraphic = rCloseFace; rClose.transition = Selectable.Transition.None;
            Configure(rivalsHost.gameObject.AddComponent<PopIn>(), ("card", rcard), ("backdrop", rivalsDim));

            // A rival's card on the shelf.
            var card = At("Card", rivalsGrid, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(236f, 330f));
            var cardHit = Img(card, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = cardHit; cardButton.transition = Selectable.Transition.None;
            card.gameObject.AddComponent<ButtonFeedback>();
            var cardShadow = Stretch("Shadow", card); cardShadow.offsetMin = new Vector2(0f, -8f); cardShadow.offsetMax = new Vector2(0f, -8f);
            Img(cardShadow, "Round", Shade(Theme.Ink, 0.5f), sliced: true);
            Img(Stretch("Face", card), "Round", Theme.Ink, sliced: true);
            var cardInner = Stretch("Border", card); cardInner.offsetMin = new Vector2(10f, 10f); cardInner.offsetMax = new Vector2(-10f, -10f);
            Img(cardInner, "Round", Theme.WithAlpha(Theme.Amber, 0.85f), sliced: true);
            var cardFill = Stretch("Fill", card); cardFill.offsetMin = new Vector2(16f, 16f); cardFill.offsetMax = new Vector2(-16f, -16f);
            Img(cardFill, "Round", Shade(Theme.Ink, 0.8f), sliced: true);
            Img(At("Picture", card, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(190f, 190f)), "Circle", Color.white);
            Txt(At("Question", card, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(190f, 170f)), "?", 130, Theme.Amber, _black);
            var cardName = Txt(At("Name", card, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(214f, 50f)), "ROSIE RUST", 32, Color.white, _black);
            cardName.enableAutoSizing = true; cardName.fontSizeMin = 18; cardName.fontSizeMax = 32;
            var cardHome = Txt(At("Home", card, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(214f, 38f)), "MEADOW RUN", 24, Theme.Amber, _condItalic);
            cardHome.enableAutoSizing = true; cardHome.fontSizeMin = 14; cardHome.fontSizeMax = 24;
            var beaten = At("Beaten", card, new Vector2(1f, 1f), new Vector2(14f, 6f), new Vector2(140f, 46f));
            beaten.pivot = new Vector2(1f, 1f); beaten.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Img(beaten, "Pill", Theme.Leaf, sliced: true);
            Txt(Stretch("Label", beaten), "BEATEN", 26, Color.white, _black);

            // A rival's whole card (tapped on the shelf).
            var detail = Stretch("RivalCard", host);
            var dim = Img(Stretch("Dim", detail), null, Theme.WithAlpha(Theme.Ink, 0.8f), raycast: true);
            var dsafe = SafeArea(detail);
            var big = At("Card", dsafe, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1840f));
            big.pivot = new Vector2(0.5f, 0.5f); big.anchoredPosition = Vector2.zero;
            var bigShadow = Stretch("Shadow", big); bigShadow.offsetMin = new Vector2(-8f, -32f); bigShadow.offsetMax = new Vector2(8f, -16f);
            Img(bigShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            var bigOutline = Stretch("Outline", big); bigOutline.offsetMin = new Vector2(-10f, -10f); bigOutline.offsetMax = new Vector2(10f, 10f);
            Img(bigOutline, "Round", Theme.Amber, sliced: true);
            Img(Stretch("Face", big), "Round", Theme.Cream, sliced: true, raycast: true);
            var scene = At("Scene", big, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 1000f));
            Img(scene, "Round", Hex("BFE3F2"), sliced: true);
            scene.gameObject.AddComponent<RectMask2D>();
            Img(At("Hill", scene, new Vector2(0.5f, 0f), new Vector2(-120f, -380f), new Vector2(1500f, 700f)), "Circle", Hex("9CC383")).preserveAspect = false;
            Img(At("Hill2", scene, new Vector2(0.5f, 0f), new Vector2(300f, -440f), new Vector2(1300f, 640f)), "Circle", Hex("7FB06C")).preserveAspect = false;
            var painting = Img(At("Painting", scene, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(860f, 1000f)), "Circle", Color.white);
            painting.rectTransform.pivot = new Vector2(0.5f, 0f);
            var dName = Txt(At("Name", big, new Vector2(0.5f, 1f), new Vector2(0f, -1050f), new Vector2(900f, 110f)), "ROSIE RUST", 96, Theme.Ink, _black);
            var dTitle = Txt(At("Title", big, new Vector2(0.5f, 1f), new Vector2(0f, -1158f), new Vector2(900f, 50f)), "THE GRUMPY FARMER", 38, Theme.Rust, _condItalic);
            dTitle.characterSpacing = 6f;
            var dBio = Txt(At("Bio", big, new Vector2(0.5f, 1f), new Vector2(0f, -1226f), new Vector2(840f, 130f)), "Bio", 36, Theme.InkSoft, _semi);
            dBio.textWrappingMode = TextWrappingModes.Normal; dBio.alignment = TextAlignmentOptions.Top;
            var homeRow = At("HomeRow", big, new Vector2(0.5f, 1f), new Vector2(0f, -1380f), new Vector2(860f, 90f));
            Img(homeRow, "Round", Theme.CreamDark, sliced: true);
            Txt(At("Label", homeRow, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(400f, 60f)), "HOME RACE", 32, Theme.InkSoft, _condItalic, TextAlignmentOptions.Left).rectTransform.pivot = new Vector2(0f, 0.5f);
            var dHome = Txt(At("Value", homeRow, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(460f, 60f)), "MEADOW RUN", 40, Theme.Ink, _black, TextAlignmentOptions.Right);
            dHome.rectTransform.pivot = new Vector2(1f, 0.5f);
            var gadgetRow = At("GadgetRow", big, new Vector2(0.5f, 1f), new Vector2(0f, -1490f), new Vector2(860f, 220f));
            Img(gadgetRow, "Round", Theme.Ink, sliced: true);
            var gIcon = At("Icon", gadgetRow, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(170f, 170f));
            gIcon.pivot = new Vector2(0f, 0.5f);
            Img(gIcon, "Circle", Shade(Theme.Ink, 0.7f));
            var dGadgetIcon = Img(At("Pic", gIcon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f)), "HayBale", Color.white);
            dGadgetIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Txt(At("Caption", gadgetRow, new Vector2(0f, 1f), new Vector2(222f, -30f), new Vector2(600f, 44f)), "HER GADGET", 30, Theme.Amber, _condItalic, TextAlignmentOptions.Left).rectTransform.pivot = new Vector2(0f, 1f);
            var dGadget = Txt(At("Name", gadgetRow, new Vector2(0f, 1f), new Vector2(222f, -74f), new Vector2(620f, 70f)), "HAY BALE DROP", 54, Color.white, _black, TextAlignmentOptions.Left);
            dGadget.rectTransform.pivot = new Vector2(0f, 1f);
            var dStatus = Txt(At("Status", gadgetRow, new Vector2(0f, 1f), new Vector2(222f, -150f), new Vector2(620f, 44f)), "BEAT ROSIE TO WIN IT", 32, Theme.Amber, _condItalic, TextAlignmentOptions.Left);
            dStatus.rectTransform.pivot = new Vector2(0f, 1f);
            var dCloseRt = At("Close", big, new Vector2(1f, 1f), Vector2.zero, new Vector2(124f, 124f));
            dCloseRt.pivot = new Vector2(0.5f, 0.5f); dCloseRt.anchoredPosition = new Vector2(-32f, -32f);
            Img(dCloseRt, "Circle", new Color(0.06f, 0.07f, 0.08f));
            var dCloseFace = Img(At("Face", dCloseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(108f, 108f)), "Circle", Theme.Cream, raycast: true);
            Img(At("Icon", dCloseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f)), "IconClose", Theme.Ink);
            var dClose = dCloseRt.gameObject.AddComponent<Button>();
            dClose.targetGraphic = dCloseFace; dClose.transition = Selectable.Transition.None;
            var dimClose = dim.gameObject.AddComponent<Button>();
            dimClose.targetGraphic = dim; dimClose.transition = Selectable.Transition.None;
            Configure(detail.gameObject.AddComponent<PopIn>(), ("card", big), ("backdrop", dim));

            var screen = host.gameObject.AddComponent<GarageScreen>();
            Configure(screen, ("panel", panel.gameObject), ("group", group), ("sheet", sheet), ("closeButton", close), ("rivalsButton", rivals), ("starsLabel", stars),
                ("vehicleBar", vehicleBar), ("vehicleBadge", vehicleBadge), ("vehicleRing", vehicleRing), ("peekLeft", peekLeft), ("peekRight", peekRight), ("outgoing", outgoing), ("outgoingCar", outgoingCar),
                ("carRoot", carRoot), ("car", vehicle), ("podium", podium), ("podiumRing", podiumRing), ("burst", burst), ("highlight", highlight),
                ("carButton", carButton), ("sparkle", Sprite("Sparkle")), ("hotspotTemplate", spot), ("hint", hint), ("buildName", buildName),
                ("tray", tray), ("stripContent", content), ("tileTemplate", tile), ("dividerTemplate", divider),
                ("tabTemplate", tabT), ("trayCount", trayCount), ("itemName", itemName), ("itemBlurb", itemBlurb), ("doneButton", doneButton),
                ("rivalsPanel", rivalsHost.gameObject), ("rivalsGrid", rivalsGrid), ("cardTemplate", card), ("rivalsClose", rClose), ("rivalsCount", rivalsCount),
                ("detail", detail.gameObject), ("detailClose", dClose), ("detailPainting", painting), ("detailName", dName), ("detailTitle", dTitle),
                ("detailBio", dBio), ("detailHome", dHome), ("detailGadget", dGadget), ("detailStatus", dStatus), ("detailGadgetIcon", dGadgetIcon));
            var gso = new SerializedObject(screen);
            var slotIcons = new[] { "IconPaint", "IconBody", "IconWing", "IconDecal", "IconTrail", "IconGadget" };
            var slotSprites = new Sprite[slotIcons.Length];
            for (int i = 0; i < slotIcons.Length; i++) slotSprites[i] = Sprite(slotIcons[i]);
            SetArray(gso, "slotIcons", slotSprites);
            gso.ApplyModifiedPropertiesWithoutUndo();
            detail.gameObject.SetActive(false);
            rivalsHost.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            return screen;
        }

        // Meeting a rival (RivalMeeting): a sunny meadow, the rival by their car, their name banner, a speech box,
        // the prize card (their gadget) and BRING IT ON!, SKIP in the corner.
        static RivalMeeting BuildRivalMeeting(RectTransform canvas)
        {
            var host = Stretch("RivalMeeting", canvas);
            var panel = Stretch("Panel", host);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            Img(Stretch("Sky", panel), null, Hex("A9D8F0"), raycast: true);
            var sky = Img(At("SkyGlow", panel, new Vector2(0.5f, 1f), new Vector2(0f, 600f), new Vector2(2400f, 2000f)), "Glow", Theme.WithAlpha(Color.white, 0.55f));
            sky.preserveAspect = false; sky.rectTransform.pivot = new Vector2(0.5f, 1f);
            Img(At("Sun", panel, new Vector2(1f, 1f), new Vector2(-120f, -260f), new Vector2(700f, 700f)), "Glow", Theme.WithAlpha(Hex("FFE58A"), 0.9f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Img(At("HillBack", panel, new Vector2(0.5f, 0f), new Vector2(-380f, -760f), new Vector2(2600f, 1700f)), "Circle", Hex("9CC383")).preserveAspect = false;
            Img(At("HillFront", panel, new Vector2(0.5f, 0f), new Vector2(420f, -1000f), new Vector2(2800f, 1700f)), "Circle", Hex("7FB06C")).preserveAspect = false;
            var tapImage = Img(Stretch("Tap", panel), null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var tap = tapImage.gameObject.AddComponent<Button>();
            tap.targetGraphic = tapImage; tap.transition = Selectable.Transition.None;
            var safe = SafeArea(panel);

            var painting = At("Painting", safe, new Vector2(0.5f, 0f), new Vector2(40f, 520f), new Vector2(1080f, 1260f));
            var paintingImage = Img(painting, "Circle", Color.white);

            // The prize.
            var prize = At("Prize", safe, new Vector2(1f, 0f), new Vector2(-215f, 1330f), new Vector2(330f, 420f));
            prize.pivot = new Vector2(0.5f, 0.5f);
            var prizeShadow = Stretch("Shadow", prize); prizeShadow.offsetMin = new Vector2(0f, -14f); prizeShadow.offsetMax = new Vector2(0f, -14f);
            Img(prizeShadow, "Round", Theme.WithAlpha(Theme.Ink, 0.5f), sliced: true);
            var prizeOutline = Stretch("Outline", prize); prizeOutline.offsetMin = new Vector2(-8f, -8f); prizeOutline.offsetMax = new Vector2(8f, 8f);
            Img(prizeOutline, "Round", Theme.Amber, sliced: true);
            Img(Stretch("Face", prize), "Round", Theme.Ink, sliced: true);
            Txt(At("Caption", prize, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(300f, 44f)), "BEAT ME TO WIN", 30, Theme.Amber, _condItalic).characterSpacing = 3f;
            var prizeDisc = At("Disc", prize, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(210f, 210f));
            prizeDisc.pivot = new Vector2(0.5f, 0.5f);
            Img(prizeDisc, "Circle", Shade(Theme.Ink, 0.7f));
            var prizeIcon = Img(At("Icon", prizeDisc, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 160f)), "HayBale", Color.white);
            prizeIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var prizeName = Txt(At("Name", prize, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(300f, 80f)), "HAY BALE DROP", 40, Color.white, _black);
            prizeName.textWrappingMode = TextWrappingModes.Normal; prizeName.enableAutoSizing = true; prizeName.fontSizeMin = 24; prizeName.fontSizeMax = 40;

            // The speech box with its name banner.
            var box = At("Box", safe, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1090f, 500f));
            var boxShadow = Stretch("Shadow", box); boxShadow.offsetMin = new Vector2(0f, -16f); boxShadow.offsetMax = new Vector2(0f, -16f);
            Img(boxShadow, "Round", Theme.WithAlpha(Theme.Ink, 0.45f), sliced: true);
            var boxOutline = Stretch("Outline", box); boxOutline.offsetMin = new Vector2(-8f, -8f); boxOutline.offsetMax = new Vector2(8f, 8f);
            Img(boxOutline, "Round", new Color(0.06f, 0.07f, 0.08f), sliced: true);
            Img(Stretch("Face", box), "Round", Theme.Cream, sliced: true);
            var line = Txt(Stretch("Line", box), "Well, well. A new face on MY meadow?", 54, Theme.Ink, _bold, TextAlignmentOptions.TopLeft);
            line.textWrappingMode = TextWrappingModes.Normal;
            line.margin = new Vector4(56f, 92f, 300f, 40f);
            var next = Img(At("Next", box, new Vector2(1f, 0f), new Vector2(-60f, 50f), new Vector2(54f, 50f)), "NavArrow", Theme.Orange).rectTransform;
            next.pivot = new Vector2(0.5f, 0.5f); next.localRotation = Quaternion.Euler(0f, 0f, 180f);
            var go = At("Go", box, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(470f, 130f));
            go.pivot = new Vector2(1f, 0f);
            Img(At("Shadow", go, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(470f, 130f)), "Pill", Theme.OrangeDeep, sliced: true);
            var goFace = Img(Stretch("Face", go), "Pill", Theme.Orange, sliced: true, raycast: true);
            Txt(Stretch("Label", go), "BRING IT ON!", 54, Color.white, _black);
            var goButton = go.gameObject.AddComponent<Button>();
            goButton.targetGraphic = goFace; goButton.transition = Selectable.Transition.None;
            go.gameObject.AddComponent<ButtonFeedback>();
            // Their face on the box's corner, in the mood of each line.
            var meetFace = Img(At("Face", safe, new Vector2(1f, 0f), new Vector2(-40f, 440f), new Vector2(330f, 330f)), "Circle", Color.white);
            meetFace.rectTransform.pivot = new Vector2(1f, 0f);
            var banner = At("Banner", safe, new Vector2(0f, 0f), new Vector2(70f, 550f), new Vector2(600f, 140f));
            banner.pivot = new Vector2(0f, 0.5f); banner.localRotation = Quaternion.Euler(0f, 0f, 3f);
            Img(At("Shadow", banner, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(600f, 140f)), "Pill", Theme.OrangeDeep, sliced: true);
            Img(Stretch("Face", banner), "Pill", Theme.Orange, sliced: true);
            var nameLabel = Txt(At("Name", banner, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(560f, 76f)), "ROSIE RUST", 64, Color.white, _black);
            nameLabel.enableAutoSizing = true; nameLabel.fontSizeMin = 36; nameLabel.fontSizeMax = 64;
            var titleLabel = Txt(At("Title", banner, new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(560f, 40f)), "THE GRUMPY FARMER", 30, Theme.Cream, _condItalic);
            titleLabel.characterSpacing = 5f;

            var skip = At("Skip", safe, new Vector2(1f, 1f), new Vector2(-40f, -44f), new Vector2(210f, 92f));
            skip.pivot = new Vector2(1f, 1f);
            var skipFace = Img(skip, "Pill", Theme.WithAlpha(Theme.Ink, 0.55f), sliced: true, raycast: true);
            Txt(Stretch("Label", skip), "SKIP", 42, Color.white, _black);
            var skipButton = skip.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipFace; skipButton.transition = Selectable.Transition.None;

            var meeting = host.gameObject.AddComponent<RivalMeeting>();
            Configure(meeting, ("panel", panel.gameObject), ("group", group), ("tapArea", tap), ("skipButton", skipButton), ("goButton", goButton),
                ("painting", painting), ("paintingImage", paintingImage), ("banner", banner), ("box", box), ("next", next), ("prize", prize),
                ("nameLabel", nameLabel), ("titleLabel", titleLabel), ("lineLabel", line), ("prizeName", prizeName), ("prizeIcon", prizeIcon), ("face", meetFace));
            panel.gameObject.SetActive(false);
            return meeting;
        }

        // The races as a deck of cards over the map (RaceDeck): a dim backdrop, the cards in the middle (a template
        // card copied for each race), the cross, and the bar of race numbers along the bottom.
        static RaceDeck BuildRaceDeck(RectTransform canvas)
        {
            var host = Stretch("RaceDeck", canvas);
            var panel = Stretch("Panel", host);
            var backdrop = Img(Stretch("Backdrop", panel), null, Theme.WithAlpha(Theme.Ink, 0.72f), raycast: true);
            var safe = SafeArea(panel);
            var stage = Stretch("Stage", safe);

            // ---- the card
            const float W = 820f, H = 1700f;
            var card = At("Card", stage, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(W, H));
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0f, 60f);
            var group = card.gameObject.AddComponent<CanvasGroup>();
            var glow = At("Glow", card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W + 340f, H + 340f));
            glow.pivot = new Vector2(0.5f, 0.5f); glow.anchoredPosition = Vector2.zero;
            Img(glow, "Glow", Theme.WithAlpha(Theme.Amber, 0.3f)).preserveAspect = false;
            var cardShadow = Stretch("Shadow", card);
            cardShadow.offsetMin = new Vector2(-8f, -32f); cardShadow.offsetMax = new Vector2(8f, -16f);
            Img(cardShadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            // A crisp black outline round the card.
            var outline = Stretch("Outline", card);
            outline.offsetMin = new Vector2(-8f, -8f); outline.offsetMax = new Vector2(8f, 8f);
            Img(outline, "Round", new Color(0.06f, 0.07f, 0.08f), sliced: true);
            var face = Img(Stretch("Face", card), "Round", Theme.Cream, sliced: true, raycast: true);
            var focus = face.gameObject.AddComponent<Button>();
            focus.targetGraphic = face; focus.transition = Selectable.Transition.None;
            // The race number on a medallion over the top left corner.
            var medal = At("Medal", card, new Vector2(0f, 1f), new Vector2(-26f, 34f), new Vector2(170f, 182f));
            Img(At("Shadow", medal, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.OrangeDeep);
            // Frost over the parts sticking out past the card (like the card's own veil, for a card at the side).
            var medalShadowVeil = Img(At("ShadowVeil", medal, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.WithAlpha(Theme.Cream, 0f));
            Img(At("Face", medal, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.Orange);
            Img(At("Rim", medal, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(146f, 146f)), "Ring", Theme.WithAlpha(Color.white, 0.55f));
            var number = Txt(At("Number", medal, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(170f, 130f)), "1", 100, Color.white, _black);
            var medalVeil = Img(At("Veil", medal, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.WithAlpha(Theme.Cream, 0f));
            // The cross on the top right corner closes the deck.
            var closeRt = At("Close", card, new Vector2(1f, 1f), new Vector2(30f, 30f), new Vector2(124f, 124f));
            closeRt.pivot = new Vector2(0.5f, 0.5f); closeRt.anchoredPosition = new Vector2(-32f, -32f);
            Img(closeRt, "Circle", new Color(0.06f, 0.07f, 0.08f));
            var closeFace = Img(At("Face", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(108f, 108f)), "Circle", Theme.Cream, raycast: true);
            Img(At("Icon", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f)), "IconClose", Theme.Ink);
            var closeVeil = Img(At("Veil", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124f, 124f)), "Circle", Theme.WithAlpha(Theme.Cream, 0f));
            var close = closeRt.gameObject.AddComponent<Button>();
            close.targetGraphic = closeFace; close.transition = Selectable.Transition.None;

            // Cup and race number over the name, both centred between the medallion and the cross.
            var chapter = Txt(At("Chapter", card, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(480f, 46f)), "VALLEY CUP · RACE 1", 36, Theme.Rust, _condItalic);
            chapter.characterSpacing = 6f;
            var title = Txt(At("Title", card, new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(560f, 120f)), "SUNDAY LOOP", 100, Theme.Ink, _black);
            title.enableAutoSizing = true; title.fontSizeMin = 56; title.fontSizeMax = 100;
            var records = Txt(At("Records", card, new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(W - 100f, 50f)), "NO TIME YET", 36, Theme.Rust, _cond);
            records.characterSpacing = 4f;

            // The track from above in a dark frame sized to the picture's shape; its blurred copy shows off-centre,
            // a band of light sweeps over it, and a lock covers a race still locked.
            var frame = At("Track", card, new Vector2(0.5f, 1f), new Vector2(0f, -756f), new Vector2(W - 80f, 900f));
            frame.pivot = new Vector2(0.5f, 0.5f); frame.anchoredPosition = new Vector2(0f, -756f);
            Img(frame, "Round", Theme.Ink, sliced: true);
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true; // clips the light band (also on a tilted card)
            var picture = Img(Stretch("Picture", frame), null, Color.white);
            picture.rectTransform.offsetMin = new Vector2(12f, 12f); picture.rectTransform.offsetMax = new Vector2(-12f, -12f);
            picture.preserveAspect = true;
            var blurred = Stretch("Blurred", frame).gameObject.AddComponent<RawImage>();
            blurred.rectTransform.offsetMin = new Vector2(12f, 12f); blurred.rectTransform.offsetMax = new Vector2(-12f, -12f);
            blurred.raycastTarget = false;
            var shine = At("Shine", frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 2400f));
            shine.pivot = new Vector2(0.5f, 0.5f); shine.localRotation = Quaternion.Euler(0f, 0f, -24f);
            Img(shine, null, new Color(1f, 1f, 1f, 0.2f));
            // A locked race: no track yet, a grey plate (like a locked cup's page) with a lock and the stars it needs.
            var lockCover = Stretch("Lock", frame);
            lockCover.offsetMin = new Vector2(12f, 12f); lockCover.offsetMax = new Vector2(-12f, -12f);
            Img(lockCover, "Round", Theme.Stone, sliced: true);
            var lockPattern = Img(Stretch("Pattern", lockCover), "Checker", Theme.WithAlpha(Theme.StoneDark, 0.08f));
            lockPattern.type = Image.Type.Tiled; lockPattern.preserveAspect = false; lockPattern.pixelsPerUnitMultiplier = 0.6f;
            Img(At("Ring", lockCover, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(260f, 260f)), "Circle", Theme.WithAlpha(Theme.StoneDark, 0.18f));
            Img(At("Icon", lockCover, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(150f, 150f)), "IconLock", Theme.StoneDark);
            var lockLabel = Txt(At("Label", lockCover, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(700f, 160f)), "0 / 2\nSTARS TO UNLOCK", 46, Theme.StoneDark, _condItalic);
            lockLabel.textWrappingMode = TextWrappingModes.Normal;
            Configure(frame.gameObject.AddComponent<PreviewFrame>(), ("picture", picture), ("maxSize", new Vector2(W - 80f, 900f)), ("inset", 12f));

            // Difficulty: a speedometer and five bars, centred under the picture.
            // The five bars centred on the card, the speedometer just left of them.
            const float PipsWidth = 5f * 64f + 4f * 14f, PipsLeft = (W - PipsWidth) * 0.5f;
            Img(At("DifficultyIcon", card, new Vector2(0f, 1f), new Vector2(PipsLeft - 78f, -1222f), new Vector2(60f, 60f)), "IconGauge", Theme.Rust);
            var pips = new Image[5];
            for (int i = 0; i < pips.Length; i++)
                pips[i] = Rounded(Img(At($"Pip{i + 1}", card, new Vector2(0f, 1f), new Vector2(PipsLeft + i * 78f, -1238f), new Vector2(64f, 28f)), "Round", Theme.Orange, sliced: true), 8f);

            // The places that earn stars as a track to climb: a bar filling from the left through three round badges
            // (one, two, three stars) with the place under each; earned ones gold, the next one ringed.
            var timesRt = At("StarTimes", card, new Vector2(0.5f, 1f), new Vector2(0f, -1286f), new Vector2(W - 80f, 140f));
            const float NodeGap = 250f, TrackLeft = -310f, TrackRight = 250f;
            var trackRt = At("Track", timesRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TrackRight - TrackLeft, 24f));
            trackRt.pivot = new Vector2(0f, 0.5f); trackRt.anchoredPosition = new Vector2(TrackLeft, 22f);
            Rounded(Img(trackRt, "Round", Theme.CreamDark, sliced: true), 12f);
            var fillRt = At("Fill", trackRt, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 24f));
            fillRt.pivot = new Vector2(0f, 0.5f);
            Rounded(Img(fillRt, "Round", Theme.Amber, sliced: true), 12f);
            var nextRt = At("Next", timesRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));
            nextRt.pivot = new Vector2(0.5f, 0.5f);
            Img(nextRt, "Ring", Theme.Orange);
            var times = new TMP_Text[3];
            var groups = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var g = At($"Star{i + 1}", timesRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 140f));
                g.pivot = new Vector2(0.5f, 0.5f);
                g.anchoredPosition = new Vector2((i - 1) * NodeGap, 0f);
                groups[i] = g;
                var rim = At("Rim", g, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(108f, 108f));
                rim.pivot = new Vector2(0.5f, 0.5f); rim.anchoredPosition = new Vector2(0f, 22f);
                Img(rim, "Circle", Theme.OrangeDeep);
                var badgeFace = At("Face", g, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
                badgeFace.pivot = new Vector2(0.5f, 0.5f); badgeFace.anchoredPosition = new Vector2(0f, 22f);
                Img(badgeFace, "Circle", Theme.CreamDark);
                (Vector2 at, float size)[] layout = i == 0 ? new[] { (new Vector2(0f, 22f), 58f) }
                    : i == 1 ? new[] { (new Vector2(-17f, 22f), 42f), (new Vector2(17f, 22f), 42f) }
                    : new[] { (new Vector2(-24f, 14f), 34f), (new Vector2(0f, 32f), 38f), (new Vector2(24f, 14f), 34f) };
                for (int k = 0; k < layout.Length; k++)
                {
                    var star = At($"S{k}", g, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * layout[k].size);
                    star.pivot = new Vector2(0.5f, 0.5f); star.anchoredPosition = layout[k].at;
                    Img(star, "IconStar", Theme.Amber);
                }
                times[i] = Txt(At("Time", g, new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(200f, 56f)), "1ST", 42, Theme.Ink, _cond);
            }
            // START along the bottom, with a ring breathing out of it.
            var startRt = At("StartButton", card, new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(600f, 178f));
            var ring = At("Ring", startRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 7f), new Vector2(600f, 164f));
            ring.pivot = new Vector2(0.5f, 0.5f); ring.anchoredPosition = new Vector2(0f, 7f);
            var ringImage = Img(ring, "Round", Theme.Orange, sliced: true);
            ringImage.fillCenter = false; ringImage.pixelsPerUnitMultiplier = 3f;
            var startShadowRt = Stretch("Shadow", startRt);
            startShadowRt.offsetMax = new Vector2(0f, -14f);
            var startShadow = Img(startShadowRt, "Round", Theme.OrangeDeep, sliced: true);
            var startFaceRt = Stretch("Face", startRt);
            startFaceRt.offsetMin = new Vector2(0f, 14f);
            var startFace = Img(startFaceRt, "Round", Theme.Orange, sliced: true, raycast: true);
            var startLabel = Txt(Stretch("Label", startFaceRt), "START", 80, Color.white, _black);
            startLabel.characterSpacing = 6f;
            var start = startRt.gameObject.AddComponent<Button>();
            start.targetGraphic = startFace; start.transition = Selectable.Transition.None;

            // The race's rival leaning in over the bottom left of the picture: their face (half over the card's edge),
            // a speech bubble and their gadget as the prize.
            var rivalRt = At("Rival", card, new Vector2(0f, 1f), new Vector2(60f, -1060f), new Vector2(10f, 10f));
            rivalRt.pivot = new Vector2(0.5f, 0.5f);
            var rivalGroup = rivalRt.gameObject.AddComponent<CanvasGroup>();
            rivalGroup.blocksRaycasts = true; // the face can be tapped (to show the bubble again)
            var bubble = At("Bubble", rivalRt, new Vector2(0.5f, 0.5f), new Vector2(110f, 64f), new Vector2(560f, 130f));
            bubble.pivot = new Vector2(0f, 0.5f);
            var bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();
            bubbleGroup.blocksRaycasts = false;
            var bubbleOutline = Stretch("Outline", bubble); bubbleOutline.offsetMin = new Vector2(-7f, -7f); bubbleOutline.offsetMax = new Vector2(7f, 7f);
            Img(bubbleOutline, "Round", new Color(0.06f, 0.07f, 0.08f), sliced: true);
            Img(Stretch("Face", bubble), "Round", Color.white, sliced: true);
            var rivalLine = Txt(Stretch("Line", bubble), "My meadow, my rules!", 40, Theme.Ink, _bold);
            rivalLine.textWrappingMode = TextWrappingModes.Normal; rivalLine.margin = new Vector4(56f, 10f, 24f, 10f);
            rivalLine.enableAutoSizing = true; rivalLine.fontSizeMin = 24; rivalLine.fontSizeMax = 40;
            var prizeChip = At("Prize", rivalRt, new Vector2(0.5f, 0.5f), new Vector2(130f, -60f), new Vector2(470f, 70f));
            prizeChip.pivot = new Vector2(0f, 0.5f);
            var prizeGroup = prizeChip.gameObject.AddComponent<CanvasGroup>();
            prizeGroup.blocksRaycasts = false;
            Img(prizeChip, "Pill", Theme.Ink, sliced: true);
            var rivalPrize = Txt(Stretch("Label", prizeChip), "WIN  HAY BALE DROP", 32, Theme.Amber, _black);
            rivalPrize.margin = new Vector4(40f, 0f, 20f, 0f);
            Img(At("Shadow", rivalRt, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(250f, 250f)), "Circle", Theme.WithAlpha(Theme.Ink, 0.5f));
            Img(At("Ring", rivalRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f)), "Circle", new Color(0.06f, 0.07f, 0.08f));
            var rivalPicture = Img(At("Picture", rivalRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(234f, 234f)), "Circle", Color.white, raycast: true);
            var rivalFaceButton = rivalPicture.gameObject.AddComponent<Button>();
            rivalFaceButton.targetGraphic = rivalPicture; rivalFaceButton.transition = Selectable.Transition.None;
            rivalRt.gameObject.SetActive(false);

            // The frost over a card at the side of the deck.
            // It covers the black outline too; what sticks out past the card (medallion, cross) lies above it
            // with frost of its own, so the two never overlap.
            var veilRt = Stretch("Veil", card);
            veilRt.offsetMin = new Vector2(-8f, -8f); veilRt.offsetMax = new Vector2(8f, 8f);
            var veil = Img(veilRt, "Round", Theme.WithAlpha(Theme.Cream, 0f), sliced: true);
            medal.SetAsLastSibling(); closeRt.SetAsLastSibling();

            var raceCard = card.gameObject.AddComponent<RaceCard>();
            Configure(raceCard, ("group", group), ("focusButton", focus), ("veil", veil), ("glow", glow),
                ("numberLabel", number), ("chapterLabel", chapter), ("titleLabel", title), ("recordsLabel", records),
                ("picture", picture), ("blurredPicture", blurred), ("shine", shine), ("lockCover", lockCover.gameObject), ("lockLabel", lockLabel),
                ("closeButton", close),
                ("startButton", start), ("startFace", startFace), ("startShadow", startShadow), ("startLabel", startLabel), ("startRing", ring),
                ("rival", rivalGroup), ("rivalPicture", rivalPicture), ("rivalLine", rivalLine), ("rivalPrize", rivalPrize),
                ("rivalBubble", bubbleGroup), ("rivalPrizeGroup", prizeGroup), ("rivalFace", rivalFaceButton),
                ("starFill", fillRt), ("starNext", nextRt), ("sparkle", Sprite("Sparkle")));
            var cso = new SerializedObject(raceCard);
            SetArray(cso, "difficultyPips", pips); SetArray(cso, "starGroups", groups); SetArray(cso, "starTimes", times);
            SetArray(cso, "edgeVeils", new[] { medalShadowVeil, medalVeil, closeVeil });
            cso.ApplyModifiedPropertiesWithoutUndo();

            // ---- the bar of race numbers
            var strip = At("RaceBar", safe, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1100f, 172f));
            Img(strip, "Round", Theme.Ink, sliced: true, raycast: true);
            var raceBadge = At("Race", strip, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(118f, 118f));
            raceBadge.pivot = new Vector2(0.5f, 0.5f);
            var raceFace = Img(At("Face", raceBadge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(118f, 118f)), "Circle", Theme.Cream, raycast: true);
            Txt(At("Number", raceBadge, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(118f, 118f)), "1", 64, Theme.Ink, _black);
            Img(At("Lock", raceBadge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f)), "IconLock", Theme.StoneDark);
            var raceButton = raceBadge.gameObject.AddComponent<Button>();
            raceButton.targetGraphic = raceFace; raceButton.transition = Selectable.Transition.None;
            raceBadge.gameObject.AddComponent<ButtonFeedback>();
            var stripRing = At("Ring", strip, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            stripRing.pivot = new Vector2(0.5f, 0.5f);
            Img(stripRing, "Ring", Theme.Amber);

            var challenge = BuildDeckChallengeCard(stage, W, H);

            var deck = host.gameObject.AddComponent<RaceDeck>();
            Configure(deck, ("panel", panel.gameObject), ("backdrop", backdrop), ("cardTemplate", raceCard), ("sparkle", Sprite("Sparkle")), ("flashRing", Sprite("Ring")),
                ("strip", strip), ("stripBadge", raceBadge), ("stripRing", stripRing), ("challengeCard", challenge));
            panel.gameObject.SetActive(false);
            return deck;
        }

        // The cup's challenge as a card in the deck (DeckChallengeCard), tucked behind its rival's race card: the
        // CHALLENGE ribbon, its picture, name and what to do, your best, the prize on your car, PLAY; a frost while it
        // lies behind, and the CHALLENGE tab on its edge.
        static DeckChallengeCard BuildDeckChallengeCard(RectTransform stage, float W, float H)
        {
            var card = At("ChallengeCard", stage, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(W, H));
            card.pivot = new Vector2(0.5f, 0.5f); card.anchoredPosition = new Vector2(0f, 60f);
            var group = card.gameObject.AddComponent<CanvasGroup>();
            var shadow = Stretch("Shadow", card); shadow.offsetMin = new Vector2(-8f, -32f); shadow.offsetMax = new Vector2(8f, -16f);
            Img(shadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            var outline = Stretch("Outline", card); outline.offsetMin = new Vector2(-8f, -8f); outline.offsetMax = new Vector2(8f, 8f);
            Img(outline, "Round", new Color(0.06f, 0.07f, 0.08f), sliced: true);
            var bodyImage = Img(Stretch("Face", card), "Round", Theme.Cream, sliced: true, raycast: true);
            var body = card.gameObject.AddComponent<Button>();
            body.targetGraphic = bodyImage; body.transition = Selectable.Transition.None;

            // The cup and CHALLENGE, the name, your best (like a race card's header).
            var chapter = Txt(At("Chapter", card, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(480f, 46f)), "VALLEY CUP · CHALLENGE", 36, Theme.Rust, _condItalic);
            chapter.characterSpacing = 6f;
            var title = Txt(At("Title", card, new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(560f, 120f)), "CHICKEN CHASE", 100, Theme.Ink, _black);
            title.enableAutoSizing = true; title.fontSizeMin = 56; title.fontSizeMax = 100;
            var best = Txt(At("Best", card, new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(W - 100f, 50f)), "NOT PLAYED YET", 36, Theme.Rust, _cond);
            best.characterSpacing = 4f;

            // The track in a frame, the runaway chickens pecking about on it; a lock over it while locked.
            var frame = At("Track", card, new Vector2(0.5f, 1f), new Vector2(0f, -695f), new Vector2(W - 80f, 770f));
            frame.pivot = new Vector2(0.5f, 0.5f); frame.anchoredPosition = new Vector2(0f, -695f);
            Img(frame, "Round", Theme.Ink, sliced: true);
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var picture = Img(Stretch("Picture", frame), null, Color.white);
            picture.rectTransform.offsetMin = new Vector2(12f, 12f); picture.rectTransform.offsetMax = new Vector2(-12f, -12f);
            picture.preserveAspect = true;
            var hens = At("Chickens", frame, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            foreach (var (x, y, size) in new[] { (-170f, 230f, 80f), (70f, 260f, 70f), (190f, 130f, 84f), (-90f, 90f, 74f), (150f, -45f, 78f), (-190f, -85f, 84f), (30f, -175f, 72f), (-70f, -285f, 80f), (180f, -250f, 76f) })
                Img(At("Hen", hens, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size)), "Chicken", Color.white).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var lockCover = Stretch("Lock", frame);
            lockCover.offsetMin = new Vector2(12f, 12f); lockCover.offsetMax = new Vector2(-12f, -12f);
            Img(lockCover, "Round", Theme.WithAlpha(Theme.Ink, 0.72f), sliced: true);
            Img(At("Ring", lockCover, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(220f, 220f)), "Circle", Theme.WithAlpha(Theme.Cream, 0.15f));
            Img(At("Icon", lockCover, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(124f, 124f)), "IconLock", Theme.Cream);
            var lockLabel = Txt(At("Label", lockCover, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(700f, 140f)), "BEAT ROSIE IN\nMEADOW RUN", 46, Theme.Cream, _condItalic);
            lockLabel.textWrappingMode = TextWrappingModes.Normal;
            Configure(frame.gameObject.AddComponent<PreviewFrame>(), ("picture", picture), ("maxSize", new Vector2(W - 80f, 770f)), ("inset", 12f));

            // What to do: two chips.
            RectTransform Chip(string name, float x, string icon, string text)
            {
                var chip = At(name, card, new Vector2(0.5f, 1f), new Vector2(x, -1137f), new Vector2(340f, 86f));
                chip.pivot = new Vector2(0.5f, 0.5f); chip.anchoredPosition = new Vector2(x, -1137f);
                Img(At("Shadow", chip, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(340f, 86f)), "Round", Theme.CreamDark, sliced: true);
                Img(Stretch("Face", chip), "Round", Color.white, sliced: true);
                Img(At("Icon", chip, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(60f, 60f)), icon, icon == "Chicken" ? Color.white : Theme.Rust).rectTransform.pivot = new Vector2(0f, 0.5f);
                var label = Txt(At("Label", chip, new Vector2(1f, 0.5f), new Vector2(-24f, 2f), new Vector2(220f, 80f)), text, 40, Theme.Ink, _black, TextAlignmentOptions.Right);
                label.rectTransform.pivot = new Vector2(1f, 0.5f); label.enableAutoSizing = true; label.fontSizeMin = 24; label.fontSizeMax = 40;
                return chip;
            }
            Chip("Goal", -180f, "Chicken", "15 CHICKENS");
            Chip("Time", 180f, "IconGauge", "75 SECONDS");

            // The prize: a dark panel, your car wearing it on a lit round stage, WIN THE PRIZE and its name; won, the
            // panel turns gold with a WON! stamp.
            var prize = At("Prize", card, new Vector2(0.5f, 1f), new Vector2(0f, -1300f), new Vector2(W - 80f, 190f));
            prize.pivot = new Vector2(0.5f, 0.5f); prize.anchoredPosition = new Vector2(0f, -1300f);
            var prizeOutlineRt = Stretch("Outline", prize); prizeOutlineRt.offsetMin = new Vector2(-6f, -6f); prizeOutlineRt.offsetMax = new Vector2(6f, 6f);
            var prizeOutline = Img(prizeOutlineRt, "Round", Theme.Ink, sliced: true);
            var prizeFace = Img(Stretch("Face", prize), "Round", Theme.Ink, sliced: true);
            var stageDisc = At("Stage", prize, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(166f, 166f));
            stageDisc.pivot = new Vector2(0f, 0.5f);
            Img(At("Glow", stageDisc, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 240f)), "Glow", Theme.WithAlpha(Theme.Amber, 0.55f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Img(Stretch("Floor", stageDisc), "Circle", Shade(Theme.Ink, 0.7f));
            Img(At("Ring", stageDisc, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(166f, 166f)), "Ring", Theme.Amber);
            // Just the chicken (the prize is the chicken that rides on your roof).
            var prizeIcon = Img(At("Chicken", stageDisc, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132f, 132f)), "Chicken", Color.white);
            prizeIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f); prizeIcon.preserveAspect = true;
            var prizeCaption = Txt(At("Caption", prize, new Vector2(0f, 0.5f), new Vector2(210f, 50f), new Vector2(480f, 40f)), "WIN THE PRIZE", 32, Theme.Amber, _condItalic, TextAlignmentOptions.Left);
            prizeCaption.rectTransform.pivot = new Vector2(0f, 0.5f); prizeCaption.characterSpacing = 4f;
            var prizeName = Txt(At("Name", prize, new Vector2(0f, 0.5f), new Vector2(210f, 2f), new Vector2(480f, 66f)), "CHICKEN RIDER", 60, Color.white, _black, TextAlignmentOptions.Left);
            prizeName.rectTransform.pivot = new Vector2(0f, 0.5f);
            var prizeSub = Txt(At("Sub", prize, new Vector2(0f, 0.5f), new Vector2(210f, -48f), new Vector2(480f, 40f)), "A CHICKEN FOR YOUR ROOF", 28, Theme.WithAlpha(Theme.Cream, 0.8f), _condItalic, TextAlignmentOptions.Left);
            prizeSub.rectTransform.pivot = new Vector2(0f, 0.5f); prizeSub.characterSpacing = 2f;
            var won = At("Won", prize, new Vector2(1f, 0.5f), new Vector2(-24f, 4f), new Vector2(150f, 150f));
            won.pivot = new Vector2(1f, 0.5f); won.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Img(won, "Circle", Theme.Leaf);
            Img(At("Ring", won, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132f, 132f)), "RingDashed", Theme.WithAlpha(Color.white, 0.8f));
            Img(At("Check", won, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(54f, 54f)), "IconCheck", Color.white);
            Txt(At("Label", won, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(130f, 44f)), "WON!", 38, Color.white, _black);

            // PLAY along the bottom, like START.
            var play = At("Play", card, new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(600f, 178f));
            var playShadowRt = Stretch("Shadow", play); playShadowRt.offsetMax = new Vector2(0f, -14f);
            Img(playShadowRt, "Round", Theme.OrangeDeep, sliced: true);
            var playFaceRt = Stretch("Face", play); playFaceRt.offsetMin = new Vector2(0f, 14f);
            var playFace = Img(playFaceRt, "Round", Theme.Orange, sliced: true, raycast: true);
            var playLabel = Txt(Stretch("Label", playFaceRt), "PLAY", 76, Color.white, _black);
            playLabel.characterSpacing = 6f; playLabel.enableAutoSizing = true; playLabel.fontSizeMin = 36; playLabel.fontSizeMax = 76;
            var playButton = play.gameObject.AddComponent<Button>();
            playButton.targetGraphic = playFace; playButton.transition = Selectable.Transition.None;
            play.gameObject.AddComponent<ButtonFeedback>();

            var veilRt = Stretch("Veil", card); veilRt.offsetMin = new Vector2(-8f, -8f); veilRt.offsetMax = new Vector2(8f, 8f);
            var veil = Img(veilRt, "Round", Theme.WithAlpha(Theme.Cream, 0f), sliced: true);
            // The chicken on a medallion over the top left corner, like a race card's number (with the same frost).
            var medal = At("Medal", card, new Vector2(0f, 1f), new Vector2(-26f, 34f), new Vector2(170f, 182f));
            Img(At("Shadow", medal, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.OrangeDeep);
            var medalShadowVeil = Img(At("ShadowVeil", medal, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.WithAlpha(Theme.Cream, 0f));
            Img(At("Face", medal, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.Orange);
            Img(At("Rim", medal, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(146f, 146f)), "Ring", Theme.WithAlpha(Color.white, 0.55f));
            var tabIcon = Img(At("Icon", medal, new Vector2(0.5f, 1f), new Vector2(0f, -85f), new Vector2(118f, 118f)), "Chicken", Color.white);
            tabIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var medalVeil = Img(At("Veil", medal, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(170f, 170f)), "Circle", Theme.WithAlpha(Theme.Cream, 0f));
            // The cross on the top right corner closes the deck (as on a race card).
            var closeRt = At("Close", card, new Vector2(1f, 1f), new Vector2(30f, 30f), new Vector2(124f, 124f));
            closeRt.pivot = new Vector2(0.5f, 0.5f); closeRt.anchoredPosition = new Vector2(-32f, -32f);
            Img(closeRt, "Circle", new Color(0.06f, 0.07f, 0.08f));
            var closeFace = Img(At("Face", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(108f, 108f)), "Circle", Theme.Cream, raycast: true);
            Img(At("Icon", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f)), "IconClose", Theme.Ink);
            var closeVeil = Img(At("Veil", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124f, 124f)), "Circle", Theme.WithAlpha(Theme.Cream, 0f));
            var close = closeRt.gameObject.AddComponent<Button>();
            close.targetGraphic = closeFace; close.transition = Selectable.Transition.None;
            closeRt.gameObject.AddComponent<ButtonFeedback>();

            var comp = card.gameObject.AddComponent<DeckChallengeCard>();
            Configure(comp, ("group", group), ("body", body), ("playButton", playButton), ("picture", picture), ("tabIcon", tabIcon), ("playFace", playFace), ("veil", veil),
                ("chapter", chapter), ("title", title), ("best", best), ("prizeName", prizeName), ("playLabel", playLabel), ("lockLabel", lockLabel),
                ("chickens", hens), ("lockCover", lockCover.gameObject), ("prizeWon", won.gameObject), ("prizeIcon", prizeIcon), ("closeButton", close),
                ("prizeCaption", prizeCaption), ("prizeSub", prizeSub), ("prizeFace", prizeFace), ("prizeOutline", prizeOutline));
            var dso = new SerializedObject(comp);
            SetArray(dso, "edgeVeils", new[] { medalShadowVeil, medalVeil, closeVeil });
            dso.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);
            return comp;
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

            var starsPill = At("Stars", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(160f, 60f));
            Img(starsPill, "Pill", Theme.Ink, sliced: true);
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
                stars[i] = Img(At($"Star{i + 1}", starsPill, new Vector2(0.5f, 0.5f), new Vector2(-44f + i * 44f, 0f), new Vector2(40f, 40f)), "IconStar", Theme.Amber);

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

            // The rival who waits at this stop: their face on the badge's shoulder.
            var rival = At("Rival", root, new Vector2(0.5f, 0.5f), new Vector2(72f, 66f), new Vector2(100f, 100f));
            rival.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", rival, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(100f, 100f)), "Circle", Shade(Theme.Ink, 0.7f));
            Img(Stretch("Ring", rival), "Circle", Theme.Ink);
            var rivalPicture = Img(At("Picture", rival, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f)), "Circle", Color.white);
            var rivalMystery = Txt(At("Mystery", rival, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(90f, 90f)), "?", 64, Theme.Amber, _black);
            var rivalBeaten = At("Beaten", rival, new Vector2(0.5f, 0.5f), new Vector2(34f, -34f), new Vector2(42f, 42f));
            Img(rivalBeaten, "Circle", Theme.Leaf);
            Img(At("Icon", rivalBeaten, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f)), "IconCheck", Color.white);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<ButtonFeedback>();

            var mapNode = root.gameObject.AddComponent<MapNode>();
            var so = new SerializedObject(mapNode);
            Set(so, "button", button); Set(so, "shadow", shadow); Set(so, "face", face); Set(so, "ring", ring);
            Set(so, "dashedRing", dashed); Set(so, "number", number); Set(so, "lockIcon", lockIcon);
            Set(so, "starsPill", starsPill.gameObject); SetArray(so, "stars", stars);
            Set(so, "requirement", req.gameObject); Set(so, "requirementText", reqText);
            Set(so, "selection", selection.gameObject); Set(so, "caption", caption); Set(so, "captionRoot", capRt.gameObject);
            Set(so, "rivalBadge", rival.gameObject); Set(so, "rivalPicture", rivalPicture); Set(so, "rivalMystery", rivalMystery.gameObject); Set(so, "rivalBeaten", rivalBeaten.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            rival.gameObject.SetActive(false);
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

            // HUD bar: LAP | TIME on a pine-green card with a hard shadow, between the menu button and the road
            // ahead in the corner (your place is on the ladder under the menu button).
            const float aheadSize = 288f;
            var hud = Rt("Hud", safe);
            hud.anchorMin = new Vector2(0f, 1f); hud.anchorMax = new Vector2(1f, 1f); hud.pivot = new Vector2(0.5f, 1f);
            float hudLeft = 210f, hudRight = 48f + aheadSize + 24f;
            hud.anchoredPosition = new Vector2((hudLeft - hudRight) * 0.5f, -48f); hud.sizeDelta = new Vector2(-(hudLeft + hudRight), 138f);
            var hudShadow = Stretch("Shadow", hud);
            hudShadow.offsetMin = new Vector2(0f, -12f); hudShadow.offsetMax = new Vector2(0f, -12f);
            Img(hudShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Fill", hud), "Round", Theme.Ink, sliced: true);
            var lap = HudColumn(hud, "Lap", 0f, 0.38f, "LAP", "1/3", 76, _condItalic);
            var time = HudColumn(hud, "Time", 0.38f, 1f, "TIME", "0:00.00", 76, _condItalic);
            var flashRt = At("LapFlash", safe, new Vector2(0.5f, 1f), new Vector2(-30f, -224f), new Vector2(440f, 84f));
            Img(flashRt, "Pill", Theme.Cream, sliced: true);
            var flash = Txt(Stretch("Label", flashRt), "LAP 0:21.56", 46, Theme.Ink, _condItalic);
            flash.characterSpacing = 4f;
            var warning = Txt(At("Warning", safe, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(1050f, 240f)), "WRONG WAY", 116, Theme.Orange, _black);
            warning.fontSharedMaterial = _blackOutline;
            warning.enableAutoSizing = true; warning.fontSizeMin = 50; warning.fontSizeMax = 116;
            var hudComp = hud.gameObject.AddComponent<RaceHud>();
            var hso = new SerializedObject(hudComp);
            Set(hso, "race", race); Set(hso, "lapLabel", lap); Set(hso, "timeLabel", time);
            Set(hso, "lapFlashLabel", flash); Set(hso, "lapFlashRoot", flashRt.gameObject); Set(hso, "warningLabel", warning);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // The road around the car, in the top right corner: fixed like the screen, the car in the middle.
            var aheadRt = At("TrackAhead", safe, new Vector2(1f, 1f), new Vector2(-48f, -48f), new Vector2(aheadSize, aheadSize));
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

            // Who is around you: the ladder top left (the car ahead, you, the car behind), place tags over the car
            // ahead and the one behind, and a rival with a face speaking (their home race).
            var watch = Stretch("Rivals", safe);
            var tag = At("Tag", watch, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 62f));
            var tagPoint = Img(At("Point", tag, new Vector2(0.5f, 0f), new Vector2(0f, -12f), new Vector2(28f, 24f)), "NavArrow", Theme.Ink).rectTransform;
            tagPoint.pivot = new Vector2(0.5f, 0.5f); tagPoint.localRotation = Quaternion.Euler(0f, 0f, 180f);
            Img(Stretch("Pill", tag), "Pill", Theme.Ink, sliced: true);
            var tagFace = RivalFace(tag, "Face", new Vector2(0f, 0.5f), new Vector2(4f, 0f), 54f);
            tagFace.pivot = new Vector2(0f, 0.5f);
            Txt(Stretch("Label", tag), "3RD", 40, Theme.Cream, _black);

            var ladder = At("Ladder", watch, new Vector2(0f, 1f), new Vector2(48f, -222f), new Vector2(252f, 276f));
            var ladderShadow = Stretch("Shadow", ladder);
            ladderShadow.offsetMin = new Vector2(0f, -12f); ladderShadow.offsetMax = new Vector2(0f, -12f);
            Img(ladderShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Fill", ladder), "Round", Theme.Ink, sliced: true);
            var rows = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                bool you = i == 1;
                var row = At(i == 0 ? "Ahead" : you ? "You" : "Behind", ladder, new Vector2(0.5f, 1f), new Vector2(0f, -42f - 96f * i), new Vector2(252f, you ? 108f : 84f));
                row.pivot = new Vector2(0.5f, 0.5f);
                if (you)
                {
                    var highlight = Stretch("Highlight", row);
                    highlight.offsetMin = new Vector2(8f, 8f); highlight.offsetMax = new Vector2(-8f, -8f);
                    Img(highlight, "Round", Theme.Orange, sliced: true);
                }
                var place = Txt(At("Place", row, new Vector2(0f, 0.5f), new Vector2(26f, you ? 2f : 0f), new Vector2(120f, 80f)),
                    i == 0 ? "2ND" : you ? "3RD" : "4TH", you ? 66 : 42, you ? Color.white : Theme.Cream, _black, TextAlignmentOptions.Left);
                place.rectTransform.pivot = new Vector2(0f, 0.5f);
                RivalFace(row, "Face", new Vector2(1f, 0.5f), new Vector2(-16f, 0f), you ? 96f : 78f, 1.45f).pivot = new Vector2(1f, 0.5f);
                rows[i] = row;
            }
            // FINAL LAP and the like: a pill hanging under the ladder for a few seconds.
            var header = At("Header", ladder, new Vector2(0.5f, 0f), new Vector2(0f, -26f), new Vector2(252f, 60f));
            header.pivot = new Vector2(0.5f, 1f);
            Img(Stretch("Pill", header), "Pill", Theme.Amber, sliced: true);
            var headerLabel = Txt(Stretch("Label", header), "FINAL LAP", 34, Theme.Ink, _condItalic);
            headerLabel.characterSpacing = 3f;
            headerLabel.enableAutoSizing = true; headerLabel.fontSizeMin = 22; headerLabel.fontSizeMax = 34;
            headerLabel.margin = new Vector4(14f, 0f, 14f, 0f);

            // A rival with a face speaking: their portrait and name, and a speech bubble (above the results too).
            var says = At("Says", watch, new Vector2(0.5f, 0f), new Vector2(0f, 560f), new Vector2(1060f, 300f));
            says.pivot = new Vector2(0.5f, 0.5f);
            var saysCanvas = says.gameObject.AddComponent<Canvas>();
            saysCanvas.overrideSorting = true; saysCanvas.sortingOrder = 20;
            // Their face as a sticker (it changes with the mood of the line).
            var portrait = At("Portrait", says, new Vector2(0f, 0.5f), new Vector2(140f, 20f), new Vector2(290f, 290f));
            portrait.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Picture", portrait, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(290f, 290f)), "Circle", Color.white);
            var namePlate = At("Name", portrait, new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(250f, 58f));
            namePlate.pivot = new Vector2(0.5f, 0.5f);
            Img(Stretch("Pill", namePlate), "Pill", Theme.Orange, sliced: true);
            var nameLabel = Txt(Stretch("Label", namePlate), "ROSIE RUST", 34, Color.white, _black);
            nameLabel.enableAutoSizing = true; nameLabel.fontSizeMin = 20; nameLabel.fontSizeMax = 34;
            var bubble = At("Bubble", says, new Vector2(0f, 0.5f), new Vector2(290f, 20f), new Vector2(760f, 200f));
            bubble.pivot = new Vector2(0f, 0.5f);
            var bubbleShadow = Stretch("Shadow", bubble);
            bubbleShadow.offsetMin = new Vector2(0f, -10f); bubbleShadow.offsetMax = new Vector2(0f, -10f);
            Img(bubbleShadow, "Round", Theme.WithAlpha(Theme.Ink, 0.35f), sliced: true);
            var tail = Img(At("Tail", bubble, new Vector2(0f, 0.5f), new Vector2(-22f, -10f), new Vector2(54f, 50f)), "NavArrow", Theme.Cream).rectTransform;
            tail.pivot = new Vector2(0.5f, 0.5f); tail.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Img(Stretch("Fill", bubble), "Round", Theme.Cream, sliced: true);
            var line = Txt(Stretch("Label", bubble), "My meadow, my rules. Try to keep up!", 50, Theme.Ink, _bold, TextAlignmentOptions.MidlineLeft);
            line.textWrappingMode = TextWrappingModes.Normal;
            line.margin = new Vector4(36f, 20f, 36f, 20f);
            line.enableAutoSizing = true; line.fontSizeMin = 30; line.fontSizeMax = 50;

            // The gadget's button, bottom right (only with a gadget from the garage).
            var gadget = At("Gadget", safe, new Vector2(1f, 0f), new Vector2(-56f, 90f), new Vector2(220f, 220f));
            gadget.pivot = new Vector2(0.5f, 0.5f); gadget.anchoredPosition = new Vector2(-166f, 200f);
            var gadgetGlow = Img(At("Glow", gadget, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 330f)), "Glow", Theme.WithAlpha(Theme.Amber, 0.75f));
            gadgetGlow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", gadget, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(210f, 210f)), "Circle", Shade(Theme.Ink, 0.6f));
            Img(At("Ring", gadget, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(214f, 214f)), "Circle", Theme.Amber);
            var gadgetFace = Img(At("Face", gadget, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(196f, 196f)), "Circle", Theme.Ink, raycast: true);
            var gadgetIcon = Img(At("Icon", gadget, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(130f, 130f)), "HayBale", Color.white);
            gadgetIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var gadgetLabelRt = At("Label", gadget, new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(190f, 52f));
            gadgetLabelRt.pivot = new Vector2(0.5f, 0.5f);
            Img(gadgetLabelRt, "Pill", Theme.Orange, sliced: true);
            var gadgetLabel = Txt(Stretch("Text", gadgetLabelRt), "HAY!", 32, Color.white, _black);
            var gadgetButton = gadget.gameObject.AddComponent<Button>();
            gadgetButton.targetGraphic = gadgetFace; gadgetButton.transition = Selectable.Transition.None;
            Configure(gadget.gameObject.AddComponent<GadgetButton>(), ("race", race), ("button", gadgetButton), ("face", gadgetFace), ("icon", gadgetIcon),
                ("glow", gadgetGlow), ("label", gadgetLabel));

            // The first win against a rival with a face: their prizes, over the results.
            var rewardHost = Stretch("RivalReward", canvas);
            var rewardCanvas = rewardHost.gameObject.AddComponent<Canvas>();
            rewardCanvas.overrideSorting = true; rewardCanvas.sortingOrder = 30;
            rewardHost.gameObject.AddComponent<GraphicRaycaster>();
            var rewardPanel = Stretch("Panel", rewardHost);
            var rewardGroup = rewardPanel.gameObject.AddComponent<CanvasGroup>();
            Img(Stretch("Dim", rewardPanel), null, Theme.WithAlpha(Theme.Ink, 0.82f), raycast: true);
            Img(At("Rays", rewardPanel, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1600f, 1600f)), "Rays", Theme.WithAlpha(Theme.Amber, 0.18f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var rsafe = SafeArea(rewardPanel);
            var rcard = At("Card", rsafe, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(980f, 1500f));
            rcard.pivot = new Vector2(0.5f, 0.5f); rcard.anchoredPosition = new Vector2(0f, -40f);
            var rOutline = Stretch("Outline", rcard); rOutline.offsetMin = new Vector2(-10f, -10f); rOutline.offsetMax = new Vector2(10f, 10f);
            Img(rOutline, "Round", Theme.Amber, sliced: true);
            Img(Stretch("Face", rcard), "Round", Theme.Cream, sliced: true);
            var rTitle = Txt(At("Title", rcard, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(920f, 120f)), "YOU BEAT ROSIE RUST!", 84, Theme.Orange, _black);
            rTitle.enableAutoSizing = true; rTitle.fontSizeMin = 50; rTitle.fontSizeMax = 84; rTitle.fontSharedMaterial = _blackShadow;
            var rPortrait = At("Portrait", rcard, new Vector2(0f, 1f), new Vector2(190f, -40f), new Vector2(300f, 300f));
            rPortrait.pivot = new Vector2(0.5f, 0.5f); rPortrait.anchoredPosition = new Vector2(190f, -40f);
            var rPortraitImage = Img(At("Picture", rPortrait, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 320f)), "Circle", Color.white);
            var rBubble = At("Bubble", rcard, new Vector2(0f, 1f), new Vector2(360f, -150f), new Vector2(580f, 170f));
            rBubble.pivot = new Vector2(0f, 0.5f);
            var rbOutline = Stretch("Outline", rBubble); rbOutline.offsetMin = new Vector2(-7f, -7f); rbOutline.offsetMax = new Vector2(7f, 7f);
            Img(rbOutline, "Round", new Color(0.06f, 0.07f, 0.08f), sliced: true);
            Img(Stretch("Face", rBubble), "Round", Color.white, sliced: true);
            var rLine = Txt(Stretch("Line", rBubble), "Hmph. Beginner's luck!", 44, Theme.Ink, _bold);
            rLine.textWrappingMode = TextWrappingModes.Normal; rLine.margin = new Vector4(30f, 12f, 30f, 12f);
            rLine.enableAutoSizing = true; rLine.fontSizeMin = 26; rLine.fontSizeMax = 44;
            Txt(At("New", rcard, new Vector2(0.5f, 1f), new Vector2(0f, -450f), new Vector2(900f, 60f)), "NEW IN YOUR GARAGE", 44, Theme.Rust, _condItalic).characterSpacing = 6f;
            RectTransform Prize(string name, float x, string caption)
            {
                var p = At(name, rcard, new Vector2(0.5f, 1f), new Vector2(x, -830f), new Vector2(400f, 620f));
                p.pivot = new Vector2(0.5f, 0.5f); p.anchoredPosition = new Vector2(x, -830f);
                var ps = Stretch("Shadow", p); ps.offsetMin = new Vector2(0f, -12f); ps.offsetMax = new Vector2(0f, -12f);
                Img(ps, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
                Img(Stretch("Face", p), "Round", Theme.Ink, sliced: true);
                Txt(At("Caption", p, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(380f, 44f)), caption, 32, Theme.Amber, _condItalic).characterSpacing = 4f;
                return p;
            }
            var prizeGadget = Prize("Gadget", -215f, "GADGET");
            var rGadgetIcon = Img(At("Icon", prizeGadget, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(260f, 260f)), "HayBale", Color.white);
            rGadgetIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var rGadgetName = Txt(At("Name", prizeGadget, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(370f, 100f)), "HAY BALE DROP", 46, Color.white, _black);
            rGadgetName.textWrappingMode = TextWrappingModes.Normal;
            var prizePart = Prize("Part", 215f, "TOP PART");
            var partCarRt = At("Car", prizePart, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(190f, 333f));
            partCarRt.pivot = new Vector2(0.5f, 0.5f);
            var pBody = Img(Stretch("Body", partCarRt), "Car", Theme.Orange); pBody.preserveAspect = false;
            var pDetails = Img(Stretch("Details", partCarRt), "CarDetails", Color.white); pDetails.preserveAspect = false;
            var partCar = partCarRt.gameObject.AddComponent<VehicleIcon>();
            Configure(partCar, ("body", pBody), ("details", pDetails));
            var rPartName = Txt(At("Name", prizePart, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(370f, 100f)), "HAY-BALE SPOILER", 46, Color.white, _black);
            rPartName.textWrappingMode = TextWrappingModes.Normal;
            var ok = BigRoundButton(rcard, "Ok", new Vector2(0.5f, 0f), new Vector2(0f, -70f), 230f, "COOL!", out _, out _, out _, out _);
            var rewardComp = rewardHost.gameObject.AddComponent<RivalReward>();
            Configure(rewardComp, ("panel", rewardPanel.gameObject), ("group", rewardGroup), ("card", rcard), ("portrait", rPortrait), ("bubble", rBubble),
                ("portraitImage", rPortraitImage), ("gadgetIcon", rGadgetIcon), ("title", rTitle), ("line", rLine), ("gadgetName", rGadgetName),
                ("partName", rPartName), ("partCar", partCar), ("okButton", ok));
            var rewardSo = new SerializedObject(rewardComp);
            SetArray(rewardSo, "prizes", new[] { prizeGadget, prizePart });
            rewardSo.ApplyModifiedPropertiesWithoutUndo();
            rewardPanel.gameObject.SetActive(false);

            // The end of a challenge: the result, the prize on the first win, AGAIN and MAP.
            var chHost = Stretch("ChallengeResult", canvas);
            var chCanvas = chHost.gameObject.AddComponent<Canvas>();
            chCanvas.overrideSorting = true; chCanvas.sortingOrder = 30;
            chHost.gameObject.AddComponent<GraphicRaycaster>();
            var chPanel = Stretch("Panel", chHost);
            var chGroup = chPanel.gameObject.AddComponent<CanvasGroup>();
            Img(Stretch("Dim", chPanel), null, Theme.WithAlpha(Theme.Ink, 0.8f), raycast: true);
            var chSafe = SafeArea(chPanel);
            var chCard = At("Card", chSafe, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1420f));
            chCard.pivot = new Vector2(0.5f, 0.5f); chCard.anchoredPosition = Vector2.zero;
            var chOutline = Stretch("Outline", chCard); chOutline.offsetMin = new Vector2(-10f, -10f); chOutline.offsetMax = new Vector2(10f, 10f);
            Img(chOutline, "Round", Theme.Orange, sliced: true);
            Img(Stretch("Face", chCard), "Round", Theme.Cream, sliced: true);
            Txt(At("Kind", chCard, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 50f)), "CHICKEN CHASE", 40, Theme.Rust, _condItalic).characterSpacing = 8f;
            var chTitleR = Txt(At("Title", chCard, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(900f, 150f)), "ALL CAUGHT!", 130, Theme.Orange, _black);
            chTitleR.fontSharedMaterial = _blackShadow; chTitleR.enableAutoSizing = true; chTitleR.fontSizeMin = 70; chTitleR.fontSizeMax = 130;
            Img(At("Chicken", chCard, new Vector2(0.5f, 1f), new Vector2(-170f, -300f), new Vector2(190f, 190f)), "Chicken", Color.white);
            var chScore = Txt(At("Score", chCard, new Vector2(0.5f, 1f), new Vector2(90f, -300f), new Vector2(420f, 150f)), "15 / 15", 120, Theme.Ink, _black);
            var chBest = Txt(At("Best", chCard, new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(900f, 60f)), "BEST 15 / 15", 44, Theme.InkSoft, _condItalic);
            chBest.characterSpacing = 6f;
            var chPrize = At("Prize", chCard, new Vector2(0.5f, 1f), new Vector2(0f, -830f), new Vector2(820f, 520f));
            chPrize.pivot = new Vector2(0.5f, 0.5f); chPrize.anchoredPosition = new Vector2(0f, -830f);
            Img(chPrize, "Round", Theme.Ink, sliced: true);
            Txt(At("Caption", chPrize, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(780f, 50f)), "NEW IN YOUR GARAGE", 38, Theme.Amber, _condItalic).characterSpacing = 6f;
            var chCarRt = At("Car", chPrize, new Vector2(0f, 0.5f), new Vector2(150f, -20f), new Vector2(190f, 333f));
            chCarRt.pivot = new Vector2(0.5f, 0.5f);
            var chBody = Img(Stretch("Body", chCarRt), "Car", Theme.Orange); chBody.preserveAspect = false;
            var chDetails = Img(Stretch("Details", chCarRt), "CarDetails", Color.white); chDetails.preserveAspect = false;
            var chPrizeCar = chCarRt.gameObject.AddComponent<VehicleIcon>();
            Configure(chPrizeCar, ("body", chBody), ("details", chDetails));
            var chPrizeName = Txt(At("Name", chPrize, new Vector2(1f, 0.5f), new Vector2(-40f, 10f), new Vector2(480f, 160f)), "CHICKEN RIDER", 64, Color.white, _black, TextAlignmentOptions.Right);
            chPrizeName.rectTransform.pivot = new Vector2(1f, 0.5f); chPrizeName.textWrappingMode = TextWrappingModes.Normal;
            Txt(At("Kind", chPrize, new Vector2(1f, 0.5f), new Vector2(-40f, -90f), new Vector2(480f, 50f)), "A PART FOR YOUR ROOF", 32, Theme.Amber, _condItalic, TextAlignmentOptions.Right).rectTransform.pivot = new Vector2(1f, 0.5f);
            var again = SquareButton(chCard, "Again", new Vector2(0.5f, 0f), new Vector2(-150f, 80f), "IconRetry", 170f);
            var toMap = BigRoundButton(chCard, "Map", new Vector2(0.5f, 0f), new Vector2(150f, 40f), 230f, "MAP", out _, out _, out _, out _);
            var chResult = chHost.gameObject.AddComponent<ChallengeResult>();
            Configure(chResult, ("panel", chPanel.gameObject), ("group", chGroup), ("card", chCard), ("prize", chPrize), ("title", chTitleR), ("score", chScore),
                ("best", chBest), ("prizeName", chPrizeName), ("prizeCar", chPrizeCar), ("againButton", again), ("mapButton", toMap));
            chPanel.gameObject.SetActive(false);

            var watchComp = watch.gameObject.AddComponent<RivalWatch>();
            Configure(watchComp, ("race", race), ("tagTemplate", tag), ("ladder", ladder), ("ladderHeader", header), ("says", says), ("reward", rewardComp));
            var wso = new SerializedObject(watchComp);
            SetArray(wso, "ladderRows", rows);
            wso.ApplyModifiedPropertiesWithoutUndo();

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
            BuildOnlineRaceView(canvas, race);
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
            card.anchoredPosition = new Vector2(87f, -48f); card.sizeDelta = new Vector2(-270f, 590f); // room for the demo under the text
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
                "Finish in the top five for stars, win for all three. Stars open new races.", 44, Theme.Ink, _semi);
            text.textWrappingMode = TextWrappingModes.Normal;
            var go = BigRoundButton(panel, "MapButton", new Vector2(0.5f, 0f), new Vector2(0f, 50f), 190f, "MAP", out _, out _, out _, out _);

            // Covers the whole screen for a moment while the car is moved to the next step's spot.
            var fadeRt = Stretch("TutorialFade", (RectTransform)safe.parent);
            Img(fadeRt, null, Theme.Ink);
            var fade = fadeRt.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f; fade.blocksRaycasts = false; fade.interactable = false;

            // The demo under the hint card: white fingers act out the step, a mini car shows what happens.
            // Inside the hint card, under its text: a small dark screen acting out the step.
            var demo = At("Demo", card, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(820f, 300f)); // on the card's bottom edge
            demo.localScale = Vector3.one * 1.04f; // as wide as the card
            demo.gameObject.AddComponent<RectMask2D>(); // the jumping car stays inside its area
            var demoGroup = demo.gameObject.AddComponent<CanvasGroup>();
            demoGroup.blocksRaycasts = false; demoGroup.interactable = false;
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
            for (int i = 0; i < lines.Length; i++) lines[i] = Part($"SpeedLine{i}", null, new Vector2(8f, 72f), Theme.WithAlpha(Theme.Ink, 0.5f), center);
            var ring = Part("Ring", "Ring", new Vector2(190f, 190f), Theme.WithAlpha(Theme.Ink, 0.7f), center);
            ring.anchoredPosition = new Vector2(-285f, 0f);
            var knob = Part("Knob", "Circle", new Vector2(72f, 72f), Theme.WithAlpha(Theme.Ink, 0.35f), center);
            var press = Part("Press", "Circle", new Vector2(86f, 86f), Theme.WithAlpha(Theme.Ink, 0.3f), center);
            var ripple = Part("Ripple", "Ring", new Vector2(110f, 110f), Theme.Ink, center);
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
            Rounded(Img(tag, "Round", Theme.Ink, sliced: true), 16f);
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
            host.gameObject.AddComponent<KeepPalette>(); // the same colours on every cup
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
            var soundIcon = Img(At("Icon", sound, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(84f, 84f)), "IconSound", Theme.Ink);
            soundIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            var slider = BuildSlider(sound);

            var joystickButton = MenuButton(card, "JoystickButton", "JOYSTICK   VISIBLE", Color.white, Theme.Ink, Theme.Ink, 130f, out var joystickLabel, 46);
            var driftButton = MenuButton(card, "DriftButton", "DRIFTING   ON", Color.white, Theme.Ink, Theme.Ink, 130f, out var driftLabel, 46);
            var assistButton = MenuButton(card, "AssistButton", "STEERING HELP   ON", Color.white, Theme.Ink, Theme.Ink, 130f, out var assistLabel, 46);
            var zoomButton = MenuButton(card, "ZoomButton", "CAMERA   NORMAL", Color.white, Theme.Ink, Theme.Ink, 130f, out var zoomLabel, 46);
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
            Set(so, "volumeSlider", slider); Set(so, "soundIcon", soundIcon); Set(so, "soundOn", Sprite("IconSound")); Set(so, "soundOff", Sprite("IconMute")); Set(so, "joystickButton", joystickButton); Set(so, "joystickLabel", joystickLabel);
            Set(so, "resetButton", resetButton); Set(so, "resetLabel", resetLabel); Set(so, "tutorialButton", tutorialButton);
            Set(so, "cupsButton", cupsButton); Set(so, "cupsLabel", cupsLabel);
            Set(so, "assistButton", assistButton); Set(so, "assistLabel", assistLabel);
            Set(so, "driftButton", driftButton); Set(so, "driftLabel", driftLabel);
            Set(so, "zoomButton", zoomButton); Set(so, "zoomLabel", zoomLabel);
            Set(so, "catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Data/Levels/LevelCatalog.asset"));
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
        }

        // A flat bar with rounded corners and a rounded square knob (not pills).
        static Slider BuildSlider(RectTransform parent)
        {
            var root = At("Slider", parent, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(620f, 110f));
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            var track = Stretch("Track", root);
            track.anchorMin = new Vector2(0f, 0.38f); track.anchorMax = new Vector2(1f, 0.62f);
            Rounded(Img(track, "Round", Theme.CreamDark, sliced: true), 8f);
            var fillArea = Stretch("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.38f); fillArea.anchorMax = new Vector2(1f, 0.62f);
            fillArea.offsetMax = new Vector2(-40f, 0f);
            var fill = Stretch("Fill", fillArea);
            fill.offsetMax = new Vector2(40f, 0f);
            Rounded(Img(fill, "Round", Theme.Orange, sliced: true), 8f);
            var handleArea = Stretch("Handle Slide Area", root);
            handleArea.offsetMin = new Vector2(40f, 0f); handleArea.offsetMax = new Vector2(-40f, 0f);
            var handle = Rt("Handle", handleArea);
            handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(80f, 0f);
            Rounded(Img(At("Shadow", handle, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(80f, 84f)), "Round", Theme.Ink, sliced: true), 18f);
            var knob = Rounded(Img(At("Knob", handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 84f)), "Round", Color.white, sliced: true, raycast: true), 18f);
            // Two little grip lines on the knob.
            Rounded(Img(At("Grip1", handle, new Vector2(0.5f, 0.5f), new Vector2(-9f, 0f), new Vector2(7f, 34f)), "Round", Theme.Stone, sliced: true), 3f);
            Rounded(Img(At("Grip2", handle, new Vector2(0.5f, 0.5f), new Vector2(9f, 0f), new Vector2(7f, 34f)), "Round", Theme.Stone, sliced: true), 3f);
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

        // A rival's face (for now: its car in its colours on a disc; the Car/Body is tinted by the race).
        static RectTransform RivalFace(RectTransform parent, string name, Vector2 anchor, Vector2 position, float size, float carScale = 1f)
        {
            var face = At(name, parent, anchor, position, new Vector2(size, size));
            face.pivot = new Vector2(0.5f, 0.5f);
            Img(face, "Circle", Theme.Ink);
            Img(At("Disc", face, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.84f, size * 0.84f)), "Circle", Theme.Cream);
            var car = At("Car", face, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.34f * carScale, size * 0.6f * carScale));
            car.localRotation = Quaternion.Euler(0f, 0f, -35f);
            Img(Stretch("Body", car), "Car", Theme.Orange).preserveAspect = false;
            Img(Stretch("Details", car), "CarDetails", Color.white).preserveAspect = false;
            // A rival with a face: their portrait instead of the car (switched on by the race).
            var picture = Img(At("Picture", face, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.9f, size * 0.9f)), "Circle", Color.white);
            picture.gameObject.SetActive(false);
            return face;
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
                ("snowRunoff", Mat("SnowRunoff")), ("snowScenery", Mat("SceneryWinter")), ("scenery", sceneryRenderer), ("snowfall", snowfall),
                ("snowmobileBody", Sprite("Snowmobile")), ("snowmobileDetails", Sprite("SnowmobileDetails")), ("snowmobileShadow", Sprite("SnowmobileShadow")), ("snowmobileSki", Sprite("SnowmobileSki")));
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
            var icePatches = Layer("IcePatches", "IcePatch", -46); // over the road and its kerbs, like the oil
            // The winter surprises' sprites on the hazards.
            var hazards = Object.FindAnyObjectByType<SomeGame.Track.TrackHazards>();
            if (hazards != null)
                Configure(hazards, ("snowballSprite", Sprite("Snowball")), ("jumpPadSprite", Sprite("JumpPad")), ("penguinSprite", Sprite("Penguin")),
                    ("dashPanelSprite", Sprite("DashPanel")), ("snowPileSprite", Sprite("SnowPile")),
                    ("liftStationSprite", Sprite("LiftStation")), ("liftTowerSprite", Sprite("LiftTower")), ("liftCableSprite", Sprite("LiftCable")), ("liftHangerSprite", Sprite("LiftHanger")));
            var features = Object.FindFirstObjectByType<SomeGame.Track.TrackFeatures>();
            if (features != null) Configure(features, ("snowShore", Mat("SnowShore")));
            // The banked walls have their own layer; "Banks" is the river banks' layer (TrackFeatures), in sand.
            var bankLayer = Layer("BankWalls", "IceWall", -47);
            var riverBanks = track.transform.Find("Banks");
            if (riverBanks != null && riverBanks.TryGetComponent(out MeshRenderer riverBankRenderer))
            {
                riverBankRenderer.sharedMaterial = Mat("Sand");
                riverBankRenderer.sortingOrder = -45;
            }
            var banks = track.GetComponent<SomeGame.Track.TrackBanks>();
            if (banks == null) banks = track.gameObject.AddComponent<SomeGame.Track.TrackBanks>();
            Configure(banks, ("track", track), ("walls", bankLayer));
            var branches = track.GetComponent<SomeGame.Track.TrackBranches>();
            if (branches == null) branches = track.gameObject.AddComponent<SomeGame.Track.TrackBranches>(); // Unity's null: no ?? here
            Configure(branches, ("track", track), ("roads", roads), ("edges", edges), ("ice", ice), ("roofs", roofs), ("roofShadows", roofShadows), ("icePatches", icePatches), ("meadowRoad", Mat("Road")), ("snowRoad", Mat("WinterRoad")));

            // The journey race: the houses it drives through, the set pieces (arches, trains, forest logs,
            // windmill), sheep, the cat, the robot vacuum, the ball and boulders.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(track.gameObject); // e.g. the old ground-zone component
            var houses = track.GetComponent<SomeGame.Track.TrackHouses>();
            if (houses == null) houses = track.gameObject.AddComponent<SomeGame.Track.TrackHouses>();
            Configure(houses, ("track", track), ("plain", Mat("Plain")));
            var hso = new SerializedObject(houses);
            SetArray(hso, "floors", new[] { Mat("FloorParquet"), Mat("FloorTiles"), Mat("FloorCarpet"), Mat("FloorPlanks"), Mat("FloorSawdust"), Mat("FloorCobbles"), Mat("FloorIce"), Mat("FloorRock"), Mat("FloorMarble"), Mat("FloorOak"), Mat("FloorGlass") });
            SetArray(hso, "roofs", new[] { Mat("RoofRed"), Mat("RoofBlue"), Mat("RoofCircus"), Mat("RoofSlate"), Mat("RoofIce"), Mat("RoofGinger"), Mat("RoofCave"), Mat("RoofChalet") });
            hso.ApplyModifiedPropertiesWithoutUndo();
            var journey = track.GetComponent<SomeGame.Track.TrackJourney>();
            if (journey == null) journey = track.gameObject.AddComponent<SomeGame.Track.TrackJourney>();
            var jso = new SerializedObject(journey);
            SetArray(jso, "boatSprites", new[] { Sprite("Sailboat"), Sprite("Rowboat"), Sprite("SwanBoat"), Sprite("MarkerBuoy"), Sprite("Ducks"), Sprite("Iceberg"), Sprite("Trawler"), Sprite("Icebreaker"), Sprite("SealFloe") });
            jso.ApplyModifiedPropertiesWithoutUndo();
            Material particles = hazards != null ? new SerializedObject(hazards).FindProperty("particleMaterial").objectReferenceValue as Material : null;
            Configure(journey, ("track", track), ("pillarSprite", Sprite("ArchPillar")), ("bannerSprite", Sprite("Banner")),
                ("locoSprite", Sprite("MineLoco")), ("wagonSprite", Sprite("MineWagon")), ("crossingSprite", Sprite("CrossingLight")),
                ("barrierSprite", Sprite("Barrier")), ("lampSprite", Sprite("SparkDot")), ("railsMaterial", Mat("Rails")),
                ("logSprite", Sprite("ForestLog")), ("windmillTowerSprite", Sprite("WindmillTower")), ("windmillSailsSprite", Sprite("WindmillSails")),
                ("cowSprite", Sprite("Cow")), ("fenceSprite", Sprite("Fence")), ("buntingSprite", Sprite("Bunting")),
                ("carouselBaseSprite", Sprite("CarouselBase")), ("carouselTopSprite", Sprite("CarouselTop")),
                ("swingHubSprite", Sprite("SwingHub")), ("swingSeatsSprite", Sprite("SwingSeats")),
                ("teacupFloorSprite", Sprite("TeacupFloor")), ("teacupSprite", Sprite("Teacup")),
                ("coasterCarSprite", Sprite("CoasterCar")), ("coasterRailsMaterial", Mat("CoasterRails")),
                ("bridgeLeafSprite", Sprite("BridgeLeaf")), ("stoneBridgeSprite", Sprite("StoneBridge")), ("ringSprite", Sprite("BoostRing")),
                ("lanternSprite", Sprite("Lantern")), ("floatLanternSprite", Sprite("FloatLantern")), ("glowSprite", Sprite("Glow")),
                ("headlightSprite", Sprite("Headlight")), ("auroraSprite", Sprite("Aurora")), ("waterfallMaterial", Mat("Waterfall")),
                ("toyLocoSprite", Sprite("ToyLoco")), ("toyWagonSprite", Sprite("ToyWagon")), ("toyRailsMaterial", Mat("ToyRails")),
                ("particleMaterial", particles), ("font", _black));
            // The mountain: chasms to fall into, caves through the rock.
            var mountain = track.GetComponent<SomeGame.Track.TrackMountain>();
            if (mountain == null) mountain = track.gameObject.AddComponent<SomeGame.Track.TrackMountain>();
            Configure(mountain, ("track", track), ("plain", Mat("Plain")), ("floorMaterial", Mat("FloorRock")),
                ("crystalSprite", Sprite("IceCrystal")), ("stalagmiteSprite", Sprite("Stalagmite")), ("glowSprite", Sprite("Glow")),
                ("boulderSprite", Sprite("CaveBoulder")), ("particleMaterial", particles), ("snowMaterial", Mat("SnowGround")));
            // The fjord that breaks up lap by lap, and the ways round that open on later laps.
            var fjord = track.GetComponent<SomeGame.Track.FjordIce>();
            if (fjord == null) fjord = track.gameObject.AddComponent<SomeGame.Track.FjordIce>();
            Configure(fjord, ("track", track), ("plain", Mat("Plain")), ("water", Mat("Water")), ("ice", Mat("FloorIce")), ("shore", Mat("SnowShore")),
                ("road", Mat("WinterRoad")), ("line", Mat("Line")), ("particleMaterial", particles),
                ("shipSprite", Sprite("Icebreaker")), ("floeSprite", Sprite("FjordFloe")), ("snowBankSprite", Sprite("SnowPile")));
            if (hazards != null) Configure(hazards, ("sealSprite", Sprite("Seal")));
            // The express: trains to ride, the level crossing, gift boxes.
            var express = track.GetComponent<SomeGame.Track.TrackTrain>();
            if (express == null) express = track.gameObject.AddComponent<SomeGame.Track.TrackTrain>();
            Configure(express, ("track", track), ("plain", Mat("Plain")), ("particleMaterial", particles),
                ("locoSprite", Sprite("SteamLoco")), ("wagonSprite", Sprite("FlatWagon")), ("giftSprite", Sprite("GiftBox")));
            // The avalanche race: viaducts over the road below, the avalanche, ice bridges, falling trees.
            var layers = track.GetComponent<SomeGame.Track.TrackLayers>();
            if (layers == null) layers = track.gameObject.AddComponent<SomeGame.Track.TrackLayers>();
            Configure(layers, ("track", track), ("plain", Mat("Plain")));
            var avalanche = track.GetComponent<SomeGame.Track.TrackAvalanche>();
            if (avalanche == null) avalanche = track.gameObject.AddComponent<SomeGame.Track.TrackAvalanche>();
            Configure(avalanche, ("track", track), ("plain", Mat("Plain")), ("particleMaterial", particles),
                ("pineSprite", Sprite("StandingPine")), ("fallenPineSprite", Sprite("FallenPine")));
            // Trick jumps: swipe the tricks or fall.
            var tricks = track.GetComponent<SomeGame.Track.TrackTricks>();
            if (tricks == null) tricks = track.gameObject.AddComponent<SomeGame.Track.TrackTricks>();
            Configure(tricks, ("track", track));
            // Turntables: spinning carousel floors the road crosses.
            var turntables = track.GetComponent<SomeGame.Track.TrackTurntables>();
            if (turntables == null) turntables = track.gameObject.AddComponent<SomeGame.Track.TrackTurntables>();
            Configure(turntables, ("track", track), ("plain", Mat("Plain")));
            // The snow park: snow kickers, nets, snow tunnels, chairlifts.
            var park = track.GetComponent<SomeGame.Track.TrackPark>();
            if (park == null) park = track.gameObject.AddComponent<SomeGame.Track.TrackPark>();
            Configure(park, ("track", track), ("plain", Mat("Plain")));
            // The helix inside a mountain: stacked loops, each car on its level.
            var helix = track.GetComponent<SomeGame.Track.TrackHelix>();
            if (helix == null) helix = track.gameObject.AddComponent<SomeGame.Track.TrackHelix>();
            Configure(helix, ("track", track), ("plain", Mat("Plain")));
            // Wooden platforms, holes in the ice, hatches.
            var platforms = track.GetComponent<SomeGame.Track.TrackPlatforms>();
            if (platforms == null) platforms = track.gameObject.AddComponent<SomeGame.Track.TrackPlatforms>();
            Configure(platforms, ("track", track), ("plain", Mat("Plain")));
            // The luxury chalets: runner carpets, sliding doors, pools, bowling, billiards, glass floors, windows.
            var chalets = track.GetComponent<SomeGame.Track.TrackChalets>();
            if (chalets == null) chalets = track.gameObject.AddComponent<SomeGame.Track.TrackChalets>();
            Configure(chalets, ("track", track), ("plain", Mat("Plain")), ("particleMaterial", particles));
            // The weather: storms (whiteout, gusts, lightning), snow twisters.
            var storm = track.GetComponent<SomeGame.Track.TrackStorm>();
            if (storm == null) storm = track.gameObject.AddComponent<SomeGame.Track.TrackStorm>();
            Configure(storm, ("track", track), ("particleMaterial", particles));
            // The town's life: Santa's sleigh dropping presents, snowball fights.
            var town = track.GetComponent<SomeGame.Track.TrackTown>();
            if (town == null) town = track.gameObject.AddComponent<SomeGame.Track.TrackTown>();
            Configure(town, ("track", track), ("sleighSprite", Sprite("SantaSleigh")), ("giftSprite", Sprite("GiftBox")), ("snowballSprite", Sprite("Snowball")), ("particleMaterial", particles));
            // The freestyle park: flumes, grind rails, the pinball rink, slalom gates.
            var rides = track.GetComponent<SomeGame.Track.TrackRides>();
            if (rides == null) rides = track.gameObject.AddComponent<SomeGame.Track.TrackRides>();
            Configure(rides, ("track", track), ("plain", Mat("Plain")), ("iceFloor", Mat("FloorIce")), ("particleMaterial", particles),
                ("bumperSprite", Sprite("PinballBumper")), ("gateRedSprite", Sprite("GateRed")), ("gateBlueSprite", Sprite("GateBlue")), ("glowSprite", Sprite("Glow")));
            if (hazards != null) Configure(hazards, ("sheepSprite", Sprite("Sheep")), ("boulderSprite", Sprite("Boulder")),
                ("catSprite", Sprite("Cat")), ("vacuumSprite", Sprite("Vacuum")), ("ballSprite", Sprite("BeachBall")), ("curlingSprite", Sprite("CurlingStone")), ("reindeerSprite", Sprite("Reindeer")), ("iceFloeSprite", Sprite("IceFloe")),
                ("mushroomPadSprite", Sprite("MushroomPad")), ("trampolineSprite", Sprite("Trampoline")));
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
