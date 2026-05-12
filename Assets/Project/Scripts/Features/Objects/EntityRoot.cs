using UnityEngine;
using System;

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

    // ── Backward-compatible direct init ───────────────────
    public void Init(EntityRuntime runtime)
    {
        entity = runtime;
        if (entity != null) entity.Owner = this;
    }

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
        IsReady = true;
        OnEntityReady?.Invoke(entity);

        // 3. Bây giờ mới cho phép các Module chạy
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

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
