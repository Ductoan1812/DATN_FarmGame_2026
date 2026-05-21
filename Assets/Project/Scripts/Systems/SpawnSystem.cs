using UnityEngine;

/// <summary>
/// Lắng nghe SpawnRequestPublish / DespawnRequestPublish / DestroyEntityRequestPublish từ EventBus.
/// Dùng PlacementRule từ EntityData để validate trước khi spawn.
///
/// Phân biệt:
///   - DespawnRequestPublish    → remove GameObject + world pos, GIỮ entity trong Registry (cho respawn).
///   - DestroyEntityRequestPublish → remove GameObject + Unregister entity (hủy vĩnh viễn).
/// </summary>
public class SpawnSystem : MonoBehaviour
{
    private EventBus            _bus;
    private WorldEntityService  _worldService;
    private WorldObjectRegistry _worldObjects;
    private EntityService       _entityService;

    // ── Setup ──────────────────────────────────────────────

    public void Init(EventBus bus, WorldEntityService worldService,
                     WorldObjectRegistry worldObjects, EntityService entityService)
    {
        _bus           = bus;
        _worldService  = worldService;
        _worldObjects  = worldObjects;
        _entityService = entityService;

        _bus.Subscribe<SpawnRequestPublish>(OnSpawnRequest);
        _bus.Subscribe<DespawnRequestPublish>(OnDespawnRequest);
        _bus.Subscribe<DestroyEntityRequestPublish>(OnDestroyEntityRequest);
    }

    public void RebindWorldService(WorldEntityService worldService)
    {
        _worldService = worldService;
    }

    private void OnDestroy()
    {
        if (_bus == null) return;
        _bus.Unsubscribe<SpawnRequestPublish>(OnSpawnRequest);
        _bus.Unsubscribe<DespawnRequestPublish>(OnDespawnRequest);
        _bus.Unsubscribe<DestroyEntityRequestPublish>(OnDestroyEntityRequest);
    }

    // ── Spawn ──────────────────────────────────────────────

