using System.Collections.Generic;
using NUnit.Framework;

public class StarRulesTests
{
    [Test]
    public void BestAnswerOrShorter_ThreeStars()
    {
        Assert.AreEqual(3, StarRules.ForBlocks(5, 5));
        Assert.AreEqual(3, StarRules.ForBlocks(4, 5));
    }

    [Test]
    public void AFewExtraBlocks_TwoStars_ThenOne()
    {
        Assert.AreEqual(2, StarRules.ForBlocks(6, 5));
        Assert.AreEqual(2, StarRules.ForBlocks(5 + StarRules.TwoStarSlack, 5));
        Assert.AreEqual(1, StarRules.ForBlocks(5 + StarRules.TwoStarSlack + 1, 5));
    }

    [Test]
    public void NoKnownBest_FullStars()
    {
        Assert.AreEqual(3, StarRules.ForBlocks(20, 0));
    }

    [Test]
    public void CountBlocks_MatchesTheSimulatorsCount()
    {
        // "F 3x R F" = 4 blocks, like MissionSimulator.CountBlocks
        List<Command> program = new List<Command>
        {
            new Command(CommandType.StartFlag),
            new Command(CommandType.Forward, 3),
            new Command(CommandType.TurnRight),
            new Command(CommandType.Forward)
        };

        Assert.AreEqual(MissionSimulator.CountBlocks("F 3x R F"), ProgramEdits.CountBlocks(program));
    }

    [Test]
    public void IconSpan_SkipsMultiplierIcons()
    {
        List<Command> program = new List<Command>
        {
            new Command(CommandType.StartFlag),
            new Command(CommandType.Forward, 3),
            new Command(CommandType.TurnRight)
        };

        int first, count;

        ProgramEdits.IconSpan(program, 1, out first, out count);
        Assert.AreEqual(1, first);
        Assert.AreEqual(2, count);

        ProgramEdits.IconSpan(program, 2, out first, out count);
        Assert.AreEqual(3, first);
        Assert.AreEqual(1, count);
    }
}
