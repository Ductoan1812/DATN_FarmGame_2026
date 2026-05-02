/// <summary>
/// Lắng nghe DieEvent → publish DestroyEntityRequestPublish.
/// SpawnSystem sẽ remove GameObject + EntityService.Destroy (unregister khỏi registry).
/// </summary>
public class MortalRuntime : IModuleRuntime, IHandleEvent<DieEvent>
{
    private readonly MortalModule _data;

    public MortalRuntime(MortalModule data)
    {
        _data = data;
    }

    public void Handle(DieEvent e)
    {
        if (e?.entity == null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Publish(new DestroyEntityRequestPublish(e.entity.id));
    }

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Mortal", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => other is MortalRuntime;
}
