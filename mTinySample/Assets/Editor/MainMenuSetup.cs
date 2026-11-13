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
    public const string DoNotePath = "Assets/Image/mtiny command blocks/Notes/Layer 24.png";

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

    // One colour per stage (the redesign's block-family colours)
    private static readonly Color[] StageColors =
    {
        new Color(0.24f, 0.75f, 0.42f),
        new Color(0.29f, 0.62f, 1f),
        new Color(1f, 0.79f, 0.24f),
        new Color(1f, 0.49f, 0.71f),
        new Color(0.61f, 0.42f, 1f)
    };

    // The block each stage adds, pictured on its card
    private static readonly string[] StageIcons =
    {
        "Assets/Image/mtiny command blocks/New/Layer 12.png",
        "Assets/Image/mtiny command blocks/New/x2.png",
        "Assets/Image/mtiny command blocks/New/Happy.png",
        DoNotePath,
        null
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
        // Five compact cards: the stage's block picture, its number and
        // name, and the stars earned. Tap to jump along the path.
        int stageCount = MissionStages.Count;
        ui.stageCards = new MissionMapUI.StageCard[stageCount];

        for (int s = 0; s < stageCount; s++)
        {
            MissionStage stage = (MissionStage)s;
            Vector2 cardPos = new Vector2(190f, -222f - s * 182f);

            // Highlight ring sits behind the card (drawn first)
            Image highlight = EnsureImage(root, $"cardHighlight{s}", ring, new Vector2(0f, 1f), cardPos, new Vector2(330f, 186f));
            highlight.type = Image.Type.Sliced;
            highlight.preserveAspect = false;
            highlight.color = Color.white;
            highlight.raycastTarget = false;

            Image cardImage = EnsureImage(root, $"stageCard{s}", rounded, new Vector2(0f, 1f), cardPos, new Vector2(300f, 160f));
            cardImage.type = Image.Type.Sliced;
            cardImage.preserveAspect = false;
            cardImage.color = Color.white;
            Button card = cardImage.gameObject.AddComponent<Button>();
            card.targetGraphic = cardImage;
            UnityEventTools.AddIntPersistentListener(card.onClick, ui.OnStagePressed, s);

            // Coloured tile with the stage's block
            Image tile = EnsureImage(card.transform, "iconTile", rounded, new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(116f, 116f));
            tile.type = Image.Type.Sliced;
            tile.preserveAspect = false;
            tile.color = StageColors[s];
            tile.raycastTarget = false;
            Sprite blockArt = StageIcons[s] != null ? LoadSprite(StageIcons[s]) : star;
            Image blockIcon = EnsureImage(tile.transform, "icon", blockArt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 84f));
            blockIcon.raycastTarget = false;

            TMP_Text number = Label(card.transform, "stageNo", font, MissionStages.Number(stage), 26, StageColors[s],
                new Vector2(0f, 1f), new Vector2(212f, -30f), new Vector2(160f, 34f));
            number.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text name = Label(card.transform, "name", font, MissionStages.Name(stage), 34, Ink,
                new Vector2(0f, 0.5f), new Vector2(212f, 4f), new Vector2(160f, 46f));
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.enableAutoSizing = true;
            name.fontSizeMin = 20;
            name.fontSizeMax = 34;

            Image cardStar = EnsureImage(card.transform, "starIcon", star, new Vector2(0f, 0f), new Vector2(150f, 34f), new Vector2(36f, 36f));
            cardStar.raycastTarget = false;
            TMP_Text cardStars = Label(card.transform, "stars", font, "0/30", 28, Ink, new Vector2(0f, 0f),
                new Vector2(226f, 34f), new Vector2(110f, 40f));
            cardStars.alignment = TextAlignmentOptions.MidlineLeft;

            Image cardLock = EnsureImage(tile.transform, "lockIcon", padlock, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
            cardLock.raycastTarget = false;

            Image cardTrophy = EnsureImage(card.transform, "trophyIcon", trophy, new Vector2(1f, 1f), new Vector2(-26f, -28f), new Vector2(42f, 42f));
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

        int slots = StageMapModel.TrophySlot(catalog, (MissionStage)(MissionStages.Count - 1)) + 1;
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
        for (int s = 0; s < MissionStages.Count; s++)
        {
            string stageTitle = MissionStages.Name((MissionStage)s) + " · " + MissionStages.Tagline((MissionStage)s);
            Vector2 first = SlotPosition(StageMapModel.MissionSlot(catalog, catalog.IndicesInStage((MissionStage)s)[0]));
            Image label = EnsureImage(content, $"stageLabel{s}", rounded, new Vector2(0f, 0.5f),
                new Vector2(first.x + 150f, 360f), new Vector2(420f, 76f));
            label.type = Image.Type.Sliced;
            label.preserveAspect = false;
            label.pixelsPerUnitMultiplier = 1.6f;
            label.color = StageColors[s];
            label.raycastTarget = false;
            TMP_Text stageText = Label(label.transform, "text", font, stageTitle, 38, Color.white, new Vector2(0.5f, 0.5f),
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
        ui.trophies = new MissionMapUI.StageTrophy[MissionStages.Count];
        for (int s = 0; s < MissionStages.Count; s++)
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

        ui.popupStageText = Label(cardBody.transform, "stage", font, "Let's Start! · Level 1", 34, Color.white,
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
        ui.popupSoundHint = Hint(hints, "soundHint", "Layer 24");
        ui.popupSoundHint.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DoNotePath);

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
        TMP_Text playLabel = Label(playBody.transform, "label", font, "Play", 56, StageColors[0], new Vector2(0.5f, 0.5f),
            new Vector2(30f, 0f), new Vector2(200f, 90f));

        ui.popup = popup.gameObject;
        ui.popupCard = cardBody;
        ui.popupPlayIcon = playIcon;
        ui.popupPlayLabel = playLabel;
        ui.stageColors = StageColors;
        popup.gameObject.SetActive(false);

        // Teacher reset refreshes this map
        TeacherResetHold hold = Object.FindFirstObjectByType<TeacherResetHold>(FindObjectsInactive.Include);
        if (hold != null)
        {
            hold.stageMap = ui;
            EditorUtility.SetDirty(hold);
        }

        UIEnhancer.AddTouchFeedback(scene);

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
    // SETTINGS
    // =========================================================
    // A gear button on the title screen opens Settings, where
    // "Reset progress" asks the existing Yes/No question before
    // clearing stars, unlocks and trophies.
    // =========================================================

    [MenuItem("mTiny/Setup/Add Settings")]
    public static void AddSettings()
    {
        UIEnhancer.WriteIcon("icon_gear", (x, y) =>
        {
            float dx = x - 64f, dy = y - 64f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f;
            bool tooth = r <= 58f && (angle % 45f) < 20f;
            return (r >= 22f && r <= 46f) || (r > 46f && tooth);
        });

        // Two beamed eighth notes
        UIEnhancer.WriteIcon("icon_music", (x, y) =>
        {
            bool heads = Ellipse(x, y, 34f, 30f, 18f, 13f) || Ellipse(x, y, 90f, 38f, 18f, 13f);
            bool stems = (x >= 44f && x <= 54f && y >= 30f && y <= 108f) ||
                         (x >= 100f && x <= 110f && y >= 38f && y <= 116f);
            bool beam = x >= 44f && x <= 110f && Mathf.Abs(y - (100f + (x - 44f) * 0.12f)) <= 9f;
            return heads || stems || beam;
        });

        // Speaker with two sound waves
        UIEnhancer.WriteIcon("icon_sound", (x, y) =>
        {
            bool body = x >= 14f && x <= 40f && y >= 46f && y <= 82f;
            bool cone = x >= 38f && x <= 66f && Mathf.Abs(y - 64f) <= 18f + (x - 38f) * 1.1f;
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(62f, 64f));
            bool front = x > 72f && Mathf.Abs(y - 64f) < (x - 62f) * 1.1f;
            bool waves = front && ((d >= 22f && d <= 32f) || (d >= 42f && d <= 52f));
            return body || cone || waves;
        });

        // Thick slash drawn over an icon when its sound is off
        UIEnhancer.WriteIcon("icon_slash", (x, y) =>
            Mathf.Abs(x + y - 128f) <= 13f && x >= 10f && x <= 118f);

        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        Transform bg = Find(scene, "bg");
        Transform canvas = bg.parent;
        TMP_FontAsset font = Find(scene, "Text (TMP)", Find(scene, "Explore", bg)).GetComponent<TMP_Text>().font;
        TeacherResetHold hold = Object.FindFirstObjectByType<TeacherResetHold>(FindObjectsInactive.Include);

        Sprite circle = UIEnhancer.LoadIcon("node_circle");
        Sprite rounded = UIEnhancer.LoadIcon("ui_rounded");
        Sprite gear = UIEnhancer.LoadIcon("icon_gear");
        Sprite replay = UIEnhancer.LoadIcon("icon_replay");
        Color orange = new Color(0.85f, 0.49f, 0.16f);

        foreach (string oldName in new[] { "settingsButton", "settingsPanel" })
        {
            Transform old = Find(scene, oldName, canvas);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // ---------------- PANEL ----------------
        RectTransform panel = NewRect("settingsPanel", canvas);
        Stretch(panel);
        Image dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.5f);

        Image box = EnsureImage(panel, "box", rounded, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 900f));
        box.type = Image.Type.Sliced;
        box.preserveAspect = false;
        box.color = new Color(1f, 0.95f, 0.78f);

        Label(box.transform, "title", font, "Settings", 76, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -85f), new Vector2(700f, 100f));

        // Music and Sounds switches; each one paints itself on or off
        SoundRow(box.transform, rounded, font, "musicToggle", "icon_music", SoundChannel.Music, "Music", 205f);
        VolumeRow(box.transform, rounded, circle, orange, 70f);
        SoundRow(box.transform, rounded, font, "soundToggle", "icon_sound", SoundChannel.Effects, "Sounds", -70f);

        Button resetButton = SettingsRow(box.transform, rounded, font, "resetButton", replay, "Reset progress", orange, -265f).button;

        // Reset opens the Yes/No question; Settings closes behind it
        if (hold != null && hold.confirmPanel != null)
        {
            UnityEventTools.AddBoolPersistentListener(resetButton.onClick, hold.confirmPanel.SetActive, true);
            hold.confirmPanel.transform.SetAsLastSibling();
        }
        UnityEventTools.AddBoolPersistentListener(resetButton.onClick, panel.gameObject.SetActive, false);

        Button closeButton = CircleButton(box.transform, "closeButton", circle, UIEnhancer.LoadIcon("icon_undo"), orange,
            new Vector2(1f, 1f), new Vector2(-70f, -70f), 100f);
        closeButton.GetComponentsInChildren<Image>(true).First(i => i.name == "icon").gameObject.SetActive(false);
        Label(closeButton.transform, "x", font, "X", 60, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
        UnityEventTools.AddBoolPersistentListener(closeButton.onClick, panel.gameObject.SetActive, false);

        panel.gameObject.SetActive(false);

        // ---------------- GEAR BUTTON ----------------
        Button gearButton = CircleButton(bg, "settingsButton", circle, gear, orange,
            new Vector2(1f, 1f), new Vector2(-90f, -90f), 130f);
        UnityEventTools.AddBoolPersistentListener(gearButton.onClick, panel.gameObject.SetActive, true);

        // Keep the confirm question on top of everything
        if (hold != null && hold.confirmPanel != null)
        {
            hold.confirmPanel.transform.SetAsLastSibling();
        }

        UIEnhancer.AddTouchFeedback(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[mTiny] Settings with Music, Sounds and Reset progress added to the Main Menu.");
    }

    // A wide Settings button: icon on the left, label beside it
    private static (Button button, Image icon, TMP_Text label) SettingsRow(Transform box, Sprite rounded, TMP_FontAsset font,
        string name, Sprite icon, string text, Color color, float y)
    {
        Image body = EnsureImage(box, name, rounded, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(560f, 150f));
        body.type = Image.Type.Sliced;
        body.preserveAspect = false;
        body.color = color;
        Button button = body.gameObject.AddComponent<Button>();
        button.targetGraphic = body;

        Image glyph = EnsureImage(body.transform, "icon", icon, new Vector2(0f, 0.5f), new Vector2(75f, 0f), new Vector2(84f, 84f));
        glyph.raycastTarget = false;

        TMP_Text label = Label(body.transform, "label", font, text, 54, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(65f, 0f), new Vector2(390f, 110f));
        label.enableAutoSizing = true;
        label.fontSizeMin = 30;
        label.fontSizeMax = 54;

        return (button, glyph, label);
    }

    private static void SoundRow(Transform box, Sprite rounded, TMP_FontAsset font,
        string name, string iconName, SoundChannel channel, string title, float y)
    {
        var row = SettingsRow(box, rounded, font, name, UIEnhancer.LoadIcon(iconName), title + ": ON",
            new Color(0.36f, 0.69f, 0.25f), y);

        Image slash = EnsureImage(row.icon.transform, "offSlash", UIEnhancer.LoadIcon("icon_slash"),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
        slash.color = new Color(0.85f, 0.2f, 0.15f);
        slash.raycastTarget = false;
        slash.gameObject.SetActive(false);

        SoundToggleButton toggle = row.button.gameObject.AddComponent<SoundToggleButton>();
        toggle.channel = channel;
        toggle.title = title;
        toggle.face = (Image)row.button.targetGraphic;
        toggle.label = row.label;
        toggle.offSlash = slash;
    }

    // Music volume slider: quiet speaker, chunky track, big round handle
    // a small finger can grab, loud speaker
    private static void VolumeRow(Transform box, Sprite rounded, Sprite circle, Color handleColor, float y)
    {
        Sprite speaker = UIEnhancer.LoadIcon("icon_sound");

        RectTransform row = NewRect("musicVolume", box);
        Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(560f, 110f));

        Image quiet = EnsureImage(row, "quietIcon", speaker, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(52f, 52f));
        quiet.color = Ink;
        quiet.raycastTarget = false;
        Image loud = EnsureImage(row, "loudIcon", speaker, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(80f, 80f));
        loud.color = Ink;
        loud.raycastTarget = false;

        RectTransform sliderRect = NewRect("slider", row);
        Place(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(-6f, 0f), new Vector2(380f, 100f));

        Image track = EnsureImage(sliderRect, "track", rounded, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TrackBar(track, new Color(0.84f, 0.73f, 0.52f));

        // The slider stretches the fill to its area's full height, so the
        // area is the track's height. It stops a handle-radius short of
        // the right end so the fill always ends under the handle.
        RectTransform fillArea = NewRect("fillArea", sliderRect);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(0f, -15f);
        fillArea.offsetMax = new Vector2(-45f, 15f);
        Image fill = EnsureImage(fillArea, "fill", rounded, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        TrackBar(fill, new Color(0.36f, 0.69f, 0.25f));
        Stretch(fill.rectTransform);
        fill.raycastTarget = false;

        // Inset by half the handle so it never pokes past the track ends
        RectTransform handleArea = NewRect("handleArea", sliderRect);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(45f, 0f);
        handleArea.offsetMax = new Vector2(-45f, 0f);
        Image handle = EnsureImage(handleArea, "handle", circle, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        handle.rectTransform.anchorMin = new Vector2(0f, 0f);
        handle.rectTransform.anchorMax = new Vector2(0f, 1f);
        handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        handle.rectTransform.sizeDelta = new Vector2(90f, 0f);
        handle.color = handleColor;

        Slider slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.value = SoundSettings.DefaultMusicVolume / 100f;
        sliderRect.gameObject.AddComponent<MusicVolumeSlider>();
    }

    // A rounded bar 30 px tall stretched along the slider
    private static void TrackBar(Image bar, Color color)
    {
        RectTransform rect = bar.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 30f);

        bar.type = Image.Type.Sliced;
        bar.preserveAspect = false;
        bar.pixelsPerUnitMultiplier = 3f;
        bar.color = color;
    }

    private static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float u = (x - cx) / rx, v = (y - cy) / ry;
        return u * u + v * v <= 1f;
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
