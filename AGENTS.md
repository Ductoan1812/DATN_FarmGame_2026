# DATN_FarmGame — Agent Rules

## Goal

Unity 2D top-down farming/action RPG, solo dev. Finish the coreplay loop first:

1. New game → valid player data
2. Farm crops → harvest → receive items
3. Buy/sell through shop
4. Mine/fight → receive drops
5. Complete quests → gain rewards
6. Level up → stronger → save/load → repeat

**Priority:** gameplay feel, systems correctness, progression L1–L50, balance, data completeness, save/load, maintainable logic.

**Defer:** UI polish, decorative animations, visual redesigns, friendship, festivals, procedural dungeons, complex crafting UI, fishing, season complexity, large refactors.

---

## Response Format

Use when applicable: Problem → Analysis → Solution → Steps → Test → Risks → Future.

Be concise, implementation-focused. No filler or architecture-for-show.

---

## Dev Rules

**Prefer:** minimal safe changes, existing patterns, readable code, data-driven tuning, clear ownership, fast iteration, low maintenance cost.

**Never:** rewrite working systems without reason, break APIs casually, duplicate logic, put gameplay logic in UI, add enterprise architecture, add new systems before checking if existing ones can extend.

Choose the simplest maintainable solution that finishes the coreplay loop.

---

## MVP Scope

Vertical slice, not open-ended sim.

**Pillars:** farming, crop growth/harvest, inventory/hotbar/equipment, shop, dialogue/quest, combat, mining/drops, EXP/level-up (L1–L50), save/load.

**Level bands (data-driven, 5 tiers):**
- L1–10: basic farming/enemy/ore/gear
- L11–20: stronger enemy, better crops, tier 2
- L21–30: deeper combat/mining, tier 3
- L31–40: elite tier, stronger economy
- L41–50: final challenge, strongest gear, endgame feel

---

## Architecture (3 Layers)

### 1. Data & Runtime
- `EntityData` (static config), `EntityRuntime` (dynamic state)
- `ModuleData` (module config), `ModuleRuntime` (module logic)
- Keep runtime logic scene-independent. Save only IDs + dynamic values.

### 2. Unity Bridge
- MonoBehaviours as bridges/adapters/lifecycle hooks only (`PlayerBridge`, interaction components, animation bridges).
- No core gameplay rules in MonoBehaviour — use runtime/service.

### 3. Services
- `EntityService`, `InventoryService`, `ShopService`, `QuestService`, `WorldEntityService`, `TimeManager`, registries, save/load.
- One clear responsibility per service. No duplicated business rules.

---

## State Mutation

All state changes go through the owning API:
- Inventory → `InventoryService`
- Shop/money → `ShopService`
- Quest → `QuestService`
- Time → `TimeManager`
- EXP/level → single progression API

No direct field writes outside the owning service (except inside that service, migration code, editor tools, or debug commands). If no API exists, create the smallest focused one.

---

## Balance

Balance = coreplay, not polish. Every reward must answer: what does the player gain, how does it help the next loop, does it differentiate L1 vs L50?

Data-driven: crop price/growth, enemy stats/drops, ore value, tool stats, equipment stats, quest rewards, shop prices.

---

## UI (Current Phase)

Functional only. Must: show correct data, allow interactions, correct anchors, TMP with Vietnamese support, no broken text. Does not need: polish, decorative panels, animation, final art.

---

## Performance

Avoid in hot paths: `FindObjectOfType`, repeated `GetComponent`, LINQ, unnecessary allocations, reflection. Cache references.

---

## Debugging

Reproduce → read logs/code → find root cause → fix smallest location → explain why → provide test steps. No trial-and-error.

---

## Refactoring

Only when it directly improves current work. Small, isolated, compatibility-preserving. No large renames, API breaks, or mass file moves.

---

## Git Workflow

Solo project, Unity assets conflict easily (scenes, prefabs, SOs, meta files).

- `main` stays playable.
- One integration branch (e.g. `codex/coreplay-mvp`).
- Short-lived feature branches for risky/isolated code-only work; merge quickly.
- Scene/prefab/data work on integration branch directly.
- Never edit same scene/prefab in two branches.
- Commit order: code API → data assets → scene hookup.
- Never revert unrelated user changes.
- Use: Visible Meta Files, Force Text, Git LFS for large binaries, UnityYAMLMerge if available.

---

## Automated Testing Workflow (Agent)

After implementing a feature, agent MUST verify it works using this loop:

### Test Loop
```
1. CODE → implement feature
2. COMPILE → check getDiagnostics (no errors)
3. PLAY → mcp_aura_unity_play_game()
4. TEST → execute test steps via mcp_aura_unity_execute_script (C# script that calls DebugConsole commands programmatically or directly invokes services)
5. VERIFY → read Unity logs (mcp_aura_unity_get_unity_logs) + check entity state
6. STOP → mcp_aura_unity_stop_game()
7. If FAIL → fix code → go to step 2
8. If PASS → report success, move to next task
```

### How to Test (agent cannot move player or click UI)

