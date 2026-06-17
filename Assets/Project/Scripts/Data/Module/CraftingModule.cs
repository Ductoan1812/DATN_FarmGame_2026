using System.Collections.Generic;

[System.Serializable]
public class CraftingModule : IModuleData
{
    public string optionTextKey = "ui.crafting.open";
    public int priority = 35;
    public List<RecipeData> recipes = new();

    public override IModuleRuntime CreateRuntime()
    {
        return new CraftingRuntime(this);
    }
}
