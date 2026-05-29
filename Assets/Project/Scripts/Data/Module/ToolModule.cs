using UnityEngine;

/// <summary>
/// Module tool — gắn vào EntityData của tool (cuốc, liềm, rìu...).
/// Giữ toolType + các anim trigger. Mọi chỉ số (cooldown, damage, range...) lấy từ EntityData.baseStats.
/// </summary>
[System.Serializable]
public class ToolModule : IModuleData
{
    public ToolType toolType;

    [Tooltip("Tên trigger trong Animator. Để trống = dùng tên ToolType làm default (Hoe, Scythe...).")]
    public string animTrigger;

    [Tooltip("Trigger riêng cho hành động phụ của tool, hiện dùng cho WateringCan khi lấy nước. Để trống = dùng Anim Trigger.")]
    public string refillAnimTrigger;

    /// <summary>Trả về trigger name: ưu tiên animTrigger, fallback về toolType.ToString().</summary>
    public string GetAnimTrigger()
    {
        return string.IsNullOrEmpty(animTrigger) ? toolType.ToString() : animTrigger;
    }

    /// <summary>Trigger riêng cho refill của WateringCan. Để trống = dùng trigger chính.</summary>
    public string GetRefillAnimTrigger()
    {
        return string.IsNullOrEmpty(refillAnimTrigger) ? GetAnimTrigger() : refillAnimTrigger;
    }

    public override IModuleRuntime CreateRuntime()
    {
        switch (toolType)
        {
            case ToolType.Hoe:
                return new HoeRuntime(this);
            case ToolType.WateringCan:
                return new WateringCanRuntime(this);
            case ToolType.Scythe:
                return new ScytheRuntime(this);
            case ToolType.Axe:
                return new AxeRuntime(this);
            case ToolType.Pickaxe:
                return new PickaxeRuntime(this);
            case ToolType.Fertilizer:
                return new FertilizerRuntime(this);
            default:
                Debug.LogWarning($"[ToolModule] ToolType {toolType} chưa có Runtime.");
                return null;
        }
    }
}