    private void OnSpawnRequest(SpawnRequestPublish req)
    {
        // Validate prefab
        var prefab = _worldObjects.GetPrefab(req.idPrefab);
        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnSystem] Prefab not found: '{req.idPrefab}'");
            return;
        }

        // Resolve EntityRuntime
        EntityRuntime spawnRuntime = ResolveRuntime(req);
        if (spawnRuntime == null)
        {
            Debug.LogWarning($"[SpawnSystem] Cannot resolve EntityRuntime for '{req.idPrefab}'");
            return;
        }

        // Lấy PlacementRule từ EntityData
        var rule = spawnRuntime.entityData.placementRule;

        // Tính occupied cells
        var origin = WorldToCell(req.worldPos);
        var cells  = WorldEntityService.BuildOccupiedCells(origin, spawnRuntime);

        var ep = new EntityPosition
        {
            idRuntime     = spawnRuntime.id,
            idPrefab      = req.idPrefab,
            pos           = req.worldPos,
            occupiedCells = cells,
            layer         = rule.occupyLayer
        };
        ApplyScenePayload(ep, req);

        // Đăng ký vào SpatialEntityRegistry
        if (req.bypassValidation)
        {
            _worldService.ForceRegisterSpawn(ep);
        }
        else
        {
            var result = _worldService.TryRegisterSpawn(ep, rule);
            if (result != SpawnResult.Success)
            {
                Debug.LogWarning($"[SpawnSystem] Spawn failed ({result}): '{req.idPrefab}' at {req.worldPos}");
                return;
            }
        }

        // Split nếu được yêu cầu
        EntityRuntime spawnEntity;
        if (req.splitOnSpawn)
        {
            spawnEntity = _entityService.Split(spawnRuntime, 1);
            if (spawnEntity == null)
            {
                _worldService.TryUnregister(ep.idRuntime);
                Debug.LogWarning($"[SpawnSystem] Split failed (amount = 0?): '{req.idPrefab}'");
                return;
            }
            // Cập nhật idRuntime trong registry sang entity mới (không unregister/re-register)
            _worldService.UpdateEntityId(spawnRuntime.id, spawnEntity.id);
        }
        else
        {
            spawnEntity = spawnRuntime;
        }

        var respawnRuntime = spawnEntity.GetModule<RespawnRuntime>();
        if (respawnRuntime != null)
            respawnRuntime.CurrentRespawnPosition = req.worldPos;

        GameManager.Instance?.ClearZoneTracker?.RegisterSpawn(
            spawnEntity.id,
            ep.spawnGroupId,
            ep.occupiedCells,
            ep.savePolicy);

        // Instantiate GameObject
        var obj = Instantiate(prefab, new Vector3(req.worldPos.x, req.worldPos.y, 0), Quaternion.identity);
        obj.name = $"{req.idPrefab}_{spawnEntity.id[..8]}";

        // Gắn EntityRuntime vào EntityRoot
        var root = obj.GetComponent<EntityRoot>();
        if (root != null)
        {
            root.entityService = _entityService;
            root.Add(spawnEntity); // EntityRoot.Add đã tự gọi SpawnedEvent
        }

        Debug.Log($"[SpawnSystem] Spawned '{req.idPrefab}' layer={rule.occupyLayer} id={spawnRuntime.id[..8]}");
    }

    // ── Despawn (giữ entity trong registry) ───────────────

    private void OnDespawnRequest(DespawnRequestPublish req)
    {
        DespawnGameObject(req.idRuntime);
    }

    // ── Destroy (hủy vĩnh viễn) ────────────────────────────

    private void OnDestroyEntityRequest(DestroyEntityRequestPublish req)
    {
        DespawnGameObject(req.idRuntime);

        var entity = _entityService.Get(req.idRuntime);
        if (entity != null)
        {
            _entityService.Destroy(entity);
            Debug.Log($"[SpawnSystem] Destroyed entity '{req.idRuntime[..8]}' permanently.");
        }
    }

    private void DespawnGameObject(string idRuntime)
    {
        var ep = _worldService.GetEntityPosition(idRuntime);
        if (ep == null)
        {
            Debug.LogWarning($"[SpawnSystem] Entity not found in world: '{idRuntime}'");
            return;
        }

        var obj = GameObject.Find($"{ep.idPrefab}_{idRuntime[..8]}");
        if (obj != null) Destroy(obj);

        GameManager.Instance?.ClearZoneTracker?.NotifyEntityRemoved(idRuntime);
        _worldService.TryUnregister(idRuntime);
        Debug.Log($"[SpawnSystem] Despawned GameObject '{idRuntime[..8]}'");
    }

    // ── Load callback ──────────────────────────────────────

    public void ReinstantiateFromSave(EntityPosition ep, EntityRuntime runtime = null)
    {
        var prefab = _worldObjects.GetPrefab(ep.idPrefab);
        if (prefab == null) { Debug.LogWarning($"[SpawnSystem] Prefab not found on load: '{ep.idPrefab}'"); return; }

        var obj = Instantiate(prefab, new Vector3(ep.pos.x, ep.pos.y, 0), Quaternion.identity);
        obj.name = $"{ep.idPrefab}_{ep.idRuntime[..8]}";

        if (runtime != null)
        {
            var root = obj.GetComponent<EntityRoot>();
            if (root != null) { root.entityService = _entityService; root.Add(runtime); }
        }

        GameManager.Instance?.ClearZoneTracker?.RegisterSpawn(
            ep.idRuntime,
            ep.spawnGroupId,
            ep.occupiedCells,
            ep.savePolicy);
    }

    // ── Private ────────────────────────────────────────────

    private EntityRuntime ResolveRuntime(SpawnRequestPublish req)
    {
        if (req.runtime == null)
        {
            if (req.entityData == null) return null;
            return _entityService.Create(req.entityData, req.spawnAmount);
        }

        if (req.spawnAmount >= req.runtime.Amount)
            return req.runtime;

        return _entityService.Split(req.runtime, req.spawnAmount);
    }

    private static void ApplyScenePayload(EntityPosition ep, SpawnRequestPublish req)
    {
        if (ep == null) return;

        if (req.payload is SceneSpawnPayload scenePayload)
        {
            ep.persistentId = scenePayload.persistentId;
            ep.savePolicy = scenePayload.savePolicy;
            ep.spawnGroupId = scenePayload.spawnGroupId;
            ep.respawnMinutes = Mathf.Max(0, scenePayload.respawnMinutes);
            ep.initialAmount = Mathf.Max(1, scenePayload.initialAmount);
            ep.availableAtGameMinute = Mathf.Max(0, scenePayload.availableAtGameMinute);
            return;
        }

        if (req.idPrefab == ObjectType.EntityDrop || req.idPrefab == ObjectType.Player01)
            ep.savePolicy = SceneEntitySavePolicy.Temporary;
    }

    private static Vector2Int WorldToCell(Vector2 worldPos) =>
        new(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
}
