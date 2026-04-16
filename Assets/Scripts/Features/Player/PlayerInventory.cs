using System;
using UnityEngine;

/// <summary>
/// Bridge giữa InventoryRuntime (data) và UI (view).
/// Không giữ data — chỉ trỏ tới EntityRoot.entity.
/// Đợi WorldReady trước khi init.
/// </summary>
[RequireComponent(typeof(EntityRoot))]
public class PlayerInventory : MonoBehaviour
{
    private EntityRoot _root;
    private InventoryService _inventoryService;
    private bool _ready;

    public int SelectedHotbarIndex { get; private set; }
    public EntityRuntime SelectedItem { get; private set; }

    // ── Event cho UI bind ──
    public event Action OnInventoryChanged;
    public event Action<int> OnHotbarSelectionChanged;

    // ── Shortcut ──
    private EntityRuntime Entity => _root?.GetEntity();

    private void Awake()
    {
        _root = GetComponent<EntityRoot>();
    }

    private void OnEnable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Subscribe<WorldReady>(OnWorldReady);
    }

    private void OnDisable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Unsubscribe<WorldReady>(OnWorldReady);
    }

    private void OnWorldReady(WorldReady _)
    {
        _inventoryService = GameManager.Instance.InventoryService;
        _ready = true;
        RefreshSelectedItem();
        OnInventoryChanged?.Invoke();
        Debug.Log("[PlayerInventory] Ready.");
    }

    // ══════ Hotbar ══════

    public void SelectSlot(int index)
    {
        if (!_ready) return;
        var hotbar = GetInventory(InventoryType.Hotbar);
        if (hotbar == null || index < 0 || index >= hotbar.MaxSlots) return;

        SelectedHotbarIndex = index;
        RefreshSelectedItem();
        OnHotbarSelectionChanged?.Invoke(index);
    }

    public void CycleHotbar(int delta)
    {
        if (!_ready) return;
        var hotbar = GetInventory(InventoryType.Hotbar);
        if (hotbar == null) return;

        int next = (SelectedHotbarIndex + delta + hotbar.MaxSlots) % hotbar.MaxSlots;
        SelectSlot(next);
    }

    // ══════ Pickup ══════

    public int Pickup(EntityRuntime pickupEntity)
    {
        if (!_ready || Entity == null) return 0;
        int received = _inventoryService.Pickup(pickupEntity, Entity);
        if (received > 0) NotifyChanged();
        return received;
    }

    // ══════ Consume ══════

    public bool ConsumeSelected(int amount = 1)
    {
        if (!_ready || SelectedItem == null || Entity == null) return false;
        bool ok = _inventoryService.Consume(SelectedItem, Entity, amount);
        if (ok) { RefreshSelectedItem(); NotifyChanged(); }
        return ok;
    }

    // ══════ Query ══════

    public InventoryRuntime GetInventory(InventoryType type)
    {
        if (Entity == null) return null;
        if (_inventoryService == null) return null;
        return _inventoryService.GetInventory(Entity, type);
    }

    public int CountEntity(string entityDataId)
    {
        if (Entity == null) return 0;
        return _inventoryService.CountEntity(Entity, entityDataId);
    }

    // ══════ Internal ══════

    private void RefreshSelectedItem()
    {
        var hotbar = GetInventory(InventoryType.Hotbar);
        SelectedItem = hotbar?.GetSlot(SelectedHotbarIndex)?.entity;
    }

    private void NotifyChanged()
    {
        RefreshSelectedItem();
        OnInventoryChanged?.Invoke();
    }
}
