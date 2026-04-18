using UnityEngine;

/// <summary>
/// Xử lý drop items + despawn khi entity chết.
/// Trách nhiệm DUY NHẤT:
///   - Lắng nghe DieEvent
///   - Spawn drop items
///   - Publish DespawnRequest (xóa GO khỏi world)
/// </summary>
public class DropRuntime : IModuleRuntime, IHandleEvent<DieEvent>
{
    public DropEntry[] harvestDrops;

    public DropRuntime(DropModule data)
    {
        harvestDrops = data.harvestDrops;
    }

    public void Handle(DieEvent e)
    {
        // ── Drop items ────────────────────────────────────────────────────────
        if (harvestDrops != null && harvestDrops.Length > 0)
        {
            var owner = e.entity?.Owner;
            var go = owner?.GameObject;

            if (go != null)
            {
                var pos3 = go.transform.position;
                var dropPos = new Vector2(pos3.x, pos3.y);

                foreach (var entry in harvestDrops)
                {
                    if (entry == null || entry.item == null) continue;
                    if (Random.value > entry.dropChance) continue;

                    int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                    if (amount <= 0) continue;

                    var req = new SpawnRequest(
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

        // ── Despawn entity khỏi world ─────────────────────────────────────────
        if (e.entity != null)
        {
            var req = new DespawnRequest(e.entity.Id);
            GameManager.Instance?.EventBus?.Publish(req);
        }
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Drop", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => true;
}
