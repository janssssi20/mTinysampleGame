using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Kid-friendly, touch-first pass over the game's UI (K - Grade 1):
//  - every tap target at least ~120 px on the 1920x1080 reference canvas
//  - pictures before words, no scrollbars to aim at
//  - buttons squish when touched (no hover on touchscreens)
// Safe to re-run; each step sets absolute values.
public static class UIEnhancer
{
    private const string IconFolder = "Assets/Image/ui";

    private static readonly Color Cream = new Color(1f, 0.94f, 0.70f);
    private static readonly Color Orange = new Color(0.851f, 0.486f, 0.161f);

    [MenuItem("mTiny/Setup/Enhance UI (All Scenes)")]
    public static void EnhanceAll()
    {
        CreateIcons();

        EnhancePlayScene("Assets/Scenes/Mission.unity", true);
        EnhancePlayScene("Assets/Scenes/World.unity", false);
        EnhanceMainMenu();

        Debug.Log("[mTiny] UI enhanced in Mission, World and MainMenu.");
    }

    // =========================================================
    // MISSION / WORLD
    // =========================================================

    private static void EnhancePlayScene(string path, bool isMission)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Cream;

        CommandManager commands = Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        CommandButtonManager buttonManager = Object.FindFirstObjectByType<CommandButtonManager>(FindObjectsInactive.Include);

