using System.Collections.Generic;

public class EntityRegistry
{
    private Dictionary<string, EntityRuntime> entities = new();

    public void Register(EntityRuntime entity)
    {
        entities[entity.Id] = entity;
    }

    public void Unregister(EntityRuntime entity)
    {
        entities.Remove(entity.Id);
    }

    public EntityRuntime Get(string id)
    {
        return entities.TryGetValue(id, out var entity) ? entity : null;
    }

    public IEnumerable<EntityRuntime> GetAll()
    {
        return entities.Values;
    }

    public void Clear()
    {
        entities.Clear();
    }
}
