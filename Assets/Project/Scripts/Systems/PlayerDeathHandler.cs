using UnityEngine;

/// <summary>
/// Applies the player-specific death consequence. Respawn itself stays owned by RespawnRuntime.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private Vector2 homeRespawnPosition = new(10f, 10f);
    [SerializeField, Range(0f, 1f)] private float staminaAfterDeathRatio = 0.25f;

    private EventBus eventBus;
    private HealthRuntime subscribedHealth;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (eventBus != null)
        {
            eventBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            eventBus = null;
        }
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.EventBus == null)
            return;

        if (eventBus != gm.EventBus)
        {
            if (eventBus != null)
                eventBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            eventBus = gm.EventBus;
            eventBus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        }

        BindCurrentPlayer();
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        BindCurrentPlayer();
    }

    private void BindCurrentPlayer()
    {
        var player = FindAnyObjectByType<PlayerControler>();
        var root = player != null ? player.GetComponent<EntityRoot>() : null;
        var entity = root != null ? root.GetEntity() : null;
        var health = entity?.GetModule<HealthRuntime>();
        if (health == null || health == subscribedHealth)
            return;

        Unsubscribe();
        subscribedHealth = health;
        subscribedHealth.OnDied += OnPlayerDied;
    }

    private void Unsubscribe()
    {
        if (subscribedHealth != null)
        {
            subscribedHealth.OnDied -= OnPlayerDied;
            subscribedHealth = null;
        }
    }

    private void OnPlayerDied(EntityRuntime player)
    {
        if (player == null)
            return;

        float maxStamina = player.stats.Get(StatType.MaxStamina);
        if (maxStamina > 0f)
            player.stats.Set(StatType.Stamina, Mathf.Max(0f, maxStamina * staminaAfterDeathRatio));

        var respawn = player.GetModule<RespawnRuntime>();
        if (respawn != null)
            respawn.CurrentRespawnPosition = homeRespawnPosition;

        eventBus?.Publish(new PlayerDeathPublish(player));
        Debug.Log($"[PlayerDeathHandler] Player death penalty applied. Stamina={player.stats.Get(StatType.Stamina):F0}/{maxStamina:F0}, respawn={homeRespawnPosition}.");
    }
}
