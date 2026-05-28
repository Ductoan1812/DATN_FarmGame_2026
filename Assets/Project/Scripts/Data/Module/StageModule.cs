using UnityEngine;

[System.Serializable]
public class StageModule : IModuleData
{
    public GrowthStage[] stages;

    [Tooltip("Sprite hiển thị khi cây héo khi qua mùa mới. Null = không có visual héo.")]
    public Sprite wiltSprite;

    [Tooltip("Cây tái thu hoạch? Nếu >= 0, sau harvest sẽ reset về stage này thay vì chết. VD: regrowStageIndex=2 → sau harvest quay về stage 2.")]
    public int regrowStageIndex = -1;

    [Tooltip("Nếu bật, cây sẽ chuyển sang héo khi qua mùa mới.")]
    public bool wiltOnSeasonChange = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new StageRuntime(this);
    }
}
