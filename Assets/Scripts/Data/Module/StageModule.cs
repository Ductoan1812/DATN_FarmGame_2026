public class StageModule :IModuleData 
{
    public GrowthStage[] stages;
    public override IModuleRuntime CreateRuntime()
    {
        return new StageRuntime(this);
    }
}
