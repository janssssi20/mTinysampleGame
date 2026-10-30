using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MissionRouteTests
{
    // Up from (0,0) to (0,2), then right to the goal at (2,2)
    private static MissionDefinition Corner()
    {
        return new MissionDefinition
        {
            start = new Vector2Int(0, 0),
            path = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2) },
            goal = new Vector2Int(2, 2)
        };
    }

    [Test]
    public void Openings_Start_FacesTheNextCell()
    {
        Assert.AreEqual(Side.N, MissionRoute.Openings(Corner(), new Vector2Int(0, 0)));
    }

    [Test]
    public void Openings_StraightStretch_OpensBothWays()
    {
        Assert.AreEqual(Side.N | Side.S, MissionRoute.Openings(Corner(), new Vector2Int(0, 1)));
        Assert.AreEqual(Side.E | Side.W, MissionRoute.Openings(Corner(), new Vector2Int(1, 2)));
    }

    [Test]
    public void Openings_Turn_OpensToBothNeighbours()
    {
        Assert.AreEqual(Side.S | Side.E, MissionRoute.Openings(Corner(), new Vector2Int(0, 2)));
    }

    [Test]
    public void Openings_Goal_FacesThePreviousCell()
    {
        Assert.AreEqual(Side.W, MissionRoute.Openings(Corner(), new Vector2Int(2, 2)));
    }

    [Test]
    public void ShapeFor_StraightsAndEnds_UseStraightRoads()
    {
        Assert.AreEqual(RoadShape.EastWest, MissionRoute.ShapeFor(Side.E | Side.W));
        Assert.AreEqual(RoadShape.EastWest, MissionRoute.ShapeFor(Side.W));
        Assert.AreEqual(RoadShape.NorthSouth, MissionRoute.ShapeFor(Side.N | Side.S));
        Assert.AreEqual(RoadShape.NorthSouth, MissionRoute.ShapeFor(Side.N));
    }

    [Test]
    public void ShapeFor_Turns_UseMatchingCorners()
    {
        Assert.AreEqual(RoadShape.NorthEast, MissionRoute.ShapeFor(Side.N | Side.E));
        Assert.AreEqual(RoadShape.EastSouth, MissionRoute.ShapeFor(Side.E | Side.S));
        Assert.AreEqual(RoadShape.SouthWest, MissionRoute.ShapeFor(Side.S | Side.W));
        Assert.AreEqual(RoadShape.NorthWest, MissionRoute.ShapeFor(Side.N | Side.W));
    }

    [Test]
    public void IsConnected_GapInRoute_IsFalse()
    {
        MissionDefinition broken = Corner();
        broken.path.RemoveAt(1);

        Assert.IsTrue(MissionRoute.IsConnected(Corner()));
        Assert.IsFalse(MissionRoute.IsConnected(broken));
    }
}
