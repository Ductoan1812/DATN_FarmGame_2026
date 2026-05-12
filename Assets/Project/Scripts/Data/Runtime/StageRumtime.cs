using UnityEngine;
using System;

public class StageRuntime : IModuleRuntime, IHandleEvent<NextDayEvent>, IHandleEvent<SpawnedEvent>
{
    public int currentStageIndex = 0;
    public bool CanHarvest ;
    private int daysInCurrentStage = 0;
    private int TotaldaysInCurrentStage;
    private StageModule data;
    private IEntityContainer Owner { get; set; }
    private SpriteRenderer spriteRenderer;

    /// <summary>True nếu stage hiện tại cho phép thu hoạch.</summary>
    public bool CanHarvest =>
        data?.stages != null
        && currentStageIndex >= 0
        && currentStageIndex < data.stages.Length
        && data.stages[currentStageIndex].canHarvest;

    public StageRuntime(StageModule data)
    {
        this.data = data;
        if (data.stages.Length > 0) currentStageIndex = 0;
        TotaldaysInCurrentStage = data.stages[currentStageIndex].daysToGrow;
        CanHarvest = data.stages[currentStageIndex].canHarvest;
    }

    public ModuleSaveData ToSaveData()
    {
        var s = new StageModuleSave { currentStageIndex = this.currentStageIndex, daysInCurrentStage = this.daysInCurrentStage };
        return new ModuleSaveData { moduleType = "Stage", dataJson = JsonUtility.ToJson(s) };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;
        var s = JsonUtility.FromJson<StageModuleSave>(save.dataJson);
        currentStageIndex = s.currentStageIndex;
        daysInCurrentStage = s.daysInCurrentStage;
        CanHarvest = data.stages[currentStageIndex].canHarvest;
    }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not StageRuntime o) return false;
        return currentStageIndex == o.currentStageIndex;
    }

    [Serializable]
    private class StageModuleSave
    {
        public int currentStageIndex;
        public int daysInCurrentStage;
    }

    public void UpdateStage()
    {
        if (Owner == null) return;
        if (data?.stages == null || data.stages.Length == 0) return;

        TotaldaysInCurrentStage = data.stages[currentStageIndex].daysToGrow;
        daysInCurrentStage++;
        Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' đã ở stage {currentStageIndex} được {daysInCurrentStage}/{TotaldaysInCurrentStage} ngày.");
        if (daysInCurrentStage >= TotaldaysInCurrentStage)
        {
            if (currentStageIndex < data.stages.Length - 1)
            {
                currentStageIndex++;
                daysInCurrentStage = 0;
                CanHarvest = data.stages[currentStageIndex].canHarvest;
                if (spriteRenderer == null)
                    spriteRenderer = Owner.GameObject.GetComponentInChildren<SpriteRenderer>();

                if (spriteRenderer != null)
                    spriteRenderer.sprite = data.stages[currentStageIndex].sprite;
                
            }
            else
            {
                daysInCurrentStage = TotaldaysInCurrentStage;
                Debug.LogWarning($"[StageRuntime] '{Owner.GameObject.name}' đã đạt giai đoạn cuối cùng ({currentStageIndex}). Không thể tăng stage nữa.");
            }
        }
    }

    public void Handle(SpawnedEvent e)
    {
        Owner = e.entity.Owner;
        if (Owner?.GameObject == null) return;

        spriteRenderer = Owner.GameObject.GetComponentInChildren<SpriteRenderer>()
                      ?? Owner.GameObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[StageRuntime] Không tìm thấy SpriteRenderer trên '{Owner.GameObject.name}'");
            return;
        }

        CanHarvest = data.stages[currentStageIndex].canHarvest;
        spriteRenderer.sprite = data.stages[currentStageIndex].sprite;
        Debug.Log($"[StageRuntime] Đã set sprite stage {currentStageIndex} cho '{Owner.GameObject.name}' (CanHarvest={CanHarvest})");
    }

    public void Handle(NextDayEvent e)
    {
        if (Owner == null) return;
        UpdateStage();
    }
}
