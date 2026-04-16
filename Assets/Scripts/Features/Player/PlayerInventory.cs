using System;
using UnityEngine;

/// <summary>
/// Bridge giữa InventoryRuntime (data) và UI (view).
/// Không giữ data — chỉ forward refs từ InventoryRuntime.
/// Đợi WorldReady trước khi init.
/// </summary>
[RequireComponent(typeof(EntityRoot))]
public class PlayerInventory : MonoBehaviour
{
    private EntityRoot _root;
    private InventoryService _inventoryService;
    private bool _ready;

    // ── Refs từ Runtime (không giữ data) ──
    public int SelectedHotbarIndex => GetHotbar()?.SelectedIndex ?? 0;
    public EntityRuntime SelectedItem => GetHotbar()?.SelectedEntity;

    // ── Event cho UI bind ──
    public event Action OnInventoryChanged;
    public event Action<int> OnHotbarSelectionChanged;

    // ── Shortcut ──
    private EntityRuntime Entity => _root?.GetEntity();

    // Cache hotbar ref (invalidate khi chưa ready)
    private InventoryRuntime _cachedHotbar;

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
        // Unsubscribe hotbar event
        if (_cachedHotbar != null)
            _cachedHotbar.OnSelectionChanged -= ForwardSelectionChanged;

        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Unsubscribe<WorldReady>(OnWorldReady);
    }

    private void OnWorldReady(WorldReady _)
    {
        _inventoryService = GameManager.Instance.InventoryService;
        _ready = true;

        // Cache + subscribe hotbar selection event
        _cachedHotbar = GetInventory(InventoryType.Hotbar);
        if (_cachedHotbar != null)
            _cachedHotbar.OnSelectionChanged += ForwardSelectionChanged;

        OnInventoryChanged?.Invoke();
        Debug.Log("[PlayerInventory] Ready.");
    }

    // ══════ Hotbar ══════

    public void SelectSlot(int index)
    {
        if (!_ready) return;
        GetHotbar()?.SelectSlot(index);
    }

    public void CycleHotbar(int delta)
    {
        if (!_ready) return;
        GetHotbar()?.CycleSelection(delta);
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
        if (ok) NotifyChanged();
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

    private InventoryRuntime GetHotbar()
    {
        return _cachedHotbar ?? GetInventory(InventoryType.Hotbar);
    }

    /// <summary>Forward event từ InventoryRuntime → UI.</summary>
    private void ForwardSelectionChanged(int index)
    {
        OnHotbarSelectionChanged?.Invoke(index);
    }

    private void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
