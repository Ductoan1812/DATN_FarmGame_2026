using UnityEngine;

public interface IEntityContainer
{
    public GameObject GameObject { get; }
    bool Add(EntityRuntime entity);
    bool Remove(EntityRuntime entity, int amount = 1);
}
