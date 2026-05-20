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
    public string objectType;
    public string persistentId;
    public int savePolicy;
    public string spawnGroupId;
    public int respawnMinutes;
    public int initialAmount;
    public int availableAtGameMinute;
    public float posX, posY;
    public int[] cellsX, cellsY;
    public int layer; // EntityLayer as int
}

[Serializable]
public class LayerEntitySave
{
    public int layer;
    public string entityId;
}

/// <summary>
/// Save container mới: chỉ lưu entity positions + tile dirty changes.
/// Không lưu ô trống, không lưu ground tile (dùng TileRegistry dirty).
/// </summary>
[Serializable]
public class WorldSaveContainer
{
    public EntityPositionSave[] entities;
    public EntityPositionSave[] inactiveEntities;
    public TileChangeSave[]     tileChanges; // dirty tiles only
}

// ─── Spawn Result ─────────────────────────────────────────────────────────────

public enum SpawnResult { Success, CellBlocked, ConditionFailed, PrefabNotFound }

// ─── Service ──────────────────────────────────────────────────────────────────

/// <summary>
/// Business logic layer cho world entities.
/// Dùng:
///   - SpatialEntityRegistry: entity positions + spatial map
///   - TileRegistry: tile baseline + dirty
///   - PlacementValidator: kiểm tra điều kiện spawn
/// </summary>
public class WorldEntityService
{
    private readonly SpatialEntityRegistry _spatial;
    private readonly TileRegistry          _tileRegistry;
    private readonly PlacementValidator    _validator;
    private readonly TileData              _tileData;
    private readonly Tilemap               _tilemap; // reference tilemap cho WorldToCell
    private readonly Dictionary<string, EntityPosition> _inactiveRegenerating = new();

    public WorldEntityService(
        SpatialEntityRegistry spatial,
        TileRegistry tileRegistry,
        TileData tileData,
        Tilemap tilemap)
    {
        _spatial      = spatial      ?? throw new ArgumentNullException(nameof(spatial));
        _tileRegistry = tileRegistry ?? throw new ArgumentNullException(nameof(tileRegistry));
        _tileData     = tileData;
        _tilemap      = tilemap;
        _validator    = new PlacementValidator(spatial, tileRegistry, tileData);
    }

    // ── Ground (delegate to TileRegistry) ─────────────────────────────────────

    /// <summary>Set ground tile tại cell trên Tm_Ground. Tự ghi dirty nếu khác baseline.</summary>
    public void SetGround(Vector2Int cell, TileBase tile)
    {
        _tileRegistry.SetTile("Tm_Ground", cell, tile);
    }

    public void SetGroundByName(Vector2Int cell, string tileName)
    {
        var tile = ResolveTile(tileName);
        if (tile != null) SetGround(cell, tile);
    }

    public TileBase GetGround(Vector2Int cell) => _tileRegistry.GetTile("Tm_Ground", cell);

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
        _spatial.RegisterEntity(ep);
        if (!_spatial.AddToSpatial(ep, rule.occupyLayer))
        {
            _spatial.UnregisterEntity(ep.idRuntime);
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

        _spatial.RegisterEntity(ep);
        _spatial.AddToSpatial(ep, ep.layer);
    }

    // ── Despawn ───────────────────────────────────────────────────────────────

    public bool TryUnregister(string idRuntime)
    {
        var ep = _spatial.GetEntity(idRuntime);
        if (ep == null) return false;
        _spatial.RemoveFromSpatial(ep, ep.layer);
        _spatial.UnregisterEntity(idRuntime);
        return true;
    }

    // ── Move ──────────────────────────────────────────────────────────────────

