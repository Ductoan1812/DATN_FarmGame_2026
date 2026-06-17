using UnityEngine;

/// <summary>
/// Gắn vào EntityData của item có thể đặt xuống (hạt giống, cọc gỗ, rương...).
/// Khi PrimaryActionEvent kích hoạt → validate → play animation → spawn entity tại ô phía trước actor.
/// </summary>
[System.Serializable]
public class PlacementModule : IModuleData
{
    [Tooltip("Loại object sẽ được spawn xuống world")]
    public ObjectType objectTypeToSpawn;
    [Tooltip("EntityData world object sau khi đặt xuống. Để trống sẽ dùng item đang cầm như behavior cũ.")]
    public EntityData placedEntityData;
    [Tooltip("True = spawn giữa Tile")]
    public bool centerTile = true;
    [Tooltip("Tên trigger trong Animator. Để trống = mặc định 'Sow'.")]
    public string animTrigger;

    public override IModuleRuntime CreateRuntime()
    {
        return new PlacementRuntime(this);
    }
}
