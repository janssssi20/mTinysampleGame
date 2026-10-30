using UnityEngine;

public enum TileType
{
    None,
    Hospital,
    ChildrenCrossing,
    PoliceStation,
    TheftIncident,
    BumpedIncident,
    Construction
}

public class SpecialTile : MonoBehaviour
{
    [Header("Tile Settings")]
    public TileType tileType = TileType.None;
}