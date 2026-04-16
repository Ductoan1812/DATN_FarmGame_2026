using UnityEngine;
using System;

public class HarvestRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>
{
    public HarvestModule data;
    private EntityRuntime _entity;

    public event Action OnDied;

    public HarvestRuntime(HarvestModule data)
    {
        this.data = data;
    }

    // ── Khởi tạo Hp khi entity được spawn ──
    public void Handle(SpawnedEvent e)
    {
        _entity = e.entity;
        float maxHp = _entity.stats.Get(StatType.MaxHp);
        if (maxHp > 0)
            _entity.stats.Set(StatType.Hp, maxHp);
    }

    // ── Harvest không cần tool (chuột phải trực tiếp) ──
    public void TryHarvest()
    {
        if (data.harvestTool != ToolType.None)
        {
            Debug.Log($"[HarvestRuntime] Cần tool: {data.harvestTool}");
            return;
        }
        Die();
    }

    // ── Nhận sát thương từ tool ──
    public bool ApplyDamage(int damage, ToolType toolType)
    {
        if (data.harvestTool == ToolType.None)
        {
            Debug.Log("[HarvestRuntime] Entity này không cần tool, dùng TryHarvest.");
            return false;
        }

        if (data.harvestTool != toolType)
        {
            Debug.Log($"[HarvestRuntime] Sai tool. Cần {data.harvestTool}, dùng {toolType}");
            return false;
        }

        _entity.stats.AddFlat(StatType.Hp, -damage);

        if (_entity.stats.Get(StatType.Hp) <= 0)
        {
            Die();
            return true;
        }
        return false;
    }

    private void Die()
    {
        OnDied?.Invoke();
        _entity.TriggerEvent(new DoDropEvent(_entity));
    }

    // ── Save / Load ──
    public ModuleSaveData ToSaveData()
    {
        return new ModuleSaveData { moduleType = "Harvest", dataJson = string.Empty };
    }

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not HarvestRuntime o) return false;
        return data.harvestTool == o.data.harvestTool;
    }
}
