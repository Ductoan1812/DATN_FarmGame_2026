using UnityEngine;

public class BuildingPlacementRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>
{
    private readonly BuildingModule data;

    public BuildingPlacementRuntime(BuildingModule data)
    {
        this.data = data;
    }

    public void Handle(PrimaryActionEvent e)
    {
        if (e.actor == null || data.buildingEntity == null) return;

        var gm = GameManager.Instance;
        if (gm?.EventBus == null) return;

        Vector2 actorPos = Vector2.zero;
        var actorGO = e.actor.Owner?.GameObject;
        if (actorGO != null)
        {
            actorPos = actorGO.transform.position;
        }

        Vector2Int target = new Vector2Int(Mathf.FloorToInt(actorPos.x), Mathf.FloorToInt(actorPos.y));

        gm.EventBus.Publish(new SpawnRequestPublish(
            worldPos: target,
            idPrefab: data.buildingPrefabId,
            entityData: data.buildingEntity,
            spawnAmount: 1,
            bypassValidation: false
        ));

        // TODO: consume item after SpawnSystem confirms placement success (callback needed)
    }

    public ModuleSaveData ToSaveData()
    {
        return new ModuleSaveData
        {
            moduleType = "BuildingPlacement",
            dataJson = string.Empty
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
    }

    public bool Equals(IModuleRuntime other)
    {
        return other is BuildingPlacementRuntime;
    }
}
