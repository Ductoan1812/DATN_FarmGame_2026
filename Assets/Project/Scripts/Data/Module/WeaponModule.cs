using UnityEngine;

/// <summary>
/// Module vũ khí chiến đấu riêng, tách khỏi farm tool.
/// Stat chính vẫn lấy từ EntityData.baseStats để balance data-driven.
/// </summary>
[System.Serializable]
public class WeaponModule : IModuleData
{
    public WeaponArchetype archetype = WeaponArchetype.Sword;

    [Tooltip("Tên trigger Animator. Để trống = Sword dùng Slash1H, Spear dùng Jab.")]
    public string animTrigger;

    [Tooltip("Fallback khi EntityData không có StatType.Range.")]
    public float baseRange = 1.35f;

    [Tooltip("Fallback khi EntityData không có StatType.Attack.")]
    public float baseDamage = 4f;

    [Tooltip("Fallback khi EntityData không có StatType.CoolDown.")]
    public float cooldown = 0.45f;

    [Tooltip("Thể lực tiêu hao mỗi lần đánh.")]
    public float staminaCost = 5f;

    [Tooltip("Lực đẩy mục tiêu khi trúng đòn.")]
    public float knockback = 1.5f;

    public string GetAnimTrigger()
    {
        if (!string.IsNullOrWhiteSpace(animTrigger))
            return animTrigger;

        return archetype == WeaponArchetype.Spear ? "Jab" : "Slash1H";
    }

    public override IModuleRuntime CreateRuntime()
    {
        return new WeaponRuntime(this);
    }
}
