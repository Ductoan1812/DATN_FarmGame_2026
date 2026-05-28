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
    private EntityRoot _worldRoot;
    private float _nextTriggerTime;
    private float _enabledTime;
    private bool _pickupInProgress;

    private void OnEnable()
    {
        _enabledTime = Time.time;
        _worldRoot = GetComponent<EntityRoot>();
    }

    private void OnDisable()
    {
        _entity = null;
        _pickupInProgress = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_pickupInProgress) return;
        if (Time.time < _nextTriggerTime) return;
        if (Time.time < _enabledTime + pickupDelay) return;
        if (!other.CompareTag(targetTag)) return;

        // Lazy-cache entity (thay cho polling trong Update)
        if (_entity == null)
        {
            _entity = GetComponent<EntityRoot>()?.GetEntity();
            if (_entity == null) return;
        }

        if (!IsStillWorldOwned())
        {
            BeginCollectAndDespawn(other.transform, _entity.id);
            return;
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

        if (IsStillWorldOwned())
            return;

        BeginCollectAndDespawn(other.transform, entityId);
    }

    private bool IsStillWorldOwned()
    {
        return _entity != null
               && !_entity.IsEmpty
               && _worldRoot != null
               && ReferenceEquals(_entity.Owner, _worldRoot);
    }

    private void BeginCollectAndDespawn(Transform target, string entityId)
    {
        _pickupInProgress = true;

        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        void DespawnNow()
        {
            if (GameManager.Instance?.EventBus == null) return;
            GameManager.Instance.EventBus.Publish(new DespawnRequestPublish(entityId));
        }

        var motion = GetComponent<DropMotionObject>();
        if (motion != null && motion.isActiveAndEnabled)
        {
            motion.OnPickedUp(target, DespawnNow);
            return;
        }

        DespawnNow();
    }
}
