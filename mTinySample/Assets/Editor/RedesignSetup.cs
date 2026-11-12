using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// The K-G2 redesign: soft sky and cream backgrounds, one colour per block
// family, chunky "press me" buttons, Fredoka and Andika type, pop-in
// panels and fold-away choosers for modes and camera views.
// Run after the other mTiny setups; safe to run again.
public static class RedesignSetup
{
    // ---------------- PALETTE ----------------
    public static readonly Color Sky = Hex("EAF6FF");
    public static readonly Color Mint = Hex("E8F7EE");
    public static readonly Color Cream = Hex("FFF8E7");
    public static readonly Color Ink = Hex("2E2A4F");
    public static readonly Color Go = Hex("3DBE6B");
    public static readonly Color Move = Hex("4A9DFF");
    public static readonly Color Repeat = Hex("FFC93C");
    public static readonly Color Feel = Hex("FF7EB6");
    public static readonly Color Music = Hex("9B6BFF");
    public static readonly Color Orange = Hex("FF8A5B");
    public static readonly Color Locked = Hex("D9D4E8");
    public static readonly Color Soft = Hex("F2F5F9");
    public static readonly Color Edge = Hex("D5DDEA");

    // Block headings use the colours of the block art itself
    private static readonly Color MoveInk = Hex("2F86C8");
    private static readonly Color RepeatInk = Hex("B87F0C");
    private static readonly Color FeelInk = Hex("C86AB8");
    private static readonly Color MusicInk = Hex("9B528B");

    private const string FontFolder = "Assets/Fonts";
    private static TMP_FontAsset title;
    private static TMP_FontAsset body;

