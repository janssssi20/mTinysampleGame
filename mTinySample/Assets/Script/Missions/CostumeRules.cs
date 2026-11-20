// mTiny's costumes: one is earned by finishing every level of a stage.
// Stage 1 Panda, 2 Chicken, 3 Dog, 4 Cat, 5 Golden mTiny.
public enum Costume
{
    None,
    Panda,
    Chicken,
    Dog,
    Cat,
    Golden
}

public static class CostumeRules
{
    public const int Count = 6;

    private const string SelectedKey = "mtiny.costume";
    private const string ShownPrefix = "mtiny.costume.shown.";

    private static readonly string[] Names =
    {
        "Plain mTiny", "Panda mask", "Chicken mask", "Dog mask", "Cat mask", "Golden mTiny"
    };

    private static readonly string[] ShortNames = { "Plain mTiny", "Panda", "Chicken", "Dog", "Cat", "Golden" };

    public static string Name(Costume c) => Names[(int)c];

    public static string ShortName(Costume c) => ShortNames[(int)c];

    // The stage that gives a costume (Plain mTiny is always yours)
    public static MissionStage StageFor(Costume c) => (MissionStage)((int)c - 1);

    public static Costume ForStage(MissionStage stage) => (Costume)((int)stage + 1);

    public static int LevelsInStage(MissionCatalog catalog, MissionStage stage)
    {
        return catalog.IndicesInStage(stage).Count;
    }

    // Levels of a stage won with at least one star
    public static int LevelsWon(MissionProgress progress, MissionCatalog catalog, MissionStage stage)
    {
        int won = 0;
        foreach (int i in catalog.IndicesInStage(stage))
        {
            if (progress.GetBestStars(i) > 0) won++;
        }
        return won;
    }

    // Earned by winning every level of the costume's stage
    public static bool IsEarned(MissionProgress progress, MissionCatalog catalog, Costume c)
    {
        if (c == Costume.None) return true;
        MissionStage stage = StageFor(c);
        int total = LevelsInStage(catalog, stage);
        return total > 0 && LevelsWon(progress, catalog, stage) >= total;
    }

    // Can be worn: earned, or every costume while "Unlock all Stages" is on
    public static bool IsUnlocked(MissionProgress progress, MissionCatalog catalog, Costume c)
    {
        return progress.UnlockAllOn || IsEarned(progress, catalog, c);
    }

    // The costume mTiny wears (a locked one falls back to plain)
    public static Costume Selected(IProgressStore store, MissionProgress progress, MissionCatalog catalog)
    {
        int raw = store.GetInt(SelectedKey, 0);
        Costume c = raw >= 0 && raw < Count ? (Costume)raw : Costume.None;
        return IsUnlocked(progress, catalog, c) ? c : Costume.None;
    }

    // Without unlock checks (for places with no catalog, like Explore)
    public static Costume SelectedRaw(IProgressStore store)
    {
        int raw = store.GetInt(SelectedKey, 0);
        return raw >= 0 && raw < Count ? (Costume)raw : Costume.None;
    }

    public static void Select(IProgressStore store, Costume c)
    {
        store.SetInt(SelectedKey, (int)c);
        store.Save();
    }

    // True once, the first time a costume is earned: show "New mask!"
    // (the Unlock all switch opens costumes but never announces them)
    public static bool TakeNewUnlock(MissionProgress progress, MissionCatalog catalog, IProgressStore store, Costume c)
    {
        if (c == Costume.None || !IsEarned(progress, catalog, c)) return false;
        string key = ShownPrefix + c;
        if (store.GetInt(key, 0) != 0) return false;
        store.SetInt(key, 1);
        store.Save();
        return true;
    }

    // Teacher reset: back to plain mTiny, every reward hidden again
    public static void Reset(IProgressStore store)
    {
        store.DeleteKey(SelectedKey);
        for (int i = 0; i < Count; i++) store.DeleteKey(ShownPrefix + (Costume)i);
    }
}
