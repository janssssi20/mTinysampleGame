using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Marked tiles for Stages 3-5: imports the badge art, gives it to the
// Mission scene's board builder and re-seeds the catalog (which holds
// each level's marked tiles). Touches nothing else; safe to run again.
public static class TileMarkersSetup
{
    private const string ArtFolder = "Assets/Image/cues/";

    [MenuItem("mTiny/Setup/Tile Markers")]
    public static void Run()
    {
        MissionCatalogSeeder.Seed();
        AssignArt();
        Debug.Log("[mTiny] Tile markers ready.");
    }

    public static void AssignArt()
    {
        foreach (string name in new[] { "cue_feel", "cue_music", "cue_done", "cue_glow" })
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(ArtFolder + name + ".png");
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 256f;   // the badge is one unit wide
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Mission.unity", OpenSceneMode.Single);
        var builder = Object.FindFirstObjectByType<MissionBoardBuilder>(FindObjectsInactive.Include);
        builder.cueFeelingSprite = Load("cue_feel");
        builder.cueMusicSprite = Load("cue_music");
        builder.cueDoneSprite = Load("cue_done");
        builder.cueGlowSprite = Load("cue_glow");
        builder.cueSize = 0.5f;
        EditorUtility.SetDirty(builder);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + name + ".png");
    }
}
