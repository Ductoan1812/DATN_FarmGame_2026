using UnityEngine;

public class ExpRewardRuntime : IModuleRuntime, IHandleEvent<DieEvent>
{
    private readonly ExpRewardModule data;

    public ExpRewardRuntime(ExpRewardModule data)
    {
        this.data = data;
    }

    public void Handle(DieEvent e)
    {
        if (data == null || data.rewardExp <= 0) return;
        if (ShouldSuppressImmatureHarvestExp(e.entity)) return;

        EntityRuntime receiver = e.killer;
        if (receiver == null && data.requireKiller)
            return;

        if (receiver == null)
        {
            Debug.LogWarning("[ExpRewardRuntime] Reward skipped because receiver is null.");
            return;
        }

        GameManager.Instance?.ProgressionService?.GrantExp(receiver, data.rewardExp, data.sourceType, e.entity);
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is ExpRewardRuntime;

    private static bool ShouldSuppressImmatureHarvestExp(EntityRuntime source)
    {
        var harvest = source?.GetModule<HarvestRuntime>();
        return harvest != null && harvest.HasGrowthGate() && !harvest.IsReadyForHarvest();
    }
}
