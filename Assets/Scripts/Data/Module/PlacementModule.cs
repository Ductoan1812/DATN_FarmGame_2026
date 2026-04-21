using UnityEngine;

/// <summary>
/// Gắn vào EntityData của item có thể đặt xuống (hạt giống, cọc gỗ, rương...).
/// Khi PrimaryActionEvent kích hoạt → PlacementRuntime spawn entity tại ô phía trước actor.
/// </summary>
[System.Serializable]
public class PlacementModule : IModuleData
{
    [Tooltip("Loại object sẽ được spawn xuống world")]
    public ObjectType objectTypeToSpawn;
    [Tooltip("True = spawn giữa Tile")]
    public bool centerTile = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new PlacementRuntime(this);
    }
}
