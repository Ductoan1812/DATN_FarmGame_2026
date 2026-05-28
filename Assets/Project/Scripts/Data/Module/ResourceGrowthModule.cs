using UnityEngine;

[System.Serializable]
public class ResourceGrowthModule : IModuleData
{
    public GrowthStage[] stages;

    public override IModuleRuntime CreateRuntime()
    {
        return new ResourceGrowthRuntime(this);
    }
}
