using System.Collections.Generic;
using System.Text;
using UnityEngine;

// What a square holds: a plain road (which picks its own straight,
// corner or junction piece) or one of the special city pieces.
// Saved as numbers: add new pieces at the end.
public enum MapPiece
{
    Road,
    Hospital,
    Police,
    Crossing,
    Thief
}

// A map a child builds in Explore: a set of road cells on a small grid.
// Each road joins every neighbouring road, so the right puzzle piece
// (straight, corner or junction) is picked automatically. Special pieces
// are straight roads with a picture, laid along their neighbours.
public sealed class MapLayout
{
    public readonly int width;
    public readonly int height;
    public readonly Vector2Int start;

    private readonly HashSet<Vector2Int> roads = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, MapPiece> specials = new Dictionary<Vector2Int, MapPiece>();

    public MapLayout(int width, int height, Vector2Int start)
    {
        this.width = width;
        this.height = height;
        this.start = start;
        roads.Add(start);
    }

    public IEnumerable<Vector2Int> Roads
    {
        get { return roads; }
    }

    public int Count
    {
        get { return roads.Count; }
    }

    public bool Contains(Vector2Int cell)
    {
        return roads.Contains(cell);
    }

    public bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    }

    // Tap a cell: adds a road, or takes it away. The start tile stays.
    // Returns true when the map changed.
    public bool Toggle(Vector2Int cell)
    {
        if (!InBounds(cell) || cell == start)
        {
            return false;
        }

        if (!roads.Remove(cell))
        {
            roads.Add(cell);
        }

        specials.Remove(cell);
        return true;
    }

    // Lay a piece with the picked tool. The road tool adds or takes away
    // a road. A special piece goes on an empty square or a road; tapping
    // the same special again turns it back into a plain road. The start
    // square always stays a plain road.
    public bool Place(Vector2Int cell, MapPiece piece)
    {
        if (piece == MapPiece.Road)
        {
            return Toggle(cell);
        }

        if (!InBounds(cell) || cell == start)
        {
            return false;
        }

        MapPiece current;

        if (specials.TryGetValue(cell, out current) && current == piece)
        {
            specials.Remove(cell);
            return true;
        }

        roads.Add(cell);
        specials[cell] = piece;
        return true;
    }

    public MapPiece PieceAt(Vector2Int cell)
    {
        MapPiece piece;
        return specials.TryGetValue(cell, out piece) ? piece : MapPiece.Road;
    }

    // Special pictures have their road running east-west. When the roads
    // around one only come from north or south, the piece is turned.
    public bool RunsNorthSouth(Vector2Int cell)
    {
        Side sides = SidesFor(cell);
        return (sides & (Side.E | Side.W)) == 0 && (sides & (Side.N | Side.S)) != 0;
    }

    public void Clear()
    {
        roads.Clear();
        specials.Clear();
        roads.Add(start);
    }

    // Sides where this road meets another road
    public Side SidesFor(Vector2Int cell)
    {
        Side sides = Side.None;

        if (roads.Contains(cell + Vector2Int.up)) sides |= Side.N;
        if (roads.Contains(cell + Vector2Int.right)) sides |= Side.E;
        if (roads.Contains(cell + Vector2Int.down)) sides |= Side.S;
        if (roads.Contains(cell + Vector2Int.left)) sides |= Side.W;

        return sides;
    }

    public RoadShape ShapeFor(Vector2Int cell)
    {
        return MissionRoute.ShapeFor(SidesFor(cell));
    }

    // "x,y;x,y:2;..." for saving between visits (":n" = special piece n)
    public string Save()
    {
        StringBuilder text = new StringBuilder();

        foreach (Vector2Int cell in roads)
        {
            if (text.Length > 0) text.Append(';');
            text.Append(cell.x).Append(',').Append(cell.y);

            MapPiece piece = PieceAt(cell);
            if (piece != MapPiece.Road) text.Append(':').Append((int)piece);
        }

        return text.ToString();
    }

    // Loads a saved map, skipping anything that no longer fits the grid
    public void Load(string saved)
    {
        Clear();

        if (string.IsNullOrEmpty(saved))
        {
            return;
        }

        foreach (string entry in saved.Split(';'))
        {
            string[] parts = entry.Split(':');
            string[] xy = parts[0].Split(',');
            int x, y;

            if (xy.Length == 2 && int.TryParse(xy[0], out x) && int.TryParse(xy[1], out y))
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!InBounds(cell))
                {
                    continue;
                }

                roads.Add(cell);

                int kind;
                if (parts.Length == 2 && int.TryParse(parts[1], out kind) && cell != start &&
                    System.Enum.IsDefined(typeof(MapPiece), kind) && kind != (int)MapPiece.Road)
                {
                    specials[cell] = (MapPiece)kind;
                }
            }
        }
    }
}
