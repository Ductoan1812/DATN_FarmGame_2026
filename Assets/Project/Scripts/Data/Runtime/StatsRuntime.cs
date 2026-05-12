using UnityEngine;
using System.Collections.Generic;
public class StatsRuntime
{
    private Dictionary<StatType, Stat> stats = new();

    public StatsRuntime Clone()
    {
        var clone = new StatsRuntime();
        clone.stats = new Dictionary<StatType, Stat>();
        foreach (var kv in stats)
        {
            clone.stats[kv.Key] = new Stat
            {
                baseValue = kv.Value.baseValue,
                flatBonus = kv.Value.flatBonus,
                percentBonus = kv.Value.percentBonus
            };
        }
        return clone;
    }

    public StatSaveData ToSaveData()
    {
        var save = new StatSaveData();
        if (stats == null || stats.Count == 0)
        {
            save.entries = new StatEntrySave[0];
            return save;
        }

        save.entries = new StatEntrySave[stats.Count];
        int i = 0;
        foreach (var kv in stats)
        {
            save.entries[i++] = new StatEntrySave
            {
                statType = kv.Key,
                baseValue = kv.Value.baseValue,
                flatBonus = kv.Value.flatBonus,
                percentBonus = kv.Value.percentBonus
            };
        }
        return save;
    }

    public static StatsRuntime FromSaveData(StatSaveData save)
    {
        var runtime = new StatsRuntime();
        runtime.stats = new Dictionary<StatType, Stat>();
        if (save == null || save.entries == null) return runtime;
        foreach (var e in save.entries)
        {
            runtime.stats[e.statType] = new Stat
            {
                baseValue = e.baseValue,
                flatBonus = e.flatBonus,
                percentBonus = e.percentBonus
            };
        }
        return runtime;
    }

    public bool Equals(StatsRuntime other)
    {
        if (other == null) return false;
        if (stats.Count != other.stats.Count) return false;
        foreach (var kv in stats)
        {
            if (!other.stats.TryGetValue(kv.Key, out var otherStat)) return false;
            if (kv.Value.baseValue != otherStat.baseValue) return false;
            if (kv.Value.flatBonus != otherStat.flatBonus) return false;
            if (kv.Value.percentBonus != otherStat.percentBonus) return false;
        }
        return true;
    }

    // Khởi tạo từ Data
    public void Init(StatsData data)
    {
        foreach (var entry in data.baseStats)
        {
            stats[entry.statType] = new Stat
            {
                baseValue = entry.value
            };
        }
    }

    // Lấy giá trị cuối
    public float Get(StatType type)
    {
        if (!stats.ContainsKey(type)) return 0;
        return stats[type].GetValue();
    }

    /// <summary>Fired khi bất kỳ stat nào thay đổi. (StatType, newValue)</summary>
    public event System.Action<StatType, float> OnChanged;

    // Set baseValue trực tiếp
    public void Set(StatType type, float value)
    {
        EnsureStat(type).baseValue = value;
        OnChanged?.Invoke(type, Get(type));
    }

    // Cộng thẳng
    public void AddFlat(StatType type, float value)
    {
        EnsureStat(type).flatBonus += value;
        OnChanged?.Invoke(type, Get(type));
    }

    // Cộng %
    public void AddPercent(StatType type, float value)
    {
        EnsureStat(type).percentBonus += value;
        OnChanged?.Invoke(type, Get(type));
    }

    private Stat EnsureStat(StatType type)
    {
        if (!stats.TryGetValue(type, out var stat))
            stats[type] = stat = new Stat();
        return stat;
    }
}