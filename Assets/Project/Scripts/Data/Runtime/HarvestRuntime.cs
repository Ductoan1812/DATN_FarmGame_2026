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

        TryHarvest(e.initiator, data.destroyOnHarvest);
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

        if (!data.TryGetToolBehavior(e.toolType, out _, out _, out _))
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
        return TryHarvest(interactor, data.destroyOnHarvest);
    }

    public bool TryHarvest(EntityRuntime interactor, bool destroyOnHarvest)
    {
        var stage = _entity.GetModule<StageRuntime>();
        var resourceGrowth = _entity.GetModule<ResourceGrowthRuntime>();
        bool hasGrowthGate = stage != null || resourceGrowth != null;
        if (hasGrowthGate && !IsHarvestable()) return false;

        if (stage != null && stage.IsRegrowable)
            Debug.Log($"[HarvestRuntime] '{_entity.entityData?.keyName}' dùng fallback regrowStageIndex cũ.");

        bool dropsAlreadyHandled = GrantHarvestDrops(interactor);
        if (stage != null && stage.TryAdvanceAfterHarvest())
        {
            Debug.Log($"[HarvestRuntime] '{_entity.entityData?.keyName}' harvest xong chuyển sang stage {stage.currentStageIndex}.");
            return true;
        }

        if (!destroyOnHarvest)
            return true;

        _entity.TriggerEvent(new DieEvent(_entity, interactor, suppressWorldDrops: dropsAlreadyHandled));
        return true;
    }

    public bool TryRegrowableHarvest(EntityRuntime interactor)
    {
        var stage = _entity.GetModule<StageRuntime>();
        if (stage == null) return false;

        GrantHarvestDrops(interactor);
        bool transitioned = stage.TryAdvanceAfterHarvest();
        if (transitioned)
            Debug.Log($"[HarvestRuntime] '{_entity.entityData?.keyName}' harvest xong chuyển sang stage {stage.currentStageIndex}.");
        return transitioned;
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

    public bool TryHandleDamageHarvest(TakeDamageEvent e)
    {
        if (_entity == null)
            return false;

        if (!data.HasAnyToolHarvest ||
            !data.TryGetToolBehavior(e.toolType, out bool causesDamage, out bool destroyOnHarvestForTool, out bool oneHitDestroyForTool))
            return false;

        if (!IsHarvestable())
            return false;

        if (causesDamage)
        {
            if (!oneHitDestroyForTool)
                return false;

            _entity.TriggerEvent(new DieEvent(_entity, e.attacker));
            return true;
        }

        return TryHarvest(e.attacker, destroyOnHarvestForTool);
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
               && data.harvestCausesDamage == o.data.harvestCausesDamage
               && data.destroyOnHarvest == o.data.destroyOnHarvest
               && data.oneHitDestroy == o.data.oneHitDestroy
               && HaveSameAdditionalTools(data.additionalHarvestTools, o.data.additionalHarvestTools);
    }

    private static bool HaveSameAdditionalTools(HarvestToolOption[] left, HarvestToolOption[] right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return left == null && right == null;

        if (left.Length != right.Length)
            return false;

        for (int i = 0; i < left.Length; i++)
        {
            HarvestToolOption leftOption = left[i];
            HarvestToolOption rightOption = right[i];
            if (leftOption == null || rightOption == null)
            {
                if (leftOption != rightOption)
                    return false;
                continue;
            }

            if (leftOption.toolType != rightOption.toolType ||
                leftOption.harvestCausesDamage != rightOption.harvestCausesDamage ||
                leftOption.destroyOnHarvest != rightOption.destroyOnHarvest ||
                leftOption.oneHitDestroy != rightOption.oneHitDestroy)
                return false;
        }

        return true;
    }
}
