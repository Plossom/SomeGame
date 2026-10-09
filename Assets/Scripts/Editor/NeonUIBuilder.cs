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
    /// Builds the Night Neon UI for the Map and Race scenes from shared building blocks and wires
    /// it to the runtime components. Re-running replaces the scene's "UI" canvas.
    /// Units are canvas units of the 1170x2532 reference resolution.
    /// </summary>
    public static class NeonUIBuilder
    {
        const string Art = "Assets/Art/Neon/";
        const string Fonts = "Assets/Art/Fonts/ChakraPetch/";

        static TMP_FontAsset _medium, _semi, _bold, _italic;

        [MenuItem("SomeGame/Rebuild Neon UI (Map + Race)")]
        public static void RebuildAll()
        {
            EditorSceneManager.SaveOpenScenes();
            BuildMap();
            BuildRace();
        }

        // ================================================================== MAP

        public static void BuildMap()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
            LoadFonts();
            var old = GameObject.Find("UI");
            if (old != null) Object.DestroyImmediate(old);
            Camera.main.backgroundColor = NeonTheme.Background;

            var canvas = CreateCanvas();

            // Background: grid ground plus a dark landmass at the bottom.
            var bg = Stretch("Background", canvas);
            var bgImage = Img(bg, "Ground", Color.white);
            bgImage.type = Image.Type.Tiled;
            bgImage.pixelsPerUnitMultiplier = 3.3f;
            var land = At("Land", canvas, new Vector2(0.5f, 0f), new Vector2(0f, -380f), new Vector2(2000f, 1300f));
            Img(land, "Circle", NeonTheme.Panel);

            // Scrolling map.
            var scrollRect = Stretch("Map", canvas);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch("Viewport", scrollRect);
            viewport.gameObject.AddComponent<RectMask2D>();
            Img(viewport, null, new Color(0, 0, 0, 0), raycast: true);
            var content = Rt("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.sizeDelta = new Vector2(0f, 3000f);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var node = BuildNodeTemplate(content);

            // Readability scrims behind the fixed top and bottom areas.
            var topScrim = Rt("TopScrim", canvas);
            topScrim.anchorMin = new Vector2(0f, 1f); topScrim.anchorMax = new Vector2(1f, 1f); topScrim.pivot = new Vector2(0.5f, 1f);
            topScrim.sizeDelta = new Vector2(0f, 1050f);
            Img(topScrim, "FadeV", NeonTheme.Background);
            var bottomScrim = Rt("BottomScrim", canvas);
            bottomScrim.anchorMin = new Vector2(0f, 0f); bottomScrim.anchorMax = new Vector2(1f, 0f); bottomScrim.pivot = new Vector2(0.5f, 0f);
            bottomScrim.sizeDelta = new Vector2(0f, 700f);
            bottomScrim.localRotation = Quaternion.Euler(0f, 0f, 180f);
            bottomScrim.anchoredPosition = new Vector2(0f, 700f);
            Img(bottomScrim, "FadeV", NeonTheme.Background);

            var safe = SafeArea(canvas);
            var menuButton = SquareIconButton(safe, "MenuButton", new Vector2(0f, 1f), new Vector2(60f, -60f), "IconMenu", NeonTheme.Text, NeonTheme.Border);

            // Total stars pill (top right).
            var pill = At("Stars", safe, new Vector2(1f, 1f), new Vector2(-60f, -60f), new Vector2(250f, 138f));
            Img(pill, "Chip", NeonTheme.Panel, sliced: true);
            Img(Stretch("Outline", pill), "ChipOutline", NeonTheme.Lime, sliced: true);
            Img(At("Icon", pill, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(66f, 66f)), "IconStar", NeonTheme.Lime);
            var total = Txt(At("Count", pill, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(130f, 110f)), "0", 66, NeonTheme.Lime, _bold, TextAlignmentOptions.Right);

            // Selected race info.
            var info = At("Info", safe, new Vector2(0f, 1f), new Vector2(72f, -270f), new Vector2(1030f, 520f));
            var chapter = Txt(At("Chapter", info, new Vector2(0f, 1f), Vector2.zero, new Vector2(1030f, 60f)), "CHAPTER 01 // NEON GRID", 39, NeonTheme.Cyan, _bold, TextAlignmentOptions.Left);
            chapter.characterSpacing = 12f;
            var title = Txt(At("Title", info, new Vector2(0f, 1f), new Vector2(0f, -60f), new Vector2(1030f, 190f)), "GRAND <color=#FF3D7F>PRIX</color>", 150, NeonTheme.Text, _italic, TextAlignmentOptions.Left);
            title.enableAutoSizing = true; title.fontSizeMin = 80; title.fontSizeMax = 150;
            var chips = At("Chips", info, new Vector2(0f, 1f), new Vector2(0f, -270f), new Vector2(1030f, 84f));
            var row = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 24f; row.childControlWidth = true; row.childControlHeight = true; row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            var laps = Chip(chips, "3 LAPS");
            var rivals = Chip(chips, "3 RIVALS");
            var best = Chip(chips, "BEST —");

            // Bottom: hint, star times, START.
            var bottom = At("Bottom", safe, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1050f, 460f));
            var hint = Txt(At("Hint", bottom, new Vector2(0f, 0f), new Vector2(0f, 250f), new Vector2(620f, 60f)), "WIN TO EARN STARS", 36, NeonTheme.Dim, _semi, TextAlignmentOptions.Left);
            hint.characterSpacing = 8f;
            var timesRow = At("StarTimes", bottom, new Vector2(0f, 0f), new Vector2(0f, 150f), new Vector2(640f, 80f));
            var times = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var group = At($"Star{i + 1}", timesRow, new Vector2(0f, 0.5f), new Vector2(i * 215f, 0f), new Vector2(205f, 80f));
                for (int k = 0; k <= i; k++)
                    Img(At($"S{k}", group, new Vector2(0f, 0.5f), new Vector2(k * 40f, 26f), new Vector2(40f, 40f)), "IconStar", NeonTheme.Lime);
                times[i] = Txt(At("Time", group, new Vector2(0f, 0.5f), new Vector2(0f, -22f), new Vector2(205f, 50f)), "1:00.00", 42, NeonTheme.Muted, _bold, TextAlignmentOptions.Left);
            }
            var start = BigRoundButton(bottom, "StartButton", new Vector2(1f, 0f), new Vector2(0f, 0f), 380f, "START", out var startFill, out var startLabel, out var startRing);

            var map = canvas.gameObject.AddComponent<MapScreen>();
            var so = new SerializedObject(map);
            Set(so, "catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Data/Levels/LevelCatalog.asset"));
            Set(so, "scroll", scroll); Set(so, "content", content); Set(so, "nodeTemplate", node);
            Set(so, "totalStars", total); Set(so, "chapterLabel", chapter); Set(so, "titleLabel", title);
            Set(so, "lapsChip", laps); Set(so, "rivalsChip", rivals); Set(so, "bestChip", best);
            SetArray(so, "starTimes", times);
            Set(so, "hint", hint); Set(so, "startButton", start); Set(so, "startFill", startFill);
            Set(so, "startLabel", startLabel); Set(so, "startRing", startRing.gameObject);
            Set(so, "pathSprite", Sprite("Chip")); Set(so, "glowSprite", Sprite("Glow"));
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildMenu(canvas, menuButton, null, null);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static MapNode BuildNodeTemplate(RectTransform content)
        {
            var node = At("NodeTemplate", content, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(240f, 240f));
            var hit = Img(node, null, new Color(0, 0, 0, 0), raycast: true);
            var glow = Img(At("Glow", node, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f)), "Glow", NeonTheme.Lime);
            var ring = At("SelectionRing", node, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f));
            var ringImage = Img(ring, "RingThin", NeonTheme.Magenta);
            var pulse = ring.gameObject.AddComponent<UIPulse>();
            Configure(pulse, ("speed", 0.9f), ("ripple", true), ("fade", ringImage), ("scale", new Vector2(0.85f, 1.35f)), ("alpha", new Vector2(0f, 0.9f)));
            var fill = Img(At("Fill", node, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f)), "Diamond", NeonTheme.Panel);
            var outline = Img(At("Outline", node, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f)), "DiamondOutline", NeonTheme.Lime);
            var number = Txt(At("Number", node, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(160f, 120f)), "1", 78, NeonTheme.Lime, _bold);
            var lockIcon = Img(At("Lock", node, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f)), "IconLock", NeonTheme.Dim);
            var starsRow = At("Stars", node, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(210f, 64f));
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
                stars[i] = Img(At($"Star{i + 1}", starsRow, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 64f, 0f), new Vector2(56f, 56f)), "IconStar", NeonTheme.Lime);
            var req = At("Requirement", node, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(150f, 66f));
            Img(req, "Chip", NeonTheme.Background, sliced: true);
            Img(Stretch("Outline", req), "ChipOutline", NeonTheme.Cyan, sliced: true);
            var reqText = Txt(At("Count", req, new Vector2(0.5f, 0.5f), new Vector2(-18f, 0f), new Vector2(70f, 60f)), "5", 40, NeonTheme.Cyan, _bold, TextAlignmentOptions.Right);
            Img(At("Star", req, new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(36f, 36f)), "IconStar", NeonTheme.Cyan);

            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            node.gameObject.AddComponent<ButtonFeedback>();

            var mapNode = node.gameObject.AddComponent<MapNode>();
            var so = new SerializedObject(mapNode);
            Set(so, "button", button); Set(so, "fill", fill); Set(so, "outline", outline); Set(so, "glow", glow);
            Set(so, "selectionRing", ring.gameObject); Set(so, "number", number); Set(so, "lockIcon", lockIcon);
            SetArray(so, "stars", stars); Set(so, "requirement", req.gameObject); Set(so, "requirementText", reqText);
            so.ApplyModifiedPropertiesWithoutUndo();
            node.gameObject.SetActive(false);
            return mapNode;
        }

        // ================================================================== RACE

        public static void BuildRace()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Race.unity");
            LoadFonts();
            var old = GameObject.Find("UI");
            if (old != null) Object.DestroyImmediate(old);
            var race = Object.FindAnyObjectByType<RaceManager>();

            var canvas = CreateCanvas();

            // Floating joystick (whole screen is the touch area).
            var area = Stretch("JoystickArea", canvas);
            var baseRt = At("JoystickBase", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 360f));
            Img(At("Glow", baseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 520f)), "Glow", NeonTheme.WithAlpha(NeonTheme.Cyan, 0.18f));
            Img(baseRt, "RingThin", NeonTheme.WithAlpha(NeonTheme.Cyan, 0.75f));
            var knob = At("Knob", baseRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            Img(At("Glow", knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f)), "Glow", NeonTheme.WithAlpha(NeonTheme.Cyan, 0.6f));
            Img(knob, "Circle", NeonTheme.Cyan);
            baseRt.gameObject.SetActive(false);
            var joystick = area.gameObject.AddComponent<FloatingJoystick>();
            var jso = new SerializedObject(joystick);
            Set(jso, "canvas", canvas.GetComponent<Canvas>()); Set(jso, "background", baseRt); Set(jso, "knob", knob);
            jso.ApplyModifiedPropertiesWithoutUndo();
            var rso = new SerializedObject(race);
            Set(rso, "joystick", joystick);
            rso.ApplyModifiedPropertiesWithoutUndo();

            var safe = SafeArea(canvas);
            var menuButton = SquareIconButton(safe, "MenuButton", new Vector2(0f, 1f), new Vector2(48f, -48f), "IconMenu", NeonTheme.Text, NeonTheme.Border);

            // HUD bar: LAP | TIME | POS.
            var hud = Rt("Hud", safe);
            hud.anchorMin = new Vector2(0f, 1f); hud.anchorMax = new Vector2(1f, 1f); hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = new Vector2(87f, -48f); hud.sizeDelta = new Vector2(-270f, 138f);
            Img(hud, "Chip", NeonTheme.WithAlpha(NeonTheme.Panel, 0.88f), sliced: true);
            Img(Stretch("Outline", hud), "ChipOutline", NeonTheme.Border, sliced: true);
            var lap = HudColumn(hud, "Lap", 0f, 0.27f, "LAP", "1/3", 72, _bold);
            var time = HudColumn(hud, "Time", 0.27f, 0.68f, "TIME", "0:00.00", 72, _bold);
            var pos = HudColumn(hud, "Position", 0.68f, 1f, "POS", "P4", 84, _italic);
            var flash = Txt(At("LapFlash", safe, new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(1000f, 80f)), "LAP 0:21.56", 48, NeonTheme.Cyan, _bold);
            flash.characterSpacing = 6f;
            var warning = Txt(At("Warning", safe, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1050f, 240f)), "WRONG WAY", 110, NeonTheme.Magenta, _italic);
            warning.enableAutoSizing = true; warning.fontSizeMin = 50; warning.fontSizeMax = 110;
            var hudComp = hud.gameObject.AddComponent<RaceHud>();
            var hso = new SerializedObject(hudComp);
            Set(hso, "race", race); Set(hso, "lapLabel", lap); Set(hso, "timeLabel", time); Set(hso, "positionLabel", pos);
            Set(hso, "lapFlashLabel", flash); Set(hso, "warningLabel", warning);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Countdown.
            var cd = Stretch("Countdown", canvas);
            var cdLabel = Txt(At("Number", cd, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1000f, 520f)), "3", 420, NeonTheme.Text, _italic);
            var cdGlow = Img(At("Glow", cdLabel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 760f)), "Glow", NeonTheme.WithAlpha(NeonTheme.Cyan, 0.55f));
            cdGlow.transform.SetAsFirstSibling();
            cdLabel.gameObject.SetActive(false);
            var cdComp = cd.gameObject.AddComponent<CountdownView>();
            var cso = new SerializedObject(cdComp);
            Set(cso, "race", race); Set(cso, "label", cdLabel); Set(cso, "glow", cdGlow);
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
            var cap = Txt(At("Caption", col, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(260f, 36f)), caption, 26, NeonTheme.Dim, _semi);
            cap.characterSpacing = 10f;
            var val = Txt(At("Value", col, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(380f, 96f)), value, size, NeonTheme.Text, font);
            val.enableAutoSizing = true; val.fontSizeMin = 36; val.fontSizeMax = size;
            return val;
        }

        static void BuildResults(RectTransform canvas, RaceManager race)
        {
            var root = Stretch("FinishScreen", canvas);
            var panel = Stretch("Panel", root);
            Img(panel, null, NeonTheme.WithAlpha(NeonTheme.Background, 0.94f), raycast: true);
            var grid = Stretch("Grid", panel);
            var gridImage = Img(grid, "Ground", NeonTheme.WithAlpha(Color.white, 0.6f));
            gridImage.type = Image.Type.Tiled;
            gridImage.pixelsPerUnitMultiplier = 3.3f;

            // Neon slashes.
            var slash = At("Slash", panel, new Vector2(0.5f, 1f), new Vector2(0f, -700f), new Vector2(1700f, 18f));
            slash.localRotation = Quaternion.Euler(0f, 0f, -8f);
            Img(slash, null, NeonTheme.Magenta);
            Img(At("Glow", slash, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700f, 160f)), "Glow", NeonTheme.WithAlpha(NeonTheme.Magenta, 0.35f)).type = Image.Type.Simple;
            var slash2 = At("Slash2", panel, new Vector2(0.5f, 1f), new Vector2(0f, -760f), new Vector2(1700f, 6f));
            slash2.localRotation = Quaternion.Euler(0f, 0f, -8f);
            Img(slash2, null, NeonTheme.WithAlpha(NeonTheme.Cyan, 0.75f));

            var safe = SafeArea(panel);
            var raceLabel = Txt(At("Race", safe, new Vector2(1f, 1f), new Vector2(-60f, -90f), new Vector2(800f, 60f)), "CHAPTER 01 // NEON GRID", 36, NeonTheme.Cyan, _bold, TextAlignmentOptions.Right);
            raceLabel.characterSpacing = 10f;
            var subtitle = Txt(At("Subtitle", safe, new Vector2(0f, 1f), new Vector2(78f, -250f), new Vector2(1000f, 60f)), "FINISHED // WINNER", 40, NeonTheme.Muted, _semi, TextAlignmentOptions.Left);
            subtitle.characterSpacing = 10f;
            var position = Txt(At("Position", safe, new Vector2(0f, 1f), new Vector2(60f, -290f), new Vector2(800f, 380f)), "P1", 380, NeonTheme.Lime, _italic, TextAlignmentOptions.TopLeft);
            Pop(position.rectTransform, 0.05f);
            var tag = At("NewBest", safe, new Vector2(1f, 1f), new Vector2(-72f, -420f), new Vector2(330f, 84f));
            tag.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Img(tag, "Chip", NeonTheme.Magenta, sliced: true);
            Txt(Stretch("Label", tag), "NEW BEST LAP", 38, NeonTheme.Background, _bold);
            Pop(tag, 0.5f);

            var starsRow = At("Stars", safe, new Vector2(0.5f, 1f), new Vector2(0f, -1000f), new Vector2(900f, 330f));
            var stars = new Image[3];
            float[] sizes = { 230f, 300f, 230f };
            float[] xs = { -290f, 0f, 290f };
            for (int i = 0; i < 3; i++)
            {
                var s = At($"Star{i + 1}", starsRow, new Vector2(0.5f, 0.5f), new Vector2(xs[i], i == 1 ? 30f : 0f), new Vector2(sizes[i], sizes[i]));
                stars[i] = Img(s, "IconStar", NeonTheme.Lime);
                Pop(s, 0.25f + i * 0.18f);
            }

            var rows = At("Rows", safe, new Vector2(0.5f, 1f), new Vector2(0f, -1290f), new Vector2(1030f, 560f));
            var totalValue = ResultRow(rows, "Total", 0f, 190f, "TOTAL", 90, NeonTheme.Text, out _);
            var bestValue = ResultRow(rows, "BestLap", -215f, 160f, "BEST LAP", 66, NeonTheme.Cyan, out _);
            var nextValue = ResultRow(rows, "NextStar", -400f, 160f, "2ND STAR AT", 66, NeonTheme.Text, out var nextLabel);
            Pop(rows, 0.15f);

            var bottom = At("Bottom", safe, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1050f, 460f));
            var retry = SquareIconButton(bottom, "RetryButton", new Vector2(0f, 0f), new Vector2(0f, 80f), "IconRetry", NeonTheme.Cyan, NeonTheme.Cyan, 230f);
            var next = BigRoundButton(bottom, "NextButton", new Vector2(1f, 0f), new Vector2(0f, 0f), 400f, "NEXT", out _, out _, out _);

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

        static TMP_Text ResultRow(RectTransform parent, string name, float y, float height, string label, float valueSize, Color valueColor, out TMP_Text labelText)
        {
            var row = At(name, parent, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1030f, height));
            Img(row, "Chip", NeonTheme.Panel, sliced: true);
            Img(Stretch("Outline", row), "ChipOutline", NeonTheme.Border, sliced: true);
            labelText = Txt(At("Label", row, new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(560f, 60f)), label, 36, NeonTheme.Muted, _semi, TextAlignmentOptions.Left);
            labelText.characterSpacing = 10f;
            return Txt(At("Value", row, new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(460f, height)), "0:00.00", valueSize, valueColor, _bold, TextAlignmentOptions.Right);
        }

        // ================================================================== MENU

        static void BuildMenu(RectTransform canvas, Button openButton, RaceManager race, FloatingJoystick joystick)
        {
            var host = Stretch("GameMenu", canvas);
            var panel = Stretch("Panel", host);
            var dimRt = Stretch("Dim", panel);
            var dimImage = Img(dimRt, null, NeonTheme.WithAlpha(Color.black, 0.75f), raycast: true);
            var dim = dimRt.gameObject.AddComponent<Button>();
            dim.targetGraphic = dimImage;
            dim.transition = Selectable.Transition.None;

            var card = At("Card", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(930f, 1000f));
            Img(card, "Panel", NeonTheme.Panel, sliced: true, raycast: true);
            Img(Stretch("Outline", card), "PanelOutline", NeonTheme.Border, sliced: true);
            var accent = At("Accent", card, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(240f, 10f));
            Img(accent, "Chip", NeonTheme.Magenta, sliced: true);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(80, 80, 80, 90);
            layout.spacing = 34f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card.Find("Outline").gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            accent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var closeRt = At("Close", card, new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(110f, 110f));
            closeRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var closeImage = Img(closeRt, null, new Color(0, 0, 0, 0), raycast: true);
            Img(At("Icon", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f)), "IconClose", NeonTheme.Dim);
            var close = closeRt.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImage;
            closeRt.gameObject.AddComponent<ButtonFeedback>();

            var title = Txt(Layout(Rt("Title", card), 770f, 140f), "PAUSED", 120, NeonTheme.Text, _italic);
            var resume = MenuButton(card, "ResumeButton", "RESUME", NeonTheme.Lime, true, 180f);
            var restart = MenuButton(card, "RestartButton", "RESTART", NeonTheme.Cyan, false, 150f);
            var mainMenu = MenuButton(card, "MainMenuButton", "MAIN MENU", NeonTheme.Muted, false, 150f);

            var settings = Txt(Layout(Rt("SettingsTitle", card), 770f, 90f), "SETTINGS", 34, NeonTheme.Dim, _semi);
            settings.characterSpacing = 14f;

            var sound = Layout(Rt("Sound", card), 770f, 110f);
            Img(At("Icon", sound, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(84f, 84f)), "IconSound", NeonTheme.Cyan).rectTransform.pivot = new Vector2(0f, 0.5f);
            var slider = BuildSlider(sound);

            var joystickButton = MenuButton(card, "JoystickButton", "JOYSTICK   VISIBLE", NeonTheme.Text, false, 130f, out var joystickLabel, NeonTheme.Border, 44);

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
            track.anchorMin = new Vector2(0f, 0.42f); track.anchorMax = new Vector2(1f, 0.58f);
            Img(track, "Chip", NeonTheme.PanelRaised, sliced: true);
            var fillArea = Stretch("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.42f); fillArea.anchorMax = new Vector2(1f, 0.58f);
            fillArea.offsetMax = new Vector2(-40f, 0f);
            var fill = Stretch("Fill", fillArea);
            fill.offsetMax = new Vector2(40f, 0f);
            Img(fill, "Chip", NeonTheme.Cyan, sliced: true);
            var handleArea = Stretch("Handle Slide Area", root);
            handleArea.offsetMin = new Vector2(40f, 0f); handleArea.offsetMax = new Vector2(-40f, 0f);
            var handle = Rt("Handle", handleArea);
            handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(84f, 0f);
            Img(At("Glow", handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f)), "Glow", NeonTheme.WithAlpha(NeonTheme.Cyan, 0.5f));
            var knob = Img(At("Knob", handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f)), "Circle", NeonTheme.Text, raycast: true);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = knob;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
            return slider;
        }

        static Button MenuButton(RectTransform card, string name, string label, Color color, bool filled, float height) =>
            MenuButton(card, name, label, color, filled, height, out _, color, 58);

        static Button MenuButton(RectTransform card, string name, string label, Color color, bool filled, float height,
            out TMP_Text text, Color outlineColor, float fontSize)
        {
            var rt = Layout(Rt(name, card), 770f, height);
            var bg = Img(rt, "Chip", filled ? color : NeonTheme.Panel, sliced: true, raycast: true);
            if (!filled) Img(Stretch("Outline", rt), "ChipOutline", outlineColor, sliced: true);
            text = Txt(Stretch("Label", rt), label, fontSize, filled ? NeonTheme.Background : color, _bold);
            text.characterSpacing = 6f;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        // ================================================================== building blocks

        static void LoadFonts()
        {
            _medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + "ChakraPetch-Medium SDF.asset");
            _semi = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + "ChakraPetch-SemiBold SDF.asset");
            _bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + "ChakraPetch-Bold SDF.asset");
            _italic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + "ChakraPetch-BoldItalic SDF.asset");
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

        static Button SquareIconButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, string icon, Color iconColor, Color outline, float size = 138f)
        {
            var rt = At(name, parent, anchor, position, new Vector2(size, size));
            var bg = Img(rt, "Chip", NeonTheme.Panel, sliced: true, raycast: true);
            Img(Stretch("Outline", rt), "ChipOutline", outline, sliced: true);
            Img(At("Icon", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f)), icon, iconColor);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        static Button BigRoundButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, float size, string label,
            out Image fill, out TMP_Text text, out RectTransform ring)
        {
            var rt = At(name, parent, anchor, position, new Vector2(size * 1.18f, size * 1.18f));
            Img(At("Glow", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 1.9f, size * 1.9f)), "Glow", NeonTheme.WithAlpha(NeonTheme.Magenta, 0.55f));
            ring = At("Ring", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 1.18f, size * 1.18f));
            var ringImage = Img(ring, "RingThin", NeonTheme.Magenta);
            var pulse = ring.gameObject.AddComponent<UIPulse>();
            Configure(pulse, ("speed", 0.7f), ("ripple", true), ("fade", ringImage), ("scale", new Vector2(0.92f, 1.22f)), ("alpha", new Vector2(0f, 0.8f)));
            fill = Img(At("Fill", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size)), "Circle", NeonTheme.Magenta, raycast: true);
            text = Txt(At("Label", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size * 0.5f)), label, size * 0.24f, NeonTheme.Background, _italic);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        static TMP_Text Chip(RectTransform parent, string text)
        {
            var rt = Rt("Chip", parent);
            Img(rt, "Chip", NeonTheme.WithAlpha(NeonTheme.Panel, 0.9f), sliced: true);
            Img(Stretch("Outline", rt), "ChipOutline", NeonTheme.Border, sliced: true);
            var fit = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(30, 30, 10, 10);
            fit.childControlWidth = true; fit.childControlHeight = true;
            rt.Find("Outline").gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var label = Txt(Rt("Label", rt), text, 36, NeonTheme.Muted, _semi);
            label.characterSpacing = 6f;
            return label;
        }

        static void Pop(RectTransform rt, float delay)
        {
            var pop = rt.gameObject.AddComponent<UIPopIn>();
            Configure(pop, ("delay", delay));
        }

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
            // Sliced art drawn at half density: rounder corners and outlines thick enough for a phone screen.
            if (sliced) image.pixelsPerUnitMultiplier = 0.5f;
            image.preserveAspect = !sliced && sprite != null && sprite != "Ground" && sprite != "FadeV" && sprite != "Glow";
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
