using System;
using System.Collections.Generic;
using UnityEngine;

public static class QuestService
{
    public static InteractionOptionRuntime CreateInteractionOption(
        EntityRuntime player,
        EntityRuntime questOwner,
        QuestGraphData graph,
        int priority)
    {
        if (player == null || graph == null || string.IsNullOrWhiteSpace(graph.id))
            return null;

        if (!UnlockService.IsUnlocked(player, graph.visibilityRequirement))
            return null;

        var log = player.GetModule<QuestLogRuntime>();
        var state = log?.GetState(graph.id) ?? QuestState.NotStarted;

        if (state == QuestState.Completed)
        {
            return new InteractionOptionRuntime(
                $"quest.{graph.id}.completed",
                graph.completedOptionKey,
                priority + 3,
                () => ShowQuest(player, questOwner, graph));
        }

        if (state == QuestState.NotStarted)
        {
            return new InteractionOptionRuntime(
                $"quest.{graph.id}.offer",
                graph.offerOptionKey,
                priority,
                () => AcceptQuest(player, questOwner, graph));
        }

        if (CanComplete(player, graph))
        {
            return new InteractionOptionRuntime(
                $"quest.{graph.id}.complete",
                graph.completeOptionKey,
                priority,
                () => CompleteQuest(player, questOwner, graph));
        }

        return new InteractionOptionRuntime(
            $"quest.{graph.id}.view",
            graph.inProgressOptionKey,
            priority + 1,
            () => ShowQuest(player, questOwner, graph));
    }

    public static bool AcceptQuest(EntityRuntime player, EntityRuntime questOwner, QuestGraphData graph)
    {
        var log = player?.GetModule<QuestLogRuntime>();
        if (log == null || graph == null) return false;
        if (!log.SetState(graph.id, QuestState.InProgress)) return false;

        PublishState(player, graph.id, QuestState.InProgress);
        ShowQuest(player, questOwner, graph);
        return true;
    }

    public static bool CompleteQuest(EntityRuntime player, EntityRuntime questOwner, QuestGraphData graph)
    {
        var log = player?.GetModule<QuestLogRuntime>();
        if (log == null || graph == null || string.IsNullOrWhiteSpace(graph.id)) return false;
        if (!CanComplete(player, graph)) return false;

        var inventoryService = GameManager.Instance?.InventoryService;
        bool hasRequiredItems = HasRequiredItemObjectives(graph);
        if (hasRequiredItems && inventoryService == null) return false;

        if (hasRequiredItems)
        {
            foreach (var objective in graph.objectives)
            {
                if (objective == null || objective.requiredAmount <= 0) continue;
                if (string.IsNullOrWhiteSpace(objective.requiredEntityDataId)) continue;
                if (!inventoryService.Remove(objective.requiredEntityDataId, objective.requiredAmount, player))
                    return false;
            }
        }

        if (!log.SetState(graph.id, QuestState.Completed)) return false;
        ApplyRewards(player, graph);

        PublishState(player, graph.id, QuestState.Completed);
        ShowQuest(player, questOwner, graph);
        return true;
    }

    public static bool CanComplete(EntityRuntime player, QuestGraphData graph)
    {
        if (player == null || graph == null) return false;
        if (graph.objectives == null || graph.objectives.Count == 0) return true;

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null) return false;

        foreach (var objective in graph.objectives)
        {
            if (objective == null || objective.requiredAmount <= 0) continue;
            if (string.IsNullOrWhiteSpace(objective.requiredEntityDataId)) continue;
            if (inventoryService.CountEntity(player, objective.requiredEntityDataId) < objective.requiredAmount)
                return false;
        }

