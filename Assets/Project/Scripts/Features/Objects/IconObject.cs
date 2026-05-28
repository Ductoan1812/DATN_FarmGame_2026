using UnityEngine;

[DisallowMultipleComponent]
public class IconObject : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer shadowRenderer;

    [Header("Sizing")]
    [SerializeField] private BoxCollider2D pickupCollider;
    [SerializeField] private Vector2 maxWorldSize = new(0.9f, 0.9f);
    [SerializeField] private Vector2 colliderSizeMultiplier = new(0.72f, 0.42f);
    [SerializeField] private Vector2 colliderMinSize = new(0.35f, 0.25f);
    [SerializeField] private float colliderYOffset = -0.02f;

    [Header("Shadow")]
    [SerializeField] private Transform shadowRoot;
    [SerializeField] private Vector2 shadowWorldSizeMultiplier = new(0.85f, 0.32f);
    [SerializeField] private Vector3 shadowLocalOffset = new(0f, -0.22f, 0f);

    private EntityRuntime _entityRuntime;
    private EntityRoot _root;
    private DropMotionObject _motion;
    private Vector3 _baseVisualLocalPosition;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visualRoot == null && spriteRenderer != null)
            visualRoot = spriteRenderer.transform;

        if (shadowRoot == null)
            shadowRoot = transform.Find("Shadow");

        if (shadowRenderer == null && shadowRoot != null)
            shadowRenderer = shadowRoot.GetComponent<SpriteRenderer>();

        if (pickupCollider == null)
            pickupCollider = GetComponent<BoxCollider2D>();

        _motion = GetComponent<DropMotionObject>();
        if (visualRoot != null)
            _baseVisualLocalPosition = visualRoot.localPosition;
    }

    private void OnEnable()
    {
        _root = GetComponent<EntityRoot>();
        if (_root != null)
        {
            _root.OnEntityReady += SetEntityRoot;
            if (_root.IsReady)
                SetEntityRoot(_root.GetEntity());
        }

        if (spriteRenderer != null)
            spriteRenderer.sprite = null;

        TrySetIcon();
    }

    private void OnDisable()
    {
        if (_root != null)
            _root.OnEntityReady -= SetEntityRoot;

        _entityRuntime = null;

        if (spriteRenderer != null)
            spriteRenderer.sprite = null;
    }

    private void SetEntityRoot(EntityRuntime runtime)
    {
        _entityRuntime = runtime;
        TrySetIcon();
    }

    private void TrySetIcon()
    {
        if (spriteRenderer == null || _entityRuntime?.entityData == null)
            return;

        var icon = _entityRuntime.entityData.icon;
        if (icon == null)
            return;

        spriteRenderer.sprite = icon;
        ApplyVisualSizing(icon);
    }

    private void ApplyVisualSizing(Sprite icon)
    {
        if (visualRoot == null || icon == null)
            return;

        var spriteSize = icon.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        float scale = Mathf.Min(
            maxWorldSize.x / spriteSize.x,
            maxWorldSize.y / spriteSize.y);

        if (!float.IsFinite(scale) || scale <= 0f)
            scale = 1f;

        var finalVisualSize = spriteSize * scale;

        visualRoot.localPosition = _baseVisualLocalPosition;
        visualRoot.localScale = Vector3.one * scale;

        if (pickupCollider != null)
        {
            pickupCollider.size = new Vector2(
                Mathf.Max(colliderMinSize.x, finalVisualSize.x * colliderSizeMultiplier.x),
                Mathf.Max(colliderMinSize.y, finalVisualSize.y * colliderSizeMultiplier.y));
            pickupCollider.offset = new Vector2(0f, finalVisualSize.y * colliderYOffset);
        }

        if (shadowRoot != null && shadowRenderer != null && shadowRenderer.sprite != null)
        {
            var shadowSpriteSize = shadowRenderer.sprite.bounds.size;
            if (shadowSpriteSize.x > 0f && shadowSpriteSize.y > 0f)
            {
                var targetShadowSize = new Vector2(
                    Mathf.Max(0.1f, finalVisualSize.x * shadowWorldSizeMultiplier.x),
                    Mathf.Max(0.05f, finalVisualSize.y * shadowWorldSizeMultiplier.y));

                shadowRoot.localScale = new Vector3(
                    targetShadowSize.x / shadowSpriteSize.x,
                    targetShadowSize.y / shadowSpriteSize.y,
                    1f);
            }

            shadowRoot.localPosition = shadowLocalOffset;
        }

        _motion?.RefreshVisualBases();
    }
}
