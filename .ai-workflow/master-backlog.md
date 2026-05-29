# Master Backlog

Use this file as the prioritized source of truth for unattended work slices.

## Priority scale

- `P0`: blocking core gameplay or progress
- `P1`: required for a complete playable loop
- `P2`: important polish or clarity improvement
- `P3`: nice-to-have follow-up

## Todo

- [ ] Marker seeding reliability pass
  - Priority: `P0`
  - Area: world seeding, respawn, save/load
  - Goal: verify marker-seeded plants/trees/NPCs persist and respawn correctly without duplicate or incorrect initialization
  - Files likely touched: `SceneContentScanner`, `SpawnSystem`, `SceneSpawnTile`, `SceneSpawnPayload`, related marker assets
  - Acceptance: marker-seeded entities initialize once, random start stage works only on fresh seed/respawn, normal save/load preserves true state

- [ ] Plant taxonomy and validation pass
  - Priority: `P1`
  - Area: prefab and marker correctness
  - Goal: formalize and enforce correct use of `Plant01`, `Plant02`, and `TreeNode`
  - Files likely touched: editor validation/tooling, marker assets, world object definitions, `.md` documentation
  - Acceptance: wrong prefab/type combinations are easier to detect; marker setup rules are documented and actionable

- [ ] Fruit tree and regrow crop data completion pass
  - Priority: `P1`
  - Area: content data, stage loop, harvest/drop consistency
  - Goal: audit and complete data for multi-harvest crops and fruit trees so they behave consistently
  - Files likely touched: `seed_*_tree.asset`, regrow crop assets, workbench/editor helpers
  - Acceptance: loop stages, drops, destroy rules, and harvest methods are internally consistent across affected plants

- [ ] Interaction and feedback readability pass
  - Priority: `P1`
  - Area: player feedback, prompts, micro-response
  - Goal: improve clarity of interactions that already exist so the game communicates state better
  - Files likely touched: prompt scripts, reaction scripts, small gameplay feedback components, UI hooks
  - Acceptance: important interactions are easier to read without changing the core rules

- [ ] Save/load regression checklist for farming world state
  - Priority: `P1`
  - Area: technical robustness
  - Goal: codify and verify the critical saved states for farming objects and tools
  - Files likely touched: `.ai-workflow` docs, save/load helpers, targeted runtime fixes if failures are found
  - Acceptance: a repeatable checklist exists and the main risky states are covered

- [ ] User-owned asset follow-up list
  - Priority: `P1`
  - Area: art, animation, scene dressing
  - Goal: keep a visible list of tasks that Codex cannot finish alone and that require user art or scene work
  - Files likely touched: `.ai-workflow/agent-handoff.md`, dedicated art todo notes if needed
  - Acceptance: every system-complete-but-art-incomplete area leaves behind a clear user action item

## In Progress

- [ ] Marker seeding reliability pass
  - Priority: `P0`
  - Current slice:
    - random start stage is applied only through marker payload on fresh seed / respawn
    - fixed persistence gap for destroyed `Persistent` marker entities by saving removed `persistentId` tombstones in `WorldEntityService`
  - Remaining checks:
    - verify `Persistent` marker destroyed once does not reseed on reload
    - verify `Regenerating` marker still randomizes only on respawn and not on normal load
    - audit marker assets that should use `Persistent` vs `Regenerating`

## Done

- [x] Automation workflow bootstrap
  - Result: `.codex/AGENTS.md` updated, `.ai-workflow` files created, and heartbeat follow-up scheduled

- [x] Farming core loop implementation and stabilization baseline
  - Result: planting, watering, harvest flow, refill flow, water meter UI, and stage/harvest refactors are in place and now need completion-oriented follow-up
