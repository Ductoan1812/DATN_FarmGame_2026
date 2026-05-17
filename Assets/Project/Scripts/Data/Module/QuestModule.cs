using System.Collections.Generic;

[System.Serializable]
public class QuestModule : IModuleData
{
    public List<QuestGraphData> quests = new();
    public int priority = 20;

    public override IModuleRuntime CreateRuntime()
    {
        return new QuestRuntime(this);
    }
}
