using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks soil quality per tile cell.
/// Used by fertilizer and future growth tuning.
/// </summary>
public class SoilQualityTracker
{
    private readonly Dictionary<Vector2Int, int> _qualityByCell = new();

    public int GetQuality(Vector2Int cell)
    {
        return _qualityByCell.TryGetValue(cell, out int quality) ? Mathf.Max(0, quality) : 0;
    }

    public void SetQuality(Vector2Int cell, int quality)
    {
        _qualityByCell[cell] = Mathf.Max(0, quality);
    }

    public int IncreaseQuality(Vector2Int cell, int amount = 1)
    {
        if (amount <= 0) return GetQuality(cell);

        int next = Mathf.Max(0, GetQuality(cell) + amount);
        _qualityByCell[cell] = next;
        return next;
    }

    public void Clear()
    {
        _qualityByCell.Clear();
    }

    public List<SoilCellDto> ExportSoilCells()
    {
        var result = new List<SoilCellDto>(_qualityByCell.Count);
        foreach (var kvp in _qualityByCell)
        {
            if (kvp.Value <= 0) continue;
            result.Add(new SoilCellDto { x = kvp.Key.x, y = kvp.Key.y, quality = kvp.Value });
        }

        return result;
    }

    public void ImportSoilCells(List<SoilCellDto> cells)
    {
        _qualityByCell.Clear();
        if (cells == null) return;

        foreach (var cell in cells)
        {
            if (cell.quality <= 0) continue;
            _qualityByCell[new Vector2Int(cell.x, cell.y)] = cell.quality;
        }
    }
}
