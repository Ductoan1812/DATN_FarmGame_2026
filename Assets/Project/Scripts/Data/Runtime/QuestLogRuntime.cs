using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestLogRuntime : IModuleRuntime
{
    private readonly Dictionary<string, QuestState> states = new();
    private readonly Dictionary<string, int> objectiveProgress = new();

    public QuestState GetState(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return QuestState.NotStarted;
        return states.TryGetValue(questId, out var state) ? state : QuestState.NotStarted;
    }

    public bool SetState(string questId, QuestState state)
    {
        if (string.IsNullOrWhiteSpace(questId)) return false;
        states[questId] = state;
        return true;
    }

    public int GetObjectiveProgress(string questId, string objectiveId)
    {
        string key = BuildObjectiveKey(questId, objectiveId);
        if (string.IsNullOrEmpty(key)) return 0;
        return objectiveProgress.TryGetValue(key, out int value) ? Mathf.Max(0, value) : 0;
    }

    public bool SetObjectiveProgress(string questId, string objectiveId, int value)
    {
        string key = BuildObjectiveKey(questId, objectiveId);
        if (string.IsNullOrEmpty(key)) return false;
        objectiveProgress[key] = Mathf.Max(0, value);
        return true;
    }

    public int AddObjectiveProgress(string questId, string objectiveId, int amount)
    {
        int updated = GetObjectiveProgress(questId, objectiveId) + Mathf.Max(0, amount);
        SetObjectiveProgress(questId, objectiveId, updated);
        return updated;
    }

    public ModuleSaveData ToSaveData()
    {
        var entries = new List<QuestLogEntrySave>();
        foreach (var pair in states)
        {
            entries.Add(new QuestLogEntrySave
            {
                questId = pair.Key,
                state = pair.Value
            });
        }

        var progressEntries = new List<QuestObjectiveProgressSave>();
        foreach (var pair in objectiveProgress)
        {
            progressEntries.Add(new QuestObjectiveProgressSave
            {
                key = pair.Key,
                value = Mathf.Max(0, pair.Value)
            });
        }

        return new ModuleSaveData
        {
            moduleType = "QuestLog",
            dataJson = JsonUtility.ToJson(new QuestLogSaveData
            {
                entries = entries.ToArray(),
                objectiveEntries = progressEntries.ToArray()
            })
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;

        var data = JsonUtility.FromJson<QuestLogSaveData>(save.dataJson);
        states.Clear();
        objectiveProgress.Clear();

        if (data?.entries != null)
        {
            foreach (var entry in data.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.questId)) continue;
                states[entry.questId] = entry.state;
            }
        }

        if (data?.objectiveEntries != null)
        {
            foreach (var entry in data.objectiveEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                objectiveProgress[entry.key] = Mathf.Max(0, entry.value);
            }
        }
    }

    public bool MatchesSave(ModuleSaveData save)
    {
        return save?.moduleType == "QuestLog";
    }

    public bool Equals(IModuleRuntime other) => other is QuestLogRuntime;

    private static string BuildObjectiveKey(string questId, string objectiveId)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return string.Empty;

        return $"{questId.Trim()}::{objectiveId.Trim()}";
    }

    [Serializable]
    private class QuestLogSaveData
    {
        public QuestLogEntrySave[] entries;
        public QuestObjectiveProgressSave[] objectiveEntries;
    }

    [Serializable]
    private class QuestLogEntrySave
    {
        public string questId;
        public QuestState state;
    }

    [Serializable]
    private class QuestObjectiveProgressSave
    {
        public string key;
        public int value;
    }
}
