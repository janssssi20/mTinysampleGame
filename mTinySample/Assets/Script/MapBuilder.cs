using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// "Build my map" in Explore: children tap grid squares to lay road
// pieces, which join up by themselves, then code mTiny across their own
// map. The city board is hidden while their map is in use.
public class MapBuilder : MonoBehaviour
{
    private const string SaveKey = "mtiny.mymap";

    [Header("Boards")]
    [Tooltip("The hand-made city board's pieces: tiles, walls and goals (not its panels).")]
    public GameObject[] cityBoard;
    public Transform tilesParent;

    [Header("Road Pieces (city set, same as the missions)")]
    public GameObject roadEastWest;
    public GameObject roadNorthSouth;
    public GameObject roadNorthEast;
    public GameObject roadEastSouth;
    public GameObject roadSouthWest;
    public GameObject roadNorthWest;
    public GameObject roadJunction;

    [Header("Special Pieces")]
    [Tooltip("A special city tile (with its trigger) to copy for each special piece.")]
    public GameObject specialBase;

    [Tooltip("Picture for each special piece, in MapPiece order after Road.")]
    public Sprite[] specialSprites;

    [Tooltip("What mTiny does on each special piece, in the same order.")]
    public TileType[] specialTypes;

    [Header("Tool Buttons")]
    [Tooltip("Frames of the piece buttons, in MapPiece order (Road first).")]
    public UnityEngine.UI.Image[] toolFrames;
    public Color toolSelected = new Color(0.85f, 0.49f, 0.16f);
    public Color toolIdle = new Color(1f, 1f, 1f, 0.6f);

    // The piece a tap lays down
    public MapPiece SelectedPiece { get; private set; }

    [Header("Grid")]
    public int width = 7;
    public int height = 6;
    public float cellSize = 1.8f;
    [Tooltip("World position of the bottom-left square's centre.")]
    public Vector3 origin;
    public Vector2Int startCell = new Vector2Int(0, 0);

    [Tooltip("Faint square drawn on empty cells while building.")]
    public Sprite markerSprite;
    public Color markerColor = new Color(1f, 1f, 1f, 0.45f);
    public Color startMarkerColor = new Color(0.36f, 0.69f, 0.25f, 0.6f);

    public CommandButtonManager buttonManager;

    public bool IsEditing { get; private set; }
    public bool UsingMyMap { get; private set; }

    private MapLayout layout;
    private Transform markersParent;
    private Vector3 cityStartPos;
    private Quaternion cityStartRot;
    private bool cityStartKnown;


    private void Awake()
    {
        EnsureLayout();
        SelectPiece((int)MapPiece.Road);
    }

    // Also used by editor previews, where Awake doesn't run
    private void EnsureLayout()
    {
        if (layout != null)
        {
            return;
        }

        layout = new MapLayout(width, height, startCell);
        layout.Load(PlayerPrefs.GetString(SaveKey, ""));
    }

    private void Update()
    {
        if (!IsEditing)
        {
            return;
        }

        Vector2 screen;

        if (!TappedBoard(out screen))
        {
            return;
        }

        // Where the tap meets the mat, in any camera view (top or tilted)
        Ray ray = Camera.main.ScreenPointToRay(screen);
        float distance;

        if (!new Plane(Vector3.back, origin).Raycast(ray, out distance))
        {
            return;
        }

        Vector3 world = ray.GetPoint(distance);
        Vector2Int cell = new Vector2Int(
            Mathf.RoundToInt((world.x - origin.x) / cellSize),
            Mathf.RoundToInt((world.y - origin.y) / cellSize));

        ToggleCell(cell);
    }

    // Public so a tap can also be simulated by tools and tests
    // Piece buttons pass their MapPiece number
    public void SelectPiece(int piece)
    {
        SelectedPiece = (MapPiece)piece;

        if (toolFrames == null)
        {
            return;
        }

        for (int i = 0; i < toolFrames.Length; i++)
        {
            if (toolFrames[i] != null) toolFrames[i].color = i == piece ? toolSelected : toolIdle;
        }
    }

    public bool ToggleCell(Vector2Int cell)
    {
        EnsureLayout();

        if (!layout.Place(cell, SelectedPiece))
        {
            return false;
        }

        AudioManager.Play(layout.Contains(cell) ? Sfx.Step : Sfx.Turn);
        Rebuild();
        PlayerPrefs.SetString(SaveKey, layout.Save());
        PlayerPrefs.Save();
        return true;
    }

    // A tap or click that landed on the board, not on a button. (The
    // screen's full-size background image is UI too, so "over any UI"
    // would swallow every tap: only real buttons count.)
    private static bool TappedBoard(out Vector2 screen)
    {
        screen = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase != TouchPhase.Began)
            {
                return false;
            }

