using UnityEngine;

/// <summary>
/// Pure container — chỉ giữ reference tới EntityRuntime.
/// Không tự tạo entity. Entity được gán từ bên ngoài (SpawnSystem, SaveLoadManager).
/// </summary>
[DisallowMultipleComponent]
public class EntityRoot : MonoBehaviour, IEntityContainer
{
    GameObject IEntityContainer.GameObject => this.gameObject;

    private EntityRuntime entity;

    public EntityService entityService;

    public void Init(EntityRuntime runtime)
    {
        entity = runtime;
        if (entity != null) entity.Owner = this;
    }

    public EntityRuntime GetEntity()
    {
        return entity;
    }

    public bool Add(EntityRuntime entity)
    {
        this.entity = entity;
        if (entity != null) entity.Owner = this;
        entity.TriggerEvent(new SpawnedEvent(entity));
        return true;
    }

    public bool Remove(EntityRuntime entity, int amount = 1)
    {
        if (this.entity != entity) return false;
        this.entity = null;
        return true;
    }
}
