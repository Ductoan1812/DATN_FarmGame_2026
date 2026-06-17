using UnityEngine;

[System.Serializable]
public class BuildingModule : IModuleData
{
    public EntityData buildingEntity;
    public ObjectType buildingPrefabId = ObjectType.EntityDrop;
    public bool consumeItemOnSuccess = true;
    public int priority = 20;

    public override IModuleRuntime CreateRuntime()
    {
        return new BuildingPlacementRuntime(this);
    }
}
