using UnityEngine;

/// <summary>
/// Fertilizer tool:
/// - target cell in front of actor
/// - increments soil quality
/// - consumes one fertilizer item
/// </summary>
public class FertilizerRuntime : ToolRuntime
{
    public FertilizerRuntime(ToolModule data) : base(data) { }

    protected override bool Validate(GameObject actorGO, PrimaryActionEvent e)
    {
        var gm = GameManager.Instance;
        if (gm == null) return false;

        var worldService = gm.WorldService;
        var soil = gm.SoilQualityTracker;
        if (worldService == null || soil == null) return false;

        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        bool hasPlant = worldService.HasBlockerAt(cell2d, EntityLayer.Plant);
        bool hasTilledGround = worldService.GetGround(cell2d) == gm.TileData?.plowedTile;
        if (!hasPlant && !hasTilledGround)
        {
            Debug.Log("[FertilizerRuntime] Ô này chưa cuốc hoặc chưa có cây.");
            return false;
        }

        return true;
    }

    protected override void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        int quality = gm.SoilQualityTracker?.IncreaseQuality(cell2d, 1) ?? 0;
        if (gm.InventoryService != null && item != null && actor != null)
            gm.InventoryService.Consume(item, actor, 1);

        Debug.Log($"[FertilizerRuntime] Bón phân tại {cell2d}. SoilQuality={quality}");
    }
}
