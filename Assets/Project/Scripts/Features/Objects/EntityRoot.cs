using UnityEngine;
using System;

<<<<<<< HEAD:Assets/Project/Scripts/Features/Objects/EntityRoot.cs
=======
/// <summary>
/// Pure container — chỉ giữ reference tới EntityRuntime và các service cần thiết.
/// KHÔNG chứa logic gameplay. Nhiệm vụ duy nhất:
///   1. Get/Set entity + refs.
///   2. Fire SpawnedEvent qua entity.
///   3. Bật GameObject (SetActive true) SAU KHI event đã fire xong.
///
/// Prefab phải được lưu ở trạng thái Inactive để các Module khác
/// không Awake/OnEnable trước khi refs được inject.
/// </summary>
>>>>>>> 888ec29c68b2b70711a676afb7310e7d9ce9d36f:Assets/Scripts/Features/Objects/EntityRoot.cs
[DisallowMultipleComponent]
public class EntityRoot : MonoBehaviour, IEntityContainer
{
    GameObject IEntityContainer.GameObject => this.gameObject;

    // ── Refs (được SpawnSystem inject) ─────────────────────
    public EntityService entityService;
    public event Action<EntityRuntime> OnEntityReady;

    public EntityRuntime GetEntity() => entity;

    // ── State ──────────────────────────────────────────────
    private EntityRuntime entity;
    public bool IsReady { get; private set; }

<<<<<<< HEAD:Assets/Project/Scripts/Features/Objects/EntityRoot.cs
=======
    // ── Getter ─────────────────────────────────────────────
    public EntityRuntime GetEntity() => entity;

    /// <summary>
    /// Gán entity vào root. Quy ước: GameObject đang Inactive khi gọi Add.
    /// Thứ tự: set entity → fire SpawnedEvent → SetActive(true).
    /// Sau bước này, Awake/OnEnable/Start của các Module khác mới chạy
    /// và chắc chắn đã có đủ entity + refs.
    /// </summary>
>>>>>>> 888ec29c68b2b70711a676afb7310e7d9ce9d36f:Assets/Scripts/Features/Objects/EntityRoot.cs
    public bool Add(EntityRuntime entity)
    {
        if (entity == null)
        {
            Debug.LogWarning($"[EntityRoot] Add called with null entity on '{name}'.");
            return false;
        }

        // 1. Set refs
        this.entity = entity;
        entity.Owner = this;

        // 2. Fire SpawnedEvent TRƯỚC khi GameObject active
        entity.TriggerEvent(new SpawnedEvent(entity));
<<<<<<< HEAD:Assets/Project/Scripts/Features/Objects/EntityRoot.cs
        OnEntityReady?.Invoke(entity);
=======
        IsReady = true;

        // 3. Bây giờ mới cho phép các Module chạy
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

>>>>>>> 888ec29c68b2b70711a676afb7310e7d9ce9d36f:Assets/Scripts/Features/Objects/EntityRoot.cs
        return true;
    }

    public bool Remove(EntityRuntime entity, int amount = 1)
    {
        if (this.entity != entity) return false;
        this.entity = null;
        IsReady = false;
        return true;
    }
}
