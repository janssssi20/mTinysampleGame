using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Puts the owner's 3D mTiny in the robot and adds the camera views
// (Top / Tilt / Follow) with their buttons to the Mission and Explore
// scenes. Safe to run again: it replaces what it made last time.
public static class ThreeDViewSetup
{
    private const string RobotPrefab = "Assets/Prefab/robot.prefab";
    private const string ModelPath = "Assets/Models/mTiny/mtiny.fbx";
    private const float ModelScale = 0.55f;

    private static readonly Color Ink = new Color(0.39f, 0.22f, 0.02f);
    private static readonly Color Orange = new Color(0.85f, 0.49f, 0.16f);
    private static readonly Color Tray = new Color(0.98f, 0.82f, 0.48f);

    [MenuItem("mTiny/Setup/Add 3D mTiny and Camera Views")]
    public static void AddAll()
    {
        WriteIcons();
        SetupRobot();
        SetupScene("Assets/Scenes/Mission.unity", "mtiny.view.mission", isWorld: false);
        SetupScene("Assets/Scenes/World.unity", "mtiny.view.explore", isWorld: true);
        Debug.Log("[mTiny] 3D mTiny and camera views added.");
    }


    // =========================================================
    // ROBOT
    // =========================================================

    private static void SetupRobot()
    {
        GameObject robot = PrefabUtility.LoadPrefabContents(RobotPrefab);

        foreach (string old in new[] { "mTinyModel", "facingArrow" })
        {
            Transform t = robot.transform.Find(old);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // The flat picture stays on the prefab, switched off, as a fallback
        SpriteRenderer picture = robot.GetComponent<SpriteRenderer>();
        if (picture != null) picture.enabled = false;

        // Stands on the mat: model up (+Y) points off the mat (-Z) and its
        // front (+Z) points the way mTiny drives (+Y)
        // The holder does the turning and sizing; the model keeps the
        // rotation and scale it was exported with (some exports keep them
        // on the model's own top object)
        GameObject holder = new GameObject("mTinyModel");
        holder.transform.SetParent(robot.transform, false);
        holder.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.back);
        holder.transform.localScale = Vector3.one * ModelScale;

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), holder.transform);

        // A leftover cylinder behind the model in the file
        Transform stray = model.transform.Find("Cylinder");
        if (stray != null) stray.gameObject.SetActive(false);

        // Arrow on the mat in front of mTiny, shown in the top view only
        GameObject arrow = new GameObject("facingArrow", typeof(SpriteRenderer));
        arrow.transform.SetParent(robot.transform, false);
        arrow.transform.localPosition = new Vector3(0f, 0.78f, -0.01f);
        arrow.transform.localScale = Vector3.one * 0.32f;
        SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();
        arrowSprite.sprite = UIEnhancer.LoadIcon("mat_arrow");
        arrowSprite.color = Orange;
        arrowSprite.sortingOrder = 2;

        PrefabUtility.SaveAsPrefabAsset(robot, RobotPrefab);
        PrefabUtility.UnloadPrefabContents(robot);
    }


    // =========================================================
    // SCENES
    // =========================================================

    private static void SetupScene(string path, string saveKey, bool isWorld)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        CommandManager commands = Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        TMP_FontAsset font = commands.timerText.font;
        Transform header = Find(scene, "header");
        Transform canvas = header.parent;

        foreach (string old in new[] { "mTinyLight", "viewButtons" }) Remove(scene, old);

        // ---------------- LIGHT ----------------
        // Soft sun from above-front so the 3D mTiny reads; sprites ignore it
        Light sun = new GameObject("mTinyLight").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, 0.5f, 1f), Vector3.up);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.66f);

        // ---------------- CAMERA VIEWS ----------------
        Camera cam = Camera.main;
        CameraViews views = cam.GetComponent<CameraViews>() ?? cam.gameObject.AddComponent<CameraViews>();
        views.saveKey = saveKey;

        List<Transform> boards = new List<Transform>();
        if (isWorld)
        {
            boards.Add(Find(scene, "platforms"));
            Transform myMap = Find(scene, "myMapTiles");
            if (myMap != null) boards.Add(myMap);
        }
        else
        {
            boards.Add(Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include).boardParent);
        }
        views.boardRoots = boards.ToArray();

        // Panels drawn as part of the world (not screen UI) follow the camera
        MissionRunner runner = Object.FindFirstObjectByType<MissionRunner>(FindObjectsInactive.Include);
        views.screenPanels = new[] { commands.emotionPanel, commands.finishPanel, commands.instructionPanel,
                runner != null ? runner.celebrationPanel : null }
            .Where(p => p != null && p.GetComponentInParent<Canvas>(true) == null)
            .Select(p => p.transform)
            .Distinct()
            .ToArray();

        // ---------------- VIEW BUTTONS ----------------
        // Explore: under the mode buttons. Mission: in the header.
        RectTransform tray = NewRect("viewButtons", canvas);
        if (isWorld)
        {
            tray.anchorMin = tray.anchorMax = new Vector2(0f, 1f);
            tray.pivot = new Vector2(0f, 1f);
            tray.anchoredPosition = new Vector2(16f, -786f);
            tray.sizeDelta = new Vector2(320f, 150f);
        }
        else
        {
            // Top-right of the header, where the timer used to be: the
            // board can reach any corner below it
            tray.SetParent(header, false);
            tray.anchorMin = tray.anchorMax = new Vector2(1f, 1f);
            tray.pivot = new Vector2(1f, 1f);
            tray.anchoredPosition = new Vector2(-12f, -30f);
            tray.sizeDelta = new Vector2(252f, 136f);
        }
        Image trayImage = tray.gameObject.AddComponent<Image>();
        trayImage.sprite = UIEnhancer.LoadIcon("ui_rounded");
        trayImage.type = Image.Type.Sliced;
        trayImage.color = new Color(1f, 0.93f, 0.72f);

        views.topButton = ViewButton(tray, "topView", font, "Top", "view_top", 0, views.ShowTop);
        views.tiltButton = ViewButton(tray, "tiltView", font, "Tilt", "view_tilt", 1, views.ShowTilt);
        views.followButton = ViewButton(tray, "followView", font, "Follow", "view_follow", 2, views.ShowFollow);

        UIEnhancer.AddTouchFeedback(scene);
        EditorUtility.SetDirty(views);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Image ViewButton(RectTransform tray, string name, TMP_FontAsset font, string text, string icon, int slot,
        UnityAction action)
    {
        float width = (tray.sizeDelta.x - 16f) / 3f;
        RectTransform rect = NewRect(name, tray);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(8f + slot * width, 0f);
        rect.sizeDelta = new Vector2(width - 4f, tray.sizeDelta.y - 16f);

        Image face = rect.gameObject.AddComponent<Image>();
        face.sprite = UIEnhancer.LoadIcon("ui_rounded");
        face.type = Image.Type.Sliced;
        face.color = Tray;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        UnityEventTools.AddPersistentListener(button.onClick, action);

        Image glyph = MainMenuSetup.EnsureImage(rect, "icon", UIEnhancer.LoadIcon(icon), new Vector2(0.5f, 1f),
            new Vector2(0f, -42f), new Vector2(54f, 54f));
        glyph.color = Ink;
        glyph.raycastTarget = false;

        RectTransform labelRect = NewRect("label", rect);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 26f);
        labelRect.sizeDelta = new Vector2(width - 8f, 40f);
        TextMeshProUGUI label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = 30;
        label.enableWordWrapping = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18;
        label.fontSizeMax = 30;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return face;
    }


    // =========================================================
    // ICONS
    // =========================================================

    private static void WriteIcons()
    {
        // Top: a 3 x 3 map grid
        UIEnhancer.WriteIcon("view_top", (x, y) =>
        {
            float cx = (x - 10f) % 38f, cy = (y - 10f) % 38f;
            return x >= 10f && x <= 118f && y >= 10f && y <= 118f && cx <= 32f && cy <= 32f;
        });

        // Tilt: the mat seen at an angle
        UIEnhancer.WriteIcon("view_tilt", (x, y) =>
        {
            bool shape = y >= 30f && y <= 98f && Mathf.Abs(x - 64f) <= 34f + (98f - y) * 0.42f;
            bool line = Mathf.Abs(y - 64f) <= 4f;
            return shape && !line;
        });

        // Follow: mTiny's head from behind
        UIEnhancer.WriteIcon("view_follow", (x, y) =>
        {
            bool head = UIEnhancer.InsideRoundedRect(x, y, 22f, 106f, 26f) && y <= 96f;
            bool earL = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 98f)) <= 16f;
            bool earR = Vector2.Distance(new Vector2(x, y), new Vector2(96f, 98f)) <= 16f;
            return head || earL || earR;
        });

        // Arrow on the mat showing which way mTiny faces
        UIEnhancer.WriteIcon("mat_arrow", (x, y) =>
        {
            bool head = y >= 56f && y <= 118f && Mathf.Abs(x - 64f) <= (118f - y) * 0.9f;
            bool tail = y >= 10f && y <= 60f && Mathf.Abs(x - 64f) <= 16f;
            return head || tail;
        });
    }


    // =========================================================
    // HELPERS
    // =========================================================

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

    private static Transform Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root.transform;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
        }

        return null;
    }
}
