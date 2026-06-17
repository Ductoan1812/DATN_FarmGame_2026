using System;
using UnityEngine;

/// <summary>
/// API duy nhất kiểm tra unlock level/quest cho shop, recipe, quest visibility và zone gate.
/// Nếu có cả level và quest, chỉ cần đạt level hoặc hoàn thành toàn bộ quest yêu cầu.
/// Hỗ trợ mastery unlock table từ MasteryUnlockData.
/// </summary>
public static class UnlockService
{
    public const string LockedLevelKey = "ui.unlock.level_required";
    public const string LockedQuestKey = "ui.unlock.quest_required";

    private static MasteryUnlockData _masteryUnlockData;

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

    /// <summary>
    /// Check if an unlock ID is available based on player's mastery level.
    /// Returns true if no mastery requirement exists or if player meets the requirement.
    /// Safe fallback: returns true if MasteryUnlockData asset is missing.
    /// </summary>
    public static bool IsMasteryUnlocked(EntityRuntime player, string unlockId)
    {
        if (string.IsNullOrWhiteSpace(unlockId)) return true;
        if (player?.stats == null) return false;

        LoadMasteryUnlockDataIfNeeded();
        if (_masteryUnlockData == null || _masteryUnlockData.unlocks == null || _masteryUnlockData.unlocks.Length == 0)
            return true;

        int requiredLevel = GetMasteryRequirement(unlockId);
        if (requiredLevel <= 0) return true;

        int currentLevel = GetLevel(player);
        return currentLevel >= requiredLevel;
    }

    /// <summary>
    /// Get the mastery level required for an unlock ID.
    /// Returns 0 if no requirement exists.
    /// </summary>
    public static int GetMasteryRequirement(string unlockId)
    {
        if (string.IsNullOrWhiteSpace(unlockId)) return 0;

        LoadMasteryUnlockDataIfNeeded();
        if (_masteryUnlockData == null || _masteryUnlockData.unlocks == null)
            return 0;

        foreach (var entry in _masteryUnlockData.unlocks)
        {
            if (entry != null && entry.unlockId == unlockId)
                return Mathf.Max(0, entry.masteryLevel);
        }

        return 0;
    }

    /// <summary>
    /// Get all mastery unlocks newly achieved between oldLevel and newLevel (inclusive).
    /// Returns empty array if no unlocks exist in that range.
    /// </summary>
    public static MasteryUnlockData.UnlockEntry[] GetNewlyUnlockedMasteries(int oldLevel, int newLevel)
    {
        if (newLevel <= oldLevel) return System.Array.Empty<MasteryUnlockData.UnlockEntry>();

        LoadMasteryUnlockDataIfNeeded();
        if (_masteryUnlockData == null || _masteryUnlockData.unlocks == null || _masteryUnlockData.unlocks.Length == 0)
            return System.Array.Empty<MasteryUnlockData.UnlockEntry>();

        var result = new System.Collections.Generic.List<MasteryUnlockData.UnlockEntry>();
        foreach (var entry in _masteryUnlockData.unlocks)
        {
            if (entry != null && entry.masteryLevel > oldLevel && entry.masteryLevel <= newLevel)
                result.Add(entry);
        }

        return result.ToArray();
    }

    private static void LoadMasteryUnlockDataIfNeeded()
    {
        if (_masteryUnlockData != null) return;

        _masteryUnlockData = Resources.Load<MasteryUnlockData>("Data/MasteryUnlockData");
        if (_masteryUnlockData == null)
            Debug.LogWarning("[UnlockService] MasteryUnlockData not found at Resources/Data/MasteryUnlockData. Mastery unlock checks will always return true.");
    }
}
