using UnityEngine;

/// <summary>
/// Xử lý drop items khi entity chết.
/// Trách nhiệm DUY NHẤT:
///   - Lắng nghe DieEvent
///   - Spawn drop items
/// </summary>
public class DropRuntime : IModuleRuntime, IHandleEvent<DieEvent>, IHandleEvent<SpawnedEvent>
{
    public DropEntry[] harvestDrops;
    private EntityRuntime sourceEntity;

    public DropRuntime(DropModule data)
    {
        harvestDrops = data.harvestDrops;
    }

    public void Handle(SpawnedEvent e)
    {
        sourceEntity = e.entity;
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Drop", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => true;

    public void Handle(DieEvent e)
    {
        if (e.suppressWorldDrops) return;
        if (harvestDrops == null || harvestDrops.Length == 0) return;

        var owner = e.entity?.Owner;
        if (owner == null) return;

        var go = owner.GameObject;
        if (go == null) return;

        var pos3 = go.transform.position;
        var dropPos = new Vector2(pos3.x, pos3.y);
        var gm = GameManager.Instance;
        var entityService = gm?.EntityService;
        int quality = GetDropQuality(e.entity);

        foreach (var entry in harvestDrops)
        {
            if (entry == null || entry.item == null) continue;

            // Random drop chance
            if (Random.value > entry.dropChance) continue;

            // Random amount
            int totalAmount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            if (totalAmount <= 0) continue;

            SpawnWorldDrops(dropPos, entry.item, totalAmount, quality, gm, entityService);
        }
    }

    public int GrantDropsTo(EntityRuntime receiver, Vector2 fallbackWorldPos)
    {
        if (receiver == null || harvestDrops == null || harvestDrops.Length == 0)
            return 0;

        var gm = GameManager.Instance;
        var entityService = gm?.EntityService;
        var inventoryService = gm?.InventoryService;
        if (entityService == null || inventoryService == null)
            return 0;

        int quality = GetDropQuality(sourceEntity);
        int totalHandled = 0;

        foreach (var entry in harvestDrops)
        {
            if (entry == null || entry.item == null) continue;
            if (Random.value > entry.dropChance) continue;

            int totalAmount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            if (totalAmount <= 0) continue;

            int remainingToGrant = totalAmount;
            int entryReceived = 0;
            int stackSize = Mathf.Max(1, entry.item.maxStack);

            while (remainingToGrant > 0)
            {
                int chunkAmount = Mathf.Min(remainingToGrant, stackSize);
                var chunk = entityService.Create(entry.item, chunkAmount);
                chunk.SetQuality(quality);

                int received = inventoryService.Pickup(chunk, receiver);
                entryReceived += received;
                remainingToGrant -= chunkAmount;

                int leftoverAmount = Mathf.Max(0, chunkAmount - received);
                if (leftoverAmount <= 0)
                {
                    totalHandled += chunkAmount;
                    continue;
                }

                if (chunk.IsEmpty || ReferenceEquals(chunk.Owner, receiver.Owner))
                {
                    chunk = entityService.Create(entry.item, leftoverAmount);
                    chunk.SetQuality(quality);
                }

                SpawnWorldDropRuntime(fallbackWorldPos, chunk, gm);
                totalHandled += chunkAmount;
            }

            Debug.Log($"[DropRuntime] Nhận {entry.item.keyName} x{entryReceived}/{totalAmount} (quality {quality}).");
        }

        return totalHandled;
    }

    public int SpawnDropsToWorld(Vector2 worldPos)
    {
        if (harvestDrops == null || harvestDrops.Length == 0)
            return 0;

        var gm = GameManager.Instance;
        var entityService = gm?.EntityService;
        int quality = GetDropQuality(sourceEntity);
        int totalSpawned = 0;

        foreach (var entry in harvestDrops)
        {
            if (entry == null || entry.item == null) continue;
            if (Random.value > entry.dropChance) continue;

            int totalAmount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            if (totalAmount <= 0) continue;

            totalSpawned += totalAmount;
            SpawnWorldDrops(worldPos, entry.item, totalAmount, quality, gm, entityService);
        }

        return totalSpawned;
    }

    private static int GetDropQuality(EntityRuntime source)
    {
        return source?.GetModule<QualityRuntime>()?.GetHarvestQuality() ?? 1;
    }

    private static void SpawnWorldDrops(
        Vector2 worldPos,
        EntityData itemData,
        int totalAmount,
        int quality,
        GameManager gm,
        EntityService entityService)
    {
        if (gm?.EventBus == null || itemData == null || totalAmount <= 0)
            return;

        int stackSize = Mathf.Max(1, itemData.maxStack);
        int remaining = totalAmount;

        while (remaining > 0)
        {
            int chunkAmount = Mathf.Min(remaining, stackSize);
            remaining -= chunkAmount;

            if (entityService != null)
            {
                var item = entityService.Create(itemData, chunkAmount);
                item.SetQuality(quality);
                SpawnWorldDropRuntime(worldPos, item, gm);
            }
            else
            {
                gm.EventBus.Publish(new SpawnRequestPublish(
                    worldPos: worldPos,
                    idPrefab: ObjectType.EntityDrop,
                    entityData: itemData,
                    spawnAmount: chunkAmount,
                    bypassValidation: true));
            }
        }
    }

    private static void SpawnWorldDropRuntime(Vector2 worldPos, EntityRuntime runtime, GameManager gm)
    {
        if (runtime == null || gm?.EventBus == null)
            return;

        gm.EventBus.Publish(new SpawnRequestPublish(
            worldPos: worldPos,
            idPrefab: ObjectType.EntityDrop,
            runtime: runtime,
            bypassValidation: true));
    }
}