    [MenuItem("mTiny/Setup/Apply Redesign")]
    public static void ApplyAll()
    {
        title = EnsureFont(FontFolder + "/Fredoka.ttf", FontFolder + "/Fredoka SDF.asset");
        body = EnsureFont(FontFolder + "/Andika-Regular.ttf", FontFolder + "/Andika SDF.asset");

        // Fredoka's built-in weight is Light; thicken the letters so words
        // read clearly from a distance, and make Bold properly heavy
        title.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.28f);
        title.boldStyle = 1.4f;
        EditorUtility.SetDirty(title.material);
        EditorUtility.SetDirty(title);
        body.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.08f);
        EditorUtility.SetDirty(body.material);
        AssetDatabase.SaveAssets();
        WriteShapes();

        MainMenu();
        PlayScene("Assets/Scenes/Mission.unity", isWorld: false);
        PlayScene("Assets/Scenes/World.unity", isWorld: true);

        Debug.Log("[mTiny] Redesign applied.");
    }


    // =========================================================
    // TITLE, MISSION MAP, SETTINGS
    // =========================================================

    private static void MainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        ThemeScene(scene);

        Transform bg = Find(scene, "bg");
        Transform panel = Find(scene, "Panel", bg);

        // Sky, rolling hills and clouds behind everything
        Image sky = bg.GetComponent<Image>();
        sky.sprite = null;
        sky.color = Sky;
        foreach (string old in new[] { "hillBack", "hillFront", "cloud1", "cloud2", "logo", "subtitle", "speech" }) Remove(scene, old);
        Decor(bg, "hillBack", Shape("hill"), Hex("BDE7B5"), new Vector2(0.25f, 0f), new Vector2(0f, -40f), new Vector2(1500f, 520f), 0);
        Decor(bg, "hillFront", Shape("hill"), Hex("9ED79A"), new Vector2(0.8f, 0f), new Vector2(0f, -90f), new Vector2(1700f, 520f), 1);
        Decor(bg, "cloud1", Shape("cloud"), Color.white, new Vector2(0.15f, 0.8f), Vector2.zero, new Vector2(260f, 110f), 2).gameObject.AddComponent<Bob>().height = 6f;
        Decor(bg, "cloud2", Shape("cloud"), Color.white, new Vector2(0.78f, 0.72f), Vector2.zero, new Vector2(320f, 130f), 3).gameObject.AddComponent<Bob>().height = 8f;

        // The panel art becomes an invisible holder
        panel.GetComponent<Image>().enabled = false;

        // Text logo in place of the logo art (kept, switched off)
        Find(scene, "Image (1)", panel).gameObject.SetActive(false);
        TMP_Text logo = Text(panel, "logo", "m<color=#" + ColorUtility.ToHtmlStringRGB(TextColor(Go)) + ">T</color>iny", 190, Ink, title, new Vector2(0.5f, 0.5f), new Vector2(40f, 290f), new Vector2(900f, 230f));
        logo.fontStyle = FontStyles.Bold;
        Text(panel, "subtitle", "Code, drive and sing with mTiny", 52, Ink, body, new Vector2(0.5f, 0.5f), new Vector2(40f, 170f), new Vector2(1100f, 80f));

        // mTiny waves hello
        Transform mascot = Find(scene, "Image", panel);
        if (mascot.GetComponent<Bob>() == null) mascot.gameObject.AddComponent<Bob>().height = 14f;
        Image speech = Panel(panel, "speech", Color.white, new Vector2(0.5f, 0.5f), new Vector2(-470f, 70f), new Vector2(330f, 96f));
        Text(speech.transform, "text", "Hi! Let's play!", 42, Ink, title, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(310f, 80f));
        speech.gameObject.AddComponent<Bob>().height = 6f;

        // Missions and Explore as big chunky buttons
        Transform buttons = Find(scene, "buttons", panel);
        StyleTitleButton(Find(scene, "missionPanel", buttons), Go, "Missions");
        StyleTitleButton(Find(scene, "Explore", buttons), Move, "Explore");

        Transform gear = Find(scene, "settingsButton");
        Chunky(gear.GetComponent<Image>(), Color.white, circle: true);
        gear.Find("icon").GetComponent<Image>().color = Ink;

        // ---------------- MISSION MAP ----------------
        MissionMapUI map = UnityEngine.Object.FindFirstObjectByType<MissionMapUI>(FindObjectsInactive.Include);
        map.stageColors = new[] { Go, Move, Music };
        map.doneColor = Move;
        map.nextColor = Repeat;
        map.lockedColor = Locked;
        map.lockedCardTint = Hex("E6E3EF");
        EditorUtility.SetDirty(map);

        // (the title's Missions button is also called missionPanel)
        Image mapBg = Find(scene, "missionMap").parent.GetComponent<Image>();
        mapBg.sprite = null;
        mapBg.color = Cream;
        Find(scene, "missionMap").GetComponent<Image>().color = Cream;

        string[] cards = { "easyCard", "mediumCard", "hardCard" };
        Color[] stageColors = { Go, Move, Music };
        for (int i = 0; i < cards.Length; i++)
        {
            // The cards keep their pictures; only the ribbon takes the stage colour
            Transform card = Find(scene, cards[i]);
            card.Find("ribbon").GetComponent<Image>().color = stageColors[i];
        }

        Chunky(Find(scene, "homeButton").GetComponent<Image>(), Orange, circle: true);

        Transform popupCard = Find(scene, "card", Find(scene, "missionCard"));
        Chunky(popupCard.GetComponent<Image>(), Go);
        AddPop(popupCard);

        // ---------------- SETTINGS ----------------
        Transform settingsBox = Find(scene, "box", Find(scene, "settingsPanel"));
        Chunky(settingsBox.GetComponent<Image>(), Cream);
        AddPop(settingsBox);
        foreach (SoundToggleButton toggle in UnityEngine.Object.FindObjectsByType<SoundToggleButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            toggle.onColor = Go;
            toggle.offColor = Hex("B8B4C8");
            Chunky(toggle.GetComponent<Image>(), Go);
            EditorUtility.SetDirty(toggle);
        }
        Chunky(Find(scene, "resetButton", settingsBox).GetComponent<Image>(), Orange);
        Chunky(Find(scene, "closeButton", settingsBox).GetComponent<Image>(), Orange, circle: true);

        Transform confirm = Find(scene, "box", Find(scene, "resetConfirmPanel"));
        Chunky(confirm.GetComponent<Image>(), Cream);
        AddPop(confirm);
        Find(scene, "question", confirm).GetComponent<TMP_Text>().color = TextColor(Ink);
        Chunky(Find(scene, "yesButton", confirm).GetComponent<Image>(), Orange);
        Chunky(Find(scene, "noButton", confirm).GetComponent<Image>(), Move);

        Transform loading = Find(scene, "loadingScreen");
        if (loading != null) StyleLoading(loading);

        Finish(scene);
    }

    private static void StyleTitleButton(Transform button, Color color, string label)
    {
        Chunky(button.GetComponent<Image>(), color);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        text.color = Color.white;
        text.fontSize = Mathf.Max(text.fontSize, 80);
        // Room for the picture on the left
        RectTransform r = text.rectTransform;
        r.offsetMin = new Vector2(Mathf.Max(r.offsetMin.x, 240f), r.offsetMin.y);
    }

    private static void StyleLoading(Transform loading)
    {
        Image img = loading.GetComponent<Image>();
        img.sprite = null;
        img.color = Sky;
        foreach (Image i in loading.GetComponentsInChildren<Image>(true))
        {
            if (i.name == "Fill") i.color = Go;
            if (i.name == "Background") i.color = Edge;
        }
        foreach (TMP_Text t in loading.GetComponentsInChildren<TMP_Text>(true)) t.color = TextColor(Ink);
    }


    // =========================================================
    // MISSION AND EXPLORE
    // =========================================================

    private static void PlayScene(string path, bool isWorld)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        ThemeScene(scene);

        Camera cam = Camera.main;
        cam.backgroundColor = isWorld ? Mint : Sky;

        CommandManager commands = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        CameraViews views = cam.GetComponent<CameraViews>();
        SequenceUIManager bar = UnityEngine.Object.FindFirstObjectByType<SequenceUIManager>(FindObjectsInactive.Include);
        Transform header = Find(scene, "header");
        Transform canvas = header.parent;

        // ---------------- HEADER ----------------
        RoundIcon(Find(scene, "returnToHomeButton", header), Orange, UIEnhancer.LoadIcon("icon_home"), Color.white);
        RoundIcon(Find(scene, "undoButton", header), Orange, UIEnhancer.LoadIcon("icon_undo"), Color.white);
        RoundIcon(Find(scene, "resetButton", header), Color.white, UIEnhancer.LoadIcon("icon_replay"), Ink);

        // Code bar: a white "train" with a dashed slot for the next block
        Transform barRoot = Find(scene, "blockSequenceUI");
        Image frame = barRoot.Cast<Transform>().Select(t => t.GetComponent<Image>()).First(i => i != null && i.name != "Viewport");
        frame.sprite = Shape("ui_chunky");
        frame.type = Image.Type.Sliced;
        frame.preserveAspect = false;
        frame.color = Color.white;
        RectTransform frameRect = frame.rectTransform;
        frameRect.sizeDelta = new Vector2(1235f, 240f);

        Transform content = bar.content;
        Transform oldSlot = content.Find("nextSlot");
        if (oldSlot != null) UnityEngine.Object.DestroyImmediate(oldSlot.gameObject);
        GameObject slot = new GameObject("nextSlot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(content, false);
        ((RectTransform)slot.transform).sizeDelta = new Vector2(126f, 118f);
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = Shape("ui_dashed");
        slotImage.type = Image.Type.Sliced;
        slotImage.color = Edge;
        slotImage.raycastTarget = false;
        bar.nextSlot = (RectTransform)slot.transform;
        EditorUtility.SetDirty(bar);

        // ---------------- PALETTE ----------------
        RectTransform palette = (RectTransform)Find(scene, "commandblock_group");
        palette.sizeDelta = new Vector2(isWorld ? 500f : 384f, palette.sizeDelta.y);
        Image tray = Find(scene, "bg", palette).GetComponent<Image>();
        Chunky(tray, Color.white);
        GroupPalette(scene, palette, isWorld);

        // ---------------- MAT CARD ----------------
        Remove(scene, "matCard");
        GameObject mat = new GameObject("matCard", typeof(SpriteRenderer));
        SpriteRenderer matSprite = mat.GetComponent<SpriteRenderer>();
        matSprite.sprite = Shape("mat_card");
        matSprite.drawMode = SpriteDrawMode.Sliced;
        matSprite.color = Cream;
        matSprite.sortingOrder = -50;
        MatCard card = mat.AddComponent<MatCard>();
        card.views = views;

        // ---------------- PANELS ----------------
        foreach (string name in new[] { "instructionPanel", "finishPanel", "celebrationPanel" })
        {
            Transform root = Find(scene, name);
            if (root == null) continue;

            // Full-screen backdrop: a soft sky wash instead of the yellow art
            SpriteRenderer backdrop = root.GetComponent<SpriteRenderer>();
            if (backdrop != null && backdrop.sprite != null && backdrop.sprite.name != "mat_card")
            {
                Vector2 size = backdrop.sprite.bounds.size;
                backdrop.sprite = Shape("mat_card");
                backdrop.drawMode = SpriteDrawMode.Sliced;
                backdrop.size = size;
                backdrop.color = new Color(Sky.r, Sky.g, Sky.b, 0.96f);
                EditorUtility.SetDirty(backdrop);
            }

            Transform p = Find(scene, "Panel", root);
            if (p == null) continue;
            Chunky(p.GetComponent<Image>(), Cream);
            AddPop(p);

            // Finish card: the solid buttons need gaps the old art had built
            // in, and the title moves left of the button column
            if (name == "finishPanel")
            {
                Transform buttons = Find(scene, "buttons", p);
                if (buttons != null)
                {
                    // The old art overlapped on purpose (negative spacing)
                    VerticalLayoutGroup column = buttons.GetComponent<VerticalLayoutGroup>();
                    if (column != null)
                    {
                        column.spacing = 14f;
                        column.childForceExpandHeight = false;
                        column.childControlHeight = true;
                    }

                    foreach (RectTransform b in buttons.Cast<Transform>().OfType<RectTransform>())
                    {
                        LayoutElement le = b.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
                        le.preferredHeight = 100f;
                        le.minHeight = 100f;
                    }
                }

                Transform heading = Find(scene, "finishTime (1)", p);
                if (heading != null)
                {
                    RectTransform h = (RectTransform)heading;
                    h.anchoredPosition = new Vector2(-150f, h.anchoredPosition.y);
                    h.sizeDelta = new Vector2(500f, h.sizeDelta.y);
                    TMP_Text t = heading.GetComponent<TMP_Text>();
                    t.enableAutoSizing = true;
                    t.fontSizeMax = t.fontSize;
                    t.fontSizeMin = 40;
                }
            }
        }

        Transform start = Find(scene, "startGame");
        if (start != null) ChunkyLabel(start, Go, "Start", UIEnhancer.LoadIcon("icon_play"));

        foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Image img = b.GetComponent<Image>();
            if (img == null || img.sprite == null || !img.sprite.name.StartsWith("button3")) continue;
            Color c = b.name.StartsWith("replay") ? Orange : b.name.StartsWith("next") ? Go : Move;
            Chunky(img, c);
        }

        Transform banner = Find(scene, "tryAgainBanner");
        if (banner != null)
        {
            Chunky(banner.GetComponent<Image>(), Cream);
            AddPop(banner);
        }

        // ---------------- CHOOSERS ----------------
        Remove(scene, "viewButtons");
        Remove(scene, "viewChooser");
        Remove(scene, "modeChooser");

        CollapsibleChooser viewChooser;
        if (isWorld)
        {
            // Modes and views fold away under the home button
            ExploreModes modes = UnityEngine.Object.FindFirstObjectByType<ExploreModes>(FindObjectsInactive.Include);
            RectTransform oldModes = (RectTransform)modes.transform;
            foreach (Transform child in oldModes.Cast<Transform>().ToList()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            oldModes.sizeDelta = Vector2.zero;
            modes.codeButton = modes.remoteButton = modes.buildButton = null;

            CommandManager cm = commands;
            CollapsibleChooser modeChooser = Chooser(canvas, "modeChooser", new Vector2(0f, 1f), new Vector2(28f, -196f),
                new[] { "codeMode", "remoteMode", "buildMode" }, new[] { "Code", "Drive", "Build" },
                new[] { cm.forwardIcon, UIEnhancer.LoadIcon("joy_base"), UIEnhancer.LoadIcon("icon_puzzle") },
                new UnityAction[] { modes.ShowCode, modes.ShowRemote, modes.ShowBuild }, Move);
            modes.chooser = modeChooser;
            EditorUtility.SetDirty(modes);

            viewChooser = Chooser(canvas, "viewChooser", new Vector2(0f, 1f), new Vector2(28f, -326f),
                new[] { "topView", "tiltView", "followView" }, new[] { "Top", "Tilt", "Follow" },
                new[] { Shape("view_top"), Shape("view_tilt"), Shape("view_follow") },
                new UnityAction[] { views.ShowTop, views.ShowTilt, views.ShowFollow }, Repeat);

            // Explore's board moves left to sit between the choosers and the palette
            Vector3 p = cam.transform.position;
            cam.transform.position = new Vector3(3.3f, p.y, p.z);

            StyleExplorePanels(scene);
        }
        else
        {
            viewChooser = Chooser(header, "viewChooser", new Vector2(1f, 1f), new Vector2(-12f, -40f),
                new[] { "topView", "tiltView", "followView" }, new[] { "Top", "Tilt", "Follow" },
                new[] { Shape("view_top"), Shape("view_tilt"), Shape("view_follow") },
                new UnityAction[] { views.ShowTop, views.ShowTilt, views.ShowFollow }, Repeat);

            BuildGoalCard(scene, canvas);
        }

        // The mat card and Tilt view also take in the build grid
        MapBuilder builderObject = UnityEngine.Object.FindFirstObjectByType<MapBuilder>(FindObjectsInactive.Include);
        if (builderObject != null && !views.boardRoots.Contains(builderObject.transform))
        {
            views.boardRoots = views.boardRoots.Concat(new[] { builderObject.transform }).ToArray();
        }

        views.topButton = views.tiltButton = views.followButton = null;
        views.chooser = viewChooser;
        EditorUtility.SetDirty(views);

        Finish(scene);
    }

    private static void StyleExplorePanels(Scene scene)
    {
        Transform build = Find(scene, "buildPanel");
        if (build != null)
        {
            Chunky(build.GetComponent<Image>(), Color.white);
            Chunky(Find(scene, "clearButton", build).GetComponent<Image>(), Orange);
            Chunky(Find(scene, "cityButton", build).GetComponent<Image>(), Move);
        }

        MapBuilder map = UnityEngine.Object.FindFirstObjectByType<MapBuilder>(FindObjectsInactive.Include);
        if (map != null)
        {
            map.toolSelected = Repeat;
            map.toolIdle = Soft;
            map.markerColor = new Color(Edge.r, Edge.g, Edge.b, 0.9f);
            map.startMarkerColor = new Color(Go.r, Go.g, Go.b, 0.6f);
            EditorUtility.SetDirty(map);
        }

        Transform remote = Find(scene, "remotePanel");
        if (remote != null)
        {
            Transform knob = Find(scene, "knob", remote);
            if (knob != null) knob.GetComponent<Image>().color = Orange;
            foreach (string n in new[] { "sing", "happy", "beep" })
            {
                Transform b = Find(scene, n, remote);
                if (b == null) continue;
                Chunky(b.GetComponent<Image>(), n == "sing" ? Music : n == "happy" ? Feel : Move, circle: true);
            }
        }
    }


    // =========================================================
    // PALETTE GROUPS
    // =========================================================

    private static void GroupPalette(Scene scene, RectTransform palette, bool isWorld)
    {
        Transform content = Find(scene, "Content", palette);
        if (content.Find("group_Move") != null) return;

        GridLayoutGroup oldGrid = content.GetComponent<GridLayoutGroup>();
        if (oldGrid != null) UnityEngine.Object.DestroyImmediate(oldGrid);

        VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(4, 4, 8, 8);
        column.spacing = 6f;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandHeight = false;
        column.childForceExpandWidth = true;

        int columns = isWorld ? 4 : 3;
        var groups = new List<(string name, string label, string icon, Color ink, string[] blocks)>
        {
            ("Move", "Move", "icon_g_move", MoveInk, new[] { "startFlag", "forward", "left", "right" }),
            ("Repeat", "Repeat", "icon_g_repeat", RepeatInk, new[] { "2x multiplier", "3x multiplier", "4x multiplier", "5x multiplier" }),
            ("Feelings", "Feelings", "icon_g_feel", FeelInk, new[] { "happy", "sad", "angry", "surprised", "dizzy", "sleepy", "playful" }),
        };
        if (isWorld)
        {
            groups.Add(("Music", "Music", "icon_music", MusicInk,
                new[] { "noteDo", "noteRe", "noteMi", "noteFa", "noteSol", "noteLa", "noteSi", "noteHighDo" }));
        }

        foreach (var g in groups)
        {
            GameObject group = new GameObject("group_" + g.name, typeof(RectTransform));
            group.transform.SetParent(content, false);
            VerticalLayoutGroup v = group.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            // Heading: icon and word in the group's colour
            GameObject heading = new GameObject("heading", typeof(RectTransform));
            heading.transform.SetParent(group.transform, false);
            heading.AddComponent<LayoutElement>().preferredHeight = 38f;
            Image icon = Img(heading.transform, "icon", Shape(g.icon), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(30f, 30f));
            icon.color = g.ink;
            TMP_Text label = Text(heading.transform, "label", g.label, 30, g.ink, title, new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(240f, 38f));
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.alignment = TextAlignmentOptions.Left;
            label.fontStyle = FontStyles.Bold;

            // Row of blocks
            GameObject row = new GameObject("row", typeof(RectTransform));
            row.transform.SetParent(group.transform, false);
            GridLayoutGroup grid = row.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 86f);
            grid.spacing = new Vector2(5f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;

            foreach (string block in g.blocks)
            {
                Transform t = content.Cast<Transform>().FirstOrDefault(c => c.name == block);
                if (t != null) t.SetParent(row.transform, false);
            }

            PaletteGroup pg = group.AddComponent<PaletteGroup>();
            pg.heading = heading;
            pg.row = row.transform;
        }
    }


    // =========================================================
    // GOAL CARD (missions)
    // =========================================================

    private static void BuildGoalCard(Scene scene, Transform canvas)
    {
        Remove(scene, "goalCard");
        MissionBoardBuilder board = UnityEngine.Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
        CommandManager cm = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        Sprite face = MainMenuSetup.LoadSprite("Assets/Image/mtiny emotions/M-Tiny Excited_0.png") ??
                      AssetDatabase.FindAssets("M-Tiny Excited_0 t:Sprite").Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
        Sprite star = AssetDatabase.FindAssets("star (1) t:Sprite").Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();

        RectTransform root = NewRect("goalCard", canvas);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(28f, -196f);
        root.sizeDelta = new Vector2(250f, 96f);
        GoalCard goal = root.gameObject.AddComponent<GoalCard>();
        goal.board = board;

        // Chip: mTiny and the stars so far
        Image chip = Panel(root, "chip", Color.white, new Vector2(0f, 1f), Vector2.zero, new Vector2(250f, 96f));
        chip.rectTransform.pivot = new Vector2(0f, 1f);
        Button chipButton = chip.gameObject.AddComponent<Button>();
        chipButton.targetGraphic = chip;
        Image faceImg = Img(chip.transform, "face", face, new Vector2(0f, 0.5f), new Vector2(50f, 4f), new Vector2(76f, 76f));
        faceImg.gameObject.AddComponent<Bob>().height = 4f;
        goal.chipStars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            goal.chipStars[i] = Img(chip.transform, "star" + i, star, new Vector2(0f, 0.5f), new Vector2(120f + i * 44f, 4f), new Vector2(42f, 42f));
        }
        goal.chip = chipButton;

        // Card that opens below the chip
        Image cardImg = Panel(root, "card", Cream, new Vector2(0f, 1f), new Vector2(0f, -108f), new Vector2(400f, 380f));
        cardImg.rectTransform.pivot = new Vector2(0f, 1f);
        Transform card = cardImg.transform;
        AddPop(card);
        goal.card = card.gameObject;
        goal.title = Text(card, "title", "Mission", 46, Ink, title, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(360f, 64f));
        goal.title.fontStyle = FontStyles.Bold;
        goal.moveHint = Img(card, "moveHint", cm.forwardIcon, new Vector2(0f, 1f), new Vector2(64f, -126f), new Vector2(84f, 72f)).gameObject;
        goal.multiplierHint = Img(card, "multiplierHint", cm.multiplier2xIcon, new Vector2(0f, 1f), new Vector2(150f, -126f), new Vector2(84f, 72f)).gameObject;
        goal.emotionHint = Img(card, "emotionHint", cm.happyIcon, new Vector2(0f, 1f), new Vector2(236f, -126f), new Vector2(84f, 72f)).gameObject;
        Text(card, "arrow", "→", 44, Ink, title, new Vector2(1f, 1f), new Vector2(-92f, -124f), new Vector2(40f, 60f));
        goal.goalTile = Img(card, "goalTile", null, new Vector2(1f, 1f), new Vector2(-44f, -126f), new Vector2(70f, 70f));
        goal.parkGoal = MainMenuSetup.LoadSprite("Assets/Image/Tilemaps2 assets/Layer 2.png");
        goal.cityGoal = MainMenuSetup.LoadSprite("Assets/Image/Tilemaps1/Layer 15.png");
        goal.instruction = Text(card, "instruction", "Reach the flag!", 34, Ink, body, new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(360f, 110f));
        goal.instruction.enableWordWrapping = true;
        goal.best = Text(card, "best", "Best: 5 blocks", 30, Hex("6B6585"), title, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 44f));
        card.gameObject.SetActive(false);

        EditorUtility.SetDirty(goal);
    }


    // =========================================================
    // FOLD-AWAY CHOOSER
    // =========================================================

    private static CollapsibleChooser Chooser(Transform parent, string name, Vector2 anchor, Vector2 position,
        string[] optionNames, string[] labels, Sprite[] icons, UnityAction[] actions, Color selected)
    {
        RectTransform root = NewRect(name, parent);
        root.anchorMin = root.anchorMax = root.pivot = anchor;
        root.anchoredPosition = position;
        root.sizeDelta = new Vector2(220f, 104f);
        CollapsibleChooser chooser = root.gameObject.AddComponent<CollapsibleChooser>();
        bool right = anchor.x > 0.5f;

        // Chip: current choice and a little arrow
        Image chip = Panel(root, "chip", Color.white, anchor, Vector2.zero, new Vector2(220f, 104f));
        chip.rectTransform.pivot = anchor;
        Button toggle = chip.gameObject.AddComponent<Button>();
        toggle.targetGraphic = chip;
        chooser.toggle = toggle;
        chooser.toggleIcon = Img(chip.transform, "icon", icons[0], new Vector2(0f, 0.5f), new Vector2(52f, 4f), new Vector2(64f, 64f));
        chooser.drawnColor = Ink;
        if (CollapsibleChooser.IsDrawn(icons[0])) chooser.toggleIcon.color = Ink;
        chooser.toggleLabel = Text(chip.transform, "label", labels[0], 36, Ink, title, new Vector2(0f, 0.5f), new Vector2(142f, 4f), new Vector2(96f, 50f));
        chooser.toggleLabel.enableAutoSizing = true;
        chooser.toggleLabel.fontSizeMin = 24;
        chooser.toggleLabel.fontSizeMax = 36;
        chooser.toggleLabel.fontStyle = FontStyles.Bold;
        Image chevron = Img(chip.transform, "chevron", Shape("chevron"), new Vector2(1f, 0.5f), new Vector2(-24f, 4f), new Vector2(26f, 26f));
        chevron.color = Hex("8A85A3");
        chooser.chevron = chevron.rectTransform;

        // Row of all choices, opening beside the chip
        float width = optionNames.Length * 128f + 16f;
        Image row = Panel(root, "options", Color.white, anchor, new Vector2(right ? -232f : 232f, 0f), new Vector2(width, 120f));
        row.rectTransform.pivot = new Vector2(right ? 1f : 0f, anchor.y);
        AddPop(row.transform);
        chooser.optionsPanel = row.gameObject;
        chooser.optionFaces = new Image[optionNames.Length];
        chooser.icons = icons;
        chooser.labels = labels;
        chooser.selectedColor = selected;
        chooser.selectedInk = selected == Repeat ? TextColor(Ink) : Color.white;
        chooser.idleColor = Soft;
        chooser.idleInk = TextColor(Ink);

        for (int i = 0; i < optionNames.Length; i++)
        {
            Image face = Panel(row.transform, optionNames[i], Soft, new Vector2(0f, 0.5f), new Vector2(72f + i * 128f, 4f), new Vector2(120f, 96f));
            Button b = face.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            UnityEventTools.AddPersistentListener(b.onClick, actions[i]);
            UnityEventTools.AddIntPersistentListener(b.onClick, chooser.Choose, i);
            Image optionIcon = Img(face.transform, "icon", icons[i], new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(46f, 46f));
            if (CollapsibleChooser.IsDrawn(icons[i])) optionIcon.color = Ink;
            Text(face.transform, "label", labels[i], 26, Ink, title, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(116f, 34f)).fontStyle = FontStyles.Bold;
            chooser.optionFaces[i] = face;
        }

        row.gameObject.SetActive(false);
        return chooser;
    }


    // =========================================================
    // WHOLE-SCENE THEME
    // =========================================================

    // Fonts, sentence-case words and the new colours on everything
    private static void ThemeScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool isBody = BodyNames.Contains(t.name);
                t.font = isBody ? body : title;
                t.fontSharedMaterial = t.font.material;
                t.fontStyle &= ~(FontStyles.Italic | FontStyles.UpperCase | FontStyles.LowerCase | FontStyles.SmallCaps);
                if (!isBody) t.fontStyle |= FontStyles.Bold;
                if (!IsWhite(t.color)) t.color = TextColor(Ink);

                // "EASY" -> "Easy": young readers learn lowercase first
                string s = t.text;
                if (s.Length > 1 && s.Any(char.IsLetter) && s == s.ToUpperInvariant() && !s.Contains("<"))
                {
                    t.text = s.Substring(0, 1) + s.Substring(1).ToLowerInvariant();
                }
                EditorUtility.SetDirty(t);
            }

            foreach (Image i in root.GetComponentsInChildren<Image>(true))
            {
                Color mapped;
                if (Remap(i.color, out mapped)) { i.color = mapped; EditorUtility.SetDirty(i); }

                if (i.sprite == null) continue;
                bool isButton = i.GetComponent<Button>() != null;
                if (isButton && i.sprite.name == "ui_rounded") Chunky(i, i.color);
                else if (isButton && i.sprite.name == "node_circle") Chunky(i, i.color, circle: true);
            }
        }
    }

    private static readonly HashSet<string> BodyNames = new HashSet<string>
    {
        "instructionText", "message", "hint", "instruction", "subtitle"
    };

    private static bool Remap(Color c, out Color result)
    {
        var table = new (Color from, Color to)[]
        {
            (new Color(0.851f, 0.486f, 0.161f), Orange), (new Color(0.91f, 0.47f, 0.16f), Orange), (new Color(0.94f, 0.54f, 0.14f), Orange),
            (new Color(0.36f, 0.69f, 0.25f), Go), (new Color(0.36f, 0.68f, 0.24f), Go),
            (new Color(0.18f, 0.53f, 0.78f), Move),
            (new Color(0.58f, 0.33f, 0.66f), Music),
            (new Color(0.98f, 0.82f, 0.48f), Color.white),
            (new Color(1f, 0.93f, 0.72f), Cream), (new Color(1f, 0.95f, 0.78f), Cream), (new Color(1f, 0.97f, 0.88f), Cream), (new Color(1f, 0.95f, 0.72f), Cream),
            (new Color(0.84f, 0.73f, 0.52f), Edge),
            (new Color(0.39f, 0.22f, 0.02f), Ink),
        };

        foreach (var entry in table)
        {
            if (Mathf.Abs(c.r - entry.from.r) + Mathf.Abs(c.g - entry.from.g) + Mathf.Abs(c.b - entry.from.b) < 0.07f)
            {
                result = new Color(entry.to.r, entry.to.g, entry.to.b, c.a);
                return true;
            }
        }

        result = c;
        return false;
    }

    private static bool IsWhite(Color c) { return c.r > 0.95f && c.g > 0.95f && c.b > 0.95f; }

    // Text colours in this project come out one gamma step lighter than
    // set (the old brown titles did too), so text is given the linear
    // value of the colour meant to be seen. Images are not affected.
    public static Color TextColor(Color seen)
    {
        if (IsWhite(seen)) return seen;
        Color c = seen.linear;
        c.a = seen.a;
        return c;
    }


    // =========================================================
    // BUILDING BLOCKS
    // =========================================================

    // Chunky look: rounded body with a darker bottom edge (the edge is
    // drawn grey in the sprite, so any tint darkens it automatically)
    private static void Chunky(Image image, Color color, bool circle = false)
    {
        if (image == null) return;
        image.sprite = Shape(circle ? "ui_chunky_circle" : "ui_chunky");
        image.type = circle ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = circle;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = color;
        EditorUtility.SetDirty(image);
    }

    // An art button becomes a chunky circle with a white (or ink) icon
    private static void RoundIcon(Transform button, Color color, Sprite icon, Color iconColor)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        Chunky(image, color, circle: true);
        Transform old = button.Find("glyph");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        Image glyph = Img(button, "glyph", icon, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), ((RectTransform)button).sizeDelta * 0.52f);
        glyph.color = iconColor;
    }

    // An art button becomes a chunky pill with an icon and a word
    private static void ChunkyLabel(Transform button, Color color, string label, Sprite icon)
    {
        Chunky(button.GetComponent<Image>(), color);
        RectTransform r = (RectTransform)button;
        r.sizeDelta = new Vector2(380f, 140f);
        foreach (string n in new[] { "glyph", "word" })
        {
            Transform old = button.Find(n);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        Img(button, "glyph", icon, new Vector2(0f, 0.5f), new Vector2(80f, 6f), new Vector2(70f, 70f));
        Text(button, "word", label, 64, Color.white, title, new Vector2(0.5f, 0.5f), new Vector2(40f, 6f), new Vector2(230f, 100f)).fontStyle = FontStyles.Bold;
    }

    private static Image Panel(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Image image = Img(parent, name, Shape("ui_chunky"), anchor, position, size);
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static Image Decor(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 position, Vector2 size, int sibling)
    {
        Image image = Img(parent, name, sprite, anchor, position, size);
        image.color = color;
        image.preserveAspect = false;
        image.transform.SetSiblingIndex(sibling);
        return image;
    }

    private static Image Img(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform r = NewRect(name, parent);
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = size;
        Image image = r.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(Transform parent, string name, string text, float size, Color color, TMP_FontAsset font,
        Vector2 anchor, Vector2 position, Vector2 box)
    {
        RectTransform r = NewRect(name, parent);
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = box;
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = TextColor(color);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    private static void AddPop(Transform t)
    {
        if (t != null && t.GetComponent<PopIn>() == null) t.gameObject.AddComponent<PopIn>();
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Finish(Scene scene)
    {
        UIEnhancer.AddTouchFeedback(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Remove(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToList())
            {
                if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
            if (root != null && root.name == name) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Transform Find(Scene scene, string name, Transform under = null)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name && under == null) return root.transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name && (under == null || t.IsChildOf(under))) return t;
            }
        }
        return null;
    }

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }


    // =========================================================
    // FONTS
    // =========================================================

    private static TMP_FontAsset EnsureFont(string ttfPath, string assetPath)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);

        asset.atlasTextures[0].name = asset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        // Common characters ready in the atlas; anything else is added
        // as needed while playing (the font file ships with the game)
        string common = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·→";
        string missing;
        asset.TryAddCharacters(common, out missing);

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }


    // =========================================================
    // SHAPES
    // =========================================================

    private const string ShapeFolder = "Assets/Image/ui";

    private static Sprite Shape(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ShapeFolder + "/" + name + ".png");
    }

    private static void WriteShapes()
    {
        Color edge = new Color(0.8f, 0.8f, 0.84f, 1f);

        // Rounded body, darker band along the bottom
        WriteShape("ui_chunky", (x, y) =>
        {
            if (!UIEnhancer.InsideRoundedRect(x, y, 4f, 124f, 30f)) return Color.clear;
            return y < 16f ? edge : Color.white;
        }, 40, false);

        // Circle with a darker crescent underneath
        WriteShape("ui_chunky_circle", (x, y) =>
        {
            float top = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 68f));
            float under = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 60f));
            if (top <= 58f) return Color.white;
            return under <= 58f ? edge : Color.clear;
        }, 0, false);

        // Dashed outline for the empty "next block" slot
        WriteShape("ui_dashed", (x, y) =>
        {
            bool outer = UIEnhancer.InsideRoundedRect(x, y, 4f, 124f, 26f);
            bool inner = UIEnhancer.InsideRoundedRect(x, y, 12f, 116f, 18f);
            bool dash = ((int)((x + y) / 12f)) % 2 == 0;
            return outer && !inner && dash ? Color.white : Color.clear;
        }, 40, false);

        // Cream card under the puzzle mat (sliced on a SpriteRenderer)
        WriteShape("mat_card", (x, y) =>
        {
            if (!UIEnhancer.InsideRoundedRect(x, y, 4f, 124f, 30f)) return Color.clear;
            return y < 14f ? edge : Color.white;
        }, 40, true);

        WriteShape("hill", (x, y) =>
        {
            float u = (x - 64f) / 64f, v = y / 120f;
            return u * u + v * v <= 1f ? Color.white : Color.clear;
        }, 0, false);

        WriteShape("cloud", (x, y) =>
        {
            bool c = Vector2.Distance(new Vector2(x, y), new Vector2(40f, 50f)) < 30f ||
                     Vector2.Distance(new Vector2(x, y), new Vector2(70f, 64f)) < 38f ||
                     Vector2.Distance(new Vector2(x, y), new Vector2(98f, 50f)) < 26f ||
                     (y > 22f && y < 50f && x > 20f && x < 116f);
            return c ? Color.white : Color.clear;
        }, 0, false);

        WriteShape("chevron", (x, y) =>
        {
            float d = Mathf.Abs(Mathf.Abs(x - 64f) - (y - 40f));
            return d < 12f && y > 34f && y < 94f && Mathf.Abs(x - 64f) < 50f ? Color.white : Color.clear;
        }, 0, false);

        // Heading icons: crossed arrows (move), loop (repeat), smile (feelings)
        UIEnhancer.WriteIcon("icon_g_move", (x, y) =>
            (Mathf.Abs(x - 64f) < 8f && y > 20f && y < 108f) || (Mathf.Abs(y - 64f) < 8f && x > 20f && x < 108f) ||
            (y > 96f && Mathf.Abs(x - 64f) < (122f - y) * 1.1f) || (y < 32f && Mathf.Abs(x - 64f) < (y - 6f) * 1.1f) ||
            (x > 96f && Mathf.Abs(y - 64f) < (122f - x) * 1.1f) || (x < 32f && Mathf.Abs(y - 64f) < (x - 6f) * 1.1f));
        UIEnhancer.WriteIcon("icon_g_repeat", (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f));
            float a = Mathf.Atan2(y - 64f, x - 64f) * Mathf.Rad2Deg;
            bool ring = d > 34f && d < 50f && !(a > 60f && a < 110f);
            bool head = y > 82f && y < 120f && x > 56f && x < 100f && (x - 56f) < (y - 82f) * 1.2f + 6f && x - 56f > -(y - 120f) * 1.2f - 30f;
            return ring || (head && Mathf.Abs(y - 101f) < 20f - (x - 56f) * 0.45f);
        });
        UIEnhancer.WriteIcon("icon_g_feel", (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f));
            bool ring = d >= 48f && d <= 58f;
            bool eyes = Vector2.Distance(new Vector2(x, y), new Vector2(46f, 78f)) <= 8f || Vector2.Distance(new Vector2(x, y), new Vector2(82f, 78f)) <= 8f;
            float s = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 62f));
            return ring || eyes || (y < 56f && s >= 22f && s <= 31f);
        });
    }

    private static void WriteShape(string name, Func<float, float, Color> paint, int border, bool fullRect)
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 2x2 supersampling for smooth edges
                Color sum = Color.clear;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        sum += paint(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f);
                Color c = sum / 4f;
                if (c.a > 0f) { c.r /= c.a; c.g /= c.a; c.b /= c.a; }
                tex.SetPixel(x, y, c);
            }
        }

        string path = ShapeFolder + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.spriteBorder = new Vector4(border, border, border, border);
        if (fullRect)
        {
            TextureImporterSettings s = new TextureImporterSettings();
            importer.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(s);
        }
        importer.SaveAndReimport();
    }
}
