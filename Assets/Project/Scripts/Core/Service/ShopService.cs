using System.Collections.Generic;
using UnityEngine;

public static class ShopService
{
    public static void Open(EntityRuntime customer, EntityRuntime merchant, ShopModule shop)
    {
        if (customer == null || merchant == null || shop == null) return;

        var viewData = BuildView(customer, merchant, shop);
        Debug.Log($"[ShopService] Open shop: customer='{customer.entityData?.keyName}', merchant='{merchant.entityData?.keyName}', stock={viewData.StockItems.Count}, sellable={viewData.PlayerSellableItems.Count}.");
        GameManager.Instance?.EventBus?.Publish(new ShopViewPublish(viewData));
    }

    public static ShopTransactionResult TryBuy(EntityRuntime customer, EntityRuntime merchant, EntityRuntime stockItem, int amount)
    {
        if (customer == null || merchant == null || stockItem == null)
            return Fail(ShopTransactionFailReason.InvalidRequest);

        amount = Mathf.Clamp(amount, 1, stockItem.Amount);
        int unitPrice = Mathf.Max(0, stockItem.entityData?.buyPrice ?? 0);
        int totalPrice = unitPrice * amount;

        if (stockItem.entityData == null || stockItem.entityData.buyPrice < 0)
            return Fail(ShopTransactionFailReason.ItemNotBuyable);

        if (!HasMoney(customer, totalPrice))
            return Fail(ShopTransactionFailReason.NotEnoughMoney);

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null)
            return Fail(ShopTransactionFailReason.ServiceUnavailable);

        if (inventoryService.CanReceive(customer, stockItem, amount) < amount)
            return Fail(ShopTransactionFailReason.InventoryFull);

        int transferred = inventoryService.Transfer(stockItem, merchant, customer, amount);
        if (transferred != amount)
            return Fail(ShopTransactionFailReason.TransferFailed);

        AddMoney(customer, -totalPrice);
        AddMoney(merchant, totalPrice);

