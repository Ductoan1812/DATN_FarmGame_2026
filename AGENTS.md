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

## Final Principle

Every task must answer: does this make the coreplay loop more complete, balanced, testable, or reliable? If no → postpone.
