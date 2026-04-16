using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Pure data store — không có logic nghiệp vụ.
/// Layer 1: positions  — tra entity theo GUID  O(1)
/// Layer 2: spatial    — tra entity theo ô     O(1), hỗ trợ multi-layer
/// </summary>
public class WorldEntityRegistry
{
    private readonly Dictionary<string, EntityPosition> _positions = new();
    private readonly Dictionary<Vector2Int, TileEntry>  _spatial   = new();

    // ── Layer 1: Entity lookup ────────────────────────────────────────────────

    public void RegisterEntity(EntityPosition ep)
    {
        if (ep == null || string.IsNullOrEmpty(ep.idRuntime)) return;
        _positions[ep.idRuntime] = ep;
    }

    public void UnregisterEntity(string idRuntime) => _positions.Remove(idRuntime);

    /// <summary>Đổi idRuntime của entity đã đăng ký (dùng sau Split).</summary>
    public void UpdateEntityId(string oldId, string newId)
    {
        if (!_positions.TryGetValue(oldId, out var ep)) return;
        ep.idRuntime = newId;
        _positions.Remove(oldId);
        _positions[newId] = ep;

        // Cập nhật spatial map
        if (ep.occupiedCells == null) return;
        foreach (var cell in ep.occupiedCells)
            if (_spatial.TryGetValue(cell, out var entry))
                entry.UpdateEntityId(ep.layer, oldId, newId);
    }

    public EntityPosition GetEntity(string idRuntime)
    {
        _positions.TryGetValue(idRuntime, out var ep);
        return ep;
    }

    public IEnumerable<EntityPosition> GetAllEntities() => _positions.Values;

    // ── Layer 2: Spatial (multi-layer) ────────────────────────────────────────

    /// <summary>Thêm entity vào spatial map theo layer.</summary>
    public bool AddToSpatial(EntityPosition ep, EntityLayer layer)
    {
        if (ep?.occupiedCells == null) return false;

        foreach (var cell in ep.occupiedCells)
        {
            var entry = EnsureTile(cell);
            if (!entry.TryAdd(layer, ep.idRuntime))
            {
                // Rollback nếu 1 cell fail
                RemoveFromSpatial(ep, layer);
                return false;
            }
        }
        return true;
    }

    /// <summary>Xóa entity khỏi spatial map theo layer.</summary>
    public void RemoveFromSpatial(EntityPosition ep, EntityLayer layer)
    {
        if (ep?.occupiedCells == null) return;
        foreach (var cell in ep.occupiedCells)
            if (_spatial.TryGetValue(cell, out var entry))
                entry.Remove(layer, ep.idRuntime);
    }

    public TileEntry GetTileEntry(Vector2Int cell)
    {
        _spatial.TryGetValue(cell, out var e);
        return e;
    }

    public IEnumerable<string> GetEntitiesAt(Vector2Int cell)
    {
        var entry = GetTileEntry(cell);
        return entry?.entityIds ?? (IEnumerable<string>)Array.Empty<string>();
    }

    public bool HasAnyEntityAt(Vector2Int cell)
    {
        var e = GetTileEntry(cell);
        return e != null && e.layers.Count > 0;
    }

    public bool HasEntityAtLayer(Vector2Int cell, EntityLayer layer)
    {
        var e = GetTileEntry(cell);
        return e != null && e.HasEntityAt(layer);
    }

    // ── Ground ────────────────────────────────────────────────────────────────

    public void SetGround(Vector2Int cell, TileBase tile) => EnsureTile(cell).groundType = tile;
    public TileBase GetGround(Vector2Int cell) => GetTileEntry(cell)?.groundType;

    // ── Bulk (Save/Load) ──────────────────────────────────────────────────────

    public void ReplaceAll(Dictionary<string, EntityPosition> positions, Dictionary<Vector2Int, TileEntry> spatial)
    {
        _positions.Clear();
        _spatial.Clear();
        if (positions != null) foreach (var kv in positions) _positions[kv.Key] = kv.Value;
        if (spatial   != null) foreach (var kv in spatial)   _spatial[kv.Key]   = kv.Value;
    }

    public Dictionary<string, EntityPosition> SnapshotPositions()
    {
        var d = new Dictionary<string, EntityPosition>(_positions.Count);
        foreach (var kv in _positions) d[kv.Key] = kv.Value;
        return d;
    }

    public Dictionary<Vector2Int, TileEntry> SnapshotSpatial()
    {
        var d = new Dictionary<Vector2Int, TileEntry>(_spatial.Count);
        foreach (var kv in _spatial) d[kv.Key] = kv.Value;
        return d;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private TileEntry EnsureTile(Vector2Int cell)
    {
        if (!_spatial.TryGetValue(cell, out var e))
            _spatial[cell] = e = new TileEntry();
        return e;
    }
}
