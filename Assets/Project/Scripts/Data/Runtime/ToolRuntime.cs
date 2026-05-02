using UnityEngine;

public abstract class ToolRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>
{
    protected readonly ToolModule _data;
    private float _lastUseTime = -999f;

    protected ToolRuntime(ToolModule data)
    {
        _data = data;
    }

    public void Handle(PrimaryActionEvent e)
    {
        if (e.actor == null) return;
        float cd = e.item?.stats.Get(StatType.CoolDown) ?? 0.3f;
        if (cd <= 0f) cd = 0.3f;
        if (Time.time - _lastUseTime < cd) return;
        var actorGO = e.actor.Owner?.GameObject;
        if (actorGO == null)
        {
            Debug.LogWarning($"[{GetType().Name}] actor.Owner.GameObject null.");
            return;
        }
        if (!Execute(actorGO, e)) return;

        _lastUseTime = Time.time;
    }

    protected abstract bool Execute(GameObject actorGO, PrimaryActionEvent e);

    // ── Save / Load ───────────────────────────────────────────────────────────

    public virtual ModuleSaveData ToSaveData() => null;
    public virtual void ApplySaveData(ModuleSaveData save) { }
    public virtual bool Equals(IModuleRuntime other) => other?.GetType() == GetType();
}
