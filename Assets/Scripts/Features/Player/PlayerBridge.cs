using UnityEngine;

/// <summary>
/// Cầu nối giữa Entity layer (C# thuần) ↔ System/UI layer (EventBus).
/// Gắn trên Player GameObject cùng EntityRoot.
/// Mỗi vùng (region) tương ứng 1 mảng UI mà Bridge chịu trách nhiệm publish.
/// </summary>
public class PlayerBridge : MonoBehaviour
{
    private EntityRoot _entityRoot;
    private EntityRuntime _entity;
    private EventBus _eventBus;

    // ── Hotbar ──
    private InventoryRuntime _hotbar;

    // ── Backpack ──
    private InventoryRuntime _backpack;

    // ── Snapshot để so sánh thay đổi ──
    private struct SlotSnapshot
    {
        public Sprite icon;
        public int amount;
    }

    private SlotSnapshot[] _hotbarSnapshot;
    private SlotSnapshot[] _backpackSnapshot;

    // ══════════════════════════════════════════════════════════
    //  Lifecycle
    // ══════════════════════════════════════════════════════════

    private void Start()
    {
        _entityRoot = GetComponent<EntityRoot>();
        _eventBus = GameManager.Instance?.EventBus;
    }

    private void OnEnable()
    {
        var eb = GameManager.Instance?.EventBus;
        if (eb != null) eb.Subscribe<WorldReadyPublish>(OnWorldReady);
    }

    private void OnDisable()
    {
        Unbind();
        var eb = GameManager.Instance?.EventBus;
        if (eb != null) eb.Unsubscribe<WorldReadyPublish>(OnWorldReady);
    }

    private void OnWorldReady(WorldReadyPublish _) => Bind();

    // ══════════════════════════════════════════════════════════
    //  Bind / Unbind
    // ══════════════════════════════════════════════════════════

    private void Bind()
    {
        if (_entityRoot == null) _entityRoot = GetComponent<EntityRoot>();
        _entity = _entityRoot?.GetEntity();
        _eventBus = GameManager.Instance?.EventBus;
        if (_entity == null || _eventBus == null) return;

        BindStats();
        BindHotbar();
        BindBackpack();

        Debug.Log("[PlayerBridge] Bound to entity: " + _entity.entityData?.keyName);
    }

    private void Unbind()
    {
        UnbindStats();
        UnbindHotbar();
        UnbindBackpack();
        _entity = null;
    }

    // ══════════════════════════════════════════════════════════
    //  #region STATS
    // ══════════════════════════════════════════════════════════

    private void BindStats()
    {
        _entity.stats.OnChanged += OnStatsChanged;
    }

    private void UnbindStats()
    {
        if (_entity != null)
            _entity.stats.OnChanged -= OnStatsChanged;
    }

    private void OnStatsChanged(StatType statType, float newValue)
    {
        _eventBus?.Publish(new StatsChangedPublish(_entity.id, statType, newValue));
    }

    // ══════════════════════════════════════════════════════════
    //  #region HOTBAR
    // ══════════════════════════════════════════════════════════

    private void BindHotbar()
    {
        _hotbar = _entity.GetModules<InventoryRuntime>()
                         .Find(i => i.Type == InventoryType.Hotbar);

        if (_hotbar != null)
        {
            _hotbar.OnSelectionChanged += OnHotbarSelectionChanged;
            _hotbarSnapshot = new SlotSnapshot[_hotbar.MaxSlots];
        }

        _eventBus.Subscribe<InventoryChangedPublish>(OnHotbarInventoryChanged);

        PublishAllHotbarSlots();
    }

    private void UnbindHotbar()
    {
        if (_hotbar != null)
            _hotbar.OnSelectionChanged -= OnHotbarSelectionChanged;

        _eventBus?.Unsubscribe<InventoryChangedPublish>(OnHotbarInventoryChanged);
        _hotbar = null;
        _hotbarSnapshot = null;
    }

    private void OnHotbarSelectionChanged(int slotIndex)
    {
        _eventBus?.Publish(new HotbarSelectionChangedPublish(slotIndex));
    }

    private void OnHotbarInventoryChanged(InventoryChangedPublish e)
    {
        if (_entity == null || e.entityId != _entity.id) return;
        if (e.inventoryType != InventoryType.Hotbar) return;
        PublishChangedHotbarSlots();
    }

