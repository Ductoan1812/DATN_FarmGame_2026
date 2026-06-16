using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class AutoPortalTrigger2D : MonoBehaviour
{
    [SerializeField] private bool hideRenderersOnPlay = true;
    [SerializeField, Min(0.05f)] private float cooldownSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float exitRecoveryTimeoutSeconds = 1.25f;

    private EntityRoot portalRoot;
    private float nextAllowedTriggerTime;
    private bool requirePlayerExitBeforeNextTrigger;
    private float exitRequirementArmedAtRealtime;
    private Collider2D triggerCollider;
    private PlayerControler exitLockedPlayer;

    private void Awake()
    {
        portalRoot = GetComponentInParent<EntityRoot>();

        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        if (!hideRenderersOnPlay)
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
        if (Time.time < nextAllowedTriggerTime)
            return;

        var player = other.GetComponentInParent<PlayerControler>();
        if (player == null)
            return;

        if (requirePlayerExitBeforeNextTrigger)
            return;

        var portal = ResolvePortalModule();
        if (portal == null || string.IsNullOrWhiteSpace(portal.targetSceneName))
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
            portal.targetSceneName,
            portal.targetSpawnPointId,
            portal.saveBeforeTransition);

        if (!requested)
        {
            nextAllowedTriggerTime = Time.time + 0.1f;
            ClearExitRequirement();
        }
    }

    private ScenePortalModule ResolvePortalModule()
    {
        portalRoot ??= GetComponentInParent<EntityRoot>();
        var entity = portalRoot != null ? portalRoot.GetEntity() : null;
        var modules = entity?.entityData?.modules;
        if (modules == null)
            return null;

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] is ScenePortalModule portal)
                return portal;
        }

        return null;
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
