# DATN_FarmGame Codex Instructions

## Project Direction

This project is a Unity 2D top-down farming/action RPG built for a solo developer.

The current production goal is not to expand scope. The current goal is to finish the coreplay loop as fast and safely as possible:

1. New game starts with valid player data.
2. Player farms crops.
3. Player harvests and receives items.
4. Player sells/buys through shop.
5. Player mines/fights enemies and receives drops.
6. Player completes quests and gains rewards.
7. Player levels up, becomes clearly stronger, saves/loads, and repeats the loop.

Everything must serve this loop first.

Prioritize:

* gameplay feel
* core systems correctness
* progression from level 1 to level 50
* game balance
* data completeness
* save/load reliability
* maintainable backend/gameplay logic

Deprioritize until coreplay is stable:

* UI polish
* decorative animations
* large visual redesigns
* friendship systems
* festivals
* procedural dungeon generation
* complex crafting UI
* fishing
* season complexity
* large refactors

UI is temporary during this phase. It only needs to be functional, readable, correctly positioned, and technically correct.

---

## Response Structure

For technical work, use this structure when applicable:

1. Problem
2. Analysis
3. Optimal Solution
4. Implementation Steps
5. Quick Testing Method
6. Risks / Edge Cases
7. Future Expansion

Keep responses concise, implementation-focused, and practical.

Avoid vague theory, filler, and architecture for appearance only.

---

## Core Development Rules

Always prefer:

* minimal safe changes
* existing project patterns
* readable code
* data-driven tuning
* clear ownership boundaries
* fast iteration
* low solo-developer maintenance cost

Never:

* rewrite working systems without a direct reason
* break existing APIs casually
* duplicate logic across multiple scripts
* move gameplay logic into UI
* introduce enterprise-style architecture
* add a new system before checking whether an existing service/module can be extended

When multiple solutions exist, choose the simplest maintainable solution that helps finish the coreplay loop.

---

## Current MVP Scope

The game should be finished as a vertical slice, not as a huge open-ended farm sim.

Required MVP pillars:

* farming loop
* crop growth and harvest
* inventory/hotbar/equipment
* shop buy/sell
* dialogue/quest interaction
* enemy combat
* mining/resource drops
* EXP and level-up progression
* visible level 1 to level 50 power growth
* save/load for runtime state

Preferred level progression design:

* Level 1-10: basic farming, weak enemy, basic ore/tool/gear.
* Level 11-20: stronger enemy, better crop profit, tier 2 gear/tool.
* Level 21-30: deeper mining/combat, tier 3 rewards.
* Level 31-40: elite enemy/resource tier, stronger economy.
* Level 41-50: final challenge tier, strongest gear/tool, clear endgame feel.

Do not handcraft 50 unique progression steps. Use 5 level bands and data-driven stat/item/enemy tiers.

---

## Architecture Boundaries

The Unity gameplay backend is divided into three layers.

### 1. Data And Runtime Logic

This layer contains pure gameplay data and runtime behavior:

* `EntityData` = static configuration
* `EntityRuntime` = dynamic runtime state
* `ModuleData` = module configuration
* `ModuleRuntime` = module logic

Rules:

* Put reusable gameplay behavior in module runtimes when possible.
* Keep runtime logic independent from Unity scene objects when practical.
* Save only IDs, runtime state, progression state, dynamic values.
* Do not save static ScriptableObject data or prefab references directly.

### 2. Unity Bridge Components

This layer connects Unity objects to runtime logic.

Examples:

* `PlayerBridge`
* player/inventory/equipment bridges
* object interaction components
* animation/tool/action bridges
* MonoBehaviours attached to scene objects

Rules:

* MonoBehaviour should be a bridge, adapter, or lifecycle hook.
* Do not put core gameplay rules here if a runtime module or service should own them.
* Use MonoBehaviour only for Unity-specific concerns such as transforms, collisions, animation events, references, and scene setup.

### 3. Systems And Services

This layer provides shared operations for the whole game.

Examples:

* `EntityService`
* `InventoryService`
* `ShopService`
* `QuestService`
* `WorldEntityService`
* `TimeManager`
* registries and save/load systems

Rules:

* Each service must own one clear responsibility.
* If a service owns a mutation, other scripts must use that service instead of editing state directly.
* Do not duplicate the same business rule in two places.
* Services may contain both data access and logic, but must remain focused.

---

## State Mutation Ownership

State changes must go through the owning API.

Examples:

* Entity amount/stack changes must go through the owning entity/inventory service API.
* Inventory add/remove/transfer must go through `InventoryService`.
* Shop buy/sell/money transfer must go through `ShopService`.
* Quest state/rewards must go through `QuestService`.
* Time/day changes must go through `TimeManager` or its existing event flow.
* Player EXP/level progression should go through one progression API, not scattered direct stat edits.

Avoid direct mutations such as:

