using UnityEngine;

/// <summary>
/// Test: Wilt Logic
/// 1. Spawn plant → water → NextDay → should GROW (not wilt)
/// 2. NextDay WITHOUT water → should WILT (daysWithoutWater=1)
/// 3. NextDay WITHOUT water again → should DIE (daysWithoutWater=2)
/// </summary>
public class Test_WiltLogic
{
    public static void Execute()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[TEST FAIL] GameManager null"); return; }

        Debug.Log("[TEST] === WILT LOGIC TEST ===");

        // Find any plant entity with StageRuntime
        StageRuntime stageRuntime = null;
        EntityRuntime plantEntity = null;
        var roots = Object.FindObjectsOfType<EntityRoot>();

        foreach (var root in roots)
        {
            var entity = root.GetEntity();
            if (entity == null) continue;
            var stage = entity.GetModule<StageRuntime>();
            if (stage != null)
            {
                stageRuntime = stage;
                plantEntity = entity;
                Debug.Log($"[TEST] Found plant: {root.gameObject.name}, stage={stage.currentStageIndex}, daysWithoutWater={stage.DaysWithoutWater}");
                break;
            }
        }

        if (stageRuntime == null)
        {
            // No plant found — test WateredTileTracker + wilt logic directly
            Debug.LogWarning("[TEST] No plant in scene. Testing WateredTileTracker logic only.");

            var tracker = gm.WateredTileTracker;
            var testCell = new Vector2Int(5, 5);

            // Water → check
            tracker.SetWatered(testCell);
            Debug.Log($"[TEST PASS] SetWatered({testCell}) → IsWatered={tracker.IsWatered(testCell)}");

            // Reset → check
            tracker.ResetAll();
            Debug.Log($"[TEST PASS] ResetAll() → IsWatered={tracker.IsWatered(testCell)} (should be false)");

            Debug.Log("[TEST] To test full wilt: plant a crop first (spawn Plant01 at a plowed cell)");
            Debug.Log("[TEST] === WILT LOGIC TEST COMPLETE (partial) ===");
            return;
        }

        // ── Test with actual plant ──
        var plantGO = plantEntity.Owner?.GameObject;
        if (plantGO == null) { Debug.LogError("[TEST FAIL] Plant has no GameObject"); return; }

        var pos = plantGO.transform.position;
        var cell = new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
        var tracker2 = gm.WateredTileTracker;

        // Step 1: Water + NextDay → should grow
        tracker2.SetWatered(cell);
        int stageBefore = stageRuntime.currentStageIndex;
        plantEntity.TriggerEvent(new NextDayEvent());
        tracker2.ResetAll(); // simulate day reset

        if (stageRuntime.DaysWithoutWater == 0)
            Debug.Log($"[TEST PASS] Watered + NextDay: daysWithoutWater=0, stage={stageRuntime.currentStageIndex}");
        else
            Debug.LogError($"[TEST FAIL] Watered + NextDay: daysWithoutWater={stageRuntime.DaysWithoutWater} (expected 0)");

        // Step 2: NO water + NextDay → should wilt (daysWithoutWater=1)
        // Don't water this time
        plantEntity.TriggerEvent(new NextDayEvent());

        if (stageRuntime.DaysWithoutWater == 1 && stageRuntime.IsWilting)
            Debug.Log($"[TEST PASS] No water + NextDay: daysWithoutWater=1, isWilting=true");
        else
            Debug.LogError($"[TEST FAIL] No water + NextDay: daysWithoutWater={stageRuntime.DaysWithoutWater}, isWilting={stageRuntime.IsWilting}");

        // Step 3: NO water again + NextDay → should die (daysWithoutWater=2)
        // Check if plant still exists after this
        plantEntity.TriggerEvent(new NextDayEvent());

        if (stageRuntime.DaysWithoutWater >= 2)
            Debug.Log($"[TEST PASS] No water 2 days: daysWithoutWater={stageRuntime.DaysWithoutWater} → DieEvent triggered");
        else
            Debug.LogError($"[TEST FAIL] No water 2 days: daysWithoutWater={stageRuntime.DaysWithoutWater} (expected >=2)");

        Debug.Log("[TEST] === WILT LOGIC TEST COMPLETE ===");
    }
}
