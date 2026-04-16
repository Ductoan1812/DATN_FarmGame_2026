using UnityEngine;

/// <summary>
/// Tool Hoe: cuốc đất → đổi tile thành plowedTile.
/// Logic chung (cooldown, targetCell) đã ở ToolRuntime.
/// </summary>
public class HoeRuntime : ToolRuntime
{
    public HoeRuntime(ToolModule data) : base(data) { }

    protected override bool Execute(GameObject ownerGO, Vector3Int targetCell, Vector2Int cell2d)
    {
        var ws = GameManager.Instance?.WorldService;
        if (ws == null) return false;

        // Bị block bởi cây
        if (ws.HasBlockerAt(cell2d, EntityLayer.Plant))
        {
            Debug.Log("[HoeRuntime] Có cây tại ô này, không thể cuốc.");
            return false;
        }

        // Tile không thể cuốc (đã plowed/watered hoặc không tồn tại)
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
