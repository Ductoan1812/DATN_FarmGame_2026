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
        if (!data.AllowsHandHarvest)
            return;

        // Kiểm tra stage có cho phép thu hoạch không
        if (!IsHarvestable())
            return;

        TryHarvest(e.initiator);
    }

    public bool CanReceiveDamage(TakeDamageEvent e, out string reason)
    {
        reason = string.Empty;
        if (_entity == null) return true;

        if (!data.HasAnyToolHarvest)
        {
            reason = data.AllowsHandHarvest ? "Harvest by hand only." : "No harvest tool configured.";
            return false;
        }

        if (!data.AllowsTool(e.toolType))
        {
            reason = $"Wrong tool: got {e.toolType}.";
            return false;
        }

        if (!IsHarvestable())
        {
            reason = "Not harvestable yet.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Try harvest: grant drops, then either reset regrow stage or die for non-regrow crops.
    /// Returns true if handled.
    /// </summary>
    public bool TryHarvest(EntityRuntime interactor)
    {
        var stage = _entity.GetModule<StageRuntime>();
        var resourceGrowth = _entity.GetModule<ResourceGrowthRuntime>();
        bool hasGrowthGate = stage != null || resourceGrowth != null;
        if (hasGrowthGate && !IsHarvestable()) return false;

        if (stage != null && stage.IsRegrowable)
        {
            GrantHarvestDrops(interactor);
            stage.ResetToRegrowStage();
            Debug.Log($"[HarvestRuntime] Regrowable crop reset to stage {stage.currentStageIndex}.");
            return true;
        }

        bool dropsAlreadyHandled = GrantHarvestDrops(interactor);
        _entity.TriggerEvent(new DieEvent(_entity, interactor, suppressWorldDrops: dropsAlreadyHandled));
        return true;
    }

    public bool TryRegrowableHarvest(EntityRuntime interactor)
    {
        var stage = _entity.GetModule<StageRuntime>();
        if (stage == null || !stage.IsRegrowable) return false;

        GrantHarvestDrops(interactor);
        stage.ResetToRegrowStage();
        Debug.Log($"[HarvestRuntime] Regrowable crop reset to stage {stage.currentStageIndex}.");
        return true;
    }

    private bool GrantHarvestDrops(EntityRuntime interactor)
    {
        var drop = _entity.GetModule<DropRuntime>();
        if (drop == null) return false;

        Vector2 position = Vector2.zero;
        var go = _entity.Owner?.GameObject;
        if (go != null)
            position = go.transform.position;

        if (data.dropMode == HarvestDropMode.World)
            return drop.SpawnDropsToWorld(position) > 0;

        if (interactor == null) return false;

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
        if (stage != null) return stage.CanHarvest;

        var resourceGrowth = _entity.GetModule<ResourceGrowthRuntime>();
        if (resourceGrowth != null) return resourceGrowth.CanHarvest;

        return true;
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() => null;

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not HarvestRuntime o) return false;
        return data.harvestTool == o.data.harvestTool
               && data.allowHandHarvest == o.data.allowHandHarvest
               && data.dropMode == o.data.dropMode
               && HaveSameAdditionalTools(data.additionalHarvestTools, o.data.additionalHarvestTools);
    }

    private static bool HaveSameAdditionalTools(ToolType[] left, ToolType[] right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return left == null && right == null;

        if (left.Length != right.Length)
            return false;

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }
}
