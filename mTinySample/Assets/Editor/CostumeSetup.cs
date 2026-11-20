using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Costumes: mask art and materials, pictures of mTiny in each costume,
// the map's costume progress bars, the finish card's mask row, the
// "New mask!" card, the Costume room and the backgrounds (one world per
// costume, opened with it). The title's Customize button opens both.
// Only adds things; hand-made layout tweaks elsewhere are kept. Safe to
// run again.
public static class CostumeSetup
{
    private const string CostumeFolder = "Assets/Resources/Costumes";
    private const string PictureFolder = "Assets/Image/costumes";
    private const string WorldFolder = "Assets/Image/worlds";
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";
    private static readonly string[] MaskNames = { "panda", "chicken", "dog", "cat" };

    private static readonly Color Sky = Hex("EAF6FF"), Cream = Hex("FFF8E7"), Ink = Hex("2E2A4F");
    private static readonly Color Go = Hex("3DBE6B"), Move = Hex("4A9DFF"), Repeat = Hex("FFC93C");
    private static readonly Color Music = Hex("9B6BFF"), Orange = Hex("FF8A5B"), Locked = Hex("E4E0EE");

    private static TMP_FontAsset title, body;
    private static Sprite chunky, chunkyCircle, padlock, knob, barFill;

    [MenuItem("mTiny/Setup/Costumes and Backgrounds")]
    public static void Run()
    {
        title = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka SDF.asset");
        body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Andika SDF.asset");
        chunky = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/ui/ui_chunky.png");
        chunkyCircle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/ui/ui_chunky_circle.png");
        padlock = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuSetup.PadlockPath);
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        MaskAssets();
        WorldAssets();
        RenderPictures();
        barFill = AssetDatabase.LoadAssetAtPath<Sprite>(PictureFolder + "/bar_fill.png");
        BuildMainMenu();
        BuildMission();
        Debug.Log("[mTiny] Costumes ready.");
    }

    // =========================================================
    // ART
    // =========================================================

    private static void MaskAssets()
    {
        foreach (string n in MaskNames)
        {
            string path = CostumeFolder + "/mask_" + n + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();

            string matPath = CostumeFolder + "/mask_" + n + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Unlit/Transparent"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
    }

    // The six world pictures (finished in PhotoCraft), 1920 x 1080
    private static void WorldAssets()
    {
        for (int i = 0; i < CostumeRules.Count; i++)
        {
            string path = WorldPath((Costume)i);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { Debug.LogWarning("[mTiny] Missing world picture " + path); continue; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = false;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
    }

    private static string WorldPath(Costume c) => WorldFolder + "/world_" + BackgroundRules.File(c) + ".png";

    private static Sprite World(Costume c) => AssetDatabase.LoadAssetAtPath<Sprite>(WorldPath(c));

    // Pictures of the real 3D mTiny in each costume, for the cards
    private static void RenderPictures()
    {
        Directory.CreateDirectory(PictureFolder);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject modelAsset = RobotModelAsset();
        var holder = new GameObject("holder");
        Object.Instantiate(modelAsset, holder.transform).transform.localPosition = Vector3.zero;
        var wearer = holder.AddComponent<CostumeWearer>();
        wearer.wearSavedCostume = false;

        Bounds b = new Bounds(holder.transform.position, Vector3.zero);
        foreach (Renderer r in holder.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);

        var light = new GameObject("light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, -0.6f, 0.6f));
        RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.75f);

        var cam = new GameObject("cam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        Color key = new Color(1f, 0f, 1f);   // magenta: no costume uses it
        cam.backgroundColor = key;
        cam.fieldOfView = 30f;
        Vector3 dir = new Vector3(0f, 1f, -0.35f).normalized;
        cam.transform.position = b.center + dir * b.size.magnitude * 2.2f;
        cam.transform.LookAt(b.center, Vector3.back);

        for (int i = 0; i < CostumeRules.Count; i++)
        {
            wearer.Wear((Costume)i);
            const int size = 512;
            var rt = new RenderTexture(size, size, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            RenderTexture.active = null;
            cam.targetTexture = null;

            // Key out the magenta background; soft edges get their own
            // colour back (no pink fringe)
            Color[] px = tex.GetPixels();
            for (int p = 0; p < px.Length; p++)
            {
                Color c = px[p];
                float d = Mathf.Abs(c.r - key.r) + Mathf.Abs(c.g - key.g) + Mathf.Abs(c.b - key.b);
                float a = Mathf.Clamp01((d - 0.12f) / 0.6f);
                if (a > 0f && a < 1f)
                {
                    c.r = Mathf.Clamp01((c.r - (1f - a) * key.r) / a);
                    c.g = Mathf.Clamp01((c.g - (1f - a) * key.g) / a);
                    c.b = Mathf.Clamp01((c.b - (1f - a) * key.b) / a);
                }
                c.a = a;
                px[p] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            string file = PictureFolder + "/costume_" + ((Costume)i).ToString().ToLower() + ".png";
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }

        // Sun rays behind the "New mask!" picture
        const int rs = 512;
        var rays = new Texture2D(rs, rs, TextureFormat.RGBA32, false);
        for (int y = 0; y < rs; y++)
            for (int x = 0; x < rs; x++)
            {
                float dx = x - rs / 2f, dy = y - rs / 2f, r = Mathf.Sqrt(dx * dx + dy * dy) / (rs / 2f);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f;
                bool ray = Mathf.FloorToInt(ang / 12f) % 2 == 0;
                float a = ray ? Mathf.Clamp01(1f - r) * 0.9f : 0f;
                rays.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        rays.Apply();
        File.WriteAllBytes(PictureFolder + "/rays.png", rays.EncodeToPNG());
        Object.DestroyImmediate(rays);

        // Plain white piece for the progress bars to fill
        var white = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        white.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 255), 64).ToArray());
        white.Apply();
        File.WriteAllBytes(PictureFolder + "/bar_fill.png", white.EncodeToPNG());
        Object.DestroyImmediate(white);

        AssetDatabase.Refresh();
        foreach (string f in Directory.GetFiles(PictureFolder, "*.png"))
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }

    private static GameObject RobotModelAsset()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/robot.prefab");
        return prefab.transform.Find("mTinyModel").gameObject;
    }

    private static Sprite Picture(Costume c)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PictureFolder + "/costume_" + c.ToString().ToLower() + ".png");
    }

    // Small icon for a costume: the mask itself, or mTiny for plain/golden
    private static Sprite Icon(Costume c)
    {
        string mask = CostumeWearer.MaskName(c);
        return mask != null ? AssetDatabase.LoadAssetAtPath<Sprite>(CostumeFolder + "/mask_" + mask + ".png") : Picture(c);
    }

    // =========================================================
    // MAIN MENU: map bars and the Costume room
    // =========================================================

    private static void BuildMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        MissionMapUI map = Object.FindFirstObjectByType<MissionMapUI>(FindObjectsInactive.Include);

        // ---- costume bar on each stage card ----
        for (int s = 0; s < map.stageCards.Length; s++)
        {
            MissionMapUI.StageCard card = map.stageCards[s];
            Transform c = card.button.transform;
            Remove(c, "costume");

            // Stage number on the left of the top line, stars on the right;
            // the name on one line (it shrinks to fit); the trophy onto the tile
            Transform stageNo = c.Find("stageNo"), nameT = c.Find("name");
            Transform star = c.Find("starIcon"), stars = c.Find("stars");
            if (stageNo != null)
            {
                RectTransform r = (RectTransform)stageNo;
                r.pivot = new Vector2(0f, 0.5f);
                Place(r, new Vector2(0f, 1f), new Vector2(134f, -30f), new Vector2(96f, 32f));
                TMP_Text t = stageNo.GetComponent<TMP_Text>();
                t.enableAutoSizing = true; t.fontSizeMin = 16; t.fontSizeMax = 24; t.alignment = TextAlignmentOptions.MidlineLeft;
            }
            if (nameT != null)
            {
                RectTransform r = (RectTransform)nameT;
                r.pivot = new Vector2(0f, 0.5f);
                Place(r, new Vector2(0f, 0.5f), new Vector2(134f, 6f), new Vector2(158f, 42f));
                TMP_Text t = nameT.GetComponent<TMP_Text>();
                t.enableWordWrapping = false; t.enableAutoSizing = true; t.fontSizeMin = 16; t.fontSizeMax = 30;
                t.alignment = TextAlignmentOptions.MidlineLeft;
            }
            if (star != null) Place((RectTransform)star, new Vector2(0f, 1f), new Vector2(242f, -30f), new Vector2(22f, 22f));
            if (stars != null)
            {
                RectTransform r = (RectTransform)stars;
                r.pivot = new Vector2(0f, 0.5f);
                Place(r, new Vector2(0f, 1f), new Vector2(254f, -30f), new Vector2(42f, 28f));
                TMP_Text t = stars.GetComponent<TMP_Text>();
                t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = 18; t.alignment = TextAlignmentOptions.MidlineLeft;
            }
            if (card.trophyIcon != null)
            {
                Place(card.trophyIcon.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(36f, 36f));
            }

            RectTransform row = Rect("costume", c);
            Place(row, new Vector2(0f, 0f), new Vector2(213f, 30f), new Vector2(162f, 38f));
            Costume costume = CostumeRules.ForStage((MissionStage)s);
            card.maskIcon = Img(row, "mask", Icon(costume), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(46f, 36f));
            card.maskIcon.preserveAspect = true;
            Image bg = Img(row, "barBg", chunky, new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(64f, 16f));
            bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 6f; bg.color = Locked;
            card.progressFill = Img(bg.transform, "fill", null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 9f));
            card.progressFill.type = Image.Type.Filled;
            card.progressFill.fillMethod = Image.FillMethod.Horizontal;
            card.progressFill.color = Repeat;
            card.progressFill.sprite = barFill;
            card.progressFill.type = Image.Type.Filled;
            card.progressText = Txt(row, "count", "0/10", 19, Ink, title, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(40f, 30f));
            Image got = Img(row, "got", chunky, new Vector2(1f, 0.5f), new Vector2(-21f, 0f), new Vector2(46f, 24f));
            got.type = Image.Type.Sliced; got.pixelsPerUnitMultiplier = 4f; got.color = Go;
            Txt(got.transform, "text", "Got!", 15, Color.white, title, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 24f));
            card.gotBadge = got.gameObject;
            card.gotBadge.SetActive(false);
        }
        // ---- mTiny on the map path, in the chosen costume ----
        // The picture sits on a child that bobs (the marker itself is moved
        // to the next mission by the map); the old 2D blink animation is
        // switched off because it would swap the picture back.
        if (map.mtinyMarker != null)
        {
            Transform mk = map.mtinyMarker;
            Image flat = mk.GetComponent<Image>();
            if (flat != null) flat.enabled = false;
            Animator blink = mk.GetComponent<Animator>();
            if (blink != null) blink.enabled = false;
            Remove(mk, "costume");
            Image wear = Img(mk, "costume", Picture(Costume.None), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 256f));
            wear.preserveAspect = true;
            wear.gameObject.AddComponent<Bob>().height = 8f;
            map.mtinyMarkerImage = wear;
            map.costumeMarkers = new Sprite[CostumeRules.Count];
            for (int i = 0; i < CostumeRules.Count; i++) map.costumeMarkers[i] = Picture((Costume)i);
        }
        EditorUtility.SetDirty(map);

        // ---- title mascot: the live 3D mTiny in its costume ----
        // It sits inside the 2D mascot's spot (so it keeps that spot's
        // position and bob); the 2D picture is only hidden.
        Transform titlePanel = FindIn(scene, "Panel", FindIn(scene, "bg"));
        Transform mascot = FindIn(scene, "Image", titlePanel);
        if (mascot != null)
        {
            Remove(mascot, "mascot3D");
            RectTransform spot = (RectTransform)mascot;
            float side = Mathf.Max(spot.rect.width, spot.rect.height) * 1.45f;
            RectTransform live = Rect("mascot3D", mascot);
            Place(live, new Vector2(0.5f, 0.5f), new Vector2(0f, -side * 0.04f), new Vector2(side, side));
            RawImage raw = live.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            TitleMascot3D title3D = live.gameObject.AddComponent<TitleMascot3D>();
            title3D.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
            title3D.picture = raw;
            title3D.robotModel = RobotModelAsset();
            Image flat = mascot.GetComponent<Image>();
            if (flat != null) flat.enabled = false;
            EditorUtility.SetDirty(title3D);
        }

        // ---- Customize: one title-screen button opens one panel, split in
        // two (Costume | Backgrounds); each half scrolls ----
        Transform gear = FindIn(scene, "settingsButton");
        Transform bgRoot = gear.parent;
        Transform canvas = gear.GetComponentInParent<Canvas>().rootCanvas.transform;
        foreach (string old in new[] { "dressUpPanel", "backgroundsPanel", "customizePanel" }) Remove(canvas, old);

        TitleWorld(scene, bgRoot);
        DressUpUI ui;
        BackgroundsUI worlds;
        CustomizeRoom(canvas, out ui, out worlds);
        CustomizeButton(bgRoot, ui, worlds);
        TidySettings(scene);

        // The teacher's Yes/No question stays on top of everything
        TeacherResetHold hold = Object.FindFirstObjectByType<TeacherResetHold>(FindObjectsInactive.Include);
        if (hold != null && hold.confirmPanel != null) hold.confirmPanel.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // =========================================================
    // BACKGROUNDS: the title's world picture and the Backgrounds room
    // =========================================================

    // The chosen world fills the title screen; the old drawn hills and
    // clouds are only switched off (kept in the scene)
    private static void TitleWorld(Scene scene, Transform bgRoot)
    {
        Remove(bgRoot, "worldPicture");
        RectTransform pr = Rect("worldPicture", bgRoot);
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = pr.offsetMax = Vector2.zero;
        pr.SetSiblingIndex(0);
        Image picture = pr.gameObject.AddComponent<Image>();
        picture.sprite = World(Costume.None);
        picture.raycastTarget = false;
        var fit = pr.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 16f / 9f;

        foreach (string old in new[] { "hillBack", "hillFront", "cloud1", "cloud2" })
        {
            Transform t = bgRoot.Find(old);
            if (t != null) t.gameObject.SetActive(false);
        }

        // A soft pill behind the subtitle keeps it readable on every world
        Transform titlePanel = FindIn(scene, "Panel", bgRoot);
        Transform subtitle = titlePanel != null ? titlePanel.Find("subtitle") : null;
        if (subtitle != null)
        {
            Remove(titlePanel, "subtitlePill");
            var st = (RectTransform)subtitle;
            TMP_Text sText = subtitle.GetComponent<TMP_Text>();
            float w = Mathf.Min(st.sizeDelta.x, sText.GetPreferredValues(sText.text).x + 70f);
            Image pill = Img(titlePanel, "subtitlePill", chunky, st.anchorMin, st.anchoredPosition, new Vector2(w, st.sizeDelta.y - 4f));
            pill.rectTransform.anchorMax = st.anchorMax;
            pill.type = Image.Type.Sliced;
            pill.color = new Color(1f, 1f, 1f, 0.78f);
            pill.transform.SetSiblingIndex(subtitle.GetSiblingIndex());
        }

        TitleBackground tb = bgRoot.GetComponent<TitleBackground>();
        if (tb == null) tb = bgRoot.gameObject.AddComponent<TitleBackground>();
        tb.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        tb.picture = picture;
        tb.worlds = new Sprite[CostumeRules.Count];
        for (int i = 0; i < CostumeRules.Count; i++) tb.worlds[i] = World((Costume)i);
        Transform logo = titlePanel != null ? titlePanel.Find("logo") : null;
        if (logo != null)
        {
            tb.logo = logo.GetComponent<TMP_Text>();
            tb.logoOnLight = tb.logo.color;
            tb.logoOnDark = RedesignSetup.TextColor(Color.white);
        }
        EditorUtility.SetDirty(tb);
    }

    // A small landscape (frame, sun, two hills) for the Backgrounds buttons
    private static Sprite PictureIcon()
    {
        if (UIEnhancer.LoadIcon("icon_picture") == null)
        {
            UIEnhancer.WriteIcon("icon_picture", (x, y) =>
            {
                bool inFrame = x >= 8f && x <= 120f && y >= 18f && y <= 110f;
                bool border = inFrame && (x <= 18f || x >= 110f || y <= 28f || y >= 100f);
                bool sun = (x - 86f) * (x - 86f) + (y - 78f) * (y - 78f) <= 12f * 12f;
                bool hill1 = y <= 76f - Mathf.Abs(x - 44f) * 1.15f;
                bool hill2 = y <= 62f - Mathf.Abs(x - 86f) * 0.95f;
                return border || (inFrame && (sun || hill1 || hill2));
            });
        }
        return UIEnhancer.LoadIcon("icon_picture");
    }

    // Customize: the chosen world behind a dim veil, a title, and two
    // boards side by side. Left: Costume (big 3D mTiny + scrolling cards).
    // Right: Backgrounds (scrolling world cards). Cards keep their full
    // size; the lists scroll instead of squeezing.
    private static void CustomizeRoom(Transform canvas, out DressUpUI costumes, out BackgroundsUI worlds)
    {
        RectTransform panel = Rect("customizePanel", canvas);
        Fill(panel, 0f);
        Image catcher = panel.gameObject.AddComponent<Image>();
        catcher.color = Ink;                       // under the picture; also blocks taps to the title

        RectTransform backR = Rect("backdrop", panel);
        Fill(backR, 0f);
        Image backdrop = backR.gameObject.AddComponent<Image>();
        backdrop.sprite = World(Costume.None);
        backdrop.raycastTarget = false;
        var fit = backR.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 16f / 9f;
        RectTransform dimR = Rect("dim", panel);
        Fill(dimR, 0f);
        Image dim = dimR.gameObject.AddComponent<Image>();
        dim.color = new Color(Ink.r, Ink.g, Ink.b, 0.35f);
        dim.raycastTarget = false;

        Image titlePill = Img(panel, "titlePill", chunky, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(520f, 112f));
        titlePill.type = Image.Type.Sliced; titlePill.color = Cream;
        Txt(titlePill.transform, "title", "Customize", 72, Ink, title, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(500f, 100f)).fontStyle = FontStyles.Bold;

        Image close = Img(panel, "closeButton", chunkyCircle, new Vector2(0f, 1f), new Vector2(95f, -80f), new Vector2(110f, 110f));
        close.color = Orange;
        close.raycastTarget = true;
        Button closeBtn = close.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = close;
        close.gameObject.AddComponent<TouchButtonFeedback>();
        Img(close.transform, "icon", UIEnhancer.LoadIcon("icon_home"), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(58f, 58f));

        // ---------------- LEFT: COSTUME ----------------
        Image left = Board(panel, "costumeBoard", new Vector2(-475f, -60f), Icon(Costume.Panda), Color.white, "Costume", Music);

        Image floor = Img(left.transform, "floor", knob, new Vector2(0.5f, 0.5f), new Vector2(-260f, -150f), new Vector2(300f, 54f));
        floor.color = Hex("CFE3F5"); floor.preserveAspect = false;
        RectTransform previewRect = Rect("preview", left.transform);
        Place(previewRect, new Vector2(0.5f, 0.5f), new Vector2(-260f, 30f), new Vector2(420f, 420f));
        RawImage preview = previewRect.gameObject.AddComponent<RawImage>();
        preview.raycastTarget = false;
        TMP_Text nameText = Txt(left.transform, "costumeName", "Plain mTiny", 46, Ink, title, new Vector2(0.5f, 0.5f), new Vector2(-260f, -230f), new Vector2(340f, 70f));
        nameText.fontStyle = FontStyles.Bold;
        nameText.enableAutoSizing = true; nameText.fontSizeMin = 30; nameText.fontSizeMax = 46;

        RectTransform costumeList = ScrollList(left.transform, "costumeList", new Vector2(170f, -45f), new Vector2(520f, 730f),
            new Vector2(240f, 260f), new Vector2(16f, 18f));

        costumes = panel.gameObject.AddComponent<DressUpUI>();
        costumes.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        costumes.panel = panel.gameObject;
        costumes.previewImage = preview;
        costumes.costumeName = nameText;
        costumes.robotModel = RobotModelAsset();
        costumes.cards = new DressUpUI.CostumeCard[CostumeRules.Count];
        for (int i = 0; i < CostumeRules.Count; i++)
        {
            Costume costume = (Costume)i;
            RectTransform slot = Rect("slot" + i, costumeList);
            Image ring = Img(slot, "selected", chunky, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Fill(ring.rectTransform, -10f);
            ring.type = Image.Type.Sliced; ring.color = Move;
            Image cardImg = Img(slot, "card", chunky, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Fill(cardImg.rectTransform, 0f);
            cardImg.type = Image.Type.Sliced; cardImg.color = Color.white;
            cardImg.raycastTarget = true;
            Button cardBtn = cardImg.gameObject.AddComponent<Button>();
            cardBtn.targetGraphic = cardImg;
            cardImg.gameObject.AddComponent<TouchButtonFeedback>();
            Image pic = Img(cardImg.transform, "picture", Picture(costume), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(150f, 150f));
            pic.preserveAspect = true;
            Txt(cardImg.transform, "name", CostumeRules.ShortName(costume), 30, Ink, title, new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(220f, 40f)).fontStyle = FontStyles.Bold;
            TMP_Text hint = Txt(cardImg.transform, "hint", "", 22, Hex("6B6788"), body, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(220f, 32f));
            Image barBg = Img(cardImg.transform, "bar", chunky, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(170f, 14f));
            barBg.type = Image.Type.Sliced; barBg.pixelsPerUnitMultiplier = 6f; barBg.color = Locked;
            Image barFillImg = Img(barBg.transform, "fill", barFill, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 8f));
            barFillImg.type = Image.Type.Filled; barFillImg.fillMethod = Image.FillMethod.Horizontal; barFillImg.color = Repeat;
            Image lockBg = Img(cardImg.transform, "lock", chunkyCircle, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(70f, 70f));
            lockBg.color = Hex("6D6880");
            Img(lockBg.transform, "icon", padlock, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(40f, 40f));
            costumes.cards[i] = new DressUpUI.CostumeCard
            {
                costume = costume, button = cardBtn, picture = pic, lockIcon = lockBg.gameObject,
                selected = ring.gameObject, hint = hint, progressFill = barFillImg
            };
        }

        // ---------------- RIGHT: BACKGROUNDS ----------------
        Image right = Board(panel, "backgroundsBoard", new Vector2(475f, -60f), PictureIcon(), Orange, "Backgrounds", Move);
        RectTransform worldList = ScrollList(right.transform, "backgroundList", new Vector2(0f, -45f), new Vector2(860f, 730f),
            new Vector2(400f, 290f), new Vector2(20f, 24f));

        worlds = panel.gameObject.AddComponent<BackgroundsUI>();
        worlds.catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        worlds.panel = panel.gameObject;
        worlds.backdrop = backdrop;
        worlds.cards = new BackgroundsUI.WorldCard[CostumeRules.Count];
        for (int i = 0; i < CostumeRules.Count; i++)
        {
            Costume world = (Costume)i;
            RectTransform slot = Rect("slot" + i, worldList);
            Image ring = Img(slot, "selected", chunky, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Fill(ring.rectTransform, -10f);
            ring.type = Image.Type.Sliced; ring.color = Go;
            Image card = Img(slot, "card", chunky, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Fill(card.rectTransform, 0f);
            card.type = Image.Type.Sliced; card.color = Color.white;
            card.raycastTarget = true;
            Button btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card;
            card.gameObject.AddComponent<TouchButtonFeedback>();

            Image frame = Img(card.transform, "frame", chunky, new Vector2(0.5f, 1f), new Vector2(0f, -119f), new Vector2(376f, 212f));
            frame.type = Image.Type.Sliced;
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Image pic = Img(frame.transform, "picture", World(world), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(376f, 212f));

            Image nIcon = Img(card.transform, "icon", Icon(world), new Vector2(0.5f, 0f), new Vector2(-150f, 34f), new Vector2(50f, 42f));
            nIcon.preserveAspect = true;
            TMP_Text nm = Txt(card.transform, "name", BackgroundRules.Name(world), 30, Ink, title, new Vector2(0.5f, 0f), new Vector2(20f, 34f), new Vector2(320f, 44f));
            nm.fontStyle = FontStyles.Bold;

            // Locked: dark cover, padlock, the mask that opens it, the stage to finish
            RectTransform coverR = Rect("lock", frame.transform);
            Fill(coverR, 0f);
            Image cover = coverR.gameObject.AddComponent<Image>();
            cover.color = new Color(Ink.r, Ink.g, Ink.b, 0.62f);
            cover.raycastTarget = false;
            Image lockBg = Img(coverR, "padlock", chunkyCircle, new Vector2(0.5f, 0.5f), new Vector2(-56f, 26f), new Vector2(76f, 76f));
            lockBg.color = Hex("6D6880");
            Img(lockBg.transform, "icon", padlock, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(44f, 44f));
            Image lockMask = Img(coverR, "mask", Icon(world), new Vector2(0.5f, 0.5f), new Vector2(56f, 26f), new Vector2(96f, 76f));
            lockMask.preserveAspect = true;
            TMP_Text lockText = Txt(coverR, "text", "Finish Stage " + i, 32, Color.white, title, new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(360f, 46f));
            lockText.fontStyle = FontStyles.Bold;
            coverR.gameObject.SetActive(false);

            worlds.cards[i] = new BackgroundsUI.WorldCard
            {
                world = world, button = btn, picture = pic, lockCover = coverR.gameObject, lockText = lockText, selected = ring.gameObject
            };
        }

        UnityEventTools.AddPersistentListener(closeBtn.onClick, costumes.Close);
        UnityEventTools.AddPersistentListener(closeBtn.onClick, worlds.Close);
        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(costumes);
        EditorUtility.SetDirty(worlds);
    }

    // A cream board with a coloured heading (icon + word) along its top
    private static Image Board(Transform parent, string name, Vector2 pos, Sprite icon, Color iconColor, string heading, Color headColor)
    {
        Image board = Img(parent, name, chunky, new Vector2(0.5f, 0.5f), pos, new Vector2(900f, 880f));
        board.type = Image.Type.Sliced; board.color = Cream;
        Image head = Img(board.transform, "heading", chunky, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(420f, 76f));
        head.type = Image.Type.Sliced; head.color = headColor;
        Image ic = Img(head.transform, "icon", icon, new Vector2(0f, 0.5f), new Vector2(52f, 3f), new Vector2(58f, 50f));
        ic.preserveAspect = true; ic.color = iconColor == Orange ? Color.white : iconColor;
        Txt(head.transform, "label", heading, 46, Color.white, title, new Vector2(0.5f, 0.5f), new Vector2(30f, 4f), new Vector2(320f, 66f)).fontStyle = FontStyles.Bold;
        return board;
    }

    // A vertical scroll area: masked viewport, a grid of fixed-size cells
    // (2 across) that grows downward, and a slim scrollbar on the right
    private static RectTransform ScrollList(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 cell, Vector2 spacing)
    {
        RectTransform view = Rect(name, parent);
        Place(view, new Vector2(0.5f, 0.5f), pos, size);
        Image hit = view.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);       // invisible, but drags in the gaps still scroll
        view.gameObject.AddComponent<RectMask2D>();

        RectTransform content = Rect("content", view);
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = cell;
        grid.spacing = spacing;
        grid.padding = new RectOffset(12, 12, 14, 14);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;
        var sizer = content.gameObject.AddComponent<ContentSizeFitter>();
        sizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image track = Img(parent, name + "Bar", chunky, new Vector2(0.5f, 0.5f), pos + new Vector2(size.x / 2f + 12f, 0f), new Vector2(14f, size.y - 20f));
        track.type = Image.Type.Sliced; track.pixelsPerUnitMultiplier = 8f; track.color = Locked;
        track.raycastTarget = true;
        Image handle = Img(track.transform, "handle", chunky, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Fill(handle.rectTransform, 0f);
        handle.type = Image.Type.Sliced; handle.pixelsPerUnitMultiplier = 8f; handle.color = Hex("9C97B5");
        handle.raycastTarget = true;
        Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect = handle.rectTransform;
        bar.targetGraphic = handle;
        bar.direction = Scrollbar.Direction.BottomToTop;

        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return content;
    }

    private static void Fill(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset); r.offsetMax = new Vector2(-inset, -inset);
    }

    // The Customize button on the title screen. It takes the old Dress up
    // button's spot (where it was placed by hand), else the bottom-left.
    private static void CustomizeButton(Transform bgRoot, DressUpUI costumes, BackgroundsUI worlds)
    {
        Vector2 aMin = Vector2.zero, aMax = Vector2.zero, pivot = Vector2.zero, pos = new Vector2(325f, 40f);
        Transform spot = bgRoot.Find("customizeButton") ?? bgRoot.Find("dressUpButton");
        if (spot != null)
        {
            var s = (RectTransform)spot;
            aMin = s.anchorMin; aMax = s.anchorMax; pivot = s.pivot; pos = s.anchoredPosition;
        }
        Remove(bgRoot, "dressUpButton");
        Remove(bgRoot, "customizeButton");

        Image btn = Img(bgRoot, "customizeButton", chunky, aMin, pos, new Vector2(370f, 112f));
        btn.rectTransform.anchorMax = aMax;
        btn.rectTransform.pivot = pivot;
        btn.rectTransform.anchoredPosition = pos;
        btn.type = Image.Type.Sliced; btn.color = Music;
        btn.raycastTarget = true;
        Button button = btn.gameObject.AddComponent<Button>();
        button.targetGraphic = btn;
        btn.gameObject.AddComponent<TouchButtonFeedback>();
        Image mIcon = Img(btn.transform, "icon", Icon(Costume.Panda), new Vector2(0f, 0.5f), new Vector2(62f, 6f), new Vector2(84f, 60f));
        mIcon.preserveAspect = true;
        TMP_Text label = Txt(btn.transform, "label", "Customize", 44, Color.white, title, new Vector2(0f, 0.5f), new Vector2(228f, 6f), new Vector2(256f, 70f));
        label.enableAutoSizing = true; label.fontSizeMin = 34; label.fontSizeMax = 44;
        label.fontStyle = FontStyles.Bold;
        UnityEventTools.AddPersistentListener(button.onClick, costumes.Open);
        UnityEventTools.AddPersistentListener(button.onClick, worlds.Open);
    }

    // Settings goes back to sound, Unlock all Stages and Reset only
    private static void TidySettings(Scene scene)
    {
        Transform settings = FindIn(scene, "settingsPanel");
        Transform box = settings != null ? settings.Find("box") : null;
        if (box == null) return;
        Remove(box, "costumeButton");
        Remove(box, "backgroundsButton");

        var boxR = (RectTransform)box;
        boxR.sizeDelta = new Vector2(boxR.sizeDelta.x, 800f);
        SettingsRow(box, "musicToggle", 195f, 124f);
        SettingsRow(box, "musicVolume", 78f, 100f);
        SettingsRow(box, "soundToggle", -40f, 124f);
        SettingsRow(box, "resetButton", -245f, 124f);
        Transform unlockAll = box.Find("unlockTogle");
        if (unlockAll != null)
        {
            var u = (RectTransform)unlockAll;
            u.anchoredPosition = new Vector2(u.anchoredPosition.x, -140f);
        }
    }

    private static void SettingsRow(Transform box, string name, float y, float h)
    {
        var r = box.Find(name) as RectTransform;
        if (r == null) return;
        r.anchoredPosition = new Vector2(r.anchoredPosition.x, y);
        r.sizeDelta = new Vector2(r.sizeDelta.x, h);
    }

    // =========================================================
    // MISSION: finish card row and the New mask! card
    // =========================================================

    private static void BuildMission()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Mission.unity", OpenSceneMode.Single);
        MissionRunner runner = Object.FindFirstObjectByType<MissionRunner>(FindObjectsInactive.Include);

        foreach (GameObject root in scene.GetRootGameObjects().Where(r => r.name == "CostumeReward").ToList()) Object.DestroyImmediate(root);

        // ---- row on the finish card, under the stars ----
        Transform finish = FindIn(scene, "Panel", FindIn(scene, "finishPanel"));
        Remove(finish, "costumeRow");
        Image row = Img(finish, "costumeRow", chunky, new Vector2(0.5f, 0.5f), new Vector2(-122f, -246f), new Vector2(470f, 100f));
        row.type = Image.Type.Sliced; row.color = Color.white; row.raycastTarget = false;
        Image icon = Img(row.transform, "icon", null, new Vector2(0f, 0.5f), new Vector2(64f, 2f), new Vector2(100f, 72f));
        icon.preserveAspect = true;
        TMP_Text rowTitle = Txt(row.transform, "title", "Chicken mask", 32, Ink, title, new Vector2(0f, 1f), new Vector2(276f, -26f), new Vector2(300f, 40f));
        rowTitle.alignment = TextAlignmentOptions.MidlineLeft; rowTitle.fontStyle = FontStyles.Bold;
        Image barBg = Img(row.transform, "bar", chunky, new Vector2(0f, 0.5f), new Vector2(276f, -2f), new Vector2(300f, 22f));
        barBg.type = Image.Type.Sliced; barBg.pixelsPerUnitMultiplier = 3f; barBg.color = Locked;
        Image fill = Img(barBg.transform, "fill", barFill, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(288f, 14f));
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.color = Repeat;
        TMP_Text count = Txt(row.transform, "count", "5 of 10 levels", 24, Ink, body, new Vector2(0f, 0f), new Vector2(276f, 20f), new Vector2(300f, 30f));
        count.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_Text plus = Txt(row.transform, "plus", "+1", 36, Go, title, new Vector2(1f, 0.5f), new Vector2(-24f, 16f), new Vector2(60f, 44f));
        plus.fontStyle = FontStyles.Bold;
        plus.alpha = 0f;
        row.gameObject.SetActive(false);

        // ---- New mask! card on its own overlay ----
        var rootGo = new GameObject("CostumeReward", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        CostumeRewards rewards = rootGo.AddComponent<CostumeRewards>();

        RectTransform card = Rect("newMask", rootGo.transform);
        card.anchorMin = Vector2.zero; card.anchorMax = Vector2.one; card.offsetMin = card.offsetMax = Vector2.zero;
        Image dim = card.gameObject.AddComponent<Image>();
        dim.color = new Color(Ink.r, Ink.g, Ink.b, 0.45f);

        Image body_ = Img(card, "card", chunky, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 780f));
        body_.type = Image.Type.Sliced; body_.color = Cream;
        body_.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        body_.gameObject.AddComponent<PopIn>();
        Image rays = Img(body_.transform, "rays", AssetDatabase.LoadAssetAtPath<Sprite>(PictureFolder + "/rays.png"),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(980f, 980f));
        rays.color = new Color(1f, 0.85f, 0.4f, 0.75f); rays.raycastTarget = false;
        TMP_Text head = Txt(body_.transform, "title", "New mask!", 88, Orange, title, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(760f, 110f));
        head.fontStyle = FontStyles.Bold;
        TMP_Text msg = Txt(body_.transform, "message", "", 34, Ink, body, new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(740f, 90f));
        msg.enableWordWrapping = true; msg.enableAutoSizing = true; msg.fontSizeMin = 24; msg.fontSizeMax = 34;
        Image pic = Img(body_.transform, "picture", null, new Vector2(0.5f, 0.5f), new Vector2(-200f, -20f), new Vector2(330f, 330f));
        pic.preserveAspect = true; pic.gameObject.AddComponent<Bob>().height = 10f;
        TMP_Text plusSign = Txt(body_.transform, "plus", "+", 80, Orange, title, new Vector2(0.5f, 0.5f), new Vector2(-10f, -20f), new Vector2(70f, 90f));
        plusSign.fontStyle = FontStyles.Bold;
        Image worldFrame = Img(body_.transform, "worldFrame", chunky, new Vector2(0.5f, 0.5f), new Vector2(190f, -20f), new Vector2(320f, 186f));
        worldFrame.type = Image.Type.Sliced; worldFrame.color = Color.white;
        worldFrame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        Image worldPic = Img(worldFrame.transform, "world", World(Costume.Chicken), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(304f, 171f));
        worldPic.preserveAspect = false;

        Image wear = Img(body_.transform, "wearButton", chunky, new Vector2(0.5f, 0f), new Vector2(-150f, 92f), new Vector2(380f, 116f));
        wear.type = Image.Type.Sliced; wear.color = Go;
        wear.raycastTarget = true;
        Button wearBtn = wear.gameObject.AddComponent<Button>(); wearBtn.targetGraphic = wear;
        wear.gameObject.AddComponent<TouchButtonFeedback>();
        Txt(wear.transform, "label", "Use both", 44, Color.white, title, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(360f, 80f)).fontStyle = FontStyles.Bold;
        Image later = Img(body_.transform, "laterButton", chunky, new Vector2(0.5f, 0f), new Vector2(200f, 92f), new Vector2(260f, 116f));
        later.type = Image.Type.Sliced; later.color = Move;
        later.raycastTarget = true;
        Button laterBtn = later.gameObject.AddComponent<Button>(); laterBtn.targetGraphic = later;
        later.gameObject.AddComponent<TouchButtonFeedback>();
        Txt(later.transform, "label", "Later", 44, Color.white, title, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(240f, 80f)).fontStyle = FontStyles.Bold;

        rewards.row = row.gameObject;
        rewards.rowIcon = icon;
        rewards.rowTitle = rowTitle;
        rewards.rowFill = fill;
        rewards.rowCount = count;
        rewards.plusOne = plus;
        rewards.newCard = card.gameObject;
        rewards.newPicture = pic;
        rewards.newWorld = worldPic;
        rewards.newMessage = msg;
        rewards.wearButton = wearBtn;
        rewards.laterButton = laterBtn;
        rewards.rays = rays.rectTransform;
        rewards.maskIcons = new Sprite[CostumeRules.Count];
        rewards.costumePictures = new Sprite[CostumeRules.Count];
        rewards.worldPictures = new Sprite[CostumeRules.Count];
        for (int i = 0; i < CostumeRules.Count; i++)
        {
            rewards.maskIcons[i] = Icon((Costume)i);
            rewards.costumePictures[i] = Picture((Costume)i);
            rewards.worldPictures[i] = World((Costume)i);
        }
        card.gameObject.SetActive(false);

        runner.costumeRewards = rewards;
        EditorUtility.SetDirty(rewards);
        EditorUtility.SetDirty(runner);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    private static Image Img(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform r = Rect(name, parent);
        Place(r, anchor, pos, size);
        Image i = r.gameObject.AddComponent<Image>();
        i.sprite = sprite;
        i.raycastTarget = false;   // buttons switch this back on
        return i;
    }

    private static TMP_Text Txt(Transform parent, string name, string text, float size, Color color, TMP_FontAsset font,
        Vector2 anchor, Vector2 pos, Vector2 box)
    {
        RectTransform r = Rect(name, parent);
        Place(r, anchor, pos, box);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = RedesignSetup.TextColor(color);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    private static void Remove(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
    }

    private static Transform FindIn(Scene scene, string name, Transform under = null)
    {
        return scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == name && (under == null || t.IsChildOf(under)));
    }

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }
}
