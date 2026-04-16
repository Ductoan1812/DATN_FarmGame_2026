using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

// ─── Save DTOs ────────────────────────────────────────────────────────────────

[Serializable]
public class EntityPositionSave
{
    public string idRuntime;
    public string idPrefab;
    public float posX, posY;
    public int[] cellsX, cellsY;
    public int layer; // EntityLayer as int
}

[Serializable]
public class TileEntrySave
{
    public int cellX, cellY;
    public string groundName;
    public LayerEntitySave[] layerEntities;
}

[Serializable]
public class LayerEntitySave
{
    public int layer;
    public string entityId;
}

[Serializable]
public class WorldSaveContainer
{
    public EntityPositionSave[] entities;
    public TileEntrySave[]      tiles;
}

// ─── Spawn Result ─────────────────────────────────────────────────────────────

public enum SpawnResult { Success, CellBlocked, ConditionFailed, PrefabNotFound }

// ─── Service ──────────────────────────────────────────────────────────────────

/// <summary>
/// Business logic layer cho world entities.
/// Dùng PlacementValidator để kiểm tra điều kiện spawn.
/// </summary>
public class WorldEntityService
{
    private readonly WorldEntityRegistry _registry;
    private readonly PlacementValidator  _validator;
    private readonly TileData            _tileData;
    private readonly Tilemap             _tilemap;

    public WorldEntityService(WorldEntityRegistry registry, TileData tileData, Tilemap tilemap)
    {
        _registry  = registry ?? throw new ArgumentNullException(nameof(registry));
        _tileData  = tileData;
        _tilemap   = tilemap;
        _validator = new PlacementValidator(registry, tileData);
    }

    // ── Ground ────────────────────────────────────────────────────────────────

    public void SetGround(Vector2Int cell, TileBase tile)
    {
        _registry.SetGround(cell, tile);
        _tilemap?.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
    }

    public void SetGroundByName(Vector2Int cell, string tileName)
    {
        var tile = ResolveTile(tileName);
        if (tile != null) SetGround(cell, tile);
    }

    public TileBase GetGround(Vector2Int cell) => _registry.GetGround(cell);

    // ── Spawn ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Đăng ký entity vào registry.
    /// PlacementRule từ EntityData quyết định layer + điều kiện.
    /// </summary>
    public SpawnResult TryRegisterSpawn(EntityPosition ep, PlacementRule rule)
    {
        // Tính occupied cells nếu chưa có
        if (ep.occupiedCells == null || ep.occupiedCells.Length == 0)
            ep.occupiedCells = new[] { WorldToCell(ep.pos) };

        ep.layer = rule.occupyLayer;

        // Validate từng cell
        foreach (var cell in ep.occupiedCells)
        {
            var result = _validator.CanPlace(rule, cell, out var reason);
            if (result != SpawnResult.Success)
            {
                Debug.LogWarning($"[WorldEntityService] {reason}");
                return result;
            }
        }

        // Đăng ký
        _registry.RegisterEntity(ep);
        if (!_registry.AddToSpatial(ep, rule.occupyLayer))
        {
            _registry.UnregisterEntity(ep.idRuntime);
            return SpawnResult.CellBlocked;
        }

        return SpawnResult.Success;
    }

    /// <summary>
    /// Đăng ký entity KHÔNG qua PlacementValidator (dùng cho EntityDrop).
    /// </summary>
    public void ForceRegisterSpawn(EntityPosition ep)
    {
        if (ep.occupiedCells == null || ep.occupiedCells.Length == 0)
            ep.occupiedCells = new[] { WorldToCell(ep.pos) };

        _registry.RegisterEntity(ep);
        _registry.AddToSpatial(ep, ep.layer);
    }

    // ── Despawn ───────────────────────────────────────────────────────────────

    public bool TryUnregister(string idRuntime)
    {
        var ep = _registry.GetEntity(idRuntime);
        if (ep == null) return false;
        _registry.RemoveFromSpatial(ep, ep.layer);
        _registry.UnregisterEntity(idRuntime);
        return true;
    }

