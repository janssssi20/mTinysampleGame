using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Five stages of ten missions: seeds the catalog, adds music blocks to
// the mission palette, and rebuilds the mission map with five stage
// cards. Run after the other mTiny setups; safe to run again.
// Hand-made tweaks to the map's header and mission card are kept.
public static class FiveStagesSetup
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private static readonly string[] KeptParts = { "header", "missionCard" };

    private struct Look
    {
        public Vector2 position, size;
        public float fontSize;
        public bool hasText;
    }

    [MenuItem("mTiny/Setup/Five Stages")]
    public static void Run()
    {
        MissionCatalogSeeder.Seed();
        TileMarkersSetup.AssignArt();
        ExploreNotesSetup.AddNoteBlocksToMissions();
        RedesignSetup.ApplyMissionMusic();

        Dictionary<string, Look> kept = Snapshot();
        MainMenuSetup.BuildMissionMap();
        RedesignSetup.ApplyMainMenu();
        Restore(kept);

        Debug.Log("[mTiny] Five stages ready.");
    }

    private static Dictionary<string, Look> Snapshot()
    {
        var looks = new Dictionary<string, Look>();
        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        Transform map = FindMap(scene);
        if (map == null) return looks;

        foreach (string part in KeptParts)
        {
            Transform root = map.Find(part);
            if (root == null) continue;
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                TMP_Text text = rt.GetComponent<TMP_Text>();
                looks[PathIn(map, rt)] = new Look
                {
                    position = rt.anchoredPosition,
                    size = rt.sizeDelta,
                    hasText = text != null,
                    fontSize = text != null ? text.fontSize : 0f
                };
            }
        }
        return looks;
    }

    private static void Restore(Dictionary<string, Look> looks)
    {
        if (looks.Count == 0) return;
        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        Transform map = FindMap(scene);
        int restored = 0;

        foreach (RectTransform rt in map.GetComponentsInChildren<RectTransform>(true))
        {
            Look look;
            if (!looks.TryGetValue(PathIn(map, rt), out look)) continue;
            rt.anchoredPosition = look.position;
            rt.sizeDelta = look.size;
            TMP_Text text = rt.GetComponent<TMP_Text>();
            if (look.hasText && text != null && !text.enableAutoSizing) text.fontSize = look.fontSize;
            EditorUtility.SetDirty(rt);
            restored++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[mTiny] Kept " + restored + " hand-made map tweaks.");
    }

    private static Transform FindMap(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "missionMap");
    }

    private static string PathIn(Transform root, Transform t)
    {
        var names = new List<string>();
        for (Transform c = t; c != null && c != root; c = c.parent) names.Add(c.name);
        names.Reverse();
        return string.Join("/", names);
    }
}
