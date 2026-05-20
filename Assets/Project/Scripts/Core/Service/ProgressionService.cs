using UnityEngine;

public class ProgressionService
{
    public const int LevelCap = 50;

    private readonly EventBus eventBus;

    public ProgressionService(EventBus eventBus)
    {
        this.eventBus = eventBus;
    }

    public static int RequiredExp(int level)
    {
        int safeLevel = Mathf.Clamp(level, 1, LevelCap);
        int raw = 100 + 30 * safeLevel + 4 * safeLevel * safeLevel;
        return Mathf.Max(10, Mathf.RoundToInt(raw / 10f) * 10);
    }

    public void EnsureInitialized(EntityRuntime target)
    {
        if (target?.stats == null) return;

        int level = Mathf.FloorToInt(target.stats.Get(StatType.Level));
        if (level <= 0)
        {
            level = 1;
            target.stats.Set(StatType.Level, level);
        }

        int maxExp = Mathf.FloorToInt(target.stats.Get(StatType.MaxExp));
        if (maxExp <= 0)
            target.stats.Set(StatType.MaxExp, RequiredExp(level));

        if (!target.stats.Has(StatType.Exp) || target.stats.Get(StatType.Exp) < 0)
            target.stats.Set(StatType.Exp, 0);

        if (level >= LevelCap)
        {
            int capMaxExp = RequiredExp(LevelCap);
            target.stats.Set(StatType.Level, LevelCap);
            target.stats.Set(StatType.MaxExp, capMaxExp);
            target.stats.Set(StatType.Exp, capMaxExp);
        }
    }

    private static int ApplySourceMultiplier(int amount, ExpSourceType source)
    {
        return source switch
        {
            ExpSourceType.Harvest => amount * 3 / 2,          // 1.5x
            ExpSourceType.Quest   => amount * 5 / 4,          // 1.25x
            ExpSourceType.Craft   => Mathf.Max(1, amount / 2),// 0.5x, min 1
            _                     => amount,                   // 1.0x
        };
    }

    public bool GrantExp(EntityRuntime target, int amount, ExpSourceType source, EntityRuntime sourceEntity = null)
    {
        if (target?.stats == null || amount <= 0) return false;
        amount = ApplySourceMultiplier(amount, source);

        EnsureInitialized(target);

        int oldLevel = Mathf.FloorToInt(target.stats.Get(StatType.Level));
        int level = oldLevel;
        int exp = Mathf.FloorToInt(target.stats.Get(StatType.Exp)) + amount;
        int maxExp = Mathf.Max(1, Mathf.FloorToInt(target.stats.Get(StatType.MaxExp)));

        if (level >= LevelCap)
        {
            int capMaxExp = RequiredExp(LevelCap);
            target.stats.Set(StatType.Level, LevelCap);
            target.stats.Set(StatType.MaxExp, capMaxExp);
            target.stats.Set(StatType.Exp, capMaxExp);
            PublishChanged(target, source, sourceEntity, amount, oldLevel, LevelCap);
            return false;
        }

        while (level < LevelCap && exp >= maxExp)
        {
            exp -= maxExp;
            level++;
            ApplyLevelUpStats(target, level);
            eventBus?.Publish(new LevelUpPublish(target, level));

            if (level >= LevelCap)
            {
                maxExp = RequiredExp(LevelCap);
                exp = maxExp;
                break;
            }

            maxExp = RequiredExp(level);
        }

        target.stats.Set(StatType.Level, level);
        target.stats.Set(StatType.MaxExp, maxExp);
        target.stats.Set(StatType.Exp, exp);
        PublishChanged(target, source, sourceEntity, amount, oldLevel, level);

        Debug.Log($"[ProgressionService] Grant {amount} EXP ({source}) to '{target.entityData?.keyName}'. Lv {oldLevel}->{level}, EXP {exp}/{maxExp}.");
        return level > oldLevel;
    }

    private void ApplyLevelUpStats(EntityRuntime target, int newLevel)
    {
        if (target?.stats == null) return;

        float maxHp = target.stats.Get(StatType.MaxHp) + 5f;
        float maxMp = target.stats.Get(StatType.MaxMp) + 2f;

        target.stats.Set(StatType.MaxHp, maxHp);
        target.stats.Set(StatType.MaxMp, maxMp);
        target.stats.Set(StatType.Hp, maxHp);
        target.stats.Set(StatType.Mp, maxMp);

        if (newLevel % 2 == 0)
            target.stats.Set(StatType.Attack, target.stats.Get(StatType.Attack) + 1f);

        if (newLevel % 5 == 0)
            target.stats.Set(StatType.Defense, target.stats.Get(StatType.Defense) + 1f);

        // Publish để UI (HealthBarUI, PlayerInfoHUDUI) cập nhật ngay sau level up
        eventBus?.Publish(new StatsChangedPublish(target.id, StatType.Hp,    maxHp));
        eventBus?.Publish(new StatsChangedPublish(target.id, StatType.MaxHp, maxHp));
        eventBus?.Publish(new StatsChangedPublish(target.id, StatType.Mp,    maxMp));
        eventBus?.Publish(new StatsChangedPublish(target.id, StatType.MaxMp, maxMp));
    }

    private void PublishChanged(
        EntityRuntime target,
        ExpSourceType source,
        EntityRuntime sourceEntity,
        int amount,
        int oldLevel,
        int newLevel)
    {
        eventBus?.Publish(new ProgressionChangedPublish(
            target,
            source,
            sourceEntity,
            amount,
            oldLevel,
            newLevel,
            Mathf.FloorToInt(target.stats.Get(StatType.Exp)),
            Mathf.FloorToInt(target.stats.Get(StatType.MaxExp))));
    }
}