        return true;
    }

    private static bool HasRequiredItemObjectives(QuestGraphData graph)
    {
        if (graph?.objectives == null) return false;

        foreach (var objective in graph.objectives)
        {
            if (objective == null || objective.requiredAmount <= 0) continue;
            if (string.IsNullOrWhiteSpace(objective.requiredEntityDataId)) continue;
            return true;
        }

        return false;
    }

    public static void ShowQuest(EntityRuntime player, EntityRuntime questOwner, QuestGraphData graph)
    {
        if (player == null || graph == null) return;

        var log = player.GetModule<QuestLogRuntime>();
        var state = log?.GetState(graph.id) ?? QuestState.NotStarted;
        var objectives = BuildObjectiveViews(player, graph);
        var viewData = new QuestViewData(player, questOwner, graph.id, graph.titleKey, graph.descriptionKey, state, objectives);

        GameManager.Instance?.EventBus?.Publish(new QuestViewPublish(viewData));
    }

    private static IReadOnlyList<QuestObjectiveViewData> BuildObjectiveViews(EntityRuntime player, QuestGraphData graph)
    {
        var result = new List<QuestObjectiveViewData>();
        var inventoryService = GameManager.Instance?.InventoryService;

        if (graph.objectives == null) return result;

        foreach (var objective in graph.objectives)
        {
            if (objective == null) continue;
            int currentAmount = 0;
            if (inventoryService != null && !string.IsNullOrWhiteSpace(objective.requiredEntityDataId))
                currentAmount = inventoryService.CountEntity(player, objective.requiredEntityDataId);

            result.Add(new QuestObjectiveViewData(
                objective.id,
                objective.descriptionKey,
                currentAmount,
                Mathf.Max(0, objective.requiredAmount)));
        }

        return result;
    }

    private static void PublishState(EntityRuntime player, string questId, QuestState state)
    {
        GameManager.Instance?.EventBus?.Publish(new QuestStateChangedPublish(player.id, questId, state));
    }

    private static void ApplyRewards(EntityRuntime player, QuestGraphData graph)
    {
        if (player == null || graph == null) return;

        if (graph.rewardMoney > 0 && player.stats != null)
        {
            float currentMoney = player.stats.Get(StatType.Money);
            player.stats.Set(StatType.Money, currentMoney + graph.rewardMoney);
        }

        if (graph.rewardExp > 0)
            GameManager.Instance?.ProgressionService?.GrantExp(player, graph.rewardExp, ExpSourceType.Quest);

        if (graph.rewardItems == null || graph.rewardItems.Count == 0)
            return;

        var gameManager = GameManager.Instance;
        var entityService = gameManager?.EntityService;
        var inventoryService = gameManager?.InventoryService;
        var entityDataRegistry = gameManager?.EntityDataRegistry;
        if (entityService == null || inventoryService == null || entityDataRegistry == null)
            return;

        foreach (var reward in graph.rewardItems)
        {
            if (reward == null || reward.amount <= 0) continue;
            if (string.IsNullOrWhiteSpace(reward.entityDataId)) continue;

            var itemData = entityDataRegistry.Find(reward.entityDataId);
            if (itemData == null)
            {
                Debug.LogWarning($"[QuestService] Reward item data '{reward.entityDataId}' not found.");
                continue;
            }

            int remaining = reward.amount;
            while (remaining > 0)
            {
                int stackAmount = Mathf.Min(remaining, Mathf.Max(1, itemData.maxStack));
                var item = entityService.Create(itemData, stackAmount);
                int received = inventoryService.Pickup(item, player);
                remaining -= received;

                if (received < stackAmount && item != null && !item.IsEmpty)
                    entityService.Destroy(item);

                if (received <= 0)
                {
                    Debug.LogWarning($"[QuestService] Cannot grant full reward '{itemData.id}'. Inventory may be full.");
                    break;
                }
            }
        }
    }
}

public sealed class QuestViewData
{
    public EntityRuntime Player { get; }
    public EntityRuntime QuestOwner { get; }
    public string QuestId { get; }
    public string TitleKey { get; }
    public string DescriptionKey { get; }
    public QuestState State { get; }
    public IReadOnlyList<QuestObjectiveViewData> Objectives { get; }

    public QuestViewData(
        EntityRuntime player,
        EntityRuntime questOwner,
        string questId,
        string titleKey,
        string descriptionKey,
        QuestState state,
        IReadOnlyList<QuestObjectiveViewData> objectives)
    {
        Player = player;
        QuestOwner = questOwner;
        QuestId = questId;
        TitleKey = titleKey;
        DescriptionKey = descriptionKey;
        State = state;
        Objectives = objectives;
    }
}

public sealed class QuestObjectiveViewData
{
    public string Id { get; }
    public string DescriptionKey { get; }
    public int CurrentAmount { get; }
    public int RequiredAmount { get; }

    public QuestObjectiveViewData(string id, string descriptionKey, int currentAmount, int requiredAmount)
    {
        Id = id;
        DescriptionKey = descriptionKey;
        CurrentAmount = currentAmount;
        RequiredAmount = requiredAmount;
    }
}
