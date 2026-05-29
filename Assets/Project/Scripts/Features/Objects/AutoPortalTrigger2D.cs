using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class AutoPortalTrigger2D : MonoBehaviour
{
    [SerializeField] private bool hideRenderersOnPlay = true;
    [SerializeField, Min(0.05f)] private float cooldownSeconds = 0.5f;

    private EntityRoot portalRoot;
    private float nextAllowedTriggerTime;
    private bool requirePlayerExitBeforeNextTrigger;

    private void Awake()
    {
        portalRoot = GetComponentInParent<EntityRoot>();

        var trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

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
        TryTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerControler>() != null)
            requirePlayerExitBeforeNextTrigger = false;
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
            requirePlayerExitBeforeNextTrigger = true;
            return;
        }

        var interactor = player.GetComponentInParent<EntityRoot>()?.GetEntity();
        nextAllowedTriggerTime = Time.time + cooldownSeconds;
        requirePlayerExitBeforeNextTrigger = true;

        bool requested = SceneTransitionService.RequestTransition(
            interactor,
            portal.targetSceneName,
            portal.targetSpawnPointId,
            portal.saveBeforeTransition);

        if (!requested)
            nextAllowedTriggerTime = Time.time + 0.1f;
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
}
