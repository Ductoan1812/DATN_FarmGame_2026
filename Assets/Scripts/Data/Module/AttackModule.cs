using UnityEngine;

/// <summary>
/// Module cấu hình khả năng tấn công của entity (Player, Enemy...).
/// Các chỉ số Attack / CritChance / CritDamage lấy từ StatsRuntime.
/// </summary>
[System.Serializable]
public class AttackModule : IModuleData
{
    [Tooltip("Tầm tấn công (đơn vị Unity). CombatScanSystem dùng giá trị này để quét target.")]
    public float attackRange = 1.5f;

    [Tooltip("Cooldown giữa 2 lần tấn công (giây).")]
    public float attackCooldown = 0.5f;

    public override IModuleRuntime CreateRuntime()
    {
        return new AttackRuntime(this);
    }
}
