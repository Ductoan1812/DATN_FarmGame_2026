using System.Collections.Generic;

public class ProcessorRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private readonly ProcessorModule _data;
    private EntityRuntime _owner;

    public ProcessorRuntime(ProcessorModule data)
    {
        _data = data;
    }

    public void Handle(SecondaryActionEvent e)
    {
        _owner ??= e.target;
        if (_data == null || e.context == null || e.initiator == null)
            return;

        var station = _owner ?? e.target;
        if (station == null)
            return;

        e.context.AddOption(
            $"processor.{station.entityData?.id ?? station.id}",
            string.IsNullOrWhiteSpace(_data.optionTextKey) ? "ui.common.open" : _data.optionTextKey,
            _data.priority,
            () => GameManager.Instance?.EventBus?.Publish(new ProcessorViewPublish(
                new ProcessorViewData(
                    e.initiator,
                    station,
                    _data.inputInventoryType,
                    _data.outputInventoryType,
                    _data.recipes ?? new List<ProcessorRecipeEntry>()))));
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is ProcessorRuntime;
}
