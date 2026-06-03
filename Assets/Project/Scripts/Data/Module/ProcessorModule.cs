using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProcessorRecipeEntry
{
    public RecipeData recipe;
    [Min(1)]
    public int processMinutes = 60;
}

[System.Serializable]
public class ProcessorModule : IModuleData
{
    public List<ProcessorRecipeEntry> recipes = new();
    public InventoryType inputInventoryType = InventoryType.Chest;
    public InventoryType outputInventoryType = InventoryType.Chest;
    public string optionTextKey = "ui.common.open";
    public int priority = 25;

    public override IModuleRuntime CreateRuntime()
    {
        return new ProcessorRuntime(this);
    }
}
