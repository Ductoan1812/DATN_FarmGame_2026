# Gameplay Roadmap (Execution Checklist)

## Completed in code

- [x] NPC interaction pipeline via `SecondaryActionEvent` + `InteractionContext`.
- [x] Dialogue graph runtime with Condition/Event/Audio/Portrait node handling.
- [x] Quest runtime + quest log save/load + objective progress by inventory count.
- [x] Shop runtime/service with buy/sell checks, money checks, inventory checks.
- [x] Infinite stock + whitelist/all-items buy rules in `ShopModule`.
- [x] Tool combat path for `Scythe`, `Axe`, `Pickaxe` (shared damage runtime).
- [x] Enemy runtime bridge with chase/attack behavior.
- [x] Scene transition service + spawn point handoff after scene load.
- [x] `load` request now performs real scene reload.
- [x] Debug console commands: `set`, `give`, `spawn`, `save`, `load`, `scene`.

## High-priority data/setup still required in Unity scene/assets

- [ ] Run `Tools/DATN/Gameplay/Bootstrap Core Content` once to generate/update sample gameplay data.
- [ ] Verify `WorldObjectDefinition` entries for `NPCShop01`, `NPCCrafting01`, `NPCEvent01`, `OreNode01`, `Enemy01`.
- [ ] Place/Spawn sample NPC shop + ore node + enemy in playable area.
- [ ] Attach/verify `InteractablePrompt` collider targets for NPC interaction.
- [ ] Assign/verify dialogue graphs + quest graphs on NPC entity assets.
- [ ] Add at least one scene portal entity with `ScenePortalModule` and `SceneSpawnPoint` in target scene.

## Mid-priority gameplay expansion

- [ ] Crafting gameplay module/runtime (currently only structure/NPC type planned).
- [ ] Enemy variety + balancing (HP/ATK/speed/drop rates).
- [ ] Quest reward content balancing (money/items per quest).
- [ ] Multi-scene progression flow (farm -> mine -> town).

## Smoke test loop (manual)

1. `save` -> restart Play -> `load` (confirm state persistence).
2. Talk to NPC -> open dialogue -> open quest/shop branch.
3. Buy item in shop -> money decrease + inventory increase.
4. Sell item in shop -> money increase + inventory decrease.
5. Mine ore with pickaxe -> ore drop -> pickup.
6. Fight enemy -> player takes damage, enemy dies/drops/respawns as configured.
7. Complete quest objective -> complete quest -> receive rewards.
8. Use portal option or `scene <SceneName> <SpawnPointId>` -> verify spawn handoff.
