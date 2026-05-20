using System;
using UnityEngine;

/// <summary>
/// API duy nhất kiểm tra unlock level/quest cho shop, recipe, quest visibility và zone gate.
/// Nếu có cả level và quest, chỉ cần đạt level hoặc hoàn thành toàn bộ quest yêu cầu.
/// </summary>
public static class UnlockService
{
    public const string LockedLevelKey = "ui.unlock.level_required";
    public const string LockedQuestKey = "ui.unlock.quest_required";

    public static bool IsUnlocked(EntityRuntime player, UnlockRequirementData requirement)
    {
        if (requirement == null) return true;

        bool hasLevelRequirement = Mathf.Max(1, requirement.requiredLevel) > 1;
        bool hasQuestRequirement = HasQuestRequirement(requirement);

        if (!hasLevelRequirement && !hasQuestRequirement)
            return true;

        if (hasLevelRequirement && GetLevel(player) >= Mathf.Max(1, requirement.requiredLevel))
            return true;

        if (hasQuestRequirement && HasCompletedAllRequiredQuests(player, requirement.requiredQuestIds))
            return true;

        return false;
    }

    public static bool IsUnlocked(EntityRuntime player, UnlockRequirementData requirement, int fallbackRequiredLevel)
    {
        return IsUnlocked(player, MergeLevelFallback(requirement, fallbackRequiredLevel));
    }

    public static string GetLockedReasonKey(EntityRuntime player, UnlockRequirementData requirement)
    {
        if (IsUnlocked(player, requirement)) return string.Empty;
        if (requirement != null && HasQuestRequirement(requirement))
            return LockedQuestKey;

        return LockedLevelKey;
    }

    public static UnlockRequirementData MergeLevelFallback(UnlockRequirementData requirement, int fallbackRequiredLevel)
    {
        int level = Mathf.Max(1, fallbackRequiredLevel);
        if (requirement == null)
            return new UnlockRequirementData { requiredLevel = level };

        if (requirement.requiredLevel <= 1 && level > 1)
            requirement.requiredLevel = level;

        return requirement;
    }

    private static int GetLevel(EntityRuntime player)
    {
        if (player?.stats == null) return 1;
        GameManager.Instance?.ProgressionService?.EnsureInitialized(player);
        return Mathf.Max(1, Mathf.FloorToInt(player.stats.Get(StatType.Level)));
    }

    private static bool HasQuestRequirement(UnlockRequirementData requirement)
    {
        if (requirement?.requiredQuestIds == null) return false;
        foreach (string questId in requirement.requiredQuestIds)
        {
            if (!string.IsNullOrWhiteSpace(questId))
                return true;
        }

        return false;
    }

    private static bool HasCompletedAllRequiredQuests(EntityRuntime player, string[] questIds)
    {
        if (player == null || questIds == null) return false;

        var questLog = player.GetModule<QuestLogRuntime>();
        if (questLog == null) return false;

        bool foundRequirement = false;
        foreach (string questId in questIds)
        {
            if (string.IsNullOrWhiteSpace(questId)) continue;
            foundRequirement = true;
            if (questLog.GetState(questId) != QuestState.Completed)
                return false;
        }

        return foundRequirement;
    }
}
