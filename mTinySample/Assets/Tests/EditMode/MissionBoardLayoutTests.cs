using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MissionBoardLayoutTests
{
    [Test]
    public void Resolve_OutOfRange_FallsBackToFirstMission()
    {
        Assert.AreEqual(0, MissionSelection.Resolve(99, 15));
        Assert.AreEqual(0, MissionSelection.Resolve(-1, 15));
    }

    [Test]
    public void Resolve_InRange_KeepsSelection()
    {
        Assert.AreEqual(3, MissionSelection.Resolve(3, 15));
    }

    [Test]
    public void CellToWorld_CentersBoardOnCenterPoint()
    {
        MissionDefinition mission = new MissionDefinition
        {
            start = new Vector2Int(0, 0),
            path = new List<Vector2Int> { new Vector2Int(1, 0) },
            goal = new Vector2Int(2, 0)
        };

        Vector2 center = new Vector2(-0.5f, -1f);

        Vector2 middle = MissionBoardLayout.CellToWorld(mission, new Vector2Int(1, 0), center, 1.8f);
        Vector2 left = MissionBoardLayout.CellToWorld(mission, new Vector2Int(0, 0), center, 1.8f);

        Assert.AreEqual(-0.5f, middle.x, 0.0001f);
        Assert.AreEqual(-1f, middle.y, 0.0001f);
        Assert.AreEqual(-2.3f, left.x, 0.0001f);
    }

    [Test]
    public void CellToWorld_StepIsCellSize()
    {
        MissionDefinition mission = new MissionDefinition
        {
            start = new Vector2Int(0, 0),
            path = new List<Vector2Int>(),
            goal = new Vector2Int(0, 3)
        };

        Vector2 a = MissionBoardLayout.CellToWorld(mission, new Vector2Int(0, 1), Vector2.zero, 1.8f);
        Vector2 b = MissionBoardLayout.CellToWorld(mission, new Vector2Int(0, 2), Vector2.zero, 1.8f);

        Assert.AreEqual(1.8f, b.y - a.y, 0.0001f);
    }
}
