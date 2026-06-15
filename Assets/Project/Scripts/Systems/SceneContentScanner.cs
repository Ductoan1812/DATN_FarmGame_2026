using System;
using System.Collections.Generic;
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
    private static readonly Vector3Int[] RegionDirections =
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0)
    };

    [SerializeField] private SceneContext sceneContext;
    [SerializeField] private bool scanOnStart = true;
    [SerializeField, Min(0.1f)] private float ruleTickIntervalSeconds = 0.5f;

    private EventBus subscribedBus;
    private bool hasScanned;
    private float nextRuleTickTime;
    private readonly List<RuleRegionRuntime> activeRuleRegions = new();

    private sealed class RuleRegionRuntime
    {
        public string regionId;
        public SceneSpawnRuleRegionTile tile;
        public List<Vector3Int> cells;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        Debug.Log($"[SceneContentScanner] Awake — GameManager.Instance={(GameManager.Instance != null ? "ok" : "null")}, EventBus={(GameManager.Instance?.EventBus != null ? "ok" : "null")}");
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!hasScanned || activeRuleRegions.Count == 0)
            return;

        if (Time.unscaledTime < nextRuleTickTime)
            return;

        nextRuleTickTime = Time.unscaledTime + ruleTickIntervalSeconds;
        TickRuleRegions();
    }

    private void OnDisable()
    {
        activeRuleRegions.Clear();

        if (subscribedBus == null) return;
        subscribedBus.Unsubscribe<WorldObjectsSpawnedPublish>(OnWorldObjectsSpawned);
        subscribedBus.Unsubscribe<InventoryDataRestoredPublish>(OnInventoryDataRestored);
        subscribedBus = null;
    }

    // ── Event handlers ─────────────────────────────────────────────────────

    private void OnWorldObjectsSpawned(WorldObjectsSpawnedPublish _) => TryRunScan();

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
        string myScene = gameObject.scene.name;
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(myScene, activeScene, StringComparison.Ordinal))
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
        float currentElapsedSeconds = gameManager.TimeManager != null ? gameManager.TimeManager.CurrentTotalElapsedSeconds : 0f;
        var bounds = markerMap.cellBounds;
        int seededCount = 0;

        activeRuleRegions.Clear();
        activeRuleRegions.AddRange(CollectRuleRegions(markerMap, bounds));

        var validPersistentIds = new HashSet<string>(StringComparer.Ordinal);
        var validRuleEntryPrefixes = new HashSet<string>(StringComparer.Ordinal);
        var validRuleStateIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var cell in bounds.allPositionsWithin)
        {
            var tile = markerMap.GetTile<SceneSpawnTile>(cell);
            if (tile == null || tile.markerKind == SceneMarkerKind.PlayerSpawn) continue;

            string pid = SceneSpawnPayload.BuildPersistentId(
                sceneName, tile.markerKind, tile.objectType, cell, tile.spawnGroupId);
            validPersistentIds.Add(pid);
        }

        foreach (var region in activeRuleRegions)
        {
            if (region.tile?.entries == null)
                continue;

            foreach (var entry in region.tile.entries)
            {
                if (entry == null)
                    continue;

                string entryKey = entry.ResolveEntryKey();
                validRuleEntryPrefixes.Add(SceneSpawnPayload.BuildRuleRegionPersistentPrefix(
                    sceneName,
                    region.regionId,
                    entryKey));

                if (!ShouldPersistRuleState(entry))
                    continue;

                string stateId = SceneSpawnPayload.BuildRuleRegionStateId(
                    sceneName,
                    region.regionId,
                    entryKey);
                validRuleStateIds.Add(stateId);
            }
        }

        string scenePrefix = sceneName + ":";
        var staleRuntimeIds = new List<string>();
        foreach (var ep in worldService.GetAllPositions())
        {
            if (string.IsNullOrEmpty(ep?.persistentId) || !ep.persistentId.StartsWith(scenePrefix, StringComparison.Ordinal))
                continue;

            if (SceneSpawnPayload.IsRuleRegionPersistentId(ep.persistentId))
            {
                bool isValidRuleEntry = false;
                foreach (var prefix in validRuleEntryPrefixes)
                {
                    if (ep.persistentId.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        isValidRuleEntry = true;
                        break;
                    }
                }

                if (!isValidRuleEntry)
                    staleRuntimeIds.Add(ep.idRuntime);
                continue;
            }

            if (!validPersistentIds.Contains(ep.persistentId))
                staleRuntimeIds.Add(ep.idRuntime);
        }

        foreach (var id in staleRuntimeIds)
        {
            Debug.Log($"[SceneContentScanner] Cleaning up stale persistent entity: {id}");
            eventBus.Publish(new DestroyEntityRequestPublish(id));
        }

        worldService.CleanUpStalePersistentIds(scenePrefix, validPersistentIds);
        worldService.CleanUpStaleRuleRegionRecords(sceneName, validRuleEntryPrefixes, validRuleStateIds);

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

        foreach (var region in activeRuleRegions)
            seededCount += EvaluateRuleRegion(region, markerMap, worldService, eventBus, sceneName, currentElapsedSeconds, isInitialScan: true);

        nextRuleTickTime = Time.unscaledTime + ruleTickIntervalSeconds;

        if (seededCount > 0)
            Debug.Log($"[SceneContentScanner] Seeded {seededCount} marker entity/entities for scene '{sceneName}'.");
        else
            Debug.Log($"[SceneContentScanner] No new markers to seed in scene '{sceneName}' (all already seeded or no tiles).");
    }

    private void TickRuleRegions()
    {
        if (activeRuleRegions.Count == 0)
            return;

        string myScene = gameObject.scene.name;
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(myScene, activeScene, StringComparison.Ordinal))
            return;

        var gameManager = GameManager.Instance;
        var worldService = gameManager?.WorldService;
        var eventBus = gameManager?.EventBus;
        sceneContext ??= SceneContext.Current ?? FindAnyObjectByType<SceneContext>();
        sceneContext?.AutoBind();
        var markerMap = sceneContext?.RuntimeMarkers;
        if (worldService == null || eventBus == null || markerMap == null)
            return;

        float currentElapsedSeconds = gameManager.TimeManager != null ? gameManager.TimeManager.CurrentTotalElapsedSeconds : 0f;
        string sceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < activeRuleRegions.Count; i++)
            EvaluateRuleRegion(activeRuleRegions[i], markerMap, worldService, eventBus, sceneName, currentElapsedSeconds, isInitialScan: false);
    }

    private int EvaluateRuleRegion(
        RuleRegionRuntime region,
        Tilemap markerMap,
        WorldEntityService worldService,
        EventBus eventBus,
        string sceneName,
        float currentElapsedSeconds,
        bool isInitialScan)
    {
        if (region == null || region.tile == null || region.tile.entries == null || region.cells == null || region.cells.Count == 0)
            return 0;

        int spawned = 0;
        foreach (var entry in region.tile.entries)
            spawned += EvaluateRuleEntry(region, entry, markerMap, worldService, eventBus, sceneName, currentElapsedSeconds, isInitialScan);
        return spawned;
    }

    private int EvaluateRuleEntry(
        RuleRegionRuntime region,
        SceneSpawnRuleEntry entry,
        Tilemap markerMap,
        WorldEntityService worldService,
        EventBus eventBus,
        string sceneName,
        float currentElapsedSeconds,
        bool isInitialScan)
    {
        if (!TryResolveRuleMarker(entry, out var markerTile))
            return 0;

        string entryKey = entry.ResolveEntryKey();

        if (entry.spawnMode == SceneSpawnRuleMode.SpawnFreshOnSceneLoad)
        {
            if (!isInitialScan)
                return 0;

            int freshCount = entry.RollInitialCount();
            return SpawnRuleEntryInstances(region, entry, markerTile, freshCount, markerMap, worldService, eventBus, sceneName, entryKey);
        }

        string stateId = SceneSpawnPayload.BuildRuleRegionStateId(sceneName, region.regionId, entryKey);
        string prefix = SceneSpawnPayload.BuildRuleRegionPersistentPrefix(sceneName, region.regionId, entryKey);
        var state = worldService.GetOrCreateRegionRuleState(stateId);
        if (state == null)
            return 0;

        bool stateChanged = false;
        int spawned = 0;

        if (!state.hasInitialized)
        {
            state.hasInitialized = true;
            state.targetCount = entry.RollInitialCount();
            state.nextRespawnAtElapsedSeconds = 0f;
            stateChanged = true;

            if (state.targetCount > 0)
                spawned += SpawnRuleEntryInstances(region, entry, markerTile, state.targetCount, markerMap, worldService, eventBus, sceneName, entryKey);

            if (entry.spawnMode == SceneSpawnRuleMode.SpawnOnce)
            {
                worldService.SetRegionRuleState(state);
                return spawned;
            }
        }

        if (entry.spawnMode == SceneSpawnRuleMode.SpawnOnce)
        {
            if (stateChanged)
                worldService.SetRegionRuleState(state);
            return spawned;
        }

        int currentCount = worldService.CountEntitiesByPersistentPrefix(prefix);
        int missing = Mathf.Max(0, state.targetCount - currentCount);
        if (missing <= 0)
        {
            if (state.nextRespawnAtElapsedSeconds > 0f)
            {
                state.nextRespawnAtElapsedSeconds = 0f;
                stateChanged = true;
            }

            if (stateChanged)
                worldService.SetRegionRuleState(state);
            return spawned;
        }

        float delaySeconds = Mathf.Max(0.01f, entry.respawnDelaySeconds);
        if (state.nextRespawnAtElapsedSeconds <= 0f)
        {
            state.nextRespawnAtElapsedSeconds = currentElapsedSeconds + delaySeconds;
            stateChanged = true;
        }

        if (currentElapsedSeconds < state.nextRespawnAtElapsedSeconds)
        {
            if (stateChanged)
                worldService.SetRegionRuleState(state);
            return spawned;
        }

        int batchCount = Mathf.Max(1, entry.RollRespawnCount());
        int requestCount = Mathf.Min(missing, batchCount);
        spawned += SpawnRuleEntryInstances(region, entry, markerTile, requestCount, markerMap, worldService, eventBus, sceneName, entryKey);

        currentCount = worldService.CountEntitiesByPersistentPrefix(prefix);
        missing = Mathf.Max(0, state.targetCount - currentCount);
        state.nextRespawnAtElapsedSeconds = missing > 0
            ? currentElapsedSeconds + delaySeconds
            : 0f;
        worldService.SetRegionRuleState(state);
        return spawned;
    }

    private int SpawnRuleEntryInstances(
        RuleRegionRuntime region,
        SceneSpawnRuleEntry entry,
        SceneSpawnTile markerTile,
        int count,
        Tilemap markerMap,
        WorldEntityService worldService,
        EventBus eventBus,
        string sceneName,
        string entryKey)
    {
        if (count <= 0 || region.cells == null || region.cells.Count == 0)
            return 0;

        bool needsEntityData = markerTile.markerKind != SceneMarkerKind.Portal;
        if (needsEntityData && markerTile.entityData == null)
        {
            Debug.LogWarning($"[SceneContentScanner] Rule region entry '{entryKey}' missing EntityData.");
            return 0;
        }

        var candidateCells = new List<Vector3Int>(region.cells);
        Shuffle(candidateCells);

        int spawned = 0;
        for (int i = 0; i < candidateCells.Count && spawned < count; i++)
        {
            var cell = candidateCells[i];
            string persistentId = SceneSpawnPayload.BuildRuleRegionPersistentId(sceneName, region.regionId, entryKey, cell);
            if (worldService.HasPersistentId(persistentId))
                continue;

            if (!CanSpawnRuleMarkerAtCell(markerTile, cell, worldService))
                continue;

            var payload = new SceneSpawnPayload
            {
                sceneName = sceneName,
                markerKind = markerTile.markerKind,
                objectType = markerTile.objectType,
                cell = cell,
                spawnGroupId = entry.ResolveSpawnGroupId(),
                persistentId = persistentId,
                savePolicy = ResolveRuleSavePolicy(entry.spawnMode),
                respawnMinutes = 0,
                initialAmount = Mathf.Max(1, markerTile.initialAmount),
                availableAtGameMinute = 0,
                startStageIndex = ResolveStartStageIndex(markerTile)
            };

            Vector3 world = markerMap.GetCellCenterWorld(cell);
            eventBus.Publish(new SpawnRequestPublish(
                worldPos: new Vector2(world.x, world.y),
                idPrefab: markerTile.objectType,
                entityData: markerTile.entityData,
                spawnAmount: payload.initialAmount,
                bypassValidation: markerTile.bypassPlacementValidation,
                payload: payload));

            if (worldService.FindByPersistentId(persistentId) != null)
                spawned++;
        }

        return spawned;
    }

    private static bool CanSpawnRuleMarkerAtCell(SceneSpawnTile markerTile, Vector3Int cell, WorldEntityService worldService)
    {
        if (markerTile == null || worldService == null)
            return false;

        if (markerTile.bypassPlacementValidation)
            return true;

        if (markerTile.entityData?.placementRule == null)
            return true;

        return worldService.CanPlaceAt(markerTile.entityData.placementRule, new Vector2Int(cell.x, cell.y), out _);
    }

    private static bool TryResolveRuleMarker(SceneSpawnRuleEntry entry, out SceneSpawnTile markerTile)
    {
        markerTile = entry?.markerTile;
        if (markerTile == null)
            return false;

        if (markerTile.markerKind == SceneMarkerKind.PlayerSpawn)
        {
            Debug.LogWarning("[SceneContentScanner] SceneSpawnRuleRegionTile không hỗ trợ PlayerSpawn marker.");
            return false;
        }

        return true;
    }

    private static bool ShouldPersistRuleState(SceneSpawnRuleEntry entry)
    {
        if (entry == null)
            return false;

        return entry.spawnMode != SceneSpawnRuleMode.SpawnFreshOnSceneLoad;
    }

    private static SceneEntitySavePolicy ResolveRuleSavePolicy(SceneSpawnRuleMode mode)
    {
        switch (mode)
        {
            case SceneSpawnRuleMode.SpawnOnce:
                return SceneEntitySavePolicy.Persistent;
            case SceneSpawnRuleMode.RespawnTopUp:
                return SceneEntitySavePolicy.Regenerating;
            case SceneSpawnRuleMode.SpawnFreshOnSceneLoad:
            default:
                return SceneEntitySavePolicy.Temporary;
        }
    }

    private static List<RuleRegionRuntime> CollectRuleRegions(Tilemap markerMap, BoundsInt bounds)
    {
        var regions = new List<RuleRegionRuntime>();
        var visited = new HashSet<Vector3Int>();

        foreach (var cell in bounds.allPositionsWithin)
        {
            if (visited.Contains(cell))
                continue;

            var tile = markerMap.GetTile<SceneSpawnRuleRegionTile>(cell);
            if (tile == null)
                continue;

            var cells = FloodRuleRegion(markerMap, cell, tile, visited);
            if (cells.Count == 0)
                continue;

            regions.Add(new RuleRegionRuntime
            {
                tile = tile,
                cells = cells,
                regionId = BuildRuleRegionId(tile, cells)
            });
        }

        return regions;
    }

    private static List<Vector3Int> FloodRuleRegion(Tilemap markerMap, Vector3Int start, SceneSpawnRuleRegionTile tile, HashSet<Vector3Int> visited)
    {
        var result = new List<Vector3Int>();
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (visited.Contains(cell))
                continue;

            if (markerMap.GetTile<SceneSpawnRuleRegionTile>(cell) != tile)
                continue;

            visited.Add(cell);

            result.Add(cell);

            for (int i = 0; i < RegionDirections.Length; i++)
            {
                var next = cell + RegionDirections[i];
                if (!visited.Contains(next))
                    queue.Enqueue(next);
            }
        }

        return result;
    }

    private static string BuildRuleRegionId(SceneSpawnRuleRegionTile tile, List<Vector3Int> cells)
    {
        string baseKey = string.IsNullOrWhiteSpace(tile.regionKey) ? tile.name : tile.regionKey.Trim();
        baseKey = SanitizeRegionToken(baseKey);

        var ordered = new List<Vector3Int>(cells);
        ordered.Sort((a, b) =>
        {
            int x = a.x.CompareTo(b.x);
            if (x != 0) return x;
            int y = a.y.CompareTo(b.y);
            if (y != 0) return y;
            return a.z.CompareTo(b.z);
        });

        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < ordered.Count; i++)
            {
                hash = (hash ^ (uint)ordered[i].x) * 16777619u;
                hash = (hash ^ (uint)ordered[i].y) * 16777619u;
                hash = (hash ^ (uint)ordered[i].z) * 16777619u;
            }

            return $"{baseKey}_{ordered.Count}_{hash:X8}";
        }
    }

    private static string SanitizeRegionToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "region";

        return value.Trim().Replace(':', '_').Replace(' ', '_');
    }

    private static void Shuffle(List<Vector3Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int swap = UnityEngine.Random.Range(0, i + 1);
            (cells[i], cells[swap]) = (cells[swap], cells[i]);
        }
    }

    private static int ResolveStartStageIndex(SceneSpawnTile tile)
    {
        if (tile == null || tile.entityData == null)
            return -1;

        int stageCount = ResolveMarkerStageCount(tile.entityData);
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
                return UnityEngine.Random.Range(min, max + 1);

            default:
                return -1;
        }
    }

    private static int ResolveMarkerStageCount(EntityData entityData)
    {
        if (entityData?.modules == null)
            return 0;

        var stageModule = entityData.modules.OfType<StageModule>().FirstOrDefault();
        if (stageModule?.stages != null && stageModule.stages.Length > 0)
            return stageModule.stages.Length;

        var resourceGrowthModule = entityData.modules.OfType<ResourceGrowthModule>().FirstOrDefault();
        return resourceGrowthModule?.stages?.Length ?? 0;
    }
}
