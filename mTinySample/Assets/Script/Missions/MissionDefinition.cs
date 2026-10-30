using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionStage
{
    Easy,
    Medium,
    Hard
}

[Flags]
public enum BlockGroups
{
    None = 0,
    Movement = 1,
    Multiplier = 2,
    Emotion = 4,
    All = Movement | Multiplier | Emotion
}

public enum CityTileKind
{
    Hospital,
    ChildrenCrossing,
    PoliceStation
}

[Serializable]
public class CityTilePlacement
{
    public Vector2Int cell;
    public CityTileKind kind;
}

// One mission's board and rules. Cells use grid coordinates:
// column 0 is the left edge, row 0 is the bottom edge.
[Serializable]
public class MissionDefinition
{
    public string id;
    public MissionStage stage;
    public string title;

    [TextArea]
    public string instruction;

    [Header("Requirements")]
    public bool requireMovement = true;
    public bool requireMultiplier;
    public bool requireEmotion;

    [Header("Blocks shown to the player")]
    public BlockGroups visibleBlocks = BlockGroups.All;

    [Header("Board")]
    public Vector2Int start;
    public List<Vector2Int> path = new List<Vector2Int>();
    public Vector2Int goal;
    public List<CityTilePlacement> cityTiles = new List<CityTilePlacement>();

    [Tooltip("Shortest intended program, e.g. \"F 3x R F 2x\". Checked by tests.")]
    public string intendedSolution;

    public bool IsWalkable(Vector2Int cell)
    {
        if (cell == start || cell == goal || path.Contains(cell))
        {
            return true;
        }

        foreach (CityTilePlacement tile in cityTiles)
        {
            if (tile != null && tile.cell == cell)
            {
                return true;
            }
        }

        return false;
    }
}
