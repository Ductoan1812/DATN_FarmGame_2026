using UnityEngine;


public class HarvestRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>, IHandleEvent<SpawnedEvent>
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

        bool grantedDirectly = TryGrantDropsToInteractor(e.initiator);
        _entity.TriggerEvent(new DieEvent(_entity, e.initiator, suppressWorldDrops: grantedDirectly));
    }

    public bool CanReceiveDamage(TakeDamageEvent e, out string reason)
    {
        reason = string.Empty;
        if (_entity == null) return true;

        if (data.harvestTool == ToolType.None)
        {
            reason = "[HarvestRuntime] Entity này thu hoạch bằng tay, không nhận damage.";
            return false;
        }

        if (e.toolType != data.harvestTool)
        {
            reason = $"[HarvestRuntime] Sai tool. Cần {data.harvestTool}, dùng {e.toolType}";
            return false;
        }

        if (!IsHarvestable())
        {
            reason = "[HarvestRuntime] Chưa đến giai đoạn thu hoạch.";
            return false;
        }

        return true;
    }

    private bool TryGrantDropsToInteractor(EntityRuntime interactor)
    {
        if (interactor == null) return false;

        var drop = _entity.GetModule<DropRuntime>();
        if (drop == null) return false;

        Vector2 position = Vector2.zero;
        var go = _entity.Owner?.GameObject;
        if (go != null)
            position = go.transform.position;

        int received = drop.GrantDropsTo(interactor, position);
        return received > 0;
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
