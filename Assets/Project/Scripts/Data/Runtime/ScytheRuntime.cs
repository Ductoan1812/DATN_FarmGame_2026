using UnityEngine;
using System.Collections.Generic;


public class ScytheRuntime : ToolRuntime
{
    public ScytheRuntime(ToolModule data) : base(data) { }

    protected override bool Execute(GameObject actorGO, PrimaryActionEvent e)
    {
        var item = e.item;
        float range  = item?.stats.Get(StatType.Range) ?? 1.5f;
        float damage = item?.stats.Get(StatType.Attack) ?? 1f;
        if (range <= 0f) range = 1.5f;
        if (damage <= 0f) damage = 1f;

        List<EntityRuntime> targets = EntityScanSystem.GetAll(actorGO, range);

        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[ScytheRuntime] Không có target trong tầm.");
            return true;
        }

        foreach (var target in targets)
        {
            target.TriggerEvent(new TakeDamageEvent(item, damage, ToolType.Scythe));
        }
        return true;
    }
}
