using UnityEngine;

[DefaultExecutionOrder(-90)]
public class InteractionPreviewSystem : MonoBehaviour
{
    [SerializeField] private float scanRange = 1.35f;
    [SerializeField] private float scanInterval = 0.08f;
    [SerializeField] private bool autoCreateWorldHint = true;

    private EventBus eventBus;
    private EntityRuntime playerEntity;
    private TargetHighlightBridge highlightedBridge;
    private float nextScanTime;
    private string lastSignature;

    private void OnEnable()
    {
        TryBind();
        EnsureWorldHint();
    }

    private void Update()
    {
        if (!TryBind()) return;
        if (playerEntity == null) TryResolvePlayerEntity();
        if (playerEntity == null) return;

        if (Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + Mathf.Max(0.02f, scanInterval);
        ScanAndPublish();
    }

    private bool TryBind()
    {
        if (eventBus != null) return true;
        eventBus = GameManager.Instance?.EventBus;
        return eventBus != null;
    }

    private void TryResolvePlayerEntity()
    {
        var player = FindAnyObjectByType<PlayerControler>();
        if (player == null)
        {
            playerEntity = null;
            return;
        }

        var root = player.GetComponent<EntityRoot>();
        playerEntity = root?.GetEntity();
    }

    private void ScanAndPublish()
    {
        if (!TryGetOwnerGameObject(playerEntity, out var actorGo))
            return;

        var target = EntityScanSystem.GetClosest(actorGo, scanRange);
        var preview = InteractionPreviewService.Build(playerEntity, target);
        string signature = BuildSignature(preview);
        if (signature == lastSignature)
            return;

        lastSignature = signature;
        ApplyHighlight(preview.target);
        eventBus.Publish(new InteractionPreviewChangedPublish(preview));
    }

    private void ApplyHighlight(EntityRuntime target)
    {
        if (highlightedBridge != null)
        {
            highlightedBridge.SetHighlighted(false);
            highlightedBridge = null;
        }

        if (!TryGetOwnerGameObject(target, out var targetGo))
            return;

        var bridge = targetGo.GetComponent<TargetHighlightBridge>();
        if (bridge == null)
            bridge = targetGo.AddComponent<TargetHighlightBridge>();

        bridge.SetHighlighted(true);
        highlightedBridge = bridge;
    }

    private static bool TryGetOwnerGameObject(EntityRuntime entity, out GameObject ownerGo)
    {
        ownerGo = null;
        if (entity?.Owner == null)
            return false;

        try
        {
            ownerGo = entity.Owner.GameObject;
            return ownerGo != null;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static string BuildSignature(InteractionPreviewData preview)
    {
        string targetId = preview.target?.id ?? "none";
        string action = preview.actionTextKey ?? string.Empty;
        string blocked = preview.blockedReasonKey ?? string.Empty;
        string status = preview.statusTextKey ?? string.Empty;
        string required = preview.requiredTool.ToString();
        return $"{targetId}|{action}|{blocked}|{status}|{required}|{preview.isBlocked}";
    }

    private void EnsureWorldHint()
    {
        if (!autoCreateWorldHint) return;
        if (FindAnyObjectByType<WorldInteractionHintUI>() != null) return;

        var go = new GameObject("WorldInteractionHintUI");
        go.transform.SetParent(transform, false);
        go.AddComponent<WorldInteractionHintUI>();
    }
}
