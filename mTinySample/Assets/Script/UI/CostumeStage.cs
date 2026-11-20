using UnityEngine;

// A small 3D stage far from the menu: the mTiny model on a turntable,
// a camera that draws it into a picture for the UI, and one shared light.
// Used by the title screen and the Costume room.
public class CostumeStage
{
    public Transform Turntable { get; private set; }
    public CostumeWearer Wearer { get; private set; }
    public Camera Camera { get; private set; }
    public RenderTexture Texture { get; private set; }

    private const string LightName = "CostumeStageLight";

    public static CostumeStage Build(GameObject modelAsset, Vector3 position, int size, float distance, Vector3 viewFrom)
    {
        var stage = new CostumeStage();
        var root = new GameObject("CostumeStage");
        root.transform.position = position;

        stage.Turntable = new GameObject("turntable").transform;
        stage.Turntable.SetParent(root.transform, false);
        if (modelAsset != null)
        {
            Object.Instantiate(modelAsset, stage.Turntable).transform.localPosition = Vector3.zero;
        }
        stage.Wearer = stage.Turntable.gameObject.AddComponent<CostumeWearer>();
        stage.Wearer.wearSavedCostume = false;

        Bounds b = new Bounds(stage.Turntable.position, Vector3.zero);
        foreach (Renderer r in stage.Turntable.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);

        // One light for every stage (two would light each model twice)
        if (GameObject.Find(LightName) == null)
        {
            var light = new GameObject(LightName).AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, -0.6f, 0.6f));
            light.cullingMask = 1 << 0;
        }

        stage.Camera = new GameObject("camera").AddComponent<Camera>();
        stage.Camera.transform.SetParent(root.transform, false);
        stage.Camera.clearFlags = CameraClearFlags.SolidColor;
        stage.Camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        stage.Camera.fieldOfView = 30f;
        stage.Camera.cullingMask = 1 << 0;
        stage.Camera.transform.position = b.center + viewFrom.normalized * b.size.magnitude * distance;
        stage.Camera.transform.LookAt(b.center, Vector3.back);

        stage.Texture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        stage.Camera.targetTexture = stage.Texture;

        // Turn around mTiny's own up (towards the top of its head)
        stage.Turntable.position = b.center;
        foreach (Transform child in stage.Turntable) child.position -= b.center - position;
        return stage;
    }

    public void Release()
    {
        if (Texture != null) Texture.Release();
    }
}
