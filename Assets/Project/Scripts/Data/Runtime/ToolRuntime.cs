using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class cho tất cả tool runtime.
///
/// Flow (animation-driven, generic):
///   1. PrimaryActionEvent → Validate()
///   2. Nếu OK → tìm ToolActionBridge trên actor → bridge.Request()
///      → play animation
///   3. AnimationEvent("Strike") → bridge fire AnimStrikeEvent lên item
///      → Execute()
///
/// Subclass chỉ cần override Validate() và Execute().
/// animTrigger lấy từ ToolModule.animTrigger (config trong Inspector).
/// </summary>
public abstract class ToolRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>, IHandleEvent<AnimStrikeEvent>
{
    protected readonly ToolModule _data;

    protected ToolRuntime(ToolModule data)
    {
        _data = data;
    }

    // ── PrimaryAction: Validate → request animation ───────────────────────────

    public void Handle(PrimaryActionEvent e)
    {
        if (e.actor == null) return;

        var actorGO = e.actor.Owner?.GameObject;
        if (actorGO == null) return;

        if (!Validate(actorGO, e))
            return;

        // Lấy trigger name phù hợp với action đã validate
        var trigger = GetAnimationTrigger(e);

        // Tìm bridge trên actor → play animation
        var bridge = actorGO.GetComponent<ToolActionBridge>();
        if (bridge == null || bridge.IsBusy)
            return;

        bridge.Request(e.actor, e.item, trigger);
    }

    // ── AnimStrike: animation đến frame Strike → execute logic ─────────────────

    public void Handle(AnimStrikeEvent e)
    {
        if (e.actor == null) return;

        var actorGO = e.actor.Owner?.GameObject;
        if (actorGO == null) return;

        Execute(actorGO, e.actor, e.item);
    }

    // ── Abstract ──────────────────────────────────────────────────────────────

    protected abstract bool Validate(GameObject actorGO, PrimaryActionEvent e);
    protected abstract void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item);
    protected virtual string GetAnimationTrigger(PrimaryActionEvent e) => _data.GetAnimTrigger();

    // ── Save / Load ───────────────────────────────────────────────────────────

    public virtual ModuleSaveData ToSaveData() => null;
    public virtual void ApplySaveData(ModuleSaveData save) { }
    public virtual bool Equals(IModuleRuntime other) => other?.GetType() == GetType();
}

/// <summary>
/// Shared runtime cho các tool gây damage trực tiếp lên entity world.
/// Dùng để tránh duplicate logic giữa Scythe/Axe/Pickaxe.
/// </summary>
public abstract class DamageToolRuntime : ToolRuntime
{
    private readonly ToolType damageToolType;
    private readonly bool hitAllTargets;
    private readonly float defaultRange;
    private readonly float defaultDamage;

    protected DamageToolRuntime(
        ToolModule data,
        ToolType damageToolType,
        bool hitAllTargets,
        float defaultRange = 1.5f,
        float defaultDamage = 1f) : base(data)
    {
        this.damageToolType = damageToolType;
        this.hitAllTargets = hitAllTargets;
        this.defaultRange = defaultRange;
        this.defaultDamage = defaultDamage;
    }

    protected override bool Validate(GameObject actorGO, PrimaryActionEvent e)
    {
        return true;
    }

    protected override void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item)
    {
        float range = item?.stats.Get(StatType.Range) ?? defaultRange;
        float damage = item?.stats.Get(StatType.Attack) ?? defaultDamage;
        if (range <= 0f) range = defaultRange;
        if (damage <= 0f) damage = defaultDamage;

        List<EntityRuntime> targets = EntityScanSystem.GetAll(actorGO, range);
        if (targets == null || targets.Count == 0)
            return;

        if (hitAllTargets)
        {
            foreach (var target in targets)
                ApplyDamage(target, actor, damage);
            return;
        }

        var nearest = FindNearest(actorGO.transform.position, targets);
        if (nearest != null)
            ApplyDamage(nearest, actor, damage);
    }

    private void ApplyDamage(EntityRuntime target, EntityRuntime actor, float damage)
    {
        if (target == null) return;
        target.TriggerEvent(new TakeDamageEvent(actor, damage, damageToolType));
    }

    private static EntityRuntime FindNearest(Vector3 from, List<EntityRuntime> targets)
    {
        EntityRuntime nearest = null;
        float bestDistance = float.MaxValue;

        foreach (var target in targets)
        {
            var targetGo = target?.Owner?.GameObject;
            if (targetGo == null) continue;

            float distance = Vector2.Distance(from, targetGo.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }
}
