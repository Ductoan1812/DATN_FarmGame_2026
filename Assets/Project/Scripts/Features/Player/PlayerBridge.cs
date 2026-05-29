using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;
using System.Collections.Generic;

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
    private bool _bound;

    // ── Hotbar ──
    private InventoryRuntime _hotbar;

    // ── Backpack ──
    private InventoryRuntime _backpack;
    private int _selectedBackpackIndex = -1;

    // ── Equipment ──
    private PlayerEquipment _playerEquipment;
    private EquipmentRuntime _equipment;

    // ── Snapshot để so sánh thay đổi ──
    private struct SlotSnapshot
    {
        public Sprite icon;
        public int amount;
        public bool hasMeter;
        public int meterCurrent;
        public int meterMax;
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
        Bind();
    }

    private void Update()
    {
        if (!_bound)
            Bind();
    }

    private void OnEnable()
    {
        var eb = GameManager.Instance?.EventBus;
        if (eb != null) eb.Subscribe<InventoryDataRestoredPublish>(OnInventoryDataRestored);
    }

    private void OnDisable()
    {
        Unbind();
        var eb = GameManager.Instance?.EventBus;
        if (eb != null) eb.Unsubscribe<InventoryDataRestoredPublish>(OnInventoryDataRestored);
    }

    private void OnInventoryDataRestored(InventoryDataRestoredPublish _)
    {
        Bind();
    }

    // ══════════════════════════════════════════════════════════
    //  Bind / Unbind
    // ══════════════════════════════════════════════════════════

    private void Bind()
    {
        if (_bound) return;

        if (_entityRoot == null) _entityRoot = GetComponent<EntityRoot>();
        _entity = _entityRoot?.GetEntity();
        _eventBus = GameManager.Instance?.EventBus;
        if (_entity == null || _eventBus == null) return;

        BindStats();
        BindHotbar();
        BindBackpack();
        BindEquipment();
        BindDragDrop();
        BindVisualRefresh();

        _bound = true;
        PublishPlayerInfo();
        _eventBus.Publish(new PlayerReadyPublish());
        Debug.Log("[PlayerBridge] PlayerReadyPublish published.");
        Debug.Log("[PlayerBridge] Bound to entity: " + _entity.entityData?.keyName);
    }

    private void Unbind()
    {
        if (!_bound) return;

        UnbindStats();
        UnbindHotbar();
        UnbindBackpack();
        UnbindEquipment();
        UnbindDragDrop();
        UnbindVisualRefresh();
        _entity = null;
        _bound = false;
    }

    // ══════════════════════════════════════════════════════════
    //  UI Refresh Request
    // ══════════════════════════════════════════════════════════

    private void BindVisualRefresh()
    {
        _eventBus.Subscribe<InventoryVisualRefreshRequestPublish>(OnInventoryVisualRefreshRequest);
    }

    private void UnbindVisualRefresh()
    {
        _eventBus?.Unsubscribe<InventoryVisualRefreshRequestPublish>(OnInventoryVisualRefreshRequest);
    }

    private void OnInventoryVisualRefreshRequest(InventoryVisualRefreshRequestPublish _)
    {
        PublishPlayerInfo();
        PublishAllHotbarSlots();
        PublishAllBackpackSlots();
        PublishBackpackSelection(_selectedBackpackIndex);
        PublishAllEquipmentSlots();
    }

    private void PublishPlayerInfo()
    {
        if (_entity == null || _eventBus == null) return;

        _eventBus.Publish(new PlayerInfoChangedPublish(
            _entity.id,
            _entity.entityData != null ? _entity.entityData.keyName : string.Empty,
            BuildPlayerStatDisplays()));
    }

    private StatDisplay[] BuildPlayerStatDisplays()
    {
        if (_entity?.stats == null)
            return System.Array.Empty<StatDisplay>();

        var result = new List<StatDisplay>();

        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
        {
            if (!_entity.stats.Has(statType)) continue;
            result.Add(new StatDisplay(statType, _entity.stats.Get(statType)));
        }

        return result.ToArray();
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
        PublishPlayerInfo();
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
            SlotResourceMeterData meter = empty ? SlotResourceMeterData.None : BuildSlotResourceMeter(slot.entity);

            if (_hotbarSnapshot[i].icon != icon ||
                _hotbarSnapshot[i].amount != amount ||
                _hotbarSnapshot[i].hasMeter != meter.hasMeter ||
                _hotbarSnapshot[i].meterCurrent != meter.current ||
                _hotbarSnapshot[i].meterMax != meter.max)
            {
                _hotbarSnapshot[i].icon = icon;
                _hotbarSnapshot[i].amount = amount;
                _hotbarSnapshot[i].hasMeter = meter.hasMeter;
                _hotbarSnapshot[i].meterCurrent = meter.current;
                _hotbarSnapshot[i].meterMax = meter.max;

                _eventBus.Publish(new HotbarSlotChangedPublish(i, icon, amount, meter));
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
            SlotResourceMeterData meter = empty ? SlotResourceMeterData.None : BuildSlotResourceMeter(slot.entity);

            _hotbarSnapshot[i].icon = icon;
            _hotbarSnapshot[i].amount = amount;
            _hotbarSnapshot[i].hasMeter = meter.hasMeter;
            _hotbarSnapshot[i].meterCurrent = meter.current;
            _hotbarSnapshot[i].meterMax = meter.max;

            _eventBus.Publish(new HotbarSlotChangedPublish(i, icon, amount, meter));
        }

        _eventBus.Publish(new HotbarSelectionChangedPublish(selected));
    }

    // ══════════════════════════════════════════════════════════
    //  BACKPACK - Bind / Unbind
    // ══════════════════════════════════════════════════════════

    private void BindBackpack()
    {
        _backpack = _entity.GetModules<InventoryRuntime>()
                           .Find(i => i.Type == InventoryType.Backpack);

        if (_backpack != null)
            _backpackSnapshot = new SlotSnapshot[_backpack.MaxSlots];

        _eventBus.Subscribe<InventoryChangedPublish>(OnBackpackInventoryChanged);
        _eventBus.Subscribe<BackpackSlotSelectedRequestPublish>(OnBackpackSlotSelectedRequest);
        _eventBus.Subscribe<BackpackSlotPreviewRequestPublish>(OnBackpackSlotPreviewRequest);
        _eventBus.Subscribe<BackpackSortRequestPublish>(OnBackpackSortRequest);
        _eventBus.Subscribe<BackpackSplitRequestPublish>(OnBackpackSplitRequest);
        _eventBus.Subscribe<InventoryDropRequestPublish>(OnInventoryDropRequest);

        PublishAllBackpackSlots();
        PublishBackpackSelection(-1);
    }

    private void UnbindBackpack()
    {
        _eventBus?.Unsubscribe<InventoryChangedPublish>(OnBackpackInventoryChanged);
        _eventBus?.Unsubscribe<BackpackSlotSelectedRequestPublish>(OnBackpackSlotSelectedRequest);
        _eventBus?.Unsubscribe<BackpackSlotPreviewRequestPublish>(OnBackpackSlotPreviewRequest);
        _eventBus?.Unsubscribe<BackpackSortRequestPublish>(OnBackpackSortRequest);
        _eventBus?.Unsubscribe<BackpackSplitRequestPublish>(OnBackpackSplitRequest);
        _eventBus?.Unsubscribe<InventoryDropRequestPublish>(OnInventoryDropRequest);
        _backpack = null;
        _backpackSnapshot = null;
        _selectedBackpackIndex = -1;
    }

    // ══════════════════════════════════════════════════════════
    //  BACKPACK - Subscribe Event Handlers
    //  UI/Service -> PlayerBridge
    // ══════════════════════════════════════════════════════════

    private void OnBackpackInventoryChanged(InventoryChangedPublish e)
    {
        if (_entity == null || e.entityId != _entity.id) return;
        if (e.inventoryType != InventoryType.Backpack) return;
        PublishChangedBackpackSlots();
        PublishBackpackSelection(_selectedBackpackIndex);
    }

    private void OnBackpackSlotSelectedRequest(BackpackSlotSelectedRequestPublish e)
    {
        PublishBackpackSelection(e.slotIndex);
    }

    private void OnBackpackSlotPreviewRequest(BackpackSlotPreviewRequestPublish e)
    {
        PublishBackpackItemInfo(e.slotIndex, isPreview: true);
    }

    private void OnBackpackSortRequest(BackpackSortRequestPublish _)
    {
        BridgeSort();
    }

    private void OnBackpackSplitRequest(BackpackSplitRequestPublish e)
    {
        if (_entity == null || _backpack == null) return;
        if (e.slotIndex < 0 || e.slotIndex >= _backpack.MaxSlots) return;

        var slot = _backpack.GetSlot(e.slotIndex);
        if (slot == null || slot.IsEmpty) return;

        var entity = slot.entity;
        int splitAmount = Mathf.Clamp(e.splitAmount, 1, entity.Amount - 1);
        if (splitAmount <= 0) return; // Không thể tách nếu chỉ còn 1

        var entityService = GameManager.Instance?.EntityService;
        var inventoryService = GameManager.Instance?.InventoryService;
        if (entityService == null || inventoryService == null) return;

        // Split tạo entity mới
        var newEntity = entityService.Split(entity, splitAmount);
        if (newEntity == null) return;

        // Pickup entity mới vào backpack (tìm slot trống)
        int received = inventoryService.Pickup(newEntity, _entity);
        if (received <= 0)
        {
            // Không có chỗ → merge lại
            entityService.Merge(entity, newEntity);
            Debug.LogWarning("[PlayerBridge] Split failed: backpack đầy, không có slot trống.");
        }

        // InventoryService.Pickup đã publish InventoryChangedPublish
        // → OnBackpackInventoryChanged sẽ tự refresh UI.
    }

    private void OnInventoryDropRequest(InventoryDropRequestPublish e)
    {
        if (_entity == null) return;

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null) return;

        var inv = inventoryService.GetInventory(_entity, e.inventoryType);
        if (inv == null) return;
        if (e.slotIndex < 0 || e.slotIndex >= inv.MaxSlots) return;

        var slot = inv.GetSlot(e.slotIndex);
        if (slot == null || slot.IsEmpty) return;

        var entity = slot.entity;

        // Lấy vị trí Player từ Owner (chính là EntityRoot trên Player GameObject)
        Vector2 dropPos = Vector2.zero;
        if (_entityRoot != null)
            dropPos = (Vector2)_entityRoot.transform.position;

        // Clear slot
        inv.ClearSlot(e.slotIndex);

        // Publish InventoryChanged để UI refresh
        _eventBus?.Publish(new InventoryChangedPublish(_entity.id, e.inventoryType));

        // Spawn EntityDrop ra world — bypassValidation vì item rơi tự do, không bị ràng buộc placement
        _eventBus?.Publish(new SpawnRequestPublish(
            dropPos,
            ObjectType.EntityDrop,
            entity,
            bypassValidation: true));
    }

    // ══════════════════════════════════════════════════════════
    //  BACKPACK - Publish Events
    //  PlayerBridge -> UI
    // ══════════════════════════════════════════════════════════

    private void PublishChangedBackpackSlots()
    {
        if (_backpack == null || _eventBus == null || _backpackSnapshot == null) return;

        for (int i = 0; i < _backpack.MaxSlots; i++)
        {
            var slot = _backpack.GetSlot(i);
            bool empty = slot == null || slot.IsEmpty;

            Sprite icon = empty ? null : slot.entity.entityData.icon;
            int amount = empty ? 0 : slot.entity.Amount;
            SlotResourceMeterData meter = empty ? SlotResourceMeterData.None : BuildSlotResourceMeter(slot.entity);

            if (_backpackSnapshot[i].icon != icon ||
                _backpackSnapshot[i].amount != amount ||
                _backpackSnapshot[i].hasMeter != meter.hasMeter ||
                _backpackSnapshot[i].meterCurrent != meter.current ||
                _backpackSnapshot[i].meterMax != meter.max)
            {
                _backpackSnapshot[i].icon = icon;
                _backpackSnapshot[i].amount = amount;
                _backpackSnapshot[i].hasMeter = meter.hasMeter;
                _backpackSnapshot[i].meterCurrent = meter.current;
                _backpackSnapshot[i].meterMax = meter.max;

                _eventBus.Publish(new BackpackSlotChangedPublish(i, icon, amount, meter));
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
            SlotResourceMeterData meter = empty ? SlotResourceMeterData.None : BuildSlotResourceMeter(slot.entity);

            _backpackSnapshot[i].icon = icon;
            _backpackSnapshot[i].amount = amount;
            _backpackSnapshot[i].hasMeter = meter.hasMeter;
            _backpackSnapshot[i].meterCurrent = meter.current;
            _backpackSnapshot[i].meterMax = meter.max;

            _eventBus.Publish(new BackpackSlotChangedPublish(i, icon, amount, meter));
        }
    }

    private static SlotResourceMeterData BuildSlotResourceMeter(EntityRuntime entity)
    {
        if (entity == null)
            return SlotResourceMeterData.None;

        var wateringCan = entity.GetModule<WateringCanRuntime>();
        if (wateringCan == null)
            return SlotResourceMeterData.None;

        wateringCan.EnsureInitialized(entity);

        if (wateringCan.MaxCharges <= 0)
            return SlotResourceMeterData.None;

        return new SlotResourceMeterData(
            true,
            wateringCan.CurrentCharges,
            wateringCan.MaxCharges,
            new Color(0.24f, 0.74f, 0.98f, 1f));
    }

    private void PublishBackpackSelection(int slotIndex)
    {
        if (_eventBus == null || _backpack == null)
            return;

        _selectedBackpackIndex = slotIndex;
        PublishBackpackItemInfo(slotIndex, isPreview: false);
    }

    private void PublishBackpackItemInfo(int slotIndex, bool isPreview)
    {
        if (_eventBus == null || _backpack == null)
            return;

        if (slotIndex < 0 || slotIndex >= _backpack.MaxSlots)
        {
            _eventBus.Publish(new BackpackItemInfoChangedPublish(
                slotIndex,
                isPreview,
                true,
                null,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                System.Array.Empty<StatDisplay>()));
            return;
        }

        var slot = _backpack.GetSlot(slotIndex);
        bool empty = slot == null || slot.IsEmpty || slot.entity == null || slot.entity.entityData == null;
        if (empty)
        {
            _eventBus.Publish(new BackpackItemInfoChangedPublish(
                slotIndex,
                isPreview,
                true,
                null,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                System.Array.Empty<StatDisplay>()));
            return;
        }

        var entity = slot.entity;
        var data = entity.entityData;

        _eventBus.Publish(new BackpackItemInfoChangedPublish(
            slotIndex,
            isPreview,
            false,
            data.icon,
            entity.Amount,
            data.keyName,
            data.descKey,
            GetCategoryKey(data.category),
            data.sellPrice,
            BuildStatDisplays(entity)));
    }

    private StatDisplay[] BuildStatDisplays(EntityRuntime entity)
    {
        if (entity?.entityData?.baseStats?.baseStats == null)
            return System.Array.Empty<StatDisplay>();

        var result = new List<StatDisplay>();
        var seen = new HashSet<StatType>();

        foreach (var entry in entity.entityData.baseStats.baseStats)
        {
            if (entry == null) continue;
            if (!seen.Add(entry.statType)) continue;

            float value = entity.stats.Get(entry.statType);
            if (Mathf.Approximately(value, 0f)) continue;

            result.Add(new StatDisplay(entry.statType, value));
        }

        return result.ToArray();
    }

    private string GetCategoryKey(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon: return LocalizationKeys.UiCategoryWeapon;
            case ItemCategory.Tool: return LocalizationKeys.UiCategoryTool;
            case ItemCategory.Material: return LocalizationKeys.UiCategoryMaterial;
            case ItemCategory.Food: return LocalizationKeys.UiCategoryFood;
            case ItemCategory.Consumable: return LocalizationKeys.UiCategoryPotion;
            default:
                return "ui.category." + category.ToString().ToLowerInvariant();
        }
    }

    /// <summary>
    /// Sort backpack rồi publish lại toàn bộ slot.
    /// Hiện được gọi từ OnBackpackSortRequest để UI không gọi trực tiếp vào bridge.
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
    //  #region EQUIPMENT
    // ══════════════════════════════════════════════════════════

    private void BindEquipment()
    {
        _playerEquipment = GetComponent<PlayerEquipment>();
        _equipment = _entity.GetModule<EquipmentRuntime>();

        if (_playerEquipment != null)
        {
            _playerEquipment.OnEquipmentChanged += OnEquipmentChanged;
        }

        PublishAllEquipmentSlots();
    }

    private void UnbindEquipment()
    {
        if (_playerEquipment != null)
        {
            _playerEquipment.OnEquipmentChanged -= OnEquipmentChanged;
        }

        _playerEquipment = null;
        _equipment = null;
    }

    private void OnEquipmentChanged()
    {
        PublishAllEquipmentSlots();
    }

    private void PublishAllEquipmentSlots()
    {
        if (_equipment == null || _eventBus == null)
        {
            return;
        }

        foreach (EquipmentPart part in System.Enum.GetValues(typeof(EquipmentPart)))
        {
            _eventBus.Publish(new EquipmentSlotChangedPublish(part, _equipment.Get(part)));
        }
    }

    // ══════════════════════════════════════════════════════════
    //  #region DRAG & DROP
    // ══════════════════════════════════════════════════════════

    private void BindDragDrop()
    {
        _eventBus.Subscribe<SlotDragDropRequestPublish>(OnSlotDragDrop);
    }

    private void UnbindDragDrop()
    {
        _eventBus?.Unsubscribe<SlotDragDropRequestPublish>(OnSlotDragDrop);
    }

    private void OnSlotDragDrop(SlotDragDropRequestPublish e)
    {
        if (_entity == null) return;

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null) return;

        inventoryService.SwapSlots(
            _entity, e.srcType, e.srcIndex,
            _entity, e.dstType, e.dstIndex);

        // SwapSlots đã gọi PublishChanged → InventoryChangedPublish
        // → OnHotbarInventoryChanged / OnBackpackInventoryChanged sẽ tự publish lại slot data.
        // Nhưng nếu swap cross-inventory, cần đảm bảo cả hai bên đều refresh.
    }

    // ══════════════════════════════════════════════════════════
    //  #region ... (thêm vùng mới ở đây cho UI khác)
    // ══════════════════════════════════════════════════════════
}
