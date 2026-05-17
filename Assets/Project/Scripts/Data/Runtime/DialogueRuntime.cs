public class DialogueRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private readonly DialogueModule data;
    private EntityRuntime owner;

    public DialogueRuntime(DialogueModule data)
    {
        this.data = data;
    }

    public void Handle(SecondaryActionEvent e)
    {
        owner ??= e.target;
        if (e.context == null || data.graph == null) return;

        var speaker = owner ?? e.target;
        e.context.AddOption(
            $"dialogue.{data.graph.id}",
            data.optionTextKey,
            data.priority,
            () => DialogueService.Start(speaker, e.initiator, data.graph));
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is DialogueRuntime;
}