        // ---------------- HEADER ----------------
        // Layout along the top (reference 1920 wide):
        // home | sequence bar | undo | reset | play | timer
        RectTransform header = Rect(Find(scene, "header"));
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 200f);

        RectTransform home = Rect(Find(scene, "returnToHomeButton"));
        home.SetParent(header, false);
        Place(home, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -38f), new Vector2(130f, 120f));
        home.GetComponent<Image>().preserveAspect = true;

        RectTransform sequence = Rect(Find(scene, "blockSequenceUI"));
        sequence.localScale = Vector3.one * 0.78f;
        sequence.anchorMin = new Vector2(0f, 1f);
        sequence.anchorMax = new Vector2(0f, 1f);
        sequence.pivot = new Vector2(0f, 1f);
        sequence.anchoredPosition = new Vector2(180f, 6f);
        sequence.sizeDelta = new Vector2(1215f, 277f);

        Transform sequenceScrollbar = Find(scene, "Scrollbar", sequence);
        if (sequenceScrollbar != null) sequenceScrollbar.gameObject.SetActive(false);

        ScrollRect sequenceScroll = sequence.GetComponent<ScrollRect>();
        sequenceScroll.horizontalScrollbar = null;
        sequenceScroll.horizontal = true;
        sequenceScroll.vertical = false;
        sequenceScroll.movementType = ScrollRect.MovementType.Clamped;

        // The viewport was a zero-size point at the bar's corner, so the
        // icon strip only lined up with the bar's original anchoring.
        // Give it a real area inside the bar's frame instead.
        RectTransform sequenceViewport = Rect(Find(scene, "Viewport", sequence));
        sequenceViewport.anchorMin = new Vector2(0.5f, 0.5f);
        sequenceViewport.anchorMax = new Vector2(0.5f, 0.5f);
        sequenceViewport.pivot = new Vector2(0.5f, 0.5f);

        // The frame art keeps its aspect, so it draws smaller than its box
        // (and differently per scene). Fit the viewport to the drawn frame,
        // minus its rounded border.
        Image frame = sequence.Cast<Transform>()
            .Select(t => t.GetComponent<Image>())
            .First(i => i != null && i.transform != sequenceViewport);
        RectTransform frameRect = Rect(frame.transform);
        Vector2 drawn = frameRect.rect.size;
        if (frame.preserveAspect && frame.sprite != null)
        {
            float spriteAspect = frame.sprite.rect.width / frame.sprite.rect.height;
            drawn = drawn.x / drawn.y > spriteAspect
                ? new Vector2(drawn.y * spriteAspect, drawn.y)
                : new Vector2(drawn.x, drawn.x / spriteAspect);
        }
        sequenceViewport.anchoredPosition = frameRect.anchoredPosition;
        sequenceViewport.sizeDelta = new Vector2(drawn.x * 0.9f, drawn.y * 0.72f);
        sequenceScroll.viewport = sequenceViewport;

        // Icons line up from the left edge and the strip grows to fit them
        RectTransform sequenceContent = Rect(Find(scene, "Content", sequenceViewport));
        sequenceContent.anchorMin = new Vector2(0f, 0f);
        sequenceContent.anchorMax = new Vector2(0f, 1f);
        sequenceContent.pivot = new Vector2(0f, 0.5f);
        sequenceContent.anchoredPosition = Vector2.zero;
        sequenceContent.sizeDelta = new Vector2(0f, 0f);
        sequenceScroll.content = sequenceContent;

        HorizontalLayoutGroup strip = sequenceContent.GetComponent<HorizontalLayoutGroup>();
        strip.padding = new RectOffset(10, 10, 0, 0);
        strip.spacing = -8f;
        strip.childAlignment = TextAnchor.MiddleLeft;
        strip.childControlWidth = false;
        strip.childControlHeight = false;
        strip.childForceExpandWidth = false;
        strip.childForceExpandHeight = false;

        ContentSizeFitter stripFitter = sequenceContent.GetComponent<ContentSizeFitter>() ??
                                        sequenceContent.gameObject.AddComponent<ContentSizeFitter>();
        stripFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        stripFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform reset = Rect(Find(scene, "resetButton"));
        reset.SetParent(header, false);
        Place(reset, Vector2.one, Vector2.one, new Vector2(-460f, -32f), new Vector2(150f, 130f));
        reset.GetComponent<Image>().preserveAspect = true;

        Transform timer = Find(scene, "timerText");
        if (timer != null)
        {
            Place(Rect(timer), Vector2.one, Vector2.one, new Vector2(-22f, -60f), new Vector2(210f, 80f));
            TMP_Text timerText = timer.GetComponent<TMP_Text>();
            timerText.fontSize = 46;
            timerText.alignment = TextAlignmentOptions.Center;
        }

        // Undo: takes back the last block
        Transform oldUndo = Find(scene, "undoButton", header);
        if (oldUndo != null) Object.DestroyImmediate(oldUndo.gameObject);

        GameObject undo = new GameObject("undoButton", typeof(RectTransform), typeof(Image), typeof(Button));
        undo.layer = reset.gameObject.layer;
        undo.transform.SetParent(header, false);
        Place((RectTransform)undo.transform, Vector2.one, Vector2.one, new Vector2(-630f, -34f), new Vector2(125f, 125f));
        Image undoImage = undo.GetComponent<Image>();
        undoImage.sprite = LoadIcon("icon_undo");
        undoImage.color = Orange;
        undoImage.preserveAspect = true;
        UnityEventTools.AddPersistentListener(undo.GetComponent<Button>().onClick, commands.RemoveLastBlock);

        // Run leaves the scrolling palette and becomes a big Play button.
        // Only the first time: once it is in the header, its art, size and
        // position are the owner's (they chose the original run-block art).
        Transform run = Find(scene, "run");
        if (run.parent != header)
        {
            run.SetParent(header, false);
            Place(Rect(run), Vector2.one, Vector2.one, new Vector2(-274f, -28f), new Vector2(151f, 134f));
            Image runImage = run.GetComponent<Image>();
            runImage.sprite = LoadSprite("Assets/Image/mtiny command blocks/New/Layer 42.png");
            runImage.preserveAspect = true;
        }

        // ---------------- BLOCK PALETTE ----------------
        RectTransform palette = Rect(Find(scene, "commandblock_group"));
        palette.anchorMin = new Vector2(1f, 0f);
        palette.anchorMax = new Vector2(1f, 1f);
        palette.pivot = new Vector2(1f, 0.5f);
        // Tall enough for the flag plus all 14 blocks without scrolling
        palette.sizeDelta = new Vector2(310f, -212f);
        palette.anchoredPosition = new Vector2(-16f, -90f);

        // Soft rounded tray behind the blocks (the old sidebar art can't stretch)
        RectTransform paletteBg = Rect(Find(scene, "bg", palette));
        Stretch(paletteBg);
        Image trayImage = paletteBg.GetComponent<Image>();
        trayImage.sprite = LoadIcon("ui_rounded");
        trayImage.type = Image.Type.Sliced;
        trayImage.pixelsPerUnitMultiplier = 1f;
        trayImage.color = new Color(0.98f, 0.82f, 0.48f);

        foreach (string bar in new[] { "Scrollbar Vertical", "Scrollbar Horizontal" })
        {
            Transform t = Find(scene, bar, palette);
            if (t != null) t.gameObject.SetActive(false);
        }

        ScrollRect scroll = palette.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.verticalScrollbar = null;
        scroll.horizontalScrollbar = null;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        RectTransform viewport = Rect(Find(scene, "Viewport", palette));
        Stretch(viewport);
        viewport.offsetMin = new Vector2(12f, 12f);
        viewport.offsetMax = new Vector2(-12f, -12f);

        RectTransform content = Rect(Find(scene, "Content", palette));
        Object.DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>() ?? content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(122f, 94f);
        grid.spacing = new Vector2(14f, 8f);
        grid.padding = new RectOffset(0, 0, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, content.sizeDelta.y);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // The start flag comes first: the player presses it to begin the
        // sequence, which unlocks the other blocks. Then moves,
        // multipliers and feelings.
        string[] order =
        {
            "startFlag",
            "forward", "left", "right",
            "2x multiplier", "3x multiplier", "4x multiplier", "5x multiplier",
            "happy", "sad", "angry", "surprised", "dizzy", "sleepy", "playful"
        };

        for (int i = 0; i < order.Length; i++)
        {
            Transform block = Find(scene, order[i], content);
            if (block == null) continue;
            block.SetSiblingIndex(i);
            Image blockImage = block.GetComponent<Image>();
            if (blockImage != null) blockImage.preserveAspect = true;
        }

        // Start flag pulses until pressed (CommandButtonManager switches it off)
        Transform startFlag = Find(scene, "startFlag", content);
        if (startFlag != null)
        {
            startFlag.gameObject.SetActive(true);
            if (startFlag.GetComponent<PulseScale>() == null) startFlag.gameObject.AddComponent<PulseScale>();
            TouchButtonFeedback squish = startFlag.GetComponent<TouchButtonFeedback>();
            if (squish != null) Object.DestroyImmediate(squish);
        }

        // ---------------- INSTRUCTION CARD ----------------
        // Start only begins the mission; the player still presses the flag
        Button startGame = Find(scene, "startGame").GetComponent<Button>();
        ClearListeners(startGame.onClick);
        UnityEventTools.AddPersistentListener(startGame.onClick, commands.StartGame);

        if (isMission)
        {
            EnhanceInstructionCard(scene);
            EnhanceFinishPanels(scene);
        }

        AddTouchFeedback(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnhanceInstructionCard(Scene scene)
    {
        MissionBoardBuilder builder = Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
        builder.boardCenter = new Vector2(-1.5f, -1f);

        Transform panel = Find(scene, "instructionText").parent;

        TMP_Text title = Find(scene, "instructionLabelText").GetComponent<TMP_Text>();
        Place(Rect(title.transform), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(950f, 140f));
        title.fontSize = 110;
        title.enableAutoSizing = true;
        title.fontSizeMin = 60;
        title.fontSizeMax = 110;
        title.alignment = TextAlignmentOptions.Center;
        builder.titleText = title;

        TMP_Text instruction = Find(scene, "instructionText").GetComponent<TMP_Text>();
        Place(Rect(instruction.transform), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(900f, 150f));
        instruction.fontSize = 56;
        instruction.alignment = TextAlignmentOptions.Center;

        // Row of block pictures: the blocks this mission needs
        Transform oldRow = Find(scene, "hints", panel);
        if (oldRow != null) Object.DestroyImmediate(oldRow.gameObject);

        GameObject row = new GameObject("hints", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.layer = panel.gameObject.layer;
        row.transform.SetParent(panel, false);
        Place((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(700f, 150f));
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        builder.movementHint = Hint(row.transform, "moveHint", "Layer 12");
        builder.multiplierHint = Hint(row.transform, "multiplierHint", "x2");
        builder.emotionHint = Hint(row.transform, "emotionHint", "Happy");

        EditorUtility.SetDirty(builder);
    }

    private static GameObject Hint(Transform row, string name, string spriteName)
    {
        GameObject hint = new GameObject(name, typeof(RectTransform), typeof(Image));
        hint.layer = row.gameObject.layer;
        hint.transform.SetParent(row, false);
        ((RectTransform)hint.transform).sizeDelta = new Vector2(150f, 136f);

        Image image = hint.GetComponent<Image>();
        image.sprite = FindSpriteByName(spriteName);
        image.preserveAspect = true;
        image.raycastTarget = false;

        hint.AddComponent<PulseScale>().amount = 0.05f;

        return hint;
    }

    private static void EnhanceFinishPanels(Scene scene)
    {
        foreach (string panelName in new[] { "finishPanel", "celebrationPanel" })
        {
            Transform panel = Find(scene, panelName);
            Transform buttons = Find(scene, "buttons", panel);

            // Centre whichever buttons are showing (Next hides on the last mission)
            RectTransform row = Rect(buttons);
            row.anchoredPosition = new Vector2(0f, 14f);
            HorizontalLayoutGroup rowLayout = buttons.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 24f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            Transform stars = Find(scene, "starContainer", panel);
            if (stars != null) Rect(stars).anchoredPosition = new Vector2(0f, -50f);

            foreach (Button button in buttons.GetComponentsInChildren<Button>(true))
            {
                string icon =
                    button.name == "replayButton" ? "icon_replay" :
                    button.name == "nextButton" ? "icon_play" : "icon_home";

                // Match the pill art's proportions so icon and label sit on it
                RectTransform rect = Rect(button.transform);
                rect.sizeDelta = new Vector2(272f, 128f);
                Image pill = button.GetComponent<Image>();
                pill.preserveAspect = false;

                Image iconImage = MainMenuSetup.EnsureImage(button.transform, "icon", LoadIcon(icon),
                    new Vector2(0f, 0.5f), new Vector2(62f, 4f), new Vector2(72f, 72f));
                iconImage.raycastTarget = false;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                RectTransform labelRect = Rect(label.transform);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(100f, 0f);
                labelRect.offsetMax = new Vector2(-18f, 0f);
                label.enableWordWrapping = false;
                label.enableAutoSizing = true;
                label.fontSizeMin = 24;
                label.fontSizeMax = 42;
                if (button.name == "replayButton") label.text = "Again";
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        // Celebration: bigger, bouncier mTiny and star total
        Transform celebration = Find(scene, "celebrationPanel");
        RectTransform mtiny = Rect(Find(scene, "mtinyCelebration", celebration));
        mtiny.sizeDelta = new Vector2(400f, 410f);
        mtiny.anchoredPosition = new Vector2(330f, -40f);
        if (mtiny.GetComponent<PulseScale>() == null) mtiny.gameObject.AddComponent<PulseScale>();

        RectTransform star = Rect(Find(scene, "celebrationStar", celebration));
        star.sizeDelta = new Vector2(150f, 150f);
        star.anchoredPosition = new Vector2(-300f, -20f);
        if (star.GetComponent<PulseScale>() == null) star.gameObject.AddComponent<PulseScale>().amount = 0.12f;

        TMP_Text total = Find(scene, "finishTime", celebration).GetComponent<TMP_Text>();
        Place(Rect(total.transform), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50f, -20f), new Vector2(300f, 150f));
        total.fontSize = 110;
    }

    // =========================================================
    // MAIN MENU
    // =========================================================

    private static void EnhanceMainMenu()
    {
        // The mission map is built by MainMenuSetup.BuildMissionMap; here
        // the Main Menu only gets touch feedback on its buttons.
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        AddTouchFeedback(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // =========================================================
    // TOUCH FEEDBACK
    // =========================================================

    public static void AddTouchFeedback(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                // Animated or pulsing buttons already move on their own
                if (button.GetComponent<Animator>() != null || button.GetComponent<PulseScale>() != null)
                {
                    continue;
                }

                if (button.GetComponent<TouchButtonFeedback>() == null)
                {
                    button.gameObject.AddComponent<TouchButtonFeedback>();
                }
            }
        }
    }

    // =========================================================
    // ICONS
    // =========================================================
    // Flat white icons drawn in code (tinted where needed), because
    // the project's own icons are orange and vanish on orange buttons.
    // =========================================================

    private static void CreateIcons()
    {
        Directory.CreateDirectory(IconFolder);

        WriteIcon("icon_play", (x, y) =>
            InsideTriangle(x, y, new Vector2(40, 22), new Vector2(40, 106), new Vector2(108, 64)));

        WriteIcon("icon_home", (x, y) =>
            InsideTriangle(x, y, new Vector2(14, 66), new Vector2(114, 66), new Vector2(64, 112)) ||
            (x >= 28 && x <= 100 && y >= 16 && y <= 68 && !(x >= 54 && x <= 74 && y <= 48)));

        WriteIcon("icon_replay", (x, y) =>
        {
            float dx = x - 64f, dy = y - 60f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            bool ring = r >= 30f && r <= 46f && !(angle > 50f && angle < 110f);
            bool head = InsideTriangle(x, y, new Vector2(70, 120), new Vector2(70, 80), new Vector2(102, 100));
            return ring || head;
        });

        // Rounded panel, 9-sliced so it stretches cleanly
        WriteIcon("ui_rounded", (x, y) => InsideRoundedRect(x, y, 4f, 124f, 30f), border: 40);

        // Trail node: filled disc plus a separate white ring drawn on top
        WriteIcon("node_circle", (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f)) <= 58f);
        WriteIcon("node_ring", (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f));
            return d <= 63f && d >= 54f;
        });

        WriteIcon("icon_undo", (x, y) =>
        {
            // Backspace key: a left-pointing tag with an X cut out of it
            bool tag = (x >= 44 && x <= 116 && y >= 30 && y <= 98) ||
                       InsideTriangle(x, y, new Vector2(10, 64), new Vector2(46, 30), new Vector2(46, 98));
            float u = x - 78f, v = y - 64f;
            bool cross = Mathf.Abs(Mathf.Abs(u) - Mathf.Abs(v)) < 7f && Mathf.Abs(u) < 20f && Mathf.Abs(v) < 20f;
            return tag && !cross;
        });
    }

    public static bool InsideRoundedRect(float x, float y, float min, float max, float radius)
    {
        float cx = Mathf.Clamp(x, min + radius, max - radius);
        float cy = Mathf.Clamp(y, min + radius, max - radius);

        return x >= min && x <= max && y >= min && y <= max &&
               Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= radius;
    }

    public static void WriteIcon(string name, System.Func<float, float, bool> inside, int border = 0)
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 4x supersampling for smooth edges
                int hits = 0;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        if (inside(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f)) hits++;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 4f));
            }
        }

        string path = $"{IconFolder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.SaveAndReimport();
    }

    private static bool InsideTriangle(float x, float y, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 p = new Vector2(x, y);
        float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0;
        bool pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    private static float Cross(Vector2 p, Vector2 a, Vector2 b)
    {
        return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    public static Sprite LoadIcon(string name)
    {
        return LoadSprite($"{IconFolder}/{name}.png");
    }

    private static Sprite LoadSprite(string path)
    {
        return MainMenuSetup.LoadSprite(path);
    }

    private static Sprite FindSpriteByName(string spriteName)
    {
        foreach (string guid in AssetDatabase.FindAssets(spriteName + " t:Sprite"))
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))
                .OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
            if (sprite != null) return sprite;
        }

        Debug.LogWarning("[mTiny] Sprite not found: " + spriteName);
        return null;
    }

    private static Transform Find(Scene scene, string name, Transform under = null)
    {
        var pool = under != null
            ? under.GetComponentsInChildren<Transform>(true)
            : scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true));

        return pool.FirstOrDefault(t => t.name == name);
    }

    private static RectTransform Rect(Transform t)
    {
        return (RectTransform)t;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ClearListeners(UnityEngine.Events.UnityEventBase unityEvent)
    {
        while (unityEvent.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(unityEvent, 0);
        }
    }
}
