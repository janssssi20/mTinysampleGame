using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering;

// Makes the game fit Windows, Android phones/tablets and web browsers,
// including low-spec devices. Safe to run again.
//   - Player settings: landscape only; Android 32 + 64-bit (IL2CPP) with
//     OpenGL ES 3 first; web builds compressed (gzip) and size-optimised
//   - Quality: Windows High, Android and Web Medium (low-end devices drop
//     to Low by themselves, see PerformanceMode)
//   - Images: the UI sprites the game uses are packed into sprite atlases
//     (they compress properly and draw in fewer batches); other images
//     are compressed per platform (crunched on the web)
//   - Sounds: music streams, sound effects are mono
// Build helpers: mTiny > Build > Windows / Android / Web.
public static class PlatformOptimizer
{
    private const string AtlasFolder = "Assets/Image/Atlases";
    private const string TemplateName = "mTinyPWA";
    private const int MaxAtlasSize = 2048;

    // Sprite atlas groups: name -> folders (with the trailing slash)
    private static readonly (string name, string[] folders)[] Groups =
    {
        ("ui", new[] { "Assets/Image/ui/" }),
        ("blocks", new[] { "Assets/Image/mtiny command blocks/" }),
        ("tiles", new[] { "Assets/Image/Tilemaps1/", "Assets/Image/Tilemaps2 assets/" }),
        ("emotions", new[] { "Assets/Image/emotions/" }),
        ("costumes", new[] { "Assets/Image/costumes/" }),
        ("layout", new[] { "Assets/Image/layout/", "Assets/Image/cues/", "Assets/Image/" }),
    };

    // Never packed: big pictures that already compress, and unused art
    private static readonly string[] NotPacked = { "Assets/Image/worlds/", "Assets/Image/mtiny emotions/", AtlasFolder + "/" };

    [MenuItem("mTiny/Setup/Optimize for Windows, Mobile and Web")]
    public static void Run()
    {
        PlayerOptions();
        QualityDefaults();
        WebTemplate();
        var packed = Atlases();
        LooseTextures(packed);
        MaskTextures();
        Sounds();
        AssetDatabase.SaveAssets();
        Debug.Log("[mTiny] Optimised for Windows, Android and Web (" + packed.Count + " sprites in atlases).");
    }

    // =========================================================
    // PLAYER SETTINGS
    // =========================================================

