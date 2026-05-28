using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/AI/Daily Schedule", fileName = "DailySchedule")]
public class DailyScheduleData : ScriptableObject
{
    public string scheduleId;
    public List<ScheduleEntry> entries = new();
    public List<SeasonScheduleOverride> seasonOverrides = new();

    public bool TryGetEntry(Season season, int minuteOfDay, out ScheduleEntry entry)
    {
        minuteOfDay = Mathf.Clamp(minuteOfDay, 0, 1439);
        var list = ResolveEntries(season);
        return TryGetEntry(list, minuteOfDay, out entry);
    }

    private List<ScheduleEntry> ResolveEntries(Season season)
    {
        if (seasonOverrides != null)
        {
            for (int i = 0; i < seasonOverrides.Count; i++)
            {
                var overrideData = seasonOverrides[i];
                if (overrideData == null || overrideData.season != season)
                    continue;

                if (overrideData.entries != null && overrideData.entries.Count > 0)
                    return overrideData.entries;
            }
        }

        return entries;
    }

    private static bool TryGetEntry(List<ScheduleEntry> list, int minuteOfDay, out ScheduleEntry entry)
    {
        entry = null;
        if (list == null || list.Count == 0)
            return false;

        ScheduleEntry fallback = null;
        int fallbackMinute = int.MinValue;
        ScheduleEntry first = null;
        int firstMinute = int.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            var candidate = list[i];
            if (candidate == null)
                continue;

            int start = Mathf.Clamp(candidate.startMinuteOfDay, 0, 1439);
            if (start < firstMinute)
            {
                first = candidate;
                firstMinute = start;
            }

            if (start <= minuteOfDay && start >= fallbackMinute)
            {
                fallback = candidate;
                fallbackMinute = start;
            }
        }

        entry = fallback ?? first;
        return entry != null;
    }
}

[Serializable]
public class SeasonScheduleOverride
{
    public Season season;
    public List<ScheduleEntry> entries = new();
}

[Serializable]
public class ScheduleEntry
{
    [Range(0, 1439)] public int startMinuteOfDay;
    public string targetAnchorId;
    public ScheduleAction action = ScheduleAction.Stand;
    [Min(0f)] public float wanderRadius = 2f;
    [Min(0.25f)] public float waitSeconds = 2f;
    public string animationState;
}

public enum ScheduleAction
{
    Stand,
    WanderRadius,
    PatrolRoute
}
