using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime vũ khí: validate cooldown/stamina, request animation, rồi gây damage ở frame Strike.
/// Không xử lý harvest/resource để giữ tool gating rõ ràng.
/// </summary>
public class WeaponRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>, IHandleEvent<AnimStrikeEvent>
{
    private readonly WeaponModule data;
    private float lastAttackTime = -999f;

    public WeaponRuntime(WeaponModule data)
    {
        this.data = data;
    }

    public void Handle(PrimaryActionEvent e)
    {
        if (e.actor == null || e.item == null) return;
        if (Time.time - lastAttackTime < ResolveCooldown(e.item)) return;

        var actorGo = e.actor.Owner?.GameObject;
        if (actorGo == null) return;

        // ── Stamina check qua StaminaCostModule của item ─────────────────────────
        var staminaRuntime = e.item?.GetModule<StaminaCostRuntime>();
        if (staminaRuntime != null && !staminaRuntime.CanAfford(e.actor))
        {
            Debug.Log("[WeaponRuntime] Không đủ thể lực để tấn công.");
            return;
        }
        staminaRuntime?.Spend(e.actor);

        var bridge = actorGo.GetComponent<ToolActionBridge>();
        if (bridge == null || bridge.IsBusy)
            return;

        lastAttackTime = Time.time;
        bridge.Request(e.actor, e.item, data.GetAnimTrigger());
    }

    public void Handle(AnimStrikeEvent e)
    {
        if (e.actor == null || e.item == null) return;

        var actorGo = e.actor.Owner?.GameObject;
        if (actorGo == null) return;

        float range = ResolveRange(e.item);
        float damage = ResolveDamage(e.item);
        List<EntityRuntime> targets = EntityScanSystem.GetAll(actorGo, range);
        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[WeaponRuntime] Không có mục tiêu trong tầm.");
            return;
        }

        EntityRuntime target = FindNearestCombatTarget(actorGo.transform.position, targets);
        if (target == null)
        {
            Debug.Log("[WeaponRuntime] Không có mục tiêu chiến đấu hợp lệ.");
            return;
        }

        target.TriggerEvent(new TakeDamageEvent(e.actor, damage));
        ApplyKnockback(actorGo, target, data.knockback);
    }

    private float ResolveRange(EntityRuntime item)
    {
        float value = item?.stats.Get(StatType.Range) ?? 0f;
        return value > 0f ? value : Mathf.Max(0.1f, data.baseRange);
    }

    private float ResolveDamage(EntityRuntime item)
    {
        float value = item?.stats.Get(StatType.Attack) ?? 0f;
        return value > 0f ? value : Mathf.Max(1f, data.baseDamage);
    }

    private float ResolveCooldown(EntityRuntime item)
    {
        float value = item?.stats.Get(StatType.CoolDown) ?? 0f;
        return value > 0f ? value : Mathf.Max(0.05f, data.cooldown);
    }


    private static EntityRuntime FindNearestCombatTarget(Vector3 from, List<EntityRuntime> targets)
    {
        EntityRuntime nearest = null;
        float best = float.MaxValue;

        foreach (var target in targets)
        {
            if (target == null) continue;
            if (target.GetModule<HealthRuntime>() == null) continue;
            if (target.GetModule<HarvestRuntime>() != null) continue;

            var go = target.Owner?.GameObject;
            if (go == null) continue;

            float distance = Vector2.Distance(from, go.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    private static void ApplyKnockback(GameObject actorGo, EntityRuntime target, float force)
    {
        if (actorGo == null || target?.Owner?.GameObject == null || force <= 0f)
            return;

        var targetGo = target.Owner.GameObject;
        Vector2 direction = ((Vector2)targetGo.transform.position - (Vector2)actorGo.transform.position).normalized;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.up;

        var rb = targetGo.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        else
            targetGo.transform.position += (Vector3)(direction * force * 0.08f);
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is WeaponRuntime;
}
