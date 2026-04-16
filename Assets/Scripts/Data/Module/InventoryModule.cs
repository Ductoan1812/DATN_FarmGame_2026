public class InventoryModule : IModuleData
{
    public InventoryType inventoryType = InventoryType.Backpack;
    public int size = 20;

    public override IModuleRuntime CreateRuntime()
    {
        return new InventoryRuntime(this);
    }
}
