using UnityEngine;

/// <summary>
/// Bed interaction: player tương tác giường → kết thúc ngày.
/// Flow: SecondaryAction → confirm → SkipToNextDay → restore stamina/HP → reset watered tiles.
/// </summary>
public class BedRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    public BedRuntime() { }

    public void Handle(SecondaryActionEvent e)
    {
        if (e.context != null)
            e.context.IsHandledDirectly = true;

        SleepUtility.TrySleep(e.initiator);
    }

    // ── Save / Load ───────────────────────────────────────────────────────────
    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is BedRuntime;
}
