public class StorageRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private readonly StorageModule _data;
    private EntityRuntime _owner;

    public StorageRuntime(StorageModule data)
    {
        _data = data;
    }

    public void Handle(SecondaryActionEvent e)
    {
        _owner ??= e.target;
        if (_data == null || e.context == null || e.initiator == null)
            return;

        var storageOwner = _owner ?? e.target;
        if (storageOwner == null)
            return;

        e.context.AddOption(
            $"storage.{storageOwner.entityData?.id ?? storageOwner.id}",
            string.IsNullOrWhiteSpace(_data.optionTextKey) ? LocalizationKeys.UiMenuStorage : _data.optionTextKey,
            _data.priority,
            () => GameManager.Instance?.EventBus?.Publish(new StorageViewPublish(
                new StorageViewData(e.initiator, storageOwner, _data.inventoryType))));
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is StorageRuntime;
}
