using UnityEngine;

[RequireComponent(typeof(PlayerControler))]
public class PlayerInvincibilityHandler : MonoBehaviour
{
    [SerializeField] private float postHitInvincibleSeconds = 0.65f;
    [SerializeField] private float blinkInterval = 0.08f;

    private EntityRoot root;
    private EntityRuntime entity;
    private HealthRuntime health;
    private SpriteRenderer[] renderers;
    private EventBus subscribedBus;
    private float invincibleUntilRealtime;
    private float nextBlinkRealtime;
    private bool blinkVisible = true;

    private void Awake()
    {
        root = GetComponent<EntityRoot>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnEnable()
    {
        if (root != null)
            root.OnEntityReady += OnEntityReady;
        Bind(root != null ? root.GetEntity() : null);
        Subscribe();
    }

    private void Start()
    {
        Bind(root != null ? root.GetEntity() : null);
        Subscribe();
    }

    private void OnDisable()
    {
        if (root != null)
            root.OnEntityReady -= OnEntityReady;
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<DamageAppliedPublish>(OnDamageApplied);
            subscribedBus = null;
        }
        SetVisible(true);
        if (health != null)
            health.CanTakeDamage = true;
    }

    private void Update()
    {
        bool invincible = Time.realtimeSinceStartup < invincibleUntilRealtime;
        if (health != null)
            health.CanTakeDamage = !invincible;

        UpdateBlink(invincible);
    }

    private void OnEntityReady(EntityRuntime runtime)
    {
        Bind(runtime);
    }

    private void Bind(EntityRuntime runtime)
    {
        entity = runtime;
        health = entity?.GetModule<HealthRuntime>();
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<DamageAppliedPublish>(OnDamageApplied);
        subscribedBus = bus;
    }

    private void OnDamageApplied(DamageAppliedPublish evt)
    {
        if (!ReferenceEquals(evt.target, entity))
            return;

        invincibleUntilRealtime = Mathf.Max(invincibleUntilRealtime, Time.realtimeSinceStartup + postHitInvincibleSeconds);
    }

    private void UpdateBlink(bool invincible)
    {
        if (!invincible)
        {
            if (!blinkVisible)
                SetVisible(true);
            blinkVisible = true;
            return;
        }

        if (Time.realtimeSinceStartup < nextBlinkRealtime)
            return;

        blinkVisible = !blinkVisible;
        SetVisible(blinkVisible);
        nextBlinkRealtime = Time.realtimeSinceStartup + blinkInterval;
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }
}