            screen = touch.position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            screen = Input.mousePosition;
        }
        else
        {
            return false;
        }

        return !OverButton(screen);
    }

    private static readonly List<RaycastResult> hits = new List<RaycastResult>();

    private static bool OverButton(Vector2 screen)
    {
        EventSystem events = EventSystem.current;

        if (events == null)
        {
            return false;
        }

        hits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = screen }, hits);

        foreach (RaycastResult hit in hits)
        {
            if (hit.gameObject.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }


    // =========================================================
    // MODES
    // =========================================================

    // Switch to the child's map and show the grid to build on
    public void BeginEditing()
    {
        IsEditing = true;
        UseMyMap();
    }

    public void EndEditing()
    {
        IsEditing = false;
        Rebuild();
    }

    public void UseMyMap()
    {
        RememberCityStart();

        UsingMyMap = true;
        ShowCity(false);
        Rebuild();

        if (buttonManager != null)
        {
            buttonManager.SetStart(CellToWorld(startCell), Quaternion.identity);
        }
    }

    public void UseCity()
    {
        IsEditing = false;
        UsingMyMap = false;
        ShowCity(true);
        Rebuild();

        if (buttonManager != null && cityStartKnown)
        {
            buttonManager.SetStart(cityStartPos, cityStartRot);
        }
    }

    public void ClearMap()
    {
        EnsureLayout();
        layout.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        Rebuild();
    }

    private void ShowCity(bool visible)
    {
        foreach (GameObject part in cityBoard)
        {
            if (part != null) part.SetActive(visible);
        }
    }

    private void RememberCityStart()
    {
        if (!cityStartKnown && buttonManager != null)
        {
            cityStartPos = buttonManager.startPos;
            cityStartRot = buttonManager.startRot;
            cityStartKnown = true;
        }
    }


    // =========================================================
    // BUILDING
    // =========================================================

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return origin + new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }

    private void Rebuild()
    {
        EnsureLayout();
        Clear(tilesParent);

        if (markersParent == null)
        {
            markersParent = new GameObject("buildMarkers").transform;
            markersParent.SetParent(transform, false);
        }

        Clear(markersParent);

        if (!UsingMyMap)
        {
            return;
        }

        foreach (Vector2Int cell in layout.Roads)
        {
            MapPiece piece = layout.PieceAt(cell);
            GameObject tile = piece == MapPiece.Road || specialBase == null
                ? Instantiate(PieceFor(layout.ShapeFor(cell)), CellToWorld(cell), Quaternion.identity, tilesParent)
                : SpawnSpecial(piece, cell);
            tile.name = "My " + piece + " " + cell;
        }

        if (!IsEditing || markerSprite == null)
        {
            return;
        }

        // Faint squares show where roads can go; the start one is green
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (layout.Contains(cell) && cell != startCell)
                {
                    continue;
                }

                GameObject marker = new GameObject("Marker " + cell, typeof(SpriteRenderer));
                marker.transform.SetParent(markersParent, false);
                marker.transform.position = CellToWorld(cell) + new Vector3(0f, 0f, -1f);

                SpriteRenderer sprite = marker.GetComponent<SpriteRenderer>();
                sprite.sprite = markerSprite;
                sprite.color = cell == startCell ? startMarkerColor : markerColor;
                sprite.sortingOrder = cell == startCell ? 1 : 0;

                Vector2 size = markerSprite.bounds.size;
                float fit = cellSize * 0.86f;
                marker.transform.localScale = new Vector3(fit / size.x, fit / size.y, 1f);
            }
        }
    }

    // A copy of the special city tile with this piece's picture and
    // reaction, turned when its road runs north-south
    private GameObject SpawnSpecial(MapPiece piece, Vector2Int cell)
    {
        Quaternion turn = layout.RunsNorthSouth(cell) ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
        GameObject tile = Instantiate(specialBase, CellToWorld(cell), turn, tilesParent);
        int index = (int)piece - 1;

        SpriteRenderer sprite = tile.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && specialSprites != null && index < specialSprites.Length && specialSprites[index] != null)
        {
            sprite.sprite = specialSprites[index];
        }

        SpecialTile special = tile.GetComponentInChildren<SpecialTile>();
        if (special != null && specialTypes != null && index < specialTypes.Length)
        {
            special.tileType = specialTypes[index];
        }

        return tile;
    }

    private GameObject PieceFor(RoadShape shape)
    {
        switch (shape)
        {
            case RoadShape.EastWest: return roadEastWest;
            case RoadShape.NorthSouth: return roadNorthSouth;
            case RoadShape.NorthEast: return roadNorthEast;
            case RoadShape.EastSouth: return roadEastSouth;
            case RoadShape.SouthWest: return roadSouthWest;
            case RoadShape.NorthWest: return roadNorthWest;
            default: return roadJunction;
        }
    }

    private static void Clear(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
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
}
