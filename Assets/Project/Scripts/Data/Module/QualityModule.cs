using UnityEngine;

[System.Serializable]
public class QualityModule : IModuleData
{
    [Tooltip("Chất lượng tối thiểu (1 = thường, 2 = tốt, 3 = xuất sắc)")]
    public int minQuality = 1;

    [Tooltip("Chất lượng tối đa")]
    public int maxQuality = 3;

    [Tooltip("Điểm đất cần để tăng 1 sao chất lượng")]
    public int soilQualityPerStar = 1;

    public override IModuleRuntime CreateRuntime() => new QualityRuntime(this);
}
