using System;
using System.Collections.Generic;
using UnityEngine;

// Five stages of ten missions. Each adds a block family:
// 1 movement, 2 + multipliers, 3 + feelings, 4 + sounds, 5 everything on bigger maps.
public enum MissionStage
{
    Stage1,
    Stage2,
    Stage3,
    Stage4,
    Stage5
}

// Names and taglines shown on the map and the mission card
public static class MissionStages
{
    public const int Count = 5;

    private static readonly string[] Names =
    {
        "Let's Start!", "Little Learner", "Rising Star", "Super Learner", "Champion!"
    };

    private static readonly string[] Taglines =
    {
        "For beginners", "Learning the basics", "Getting better", "Ready for bigger challenges", "The ultimate challenge"
    };

    public static string Name(MissionStage stage) => Names[(int)stage];

    public static string Tagline(MissionStage stage) => Taglines[(int)stage];

    // "Stage 1"
    public static string Number(MissionStage stage) => "Stage " + ((int)stage + 1);
}

[Flags]
public enum BlockGroups
{
    None = 0,
    Movement = 1,
    Multiplier = 2,
    Emotion = 4,
    Sound = 8,
    All = Movement | Multiplier | Emotion | Sound
}

public enum CityTileKind
{
    Hospital,
    ChildrenCrossing,
    PoliceStation
}

// A marked tile: mTiny must show a feeling (or play music) while
// standing on it, or the level isn't finished
public enum CueKind
{
    Feeling,
    Music
}

[Serializable]
public class MissionCue
{
    public Vector2Int cell;
    public CueKind kind;
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
    public bool requireSound;

    [Header("Blocks shown to the player")]
    public BlockGroups visibleBlocks = BlockGroups.All;

    [Header("Board")]
    public Vector2Int start;
    public List<Vector2Int> path = new List<Vector2Int>();
    public Vector2Int goal;
    public List<CityTilePlacement> cityTiles = new List<CityTilePlacement>();

    [Tooltip("Marked tiles where a feeling or music block must be played.")]
    public List<MissionCue> cues = new List<MissionCue>();

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
