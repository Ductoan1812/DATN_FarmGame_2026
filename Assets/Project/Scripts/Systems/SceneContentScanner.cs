using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-100)]
public class SceneContentScanner : MonoBehaviour
{
    [SerializeField] private SceneContext sceneContext;
    [SerializeField] private bool scanOnStart = true;

    private EventBus subscribedBus;
    private bool hasScanned;

    private IEnumerator Start()
    {
        if (!scanOnStart) yield break;

        while (GameManager.Instance == null || GameManager.Instance.EventBus == null || GameManager.Instance.WorldService == null)
            yield return null;

        subscribedBus = GameManager.Instance.EventBus;
        subscribedBus.Subscribe<InventoryDataRestoredPublish>(OnInventoryDataRestored);
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<InventoryDataRestoredPublish>(OnInventoryDataRestored);
            subscribedBus = null;
        }
    }

    private void OnInventoryDataRestored(InventoryDataRestoredPublish _)
    {
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
        if (markerMap == null) return;

        var gameManager = GameManager.Instance;
        var worldService = gameManager?.WorldService;
        var eventBus = gameManager?.EventBus;
        if (worldService == null || eventBus == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        int currentGameMinute = gameManager.TimeManager != null ? gameManager.TimeManager.CurrentTotalMinutes : 0;
        var bounds = markerMap.cellBounds;
        int seededCount = 0;

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

            if (tile.entityData == null)
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
