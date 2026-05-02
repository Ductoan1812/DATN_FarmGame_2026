using UnityEngine;

/// <summary>
/// Tool Hoe: cuốc đất → đổi tile thành plowedTile.
/// Quét: 1 cell trước mặt.
/// </summary>
public class HoeRuntime : ToolRuntime
{
    public HoeRuntime(ToolModule data) : base(data) { }

    protected override bool Execute(GameObject actorGO, PrimaryActionEvent e)
    {
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        var ws = GameManager.Instance?.WorldService;
        if (ws == null) return false;

        var entityAtCell = EntityScanSystem.GetAtCell(cell2d);
        if (entityAtCell != null)
        {
            Debug.Log("[HoeRuntime] Có entity tại ô này, không thể cuốc.");
            return false;
        }

        if (!ws.IsTillable(cell2d))
        {
            Debug.Log("[HoeRuntime] Ô không thể cuốc.");
            return false;
        }

        var plowedTile = GameManager.Instance.TileData?.plowedTile;
        if (plowedTile == null)
        {
            Debug.LogWarning("[HoeRuntime] plowedTile chưa gán trong TileData!");
            return false;
        }

        ws.SetGround(cell2d, plowedTile);
        Debug.Log($"[HoeRuntime] Cuốc đất tại {targetCell}");
        return true;
    }
}
