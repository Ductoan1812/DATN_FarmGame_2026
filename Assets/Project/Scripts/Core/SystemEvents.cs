using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;

// ══════════════════════════════════════════════════════════
//  Events TOÀN CỤC (EventBus.Publish) — hậu tố "Publish" để phân biệt
//  với Module events (IGameEvent, qua EntityRuntime.TriggerEvent).
// ══════════════════════════════════════════════════════════

// ── Time / Calendar ───────────────────────────────────────

/// <summary>Backward compatible — StageObject và các system cũ vẫn dùng được.</summary>
public struct NextDayEventPublish { }

/// <summary>Sang ngày mới — chứa đầy đủ context (year, season, day).</summary>
public struct DayChangedPublish
{
    public readonly int year;
    public readonly Season season;
    public readonly int day;
    public DayChangedPublish(int year, Season season, int day)
    { this.year = year; this.season = season; this.day = day; }
}

/// <summary>Sang mùa mới.</summary>
public struct SeasonChangedPublish
{
    public readonly int year;
    public readonly Season season;
    public SeasonChangedPublish(int year, Season season)
    { this.year = year; this.season = season; }
}

/// <summary>Sang năm mới.</summary>
public struct YearChangedPublish
{
    public readonly int year;
    public YearChangedPublish(int year) { this.year = year; }
}

public struct GameTimeChangedPublish
{
    public readonly int day;
    public readonly int hour;
    public readonly int minute;
    public readonly float normalizedTime;

    public GameTimeChangedPublish(int day, int hour, int minute, float normalizedTime)
    {
        this.day = day;
        this.hour = hour;
        this.minute = minute;
        this.normalizedTime = normalizedTime;
    }
}

public struct GameHourChangedPublish
{
    public readonly int day;
    public readonly int hour;

    public GameHourChangedPublish(int day, int hour)
    {
        this.day = day;
        this.hour = hour;
    }
}

// ── Boot / Save-Load ──────────────────────────────────────

/// <summary>Phase 1: SaveLoadManager đã load/tạo xong EntityRegistry (data sẵn sàng).</summary>
public struct DataReadyPublish { }

/// <summary>Phase 2: SpawnSystem đã spawn xong tất cả GameObjects trong world.</summary>
public struct WorldObjectsSpawnedPublish { }

/// <summary>Phase 3: EntityService đã restore xong tất cả inventory slots.</summary>
public struct InventoryDataRestoredPublish { }

/// <summary>Phase 4: PlayerBridge đã bind xong Player entity ↔ UI.</summary>
public struct PlayerReadyPublish { }

/// <summary>Phase 5: Toàn bộ boot sequence hoàn tất — game sẵn sàng chơi.</summary>
public struct GameReadyPublish { }

/// <summary>Yêu cầu save game.</summary>
public struct SaveGameRequestPublish { }

/// <summary>Yêu cầu load game.</summary>
public struct LoadGameRequestPublish { }

// ══════════════════════════════════════════════════════════
//  UI Events — PlayerBridge / Service publish, UI subscribe.
// ══════════════════════════════════════════════════════════

/// <summary>Stats của entity thay đổi (HP, Mana, Attack...). PlayerBridge publish.</summary>
public struct StatsChangedPublish
{
    public readonly string entityId;
    public readonly StatType statType;
    public readonly float newValue;
    public StatsChangedPublish(string entityId, StatType statType, float newValue)
    { this.entityId = entityId; this.statType = statType; this.newValue = newValue; }
}

/// <summary>Inventory thay đổi (pickup, consume, swap...). InventoryService publish.</summary>
public struct InventoryChangedPublish
{
    public readonly string entityId;
    public readonly InventoryType inventoryType;
    public InventoryChangedPublish(string entityId, InventoryType inventoryType)
    { this.entityId = entityId; this.inventoryType = inventoryType; }
}

/// <summary>
/// Nội dung 1 slot hotbar thay đổi (icon, amount).
/// PlayerBridge publish chỉ cho slot thực sự thay đổi.
/// </summary>
public struct HotbarSlotChangedPublish
{
    public readonly int    index;
    public readonly Sprite icon;
    public readonly int    amount;
    public HotbarSlotChangedPublish(int index, Sprite icon, int amount)
    { this.index = index; this.icon = icon; this.amount = amount; }
}

/// <summary>
/// Selection hotbar thay đổi. PlayerBridge publish khi SelectedIndex thay đổi.
/// </summary>
public struct HotbarSelectionChangedPublish
{
    public readonly int selectedIndex;
    public HotbarSelectionChangedPublish(int selectedIndex)
    { this.selectedIndex = selectedIndex; }
}

/// <summary>
/// Nội dung 1 slot backpack thay đổi (icon, amount).
/// PlayerBridge publish mỗi khi backpack inventory thay đổi.
/// </summary>
public struct BackpackSlotChangedPublish
{
    public readonly int    index;
    public readonly Sprite icon;
    public readonly int    amount;
    public BackpackSlotChangedPublish(int index, Sprite icon, int amount)
    { this.index = index; this.icon = icon; this.amount = amount; }
}

/// <summary>
/// UI request chọn 1 slot backpack. BackpackUI publish, PlayerBridge subscribe.
/// </summary>
public struct BackpackSlotSelectedRequestPublish
{
    public readonly int slotIndex;
    public BackpackSlotSelectedRequestPublish(int slotIndex)
    { this.slotIndex = slotIndex; }
}

/// <summary>
/// UI request sort backpack. BackpackUI publish, PlayerBridge subscribe.
/// </summary>
public struct BackpackSortRequestPublish { }

