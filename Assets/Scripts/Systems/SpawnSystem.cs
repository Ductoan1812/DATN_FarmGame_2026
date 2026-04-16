using UnityEngine;

/// <summary>
/// Lắng nghe SpawnRequest / DespawnRequest từ EventBus.
/// Dùng PlacementRule từ EntityData để validate trước khi spawn.
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

        _bus.Subscribe<SpawnRequest>(OnSpawnRequest);
        _bus.Subscribe<DespawnRequest>(OnDespawnRequest);
    }

    private void OnDestroy()
    {
        if (_bus == null) return;
        _bus.Unsubscribe<SpawnRequest>(OnSpawnRequest);
        _bus.Unsubscribe<DespawnRequest>(OnDespawnRequest);
    }

    // ── Spawn ──────────────────────────────────────────────

    private void OnSpawnRequest(SpawnRequest req)
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
            idRuntime     = spawnRuntime.Id,
            idPrefab      = req.idPrefab,
            pos           = req.worldPos,
            occupiedCells = cells,
            layer         = rule.occupyLayer
        };

        // Đăng ký vào WorldEntityRegistry
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
            _worldService.UpdateEntityId(spawnRuntime.Id, spawnEntity.Id);
        }
        else
        {
            spawnEntity = spawnRuntime;
        }

        // Instantiate GameObject
        var obj = Instantiate(prefab, new Vector3(req.worldPos.x, req.worldPos.y, 0), Quaternion.identity);
        obj.name = $"{req.idPrefab}_{spawnEntity.Id[..8]}";

        // Gắn EntityRuntime vào EntityRoot
        var root = obj.GetComponent<EntityRoot>();
        if (root != null)
        {
            root.entityService = _entityService;
            root.Add(spawnEntity); // EntityRoot.Add đã tự gọi SpawnedEvent
        }

        Debug.Log($"[SpawnSystem] Spawned '{req.idPrefab}' layer={rule.occupyLayer} id={spawnRuntime.Id[..8]}");
    }

    // ── Despawn ────────────────────────────────────────────

    private void OnDespawnRequest(DespawnRequest req)
    {
        var ep = _worldService.GetEntityPosition(req.idRuntime);
        if (ep == null)
        {
            Debug.LogWarning($"[SpawnSystem] Entity not found: '{req.idRuntime}'");
            return;
        }

        var obj = GameObject.Find($"{ep.idPrefab}_{req.idRuntime[..8]}");
        if (obj != null) Destroy(obj);

        _worldService.TryUnregister(req.idRuntime);
        Debug.Log($"[SpawnSystem] Despawned '{req.idRuntime[..8]}'");
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
    }

    // ── Private ────────────────────────────────────────────

    private EntityRuntime ResolveRuntime(SpawnRequest req)
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

    private static Vector2Int WorldToCell(Vector2 worldPos) =>
        new(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
}
