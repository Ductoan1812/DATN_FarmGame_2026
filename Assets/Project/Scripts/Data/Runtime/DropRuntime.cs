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
}
