using System.Collections.Generic;
using UnityEngine;

public static class StarterLoadoutService
{
    public static void Apply(
        EntityService entityService,
        EventBus eventBus,
        EntityRuntime player,
        StarterLoadoutData loadout)
    {
        if (entityService == null || player == null || loadout == null)
            return;

        if (player.stats != null)
            player.stats.Set(StatType.Money, Mathf.Max(0, loadout.initialMoney));

        var changedTypes = new HashSet<InventoryType>();
        if (loadout.entries != null)
        {
            foreach (var entry in loadout.entries)
            {
                if (entry?.itemData == null) continue;

                var inventory = FindInventory(player, entry.inventoryType);
                if (inventory == null)
                {
                    Debug.LogWarning($"[StarterLoadoutService] Player missing {entry.inventoryType} inventory for '{entry.itemData.id}'.");
                    continue;
                }

                if (inventory.Container == null && player.Owner != null)
                    inventory.Container = player.Owner;

                var item = entityService.Create(entry.itemData, Mathf.Max(1, entry.amount));
                if (!TryPlaceAtOrNear(entityService, inventory, item, entry.slotIndex))
                {
                    entityService.Destroy(item);
                    Debug.LogWarning($"[StarterLoadoutService] Cannot place starter item '{entry.itemData.id}' into {entry.inventoryType}.");
                    continue;
                }

                changedTypes.Add(entry.inventoryType);
            }
        }

        var hotbar = FindInventory(player, InventoryType.Hotbar);
        if (hotbar != null)
            hotbar.SelectSlot(Mathf.Clamp(loadout.selectedHotbarIndex, 0, hotbar.MaxSlots - 1));

        if (eventBus != null)
        {
            foreach (var type in changedTypes)
                eventBus.Publish(new InventoryChangedPublish(player.id, type));
        }
    }

    private static InventoryRuntime FindInventory(EntityRuntime player, InventoryType type)
    {
        if (player == null) return null;
        foreach (var inventory in player.GetModules<InventoryRuntime>())
        {
            if (inventory != null && inventory.Type == type)
                return inventory;
        }

        return null;
    }

    private static bool TryPlaceAtOrNear(
        EntityService entityService,
        InventoryRuntime inventory,
        EntityRuntime item,
        int preferredSlot)
    {
        if (entityService == null || inventory == null || item == null)
            return false;

        if (TryMergeIntoExistingStacks(entityService, inventory, item))
            return true;

        int clampedSlot = Mathf.Clamp(preferredSlot, 0, Mathf.Max(0, inventory.MaxSlots - 1));
        if (TryPlaceInSlot(inventory, item, clampedSlot))
            return true;

        int empty = inventory.FindEmptySlot();
        return TryPlaceInSlot(inventory, item, empty);
    }

    private static bool TryMergeIntoExistingStacks(
        EntityService entityService,
        InventoryRuntime inventory,
        EntityRuntime item)
    {
        while (item.Amount > 0)
        {
            int stackSlot = inventory.FindStackableSlot(item);
            if (stackSlot < 0) break;

            var existing = inventory.GetSlot(stackSlot)?.entity;
            if (existing == null) break;
            entityService.Merge(existing, item);
        }

        return item.IsEmpty;
    }

    private static bool TryPlaceInSlot(InventoryRuntime inventory, EntityRuntime item, int slot)
    {
        if (inventory == null || item == null) return false;
        if (slot < 0 || slot >= inventory.MaxSlots) return false;
        if (!inventory.GetSlot(slot).IsEmpty) return false;

        inventory.SetSlot(slot, item);
        return true;
    }
}
