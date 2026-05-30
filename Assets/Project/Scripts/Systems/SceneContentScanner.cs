using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Scan Tm_RuntimeMarkers và seed world objects từ tile markers.
///
/// QUAN TRỌNG — Execution order:
///   SceneTilemapRegistry [−200] → SceneContentScanner [−100] → GameManager [0] → SaveLoadManager.Boot()
///
/// Subscribe trong Awake (không dùng coroutine) để tránh race condition với
/// InventoryDataRestoredPublish được publish đồng bộ trong GameManager.Start().
/// </summary>
[DefaultExecutionOrder(-100)]
public class SceneContentScanner : MonoBehaviour
{
    [SerializeField] private SceneContext sceneContext;
    [SerializeField] private bool scanOnStart = true;

    private EventBus subscribedBus;
    private bool hasScanned;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        Debug.Log($"[SceneContentScanner] Awake — GameManager.Instance={(GameManager.Instance != null ? "ok" : "null")}, EventBus={(GameManager.Instance?.EventBus != null ? "ok" : "null")}");
        TrySubscribe();
    }

    private void Start()
    {
        // Fallback: thử subscribe lần nữa nếu Awake chưa thành công
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus == null) return;
        subscribedBus.Unsubscribe<WorldObjectsSpawnedPublish>(OnWorldObjectsSpawned);
        subscribedBus.Unsubscribe<InventoryDataRestoredPublish>(OnInventoryDataRestored);
        subscribedBus = null;
    }

    // ── Event handlers ─────────────────────────────────────────────────────

    /// <summary>
    /// WorldObjectsSpawnedPublish fire trước InventoryDataRestored —
    /// dùng làm trigger chính để seed ngay sau khi world load.
    /// </summary>
    private void OnWorldObjectsSpawned(WorldObjectsSpawnedPublish _) => TryRunScan();

    /// <summary>
    /// Fallback: InventoryDataRestoredPublish (fire sau World) cũng trigger scan
    /// phòng trường hợp WorldObjectsSpawnedPublish bị miss.
    /// </summary>
    private void OnInventoryDataRestored(InventoryDataRestoredPublish _) => TryRunScan();

    // ── Core ──────────────────────────────────────────────────────────────

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;
        if (!scanOnStart) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null)
        {
            Debug.LogWarning($"[SceneContentScanner] TrySubscribe FAILED — GameManager={(GameManager.Instance != null ? "ok" : "null")}, EventBus=null");
            return;
        }

        bus.Subscribe<WorldObjectsSpawnedPublish>(OnWorldObjectsSpawned);
        bus.Subscribe<InventoryDataRestoredPublish>(OnInventoryDataRestored);
        subscribedBus = bus;
        Debug.Log("[SceneContentScanner] Subscribed to WorldObjectsSpawned + InventoryDataRestored.");
    }

    private void TryRunScan()
    {
        // Chỉ chạy nếu scene này đang là active scene
        // (tránh FarmScene scanner chạy khi active scene là MineScene v.v.)
        string myScene = gameObject.scene.name;
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(myScene, activeScene, System.StringComparison.Ordinal))
        {
            Debug.Log($"[SceneContentScanner] TryRunScan skipped — belongs to '{myScene}', active is '{activeScene}'");
            return;
        }

        Debug.Log($"[SceneContentScanner] TryRunScan — hasScanned={hasScanned}");
        if (hasScanned) return;
        hasScanned = true;
        ScanAndSeed();
    }

    public void ScanAndSeed()
    {
        sceneContext ??= SceneContext.Current ?? FindAnyObjectByType<SceneContext>();
        if (sceneContext == null) return;

        sceneContext.AutoBind();
        Tilemap markerMap = sceneContext.RuntimeMarkers;
        if (markerMap == null)
        {
            Debug.LogWarning("[SceneContentScanner] RuntimeMarkers tilemap not found. Cannot seed world objects.");
            return;
        }

        var gameManager = GameManager.Instance;
        var worldService = gameManager?.WorldService;
        var eventBus = gameManager?.EventBus;
        if (worldService == null || eventBus == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        int currentGameMinute = gameManager.TimeManager != null ? gameManager.TimeManager.CurrentTotalMinutes : 0;
        var bounds = markerMap.cellBounds;
        int seededCount = 0;

        // --- PRE-PASS: CLEAN UP STALE PERSISTENT ENTITIES ---
        // Gather all valid persistent IDs from tm_markers
        var validPersistentIds = new System.Collections.Generic.HashSet<string>();
        foreach (var cell in bounds.allPositionsWithin)
        {
            var tile = markerMap.GetTile<SceneSpawnTile>(cell);
            if (tile == null || tile.markerKind == SceneMarkerKind.PlayerSpawn) continue;

            string pid = SceneSpawnPayload.BuildPersistentId(
                sceneName, tile.markerKind, tile.objectType, cell, tile.spawnGroupId);
            validPersistentIds.Add(pid);
        }

        // Identify and destroy stale entities from save data
        string scenePrefix = sceneName + ":";
        var staleRuntimeIds = new System.Collections.Generic.List<string>();
        foreach (var ep in worldService.GetAllPositions())
        {
            if (!string.IsNullOrEmpty(ep.persistentId) &&
                ep.persistentId.StartsWith(scenePrefix) &&
                !validPersistentIds.Contains(ep.persistentId))
            {
                staleRuntimeIds.Add(ep.idRuntime);
            }
        }

        foreach (var id in staleRuntimeIds)
        {
            Debug.Log($"[SceneContentScanner] Cleaning up stale persistent entity (marker deleted): {id}");
            eventBus.Publish(new DestroyEntityRequestPublish(id));
        }

        // Clean up internal stale references in WorldEntityService
        worldService.CleanUpStalePersistentIds(scenePrefix, validPersistentIds);

        // --- PASS: SEED NEW MARKERS ---
        foreach (var cell in bounds.allPositionsWithin)
        {
            var tile = markerMap.GetTile<SceneSpawnTile>(cell);
            if (tile == null) continue;
            if (tile.markerKind == SceneMarkerKind.PlayerSpawn)
                continue;
            if (tile.savePolicy == SceneEntitySavePolicy.Persistent
                && gameManager?.ClearZoneTracker != null
                && gameManager.ClearZoneTracker.IsZoneCleared(tile.spawnGroupId))
                continue;

            string persistentId = SceneSpawnPayload.BuildPersistentId(
                sceneName,
                tile.markerKind,
                tile.objectType,
                cell,
                tile.spawnGroupId);

            if (worldService.TryGetInactiveRespawn(persistentId, out var inactiveRespawn))
            {
                if (!worldService.TryConsumeInactiveRespawn(persistentId, currentGameMinute, out _))
                {
                    Debug.Log($"[SceneContentScanner] Marker '{persistentId}' waiting respawn until game minute {inactiveRespawn.availableAtGameMinute}.");
                    continue;
                }
            }
            else if (worldService.HasPersistentId(persistentId))
            {
                continue;
            }

            // Portal và một số static object KHÔNG cần EntityData —
            // chỉ cần prefab instantiate, không có stats/inventory.
            // Với object có EntityData null mà không phải Portal → warning và skip.
            bool needsEntityData = tile.markerKind != SceneMarkerKind.Portal;
            if (needsEntityData && tile.entityData == null)
            {
                Debug.LogWarning($"[SceneContentScanner] Marker '{persistentId}' missing EntityData.");
                continue;
            }

            var payload = new SceneSpawnPayload
            {
                sceneName = sceneName,
                markerKind = tile.markerKind,
                objectType = tile.objectType,
                cell = cell,
                spawnGroupId = tile.spawnGroupId,
                persistentId = persistentId,
                savePolicy = tile.savePolicy,
                respawnMinutes = Mathf.Max(0, tile.respawnMinutes),
                initialAmount = Mathf.Max(1, tile.initialAmount),
                availableAtGameMinute = 0,
                startStageIndex = ResolveStartStageIndex(tile)
            };

            Vector3 world = markerMap.GetCellCenterWorld(cell);
            eventBus.Publish(new SpawnRequestPublish(
                worldPos: new Vector2(world.x, world.y),
                idPrefab: tile.objectType,
                entityData: tile.entityData,
                spawnAmount: payload.initialAmount,
                bypassValidation: tile.bypassPlacementValidation,
                payload: payload));

            seededCount++;
        }

        if (seededCount > 0)
            Debug.Log($"[SceneContentScanner] Seeded {seededCount} marker entity/entities for scene '{sceneName}'.");
        else
            Debug.Log($"[SceneContentScanner] No new markers to seed in scene '{sceneName}' (all already seeded or no tiles).");
    }

    private static int ResolveStartStageIndex(SceneSpawnTile tile)
    {
        if (tile == null || tile.entityData == null)
            return -1;

        var stageModule = tile.entityData.modules?.OfType<StageModule>().FirstOrDefault();
        int stageCount = stageModule?.stages?.Length ?? 0;
        if (stageCount <= 0)
            return -1;

        switch (tile.stageSpawnMode)
        {
            case MarkerStageSpawnMode.FixedStage:
                return Mathf.Clamp(tile.fixedStartStageIndex, 0, stageCount - 1);

            case MarkerStageSpawnMode.RandomRange:
                int min = Mathf.Clamp(tile.randomStartStageMin, 0, stageCount - 1);
                int max = Mathf.Clamp(tile.randomStartStageMax, 0, stageCount - 1);
                if (max < min)
                    (min, max) = (max, min);
                return Random.Range(min, max + 1);

            default:
                return -1;
        }
    }
}
