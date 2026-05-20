using UnityEngine;

/// <summary>
/// Module tool — gắn vào EntityData của tool (cuốc, liềm, rìu...).
/// Chỉ giữ toolType + animTrigger. Mọi chỉ số (cooldown, damage, range...) lấy từ EntityData.baseStats.
/// </summary>
[System.Serializable]
public class ToolModule : IModuleData
{
    public ToolType toolType;

    [Tooltip("Tên trigger trong Animator. Để trống = dùng tên ToolType làm default (Hoe, Scythe...).")]
    public string animTrigger;

    /// <summary>Trả về trigger name: ưu tiên animTrigger, fallback về toolType.ToString().</summary>
    public string GetAnimTrigger()
    {
        return string.IsNullOrEmpty(animTrigger) ? toolType.ToString() : animTrigger;
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
            default:
                Debug.LogWarning($"[ToolModule] ToolType {toolType} chưa có Runtime.");
                return null;
        }
    }
}
