using UnityEngine;

public class ConsumableRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>
{
    private readonly ConsumableModule _data;

    public ConsumableRuntime(ConsumableModule data)
    {
        _data = data;
    }

    public void Handle(PrimaryActionEvent e)
    {
        if (_data == null || e.actor == null || e.item == null)
            return;

        bool changed = false;
        changed |= Restore(e.actor, StatType.Hp, StatType.MaxHp, _data.restoreHp);
        changed |= Restore(e.actor, StatType.Stamina, StatType.MaxStamina, _data.restoreStamina);
        changed |= Restore(e.actor, StatType.Mp, StatType.MaxMp, _data.restoreMp);

        if (!changed && !_data.destroyOnUse)
            return;

        if (_data.destroyOnUse)
        {
            GameManager.Instance?.InventoryService?.Consume(
                e.item,
                e.actor,
                Mathf.Max(1, _data.consumeAmount));
        }

        GameManager.Instance?.EventBus?.Publish(new InventoryVisualRefreshRequestPublish());
    }

    private static bool Restore(EntityRuntime target, StatType currentType, StatType maxType, float amount)
    {
        if (target?.stats == null || amount <= 0f)
            return false;

        float current = target.stats.Get(currentType);
        float max = target.stats.Has(maxType) ? target.stats.Get(maxType) : 0f;
        float next = max > 0f
            ? Mathf.Clamp(current + amount, 0f, max)
            : current + amount;

        if (Mathf.Approximately(next, current))
            return false;

        target.stats.Set(currentType, next);
        return true;
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is ConsumableRuntime;
}
