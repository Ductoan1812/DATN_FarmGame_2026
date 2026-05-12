using UnityEngine;

/// <summary>
/// Module cho entity có khả năng hồi sinh (Player, NPC thân thiện, boss định kỳ…).
/// Khi chết:
///   1. GameObject bị despawn (ẩn khỏi world).
///   2. Sau respawnDelay giây → spawn lại entity tại vị trí respawn.
///      Entity VẪN trong EntityRegistry — giữ nguyên inventory, stats.
///
/// Vị trí respawn có thể thay đổi runtime (checkpoint…) và được lưu save/load.
/// Config trong module chỉ là giá trị khởi tạo.
/// </summary>
[System.Serializable]
public class RespawnModule : IModuleData
{
    [Tooltip("Vị trí respawn mặc định khi entity được tạo mới.")]
    public Vector2 defaultRespawnPosition = Vector2.zero;

    [Tooltip("Delay (giây) từ khi chết đến khi respawn.")]
    public float respawnDelay = 3f;

    [Tooltip("Hồi đầy HP khi respawn.")]
    public bool restoreFullHp = true;

    [Tooltip("ObjectType prefab để respawn. Phải khớp với prefab gốc của entity.")]
    public ObjectType respawnPrefabId = ObjectType.Player01;

    public override IModuleRuntime CreateRuntime()
    {
        return new RespawnRuntime(this);
    }
}
