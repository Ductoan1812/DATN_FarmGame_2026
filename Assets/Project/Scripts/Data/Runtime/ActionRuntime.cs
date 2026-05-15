using UnityEngine;

/// <summary>
/// Module trung gian xử lý cả PrimaryAction (chuột trái) và SecondaryAction (chuột phải).
///
/// Primary: tìm item đang cầm → forward PrimaryActionEvent sang item.
/// Secondary: dùng EntityScanSystem.GetClosest() tìm target có IInteractable
///            → forward SecondaryActionEvent sang target.
/// </summary>
public class ActionRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>, IHandleEvent<SecondaryActionEvent>
{
    private readonly ActionModule _data;

    public ActionRuntime(ActionModule data)
    {
        _data = data;
    }

    // ── Primary (chuột trái) ──────────────────────────────────────────────────

    public void Handle(PrimaryActionEvent e)
    {
        // Lần 2 (đã forward) → bỏ qua
        if (e.item != null) return;

        var actor = e.actor;
        if (actor == null) return;

        var hotbar = actor.GetModules<InventoryRuntime>()
                         .Find(i => i.Type == InventoryType.Hotbar);
        var selectedItem = hotbar?.SelectedEntity;

        if (selectedItem != null)
        {
            selectedItem.TriggerEvent(new PrimaryActionEvent(actor, selectedItem));
            Debug.Log($"[ActionRuntime] Forward: actor='{actor.entityData?.keyName}' → item='{selectedItem.entityData?.keyName}'");
        }
        else
        {
            actor.TriggerEvent(new PrimaryActionEvent(actor, actor));
        }
    }

    // ── Secondary (chuột phải) ────────────────────────────────────────────────

    public void Handle(SecondaryActionEvent e)
    {
        var actor = e.initiator;
        if (actor == null) return;

        var actorGO = actor.Owner?.GameObject;
        if (actorGO == null) return;

        // Tìm entity gần nhất có IInteractable
        var target = EntityScanSystem.GetClosest(actorGO, 1f);
        if (target == null)
        {
            Debug.Log("[ActionRuntime] Không có target tương tác phía trước.");
            return;
        }

        var context = new InteractionContext(actor, target);

        // Forward SecondaryActionEvent sang target
        target.TriggerEvent(new SecondaryActionEvent(actor, target, context));

        if (context.HasOptions)
        {
            var eventBus = GameManager.Instance?.EventBus;
            if (eventBus == null)
            {
                Debug.LogWarning("[ActionRuntime] Có interaction options nhưng GameManager.EventBus đang null nên UI không nhận được event.");
                return;
            }

            eventBus.Publish(new InteractionOptionsReadyPublish(actor, target, context.GetOptions()));
        }
        else
        {
            Debug.LogWarning(
                $"[ActionRuntime] Target '{target.entityData?.keyName}' tương tác được nhưng không có option nào. " +
                "Hãy kiểm tra EntityData có DialogueModule/QuestModule/ShopModule và module đã gán data hợp lệ chưa.");
        }

        Debug.Log($"[ActionRuntime] Interact: actor='{actor.entityData?.keyName}' → target='{target.entityData?.keyName}'");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is ActionRuntime;
}

/// <summary>
/// Runtime cho ScenePortalModule.
/// Đưa ra option chuyển scene khi player tương tác target.
/// </summary>
public class ScenePortalRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>
{
    private readonly ScenePortalModule data;
    private EntityRuntime owner;

    public ScenePortalRuntime(ScenePortalModule data)
    {
        this.data = data;
    }

    public void Handle(SecondaryActionEvent e)
    {
        owner ??= e.target;
        if (e.context == null || owner == null) return;
        if (string.IsNullOrWhiteSpace(data.targetSceneName)) return;

        string optionId = $"scene.portal.{owner.id}";
        string textKey = string.IsNullOrWhiteSpace(data.optionTextKey) ? "ui.scene.enter" : data.optionTextKey;
        int priority = Mathf.Max(0, data.priority);

        e.context.AddOption(
            optionId,
            textKey,
            priority,
            () => SceneTransitionService.RequestTransition(
                e.initiator,
                data.targetSceneName,
                data.targetSpawnPointId,
                data.saveBeforeTransition));
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is ScenePortalRuntime;
}
