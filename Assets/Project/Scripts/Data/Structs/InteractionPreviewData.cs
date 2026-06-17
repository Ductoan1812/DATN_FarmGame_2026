using UnityEngine;

public struct InteractionPreviewData
{
    public readonly EntityRuntime interactor;
    public readonly EntityRuntime target;
    public readonly string targetNameKey;
    public readonly string targetNameFallback;
    public readonly string actionTextKey;
    public readonly string statusTextKey;
    public readonly string blockedReasonKey;
    public readonly ToolType requiredTool;
    public readonly bool isBlocked;
    public readonly int priority;
    public readonly Sprite icon;

    public bool HasTarget => target != null;

    public InteractionPreviewData(
        EntityRuntime interactor,
        EntityRuntime target,
        string targetNameKey,
        string targetNameFallback,
        string actionTextKey,
        string statusTextKey,
        string blockedReasonKey,
        ToolType requiredTool,
        bool isBlocked,
        int priority,
        Sprite icon)
    {
        this.interactor = interactor;
        this.target = target;
        this.targetNameKey = targetNameKey;
        this.targetNameFallback = targetNameFallback;
        this.actionTextKey = actionTextKey;
        this.statusTextKey = statusTextKey;
        this.blockedReasonKey = blockedReasonKey;
        this.requiredTool = requiredTool;
        this.isBlocked = isBlocked;
        this.priority = priority;
        this.icon = icon;
    }

    public static InteractionPreviewData Empty(EntityRuntime interactor)
    {
        return new InteractionPreviewData(
            interactor,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            ToolType.None,
            false,
            int.MaxValue,
            null);
    }
}
