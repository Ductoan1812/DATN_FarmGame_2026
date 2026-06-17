using UnityEngine;

/// <summary>
/// Runtime cho StaminaCostModule.
///
/// Cung cấp hai hàm chính dùng cho ToolRuntime base và WeaponRuntime:
///   - CanAfford(actor) : kiểm tra actor còn đủ stamina không
///   - Spend(actor)     : trừ stamina sau khi hành động thành công
///
/// Quy tắc an toàn:
///   - cost &lt;= 0 → luôn cho phép (miễn phí)
///   - MaxStamina &lt;= 0 → entity không dùng stamina → luôn cho phép
///   - Không bao giờ để stamina xuống dưới 0
/// </summary>
public class StaminaCostRuntime : IModuleRuntime
{
    public readonly StaminaCostModule data;

    public StaminaCostRuntime(StaminaCostModule data)
    {
        this.data = data;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Kiểm tra actor có đủ stamina để thực hiện hành động không.
    /// Trả về true nếu miễn phí hoặc đủ stamina.
    /// </summary>
    public bool CanAfford(EntityRuntime actor)
    {
        if (data.cost <= 0f || actor?.stats == null) return true;

        float maxStamina = actor.stats.Get(StatType.MaxStamina);
        if (maxStamina <= 0f) return true;

        return actor.stats.Get(StatType.Stamina) >= data.cost;
    }

    /// <summary>
    /// Trừ stamina của actor theo data.cost.
    /// Không bao giờ để stamina xuống dưới 0.
    /// Gọi sau khi hành động đã thực thi thành công (trong Execute hoặc sau Strike).
    /// </summary>
    public void Spend(EntityRuntime actor)
    {
        if (data.cost <= 0f || actor?.stats == null) return;

        float maxStamina = actor.stats.Get(StatType.MaxStamina);
        if (maxStamina <= 0f) return;

        float current = actor.stats.Get(StatType.Stamina);
        actor.stats.Set(StatType.Stamina, UnityEngine.Mathf.Max(0f, current - data.cost));
    }

    // ── IModuleRuntime boilerplate ────────────────────────────────────────────

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is StaminaCostRuntime;
}
