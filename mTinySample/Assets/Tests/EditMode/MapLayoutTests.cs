using NUnit.Framework;
using UnityEngine;

public class MapLayoutTests
{
    private static MapLayout NewMap()
    {
        return new MapLayout(6, 5, new Vector2Int(0, 0));
    }

    [Test]
    public void StartsWithOnlyTheStartRoad_WhichCantBeRemoved()
    {
        MapLayout map = NewMap();

        Assert.AreEqual(1, map.Count);
        Assert.IsFalse(map.Toggle(new Vector2Int(0, 0)));
        Assert.IsTrue(map.Contains(new Vector2Int(0, 0)));
    }

    [Test]
    public void Toggle_AddsThenRemoves_AndIgnoresCellsOffTheGrid()
    {
        MapLayout map = NewMap();

        Assert.IsTrue(map.Toggle(new Vector2Int(1, 0)));
        Assert.IsTrue(map.Contains(new Vector2Int(1, 0)));
        Assert.IsTrue(map.Toggle(new Vector2Int(1, 0)));
        Assert.IsFalse(map.Contains(new Vector2Int(1, 0)));

        Assert.IsFalse(map.Toggle(new Vector2Int(6, 0)));
        Assert.IsFalse(map.Toggle(new Vector2Int(-1, 2)));
    }

    [Test]
    public void Pieces_FollowTheNeighbouringRoads()
    {
        MapLayout map = NewMap();
        map.Toggle(new Vector2Int(1, 0));
        map.Toggle(new Vector2Int(2, 0));
        map.Toggle(new Vector2Int(2, 1));

        Assert.AreEqual(RoadShape.EastWest, map.ShapeFor(new Vector2Int(0, 0)));
        Assert.AreEqual(RoadShape.EastWest, map.ShapeFor(new Vector2Int(1, 0)));
        Assert.AreEqual(RoadShape.NorthWest, map.ShapeFor(new Vector2Int(2, 0)));
        Assert.AreEqual(RoadShape.NorthSouth, map.ShapeFor(new Vector2Int(2, 1)));

        map.Toggle(new Vector2Int(3, 0));
        Assert.AreEqual(RoadShape.Junction, map.ShapeFor(new Vector2Int(2, 0)));
    }

    [Test]
    public void SaveAndLoad_KeepsTheMap()
    {
        MapLayout map = NewMap();
        map.Toggle(new Vector2Int(1, 0));
        map.Toggle(new Vector2Int(1, 1));

        MapLayout copy = NewMap();
        copy.Load(map.Save());

        Assert.AreEqual(3, copy.Count);
        Assert.IsTrue(copy.Contains(new Vector2Int(1, 1)));
    }

    [Test]
    public void Load_SkipsBrokenOrOffGridCells()
    {
        MapLayout map = NewMap();
        map.Load("1,0;oops;9,9;2,");

        Assert.AreEqual(2, map.Count);
        Assert.IsTrue(map.Contains(new Vector2Int(1, 0)));
    }

    [Test]
    public void SpecialPiece_GoesOnEmptyOrRoad_AndTappingAgainMakesItARoad()
    {
        MapLayout map = NewMap();
        Vector2Int cell = new Vector2Int(1, 0);

        Assert.IsTrue(map.Place(cell, MapPiece.Police));
        Assert.IsTrue(map.Contains(cell));
        Assert.AreEqual(MapPiece.Police, map.PieceAt(cell));

        Assert.IsTrue(map.Place(cell, MapPiece.Hospital));
        Assert.AreEqual(MapPiece.Hospital, map.PieceAt(cell));

        Assert.IsTrue(map.Place(cell, MapPiece.Hospital));
        Assert.AreEqual(MapPiece.Road, map.PieceAt(cell));
        Assert.IsTrue(map.Contains(cell));
    }

    [Test]
    public void SpecialPiece_NeverOnTheStartSquare()
    {
        MapLayout map = NewMap();

        Assert.IsFalse(map.Place(new Vector2Int(0, 0), MapPiece.Thief));
        Assert.AreEqual(MapPiece.Road, map.PieceAt(new Vector2Int(0, 0)));
    }

    [Test]
    public void RoadTool_TakesAwayASpecialPieceToo()
    {
        MapLayout map = NewMap();
        map.Place(new Vector2Int(1, 0), MapPiece.Crossing);

        map.Place(new Vector2Int(1, 0), MapPiece.Road);

        Assert.IsFalse(map.Contains(new Vector2Int(1, 0)));
        Assert.AreEqual(MapPiece.Road, map.PieceAt(new Vector2Int(1, 0)));
    }

    [Test]
    public void SpecialPiece_TurnsWhenRoadsComeFromNorthOrSouth()
    {
        MapLayout map = NewMap();
        map.Toggle(new Vector2Int(0, 1));
        map.Place(new Vector2Int(0, 2), MapPiece.Police);
        map.Toggle(new Vector2Int(0, 3));

        Assert.IsTrue(map.RunsNorthSouth(new Vector2Int(0, 2)));

        map.Toggle(new Vector2Int(1, 2));
        Assert.IsFalse(map.RunsNorthSouth(new Vector2Int(0, 2)));
    }

    [Test]
    public void SaveAndLoad_KeepsSpecialPieces_AndOldSavesStillLoad()
    {
        MapLayout map = NewMap();
        map.Place(new Vector2Int(1, 0), MapPiece.Thief);
        map.Toggle(new Vector2Int(2, 0));

        MapLayout copy = NewMap();
        copy.Load(map.Save());
        Assert.AreEqual(MapPiece.Thief, copy.PieceAt(new Vector2Int(1, 0)));
        Assert.AreEqual(MapPiece.Road, copy.PieceAt(new Vector2Int(2, 0)));

        MapLayout old = NewMap();
        old.Load("1,0;2,0");
        Assert.AreEqual(3, old.Count);

        MapLayout odd = NewMap();
        odd.Load("1,0:99;0,0:2");
        Assert.AreEqual(MapPiece.Road, odd.PieceAt(new Vector2Int(1, 0)));
        Assert.AreEqual(MapPiece.Road, odd.PieceAt(new Vector2Int(0, 0)));
    }
}
