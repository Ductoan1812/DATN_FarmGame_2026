using UnityEngine;

public class QualityRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>
{
    private QualityModule data;
    private EntityRuntime entity;
    private GameObject ownerGO;

    public QualityRuntime(QualityModule data)
    {
        this.data = data;
    }

    public void Handle(SpawnedEvent e)
    {
        entity = e.entity;
        ownerGO = e.entity?.Owner?.GameObject;
    }

    public int GetHarvestQuality()
    {
        if (data == null || ownerGO == null) return data?.minQuality ?? 1;

        var gm = GameManager.Instance;
        if (gm?.SoilQualityTracker == null)
            return data.minQuality;

        var cell3 = GridSystem.WorldToCell(ownerGO.transform.position);
        var cell = new Vector2Int(cell3.x, cell3.y);
        int soilQuality = gm.SoilQualityTracker.GetQuality(cell);
        int soilQualityPerStar = Mathf.Max(1, data.soilQualityPerStar);

        int quality = data.minQuality + Mathf.FloorToInt(soilQuality / (float)soilQualityPerStar);
        quality = Mathf.Clamp(quality, data.minQuality, data.maxQuality);

        var stage = entity?.GetModule<StageRuntime>();
        if (stage != null && (stage.IsWilting || stage.DaysWithoutWater > 0))
            quality = data.minQuality;

        return quality;
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is QualityRuntime;
}
