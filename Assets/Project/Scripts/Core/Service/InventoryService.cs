using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class InventoryService
{
    private readonly EntityService entityService;

    public InventoryService(EntityService entityService)
    {
        this.entityService = entityService;
    }

    public int Pickup(EntityRuntime pickupEntity, EntityRuntime receiverEntity)
    {
        int totalReceived = 0;
        var receiverContainer = receiverEntity.Owner;
        if (receiverContainer != null)
            foreach (var inv in GetAllInventories(receiverEntity))
                if (inv.Container == null) inv.Container = receiverContainer;

        string itemName = pickupEntity.entityData.keyName;
        string receiverName = receiverEntity.entityData.keyName;

        foreach (var inv in GetInventoriesByPriority(receiverEntity))
        {
            if (pickupEntity.IsEmpty) break;

            int canReceive = inv.CanReceive(pickupEntity);
            if (canReceive <= 0) continue;

            if (canReceive >= pickupEntity.Amount)
            {
                int received = pickupEntity.Amount;
                PlaceInto(inv, pickupEntity);
                totalReceived += received;
                Debug.Log($"[InventoryService] Pickup: {receiverName} nhặt {itemName} x{received} → {inv.Type}");
                break;
            }
            else
            {
                var split = entityService.Split(pickupEntity, canReceive);
                if (split != null)
                {
                    PlaceInto(inv, split);
                    totalReceived += canReceive;
                    Debug.Log($"[InventoryService] Pickup: {receiverName} nhặt {itemName} x{canReceive} → {inv.Type}");
                }
            }
        }

        if (totalReceived > 0)
        {
            Debug.Log($"[InventoryService] Pickup tổng: {receiverName} nhặt {itemName} x{totalReceived}");
            PublishChanged(receiverEntity);
        }
        else
        {
            Debug.LogWarning($"[InventoryService] Pickup thất bại: không thể nhặt {itemName} vào {receiverName}");
        }

        return totalReceived;
    }

    public int Transfer(EntityRuntime entity, EntityRuntime fromEntity, EntityRuntime toEntity, int amount = -1)
    {
        var srcInv = FindInventoryOf(fromEntity, entity);
        if (srcInv == null) { Debug.LogWarning("[InventoryService] Transfer: entity không ở trong fromEntity."); return 0; }

        int wantAmount = amount < 0 ? entity.Amount : Mathf.Min(amount, entity.Amount);
        int totalTransferred = 0;

        foreach (var dstInv in GetInventoriesByPriority(toEntity))
        {
            if (wantAmount <= 0) break;

            int canReceive = Mathf.Min(dstInv.CanReceive(entity), wantAmount);
            if (canReceive <= 0) continue;

            if (canReceive >= entity.Amount)
            {
                int srcSlot = srcInv.FindSlotOf(entity);
                srcInv.ClearSlot(srcSlot);
                PlaceInto(dstInv, entity);
                totalTransferred += canReceive;
                wantAmount = 0;
            }
            else
            {
                var split = entityService.Split(entity, canReceive);
                if (split != null)
                {
                    PlaceInto(dstInv, split);
                    totalTransferred += canReceive;
                    wantAmount -= canReceive;
                }
            }
        }

        if (totalTransferred == 0)
            Debug.LogWarning("[InventoryService] Transfer: destination đầy hoặc không có inventory phù hợp.");
        else
        {
            PublishChanged(fromEntity);
            PublishChanged(toEntity);
        }

        return totalTransferred;
    }

    public bool Consume(EntityRuntime entity, EntityRuntime ownerEntity, int amount = 1)
    {
        var inv = FindInventoryOf(ownerEntity, entity);
        if (inv == null) { Debug.LogWarning("[InventoryService] Consume: entity không ở trong ownerEntity."); return false; }

        bool depleted = entityService.TryConsume(entity, amount);
        if (depleted)
        {
            int slot = inv.FindSlotOf(entity);
            if (slot >= 0) inv.ClearSlot(slot);
        }

        PublishChanged(ownerEntity);
        return true;
    }

    public bool Remove(EntityRuntime entity, EntityRuntime ownerEntity)
    {
        var inv = FindInventoryOf(ownerEntity, entity);
        if (inv == null) { Debug.LogWarning("[InventoryService] Remove: entity không ở trong ownerEntity."); return false; }

        int slot = inv.FindSlotOf(entity);
        if (slot < 0) return false;

        inv.ClearSlot(slot);
        entityService.Destroy(entity);
        return true;
    }

    public bool Remove(string entityDataId, int amount, EntityRuntime ownerEntity)
    {
        int remaining = amount;

        foreach (var inv in GetAllInventories(ownerEntity))
        {
            for (int i = 0; i < inv.MaxSlots && remaining > 0; i++)
            {
                var slot = inv.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;
                if (slot.entity.entityData.id != entityDataId) continue;

                int take = Mathf.Min(slot.entity.Amount, remaining);
                bool depleted = entityService.TryConsume(slot.entity, take);
                remaining -= take;
                if (depleted) inv.ClearSlot(i);
            }
        }

        if (remaining > 0) Debug.LogWarning($"[InventoryService] Remove: thiếu {remaining} của {entityDataId}");
        return remaining <= 0;
    }

    public void Sort(EntityRuntime ownerEntity, InventoryType inventoryType)
    {
        var inv = GetInventory(ownerEntity, inventoryType);
        if (inv == null) { Debug.LogWarning($"[InventoryService] Sort: không tìm thấy {inventoryType}."); return; }

        var entities = inv.GetAll()
            .OrderBy(e => e.entityData.category)
            .ThenBy(e => e.entityData.keyName)
            .ThenByDescending(e => e.Amount)
            .ToList();
        for (int i = 0; i < inv.MaxSlots; i++)
            inv.ClearSlot(i);

        for (int i = 0; i < entities.Count; i++)
            inv.SetSlot(i, entities[i]);
    }

    public void SwapSlots(EntityRuntime ownerA, InventoryType typeA, int slotA,
                          EntityRuntime ownerB, InventoryType typeB, int slotB)
    {
        var invA = GetInventory(ownerA, typeA);
        var invB = GetInventory(ownerB, typeB);
        if (invA == null || invB == null) return;

        var entityA = invA.GetSlot(slotA)?.entity;
        var entityB = invB.GetSlot(slotB)?.entity;

        if (entityA != null) invA.ClearSlot(slotA);
        if (entityB != null) invB.ClearSlot(slotB);

        if (entityB != null) invA.SetSlot(slotA, entityB);
        if (entityA != null) invB.SetSlot(slotB, entityA);

        PublishChanged(ownerA);
        if (ownerA != ownerB) PublishChanged(ownerB);
    }

    public bool Contains(EntityRuntime ownerEntity, EntityRuntime entity)
        => FindInventoryOf(ownerEntity, entity) != null;

    public int CountEntity(EntityRuntime ownerEntity, string entityDataId)
    {
        int total = 0;
        foreach (var inv in GetAllInventories(ownerEntity))
            total += inv.CountEntity(entityDataId);
        return total;
    }

    public InventoryRuntime GetInventory(EntityRuntime ownerEntity, InventoryType type)
    {
        foreach (var inv in ownerEntity.GetModules<InventoryRuntime>())
            if (inv.Type == type) return inv;
        return null;
    }

    private List<InventoryRuntime> GetAllInventories(EntityRuntime entity)
        => entity.GetModules<InventoryRuntime>();

  
    private IEnumerable<InventoryRuntime> GetInventoriesByPriority(EntityRuntime entity)
    {
        var all = GetAllInventories(entity);
        var order = new[] { InventoryType.Hotbar, InventoryType.Backpack, InventoryType.Chest };
        foreach (var type in order)
        {
            var inv = all.Find(i => i.Type == type);
            if (inv != null) yield return inv;
        }
        foreach (var inv in all)
            if (!order.Contains(inv.Type)) yield return inv;
    }

    private InventoryRuntime FindInventoryOf(EntityRuntime ownerEntity, EntityRuntime entity)
    {
        foreach (var inv in GetAllInventories(ownerEntity))
            if (inv.Contains(entity)) return inv;
        return null;
    }

    private void PublishChanged(EntityRuntime entity)
    {
        var eb = GameManager.Instance?.EventBus;
        if (eb == null || entity == null) return;
        foreach (var inv in GetAllInventories(entity))
            eb.Publish(new InventoryChangedPublish(entity.id, inv.Type));
    }

  
    private void PlaceInto(InventoryRuntime inv, EntityRuntime entity)
    {
        while (entity.Amount > 0)
        {
            int stackSlot = inv.FindStackableSlot(entity);
            if (stackSlot < 0) break;
            entityService.Merge(inv.GetSlot(stackSlot).entity, entity);
        }

        if (entity.IsEmpty) return;

        int emptySlot = inv.FindEmptySlot();
        if (emptySlot < 0) { Debug.LogWarning($"[InventoryService] PlaceInto: {inv.Type} đầy."); return; }

        inv.SetSlot(emptySlot, entity);
    }
}
