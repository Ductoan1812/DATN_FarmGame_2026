using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public enum HarvestDropMode
{
    World,
    DirectToInteractor
}

[System.Serializable]
public class HarvestToolOption
{
    public ToolType toolType = ToolType.None;

    [Tooltip("Tool này thu hoạch theo kiểu gây damage lên object. Tắt nếu chỉ harvest thường.")]
    public bool harvestCausesDamage;

    [Tooltip("Một lần harvest thành công bằng tool này sẽ hủy object ngay.")]
    public bool destroyOnHarvest = true;

    [Tooltip("Chỉ áp dụng khi Harvest Causes Damage bật: một hit bằng tool này là object chết ngay.")]
    public bool oneHitDestroy;
}

[System.Serializable]
public class HarvestModule : IModuleData
{
    [Tooltip("Tool harvest chính. Các cờ bên dưới áp dụng cho tool này và cho harvest bằng tay.")]
    public ToolType harvestTool = ToolType.None;

    [Tooltip("Cho phép bấm E để harvest. Harvest bằng tay cũng dùng bộ cờ mặc định bên dưới.")]
    public bool allowHandHarvest;

    [Tooltip("Tool phụ và luật riêng của từng tool. Nên ưu tiên cấu hình ở đây nếu mỗi tool có hành vi harvest khác nhau.")]
    public HarvestToolOption[] additionalHarvestTools = System.Array.Empty<HarvestToolOption>();

    public HarvestDropMode dropMode = HarvestDropMode.World;

    [Tooltip("Luật mặc định cho Harvest Tool chính và harvest bằng tay: lần harvest này cũng được tính là gây damage.")]
    public bool harvestCausesDamage;

    [Tooltip("Luật mặc định cho Harvest Tool chính và harvest bằng tay: harvest thành công sẽ hủy object ngay.")]
    public bool destroyOnHarvest = true;

    [Tooltip("Chỉ áp dụng khi Harvest Causes Damage bật trên Harvest Tool chính hoặc harvest bằng tay.")]
    public bool oneHitDestroy;

    [Tooltip("Hệ số phạt khi dùng sai tool (0..1). Vd: 0.3 = chỉ gây 30% dame.")]
    [Range(0f, 1f)]
    public float wrongToolPenalty = 0.3f;

    public bool AllowsHandHarvest => harvestTool == ToolType.None || allowHandHarvest;

    public bool HasAnyToolHarvest =>
        harvestTool != ToolType.None ||
        (additionalHarvestTools != null && additionalHarvestTools.Any(option => option != null && option.toolType != ToolType.None));

    public bool AllowsTool(ToolType toolType)
    {
        return TryGetToolBehavior(toolType, out _, out _, out _);
    }

    public bool TryGetToolBehavior(
        ToolType toolType,
        out bool causesDamage,
        out bool destroyOnHarvestForTool,
        out bool oneHitDestroyForTool)
    {
        causesDamage = false;
        destroyOnHarvestForTool = false;
        oneHitDestroyForTool = false;

        if (toolType == ToolType.None)
            return false;

        if (harvestTool == toolType)
        {
            causesDamage = harvestCausesDamage;
            destroyOnHarvestForTool = destroyOnHarvest;
            oneHitDestroyForTool = oneHitDestroy;
            return true;
        }

        if (additionalHarvestTools == null)
            return false;

        for (int i = 0; i < additionalHarvestTools.Length; i++)
        {
            HarvestToolOption option = additionalHarvestTools[i];
            if (option == null || option.toolType != toolType)
                continue;

            causesDamage = option.harvestCausesDamage;
            destroyOnHarvestForTool = option.destroyOnHarvest;
            oneHitDestroyForTool = option.oneHitDestroy;
            return true;
        }

        return false;
    }

    public ToolType GetPrimaryRequiredTool()
    {
        if (harvestTool != ToolType.None)
            return harvestTool;

        if (additionalHarvestTools != null)
        {
            for (int i = 0; i < additionalHarvestTools.Length; i++)
            {
                HarvestToolOption option = additionalHarvestTools[i];
                if (option != null && option.toolType != ToolType.None)
                    return option.toolType;
            }
        }

        return ToolType.None;
    }

    public IReadOnlyList<ToolType> GetAllowedTools()
    {
        var result = new List<ToolType>();
        if (harvestTool != ToolType.None)
            result.Add(harvestTool);

        if (additionalHarvestTools != null)
        {
            for (int i = 0; i < additionalHarvestTools.Length; i++)
            {
                HarvestToolOption option = additionalHarvestTools[i];
                ToolType tool = option?.toolType ?? ToolType.None;
                if (tool == ToolType.None || result.Contains(tool))
                    continue;

                result.Add(tool);
            }
        }

        return result;
    }

    public override IModuleRuntime CreateRuntime()
    {
        return new HarvestRuntime(this);
    }
}
