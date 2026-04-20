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

// ── Spawn / Despawn / Destroy ─────────────────────────────

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
    public readonly bool          splitOnSpawn; // true = Split 1 unit từ runtime trước khi spawn
    public readonly bool          bypassValidation;
    public readonly object        payload;


    /// <summary>Spawn từ EntityRuntime có sẵn.</summary>
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

    /// <summary>Spawn tạo mới từ EntityData.</summary>
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

/// <summary>
/// Despawn entity khỏi world (chỉ remove GameObject + spatial position).
/// Entity runtime VẪN nằm trong EntityRegistry — dùng để respawn lại sau này.
/// </summary>
public struct DespawnRequestPublish
{
    public readonly string idRuntime;
    public DespawnRequestPublish(string idRuntime) { this.idRuntime = idRuntime; }
}

/// <summary>
/// Hủy entity vĩnh viễn: despawn GameObject + Unregister khỏi EntityRegistry.
/// MortalRuntime publish event này khi entity chết mà KHÔNG có RespawnModule.
/// </summary>
public struct DestroyEntityRequestPublish
{
    public readonly string idRuntime;
    public DestroyEntityRequestPublish(string idRuntime) { this.idRuntime = idRuntime; }
}
