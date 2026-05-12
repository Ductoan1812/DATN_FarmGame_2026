using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Quản lý trạng thái tile trên các Tilemap.
/// - Khi boot: quét tất cả tilemap → lưu baseline (snapshot gốc).
/// - Runtime: ghi nhận thay đổi vào dirty map.
/// - Save: chỉ lưu dirty (những ô thay đổi so với baseline).
/// - Load: áp dirty lên tilemap gốc.
/// </summary>
public class TileRegistry
{
    // ── Tilemap references ────────────────────────────────
    private readonly Dictionary<string, Tilemap> _tilemaps = new();

    // ── Baseline: trạng thái gốc từ Editor ────────────────
    // Key = (tilemapName, cell), Value = TileBase gốc (null nếu ô trống)
    private readonly Dictionary<(string, Vector2Int), TileBase> _baseline = new();

    // ── Dirty: chỉ những ô thay đổi so với baseline ──────
    // Key = (tilemapName, cell), Value = TileBase mới (null = đã xóa tile)
    private readonly Dictionary<(string, Vector2Int), TileBase> _dirty = new();

    // ══════════════════════════════════════════════════════
    //  INIT
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Đăng ký 1 tilemap theo tên. Gọi trước ScanBaseline.
    /// </summary>
    public void RegisterTilemap(string name, Tilemap tilemap)
    {
        if (tilemap == null) return;
        _tilemaps[name] = tilemap;
    }

    /// <summary>
    /// Quét tất cả tilemap đã đăng ký → lưu baseline.
    /// Gọi 1 lần khi boot, SAU khi RegisterTilemap.
    /// </summary>
    public void ScanBaseline()
    {
        _baseline.Clear();

        foreach (var kv in _tilemaps)
        {
            var name = kv.Key;
            var tm = kv.Value;
            if (tm == null) continue;

            tm.CompressBounds();
            var bounds = tm.cellBounds;

            foreach (var pos in bounds.allPositionsWithin)
            {
                var tile = tm.GetTile(pos);
                if (tile == null) continue;

                var cell = new Vector2Int(pos.x, pos.y);
                _baseline[(name, cell)] = tile;
            }
        }

        Debug.Log($"[TileRegistry] Scanned baseline: {_baseline.Count} tiles across {_tilemaps.Count} tilemaps.");
    }

    // ══════════════════════════════════════════════════════
    //  RUNTIME — Đọc / Ghi tile
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Lấy tile hiện tại tại cell trên tilemap chỉ định.
    /// Ưu tiên dirty → baseline → null.
    /// </summary>
    public TileBase GetTile(string tilemapName, Vector2Int cell)
    {
        var key = (tilemapName, cell);
        if (_dirty.TryGetValue(key, out var dirtyTile)) return dirtyTile;
        if (_baseline.TryGetValue(key, out var baseTile)) return baseTile;
        return null;
    }

    /// <summary>
    /// Set tile tại cell. Tự động ghi vào dirty nếu khác baseline.
    /// Nếu giống baseline → xóa khỏi dirty (revert).
    /// </summary>
    public void SetTile(string tilemapName, Vector2Int cell, TileBase newTile)
    {
        var key = (tilemapName, cell);

        // Cập nhật tilemap thực tế
        if (_tilemaps.TryGetValue(tilemapName, out var tm) && tm != null)
            tm.SetTile(new Vector3Int(cell.x, cell.y, 0), newTile);

        // So sánh với baseline
        _baseline.TryGetValue(key, out var baseTile);

        if (newTile == baseTile)
        {
            // Giống baseline → không cần lưu dirty
            _dirty.Remove(key);
        }
        else
        {
            _dirty[key] = newTile;
        }
    }

    /// <summary>
    /// Kiểm tra ô có tile hay không (trên tilemap chỉ định).
    /// </summary>
    public bool HasTile(string tilemapName, Vector2Int cell)
    {
        return GetTile(tilemapName, cell) != null;
    }

    /// <summary>
    /// Lấy Tilemap reference theo tên.
    /// </summary>
    public Tilemap GetTilemap(string name)
    {
        _tilemaps.TryGetValue(name, out var tm);
        return tm;
    }

    // ══════════════════════════════════════════════════════
    //  SAVE — Chỉ lưu dirty
    // ══════════════════════════════════════════════════════

    public List<TileChangeSave> GetDirtySnapshot()
    {
        var list = new List<TileChangeSave>(_dirty.Count);
        foreach (var kv in _dirty)
        {
            list.Add(new TileChangeSave
            {
                tilemapName = kv.Key.Item1,
                cellX       = kv.Key.Item2.x,
                cellY       = kv.Key.Item2.y,
                tileName    = kv.Value != null ? kv.Value.name : string.Empty
            });
        }
        return list;
    }

    // ══════════════════════════════════════════════════════
    //  LOAD — Áp dirty lên tilemap gốc
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Áp danh sách thay đổi lên tilemap. Gọi SAU ScanBaseline.
    /// </summary>
    public void ApplyDirty(List<TileChangeSave> changes, System.Func<string, TileBase> resolveTile)
    {
        if (changes == null) return;

        _dirty.Clear();

        foreach (var c in changes)
        {
            var cell = new Vector2Int(c.cellX, c.cellY);
            TileBase tile = string.IsNullOrEmpty(c.tileName) ? null : resolveTile(c.tileName);

            var key = (c.tilemapName, cell);
            _dirty[key] = tile;

            // Áp lên tilemap thực tế
            if (_tilemaps.TryGetValue(c.tilemapName, out var tm) && tm != null)
                tm.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
        }

        Debug.Log($"[TileRegistry] Applied {changes.Count} dirty tile changes.");
    }

    /// <summary>Xóa toàn bộ dirty (dùng khi new game).</summary>
    public void ClearDirty() => _dirty.Clear();
}

// ── Save DTO ──────────────────────────────────────────────

[System.Serializable]
public class TileChangeSave
{
    public string tilemapName;
    public int cellX, cellY;
    public string tileName; // empty = tile bị xóa
}
