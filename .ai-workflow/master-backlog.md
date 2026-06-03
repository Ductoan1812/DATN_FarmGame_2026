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

- [x] GED runtime foundations
  - Result: added `SeasonRule`, `Consumable`, `ToolRequirement`, `Storage`, and `Processor` modules/runtimes plus supporting event/tool-tier hooks

- [x] Inventory use-item flow baseline
  - Result: backpack use requests now route through `PlayerBridge`, and consumables/placeables can be identified as usable in the backpack UI

- [x] Canonical generator core playable slice
  - Result: Unity editor can now generate a representative catalog of tools, crops, fruit trees, materials, foods, nodes, animals, enemies, sample recipes, utility placeables, and shop content into the canonical `Resources` tree with VI/EN localization
