using UnityEngine;
using System;

public class StageRuntime : IModuleRuntime, IHandleEvent<NextDayEvent>, IHandleEvent<SpawnedEvent>
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
        && data.stages[currentStageIndex].canHarvest;

    /// <summary>Số ngày liên tiếp không được tưới.</summary>
    public int DaysWithoutWater => daysWithoutWater;

    /// <summary>Cây đang héo?</summary>
    public bool IsWilting => isWilting;

    public StageRuntime(StageModule data)
    {
        this.data = data;
        if (data.stages.Length > 0) currentStageIndex = 0;
        TotaldaysInCurrentStage = data.stages[currentStageIndex].daysToGrow;
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
        if (tracker != null && Owner.GameObject != null)
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
            // ── TƯỚI: grow + reset wilt ──
            if (isWilting)
            {
                // Revert sprite từ wilt về stage hiện tại
                isWilting = false;
                if (spriteRenderer != null && currentStageIndex < data.stages.Length)
                    spriteRenderer.sprite = data.stages[currentStageIndex].sprite;
                Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' hồi phục từ héo!");
            }

            daysWithoutWater = 0;

            // Grow logic
            TotaldaysInCurrentStage = data.stages[currentStageIndex].daysToGrow;
            daysInCurrentStage++;
            Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' stage {currentStageIndex}: {daysInCurrentStage}/{TotaldaysInCurrentStage} ngày.");

            if (daysInCurrentStage >= TotaldaysInCurrentStage)
            {
                if (currentStageIndex < data.stages.Length - 1)
                {
                    currentStageIndex++;
                    daysInCurrentStage = 0;
                    if (spriteRenderer == null)
                        spriteRenderer = Owner.GameObject.GetComponentInChildren<SpriteRenderer>();
                    if (spriteRenderer != null)
                        spriteRenderer.sprite = data.stages[currentStageIndex].sprite;

                    Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' lên stage {currentStageIndex}!");
                }
                else
                {
                    daysInCurrentStage = TotaldaysInCurrentStage;
                }
            }
        }
        else
        {
            // ── KHÔNG TƯỚI: wilt logic ──
            daysWithoutWater++;
            Debug.Log($"[StageRuntime] '{Owner.GameObject.name}' KHÔNG tưới! daysWithoutWater={daysWithoutWater}");

            if (daysWithoutWater == 1)
            {
                // Ngày 1 không tưới: hiện sprite héo (cảnh báo)
                isWilting = true;
                if (data.wiltSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = data.wiltSprite;
                    Debug.LogWarning($"[StageRuntime] '{Owner.GameObject.name}' đang HÉO! Cần tưới nước.");
                }
                else
                {
                    Debug.LogWarning($"[StageRuntime] '{Owner.GameObject.name}' đang héo (không có wiltSprite).");
                }
            }
            else if (daysWithoutWater >= 2)
            {
                // Ngày 2+ không tưới: cây CHẾT
                Debug.LogError($"[StageRuntime] '{Owner.GameObject.name}' CHẾT vì không tưới {daysWithoutWater} ngày!");
                if (_entity != null)
                    _entity.TriggerEvent(new DieEvent(_entity));
            }
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

        spriteRenderer.sprite = data.stages[currentStageIndex].sprite;
        Debug.Log($"[StageRuntime] Đã set sprite stage {currentStageIndex} cho '{Owner.GameObject.name}' (CanHarvest={CanHarvest})");
    }

    public void Handle(NextDayEvent e)
    {
        if (Owner == null) return;
        UpdateStage();
    }
}
