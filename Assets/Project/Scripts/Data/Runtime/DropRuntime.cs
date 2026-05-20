using UnityEngine;

/// <summary>
/// Xử lý drop items khi entity chết.
/// Trách nhiệm DUY NHẤT:
///   - Lắng nghe DieEvent
///   - Spawn drop items
/// </summary>
public class DropRuntime : IModuleRuntime, IHandleEvent<DieEvent>
{
    public DropEntry[] harvestDrops;

    public DropRuntime(DropModule data)
    {
        harvestDrops = data.harvestDrops;
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

        foreach (var entry in harvestDrops)
        {
            if (entry == null || entry.item == null) continue;

            // Random drop chance
            if (Random.value > entry.dropChance) continue;

            // Random amount
            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            if (amount <= 0) continue;

            var req = new SpawnRequestPublish(
                worldPos:         dropPos,
                idPrefab:         ObjectType.EntityDrop,
                entityData:       entry.item,
                spawnAmount:      amount,
                bypassValidation: true
            );

            GameManager.Instance.EventBus.Publish(req);
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

        int totalReceived = 0;

        foreach (var entry in harvestDrops)
        {
            if (entry == null || entry.item == null) continue;
            if (Random.value > entry.dropChance) continue;

            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            if (amount <= 0) continue;

            var item = entityService.Create(entry.item, amount);
            int received = inventoryService.Pickup(item, receiver);
            totalReceived += received;

            if (item != null && !item.IsEmpty)
            {
                gm.EventBus?.Publish(new SpawnRequestPublish(
                    worldPos: fallbackWorldPos,
                    idPrefab: ObjectType.EntityDrop,
                    runtime: item,
                    bypassValidation: true));
            }

            Debug.Log($"[DropRuntime] Nhận {entry.item.keyName} x{received}/{amount}.");
        }

        return totalReceived;
    }
}
