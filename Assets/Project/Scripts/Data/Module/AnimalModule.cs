using UnityEngine;

[System.Serializable]
public class AnimalModule : IModuleData
{
    public string speciesKey = "m3.animal.chicken.name";
    public string feedOptionTextKey = "ui.animal.feed";
    public string collectOptionTextKey = "ui.animal.collect";
    public string statusHungryKey = "ui.animal.status.hungry";
    public string statusFedKey = "ui.animal.status.fed";
    public string statusProductReadyKey = "ui.animal.status.product_ready";
    public string statusDeadKey = "ui.animal.status.dead";
    public int priority = 25;
    public EntityData feedItem;
    public EntityData productItem;
    [Min(1)] public int productAmount = 1;
    [Min(1)] public int daysWithoutFoodToDie = 3;

    public override IModuleRuntime CreateRuntime()
    {
        return new AnimalRuntime(this);
    }
}