        var result = new ShopTransactionResult(true, ShopTransactionFailReason.None, transferred, totalPrice);
        PublishResult(customer, merchant, result);
        return result;
    }

    public static ShopTransactionResult TryBuy(EntityRuntime customer, EntityRuntime merchant, ShopItemViewData stockItem, int amount)
    {
        if (stockItem == null)
            return Fail(ShopTransactionFailReason.InvalidRequest);

        return stockItem.InfiniteStock
            ? TryBuyInfinite(customer, merchant, stockItem.ItemData, amount)
            : TryBuy(customer, merchant, stockItem.Item, amount);
    }

    public static ShopTransactionResult TryBuyInfinite(EntityRuntime customer, EntityRuntime merchant, EntityData itemData, int amount)
    {
        if (customer == null || merchant == null || itemData == null)
            return Fail(ShopTransactionFailReason.InvalidRequest);

        if (itemData.buyPrice < 0)
            return Fail(ShopTransactionFailReason.ItemNotBuyable);

        amount = Mathf.Max(1, amount);
        int stackSize = Mathf.Max(1, itemData.maxStack);
        int totalPrice = Mathf.Max(0, itemData.buyPrice) * amount;

        if (!HasMoney(customer, totalPrice))
            return Fail(ShopTransactionFailReason.NotEnoughMoney);

        var gameManager = GameManager.Instance;
        var entityService = gameManager?.EntityService;
        var inventoryService = gameManager?.InventoryService;
        if (entityService == null || inventoryService == null)
            return Fail(ShopTransactionFailReason.ServiceUnavailable);

        int remaining = amount;
        while (remaining > 0)
        {
            int batch = Mathf.Min(remaining, stackSize);
            var previewItem = entityService.Create(itemData, batch);
            if (inventoryService.CanReceive(customer, previewItem, batch) < batch)
            {
                entityService.Destroy(previewItem);
                return Fail(ShopTransactionFailReason.InventoryFull);
            }

            entityService.Destroy(previewItem);
            remaining -= batch;
        }

        remaining = amount;
        int received = 0;
        while (remaining > 0)
        {
            int batch = Mathf.Min(remaining, stackSize);
            var item = entityService.Create(itemData, batch);
            int moved = inventoryService.Pickup(item, customer);
            if (moved != batch)
            {
                if (item != null && !item.IsEmpty)
                    entityService.Destroy(item);

                return Fail(ShopTransactionFailReason.TransferFailed);
            }

            received += moved;
            remaining -= moved;
        }

        AddMoney(customer, -totalPrice);
        AddMoney(merchant, totalPrice);

        var result = new ShopTransactionResult(true, ShopTransactionFailReason.None, received, totalPrice);
        PublishResult(customer, merchant, result);
        return result;
    }

    public static ShopTransactionResult TrySell(EntityRuntime seller, EntityRuntime merchant, EntityRuntime item, int amount, ShopModule shop)
    {
        if (seller == null || merchant == null || item == null || shop == null)
            return Fail(ShopTransactionFailReason.InvalidRequest);

        if (!shop.buysFromPlayer || !CanMerchantBuy(shop, item))
            return Fail(ShopTransactionFailReason.ItemNotSellable);

        amount = Mathf.Clamp(amount, 1, item.Amount);
        int unitPrice = Mathf.Max(0, item.entityData?.sellPrice ?? 0);
        int totalPrice = unitPrice * amount;

        if (item.entityData == null || item.entityData.sellPrice < 0)
            return Fail(ShopTransactionFailReason.ItemNotSellable);

        if (!HasMoney(merchant, totalPrice))
            return Fail(ShopTransactionFailReason.MerchantNotEnoughMoney);

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null)
            return Fail(ShopTransactionFailReason.ServiceUnavailable);

        if (inventoryService.CanReceive(merchant, item, amount) < amount)
            return Fail(ShopTransactionFailReason.MerchantInventoryFull);

        int transferred = inventoryService.Transfer(item, seller, merchant, amount);
        if (transferred != amount)
            return Fail(ShopTransactionFailReason.TransferFailed);

        AddMoney(seller, totalPrice);
        AddMoney(merchant, -totalPrice);

        var result = new ShopTransactionResult(true, ShopTransactionFailReason.None, transferred, totalPrice);
        PublishResult(seller, merchant, result);
        return result;
    }

    public static bool CanMerchantBuy(ShopModule shop, EntityRuntime item)
    {
        return CanMerchantBuy(shop, item?.entityData);
    }

    public static bool CanMerchantBuy(ShopModule shop, EntityData itemData)
    {
        if (shop == null || itemData == null) return false;
        if (!shop.buysFromPlayer || itemData.sellPrice < 0) return false;
        if (shop.buysAllItems) return true;
        if (shop.buyWhitelist == null || shop.buyWhitelist.Count == 0) return false;
        return shop.buyWhitelist.Contains(itemData);
    }

    private static ShopViewData BuildView(EntityRuntime customer, EntityRuntime merchant, ShopModule shop)
    {
        var stockItems = new List<ShopItemViewData>();
        var playerItems = new List<ShopItemViewData>();
        var inventoryService = GameManager.Instance?.InventoryService;

        if (shop.infiniteStock)
        {
            if (shop.initialStock != null)
            {
                foreach (var entry in shop.initialStock)
                {
                    if (entry?.itemData == null) continue;
                    stockItems.Add(new ShopItemViewData(
                        entry.itemData,
                        entry.itemData.keyName,
                        Mathf.Max(1, entry.amount),
                        entry.itemData.buyPrice,
                        entry.itemData.sellPrice,
                        entry.itemData.buyPrice >= 0,
                        CanMerchantBuy(shop, entry.itemData),
                        true));
                }
            }
        }
        else
        {
            var stockInventory = inventoryService?.GetInventory(merchant, shop.stockInventoryType);
            if (stockInventory != null)
            {
                foreach (var item in stockInventory.GetAll())
                {
                    if (item?.entityData == null) continue;
                    stockItems.Add(new ShopItemViewData(
                        item,
                        item.entityData.keyName,
                        item.Amount,
                        item.entityData.buyPrice,
                        item.entityData.sellPrice,
                        item.entityData.buyPrice >= 0,
                        CanMerchantBuy(shop, item)));
                }
            }
        }

        foreach (var inventory in customer.GetModules<InventoryRuntime>())
        {
            foreach (var item in inventory.GetAll())
            {
                if (item?.entityData == null) continue;
                bool canSell = CanMerchantBuy(shop, item);

                playerItems.Add(new ShopItemViewData(
                    item,
                    item.entityData.keyName,
                    item.Amount,
                    item.entityData.buyPrice,
                    item.entityData.sellPrice,
                    item.entityData.buyPrice >= 0,
                    canSell));
            }
        }

        return new ShopViewData(
            shop,
            customer,
            merchant,
            shop.sellsToPlayer,
            shop.buysFromPlayer,
            GetMoney(customer),
            GetMoney(merchant),
            stockItems,
            playerItems);
    }

    private static bool HasMoney(EntityRuntime entity, int amount)
    {
        return amount <= 0 || GetMoney(entity) >= amount;
    }

    private static int GetMoney(EntityRuntime entity)
    {
        if (entity?.stats == null) return 0;
        return Mathf.FloorToInt(entity.stats.Get(StatType.Money));
    }

    private static void AddMoney(EntityRuntime entity, int delta)
    {
        if (entity?.stats == null || delta == 0) return;
        entity.stats.Set(StatType.Money, Mathf.Max(0, GetMoney(entity) + delta));
    }

    private static ShopTransactionResult Fail(ShopTransactionFailReason reason)
    {
        return new ShopTransactionResult(false, reason, 0, 0);
    }

    private static void PublishResult(EntityRuntime customer, EntityRuntime merchant, ShopTransactionResult result)
    {
        GameManager.Instance?.EventBus?.Publish(new ShopTransactionResultPublish(customer, merchant, result));
    }
}

