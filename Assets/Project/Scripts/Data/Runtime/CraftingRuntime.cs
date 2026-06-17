public class CraftingRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private readonly CraftingModule data;
    private EntityRuntime owner;

    public CraftingRuntime(CraftingModule data)
    {
        this.data = data;
    }

    public void Handle(SecondaryActionEvent e)
    {
        owner ??= e.target;
        if (e.context == null || e.initiator == null) return;

        var station = owner ?? e.target;
        if (station == null) return;

        e.context.AddOption(
            $"crafting.{station.entityData?.id ?? station.id}",
            data.optionTextKey,
            data.priority,
            () => GameManager.Instance?.CraftingService?.Open(e.initiator, station, data.recipes));
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is CraftingRuntime;
}
