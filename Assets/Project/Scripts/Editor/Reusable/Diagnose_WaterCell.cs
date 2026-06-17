using UnityEngine;

/// <summary>
/// Debug: Tưới cây và kiểm tra cell nào được tưới vs cell của cây
/// </summary>
public class Diagnose_WaterCell
{
    public static void Execute()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[DIAG] GameManager null"); return; }

        var tracker = gm.WateredTileTracker;
        var roots = Object.FindObjectsOfType<EntityRoot>();

        Debug.Log("[DIAG] === WATER CELL DIAGNOSIS ===");

        // Find first plant
        EntityRoot plantRoot = null;
        StageRuntime plantStage = null;
        foreach (var root in roots)
        {
            var entity = root.GetEntity();
            if (entity == null) continue;
            var stage = entity.GetModule<StageRuntime>();
            if (stage != null) { plantRoot = root; plantStage = stage; break; }
        }

        if (plantRoot == null) { Debug.LogWarning("[DIAG] No plant found"); return; }

        var plantWorldPos = plantRoot.gameObject.transform.position;
        var plantCell3 = GridSystem.WorldToCell(plantWorldPos);
        var plantCell = new Vector2Int(plantCell3.x, plantCell3.y);

        Debug.Log($"[DIAG] Plant worldPos={plantWorldPos} → GridSystem cell={plantCell}");

        // Water the EXACT cell that GridSystem says the plant is on
        tracker.SetWatered(plantCell);
        Debug.Log($"[DIAG] Watered cell {plantCell} → IsWatered={tracker.IsWatered(plantCell)}");

        // Now trigger NextDay
        int stageBefore = plantStage.currentStageIndex;
        plantRoot.GetEntity().TriggerEvent(new NextDayEvent());
        int stageAfter = plantStage.currentStageIndex;

        if (stageAfter > stageBefore)
            Debug.Log($"[DIAG] ✅ Plant GREW: stage {stageBefore} → {stageAfter}");
        else
            Debug.LogWarning($"[DIAG] ⚠️ Plant did NOT grow: stage still {stageAfter} (may need more days in current stage)");

        Debug.Log($"[DIAG] daysWithoutWater={plantStage.DaysWithoutWater}, isWilting={plantStage.IsWilting}");
        Debug.Log("[DIAG] === DONE ===");
    }
}