public sealed class ShopViewData
{
    public ShopModule Shop { get; }
    public EntityRuntime Customer { get; }
    public EntityRuntime Merchant { get; }
    public bool CanBuyFromMerchant { get; }
    public bool CanSellToMerchant { get; }
    public int CustomerMoney { get; }
    public int MerchantMoney { get; }
    public IReadOnlyList<ShopItemViewData> StockItems { get; }
    public IReadOnlyList<ShopItemViewData> PlayerSellableItems { get; }

    public ShopViewData(
        ShopModule shop,
        EntityRuntime customer,
        EntityRuntime merchant,
        bool canBuyFromMerchant,
        bool canSellToMerchant,
        int customerMoney,
        int merchantMoney,
        IReadOnlyList<ShopItemViewData> stockItems,
        IReadOnlyList<ShopItemViewData> playerSellableItems)
    {
        Shop = shop;
        Customer = customer;
        Merchant = merchant;
        CanBuyFromMerchant = canBuyFromMerchant;
        CanSellToMerchant = canSellToMerchant;
        CustomerMoney = customerMoney;
        MerchantMoney = merchantMoney;
        StockItems = stockItems;
        PlayerSellableItems = playerSellableItems;
    }
}

public sealed class ShopItemViewData
{
    public EntityRuntime Item { get; }
    public EntityData ItemData { get; }
    public string NameKey { get; }
    public int Amount { get; }
    public int BuyPrice { get; }
    public int SellPrice { get; }
    public bool Buyable { get; }
    public bool Sellable { get; }
    public bool InfiniteStock { get; }

    public ShopItemViewData(EntityRuntime item, string nameKey, int amount, int buyPrice, int sellPrice, bool buyable, bool sellable)
    {
        Item = item;
        ItemData = item?.entityData;
        NameKey = nameKey;
        Amount = amount;
        BuyPrice = buyPrice;
        SellPrice = sellPrice;
        Buyable = buyable;
        Sellable = sellable;
        InfiniteStock = false;
    }

    public ShopItemViewData(EntityData itemData, string nameKey, int amount, int buyPrice, int sellPrice, bool buyable, bool sellable, bool infiniteStock)
    {
        Item = null;
        ItemData = itemData;
        NameKey = nameKey;
        Amount = amount;
        BuyPrice = buyPrice;
        SellPrice = sellPrice;
        Buyable = buyable;
        Sellable = sellable;
        InfiniteStock = infiniteStock;
    }
}

public readonly struct ShopTransactionResult
{
    public readonly bool Success;
    public readonly ShopTransactionFailReason FailReason;
    public readonly int Amount;
    public readonly int TotalPrice;

    public ShopTransactionResult(bool success, ShopTransactionFailReason failReason, int amount, int totalPrice)
    {
        Success = success;
        FailReason = failReason;
        Amount = amount;
        TotalPrice = totalPrice;
    }
}

public enum ShopTransactionFailReason
{
    None,
    InvalidRequest,
    ServiceUnavailable,
    ItemNotBuyable,
    ItemNotSellable,
    NotEnoughMoney,
    MerchantNotEnoughMoney,
    InventoryFull,
    MerchantInventoryFull,
    TransferFailed
}
