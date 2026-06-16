using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestProgressionSystem : MonoBehaviour
{
    private readonly List<QuestGraphData> quests = new();
    private EventBus subscribedBus;

    private void Awake()
    {
        RefreshQuestCache();
    }

    private void OnEnable()
    {
        Subscribe();
        if (quests.Count == 0)
            RefreshQuestCache();
    }

    private void Start()
    {
        Subscribe();
        if (quests.Count == 0)
            RefreshQuestCache();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<EntityDiedPublish>(OnEntityDied);
            subscribedBus = null;
        }
    }

    public void RefreshQuestCache()
    {
        quests.Clear();
        quests.AddRange(Resources.LoadAll<QuestGraphData>("Data/Quests"));
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<EntityDiedPublish>(OnEntityDied);
        subscribedBus = bus;
    }

    private void OnEntityDied(EntityDiedPublish evt)
    {
        if (evt.entity?.entityData == null || evt.killer == null)
            return;

        var killerGo = evt.killer.Owner?.GameObject;
        if (killerGo == null || killerGo.GetComponent<PlayerControler>() == null)
            return;

        var log = evt.killer.GetModule<QuestLogRuntime>();
        if (log == null)
            return;

        string killedEntityId = evt.entity.entityData.id;
        foreach (var quest in quests)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.id))
                continue;

            if (log.GetState(quest.id) != QuestState.InProgress)
                continue;

            if (quest.objectives == null)
                continue;

            bool questChanged = false;
            foreach (var objective in quest.objectives)
            {
                if (!IsKillObjectiveFor(objective, killedEntityId))
                    continue;

                int required = Mathf.Max(1, objective.requiredAmount);
                int current = log.GetObjectiveProgress(quest.id, objective.id);
                if (current >= required)
                    continue;

                log.SetObjectiveProgress(quest.id, objective.id, Mathf.Min(required, current + 1));
                questChanged = true;
            }

            if (questChanged)
                subscribedBus?.Publish(new QuestStateChangedPublish(evt.killer.id, quest.id, QuestState.InProgress));
        }
    }

    private static bool IsKillObjectiveFor(QuestObjectiveData objective, string entityDataId)
    {
        if (objective == null || objective.objectiveType != QuestObjectiveType.KillEnemy)
            return false;

        string target = !string.IsNullOrWhiteSpace(objective.targetEntityDataId)
            ? objective.targetEntityDataId
            : objective.requiredEntityDataId;

        return string.Equals(target, entityDataId, StringComparison.OrdinalIgnoreCase);
    }
}
