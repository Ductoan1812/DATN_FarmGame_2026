using UnityEngine;
using System;

[DisallowMultipleComponent]
public class EntityRoot : MonoBehaviour, IEntityContainer
{
    GameObject IEntityContainer.GameObject => this.gameObject;

    private EntityRuntime entity;

    public EntityService entityService;
    public event Action<EntityRuntime> OnEntityReady;

    public EntityRuntime GetEntity() => entity;

    public void Init(EntityRuntime runtime)
    {
        entity = runtime;
        if (entity != null) entity.Owner = this;
    }

    public bool Add(EntityRuntime entity)
    {
        this.entity = entity;
        if (entity != null) entity.Owner = this;
        entity.TriggerEvent(new SpawnedEvent(entity));
        OnEntityReady?.Invoke(entity);
        return true;
    }

    public bool Remove(EntityRuntime entity, int amount = 1)
    {
        if (this.entity != entity) return false;
        this.entity = null;
        return true;
    }
}
