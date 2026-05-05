using UnityEngine;

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

        if (!Validate(actorGO, e)) return;

        // Lấy trigger name (default = ToolType.ToString())
        var trigger = _data.GetAnimTrigger();

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

    // ── Save / Load ───────────────────────────────────────────────────────────

    public virtual ModuleSaveData ToSaveData() => null;
    public virtual void ApplySaveData(ModuleSaveData save) { }
    public virtual bool Equals(IModuleRuntime other) => other?.GetType() == GetType();
}
