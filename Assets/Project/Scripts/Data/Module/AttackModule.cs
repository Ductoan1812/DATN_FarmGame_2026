using UnityEngine;

/// <summary>
/// Module cấu hình khả năng tấn công của entity.
/// Chỉ số sát thương chính lấy từ StatsRuntime của item/weapon.
/// </summary>
[System.Serializable]
public class AttackModule : IModuleData
{
    [Tooltip("Tầm tấn công mặc định nếu weapon không có StatType.Range.")]
    public float attackRange = 1.5f;

    [Tooltip("Cooldown mặc định nếu weapon không có StatType.CoolDown.")]
    public float attackCooldown = 0.5f;

    public override IModuleRuntime CreateRuntime()
    {
        return new AttackRuntime(this);
    }
}
