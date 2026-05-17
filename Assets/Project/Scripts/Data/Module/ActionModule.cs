/// <summary>
/// Module trung gian: nhận PrimaryActionEvent từ input → kiểm tra item đang cầm
/// → forward PrimaryActionEvent(actor, item) sang item.
/// Tay không → forward PrimaryActionEvent(actor, actor) lên chính entity.
///
/// Gắn cho mọi entity có khả năng "hành động" (Player, NPC, Enemy).
/// Entity cần có InventoryModule (Hotbar) để biết item đang cầm.
/// </summary>
[System.Serializable]
public class ActionModule : IModuleData
{
    public override IModuleRuntime CreateRuntime()
    {
        return new ActionRuntime(this);
    }
}

/// <summary>
/// Module portal chuyển scene qua interaction option.
/// Gắn vào EntityData của cổng/cửa/NPC vận chuyển.
/// </summary>
[System.Serializable]
public class ScenePortalModule : IModuleData
{
    public string optionTextKey = "ui.scene.enter";
    public int priority = 40;
    public string targetSceneName;
    public string targetSpawnPointId;
    public bool saveBeforeTransition = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new ScenePortalRuntime(this);
    }
}
