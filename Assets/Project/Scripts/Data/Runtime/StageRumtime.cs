using UnityEngine;
using System;

public class StageRuntime : IModuleRuntime, IHandleEvent<NextDayEvent>, IHandleEvent<SpawnedEvent>, IHandleEvent<SeasonChangedEvent>
{
    public int currentStageIndex = 0;
    private int daysInCurrentStage = 0;
    private int daysWithoutWater = 0;
    private bool isWilting = false;
    private int TotaldaysInCurrentStage;
    private StageModule data;
    private IEntityContainer Owner { get; set; }
    private EntityRuntime _entity;
    private SpriteRenderer spriteRenderer;

    /// <summary>True nếu stage hiện tại cho phép thu hoạch.</summary>
    public bool CanHarvest =>
        data?.stages != null
        && currentStageIndex >= 0
        && currentStageIndex < data.stages.Length
        && !isWilting
        && data.stages[currentStageIndex].canHarvest;

    /// <summary>Số ngày liên tiếp không được tưới.</summary>
    public int DaysWithoutWater => daysWithoutWater;

    /// <summary>Cây đang héo?</summary>
    public bool IsWilting => isWilting;

    /// <summary>Cây có thể tái thu hoạch?</summary>
    public bool IsRegrowable => data != null && data.regrowStageIndex >= 0 && data.regrowStageIndex < (data.stages?.Length ?? 0);

    /// <summary>Reset cây về regrow stage sau harvest (chỉ dùng cho regrowable crops).</summary>
    public void ResetToRegrowStage()
    {
        if (!IsRegrowable) return;

        MoveToStage(data.regrowStageIndex, resetHp: true, clearWilt: true, logReason: "reset về regrow stage");
    }

    /// <summary>Harvest xong chuyển theo loop stage nếu có, fallback về regrowStageIndex kiểu cũ.</summary>
    public bool TryAdvanceAfterHarvest()
    {
        int nextHarvestStage = GetHarvestReturnStageIndex();
        if (nextHarvestStage >= 0 && nextHarvestStage != currentStageIndex)
        {
            MoveToStage(nextHarvestStage, resetHp: true, clearWilt: true, logReason: "chuyển sang harvest loop stage");
            return true;
        }

        if (!IsRegrowable)
            return false;

        ResetToRegrowStage();
        return true;
    }

    public StageRuntime(StageModule data)
    {
        this.data = data;
        if (data?.stages != null && data.stages.Length > 0)
        {
            currentStageIndex = 0;
            TotaldaysInCurrentStage = Mathf.Max(1, data.stages[currentStageIndex].daysToGrow);
        }
    }

    public ModuleSaveData ToSaveData()
    {
        var s = new StageModuleSave
        {
            currentStageIndex = this.currentStageIndex,
            daysInCurrentStage = this.daysInCurrentStage,
            daysWithoutWater = this.daysWithoutWater,
            isWilting = this.isWilting
        };
        return new ModuleSaveData { moduleType = "Stage", dataJson = JsonUtility.ToJson(s) };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;
        var s = JsonUtility.FromJson<StageModuleSave>(save.dataJson);
        currentStageIndex = s.currentStageIndex;
        daysInCurrentStage = s.daysInCurrentStage;
        daysWithoutWater = s.daysWithoutWater;
        isWilting = s.isWilting;
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
        public int daysWithoutWater;
        public bool isWilting;
    }

    public void UpdateStage()
    {
        if (Owner == null) return;
        if (data?.stages == null || data.stages.Length == 0) return;

        // Check watered
        bool watered = false;
        var tracker = GameManager.Instance?.WateredTileTracker;
        if (!data.requiresWater)
        {
            watered = true;
        }
        else if (tracker != null && Owner.GameObject != null)
        {
            // Dùng GridSystem.WorldToCell để đảm bảo khớp với tilemap coordinate
            // Cây spawn tại center của cell (e.g. 16.5, -11.5) → WorldToCell → (16, -11)
            var worldPos = Owner.GameObject.transform.position;
            Vector3Int cell3 = GridSystem.WorldToCell(worldPos);
            var cell = new Vector2Int(cell3.x, cell3.y);
            watered = tracker.IsWatered(cell);
            Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' worldPos={worldPos} → cell={cell} → watered={watered}");
        }

        if (watered)
        {
            // Cây đã héo do qua mùa thì không thể hồi chỉ bằng tưới nước.
            if (isWilting)
            {
                Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' đã héo do qua mùa, tưới nước không làm cây hồi lại.");
                return;
            }

            daysWithoutWater = 0;

            // Grow logic
            TotaldaysInCurrentStage = GetDaysToGrowForCurrentStage();
            daysInCurrentStage++;
            Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' stage {currentStageIndex}: {daysInCurrentStage}/{TotaldaysInCurrentStage} ngày.");

            if (daysInCurrentStage >= TotaldaysInCurrentStage)
            {
                int nextStageIndex = GetNextStageIndex();
                if (nextStageIndex >= 0 && nextStageIndex != currentStageIndex)
                {
                    MoveToStage(nextStageIndex, resetHp: false, clearWilt: false, logReason: "lên stage");
                }
                else
                {
                    daysInCurrentStage = TotaldaysInCurrentStage;
                }
            }
        }
        else
        {
            // Rule mới: không tưới thì cây chỉ đứng yên, không phát triển.
            daysWithoutWater = 0;
            Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' không được tưới hôm nay nên không phát triển.");
        }
    }

