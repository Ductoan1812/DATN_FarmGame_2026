using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tracks one-time clearable scene zones by spawnGroupId.
/// Clearable zones are persistent marker groups; regenerating resources/enemies are ignored.
/// </summary>
public class ClearZoneTracker
{
    private readonly Dictionary<string, HashSet<string>> activeEntitiesByZone = new();
    private readonly Dictionary<string, string> zoneByEntity = new();
    private readonly Dictionary<string, HashSet<Vector2Int>> cellsByZone = new();
    private readonly HashSet<string> clearedZones = new();
    private WorldEntityService worldService;
    private TileData tileData;

    public ClearZoneTracker(WorldEntityService worldService, TileData tileData)
    {
        this.worldService = worldService;
        this.tileData = tileData;
    }

    public int ClearedCount => clearedZones.Count;

    public void RebindWorldService(WorldEntityService service, TileData data = null)
    {
        worldService = service;
        if (data != null)
            tileData = data;

        ClearActiveEntities();
        ApplyAllClearedZones();
    }

    public void ClearActiveEntities()
    {
        activeEntitiesByZone.Clear();
        zoneByEntity.Clear();
    }

    public bool IsZoneCleared(string zoneId)
    {
        return !string.IsNullOrWhiteSpace(zoneId) && clearedZones.Contains(zoneId.Trim());
    }

    public void RegisterSpawn(string entityId, string zoneId, Vector2Int[] occupiedCells, SceneEntitySavePolicy savePolicy)
    {
        if (savePolicy != SceneEntitySavePolicy.Persistent || !IsClearableZoneId(zoneId))
            return;
        if (string.IsNullOrWhiteSpace(entityId) || string.IsNullOrWhiteSpace(zoneId))
            return;

        zoneId = zoneId.Trim();
        if (IsZoneCleared(zoneId))
            return;

        if (!activeEntitiesByZone.TryGetValue(zoneId, out var entities))
            activeEntitiesByZone[zoneId] = entities = new HashSet<string>();
        entities.Add(entityId);
        zoneByEntity[entityId] = zoneId;

        if (occupiedCells == null || occupiedCells.Length == 0)
            return;

        if (!cellsByZone.TryGetValue(zoneId, out var cells))
            cellsByZone[zoneId] = cells = new HashSet<Vector2Int>();
        foreach (var cell in occupiedCells)
            cells.Add(cell);
    }

    public void NotifyEntityRemoved(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
            return;
        if (!zoneByEntity.TryGetValue(entityId, out var zoneId))
            return;

        zoneByEntity.Remove(entityId);
        if (!activeEntitiesByZone.TryGetValue(zoneId, out var entities))
            return;

        entities.Remove(entityId);
        if (entities.Count > 0)
            return;

        activeEntitiesByZone.Remove(zoneId);
        MarkZoneCleared(zoneId);
    }

    public void MarkZoneCleared(string zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            return;

        zoneId = zoneId.Trim();
        if (!clearedZones.Add(zoneId))
            return;

        ApplyZoneUnlock(zoneId);
        Debug.Log($"[ClearZoneTracker] Zone cleared: {zoneId}");
    }

    public List<ClearZoneDto> ExportClearZones()
    {
        var result = new List<ClearZoneDto>(clearedZones.Count);
        foreach (var zoneId in clearedZones)
        {
            var dto = new ClearZoneDto { zoneId = zoneId };
            if (cellsByZone.TryGetValue(zoneId, out var cells))
            {
                dto.cells = new List<CellDto>(cells.Count);
                foreach (var cell in cells)
                    dto.cells.Add(new CellDto { x = cell.x, y = cell.y });
            }
            result.Add(dto);
        }
        return result;
    }

    public void ImportClearZones(List<ClearZoneDto> zones)
    {
        clearedZones.Clear();
        cellsByZone.Clear();
        ClearActiveEntities();

        if (zones != null)
        {
            foreach (var zone in zones)
            {
                if (zone == null || string.IsNullOrWhiteSpace(zone.zoneId))
                    continue;

                string zoneId = zone.zoneId.Trim();
                clearedZones.Add(zoneId);
                if (zone.cells == null)
                    continue;

                if (!cellsByZone.TryGetValue(zoneId, out var cells))
                    cellsByZone[zoneId] = cells = new HashSet<Vector2Int>();
                foreach (var cell in zone.cells)
                    cells.Add(new Vector2Int(cell.x, cell.y));
            }
        }

        ApplyAllClearedZones();
    }

    private void ApplyAllClearedZones()
    {
        foreach (var zoneId in clearedZones)
            ApplyZoneUnlock(zoneId);
    }

    private void ApplyZoneUnlock(string zoneId)
    {
        if (worldService == null || !cellsByZone.TryGetValue(zoneId, out var cells))
            return;

        TileBase unlockTile = tileData != null
            ? (tileData.grassTile != null ? tileData.grassTile : tileData.landTile)
            : null;
        if (unlockTile == null)
            return;

        foreach (var cell in cells)
            worldService.SetGround(cell, unlockTile);
    }

    private static bool IsClearableZoneId(string zoneId)
    {
        return !string.IsNullOrWhiteSpace(zoneId)
            && zoneId.IndexOf("clear", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