/// <summary>
/// UI request kéo thả slot giữa backpack ↔ hotbar (hoặc trong cùng 1 inventory).
/// DraggableSlot publish, PlayerBridge subscribe.
/// </summary>
public struct SlotDragDropRequestPublish
{
    public readonly InventoryType srcType;
    public readonly int srcIndex;
    public readonly InventoryType dstType;
    public readonly int dstIndex;
    public SlotDragDropRequestPublish(InventoryType srcType, int srcIndex, InventoryType dstType, int dstIndex)
    { this.srcType = srcType; this.srcIndex = srcIndex; this.dstType = dstType; this.dstIndex = dstIndex; }
}

/// <summary>
/// UI request tách item tại slot backpack đang chọn.
/// BackpackUI publish (khi nhấn btn_Separate), PlayerBridge subscribe.
/// </summary>
public struct BackpackSplitRequestPublish
{
    public readonly int slotIndex;
    public readonly int splitAmount;
    public BackpackSplitRequestPublish(int slotIndex, int splitAmount)
    { this.slotIndex = slotIndex; this.splitAmount = splitAmount; }
}

/// <summary>
/// UI request drop item tại slot ra world.
/// BackpackUI publish (khi nhấn Btn_Drop) hoặc DraggableSlot publish (khi kéo ra ngoài).
/// PlayerBridge subscribe → spawn EntityDrop tại vị trí Player.
/// </summary>
public struct InventoryDropRequestPublish
{
    public readonly InventoryType inventoryType;
    public readonly int slotIndex;
    public InventoryDropRequestPublish(InventoryType inventoryType, int slotIndex)
    { this.inventoryType = inventoryType; this.slotIndex = slotIndex; }
}

/// <summary>
/// Dữ liệu stat để UI hiển thị trong item info panel.
/// </summary>
public struct StatDisplay
{
    public readonly StatType statType;
    public readonly float value;
    public StatDisplay(StatType statType, float value)
    { this.statType = statType; this.value = value; }
}

/// <summary>
/// PlayerBridge publish khi item info của slot backpack được chọn thay đổi.
/// </summary>
public struct BackpackItemInfoChangedPublish
{
    public readonly int slotIndex;
    public readonly bool isEmpty;
    public readonly Sprite icon;
    public readonly int amount;
    public readonly string nameKey;
    public readonly string descKey;
    public readonly string categoryKey;
    public readonly int sellPrice;
    public readonly StatDisplay[] stats;

    public BackpackItemInfoChangedPublish(
        int slotIndex,
        bool isEmpty,
        Sprite icon,
        int amount,
        string nameKey,
        string descKey,
        string categoryKey,
        int sellPrice,
        StatDisplay[] stats)
    {
        this.slotIndex = slotIndex;
        this.isEmpty = isEmpty;
        this.icon = icon;
        this.amount = amount;
        this.nameKey = nameKey;
        this.descKey = descKey;
        this.categoryKey = categoryKey;
        this.sellPrice = sellPrice;
        this.stats = stats;
    }
}

/// <summary>
/// Nội dung 1 slot equipment thay đổi. PlayerBridge publish cho EquipmentUI.
/// </summary>
public struct EquipmentSlotChangedPublish
{
    public readonly EquipmentPart part;
    public readonly bool isEmpty;
    public readonly EntityRuntime item;
    public readonly Sprite icon;

    public EquipmentSlotChangedPublish(EquipmentPart part, EntityRuntime item)
    {
        this.part = part;
        this.item = item;
        isEmpty = item == null;
        icon = item?.entityData?.icon;
    }
}

// ══════════════════════════════════════════════════════════
//  Spawn / Despawn / Destroy
// ══════════════════════════════════════════════════════════

/// <summary>
/// Yêu cầu spawn entity vào world.
/// PlacementRule từ EntityData sẽ được dùng để validate.
/// </summary>
public struct SpawnRequestPublish
{
    public readonly Vector2       worldPos;
    public readonly ObjectType    idPrefab;
    public readonly EntityRuntime runtime;
    public readonly EntityData    entityData;
    public readonly int           spawnAmount;
    public readonly bool          splitOnSpawn;
    public readonly bool          bypassValidation;
    public readonly object        payload;

    public SpawnRequestPublish(Vector2 worldPos, ObjectType idPrefab, EntityRuntime runtime, bool splitOnSpawn = false, int spawnAmount = -1, bool bypassValidation = false, object payload = null)
    {
        this.worldPos         = worldPos;
        this.idPrefab         = idPrefab;
        this.runtime          = runtime;
        this.entityData       = runtime?.entityData;
        this.splitOnSpawn     = splitOnSpawn;
        this.spawnAmount      = spawnAmount <= 0 ? runtime?.Amount ?? 1 : spawnAmount;
        this.bypassValidation = bypassValidation;
        this.payload          = payload;
    }

    public SpawnRequestPublish(Vector2 worldPos, ObjectType idPrefab, EntityData entityData, int spawnAmount = 1, bool bypassValidation = false, object payload = null)
    {
        this.worldPos         = worldPos;
        this.idPrefab         = idPrefab;
        this.runtime          = null;
        this.entityData       = entityData;
        this.splitOnSpawn     = false;
        this.spawnAmount      = spawnAmount;
        this.bypassValidation = bypassValidation;
        this.payload          = payload;
    }
}

/// <summary>Despawn entity khỏi world (giữ trong EntityRegistry để respawn).</summary>
public struct DespawnRequestPublish
{
    public readonly string idRuntime;
    public DespawnRequestPublish(string idRuntime) { this.idRuntime = idRuntime; }
}

/// <summary>Hủy entity vĩnh viễn: despawn + Unregister.</summary>
public struct DestroyEntityRequestPublish
{
    public readonly string idRuntime;
    public DestroyEntityRequestPublish(string idRuntime) { this.idRuntime = idRuntime; }
}
