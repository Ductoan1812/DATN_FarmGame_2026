using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopModule : IModuleData
{
    [Header("Interaction")]
    public string optionTextKey = "ui.shop.open";
    public int priority = 30;

    [Header("Trade Rules")]
    public bool sellsToPlayer = true;
    public bool buysFromPlayer = true;
    public bool buysAllItems;
    public bool infiniteStock;
    [Tooltip("When true, the merchant has unlimited funds and can always pay the player when buying items.")]
    public bool infiniteMoney = true;
    public InventoryType stockInventoryType = InventoryType.Backpack;
    public List<EntityData> buyWhitelist = new();

    [Header("Initial Stock")]
    public List<ShopStockEntry> initialStock = new();

    public override IModuleRuntime CreateRuntime()
    {
        return new ShopRuntime(this);
    }
}

[System.Serializable]
public class ShopStockEntry
{
    public EntityData itemData;
    [Min(1)] public int amount = 1;
    [Min(1)] public int requiredLevel = 1;
    public UnlockRequirementData unlockRequirement = new();
}
