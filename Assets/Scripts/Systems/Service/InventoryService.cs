using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton service — nghiệp vụ inventory tập trung.
/// Biết EntityService và InventoryRuntime của bất kỳ entity nào.
/// KHÔNG tự thay đổi Amount — ủy thác cho EntityService.
/// </summary>
public class InventoryService
{
    private readonly EntityService entityService;

    public InventoryService(EntityService entityService)
    {
        this.entityService = entityService;
    }

    // ══════ Pickup — nhặt entity từ thế giới ══════

    /// <summary>
    /// Nhặt pickupEntity vào túi của receiverEntity. Ưu tiên Hotbar → Backpack.
    /// Nếu túi chỉ nhận được 1 phần: Split, phần nhặt vào túi, phần còn lại ở thế giới.
    /// Trả về tổng số lượng đã nhặt được.
    /// </summary>
    public int Pickup(EntityRuntime pickupEntity, EntityRuntime receiverEntity)
    {
        int totalReceived = 0;

        // Đảm bảo các inventory của receiver có Container để SetSlot gán Owner đúng
        var receiverContainer = receiverEntity.Owner;
        if (receiverContainer != null)
            foreach (var inv in GetAllInventories(receiverEntity))
                if (inv.Container == null) inv.Container = receiverContainer;

        foreach (var inv in GetInventoriesByPriority(receiverEntity))
        {
            if (pickupEntity.IsEmpty) break;

            int canReceive = inv.CanReceive(pickupEntity);
            if (canReceive <= 0) continue;

            if (canReceive >= pickupEntity.Amount)
            {
                totalReceived += pickupEntity.Amount;
                PlaceInto(inv, pickupEntity);
                break;
            }
            else
            {
                var split = entityService.Split(pickupEntity, canReceive);
                if (split != null)
                {
                    PlaceInto(inv, split);
                    totalReceived += canReceive;
                }
            }
        }

        return totalReceived;
    }

    // ══════ Transfer — chuyển entity giữa 2 entity ══════

    /// <summary>
    /// Chuyển entity từ túi của fromEntity sang túi của toEntity.
    /// Ví dụ: NPC → Player, Player → Chest.
    /// Trả về số lượng đã chuyển được.
    /// </summary>
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
                // Chuyển hết entity
                int srcSlot = srcInv.FindSlotOf(entity);
                srcInv.ClearSlot(srcSlot);
                PlaceInto(dstInv, entity);
                totalTransferred += canReceive;
                wantAmount = 0;
            }
            else
            {
                // Chuyển 1 phần
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

        return totalTransferred;
    }

    // ══════ Consume — tiêu thụ entity ══════

    /// <summary>
    /// Tiêu thụ amount của entity. Nếu Amount = 0 → ClearSlot + Unregister.
    /// Trả về true nếu thành công.
    /// </summary>
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

        return true;
    }

    // ══════ Remove — xóa hoàn toàn ══════

    /// <summary>Xóa entity khỏi túi và Unregister khỏi Registry.</summary>
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

    /// <summary>Xóa theo id + amount, duyệt tất cả túi của ownerEntity.</summary>
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

    // ══════ Sort — sắp xếp túi ══════

    /// <summary>Sắp xếp slot theo category → name → amount.</summary>
    public void Sort(EntityRuntime ownerEntity, InventoryType inventoryType)
    {
        var inv = GetInventory(ownerEntity, inventoryType);
        if (inv == null) { Debug.LogWarning($"[InventoryService] Sort: không tìm thấy {inventoryType}."); return; }

        var entities = inv.GetAll()
            .OrderBy(e => e.entityData.category)
            .ThenBy(e => e.entityData.keyName)
            .ThenByDescending(e => e.Amount)
            .ToList();

        // Clear tất cả slot
        for (int i = 0; i < inv.MaxSlots; i++)
            inv.ClearSlot(i);

        // Set lại theo thứ tự
        for (int i = 0; i < entities.Count; i++)
            inv.SetSlot(i, entities[i]);
    }

    // ══════ SwapSlots — drag & drop UI ══════

    /// <summary>
    /// Swap 2 slot bất kỳ, kể cả khác entity (Player ↔ Chest).
    /// </summary>
    public void SwapSlots(EntityRuntime ownerA, InventoryType typeA, int slotA,
                          EntityRuntime ownerB, InventoryType typeB, int slotB)
    {
        var invA = GetInventory(ownerA, typeA);
        var invB = GetInventory(ownerB, typeB);
        if (invA == null || invB == null) return;

        var entityA = invA.GetSlot(slotA)?.entity;
        var entityB = invB.GetSlot(slotB)?.entity;

        // Clear cả 2 trước để reset Owner
        if (entityA != null) invA.ClearSlot(slotA);
        if (entityB != null) invB.ClearSlot(slotB);

        // Set lại chéo — SetSlot tự cập nhật Owner
        if (entityB != null) invA.SetSlot(slotA, entityB);
        if (entityA != null) invB.SetSlot(slotB, entityA);
    }

    // ══════ Query ══════

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

    // ══════ Internal helpers ══════

    private List<InventoryRuntime> GetAllInventories(EntityRuntime entity)
        => entity.GetModules<InventoryRuntime>();

    /// <summary>Trả về inventories theo thứ tự ưu tiên: Hotbar → Backpack → Chest.</summary>
    private IEnumerable<InventoryRuntime> GetInventoriesByPriority(EntityRuntime entity)
    {
        var all = GetAllInventories(entity);
        var order = new[] { InventoryType.Hotbar, InventoryType.Backpack, InventoryType.Chest };
        foreach (var type in order)
        {
            var inv = all.Find(i => i.Type == type);
            if (inv != null) yield return inv;
        }
        // Các type còn lại không nằm trong order
        foreach (var inv in all)
            if (!order.Contains(inv.Type)) yield return inv;
    }

    private InventoryRuntime FindInventoryOf(EntityRuntime ownerEntity, EntityRuntime entity)
    {
        foreach (var inv in GetAllInventories(ownerEntity))
            if (inv.Contains(entity)) return inv;
        return null;
    }

    /// <summary>Đặt entity vào túi: merge stack trước → slot trống. SetSlot tự cập nhật Owner.</summary>
    private void PlaceInto(InventoryRuntime inv, EntityRuntime entity)
    {
        // 1. Merge vào stack cùng loại
        while (entity.Amount > 0)
        {
            int stackSlot = inv.FindStackableSlot(entity);
            if (stackSlot < 0) break;
            inv.GetSlot(stackSlot).entity.MergeFrom(entity);
        }

        if (entity.IsEmpty) return;

        // 2. Slot trống
        int emptySlot = inv.FindEmptySlot();
        if (emptySlot < 0) { Debug.LogWarning($"[InventoryService] PlaceInto: {inv.Type} đầy."); return; }

        inv.SetSlot(emptySlot, entity);
    }
}
