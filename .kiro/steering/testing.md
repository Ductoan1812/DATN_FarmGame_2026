# Testing Rules

After implementing a feature, verify with the real game flow.

Required loop:
1. Compile check through Unity diagnostics when available.
2. Enter Play mode when runtime behavior is involved.
3. Execute a focused test through debug commands or editor script.
4. Read Unity logs.
5. Stop Play mode.
6. Fix failures before claiming pass.

Use `[TEST PASS]` and `[TEST FAIL]` in test logs.

Do not test by bypassing the real event/system chain:
- Prefer TimeManager.SkipToNextDay over directly triggering NextDayEvent.
- Prefer debug commands that call the same runtime/service path as gameplay.
- Verify required prefab bridge components when a feature depends on scene objects.

Temporary test scripts:
- Put them in `Assets/Editor/Test_<FeatureName>.cs`.
- Delete them after the feature is confirmed.

Permanent debug commands are allowed when they help future testing, but they must call the correct runtime/service path and log clear results.
