// What the finish screen should do after a mission is won.
public struct MissionWinOutcome
{
    public bool showNext;
    public int nextIndex;
    public bool celebrate;
    public int totalStars;

    // Records the win (best stars are kept) and decides what to show.
    public static MissionWinOutcome Decide(MissionProgress progress, MissionCatalog catalog, int index, int stars)
    {
        bool celebrate = progress.RecordWin(index, stars);
        int next = catalog.NextIndex(index);

        return new MissionWinOutcome
        {
            showNext = next != -1,
            nextIndex = next,
            celebrate = celebrate,
            totalStars = progress.GetTotalStars()
        };
    }
}
