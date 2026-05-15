using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestLogRuntime : IModuleRuntime
{
    private readonly Dictionary<string, QuestState> states = new();

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

        return new ModuleSaveData
        {
            moduleType = "QuestLog",
            dataJson = JsonUtility.ToJson(new QuestLogSaveData { entries = entries.ToArray() })
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;

        var data = JsonUtility.FromJson<QuestLogSaveData>(save.dataJson);
        states.Clear();

        if (data?.entries == null) return;
        foreach (var entry in data.entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.questId)) continue;
            states[entry.questId] = entry.state;
        }
    }

    public bool MatchesSave(ModuleSaveData save)
    {
        return save?.moduleType == "QuestLog";
    }

    public bool Equals(IModuleRuntime other) => other is QuestLogRuntime;

    [Serializable]
    private class QuestLogSaveData
    {
        public QuestLogEntrySave[] entries;
    }

    [Serializable]
    private class QuestLogEntrySave
    {
        public string questId;
        public QuestState state;
    }
}
