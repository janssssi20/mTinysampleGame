using System.Collections.Generic;
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
    // CITY ROAD TILES
    // =========================================================
    // Missions with city tiles are built entirely from the city
    // puzzle set, so every piece interlocks and the road follows
    // the route. (Park and city pieces differ in size and knobs.)
    // =========================================================

    [Header("City Road Tiles")]
    public GameObject cityRoadEastWest;
    public GameObject cityRoadNorthSouth;
    public GameObject cityRoadNorthEast;
    public GameObject cityRoadEastSouth;
    public GameObject cityRoadSouthWest;
    public GameObject cityRoadNorthWest;
    public GameObject cityRoadJunction;
    public GameObject cityGoalTilePrefab;

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
    // FIT THE TOP VIEW
    // =========================================================
    // Big boards (up to 7 x 4 tiles) must sit between the code bar
    // at the top and the block palette on the right. The board is
    // centred in that free area, and the camera zooms out a little
    // only when a board would not fit.
    // =========================================================

    [Header("Fit The Top View")]
    public bool fitCamera = true;
    [Tooltip("Share of the screen height taken by the header and code bar.")]
    public float headerShare = 0.2f;
    [Tooltip("Share of the screen width taken by the block palette.")]
    public float paletteShare = 0.28f;
    [Tooltip("Room kept around the board, in world units.")]
    public float fitMargin = 1.1f;
    public float minCameraSize = 5f;

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
    public GameObject soundHint;

    // =========================================================
    // BLOCK BUTTONS
    // =========================================================

    [Header("Block Buttons")]
    public GameObject[] movementBlocks;
    public GameObject[] multiplierBlocks;
    public GameObject[] emotionBlocks;
    public GameObject[] soundBlocks;

    // =========================================================
    // MARKED TILES (Stages 3-5)
    // =========================================================
    // A badge on a road tile: mTiny must show a feeling (smiley)
    // or play music (note) while standing there.
    // =========================================================

    [Header("Marked Tiles")]
    public Sprite cueFeelingSprite;
    public Sprite cueMusicSprite;
    public Sprite cueDoneSprite;
    public Sprite cueGlowSprite;
    [Tooltip("Badge size, as a share of a tile.")]
    public float cueSize = 0.5f;
    [Tooltip("Badge offset from the tile centre (towards the top-right corner), as a share of a tile.")]
    public Vector2 cueOffset = new Vector2(0.24f, 0.24f);

    [Tooltip("Move the marked tiles to new random road tiles each time a level starts.")]
    public bool randomizeCues = true;

    public static MissionBoardBuilder Instance { get; private set; }

    // This play's marked tiles, and the shortest program for them
    public List<MissionCue> ActiveCues { get; private set; } = new List<MissionCue>();
    public int BestBlocks { get; private set; }

    private readonly List<CueMarker> cueMarkers = new List<CueMarker>();

    public MissionDefinition Current { get; private set; }
    public int CurrentIndex { get; private set; }


    private void Awake()
    {
        Instance = this;

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
        FitCamera();

        // New random places for the marked tiles each time the level starts
        ActiveCues = randomizeCues
            ? MissionSolver.RandomCues(Current, new System.Random())
            : new List<MissionCue>(Current.cues);
        BestBlocks = ActiveCues.Count > 0
            ? MissionSimulator.CountBlocks(MissionSolver.Solve(Current, ActiveCues))
            : MissionSimulator.CountBlocks(Current.intendedSolution);

        bool city = IsCityBoard;

        Transform startTile = SpawnTile(
            city ? RoadFor(Current.start) : startTilePrefab, Current.start, "Start");

        foreach (Vector2Int cell in Current.path)
        {
            SpawnTile(GetPathPrefab(cell, city), cell, "Path " + cell);
        }

        SpawnTile(city ? cityGoalTilePrefab : goalTilePrefab, Current.goal, "Goal");
        SpawnCueMarkers();

        if (gameManager != null)
        {
            gameManager.startPoint = startTile;
        }

        if (missionManager != null)
        {
            missionManager.requireMovement = Current.requireMovement;
            missionManager.requireMultiplier = Current.requireMultiplier;
            missionManager.requireEmotion = Current.requireEmotion;
            missionManager.requireSound = Current.requireSound;
            missionManager.requireFinish = true;
        }

        if (instructionText != null)
        {
            instructionText.text = Current.instruction;
        }

        // Stars count blocks against this mission's best answer
        CommandManager commands = FindFirstObjectByType<CommandManager>(FindObjectsInactive.Include);

        if (commands != null)
        {
            commands.SetBestBlocks(BestBlocks);
        }

        if (titleText != null)
        {
            titleText.text = Current.title;
        }

        SetHint(movementHint, Current.requireMovement);
        SetHint(multiplierHint, Current.requireMultiplier);
        SetHint(emotionHint, Current.requireEmotion);
        SetHint(soundHint, Current.requireSound);

        SetBlocksVisible(movementBlocks, (Current.visibleBlocks & BlockGroups.Movement) != 0);
        SetBlocksVisible(multiplierBlocks, (Current.visibleBlocks & BlockGroups.Multiplier) != 0);
        SetBlocksVisible(emotionBlocks, (Current.visibleBlocks & BlockGroups.Emotion) != 0);
        SetBlocksVisible(soundBlocks, (Current.visibleBlocks & BlockGroups.Sound) != 0);

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

    public bool IsCityBoard => Current != null && Current.cityTiles.Count > 0;

    private void SpawnCueMarkers()
    {
        cueMarkers.Clear();
        if (ActiveCues == null) return;

        foreach (MissionCue cue in ActiveCues)
        {
            if (cue == null) continue;

            GameObject marker = new GameObject("Cue " + cue.kind + " " + cue.cell);
            marker.transform.SetParent(boardParent, false);
            Vector3 centre = CellToWorld(cue.cell);
            marker.transform.position = centre + new Vector3(cueOffset.x * cellSize, cueOffset.y * cellSize, -0.5f);
            marker.transform.localScale = Vector3.one * cueSize * cellSize;

            SpriteRenderer glow = new GameObject("glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(marker.transform, false);
            glow.transform.localScale = Vector3.one * 1.7f;
            glow.sprite = cueGlowSprite;
            glow.sortingOrder = 1;

            SpriteRenderer badge = new GameObject("badge").AddComponent<SpriteRenderer>();
            badge.transform.SetParent(marker.transform, false);
            badge.sortingOrder = 2;

            CueMarker cm = marker.AddComponent<CueMarker>();
            cm.badge = badge;
            cm.glow = glow;
            cm.kind = cue.kind;
            cm.cell = cue.cell;
            cm.waitingSprite = cue.kind == CueKind.Music ? cueMusicSprite : cueFeelingSprite;
            cm.doneSprite = cueDoneSprite;
            cm.ResetCue();
            cueMarkers.Add(cm);
        }
    }

    // A new run starts: every marked tile waits again
    public void ResetCues()
    {
        foreach (CueMarker marker in cueMarkers)
        {
            if (marker != null) marker.ResetCue();
        }
    }

    // mTiny showed a feeling or played music here: tick the marked tile
    // it stands on, if that tile asks for this kind of block
    public void CuePlayed(CueKind kind, Vector3 robotPosition)
    {
        foreach (CueMarker marker in cueMarkers)
        {
            if (marker == null || marker.Done || marker.kind != kind) continue;

            Vector3 tile = CellToWorld(marker.cell);
            Vector2 offset = new Vector2(robotPosition.x - tile.x, robotPosition.y - tile.y);

            if (offset.magnitude < cellSize * 0.5f)
            {
                marker.Complete();
            }
        }
    }

    // The first marked tile not yet done (false when all are done)
    public bool MissingCue(out CueKind kind)
    {
        foreach (CueMarker marker in cueMarkers)
        {
            if (marker != null && !marker.Done)
            {
                kind = marker.kind;
                return true;
            }
        }

        kind = CueKind.Feeling;
        return false;
    }

    private void FitCamera()
    {
        Camera cam = Camera.main;
        if (!fitCamera || cam == null || !cam.orthographic)
        {
            return;
        }

        Vector2Int min = Current.start, max = Current.start;
        foreach (Vector2Int cell in MissionRoute.Cells(Current))
        {
            min = Vector2Int.Min(min, cell);
            max = Vector2Int.Max(max, cell);
        }

        float width = (max.x - min.x + 1) * cellSize + fitMargin;
        float height = (max.y - min.y + 1) * cellSize + fitMargin;
        float aspect = cam.aspect > 0f ? cam.aspect : 16f / 9f;

        float size = Mathf.Max(minCameraSize,
            height / (2f * (1f - headerShare)),
            width / (2f * (1f - paletteShare) * aspect));
        cam.orthographicSize = size;

        // Middle of the free area left of the palette and below the header
        Vector3 c = cam.transform.position;
        boardCenter = new Vector2(c.x - paletteShare * size * aspect, c.y - headerShare * size);
    }

    private GameObject RoadFor(Vector2Int cell)
    {
        switch (MissionRoute.ShapeFor(MissionRoute.Openings(Current, cell)))
        {
            case RoadShape.EastWest: return cityRoadEastWest;
            case RoadShape.NorthSouth: return cityRoadNorthSouth;
            case RoadShape.NorthEast: return cityRoadNorthEast;
            case RoadShape.EastSouth: return cityRoadEastSouth;
            case RoadShape.SouthWest: return cityRoadSouthWest;
            case RoadShape.NorthWest: return cityRoadNorthWest;
            default: return cityRoadJunction;
        }
    }

    private GameObject GetPathPrefab(Vector2Int cell, bool city)
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

        return city ? RoadFor(cell) : pathTilePrefab;
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
