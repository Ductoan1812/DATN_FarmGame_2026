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

---

## Final Principle

Every task must answer: does this make the coreplay loop more complete, balanced, testable, or reliable? If no → postpone.
