// The mission chosen on the stage map. Survives scene loads because it
// is static; the Mission scene reads it when it builds the board.
public static class MissionSelection
{
    public static int SelectedIndex { get; set; }

    // Falls back to the first mission when the selection is stale or the
    // Mission scene was opened directly.
    public static int Resolve(int selected, int missionCount)
    {
        return selected >= 0 && selected < missionCount ? selected : 0;
    }
}
