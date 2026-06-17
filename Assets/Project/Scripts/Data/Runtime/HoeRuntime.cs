using UnityEngine;

/// <summary>
/// Tool Hoe: cuốc đất → đổi tile thành plowedTile.
/// Validate: kiểm tra ô trước mặt có cuốc được không.
/// Execute: đổi tile (gọi tại frame "Hit" của animation).
/// </summary>
public class HoeRuntime : ToolRuntime
{
    public HoeRuntime(ToolModule data) : base(data) { }

    protected override bool Validate(GameObject actorGO, PrimaryActionEvent e)
    {
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        var ws = GameManager.Instance?.WorldService;
        if (ws == null) return false;

        // Kiểm tra qua SpatialRegistry — chỉ block nếu có entity chiếm cell (cây, đá...)
        if (ws.HasBlockerAt(cell2d, EntityLayer.Ground)
            || ws.HasBlockerAt(cell2d, EntityLayer.Plant)
            || ws.HasBlockerAt(cell2d, EntityLayer.Furniture))
        {
            Debug.Log("[HoeRuntime] Có entity chiếm ô này, không thể cuốc.");
            return false;
        }

        if (!ws.IsTillable(cell2d))
        {
            Debug.Log("[HoeRuntime] Ô không thể cuốc.");
            return false;
        }

        if (GameManager.Instance.TileData?.plowedTile == null)
        {
            Debug.LogWarning("[HoeRuntime] plowedTile chưa gán trong TileData!");
            return false;
        }

        return true;
    }

    protected override void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item)
    {
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        var ws = GameManager.Instance?.WorldService;
        var plowedTile = GameManager.Instance.TileData?.plowedTile;

        ws.SetGround(cell2d, plowedTile);

        Debug.Log($"[HoeRuntime] Cuốc đất tại {targetCell}");
    }
}
