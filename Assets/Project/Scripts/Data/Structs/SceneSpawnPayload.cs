using System;
using UnityEngine;

public enum SceneSpawnRuleMode
{
    SpawnOnce,
    RespawnTopUp,
    SpawnFreshOnSceneLoad
}

[Serializable]
public class SceneSpawnRuleEntry
{
    [Tooltip("ID riêng cho entry này trong cùng vùng. Nếu để trống sẽ fallback theo marker tile.")]
    public string entryId;

    [Tooltip("Marker tile gốc dùng làm template spawn.")]
    public SceneSpawnTile markerTile;

    [Min(0)] public int initialCountMin = 1;
    [Min(0)] public int initialCountMax = 1;

    public SceneSpawnRuleMode spawnMode = SceneSpawnRuleMode.SpawnOnce;

    [Header("Respawn Top-up")]
    [Min(0f)] public float respawnDelaySeconds = 30f;
    [Min(0)] public int respawnCountMin = 1;
    [Min(0)] public int respawnCountMax = 1;

    [Tooltip("Cho phép override spawnGroupId của marker tile khi spawn từ vùng.")]
    public string spawnGroupOverride;

    public string ResolveEntryKey()
    {
        if (!string.IsNullOrWhiteSpace(entryId))
            return entryId.Trim();

        if (markerTile != null)
        {
            if (!string.IsNullOrWhiteSpace(markerTile.spawnGroupId))
                return markerTile.spawnGroupId.Trim();

            return markerTile.objectType.ToString();
        }

        return "entry";
    }

    public string ResolveSpawnGroupId()
    {
        if (!string.IsNullOrWhiteSpace(spawnGroupOverride))
            return spawnGroupOverride.Trim();

        return markerTile != null ? markerTile.spawnGroupId : string.Empty;
    }

    public int RollInitialCount() => RollInclusive(initialCountMin, initialCountMax);

    public int RollRespawnCount() => RollInclusive(respawnCountMin, respawnCountMax);

    private static int RollInclusive(int a, int b)
    {
        int min = Mathf.Max(0, Mathf.Min(a, b));
        int max = Mathf.Max(0, Mathf.Max(a, b));
        return UnityEngine.Random.Range(min, max + 1);
    }
}

[Serializable]
public class SceneSpawnRuleEntryState
{
    public string stateId;
    public bool hasInitialized;
    public int targetCount;
    public float nextRespawnAtElapsedSeconds;
}

[System.Serializable]
public class SceneSpawnPayload
{
    private const string RuleRegionToken = "RuleRegion";

    public string sceneName;
    public SceneMarkerKind markerKind = SceneMarkerKind.Object;
    public ObjectType objectType;
    public Vector3Int cell;
    public string spawnGroupId;
    public string persistentId;
    public SceneEntitySavePolicy savePolicy = SceneEntitySavePolicy.Persistent;
    public int respawnMinutes;
    public int initialAmount = 1;
    public int availableAtGameMinute;
    public int startStageIndex = -1;

    public static string BuildPersistentId(
        string sceneName,
        SceneMarkerKind markerKind,
        ObjectType objectType,
        Vector3Int cell,
        string spawnGroupId)
    {
        string safeScene = string.IsNullOrWhiteSpace(sceneName) ? "Scene" : sceneName.Trim();
        string safeGroup = string.IsNullOrWhiteSpace(spawnGroupId) ? "default" : spawnGroupId.Trim();
        return $"{safeScene}:{markerKind}:{objectType}:{cell.x}_{cell.y}_{cell.z}:{safeGroup}";
    }

    public static string BuildRuleRegionStateId(string sceneName, string regionId, string entryKey)
    {
        string safeScene = SanitizeKey(sceneName, "Scene");
        string safeRegion = SanitizeKey(regionId, "region");
        string safeEntry = SanitizeKey(entryKey, "entry");
        return $"{safeScene}:{RuleRegionToken}:{safeRegion}:{safeEntry}:state";
    }

    public static string BuildRuleRegionPersistentId(string sceneName, string regionId, string entryKey, Vector3Int cell)
    {
        string safeScene = SanitizeKey(sceneName, "Scene");
        string safeRegion = SanitizeKey(regionId, "region");
        string safeEntry = SanitizeKey(entryKey, "entry");
        return $"{safeScene}:{RuleRegionToken}:{safeRegion}:{safeEntry}:{cell.x}_{cell.y}_{cell.z}";
    }

    public static string BuildRuleRegionPersistentPrefix(string sceneName, string regionId, string entryKey)
    {
        string safeScene = SanitizeKey(sceneName, "Scene");
        string safeRegion = SanitizeKey(regionId, "region");
        string safeEntry = SanitizeKey(entryKey, "entry");
        return $"{safeScene}:{RuleRegionToken}:{safeRegion}:{safeEntry}:";
    }

    public static bool IsRuleRegionPersistentId(string persistentId)
    {
        return TryParseRuleRegionPersistentId(persistentId, out _, out _, out _, out _);
    }

    public static bool TryParseRuleRegionPersistentId(
        string persistentId,
        out string sceneName,
        out string regionId,
        out string entryKey,
        out string cellToken)
    {
        sceneName = null;
        regionId = null;
        entryKey = null;
        cellToken = null;

        if (string.IsNullOrWhiteSpace(persistentId))
            return false;

        var parts = persistentId.Split(':');
        if (parts.Length != 5)
            return false;
        if (!string.Equals(parts[1], RuleRegionToken, System.StringComparison.Ordinal))
            return false;

        sceneName = parts[0];
        regionId = parts[2];
        entryKey = parts[3];
        cellToken = parts[4];
        return true;
    }

    private static string SanitizeKey(string value, string fallback)
    {
        string source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return source.Replace(':', '_').Replace(' ', '_');
    }
}
