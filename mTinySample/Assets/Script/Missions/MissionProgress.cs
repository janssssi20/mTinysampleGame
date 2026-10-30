using UnityEngine;

public interface IProgressStore
{
    int GetInt(string key, int fallback);
    void SetInt(string key, int value);
    void DeleteKey(string key);
    void Save();
}

public sealed class PlayerPrefsProgressStore : IProgressStore
{
    public int GetInt(string key, int fallback) { return PlayerPrefs.GetInt(key, fallback); }
    public void SetInt(string key, int value) { PlayerPrefs.SetInt(key, value); }
    public void DeleteKey(string key) { PlayerPrefs.DeleteKey(key); }
    public void Save() { PlayerPrefs.Save(); }
}

// Unlock and star rules for the mission journey.
// A mission is finished once it has at least one star; the next one
// unlocks then. Stages unlock with their first mission.
public sealed class MissionProgress
{
    private const string StarsKeyPrefix = "mtiny.stars.";
    private const string CelebratedKey = "mtiny.celebrated";

    private readonly MissionCatalog catalog;
    private readonly IProgressStore store;

    public MissionProgress(MissionCatalog catalog, IProgressStore store)
    {
        this.catalog = catalog;
        this.store = store;
    }

    public int GetBestStars(int index)
    {
        if (index < 0 || index >= catalog.Count)
        {
            return 0;
        }

        return store.GetInt(StarsKeyPrefix + catalog.Get(index).id, 0);
    }

    public bool IsMissionUnlocked(int index)
    {
        if (index < 0 || index >= catalog.Count)
        {
            return false;
        }

        return index == 0 || GetBestStars(index - 1) > 0;
    }

    public bool IsStageUnlocked(MissionStage stage)
    {
        var indices = catalog.IndicesInStage(stage);

        return indices.Count > 0 && IsMissionUnlocked(indices[0]);
    }

    public int GetStageStars(MissionStage stage)
    {
        int total = 0;

        foreach (int index in catalog.IndicesInStage(stage))
        {
            total += GetBestStars(index);
        }

        return total;
    }

    public int GetTotalStars()
    {
        int total = 0;

        for (int i = 0; i < catalog.Count; i++)
        {
            total += GetBestStars(i);
        }

        return total;
    }

    // Returns true when the celebration should play: the first win
    // of the final mission.
    public bool RecordWin(int index, int stars)
    {
        if (index < 0 || index >= catalog.Count || stars <= 0)
        {
            return false;
        }

        int clamped = Mathf.Clamp(stars, 1, 3);

        if (clamped > GetBestStars(index))
        {
            store.SetInt(StarsKeyPrefix + catalog.Get(index).id, clamped);
        }

        bool celebrate = false;

        if (index == catalog.Count - 1 && store.GetInt(CelebratedKey, 0) == 0)
        {
            store.SetInt(CelebratedKey, 1);
            celebrate = true;
        }

        store.Save();

        return celebrate;
    }

    public void ResetAll()
    {
        for (int i = 0; i < catalog.Count; i++)
        {
            store.DeleteKey(StarsKeyPrefix + catalog.Get(i).id);
        }

        store.DeleteKey(CelebratedKey);
        store.Save();
    }
}
