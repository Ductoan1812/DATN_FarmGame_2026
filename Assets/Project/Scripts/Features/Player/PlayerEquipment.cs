using System;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;

/// <summary>
/// Bridge giữa EquipmentRuntime và UI.
/// Lắng nghe EquipmentRuntime.OnChanged → publish event cho UI.
///
/// Xử lý logic:
///   - Hand: chọn hotbar → auto equip/unequip visual + stats (item vẫn nằm trong Hotbar)
///   - Gear: equip từ Inventory → xóa khỏi Inventory, unequip → trả lại Inventory
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

        var equip = Equipment;
        if (equip != null)
        {
            // Set owner ref + stats
            equip.SetOwner(Entity);
            equip.OwnerStats = Entity.stats;

            // Lắng nghe thay đổi từ EquipmentRuntime
            equip.OnChanged += OnEquipRuntimeChanged;
        }

        // Lắng nghe hotbar thay đổi → auto equip/unequip Hand
        _playerInventory.OnHotbarSelectionChanged += OnHotbarChanged;

        // Apply hand item ban đầu
        OnHotbarChanged(_playerInventory.SelectedHotbarIndex);
    }

    private void OnDestroy()
    {
        if (_playerInventory != null)
            _playerInventory.OnHotbarSelectionChanged -= OnHotbarChanged;

        var equip = Equipment;
        if (equip != null)
            equip.OnChanged -= OnEquipRuntimeChanged;
    }

    // ══════ EquipmentRuntime → UI ══════

    private void OnEquipRuntimeChanged()
    {
        OnEquipmentChanged?.Invoke();
    }

    // ══════ Hand — Auto từ Hotbar ══════

    private void OnHotbarChanged(int slotIndex)
    {
        var equip = Equipment;
        if (equip == null) return;

        var selectedItem = _playerInventory.SelectedItem;

        // Kiểm tra item có AppearanceModule (có sprite trên nhân vật) không
        if (selectedItem != null && IsHandItem(selectedItem))
        {
            equip.EquipHand(selectedItem);
        }
        else
        {
            equip.ClearHand();
        }
    }

    // ══════ Gear — Mũ, Áo, Giày... ══════

    /// <summary>
    /// Trang bị gear (không phải Hand). 
    /// EquipmentRuntime.Equip() tự xử lý toàn bộ ref transfer.
    /// Trả về true nếu thành công.
    /// </summary>
    public bool EquipGear(EntityRuntime entity)
    {
        if (entity == null || Entity == null) return false;

        var appearance = entity.GetModule<AppearanceRuntime>();
        if (appearance == null) return false;

        // Hand items do hotbar quản lý, không equip qua đây
        if (IsHandPart(appearance.EquipmentPart)) return false;

        var equip = Equipment;
        if (equip == null) return false;

        return equip.Equip(entity);
    }

    /// <summary>
    /// Tháo gear. Trả item lại Inventory.
    /// </summary>
    public EntityRuntime UnequipGear(EquipmentPart part)
    {
        if (IsHandPart(part)) return null; // Hand do hotbar quản lý

        var equip = Equipment;
        if (equip == null) return null;

        var entity = equip.Unequip(part);
        if (entity == null) return null;

        // Nhét lại vào Inventory
        _inventoryService.Pickup(entity, Entity);

        return entity;
    }

    // ══════ Query ══════

    public EntityRuntime GetEquipped(EquipmentPart part) => Equipment?.Get(part);

    public EntityRuntime GetHandItem()
    {
        var equip = Equipment;
        if (equip == null) return null;

        // Tìm bất kỳ hand-type slot nào đang có item
        foreach (var handPart in _handParts)
        {
            var item = equip.Get(handPart);
            if (item != null) return item;
        }
        return null;
    }

    // ══════ Private ══════

    private static readonly EquipmentPart[] _handParts =
    {
        EquipmentPart.MeleeWeapon1H,
        EquipmentPart.MeleeWeapon2H,
        EquipmentPart.Bow,
        EquipmentPart.Crossbow,
        EquipmentPart.Firearm1H,
        EquipmentPart.Firearm2H,
        EquipmentPart.SecondaryMelee1H,
        EquipmentPart.SecondaryFirearm1H,
        EquipmentPart.Shield
    };

    private static bool IsHandPart(EquipmentPart part)
    {
        return System.Array.IndexOf(_handParts, part) >= 0;
    }

    /// <summary>Item có phải hand item không (có AppearanceModule với hand-type part).</summary>
    private static bool IsHandItem(EntityRuntime entity)
    {
        var appearance = entity.GetModule<AppearanceRuntime>();
        if (appearance == null) return false;
        return IsHandPart(appearance.EquipmentPart);
    }

    private InventoryRuntime FindInventoryOf(EntityRuntime entity)
    {
        if (Entity == null) return null;
        foreach (var inv in Entity.GetModules<InventoryRuntime>())
            if (inv.Contains(entity)) return inv;
        return null;
    }
}
