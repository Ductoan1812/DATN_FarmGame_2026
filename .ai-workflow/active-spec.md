# Active Spec

## Current focus

- Feature or task cluster: Canonical GED data pipeline and playable content generation
- Player-facing outcome: The farm survival game has real `EntityData` content assets with complete ids, localization keys, core stats, and gameplay modules instead of fragmented prototype data.

## Scope

- In scope:
  - canonical `EntityData` generation under `Assets/Project/Resources/Data/Entities`
  - canonical `RecipeData` generation under `Assets/Project/Resources/Data/Recipes`
  - VI/EN localization generation for generated content
  - runtime support for season rules, consumables, tool tier gates, storage, and processor interaction hooks
  - representative playable data slices for tools, crops, trees, nodes, animals, enemies, foods, utilities, and NPC shop content
- Out of scope:
  - final sprite/icon hookup by Codex
  - broad scene art dressing, animation authoring, and tilemap art production
  - full machine/storage UI implementation beyond runtime hooks and interaction prompts

## Constraints

- Technical constraints:
  - canonical assets must live in `Resources` so build-time loading still works
  - manual code edits must use `apply_patch`
  - do not delete or silently overwrite user-owned legacy content trees without explicit approval
- Design constraints:
  - generated descriptions must communicate seasonality, growth time, harvest behavior, and practical use
  - generated archetypes should only target object types and prefab categories that the current repo can actually spawn or place
- Save/load or scene-seeding concerns:
  - new modules must not break existing save/load behavior
  - placed entities generated from canonical placeable items must still use current placement and spawn systems

## Acceptance target

- A canonical generator can run inside the current Unity Editor and create/update usable content assets.
- Generated assets include coherent ids, `keyName`, `descKey`, and representative modules/stats.
- `vi.json` and `en.json` are expanded with generated content keys.
- Core runtime supports consuming items, season placement rules, and tool tier restrictions.

## Validation plan

- Build:
  - `dotnet build Assembly-CSharp.csproj`
  - `dotnet build Assembly-CSharp-Editor.csproj`
- Editor execution:
  - run `GEDCanonicalGenerator.GenerateCorePlayableSlice` in the open Unity Editor
- Output checks:
  - inspect representative generated assets for modules/stats/localization keys
  - confirm canonical asset counts and localization files update
