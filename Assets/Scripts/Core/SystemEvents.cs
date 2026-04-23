using UnityEngine;

// ══════════════════════════════════════════════════════════
//  Events TOÀN CỤC (EventBus.Publish) — hậu tố "Publish" để phân biệt
//  với Module events (IGameEvent, qua EntityRuntime.TriggerEvent).
// ══════════════════════════════════════════════════════════

// ── Farming ───────────────────────────────────────────────
public struct NextDayEventPublish { }

// ── Boot / Save-Load ──────────────────────────────────────

/// <summary>Broadcast sau khi toàn bộ world đã load xong (entities + gameobjects).</summary>
public struct WorldReadyPublish { }

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
