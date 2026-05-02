using UnityEngine;

/// <summary>
/// Runtime của PlacementModule.
/// Lắng nghe PrimaryActionEvent → dùng actor lấy vị trí → spawn entity xuống world.
/// </summary>
public class PlacementRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>
{
    private readonly PlacementModule _data;

    public PlacementRuntime(PlacementModule data)
    {
        _data = data;
    }

    public void Handle(PrimaryActionEvent e)
    {
        if (e.actor == null) return;

        // ── Actor GameObject ──────────────────────────────────────────────────
        var actorGO = e.actor.Owner?.GameObject;
        if (actorGO == null)
        {
            Debug.LogWarning("[PlacementRuntime] actor.Owner.GameObject null.");
            return;
        }

        // ── Tính targetCell phía trước actor ──────────────────────────────────
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var worldPos   = new Vector2(targetCell.x, targetCell.y);

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[PlacementRuntime] GameManager.Instance null.");
            return;
        }

        if (_data.centerTile) worldPos += new Vector2(0.5f, 0.5f);

        // e.item = entity hạt giống đang cầm → split 1 unit khi spawn
        gm.EventBus.Publish(new SpawnRequestPublish(worldPos, _data.objectTypeToSpawn, e.item, splitOnSpawn: true));
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() => null;

    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is PlacementRuntime;
}