    public void Handle(SpawnedEvent e)
    {
        _entity = e.entity;
        Owner = e.entity.Owner;
        if (Owner?.GameObject == null) return;

        spriteRenderer = Owner.GameObject.GetComponentInChildren<SpriteRenderer>()
                      ?? Owner.GameObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[StageRuntime] Không tìm thấy SpriteRenderer trên '{Owner.GameObject.name}'");
            return;
        }

        ApplyStageVisual();
        Debug.Log($"[StageRuntime] Đã set sprite stage {currentStageIndex} cho '{Owner.GameObject.name}' (CanHarvest={CanHarvest})");
    }

    public void Handle(NextDayEvent e)
    {
        if (Owner == null) return;
        UpdateStage();
    }

    public void Handle(SeasonChangedEvent e)
    {
        if (Owner?.GameObject == null) return;
        if (data == null || !data.wiltOnSeasonChange) return;
        if (isWilting) return;

        isWilting = true;
        daysWithoutWater = 0;

        if (spriteRenderer == null)
            spriteRenderer = Owner.GameObject.GetComponentInChildren<SpriteRenderer>()
                          ?? Owner.GameObject.GetComponent<SpriteRenderer>();

        if (data.wiltSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = data.wiltSprite;

        Debug.LogWarning($"[StageRuntime] '{Owner.GameObject.name}' đã héo vì sang mùa {e.season} năm {e.year}.");
    }

    private int GetNextStageIndex()
    {
        if (data?.stages == null || currentStageIndex < 0 || currentStageIndex >= data.stages.Length)
            return -1;

        if (IsHarvestRecoveryStage())
            return data.lastStageLoopToIndex;

        if (currentStageIndex >= data.stages.Length - 1)
        {
            int loopStage = data.lastStageLoopToIndex;
            if (loopStage >= 0 && loopStage < data.stages.Length)
                return loopStage;

            return currentStageIndex;
        }

        return currentStageIndex + 1;
    }

    private int GetHarvestReturnStageIndex()
    {
        if (data?.stages == null || currentStageIndex < 0 || currentStageIndex >= data.stages.Length || !CanHarvest)
            return -1;

        int configured = data.harvestGoToStageIndex;
        if (configured >= 0 && configured < data.stages.Length)
            return configured;

        return -1;
    }

    private void MoveToStage(int stageIndex, bool resetHp, bool clearWilt, string logReason)
    {
        if (data?.stages == null) return;
        if (stageIndex < 0 || stageIndex >= data.stages.Length) return;

        currentStageIndex = stageIndex;
        daysInCurrentStage = 0;
        daysWithoutWater = 0;
        if (clearWilt)
            isWilting = false;

        TotaldaysInCurrentStage = GetDaysToGrowForCurrentStage();
        ApplyStageVisual();

        if (resetHp && _entity?.stats != null)
        {
            float maxHp = _entity.stats.Get(StatType.MaxHp);
            if (maxHp > 0f)
                _entity.stats.Set(StatType.Hp, maxHp);
        }

        if (Owner?.GameObject != null)
            Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' {logReason} {currentStageIndex}.");
    }

    private int GetDaysToGrowForCurrentStage()
    {
        if (data?.stages == null || currentStageIndex < 0 || currentStageIndex >= data.stages.Length)
            return 1;

        if (IsHarvestRecoveryStage() && data.daysToReturnAfterHarvest > 0)
            return data.daysToReturnAfterHarvest;

        return Mathf.Max(1, data.stages[currentStageIndex].daysToGrow);
    }

    private bool IsHarvestRecoveryStage()
    {
        if (data == null)
            return false;

        if (data.harvestGoToStageIndex < 0 || data.lastStageLoopToIndex < 0)
            return false;

        return currentStageIndex == data.harvestGoToStageIndex;
    }

    private void ApplyStageVisual()
    {
        if (spriteRenderer == null && Owner?.GameObject != null)
        {
            spriteRenderer = Owner.GameObject.GetComponentInChildren<SpriteRenderer>()
                          ?? Owner.GameObject.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null || data?.stages == null || currentStageIndex < 0 || currentStageIndex >= data.stages.Length)
            return;

        spriteRenderer.sprite = data.stages[currentStageIndex].sprite;
    }
}