Agent tests by writing and executing C# editor scripts that:
- Directly call service methods (GameManager.Instance.WateredTileTracker.SetWatered(...))
- Directly trigger events (entity.TriggerEvent(new PrimaryActionEvent(...)))
- Directly invoke TimeManager.SkipToNextDay()
- Read entity state and log results
- Use existing DebugConsole commands via GameManager APIs

**If a debug command doesn't exist for the action being tested:**
1. Read DebugConsole.cs to understand the command registration pattern
2. Add new commands directly into DebugConsole.RegisterBuiltInCommands()
3. New commands must follow existing pattern: `AddCommand("name", "help text", args => { ... })`
4. Commands added for testing STAY permanently (useful for future debugging)

**Examples of commands agent should create when needed:**
```
hoe <x> <y>         — Cuốc đất tại vị trí (simulate HoeRuntime)
water <x> <y>       — Tưới nước tại vị trí (simulate WateringCanRuntime)
plant <x> <y> <id>  — Trồng cây tại vị trí
sleep                — Ngủ (simulate BedRuntime: restore + NextDay)
refill               — Lấy nước đầy cho WateringCan
inspect <x> <y>     — Kiểm tra state tại ô (watered? planted? stage?)
```

**Command creation rules:**
- Command phải gọi đúng service/runtime (không bypass logic)
- VD: `hoe` phải gọi WorldEntityService.SetGround() giống HoeRuntime.Execute()
- VD: `water` phải gọi WateredTileTracker.SetWatered() giống WateringCanRuntime.Execute()
- VD: `sleep` phải restore stamina + HP + SkipToNextDay giống BedRuntime.DoSleep()
- Log kết quả rõ ràng để verify

### Available Debug Console Commands
```
give <target> <item> [amount]    — Give item to entity
set <target> <stat> <value>      — Set stat value
spawn <prefab> <x> <z> [dataId]  — Spawn entity at position
NextDay                          — Skip to next day
Time                             — Show current time
SetTime <hour> [minute]          — Set game time
PauseTime / ResumeTime           — Pause/resume time
save / load                      — Save/load game
entities                         — List all EntityRoot in scene
containers                       — List entities with inventory
list [filter]                    — List EntityData assets
exp <amount> [target]            — Grant EXP
```

### Test Script Pattern
```csharp
// Assets/Editor/Test_FeatureName.cs
public class Test_FeatureName
{
    public static void Execute()
    {
        // Setup
        var gm = GameManager.Instance;
        var player = FindObjectOfType<PlayerControler>().GetComponent<EntityRoot>().GetEntity();
        
        // Action (simulate what player would do)
        gm.WateredTileTracker.SetWatered(new Vector2Int(0, 0));
        
        // Verify
        bool watered = gm.WateredTileTracker.IsWatered(new Vector2Int(0, 0));
        if (watered) Debug.Log("[TEST PASS] Cell is watered");
        else Debug.LogError("[TEST FAIL] Cell NOT watered");
        
        // Cleanup
    }
}
```

### Rules
- ALWAYS test after implementing
- Test script names: `Test_<FeatureName>.cs` in `Assets/Editor/`
- Log format: `[TEST PASS]` or `[TEST FAIL]` for clear results
- Delete test scripts after feature is confirmed working
- If test requires Play mode: use play_game → wait → execute_script → read logs → stop_game

### CRITICAL: Test Must Use Real Game Flow (NOT Bypass)

**NEVER test by bypassing the real event/system flow.** Bypassed tests give false positives.

| ❌ WRONG (bypass) | ✅ CORRECT (real flow) |
|---|---|
| `entity.TriggerEvent(new NextDayEvent())` directly | `TimeManager.SkipToNextDay()` → EventBus → StageObject → entity |
| `WateredTileTracker.SetWatered()` directly in test | Use `water <x> <y>` debug command which calls WateringCanRuntime |
| `tracker.IsWatered()` after direct SetWatered | Check after real WateringCan tool use |
| Assume entity has all required components | Verify prefab has ALL required MonoBehaviours (StageObject, EntityRoot, etc.) |

**Before testing any feature, ALWAYS verify:**
1. Prefab has all required bridge components (StageObject, EnemyObject, etc.)
2. Event flow is complete: Input → EventBus → MonoBehaviour bridge → EntityRuntime → Module
3. Event ORDER matters: check if reset/cleanup happens before or after processing

**Known event order issue (lesson learned):**
- `TimeManager.SkipToNextDay()` publishes `DayChangedPublish`
- `GameManager` subscribes `DayChangedPublish` → calls `WateredTileTracker.ResetAll()` IMMEDIATELY
- `StageObject` also subscribes `DayChangedPublish` → forwards `NextDayEvent` to entity
- If `GameManager` handler runs BEFORE `StageObject` handler → watered tiles are reset BEFORE plant checks them → plant never sees watered=true
- Fix: plants must process grow BEFORE watered tiles reset, OR use separate events (EndOfDayGrow vs StartOfDayReset)

**When a test passes but real gameplay fails:**
- The test is bypassing something
- Check: does the test use the same event chain as real gameplay?
- Check: does the prefab in scene have all required components?
- Check: is there a MonoBehaviour bridge missing on the prefab?

---

## Final Principle

Every task must answer: does this make the coreplay loop more complete, balanced, testable, or reliable? If no → postpone.
