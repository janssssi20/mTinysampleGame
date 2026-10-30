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

        GameObject missionPanel = FindPanel(menu, "missionPanel");
        missionPanel.SetActive(true);

        StageMapUI ui = UnityEngine.Object.FindFirstObjectByType<StageMapUI>(FindObjectsInactive.Include);
        ui.BackToStages();
        Capture("2_stages");

        ui.OpenStage(0);
        Capture("3_trail");

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

        camera.targetTexture = rt;

        // Overlay canvases don't render through a camera; render them
        // in front of the board instead.
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;
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
