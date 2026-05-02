using UnityEngine;

public interface IToolHandler
{
    bool CanHandle(EntityRuntime entity);
    void Execute(Vector2Int[] targetCells, EntityRuntime entity);
}
