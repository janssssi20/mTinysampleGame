public enum NodeState
{
    Done,
    Next,
    Locked
}

// Earned once a stage's last mission is won; the tier follows the stars.
public enum TrophyTier
{
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3
}

// Display rules for the stage cards and mission path.
public static class StageMapModel
{
    // Share of a stage's stars needed for silver (10 of 15)
    private const float SilverShare = 2f / 3f;

    public static TrophyTier GetTrophy(MissionProgress progress, MissionCatalog catalog, MissionStage stage)
    {
        var indices = catalog.IndicesInStage(stage);

        if (indices.Count == 0 || progress.GetBestStars(indices[indices.Count - 1]) == 0)
        {
            return TrophyTier.None;
        }

        int stars = progress.GetStageStars(stage);
        int max = indices.Count * 3;

        if (stars >= max) return TrophyTier.Gold;
        if (stars >= max * SilverShare - 0.001f) return TrophyTier.Silver;

        return TrophyTier.Bronze;
    }

    // The first unlocked mission not yet won (the last one when all are won)
    public static int NextMissionIndex(MissionProgress progress, MissionCatalog catalog)
    {
        for (int i = 0; i < catalog.Count; i++)
        {
            if (progress.GetBestStars(i) == 0)
            {
                return i;
            }
        }

        return catalog.Count - 1;
    }

    // Position along the path: missions in play order, with each stage's
    // trophy in the slot right after its last mission.
    public static int MissionSlot(MissionCatalog catalog, int index)
    {
        return index + (int)catalog.Get(index).stage;
    }

    public static int TrophySlot(MissionCatalog catalog, MissionStage stage)
    {
        var indices = catalog.IndicesInStage(stage);

        return MissionSlot(catalog, indices[indices.Count - 1]) + 1;
    }

    public static NodeState GetNodeState(MissionProgress progress, int index)
    {
        if (progress.GetBestStars(index) > 0)
        {
            return NodeState.Done;
        }

        return progress.IsMissionUnlocked(index) ? NodeState.Next : NodeState.Locked;
    }

    public static string StarCountText(int stars, int max)
    {
        return stars + "/" + max;
    }
}
