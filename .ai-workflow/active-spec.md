# Active Spec

## Current focus

- Feature or task cluster: Combat, quest, and progression polish baseline without audio
- Status: Review bug/perf fixes implemented; needs in-editor import/playtest pass
- Player-facing outcome: Combat now gives clearer hit feedback, kill quests can progress from real enemy deaths, nights can spawn light enemy pressure, and key lifecycle moments such as autosave and death have visible UI feedback.

## Scope

- In scope:
  - publish combat telemetry from actual damage/death resolution
  - show floating damage, crit, enemy HP bars, hit flash, alert indicators, death fade, camera shake, hit stop, low HP vignette, EXP popups, level-up text, combo text, toast, and death overlay
  - add player dodge i-frame/afterimage and lightweight swing trail polish
  - persist quest objective progress for kill objectives
  - convert mixed monster objectives to the six generated enemy ids
  - add temporary night enemy spawning with day-scaled enemy selection
  - autosave on day change with visible toast
  - fix Claude review items for pooling, death timing, safer HP/alert UI, safe night spawning, and trail material fallback
- Out of scope:
  - audio and music feedback
  - final art-directed UI layout pass
  - final combat economy/drop-table tuning
  - closing the user's currently open Unity Editor to force batchmode compile

## Constraints

- Global cross-system signals use `EventBus` struct `Publish` events.
- Entity-local gameplay stays in module events such as `TakeDamageEvent` and `DieEvent`.
- `HealthRuntime` remains the source of truth for final damage after defense/tool gating.
- UI listens to publish events and does not own gameplay decisions.
- New quest objective save data is backward compatible with old `QuestLog` save payloads.

## Acceptance result

- Damage resolution now publishes `DamageAppliedPublish` with raw/final damage, HP before/after, attacker, source item, world position, and crit flag.
- Death resolution now publishes `EntityDiedPublish`, enabling kill quest progress and death feedback.
- Player death now publishes `PlayerDeathPublish` for the death overlay.
- Enemy attack start publishes `EnemyAttackStartedPublish`; enemy state changes are observable by feedback components.
- GameManager auto-registers combat/progression polish components.
- `Quest_M5_Mixed_T1` through `T5` monster objectives now target `enemy_slime1`, `enemy_slime2`, `enemy_slime3`, `enemy_orc1`, and `enemy_orc2`.
- Night spawns use generated enemy EntityData/ObjectTypes and temporary save policy.
- Damage/EXP floating text and dodge afterimages now reuse pools instead of instantiate/destroy loops.
- Enemy death destroy delay now respects the death fade lifetime.
- Night enemies are cleaned up when night ends or a new day starts.
- Six generated enemies now use `ExpSourceType.Combat`.

## Validation result

- Roslyn compile check passed after Claude review fixes, including pooled text/afterimage scripts.
- `git diff --check` passed for touched scripts/data/docs.
- Six generated enemy EntityData assets confirmed with `ExpRewardModule.sourceType = ExpSourceType.Combat`.
- Unity batchmode compile could not run because the project is already open in Unity Editor; no editor process was closed.
- Unity `Editor.log` did not show current `error CS` entries during this pass.
