using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatDefinitionDatabase", menuName = "UI/Definitions/Stat Definition Database")]
public class StatDefinitionDatabase : ScriptableObject
{
    [SerializeField] private List<StatDefinition> definitions = new();

    public IReadOnlyList<StatDefinition> Definitions => definitions;

    public bool TryGet(StatType statType, out StatDefinition definition)
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            var current = definitions[i];
            if (current == null || current.StatType != statType) continue;

            definition = current;
            return true;
        }

        definition = null;
        return false;
    }

    public StatDefinition Get(StatType statType)
    {
        TryGet(statType, out var definition);
        return definition;
    }
}

[Serializable]
public class StatDefinition
{
    [SerializeField] private StatType statType;
    [SerializeField] private Sprite icon;
    [SerializeField] private string nameKey;
    [SerializeField] private string descriptionKey;
    [SerializeField] private string valueFormat = "+{0}";

    public StatType StatType => statType;
    public Sprite Icon => icon;
    public string NameKey => nameKey;
    public string DescriptionKey => descriptionKey;
    public string ValueFormat => valueFormat;
}
