using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Data/Tiles/Tile Data", fileName = "TileData")]
public class TileData : ScriptableObject
{
    [Tooltip("Tile đất thường, không thể trồng trọt.")]
    public TileBase landTile;
    [Tooltip("Tile đã cày, có thể trồng trọt.")]
    public TileBase plowedTile;
    [Tooltip("Tile đã tưới nước, có thể trồng trọt.")]
    public TileBase wateredTile;
    [Tooltip("Tile cỏ, không thể trồng trọt.")]
    public TileBase grassTile;

    [Header("══════ Tile Tags ══════")]
    public TilePlacementEntry[] tileTagEntries;

    /// <summary>Lấy tags của 1 tile. Không tìm thấy → None.</summary>
    public PlacementTag GetTags(TileBase tile)
    {
        if (tile == null || tileTagEntries == null) return PlacementTag.None;
        foreach (var entry in tileTagEntries)
            if (entry.tile == tile) return entry.tags;
        return PlacementTag.None;
    }
}

[System.Serializable]
public struct TilePlacementEntry
{
    public TileBase tile;
    public PlacementTag tags;
}
