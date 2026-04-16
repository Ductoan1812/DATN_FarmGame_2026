using UnityEngine;

/// <summary>
/// Runtime của PlacementModule.
/// Lắng nghe UseEvent → tính targetCell phía trước owner → publish SpawnRequest.
/// Owner của entity sẽ tự chuyển sang EntityRoot mới khi SpawnSystem gọi root.Add().
/// </summary>
public class PlacementRuntime : IModuleRuntime, IHandleEvent<UseEvent>
{
    private readonly PlacementModule _data;

    public PlacementRuntime(PlacementModule data)
    {
        _data = data;
    }

    public void Handle(UseEvent e)
    {
        // ── Lấy Owner GameObject ──────────────────────────────────────────────
        var ownerGO = e.entity?.Owner?.GameObject;
        if (ownerGO == null)
        {
            Debug.LogWarning("[PlacementRuntime] Owner.GameObject null.");
            return;
        }

        // ── Tính targetCell phía trước owner ──────────────────────────────────
        var targetCell = GridSystem.GetCellInFrontOf(ownerGO);
        var worldPos   = new Vector2(targetCell.x, targetCell.y);

        // ── Clone 1 unit từ entity gốc (trừ amount, giữ nguyên entity gốc trong hotbar) ──
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[PlacementRuntime] GameManager.Instance null.");
            return;
        }

        if(_data.centerTile) worldPos += new Vector2(0.5f, 0.5f);
        // splitOnSpawn = true → SpawnSystem sẽ Split sau khi validate thành công
        gm.EventBus.Publish(new SpawnRequest(worldPos, _data.objectTypeToSpawn, e.entity, splitOnSpawn: true));
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Placement", dataJson = "" };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => other is PlacementRuntime;
}
