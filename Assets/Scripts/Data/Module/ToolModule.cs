using UnityEngine;

/// <summary>
/// Module tool — gắn vào EntityData của tool (cuốc, liềm, rìu...).
/// Chỉ giữ toolType. Mọi chỉ số (cooldown, damage, range...) lấy từ EntityData.baseStats.
/// </summary>
[System.Serializable]
public class ToolModule : IModuleData
{
    public ToolType toolType;

    public override IModuleRuntime CreateRuntime()
    {
        switch (toolType)
        {
            case ToolType.Hoe:
                return new HoeRuntime(this);
            case ToolType.Scythe:
                return new ScytheRuntime(this);
            default:
                Debug.LogWarning($"[ToolModule] ToolType {toolType} chưa có Runtime.");
                return null;
        }
    }
}
