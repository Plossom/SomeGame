using SomeGame.Online;
using SomeGame.Race;
using SomeGame.UI;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The online screens: on the map the ONLINE button, the create / join card and the room (code,
    /// race, eight seats, colours, START / READY); in the race the "waiting for racers" banner and the
    /// live results. Also saves the room's network prefab (Resources/Online/OnlineRoom).
    /// </summary>
    public static partial class UIBuilder
    {
        const string RoomPrefabPath = "Assets/Resources/Online/OnlineRoom.prefab";

        [MenuItem("SomeGame/Build Online Room Prefab")]
        public static void BuildRoomPrefab()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Online");
            var go = new GameObject("OnlineRoom");
            go.AddComponent<NetworkObject>();
            go.AddComponent<OnlineRoom>();
            PrefabUtility.SaveAsPrefabAsset(go, RoomPrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
        }

        // ================================================================== map: lobby

        static void BuildOnlineLobby(RectTransform canvas, RectTransform safe)
        {
            // The entry: a square button with two people, top right between the stars / trophies counter and
            // the cup dropdown (same column, same width as the dropdown).
            // As wide as the counters above, flatter; the people in the cup's star colour.
            var openRt = At("OnlineButton", safe, new Vector2(1f, 1f), new Vector2(-60f, -216f), new Vector2(140f, 94f));
            Img(At("Shadow", openRt, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(140f, 82f)), "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            var openFace = At("Face", openRt, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(140f, 82f));
            var openBg = Img(openFace, "Round", Theme.Ink, sliced: true, raycast: true);
            Img(At("Icon", openFace, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f)), "IconPeople", Theme.Amber);
            var open = openRt.gameObject.AddComponent<Button>();
            open.targetGraphic = openBg; open.transition = Selectable.Transition.None;
            openRt.gameObject.AddComponent<ButtonFeedback>();

            // Its own canvas over the map's: the map recolours its canvas per cup, the lobby styles itself.
            var lobbyCanvas = CreateCanvas();
            lobbyCanvas.name = "OnlineUI";
            lobbyCanvas.GetComponent<Canvas>().sortingOrder = 20;
            var host = Stretch("OnlineLobby", lobbyCanvas);
            var screen = host.gameObject.AddComponent<OnlineLobbyScreen>();
            BuildEntryCard(host, out var entry);
            BuildRoom(host, out var room);

            var so = new SerializedObject(screen);
            Set(so, "openButton", open);
            foreach (var (name, value) in entry) Set(so, name, value);
            foreach (var (name, value) in room) Set(so, name, value);
            so.ApplyModifiedPropertiesWithoutUndo();
            WireLobbyArrays(screen);
        }

        // The card: name, CREATE ROOM, the code boxes and JOIN.
        static void BuildEntryCard(RectTransform host, out System.Collections.Generic.List<(string, Object)> refs)
        {
            refs = new();
            var panel = Stretch("Entry", host);
            var dimRt = Stretch("Dim", panel);
            var dimImage = Img(dimRt, null, Theme.WithAlpha(Theme.Ink, 0.7f), raycast: true);
            var dim = dimRt.gameObject.AddComponent<Button>(); dim.targetGraphic = dimImage; dim.transition = Selectable.Transition.None;
            var safe = SafeArea(panel);

            var card = At("Card", safe, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1000f, 1650f));
            card.pivot = new Vector2(0.5f, 0.5f);
            var cardShadow = Stretch("Shadow", card); cardShadow.offsetMin = cardShadow.offsetMax = new Vector2(0f, -20f);
            Img(cardShadow, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Face", card), "Round", Theme.Cream, sliced: true, raycast: true);

            var closeRt = At("Close", card, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(110f, 110f));
            var closeImage = Img(closeRt, null, new Color(0, 0, 0, 0), raycast: true);
            Img(At("Icon", closeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f)), "IconClose", Theme.Ink);
            var close = closeRt.gameObject.AddComponent<Button>(); close.targetGraphic = closeImage;
            closeRt.gameObject.AddComponent<ButtonFeedback>();

            var chapter = Txt(At("Chapter", card, new Vector2(0f, 1f), new Vector2(70f, -64f), new Vector2(700f, 50f)), "ONLINE · UP TO 8 RACERS", 40, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            chapter.characterSpacing = 6f;
            var title = Txt(At("Title", card, new Vector2(0f, 1f), new Vector2(66f, -112f), new Vector2(860f, 130f)), "PLAY WITH FRIENDS", 110, Theme.Ink, _black, TextAlignmentOptions.Left);
            title.enableAutoSizing = true; title.fontSizeMin = 70; title.fontSizeMax = 110;

            // Three little cars swinging along side by side (animated by the screen).
            var demo = At("Cars", card, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(860f, 200f));
            demo.pivot = new Vector2(0.5f, 0.5f);
            var road = At("Road", demo, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 170f));
            Img(road, "Pill", Theme.WithAlpha(Theme.Ink, 0.08f), sliced: true);
            var cars = new RectTransform[3];
            string[] colours = { "FF7A1A", "3D7DD8", "F2C230" };
            for (int i = 0; i < cars.Length; i++)
            {
                var c = At($"Car{i + 1}", demo, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 210f, 0f), new Vector2(84f, 147f));
                c.pivot = new Vector2(0.5f, 0.5f);
                Img(At("Shadow", c, new Vector2(0.5f, 0.5f), new Vector2(8f, -10f), new Vector2(90f, 156f)), "CarShadow", new Color(0f, 0f, 0f, 0.35f)).preserveAspect = false;
                Img(Stretch("Body", c), "Car", Hex(colours[i])).preserveAspect = false;
                Img(Stretch("Details", c), "CarDetails", Color.white).preserveAspect = false;
                cars[i] = c;
            }

            // Your name, with a die for a random one.
            var nameCaption = Txt(At("NameCaption", card, new Vector2(0f, 1f), new Vector2(115f, -540f), new Vector2(600f, 50f)), "YOUR NAME", 38, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            nameCaption.characterSpacing = 8f;
            var nameField = InputField(card, "NameField", new Vector2(115f, -600f), new Vector2(620f, 130f), 64, TextAlignmentOptions.Left, 12, true);
            var dice = SquareButton(card, "DiceButton", new Vector2(0f, 1f), new Vector2(755f, -600f), "IconDice", 130f);

            var create = WideButton(card, "CreateButton", new Vector2(0f, -780f), new Vector2(770f, 170f), "CREATE ROOM", Theme.Orange, Theme.OrangeDeep, Color.white, 76, _black);

            // ---- or join a friend ----
            var or = Txt(At("Or", card, new Vector2(0.5f, 1f), new Vector2(0f, -1000f), new Vector2(770f, 50f)), "OR JOIN A FRIEND", 38, Theme.StoneDark, _condItalic);
            or.characterSpacing = 8f;
            Img(At("LineL", card, new Vector2(0.5f, 1f), new Vector2(-300f, -1022f), new Vector2(170f, 6f)), "Pill", Theme.WithAlpha(Theme.StoneDark, 0.4f), sliced: true);
            Img(At("LineR", card, new Vector2(0.5f, 1f), new Vector2(300f, -1022f), new Vector2(170f, 6f)), "Pill", Theme.WithAlpha(Theme.StoneDark, 0.4f), sliced: true);

            // Six letter boxes over a hidden input field.
            var codeRow = At("Code", card, new Vector2(0.5f, 1f), new Vector2(0f, -1080f), new Vector2(770f, 150f));
            var boxes = new Image[6];
            var letters = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                var box = At($"Box{i + 1}", codeRow, new Vector2(0f, 0.5f), new Vector2(58f + i * 131f, 0f), new Vector2(116f, 150f));
                box.pivot = new Vector2(0.5f, 0.5f);
                boxes[i] = Img(At("Shadow", box, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(116f, 150f)), "Round", Theme.Ink, sliced: true);
                Img(Stretch("Face", box), "Round", Color.white, sliced: true);
                letters[i] = Txt(Stretch("Letter", box), "", 92, Theme.Ink, _black);
            }
            var codeField = InputField(codeRow, "Field", Vector2.zero, Vector2.zero, 10, TextAlignmentOptions.Center, 6, false);
            var codeFieldRt = (RectTransform)codeField.transform;
            codeFieldRt.anchorMin = Vector2.zero; codeFieldRt.anchorMax = Vector2.one; codeFieldRt.offsetMin = codeFieldRt.offsetMax = Vector2.zero;
            codeField.contentType = TMP_InputField.ContentType.Alphanumeric;
            codeField.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            codeField.shouldHideMobileInput = true;
            codeField.textComponent.color = new Color(0f, 0f, 0f, 0f);
            codeField.caretColor = new Color(0f, 0f, 0f, 0f); codeField.customCaretColor = true;
            codeField.selectionColor = new Color(0f, 0f, 0f, 0f);
            codeField.transform.Find("Face").GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var join = WideButton(card, "JoinButton", new Vector2(0f, -1290f), new Vector2(770f, 150f), "JOIN", Color.white, Theme.Ink, Theme.Ink, 66, _black);

            var status = Txt(At("Status", card, new Vector2(0.5f, 1f), new Vector2(30f, -1478f), new Vector2(800f, 60f)), "", 40, Theme.InkSoft, _condItalic);
            status.characterSpacing = 4f;
            // While a room opens: a bar along the bottom of the card with the buggy riding on it (like loading).
            var bar = At("Progress", card, new Vector2(0.5f, 1f), new Vector2(0f, -1520f), new Vector2(800f, 40f));
            var barTrack = Rounded(Img(bar, "Round", Theme.CreamDark, sliced: true), 20f);
            var fillRt = Stretch("Fill", bar);
            fillRt.offsetMin = new Vector2(7f, 7f); fillRt.offsetMax = new Vector2(-7f, -7f);
            var barFill = Img(fillRt, "Round", Theme.Orange, sliced: true);
            Rounded(barFill, 13f);
            barFill.type = Image.Type.Filled; barFill.fillMethod = Image.FillMethod.Horizontal; barFill.fillAmount = 0.3f; barFill.preserveAspect = false;
            var carTrack = Rt("CarTrack", bar);
            carTrack.anchorMin = Vector2.zero; carTrack.anchorMax = new Vector2(1f, 0f); carTrack.sizeDelta = Vector2.zero;
            var barCar = At("Car", carTrack, new Vector2(0f, 0f), new Vector2(0f, 64f), new Vector2(46f, 80f));
            barCar.pivot = new Vector2(0.5f, 0.5f); barCar.localRotation = Quaternion.Euler(0f, 0f, -90f);
            Img(Stretch("Body", barCar), "Car", Theme.Orange).preserveAspect = false;
            Img(Stretch("Details", barCar), "CarDetails", Color.white).preserveAspect = false;
            var barLabel = Txt(At("Label", bar, new Vector2(0.5f, 0f), new Vector2(0f, -60f), new Vector2(800f, 50f)), "OPENING A ROOM  40%", 36, Theme.InkSoft, _condItalic);
            barLabel.characterSpacing = 4f;
            bar.gameObject.SetActive(false);
            create.gameObject.AddComponent<CanvasGroup>();
            join.gameObject.AddComponent<CanvasGroup>();

            Configure(panel.gameObject.AddComponent<PopIn>(), ("card", card), ("backdrop", dimImage));
            panel.gameObject.SetActive(false);
            refs.AddRange(new (string, Object)[]
            {
                ("entryPanel", panel.gameObject), ("entryClose", close), ("entryDim", dim), ("nameField", nameField), ("diceButton", dice),
                ("createButton", create), ("joinButton", join), ("codeField", codeField), ("statusLabel", status), ("progressBar", bar.gameObject), ("progressFill", barFill), ("progressCar", barCar), ("progressLabel", barLabel),
            });
            _codeLetters = letters; _codeBoxes = boxes; _demoCars = cars;
        }

        static TMP_Text[] _codeLetters;
        static Image[] _codeBoxes;
        static RectTransform[] _demoCars;

        // The room: code, race, seats, colours, START / READY.
        static void BuildRoom(RectTransform host, out System.Collections.Generic.List<(string, Object)> refs)
        {
            refs = new();
            var panel = Stretch("Room", host);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            Img(panel, null, Theme.Cream, raycast: true);
            // The meadow along the bottom, like the loading screen.
            Img(At("HillBack", panel, new Vector2(0.5f, 0f), new Vector2(-300f, -1150f), new Vector2(2000f, 1500f)), "Circle", Hex("9CC383")).preserveAspect = false;
            Img(At("HillFront", panel, new Vector2(0.5f, 0f), new Vector2(260f, -1330f), new Vector2(2200f, 1600f)), "Circle", Hex("7FB06C")).preserveAspect = false;
            var safe = SafeArea(panel);
            var content = Stretch("Content", safe);

            // Top: leave, title.
            var leave = SquareButton(content, "LeaveButton", new Vector2(0f, 1f), new Vector2(60f, -50f), "IconPlay", 140f);
            leave.transform.Find("Face/Icon").localScale = new Vector3(-1f, 1f, 1f);
            var titleRt = Rt("Title", content);
            titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f); titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(216f, -196f); titleRt.offsetMax = new Vector2(-216f, -50f);
            var title = Txt(titleRt, "ONLINE ROOM", 110, Theme.Rust, _condItalic);
            title.characterSpacing = 6f; title.enableAutoSizing = true; title.fontSizeMin = 70; title.fontSizeMax = 110;

            // The code plate: tap to copy.
            var plate = At("CodePlate", content, new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(1050f, 200f));
            var plateShadow = Stretch("Shadow", plate); plateShadow.offsetMin = plateShadow.offsetMax = new Vector2(0f, -14f);
            Img(plateShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Face", plate), "Round", Theme.Ink, sliced: true);
            var caption = Txt(At("Caption", plate, new Vector2(0f, 1f), new Vector2(44f, -26f), new Vector2(500f, 44f)), "ROOM CODE · SHARE IT", 34, Theme.Stone, _condItalic, TextAlignmentOptions.Left);
            caption.characterSpacing = 6f;
            var code = Txt(At("Code", plate, new Vector2(0f, 0f), new Vector2(40f, 16f), new Vector2(700f, 130f)), "ABC123", 118, Theme.Amber, _black, TextAlignmentOptions.BottomLeft);
            code.rectTransform.pivot = Vector2.zero; code.characterSpacing = 18f;
            var copy = SquareButton(plate, "CopyButton", new Vector2(1f, 0.5f), new Vector2(-36f, 6f), "IconCopy", 128f);
            ((RectTransform)copy.transform).pivot = new Vector2(1f, 0.5f);
            foreach (var g in new[] { "Shadow", "Face" }) copy.transform.Find(g).GetComponent<Image>().color = g == "Face" ? Theme.Cream : Theme.StoneDark;
            copy.transform.Find("Face/Icon").GetComponent<Image>().color = Theme.Ink;
            var copied = Txt(At("Copied", plate, new Vector2(1f, 1f), new Vector2(-180f, -6f), new Vector2(300f, 50f)), "COPIED!", 40, Theme.Amber, _condItalic, TextAlignmentOptions.Right);
            copied.rectTransform.pivot = new Vector2(1f, 1f);
            copied.alpha = 0f;

            // The race: cup tabs (and RANDOM) along the top, the track from above, its name, laps and
            // difficulty; the host picks the cup and pages through its races with the arrows.
            var raceCard = At("Race", content, new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(1050f, 430f));
            var raceShadow = Stretch("Shadow", raceCard); raceShadow.offsetMin = raceShadow.offsetMax = new Vector2(0f, -14f);
            Img(raceShadow, "Round", Shade(Theme.Ink, 0.6f), sliced: true);
            Img(Stretch("Face", raceCard), "Round", Theme.Ink, sliced: true);
            var frame = At("Track", raceCard, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(200f, 386f));
            frame.pivot = new Vector2(0f, 0.5f);
            Img(frame, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            frame.gameObject.AddComponent<RectMask2D>();
            var picture = Img(Stretch("Picture", frame), null, Color.white);
            picture.rectTransform.offsetMin = new Vector2(8f, 8f); picture.rectTransform.offsetMax = new Vector2(-8f, -8f);
            picture.preserveAspect = true;

            var tabs = At("CupTabs", raceCard, new Vector2(0f, 1f), new Vector2(256f, -26f), new Vector2(768f, 66f));
            var tabsLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 12f; tabsLayout.childAlignment = TextAnchor.MiddleLeft;
            tabsLayout.childControlWidth = true; tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = false; tabsLayout.childForceExpandHeight = true;
            var tabRt = Rt("Tab", tabs);
            var tabFace = Rounded(Img(tabRt, "Round", Theme.InkSoft, sliced: true, raycast: true), 14f);
            var tabLayout = tabRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.padding = new RectOffset(20, 22, 6, 6); tabLayout.spacing = 8f; tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = true; tabLayout.childControlHeight = true; tabLayout.childForceExpandWidth = false;
            var tabIcon = Rt("Icon", tabRt);
            Img(tabIcon, "IconDice", Theme.Stone);
            var tabIconSize = tabIcon.gameObject.AddComponent<LayoutElement>(); tabIconSize.preferredWidth = 38f; tabIconSize.preferredHeight = 38f;
            Txt(Rt("Label", tabRt), "VALLEY CUP", 34, Theme.Stone, _condItalic).characterSpacing = 3f;
            var tab = tabRt.gameObject.AddComponent<Button>(); tab.targetGraphic = tabFace; tab.transition = Selectable.Transition.None;
            tabRt.gameObject.AddComponent<ButtonFeedback>();

            var raceTitle = Txt(At("Name", raceCard, new Vector2(0f, 1f), new Vector2(256f, -106f), new Vector2(760f, 110f)), "SUNDAY LOOP", 92, Theme.Cream, _black, TextAlignmentOptions.Left);
            raceTitle.enableAutoSizing = true; raceTitle.fontSizeMin = 56; raceTitle.fontSizeMax = 92;
            var details = Txt(At("Details", raceCard, new Vector2(0f, 1f), new Vector2(260f, -222f), new Vector2(420f, 50f)), "3 LAPS", 40, Theme.Stone, _cond, TextAlignmentOptions.Left);
            var pips = new Image[5];
            for (int i = 0; i < 5; i++)
                pips[i] = Rounded(Img(At($"Pip{i + 1}", raceCard, new Vector2(0f, 1f), new Vector2(440f + i * 62f, -234f), new Vector2(52f, 26f)), "Round", Theme.Orange, sliced: true), 6f);
            var prev = SquareButton(raceCard, "PrevRace", new Vector2(0f, 0f), new Vector2(260f, 30f), "IconPlay", 108f);
            prev.transform.Find("Face/Icon").localScale = new Vector3(-1f, 1f, 1f);
            var next = SquareButton(raceCard, "NextRace", new Vector2(0f, 0f), new Vector2(620f, 30f), "IconPlay", 108f);
            foreach (var arrow in new[] { prev, next }) // light keys on the dark card
            {
                arrow.transform.Find("Shadow").GetComponent<Image>().color = Theme.StoneDark;
                arrow.transform.Find("Face").GetComponent<Image>().color = Theme.Cream;
                arrow.transform.Find("Face/Icon").GetComponent<Image>().color = Theme.Ink;
            }
            var counter = Txt(At("Counter", raceCard, new Vector2(0f, 0f), new Vector2(380f, 40f), new Vector2(226f, 100f)), "RACE 1/8", 44, Theme.Cream, _condItalic);

            // Seats: two columns of four.
            var playersCaption = Txt(At("PlayersCaption", content, new Vector2(0f, 1f), new Vector2(64f, -936f), new Vector2(600f, 60f)), "RACERS 1/8", 46, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            playersCaption.characterSpacing = 6f;
            var fillRt = At("FillButton", content, new Vector2(1f, 1f), new Vector2(-60f, -926f), new Vector2(330f, 80f));
            var fillBg = Rounded(Img(fillRt, "Round", Theme.Ink, sliced: true, raycast: true), 16f);
            Img(At("Icon", fillRt, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(50f, 50f)), "IconBot", Theme.Amber).rectTransform.pivot = new Vector2(0f, 0.5f);
            var fillLabel = Txt(At("Label", fillRt, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(240f, 70f)), "FILL WITH BOTS", 36, Theme.Cream, _condItalic, TextAlignmentOptions.Right);
            fillLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            var fill = fillRt.gameObject.AddComponent<Button>(); fill.targetGraphic = fillBg; fill.transition = Selectable.Transition.None;
            fillRt.gameObject.AddComponent<ButtonFeedback>();

            var cards = new OnlineSlotCard[8];
            for (int i = 0; i < 8; i++)
            {
                int col = i % 2, row = i / 2;
                cards[i] = SeatCard(content, i, new Vector2(60f + col * 540f, -1030f - row * 176f));
            }

            // Colours.
            var colourCaption = Txt(At("ColourCaption", content, new Vector2(0f, 1f), new Vector2(64f, -1742f), new Vector2(600f, 56f)), "YOUR COLOUR", 42, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            colourCaption.characterSpacing = 6f;
            var swatchRow = At("Colours", content, new Vector2(0.5f, 1f), new Vector2(0f, -1802f), new Vector2(1050f, 120f));
            var ring = At("Ring", swatchRow, new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(128f, 128f));
            ring.pivot = new Vector2(0.5f, 0.5f);
            Img(ring, "RingDashed", Theme.Ink);
            var swatches = new Button[OnlineColors.All.Length];
            for (int i = 0; i < swatches.Length; i++)
            {
                var s = At($"Swatch{i + 1}", swatchRow, new Vector2(0f, 0.5f), new Vector2(66f + i * 131f, 0f), new Vector2(100f, 100f));
                s.pivot = new Vector2(0.5f, 0.5f);
                var face = Img(s, "Circle", OnlineColors.All[i], raycast: true);
                face.name = "Swatch";
                Img(At("Edge", s, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f)), "Ring", Theme.WithAlpha(Theme.Ink, 0.25f));
                var b = s.gameObject.AddComponent<Button>(); b.targetGraphic = face; b.transition = Selectable.Transition.None;
                s.gameObject.AddComponent<ButtonFeedback>();
                swatches[i] = b;
            }
            ring.SetAsLastSibling();

            // START (host) / READY (guests) along the bottom, and a hint under it.
            var main = WideButton(content, "MainButton", Vector2.zero, new Vector2(820f, 180f), "START RACE", Theme.Orange, Theme.OrangeDeep, Color.white, 80, _black);
            var mainRt = (RectTransform)main.transform;
            mainRt.anchorMin = mainRt.anchorMax = mainRt.pivot = new Vector2(0.5f, 0f);
            mainRt.anchoredPosition = new Vector2(0f, 120f);
            var hint = Txt(At("Hint", content, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1000f, 60f)), "", 38, Theme.InkSoft, _condItalic);
            hint.characterSpacing = 4f;

            // Toast (someone joined / left), sliding down from the top.
            var toast = At("Toast", safe, new Vector2(0.5f, 1f), new Vector2(0f, 120f), new Vector2(700f, 100f));
            toast.pivot = new Vector2(0.5f, 1f);
            Img(toast, "Pill", Theme.Ink, sliced: true);
            var toastLabel = Txt(Stretch("Label", toast), "NIKO JOINED", 44, Theme.Amber, _condItalic);
            toastLabel.characterSpacing = 4f;

            panel.gameObject.SetActive(false);
            refs.AddRange(new (string, Object)[]
            {
                ("roomPanel", panel.gameObject), ("roomGroup", group), ("roomContent", content), ("leaveButton", leave), ("copyButton", copy),
                ("codeLabel", code), ("copiedLabel", copied), ("racePicture", picture), ("raceTitle", raceTitle),
                ("raceDetails", details), ("raceCounter", counter), ("racePrev", prev), ("raceNext", next), ("cupTabs", tabs), ("tabTemplate", tab), ("diceSprite", Sprite("IconDice")), ("playersCaption", playersCaption),
                ("fillButton", fill), ("swatchRing", ring), ("mainButton", main),
                ("mainFace", main.transform.Find("Face").GetComponent<Image>()), ("mainShadow", main.transform.Find("Shadow").GetComponent<Image>()),
                ("mainLabel", main.transform.Find("Face/Label").GetComponent<TMP_Text>()), ("hintLabel", hint), ("toast", toast), ("toastLabel", toastLabel),
            });
            _pips = pips; _cards = cards; _swatches = swatches;
        }

        static Image[] _pips;
        static OnlineSlotCard[] _cards;
        static Button[] _swatches;

        // One seat: white card with a hard shadow; the car (in its colour), name, tags, ready check.
        static OnlineSlotCard SeatCard(RectTransform parent, int index, Vector2 position)
        {
            var root = At($"Seat{index + 1}", parent, new Vector2(0f, 1f), position, new Vector2(510f, 156f));
            var body = At("Body", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(510f, 156f));
            body.pivot = new Vector2(0.5f, 0.5f);
            body.anchoredPosition = new Vector2(0f, 0f);
            body.name = "Card";
            var hit = Img(Stretch("Hit", body), null, new Color(0f, 0f, 0f, 0f), raycast: true);

            // Filled seat.
            var filled = Stretch("Filled", body);
            // Your own seat: an orange rim (an orange card a little bigger, behind).
            var rim = Stretch("Highlight", filled); rim.offsetMin = new Vector2(-9f, -21f); rim.offsetMax = new Vector2(9f, 9f);
            var highlight = Img(rim, "Round", Theme.Orange, sliced: true);
            var shadow = Stretch("Shadow", filled); shadow.offsetMin = new Vector2(0f, -12f); shadow.offsetMax = new Vector2(0f, -12f);
            Img(shadow, "Round", Theme.Ink, sliced: true);
            var face = Img(Stretch("Face", filled), "Round", Color.white, sliced: true);
            var car = At("Car", filled, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(58f, 102f));
            car.pivot = new Vector2(0.5f, 0.5f);
            car.localRotation = Quaternion.Euler(0f, 0f, -90f);
            Img(At("Shadow", car, new Vector2(0.5f, 0.5f), new Vector2(6f, -6f), new Vector2(62f, 108f)), "CarShadow", new Color(0f, 0f, 0f, 0.3f)).preserveAspect = false;
            var carBody = Img(Stretch("Body", car), "Car", Theme.Orange); carBody.preserveAspect = false;
            Img(Stretch("Details", car), "CarDetails", Color.white).preserveAspect = false;
            var name = Txt(At("Name", filled, new Vector2(0f, 1f), new Vector2(146f, -20f), new Vector2(262f, 66f)), "PLAYER", 52, Theme.Ink, _black, TextAlignmentOptions.Left);
            name.enableAutoSizing = true; name.fontSizeMin = 32; name.fontSizeMax = 52;
            var tags = At("Tags", filled, new Vector2(0f, 0f), new Vector2(146f, 24f), new Vector2(262f, 42f));
            var tagLayout = tags.gameObject.AddComponent<HorizontalLayoutGroup>();
            tagLayout.spacing = 8f; tagLayout.childAlignment = TextAnchor.MiddleLeft;
            tagLayout.childControlWidth = true; tagLayout.childControlHeight = true; tagLayout.childForceExpandWidth = false;
            GameObject Tag(string label, Color bg, Color text, string icon)
            {
                var t = Rt(label + "Tag", tags);
                Rounded(Img(t, "Round", bg, sliced: true), 10f);
                var l = t.gameObject.AddComponent<HorizontalLayoutGroup>();
                l.padding = new RectOffset(12, 14, 3, 3); l.spacing = 4f; l.childAlignment = TextAnchor.MiddleCenter;
                l.childControlWidth = true; l.childControlHeight = true; l.childForceExpandWidth = false;
                if (icon != null)
                {
                    var i = Rt("Icon", t);
                    Img(i, icon, text);
                    var le = i.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 26f; le.preferredHeight = 26f;
                }
                Txt(Rt("Label", t), label, 27, text, _condItalic);
                return t.gameObject;
            }
            var you = Tag("YOU", Theme.Orange, Color.white, null);
            var hostTag = Tag("HOST", Theme.Ink, Theme.Amber, null); // the crown is the winner's
            var bot = Tag("BOT", Theme.Stone, Theme.Ink, "IconBot");

            var badge = At("Ready", filled, new Vector2(1f, 0.5f), new Vector2(-52f, 0f), new Vector2(66f, 66f));
            badge.pivot = new Vector2(0.5f, 0.5f);
            var readyFace = Img(badge, "Circle", Theme.Leaf);
            var check = At("Check", badge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
            Img(check, "IconCheck", Color.white);

            var removeRt = At("Remove", filled, new Vector2(1f, 0.5f), new Vector2(-52f, 0f), new Vector2(66f, 66f));
            removeRt.pivot = new Vector2(0.5f, 0.5f);
            var removeBg = Img(removeRt, "Circle", Theme.WithAlpha(Theme.Ink, 0.1f), raycast: true);
            Img(At("Icon", removeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f)), "IconClose", Theme.Ink);
            var remove = removeRt.gameObject.AddComponent<Button>(); remove.targetGraphic = removeBg; remove.transition = Selectable.Transition.None;
            removeRt.gameObject.AddComponent<ButtonFeedback>();

            // Empty seat: a faint dashed card.
            var empty = Stretch("Empty", body);
            Img(empty, "Round", Theme.WithAlpha(Theme.Ink, 0.08f), sliced: true);
            var plus = At("Plus", empty, new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(64f, 64f));
            plus.pivot = new Vector2(0f, 0.5f);
            Img(plus, "IconPlus", Theme.InkSoft);
            var emptyLabel = Txt(At("Label", empty, new Vector2(0f, 0.5f), new Vector2(146f, 0f), new Vector2(330f, 70f)), "ADD BOT", 46, Theme.InkSoft, _condItalic, TextAlignmentOptions.Left);
            emptyLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
            emptyLabel.characterSpacing = 4f;

            var cardButton = body.gameObject.AddComponent<Button>(); cardButton.targetGraphic = hit; cardButton.transition = Selectable.Transition.None;
            body.gameObject.AddComponent<ButtonFeedback>();

            // The last winner's crown, placed after the name at runtime.
            var crown = At("Crown", filled, new Vector2(0f, 1f), new Vector2(400f, -50f), new Vector2(46f, 46f));
            crown.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", crown, new Vector2(0.5f, 0.5f), new Vector2(2f, -4f), new Vector2(46f, 46f)), "IconCrown", Theme.WithAlpha(Theme.Ink, 0.3f));
            Img(Stretch("Icon", crown), "IconCrown", new Color(1f, 0.74f, 0.12f));

            var card = root.gameObject.AddComponent<OnlineSlotCard>();
            Configure(card, ("body", body), ("filled", filled.gameObject), ("empty", empty.gameObject), ("face", face), ("highlight", highlight),
                ("car", car), ("carBody", carBody), ("nameLabel", name), ("youTag", you), ("hostTag", hostTag), ("botTag", bot),
                ("readyBadge", badge), ("readyFace", readyFace), ("readyCheck", check.gameObject), ("removeButton", remove),
                ("cardButton", cardButton), ("emptyLabel", emptyLabel), ("emptyPlus", plus.gameObject), ("crown", crown));
            return card;
        }

        /// <summary>A "Round" sliced image with smaller corners: a rectangle with rounded edges (radius in canvas units).</summary>
        static Image Rounded(Image image, float radius)
        {
            image.pixelsPerUnitMultiplier = 40f / Mathf.Max(1f, radius); // the sprite's corners are 40 px
            return image;
        }

        // A wide button with a hard shadow (like the menu buttons), anchored at the top centre of its parent.
        static Button WideButton(RectTransform parent, string name, Vector2 position, Vector2 size, string label, Color face, Color shadow, Color text,
            float fontSize, TMP_FontAsset font)
        {
            var rt = At(name, parent, new Vector2(0.5f, 1f), position, new Vector2(size.x, size.y + 14f));
            var shadowRt = Stretch("Shadow", rt); shadowRt.offsetMax = new Vector2(0f, -14f);
            Img(shadowRt, "Round", shadow, sliced: true);
            var faceRt = Stretch("Face", rt); faceRt.offsetMin = new Vector2(0f, 14f);
            var bg = Img(faceRt, "Round", face, sliced: true, raycast: true);
            var labelText = Txt(Stretch("Label", faceRt), label, fontSize, text, font);
            labelText.characterSpacing = 6f;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg; button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        // A text field on a white rounded face with a dark hard shadow.
        static TMP_InputField InputField(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions align, int limit, bool visible)
        {
            var rt = At(name, parent, new Vector2(0f, 1f), position, size);
            if (visible)
            {
                var shadowRt = Stretch("Shadow", rt); shadowRt.offsetMin = shadowRt.offsetMax = new Vector2(0f, -12f);
                Img(shadowRt, "Round", Theme.Ink, sliced: true);
            }
            var face = Img(Stretch("Face", rt), "Round", Color.white, sliced: true, raycast: true);
            var area = Stretch("Text Area", rt);
            area.offsetMin = new Vector2(36f, 6f); area.offsetMax = new Vector2(-36f, -6f);
            area.gameObject.AddComponent<RectMask2D>();
            var text = Txt(Stretch("Text", area), "", fontSize, Theme.Ink, _black);
            text.alignment = align == TextAlignmentOptions.Left ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
            var field = rt.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.targetGraphic = face;
            field.characterLimit = limit;
            field.transition = Selectable.Transition.None;
            field.caretColor = Theme.Orange; field.customCaretColor = true; field.caretWidth = 6;
            field.selectionColor = Theme.WithAlpha(Theme.Amber, 0.5f);
            field.fontAsset = _black;
            field.pointSize = fontSize;
            field.richText = false;
            return field;
        }

        // Called at the end of BuildLobby (arrays are set once the whole hierarchy exists).
        static void WireLobbyArrays(OnlineLobbyScreen screen)
        {
            var so = new SerializedObject(screen);
            SetArray(so, "codeLetters", _codeLetters);
            SetArray(so, "codeBoxes", _codeBoxes);
            SetArray(so, "demoCars", _demoCars);
            SetArray(so, "raceDifficulty", _pips);
            SetArray(so, "slotCards", _cards);
            SetArray(so, "swatches", _swatches);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== map: the tour after the tutorial

        static void BuildMapTour(RectTransform canvas, MapScreen screen, params RectTransform[] targets)
        {
            var root = Stretch("MapTour", canvas);
            var area = SafeArea(root);
            area.offsetMin = area.offsetMax = Vector2.zero;
            Object.DestroyImmediate(area.GetComponent<SafeAreaFitter>()); // the dim covers the whole screen
            var tapRt = Stretch("Tap", area);
            var tapImage = Img(tapRt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var tap = tapRt.gameObject.AddComponent<Button>(); tap.targetGraphic = tapImage; tap.transition = Selectable.Transition.None;
            var dims = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                dims[i] = Rt($"Dim{i + 1}", area);
                Img(dims[i], null, Theme.WithAlpha(Theme.Ink, 0.74f));
            }
            var frame = Rt("Frame", area);
            var frameImage = Img(frame, "Round", Theme.Amber, sliced: true);
            frameImage.fillCenter = false;
            frameImage.pixelsPerUnitMultiplier = 5f; // a thin rim with small rounded corners

            var card = At("Card", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 470f));
            card.gameObject.AddComponent<CanvasGroup>();
            var cs = Stretch("Shadow", card); cs.offsetMin = cs.offsetMax = new Vector2(0f, -16f);
            Img(cs, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Face", card), "Round", Theme.Cream, sliced: true);
            var step = Txt(At("Step", card, new Vector2(0f, 1f), new Vector2(50f, -34f), new Vector2(600f, 44f)), "MAP TOUR · 1/10", 36, Theme.Rust, _condItalic, TextAlignmentOptions.Left);
            step.characterSpacing = 6f;
            var title = Txt(At("Title", card, new Vector2(0f, 1f), new Vector2(46f, -76f), new Vector2(860f, 96f)), "THE RACE", 84, Theme.Ink, _black, TextAlignmentOptions.Left);
            var body = Txt(At("Body", card, new Vector2(0f, 1f), new Vector2(50f, -174f), new Vector2(860f, 110f)), "", 40, Theme.InkSoft, _semi, TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            var next = WideButton(card, "NextButton", Vector2.zero, new Vector2(300f, 100f), "NEXT", Theme.Orange, Theme.OrangeDeep, Color.white, 50, _black);
            var nextRt = (RectTransform)next.transform;
            nextRt.anchorMin = nextRt.anchorMax = nextRt.pivot = new Vector2(1f, 0f); nextRt.anchoredPosition = new Vector2(-40f, 34f);
            var skipRt = At("SkipButton", card, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(220f, 90f));
            var skipHit = Img(skipRt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Txt(Stretch("Label", skipRt), "SKIP TOUR", 40, Theme.StoneDark, _condItalic, TextAlignmentOptions.Left).characterSpacing = 4f;
            var skip = skipRt.gameObject.AddComponent<Button>(); skip.targetGraphic = skipHit; skip.transition = Selectable.Transition.None;
            skipRt.gameObject.AddComponent<ButtonFeedback>();

            var finger = At("Finger", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 150f));
            finger.pivot = new Vector2(0.5f, 0.96f);
            Img(finger, "Finger", Color.white);

            var tour = root.gameObject.AddComponent<MapTour>();
            Configure(tour, ("screen", screen), ("root", area.gameObject), ("area", area), ("frame", frame), ("card", card),
                ("stepLabel", step), ("titleLabel", title), ("bodyLabel", body), ("nextLabel", next.transform.Find("Face/Label").GetComponent<TMP_Text>()),
                ("nextButton", next), ("skipButton", skip), ("tapArea", tap), ("finger", finger));
            var so = new SerializedObject(tour);
            SetArray(so, "dims", dims);
            SetArray(so, "targets", targets);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== race: waiting + results

        static void BuildOnlineRaceView(RectTransform canvas, RaceManager race)
        {
            var root = Stretch("OnlineRace", canvas);
            var view = root.gameObject.AddComponent<OnlineRaceView>();
            var safe = SafeArea(root);

            // Waiting for racers: a dark pill in the middle.
            var waiting = At("Waiting", safe, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(760f, 120f));
            waiting.pivot = new Vector2(0.5f, 0.5f);
            Img(waiting, "Pill", Theme.WithAlpha(Theme.Ink, 0.9f), sliced: true);
            var waitingLabel = Txt(Stretch("Label", waiting), "WAITING FOR RACERS...", 52, Theme.Cream, _condItalic);
            waitingLabel.characterSpacing = 6f;
            Configure(waiting.gameObject.AddComponent<UIPulse>(), ("speed", 1.2f), ("scale", new Vector2(0.98f, 1.03f)));

            // Results.
            var results = Stretch("Results", root);
            var group = results.gameObject.AddComponent<CanvasGroup>();
            Img(results, null, Theme.Cream, raycast: true);
            Img(At("HillBack", results, new Vector2(0.5f, 0f), new Vector2(-300f, -1100f), new Vector2(2000f, 1500f)), "Circle", Hex("9CC383")).preserveAspect = false;
            Img(At("HillFront", results, new Vector2(0.5f, 0f), new Vector2(260f, -1280f), new Vector2(2200f, 1600f)), "Circle", Hex("7FB06C")).preserveAspect = false;
            var rSafe = SafeArea(results);
            var band = At("Checker", rSafe, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400f, 80f));
            var checker = Img(band, "Checker", Color.white);
            checker.type = Image.Type.Tiled; checker.preserveAspect = false; checker.pixelsPerUnitMultiplier = 1.4f;
            var raceLabel = Txt(At("Race", rSafe, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1000f, 60f)), "SUNDAY LOOP", 46, Theme.Rust, _condItalic);
            raceLabel.characterSpacing = 8f;
            var place = Txt(At("Place", rSafe, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(1000f, 260f)), "1ST", 250, Theme.Orange, _black);
            place.fontSharedMaterial = _blackShadow;
            place.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            place.rectTransform.anchoredPosition = new Vector2(0f, -340f);

            var list = At("List", rSafe, new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(1000f, 8 * 128f));
            var rows = new RectTransform[8];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = At($"Row{i + 1}", list, new Vector2(0.5f, 1f), new Vector2(0f, -i * 128f), new Vector2(1000f, 112f));
                row.gameObject.AddComponent<CanvasGroup>();
                var content = At("Content", row, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 112f));
                content.pivot = new Vector2(0.5f, 0.5f);
                var rim = Stretch("Highlight", content); rim.offsetMin = new Vector2(-9f, -9f); rim.offsetMax = new Vector2(9f, 9f);
                Img(rim, "Round", Theme.Orange, sliced: true);
                Img(Stretch("Face", content), "Round", Theme.Ink, sliced: true);
                var medal = At("Medal", content, new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(84f, 84f));
                medal.pivot = new Vector2(0.5f, 0.5f);
                Img(medal, "Circle", new Color(1f, 0.78f, 0.2f));
                var placeText = Txt(At("Place", content, new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(84f, 100f)), "1", 60, Theme.Amber, _black);
                placeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var car = At("Car", content, new Vector2(0f, 0.5f), new Vector2(170f, 0f), new Vector2(52f, 92f));
                car.pivot = new Vector2(0.5f, 0.5f); car.localRotation = Quaternion.Euler(0f, 0f, -90f);
                Img(Stretch("Body", car), "Car", Theme.Orange).preserveAspect = false;
                Img(Stretch("Details", car), "CarDetails", Color.white).preserveAspect = false;
                var nameText = Txt(At("Name", content, new Vector2(0f, 0.5f), new Vector2(240f, 16f), new Vector2(380f, 64f)), "PLAYER", 50, Theme.Cream, _black, TextAlignmentOptions.Left);
                nameText.rectTransform.pivot = new Vector2(0f, 0.5f);
                nameText.enableAutoSizing = true; nameText.fontSizeMin = 32; nameText.fontSizeMax = 50;
                var subText = Txt(At("Sub", content, new Vector2(0f, 0.5f), new Vector2(242f, -28f), new Vector2(400f, 40f)), "+1.25 S", 32, Theme.Stone, _condItalic, TextAlignmentOptions.Left);
                subText.rectTransform.pivot = new Vector2(0f, 0.5f);
                subText.characterSpacing = 3f;
                var rowCrown = At("Crown", content, new Vector2(0f, 0.5f), new Vector2(170f, 44f), new Vector2(48f, 48f));
                rowCrown.pivot = new Vector2(0.5f, 0.5f); rowCrown.localRotation = Quaternion.Euler(0f, 0f, 14f);
                Img(rowCrown, "IconCrown", new Color(1f, 0.78f, 0.2f));
                var timeText = Txt(At("Time", content, new Vector2(1f, 0.5f), new Vector2(-36f, 0f), new Vector2(290f, 90f)), "1:02.33", 52, Theme.Cream, _cond, TextAlignmentOptions.Right);
                timeText.rectTransform.pivot = new Vector2(1f, 0.5f);
                rows[i] = row;
            }

            var lobby = WideButton(rSafe, "LobbyButton", Vector2.zero, new Vector2(770f, 170f), "BACK TO ROOM", Theme.Orange, Theme.OrangeDeep, Color.white, 72, _black);
            var lobbyRt = (RectTransform)lobby.transform;
            lobbyRt.anchorMin = lobbyRt.anchorMax = lobbyRt.pivot = new Vector2(0.5f, 0f); lobbyRt.anchoredPosition = new Vector2(0f, 200f);
            var hint = Txt(At("Hint", rSafe, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1000f, 60f)), "", 40, Theme.InkSoft, _condItalic);
            var leave = WideButton(rSafe, "LeaveButton", Vector2.zero, new Vector2(500f, 120f), "LEAVE ROOM", Color.white, Theme.Ink, Theme.Ink, 50, _condItalic);
            var leaveRt = (RectTransform)leave.transform;
            leaveRt.anchorMin = leaveRt.anchorMax = leaveRt.pivot = new Vector2(0.5f, 0f); leaveRt.anchoredPosition = new Vector2(0f, 50f);

            // The room closed (host left / connection lost).
            var closed = Stretch("Closed", root);
            Img(closed, null, Theme.WithAlpha(Theme.Ink, 0.7f), raycast: true);
            var card = At("Card", closed, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 640f));
            card.pivot = new Vector2(0.5f, 0.5f);
            var cs = Stretch("Shadow", card); cs.offsetMin = cs.offsetMax = new Vector2(0f, -20f);
            Img(cs, "Round", Shade(Theme.Ink, 0.7f), sliced: true);
            Img(Stretch("Face", card), "Round", Theme.Cream, sliced: true);
            Img(At("Icon", card, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(120f, 120f)), "IconPeople", Theme.Rust);
            var closedLabel = Txt(At("Message", card, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(800f, 140f)), "THE HOST LEFT THE ROOM", 72, Theme.Ink, _black);
            closedLabel.textWrappingMode = TextWrappingModes.Normal;
            closedLabel.enableAutoSizing = true; closedLabel.fontSizeMin = 44; closedLabel.fontSizeMax = 72;
            var closedButton = WideButton(card, "MapButton", new Vector2(0f, -420f), new Vector2(600f, 150f), "MAP", Theme.Orange, Theme.OrangeDeep, Color.white, 66, _black);
            closed.gameObject.AddComponent<PopIn>();
            Configure(closed.GetComponent<PopIn>(), ("card", card));

            // The winner's moment, over the results: rays, trophy, the winner's car, WINNER and the name.
            var win = Stretch("Winner", root);
            var winDim = Img(win, null, new Color(0f, 0f, 0f, 0.82f), raycast: true);
            var winSafe = SafeArea(win);
            var rays = At("Rays", winSafe, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1500f, 1500f));
            rays.pivot = new Vector2(0.5f, 0.5f);
            Img(rays, "Rays", new Color(1f, 0.85f, 0.45f, 0.5f));
            Img(At("Glow", rays, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 700f)), "Circle", new Color(1f, 0.85f, 0.45f, 0.25f));
            var trophy = At("Trophy", winSafe, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(380f, 380f));
            trophy.pivot = new Vector2(0.5f, 0.5f);
            Img(At("Shadow", trophy, new Vector2(0.5f, 0.5f), new Vector2(10f, -16f), new Vector2(380f, 380f)), "IconTrophy", new Color(0f, 0f, 0f, 0.35f));
            Img(Stretch("Icon", trophy), "IconTrophy", new Color(1f, 0.78f, 0.2f));
            var winCar = At("Car", winSafe, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(130f, 228f));
            winCar.pivot = new Vector2(0.5f, 0.5f); winCar.localRotation = Quaternion.Euler(0f, 0f, -90f);
            Img(At("Shadow", winCar, new Vector2(0.5f, 0.5f), new Vector2(12f, -12f), new Vector2(140f, 240f)), "CarShadow", new Color(0f, 0f, 0f, 0.45f)).preserveAspect = false;
            var winBody = Img(Stretch("Body", winCar), "Car", Theme.Orange); winBody.preserveAspect = false;
            Img(Stretch("Details", winCar), "CarDetails", Color.white).preserveAspect = false;
            var winCaption = Txt(At("Caption", winSafe, new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), new Vector2(1000f, 110f)), "WINNER", 96, new Color(1f, 0.78f, 0.2f), _black);
            winCaption.rectTransform.pivot = new Vector2(0.5f, 0.5f); winCaption.characterSpacing = 14f;
            var winName = Txt(At("Name", winSafe, new Vector2(0.5f, 0.5f), new Vector2(0f, -370f), new Vector2(1060f, 190f)), "ZIPPY FOX", 160, Color.white, _black);
            winName.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            winName.enableAutoSizing = true; winName.fontSizeMin = 90; winName.fontSizeMax = 160;
            winName.fontSharedMaterial = _blackShadow;
            var winTap = Txt(At("Tap", winSafe, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1000f, 60f)), "TAP TO SEE THE RANKING", 42, Theme.Cream, _condItalic);
            winTap.characterSpacing = 6f;
            var confetti = Stretch("Confetti", win);
            confetti.anchorMin = confetti.anchorMax = new Vector2(0.5f, 0.5f);
            var winSkip = win.gameObject.AddComponent<Button>(); winSkip.targetGraphic = winDim; winSkip.transition = Selectable.Transition.None;
            Configure(view, ("winner", win.gameObject), ("winnerDim", winDim), ("winnerRays", rays), ("winnerTrophy", trophy), ("winnerCar", winCar),
                ("winnerCarBody", winBody), ("winnerCaption", winCaption), ("winnerName", winName), ("winnerTap", winTap), ("winnerSkip", winSkip),
                ("confettiLayer", confetti), ("confettiSprite", Sprite("Round")));
            closed.SetAsLastSibling();

            Configure(view, ("race", race), ("waiting", waiting.gameObject), ("waitingLabel", waitingLabel), ("results", results.gameObject),
                ("resultsGroup", group), ("placeLabel", place), ("raceLabel", raceLabel), ("lobbyButton", lobby), ("leaveButton", leave),
                ("lobbyLabel", lobby.transform.Find("Face/Label").GetComponent<TMP_Text>()), ("hintLabel", hint), ("closed", closed.gameObject),
                ("closedLabel", closedLabel), ("closedButton", closedButton));
            var so = new SerializedObject(view);
            SetArray(so, "rows", rows);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
