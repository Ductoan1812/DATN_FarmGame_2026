# Agent Handoff

## Last completed slice

- Summary:
  - implemented core GED runtime foundations for season rules, consumables, tool tier gates, storage hooks, and processor hooks
  - added use-item flow from backpack UI through `PlayerBridge` so consumables and placeables have a gameplay path
  - created `GEDCanonicalGenerator` and executed it inside the currently open Unity Editor via Unity MCP
  - generated canonical assets under `Assets/Project/Resources/Data/Entities` and `Assets/Project/Resources/Data/Recipes`
  - expanded `Assets/Resources/Localization/vi.json` and `Assets/Resources/Localization/en.json`
  - extended the generator to include supported utility/placeable content such as sprinklers and beds
  - improved interaction preview support for `BedModule`, `StorageModule`, and `ProcessorModule`
- Build / verification result:
  - `dotnet build Assembly-CSharp.csproj` passed
  - `dotnet build Assembly-CSharp-Editor.csproj` passed
  - Unity MCP execution of `GEDCanonicalGenerator.GenerateCorePlayableSlice` succeeded in the open editor

## Files changed

- `Assets/Project/Scripts/Core/ModuleEvent.cs`
- `Assets/Project/Scripts/Core/Service/InteractionPreviewService.cs`
- `Assets/Project/Scripts/Core/SystemEvents.cs`
- `Assets/Project/Scripts/Data/Enums/OutOfSeasonBehavior.cs`
- `Assets/Project/Scripts/Data/Module/ConsumableModule.cs`
- `Assets/Project/Scripts/Data/Module/ProcessorModule.cs`
- `Assets/Project/Scripts/Data/Module/SeasonRuleModule.cs`
- `Assets/Project/Scripts/Data/Module/StorageModule.cs`
- `Assets/Project/Scripts/Data/Module/ToolModule.cs`
- `Assets/Project/Scripts/Data/Module/ToolRequirementModule.cs`
- `Assets/Project/Scripts/Data/Runtime/BedRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/ConsumableRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/HealthRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/PlacementRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/ProcessorRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/SeasonRuleRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/StageRumtime.cs`
- `Assets/Project/Scripts/Data/Runtime/StorageRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/ToolRequirementRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/ToolRuntime.cs`
- `Assets/Project/Scripts/Editor/OneOff/GEDCanonicalGenerator.cs`
- `Assets/Project/Scripts/Features/Player/PlayerBridge.cs`
- `Assets/Project/Scripts/UI/BackpackUI.cs`
- `Assets/Project/Scripts/UI/Localization/LocalizationKeys.cs`
- `Assets/Project/Resources/Data/Entities/...`
- `Assets/Project/Resources/Data/Recipes/...`
- `Assets/Resources/Localization/vi.json`
- `Assets/Resources/Localization/en.json`
- `.ai-workflow/active-spec.md`
- `.ai-workflow/master-backlog.md`
- `.ai-workflow/agent-handoff.md`

## Current state

- What is working:
  - canonical generator creates 90+ entity assets and recipe assets in the `Resources` tree
  - generated content includes tools, crops, trees, nodes, foods, animals, enemies, utilities, and sample shop content
  - VI/EN localization files now include generated name and description keys
  - backpack use flow can drive consumables and placement items
  - season placement checks and tool tier checks are active in runtime
- What is partially done:
  - `StorageModule` and `ProcessorModule` only have interaction/runtime event hooks; player-facing UI is not finished
  - the generator currently covers a representative slice, not the full GED catalog
  - unsupported archetypes like chest/fence/full machine roster still need either object type/prefab coverage or a deliberate integration plan
- Known risks or blockers:
  - the worktree already contains many unrelated deletions and legacy asset churn outside the new canonical tree; do not auto-stage broad repo changes blindly
  - batch mode cannot run while the project is open, so use Unity MCP against the live editor for generation during active editor sessions
  - generated assets currently use null icons/sprites and still need user-owned art hookup

## Exact next step

- Continue the `GED canonical content expansion pass`:
  - expand the crop and fruit-tree roster from `Assets/Project/Docs/Design/GED.md`
  - add validator tooling for duplicate ids/missing localization/missing required modules
  - decide which supported placeable/world archetypes can be canonically generated next without inventing unsupported object types

## User-owned follow-up items

- Assign final sprites and icons for generated items and world entities
- Replace placeholder or reused prefabs with final art-prefab setups where needed
- Build or refine scene dressing, tilemap art, and custom animations for the generated content families

## Notes for next unattended run

- Read `.ai-workflow/active-spec.md`
- Read `.ai-workflow/master-backlog.md`
- Keep using Unity MCP against the live editor when the project is already open
- Avoid staging or deleting the large legacy `Assets/Project/ScriptableObjects/...` tree without explicit user approval
