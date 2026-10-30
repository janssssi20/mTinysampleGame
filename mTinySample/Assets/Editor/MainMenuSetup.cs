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
// a five-node mission trail (StageMapUI). Safe to re-run.
public static class MainMenuSetup
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";
    public const string PadlockPath = "Assets/Image/ui/padlock.png";
    public const string StarPath = "Assets/Image/star (1).png";

    private static readonly string[] StageNames = { "Easy", "Medium", "Hard" };

    private static readonly Vector2[] NodePositions =
    {
        new Vector2(-420f, -170f),
        new Vector2(-210f, 40f),
        new Vector2(0f, -170f),
        new Vector2(210f, 40f),
        new Vector2(420f, -170f)
    };

    [MenuItem("mTiny/Setup/Build Stage Map")]
    public static void BuildStageMap()
    {
        Sprite padlock = EnsurePadlockSprite();
        Sprite star = LoadSprite(StarPath);
        Sprite dot = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        Transform root = Find(scene, "mainMenuMissionPanel").transform;
        Transform cardsRow = Find(scene, "buttons", root) ?? Find(scene, "stageCards", root);
        cardsRow.name = "stageCards";

        TMP_Text banner = Find(scene, "Panel", root).GetComponentInChildren<TMP_Text>(true);
        Button close = Find(scene, "close", root).GetComponent<Button>();

        // mTiny art from the title screen
        Animator titleMtiny = Find(scene, "Image", Find(scene, "bg").transform).GetComponent<Animator>();

        StageMapUI ui = root.GetComponent<StageMapUI>() ?? root.gameObject.AddComponent<StageMapUI>();
        ui.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        ui.stagePanel = cardsRow.gameObject;
        ui.bannerText = banner;

        // ---------------- STAGE CARDS ----------------
        Button[] cards = cardsRow.GetComponentsInChildren<Button>(true)
            .Where(b => b.transform.parent == cardsRow).ToArray();

        ui.stageCards = new StageMapUI.StageCard[3];

        for (int i = 0; i < 3; i++)
        {
            Button card = cards[i];
            card.name = StageNames[i].ToLower() + "Card";

            // Labels sit at different heights: top one is the title
            TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true)
                .OrderByDescending(t => ((RectTransform)t.transform).anchoredPosition.y).ToArray();

            labels[0].text = StageNames[i];

            TMP_Text starsText = labels[1];
            starsText.text = "0/15";

            RectTransform starsRect = (RectTransform)starsText.transform;
            Image starIcon = EnsureImage(card.transform, "starIcon", star,
                starsRect.anchorMin, starsRect.anchoredPosition + new Vector2(-75f, 0f), new Vector2(40f, 40f));
            starIcon.raycastTarget = false;

            Image lockIcon = EnsureImage(card.transform, "lockIcon", padlock,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(150f, 150f));
            lockIcon.raycastTarget = false;

            ClearListeners(card.onClick);
            UnityEventTools.AddIntPersistentListener(card.onClick, ui.OnStagePressed, i);

            ui.stageCards[i] = new StageMapUI.StageCard
            {
                button = card,
                starsText = starsText,
                lockIcon = lockIcon.gameObject
            };
        }

        // ---------------- TRAIL PANEL ----------------
        GameObject oldTrail = Find(scene, "trailPanel", root)?.gameObject;
        if (oldTrail != null) Object.DestroyImmediate(oldTrail);

        RectTransform trail = NewRect("trailPanel", root);
        trail.anchorMin = Vector2.zero;
        trail.anchorMax = Vector2.one;
        trail.offsetMin = Vector2.zero;
        trail.offsetMax = Vector2.zero;
        trail.SetSiblingIndex(close.transform.GetSiblingIndex());

        // Dotted path first so nodes draw on top of it
        for (int n = 0; n + 1 < NodePositions.Length; n++)
        {
            Vector2 a = NodePositions[n];
            Vector2 b = NodePositions[n + 1];
            int dots = Mathf.FloorToInt(Vector2.Distance(a, b) / 40f);

            for (int d = 1; d < dots; d++)
            {
                Image dotImage = EnsureImage(trail, $"dot {n}-{d}", dot, new Vector2(0.5f, 0.5f),
                    Vector2.Lerp(a, b, d / (float)dots), new Vector2(18f, 18f));
                dotImage.color = new Color(1f, 1f, 1f, 0.7f);
                dotImage.raycastTarget = false;
            }
        }

        TMP_FontAsset font = banner.font;
        Sprite nodeSprite = close.GetComponent<Image>().sprite;

        ui.nodes = new StageMapUI.TrailNode[5];

        for (int n = 0; n < 5; n++)
        {
            Image background = EnsureImage(trail, $"node{n + 1}", nodeSprite, new Vector2(0.5f, 0.5f),
                NodePositions[n], new Vector2(150f, 150f));

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            UnityEventTools.AddIntPersistentListener(button.onClick, ui.OnNodePressed, n);

            TextMeshProUGUI label = NewRect("label", background.transform).gameObject.AddComponent<TextMeshProUGUI>();
            Stretch((RectTransform)label.transform);
            label.font = font;
            label.text = (n + 1).ToString();
            label.fontSize = 70;
            label.color = new Color(0.25f, 0.2f, 0.15f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            Image[] stars = new Image[3];

            for (int s = 0; s < 3; s++)
            {
                stars[s] = EnsureImage(background.transform, $"star{s + 1}", star, new Vector2(0.5f, 0.5f),
                    new Vector2((s - 1) * 45f, 100f), new Vector2(42f, 42f));
                stars[s].raycastTarget = false;
            }

            Image nodeLock = EnsureImage(background.transform, "lockIcon", padlock, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(90f, 90f));
            nodeLock.raycastTarget = false;

            ui.nodes[n] = new StageMapUI.TrailNode
            {
                button = button,
                background = background,
                stars = stars,
                lockIcon = nodeLock.gameObject
            };
        }

        // mTiny stands beside the next mission
        Image marker = EnsureImage(trail, "mtinyMarker", titleMtiny.GetComponent<Image>().sprite,
            new Vector2(0.5f, 0.5f), NodePositions[0] + ui.markerOffset, new Vector2(140f, 145f));
        marker.preserveAspect = true;
        marker.raycastTarget = false;
        marker.gameObject.AddComponent<Animator>().runtimeAnimatorController = titleMtiny.runtimeAnimatorController;
        ui.mtinyMarker = (RectTransform)marker.transform;

        // Back arrow mirrors the close button on the other corner
        GameObject back = Object.Instantiate(close.gameObject, trail);
        back.name = "back";
        RectTransform backRect = (RectTransform)back.transform;
        RectTransform closeRect = (RectTransform)close.transform;
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(-closeRect.anchoredPosition.x, closeRect.anchoredPosition.y);
        back.GetComponentInChildren<TMP_Text>(true).text = "<";
        Button backButton = back.GetComponent<Button>();
        ClearListeners(backButton.onClick);
        UnityEventTools.AddPersistentListener(backButton.onClick, ui.BackToStages);

        ui.trailPanel = trail.gameObject;
        trail.gameObject.SetActive(false);

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[mTiny] Stage map built in MainMenu.");
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
        hold.stageMap = Object.FindFirstObjectByType<StageMapUI>(FindObjectsInactive.Include);

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
