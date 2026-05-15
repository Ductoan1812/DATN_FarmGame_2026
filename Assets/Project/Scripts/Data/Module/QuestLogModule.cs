[System.Serializable]
public class QuestLogModule : IModuleData
{
    public override IModuleRuntime CreateRuntime()
    {
        return new QuestLogRuntime();
    }
}
