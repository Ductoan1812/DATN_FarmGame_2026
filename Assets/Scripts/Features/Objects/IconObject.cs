using UnityEngine;

[DisallowMultipleComponent]
public class IconObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private EntityRoot _root;

    private void Awake()
    {
        _root = GetComponent<EntityRoot>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Subscribe<WorldReadyPublish>(OnWorldReady);

        // Nếu entity đã có sẵn (spawn sau WorldReady)
        TrySetIcon();
    }

    private void OnDisable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Unsubscribe<WorldReadyPublish>(OnWorldReady);

        if (spriteRenderer != null) spriteRenderer.sprite = null;
    }

    private void OnWorldReady(WorldReadyPublish _)
    {
        TrySetIcon();
    }

    private void TrySetIcon()
    {
        var entity = _root?.GetEntity();
        if (entity != null && entity.entityData != null && spriteRenderer != null)
            spriteRenderer.sprite = entity.entityData.icon;
    }
}
