using UnityEngine;

// Grid-to-world placement for a mission board. The board's bounding box
// (start, path and goal) is centred on a fixed point on screen.
public static class MissionBoardLayout
{
    public static Vector2 CellToWorld(MissionDefinition mission, Vector2Int cell, Vector2 center, float cellSize)
    {
        Vector2Int min = mission.start;
        Vector2Int max = mission.start;

        Include(mission.goal, ref min, ref max);

        foreach (Vector2Int pathCell in mission.path)
        {
            Include(pathCell, ref min, ref max);
        }

        Vector2 middle = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);

        return center + (new Vector2(cell.x, cell.y) - middle) * cellSize;
    }

    private static void Include(Vector2Int cell, ref Vector2Int min, ref Vector2Int max)
    {
        min = Vector2Int.Min(min, cell);
        max = Vector2Int.Max(max, cell);
    }
}
