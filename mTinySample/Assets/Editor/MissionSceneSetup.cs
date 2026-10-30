using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-time setup for the single Mission scene, plus a check that every
// mission in the catalog builds a valid board. Safe to re-run.
public static class MissionSceneSetup
{
    private const string TileFolder = "Assets/Prefab/Tiles";
    private const string MissionScenePath = "Assets/Scenes/Mission.unity";
    private const string TemplateScenePath = "Assets/Scenes/Mission1.unity";
    private const string WorldScenePath = "Assets/Scenes/World.unity";
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";

    // Board must stay inside the area Mission1-3 used for their tiles
    // Tile centres must stay clear of the header (top) and block palette (right)
    private static readonly Rect PlayArea = new Rect(-8.0f, -4.0f, 13.0f, 6.1f);

    [MenuItem("mTiny/Setup/Create Mission Scene")]
    public static void CreateMissionScene()
    {
        CreateTilePrefabs();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MissionScenePath) == null)
        {
            AssetDatabase.CopyAsset(TemplateScenePath, MissionScenePath);
        }

        Scene scene = EditorSceneManager.OpenScene(MissionScenePath, OpenSceneMode.Single);

        RemoveHandPlacedBoard(scene);

        GameObject board = FindInScene(scene, "Board") ?? new GameObject("Board");
        SceneManager.MoveGameObjectToScene(board, scene);

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        MissionBoardBuilder builder = gameManager.GetComponent<MissionBoardBuilder>() ??
                                      gameManager.gameObject.AddComponent<MissionBoardBuilder>();

        builder.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        builder.pathTilePrefab = LoadTile("PathTile");
        builder.startTilePrefab = LoadTile("StartTile");
        builder.goalTilePrefab = LoadTile("GoalTile");
        builder.hospitalTilePrefab = LoadTile("HospitalTile");
        builder.crossingTilePrefab = LoadTile("CrossingTile");
        builder.policeTilePrefab = LoadTile("PoliceTile");
        builder.boardParent = board.transform;
        builder.gameManager = gameManager;
        builder.missionManager = Object.FindFirstObjectByType<MissionManager>(FindObjectsInactive.Include);
        builder.instructionText = FindInScene(scene, "instructionText").GetComponent<TMP_Text>();

        builder.movementBlocks = FindAll(scene, "forward", "left", "right");
        builder.multiplierBlocks = FindAll(scene, "2x multiplier", "3x multiplier", "4x multiplier", "5x multiplier");
        builder.emotionBlocks = FindAll(scene, "happy", "sad", "angry", "surprised", "dizzy", "sleepy", "playful");

        EditorUtility.SetDirty(builder);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene(MissionScenePath, true),
            new EditorBuildSettingsScene(WorldScenePath, true)
        };

        Debug.Log("[mTiny] Mission scene built and Build Settings updated.");
    }

    [MenuItem("mTiny/Setup/Validate Mission Boards")]
    public static void ValidateBoards()
    {
        Scene scene = EditorSceneManager.OpenScene(MissionScenePath, OpenSceneMode.Single);
        MissionBoardBuilder builder = Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);

        int failures = 0;

        for (int i = 0; i < builder.catalog.Count; i++)
        {
            Transform start = builder.Build(i);
            MissionDefinition mission = builder.Current;

            List<string> problems = new List<string>();

            int expectedTiles = mission.path.Count + 2;

            if (builder.boardParent.childCount != expectedTiles)
                problems.Add($"{builder.boardParent.childCount} tiles, expected {expectedTiles}");

            if (builder.gameManager.startPoint != start)
                problems.Add("GameManager.startPoint not set to the start tile");

            foreach (Transform tile in builder.boardParent)
            {
                if (!PlayArea.Contains((Vector2)tile.position))
                    problems.Add($"{tile.name} outside play area at {tile.position}");
            }

            int cityTiles = builder.boardParent.GetComponentsInChildren<SpecialTile>().Count(t => t.tileType != TileType.None);

            if (cityTiles != mission.cityTiles.Count)
                problems.Add($"{cityTiles} city tiles, expected {mission.cityTiles.Count}");

            foreach (Transform tile in builder.boardParent)
            {
                // Tiles must cover exactly one grid cell in world space
                Vector2 colliderSize = tile.GetComponent<Collider2D>().bounds.size;
                Vector2 spriteSize = tile.GetComponent<SpriteRenderer>().bounds.size;

                if (colliderSize.x > builder.cellSize * 1.05f || colliderSize.x < builder.cellSize * 0.3f)
                    problems.Add($"{tile.name} collider is {colliderSize.x:F2} wide, cell is {builder.cellSize}");

                if (spriteSize.x > builder.cellSize * 1.2f || spriteSize.x < builder.cellSize * 0.5f)
                    problems.Add($"{tile.name} sprite is {spriteSize.x:F2} wide, cell is {builder.cellSize}");

                // RobotController reads the first SpecialTile on the tile
                SpecialTile special = tile.GetComponent<SpecialTile>();
                SpecialTile[] specials = tile.GetComponents<SpecialTile>();

                if (specials.Length > 1)
                    problems.Add($"{tile.name} has {specials.Length} SpecialTile components");

                if (special != null && special.tileType == TileType.None && specials.Any(s => s.tileType != TileType.None))
                    problems.Add($"{tile.name} first SpecialTile is None");
            }

            if (problems.Count == 0)
            {
                Debug.Log($"[mTiny] OK {mission.id}");
            }
            else
            {
                failures++;
                Debug.LogError($"[mTiny] FAIL {mission.id}: {string.Join("; ", problems)}");
            }
        }

        // Leave the saved scene without a pre-built board
        EditorSceneManager.OpenScene(MissionScenePath, OpenSceneMode.Single);

        Debug.Log($"[mTiny] Validation finished: {builder.catalog.Count - failures}/{builder.catalog.Count} boards OK.");

        if (Application.isBatchMode && failures > 0)
        {
            EditorApplication.Exit(3);
        }
    }

    [MenuItem("mTiny/Setup/Add Finish Flow")]
    public static void AddFinishFlow()
    {
        // mTiny's excited animation from the Main Menu
        Scene menu = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        Animator menuMtiny = FindInScene(menu, "Image").GetComponent<Animator>();
        RuntimeAnimatorController mtinyController = menuMtiny.runtimeAnimatorController;
        Sprite mtinySprite = menuMtiny.GetComponent<Image>().sprite;

        Scene scene = EditorSceneManager.OpenScene(MissionScenePath, OpenSceneMode.Single);

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        MissionRunner runner = gameManager.GetComponent<MissionRunner>() ??
                               gameManager.gameObject.AddComponent<MissionRunner>();

        GameObject finishPanel = FindInScene(scene, "finishPanel");
        Transform buttons = FindChild(finishPanel.transform, "buttons");
        Button replay = FindChild(buttons, "replayButton").GetComponent<Button>();

        // ---------------- NEXT BUTTON ----------------
        Transform existingNext = FindChild(buttons, "nextButton");
        if (existingNext != null) Object.DestroyImmediate(existingNext.gameObject);

        GameObject next = Object.Instantiate(replay.gameObject, buttons);
        next.name = "nextButton";
        next.transform.SetAsLastSibling();
        next.GetComponentInChildren<TMP_Text>(true).text = "Next";

        foreach (RectTransform button in buttons)
        {
            button.sizeDelta = new Vector2(300f, button.sizeDelta.y);
        }

        SetOnlyListener(replay.onClick, runner.Replay);
        SetOnlyListener(next.GetComponent<Button>().onClick, runner.Next);

        // ---------------- CELEBRATION PANEL ----------------
        GameObject oldCelebration = FindInScene(scene, "celebrationPanel");
        if (oldCelebration != null) Object.DestroyImmediate(oldCelebration);

        GameObject celebration = Object.Instantiate(finishPanel, finishPanel.transform.parent);
        celebration.name = "celebrationPanel";
        celebration.SetActive(false);

        Transform panel = FindChild(celebration.transform, "Panel");
        FindChild(panel, "finishTime (1)").GetComponent<TMP_Text>().text = "You did it!";

        TMP_Text starsText = FindChild(panel, "finishTime").GetComponent<TMP_Text>();
        starsText.text = "0/45";

        // The font has no star glyph, so the total sits beside a star image
        RectTransform starsRect = (RectTransform)starsText.transform;
        Image starIcon = MainMenuSetup.EnsureImage(panel, "celebrationStar", MainMenuSetup.LoadSprite(MainMenuSetup.StarPath),
            starsRect.anchorMin, starsRect.anchoredPosition + new Vector2(-170f, 0f), new Vector2(90f, 90f));
        starIcon.raycastTarget = false;

        Object.DestroyImmediate(FindChild(panel, "starContainer").gameObject);

        Transform celebrationButtons = FindChild(panel, "buttons");
        Object.DestroyImmediate(FindChild(celebrationButtons, "replayButton").gameObject);
        Object.DestroyImmediate(FindChild(celebrationButtons, "nextButton").gameObject);

        GameObject mtiny = new GameObject("mtinyCelebration", typeof(RectTransform), typeof(Image), typeof(Animator));
        mtiny.transform.SetParent(panel, false);
        RectTransform mtinyRect = (RectTransform)mtiny.transform;
        mtinyRect.anchoredPosition = new Vector2(0f, -110f);
        mtinyRect.sizeDelta = new Vector2(260f, 270f);
        mtiny.GetComponent<Image>().sprite = mtinySprite;
        mtiny.GetComponent<Image>().preserveAspect = true;
        mtiny.GetComponent<Animator>().runtimeAnimatorController = mtinyController;

        // ---------------- RUNNER ----------------
        runner.board = gameManager.GetComponent<MissionBoardBuilder>();
        runner.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        runner.finishPanel = finishPanel;
        runner.nextButton = next;
        runner.celebrationPanel = celebration;
        runner.celebrationStarsText = starsText;

        EditorUtility.SetDirty(runner);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[mTiny] Finish flow added: Next, Replay and celebration panel.");
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static void SetOnlyListener(UnityEngine.Events.UnityEvent unityEvent, UnityEngine.Events.UnityAction action)
    {
        while (unityEvent.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(unityEvent, 0);
        }

        UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent, action);
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }

        return null;
    }

    private static void CreateTilePrefabs()
    {
        if (!AssetDatabase.IsValidFolder(TileFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefab", "Tiles");
        }

        Scene template = EditorSceneManager.OpenScene(TemplateScenePath, OpenSceneMode.Single);

        SaveTile(FindInScene(template, "puzzlepart"), "PathTile");
        SaveTile(FindInScene(template, "Start"), "StartTile");
        SaveTile(FindInScene(template, "Goal"), "GoalTile");

        Scene world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Additive);
        SpecialTile[] specials = Object.FindObjectsByType<SpecialTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        SaveTile(FindSpecial(specials, TileType.Hospital, "Layer 8"), "HospitalTile");
        SaveTile(FindSpecial(specials, TileType.ChildrenCrossing, "Layer 13"), "CrossingTile");
        SaveTile(FindSpecial(specials, TileType.PoliceStation, "Layer 4"), "PoliceTile");

        EditorSceneManager.CloseScene(world, true);
    }

    private static void SaveTile(GameObject source, string prefabName)
    {
        GameObject copy = Object.Instantiate(source);
        copy.name = prefabName;
        copy.transform.position = Vector3.zero;

        // Source tiles were scaled by their 'platforms' parent; bake that in
        // so a tile is one grid cell wide under the unscaled Board.
        copy.transform.localScale = source.transform.lossyScale;

        // RobotController reads only the first SpecialTile; drop stray None ones
        SpecialTile[] specials = copy.GetComponents<SpecialTile>();

        if (specials.Any(s => s.tileType != TileType.None))
        {
            foreach (SpecialTile special in specials.Where(s => s.tileType == TileType.None))
            {
                Object.DestroyImmediate(special);
            }
        }

        PrefabUtility.SaveAsPrefabAsset(copy, $"{TileFolder}/{prefabName}.prefab");
        Object.DestroyImmediate(copy);
    }

    private static GameObject LoadTile(string prefabName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{TileFolder}/{prefabName}.prefab");
    }

    private static GameObject FindSpecial(SpecialTile[] specials, TileType type, string spriteName)
    {
        SpecialTile match = specials.FirstOrDefault(t =>
            t.tileType == type &&
            t.GetComponent<SpriteRenderer>() != null &&
            t.GetComponent<SpriteRenderer>().sprite != null &&
            t.GetComponent<SpriteRenderer>().sprite.name == spriteName);

        return (match != null ? match : specials.First(t => t.tileType == type)).gameObject;
    }

    private static void RemoveHandPlacedBoard(Scene scene)
    {
        GameObject platforms = FindInScene(scene, "platforms");

        foreach (Transform child in platforms.transform.Cast<Transform>().ToList())
        {
            if (child.name == "Start" || child.name == "Goal" || child.name.StartsWith("puzzlepart"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // GameManager spawns the robot on the start tile; hand-placed
        // robots would compete with it for button presses.
        foreach (RobotController robot in Object.FindObjectsByType<RobotController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(robot.gameObject) ?? robot.gameObject;
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName)
                {
                    return t.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject[] FindAll(Scene scene, params string[] names)
    {
        return names.Select(n => FindInScene(scene, n)).Where(go => go != null).ToArray();
    }
}
