using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the play aids into the Mission and Explore scenes:
//  - Mission: the Try again card.
//  - Explore: free play, mode buttons (Code / Remote / Build my map),
//    the remote and the map builder.
// Safe to run again: everything it made last time is replaced.
public static class PlayAidsSetup
{
    private const string MissionPath = "Assets/Scenes/Mission.unity";
    private const string WorldPath = "Assets/Scenes/World.unity";

    private static readonly Color Ink = new Color(0.39f, 0.22f, 0.02f);
    private static readonly Color Orange = new Color(0.85f, 0.49f, 0.16f);
    private static readonly Color Green = new Color(0.36f, 0.69f, 0.25f);
    private static readonly Color Blue = new Color(0.18f, 0.53f, 0.78f);
    private static readonly Color Tray = new Color(0.98f, 0.82f, 0.48f);
    private static readonly Color Card = new Color(1f, 0.97f, 0.88f);

    [MenuItem("mTiny/Setup/Add Play Aids")]
    public static void AddAll()
    {
        WriteIcons();

        // Mission first: Explore's map builder borrows its road pieces
        MissionBoardBuilder roads = SetupMission();
        SetupWorld(roads);

        Debug.Log("[mTiny] Play aids added to Mission and Explore.");
    }


    // =========================================================
    // MISSION
    // =========================================================

    private static MissionBoardBuilder SetupMission()
    {
        Scene scene = EditorSceneManager.OpenScene(MissionPath, OpenSceneMode.Single);
        CommandManager commands = Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        TMP_FontAsset font = commands.timerText.font;
        Transform header = Find(scene, "header");
        Transform canvas = header.parent;

        SetRing(scene);

        // ---------------- TRY AGAIN CARD ----------------
        Remove(scene, "tryAgainBanner");

        Image card = Panel(canvas, "tryAgainBanner", new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(1180f, 170f), Card);
        card.rectTransform.pivot = new Vector2(0.5f, 0f);

        Image face = MainMenuSetup.EnsureImage(card.transform, "face",
            MainMenuSetup.LoadSprite("Assets/Image/mtiny command blocks/New/Sad.png"),
            new Vector2(0f, 0.5f), new Vector2(95f, 0f), new Vector2(120f, 120f));
        face.raycastTarget = false;

        TMP_Text message = Label(card.transform, "message", font, TryAgainBanner.MessageFor(TryAgainReason.Bumped), 46, Ink,
            new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(640f, 140f));
        message.enableWordWrapping = true;
        message.enableAutoSizing = true;
        message.fontSizeMin = 30;
        message.fontSizeMax = 46;

        Button tryAgain = WideButton(card.transform, "tryAgainButton", font, "Try again",
            UIEnhancer.LoadIcon("icon_replay"), Green, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(290f, 120f));

        TryAgainBanner banner = card.gameObject.AddComponent<TryAgainBanner>();
        banner.message = message;
        banner.tryAgainButton = tryAgain;
        card.gameObject.SetActive(false);
        commands.tryAgainBanner = banner;

        // Read-aloud speaker buttons from an earlier version
        Remove(scene, "readAloudButton");

        UIEnhancer.AddTouchFeedback(scene);
        EditorUtility.SetDirty(commands);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
    }


    // =========================================================
    // EXPLORE
    // =========================================================

