using UnityEngine;

/// <summary>
/// Diagnose: Tại sao cây không grow sau khi tưới + NextDay?
/// </summary>
public class Diagnose_PlantGrowth
{
    public static void Execute()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[DIAG] GameManager null"); return; }

        Debug.Log("[DIAG] === PLANT GROWTH DIAGNOSIS ===");

        // 1. Tìm tất cả plant entities
        var roots = Object.FindObjectsOfType<EntityRoot>();
        int plantCount = 0;
        foreach (var root in roots)
        {
            var entity = root.GetEntity();
            if (entity == null) continue;
            var stage = entity.GetModule<StageRuntime>();
            if (stage == null) continue;

            plantCount++;
            var pos = root.gameObject.transform.position;
            var cell = new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
            bool watered = gm.WateredTileTracker?.IsWatered(cell) ?? false;

            Debug.Log($"[DIAG] Plant: {root.gameObject.name} | entityData={entity.entityData?.id} | pos={pos} | cell={cell} | stage={stage.currentStageIndex} | watered={watered} | daysWithoutWater={stage.DaysWithoutWater}");
        }

        if (plantCount == 0)
            Debug.LogWarning("[DIAG] NO PLANTS FOUND in scene!");

        // 2. Tìm StageObject MonoBehaviours
        var stageObjects = Object.FindObjectsOfType<StageObject>();
        Debug.Log($"[DIAG] StageObject count: {stageObjects.Length}");
        foreach (var so in stageObjects)
            Debug.Log($"[DIAG]   StageObject: {so.gameObject.name}");

        // 3. Check WateredTileTracker
        var tracker = gm.WateredTileTracker;
        Debug.Log($"[DIAG] WateredTileTracker: {(tracker != null ? "OK" : "NULL")}");

        // 4. Check TimeManager
        var tm = gm.TimeManager;
        Debug.Log($"[DIAG] TimeManager: {(tm != null ? $"Day={tm.Day}, Hour={tm.Hour}" : "NULL")}");

        // 5. Simulate: water cell (0,0) then trigger NextDay on all plants
        if (plantCount > 0)
        {
            Debug.Log("[DIAG] Simulating: water all plant cells + trigger NextDay...");
            foreach (var root in roots)
            {
                var entity = root.GetEntity();
                if (entity == null) continue;
                var stage = entity.GetModule<StageRuntime>();
                if (stage == null) continue;

                var pos = root.gameObject.transform.position;
                var cell = new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));

                // Water the cell
                tracker?.SetWatered(cell);
                Debug.Log($"[DIAG] Watered cell {cell} for {root.gameObject.name}");

                int stageBefore = stage.currentStageIndex;
                // Trigger NextDay directly
                entity.TriggerEvent(new NextDayEvent());
                int stageAfter = stage.currentStageIndex;

                if (stageAfter > stageBefore)
                    Debug.Log($"[DIAG] ✅ {root.gameObject.name}: stage {stageBefore} → {stageAfter} (GREW!)");
                else
                    Debug.LogWarning($"[DIAG] ⚠️ {root.gameObject.name}: stage still {stageAfter} (did NOT grow yet — may need more days)");
            }
        }

        Debug.Log("[DIAG] === DIAGNOSIS COMPLETE ===");
    }
}
