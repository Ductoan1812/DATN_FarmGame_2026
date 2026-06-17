using UnityEngine;

public enum ScenePortalPointMode
{
    Exit = 0,
    Entry = 1
}

[DisallowMultipleComponent]
public class ScenePortalTrigger2D : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private ScenePortalPointMode mode = ScenePortalPointMode.Exit;
    [SerializeField] private string _spawnPointId;

    [Header("Exit Settings")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;
    [SerializeField] private bool saveBeforeTransition = true;

    [Header("Trigger")]
    [SerializeField] private bool hideRenderersOnPlay = false;
    [SerializeField, Min(0.05f)] private float cooldownSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float exitRecoveryTimeoutSeconds = 1.25f;

    private float nextAllowedTriggerTime;
    private bool requirePlayerExitBeforeNextTrigger;
    private float exitRequirementArmedAtRealtime;
    private Collider2D triggerCollider;
    private PlayerControler exitLockedPlayer;

    public ScenePortalPointMode Mode
    {
        get => mode;
        set => mode = value;
    }

    public bool IsEntryPoint => mode == ScenePortalPointMode.Entry;
    public string SpawnPointId => string.IsNullOrWhiteSpace(_spawnPointId) ? string.Empty : _spawnPointId.Trim();

    // Backward-compatible member name for existing editor/setup scripts.
    public string spawnPointId
    {
        get => _spawnPointId;
        set => _spawnPointId = value;
    }

    protected virtual ScenePortalPointMode? ForcedMode => null;

    private void Reset()
    {
        ApplyForcedMode();
        EnsureExitTriggerState();
    }

    private void OnValidate()
    {
        ApplyForcedMode();
        EnsureExitTriggerState();
    }

    private void Awake()
    {
        ApplyForcedMode();
        EnsureExitTriggerState();
        triggerCollider = GetComponent<Collider2D>();

        if (!hideRenderersOnPlay || mode != ScenePortalPointMode.Exit)
            return;

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RefreshExitRequirementState();
        TryTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        RefreshExitRequirementState();
        TryTrigger(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerControler>();
        if (player != null)
            ReleaseExitRequirementIfSeparated(player);
    }

    private void Update()
    {
        RefreshExitRequirementState();
    }

    private void TryTrigger(Collider2D other)
    {
        if (mode != ScenePortalPointMode.Exit)
            return;

        if (Time.time < nextAllowedTriggerTime)
            return;

        var player = other.GetComponentInParent<PlayerControler>();
        if (player == null)
            return;

        if (requirePlayerExitBeforeNextTrigger)
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
            return;

        if (SceneTransitionService.ArePortalTriggersSuppressed())
        {
            ArmExitRequirement(player);
            return;
        }

        var interactor = player.GetComponentInParent<EntityRoot>()?.GetEntity();
        nextAllowedTriggerTime = Time.time + cooldownSeconds;
        ArmExitRequirement(player);

        bool requested = SceneTransitionService.RequestTransition(
            interactor,
            targetSceneName,
            targetSpawnPointId,
            saveBeforeTransition);

        if (!requested)
        {
            nextAllowedTriggerTime = Time.time + 0.1f;
            ClearExitRequirement();
        }
    }

    private void ApplyForcedMode()
    {
        if (ForcedMode.HasValue)
            mode = ForcedMode.Value;
    }

    private void EnsureExitTriggerState()
    {
        if (mode != ScenePortalPointMode.Exit)
            return;

        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void ArmExitRequirement(PlayerControler player)
    {
        requirePlayerExitBeforeNextTrigger = true;
        exitLockedPlayer = player;
        exitRequirementArmedAtRealtime = Time.realtimeSinceStartup;
    }

    private void ClearExitRequirement()
    {
        requirePlayerExitBeforeNextTrigger = false;
        exitLockedPlayer = null;
        exitRequirementArmedAtRealtime = 0f;
    }

    private void RefreshExitRequirementState()
    {
        if (!requirePlayerExitBeforeNextTrigger)
            return;

        if (triggerCollider == null || !triggerCollider.enabled || !triggerCollider.gameObject.activeInHierarchy)
        {
            ClearExitRequirement();
            return;
        }

        if (exitLockedPlayer == null || !exitLockedPlayer.gameObject.activeInHierarchy)
        {
            if (Time.realtimeSinceStartup >= exitRequirementArmedAtRealtime + exitRecoveryTimeoutSeconds)
                ClearExitRequirement();
            return;
        }

        ReleaseExitRequirementIfSeparated(exitLockedPlayer);
    }

    private void ReleaseExitRequirementIfSeparated(PlayerControler player)
    {
        if (player == null || IsPlayerOverlappingTrigger(player))
            return;

        ClearExitRequirement();
    }

    private bool IsPlayerOverlappingTrigger(PlayerControler player)
    {
        if (player == null || triggerCollider == null)
            return false;

        var playerColliders = player.GetComponentsInChildren<Collider2D>(false);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            var playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy)
                continue;

            if (triggerCollider.Distance(playerCollider).isOverlapped)
                return true;
        }

        return false;
    }
}

[AddComponentMenu("")]
public class SceneSpawnPoint : ScenePortalTrigger2D
{
    protected override ScenePortalPointMode? ForcedMode => ScenePortalPointMode.Entry;
}
