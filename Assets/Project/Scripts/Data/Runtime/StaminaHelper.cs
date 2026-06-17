using UnityEngine;

/// <summary>
/// Static utility cho các hành động tiêu stamina KHÔNG gắn với item module.
/// Ví dụ: dodge của player, ability, v.v.
///
/// Với tool và weapon: dùng StaminaCostRuntime (query qua item.GetModule) thay thế.
/// </summary>
public static class StaminaHelper
{
    /// <summary>
    /// Kiểm tra và trừ stamina cùng lúc.
    /// Trả về true + trừ ngay nếu đủ.
    /// Trả về false + không làm gì nếu không đủ.
    /// Nếu cost &lt;= 0 hoặc entity không có MaxStamina → luôn trả true (miễn phí).
    /// </summary>
    public static bool TrySpend(EntityRuntime actor, float cost)
    {
        if (cost <= 0f || actor?.stats == null) return true;

        float maxStamina = actor.stats.Get(StatType.MaxStamina);
        if (maxStamina <= 0f) return true;

        float stamina = actor.stats.Get(StatType.Stamina);
        if (stamina < cost) return false;

        actor.stats.Set(StatType.Stamina, Mathf.Max(0f, stamina - cost));
        return true;
    }

    /// <summary>
    /// Chỉ kiểm tra, không trừ.
    /// </summary>
    public static bool HasStamina(EntityRuntime actor, float cost)
    {
        if (cost <= 0f || actor?.stats == null) return true;

        float maxStamina = actor.stats.Get(StatType.MaxStamina);
        if (maxStamina <= 0f) return true;

        return actor.stats.Get(StatType.Stamina) >= cost;
    }
}