    private static void SetupWorld(MissionBoardBuilder roads)
    {
        GameObject specialBase = roads.hospitalTilePrefab;
        GameObject eastWest = roads.cityRoadEastWest, northSouth = roads.cityRoadNorthSouth,
            northEast = roads.cityRoadNorthEast, eastSouth = roads.cityRoadEastSouth,
            southWest = roads.cityRoadSouthWest, northWest = roads.cityRoadNorthWest,
            junction = roads.cityRoadJunction;
        float cellSize = roads.cellSize;

        Scene scene = EditorSceneManager.OpenScene(WorldPath, OpenSceneMode.Single);
        CommandManager commands = Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        CommandButtonManager buttonManager = Object.FindFirstObjectByType<CommandButtonManager>(FindObjectsInactive.Include);
        TMP_FontAsset font = commands.timerText.font;
        Transform header = Find(scene, "header");
        Transform canvas = header.parent;

        commands.freePlay = true;
        SetRing(scene);

        foreach (string old in new[] { "readAloudButton", "exploreModes", "remotePanel", "buildPanel", "mapBuilder", "myMapTiles" })
        {
            Remove(scene, old);
        }

        // ---------------- MAP BUILDER ----------------
        // The board's pieces (tiles, walls, goals); the feeling, finish
        // and instruction panels kept under the same parent stay visible
        Transform platforms = Find(scene, "platforms");
        GameObject[] city = platforms.Cast<Transform>()
            .Where(t => !t.name.EndsWith("Panel"))
            .Select(t => t.gameObject)
            .ToArray();

        // The build grid sits exactly on the city's puzzle grid
        Vector3[] tilePositions = platforms.Cast<Transform>()
            .Where(t => t.name.StartsWith("puzzlepart") || t.name == "Start")
            .Select(t => t.position)
            .ToArray();
        float minX = tilePositions.Min(p => p.x), maxX = tilePositions.Max(p => p.x);
        float minY = tilePositions.Min(p => p.y), maxY = tilePositions.Max(p => p.y);
        int width = Mathf.Min(7, Mathf.RoundToInt((maxX - minX) / cellSize) + 1);
        int height = Mathf.Min(6, Mathf.RoundToInt((maxY - minY) / cellSize) + 1);
        Vector3 origin = new Vector3(minX, minY, tilePositions[0].z);

        GameObject tiles = new GameObject("myMapTiles");
        GameObject builderObject = new GameObject("mapBuilder");
        MapBuilder builder = builderObject.AddComponent<MapBuilder>();
        builder.cityBoard = city;
        builder.tilesParent = tiles.transform;
        builder.roadEastWest = eastWest;
        builder.roadNorthSouth = northSouth;
        builder.roadNorthEast = northEast;
        builder.roadEastSouth = eastSouth;
        builder.roadSouthWest = southWest;
        builder.roadNorthWest = northWest;
        builder.roadJunction = junction;
        builder.width = width;
        builder.height = height;
        builder.cellSize = cellSize;
        builder.origin = origin;
        builder.startCell = new Vector2Int(0, 0);
        builder.markerSprite = UIEnhancer.LoadIcon("ui_rounded");
        builder.buttonManager = buttonManager;

        // ---------------- MODE BUTTONS (left) ----------------
        RectTransform modes = NewRect("exploreModes", canvas);
        modes.anchorMin = modes.anchorMax = new Vector2(0f, 1f);
        modes.pivot = new Vector2(0f, 1f);
        modes.anchoredPosition = new Vector2(16f, -196f);
        modes.sizeDelta = new Vector2(320f, 560f);

        Button codeButton = ModeButton(modes, "codeMode", font, "Code", commands.forwardIcon, 0);
        Button remoteButton = ModeButton(modes, "remoteMode", font, "Remote", MainMenuSetup.LoadSprite("Assets/Image/mtinyRemote.png"), 1);
        Button buildButton = ModeButton(modes, "buildMode", font, "Build map", UIEnhancer.LoadIcon("icon_puzzle"), 2);

        // ---------------- REMOTE (right, in place of the palette) ----------------
        // Looks like a real remote: panda-ear top, a joystick to drive and
        // big round buttons to sing, smile and beep
        RectTransform remote = NewRect("remotePanel", canvas);
        remote.anchorMin = remote.anchorMax = new Vector2(1f, 0.5f);
        remote.pivot = new Vector2(1f, 0.5f);
        remote.anchoredPosition = new Vector2(-40f, -90f);
        remote.sizeDelta = new Vector2(340f, 820f);
        RemoteControl control = remote.gameObject.AddComponent<RemoteControl>();

        Sprite circle = UIEnhancer.LoadIcon("node_circle");
        Color earColor = new Color(0.22f, 0.22f, 0.25f);
        Color shell = new Color(0.82f, 0.82f, 0.86f);

        Round(remote, "earLeft", circle, earColor, new Vector2(-112f, 372f), 86f);
        Round(remote, "earRight", circle, earColor, new Vector2(112f, 372f), 86f);
        Panel(remote, "outline", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(316f, 796f), shell);
        Panel(remote, "body", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 780f), Color.white);
        Label(remote, "title", font, "mTiny", 54, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(260f, 70f));

