using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Builds the emotion card (EmotionStage) into the Mission and World
// scenes. The card lives on its own overlay canvas above everything, so
// the 3D mTiny and the board can't cover it. Safe to run again.
public static class EmotionStageSetup
{
    private const string ArtFolder = "Assets/Image/emotions/stage";
    private static readonly string[] Scenes = { "Assets/Scenes/Mission.unity", "Assets/Scenes/World.unity" };

    private static readonly (string key, string path)[] Sheets =
    {
        ("happy", "Assets/Image/emotions/M-Tiny Happy.png"),
        ("sad", "Assets/Image/emotions/M-Tiny Sad.png"),
        ("angry", "Assets/Image/emotions/M- Tiny Angry.png"),
        ("surprise", "Assets/Image/emotions/M-Tiny Excited.png"),
        ("dizzy", "Assets/Image/emotions/M-Tiny Dizzy.png"),
        ("sleepy", "Assets/Image/emotions/M-Tiny Snooze.png"),
        ("playful", "Assets/Image/emotions/M-Tiny Playful.png"),
    };

    [MenuItem("mTiny/Setup/Emotion Stage")]
    public static void Run()
    {
        ImportArt();
        foreach (string path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (Build()) EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("[mTiny] Emotion stage built.");
    }

    private static void ImportArt()
    {
        foreach (string file in Directory.GetFiles(ArtFolder, "*.png"))
        {
            string path = file.Replace('\\', '/');
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 300f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            Vector4 border = Vector4.zero;
            if (name == "fx_bubble") border = new Vector4(54, 54, 54, 54);
            if (name == "fx_rrect") border = new Vector4(40, 40, 40, 40);
            imp.spriteBorder = border;
            imp.SaveAndReimport();
        }
    }

    private static bool Build()
    {
        var manager = Object.FindObjectsOfType<CommandManager>(true).FirstOrDefault();
        if (manager == null)
        {
            Debug.LogWarning("[mTiny] No CommandManager in " + EditorSceneManager.GetActiveScene().path);
            return false;
        }

        var old = GameObject.Find("EmotionStage");
        if (old == null)
        {
            var stageOld = Object.FindObjectsOfType<EmotionStage>(true).FirstOrDefault();
            if (stageOld != null) old = stageOld.gameObject;
        }
        if (old != null) Object.DestroyImmediate(old);

        // The old board picture stays in the scene but never shows
        if (manager.emotionPanel != null && manager.emotionPanel.GetComponent<EmotionStage>() == null)
        {
            manager.emotionPanel.SetActive(false);
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka SDF.asset");
        Sprite rrect = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/fx_rrect.png");
        Color ink = Hex("2E2A4F");

        var root = new GameObject("EmotionStage", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // A soft wash over the game so the card stands out (taps still pass)
        var dim = NewRect("dim", root.transform);
        Stretch(dim);
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(ink.r, ink.g, ink.b, 0.3f);
        dimImg.raycastTarget = false;

        var holder = NewRect("cardHolder", root.transform);
        holder.sizeDelta = new Vector2(600, 400);
        holder.localScale = Vector3.one * 1.55f;

        var shadow = NewRect("cardShadow", holder);
        Stretch(shadow);
        shadow.offsetMin = new Vector2(0, -8);
        shadow.offsetMax = new Vector2(0, -8);
        var shadowImg = shadow.gameObject.AddComponent<Image>();
        shadowImg.sprite = rrect;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.pixelsPerUnitMultiplier = 0.55f;
        shadowImg.color = new Color(ink.r, ink.g, ink.b, 0.28f);
        shadowImg.raycastTarget = false;

        var card = NewRect("card", holder);
        Stretch(card);
        var cardImg = card.gameObject.AddComponent<Image>();
        cardImg.sprite = rrect;
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 0.55f;
        cardImg.color = Hex("EAF6FF");
        cardImg.raycastTarget = false;
        card.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        var content = NewRect("content", card);
        Stretch(content);

        var chip = NewRect("titleChip", card);
        chip.anchorMin = chip.anchorMax = new Vector2(0, 1);
        chip.pivot = new Vector2(0, 1);
        chip.anchoredPosition = new Vector2(14, -12);
        var chipImg = chip.gameObject.AddComponent<Image>();
        chipImg.sprite = rrect;
        chipImg.type = Image.Type.Sliced;
        chipImg.pixelsPerUnitMultiplier = 1.4f;
        chipImg.color = new Color(1, 1, 1, 0.88f);
        chipImg.raycastTarget = false;
        var layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 3, 3);
        layout.childControlWidth = layout.childControlHeight = true;
        var fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var titleRt = NewRect("title", chip);
        var title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
        title.font = font;
        title.fontSize = 22;
        title.fontStyle = FontStyles.Bold;
        title.color = ink.linear;
        title.text = "Happy";
        title.enableWordWrapping = false;
        title.raycastTarget = false;

        var stage = root.AddComponent<EmotionStage>();
        stage.card = card;
        stage.cardBack = cardImg;
        stage.content = content;
        stage.titleChip = chipImg;
        stage.title = title;
        stage.font = font;

        var sets = new List<EmotionStage.FaceSet>();
        foreach (var (key, path) in Sheets)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => FrameIndex(s.name)).ToArray();
            if (frames.Length == 0) Debug.LogWarning("[mTiny] No frames in " + path);
            sets.Add(new EmotionStage.FaceSet { key = key, frames = frames });
        }
        stage.faces = sets.ToArray();
        stage.art = Directory.GetFiles(ArtFolder, "*.png")
            .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/')))
            .Where(s => s != null).ToArray();

        manager.emotionStage = stage;
        // Resets and finishes already hide "the emotion panel": point it here
        manager.emotionPanel = root;
        EditorUtility.SetDirty(manager);

        root.SetActive(false);
        Debug.Log("[mTiny] Emotion stage added to " + EditorSceneManager.GetActiveScene().path +
                  " with " + stage.art.Length + " art pieces and " + sets.Sum(s => s.frames.Length) + " face frames.");
        return true;
    }

    private static int FrameIndex(string name)
    {
        int i = name.LastIndexOf('_');
        int n;
        return i >= 0 && int.TryParse(name.Substring(i + 1), out n) ? n : 0;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }
}
