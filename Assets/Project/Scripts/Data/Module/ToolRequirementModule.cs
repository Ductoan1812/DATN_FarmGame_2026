using UnityEngine;

[System.Serializable]
public class ToolRequirementModule : IModuleData
{
    public ToolType requiredToolType = ToolType.None;

    [Min(1)]
    public int minimumToolTier = 1;

    [Range(0f, 1f)]
    [Tooltip("Nhân sát thương khi dùng sai tool hoặc tool dưới tier yêu cầu nhưng vẫn cho phép gây dame.")]
    public float wrongToolPenalty = 0f;

    [Tooltip("Sai loại tool thì chặn hoàn toàn sát thương.")]
    public bool blockDamageIfWrongTool = true;

    [Tooltip("Đúng loại tool nhưng tier quá thấp thì chặn hoàn toàn sát thương.")]
    public bool blockDamageIfBelowTier = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new ToolRequirementRuntime(this);
    }
}
