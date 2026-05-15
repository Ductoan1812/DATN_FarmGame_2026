public class QuestRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private readonly QuestModule data;
    private EntityRuntime owner;

    public QuestRuntime(QuestModule data)
    {
        this.data = data;
    }

    public void Handle(SecondaryActionEvent e)
    {
        owner ??= e.target;
        if (e.context == null || data.quests == null) return;

        var questOwner = owner ?? e.target;
        foreach (var quest in data.quests)
        {
            var option = QuestService.CreateInteractionOption(e.initiator, questOwner, quest, data.priority);
            if (option == null) continue;

            e.context.AddOption(option.Id, option.TextKey, option.Priority, option.Execute);
        }
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is QuestRuntime;
}
