using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MissionSimulatorTests
{
    // Straight line from (2,0) up to the goal at (2,3)
    private static MissionDefinition StraightUp()
    {
        return new MissionDefinition
        {
            id = "T1",
            requireMovement = true,
            start = new Vector2Int(2, 0),
            path = new List<Vector2Int> { new Vector2Int(2, 1), new Vector2Int(2, 2) },
            goal = new Vector2Int(2, 3)
        };
    }

    [Test]
    public void Run_StraightUp_ReachesGoal()
    {
        Assert.AreEqual(SimResult.ReachedGoal, MissionSimulator.Run(StraightUp(), "F F F"));
    }

    [Test]
    public void Run_LeftTurnIsCounterClockwise()
    {
        MissionDefinition mission = new MissionDefinition
        {
            id = "T2",
            requireMovement = true,
            start = new Vector2Int(4, 0),
            path = new List<Vector2Int> { new Vector2Int(4, 1), new Vector2Int(3, 1), new Vector2Int(2, 1) },
            goal = new Vector2Int(1, 1)
        };

        Assert.AreEqual(SimResult.ReachedGoal, MissionSimulator.Run(mission, "F L F F F"));
    }

    [Test]
    public void Run_MultiplierRepeatsPreviousBlock()
    {
        Assert.AreEqual(SimResult.ReachedGoal, MissionSimulator.Run(StraightUp(), "F 3x"));
    }

    [Test]
    public void Run_StepOffPath_LeftPath()
    {
        Assert.AreEqual(SimResult.LeftPath, MissionSimulator.Run(StraightUp(), "R F"));
    }

    [Test]
    public void Run_ProgramTooShort_EndedShort()
    {
        Assert.AreEqual(SimResult.EndedShort, MissionSimulator.Run(StraightUp(), "F"));
    }

    [Test]
    public void Run_RequiresMultiplierButNoneUsed_MissingMultiplier()
    {
        MissionDefinition mission = StraightUp();
        mission.requireMultiplier = true;

        Assert.AreEqual(SimResult.MissingMultiplier, MissionSimulator.Run(mission, "F F F"));
    }

    [Test]
    public void Run_RequiresEmotion_HappyCounts()
    {
        MissionDefinition mission = StraightUp();
        mission.requireEmotion = true;

        Assert.AreEqual(SimResult.ReachedGoal, MissionSimulator.Run(mission, "Happy F 3x"));
    }

    [Test]
    public void Run_UnknownToken_BadToken()
    {
        Assert.AreEqual(SimResult.BadToken, MissionSimulator.Run(StraightUp(), "F Jump"));
    }

    [Test]
    public void CountBlocks_CountsEveryToken()
    {
        Assert.AreEqual(4, MissionSimulator.CountBlocks("Happy F 3x R"));
    }
}
