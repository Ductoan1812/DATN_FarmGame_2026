using UnityEngine;

[System.Serializable]
public class ExpRewardModule : IModuleData
{
    [Min(0)] public int rewardExp;
    public ExpSourceType sourceType = ExpSourceType.Other;
    public bool requireKiller = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new ExpRewardRuntime(this);
    }
}
