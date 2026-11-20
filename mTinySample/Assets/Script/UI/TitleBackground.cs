using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The title screen's world picture: the background the child chose in
// Customize. On a dark world the logo turns light.
public class TitleBackground : MonoBehaviour
{
    public MissionCatalog catalog;
    public Image picture;
    public Sprite[] worlds = new Sprite[CostumeRules.Count];   // index = Costume

    [Header("Logo")]
    public TMP_Text logo;
    public Color logoOnLight = new Color(0.18f, 0.16f, 0.31f);
    public Color logoOnDark = Color.white;

    private void OnEnable()
    {
        MissionMapUI.UnlockAllChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        MissionMapUI.UnlockAllChanged -= Refresh;
    }

    public void Refresh()
    {
        var store = new PlayerPrefsProgressStore();
        Costume c = catalog != null
            ? BackgroundRules.Selected(store, new MissionProgress(catalog, store), catalog)
            : BackgroundRules.SelectedRaw(store);
        if (picture != null && (int)c < worlds.Length && worlds[(int)c] != null) picture.sprite = worlds[(int)c];
        if (logo != null) logo.color = BackgroundRules.IsDark(c) ? logoOnDark : logoOnLight;
    }

    public static void RefreshAll()
    {
        foreach (TitleBackground t in FindObjectsByType<TitleBackground>(FindObjectsSortMode.None)) t.Refresh();
    }
}