    public bool MoveEntity(string idRuntime, Vector2 newPos, Vector2Int[] newCells = null)
    {
        var ep = _spatial.GetEntity(idRuntime);
        if (ep == null) return false;

        _spatial.RemoveFromSpatial(ep, ep.layer);
        ep.pos           = newPos;
        ep.occupiedCells = newCells ?? new[] { WorldToCell(newPos) };
        _spatial.AddToSpatial(ep, ep.layer);
        return true;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public void UpdateEntityId(string oldId, string newId) => _spatial.UpdateEntityId(oldId, newId);

    public bool CanPlaceAt(PlacementRule rule, Vector2Int cell, out string reason)
        => _validator.CanPlace(rule, cell, out reason) == SpawnResult.Success;

    /// <summary>Kiểm tra ô có bị block bởi entity ở layer chỉ định không.</summary>
    public bool HasBlockerAt(Vector2Int cell, EntityLayer layer)
        => _spatial.HasEntityAtLayer(cell, layer);

    /// <summary>Kiểm tra tile tại ô có thể cuốc được không (tồn tại, chưa plowed/watered).</summary>
    public bool IsTillable(Vector2Int cell)
    {
        if (_tileData == null || _tileRegistry == null) return false;
        var tile = _tileRegistry.GetTile("Tm_Ground", cell);
        if (tile == null) return false;
        return tile != _tileData.plowedTile && tile != _tileData.wateredTile;
    }

    public IEnumerable<string> GetEntitiesAt(Vector2Int cell) => _spatial.GetEntitiesAt(cell);
    public EntityPosition GetEntityPosition(string idRuntime)  => _spatial.GetEntity(idRuntime);

    public bool HasPersistentId(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId)) return false;
        foreach (var ep in _spatial.GetAllEntities())
        {
            if (ep == null) continue;
            if (string.Equals(ep.persistentId, persistentId, StringComparison.Ordinal))
                return true;
        }
        return _inactiveRegenerating.ContainsKey(persistentId);
    }

    public EntityPosition FindByPersistentId(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId)) return null;
        foreach (var ep in _spatial.GetAllEntities())
        {
            if (ep == null) continue;
            if (string.Equals(ep.persistentId, persistentId, StringComparison.Ordinal))
                return ep;
        }
        if (_inactiveRegenerating.TryGetValue(persistentId, out var inactive))
            return inactive;
        return null;
    }

    public bool TryGetInactiveRespawn(string persistentId, out EntityPosition ep)
    {
        ep = null;
        if (string.IsNullOrWhiteSpace(persistentId)) return false;
        return _inactiveRegenerating.TryGetValue(persistentId, out ep);
    }

    public bool TryConsumeInactiveRespawn(string persistentId, int currentGameMinute, out EntityPosition ep)
    {
        ep = null;
        if (!TryGetInactiveRespawn(persistentId, out var inactive))
            return false;

        if (currentGameMinute < inactive.availableAtGameMinute)
        {
            ep = inactive;
            return false;
        }

        ep = inactive;
        _inactiveRegenerating.Remove(persistentId);
        return true;
    }

    public bool ScheduleInactiveRespawn(EntityRuntime entity, int availableAtGameMinute)
    {
        if (entity == null || string.IsNullOrWhiteSpace(entity.id)) return false;

        var ep = _spatial.GetEntity(entity.id);
        if (ep == null || string.IsNullOrWhiteSpace(ep.persistentId))
            return false;

        if (ep.savePolicy != SceneEntitySavePolicy.Regenerating || ep.respawnMinutes <= 0)
            return false;

        var inactive = ClonePosition(ep);
        inactive.availableAtGameMinute = Mathf.Max(0, availableAtGameMinute);
        _inactiveRegenerating[inactive.persistentId] = inactive;
        return true;
    }

    /// <summary>Truy cập TileRegistry (cho các hệ thống cần đọc tile trực tiếp).</summary>
    public TileRegistry TileRegistry => _tileRegistry;

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
        // 1. Save entity positions — chỉ entity đang tồn tại
        var entitySaves = new List<EntityPositionSave>();
        foreach (var ep in _spatial.GetAllEntities())
        {
            if (ep.savePolicy == SceneEntitySavePolicy.Temporary)
                continue;

            var s = new EntityPositionSave
            {
                idRuntime = ep.idRuntime,
                idPrefab  = ep.idPrefab.ToString(),
                objectType = ep.idPrefab.ToString(),
                persistentId = ep.persistentId,
                savePolicy = (int)ep.savePolicy,
                spawnGroupId = ep.spawnGroupId,
                respawnMinutes = ep.respawnMinutes,
                initialAmount = ep.initialAmount,
                availableAtGameMinute = ep.availableAtGameMinute,
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

        // 2. Save tile dirty — chỉ những ô thay đổi so với baseline
        var tileChanges = _tileRegistry.GetDirtySnapshot();

        var container = new WorldSaveContainer
        {
            entities         = entitySaves.ToArray(),
            inactiveEntities = BuildInactiveSaveList().ToArray(),
            tileChanges      = tileChanges.ToArray()
        };

        File.WriteAllText(SavePath(filename), JsonUtility.ToJson(container, true));
        Debug.Log($"[WorldEntityService] Saved {entitySaves.Count} entities, {_inactiveRegenerating.Count} inactive respawn marker(s), {tileChanges.Count} tile changes (dirty only).");
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

        // 1. Rebuild entity positions + spatial
        var positions = new Dictionary<string, EntityPosition>();
        var spatial   = new Dictionary<Vector2Int, TileEntry>();

        if (container.entities != null)
        {
            foreach (var s in container.entities)
            {
                var ep = FromSave(s);
                if (ep == null) continue;
                positions[s.idRuntime] = ep;

                // Rebuild spatial map
                foreach (var cell in ep.occupiedCells)
                {
                    if (!spatial.TryGetValue(cell, out var entry))
                        spatial[cell] = entry = new TileEntry();
                    entry.TryAdd(ep.layer, ep.idRuntime);
                }
            }
        }

        _spatial.ReplaceAll(positions, spatial);

        _inactiveRegenerating.Clear();
        if (container.inactiveEntities != null)
        {
            foreach (var s in container.inactiveEntities)
            {
                var ep = FromSave(s);
                if (ep == null || string.IsNullOrWhiteSpace(ep.persistentId)) continue;
                _inactiveRegenerating[ep.persistentId] = ep;
            }
        }

        // 2. Apply tile dirty changes lên TileRegistry
        // (TileRegistry đã ScanBaseline trước đó, giờ chỉ áp dirty)
        if (container.tileChanges != null)
        {
            _tileRegistry.ApplyDirty(
                new List<TileChangeSave>(container.tileChanges),
                tileName => ResolveTile(tileName)
            );
        }

        // 3. Callback cho mỗi entity → SpawnSystem reinstantiate
        if (onEntityLoaded != null)
            foreach (var ep in positions.Values)
                onEntityLoaded(ep);

        Debug.Log($"[WorldEntityService] Loaded {positions.Count} entities, {_inactiveRegenerating.Count} inactive respawn marker(s), {container.tileChanges?.Length ?? 0} tile changes.");
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

    private static string SavePath(string filename) =>
        Path.Combine(Application.persistentDataPath, filename);

    private static EntityPosition ClonePosition(EntityPosition ep)
    {
        return new EntityPosition
        {
            idRuntime = ep.idRuntime,
            idPrefab = ep.idPrefab,
            pos = ep.pos,
            occupiedCells = ep.occupiedCells != null ? (Vector2Int[])ep.occupiedCells.Clone() : null,
            layer = ep.layer,
            persistentId = ep.persistentId,
            savePolicy = ep.savePolicy,
            spawnGroupId = ep.spawnGroupId,
            respawnMinutes = ep.respawnMinutes,
            initialAmount = Mathf.Max(1, ep.initialAmount),
            availableAtGameMinute = ep.availableAtGameMinute
        };
    }

    private List<EntityPositionSave> BuildInactiveSaveList()
    {
        var saves = new List<EntityPositionSave>();
        foreach (var ep in _inactiveRegenerating.Values)
            saves.Add(ToSave(ep));
        return saves;
    }

    private static EntityPositionSave ToSave(EntityPosition ep)
    {
        var s = new EntityPositionSave
        {
            idRuntime = ep.idRuntime,
            idPrefab = ep.idPrefab.ToString(),
            objectType = ep.idPrefab.ToString(),
            persistentId = ep.persistentId,
            savePolicy = (int)ep.savePolicy,
            spawnGroupId = ep.spawnGroupId,
            respawnMinutes = ep.respawnMinutes,
            initialAmount = ep.initialAmount,
            availableAtGameMinute = ep.availableAtGameMinute,
            posX = ep.pos.x,
            posY = ep.pos.y,
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

        return s;
    }

    private EntityPosition FromSave(EntityPositionSave s)
    {
        if (s == null) return null;

        var cells = (s.cellsX != null && s.cellsX.Length > 0)
            ? BuildCells(s.cellsX, s.cellsY)
            : new[] { WorldToCell(new Vector2(s.posX, s.posY)) };

        string objectTypeRaw = !string.IsNullOrWhiteSpace(s.objectType) ? s.objectType : s.idPrefab;
        if (!Enum.TryParse<ObjectType>(objectTypeRaw, out var objType))
        {
            Debug.LogWarning($"[WorldEntityService] Unknown ObjectType '{objectTypeRaw}', skipping.");
            return null;
        }

        return new EntityPosition
        {
            idRuntime = s.idRuntime,
            idPrefab = objType,
            pos = new Vector2(s.posX, s.posY),
            occupiedCells = cells,
            layer = (EntityLayer)s.layer,
            persistentId = s.persistentId,
            savePolicy = Enum.IsDefined(typeof(SceneEntitySavePolicy), s.savePolicy)
                ? (SceneEntitySavePolicy)s.savePolicy
                : SceneEntitySavePolicy.Persistent,
            spawnGroupId = s.spawnGroupId,
            respawnMinutes = s.respawnMinutes,
            initialAmount = Mathf.Max(1, s.initialAmount),
            availableAtGameMinute = s.availableAtGameMinute
        };
    }
}
