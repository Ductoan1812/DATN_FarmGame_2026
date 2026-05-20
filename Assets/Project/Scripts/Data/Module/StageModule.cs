using UnityEngine;

[System.Serializable]
public class StageModule : IModuleData
{
    public GrowthStage[] stages;

    [Tooltip("Sprite hiển thị khi cây héo (không tưới 1 ngày). Null = không có visual héo.")]
    public Sprite wiltSprite;

    public override IModuleRuntime CreateRuntime()
    {
        return new StageRuntime(this);
    }
}
