using UnityEngine;

[System.Serializable]
public class HarvestModule : IModuleData
{
    public ToolType harvestTool = ToolType.None;

    [Tooltip("Hệ số phạt khi dùng sai tool (0..1). Vd: 0.3 = chỉ gây 30% dame.")]
    [Range(0f, 1f)]
    public float wrongToolPenalty = 0.3f;

    public override IModuleRuntime CreateRuntime()
    {
        return new HarvestRuntime(this);
    }
}
