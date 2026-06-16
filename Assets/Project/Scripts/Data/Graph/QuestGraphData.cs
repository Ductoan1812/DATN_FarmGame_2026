using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Quest Graph", fileName = "NewQuestGraph")]
public class QuestGraphData : ScriptableObject
{
    public string id;
    public string titleKey;
    public string descriptionKey;

    [Header("Interaction Keys")]
    public string offerOptionKey = "ui.quest.accept";
    public string inProgressOptionKey = "ui.quest.view";
    public string completeOptionKey = "ui.quest.complete";
    public string completedOptionKey = "ui.quest.completed";

    [Header("Unlock")]
    public UnlockRequirementData visibilityRequirement = new();

    [Header("Objectives")]
    public List<QuestObjectiveData> objectives = new();

    [Header("Rewards")]
    public int rewardMoney;
    public int rewardExp;
    public List<QuestRewardItemData> rewardItems = new();
}

[Serializable]
public class QuestObjectiveData
{
    public string id;
    public string descriptionKey;
    public QuestObjectiveType objectiveType = QuestObjectiveType.Inventory;
    public string targetEntityDataId;
    public string requiredEntityDataId;
    public int requiredAmount = 1;
}

[Serializable]
public class QuestRewardItemData
{
    public string entityDataId;
    public int amount = 1;
}

public enum QuestState
{
    NotStarted,
    InProgress,
    Completed
}

public enum QuestObjectiveType
{
    Inventory,
    KillEnemy,
    SurviveNight
}
