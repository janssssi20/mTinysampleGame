using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Renders the game's screens to PNGs without entering play mode, so UI
// changes can be reviewed from batch mode. Scenes are not saved.
// Output folder: MTINY_PREVIEW_DIR environment variable, else Temp/UIPreview.
public static class UIPreview
{
    private const int Width = 1920;
    private const int Height = 1080;

    private static string OutDir =>
        Environment.GetEnvironmentVariable("MTINY_PREVIEW_DIR") ?? "Temp/UIPreview";

    [MenuItem("mTiny/Preview/Capture All Screens")]
    public static void CaptureAll()
    {
        Directory.CreateDirectory(OutDir);

        // ---------------- MAIN MENU ----------------
        Scene menu = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        Capture("1_menu");

        GameObject settingsPanel = FindPanel(menu, "settingsPanel");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Capture("1b_settings");
            settingsPanel.SetActive(false);
        }

        GameObject missionPanel = FindPanel(menu, "missionPanel");
        missionPanel.SetActive(true);

        // Uses the real saved progress (read-only) so previews never
        // change the player's stars.
        MissionMapUI ui = UnityEngine.Object.FindFirstObjectByType<MissionMapUI>(FindObjectsInactive.Include);
        ui.Refresh();
        ui.pathScroll.horizontalNormalizedPosition = 0f;
        Capture("2_map_start");

        ui.pathScroll.horizontalNormalizedPosition = 0.55f;
        Capture("3_map_middle");

        ui.OnNodePressed(0);
        Capture("3b_mission_card");
        ui.ClosePopup();

