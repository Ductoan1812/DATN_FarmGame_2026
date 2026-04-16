using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bridge giữa EquipmentRuntime và UI.
/// Xử lý logic:
///   - Hand: chọn hotbar → auto equip/unequip (item vẫn nằm trong Hotbar)
///   - Gear: kéo vào ô equipment → xóa khỏi Inventory, kéo ra → trả lại Inventory
/// </summary>
[RequireComponent(typeof(EntityRoot))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerEquipment : MonoBehaviour
{
    private EntityRoot _root;
    private PlayerInventory _playerInventory;
    private InventoryService _inventoryService;

    // ── Event cho UI ──
    public event Action OnEquipmentChanged;

    // ── Shortcut ──
    private EntityRuntime Entity => _root?.GetEntity();
    private EquipmentRuntime Equipment => Entity?.GetModule<EquipmentRuntime>();

    private void Awake()
    {
        _root = GetComponent<EntityRoot>();
        _playerInventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        _inventoryService = GameManager.Instance.InventoryService;

        // Init OwnerStats cho EquipmentRuntime
        var equip = Equipment;
        if (equip != null)
            equip.OwnerStats = Entity.stats;

        // Lắng nghe hotbar thay đổi → auto equip/unequip Hand
        _playerInventory.OnHotbarSelectionChanged += OnHotbarChanged;

        // Apply hand item ban đầu
        OnHotbarChanged(_playerInventory.SelectedHotbarIndex);
    }

    private void OnDestroy()
    {
        if (_playerInventory != null)
            _playerInventory.OnHotbarSelectionChanged -= OnHotbarChanged;
    }

    // ══════ Hand — Auto từ Hotbar ══════

    private void OnHotbarChanged(int slotIndex)
    {
        var equip = Equipment;
        if (equip == null) return;

        var selectedItem = _playerInventory.SelectedItem;

        // Kiểm tra item có phải Hand equipment không
        if (selectedItem != null && IsHandItem(selectedItem))
        {
            equip.Equip(selectedItem); // Equip tự remove stats cũ + apply mới
        }
        else
        {
            equip.ClearHand(); // Không phải Hand → clear
        }

        OnEquipmentChanged?.Invoke();
    }

    // ══════ Gear — Mũ, Áo, Giày... ══════

    /// <summary>
    /// Trang bị gear (không phải Hand). Xóa khỏi Inventory → vào EquipmentRuntime.
    /// Trả về true nếu thành công.
    /// </summary>
    public bool EquipGear(EntityRuntime entity)
    {
        if (entity == null || Entity == null) return false;

        var equipInfo = entity.GetModule<EquipRuntime>();
        if (equipInfo == null || equipInfo.GetEquipSlot() == EquipSlot.Hand) return false;

        var equip = Equipment;
        if (equip == null) return false;

        // Xóa khỏi Inventory
        var inv = FindInventoryOf(entity);
        if (inv != null)
        {
            int slot = inv.FindSlotOf(entity);
            if (slot >= 0) inv.ClearSlot(slot);
        }

        // Equip — nếu slot đã có item cũ → trả lại Inventory
        var previous = equip.Equip(entity);
        if (previous != null)
            _inventoryService.Pickup(previous, Entity);

        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Tháo gear. Trả item lại Inventory.
    /// </summary>
    public EntityRuntime UnequipGear(EquipSlot slot)
    {
        if (slot == EquipSlot.Hand) return null; // Hand do hotbar quản lý

        var equip = Equipment;
        if (equip == null) return null;

        var entity = equip.Unequip(slot);
        if (entity == null) return null;

        // Nhét lại vào Inventory
        _inventoryService.Pickup(entity, Entity);

        OnEquipmentChanged?.Invoke();
        return entity;
    }

    // ══════ Query ══════

    public EntityRuntime GetEquipped(EquipSlot slot) => Equipment?.Get(slot);

    public EntityRuntime GetHandItem() => Equipment?.Get(EquipSlot.Hand);

    // ══════ Private ══════

    private static bool IsHandItem(EntityRuntime entity)
    {
        var equipInfo = entity.GetModule<EquipRuntime>();
        return equipInfo != null && equipInfo.GetEquipSlot() == EquipSlot.Hand;
    }

    private InventoryRuntime FindInventoryOf(EntityRuntime entity)
    {
        if (Entity == null) return null;
        foreach (var inv in Entity.GetModules<InventoryRuntime>())
            if (inv.Contains(entity)) return inv;
        return null;
    }
}