        // Joystick: a well with arrows, and an orange knob to push
        Image well = Round(remote, "joystick", UIEnhancer.LoadIcon("joy_base"), shell, new Vector2(0f, 175f), 250f);
        well.raycastTarget = true;
        Image knob = Round(well.transform, "knob", circle, Orange, Vector2.zero, 112f);
        Round(knob.transform, "shine", UIEnhancer.LoadIcon("node_ring"), new Color(1f, 1f, 1f, 0.8f), Vector2.zero, 84f);
        RemoteJoystick stick = well.gameObject.AddComponent<RemoteJoystick>();
        stick.knob = knob.rectTransform;
        stick.radius = 80f;
        control.joystick = stick;

        RoundButton(remote, "sing", circle, new Color(0.6f, 0.33f, 0.55f), UIEnhancer.LoadIcon("icon_music"), new Vector2(-72f, -75f), control.Sing);
        RoundButton(remote, "happy", circle, new Color(0.83f, 0.45f, 0.75f), UIEnhancer.LoadIcon("icon_smile"), new Vector2(72f, -75f), control.Happy);
        RoundButton(remote, "beep", circle, Blue, UIEnhancer.LoadIcon("icon_bell"), new Vector2(0f, -205f), control.Beep);

        Label(remote, "hint", font, "Push the stick to drive!", 30, Ink, new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(260f, 80f))
            .enableWordWrapping = true;
        remote.gameObject.SetActive(false);

