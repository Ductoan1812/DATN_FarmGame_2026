using UnityEngine;

public class ShopRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private readonly ShopModule data;
    private EntityRuntime owner;
    private bool stockInitialized;

    public ShopRuntime(ShopModule data)
    {
        this.data = data;
    }

    public void Handle(SecondaryActionEvent e)
    {
        owner ??= e.target;
        if (e.context == null) return;

        var shopOwner = owner ?? e.target;
        if (shopOwner == null) return;

        if (!data.infiniteStock)
            EnsureInitialStock(shopOwner);

        if (!data.sellsToPlayer && !data.buysFromPlayer)
            return;

        var inventory = GameManager.Instance?.InventoryService?.GetInventory(shopOwner, data.stockInventoryType);
        if (!data.infiniteStock && inventory == null)
        {
            Debug.LogWarning($"[ShopRuntime] '{shopOwner.entityData?.keyName}' has ShopModule but no {data.stockInventoryType} InventoryModule.");
            return;
        }

        if (data.infiniteStock && data.buysFromPlayer && inventory == null)
            Debug.LogWarning($"[ShopRuntime] '{shopOwner.entityData?.keyName}' can sell infinite stock, but cannot buy from player without {data.stockInventoryType} InventoryModule.");

        e.context.AddOption(
            $"shop.{shopOwner.entityData?.id ?? shopOwner.id}",
            data.optionTextKey,
            data.priority,
            () => ShopService.Open(e.initiator, shopOwner, data));
    }

    private void EnsureInitialStock(EntityRuntime shopOwner)
    {
        if (stockInitialized) return;
        stockInitialized = true;

        if (data.initialStock == null || data.initialStock.Count == 0)
            return;

        var gameManager = GameManager.Instance;
        var entityService = gameManager?.EntityService;
        var inventoryService = gameManager?.InventoryService;
        if (entityService == null || inventoryService == null)
            return;

        var inventory = inventoryService.GetInventory(shopOwner, data.stockInventoryType);
        if (inventory == null)
        {
            Debug.LogWarning($"[ShopRuntime] Cannot seed stock because '{shopOwner.entityData?.keyName}' has no {data.stockInventoryType} inventory.");
            return;
        }

        foreach (var entry in data.initialStock)
        {
            if (entry?.itemData == null || entry.amount <= 0) continue;

            int remaining = entry.amount;
            while (remaining > 0)
            {
                int stackAmount = Mathf.Min(remaining, Mathf.Max(1, entry.itemData.maxStack));
                var item = entityService.Create(entry.itemData, stackAmount);
                int received = inventoryService.Pickup(item, shopOwner);
                remaining -= received;

                if (received < stackAmount && item != null && !item.IsEmpty)
                    entityService.Destroy(item);

                if (received <= 0)
                    break;
            }
        }
    }

    public ModuleSaveData ToSaveData()
    {
        return new ModuleSaveData
        {
            moduleType = "Shop",
            dataJson = JsonUtility.ToJson(new ShopSaveData { stockInitialized = stockInitialized })
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrWhiteSpace(save.dataJson)) return;
        var saveData = JsonUtility.FromJson<ShopSaveData>(save.dataJson);
        stockInitialized = saveData != null && saveData.stockInitialized;
    }

    public bool MatchesSave(ModuleSaveData save) => save?.moduleType == "Shop";
    public bool Equals(IModuleRuntime other) => other is ShopRuntime;

    [System.Serializable]
    private class ShopSaveData
    {
        public bool stockInitialized;
    }
}
