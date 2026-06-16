# Master Backlog

Use this file as the prioritized source of truth for unattended work slices.

## Priority scale

- `P0`: blocking core gameplay or progress
- `P1`: required for a complete playable loop
- `P2`: important polish or clarity improvement
- `P3`: nice-to-have follow-up

## In Progress

- [ ] GED canonical content expansion pass
  - Priority: `P0`
  - Area: content data pipeline, localization, gameplay-ready assets
  - Goal: expand the new canonical generator from a core playable slice into a broad GED-backed catalog that covers remaining crops, fruit trees, materials, foods, animals, enemies, and placeable utility content
  - Acceptance:
    - generator runs inside the open Unity Editor
    - canonical assets continue to land in `Assets/Project/Resources/Data/Entities`
    - each added archetype has coherent `id`, `keyName`, `descKey`, core modules, and VI/EN entries

## Todo

- [ ] Place and tune the six enemy spawn markers in production scenes
  - Priority: `P1`
  - Area: encounter design and scene content
  - Goal: use the generated `Marker_Slime1` through `Marker_Orc3` tiles to build readable difficulty zones
  - Acceptance: markers are placed in intended scenes, paths are reachable, spawn density is controlled, and no marker overlaps portals or critical farming space

- [ ] Add enemy material drops and reward sinks
  - Priority: `P1`
  - Area: combat progression
  - Goal: connect the six enemies to useful material drops and downstream crafting/economy uses
  - Acceptance: each tier has a coherent reward purpose, drop rates are documented, and drops feed at least one useful recipe/shop/progression sink

- [ ] Playtest combat feedback, kill quests, and night spawns
  - Priority: `P1`
  - Area: combat QA
  - Goal: verify the newly wired polish and progression systems in live scenes
  - Acceptance: pooled damage text, pooled afterimages, HP bars, hit flash, hit stop, camera shake, player i-frame, death UI, kill quest progress, autosave toast, and night spawn cleanup all work without console errors

- [ ] Run a progression balance pass for six-enemy combat
  - Priority: `P2`
  - Area: balance
  - Goal: tune HP, defense, damage, cooldowns, EXP, and encounter counts against actual player weapons and healing access
  - Acceptance: Slime1 is an accessible first enemy, Orc3 is difficult but achievable at its intended progression tier, and time-to-kill is documented

- [ ] Expand crop and fruit tree roster from GED
  - Priority: `P1`
  - Area: farming content
  - Goal: move beyond the representative crop slice and cover the broader seasonal farming catalog defined in `GED.md`
  - Files likely touched: `GEDCanonicalGenerator`, localization output, generated crop/tree assets
  - Acceptance: major seasonal crop families and fruit tree families exist as seed item + harvest item + world entity trios

- [ ] Add machine, storage, and placeable coverage where the repo supports it
  - Priority: `P1`
  - Area: utility/building data
  - Goal: extend canonical data for supported placeables such as sprinklers and beds, then add chest/processor archetypes once matching object types or prefabs exist
  - Files likely touched: generator, runtime modules, world object definitions, future prefab/object type mappings
  - Acceptance: supported placeables are fully usable; unsupported archetypes are explicitly tracked instead of half-generated

- [ ] Finish gameplay-facing storage and processor UI flow
  - Priority: `P1`
  - Area: interaction UX
  - Goal: convert the new storage/processor runtime hooks into player-facing windows and actions
  - Files likely touched: UI event subscribers, panel scripts, runtime interaction flow
  - Acceptance: storage and processor entities can be opened and used through actual UI, not only event stubs

- [ ] Refactor `BackpackUI` onto the shared menu shell
  - Priority: `P1`
  - Area: UI architecture and inventory UX
  - Goal: keep the current backpack behavior but align its outer shell and section framing with the shared menu window pattern
  - Files likely touched: `BackpackUI`, shared UI helper(s), maybe `InventoryWindowUI` if section ownership needs clarification
  - Acceptance: the backpack tab keeps its item-grid behavior while dropping duplicated shell/chrome logic

