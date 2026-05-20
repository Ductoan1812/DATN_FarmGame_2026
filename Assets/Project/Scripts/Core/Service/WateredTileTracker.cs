using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Track ô đã tưới nước trong ngày hiện tại.
/// Dùng Tilemap riêng (tmWatered) làm source of truth.
/// Reset toàn bộ mỗi đầu ngày mới (ClearAllTiles).
/// 
/// Ai ghi: WateringCanRuntime, SprinklerRuntime, WeatherSystem (khi mưa).
/// Ai đọc: StageRuntime (check grow), WateringCanRuntime (check đã tưới chưa), AIAssistantService.
/// </summary>
public class WateredTileTracker
{
    private readonly Tilemap _tmWatered;
    private readonly TileBase _wateredTile;
    private readonly Tilemap _tmGround;
    private readonly TileData _tileData;

    public WateredTileTracker(Tilemap tmWatered, Tilemap tmGround, TileData tileData)
    {
        _tmWatered = tmWatered;
        _tmGround = tmGround;
        _tileData = tileData;
        _wateredTile = tileData.wateredTile;
    }

    // ══════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════

    /// <summary>Đánh dấu ô đã tưới (đặt tile lên tmWatered).</summary>
    public void SetWatered(Vector2Int cell)
    {
        if (_tmWatered == null || _wateredTile == null) return;
        _tmWatered.SetTile(new Vector3Int(cell.x, cell.y, 0), _wateredTile);
    }

    /// <summary>Check ô đã tưới chưa (có tile trên tmWatered?).</summary>
    public bool IsWatered(Vector2Int cell)
    {
        if (_tmWatered == null) return false;
        return _tmWatered.GetTile(new Vector3Int(cell.x, cell.y, 0)) != null;
    }

    /// <summary>Reset toàn bộ — xóa hết tiles trên tmWatered. Gọi mỗi đầu ngày mới.</summary>
    public void ResetAll()
    {
        if (_tmWatered == null) return;
        _tmWatered.ClearAllTiles();
    }

    /// <summary>Tưới tất cả ô đã cày (plowed) — gọi khi trời mưa.</summary>
    public void WaterAllPlowedCells()
    {
        if (_tmWatered == null || _tmGround == null || _tileData == null) return;

        var bounds = _tmGround.cellBounds;
        var plowedTile = _tileData.plowedTile;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                var groundTile = _tmGround.GetTile(pos);
                if (groundTile == plowedTile)
                {
                    _tmWatered.SetTile(pos, _wateredTile);
                }
            }
        }

        Debug.Log("[WateredTileTracker] Mưa đã tưới tất cả ô đã cày.");
    }

    /// <summary>Đếm số ô đã tưới (cho AI Assistant / UI).</summary>
    public int GetWateredCount()
    {
        if (_tmWatered == null) return 0;

        int count = 0;
        var bounds = _tmWatered.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                if (_tmWatered.GetTile(new Vector3Int(x, y, 0)) != null)
                    count++;
        return count;
    }
}
