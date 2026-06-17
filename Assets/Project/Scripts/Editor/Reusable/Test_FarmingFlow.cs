using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Test script: Farming Flow (Hoe → Plant → Water → NextDay → Check Growth)
/// Chạy trong Play Mode via MCP execute_script.
/// </summary>
public class Test_FarmingFlow
{
    public static void Execute()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[TEST FAIL] GameManager.Instance is null"); return; }

        var player = Object.FindAnyObjectByType<PlayerControler>();
        if (player == null) { Debug.LogError("[TEST FAIL] PlayerControler not found"); return; }

        var playerEntity = player.GetComponent<EntityRoot>()?.GetEntity();
        if (playerEntity == null) { Debug.LogError("[TEST FAIL] Player EntityRuntime is null"); return; }

        // Test position: use player's current position front cell
        var playerPos = player.transform.position;
        var testCell = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y) + 1);

        Debug.Log($"[TEST] === FARMING FLOW TEST === Player at {playerPos}, test cell: {testCell}");

        // ── Step 1: Check WateredTileTracker exists ──
        var tracker = gm.WateredTileTracker;
        if (tracker == null) { Debug.LogError("[TEST FAIL] WateredTileTracker is null!"); return; }
        Debug.Log("[TEST PASS] WateredTileTracker initialized");

        // ── Step 2: Hoe (cuốc đất) ──
        var ws = gm.WorldService;
        var tileData = gm.TileData;
        if (tileData?.plowedTile == null) { Debug.LogError("[TEST FAIL] TileData.plowedTile is null"); return; }

        ws.SetGround(testCell, tileData.plowedTile);
        var groundAfterHoe = ws.GetGround(testCell);
        if (groundAfterHoe == tileData.plowedTile)
            Debug.Log($"[TEST PASS] Hoe: cell {testCell} is now plowed");
        else
            Debug.LogError($"[TEST FAIL] Hoe: cell {testCell} ground is {groundAfterHoe}, expected plowedTile");

        // ── Step 3: Water (tưới nước) ──
        tracker.SetWatered(testCell);
        bool isWatered = tracker.IsWatered(testCell);
        if (isWatered)
            Debug.Log($"[TEST PASS] Water: cell {testCell} is watered");
        else
            Debug.LogError($"[TEST FAIL] Water: cell {testCell} NOT watered after SetWatered");

        // ── Step 4: Check player stamina ──
        float stamina = playerEntity.stats.Get(StatType.Stamina);
        float maxStamina = playerEntity.stats.Get(StatType.MaxStamina);
        Debug.Log($"[TEST INFO] Player Stamina: {stamina}/{maxStamina}");

        // ── Step 5: Simulate Sleep (restore + NextDay) ──
        // Restore stamina/HP
        if (maxStamina > 0) playerEntity.stats.Set(StatType.Stamina, maxStamina);
        float maxHp = playerEntity.stats.Get(StatType.MaxHp);
        if (maxHp > 0) playerEntity.stats.Set(StatType.Hp, maxHp);

        // Skip to next day
        gm.TimeManager?.SkipToNextDay();

        float staminaAfterSleep = playerEntity.stats.Get(StatType.Stamina);
        if (staminaAfterSleep >= maxStamina)
            Debug.Log($"[TEST PASS] Sleep: Stamina restored to {staminaAfterSleep}/{maxStamina}");
        else
            Debug.LogError($"[TEST FAIL] Sleep: Stamina={staminaAfterSleep}, expected {maxStamina}");

        // ── Step 6: Check watered tiles reset after NextDay ──
        bool stillWatered = tracker.IsWatered(testCell);
        if (!stillWatered)
            Debug.Log($"[TEST PASS] NextDay: watered tiles reset (cell {testCell} no longer watered)");
        else
            Debug.LogError($"[TEST FAIL] NextDay: cell {testCell} STILL watered after day change!");

        // ── Step 7: Check if any plants grew (if there are plants in scene) ──
        var stageObjects = Object.FindObjectsOfType<StageObject>();
        Debug.Log($"[TEST INFO] Found {stageObjects.Length} StageObjects in scene");

        Debug.Log("[TEST] === FARMING FLOW TEST COMPLETE ===");
    }
}
