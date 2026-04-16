using UnityEngine;

// ── Farming ───────────────────────────────────────────────
public struct NextDayEventPublish { }

// ── Boot / Save-Load ──────────────────────────────────────
/// <summary>Broadcast sau khi toàn bộ world đã load xong (entities + gameobjects).</summary>
public struct WorldReady { }

/// <summary>Yêu cầu save game.</summary>
public struct SaveGameRequest { }

/// <summary>Yêu cầu load game.</summary>
public struct LoadGameRequest { }

// ── Spawn ─────────────────────────────────────────────────

/// <summary>
/// Yêu cầu spawn entity vào world.
/// PlacementRule từ EntityData sẽ được dùng để validate.
/// </summary>
public struct SpawnRequest
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
    public SpawnRequest(Vector2 worldPos, ObjectType idPrefab, EntityRuntime runtime, bool splitOnSpawn = false, int spawnAmount = -1, bool bypassValidation = false, object payload = null)
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
    public SpawnRequest(Vector2 worldPos, ObjectType idPrefab, EntityData entityData, int spawnAmount = 1, bool bypassValidation = false, object payload = null)
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

/// <summary>Despawn entity theo runtime ID.</summary>
public struct DespawnRequest
{
    public readonly string idRuntime;
    public DespawnRequest(string idRuntime) { this.idRuntime = idRuntime; }
}