    // ── Move ──────────────────────────────────────────────────────────────────

    public bool MoveEntity(string idRuntime, Vector2 newPos, Vector2Int[] newCells = null)
    {
        var ep = _registry.GetEntity(idRuntime);
        if (ep == null) return false;

        _registry.RemoveFromSpatial(ep, ep.layer);
        ep.pos           = newPos;
        ep.occupiedCells = newCells ?? new[] { WorldToCell(newPos) };
        _registry.AddToSpatial(ep, ep.layer);
        return true;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public void UpdateEntityId(string oldId, string newId) => _registry.UpdateEntityId(oldId, newId);

    public bool CanPlaceAt(PlacementRule rule, Vector2Int cell, out string reason)
        => _validator.CanPlace(rule, cell, out reason) == SpawnResult.Success;

    /// <summary>Kiểm tra ô có bị block bởi entity ở layer chỉ định không.</summary>
    public bool HasBlockerAt(Vector2Int cell, EntityLayer layer)
        => _registry.HasEntityAtLayer(cell, layer);

    /// <summary>Kiểm tra tile tại ô có thể cuốc được không (tồn tại, chưa plowed/watered).</summary>
    public bool IsTillable(Vector2Int cell)
    {
        if (_tileData == null || _tilemap == null) return false;
        var tile = _tilemap.GetTile(new Vector3Int(cell.x, cell.y, 0));
        if (tile == null) return false;
        return tile != _tileData.plowedTile && tile != _tileData.wateredTile;
    }

    public IEnumerable<string> GetEntitiesAt(Vector2Int cell) => _registry.GetEntitiesAt(cell);
    public EntityPosition GetEntityPosition(string idRuntime)  => _registry.GetEntity(idRuntime);

    // ── Helper ────────────────────────────────────────────────────────────────

    public static Vector2Int[] BuildOccupiedCells(Vector2Int origin, EntityRuntime runtime)
    {
        int areaX = 1, areaY = 1;
        if (runtime?.stats != null)
        {
            int x = Mathf.RoundToInt(runtime.stats.Get(StatType.AreaX));
            int y = Mathf.RoundToInt(runtime.stats.Get(StatType.AreaY));
            if (x > 0) areaX = x;
            if (y > 0) areaY = y;
        }

        var cells = new Vector2Int[areaX * areaY];
        int i = 0;
        for (int dx = 0; dx < areaX; dx++)
        for (int dy = 0; dy < areaY; dy++)
            cells[i++] = origin + new Vector2Int(dx, dy);
        return cells;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    public void Save(string filename)
    {
        var entitySaves = new List<EntityPositionSave>();
        foreach (var ep in _registry.GetAllEntities())
        {
            var s = new EntityPositionSave
            {
                idRuntime = ep.idRuntime,
                idPrefab  = ep.idPrefab.ToString(),
                posX  = ep.pos.x,
                posY  = ep.pos.y,
                layer = (int)ep.layer
            };
            if (ep.occupiedCells != null)
            {
                s.cellsX = new int[ep.occupiedCells.Length];
                s.cellsY = new int[ep.occupiedCells.Length];
                for (int i = 0; i < ep.occupiedCells.Length; i++)
                {
                    s.cellsX[i] = ep.occupiedCells[i].x;
                    s.cellsY[i] = ep.occupiedCells[i].y;
                }
            }
            entitySaves.Add(s);
        }

        var tileSaves = new List<TileEntrySave>();
        foreach (var kv in _registry.SnapshotSpatial())
        {
            var layerList = new List<LayerEntitySave>();
            foreach (var lkv in kv.Value.layers)
                layerList.Add(new LayerEntitySave { layer = (int)lkv.Key, entityId = lkv.Value });

            tileSaves.Add(new TileEntrySave
            {
                cellX         = kv.Key.x,
                cellY         = kv.Key.y,
                groundName    = kv.Value.groundType?.name ?? string.Empty,
                layerEntities = layerList.ToArray()
            });
        }

        File.WriteAllText(SavePath(filename),
            JsonUtility.ToJson(new WorldSaveContainer
            {
                entities = entitySaves.ToArray(),
                tiles    = tileSaves.ToArray()
            }, true));

        Debug.Log($"[WorldEntityService] Saved {entitySaves.Count} entities, {tileSaves.Count} tiles.");
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    public void Load(string filename, Action<EntityPosition> onEntityLoaded = null)
    {
        var path = SavePath(filename);
        if (!File.Exists(path)) { Debug.LogWarning($"[WorldEntityService] File not found: {path}"); return; }

        WorldSaveContainer container;
        try { container = JsonUtility.FromJson<WorldSaveContainer>(File.ReadAllText(path)); }
        catch (Exception ex) { Debug.LogError($"[WorldEntityService] Parse error: {ex.Message}"); return; }
        if (container == null) return;

        var positions = new Dictionary<string, EntityPosition>();
        if (container.entities != null)
        {
            foreach (var s in container.entities)
            {
                var cells = (s.cellsX != null && s.cellsX.Length > 0)
                    ? BuildCells(s.cellsX, s.cellsY)
                    : new[] { WorldToCell(new Vector2(s.posX, s.posY)) };

                if (!System.Enum.TryParse<ObjectType>(s.idPrefab, out var objType))
                {
                    Debug.LogWarning($"[WorldEntityService] Unknown ObjectType '{s.idPrefab}', skipping.");
                    continue;
                }
                positions[s.idRuntime] = new EntityPosition
                {
                    idRuntime     = s.idRuntime,
                    idPrefab      = objType,
                    pos           = new Vector2(s.posX, s.posY),
                    occupiedCells = cells,
                    layer         = (EntityLayer)s.layer
                };
            }
        }

        var spatial = new Dictionary<Vector2Int, TileEntry>();
        if (container.tiles != null)
        {
            foreach (var t in container.tiles)
            {
                var entry = new TileEntry { groundType = ResolveTile(t.groundName) };
                if (t.layerEntities != null)
                    foreach (var le in t.layerEntities)
                        entry.TryAdd((EntityLayer)le.layer, le.entityId);

                spatial[new Vector2Int(t.cellX, t.cellY)] = entry;
            }
        }

        _registry.ReplaceAll(positions, spatial);
        RebuildTilemap(spatial);

        if (onEntityLoaded != null)
            foreach (var ep in positions.Values)
                onEntityLoaded(ep);

        Debug.Log($"[WorldEntityService] Loaded {positions.Count} entities, {spatial.Count} tiles.");
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private Vector2Int WorldToCell(Vector2 worldPos)
    {
        if (_tilemap != null)
        {
            var c = _tilemap.WorldToCell(new Vector3(worldPos.x, worldPos.y, 0));
            return new Vector2Int(c.x, c.y);
        }
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }

    private static Vector2Int[] BuildCells(int[] xs, int[] ys)
    {
        var cells = new Vector2Int[xs.Length];
        for (int i = 0; i < xs.Length; i++) cells[i] = new Vector2Int(xs[i], ys[i]);
        return cells;
    }

    private TileBase ResolveTile(string name)
    {
        if (string.IsNullOrEmpty(name) || _tileData == null) return null;
        if (_tileData.landTile?.name    == name) return _tileData.landTile;
        if (_tileData.plowedTile?.name  == name) return _tileData.plowedTile;
        if (_tileData.wateredTile?.name == name) return _tileData.wateredTile;
        if (_tileData.grassTile?.name   == name) return _tileData.grassTile;
        return null;
    }

    private void RebuildTilemap(Dictionary<Vector2Int, TileEntry> spatial)
    {
        if (_tilemap == null) return;
        // Chỉ overlay tiles có groundType từ save, không xóa tilemap gốc
        foreach (var kv in spatial)
            if (kv.Value.groundType != null)
                _tilemap.SetTile(new Vector3Int(kv.Key.x, kv.Key.y, 0), kv.Value.groundType);
    }

    private static string SavePath(string filename) =>
        Path.Combine(Application.persistentDataPath, filename);
}
