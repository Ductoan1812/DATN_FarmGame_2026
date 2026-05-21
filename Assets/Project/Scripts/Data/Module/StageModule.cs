using UnityEngine;

[System.Serializable]
public class StageModule : IModuleData
{
    public GrowthStage[] stages;

    [Tooltip("Sprite hiển thị khi cây héo (không tưới 1 ngày). Null = không có visual héo.")]
    public Sprite wiltSprite;

    [Tooltip("Cây tái thu hoạch? Nếu >= 0, sau harvest sẽ reset về stage này thay vì chết. VD: regrowStageIndex=2 → sau harvest quay về stage 2.")]
    public int regrowStageIndex = -1;

    public override IModuleRuntime CreateRuntime()
    {
        return new StageRuntime(this);
    }
}
