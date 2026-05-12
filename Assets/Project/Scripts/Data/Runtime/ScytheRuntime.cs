using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tool Scythe: quét damage tất cả entity trong tầm.
/// Validate: luôn cho phép vung.
/// Execute: gây damage (gọi tại frame "Hit" của animation).
/// </summary>
public class ScytheRuntime : ToolRuntime
{
    public ScytheRuntime(ToolModule data) : base(data) { }

    protected override bool Validate(GameObject actorGO, PrimaryActionEvent e)
    {
        return true;
    }

    protected override void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item)
    {
        float range  = item?.stats.Get(StatType.Range) ?? 1.5f;
        float damage = item?.stats.Get(StatType.Attack) ?? 1f;
        if (range <= 0f) range = 1.5f;
        if (damage <= 0f) damage = 1f;

        List<EntityRuntime> targets = EntityScanSystem.GetAll(actorGO, range);

        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[ScytheRuntime] Không có target trong tầm.");
            return;
        }

        foreach (var target in targets)
        {
            target.TriggerEvent(new TakeDamageEvent(item, damage, ToolType.Scythe));
        }
    }
}
