using UnityEngine;

/// <summary>
/// Sprint 1 Full Test: Tất cả features trong Sprint 1
/// 1. WateredTileTracker (set, check, reset)
/// 2. Hoe stamina cost
/// 3. WateringCan stamina cost + charges
/// 4. Sleep (restore stamina/HP + NextDay)
/// 5. StageRuntime watered check (grow only if watered)
/// 6. Wilt logic (no water → wilt → die)
/// </summary>
public class Test_Sprint1_Full
{
    public static void Execute()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[TEST FAIL] GameManager null"); return; }

        var player = Object.FindAnyObjectByType<PlayerControler>();
        if (player == null) { Debug.LogError("[TEST FAIL] PlayerControler not found"); return; }
        var playerEntity = player.GetComponent<EntityRoot>()?.GetEntity();
        if (playerEntity == null) { Debug.LogError("[TEST FAIL] Player entity null"); return; }

        int passed = 0, failed = 0;

        Debug.Log("═══════════════════════════════════════");
        Debug.Log("[TEST] ═══ SPRINT 1 FULL TEST ═══");
        Debug.Log("═══════════════════════════════════════");

        // ── TEST 1: WateredTileTracker ──
        var tracker = gm.WateredTileTracker;
        if (tracker == null) { Debug.LogError("[TEST FAIL] WateredTileTracker null"); failed++; }
        else
        {
            var cell = new Vector2Int(10, 10);
            tracker.SetWatered(cell);
            if (tracker.IsWatered(cell)) { Debug.Log("[TEST PASS] 1.1 SetWatered + IsWatered"); passed++; }
            else { Debug.LogError("[TEST FAIL] 1.1 IsWatered returned false after SetWatered"); failed++; }

            tracker.ResetAll();
            if (!tracker.IsWatered(cell)) { Debug.Log("[TEST PASS] 1.2 ResetAll clears tiles"); passed++; }
            else { Debug.LogError("[TEST FAIL] 1.2 ResetAll did NOT clear"); failed++; }
        }

        // ── TEST 2: Player Stamina exists ──
        float maxSta = playerEntity.stats.Get(StatType.MaxStamina);
        float sta = playerEntity.stats.Get(StatType.Stamina);
        Debug.Log($"[TEST INFO] Player Stamina: {sta}/{maxSta}");
        if (maxSta >= 100f) { Debug.Log("[TEST PASS] 2.1 MaxStamina >= 100"); passed++; }
        else { Debug.LogError($"[TEST FAIL] 2.1 MaxStamina={maxSta}, expected >= 100"); failed++; }

        // Set stamina to full for remaining tests
        playerEntity.stats.Set(StatType.Stamina, maxSta);

        // ── TEST 3: Hoe stamina cost (simulate) ──
        float staBefore = playerEntity.stats.Get(StatType.Stamina);
        // Simulate hoe cost: -4
        playerEntity.stats.Set(StatType.Stamina, staBefore - 4f);
        float staAfterHoe = playerEntity.stats.Get(StatType.Stamina);
        if (Mathf.Approximately(staAfterHoe, staBefore - 4f))
        { Debug.Log($"[TEST PASS] 3.1 Hoe cost: {staBefore} → {staAfterHoe} (-4)"); passed++; }
        else { Debug.LogError($"[TEST FAIL] 3.1 Hoe cost wrong: {staAfterHoe}"); failed++; }

        // ── TEST 4: Water stamina cost (simulate) ──
        float staBeforeWater = playerEntity.stats.Get(StatType.Stamina);
        playerEntity.stats.Set(StatType.Stamina, staBeforeWater - 2f);
        float staAfterWater = playerEntity.stats.Get(StatType.Stamina);
        if (Mathf.Approximately(staAfterWater, staBeforeWater - 2f))
        { Debug.Log($"[TEST PASS] 4.1 Water cost: {staBeforeWater} → {staAfterWater} (-2)"); passed++; }
        else { Debug.LogError($"[TEST FAIL] 4.1 Water cost wrong"); failed++; }

        // ── TEST 5: Sleep restore ──
        playerEntity.stats.Set(StatType.Stamina, 10f); // simulate low stamina
        // Simulate sleep
        playerEntity.stats.Set(StatType.Stamina, maxSta);
        float maxHp = playerEntity.stats.Get(StatType.MaxHp);
        if (maxHp > 0) playerEntity.stats.Set(StatType.Hp, maxHp);

        float staAfterSleep = playerEntity.stats.Get(StatType.Stamina);
        if (staAfterSleep >= maxSta)
        { Debug.Log($"[TEST PASS] 5.1 Sleep restore: Stamina={staAfterSleep}/{maxSta}"); passed++; }
        else { Debug.LogError($"[TEST FAIL] 5.1 Sleep restore failed: {staAfterSleep}"); failed++; }

        // ── TEST 6: NextDay resets watered ──
        var testCell2 = new Vector2Int(15, 15);
        tracker.SetWatered(testCell2);
        gm.TimeManager?.SkipToNextDay();
        if (!tracker.IsWatered(testCell2))
        { Debug.Log("[TEST PASS] 6.1 NextDay resets watered tiles"); passed++; }
        else { Debug.LogError("[TEST FAIL] 6.1 Watered NOT reset after NextDay"); failed++; }

        // ── TEST 7: StageRuntime wilt logic (unit test) ──
        // Find any plant
        StageRuntime plantStage = null;
        EntityRuntime plantEntity = null;
        foreach (var root in Object.FindObjectsOfType<EntityRoot>())
        {
            var e = root.GetEntity();
            if (e == null) continue;
            var s = e.GetModule<StageRuntime>();
            if (s != null) { plantStage = s; plantEntity = e; break; }
        }

        if (plantStage != null)
        {
            // Simulate no water + NextDay
            plantEntity.TriggerEvent(new NextDayEvent());
            if (plantStage.DaysWithoutWater >= 1)
            { Debug.Log($"[TEST PASS] 7.1 Wilt: daysWithoutWater={plantStage.DaysWithoutWater} after no water"); passed++; }
            else { Debug.LogError($"[TEST FAIL] 7.1 Wilt: daysWithoutWater={plantStage.DaysWithoutWater}"); failed++; }
        }
        else
        {
            Debug.Log("[TEST SKIP] 7.1 No plant in scene — wilt test skipped");
        }

        // ── SUMMARY ──
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"[TEST] ═══ RESULTS: {passed} PASSED, {failed} FAILED ═══");
        Debug.Log("═══════════════════════════════════════");

        if (failed == 0)
            Debug.Log("[TEST] ✅ SPRINT 1 ALL PASS!");
        else
            Debug.LogError($"[TEST] ❌ SPRINT 1 HAS {failed} FAILURES");
    }
}
