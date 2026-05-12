using UnityEngine;

/// <summary>
/// Module cấu hình khả năng nhận sát thương của entity (Player, Enemy...).
/// Hp / MaxHp thực tế nằm trong StatsRuntime (StatType.Hp / MaxHp).
/// </summary>
[System.Serializable]
public class HealthModule : IModuleData
{
    [Tooltip("Cho phép entity nhận sát thương. Tắt để tạm thời miễn sát thương (invincible).")]
    public bool canTakeDamage = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new HealthRuntime(this);
    }
}