        // ---------------- BUILD PANEL (right) ----------------
        Image build = Panel(canvas, "buildPanel", new Vector2(1f, 0.5f), new Vector2(-40f, -90f), new Vector2(340f, 820f), Tray);
        build.rectTransform.pivot = new Vector2(1f, 0.5f);
        Label(build.transform, "title", font, "Build my map", 46, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(320f, 70f));
        Label(build.transform, "hint", font, "Pick a piece, then tap a square.",
            28, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -432f), new Vector2(290f, 80f)).enableWordWrapping = true;

        // Piece picker: plain road plus the special city pieces. Road joins
        // up by itself; specials are straight roads with a picture.
        builder.specialBase = specialBase;
        builder.specialSprites = new[] { "Layer 8", "Layer 16", "Layer 4", "Layer 24" }
            .Select(n => MainMenuSetup.LoadSprite("Assets/Image/Tilemaps1/" + n + ".png"))
            .ToArray();
        builder.specialTypes = new[] { TileType.Hospital, TileType.PoliceStation, TileType.ChildrenCrossing, TileType.TheftIncident };

        Sprite[] pictures =
        {
            eastWest.GetComponentInChildren<SpriteRenderer>().sprite,
            builder.specialSprites[0], builder.specialSprites[1], builder.specialSprites[2], builder.specialSprites[3]
        };
        string[] names = { "Road", "Hospital", "Police", "Crossing", "Thief" };
        Vector2[] spots = { new Vector2(-100f, -170f), new Vector2(0f, -170f), new Vector2(100f, -170f), new Vector2(-50f, -310f), new Vector2(50f, -310f) };
        builder.toolFrames = new Image[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            Image frame = Panel(build.transform, "piece" + names[i], new Vector2(0.5f, 1f), spots[i], new Vector2(94f, 94f), Color.white);
            Button pick = frame.gameObject.AddComponent<Button>();
            pick.targetGraphic = frame;
            UnityEventTools.AddIntPersistentListener(pick.onClick, builder.SelectPiece, i);

            Image picture = MainMenuSetup.EnsureImage(frame.transform, "picture", pictures[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78f, 78f));
            picture.raycastTarget = false;

            TMP_Text label = Label(build.transform, "label" + names[i], font, names[i], 20, Ink, new Vector2(0.5f, 1f),
                spots[i] + new Vector2(0f, -62f), new Vector2(92f, 28f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 13;
            label.fontSizeMax = 20;
            builder.toolFrames[i] = frame;
        }

        ExploreModes explore = modes.gameObject.AddComponent<ExploreModes>();

        Button clear = WideButton(build.transform, "clearButton", font, "Clear", UIEnhancer.LoadIcon("icon_undo"), Orange,
            new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(280f, 110f));
        UnityEventTools.AddPersistentListener(clear.onClick, explore.ClearMyMap);

        Button cityButton = WideButton(build.transform, "cityButton", font, "City map", UIEnhancer.LoadIcon("icon_home"), Blue,
            new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(280f, 110f));
        UnityEventTools.AddPersistentListener(cityButton.onClick, explore.BackToCity);
        build.gameObject.SetActive(false);

        // ---------------- WIRING ----------------
        explore.codeButton = (Image)codeButton.targetGraphic;
        explore.remoteButton = (Image)remoteButton.targetGraphic;
        explore.buildButton = (Image)buildButton.targetGraphic;
        explore.palette = Find(scene, "commandblock_group").gameObject;
        explore.sequenceBar = Find(scene, "blockSequenceUI").gameObject;
        explore.runButton = Find(scene, "run", header).gameObject;
        explore.undoButton = Find(scene, "undoButton", header).gameObject;
        explore.remotePanel = remote.gameObject;
        explore.buildPanel = build.gameObject;
        explore.mapBuilder = builder;
        explore.buttonManager = buttonManager;

        UnityEventTools.AddPersistentListener(codeButton.onClick, explore.ShowCode);
        UnityEventTools.AddPersistentListener(remoteButton.onClick, explore.ShowRemote);
        UnityEventTools.AddPersistentListener(buildButton.onClick, explore.ShowBuild);

        UIEnhancer.AddTouchFeedback(scene);
        EditorUtility.SetDirty(commands);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }


    // =========================================================
    // PIECES
    // =========================================================

    private static void WriteIcons()
    {
        // Ring drawn over the block mTiny is running (9-sliced)
        UIEnhancer.WriteIcon("ui_ring", (x, y) =>
            UIEnhancer.InsideRoundedRect(x, y, 4f, 124f, 30f) && !UIEnhancer.InsideRoundedRect(x, y, 16f, 112f, 20f), border: 40);

        // Joystick well: a soft ring with an arrow on each side
        UIEnhancer.WriteIcon("joy_base", (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f));
            bool well = d <= 62f;
            bool up = Mathf.Abs(x - 64f) <= (122f - y) * 0.8f && y >= 110f && y <= 122f;
            bool down = Mathf.Abs(x - 64f) <= (y - 6f) * 0.8f && y >= 6f && y <= 18f;
            bool left = Mathf.Abs(y - 64f) <= (x - 6f) * 0.8f && x >= 6f && x <= 18f;
            bool right = Mathf.Abs(y - 64f) <= (122f - x) * 0.8f && x >= 110f && x <= 122f;
            return well && !(up || down || left || right);
        });

        // Smiling face for the happy button
        UIEnhancer.WriteIcon("icon_smile", (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f));
            bool ring = d >= 48f && d <= 58f;
            bool eyes = Vector2.Distance(new Vector2(x, y), new Vector2(46f, 78f)) <= 8f ||
                        Vector2.Distance(new Vector2(x, y), new Vector2(82f, 78f)) <= 8f;
            float s2 = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 62f));
            bool smile = y < 56f && s2 >= 22f && s2 <= 31f;
            return ring || eyes || smile;
        });

        // Bell for the beep button
        UIEnhancer.WriteIcon("icon_bell", (x, y) =>
        {
            bool dome = y >= 34f && y <= 100f && Mathf.Abs(x - 64f) <= 14f + (100f - y) * 0.42f;
            bool top = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 104f)) <= 9f;
            bool rim = y >= 26f && y <= 36f && Mathf.Abs(x - 64f) <= 50f;
            bool clapper = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 16f)) <= 10f;
            return dome || top || rim || clapper;
        });

        // Puzzle piece for Build my map
        UIEnhancer.WriteIcon("icon_puzzle", (x, y) =>
        {
            bool square = x >= 24f && x <= 96f && y >= 18f && y <= 90f;
            bool topKnob = Vector2.Distance(new Vector2(x, y), new Vector2(60f, 100f)) <= 16f;
            bool sideKnob = Vector2.Distance(new Vector2(x, y), new Vector2(106f, 54f)) <= 16f;
            bool sideHole = Vector2.Distance(new Vector2(x, y), new Vector2(24f, 54f)) <= 14f;
            return (square || topKnob || sideKnob) && !sideHole;
        });
    }

    private static void SetRing(Scene scene)
    {
        SequenceUIManager bar = Object.FindFirstObjectByType<SequenceUIManager>(FindObjectsInactive.Include);
        bar.ringSprite = UIEnhancer.LoadIcon("ui_ring");
        EditorUtility.SetDirty(bar);
    }

    private static Image Round(Transform parent, string name, Sprite sprite, Color color, Vector2 position, float size)
    {
        Image image = MainMenuSetup.EnsureImage(parent, name, sprite, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void RoundButton(Transform parent, string name, Sprite circle, Color color, Sprite icon, Vector2 position, UnityAction action)
    {
        Image face = Round(parent, name, circle, color, position, 118f);
        face.raycastTarget = true;
        Button button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        UnityEventTools.AddPersistentListener(button.onClick, action);
        Round(face.transform, "icon", icon, Color.white, Vector2.zero, 66f);
    }

    private static Button ModeButton(Transform parent, string name, TMP_FontAsset font, string text, Sprite icon, int slot)
    {
        Image body = Panel(parent, name, new Vector2(0.5f, 1f), new Vector2(0f, -90f - slot * 190f), new Vector2(300f, 170f), Tray);
        Button button = body.gameObject.AddComponent<Button>();
        button.targetGraphic = body;

        Image glyph = MainMenuSetup.EnsureImage(body.transform, "icon", icon, new Vector2(0f, 0.5f), new Vector2(75f, 0f), new Vector2(100f, 100f));
        glyph.raycastTarget = false;

        TMP_Text label = Label(body.transform, "label", font, text, 42, Ink, new Vector2(0.5f, 0.5f), new Vector2(68f, 0f), new Vector2(150f, 120f));
        label.enableWordWrapping = true;
        label.enableAutoSizing = true;
        label.fontSizeMin = 26;
        label.fontSizeMax = 42;

        return button;
    }

    private static Button WideButton(Transform parent, string name, TMP_FontAsset font, string text, Sprite icon, Color color,
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        Image body = Panel(parent, name, anchor, position, size, color);
        Button button = body.gameObject.AddComponent<Button>();
        button.targetGraphic = body;

        Image glyph = MainMenuSetup.EnsureImage(body.transform, "icon", icon, new Vector2(0f, 0.5f), new Vector2(size.y * 0.55f, 0f),
            new Vector2(size.y * 0.6f, size.y * 0.6f));
        glyph.raycastTarget = false;

        TMP_Text label = Label(body.transform, "label", font, text, 46, Color.white, new Vector2(0.5f, 0.5f),
            new Vector2(size.y * 0.4f, 0f), new Vector2(size.x - size.y * 1.1f, size.y * 0.8f));
        label.enableAutoSizing = true;
        label.fontSizeMin = 26;
        label.fontSizeMax = 46;

        return button;
    }

    private static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        Image image = MainMenuSetup.EnsureImage(parent, name, UIEnhancer.LoadIcon("ui_rounded"), anchor, position, size);
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = color;
        return image;
    }

    private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, string text, float size, Color color,
        Vector2 anchor, Vector2 position, Vector2 box)
    {
        RectTransform rect = NewRect(name, parent);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = box;

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

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Remove(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToList())
            {
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            if (root != null && root.name == name) Object.DestroyImmediate(root);
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
}
