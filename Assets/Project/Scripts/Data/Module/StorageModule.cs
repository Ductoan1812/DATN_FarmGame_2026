using UnityEngine;

[System.Serializable]
public class StorageModule : IModuleData
{
    public InventoryType inventoryType = InventoryType.Chest;
    public string optionTextKey = LocalizationKeys.UiMenuStorage;
    public int priority = 15;

    public override IModuleRuntime CreateRuntime()
    {
        return new StorageRuntime(this);
    }
}
