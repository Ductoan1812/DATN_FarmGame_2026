using UnityEngine;

/// <summary>
/// Physics bridge: phát hiện Player chạm EntityDrop → gọi InventoryService.Pickup.
/// - Nhặt thành công (received > 0) → chạy animation fly-to-player (nếu có DropMotionObject)
///   → publish DespawnRequestPublish để dọn GameObject + spatial registry.
/// - Nhặt thất bại (received == 0, inventory đầy) → chỉ reset cooldown, giữ GO.
///
/// DropMotionObject KHÔNG còn tự lắng OnTriggerEnter2D — chỉ expose OnPickedUp() để file này gọi.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PickUpObject : MonoBehaviour
{
    [SerializeField] public string targetTag = "Player";
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float pickupDelay = 3f;

    private EntityRuntime _entity;
    private float _nextTriggerTime;
    private float _enabledTime;

    private void OnEnable()
    {
        _enabledTime = Time.time;
    }

    private void OnDisable()
    {
        _entity = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < _nextTriggerTime) return;
        if (Time.time < _enabledTime + pickupDelay) return;
        if (!other.CompareTag(targetTag)) return;

        // Lazy-cache entity (thay cho polling trong Update)
        if (_entity == null)
        {
            _entity = GetComponent<EntityRoot>()?.GetEntity();
            if (_entity == null) return;
        }

        var ownerEntity = other.GetComponent<EntityRoot>()?.GetEntity();
        if (ownerEntity == null) return;

        string entityId = _entity.id;
        int received = GameManager.Instance.InventoryService.Pickup(_entity, ownerEntity);

        _nextTriggerTime = Time.time + cooldown;

        if (received <= 0)
        {
            // Inventory đầy — giữ GO, chỉ chờ cooldown.
            return;
        }

        // Nhặt được ít nhất 1 unit → GO không còn ý nghĩa ở world.
        // Chạy animation fly-to-player nếu có, rồi despawn.
        var motion = GetComponent<DropMotionObject>();
        if (motion != null)
            motion.OnPickedUp(other.transform);

        // Despawn đồng bộ qua EventBus (dọn cả GameObject + spatial registry).
        GameManager.Instance.EventBus.Publish(new DespawnRequestPublish(entityId));
    }
}
