public class HarvestModule : IModuleData
{
    public ToolType harvestTool = ToolType.None;

    public override IModuleRuntime CreateRuntime()
    {
        return new HarvestRuntime(this);
    }
}
