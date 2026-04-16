using UnityEngine;

/// <summary>
/// Module cuốc đất — gắn vào EntityData của cái cuốc.
/// </summary>
[System.Serializable]
public class ToolModule : IModuleData
{
    [Tooltip("Thời gian hồi giữa 2 lần cuốc (giây)")]
    public float cooldown = 0.3f;
    public ToolType toolType;


    public override IModuleRuntime CreateRuntime()
    {   
        switch (toolType)        {
            case ToolType.Hoe:
                return new HoeRuntime(this);
            default:
                Debug.LogWarning($"[ToolModule] ToolType {toolType} chưa có Runtime, ");
                return null;
        }
    }
}
