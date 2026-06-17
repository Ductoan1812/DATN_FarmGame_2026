public class DropModule : IModuleData
{
    public DropEntry[] harvestDrops;
    public DropEntry[] deathDrops;
    public bool includeHarvestDropsOnDestroyWhenHarvestable;

    public override IModuleRuntime CreateRuntime()
    {
        return new DropRuntime(this);
    }
}