    private static void PlayerOptions()
    {
        // Landscape only: the screens are built for it
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.stripEngineCode = true;

        // Windows: fullscreen window that can also be resized
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;

        // Android: 32 and 64-bit (64-bit-only phones and Google Play need
        // ARM64, which needs IL2CPP); OpenGL ES 3 first, the safest on
        // cheap tablets, Vulkan as the second choice
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan });

        // Web: compressed files that any web server can host, small code,
        // cached between visits, a modest memory ceiling for phones
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.initialMemorySize = 32;
        PlayerSettings.WebGL.maximumMemorySize = 512;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
    }

    // Windows High, Android and Web Medium (instead of Ultra)
    private static void QualityDefaults()
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
        if (asset == null) return;
        var so = new SerializedObject(asset);
        SerializedProperty map = so.FindProperty("m_PerPlatformDefaultQuality");
        var wanted = new Dictionary<string, string> { { "Standalone", "High" }, { "WebGL", "Medium" }, { "Android", "Medium" } };
        for (int i = 0; i < map.arraySize; i++)
        {
            SerializedProperty pair = map.GetArrayElementAtIndex(i);
            string platform = pair.FindPropertyRelative("first").stringValue;
            string level;
            if (!wanted.TryGetValue(platform, out level)) continue;
            int index = Array.IndexOf(QualitySettings.names, level);
            if (index >= 0) pair.FindPropertyRelative("second").intValue = index;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The web page: Unity's PWA page, but phones draw at most 1.5x their
    // screen size (2-3x screens are very heavy for low-spec phones)
    private static void WebTemplate()
    {
        string source = Path.Combine(EditorApplication.applicationContentsPath,
            "PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/PWA");
        string target = "Assets/WebGLTemplates/" + TemplateName;
        if (!Directory.Exists(target))
        {
            if (!Directory.Exists(source)) { Debug.LogWarning("[mTiny] Web module not found: web page left as it is."); return; }
            CopyFolder(source, target);
        }
        string index = Path.Combine(target, "index.html");
        string html = File.ReadAllText(index);
        const string marker = "// mTiny: phones draw at most 1.5x";
        if (!html.Contains(marker))
        {
            const string anchor = "        showBanner: unityShowBanner,\n      };";
            string fixedHtml = html.Replace("\r\n", "\n");
            if (fixedHtml.Contains(anchor))
            {
                fixedHtml = fixedHtml.Replace(anchor, anchor + "\n\n      " + marker + " (2-3x screens are heavy)\n" +
                    "      if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {\n" +
                    "        config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);\n" +
                    "      }");
                File.WriteAllText(index, fixedHtml);
            }
            else Debug.LogWarning("[mTiny] Web page: could not find where to add the phone setting.");
        }
        AssetDatabase.Refresh();
        PlayerSettings.WebGL.template = "PROJECT:" + TemplateName;
    }

    private static void CopyFolder(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (string f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
        foreach (string d in Directory.GetDirectories(from)) CopyFolder(d, Path.Combine(to, Path.GetFileName(d)));
    }

    // =========================================================
    // SPRITE ATLASES
    // =========================================================

    // Packs the sprites the game uses; returns the packed texture paths
    private static HashSet<string> Atlases()
    {
        Directory.CreateDirectory(AtlasFolder);
        HashSet<string> used = UsedTextures(out HashSet<string> asTexture);

        var packed = new HashSet<string>();
        foreach (var group in Groups)
        {
            var members = used.Where(p =>
                    group.folders.Any(f => p.StartsWith(f) && (f != "Assets/Image/" || Path.GetDirectoryName(p).Replace('\\', '/') == "Assets/Image")) &&
                    !NotPacked.Any(n => p.StartsWith(n)) &&
                    !asTexture.Contains(p) && Fits(p))
                .OrderBy(p => p).ToList();

            // Command blocks are drawn about 110 px tall: on phones, tablets
            // and the web they are packed at 256 px (Windows keeps full size
            // for big monitors)
            if (group.name == "blocks")
            {
                foreach (string m in members)
                {
                    var src = (TextureImporter)AssetImporter.GetAtPath(m);
                    src.SetPlatformTextureSettings(Platform("Android", true, TextureImporterFormat.RGBA32, 256, TextureImporterCompression.Uncompressed));
                    src.SetPlatformTextureSettings(Platform("WebGL", true, TextureImporterFormat.RGBA32, 256, TextureImporterCompression.Uncompressed));
                    src.SaveAndReimport();
                }
            }

            string path = AtlasFolder + "/atlas_" + group.name + ".spriteatlasv2";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            if (members.Count == 0) continue;

            var atlas = new SpriteAtlasAsset();
            atlas.Add(members.Select(m => (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Texture2D>(m)).Where(o => o != null).ToArray());
            SpriteAtlasAsset.Save(atlas, path);
            AssetDatabase.ImportAsset(path);

            var imp = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            imp.includeInBuild = true;
            imp.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1, padding = 4, enableRotation = false, enableTightPacking = false, enableAlphaDilation = true
            };
            imp.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear, anisoLevel = 0
            };
            imp.SetPlatformSettings(Platform("DefaultTexturePlatform", false, TextureImporterFormat.Automatic, MaxAtlasSize));
            imp.SetPlatformSettings(Platform("Standalone", true, TextureImporterFormat.BC7, MaxAtlasSize));
            imp.SetPlatformSettings(Platform("Android", true, TextureImporterFormat.ETC2_RGBA8, MaxAtlasSize));
            imp.SetPlatformSettings(Platform("WebGL", true, TextureImporterFormat.DXT5Crunched, MaxAtlasSize));
            imp.SaveAndReimport();
            packed.UnionWith(members);
        }
        return packed;
    }

    // Textures used by the build's scenes and Resources. asTexture: used
    // as a plain texture (a material or a RawImage), so not packable.
    private static HashSet<string> UsedTextures(out HashSet<string> asTexture)
    {
        // Every listed scene, switched on or not, so a scene turned back on
        // later is covered too
        var roots = EditorBuildSettings.scenes.Select(s => s.path).ToList();
        roots.AddRange(AssetDatabase.FindAssets("", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath));
        string[] deps = AssetDatabase.GetDependencies(roots.ToArray(), true);

        var used = new HashSet<string>(deps.Where(p => p.StartsWith("Assets/Image/") && IsImage(p)));
        asTexture = new HashSet<string>();
        var textureRef = new Regex(@"fileID: 2800000, guid: ([0-9a-f]{32})");
        foreach (string d in deps.Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".mat") || p.EndsWith(".asset")))
        {
            foreach (Match m in textureRef.Matches(File.ReadAllText(d)))
            {
                string p = AssetDatabase.GUIDToAssetPath(m.Groups[1].Value);
                if (!string.IsNullOrEmpty(p)) asTexture.Add(p);
            }
        }
        return used;
    }

    private static bool IsImage(string p)
    {
        string e = Path.GetExtension(p).ToLowerInvariant();
        return e == ".png" || e == ".jpg" || e == ".jpeg" || e == ".tga" || e == ".psd";
    }

    // Sprites only, and small enough for one atlas page
    private static bool Fits(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null || imp.textureType != TextureImporterType.Sprite) return false;
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);
        return w <= MaxAtlasSize - 16 && h <= MaxAtlasSize - 16;
    }

    private static TextureImporterPlatformSettings Platform(string name, bool overridden, TextureImporterFormat format, int max,
        TextureImporterCompression compression = TextureImporterCompression.Compressed)
    {
        return new TextureImporterPlatformSettings
        {
            name = name, overridden = overridden, format = format, maxTextureSize = max,
            textureCompression = compression, compressionQuality = 50, crunchedCompression = format.ToString().Contains("Crunched")
        };
    }

    // =========================================================
    // OTHER IMAGES
    // =========================================================

    // Images the game uses that are not in an atlas: compressed on every
    // platform. Sides that are not a multiple of 4 can't be compressed,
    // so the mask pictures (made for this game) get a transparent edge.
    private static void LooseTextures(HashSet<string> packed)
    {
        var loose = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Image", "Assets/Resources" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => IsImage(p) && !packed.Contains(p) && !p.StartsWith("Assets/Image/mtiny emotions/") && !p.StartsWith(AtlasFolder))
            .ToList();

        foreach (string path in loose)
        {
            if (path.StartsWith("Assets/Resources/Costumes/mask_"))
            {
                if (path.EndsWith("_3d.png")) continue;      // set up by MaskTextures
                PadToMultipleOf4(path);
                var flat = (TextureImporter)AssetImporter.GetAtPath(path);
                flat.mipmapEnabled = false;                  // the flat icon; the 3D copy has mipmaps
            }

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            bool opaque = path.StartsWith("Assets/Image/worlds/");
            if (opaque) imp.alphaSource = TextureImporterAlphaSource.None;
            bool alpha = !opaque && imp.DoesSourceTextureHaveAlpha();
            imp.GetSourceTextureWidthAndHeight(out int w, out int h);
            bool blocks = w % 4 == 0 && h % 4 == 0;
            // Keep the full size: shrinking a picture wider than 2048 can give
            // sides that are not a multiple of 4 (which can't be compressed)
            int max = Mathf.Min(4096, Mathf.NextPowerOfTwo(Mathf.Max(w, h)));

            if (imp.textureCompression == TextureImporterCompression.Uncompressed)
                imp.textureCompression = TextureImporterCompression.Compressed;
            if (blocks)
            {
                imp.SetPlatformTextureSettings(Platform("Standalone", true, alpha ? TextureImporterFormat.BC7 : TextureImporterFormat.DXT1, max));
                imp.SetPlatformTextureSettings(Platform("Android", true, alpha ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4, max));
                imp.SetPlatformTextureSettings(Platform("WebGL", true, alpha ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT1Crunched, max));
            }
            imp.SaveAndReimport();
        }
    }

    // The 3D masks: a 512 x 512 copy of each mask picture with mipmaps
    // (power-of-two sizes are the only ones that compress with mipmaps on
    // every device); the mask material uses it. The flat picture stays for
    // the icons and gives the 3D card its shape.
    public static void MaskTextures()
    {
        foreach (string guid in AssetDatabase.FindAssets("mask_ t:Texture2D", new[] { "Assets/Resources/Costumes" }))
        {
            string flatPath = AssetDatabase.GUIDToAssetPath(guid);
            if (flatPath.EndsWith("_3d.png") || !flatPath.EndsWith(".png")) continue;
            string path3d = flatPath.Substring(0, flatPath.Length - 4) + "_3d.png";

            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(flatPath));
            const int size = 512;
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = src.GetPixelBilinear((x + 0.5f) / size, (y + 0.5f) / size);
            dst.SetPixels(px);
            dst.Apply();
            File.WriteAllBytes(path3d, dst.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(src);
            UnityEngine.Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(path3d);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path3d);
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SetPlatformTextureSettings(Platform("Standalone", true, TextureImporterFormat.BC7, size));
            imp.SetPlatformTextureSettings(Platform("Android", true, TextureImporterFormat.ETC2_RGBA8, size));
            imp.SetPlatformTextureSettings(Platform("WebGL", true, TextureImporterFormat.DXT5Crunched, size));
            imp.SaveAndReimport();

            string matPath = flatPath.Substring(0, flatPath.Length - 4) + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path3d);
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void PadToMultipleOf4(string path)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);
        int nw = (w + 3) / 4 * 4, nh = (h + 3) / 4 * 4;
        if (nw == w && nh == h) return;

        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        src.LoadImage(File.ReadAllBytes(path));
        var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        var clear = new Color32[nw * nh];
        dst.SetPixels32(clear);
        // Centre the picture so the mask stays where it was
        int ox = (nw - w) / 2, oy = (nh - h) / 2;
        dst.SetPixels(ox, oy, w, h, src.GetPixels());
        dst.Apply();
        File.WriteAllBytes(path, dst.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(src);
        UnityEngine.Object.DestroyImmediate(dst);
        AssetDatabase.ImportAsset(path);
    }

    // =========================================================
    // SOUNDS
    // =========================================================

    private static void Sounds()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) continue;
            bool music = Path.GetFileName(path).StartsWith("music_");
            imp.forceToMono = !music;
            imp.loadInBackground = music;
            if (music)
            {
                // Streams from disk on Windows and Android instead of
                // sitting in memory (the web always streams compressed)
                foreach (string platform in new[] { "Standalone", "Android" })
                {
                    AudioImporterSampleSettings s = imp.GetOverrideSampleSettings(platform);
                    s.loadType = AudioClipLoadType.Streaming;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.6f;
                    imp.SetOverrideSampleSettings(platform, s);
                }
            }
            imp.SaveAndReimport();
        }
    }

    // Lists the images the game uses that still come out uncompressed
    // for the current build target (Console)
    [MenuItem("mTiny/Report/Uncompressed Images")]
    public static void ReportUncompressed()
    {
        HashSet<string> used = UsedTextures(out _);
        used.UnionWith(AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath));
        var atlases = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { AtlasFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a != null).ToList();
        int n = 0;
        foreach (string p in used.OrderBy(x => x))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (tex == null) continue;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (sprite != null && atlases.Any(a => a.CanBindTo(sprite))) continue;   // packed: the atlas is what ships
            string f = tex.format.ToString();
            if (f.StartsWith("RGBA32") || f.StartsWith("ARGB32") || f.StartsWith("RGB24") || f.StartsWith("BGRA32"))
            {
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                Debug.Log("[mTiny] uncompressed " + f + " " + tex.width + "x" + tex.height + " mips=" + tex.mipmapCount +
                          " type=" + (imp != null ? imp.textureType.ToString() : "?") + " " + p);
                n++;
            }
        }
        Debug.Log("[mTiny] " + n + " uncompressed images for " + EditorUserBuildSettings.activeBuildTarget);
    }

    // =========================================================
    // LOW-END PREVIEW AND BUILDS
    // =========================================================

    private const string SimulateMenu = "mTiny/Preview/Simulate Low-End Device";

    [MenuItem(SimulateMenu)]
    private static void ToggleSimulate()
    {
        bool on = !EditorPrefs.GetBool(PerformanceMode.SimulateKey, false);
        EditorPrefs.SetBool(PerformanceMode.SimulateKey, on);
        Debug.Log("[mTiny] Simulate low-end device: " + (on ? "on" : "off") + " (applies from the next Play).");
    }

    [MenuItem(SimulateMenu, true)]
    private static bool ToggleSimulateCheck()
    {
        Menu.SetChecked(SimulateMenu, EditorPrefs.GetBool(PerformanceMode.SimulateKey, false));
        return true;
    }

    private static string BuildFolder => Environment.GetEnvironmentVariable("MTINY_BUILD_DIR") ?? "Builds";

    [MenuItem("mTiny/Build/Windows")]
    public static void BuildWindows() { Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Windows/mTiny.exe"); }

    [MenuItem("mTiny/Build/Android")]
    public static void BuildAndroid() { Build(BuildTarget.Android, BuildTargetGroup.Android, "Android/mTiny.apk"); }

    [MenuItem("mTiny/Build/Web")]
    public static void BuildWeb() { Build(BuildTarget.WebGL, BuildTargetGroup.WebGL, "Web"); }

    private static void Build(BuildTarget target, BuildTargetGroup group, string relative)
    {
        string path = Path.Combine(BuildFolder, relative);
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = target,
            targetGroup = group,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary sum = report.summary;
        Debug.Log("[mTiny] Build " + target + ": " + sum.result + ", " + (sum.totalSize / (1024f * 1024f)).ToString("0.0") +
                  " MB, " + sum.totalErrors + " errors, " + sum.totalTime.TotalMinutes.ToString("0.0") + " min -> " + path);
        if (Application.isBatchMode) EditorApplication.Exit(sum.result == BuildResult.Succeeded ? 0 : 1);
    }
}