```csharp
entity.Amount = 2;
player.stats.Set(StatType.Level, value); // outside the owning progression flow
inventory.slots[index] = item;           // outside InventoryService
```

Direct mutation is allowed only inside the owning service/runtime, migration code, editor bootstrap tools, or narrowly scoped debug commands.

If no owning API exists yet, create the smallest focused API rather than scattering direct writes.

---

## Coreplay And Balance Rules

Balance is part of coreplay, not polish.

Every gameplay reward must answer:

* What does the player gain?
* How does it help the next loop?
* Does it make level 1 and level 50 feel different?
* Does it affect money, EXP, gear, tool power, unlocks, or quest progress?

Progression should be data-driven where possible:

* crop price and growth time
* enemy HP/Attack/Defense/EXP/drop
* ore HP/drop value
* tool Attack/Range/CoolDown
* equipment stats
* quest reward money/EXP/item
* shop stock and prices

Do not add content that does not support the loop.

---

## UI Rules For Current Phase

UI is functional tooling first.

UI must:

* show correct data
* allow required interactions
* be positioned predictably
* use correct anchors and Canvas setup
* use TextMeshPro or a font setup that supports Vietnamese
* avoid broken text, missing glyphs, and unreadable labels
* be generated by editor/runtime scripts when that is faster and safer

UI does not need:

* visual polish
* decorative panels
* complex animation
* final art
* unnecessary layout redesign

Temporary UI is acceptable only if it is technically clean enough to test gameplay reliably.

---

## Unity Performance Rules

Avoid:

* `FindObjectOfType` or `FindAnyObjectByType` in gameplay loops
* repeated `GetComponent` in hot paths
* LINQ in hot paths
* unnecessary allocations in `Update`
* reflection-heavy runtime logic

Cache references where practical.

Performance matters after correctness, stability, readability, and maintainability.

---

## Debugging Rules

When fixing bugs:

1. Reproduce or inspect the relevant flow.
2. Read logs and current code first.
3. Identify the root cause.
4. Fix the smallest responsible location.
5. Explain why the issue happened.
6. Provide quick manual test steps.

Avoid random trial-and-error changes.

---

## Refactor Rules

Refactor only when it directly improves current work.

Prefer:

* small isolated refactors
* compatibility-preserving changes
* migration-safe edits
* clear method extraction when it removes real duplication

Avoid:

* renaming large systems
* changing public APIs without need
* moving many files at once
* rewriting working gameplay

---

## AgentMemory Rules

Use AgentMemory as the persistent project memory layer.

At the start of non-trivial work:

* search memory for relevant project decisions, known bugs, architecture rules, and user preferences
* keep memory queries specific to the task

During work:

* trust current code over memory if they conflict
* verify assumptions before editing

At the end of meaningful work:

* save durable decisions, architecture rules, project preferences, and recurring lessons
* do not save transient command output, secrets, or noisy progress notes

---

## Git Workflow For Unity

Unity projects should not use many long-lived feature branches in parallel.

Reason:

* scenes (`.unity`) are shared integration files
* prefabs (`.prefab`) are often touched by multiple systems
* ScriptableObjects (`.asset`) contain serialized data that can conflict
* `.meta` files are required and must stay paired with assets
* YAML conflicts in Unity assets are harder to review than normal code conflicts

Preferred workflow for this solo project:

* `main` must stay playable and stable.
* Use one main integration branch for the current MVP, for example `codex/coreplay-mvp`.
* Use short-lived feature branches only when the work is risky or clearly isolated.
* Merge feature branches back quickly after compile and manual gameplay testing.
* Do not keep multiple asset-heavy branches alive at the same time.

Branching rules:

* Code-only work can use short feature branches.
* Scene/prefab/data-heavy work should usually happen directly on the current integration branch.
* Never edit the same scene/prefab/ScriptableObject in two active branches unless absolutely necessary.
* If a task requires scene setup, finish and merge it before starting another scene-heavy task.
* Prefer additive scenes, prefab variants, ScriptableObject assets, and editor bootstrap scripts to reduce direct edits to one shared scene.

Commit rules:

* Commit small playable milestones.
* Keep code, data, and scene setup changes separated when practical.
* Prefer this order: code API first, data assets second, scene/prefab hookup last.
* Before large work, check `git status` and understand existing dirty files.
* Never revert unrelated user changes.

Unity repository setup should use:

* Visible Meta Files
* Force Text asset serialization
* Git LFS for large binary assets when needed
* UnityYAMLMerge/Smart Merge if available

Milestone tags are recommended:

* `coreloop-week1`
* `coreloop-playable`
* `mvp-balance-pass-1`
* `mvp-release-candidate`

The goal is not a complex Git workflow. The goal is to keep one playable line of development while avoiding Unity asset merge pain.

---

## Final Principle

Until the coreplay loop is complete, every task should be judged by one question:

Does this make the playable game loop more complete, more balanced, more testable, or more reliable?

If the answer is no, postpone it.
