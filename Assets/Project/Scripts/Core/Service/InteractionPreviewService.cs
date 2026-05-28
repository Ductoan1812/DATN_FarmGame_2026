using System.Collections.Generic;
using UnityEngine;

public static class InteractionPreviewService
{
    private const string DefaultQuestOptionTextKey = "ui.quest.view";
    private const string DefaultHarvestOptionTextKey = "ui.interaction.harvest";
    private const string HarvestNotReadyTextKey = "ui.interaction.not_ready";
    private const string NeedToolReasonTextKey = "ui.interaction.need_tool_format";

    public static InteractionPreviewData Build(EntityRuntime interactor, EntityRuntime target)
    {
        if (target == null)
            return InteractionPreviewData.Empty(interactor);

        string targetNameKey = target.entityData?.keyName;
        string targetNameFallback = target.entityData?.id ?? target.id;
        Sprite icon = target.entityData?.icon;

        string actionTextKey = string.Empty;
        int bestPriority = int.MaxValue;
        ToolType requiredTool = ToolType.None;
        string statusTextKey = string.Empty;
        string blockedReasonKey = string.Empty;
        bool isBlocked = false;

        var modules = target.entityData?.modules;
        if (modules != null)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null) continue;

                switch (module)
                {
                    case DialogueModule dialogue:
                        TryChooseOption(dialogue.optionTextKey, dialogue.priority, ref actionTextKey, ref bestPriority);
                        break;
                    case QuestModule quest:
                        TryChooseOption(DefaultQuestOptionTextKey, quest.priority, ref actionTextKey, ref bestPriority);
                        break;
                    case ShopModule shop:
                        TryChooseOption(shop.optionTextKey, shop.priority, ref actionTextKey, ref bestPriority);
                        break;
                    case CraftingModule crafting:
                        TryChooseOption(crafting.optionTextKey, crafting.priority, ref actionTextKey, ref bestPriority);
                        break;
                    case ScenePortalModule portal:
                        TryChooseOption(portal.optionTextKey, portal.priority, ref actionTextKey, ref bestPriority);
                        break;
                    case AnimalModule animal:
                        var runtime = target.GetModule<AnimalRuntime>();
                        TryChooseOption(runtime?.PrimaryOptionTextKey ?? animal.feedOptionTextKey, animal.priority, ref actionTextKey, ref bestPriority);
                        statusTextKey = runtime?.StatusTextKey ?? animal.statusHungryKey;
                        break;
                    case HarvestModule harvest:
                        TryChooseOption(DefaultHarvestOptionTextKey, 25, ref actionTextKey, ref bestPriority);
                        ResolveHarvestState(interactor, target, harvest, out requiredTool, out statusTextKey, out blockedReasonKey, out isBlocked);
                        break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(actionTextKey))
            actionTextKey = "ui.common.use";

        return new InteractionPreviewData(
            interactor,
            target,
            targetNameKey,
            targetNameFallback,
            actionTextKey,
            statusTextKey,
            blockedReasonKey,
            requiredTool,
            isBlocked,
            bestPriority,
            icon);
    }

    private static void ResolveHarvestState(
        EntityRuntime interactor,
        EntityRuntime target,
        HarvestModule harvest,
        out ToolType requiredTool,
        out string statusTextKey,
        out string blockedReasonKey,
        out bool isBlocked)
    {
        requiredTool = harvest.AllowsHandHarvest ? ToolType.None : harvest.GetPrimaryRequiredTool();
        statusTextKey = string.Empty;
        blockedReasonKey = string.Empty;
        isBlocked = false;

        var stage = target.GetModule<StageRuntime>();
        if (stage != null && !stage.CanHarvest)
        {
            isBlocked = true;
            statusTextKey = HarvestNotReadyTextKey;
            blockedReasonKey = HarvestNotReadyTextKey;
            return;
        }

        var resourceGrowth = target.GetModule<ResourceGrowthRuntime>();
        if (resourceGrowth != null && !resourceGrowth.CanHarvest)
        {
            isBlocked = true;
            statusTextKey = HarvestNotReadyTextKey;
            blockedReasonKey = HarvestNotReadyTextKey;
            return;
        }

        if (harvest.AllowsHandHarvest)
            return;

        ToolType selectedTool = ResolveSelectedTool(interactor);
        if (harvest.AllowsTool(selectedTool))
            return;

        isBlocked = true;
        ToolType neededTool = harvest.GetPrimaryRequiredTool();
        blockedReasonKey = BuildNeedToolKey(neededTool);
        requiredTool = neededTool;
    }

    private static ToolType ResolveSelectedTool(EntityRuntime interactor)
    {
        if (interactor == null) return ToolType.None;

        var inventories = interactor.GetModules<InventoryRuntime>();
        InventoryRuntime hotbar = null;
        for (int i = 0; i < inventories.Count; i++)
        {
            if (inventories[i] == null) continue;
            if (inventories[i].Type != InventoryType.Hotbar) continue;
            hotbar = inventories[i];
            break;
        }

        var selected = hotbar?.SelectedEntity;
        if (selected == null) return ToolType.None;

        if (selected.GetModule<PickaxeRuntime>() != null) return ToolType.Pickaxe;
        if (selected.GetModule<AxeRuntime>() != null) return ToolType.Axe;
        if (selected.GetModule<ScytheRuntime>() != null) return ToolType.Scythe;
        if (selected.GetModule<HoeRuntime>() != null) return ToolType.Hoe;
        return ToolType.None;
    }

    private static string BuildNeedToolKey(ToolType toolType)
    {
        string toolKey = toolType switch
        {
            ToolType.Axe => "ui.tool.axe",
            ToolType.Pickaxe => "ui.tool.pickaxe",
            ToolType.Scythe => "ui.tool.scythe",
            ToolType.Hoe => "ui.tool.hoe",
            ToolType.WateringCan => "ui.tool.watering_can",
            ToolType.FishingRod => "ui.tool.fishing_rod",
            _ => "ui.tool.unknown"
        };

        return $"{NeedToolReasonTextKey}|{toolKey}";
    }

    private static void TryChooseOption(string textKey, int priority, ref string currentTextKey, ref int currentPriority)
    {
        if (string.IsNullOrWhiteSpace(textKey)) return;

        if (priority < currentPriority)
        {
            currentPriority = priority;
            currentTextKey = textKey;
        }
    }
}
