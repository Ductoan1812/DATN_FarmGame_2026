using UnityEngine;

[System.Serializable]
public class StageModule : IModuleData
{
    public GrowthStage[] stages;

    [Tooltip("Nếu bật, entity phải được tưới mới phát triển. Tắt cho cây thân gỗ, cây ăn quả, resource sống độc lập.")]
    public bool requiresWater = true;

    [Tooltip("Sprite hiển thị khi cây héo khi qua mùa mới. Null = không có visual héo.")]
    public Sprite wiltSprite;

    [Tooltip("Stage fallback sau harvest nếu chưa set stage chuyển riêng. -1 = harvest xong bị hủy.")]
    public int regrowStageIndex = -1;

    [Tooltip("Harvest ở stage chín sẽ chuyển sang stage này thay vì bị hủy. -1 = harvest xong bị hủy.")]
    public int harvestGoToStageIndex = -1;

    [Tooltip("Sau khi harvest xong và chuyển sang harvestGoToStageIndex, đủ số ngày chờ sẽ quay về stage này. -1 = không loop.")]
    public int lastStageLoopToIndex = -1;

    [Tooltip("Số ngày chờ ở stage đã thu hoạch trước khi quay lại lastStageLoopToIndex. <= 0 = dùng daysToGrow của chính stage đó.")]
    public int daysToReturnAfterHarvest = -1;

    [Tooltip("Nếu bật, cây sẽ chuyển sang héo khi qua mùa mới.")]
    public bool wiltOnSeasonChange = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new StageRuntime(this);
    }
}
