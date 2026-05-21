using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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
    private readonly HashSet<Vector2Int> _wateredCells = new();

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
        _wateredCells.Add(cell);
        if (_tmWatered == null || _wateredTile == null) return;
        _tmWatered.SetTile(new Vector3Int(cell.x, cell.y, 0), _wateredTile);
    }

    /// <summary>Check ô đã tưới chưa (có tile trên tmWatered?).</summary>
    public bool IsWatered(Vector2Int cell)
    {
        if (_wateredCells.Contains(cell)) return true;
        if (_tmWatered == null) return false;
        return _tmWatered.GetTile(new Vector3Int(cell.x, cell.y, 0)) != null;
    }

    /// <summary>Reset toàn bộ — xóa hết tiles trên tmWatered. Gọi mỗi đầu ngày mới.</summary>
    public void ResetAll()
    {
        _wateredCells.Clear();
        if (_tmWatered == null) return;
        _tmWatered.ClearAllTiles();
    }

    /// <summary>Tưới tất cả ô đã cày (plowed) — gọi khi trời mưa.</summary>
    public void WaterAllPlowedCells()
    {
        if (_tmGround == null || _tileData == null) return;

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
                    var cell = new Vector2Int(x, y);
                    _wateredCells.Add(cell);
                    if (_tmWatered != null && _wateredTile != null)
                        _tmWatered.SetTile(pos, _wateredTile);
                }
            }
        }

        Debug.Log("[WateredTileTracker] Mưa đã tưới tất cả ô đã cày.");
    }

    /// <summary>Export tất cả ô đang được tưới (dùng cho save).</summary>
    public System.Collections.Generic.List<WateredCellDto> ExportWateredCells()
    {
        var result = new System.Collections.Generic.List<WateredCellDto>();
        foreach (var cell in _wateredCells)
            result.Add(new WateredCellDto { x = cell.x, y = cell.y });
        return result;
    }

    /// <summary>Import danh sách ô đã tưới từ save (clear trước, rồi restore).</summary>
    public void ImportWateredCells(System.Collections.Generic.List<WateredCellDto> cells)
    {
        _wateredCells.Clear();
        if (cells == null) return;
        foreach (var c in cells)
        {
            var cell = new Vector2Int(c.x, c.y);
            _wateredCells.Add(cell);
            if (_tmWatered != null && _wateredTile != null)
                _tmWatered.SetTile(new Vector3Int(c.x, c.y, 0), _wateredTile);
        }
    }

    /// <summary>Đếm số ô đã tưới (cho AI Assistant / UI).</summary>
    public int GetWateredCount()
    {
        return _wateredCells.Count;
    }
}
