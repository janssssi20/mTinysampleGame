public enum NodeState
{
    Done,
    Next,
    Locked
}

// Display rules for the stage cards and mission trail.
public static class StageMapModel
{
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
