using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Turns the Main Menu's three-button mission panel into stage cards plus
// a left-to-right mission map (MissionMapUI). Safe to re-run.
public static class MainMenuSetup
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";
    public const string PadlockPath = "Assets/Image/ui/padlock.png";
    public const string StarPath = "Assets/Image/star (1).png";

    // =========================================================
    // MISSION MAP
    // =========================================================
    // Replaces the old stage-card + trail panel with one screen:
    // stage cards down the left (quick jump) and a left-to-right
    // winding path of every mission with a trophy after each stage.
    // =========================================================

    private const float SlotSpacing = 200f;
    private const float PathMargin = 150f;
    private const float WaveHeight = 165f;

    private static readonly Color[] StageColors =
    {
        new Color(0.91f, 0.47f, 0.16f),
        new Color(0.36f, 0.68f, 0.24f),
        new Color(0.58f, 0.33f, 0.66f)
    };

    private static readonly Color Ink = new Color(0.39f, 0.22f, 0.02f);
    private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.2f);

    [MenuItem("mTiny/Setup/Build Mission Map")]
    public static void BuildMissionMap()
    {
        CreateMapIcons();

        Sprite padlock = EnsurePadlockSprite();
        Sprite star = LoadSprite(StarPath);
        Sprite rounded = UIEnhancer.LoadIcon("ui_rounded");
        Sprite ring = UIEnhancer.LoadIcon("ui_rounded_ring");
        Sprite circle = UIEnhancer.LoadIcon("node_circle");
        Sprite nodeRing = UIEnhancer.LoadIcon("node_ring");
        Sprite trophy = UIEnhancer.LoadIcon("icon_trophy");
        Sprite home = UIEnhancer.LoadIcon("icon_home");
        Sprite play = UIEnhancer.LoadIcon("icon_play");
        Sprite dot = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        MissionCatalog catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);

        Transform bg = Find(scene, "bg");
        GameObject missionPanel = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .First(t => t.name == "missionPanel" && t.GetComponent<Button>() == null).gameObject;

        TMP_FontAsset font = Find(scene, "Text (TMP)", Find(scene, "Explore", bg)).GetComponent<TMP_Text>().font;
        Animator titleMtiny = Find(scene, "Image", bg).GetComponent<Animator>();

        // Card art from the old stage cards, before they go
        Sprite[] cardArt = new Sprite[3];
        string[] oldCards = { "easyCard", "mediumCard", "hardCard" };
        for (int i = 0; i < 3; i++)
        {
            Transform old = Find(scene, oldCards[i], missionPanel.transform);
            cardArt[i] = old != null ? old.GetComponent<Image>().sprite : rounded;
        }

        foreach (string oldName in new[] { "mainMenuMissionPanel", "missionMap" })
        {
            Transform old = Find(scene, oldName, missionPanel.transform);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        RectTransform root = NewRect("missionMap", missionPanel.transform);
        Stretch(root);
        MissionMapUI ui = root.gameObject.AddComponent<MissionMapUI>();
        ui.catalog = catalog;

        // Solid backdrop so the title screen never shows through
        Image backdrop = root.gameObject.AddComponent<Image>();
        backdrop.color = new Color(1f, 0.95f, 0.72f);

        // ---------------- HEADER ----------------
        RectTransform header = NewRect("header", root);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 130f);
        header.anchoredPosition = Vector2.zero;

        Button homeButton = CircleButton(header, "homeButton", circle, home, StageColors[0],
            new Vector2(0f, 0.5f), new Vector2(95f, 0f), 110f);
        UnityEventTools.AddBoolPersistentListener(homeButton.onClick, missionPanel.SetActive, false);

        Label(header, "title", font, "Missions", 72, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 110f));

        Image totalStar = EnsureImage(header, "totalStar", star, new Vector2(1f, 0.5f), new Vector2(-290f, 0f), new Vector2(72f, 72f));
        totalStar.raycastTarget = false;
        ui.totalStarsText = Label(header, "totalStars", font, "0/45", 58, Ink, new Vector2(1f, 0.5f),
            new Vector2(-150f, 0f), new Vector2(220f, 90f));
        ui.totalStarsText.alignment = TextAlignmentOptions.MidlineLeft;

        // ---------------- STAGE CARDS (LEFT) ----------------
        string[] stageNames = { "EASY", "MEDIUM", "HARD" };
        ui.stageCards = new MissionMapUI.StageCard[3];

        for (int s = 0; s < 3; s++)
        {
            Vector2 cardPos = new Vector2(190f, -250f - s * 290f);

            // Highlight ring sits behind the card (drawn first)
            Image highlight = EnsureImage(root, $"cardHighlight{s}", ring, new Vector2(0f, 1f), cardPos, new Vector2(318f, 284f));
            highlight.type = Image.Type.Sliced;
            highlight.preserveAspect = false;
            highlight.color = Color.white;
            highlight.raycastTarget = false;

            Image cardImage = EnsureImage(root, $"{stageNames[s].ToLower()}Card", cardArt[s], new Vector2(0f, 1f), cardPos, new Vector2(280f, 254f));
            cardImage.preserveAspect = true;
            Button card = cardImage.gameObject.AddComponent<Button>();
            card.targetGraphic = cardImage;
            UnityEventTools.AddIntPersistentListener(card.onClick, ui.OnStagePressed, s);

            Image ribbon = EnsureImage(card.transform, "ribbon", rounded, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(230f, 62f));
            ribbon.type = Image.Type.Sliced;
            ribbon.preserveAspect = false;
            ribbon.pixelsPerUnitMultiplier = 1.6f;
            ribbon.color = StageColors[s];
            ribbon.raycastTarget = false;
            TMP_Text ribbonLabel = Label(ribbon.transform, "label", font, stageNames[s], 40, Color.white,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 60f));
            ribbonLabel.enableAutoSizing = true;
            ribbonLabel.fontSizeMin = 22;
            ribbonLabel.fontSizeMax = 40;

            Image cardStar = EnsureImage(card.transform, "starIcon", star, new Vector2(0.5f, 0f), new Vector2(-60f, 44f), new Vector2(46f, 46f));
            cardStar.raycastTarget = false;
            TMP_Text cardStars = Label(card.transform, "stars", font, "0/15", 38, Ink, new Vector2(0.5f, 0f),
                new Vector2(30f, 44f), new Vector2(140f, 56f));
            cardStars.alignment = TextAlignmentOptions.MidlineLeft;

            Image cardLock = EnsureImage(card.transform, "lockIcon", padlock, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(120f, 120f));
            cardLock.raycastTarget = false;

            Image cardTrophy = EnsureImage(card.transform, "trophyIcon", trophy, new Vector2(1f, 1f), new Vector2(-28f, -88f), new Vector2(58f, 58f));
            cardTrophy.raycastTarget = false;

            ui.stageCards[s] = new MissionMapUI.StageCard
            {
                button = card,
                starsText = cardStars,
                lockIcon = cardLock.gameObject,
                trophyIcon = cardTrophy,
                highlight = highlight.gameObject
            };
        }

        // ---------------- PATH (SCROLLS LEFT TO RIGHT) ----------------
        RectTransform scrollRect = NewRect("pathScroll", root);
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(360f, 20f);
        scrollRect.offsetMax = new Vector2(-20f, -140f);
        Image scrollBg = scrollRect.gameObject.AddComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.001f);
        ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.decelerationRate = 0.12f;

        RectTransform viewport = NewRect("Viewport", scrollRect);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        int slots = StageMapModel.TrophySlot(catalog, MissionStage.Hard) + 1;
        RectTransform content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(PathMargin * 2f + (slots - 1) * SlotSpacing, 0f);

        scroll.viewport = viewport;
        scroll.content = content;
        ui.pathScroll = scroll;
        ui.pathContent = content;

        // Dotted road between neighbouring slots, drawn first
        for (int slot = 0; slot + 1 < slots; slot++)
        {
            Vector2 a = SlotPosition(slot), b = SlotPosition(slot + 1);
            for (int d = 1; d < 5; d++)
            {
                Image roadDot = EnsureImage(content, $"dot {slot}-{d}", dot, new Vector2(0f, 0.5f),
                    Vector2.Lerp(a, b, d / 5f), new Vector2(26f, 26f));
                roadDot.color = new Color(0.94f, 0.82f, 0.48f);
                roadDot.raycastTarget = false;
            }
        }

        // Stage labels where each stage's part of the path begins
        string[] stageTitles = { "Easy · Learn to move", "Medium · Repeat", "Hard · Feelings and city" };
        for (int s = 0; s < 3; s++)
        {
            Vector2 first = SlotPosition(StageMapModel.MissionSlot(catalog, catalog.IndicesInStage((MissionStage)s)[0]));
            Image label = EnsureImage(content, $"stageLabel{s}", rounded, new Vector2(0f, 0.5f),
                new Vector2(first.x + 150f, 360f), new Vector2(420f, 76f));
            label.type = Image.Type.Sliced;
            label.preserveAspect = false;
            label.pixelsPerUnitMultiplier = 1.6f;
            label.color = StageColors[s];
            label.raycastTarget = false;
            TMP_Text stageText = Label(label.transform, "text", font, stageTitles[s], 38, Color.white, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400f, 70f));
            stageText.enableAutoSizing = true;
            stageText.fontSizeMin = 24;
            stageText.fontSizeMax = 38;
        }

        // Missions
        ui.nodes = new MissionMapUI.PathNode[catalog.Count];
        for (int i = 0; i < catalog.Count; i++)
        {
            Vector2 pos = SlotPosition(StageMapModel.MissionSlot(catalog, i));
            int number = catalog.IndicesInStage(catalog.Get(i).stage).IndexOf(i) + 1;

            RectTransform node = NewRect($"mission{i + 1}", content);
            Place(node, new Vector2(0f, 0.5f), pos, new Vector2(150f, 150f));

            // Chunky 3D button: dark base below a coloured face
            Image baseImage = EnsureImage(node, "base", circle, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(150f, 150f));
            baseImage.color = Shadow;
            baseImage.raycastTarget = false;

            Image face = EnsureImage(node, "face", circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            Image faceRing = EnsureImage(node, "ring", nodeRing, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            faceRing.raycastTarget = false;

            TMP_Text label = Label(node, "number", font, number.ToString(), 80, Color.white, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 4f), new Vector2(140f, 120f));

            Image[] stars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                stars[s] = EnsureImage(node, $"star{s + 1}", star, new Vector2(0.5f, 0.5f),
                    new Vector2((s - 1) * 52f, 108f), new Vector2(50f, 50f));
                stars[s].raycastTarget = false;
            }

            Image nodeLock = EnsureImage(node, "lockIcon", padlock, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
            nodeLock.raycastTarget = false;

            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            UnityEventTools.AddIntPersistentListener(button.onClick, ui.OnNodePressed, i);
            node.gameObject.AddComponent<PulseScale>().enabled = false;

            ui.nodes[i] = new MissionMapUI.PathNode
            {
                button = button,
                face = face,
                number = label,
                stars = stars,
                lockIcon = nodeLock.gameObject
            };
        }

        // Trophies: one after each stage, coloured bronze / silver / gold
        ui.trophies = new MissionMapUI.StageTrophy[3];
        for (int s = 0; s < 3; s++)
        {
            Vector2 pos = SlotPosition(StageMapModel.TrophySlot(catalog, (MissionStage)s));

            RectTransform cup = NewRect($"trophy{s + 1}", content);
            Place(cup, new Vector2(0f, 0.5f), pos, new Vector2(150f, 150f));

            Image cupBase = EnsureImage(cup, "base", rounded, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(140f, 140f));
            cupBase.type = Image.Type.Sliced;
            cupBase.preserveAspect = false;
            cupBase.color = Shadow;
            cupBase.raycastTarget = false;

            Image plate = EnsureImage(cup, "plate", rounded, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 140f));
            plate.type = Image.Type.Sliced;
            plate.preserveAspect = false;
            plate.color = new Color(1f, 0.97f, 0.86f);
            plate.raycastTarget = false;

            Image icon = EnsureImage(cup, "icon", trophy, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));
            icon.raycastTarget = false;

            cup.gameObject.AddComponent<PulseScale>().enabled = false;

            ui.trophies[s] = new MissionMapUI.StageTrophy { rect = cup, icon = icon };
        }

        // START bubble and mTiny at the next mission
        Image bubble = EnsureImage(content, "startBubble", rounded, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(170f, 64f));
        bubble.type = Image.Type.Sliced;
        bubble.preserveAspect = false;
        bubble.pixelsPerUnitMultiplier = 1.6f;
        bubble.color = Color.white;
        bubble.raycastTarget = false;
        Label(bubble.transform, "text", font, "START", 40, StageColors[0], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 60f));
        bubble.gameObject.AddComponent<PulseScale>().amount = 0.06f;
        ui.startBubble = (RectTransform)bubble.transform;

        Image marker = EnsureImage(content, "mtinyMarker", titleMtiny.GetComponent<Image>().sprite, new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(130f, 134f));
        marker.raycastTarget = false;
        marker.gameObject.AddComponent<Animator>().runtimeAnimatorController = titleMtiny.runtimeAnimatorController;
        ui.mtinyMarker = (RectTransform)marker.transform;

        // ---------------- CONFETTI ----------------
        RectTransform confettiLayer = NewRect("confetti", root);
        Stretch(confettiLayer);
        ConfettiBurst burst = confettiLayer.gameObject.AddComponent<ConfettiBurst>();
        burst.pieceSprite = rounded;
        ui.confetti = burst;

        // ---------------- MISSION CARD ----------------
        RectTransform popup = NewRect("missionCard", root);
        Stretch(popup);
        Image dim = popup.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.45f);
        Button dimButton = popup.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(dimButton.onClick, ui.ClosePopup);

        Image cardBody = EnsureImage(popup, "card", rounded, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 470f));
        cardBody.type = Image.Type.Sliced;
        cardBody.preserveAspect = false;
        cardBody.color = StageColors[0];

        ui.popupStageText = Label(cardBody.transform, "stage", font, "Easy · Mission 1", 34, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(560f, 50f));
        ui.popupTitleText = Label(cardBody.transform, "title", font, "Mission", 62, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(580f, 80f));
        ui.popupTitleText.enableAutoSizing = true;
        ui.popupTitleText.fontSizeMin = 36;
        ui.popupTitleText.fontSizeMax = 62;

        RectTransform hints = NewRect("hints", cardBody.transform);
        Place(hints, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 120f));
        HorizontalLayoutGroup hintLayout = hints.gameObject.AddComponent<HorizontalLayoutGroup>();
        hintLayout.spacing = 26f;
        hintLayout.childAlignment = TextAnchor.MiddleCenter;
        hintLayout.childControlWidth = false;
        hintLayout.childControlHeight = false;
        hintLayout.childForceExpandWidth = false;
        hintLayout.childForceExpandHeight = false;
        ui.popupMoveHint = Hint(hints, "moveHint", "Layer 12");
        ui.popupMultiplierHint = Hint(hints, "multiplierHint", "x2");
        ui.popupEmotionHint = Hint(hints, "emotionHint", "Happy");

        Image playBody = EnsureImage(cardBody.transform, "playButton", rounded, new Vector2(0.5f, 0f),
            new Vector2(0f, 95f), new Vector2(440f, 120f));
        playBody.type = Image.Type.Sliced;
        playBody.preserveAspect = false;
        playBody.color = Color.white;
        Button playButton = playBody.gameObject.AddComponent<Button>();
        playButton.targetGraphic = playBody;
        UnityEventTools.AddPersistentListener(playButton.onClick, ui.OnPlayPressed);
        Image playIcon = EnsureImage(playBody.transform, "icon", play, new Vector2(0.5f, 0.5f), new Vector2(-80f, 0f), new Vector2(64f, 64f));
        playIcon.color = StageColors[0];
        playIcon.raycastTarget = false;
        Label(playBody.transform, "label", font, "Play", 56, StageColors[0], new Vector2(0.5f, 0.5f),
            new Vector2(30f, 0f), new Vector2(200f, 90f));

        ui.popup = popup.gameObject;
        popup.gameObject.SetActive(false);

        // Teacher reset refreshes this map
        TeacherResetHold hold = Object.FindFirstObjectByType<TeacherResetHold>(FindObjectsInactive.Include);
        if (hold != null)
        {
            hold.stageMap = ui;
            EditorUtility.SetDirty(hold);
        }

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[mTiny] Mission map built in MainMenu.");
    }

    // Winding left-to-right path: x by slot, y on a gentle wave
    private static Vector2 SlotPosition(int slot)
    {
        return new Vector2(PathMargin + slot * SlotSpacing, Mathf.Sin(slot * 0.85f) * WaveHeight - 30f);
    }

    private static Button CircleButton(Transform parent, string name, Sprite circle, Sprite icon, Color color,
        Vector2 anchor, Vector2 position, float size)
    {
        Image face = EnsureImage(parent, name, circle, anchor, position, new Vector2(size, size));
        face.color = color;
        Button button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        Image glyph = EnsureImage(face.transform, "icon", icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.55f, size * 0.55f));
        glyph.raycastTarget = false;
        return button;
    }

    private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, string text, float size, Color color,
        Vector2 anchor, Vector2 position, Vector2 box)
    {
        RectTransform rect = NewRect(name, parent);
        Place(rect, anchor, position, box);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }

    private static GameObject Hint(Transform row, string name, string spriteName)
    {
        RectTransform rect = NewRect(name, row);
        rect.sizeDelta = new Vector2(132f, 112f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.FindAssets(spriteName + " t:Sprite")
            .SelectMany(g => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)).OfType<Sprite>())
            .FirstOrDefault(sp => sp.name == spriteName);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return rect.gameObject;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void CreateMapIcons()
    {
        UIEnhancer.WriteIcon("icon_trophy", (x, y) =>
        {
            float bowl = (x - 64f) * (x - 64f) / (34f * 34f) + (y - 112f) * (y - 112f) / (58f * 58f);
            bool cup = y <= 112f && y >= 56f && bowl <= 1f;
            bool rim = y > 104f && y <= 114f && x >= 30f && x <= 98f;
            float hl = Vector2.Distance(new Vector2(x, y), new Vector2(30f, 92f));
            float hr = Vector2.Distance(new Vector2(x, y), new Vector2(98f, 92f));
            bool handles = (hl >= 10f && hl <= 18f && x < 34f) || (hr >= 10f && hr <= 18f && x > 94f);
            bool stem = x >= 57f && x <= 71f && y >= 32f && y <= 60f;
            bool baseBlock = x >= 38f && x <= 90f && y >= 14f && y <= 32f;
            return cup || rim || handles || stem || baseBlock;
        });

        UIEnhancer.WriteIcon("ui_rounded_ring", (x, y) =>
            UIEnhancer.InsideRoundedRect(x, y, 4f, 124f, 30f) && !UIEnhancer.InsideRoundedRect(x, y, 14f, 114f, 22f),
            border: 40);
    }

    [MenuItem("mTiny/Setup/Add Teacher Reset")]
    public static void AddTeacherReset()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        Transform bg = Find(scene, "bg");
        Image title = bg.GetComponentsInChildren<Image>(true).First(i => i.sprite != null && i.sprite.name == "mtinyTitle");
        title.raycastTarget = true;

        TeacherResetHold hold = title.GetComponent<TeacherResetHold>() ?? title.gameObject.AddComponent<TeacherResetHold>();
        hold.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        hold.stageMap = Object.FindFirstObjectByType<MissionMapUI>(FindObjectsInactive.Include);

        // Confirm panel: a dimmed full-screen layer over the whole canvas
        Transform canvas = bg.parent;
        GameObject oldPanel = Find(scene, "resetConfirmPanel", canvas)?.gameObject;
        if (oldPanel != null) Object.DestroyImmediate(oldPanel);

        RectTransform panel = NewRect("resetConfirmPanel", canvas);
        Stretch(panel);
        panel.SetAsLastSibling();
        Image dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);

        Image box = EnsureImage(panel, "box", Find(scene, "Panel", bg).GetComponent<Image>().sprite,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 500f));
        box.preserveAspect = false;
        box.type = Image.Type.Sliced;

        TMP_Text template = Find(scene, "Text (TMP)", Find(scene, "Explore", bg)).GetComponent<TMP_Text>();

        TextMeshProUGUI question = NewRect("question", box.transform).gameObject.AddComponent<TextMeshProUGUI>();
        RectTransform questionRect = (RectTransform)question.transform;
        questionRect.anchoredPosition = new Vector2(0f, 90f);
        questionRect.sizeDelta = new Vector2(780f, 160f);
        question.font = template.font;
        question.fontSize = 60;
        question.color = template.color;
        question.alignment = TextAlignmentOptions.Center;
        question.text = "Reset all progress?";

        GameObject explore = Find(scene, "Explore", bg).gameObject;
        CreateConfirmButton(explore, box.transform, "yesButton", "Yes", new Vector2(-190f, -110f), hold.ConfirmYes);
        CreateConfirmButton(explore, box.transform, "noButton", "No", new Vector2(190f, -110f), hold.ConfirmNo);

        hold.confirmPanel = panel.gameObject;
        panel.gameObject.SetActive(false);

        EditorUtility.SetDirty(hold);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[mTiny] Teacher reset added to the mTiny title.");
    }

    private static void CreateConfirmButton(GameObject template, Transform parent, string name, string label,
        Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject button = Object.Instantiate(template, parent);
        button.name = name;

        // Keep only the label; the template's icon belongs to Explore
        foreach (Image icon in button.GetComponentsInChildren<Image>(true).Where(i => i.gameObject != button).ToList())
        {
            Object.DestroyImmediate(icon.gameObject);
        }

        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(320f, 150f);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        Stretch((RectTransform)text.transform);
        text.alignment = TextAlignmentOptions.Center;

        Button uiButton = button.GetComponent<Button>();
        ClearListeners(uiButton.onClick);
        UnityEventTools.AddPersistentListener(uiButton.onClick, action);
    }

    // =========================================================
    // PADLOCK SPRITE
    // =========================================================
    // A flat grey padlock drawn in code; the project has no lock art.
    // =========================================================

    public static Sprite EnsurePadlockSprite()
    {
        Sprite existing = LoadSprite(PadlockPath);
        if (existing != null) return existing;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color body = new Color(0.36f, 0.38f, 0.42f, 1f);
        Color hole = new Color(0.9f, 0.9f, 0.9f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = clear;

                // Shackle: ring around (64, 78), radius 26-38, upper half only
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 78f));
                if (y >= 70 && dist >= 26f && dist <= 38f) c = body;

                // Body: rounded rectangle
                if (x >= 22 && x <= 106 && y >= 8 && y <= 74 &&
                    !(Corner(x, y, 22, 8) || Corner(x, y, 106, 8) || Corner(x, y, 22, 74) || Corner(x, y, 106, 74)))
                {
                    c = body;
                }

                // Keyhole
                if (Vector2.Distance(new Vector2(x, y), new Vector2(64f, 48f)) <= 9f ||
                    (x >= 60 && x <= 68 && y >= 24 && y <= 48))
                {
                    c = hole;
                }

                tex.SetPixel(x, y, c);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PadlockPath));
        File.WriteAllBytes(PadlockPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(PadlockPath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PadlockPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return LoadSprite(PadlockPath);
    }

    private static bool Corner(int x, int y, int cx, int cy)
    {
        const int r = 10;
        int dx = x < cx ? cx - x : x - cx;
        int dy = y < cy ? cy - y : y - cy;
        return dx < r && dy < r && (r - dx) * (r - dx) + (r - dy) * (r - dy) > r * r;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    public static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    public static Image EnsureImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null ? (RectTransform)existing : NewRect(name, parent);

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        return image;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
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

    private static Transform Find(Scene scene, string name, Transform under = null)
    {
        IEnumerable<Transform> pool = under != null
            ? under.GetComponentsInChildren<Transform>(true)
            : scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true));

        return pool.FirstOrDefault(t => t.name == name);
    }
}
