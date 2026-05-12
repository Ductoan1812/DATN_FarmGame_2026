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

        // Forward SecondaryActionEvent sang target
        target.TriggerEvent(new SecondaryActionEvent(actor));
        Debug.Log($"[ActionRuntime] Interact: actor='{actor.entityData?.keyName}' → target='{target.entityData?.keyName}'");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is ActionRuntime;
}
