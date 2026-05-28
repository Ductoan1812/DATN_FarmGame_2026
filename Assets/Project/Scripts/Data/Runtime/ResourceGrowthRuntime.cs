using System;
using UnityEngine;

public class ResourceGrowthRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>, IHandleEvent<NextDayEvent>
{
    public int CurrentStageIndex => currentStageIndex;
    public int DaysInCurrentStage => daysInCurrentStage;
    public bool CanHarvest =>
        data?.stages != null
        && currentStageIndex >= 0
        && currentStageIndex < data.stages.Length
        && data.stages[currentStageIndex].canHarvest;

    private readonly ResourceGrowthModule data;
    private IEntityContainer owner;
    private SpriteRenderer spriteRenderer;
    private Sprite fallbackSprite;
    private int currentStageIndex;
    private int daysInCurrentStage;

    public ResourceGrowthRuntime(ResourceGrowthModule data)
    {
        this.data = data;
    }

    public void Handle(SpawnedEvent e)
    {
        owner = e.entity?.Owner;
        if (owner?.GameObject == null) return;

        spriteRenderer = owner.GameObject.GetComponentInChildren<SpriteRenderer>()
                      ?? owner.GameObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            fallbackSprite = spriteRenderer.sprite;

        ClampState();
        ApplyStageVisual();
    }

    public void Handle(NextDayEvent e)
    {
        if (data?.stages == null || data.stages.Length == 0)
            return;

        ClampState();
        if (currentStageIndex >= data.stages.Length - 1)
        {
            daysInCurrentStage = Mathf.Max(daysInCurrentStage, data.stages[currentStageIndex].daysToGrow);
            return;
        }

        int stageDays = Mathf.Max(1, data.stages[currentStageIndex].daysToGrow);
        daysInCurrentStage++;
        if (daysInCurrentStage < stageDays)
            return;

        currentStageIndex = Mathf.Min(currentStageIndex + 1, data.stages.Length - 1);
        daysInCurrentStage = 0;
        ApplyStageVisual();
    }

    public ModuleSaveData ToSaveData()
    {
        var save = new ResourceGrowthModuleSave
        {
            currentStageIndex = currentStageIndex,
            daysInCurrentStage = daysInCurrentStage
        };
        return new ModuleSaveData
        {
            moduleType = "ResourceGrowth",
            dataJson = JsonUtility.ToJson(save)
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrWhiteSpace(save.dataJson))
            return;

        var state = JsonUtility.FromJson<ResourceGrowthModuleSave>(save.dataJson);
        currentStageIndex = state.currentStageIndex;
        daysInCurrentStage = state.daysInCurrentStage;
        ClampState();
    }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not ResourceGrowthRuntime runtime)
            return false;

        return currentStageIndex == runtime.currentStageIndex
            && daysInCurrentStage == runtime.daysInCurrentStage;
    }

    public bool MatchesSave(ModuleSaveData save) => string.Equals(save?.moduleType, "ResourceGrowth", StringComparison.Ordinal);

    private void ClampState()
    {
        int stageCount = data?.stages?.Length ?? 0;
        if (stageCount <= 0)
        {
            currentStageIndex = 0;
            daysInCurrentStage = 0;
            return;
        }

        currentStageIndex = Mathf.Clamp(currentStageIndex, 0, stageCount - 1);
        daysInCurrentStage = Mathf.Max(0, daysInCurrentStage);
    }

    private void ApplyStageVisual()
    {
        if (spriteRenderer == null || data?.stages == null || data.stages.Length == 0)
            return;

        Sprite stageSprite = data.stages[currentStageIndex].sprite != null
            ? data.stages[currentStageIndex].sprite
            : fallbackSprite;

        if (stageSprite != null)
            spriteRenderer.sprite = stageSprite;
    }

    [Serializable]
    private class ResourceGrowthModuleSave
    {
        public int currentStageIndex;
        public int daysInCurrentStage;
    }
}
