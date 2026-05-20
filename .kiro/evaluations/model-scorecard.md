# Kiro Model Scorecard

Scoring scale:
- 5 = strong, clean, passes with little correction.
- 3 = usable, needs normal review/fix.
- 1 = poor fit, repeated failures or unsafe scope creep.

| Sprint | Task | Model | Compile | Runtime Test | Scope Control | Architecture Fit | Iterations | Result | Notes |
|---|---|---|---:|---:|---:|---:|---:|---|---|
| 0/1 | Sprint 1 stabilization audit | claude-sonnet-4.6 | 5 | 4 | 5 | 4 | 1 | PASS | Kiro made no speculative edits; Unity compile 0 errors; `Test_Sprint1_Full` 8 pass / 0 fail. Runtime logs include expected StageRuntime death as Error and existing asset/font warnings. |
| 1.12 | Save/load watered tile state | claude-sonnet-4.6 | 5 | 5 | 4 | 4 | 1 | PASS | Kiro implemented DTO export/import and SaveLoadManager wiring. Codex independently verified Play Mode `[TEST PASS] Watered cells survive save/load`. Minor API note: `LoadSystemDataPublic()` is test/debug-oriented. |

Model policy:
- Start with `claude-sonnet-4.6`.
- Use `claude-sonnet-4.5` only for very small low-risk tasks.
- Escalate to `claude-opus-4.5` after two repeated failures on the same task.
- Use `claude-opus-4.6` or `claude-opus-4.7` for save/load, progression, or cross-system failures.
