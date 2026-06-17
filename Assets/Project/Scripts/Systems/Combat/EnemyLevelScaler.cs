using System.Text.RegularExpressions;
using UnityEngine;

public static class EnemyLevelScaler
{
    private const int DefaultLevel = 1;
    private const int MaxLevel = 99;
    private const float HpGrowthPerLevel = 0.18f;
    private const float AttackGrowthPerLevel = 0.12f;
    private const float DefenseGrowthPerLevel = 0.25f;

    private static readonly Regex LevelPattern = new(
        @"(?:mine[_-]?level|level|lvl|l)[_-]?(\d{1,3})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void ApplySpawnLevel(EntityRuntime entity, ObjectType prefabId, SceneSpawnPayload payload)
    {
        if (entity?.stats == null || !IsEnemyPrefab(prefabId))
            return;

        var stats = entity.stats;
        int level = ResolveLevel(payload);
        bool hasExistingLevel = stats.Has(StatType.Level);
        int existingLevel = Mathf.RoundToInt(stats.Get(StatType.Level));
        if (hasExistingLevel && existingLevel >= level)
            return;

        if (level <= DefaultLevel)
        {
            stats.Set(StatType.Level, DefaultLevel);
            return;
        }

        float hpMultiplier = 1f + (level - 1) * HpGrowthPerLevel;
        float attackMultiplier = 1f + (level - 1) * AttackGrowthPerLevel;

        float baseMaxHp = stats.Get(StatType.MaxHp);
        if (baseMaxHp > 0f)
        {
            float scaledMaxHp = Mathf.Ceil(baseMaxHp * hpMultiplier);
            stats.Set(StatType.MaxHp, scaledMaxHp);
            stats.Set(StatType.Hp, scaledMaxHp);
        }

        float baseAttack = stats.Get(StatType.Attack);
        if (baseAttack > 0f)
            stats.Set(StatType.Attack, Mathf.Ceil(baseAttack * attackMultiplier));

        float baseDefense = stats.Get(StatType.Defense);
        stats.Set(StatType.Defense, baseDefense + Mathf.Floor((level - 1) * DefenseGrowthPerLevel));
        stats.Set(StatType.Level, level);
    }

    private static int ResolveLevel(SceneSpawnPayload payload)
    {
        if (payload == null)
            return DefaultLevel;

        if (TryParseLevel(payload.spawnGroupId, out int level)
            || TryParseLevel(payload.persistentId, out level)
            || TryParseLevel(payload.sceneName, out level))
        {
            return Mathf.Clamp(level, DefaultLevel, MaxLevel);
        }

        return DefaultLevel;
    }

    private static bool TryParseLevel(string value, out int level)
    {
        level = DefaultLevel;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var match = LevelPattern.Match(value);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int parsed))
            return false;

        level = parsed;
        return true;
    }

    private static bool IsEnemyPrefab(ObjectType prefabId)
    {
        string name = prefabId.ToString();
        return name.StartsWith("Enemy", System.StringComparison.Ordinal);
    }
}
