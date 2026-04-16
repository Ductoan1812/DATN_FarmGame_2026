public class DropModule : IModuleData
{
    public DropEntry[] harvestDrops;  

    public override IModuleRuntime CreateRuntime()
    {
        return new DropRuntime(this);
    }
}