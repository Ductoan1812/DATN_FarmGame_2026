using UnityEngine;
using System.Collections.Generic;

public class AttackRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>
{
    private readonly AttackModule _data;
    private float _lastAttackTime = -999f;

    public AttackRuntime(AttackModule data)
    {
        _data = data;
    }

    public void Handle(PrimaryActionEvent e)
    {
        var weapon = e.item;
        if (weapon == null) return;
        var actorGO = e.actor?.Owner?.GameObject;
        if (actorGO == null)
        {
            Debug.LogWarning("[AttackRuntime] actor.Owner.GameObject null.");
            return;
        }
        float cooldown = _data.attackCooldown;
        float range    = _data.attackRange;

        float wCd    = weapon.stats.Get(StatType.CoolDown);
        float wRange = weapon.stats.Get(StatType.Range);
        if (wCd > 0f) cooldown = wCd;
        if (wRange > 0f) range = wRange;
        if (Time.time - _lastAttackTime < cooldown) return;
        float attack     = weapon.stats.Get(StatType.Attack);
        float critChance = weapon.stats.Get(StatType.CritChance);
        float critDamage = weapon.stats.Get(StatType.CritDamage);

        bool isCrit       = Random.value < Mathf.Clamp01(critChance);
        float finalDamage = isCrit ? attack * (1f + critDamage) : attack;
        List<EntityRuntime> targets = EntityScanSystem.GetAll(actorGO, range);

        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[AttackRuntime] Không có target trong tầm.");
            _lastAttackTime = Time.time;
            return;
        }
        foreach (var target in targets)
        {
            if (target == null) continue;
            if (target.GetModule<HealthRuntime>() == null) continue;
            if (target.GetModule<HarvestRuntime>() != null) continue;
            target.TriggerEvent(new TakeDamageEvent(e.actor, finalDamage, sourceItem: weapon, isCrit: isCrit));
        }

        _lastAttackTime = Time.time;
    }


    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is AttackRuntime;
}
