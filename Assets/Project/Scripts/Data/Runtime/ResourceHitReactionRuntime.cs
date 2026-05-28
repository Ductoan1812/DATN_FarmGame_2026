using UnityEngine;

public class ResourceHitReactionRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>, IHandleEvent<TakeDamageEvent>
{
    private readonly ResourceHitReactionModule data;
    private EntityRuntime entity;
    private ResourceHitReactionObject reactionObject;
    private ToolType requiredTool = ToolType.None;

    public ResourceHitReactionRuntime(ResourceHitReactionModule data)
    {
        this.data = data;
    }

    public void Handle(SpawnedEvent e)
    {
        entity = e.entity;
        requiredTool = entity?.GetModule<HarvestRuntime>()?.data?.harvestTool ?? ToolType.None;

        var owner = entity?.Owner?.GameObject;
        if (owner == null)
            return;

        reactionObject = owner.GetComponent<ResourceHitReactionObject>();
        if (reactionObject == null)
            reactionObject = owner.AddComponent<ResourceHitReactionObject>();

        reactionObject.Configure(data);
    }

    public void Handle(TakeDamageEvent e)
    {
        if (reactionObject == null || data == null)
            return;

        if (data.reactOnlyToHarvestTool && requiredTool != ToolType.None && e.toolType != requiredTool)
            return;

        reactionObject.PlayHit();
    }

    public ModuleSaveData ToSaveData() => null;

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => other is ResourceHitReactionRuntime;
}
