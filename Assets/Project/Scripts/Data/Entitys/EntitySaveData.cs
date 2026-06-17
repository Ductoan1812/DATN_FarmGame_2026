using System;
using UnityEngine;

[Serializable]
public class StatEntrySave
{
    public StatType statType;
    public float baseValue;
    public float flatBonus;
    public float percentBonus;
}

[Serializable]
public class StatSaveData
{
    public StatEntrySave[] entries;
}

[Serializable]
public class ModuleSaveData
{
    public string moduleType;
    public string dataJson;
}

[Serializable]
public class EntitySaveData
{
    public string id;
    public string entityDataId;
    public int amount;
    public int quality = 1;
    public StatSaveData stats;
    public ModuleSaveData[] modules;
}
