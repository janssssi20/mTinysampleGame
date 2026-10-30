using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum Side
{
    None = 0,
    N = 1,
    E = 2,
    S = 4,
    W = 8
}

public enum RoadShape
{
    EastWest,
    NorthSouth,
    NorthEast,
    EastSouth,
    SouthWest,
    NorthWest,
    Junction
}

// The route a mission's board follows: start, then the path in order,
// then the goal. Road tiles use it to know which sides the road leaves by.
public static class MissionRoute
{
    public static List<Vector2Int> Cells(MissionDefinition mission)
    {
        List<Vector2Int> cells = new List<Vector2Int> { mission.start };
        cells.AddRange(mission.path);
        cells.Add(mission.goal);
        return cells;
    }

    // True when every step of the route moves to a neighbouring cell
    public static bool IsConnected(MissionDefinition mission)
    {
        List<Vector2Int> cells = Cells(mission);

        for (int i = 1; i < cells.Count; i++)
        {
            Vector2Int step = cells[i] - cells[i - 1];

            if (Mathf.Abs(step.x) + Mathf.Abs(step.y) != 1)
            {
                return false;
            }
        }

        return true;
    }

    // Sides of this cell the route leaves by (towards the cells before
    // and after it on the route)
    public static Side Openings(MissionDefinition mission, Vector2Int cell)
    {
        List<Vector2Int> cells = Cells(mission);
        int index = cells.IndexOf(cell);

        if (index < 0)
        {
            return Side.None;
        }

        Side sides = Side.None;

        if (index > 0)
        {
            sides |= Toward(cell, cells[index - 1]);
        }

        if (index < cells.Count - 1)
        {
            sides |= Toward(cell, cells[index + 1]);
        }

        return sides;
    }

    // Road piece for a cell: ends use the straight road they continue
    public static RoadShape ShapeFor(Side sides)
    {
        switch (sides)
        {
            case Side.E | Side.W:
            case Side.E:
            case Side.W:
                return RoadShape.EastWest;

            case Side.N | Side.S:
            case Side.N:
            case Side.S:
                return RoadShape.NorthSouth;

            case Side.N | Side.E: return RoadShape.NorthEast;
            case Side.E | Side.S: return RoadShape.EastSouth;
            case Side.S | Side.W: return RoadShape.SouthWest;
            case Side.N | Side.W: return RoadShape.NorthWest;

            default: return RoadShape.Junction;
        }
    }

    private static Side Toward(Vector2Int from, Vector2Int to)
    {
        Vector2Int d = to - from;

        if (d == Vector2Int.up) return Side.N;
        if (d == Vector2Int.down) return Side.S;
        if (d == Vector2Int.right) return Side.E;
        if (d == Vector2Int.left) return Side.W;

        return Side.None;
    }
}
