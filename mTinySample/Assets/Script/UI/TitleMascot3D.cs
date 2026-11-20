using UnityEngine;
using UnityEngine.UI;

// The title screen's mTiny, drawn live in 3D and wearing the costume the
// child picked in the Costume room. It sways gently from side to side;
// the mascot spot it sits in keeps its own bob.
public class TitleMascot3D : MonoBehaviour
{
    public MissionCatalog catalog;
    public RawImage picture;
    public GameObject robotModel;
    public Vector3 stagePosition = new Vector3(-500f, 500f, 0f);
    public float swayDegrees = 14f;

    private CostumeStage stage;

    private void OnEnable()
    {
        if (stage == null)
        {
            stage = CostumeStage.Build(robotModel, stagePosition, 512, 1.7f, new Vector3(0f, 1f, -0.3f));
            if (picture != null) picture.texture = stage.Texture;
        }
        stage.Camera.enabled = true;
        MissionMapUI.UnlockAllChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        MissionMapUI.UnlockAllChanged -= Refresh;
        if (stage != null && stage.Camera != null) stage.Camera.enabled = false;
    }

    // Wear the saved costume (called again when the Costume room closes)
    public void Refresh()
    {
        if (stage == null) return;
        var store = new PlayerPrefsProgressStore();
        Costume c = catalog != null
            ? CostumeRules.Selected(store, new MissionProgress(catalog, store), catalog)
            : CostumeRules.SelectedRaw(store);
        stage.Wearer.Wear(c);
    }

    private void Update()
    {
        if (stage == null) return;
        float angle = Mathf.Sin(Time.unscaledTime * 0.8f) * swayDegrees;
        stage.Turntable.localRotation = Quaternion.AngleAxis(angle, Vector3.back);
    }

    private void OnDestroy()
    {
        if (stage != null) stage.Release();
    }
}
