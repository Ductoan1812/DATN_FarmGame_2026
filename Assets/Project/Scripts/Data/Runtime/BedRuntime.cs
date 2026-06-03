using UnityEngine;

/// <summary>
/// Bed interaction: player tương tác giường → kết thúc ngày.
/// Flow: SecondaryAction → confirm → SkipToNextDay → restore stamina/HP → reset watered tiles.
/// </summary>
public class BedRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private EntityRuntime _owner;

    public BedRuntime() { }

    public void Handle(SecondaryActionEvent e)
    {
        if (e.context == null) return;

        _owner ??= e.target;

        e.context.AddOption(
            "bed.sleep",
            LocalizationKeys.UiBedSleep,
            100, // high priority
            () => DoSleep(e.initiator)
        );
    }

    private void DoSleep(EntityRuntime player)
    {
        if (player == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // 1. Restore Stamina = MaxStamina
        float maxStamina = player.stats.Get(StatType.MaxStamina);
        if (maxStamina > 0f)
            player.stats.Set(StatType.Stamina, maxStamina);

        // 2. Restore HP = MaxHP
        float maxHp = player.stats.Get(StatType.MaxHp);
        if (maxHp > 0f)
            player.stats.Set(StatType.Hp, maxHp);

        // 3. Skip to next day (triggers DayChangedPublish → StageRuntime grow → WateredTileTracker reset)
        gm.TimeManager?.SkipToNextDay();

        Debug.Log($"[BedRuntime] Đã ngủ! Stamina={maxStamina}, HP={maxHp}. Sang ngày mới.");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────
    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is BedRuntime;
}