    private void PublishChangedHotbarSlots()
    {
        if (_hotbar == null || _eventBus == null || _hotbarSnapshot == null) return;

        for (int i = 0; i < _hotbar.MaxSlots; i++)
        {
            var slot = _hotbar.GetSlot(i);
            bool empty = slot == null || slot.IsEmpty;

            Sprite icon = empty ? null : slot.entity.entityData.icon;
            int amount = empty ? 0 : slot.entity.Amount;

            if (_hotbarSnapshot[i].icon != icon || _hotbarSnapshot[i].amount != amount)
            {
                _hotbarSnapshot[i].icon = icon;
                _hotbarSnapshot[i].amount = amount;

                _eventBus.Publish(new HotbarSlotChangedPublish(i, icon, amount));
            }
        }
    }

    private void PublishAllHotbarSlots()
    {
        if (_hotbar == null || _eventBus == null) return;

        if (_hotbarSnapshot == null)
            _hotbarSnapshot = new SlotSnapshot[_hotbar.MaxSlots];

        int selected = _hotbar.SelectedIndex;

        for (int i = 0; i < _hotbar.MaxSlots; i++)
        {
            var slot = _hotbar.GetSlot(i);
            bool empty = slot == null || slot.IsEmpty;

            Sprite icon = empty ? null : slot.entity.entityData.icon;
            int amount = empty ? 0 : slot.entity.Amount;

            _hotbarSnapshot[i].icon = icon;
            _hotbarSnapshot[i].amount = amount;

            _eventBus.Publish(new HotbarSlotChangedPublish(i, icon, amount));
        }

        _eventBus.Publish(new HotbarSelectionChangedPublish(selected));
    }

    // ══════════════════════════════════════════════════════════
    //  #region BACKPACK
    // ══════════════════════════════════════════════════════════

    private void BindBackpack()
    {
        _backpack = _entity.GetModules<InventoryRuntime>()
                           .Find(i => i.Type == InventoryType.Backpack);

        if (_backpack != null)
            _backpackSnapshot = new SlotSnapshot[_backpack.MaxSlots];

        _eventBus.Subscribe<InventoryChangedPublish>(OnBackpackInventoryChanged);

        PublishAllBackpackSlots();
    }

    private void UnbindBackpack()
    {
        _eventBus?.Unsubscribe<InventoryChangedPublish>(OnBackpackInventoryChanged);
        _backpack = null;
        _backpackSnapshot = null;
    }

    private void OnBackpackInventoryChanged(InventoryChangedPublish e)
    {
        if (_entity == null || e.entityId != _entity.id) return;
        if (e.inventoryType != InventoryType.Backpack) return;
        PublishChangedBackpackSlots();
    }

    private void PublishChangedBackpackSlots()
    {
        if (_backpack == null || _eventBus == null || _backpackSnapshot == null) return;

        for (int i = 0; i < _backpack.MaxSlots; i++)
        {
            var slot = _backpack.GetSlot(i);
            bool empty = slot == null || slot.IsEmpty;

            Sprite icon = empty ? null : slot.entity.entityData.icon;
            int amount = empty ? 0 : slot.entity.Amount;

            if (_backpackSnapshot[i].icon != icon || _backpackSnapshot[i].amount != amount)
            {
                _backpackSnapshot[i].icon = icon;
                _backpackSnapshot[i].amount = amount;

                _eventBus.Publish(new BackpackSlotChangedPublish(i, icon, amount));
            }
        }
    }

    private void PublishAllBackpackSlots()
    {
        if (_backpack == null || _eventBus == null) return;

        if (_backpackSnapshot == null)
            _backpackSnapshot = new SlotSnapshot[_backpack.MaxSlots];

        for (int i = 0; i < _backpack.MaxSlots; i++)
        {
            var slot = _backpack.GetSlot(i);
            bool empty = slot == null || slot.IsEmpty;

            Sprite icon = empty ? null : slot.entity.entityData.icon;
            int amount = empty ? 0 : slot.entity.Amount;

            _backpackSnapshot[i].icon = icon;
            _backpackSnapshot[i].amount = amount;

            _eventBus.Publish(new BackpackSlotChangedPublish(i, icon, amount));
        }
    }

    /// <summary>
    /// Public — UI (btn_sort) gọi trực tiếp.
    /// Sort backpack rồi publish lại toàn bộ slot.
    /// </summary>
    public void BridgeSort()
    {
        if (_entity == null || _backpack == null) return;

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null) return;

        inventoryService.Sort(_entity, InventoryType.Backpack);
        PublishAllBackpackSlots();
    }

    // ══════════════════════════════════════════════════════════
    //  #region ... (thêm vùng mới ở đây cho UI khác)
    // ══════════════════════════════════════════════════════════
}
