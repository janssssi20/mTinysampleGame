using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds note blocks (Do Re Mi Fa Sol La Si and a high Do) to the palette
// of Explore mode (World scene) and of missions (Mission scene). Each
// block adds a note to the program; when it runs, mTiny sings it.
public static class ExploreNotesSetup
{
    private const string WorldPath = "Assets/Scenes/World.unity";
    private const string MissionPath = "Assets/Scenes/Mission.unity";
    private const string NotesFolder = "Assets/Image/mtiny command blocks/Notes/";

    // NoteType order -> the owner's art. HighDo is the Do block with an
    // up arrow, drawn to match.
    private static readonly string[] NoteArt =
    {
        "Layer 24", "Layer 29", "Layer 30", "Layer 25", "Layer 28", "Layer 31", "Layer 26", "HighDo"
    };

    [MenuItem("mTiny/Setup/Add Note Blocks to Explore")]
    public static void AddNoteBlocks()
    {
        AddNoteBlocks(WorldPath);
    }

    // Stages 4 and 5 use sound blocks: the mission palette gets them too,
    // shown only when a mission's visible blocks include Sound
    [MenuItem("mTiny/Setup/Add Note Blocks to Missions")]
    public static void AddNoteBlocksToMissions()
    {
        Scene scene = AddNoteBlocks(MissionPath);

        MissionBoardBuilder builder = Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
        RectTransform palette = (RectTransform)Find(scene, "commandblock_group");
        builder.soundBlocks = palette.GetComponentsInChildren<CommandButton>(true)
            .Where(c => c.name.StartsWith("note"))
            .Select(c => c.gameObject)
            .ToArray();

        // A Do block on the instruction card for missions that need a sound
        if (builder.emotionHint != null)
        {
            Transform row = builder.emotionHint.transform.parent;
            Transform old = row.Find("soundHint");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            GameObject hint = Object.Instantiate(builder.emotionHint, row);
            hint.name = "soundHint";
            hint.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NotesFolder + "Layer 24.png");
            builder.soundHint = hint;
        }

        EditorUtility.SetDirty(builder);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[mTiny] Mission palette has " + builder.soundBlocks.Length + " note blocks.");
    }

    private static Scene AddNoteBlocks(string scenePath)
    {
        MatchImportSettings(NotesFolder + "HighDo.png", NotesFolder + "Layer 24.png");

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        CommandManager commands = Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        CommandButtonManager buttonManager = Object.FindFirstObjectByType<CommandButtonManager>(FindObjectsInactive.Include);
        RectTransform palette = (RectTransform)Find(scene, "commandblock_group");
        Transform content = Find(scene, "Content", palette);

        // An emotion block is the template: same button feel, layer and
        // lock-until-flag behaviour as the rest of the palette
        Transform template = Find(scene, "happy", palette);

        Sprite[] icons = NoteArt
            .Select(name => AssetDatabase.LoadAssetAtPath<Sprite>(NotesFolder + name + ".png"))
            .ToArray();

        // Clear earlier runs: the old separate tray and any note blocks
        buttonManager.commandButtons.RemoveAll(b => b == null || b.name.StartsWith("note"));
        Transform oldTray = Find(scene, "noteblock_group");
        if (oldTray != null) Object.DestroyImmediate(oldTray.gameObject);
        foreach (Transform old in palette.GetComponentsInChildren<Transform>(true)
                     .Where(t => t.name.StartsWith("note") && t.GetComponent<CommandButton>() != null).ToList())
        {
            Object.DestroyImmediate(old.gameObject);
        }

        // A palette already sorted into families gets its Music group from
        // the redesign setup; the blocks wait at the end of the list
        Transform musicGroup = content.Find("group_Music");
        if (musicGroup != null) Object.DestroyImmediate(musicGroup.gameObject);

        // ---------------- PALETTE ----------------
        // Three columns so all 23 blocks fit without scrolling
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            palette.sizeDelta = new Vector2(440f, palette.sizeDelta.y);
            grid.constraintCount = 3;
        }

        // ---------------- BLOCKS ----------------
        // After the feelings, in scale order
        string[] names = System.Enum.GetNames(typeof(NoteType));

        for (int i = 0; i < names.Length; i++)
        {
            GameObject block = Object.Instantiate(template.gameObject, content);
            block.name = "note" + names[i];
            block.SetActive(true);
            block.transform.SetAsLastSibling();

            Image image = block.GetComponent<Image>();
            image.sprite = icons[i];
            image.preserveAspect = true;

            CommandButton command = block.GetComponent<CommandButton>();
            command.commandType = CommandType.Note;
            command.noteType = (NoteType)i;
            command.icon = icons[i];

            Button button = block.GetComponent<Button>();
            ClearListeners(button.onClick);
            UnityEventTools.AddPersistentListener(button.onClick, command.AddNote);

            // The note itself is the sound; no extra tap "pop"
            TouchButtonFeedback squish = block.GetComponent<TouchButtonFeedback>() ??
                                         block.AddComponent<TouchButtonFeedback>();
            squish.tapSound = false;

            // Locked until the start flag is pressed, like every block
            buttonManager.commandButtons.Add(button);
        }

        commands.noteIcons = icons;

        EditorUtility.SetDirty(commands);
        EditorUtility.SetDirty(buttonManager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[mTiny] " + names.Length + " note blocks added to " + scenePath);
        return scene;
    }

    // Imports a drawn block exactly like the owner's art (sprite, filtering, size)
    private static void MatchImportSettings(string path, string referencePath)
    {
        TextureImporter target = (TextureImporter)AssetImporter.GetAtPath(path);
        TextureImporter reference = (TextureImporter)AssetImporter.GetAtPath(referencePath);

        TextureImporterSettings settings = new TextureImporterSettings();
        reference.ReadTextureSettings(settings);
        target.SetTextureSettings(settings);
        target.textureCompression = reference.textureCompression;
        target.SaveAndReimport();
    }

    private static void ClearListeners(UnityEngine.Events.UnityEventBase unityEvent)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(unityEvent, i);
        }
    }

    private static Transform Find(Scene scene, string name, Transform under = null)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name && (under == null || t.IsChildOf(under)))
                {
                    return t;
                }
            }
        }

        return null;
    }
}
