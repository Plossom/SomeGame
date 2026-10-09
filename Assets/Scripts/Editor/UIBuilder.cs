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
            var world = At("World", map, new Vector2(0.5f, 0.5f), Vector2.zero, MapLayout.Size);
            Img(Stretch("Background", world), "MapBackground", Color.white).preserveAspect = false;
            var node = BuildNode(world);
            var car = At("Car", world, Vector2.zero, Vector2.zero, new Vector2(64f, 112f));
            car.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", car, new Vector2(0.5f, 0.5f), new Vector2(6f, -8f), new Vector2(70f, 120f)), "CarShadow", new Color(0f, 0f, 0f, 0.45f)).preserveAspect = false;
            Img(Stretch("Body", car), "Car", Theme.Orange).preserveAspect = false;
            Img(Stretch("Details", car), "CarDetails", Color.white).preserveAspect = false;

            var safe = SafeArea(canvas);
            var menuButton = SquareButton(safe, "MenuButton", new Vector2(0f, 1f), new Vector2(60f, -50f), "IconMenu", 140f);

            var starPill = At("Stars", safe, new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(250f, 140f));
            Img(starPill, "Round", Theme.Ink, sliced: true);
            Img(At("Icon", starPill, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(70f, 70f)), "IconStar", Theme.Amber);
            var total = Txt(At("Count", starPill, new Vector2(1f, 0.5f), new Vector2(-36f, -2f), new Vector2(130f, 120f)), "0", 78, Theme.Amber, _condItalic, TextAlignmentOptions.Right);

            var header = At("Header", safe, new Vector2(0f, 1f), new Vector2(72f, -250f), new Vector2(1030f, 420f));
            var chapter = Txt(At("Chapter", header, new Vector2(0f, 1f), Vector2.zero, new Vector2(1030f, 60f)), "ALPINE CUP · RACE 1", 44, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            chapter.characterSpacing = 8f;
            var title = Txt(At("Title", header, new Vector2(0f, 1f), new Vector2(-6f, -52f), new Vector2(1030f, 190f)), "MEADOW RUN", 176, Theme.Ink, _black, TextAlignmentOptions.TopLeft);
            title.enableAutoSizing = true; title.fontSizeMin = 100; title.fontSizeMax = 176;
            var chips = At("Chips", header, new Vector2(0f, 1f), new Vector2(0f, -258f), new Vector2(1030f, 82f));
            var chipRow = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipRow.spacing = 22f; chipRow.childControlWidth = true; chipRow.childControlHeight = true;
            chipRow.childForceExpandWidth = false; chipRow.childForceExpandHeight = true; chipRow.childAlignment = TextAnchor.MiddleLeft;
            var laps = Chip(chips, "2 laps");
            var rivals = Chip(chips, "3 rivals");
            var best = Chip(chips, "Best —");

            // Bottom: hint, star times, START.
            var bottom = At("Bottom", safe, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1050f, 480f));
            var hintRt = At("Hint", bottom, new Vector2(0f, 0f), new Vector2(0f, 330f), new Vector2(600f, 80f));
            Img(hintRt, "Pill", Theme.WithAlpha(Theme.Cream, 0.95f), sliced: true);
            var hint = Txt(Stretch("Label", hintRt), "Win to open the next race", 38, Theme.Ink, _bold);
            hint.enableAutoSizing = true; hint.fontSizeMin = 26; hint.fontSizeMax = 38;
            var timesRt = At("StarTimes", bottom, new Vector2(0f, 0f), new Vector2(0f, 190f), new Vector2(600f, 110f));
            Img(timesRt, "Round", Theme.Ink, sliced: true);
            var times = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var group = At($"Star{i + 1}", timesRt, new Vector2(0f, 0.5f), new Vector2(22f + i * 194f, 0f), new Vector2(190f, 100f));
                group.pivot = new Vector2(0f, 0.5f);
                for (int k = 0; k <= i; k++)
                    Img(At($"S{k}", group, new Vector2(0f, 1f), new Vector2(6f + k * 30f, -12f), new Vector2(30f, 30f)), "IconStar", Theme.Amber).rectTransform.pivot = new Vector2(0f, 1f);
                times[i] = Txt(At("Time", group, new Vector2(0f, 0f), new Vector2(4f, 4f), new Vector2(186f, 58f)), "1:00.00", 48, Theme.Cream, _cond, TextAlignmentOptions.BottomLeft);
                times[i].rectTransform.pivot = new Vector2(0f, 0f);
            }
            var start = BigRoundButton(bottom, "StartButton", new Vector2(1f, 0f), new Vector2(0f, 10f), 380f, "START",
                out var startFace, out var startShadow, out var startLabel, out var startRing);

            BuildMenu(canvas, menuButton, null, null);

            var screen = canvas.gameObject.AddComponent<MapScreen>();
            var so = new SerializedObject(screen);
            Set(so, "catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Data/Levels/LevelCatalog.asset"));
            Set(so, "world", world); Set(so, "nodeTemplate", node); Set(so, "car", car); Set(so, "totalStars", total);
            Set(so, "chapterLabel", chapter); Set(so, "titleLabel", title);
            Set(so, "lapsChip", laps); Set(so, "rivalsChip", rivals); Set(so, "bestChip", best);
            SetArray(so, "starTimes", times); Set(so, "hint", hint);
            Set(so, "startButton", start); Set(so, "startFace", startFace); Set(so, "startShadow", startShadow);
            Set(so, "startLabel", startLabel); Set(so, "startRing", startRing.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MapScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MapScenePath, true),
                new EditorBuildSettingsScene(RaceScenePath, true),
            };
        }

        static MapNode BuildNode(RectTransform world)
        {
            var root = At("NodeTemplate", world, Vector2.zero, Vector2.zero, new Vector2(170f, 170f));
            root.pivot = new Vector2(0.5f, 0.5f);
            var selection = At("Selection", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f));
            var selImage = Img(selection, "RingDashed", Color.white);
            Configure(selection.gameObject.AddComponent<UIPulse>(), ("speed", 0.6f), ("fade", selImage), ("scale", new Vector2(0.94f, 1.08f)), ("alpha", new Vector2(0.55f, 1f)));
            var shadow = Img(At("Shadow", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(150f, 150f)), "Circle", Theme.Ink);
            var face = Img(At("Face", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "Circle", Theme.White, raycast: true);
            var ring = Img(At("Ring", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "Ring", Theme.Ink);
            var dashed = Img(At("Dashed", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f)), "RingDashed", Theme.StoneDark);
            var number = Txt(At("Number", root, new Vector2(0.5f, 0.5f), new Vector2(-2f, 0f), new Vector2(150f, 150f)), "1", 92, Theme.Ink, _black);
            var lockIcon = Img(At("Lock", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66f, 66f)), "IconLock", Theme.StoneDark);

            var starsPill = At("Stars", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(170f, 60f));
            Img(starsPill, "Pill", Theme.Ink, sliced: true);
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
                stars[i] = Img(At($"Star{i + 1}", starsPill, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 46f, 0f), new Vector2(40f, 40f)), "IconStar", Theme.Amber);

            var req = At("Requirement", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 128f), new Vector2(140f, 64f));
            Img(req, "Pill", Theme.Ink, sliced: true);
            var reqText = Txt(At("Count", req, new Vector2(0.5f, 0.5f), new Vector2(-20f, 0f), new Vector2(70f, 60f)), "2", 46, Theme.Amber, _condItalic, TextAlignmentOptions.Right);
            Img(At("Icon", req, new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(38f, 38f)), "IconStar", Theme.Amber);

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
            Set(so, "starsPill", starsPill.gameObject); SetArray(so, "stars", stars);
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
            BuildMenu(canvas, menuButton, race, joystick);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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
            var band = At("Checker", safe, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1400f, 104f));
            var checker = Img(band, "Checker", Color.white);
            checker.type = Image.Type.Tiled; checker.preserveAspect = false; checker.pixelsPerUnitMultiplier = 1.2f;

            var raceLabel = Txt(At("Race", safe, new Vector2(1f, 1f), new Vector2(-60f, -90f), new Vector2(800f, 60f)), "ALPINE CUP · RACE 1", 42, Theme.Rust, _condItalic, TextAlignmentOptions.Right);
            raceLabel.characterSpacing = 8f;
            var position = Txt(At("Position", safe, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(1100f, 420f)), "1ST", 400, Theme.Orange, _black);
            position.fontSharedMaterial = _blackShadow;
            Pop(position.rectTransform, 0.05f);
            var subtitle = Txt(At("Subtitle", safe, new Vector2(0.5f, 1f), new Vector2(0f, -770f), new Vector2(1000f, 110f)), "PLACE", 100, Theme.Ink, _black);
            subtitle.characterSpacing = 10f;
            var tag = At("NewBest", safe, new Vector2(1f, 1f), new Vector2(-80f, -420f), new Vector2(330f, 90f));
            tag.localRotation = Quaternion.Euler(0f, 0f, 8f);
            Img(tag, "Pill", Theme.Ink, sliced: true);
            Txt(Stretch("Label", tag), "NEW BEST LAP", 40, Theme.Amber, _condItalic);
            Pop(tag, 0.5f);

            var starsRow = At("Stars", safe, new Vector2(0.5f, 1f), new Vector2(0f, -1000f), new Vector2(900f, 330f));
            var stars = new Image[3];
            float[] sizes = { 230f, 300f, 230f };
            float[] xs = { -280f, 0f, 280f };
            for (int i = 0; i < 3; i++)
            {
                var s = At($"Star{i + 1}", starsRow, new Vector2(0.5f, 0.5f), new Vector2(xs[i], i == 1 ? 34f : 0f), new Vector2(sizes[i], sizes[i]));
                Img(At("Outline", s, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(sizes[i] * 1.14f, sizes[i] * 1.14f)), "IconStar", Theme.Ink);
                stars[i] = Img(At("Fill", s, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sizes[i], sizes[i])), "IconStar", Theme.Amber);
                Pop(s, 0.25f + i * 0.18f);
            }

            var card = At("Card", safe, new Vector2(0.5f, 1f), new Vector2(0f, -1330f), new Vector2(1030f, 480f));
            Img(card, "Round", Theme.Ink, sliced: true);
            var totalValue = ResultRow(card, "Total", -26f, "Total time", 96, Color.white, out _);
            Img(At("Divider", card, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(950f, 4f)), null, Theme.WithAlpha(Theme.Cream, 0.15f));
            var bestValue = ResultRow(card, "BestLap", -200f, "Best lap", 66, Theme.Amber, out _);
            var nextValue = ResultRow(card, "NextStar", -330f, "2nd star at", 66, Color.white, out var nextLabel);
            Pop(card, 0.15f);

            var bottom = At("Bottom", safe, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1050f, 460f));
            var retry = SquareButton(bottom, "RetryButton", new Vector2(0f, 0f), new Vector2(0f, 80f), "IconRetry", 200f, white: true);
            var next = BigRoundButton(bottom, "NextButton", new Vector2(1f, 0f), new Vector2(0f, 0f), 400f, "NEXT", out _, out _, out _, out _);

            panel.gameObject.SetActive(false);
            var finish = root.gameObject.AddComponent<FinishScreen>();
            var so = new SerializedObject(finish);
            Set(so, "race", race); Set(so, "panel", panel.gameObject); Set(so, "raceLabel", raceLabel);
            Set(so, "positionLabel", position); Set(so, "subtitle", subtitle); Set(so, "newBestTag", tag.gameObject);
            SetArray(so, "stars", stars); Set(so, "totalValue", totalValue); Set(so, "bestLapValue", bestValue);
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

            var menu = host.gameObject.AddComponent<GameMenu>();
            var so = new SerializedObject(menu);
            Set(so, "race", race); Set(so, "joystick", joystick);
            Set(so, "openButton", openButton); Set(so, "panel", panel.gameObject); Set(so, "closeArea", dim);
            Set(so, "closeButton", close); Set(so, "title", title);
            Set(so, "resumeButton", resume); Set(so, "restartButton", restart); Set(so, "mainMenuButton", mainMenu);
            Set(so, "volumeSlider", slider); Set(so, "joystickButton", joystickButton); Set(so, "joystickLabel", joystickLabel);
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
            Img(rt, "Round", Color.white, sliced: true);
            var fit = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(30, 30, 8, 8);
            fit.childControlWidth = true; fit.childControlHeight = true;
            var label = Txt(Rt("Label", rt), text, 40, Theme.Ink, _bold);
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
                    case bool b: p.boolValue = b; break;
                    case Vector2 v: p.vector2Value = v; break;
                    case Object o: p.objectReferenceValue = o; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
