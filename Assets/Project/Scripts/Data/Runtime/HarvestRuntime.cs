using UnityEngine;


public class HarvestRuntime : IModuleRuntime, IHandleEvent<TakeDamageEvent>, IHandleEvent<SecondaryActionEvent>, IHandleEvent<SpawnedEvent>
{
    public HarvestModule data;
    private EntityRuntime _entity;

    public HarvestRuntime(HarvestModule data)
    {
        this.data = data;
    }

    public void Handle(SpawnedEvent e)
    {
        _entity = e.entity;
    }

    public void Handle(TakeDamageEvent e)
    {
        if (_entity == null) return;
        if (data.harvestTool == ToolType.None)
        {
            Debug.Log("[HarvestRuntime] Entity này thu hoạch bằng tay, không nhận damage.");
            var health = _entity.GetModule<HealthRuntime>();
            if (health != null) health.CanTakeDamage = false;
            return;
        }
        if (e.toolType != data.harvestTool)
        {
            Debug.Log($"[HarvestRuntime] Sai tool. Cần {data.harvestTool}, dùng {e.toolType}");
            var health = _entity.GetModule<HealthRuntime>();
            if (health != null) health.CanTakeDamage = false;
            return;
        }

        // Kiểm tra stage có cho phép thu hoạch không
        if (!IsHarvestable())
        {
            Debug.Log($"[HarvestRuntime] Chưa đến giai đoạn thu hoạch.");
            var health = _entity.GetModule<HealthRuntime>();
            if (health != null) health.CanTakeDamage = false;
            return;
        }

        var hp = _entity.GetModule<HealthRuntime>();
        if (hp != null) hp.CanTakeDamage = true;
    }

    public void Handle(SecondaryActionEvent e)
    {
        if (_entity == null) return;
        if (data.harvestTool != ToolType.None)
        {
            Debug.Log($"[HarvestRuntime] Cần tool: {data.harvestTool}");
            return;
        }

        // Kiểm tra stage có cho phép thu hoạch không
        if (!IsHarvestable())
        {
            Debug.Log($"[HarvestRuntime] Chưa đến giai đoạn thu hoạch.");
            return;
        }

        _entity.TriggerEvent(new DieEvent(_entity));
    }

    /// <summary>
    /// Kiểm tra entity có ở trạng thái thu hoạch được không.
    /// Nếu entity không có StageRuntime → luôn cho phép (entity không có stage = luôn harvestable).
    /// Nếu có StageRuntime → check CanHarvest của stage hiện tại.
    /// </summary>
    private bool IsHarvestable()
    {
        var stage = _entity.GetModule<StageRuntime>();
        if (stage == null) return true; // Không có stage → luôn cho phép
        return stage.CanHarvest;
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() => null;

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not HarvestRuntime o) return false;
        return data.harvestTool == o.data.harvestTool;
    }
}
