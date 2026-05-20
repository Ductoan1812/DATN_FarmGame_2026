# Sprint Log

## Sprint 0 - AI Automation Workflow

Status: Done

Goals:
- Configure Kiro workspace steering.
- Configure implementer and fixer agents.
- Track model quality per sprint.
- Establish one-agent-at-a-time workflow.

Initial model choice for Kiro: `claude-sonnet-4.6`.

Prototype source:
- `Assets/Project/Docs/Design/05_PrototypePlan.md`

Observation:
- Sprint 1 is the current priority.
- The worktree already contains modified/new files for Sprint 1 systems, so the first Kiro task should audit and finish the current Sprint 1 state instead of recreating it from scratch.

Result:
- Kiro `claude-sonnet-4.6` audited Sprint 1 code and made no code edits.
- Codex verified Unity compilation: 0 errors.
- Codex ran Play Mode test `Assets/Editor/Test_Sprint1_Full.cs`: 8 passed, 0 failed.
- Remaining Sprint 1 code gap: watered tile state is not saved/loaded in `SystemSaveData` and `SaveLoadManager`.
- Codex leader/QA role rules recorded in `.ai-workflow/CODEX_LEADER_RULES.md`.

## Sprint 1.12 - Save Watered State

Status: Done

Goal:
- Save and load current-day watered tile cells so mid-day save/load preserves farming state.

Expected owner files:
- `Assets/Project/Scripts/Core/Service/WateredTileTracker.cs`
- `Assets/Project/Scripts/Data/Structs/SystemSaveData.cs`
- `Assets/Project/Scripts/Systems/SaveLoadManager.cs`

Result:
- Kiro `claude-sonnet-4.6` added JsonUtility-safe watered cell DTOs.
- WateredTileTracker can export/import watered cells.
- SaveLoadManager now saves and loads watered cells with system data.
- Kiro test passed.
- Codex independent Play Mode test passed: `[TEST PASS] Watered cells survive save/load`.
- Unity compilation after cleanup: 0 errors.

Next candidate:
- Review remaining Sprint 1 editor/asset hookups: watered tile assignment, bed/watering can EntityData, FarmScene placement.
- Or begin Sprint 2 with WeatherSystem after confirming Sprint 1 asset setup is complete.

## Sprint 2.1-2.4 - Weather Foundation

Status: Done

Goal:
- Add WeatherType, WeatherConfig, WeatherSystem.
- Generate/hold current day weather.
- Rain should auto-water plowed cells for the current day.
- Preserve day transition order: plants process yesterday water first, watered tiles reset second, rain applies to the new day third.

Initial Kiro model: `claude-sonnet-4.6`.

Review note:
- Kiro implemented weather state, rain auto-water, save/load, debug commands, and automatic next-day weather roll.
- Codex verified compile: 0 errors.
- Kiro Play Mode test passed across multiple day transitions.
- Rain now rolls and applies inside the same day-change handler, after plant growth and before/after reset in explicit order.
- Next sprint candidate: Weather UI hook, weather asset tuning, then Sprint 2.5+ fertilizer/quality work.
