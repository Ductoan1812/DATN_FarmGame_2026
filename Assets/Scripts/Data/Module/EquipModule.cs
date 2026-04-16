public class EquipModule : IModuleData
{
    public EquipSlot equipSlot;  

    public override IModuleRuntime CreateRuntime()
    {
        return new EquipRuntime(this);
    }
}