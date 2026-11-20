// mTiny's backgrounds ("worlds"): one for each costume, opened together
// with it. Plain mTiny's Sunny Park is always yours; finishing a stage
// gives its mask and its world. "Unlock all Stages" opens them all.
public static class BackgroundRules
{
    private const string SelectedKey = "mtiny.background";

    private static readonly string[] Names =
    {
        "Sunny Park", "Bamboo Forest", "Happy Farm", "Dog Town", "Cosy Cat Room", "Champion Stage"
    };

    // Picture file names under Assets/Image/worlds (world_<file>.png)
    private static readonly string[] Files = { "park", "bamboo", "farm", "dogtown", "catroom", "champion" };

    public static string Name(Costume c) => Names[(int)c];

    public static string File(Costume c) => Files[(int)c];

    // A dark world: the title's logo turns light on it
    public static bool IsDark(Costume c) => c == Costume.Golden;

    public static bool IsUnlocked(MissionProgress progress, MissionCatalog catalog, Costume c)
    {
        return CostumeRules.IsUnlocked(progress, catalog, c);
    }

    // The world on the title screen (a locked one falls back to Sunny Park)
    public static Costume Selected(IProgressStore store, MissionProgress progress, MissionCatalog catalog)
    {
        Costume c = SelectedRaw(store);
        return IsUnlocked(progress, catalog, c) ? c : Costume.None;
    }

    public static Costume SelectedRaw(IProgressStore store)
    {
        int raw = store.GetInt(SelectedKey, 0);
        return raw >= 0 && raw < CostumeRules.Count ? (Costume)raw : Costume.None;
    }

    public static void Select(IProgressStore store, Costume c)
    {
        store.SetInt(SelectedKey, (int)c);
        store.Save();
    }

    // Teacher reset: back to Sunny Park
    public static void Reset(IProgressStore store)
    {
        store.DeleteKey(SelectedKey);
    }
}
