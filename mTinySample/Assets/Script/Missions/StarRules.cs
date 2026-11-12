// Stars for a finished mission, earned by planning rather than speed:
// a slow but careful child still gets full marks.
public static class StarRules
{
    // A few extra blocks over the best answer still earn two stars
    public const int TwoStarSlack = 3;

    public static int ForBlocks(int blocksUsed, int bestBlocks)
    {
        if (bestBlocks <= 0 || blocksUsed <= bestBlocks)
        {
            return 3;
        }

        return blocksUsed <= bestBlocks + TwoStarSlack ? 2 : 1;
    }
}
