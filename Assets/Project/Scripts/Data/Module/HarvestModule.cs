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
public class HarvestModule : IModuleData
{
    public ToolType harvestTool = ToolType.None;
    public bool allowHandHarvest;
    public ToolType[] additionalHarvestTools = System.Array.Empty<ToolType>();
    public HarvestDropMode dropMode = HarvestDropMode.World;

    [Tooltip("Hệ số phạt khi dùng sai tool (0..1). Vd: 0.3 = chỉ gây 30% dame.")]
    [Range(0f, 1f)]
    public float wrongToolPenalty = 0.3f;

    public bool AllowsHandHarvest => harvestTool == ToolType.None || allowHandHarvest;

    public bool HasAnyToolHarvest =>
        harvestTool != ToolType.None ||
        (additionalHarvestTools != null && additionalHarvestTools.Any(tool => tool != ToolType.None));

    public bool AllowsTool(ToolType toolType)
    {
        if (toolType == ToolType.None)
            return false;

        if (harvestTool == toolType)
            return true;

        return additionalHarvestTools != null && additionalHarvestTools.Any(tool => tool == toolType);
    }

    public ToolType GetPrimaryRequiredTool()
    {
        if (harvestTool != ToolType.None)
            return harvestTool;

        if (additionalHarvestTools != null)
        {
            for (int i = 0; i < additionalHarvestTools.Length; i++)
            {
                if (additionalHarvestTools[i] != ToolType.None)
                    return additionalHarvestTools[i];
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
                ToolType tool = additionalHarvestTools[i];
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
