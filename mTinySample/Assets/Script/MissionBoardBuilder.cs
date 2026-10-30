using TMPro;
using UnityEngine;

// Builds the chosen mission's board when the Mission scene loads:
// tiles, robot start point, requirements, instruction and visible blocks.
// Runs before GameManager so the robot spawns on the new start tile.
[DefaultExecutionOrder(-100)]
public class MissionBoardBuilder : MonoBehaviour
{
    // =========================================================
    // MISSION DATA
    // =========================================================

    [Header("Mission Data")]
    public MissionCatalog catalog;

    // =========================================================
    // TILE PREFABS
    // =========================================================

    [Header("Tile Prefabs")]
    public GameObject pathTilePrefab;
    public GameObject startTilePrefab;
    public GameObject goalTilePrefab;
    public GameObject hospitalTilePrefab;
    public GameObject crossingTilePrefab;
    public GameObject policeTilePrefab;

    // =========================================================
    // BOARD LAYOUT
    // =========================================================

    [Header("Board Layout")]
    public Transform boardParent;
    public Vector2 boardCenter = new Vector2(-1f, -1f);
    public float cellSize = 1.8f;

    [Tooltip("Tiles sit at the same depth as the original hand-placed platforms.")]
    public float tileZ = 90f;

    // =========================================================
    // SCENE REFERENCES
    // =========================================================

    [Header("Scene References")]
    public GameManager gameManager;
    public MissionManager missionManager;
    public TMP_Text instructionText;

    [Tooltip("Mission title on the instruction card.")]
    public TMP_Text titleText;

    // =========================================================
    // INSTRUCTION HINTS
    // =========================================================
    // Pictures of the blocks a mission needs, for children who
    // can't read the instruction yet.
    // =========================================================

    [Header("Instruction Hints")]
    public GameObject movementHint;
    public GameObject multiplierHint;
    public GameObject emotionHint;

    // =========================================================
    // BLOCK BUTTONS
    // =========================================================

    [Header("Block Buttons")]
    public GameObject[] movementBlocks;
    public GameObject[] multiplierBlocks;
    public GameObject[] emotionBlocks;

    public MissionDefinition Current { get; private set; }
    public int CurrentIndex { get; private set; }


    private void Awake()
    {
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogError("MissionBoardBuilder: Mission catalog is missing or empty!");
            return;
        }

        Build(MissionSelection.Resolve(MissionSelection.SelectedIndex, catalog.Count));
    }


    // =========================================================
    // BUILD
    // =========================================================
    // Public so editor tools can preview a mission without play mode.
    // =========================================================

    public Transform Build(int index)
    {
        CurrentIndex = index;
        Current = catalog.Get(index);

        ClearBoard();

        Transform startTile = SpawnTile(startTilePrefab, Current.start, "Start");

        foreach (Vector2Int cell in Current.path)
        {
            SpawnTile(GetPathPrefab(cell), cell, "Path " + cell);
        }

        SpawnTile(goalTilePrefab, Current.goal, "Goal");

        if (gameManager != null)
        {
            gameManager.startPoint = startTile;
        }

        if (missionManager != null)
        {
            missionManager.requireMovement = Current.requireMovement;
            missionManager.requireMultiplier = Current.requireMultiplier;
            missionManager.requireEmotion = Current.requireEmotion;
            missionManager.requireFinish = true;
        }

        if (instructionText != null)
        {
            instructionText.text = Current.instruction;
        }

        if (titleText != null)
        {
            titleText.text = Current.title;
        }

        SetHint(movementHint, Current.requireMovement);
        SetHint(multiplierHint, Current.requireMultiplier);
        SetHint(emotionHint, Current.requireEmotion);

        SetBlocksVisible(movementBlocks, (Current.visibleBlocks & BlockGroups.Movement) != 0);
        SetBlocksVisible(multiplierBlocks, (Current.visibleBlocks & BlockGroups.Multiplier) != 0);
        SetBlocksVisible(emotionBlocks, (Current.visibleBlocks & BlockGroups.Emotion) != 0);

        Debug.Log($"Mission {Current.id} ({Current.title}) built.");

        return startTile;
    }


    // =========================================================
    // CELL TO WORLD
    // =========================================================

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector2 position = MissionBoardLayout.CellToWorld(Current, cell, boardCenter, cellSize);

        return new Vector3(position.x, position.y, tileZ);
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private GameObject GetPathPrefab(Vector2Int cell)
    {
        foreach (CityTilePlacement tile in Current.cityTiles)
        {
            if (tile == null || tile.cell != cell)
            {
                continue;
            }

            switch (tile.kind)
            {
                case CityTileKind.Hospital:
                    return hospitalTilePrefab;

                case CityTileKind.ChildrenCrossing:
                    return crossingTilePrefab;

                case CityTileKind.PoliceStation:
                    return policeTilePrefab;
            }
        }

        return pathTilePrefab;
    }

    private Transform SpawnTile(GameObject prefab, Vector2Int cell, string tileName)
    {
        GameObject tile = Instantiate(prefab, CellToWorld(cell), Quaternion.identity, boardParent);

        tile.name = tileName;

        return tile.transform;
    }

    private void ClearBoard()
    {
        if (boardParent == null)
        {
            return;
        }

        for (int i = boardParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = boardParent.GetChild(i).gameObject;

            // Detach first so the new board never overlaps the old one
            child.transform.SetParent(null);

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private static void SetHint(GameObject hint, bool visible)
    {
        if (hint != null)
        {
            hint.SetActive(visible);
        }
    }

    private static void SetBlocksVisible(GameObject[] blocks, bool visible)
    {
        if (blocks == null)
        {
            return;
        }

        foreach (GameObject block in blocks)
        {
            if (block != null)
            {
                block.SetActive(visible);
            }
        }
    }
}
