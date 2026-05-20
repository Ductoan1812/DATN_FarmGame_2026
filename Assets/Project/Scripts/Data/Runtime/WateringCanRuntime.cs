using UnityEngine;

/// <summary>
/// Tool WateringCan: tưới nước → đặt wateredTile lên tmWatered tilemap.
/// Validate: ô trước mặt phải là plowed (hoặc có plant) + chưa tưới hôm nay.
/// Execute: WateredTileTracker.SetWatered(cell) + trừ stamina.
/// </summary>
public class WateringCanRuntime : ToolRuntime
{
    public WateringCanRuntime(ToolModule data) : base(data) { }

    protected override bool Validate(GameObject actorGO, PrimaryActionEvent e)
    {
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        var gm = GameManager.Instance;
        if (gm == null) return false;

        // Debug: log actor stats
        if (e.actor?.stats != null)
        {
            float dbgStamina = e.actor.stats.Get(StatType.Stamina);
            float dbgMax = e.actor.stats.Get(StatType.MaxStamina);
            Debug.Log($"[WateringCanRuntime] Validate: actor={e.actor.entityData?.keyName}, Stamina={dbgStamina}/{dbgMax}, cell={cell2d}");
        }

        var tracker = gm.WateredTileTracker;
        if (tracker == null)
        {
            Debug.LogWarning("[WateringCanRuntime] WateredTileTracker chưa khởi tạo!");
            return false;
        }

        // Đã tưới rồi → không tưới lại
        if (tracker.IsWatered(cell2d))
        {
            Debug.Log("[WateringCanRuntime] Ô này đã tưới hôm nay.");
            return false;
        }

        // Check ô phải là plowed HOẶC có plant entity
        var ws = gm.WorldService;
        if (ws == null) return false;

        var tileData = gm.TileData;
        if (tileData?.plowedTile == null) return false;

        // Check ground tile = plowed?
        bool isPlowed = false;
        var groundTile = ws.GetGround(cell2d);
        if (groundTile == tileData.plowedTile)
            isPlowed = true;

        // Hoặc có plant entity tại ô?
        bool hasPlant = ws.HasBlockerAt(cell2d, EntityLayer.Plant);

        if (!isPlowed && !hasPlant)
        {
            Debug.Log("[WateringCanRuntime] Ô không phải đất cày và không có cây.");
            return false;
        }

        // Check stamina
        if (e.actor?.stats == null) return false;
        float maxStamina = e.actor.stats.Get(StatType.MaxStamina);
        if (maxStamina > 0f) // Chỉ check stamina nếu entity có MaxStamina
        {
            float stamina = e.actor.stats.Get(StatType.Stamina);
            if (stamina < 2f)
            {
                Debug.Log($"[WateringCanRuntime] Không đủ thể lực để tưới. Stamina={stamina}/{maxStamina}");
                return false;
            }
        }

        return true;
    }

    protected override void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item)
    {
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        var tracker = GameManager.Instance?.WateredTileTracker;
        if (tracker == null) return;

        // Tưới: đặt tile lên tmWatered
        tracker.SetWatered(cell2d);

        // Trừ stamina
        if (actor?.stats != null)
        {
            float current = actor.stats.Get(StatType.Stamina);
            actor.stats.Set(StatType.Stamina, Mathf.Max(0f, current - 2f));
        }

        Debug.Log($"[WateringCanRuntime] Tưới nước tại {cell2d}");
    }
}