        // ---------------- MISSION ----------------
        Scene mission = EditorSceneManager.OpenScene("Assets/Scenes/Mission.unity", OpenSceneMode.Single);
        MissionBoardBuilder builder = UnityEngine.Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
        CommandManager commands = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);

        builder.Build(12);

        commands.instructionPanel.SetActive(true);
        Capture("4_instruction");
        commands.instructionPanel.SetActive(false);

        Capture("5_play");

        commands.finishPanel.SetActive(true);
        Capture("6_finish");
        commands.finishPanel.SetActive(false);

        MissionRunner runner = UnityEngine.Object.FindFirstObjectByType<MissionRunner>(FindObjectsInactive.Include);
        runner.celebrationPanel.SetActive(true);
        Capture("7_celebration");

        // ---------------- WORLD ----------------
        EditorSceneManager.OpenScene("Assets/Scenes/World.unity", OpenSceneMode.Single);
        CommandManager worldCommands = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        if (worldCommands.instructionPanel != null) worldCommands.instructionPanel.SetActive(false);
        Capture("8_world");

        Debug.Log("[mTiny] Previews written to " + Path.GetFullPath(OutDir));
    }

    [MenuItem("mTiny/Preview/Capture Play Aids")]
    public static void CapturePlayAids()
    {
        Directory.CreateDirectory(OutDir);

        // ---------------- MISSION: run highlight and Try again ----------------
        EditorSceneManager.OpenScene("Assets/Scenes/Mission.unity", OpenSceneMode.Single);
        MissionBoardBuilder builder = UnityEngine.Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
        CommandManager commands = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        SequenceUIManager bar = UnityEngine.Object.FindFirstObjectByType<SequenceUIManager>(FindObjectsInactive.Include);
        builder.Build(1);
        commands.instructionPanel.SetActive(true);
        Capture("9_instruction_speaker");
        commands.instructionPanel.SetActive(false);

        // flag, forward x3, right, forward: the robot bumped on the last block
        foreach (Sprite icon in new[] { commands.StartFlagIcon, commands.forwardIcon, commands.multiplier3xIcon, commands.rightIcon, commands.forwardIcon })
        {
            bar.AddVisualBlock(icon);
        }
        bar.Highlight(1, 2);
        Capture("10_run_highlight");

        bar.Highlight(4, 1, problem: true);
        commands.tryAgainBanner.message.text = TryAgainBanner.MessageFor(TryAgainReason.Bumped);
        commands.tryAgainBanner.gameObject.SetActive(true);
        Capture("11_try_again");

        // ---------------- EXPLORE: modes ----------------
        Scene world = EditorSceneManager.OpenScene("Assets/Scenes/World.unity", OpenSceneMode.Single);
        CommandManager worldCommands = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        if (worldCommands.instructionPanel != null) worldCommands.instructionPanel.SetActive(false);
        ExploreModes modes = UnityEngine.Object.FindFirstObjectByType<ExploreModes>(FindObjectsInactive.Include);
        MapBuilder map = UnityEngine.Object.FindFirstObjectByType<MapBuilder>(FindObjectsInactive.Include);

        modes.SetMode(ExploreMode.Code);
        Capture("12_explore_code");

        modes.SetMode(ExploreMode.Remote);
        Capture("13_explore_remote");

        // A small loop of road to show the pieces joining up (preview only:
        // the saved map in PlayerPrefs is put back afterwards)
        string saved = PlayerPrefs.GetString("mtiny.mymap", "");
        map.ClearMap();
        modes.SetMode(ExploreMode.Build);
        foreach (Vector2Int cell in new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2),
                     new Vector2Int(3, 2), new Vector2Int(4, 2), new Vector2Int(4, 3) })
        {
            map.ToggleCell(cell);
        }

        // A few special pieces: crossing and police on east-west roads,
        // hospital on a north-south stretch (turned)
        map.SelectPiece((int)MapPiece.Crossing);
        map.ToggleCell(new Vector2Int(1, 0));
        map.SelectPiece((int)MapPiece.Police);
        map.ToggleCell(new Vector2Int(3, 2));
        map.SelectPiece((int)MapPiece.Hospital);
        map.ToggleCell(new Vector2Int(2, 1));
        Capture("14_explore_build");
        map.SelectPiece((int)MapPiece.Road);

        modes.SetMode(ExploreMode.Code);
        Capture("15_explore_my_map");

        if (string.IsNullOrEmpty(saved)) PlayerPrefs.DeleteKey("mtiny.mymap");
        else PlayerPrefs.SetString("mtiny.mymap", saved);

        Debug.Log("[mTiny] Play aid previews written to " + Path.GetFullPath(OutDir));
    }

    [MenuItem("mTiny/Preview/Capture Camera Views")]
    public static void CaptureViews()
    {
        Directory.CreateDirectory(OutDir);

        foreach (string scenePath in new[] { "Assets/Scenes/World.unity", "Assets/Scenes/Mission.unity" })
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            string tag = scenePath.Contains("World") ? "explore" : "mission";

            CommandManager commands = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
            if (commands.instructionPanel != null) commands.instructionPanel.SetActive(false);

            MissionBoardBuilder builder = UnityEngine.Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
            if (builder != null) builder.Build(12);

            // The robot is spawned at runtime; stand one on the start tile
            GameManager gm = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(gm.robotPrefab);
            Vector3 start = builder != null ? builder.CellToWorld(builder.Current.start) : gm.startPoint.position;
            robot.transform.position = new Vector3(start.x, start.y, start.z - 0.02f);

            CameraViews views = Camera.main.GetComponent<CameraViews>();

            foreach (CameraView view in new[] { CameraView.Top, CameraView.Tilt, CameraView.Follow })
            {
                views.Preview(view);
                Capture(tag + "_view_" + view.ToString().ToLowerInvariant());
            }
        }

        Debug.Log("[mTiny] Camera view previews written to " + Path.GetFullPath(OutDir));
    }

    [MenuItem("mTiny/Preview/Capture Mission Boards")]
    public static void CaptureBoards()
    {
        Directory.CreateDirectory(OutDir);

        EditorSceneManager.OpenScene("Assets/Scenes/Mission.unity", OpenSceneMode.Single);
        MissionBoardBuilder builder = UnityEngine.Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
        CommandManager commands = UnityEngine.Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        commands.instructionPanel.SetActive(false);

        for (int i = 0; i < builder.catalog.Count; i++)
        {
            builder.Build(i);
            Capture("board_" + builder.Current.id);
        }

        Debug.Log("[mTiny] Board previews written to " + Path.GetFullPath(OutDir));
    }

    private static void Capture(string name)
    {
        Camera camera = Camera.main;
        RenderTexture rt = new RenderTexture(Width, Height, 24);

        // The cream mat card sizes itself while playing; do it here too
        foreach (MatCard mat in UnityEngine.Object.FindObjectsByType<MatCard>(FindObjectsSortMode.None)) mat.Fit();

        camera.targetTexture = rt;

        // Overlay canvases don't render through a camera; render them
        // in front of the board instead.
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                // Angled views keep the UI right at the lens (see CameraViews)
                canvas.planeDistance = camera.orthographic ? 5f : 0.6f;
                canvas.sortingOrder = 100;
            }
        }

        Canvas.ForceUpdateCanvases();

        foreach (RectTransform rect in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
        {
            if (rect.GetComponent<LayoutGroup>() != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }

        Canvas.ForceUpdateCanvases();
        camera.Render();

        RenderTexture.active = rt;
        Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        image.Apply();
        RenderTexture.active = null;

        File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), image.EncodeToPNG());

        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(rt);
    }

    private static GameObject FindPanel(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name && t.GetComponent<Button>() == null)
                {
                    return t.gameObject;
                }
            }
        }

        return null;
    }
}