- [ ] Refactor `EquipmentUI` onto the shared menu shell
  - Priority: `P1`
  - Area: UI architecture and equipment UX
  - Goal: align the paper-doll/stat screen with the same shell system without collapsing its slot/stat/preview structure into the backpack form
  - Files likely touched: `EquipmentUI`, shared UI helper(s)
  - Acceptance: equipment keeps its slot-specific structure and preview/stat breakdown while sharing shell styling with other tabs

- [ ] Add canonical validator and audit tooling for generated GED assets
  - Priority: `P1`
  - Area: editor tooling and production readiness
  - Goal: detect missing modules, missing localization, duplicate ids, empty desc keys, missing world object mappings, and unsupported archetypes early
  - Files likely touched: editor generator/validator scripts
  - Acceptance: one validation run reports actionable warnings before content is shipped

- [ ] Complete recipe and progression chains from generated materials
  - Priority: `P1`
  - Area: economy and crafting loop
  - Goal: expand from sample recipes into coherent downstream uses for crops, wood, ore, animal products, and enemy drops
  - Files likely touched: generator, recipe assets, pricing/balance values
  - Acceptance: representative materials clearly flow into craft/process/use outcomes

- [ ] Wire canonical content into shops, starter loadouts, and scene content where safe
  - Priority: `P2`
  - Area: integration
  - Goal: start replacing or supplementing legacy references with canonical generated assets in carefully scoped places
  - Files likely touched: NPC shop data, bootstrap/loadout scripts, marker assets
  - Acceptance: selected gameplay loops use canonical data end-to-end without breaking existing scenes

- [ ] User-owned art and scene hookup list for generated GED assets
  - Priority: `P1`
  - Area: art and scene ownership
  - Goal: keep a clear list of sprite/icon/prefab/tilemap tasks that still require user action after logic/data are complete
  - Files likely touched: handoff notes, dedicated art todo docs if needed
  - Acceptance: generated content leaves behind explicit follow-up items for visuals and scene dressing

## Done

- [x] Claude review bug/perf pass for combat polish
  - Result: pooled floating combat/EXP text, pooled dodge afterimages, hardened enemy death fade timing, safer enemy HP bar and alert binding, smoothed low-HP vignette, safe trail material fallback, validated night spawn placement/cleanup, and set generated enemy EXP source to Combat

- [x] Combat feedback and kill-objective progression baseline
  - Result: added combat telemetry, visual hit feedback, player i-frame/dodge polish, EXP/level/combo/toast/death UI, persistent kill objective progress, converted mixed monster quests to generated enemy ids, temporary night enemy spawning, and autosave-on-day-change

- [x] Add balanced enemies to all mine RuleRegions
  - Result: updated the mine content generator and regenerated all 20 presets with progressive Slime1-3 and Orc1-3 populations, `RespawnTopUp`, tier-scaled delays, and a successful Level 01 Play Mode spawn

- [x] Six-enemy animated combat vertical slice
  - Result: generated directional animation content, controllers, EntityData, prefab variants, world definitions, localization, and spawn marker tiles for Slime1-3 and Orc1-3; added obstacle-aware AI, animation-event attacks, hurt/death handling, delayed despawn, and verified damage plus EXP rewards in Play Mode

- [x] Shared shell helper baseline for menu windows
  - Result: added `MenuWindowShellUI` and applied it to `QuestLogWindowUI` and `SettingsWindowUI`, including compile-safe runtime project wiring

- [x] GED runtime foundations
  - Result: added `SeasonRule`, `Consumable`, `ToolRequirement`, `Storage`, and `Processor` modules/runtimes plus supporting event/tool-tier hooks

- [x] Inventory use-item flow baseline
  - Result: backpack use requests now route through `PlayerBridge`, and consumables/placeables can be identified as usable in the backpack UI

- [x] Canonical generator core playable slice
  - Result: Unity editor can now generate a representative catalog of tools, crops, fruit trees, materials, foods, nodes, animals, enemies, sample recipes, utility placeables, and shop content into the canonical `Resources` tree with VI/EN localization
