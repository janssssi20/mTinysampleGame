using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds the Notes tray to Explore mode (World scene): seven solfège
// blocks, Do to Si, on the left of the board. Each block adds a note to
// the program; when it runs, mTiny sings the note and bounces.
public static class ExploreNotesSetup
{
    private const string WorldPath = "Assets/Scenes/World.unity";
    private const string NotesFolder = "Assets/Image/mtiny command blocks/Notes/";

    // NoteType order (Do Re Mi Fa Sol La Si) -> the owner's art
    private static readonly string[] NoteArt =
    {
        "Layer 24", "Layer 29", "Layer 30", "Layer 25", "Layer 28", "Layer 31", "Layer 26"
    };

    private static readonly Color Tray = new Color(0.98f, 0.82f, 0.48f);
    private static readonly Color Ink = new Color(0.39f, 0.22f, 0.02f);

    [MenuItem("mTiny/Setup/Add Note Blocks to Explore")]
    public static void AddNoteBlocks()
    {
        Scene scene = EditorSceneManager.OpenScene(WorldPath, OpenSceneMode.Single);

        CommandManager commands = Object.FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);
        CommandButtonManager buttonManager = Object.FindFirstObjectByType<CommandButtonManager>(FindObjectsInactive.Include);
        Transform palette = Find(scene, "commandblock_group");
        Transform canvas = palette.parent;

        // An emotion block is the template: same button feel, layer and
        // lock-until-flag behaviour as the rest of the palette
        Transform template = Find(scene, "happy", palette);
        // The game's rounded font (the timer uses it), not TMP's default
        TMP_FontAsset font = commands.timerText != null ? commands.timerText.font : TMP_Settings.defaultFontAsset;

        Sprite[] icons = NoteArt
            .Select(name => AssetDatabase.LoadAssetAtPath<Sprite>(NotesFolder + name + ".png"))
            .ToArray();

        Transform old = Find(scene, "noteblock_group", canvas);
        if (old != null)
        {
            buttonManager.commandButtons.RemoveAll(b => b == null || b.transform.IsChildOf(old));
            Object.DestroyImmediate(old.gameObject);
        }

        // ---------------- TRAY ----------------
        // Mirrors the block palette: same top edge and width, on the left
        RectTransform group = NewRect("noteblock_group", canvas);
        group.gameObject.layer = palette.gameObject.layer;
        group.anchorMin = new Vector2(0f, 1f);
        group.anchorMax = new Vector2(0f, 1f);
        group.pivot = new Vector2(0f, 1f);
        group.anchoredPosition = new Vector2(16f, -196f);
        group.sizeDelta = new Vector2(310f, 520f);
        group.SetSiblingIndex(palette.GetSiblingIndex());

        Image tray = group.gameObject.AddComponent<Image>();
        tray.sprite = UIEnhancer.LoadIcon("ui_rounded");
        tray.type = Image.Type.Sliced;
        tray.color = Tray;

        RectTransform title = NewRect("title", group);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.anchoredPosition = new Vector2(0f, -14f);
        title.sizeDelta = new Vector2(0f, 76f);
        TextMeshProUGUI label = title.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = "Notes";
        label.fontSize = 56;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        RectTransform content = NewRect("Content", group);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(12f, 12f);
        content.offsetMax = new Vector2(-12f, -96f);
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(122f, 94f);
        grid.spacing = new Vector2(14f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;

        // ---------------- BLOCKS ----------------
        string[] names = System.Enum.GetNames(typeof(NoteType));

        for (int i = 0; i < names.Length; i++)
        {
            GameObject block = Object.Instantiate(template.gameObject, content);
            block.name = "note" + names[i];
            block.SetActive(true);

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

        Debug.Log("[mTiny] Note blocks added to Explore (template children: " + template.childCount + ").");
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
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
